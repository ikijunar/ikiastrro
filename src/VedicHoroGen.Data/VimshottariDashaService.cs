using System.Text.Json;
using System.Text.Json.Serialization;
using VedicHoroGen.Core.Dasha;
using VedicHoroGen.Core.Models;

namespace VedicHoroGen.Data;

/// <summary>
/// Computes and stores Vimshottari Dasha for a person: a tbl_ChartResults row
/// (ChartType="VimshottariDasha") plus its tbl_Chart_DashaPeriods tree. The dedicated write path
/// for Dasha, per the architecture decision recorded in methods_prodmag.md (2026-08-27) — bypasses
/// ChartCalculationOrchestrator/ChartAnalyzer entirely, since Dasha has no planet-position/house
/// shape for those to work with. Used by both the CLI and the Web app.
/// </summary>
public class VimshottariDashaService
{
    private readonly ChartResultsRepository _chartResultsRepo;
    private readonly DashaPeriodsRepository _dashaPeriodsRepo;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public VimshottariDashaService(ChartResultsRepository chartResultsRepo, DashaPeriodsRepository dashaPeriodsRepo)
    {
        _chartResultsRepo = chartResultsRepo;
        _dashaPeriodsRepo = dashaPeriodsRepo;
    }

    /// <summary>
    /// Computes and (re)stores this person's Dasha. Safe to call again after a birth-time
    /// correction or similar recompute — replaces any existing VimshottariDasha ChartResults row
    /// (and its periods) for this person rather than leaving the old one orphaned. Leaves every
    /// other chart type (D1, D9, ...) for this person untouched. Returns the freshly-computed tree
    /// alongside the stored ChartResult so callers (e.g. the CLI's summary print) don't need to
    /// recompute it a second time just to display it.
    /// </summary>
    public (ChartResult Result, List<DashaPeriod> Tree) ComputeAndStore(BirthDetails birthDetails)
    {
        var tree = VimshottariDashaCalculator.Compute(birthDetails);
        var resultJson = JsonSerializer.Serialize(tree, JsonOptions);

        _dashaPeriodsRepo.DeleteByBirthDetailId(birthDetails.Id);
        _chartResultsRepo.DeleteByBirthDetailIdAndChartType(birthDetails.Id, VimshottariDashaCalculator.ChartType);

        var chartResult = new ChartResult
        {
            BirthDetailId = birthDetails.Id,
            ChartType = VimshottariDashaCalculator.ChartType,
            Ayanamsha = "Lahiri",
            HouseSystem = "N/A",   // no house-system concept applies to Dasha — explicit rather than silently reusing the "WholeSign" default, which would misleadingly imply houses were involved
            EngineVersion = "SwissEphNet 2.8.0.2 (Moshier, Lahiri sidereal) + classical Vimshottari (365.2425 days/year)",
            ResultJson = resultJson,
            ComputedAt = DateTime.UtcNow
        };
        _chartResultsRepo.Insert(chartResult); // populates chartResult.Id

        _dashaPeriodsRepo.InsertTree(chartResult.Id, birthDetails.Id, tree);

        return (chartResult, tree);
    }
}
