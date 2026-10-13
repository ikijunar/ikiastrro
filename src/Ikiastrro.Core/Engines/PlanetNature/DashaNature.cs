using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Dasha;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.Engines.Strength;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Engines.PlanetNature;

/// <param name="Level">Mahadasha, Antardasha, Pratyantardasha, Sookshma, Prana, Deha, or "Level 7" (the classics name six).</param>
public sealed record RunningLord(string Level, string Planet, DateTime End);

/// <summary>The Vimshottari lords running on a date, read from the stored dasha tree.</summary>
public static class RunningDasha
{
    private static readonly string[] Levels = { "Mahadasha", "Antardasha", "Pratyantardasha", "Sookshma", "Prana", "Deha", "Level 7" };
    private const int StoredLevels = 3;
    private const double DaysPerYear = 365.2425;

    public static IReadOnlyList<RunningLord> At(IReadOnlyList<DashaPeriodRecord> roots, DateTime date)
    {
        var result = new List<RunningLord>();
        var level = roots;
        for (var depth = 0; depth < StoredLevels; depth++)
        {
            var hit = level.FirstOrDefault(p => p.StartDate <= date && date < p.EndDate);
            if (hit is null) break;
            result.Add(new RunningLord(Levels[depth], hit.Lord, hit.EndDate));
            level = hit.Children;
        }
        if (result.Count == StoredLevels) Descend(result, date);
        return result;
    }

    /// <summary>
    /// Levels 4-7 are not stored (9^7 rows per chart); they follow from the Vimshottari proportions. The stored pratyantardasha may be
    /// the cut-off first period of the chart, so its full span is rebuilt from the three lords and anchored on its end date, and each
    /// deeper level splits its parent in the usual order, starting with the parent's own lord.
    /// </summary>
    private static void Descend(List<RunningLord> result, DateTime date)
    {
        var order = AstroMath.NakshatraLordOrder;
        var years = AstroMath.VimshottariYearsByLord;
        var lords = result.Select(r => Enum.Parse<PlanetName>(r.Planet)).ToList();
        var fullDays = lords.Aggregate(1.0, (acc, l) => acc * years[l] / 120.0) * 120 * DaysPerYear;
        var start = result[2].End.AddDays(-fullDays);
        var parent = lords[2];
        while (result.Count < Levels.Length)
        {
            var from = order.ToList().IndexOf(parent);
            var cursor = start;
            for (var slot = 0; slot < 9; slot++)
            {
                var lord = order[(from + slot) % 9];
                var days = fullDays * years[lord] / 120.0;
                var end = cursor.AddDays(days);
                if (date < end || slot == 8)
                {
                    result.Add(new RunningLord(Levels[result.Count], lord.ToString(), end));
                    parent = lord; start = cursor; fullDays = days;
                    break;
                }
                cursor = end;
            }
        }
    }
}

/// <param name="Text">One fact, worded as the book states it.</param>
/// <param name="Source">Where it comes from, e.g. "Phaladeepika 20.27".</param>
/// <param name="Adverse">True when the rule marks the period as adverse; false for informational facts.</param>
public sealed record DashaFact(string Text, string Source, bool Adverse);

/// <param name="Running">The level this planet is running at, or null when its dasha is not running.</param>
/// <param name="Lordship">Its functional nature by school, restated so the dasha cell is self-contained.</param>
public sealed record DashaReading(
    string Planet, IReadOnlyList<RunningLord> Runs, string Lordship, IReadOnlyList<DashaFact> Facts)
{
    /// <summary>The highest level this planet is running at, or null.</summary>
    public RunningLord? Running => Runs.FirstOrDefault();
    public bool HasAdverseFlag => Facts.Any(f => f.Adverse);
}

