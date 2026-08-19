using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nocturne.API.Models.Responses;
using Nocturne.API.Multitenancy;
using Nocturne.Core.Models.Authorization;
using Nocturne.Core.Models.Configuration;
using Nocturne.Infrastructure.Cache.Abstractions;
using Nocturne.Infrastructure.Data;
using Nocturne.Infrastructure.Data.Entities;
using Nocturne.Infrastructure.Data.Security;

namespace Nocturne.API.Services.Auth;

/// <summary>
/// Manages a tenant's single public share link: its token, the Public subject's read role, and
/// the 24-hour/full-history scope. Runs on the request-scoped <see cref="NocturneDbContext"/>;
/// the membership tables are not RLS-scoped, so tenant isolation comes from the explicit tenant-id
/// predicate on every query — those predicates must be preserved.
/// </summary>
public interface IShareLinkService
{
    /// <summary>
    /// Reports the link's state. <see cref="ShareLinkDto.Url"/> is always null — only the token's
    /// digest is stored, so the URL is knowable only to the caller of <see cref="RotateAsync"/>.
    /// </summary>
    Task<ShareLinkDto> GetAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Mints a new token, replacing any existing one, and returns the resulting
    /// <see cref="ShareLinkDto.Url"/>. This is the only call that can return the URL.
    /// </summary>
    Task<ShareLinkDto> RotateAsync(Guid tenantId, CancellationToken ct = default);
    Task<ShareLinkDto> DisableAsync(Guid tenantId, CancellationToken ct = default);
    Task<ShareLinkDto> SetFullHistoryAsync(Guid tenantId, bool fullHistory, CancellationToken ct = default);

    /// <summary>
    /// Replace the data categories anonymous viewers can see. <paramref name="scopes"/> must be a
    /// subset of <see cref="TenantPermissions.PublicShareScopes"/>; an empty list leaves the link
    /// live but shares nothing. Any role grant on the Public subject is dropped so these scopes are
    /// authoritative.
    /// </summary>
    Task<ShareLinkDto> SetScopesAsync(Guid tenantId, IReadOnlyList<string> scopes, CancellationToken ct = default);

    /// <summary>
    /// Merges <paramref name="appearance"/> over the stored share appearance and persists it.
    /// Only non-null fields are applied, so a partial payload updates just those aspects; null
    /// fields in the stored value keep whatever was set before. Does not require an active link.
    /// </summary>
    Task<ShareLinkDto> SetAppearanceAsync(Guid tenantId, ShareAppearance appearance, CancellationToken ct = default);
}

/// <inheritdoc />
public sealed class ShareLinkService : IShareLinkService
{
    private const string PublicSubjectName = "Public";
    private const int MaxTokenAttempts = 5;

    private readonly NocturneDbContext _dbContext;
    private readonly IShareTokenGenerator _tokenGenerator;
    private readonly ShareTokenCacheService _shareTokenCache;
    private readonly PublicAccessCacheService _publicAccessCache;
    private readonly ICacheService _cacheService;
    private readonly string _baseDomain;

    public ShareLinkService(
        NocturneDbContext dbContext,
        IShareTokenGenerator tokenGenerator,
        ShareTokenCacheService shareTokenCache,
        PublicAccessCacheService publicAccessCache,
        ICacheService cacheService,
        IOptions<BaseDomainOptions> baseDomain)
    {
        _dbContext = dbContext;
        _tokenGenerator = tokenGenerator;
        _shareTokenCache = shareTokenCache;
        _publicAccessCache = publicAccessCache;
        _cacheService = cacheService;
        _baseDomain = baseDomain.Value.BaseDomain;
    }

    public async Task<ShareLinkDto> GetAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _dbContext.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new InvalidOperationException($"Tenant {tenantId} not found");

        var member = await _dbContext.TenantMembers.AsNoTracking()
            .Include(m => m.MemberRoles)
                .ThenInclude(mr => mr.TenantRole)
            .Include(m => m.Subject)
            .FirstOrDefaultAsync(m => m.TenantId == tenantId
                && m.Subject!.IsSystemSubject && m.Subject.Name == PublicSubjectName, ct);

