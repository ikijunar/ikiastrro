using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.PlanetaryStates;
using Ikiastrro.Core.Engines.Relationships;
using Ikiastrro.Core.Pipeline;
using Ikiastrro.Core.Engines.Dasha;
using Ikiastrro.Core.Engines.Karakas;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Engines.Strength;

namespace Ikiastrro.Data;

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
    private readonly ChartMultiGrahaConjunctionRepository _multiGrahaConjunctionsRepo;
    private readonly ChartAspectsRepository _aspectsRepo;
    private readonly PlanetaryStateRuleRepository _planetaryStateRuleRepo;
    private readonly PlanetaryStateRepository _planetaryStateRepo;
    private readonly RuleSetRepository _ruleSetRepo;
    private readonly ChartTypeRepository _chartTypeRepo;
    private readonly AyanamsaRuleRepository _ayanamsaRuleRepo;
    private readonly PlanetaryStrengthRepository _planetaryStrengthRepo;
    private readonly BhavaStrengthRepository _bhavaStrengthRepo;
    private readonly VargottamaRepository _vargottamaRepo;
    private readonly YogaInputRepository _yogaInputRepo;

    // The avastha rule/dim rows are the same for the whole GenerateAll/Recompute call — load once.
    private PlanetaryStateRuleSet? _planetaryStateRules;
    private PlanetaryStateRuleSet PlanetaryStateRules => _planetaryStateRules ??= _planetaryStateRuleRepo.GetActiveRuleSet();

    public ChartGenerationService(
        ChartCalculationOrchestrator orchestrator, VimshottariDashaService dashaService,
        ChartResultsRepository chartResultsRepo, ChartKeyDetailsRepository keyDetailsRepo,
        ChartHouseLordsRepository houseLordsRepo, ChartConjunctionsRepository conjunctionsRepo,
        ChartMultiGrahaConjunctionRepository multiGrahaConjunctionsRepo,
        ChartAspectsRepository aspectsRepo,
        PlanetaryStateRuleRepository planetaryStateRuleRepo, PlanetaryStateRepository planetaryStateRepo,
        RuleSetRepository ruleSetRepo, ChartTypeRepository chartTypeRepo,
        AyanamsaRuleRepository ayanamsaRuleRepo,
        PlanetaryStrengthRepository planetaryStrengthRepo,
        BhavaStrengthRepository bhavaStrengthRepo,
        VargottamaRepository vargottamaRepo, YogaInputRepository yogaInputRepo)
    {
        _orchestrator = orchestrator;
        _dashaService = dashaService;
        _chartResultsRepo = chartResultsRepo;
        _keyDetailsRepo = keyDetailsRepo;
        _houseLordsRepo = houseLordsRepo;
        _conjunctionsRepo = conjunctionsRepo;
        _multiGrahaConjunctionsRepo = multiGrahaConjunctionsRepo;
        _aspectsRepo = aspectsRepo;
        _planetaryStateRuleRepo = planetaryStateRuleRepo;
        _planetaryStateRepo = planetaryStateRepo;
        _ruleSetRepo = ruleSetRepo;
        _chartTypeRepo = chartTypeRepo;
        _ayanamsaRuleRepo = ayanamsaRuleRepo;
        _planetaryStrengthRepo = planetaryStrengthRepo;
        _bhavaStrengthRepo = bhavaStrengthRepo;
        _vargottamaRepo = vargottamaRepo;
        _yogaInputRepo = yogaInputRepo;
    }

    private AyanamsaDefinition ResolveAyanamsa(AyanamsaDefinition? requested) =>
        requested ?? _ayanamsaRuleRepo.GetActiveDefault();

    /// <summary>Every registered chart type + Vimshottari Dasha, replacing whatever exists.</summary>
    public GenerationReport GenerateAll(BirthDetails birthDetails, AyanamsaDefinition? ayanamsa = null)
    {
        ayanamsa = ResolveAyanamsa(ayanamsa);
        var activeRuleSetId = _ruleSetRepo.GetActive().Id;
        var codeToChartTypeId = _chartTypeRepo.CodeToId();

        // Delete-first: analytics tables never hold Dasha rows, so a blanket delete is safe;
        // ChartResults are removed per chart type so the VimshottariDasha result row is left for
        // VimshottariDashaService to manage.
        _keyDetailsRepo.DeleteByBirthDetailId(birthDetails.Id);
        _houseLordsRepo.DeleteByBirthDetailId(birthDetails.Id);
        _conjunctionsRepo.DeleteByBirthDetailId(birthDetails.Id);
        _multiGrahaConjunctionsRepo.DeleteByBirthDetailId(birthDetails.Id);  // after pair rows (they FK the groups)
        _aspectsRepo.DeleteByBirthDetailId(birthDetails.Id);
        _planetaryStateRepo.DeleteByBirthDetailId(birthDetails.Id);
        foreach (var calc in _orchestrator.Calculators)
            _chartResultsRepo.DeleteByBirthDetailIdAndChartType(birthDetails.Id, calc.ChartType);

        var written = PersistCharts(birthDetails, _orchestrator.CalculateAll(birthDetails, ayanamsa), activeRuleSetId, codeToChartTypeId, ayanamsa);

        _dashaService.ComputeAndStore(birthDetails, ayanamsa);
        return new GenerationReport(written, DashaWritten: true, Skipped: Array.Empty<string>());
    }

    /// <summary>Only the chart types this person is currently missing (+ Dasha if missing).</summary>
    public GenerationReport GenerateMissing(BirthDetails birthDetails, AyanamsaDefinition? ayanamsa = null)
    {
        ayanamsa = ResolveAyanamsa(ayanamsa);
        var activeRuleSetId = _ruleSetRepo.GetActive().Id;
        var codeToChartTypeId = _chartTypeRepo.CodeToId();

        var existing = _chartResultsRepo.GetByBirthDetailId(birthDetails.Id).Select(r => r.ChartType).ToHashSet();
        var toBuild = _orchestrator.Calculators.Where(c => !existing.Contains(c.ChartType)).Select(c => c.ChartType).ToList();
        var ctx = SwissEphemerisProvider.GetSiderealPositions(birthDetails, ayanamsa);

        var written = new List<string>();
        foreach (var chartType in toBuild)
        {
            var input = _orchestrator.ComputeAnalysisInput(chartType, birthDetails, ayanamsa);
            var calc = _orchestrator.Calculators.First(c => c.ChartType == chartType);
            var result = calc.BuildResult(birthDetails, input);
            result.RuleSetId = activeRuleSetId;
            result.CalculationKind = "PositionChart";
            result.ChartTypeId = codeToChartTypeId[result.ChartType];
            result.AyanamshaDegrees = ctx.AyanamshaDegrees;
            result.SiderealTimeHours = ctx.LocalSiderealTimeHours;
            result.Ayanamsha = (ayanamsa ?? AyanamsaDefinition.Default).DisplayName;
            _chartResultsRepo.InsertAll(new[] { result });   // populates result.Id
            PersistAnalytics(result.Id, input, CharaKarakaByPlanet(ctx), ctx, SwissEphemerisProvider.GetSunTimes(birthDetails), activeRuleSetId,
                input.ChartType == "D1" ? _orchestrator.CalculateAll(birthDetails, ayanamsa).Select(c => c.Input).ToList() : new[] { input });
            written.Add(chartType);
        }

        var dashaWritten = false;
        if (!existing.Contains(VimshottariDashaCalculator.ChartType)) { _dashaService.ComputeAndStore(birthDetails, ayanamsa); dashaWritten = true; }

        var skipped = _orchestrator.Calculators.Select(c => c.ChartType).Where(existing.Contains).ToList();
        return new GenerationReport(written, dashaWritten, skipped);
    }

    /// <summary>Re-derive the 4 analytics tables for ChartResults that already exist (optionally one type).</summary>
    public GenerationReport RecomputeAnalytics(BirthDetails birthDetails, string? chartTypeFilter,
        AyanamsaDefinition? ayanamsa = null)
    {
        ayanamsa = ResolveAyanamsa(ayanamsa);
        var activeRuleSetId = _ruleSetRepo.GetActive().Id;
        var codeToChartTypeId = _chartTypeRepo.CodeToId();

        var results = _chartResultsRepo.GetByBirthDetailId(birthDetails.Id)
            .Where(r => _orchestrator.Calculators.Any(c => c.ChartType == r.ChartType))
            .Where(r => chartTypeFilter is null || r.ChartType == chartTypeFilter)
            .ToList();

        var ctx = SwissEphemerisProvider.GetSiderealPositions(birthDetails, ayanamsa);

        var written = new List<string>();
        foreach (var result in results)
        {
            // This path only re-derives the analytics tables; it does not re-insert ChartResults.
            // Keep the in-memory header fields consistent with the active rule set / chart-type dim
            // so anything reading `result` after this call sees the same stamp GenerateAll writes.
            result.RuleSetId = activeRuleSetId;
            result.CalculationKind = "PositionChart";
            result.ChartTypeId = codeToChartTypeId[result.ChartType];
            result.AyanamshaDegrees = ctx.AyanamshaDegrees;
            result.SiderealTimeHours = ctx.LocalSiderealTimeHours;
            var input = _orchestrator.ComputeAnalysisInput(result.ChartType, birthDetails, ayanamsa);
            _keyDetailsRepo.DeleteByChartResultId(result.Id);
            _houseLordsRepo.DeleteByChartResultId(result.Id);
            _conjunctionsRepo.DeleteByChartResultId(result.Id);
            _multiGrahaConjunctionsRepo.DeleteByChartResultId(result.Id);  // after pair rows (they FK the groups)
            _aspectsRepo.DeleteByChartResultId(result.Id);
            _planetaryStateRepo.DeleteByChartResultId(result.Id);
            PersistAnalytics(result.Id, input, CharaKarakaByPlanet(ctx), ctx, SwissEphemerisProvider.GetSunTimes(birthDetails), activeRuleSetId,
                input.ChartType == "D1" ? _orchestrator.CalculateAll(birthDetails, ayanamsa).Select(c => c.Input).ToList() : new[] { input });
            written.Add(result.ChartType);
        }
        return new GenerationReport(written, DashaWritten: false, Skipped: Array.Empty<string>());
    }

    private List<string> PersistCharts(
        BirthDetails bd, IReadOnlyList<(ChartResult Result, ChartAnalysisInput Input)> computed,
        int activeRuleSetId, IReadOnlyDictionary<string, int> codeToChartTypeId,
        AyanamsaDefinition? ayanamsa)
    {
        // Numeric ayanamsha + local sidereal time are one-per-person; compute once and
        // stamp every chart row (denormalised, same as the ChartType string).
        var ctx = SwissEphemerisProvider.GetSiderealPositions(bd, ayanamsa);
        var selected = ayanamsa ?? AyanamsaDefinition.Default;
        foreach (var (result, _) in computed)
        {
            result.RuleSetId = activeRuleSetId;
            result.CalculationKind = "PositionChart";
            result.ChartTypeId = codeToChartTypeId[result.ChartType];
            result.AyanamshaDegrees = ctx.AyanamshaDegrees;
            result.SiderealTimeHours = ctx.LocalSiderealTimeHours;
            result.Ayanamsha = selected.DisplayName;
        }
        _chartResultsRepo.InsertAll(computed.Select(c => c.Result));   // populates each Result.Id
        var charaKarakaByPlanet = CharaKarakaByPlanet(ctx);
        foreach (var (result, input) in computed)
            PersistAnalytics(result.Id, input, charaKarakaByPlanet, ctx, SwissEphemerisProvider.GetSunTimes(bd), activeRuleSetId, computed.Select(c => c.Input).ToList());
        return computed.Select(c => c.Result.ChartType).ToList();
    }

    /// <summary>
    /// The Jaimini 8-karaka (Ashta) label per graha, from this person's D1 degree-within-sign.
    /// Computed once per person and stamped onto the graha KeyDetail rows of every chart type
    /// (a chara karaka is a whole-life fact, not a per-varga one). Keyed/valued as strings so
    /// it drops straight onto <see cref="ChartKeyDetail.CharaKaraka"/>.
    /// </summary>
    private static IReadOnlyDictionary<string, string> CharaKarakaByPlanet(SiderealPositions ctx)
    {
        var degIn = new Dictionary<PlanetName, double>();
        foreach (var p in new[] { PlanetName.Sun, PlanetName.Moon, PlanetName.Mars, PlanetName.Mercury,
                                  PlanetName.Jupiter, PlanetName.Venus, PlanetName.Saturn, PlanetName.Rahu })
            degIn[p] = ctx.PlanetLongitudes[p] % 30.0;
        return CharaKarakaCalculator.Assign(degIn)
            .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.ToString());
    }

    private void PersistAnalytics(int chartResultId, ChartAnalysisInput input,
        IReadOnlyDictionary<string, string> charaKarakaByPlanet,
        SiderealPositions positions, SunTimes sunTimes, int ruleSetId,
        IReadOnlyList<ChartAnalysisInput>? allCharts = null)
    {
        var (keyDetails, houseLords, conjunctions, aspects) = ChartAnalyzer.Compute(input);
        foreach (var r in keyDetails)
            if (r.PointKind == "Graha" && charaKarakaByPlanet.TryGetValue(r.Planet, out var ck))
                r.CharaKaraka = ck;
        var planetaryStates = PlanetaryStateComputer.Compute(input, keyDetails, PlanetaryStateRules);

        // Multi-graha conjunction groups: derived from the built graha KeyDetail rows (which already
        // carry the stitched DignityStatus / IsCombust). The 2-planet case is a group too.
        var multiGrahaGroups = RelationshipEngine.BuildMultiGrahaConjunctions(input, keyDetails);

        foreach (var r in keyDetails)      r.ChartResultId = chartResultId;
        foreach (var r in houseLords)      r.ChartResultId = chartResultId;
        foreach (var r in conjunctions)    r.ChartResultId = chartResultId;
        foreach (var r in aspects)         r.ChartResultId = chartResultId;
        foreach (var r in planetaryStates) r.ChartResultId = chartResultId;
        foreach (var g in multiGrahaGroups) g.ChartResultId = chartResultId;

        // Order per spec §4.7: KeyDetails -> HouseLords -> Conjunctions -> Groups -> GroupMembers
        // -> Aspects, then a pass to link each pair row to its group by (ChartResultId, SignId).
        _keyDetailsRepo.InsertAll(keyDetails);
        _houseLordsRepo.InsertAll(houseLords);
        if (conjunctions.Count > 0) _conjunctionsRepo.InsertAll(conjunctions);
        if (multiGrahaGroups.Count > 0)
        {
            _multiGrahaConjunctionsRepo.InsertAll(multiGrahaGroups);
            _multiGrahaConjunctionsRepo.LinkPairRows(chartResultId);
        }
        if (aspects.Count > 0) _aspectsRepo.InsertAll(aspects);
        if (planetaryStates.Count > 0) _planetaryStateRepo.InsertAll(planetaryStates);

        // Strength facts are materialised on the D1 row, using the complete generated chart bundle
        // so Saptavargaja can see D1/D2/D3/D7/D9/D12/D30 placements.
        if (input.ChartType.Equals("D1", StringComparison.OrdinalIgnoreCase))
        {
            _planetaryStrengthRepo.DeleteByChartResultId(chartResultId);
            _bhavaStrengthRepo.DeleteByChartResultId(chartResultId);
            _vargottamaRepo.DeleteByChartResultId(chartResultId);
            var charts = allCharts ?? new[] { input };
            var strengths = ShadbalaCalculator.Calculate(charts, positions, sunTimes);
            _yogaInputRepo.Replace(chartResultId, ruleSetId, positions, charts, sunTimes, strengths);
            _planetaryStrengthRepo.InsertAll(chartResultId, ruleSetId, strengths);
            _bhavaStrengthRepo.InsertAll(chartResultId, ruleSetId,
                BhavaBalaCalculator.Calculate(input, strengths));
            _vargottamaRepo.InsertAll(chartResultId, ruleSetId, VargottamaDetector.Calculate(charts));
        }
    }
}
