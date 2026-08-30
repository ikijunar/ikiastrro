namespace VedicHoroGen.Core.LifeArea;

/// <summary>The four life-area groupings the Web workspace is organised around.</summary>
public enum LifeArea { PersonalityHealth, Relationships, Career, Money }

/// <param name="Houses">D1 houses classically ruling this area (1-based).</param>
/// <param name="Karakas">Sthira karaka planets for this area — plain names, matching ChartKeyDetail.Planet.</param>
/// <param name="Vargas">Divisional charts shown on this tab; DefaultVarga is the right-slot default.</param>
public record LifeAreaSpec(int[] Houses, string[] Karakas, string[] Vargas, string DefaultVarga);

/// <summary>
/// Static map of life area → its classical houses / karakas / vargas. Sourced from B.V. Raman,
/// "How to Judge a Horoscope" Vol. 1, p.12-13 (house significations) — the same source basis as the
/// reserved migration 030 (tbl_Dim_HouseSignification). TODO: switch to reading tbl_Dim_* once
/// migration 030 ships and the "engine reads reference tables" decision is made.
/// </summary>
public static class LifeAreaMap
{
    public static readonly IReadOnlyDictionary<LifeArea, LifeAreaSpec> Specs = new Dictionary<LifeArea, LifeAreaSpec>
    {
        [LifeArea.PersonalityHealth] = new(
            Houses: new[] { 1, 6, 8, 3 },
            Karakas: new[] { "Sun", "Moon", "Saturn" },
            Vargas: new[] { "D1", "D6" }, DefaultVarga: "D6"),
        [LifeArea.Relationships] = new(
            Houses: new[] { 7, 5, 2, 11, 4 },
            Karakas: new[] { "Venus", "Jupiter", "Moon", "Mercury" },
            Vargas: new[] { "D9" }, DefaultVarga: "D9"),
        [LifeArea.Career] = new(
            Houses: new[] { 10, 6, 7, 2, 11, 1 },
            Karakas: new[] { "Sun", "Saturn", "Mercury", "Jupiter" },
            Vargas: new[] { "D10" }, DefaultVarga: "D10"),
        [LifeArea.Money] = new(
            Houses: new[] { 2, 11, 9, 5, 12 },
            Karakas: new[] { "Jupiter", "Venus", "Mercury" },
            Vargas: new[] { "D2", "D11" }, DefaultVarga: "D2"),
    };
}
