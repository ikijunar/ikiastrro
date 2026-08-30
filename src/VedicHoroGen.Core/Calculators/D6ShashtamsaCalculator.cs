using System.Text.Json;
using VedicHoroGen.Core.Models;

namespace VedicHoroGen.Core.Calculators;

/// <summary>D6 (Shashtamsa) — health. IChartCalculator wrapper over D6ShashtamsaChartComputer; mirrors D9NavamsaCalculator.</summary>
public class D6ShashtamsaCalculator : IChartCalculator
{
    public string ChartType => "D6";

    public ChartAnalysisInput ComputeAnalysisInput(BirthDetails birthDetails) => D6ShashtamsaChartComputer.Compute(birthDetails);

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
