using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nocturne.Infrastructure.Data.Entities;

/// <summary>
/// Global routing + settings table mapping a chat platform identity to a Nocturne
/// tenant + user. Replaces the per-tenant chat_identity_links table. Supports
/// multiple links per Discord user (one per tenant), with slug-style routing
/// labels, free-form display names, and a default flag for bare-command resolution.
///
/// Lives in the public schema and is NOT tenant-scoped — every query that needs
/// tenant scoping must add an explicit WHERE tenant_id = @tenantId clause.
/// </summary>
[Table("chat_identity_directory")]
public class ChatIdentityDirectoryEntry
{
    [Key, Column("id")]
    public Guid Id { get; set; }

    [Column("platform"), MaxLength(16)]
    public string Platform { get; set; } = string.Empty;

    [Column("platform_user_id"), MaxLength(256)]
    public string PlatformUserId { get; set; } = string.Empty;

    [Column("platform_channel_id"), MaxLength(256)]
    public string? PlatformChannelId { get; set; }

    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Column("nocturne_user_id")]
    public Guid NocturneUserId { get; set; }

    /// <summary>Slug-style routing key used by /bg &lt;label&gt;. Defaults to tenant slug.</summary>
    [Column("label"), MaxLength(64)]
    public string Label { get; set; } = string.Empty;

    /// <summary>Free-form display name shown in disambiguation UI. Defaults to tenant display name.</summary>
    [Column("display_name"), MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>If true, this link is used when /bg is invoked without a label arg. At most one per (platform, platform_user_id).</summary>
    [Column("is_default")]
    public bool IsDefault { get; set; }

    [Column("display_unit"), MaxLength(8)]
    public string DisplayUnit { get; set; } = "mg/dL";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }
}
