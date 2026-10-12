using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Dasha;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.Engines.Strength;
using Ikiastrro.Core.Engines.Transits;
using Ikiastrro.Core.LifeMatters;
using Ikiastrro.Core.LifeMatters.Activation;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Data.Statistics;

/// <summary>An application-facing timing result plus any facts that could not be loaded.</summary>
public sealed record LifeMatterTimingDataResult(
    int BirthDetailId,
    int? LifeMatterId,
    string LifeMatterCode,
    DateTime AsOfUtc,
    LifeMatterTimingResult? Timing,
    IReadOnlyList<string> MissingData)
{
    public bool IsEvaluated => Timing is not null;
}

/// <summary>Pure conversion from persisted chart rows to the Core timing inputs.</summary>
public static class LifeMatterTimingDataAdapter
{
    public static IReadOnlyDictionary<string, DashaMatterChart> BuildCharts(
        IEnumerable<ChartResult> results,
        Func<int, IReadOnlyList<ChartKeyDetail>> loadDetails)
    {
        var charts = new Dictionary<string, DashaMatterChart>(StringComparer.OrdinalIgnoreCase);
        foreach (var result in results.Where(result => result.ChartType.StartsWith('D')))
        {
            var details = loadDetails(result.Id);
            var ascendant = details.FirstOrDefault(row =>
                row.PointKind == "Graha" && row.Planet == "Ascendant");
            if (ascendant is null || !Enum.TryParse<ZodiacName>(ascendant.Sign, out var ascendantSign))
                continue;

            var planets = details.Where(row => row.PointKind == "Graha")
                .Select(ToPosition).ToList();
            var points = details.Where(row => row.PointKind != "Graha" &&
                                              Enum.TryParse<ZodiacName>(row.Sign, out _))
                .GroupBy(row => row.Planet, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key,
                    group => Enum.Parse<ZodiacName>(group.First().Sign),
                    StringComparer.OrdinalIgnoreCase);
            charts[result.ChartType] = new DashaMatterChart(
                new ChartAnalysisInput(result.ChartType, ascendantSign, planets), points);
        }
        return charts;
    }

    public static GocharaNatalContext? BuildNatalContext(
        IReadOnlyList<ChartKeyDetail> d1, IReadOnlyDictionary<PlanetName, StrengthTier>? planetStrength = null,
        IReadOnlyDictionary<int, StrengthTier>? houseStrength = null)
    {
        var ascendant = d1.FirstOrDefault(row => row.PointKind == "Graha" && row.Planet == "Ascendant");
        if (ascendant is null || !Enum.TryParse<ZodiacName>(ascendant.Sign, out var ascendantSign))
            return null;

        var natalSigns = d1.Where(row => row.PointKind == "Graha" && row.Planet != "Ascendant")
            .Select(row => (Parsed: Enum.TryParse<PlanetName>(row.Planet, out var planet) &&
                                   Enum.TryParse<ZodiacName>(row.Sign, out var sign),
                            Planet: Enum.TryParse<PlanetName>(row.Planet, out var p) ? p : default,
                            Sign: Enum.TryParse<ZodiacName>(row.Sign, out var s) ? s : default))
            .Where(item => item.Parsed)
            .ToDictionary(item => item.Planet, item => item.Sign);
        var houseLords = Enumerable.Range(1, 12).ToDictionary(
            house => house,
            house => Enum.Parse<PlanetName>(HouseEngine.GetSignLord(
                HouseEngine.GetHouseSign(ascendantSign, house))));
        return new GocharaNatalContext(ascendantSign, houseLords, natalSigns,
            planetStrength ?? new Dictionary<PlanetName, StrengthTier>(),
            houseStrength ?? new Dictionary<int, StrengthTier>());
    }

    public static IReadOnlyList<GocharaPlanetQualifier> QualifyTransits(
        IEnumerable<PlanetTransitSnapshot> snapshots,
        GocharaNatalContext natal,
        GocharaDashaLords dasha) =>
        snapshots.Where(snapshot =>
                snapshot.Planet is PlanetName.Jupiter or PlanetName.Saturn or PlanetName.Rahu or PlanetName.Ketu &&
                snapshot.SignId is >= 1 and <= 12)
            .Select(snapshot => GocharaQualifierBuilder.Qualify(
                snapshot.Planet, (ZodiacName)(snapshot.SignId - 1), natal, dasha))
            .ToList();

