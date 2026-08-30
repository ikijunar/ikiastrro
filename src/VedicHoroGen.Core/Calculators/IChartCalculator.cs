using VedicHoroGen.Core.Models;

namespace VedicHoroGen.Core.Calculators;

/// <summary>
/// One computable chart/analysis type. Each future addition (D2, D10, Vimshottari Dasha,
/// Ashtakavarga, yoga detection, ...) is a new class implementing this interface —
/// existing calculators are never touched.
///
/// Split into two steps (not one "Calculate") so callers (ChartCalculationOrchestrator, and via it
/// the CLI/Web write paths) can hand the same raw ComputeAnalysisInput result to both BuildResult
/// (for the stored ChartResult.ResultJson) and ChartAnalyzer (for the shared KeyDetails/HouseLords/
/// Conjunctions/Aspects tables) without recomputing the chart or parsing it back out of JSON — this
/// is the "standard operation" every chart type plugs into, D1 and D9 included.
/// </summary>
public interface IChartCalculator
{
    /// <summary>e.g. "D1", "D9" — matches ChartResult.ChartType.</summary>
    string ChartType { get; }

    /// <summary>Computes the chart's Ascendant + planet placements for the given birth details, using its EffectiveTimeOfBirth.</summary>
    ChartAnalysisInput ComputeAnalysisInput(BirthDetails birthDetails);

    /// <summary>Packages an already-computed analysis input into the ChartResult row to store (ResultJson shape is chart-type-specific).</summary>
    ChartResult BuildResult(BirthDetails birthDetails, ChartAnalysisInput analysisInput);
}
