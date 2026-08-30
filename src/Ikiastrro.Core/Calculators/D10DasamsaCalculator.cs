using System.Text.Json;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Calculators;

/// <summary>D10 (Dasamsa) — career. IChartCalculator wrapper over D10DasamsaChartComputer; mirrors D9NavamsaCalculator.</summary>
public class D10DasamsaCalculator : IChartCalculator
{
    public string ChartType => "D10";

    public ChartAnalysisInput ComputeAnalysisInput(BirthDetails birthDetails) => D10DasamsaChartComputer.Compute(birthDetails);

    public ChartResult BuildResult(BirthDetails birthDetails, ChartAnalysisInput analysisInput)
    {
        var resultJson = JsonSerializer.Serialize(new
        {
            VargaLagna = new { Sign = analysisInput.AscendantSign.ToString() },
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
