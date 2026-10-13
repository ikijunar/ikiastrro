using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Dasha;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Engines.PlanetNature;

/// <param name="Level">Mahadasha, Antardasha or Pratyantardasha.</param>
public sealed record RunningLord(string Level, string Planet, DateTime End);

/// <summary>The Vimshottari lords running on a date, read from the stored dasha tree.</summary>
public static class RunningDasha
{
    private static readonly string[] Levels = { "Mahadasha", "Antardasha", "Pratyantardasha" };

    public static IReadOnlyList<RunningLord> At(IReadOnlyList<DashaPeriodRecord> roots, DateTime date)
    {
        var result = new List<RunningLord>();
        var level = roots;
        for (var depth = 0; depth < Levels.Length; depth++)
        {
            var hit = level.FirstOrDefault(p => p.StartDate <= date && date < p.EndDate);
            if (hit is null) break;
            result.Add(new RunningLord(Levels[depth], hit.Lord, hit.EndDate));
            level = hit.Children;
        }
        return result;
    }
}

/// <param name="Text">One fact, worded as the book states it.</param>
/// <param name="Source">Where it comes from, e.g. "Phaladeepika 20.27".</param>
/// <param name="Adverse">True when the rule marks the period as adverse; false for informational facts.</param>
public sealed record DashaFact(string Text, string Source, bool Adverse);

/// <param name="Running">The level this planet is running at, or null when its dasha is not running.</param>
/// <param name="Lordship">Its functional nature by school, restated so the dasha cell is self-contained.</param>
public sealed record DashaReading(
    string Planet, RunningLord? Running, string Lordship, IReadOnlyList<DashaFact> Facts)
{
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
            var run = running.FirstOrDefault(r => r.Planet == row.Planet);

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

            var lordship = string.Join(" · ", row.Functional.Select(f => $"{ShortName(f.SchoolCode)} {Label(f.Verdict)}"));
            readings.Add(new DashaReading(row.Planet, run, lordship, facts));
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
