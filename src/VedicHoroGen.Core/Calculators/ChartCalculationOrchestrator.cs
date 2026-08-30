using VedicHoroGen.Core.Models;

namespace VedicHoroGen.Core.Calculators;

/// <summary>
/// Dispatches a BirthDetails record to every registered IChartCalculator.
/// CreateDefault registers D1 + D2 + D6 + D9 + D10 + D11 (numeric order); later phases add
/// calculators there (or via DI) without changing this class's logic.
/// </summary>
public class ChartCalculationOrchestrator
{
    private readonly List<IChartCalculator> _calculators;

    public ChartCalculationOrchestrator(IEnumerable<IChartCalculator> calculators)
    {
        _calculators = calculators.ToList();
    }

    /// <summary>The registered calculators, in registration order — used by the CLI's backfill-charts / recompute-keydetails to enumerate every chart type that has an IChartCalculator (i.e. everything except VimshottariDasha).</summary>
    public IReadOnlyList<IChartCalculator> Calculators => _calculators;

    /// <summary>Default orchestrator: D1 + D2 + D6 + D9 + D10 + D11 (numeric order). Register new calculators here.</summary>
    public static ChartCalculationOrchestrator CreateDefault() =>
        new(new IChartCalculator[]
        {
            new D1RasiCalculator(), new D2HoraCalculator(), new D6ShashtamsaCalculator(),
            new D9NavamsaCalculator(), new D10DasamsaCalculator(), new D11RudramsaCalculator()
        });

    /// <summary>
    /// Runs every registered calculator, returning both the ChartResult to store (ResultJson) and the
    /// raw ChartAnalysisInput behind it — callers pass the latter straight to ChartAnalyzer.Compute to
    /// get that chart type's KeyDetails/HouseLords/Conjunctions/Aspects rows, without re-deriving
    /// anything or caring which chart type it is. This is the one place a new calculator (D2, D10, ...)
    /// needs to be registered for it to get full analytical-table treatment automatically.
    /// </summary>
    public IReadOnlyList<(ChartResult Result, ChartAnalysisInput Input)> CalculateAll(BirthDetails birthDetails)
    {
        var results = new List<(ChartResult, ChartAnalysisInput)>();
        foreach (var calculator in _calculators)
        {
            var input = calculator.ComputeAnalysisInput(birthDetails);
            var result = calculator.BuildResult(birthDetails, input);
            results.Add((result, input));
        }
        return results;
    }

    /// <summary>
    /// Recomputes just one chart type's analysis input for already-stored birth details — used by the
    /// CLI's `backfill-analytics` mode to re-derive KeyDetails/HouseLords/Conjunctions/Aspects for a
    /// ChartResult that predates a given chart type having them (e.g. D9 rows saved before D9 got the
    /// same analytical tables as D1). Recomputes from Swiss Ephemeris rather than round-tripping the
    /// stored ResultJson, so it works even for old rows whose JSON predates a field this shape now
    /// needs (e.g. NirayanaLongitudeDegrees, added to D9's JSON in the same change that added this).
    /// </summary>
    public ChartAnalysisInput ComputeAnalysisInput(string chartType, BirthDetails birthDetails)
    {
        var calculator = _calculators.FirstOrDefault(c => c.ChartType == chartType)
            ?? throw new InvalidOperationException($"No calculator registered for chart type '{chartType}'.");
        return calculator.ComputeAnalysisInput(birthDetails);
    }
}
