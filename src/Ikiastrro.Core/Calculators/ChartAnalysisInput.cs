using Ikiastrro.Core.Astro;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Calculators;

/// <summary>
/// Raw computed chart data for ANY chart type — Ascendant sign + every chart point (planets, plus
/// the Ascendant itself as a "planet" entry) — the one shape every IChartCalculator hands to
/// ChartAnalyzer. Replaces the old D1-only "D1ChartData": D1 and D9 both produce this today, and it's
/// the shape any future divisional chart (D2, D10, D12, ...) produces too, so ChartAnalyzer's
/// dignity/lordship/conjunction/aspect logic is written once and never touched again.
/// </summary>
public record ChartAnalysisInput(string ChartType, ZodiacName AscendantSign, List<PlanetPosition> Planets)
{
    /// <summary>AL, the 12 Bhava Arudhas, HL, Gulika, Maandi — each projected into THIS
    /// chart's zodiac with the same IVargaSignRule as the planets. Position only (Sign /
    /// VargaLongitudeDegrees / HouseNumber); no dignity/nakshatra/combustion/aspects/karaka.
    /// Empty when special points were not supplied (older callers, verify-vargas).</summary>
    public IReadOnlyList<PlanetPosition> SpecialPoints { get; init; } = Array.Empty<PlanetPosition>();
}
