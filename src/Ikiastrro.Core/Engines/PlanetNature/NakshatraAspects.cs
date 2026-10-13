namespace Ikiastrro.Core.Engines.PlanetNature;

/// <param name="Planet">The other graha.</param>
/// <param name="Nth">Which constellation from the aspecting planet's own star the other one stands in (1 = the same star).</param>
public sealed record NakshatraAspectLink(string Planet, int Nth);

/// <param name="Aspects">Planets standing in a star this planet aspects.</param>
/// <param name="AspectedBy">Planets whose star-aspect falls on this planet's star.</param>
public sealed record NakshatraAspectReading(IReadOnlyList<NakshatraAspectLink> Aspects, IReadOnlyList<NakshatraAspectLink> AspectedBy);

/// <summary>
/// PVR Vedic Astrology: An Integrated Approach §26.5 "Nakshatra-based Aspects": the Sun and Moon aspect the 14th and 15th
/// constellations from them, Mars the 1st, 3rd, 7th, 8th and 15th, Mercury and Venus the 1st and 15th, Jupiter the 10th, 15th and
/// 19th, Saturn the 3rd, 5th, 15th and 19th. A natural benefic gives good results related to the constellations it aspects and a
/// natural malefic bad ones. Counted inclusively from the planet's own star (the 1st is that star). The book lists the seven
/// planets only, so the nodes neither aspect nor appear.
/// </summary>
public static class NakshatraAspects
{
    private static readonly string[] Stars =
    {
        "Ashwini", "Bharani", "Krittika", "Rohini", "Mrigashira", "Ardra", "Punarvasu", "Pushya", "Ashlesha", "Magha", "Purva Phalguni",
        "Uttara Phalguni", "Hasta", "Chitra", "Swati", "Vishakha", "Anuradha", "Jyeshtha", "Mula", "Purva Ashadha", "Uttara Ashadha",
        "Shravana", "Dhanishta", "Shatabhisha", "Purva Bhadrapada", "Uttara Bhadrapada", "Revati",
    };

    private static readonly Dictionary<string, int[]> Offsets = new()
    {
        ["Sun"] = new[] { 14, 15 }, ["Moon"] = new[] { 14, 15 }, ["Mars"] = new[] { 1, 3, 7, 8, 15 },
        ["Mercury"] = new[] { 1, 15 }, ["Venus"] = new[] { 1, 15 }, ["Jupiter"] = new[] { 10, 15, 19 }, ["Saturn"] = new[] { 3, 5, 15, 19 },
    };

    /// <summary>The nth constellation counted from <paramref name="from"/> (inclusive), as a star name.</summary>
    public static string? Aspected(string from, int nth)
    {
        var i = Array.FindIndex(Stars, s => s.Equals(from, StringComparison.OrdinalIgnoreCase));
        return i < 0 ? null : Stars[(i + nth - 1) % 27];
    }

    /// <param name="stars">Each graha's nakshatra name, by planet.</param>
    public static NakshatraAspectReading Read(string planet, IReadOnlyDictionary<string, string?> stars)
    {
        var aspects = new List<NakshatraAspectLink>();
        var by = new List<NakshatraAspectLink>();
        foreach (var (other, star) in stars)
        {
            if (other == planet || star is null) continue;
            if (Offsets.TryGetValue(planet, out var mine) && stars.GetValueOrDefault(planet) is { } own)
                foreach (var n in mine)
                    if (string.Equals(Aspected(own, n), star, StringComparison.OrdinalIgnoreCase)) aspects.Add(new(other, n));
            if (Offsets.TryGetValue(other, out var theirs) && stars.GetValueOrDefault(planet) is { } mineStar)
                foreach (var n in theirs)
                    if (string.Equals(Aspected(star, n), mineStar, StringComparison.OrdinalIgnoreCase)) by.Add(new(other, n));
        }
        return new NakshatraAspectReading(aspects, by);
    }
}
