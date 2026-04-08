using Microsoft.EntityFrameworkCore;
using Nocturne.Infrastructure.Data;
using Nocturne.Infrastructure.Data.Entities;

namespace Nocturne.API.Services.Chat;

/// <summary>
/// Tenant-scoped facade over the chat identity directory. Handles claim flows,
/// direct link creation, and pending-link lookups for the current tenant.
/// </summary>
public sealed class ChatIdentityService(
    ChatIdentityDirectoryService directory,
    ChatIdentityPendingLinkService pendingLinks,
    IDbContextFactory<NocturneDbContext> contextFactory,
    ILogger<ChatIdentityService> logger)
{
    public Task<IReadOnlyList<ChatIdentityDirectoryEntry>> GetByTenantAsync(
        Guid tenantId, CancellationToken ct)
        => directory.GetByTenantAsync(tenantId, ct);

    public async Task<ChatIdentityDirectoryEntry> ClaimPendingLinkAsync(
        Guid tenantId, Guid userId, string token, CancellationToken ct)
    {
        var pending = await pendingLinks.TryConsumeAsync(token, ct)
            ?? throw new InvalidOperationException("Token expired or already used");

        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var tenant = await db.Tenants.AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new { t.Slug, t.DisplayName })
            .FirstAsync(ct);

        if (pending.TenantSlug is not null &&
            !string.Equals(pending.TenantSlug, tenant.Slug, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Token does not belong to this tenant");
        }

        var entry = await directory.CreateLinkAsync(
            pending.Platform,
            pending.PlatformUserId,
            tenantId,
            userId,
            suggestedLabel: tenant.Slug,
            suggestedDisplayName: tenant.DisplayName,
            ct);

        logger.LogInformation(
            "Claimed pending link token -> tenant {TenantId}, label {Label}",
            tenantId, entry.Label);

        return entry;
    }

    public async Task<ChatIdentityDirectoryEntry> CreateDirectLinkAsync(
        Guid tenantId, Guid userId, string platform, string platformUserId, CancellationToken ct)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var tenant = await db.Tenants.AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new { t.Slug, t.DisplayName })
            .FirstAsync(ct);

        return await directory.CreateLinkAsync(
            platform, platformUserId, tenantId, userId, tenant.Slug, tenant.DisplayName, ct);
    }

    public Task SetDefaultAsync(Guid tenantId, Guid linkId, CancellationToken ct)
        => directory.SetDefaultAsync(linkId, ct);

    public Task RenameLabelAsync(Guid tenantId, Guid linkId, string newLabel, CancellationToken ct)
        => directory.RenameLabelAsync(linkId, newLabel, ct);

    public Task UpdateDisplayNameAsync(Guid tenantId, Guid linkId, string newDisplayName, CancellationToken ct)
        => directory.UpdateDisplayNameAsync(linkId, newDisplayName, ct);

    public Task RevokeAsync(Guid tenantId, Guid linkId, CancellationToken ct)
        => directory.RevokeAsync(linkId, ct);

    /// <summary>
    /// Read-only lookup for the authorize page. Does NOT consume the token.
    /// </summary>
    public async Task<ChatIdentityPendingLinkView?> GetPendingAsync(string token, CancellationToken ct)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);
        var row = await db.ChatIdentityPendingLinks.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Token == token, ct);
        if (row is null || row.ExpiresAt < DateTime.UtcNow) return null;
        return new ChatIdentityPendingLinkView
        {
            Platform = row.Platform,
            PlatformUserId = row.PlatformUserId,
            TenantSlug = row.TenantSlug,
            Source = row.Source,
        };
    }
}

public class ChatIdentityPendingLinkView
{
    public string Platform { get; set; } = string.Empty;
    public string PlatformUserId { get; set; } = string.Empty;
    public string? TenantSlug { get; set; }
    public string Source { get; set; } = string.Empty;
}
