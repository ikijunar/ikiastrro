using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.LifeMatters;
using Ikiastrro.Data;
using Ikiastrro.Data.Statistics;
using Ikiastrro.Web.Components.Charts;

namespace Ikiastrro.Web.Components.LifeMatters;

/// <summary>One marriage matter read from Lagna in the main birth chart (D1): the figure is the same
/// Key Inference strength % for the matter's own houses, and <see cref="Houses"/> names them.</summary>
public sealed record MarriageMatterRow(string Matter, string Houses, int? Percent);

/// <summary>A person's marriage area from Key Inference: the area overview figure and each matter.</summary>
/// <param name="Seventh">PVR step 5 read on the 7th house (Ch. 13): which planets support or obstruct it.
/// Null when the chart lacks one of the nine grahas.</param>
public sealed record MarriageMatters(int? AreaPercent, string AreaHouses, IReadOnlyList<MarriageMatterRow> Matters,
    TargetInfluenceReading? Seventh = null);

/// <summary>
/// The "Marriage" area of Key Inference (tbl_Rule_LifeMatterReference, category MARRIAGE_SPOUSE) read
/// for one saved person from Lagna in D1, for the Compatibility page. It reuses
/// <see cref="LifeMatterStatistics"/> and the focus rules exactly as Key Inference does; it adds no
/// rule and no verdict of its own. Null when the person has no stored D1 or strength facts.
/// </summary>
public sealed class MarriageMattersReader(
    ChartResultsRepository chartResults,
    ChartKeyDetailsRepository keyDetails,
    LifeMatterReferenceRepository references,
    LifeMatterFocusRepository foci,
    KarakaMatterRepository karakaMatters,
    PlanetaryStrengthRepository strength,
    AshtakavargaRepository ashtakavarga,
    BhavaStrengthRepository bhava,
    AmsabalaRepository amsabala,
    ArgalaFactRepository argala)
{
    private const byte RuleSetId = 1;
    private const string Area = "MARRIAGE_SPOUSE";
    private static readonly LifeMatterFocusResolver Resolver = new();

    public MarriageMatters? Read(int birthDetailId)
    {
        var d1 = chartResults.GetByBirthDetailId(birthDetailId).FirstOrDefault(c => c.ChartType == "D1");
        if (d1 is null) return null;
        var grahas = keyDetails.GetByChartResultId(d1.Id);
        var asc = grahas.FirstOrDefault(k => k.Planet == "Ascendant")?.Sign;
        var steps = references.GetAllSteps(RuleSetId).Where(s => s.CategoryCode == Area).OrderBy(s => s.DisplayOrder).ToList();
        if (asc is null || steps.Count == 0) return null;

        var subjects = foci.GetSubjects(RuleSetId);
        var focusRules = foci.GetFoci(RuleSetId);
        var karakas = karakaMatters.GetForRuleSet(RuleSetId);
        var resolved = steps.ToDictionary(s => s.LifeMatterId,
            s => Resolver.Resolve(RuleSetId, s.LifeMatterId, subjects, karakas, focusRules));

        var stats = new LifeMatterStatistics("D1", asc, ashtakavarga.GetByBirthDetailId(birthDetailId),
            bhava.GetSummaryByBirthDetailId(birthDetailId), strength.GetSummaryByBirthDetailId(birthDetailId),
            ArgalaFacts.ForChart(argala.GetByBirthDetailId(birthDetailId), "D1", asc, grahas),
            bhava.GetComponentsByBirthDetailId(birthDetailId), amsabala.GetByBirthDetailId(birthDetailId));

        string Sign(int house) => LifeMatterSignMath.ResolveHouseSign(asc, house);
        static string Ord(int n) => n switch { 1 => "1st", 2 => "2nd", 3 => "3rd", _ => $"{n}th" };

        // Area overview: the area's most-focused Lagna house, as Key Inference's area figure does.
        var counts = resolved.Values.SelectMany(r => r.HouseAndSpecialPointFoci)
            .Where(f => f.FocusKind == LifeMatterFocusKind.House && f.ReferenceCode == "LAGNA" && f.HouseNumber is not null)
            .GroupBy(f => f.HouseNumber!.Value).Select(g => (House: g.Key, Count: g.Count())).ToList();
        var core = counts.Count == 0 ? [1] : counts.Where(c => c.Count == counts.Max(x => x.Count)).Select(c => c.House).Order().ToList();
        var area = stats.ForSign(Sign(core[0])).StrengthPercent;

        var rows = new List<MarriageMatterRow>();
        foreach (var step in steps)
        {
            var houses = resolved[step.LifeMatterId].HouseAndSpecialPointFoci
                .Where(f => f.FocusKind == LifeMatterFocusKind.House && f.ReferenceCode == "LAGNA" && f.HouseNumber is not null)
                .Select(f => f.HouseNumber!.Value).Distinct().ToList();
            if (houses.Count == 0) continue;
            var karakaPlanets = resolved[step.LifeMatterId].Karakas
                .Select(k => k.KarakaCode.StartsWith("GRAHA_", StringComparison.Ordinal)
                    ? grahas.FirstOrDefault(g => string.Equals(g.Planet, k.KarakaCode["GRAHA_".Length..], StringComparison.OrdinalIgnoreCase))?.Planet
                    : k.KarakaCode.StartsWith("KARAKA_", StringComparison.Ordinal)
                        ? grahas.FirstOrDefault(g => string.Equals(g.CharaKaraka, k.KarakaCode["KARAKA_".Length..], StringComparison.OrdinalIgnoreCase))?.Planet
                        : null)
                .Where(p => p is not null).Select(p => p!).ToList();
            var pct = LifeMatterStatistics.Mean(houses.Select(h => stats.ForSign(Sign(h), karakaPlanets).StrengthPercent));
            rows.Add(new MarriageMatterRow(step.MatterText, string.Join(" & ", houses.Select(Ord)) + " house", pct));
        }

        // PVR step 5 on the 7th house, with the marriage matters' karakas (Venus, Jupiter, the Darakaraka...).
        var placements = new Dictionary<PlanetName, ZodiacName>();
        foreach (var g in grahas.Where(k => k.PointKind == "Graha"))
            if (Enum.TryParse<PlanetName>(g.Planet, out var planet) && Enum.TryParse<ZodiacName>(g.Sign, out var sign))
                placements[planet] = sign;
        TargetInfluenceReading? seventh = null;
        if (placements.Count >= 9)
        {
            var target = Enum.Parse<ZodiacName>(Sign(7));
            var karakaNames = resolved.Values.SelectMany(r => r.Karakas)
                .Select(k => k.KarakaCode.StartsWith("GRAHA_", StringComparison.Ordinal) ? k.KarakaCode["GRAHA_".Length..]
                    : k.KarakaCode.StartsWith("KARAKA_", StringComparison.Ordinal)
                        ? grahas.FirstOrDefault(g => string.Equals(g.CharaKaraka, k.KarakaCode["KARAKA_".Length..], StringComparison.OrdinalIgnoreCase))?.Planet
                        : null);
            var karakaSet = Planets(karakaNames);
            var arg = stats.ForSign(target.ToString()).Argala;
            seventh = TargetInfluences.Read(Enum.Parse<ZodiacName>(asc), placements, target, karakaSet,
                Planets(arg.Pairs.Where(p => p.Verdict is ArgalaVerdict.Holds or ArgalaVerdict.Contested).SelectMany(p => p.ArgalaPlanets)),
                Planets(arg.Pairs.SelectMany(p => p.ObstructingPlanets)), Ikiastrro.Core.Engines.Houses.MoonPhase.Of(grahas));
        }
        return new MarriageMatters(area, string.Join(" & ", core.Select(Ord)) + " house", rows, seventh);
    }

    private static List<PlanetName> Planets(IEnumerable<string?> names) =>
        names.Select(n => Enum.TryParse<PlanetName>(n, true, out var p) ? p : (PlanetName?)null)
            .Where(p => p is not null).Select(p => p!.Value).Distinct().ToList();
}
