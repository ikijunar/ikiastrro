using Ikiastrro.Core.Models;
using Ikiastrro.Web.Components.Charts;

namespace Ikiastrro.Web.Tests;

/// <summary>
/// One fixed synthetic chart that feeds every chart-component snapshot. Aries Lagna, a
/// plausible graha layout with a spread of dignities, one retrograde, and a D9 that is
/// vargottama for some grahas and not others.
///
/// It is deliberately hand-built, not run through the engine: snapshot tests verify a
/// component's *rendering*, so the input must not shift when the astrology engine changes.
/// Change this fixture only on purpose, and regenerate every snapshot when you do.
/// </summary>
internal static class ChartFixture
{
    // --- SouthIndianGrid: sign -> dignity-dot glyphs (Ascendant is filtered out upstream) ---
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<GridPlanetGlyph>> GridGlyphs =
        new Dictionary<string, IReadOnlyList<GridPlanetGlyph>>
        {
            ["Aries"]       = new[] { new GridPlanetGlyph("Sun", "exalted", false, false) },
            ["Taurus"]      = new[] { new GridPlanetGlyph("Moon", "exalted", false, false) },
            ["Gemini"]      = new[] { new GridPlanetGlyph("Rahu", null, false, false) },
            ["Cancer"]      = new[] { new GridPlanetGlyph("Jupiter", "exalted", false, false) },
            ["Libra"]       = new[] { new GridPlanetGlyph("Saturn", "exalted", false, false) },
            ["Sagittarius"] = new[] { new GridPlanetGlyph("Ketu", null, false, false) },
            ["Capricornus"] = new[] { new GridPlanetGlyph("Mars", "exalted", false, false) },
            ["Pisces"]      = new[]
            {
                new GridPlanetGlyph("Mercury", "debilitated", true, true),
                new GridPlanetGlyph("Venus", "exalted", false, false),
            },
        };

    // --- MiniGrid: sign -> plain planet names ---
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> GridNames =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["Aries"]       = new[] { "Ascendant", "Sun" },
            ["Taurus"]      = new[] { "Moon" },
            ["Gemini"]      = new[] { "Rahu" },
            ["Cancer"]      = new[] { "Jupiter" },
            ["Libra"]       = new[] { "Saturn" },
            ["Sagittarius"] = new[] { "Ketu" },
            ["Capricornus"] = new[] { "Mars" },
            ["Pisces"]      = new[] { "Mercury", "Venus" },
        };

    // --- PolarWheel: one point per graha + Ascendant, at fixed nirayana longitudes ---
    public static readonly IReadOnlyList<PolarWheel.Point> WheelPoints = new[]
    {
        new PolarWheel.Point("Ascendant", 15, "--accent"),
        new PolarWheel.Point("Sun", 20, "--planet-sun"),
        new PolarWheel.Point("Moon", 45, "--planet-moon"),
        new PolarWheel.Point("Rahu", 75, "--planet-rahu"),
        new PolarWheel.Point("Jupiter", 100, "--planet-jupiter"),
        new PolarWheel.Point("Saturn", 190, "--planet-saturn"),
        new PolarWheel.Point("Ketu", 255, "--planet-ketu"),
        new PolarWheel.Point("Mars", 285, "--planet-mars"),
        new PolarWheel.Point("Venus", 340, "--planet-venus"),
        new PolarWheel.Point("Mercury", 350, "--planet-mercury"),
    };

    public static readonly IReadOnlyList<PolarWheel.Chord> WheelChords = new[]
    {
        new PolarWheel.Chord(20, 190, "--aspect-faint"),   // Sun opposite Saturn
        new PolarWheel.Chord(45, 285, "--aspect-faint"),   // Moon square-ish Mars
    };

    // --- ChartKeyDetail rows for CombinedD1D9Grid + VargottamaStrip ---
    public static readonly IReadOnlyList<ChartKeyDetail> D1 = new[]
    {
        Graha("Ascendant", "Aries"),
        Graha("Sun", "Aries"),
        Graha("Moon", "Taurus"),
        Graha("Mars", "Capricornus"),
        Graha("Mercury", "Pisces"),
        Graha("Jupiter", "Cancer"),
        Graha("Venus", "Pisces"),
        Graha("Saturn", "Libra"),
        Graha("Rahu", "Gemini"),
        Graha("Ketu", "Sagittarius"),
        Point("AL", "Leo", "Arudha"),
        Point("Gulika", "Virgo", "Upagraha"),
    };

    public static readonly IReadOnlyList<ChartKeyDetail> D9 = new[]
    {
        Graha("Ascendant", "Leo"),
        Graha("Sun", "Aries"),          // vargottama
        Graha("Moon", "Scorpio"),
        Graha("Mars", "Capricornus"),   // vargottama
        Graha("Mercury", "Gemini"),
        Graha("Jupiter", "Cancer"),     // vargottama
        Graha("Venus", "Libra"),
        Graha("Saturn", "Aquarius"),
        Graha("Rahu", "Pisces"),
        Graha("Ketu", "Virgo"),
    };

    private static ChartKeyDetail Graha(string planet, string sign) =>
        new() { Planet = planet, Sign = sign, PointKind = "Graha" };

    private static ChartKeyDetail Point(string label, string sign, string kind) =>
        new() { Planet = label, Sign = sign, PointKind = kind };
}
