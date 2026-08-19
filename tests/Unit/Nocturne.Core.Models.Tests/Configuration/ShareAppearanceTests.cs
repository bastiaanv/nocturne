using FluentAssertions;
using Nocturne.Core.Models.Configuration;
using Xunit;

namespace Nocturne.Core.Models.Tests.Configuration;

[Trait("Category", "Unit")]
public class ShareAppearanceTests
{
    // ----- Deserialize -----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deserialize_returns_empty_for_null_or_blank(string? json)
    {
        var appearance = ShareAppearance.Deserialize(json);

        appearance.Should().NotBeNull();
        appearance.IsEmpty.Should().BeTrue();
        appearance.GlucoseUnits.Should().BeNull();
        appearance.ColorScheme.Should().BeNull();
    }

    [Fact]
    public void Deserialize_returns_empty_for_malformed_json()
    {
        ShareAppearance.Deserialize("{not valid json").Should().NotBeNull();
    }

    [Fact]
    public void Serialize_then_Deserialize_round_trips_values()
    {
        var original = new ShareAppearance
        {
            GlucoseUnits = "mmol",
            TimeFormat = "24",
            ColorScheme = "dark",
            ColorTheme = "trio",
            RegionFormat = "en-GB",
        };

        var restored = ShareAppearance.Deserialize(original.Serialize());

        restored.GlucoseUnits.Should().Be("mmol");
        restored.TimeFormat.Should().Be("24");
        restored.ColorScheme.Should().Be("dark");
        restored.ColorTheme.Should().Be("trio");
        restored.RegionFormat.Should().Be("en-GB");
        restored.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Serialize_does_not_include_the_computed_IsEmpty_sentinel()
    {
        var json = new ShareAppearance { GlucoseUnits = "mmol" }.Serialize();

        json.Should().NotContain("isEmpty");
    }

    // ----- MergeWith -----

    [Fact]
    public void MergeWith_preserves_unset_fields()
    {
        var existing = new ShareAppearance { GlucoseUnits = "mmol", TimeFormat = "24" };
        var incoming = new ShareAppearance { GlucoseUnits = "mg/dl" };

        existing.MergeWith(incoming);

        existing.GlucoseUnits.Should().Be("mg/dl"); // overwritten
        existing.TimeFormat.Should().Be("24"); // preserved (incoming was null)
    }

    [Fact]
    public void IsEmpty_is_false_once_any_field_is_set()
    {
        new ShareAppearance { ColorScheme = "system" }.IsEmpty.Should().BeFalse();
        new ShareAppearance().IsEmpty.Should().BeTrue();
    }

    // ----- Validate -----

    [Fact]
    public void Validate_accepts_all_null_fields()
    {
        new ShareAppearance().Validate().Should().BeNull();
    }

    [Fact]
    public void Validate_accepts_valid_values()
    {
        var appearance = new ShareAppearance
        {
            GlucoseUnits = "mmol",
            TimeFormat = "24",
            ColorScheme = "dark",
            ColorTheme = "aaps",
            RegionFormat = "de-DE",
        };

        appearance.Validate().Should().BeNull();
    }

    [Theory]
    [InlineData(nameof(ShareAppearance.GlucoseUnits), "kPa", "glucoseUnits")]
    [InlineData(nameof(ShareAppearance.TimeFormat), "48", "timeFormat")]
    [InlineData(nameof(ShareAppearance.ColorScheme), "sepia", "colorScheme")]
    [InlineData(nameof(ShareAppearance.ColorTheme), "midnight", "colorTheme")]
    [InlineData(nameof(ShareAppearance.RegionFormat), "zz-ZZ", "regionFormat")]
    public void Validate_rejects_invalid_value(string property, string value, string expectedField)
    {
        var appearance = new ShareAppearance();
        typeof(ShareAppearance)
            .GetProperty(property)!
            .SetValue(appearance, value);

        appearance.Validate().Should().NotBeNull().And.Contain(expectedField);
    }
}
