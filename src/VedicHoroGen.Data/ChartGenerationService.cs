using VedicHoroGen.Core.Calculators;
using VedicHoroGen.Core.Dasha;
using VedicHoroGen.Core.Models;

namespace VedicHoroGen.Data;

/// <summary>
/// The one place "compute and store every chart type + Vimshottari Dasha for a persisted BirthDetails"
/// lives. Replaces the pipeline previously copy-pasted in Add.razor and five spots of the CLI.
///
/// Boundaries: the caller has already inserted the BirthDetails row (it has an Id) and resolved
/// place/lat-long. Idempotent per person via delete-first-then-regenerate — a partial failure is
/// fully recovered by calling the same method again (there is no cross-repo DB transaction; the
/// repo layer opens a connection per call, so that would need every write method to accept an
/// injected transaction — deferred, see the plan's Global Constraints).
/// </summary>
public class ChartGenerationService
{
    private readonly ChartCalculationOrchestrator _orchestrator;
    private readonly VimshottariDashaService _dashaService;
    private readonly ChartResultsRepository _chartResultsRepo;
    private readonly ChartKeyDetailsRepository _keyDetailsRepo;
    private readonly ChartHouseLordsRepository _houseLordsRepo;
    private readonly ChartConjunctionsRepository _conjunctionsRepo;
    private readonly ChartAspectsRepository _aspectsRepo;

    public ChartGenerationService(
        ChartCalculationOrchestrator orchestrator, VimshottariDashaService dashaService,
        ChartResultsRepository chartResultsRepo, ChartKeyDetailsRepository keyDetailsRepo,
        ChartHouseLordsRepository houseLordsRepo, ChartConjunctionsRepository conjunctionsRepo,
        ChartAspectsRepository aspectsRepo)
    {
        _orchestrator = orchestrator;
        _dashaService = dashaService;
        _chartResultsRepo = chartResultsRepo;
        _keyDetailsRepo = keyDetailsRepo;
        _houseLordsRepo = houseLordsRepo;
        _conjunctionsRepo = conjunctionsRepo;
        _aspectsRepo = aspectsRepo;
    }

    /// <summary>Every registered chart type + Vimshottari Dasha, replacing whatever exists.</summary>
    public GenerationReport GenerateAll(BirthDetails birthDetails)
    {
        // Delete-first: analytics tables never hold Dasha rows, so a blanket delete is safe;
        // ChartResults are removed per chart type so the VimshottariDasha result row is left for
        // VimshottariDashaService to manage.
        _keyDetailsRepo.DeleteByBirthDetailId(birthDetails.Id);
        _houseLordsRepo.DeleteByBirthDetailId(birthDetails.Id);
        _conjunctionsRepo.DeleteByBirthDetailId(birthDetails.Id);
        _aspectsRepo.DeleteByBirthDetailId(birthDetails.Id);
        foreach (var calc in _orchestrator.Calculators)
            _chartResultsRepo.DeleteByBirthDetailIdAndChartType(birthDetails.Id, calc.ChartType);

        var written = PersistCharts(birthDetails, _orchestrator.CalculateAll(birthDetails));

        _dashaService.ComputeAndStore(birthDetails);
        return new GenerationReport(written, DashaWritten: true, Skipped: Array.Empty<string>());
    }

    /// <summary>Only the chart types this person is currently missing (+ Dasha if missing).</summary>
    public GenerationReport GenerateMissing(BirthDetails birthDetails)
    {
        var existing = _chartResultsRepo.GetByBirthDetailId(birthDetails.Id).Select(r => r.ChartType).ToHashSet();
        var toBuild = _orchestrator.Calculators.Where(c => !existing.Contains(c.ChartType)).Select(c => c.ChartType).ToList();

        var written = new List<string>();
        foreach (var chartType in toBuild)
        {
            var input = _orchestrator.ComputeAnalysisInput(chartType, birthDetails);
            var calc = _orchestrator.Calculators.First(c => c.ChartType == chartType);
            var result = calc.BuildResult(birthDetails, input);
            _chartResultsRepo.InsertAll(new[] { result });   // populates result.Id
            PersistAnalytics(birthDetails.Id, result.Id, input);
            written.Add(chartType);
        }

        var dashaWritten = false;
        if (!existing.Contains(VimshottariDashaCalculator.ChartType)) { _dashaService.ComputeAndStore(birthDetails); dashaWritten = true; }

        var skipped = _orchestrator.Calculators.Select(c => c.ChartType).Where(existing.Contains).ToList();
        return new GenerationReport(written, dashaWritten, skipped);
    }

    /// <summary>Re-derive the 4 analytics tables for ChartResults that already exist (optionally one type).</summary>
    public GenerationReport RecomputeAnalytics(BirthDetails birthDetails, string? chartTypeFilter)
    {
        var results = _chartResultsRepo.GetByBirthDetailId(birthDetails.Id)
            .Where(r => _orchestrator.Calculators.Any(c => c.ChartType == r.ChartType))
            .Where(r => chartTypeFilter is null || r.ChartType == chartTypeFilter)
            .ToList();

        var written = new List<string>();
        foreach (var result in results)
        {
            var input = _orchestrator.ComputeAnalysisInput(result.ChartType, birthDetails);
            _keyDetailsRepo.DeleteByChartResultId(result.Id);
            _houseLordsRepo.DeleteByChartResultId(result.Id);
            _conjunctionsRepo.DeleteByChartResultId(result.Id);
            _aspectsRepo.DeleteByChartResultId(result.Id);
            PersistAnalytics(birthDetails.Id, result.Id, input);
            written.Add(result.ChartType);
        }
        return new GenerationReport(written, DashaWritten: false, Skipped: Array.Empty<string>());
    }

    private List<string> PersistCharts(BirthDetails bd, IReadOnlyList<(ChartResult Result, ChartAnalysisInput Input)> computed)
    {
        _chartResultsRepo.InsertAll(computed.Select(c => c.Result));   // populates each Result.Id
        foreach (var (result, input) in computed)
            PersistAnalytics(bd.Id, result.Id, input);
        return computed.Select(c => c.Result.ChartType).ToList();
    }

    private void PersistAnalytics(int birthDetailId, int chartResultId, ChartAnalysisInput input)
    {
        var (keyDetails, houseLords, conjunctions, aspects) = ChartAnalyzer.Compute(input);
        foreach (var r in keyDetails)    { r.ChartResultId = chartResultId; r.BirthDetailId = birthDetailId; }
        foreach (var r in houseLords)    { r.ChartResultId = chartResultId; r.BirthDetailId = birthDetailId; }
        foreach (var r in conjunctions)  { r.ChartResultId = chartResultId; r.BirthDetailId = birthDetailId; }
        foreach (var r in aspects)       { r.ChartResultId = chartResultId; r.BirthDetailId = birthDetailId; }
        _keyDetailsRepo.InsertAll(keyDetails);
        _houseLordsRepo.InsertAll(houseLords);
        if (conjunctions.Count > 0) _conjunctionsRepo.InsertAll(conjunctions);
        if (aspects.Count > 0) _aspectsRepo.InsertAll(aspects);
    }
}
