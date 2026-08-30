using System.Text.Json;
using VedicHoroGen.Core.Models;

namespace VedicHoroGen.Core.Calculators;

/// <summary>
/// Computes the D9 (Navamsa) chart: Navamsa Lagna + all 9 planets' Navamsa sign and house
/// (house counted Whole-Sign from the Navamsa Lagna, per <c>AstroMath.CountFromSignToSign</c>).
/// Degree-within-sign and nakshatra/pada are not applicable at the varga level (classical
/// divisional-chart display shows sign only), so those fields are left null for D9.
///
/// The actual computation lives in D9ChartComputer (mirrors D1RasiCalculator/D1ChartComputer's
/// split), shared with ChartAnalyzer so both consume the same data.
/// </summary>
public class D9NavamsaCalculator : IChartCalculator
{
    public string ChartType => "D9";

    public ChartAnalysisInput ComputeAnalysisInput(BirthDetails birthDetails) => D9ChartComputer.Compute(birthDetails);

    public ChartResult BuildResult(BirthDetails birthDetails, ChartAnalysisInput analysisInput)
    {
        var resultJson = JsonSerializer.Serialize(new
        {
            NavamsaLagna = new { Sign = analysisInput.AscendantSign.ToString() },
            Planets = analysisInput.Planets
        }, new JsonSerializerOptions { WriteIndented = true });

        return new ChartResult
        {
            BirthDetailId = birthDetails.Id,
            ChartType = ChartType,
            Ayanamsha = "Lahiri",
            HouseSystem = "WholeSign",
            EngineVersion = "SwissEphNet 2.8.0.2 (Moshier, Lahiri sidereal)",
            ResultJson = resultJson,
            ComputedAt = DateTime.UtcNow
        };
    }
}
