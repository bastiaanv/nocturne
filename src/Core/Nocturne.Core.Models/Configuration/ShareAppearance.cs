using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nocturne.Core.Models.Configuration;

/// <summary>
/// The presentation appearance for the tenant's public share link — the subset of the
/// appearance settings an admin can pin for anonymous viewers, so a link shows glucose,
/// colors and times exactly as configured instead of the guest's own preference defaults.
/// Stored as a JSONB blob on the tenant and served to the public view via the status endpoint.
/// Every property is nullable so a PATCH carries only the fields being changed.
/// </summary>
public class ShareAppearance
{
    /// <summary>Shared JSON options for (de)serializing the blob (camelCase / web defaults).</summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Deserializes a stored appearance blob, returning an empty (all-null) instance when the
    /// input is null/blank or cannot be parsed. Never throws — a corrupt blob degrades to defaults.
    /// </summary>
    public static ShareAppearance Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ShareAppearance();
        }

        try
        {
            return JsonSerializer.Deserialize<ShareAppearance>(json, JsonOptions)
                ?? new ShareAppearance();
        }
        catch (JsonException)
        {
            return new ShareAppearance();
        }
    }

    /// <summary>Serializes this instance to the JSONB storage representation.</summary>
    public string Serialize() => JsonSerializer.Serialize(this, JsonOptions);

    // Allowed values for the constrained string preferences (mirror the frontend literal unions).
    private static readonly HashSet<string> AllowedGlucoseUnits = new(StringComparer.Ordinal) { "mg/dl", "mmol" };
    private static readonly HashSet<string> AllowedTimeFormats = new(StringComparer.Ordinal) { "12", "24" };
    private static readonly HashSet<string> AllowedColorSchemes = new(StringComparer.Ordinal) { "system", "light", "dark" };
    private static readonly HashSet<string> AllowedColorThemes = new(StringComparer.Ordinal) { "nocturne", "trio", "aaps", "classic" };
    private static readonly HashSet<string> AllowedRegionFormats = new(StringComparer.Ordinal)
    {
        "",
        "en-US", "en-GB", "en-AU", "en-CA", "en-IE", "en-NZ", "en-ZA",
        "de-DE", "fr-FR", "es-ES", "it-IT", "nl-NL", "pl-PL", "pt-PT", "pt-BR",
        "sv-SE", "nb-NO", "da-DK", "fi-FI", "cs-CZ", "ru-RU", "ja-JP",
    };

    /// <summary>True when no field is set — an empty appearance that adds nothing over defaults.</summary>
    [JsonIgnore]
    public bool IsEmpty =>
        GlucoseUnits == null && TimeFormat == null && ColorScheme == null
        && ColorTheme == null && RegionFormat == null;

    /// <summary>
    /// Merges the non-null fields of <paramref name="incoming"/> into this instance so unset
    /// fields are preserved.
    /// </summary>
    public void MergeWith(ShareAppearance incoming)
    {
        GlucoseUnits = incoming.GlucoseUnits ?? GlucoseUnits;
        TimeFormat = incoming.TimeFormat ?? TimeFormat;
        ColorScheme = incoming.ColorScheme ?? ColorScheme;
        ColorTheme = incoming.ColorTheme ?? ColorTheme;
        RegionFormat = incoming.RegionFormat ?? RegionFormat;
    }

    /// <summary>
    /// Validates the constrained string values. Returns a "field: message" description of the
    /// first invalid value, or null when every present value is acceptable. Null fields are
    /// skipped (a partial payload only validates what it carries).
    /// </summary>
    public string? Validate()
    {
        return Check("glucoseUnits", GlucoseUnits, AllowedGlucoseUnits)
            ?? Check("timeFormat", TimeFormat, AllowedTimeFormats)
            ?? Check("colorScheme", ColorScheme, AllowedColorSchemes)
            ?? Check("colorTheme", ColorTheme, AllowedColorThemes)
            ?? Check("regionFormat", RegionFormat, AllowedRegionFormats);

        static string? Check(string field, string? value, HashSet<string> allowed) =>
            value != null && !allowed.Contains(value)
                ? $"{field}: '{value}' is not allowed. Valid values: {string.Join(", ", allowed)}"
                : null;
    }

    /// <summary>Glucose units: "mg/dl" or "mmol".</summary>
    [JsonPropertyName("glucoseUnits")]
    public string? GlucoseUnits { get; set; }

    /// <summary>Time format: "12" or "24".</summary>
    [JsonPropertyName("timeFormat")]
    public string? TimeFormat { get; set; }

    /// <summary>Color scheme: "system", "light", or "dark".</summary>
    [JsonPropertyName("colorScheme")]
    public string? ColorScheme { get; set; }

    /// <summary>Color theme: "nocturne", "trio", "aaps", or "classic".</summary>
    [JsonPropertyName("colorTheme")]
    public string? ColorTheme { get; set; }

    /// <summary>Regional format as a BCP-47 tag (e.g. "en-GB"); empty string follows the display language.</summary>
    [JsonPropertyName("regionFormat")]
    public string? RegionFormat { get; set; }
}