        return ToDto(tenant, member);
    }

    public async Task<ShareLinkDto> RotateAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new InvalidOperationException($"Tenant {tenantId} not found");
        var member = await GetPublicMemberAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Public subject membership not found");

        var oldTokenHash = tenant.ShareToken;
        var wasEnabled = oldTokenHash != null;
        var newToken = await GenerateUniqueTokenAsync(ct);
        var now = DateTime.UtcNow;

        // On first enable, seed the default public scopes (glucose + statistics) as direct
        // permissions on the Public subject, and default to a 24-hour window. Re-rotation only
        // swaps the token — the owner's chosen scopes and window are preserved.
        if (!wasEnabled)
        {
            member.DirectPermissions = [.. TenantPermissions.DefaultPublicShareScopes];
            member.LimitTo24Hours = true;
        }

        tenant.ShareToken = CredentialHash.ShareToken(newToken);
        tenant.ShareTokenSetAt = now;
        member.SysUpdatedAt = now;

        await _dbContext.SaveChangesAsync(ct);

        if (oldTokenHash != null)
            _shareTokenCache.EvictByHash(oldTokenHash);
        _publicAccessCache.Evict(tenantId);

        // The only moment the URL can be produced: the token itself is not stored.
        return ToDto(tenant, member, newToken);
    }

    public async Task<ShareLinkDto> DisableAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new InvalidOperationException($"Tenant {tenantId} not found");
        var member = await GetPublicMemberAsync(tenantId, ct);
        var oldTokenHash = tenant.ShareToken;

        tenant.ShareToken = null;
        tenant.ShareTokenSetAt = null;

        if (member != null)
        {
            // Remove the Public subject's roles and scopes so anonymous read no longer resolves.
            _dbContext.TenantMemberRoles.RemoveRange(member.MemberRoles);
            member.MemberRoles.Clear();
            member.DirectPermissions = null;
            member.SysUpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(ct);

        if (oldTokenHash != null)
            _shareTokenCache.EvictByHash(oldTokenHash);
        _publicAccessCache.Evict(tenantId);

        return ToDto(tenant, member);
    }

    public async Task<ShareLinkDto> SetFullHistoryAsync(Guid tenantId, bool fullHistory, CancellationToken ct = default)
    {
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new InvalidOperationException($"Tenant {tenantId} not found");
        var member = await GetPublicMemberAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Public subject membership not found");

        member.LimitTo24Hours = !fullHistory;
        member.SysUpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);
        _publicAccessCache.Evict(tenantId);

        return ToDto(tenant, member);
    }

    public async Task<ShareLinkDto> SetScopesAsync(Guid tenantId, IReadOnlyList<string> scopes, CancellationToken ct = default)
    {
        var invalid = scopes.Where(s => !TenantPermissions.PublicShareScopes.Contains(s)).ToList();
        if (invalid.Count > 0)
            throw new ArgumentException($"Invalid public share scopes: {string.Join(", ", invalid)}", nameof(scopes));

        var tenant = await _dbContext.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new InvalidOperationException($"Tenant {tenantId} not found");
        var member = await GetPublicMemberAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Public subject membership not found");

        // Drop any role grant so the chosen scopes are authoritative. This also migrates legacy
        // links (which carried the Viewer role) onto the direct-permission model on first edit.
        if (member.MemberRoles.Count > 0)
        {
            _dbContext.TenantMemberRoles.RemoveRange(member.MemberRoles);
            member.MemberRoles.Clear();
        }

        member.DirectPermissions = scopes.Distinct().ToList();
        member.SysUpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);
        _publicAccessCache.Evict(tenantId);

        return ToDto(tenant, member);
    }

    public async Task<ShareLinkDto> SetAppearanceAsync(Guid tenantId, ShareAppearance appearance, CancellationToken ct = default)
    {
        if (appearance.Validate() is { } error)
            throw new ArgumentException(error, nameof(appearance));

        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new InvalidOperationException($"Tenant {tenantId} not found");
        var member = await GetPublicMemberAsync(tenantId, ct);

        if (tenant.ShareAppearance == null)
        {
            tenant.ShareAppearance = appearance.Serialize();
        }
        else
        {
            // Merge so a partial payload honors earlier fields rather than wiping them.
            var current = ShareAppearance.Deserialize(tenant.ShareAppearance);
            current.MergeWith(appearance);
            tenant.ShareAppearance = current.Serialize();
        }

        await _dbContext.SaveChangesAsync(ct);

        // The status endpoint caches the appearance it serves to anonymous viewers; drop its
        // cached copy so a change reaches the public view immediately rather than after the TTL.
        await _cacheService.RemoveAsync($"status:system:{tenantId}");
        await _cacheService.RemoveAsync($"status:system:{tenantId}:demo");

        return ToDto(tenant, member);
    }

    private Task<TenantMemberEntity?> GetPublicMemberAsync(Guid tenantId, CancellationToken ct) =>
        _dbContext.TenantMembers
            .Include(m => m.MemberRoles)
                .ThenInclude(mr => mr.TenantRole)
            .Include(m => m.Subject)
            .FirstOrDefaultAsync(m => m.TenantId == tenantId
                && m.Subject!.IsSystemSubject && m.Subject.Name == PublicSubjectName, ct);

    /// <summary>
    /// Mints a token whose digest is not already stored. Returns the token itself; the caller
    /// stores <see cref="CredentialHash.ShareToken"/> of it.
    /// </summary>
    private async Task<string> GenerateUniqueTokenAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < MaxTokenAttempts; attempt++)
        {
            var candidate = _tokenGenerator.Generate();
            var candidateHash = CredentialHash.ShareToken(candidate);
            var exists = await _dbContext.Tenants.AnyAsync(t => t.ShareToken == candidateHash, ct);
            if (!exists)
                return candidate;
        }

        throw new InvalidOperationException("Unable to generate a unique share token after several attempts");
    }

    /// <summary>
    /// Projects the share link. <paramref name="token"/> is supplied only by the rotate path, which
    /// has just minted it; every other path can only report that a link exists, because the stored
    /// value is a digest and the URL cannot be reconstructed from it.
    /// </summary>
    private ShareLinkDto ToDto(TenantEntity tenant, TenantMemberEntity? member, string? token = null) => new()
    {
        Enabled = tenant.ShareToken != null,
        Url = token != null ? $"https://{token}.share.{_baseDomain}" : null,
        FullHistory = member is { LimitTo24Hours: false },
        Scopes = ComputeScopes(member),
        LastAccessedAt = tenant.ShareLastAccessedAt,
        Appearance = ResolveAppearance(tenant),
    };

    private static ShareAppearance? ResolveAppearance(TenantEntity tenant)
    {
        var appearance = ShareAppearance.Deserialize(tenant.ShareAppearance);
        return appearance.IsEmpty ? null : appearance;
    }

    /// <summary>
    /// The public-shareable read scopes the Public subject currently resolves to — the union of any
    /// role-granted permissions and direct permissions, narrowed to <see cref="TenantPermissions.PublicShareScopes"/>.
    /// Requires <see cref="TenantMemberEntity.MemberRoles"/> (with their roles) to be loaded.
    /// </summary>
    private static List<string> ComputeScopes(TenantMemberEntity? member)
    {
        if (member == null)
            return [];

        var rolePermissions = member.MemberRoles
            .SelectMany(mr => mr.TenantRole?.Permissions ?? Enumerable.Empty<string>());
        var directPermissions = member.DirectPermissions ?? Enumerable.Empty<string>();

        return rolePermissions
            .Concat(directPermissions)
            .Where(TenantPermissions.PublicShareScopes.Contains)
            .Distinct()
            .ToList();
    }
}