/// <summary>
/// What the classics say about a planet's dasha, as facts with their source, for every planet and marked for the ones running.
/// Phaladeepika ch.20 (Mantreswara, tr. Kapoor; read in the local text): v.24 the dasas of the lords of the 6th, 8th and 12th bring
/// misery; v.27 planets, benefic or malefic, placed in debilitation, an inimical sign or the 6th or 12th give adverse results in
/// their dasa; v.30 a planet's good effect scales with its sign (exalted maximum … debilitated almost nil, combustion like
/// debilitation); v.29 an antardasa lord gives the effects of the house it holds counted from the dasa lord, bad in the 6th, 8th
/// or 12th. The lordship line is the functional-nature columns again (BPHS ch.34 / Laghu Parashari, PVR, Raman). The rules are in
/// code with their citations, like LagnaFunctionalNature; no score is made — Phaladeepika gives fractions only for the good
/// effect, and says the results reverse for malefics without saying how.
/// </summary>
public static class DashaNatureReader
{
    public static IReadOnlyList<DashaReading> Read(
        IReadOnlyList<PlanetNatureRow> rows, IReadOnlyList<ChartKeyDetail> keyDetails, ZodiacName lagna, IReadOnlyList<RunningLord> running)
    {
        var grahas = keyDetails.Where(k => k.PointKind == "Graha" && k.Planet != "Ascendant").ToDictionary(k => k.Planet);
        var readings = new List<DashaReading>();

        foreach (var row in rows)
        {
            if (!grahas.TryGetValue(row.Planet, out var g)) continue;
            var facts = new List<DashaFact>();
            var runs = running.Where(r => r.Planet == row.Planet).ToList();
            var run = runs.FirstOrDefault();

            if (Enum.TryParse<PlanetName>(row.Planet, out var pn))
            {
                var bad = SchoolFunctionalNature.OwnedHouses(lagna, pn).Intersect(new[] { 6, 8, 12 }).ToArray();
                if (bad.Length > 0)
                    facts.Add(new($"Lord of the {string.Join(" and ", bad.Select(Ord))}: its dasa brings misery", "Phaladeepika 20.24", true));
            }

            var why = new List<string>();
            if (g.DignityStatus is "Debilitated") why.Add("its sign of debilitation");
            else if (g.DignityStatus is "Enemy" or "Great Enemy") why.Add("an inimical sign");
            if (g.HouseNumberFromLagna is 6 or 12) why.Add($"the {Ord(g.HouseNumberFromLagna)} house");
            if (why.Count > 0)
                facts.Add(new($"Placed in {string.Join(" and ", why)}: benefic or malefic, it gives adverse results in its dasa", "Phaladeepika 20.27", true));

            var share = ShareOfGood(g);
            if (share is not null) facts.Add(new(share, "Phaladeepika 20.30", false));

            if (run is { Level: "Antardasha" or "Pratyantardasha" } && ParentOf(run, running) is { } parent && grahas.TryGetValue(parent.Planet, out var pg)
                && Enum.TryParse<ZodiacName>(pg.Sign, out var fromSign) && Enum.TryParse<ZodiacName>(g.Sign, out var toSign))
            {
                var n = AstroMath.CountFromSignToSign(fromSign, toSign);
                var lowLevel = run.Level == "Pratyantardasha";
                var adverse = n is 6 or 8 or 12;
                var tail = adverse ? "bad, as it is the 6th, 8th or 12th from the dasa lord" : "good, as it is not the 6th, 8th or 12th from the dasa lord";
                var note = lowLevel ? " (the rule is the book's for antardasa; applied one level down here)" : "";
                facts.Add(new($"Holds the {Ord(n)} house from {parent.Planet}, the {parent.Level.ToLowerInvariant()} lord — {tail}{note}", "Phaladeepika 20.29", adverse));
            }

            if (run is { Level: "Antardasha" or "Pratyantardasha" } && ParentOf(run, running) is { } dasaLord && Enum.TryParse<PlanetName>(row.Planet, out var me))
            {
                var tests = new List<string>();
                if (Enum.TryParse<PlanetName>(dasaLord.Planet, out var dl) && PvrDignityEvaluator.IsNaturalEnemy(me, dl)) tests.Add($"inimical to {dasaLord.Planet}, the {dasaLord.Level.ToLowerInvariant()} lord");
                if (g.DignityStatus is "Enemy" or "Great Enemy" or "Debilitated") tests.Add("in an inimical sign");
                if (g.HouseNumberFromLagna == 6) tests.Add("posited in the 6th");
                if (Enum.TryParse<PlanetName>(HouseEngine.GetSignLord(lagna), out var ll) && ll != me && PvrDignityEvaluator.IsNaturalEnemy(me, ll)) tests.Add($"inimical to {ll}, the Lagna lord");
                if (tests.Count > 0)
                {
                    var note = run.Level == "Pratyantardasha" ? " (the rule is the book's for antardasa; applied one level down here)" : "";
                    facts.Add(new($"Danger from enemies, loss of position, hostility of former friends in the {run.Level.ToLowerInvariant()}: it is {string.Join(" and ", tests)}{note}", "Phaladeepika 20.28", true));
                }
            }

            var lordship = string.Join(" · ",row.Functional.Select(f => $"{ShortName(f.SchoolCode)} {Label(f.Verdict)}"));
            readings.Add(new DashaReading(row.Planet, runs, lordship, facts));
        }
        return readings;
    }

    private static RunningLord? ParentOf(RunningLord run, IReadOnlyList<RunningLord> running)
    {
        var i = running.ToList().IndexOf(run);
        return i > 0 ? running[i - 1] : null;
    }

    /// <summary>Phaladeepika 20.30: the share of its good effect a benefic gives, by the sign it holds.</summary>
    private static string? ShareOfGood(ChartKeyDetail g)
    {
        var sign = g.IsCombust == true
            ? "Combust, so like a planet in its debilitation: almost nil of its good effect"
            : g.DignityStatus switch
            {
                "Exalted" => "Exalted: the maximum of its good effect",
                "Moolatrikona" => "Moolatrikona: three-quarters of its good effect",
                "Own Sign" => "In its own sign: half of its good effect",
                "Friend" or "Great Friend" => "In a friend's sign: one-quarter of its good effect",
                "Enemy" or "Great Enemy" => "In an inimical sign: very little of its good effect",
                "Debilitated" => "Debilitated: almost nil of its good effect",
                _ => null,
            };
        return sign is null ? null : sign + "; for a malefic or the lord of a malefic house the book says the results reverse";
    }

    private static string Ord(int n) => n switch { 1 => "1st", 2 => "2nd", 3 => "3rd", _ => $"{n}th" };

    private static string ShortName(string code) => code switch { "PVR" => "PVR", "BPHS" => "BPHS", "RAMAN" => "Raman", _ => code };

    private static string Label(SchoolFunctional f) => f switch
    {
        SchoolFunctional.Yogakaraka => "yogakāraka", SchoolFunctional.Benefic => "benefic", SchoolFunctional.Neutral => "neutral",
        SchoolFunctional.Malefic => "malefic", SchoolFunctional.Mixed => "mixed", _ => "—",
    };
}
