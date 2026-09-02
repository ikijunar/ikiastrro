using System.Text.Json;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.SpecialPoints;

namespace Ikiastrro.Core.Calculators;

/// <summary>
/// Computes the D1 (Rasi) chart: Ascendant + all 9 planets' sign, exact degree, nakshatra/pada, and
/// house. The actual computation lives in D1ChartComputer (Swiss Ephemeris-backed), shared with
/// ChartAnalyzer so both consume the same data.
/// </summary>
public class D1RasiCalculator : IChartCalculator
{
    public string ChartType => "D1";

    public ChartAnalysisInput ComputeAnalysisInput(
        BirthDetails birthDetails, IReadOnlyList<SpecialPointSeed>? specialPoints = null)
        => D1ChartComputer.Compute(birthDetails, specialPoints);

    public ChartResult BuildResult(BirthDetails birthDetails, ChartAnalysisInput analysisInput)
    {
        var resultJson = JsonSerializer.Serialize(new
        {
            Ascendant = new { Sign = analysisInput.AscendantSign.ToString() },
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
