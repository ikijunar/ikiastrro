using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.Strength;

public sealed record VargottamaResult(
    string Planet,
    string D1Sign,
    string D9Sign,
    bool IsVargottama);

public static class VargottamaDetector
{
    private static readonly string[] SupportedPoints =
        { "Ascendant", "Sun", "Moon", "Mars", "Mercury", "Jupiter", "Venus", "Saturn", "Rahu", "Ketu" };

    /// <summary>Vargottama means the D1 and D9 signs are identical. Includes Lagna and nodes for display.</summary>
    public static IReadOnlyList<VargottamaResult> Calculate(IReadOnlyList<ChartAnalysisInput> charts)
    {
        var d1 = charts.FirstOrDefault(c => c.ChartType.Equals("D1", StringComparison.OrdinalIgnoreCase));
        var d9 = charts.FirstOrDefault(c => c.ChartType.Equals("D9", StringComparison.OrdinalIgnoreCase));
        if (d1 is null || d9 is null) return Array.Empty<VargottamaResult>();

        var d1ByPlanet = d1.Planets.Where(p => SupportedPoints.Contains(p.Planet, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(p => p.Planet, p => p.Sign, StringComparer.OrdinalIgnoreCase);
        var results = new List<VargottamaResult>();
        foreach (var p in d9.Planets.Where(p => SupportedPoints.Contains(p.Planet, StringComparer.OrdinalIgnoreCase)))
        {
            if (!d1ByPlanet.TryGetValue(p.Planet, out var d1Sign)) continue;
            results.Add(new VargottamaResult(p.Planet, d1Sign, p.Sign,
                string.Equals(d1Sign, p.Sign, StringComparison.OrdinalIgnoreCase)));
        }
        return results;
    }
}
