using System.Text.Json;
using Ikiastrro.Core.Astro;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.SpecialPoints;

namespace Ikiastrro.Core.Calculators;

/// <summary>
/// The one IChartCalculator for every divisional chart. Constructed once per
/// tbl_Rule_VargaScheme row by ChartCalculationOrchestrator.CreateDefault.
/// Delegates position maths to VargaChartComputer + the scheme's IVargaSignRule;
/// stamps the scheme's MethodCode onto ChartResult.VargaMethod. Replaces the
/// bespoke D2/D6/D9/D10/D11 calculators (D1 keeps its own D1RasiCalculator).
/// </summary>
public sealed class VargaCalculator : IChartCalculator
{
    private const string EngineVersionString = "SwissEphNet 2.8.0.2 (Moshier, Lahiri sidereal)";

    private readonly string _chartType;
    private readonly VargaScheme _scheme;
    private readonly IVargaSignRule _rule;

    public VargaCalculator(string chartType, VargaScheme scheme)
    {
        _chartType = chartType;
        _scheme = scheme;
        _rule = VargaSignRuleFactory.For(scheme.SignRuleKey, scheme.DivisionFactor);
    }

    public string ChartType => _chartType;

    public ChartAnalysisInput ComputeAnalysisInput(
        BirthDetails birthDetails, IReadOnlyList<SpecialPointSeed>? specialPoints = null)
    {
        var input = VargaChartComputer.Compute(birthDetails, _scheme.DivisionFactor, _rule, specialPoints);
        return input with { ChartType = _chartType };
    }

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
            ChartType = _chartType,
            Ayanamsha = "Lahiri",
            HouseSystem = "WholeSign",
            EngineVersion = EngineVersionString,
            VargaMethod = _scheme.MethodCode,
            ResultJson = resultJson,
            ComputedAt = DateTime.UtcNow
        };
    }
}