    private static PlanetPosition ToPosition(ChartKeyDetail row) => new()
    {
        Planet = row.Planet,
        PointKind = row.PointKind,
        Sign = row.Sign,
        DegreesInSign = row.DegreesInSignDisplay ?? string.Empty,
        NirayanaLongitudeDegrees = row.NirayanaLongitudeDegrees,
        VargaLongitudeDegrees = row.VargaLongitudeDegrees,
        EclipticLatitudeDegrees = row.EclipticLatitudeDegrees,
        SpeedLongitudeDegPerDay = row.SpeedLongitudeDegPerDay,
        Nakshatra = row.Nakshatra,
        NakshatraPada = row.NakshatraPada,
        HouseNumber = row.HouseNumberFromLagna,
        IsRetrograde = row.IsRetrograde,
    };
}

/// <summary>
/// Loads one saved person's persisted LifeMatter timing inputs and invokes the Core orchestrator.
/// The relevant varga is evaluated independently and attached to the D1 promise before activation;
/// missing varga facts remain explicit and leave that stage NotEvaluated.
/// </summary>
public sealed class LifeMatterTimingDataService(
    LifeMatterPromiseService promises,
    LifeMatterReferenceRepository references,
    LifeMatterFocusRepository focusRepository,
    KarakaMatterRepository karakaRepository,
    ChartResultsRepository chartResults,
    ChartKeyDetailsRepository keyDetails,
    DashaPeriodsRepository dashaPeriods,
    GocharaRepository gochara,
    PlanetaryStrengthRepository planetaryStrength,
    BhavaStrengthRepository bhavaStrength,
    AshtakavargaRepository ashtakavarga,
    AmsabalaRepository amsabala,
    ArgalaFactRepository argala,
    NaisargikaKarakaRepository naisargika)
{
    private const byte RuleSetId = 1;
    private static readonly LifeMatterFocusResolver FocusResolver = new();

    public LifeMatterTimingDataResult Read(int birthDetailId, string lifeMatterCode, DateTime asOfUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(birthDetailId);
        ArgumentException.ThrowIfNullOrWhiteSpace(lifeMatterCode);
        var utc = DateTime.SpecifyKind(asOfUtc, DateTimeKind.Utc);
        var missing = new List<string>();

        var step = references.GetAllSteps(RuleSetId).FirstOrDefault(row =>
            string.Equals(row.LifeMatterCode, lifeMatterCode, StringComparison.OrdinalIgnoreCase));
        if (step is null)
            return Missing(null, $"LifeMatter {lifeMatterCode} is not active in rule set {RuleSetId}.");

        var reading = promises.ReadAll(birthDetailId)?.FirstOrDefault(row =>
            string.Equals(row.LifeMatterCode, lifeMatterCode, StringComparison.OrdinalIgnoreCase));
        if (reading?.Primary is null)
            return Missing(step.LifeMatterId, reading?.Note ?? "No persisted D1 promise is available.");

        var focus = FocusResolver.Resolve(RuleSetId, step.LifeMatterId,
            focusRepository.GetSubjects(RuleSetId), karakaRepository.GetForRuleSet(RuleSetId),
            focusRepository.GetFoci(RuleSetId));
        var storedResults = chartResults.GetByBirthDetailId(birthDetailId);
        var charts = LifeMatterTimingDataAdapter.BuildCharts(storedResults, keyDetails.GetByChartResultId);
        if (!charts.TryGetValue("D1", out var d1))
            return Missing(step.LifeMatterId, "The persisted D1 chart is missing or incomplete.");

        var requiredChart = focus.Subject?.ChartTypeCode ?? "D1";
        if (!charts.ContainsKey(requiredChart))
            missing.Add($"The relevant {requiredChart} chart is missing; its dasha rules cannot be evaluated.");
        var promise = reading.Primary.Promise;
        if (!string.Equals(requiredChart, "D1", StringComparison.OrdinalIgnoreCase) &&
            charts.ContainsKey(requiredChart))
        {
            var domainResult = storedResults.First(result =>
                string.Equals(result.ChartType, requiredChart, StringComparison.OrdinalIgnoreCase));
            var domainDetails = keyDetails.GetByChartResultId(domainResult.Id);
            var domainAscendant = domainDetails.FirstOrDefault(row =>
                row.PointKind == "Graha" && row.Planet == "Ascendant")?.Sign;
            if (domainAscendant is null)
                missing.Add($"The relevant {requiredChart} chart has no persisted Ascendant; varga support was not evaluated.");
            else
            {
                var shadbala = planetaryStrength.GetSummaryByBirthDetailId(birthDetailId);
                var domainStats = new LifeMatterStatistics(requiredChart, domainAscendant,
                    ashtakavarga.GetByBirthDetailId(birthDetailId), [], shadbala,
                    ArgalaFacts.ForChart(argala.GetByBirthDetailId(birthDetailId), requiredChart,
                        domainAscendant, domainDetails.Where(row => row.PointKind == "Graha")), null, amsabala.GetByBirthDetailId(birthDetailId));
                var naturalRules = naisargika.LoadActive();
                var domain = LifeMatterVargaConfirmationAdapter.Read(reading.Primary, focus,
                    requiredChart, domainDetails, domainAscendant, domainStats, shadbala,
                    LifeMatterPromiseAdapter.Significations(naturalRules.Details),
                    LifeMatterPromiseAdapter.HouseMatters(naturalRules.Houses));
                promise = promise with { Domain = domain };
            }
        }

        var availableRules = DashaMatters.EvaluateExtended(charts);

        var dasha = dashaPeriods.GetLordsOnDate(birthDetailId, utc.Date);
        if (dasha is null)
            return Missing(step.LifeMatterId, $"No stored Vimshottari period covers {utc:yyyy-MM-dd}.", missing);

        var d1Result = storedResults.First(result => result.ChartType == "D1");
        var planetBands = planetaryStrength.GetSummaryByBirthDetailId(birthDetailId)
            .Where(row => Enum.TryParse<PlanetName>(row.Planet, true, out _))
            .ToDictionary(row => Enum.Parse<PlanetName>(row.Planet, true),
                row => StrengthBands.ShadbalaPercentOfMinimum.Classify(row.PercentOfMinimum));
        var rupas = IndependentBhavaBala.RupasByHouse(
            bhavaStrength.GetComponentsByBirthDetailId(birthDetailId));
        var houseBands = new Dictionary<int, StrengthTier>();
        if (rupas.Count >= 2)
        {
            var values = rupas.Values.Select(value => (double)value).ToList();
            var mean = values.Average();
            var sd = Math.Sqrt(values.Sum(value => (value - mean) * (value - mean)) / values.Count);
            foreach (var (house, value) in rupas)
                houseBands[house] = StrengthBands.IndependentBhavaBalaZ.Classify(
                    sd == 0 ? 0m : (decimal)(((double)value - mean) / sd));
        }
        var natal = LifeMatterTimingDataAdapter.BuildNatalContext(
            keyDetails.GetByChartResultId(d1Result.Id), planetBands, houseBands);
        if (natal is null)
            return Missing(step.LifeMatterId, "The persisted D1 natal context is incomplete.", missing);

        var transits = LifeMatterTimingDataAdapter.QualifyTransits(gochara.GetSnapshots(utc), natal, dasha);
        var timing = LifeMatterTimingOrchestrator.Evaluate(new(
            promise, focus, availableRules, dasha, transits));
        missing.AddRange(timing.Selection.MissingMappings);
        return new LifeMatterTimingDataResult(birthDetailId, step.LifeMatterId,
            step.LifeMatterCode, utc, timing, missing.Distinct().ToList());

        LifeMatterTimingDataResult Missing(int? matterId, string note, IEnumerable<string>? prior = null) =>
            new(birthDetailId, matterId, lifeMatterCode, utc, null,
                [.. (prior ?? []), note]);
    }
}
