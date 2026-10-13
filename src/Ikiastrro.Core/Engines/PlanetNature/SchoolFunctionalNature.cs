using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Houses;

namespace Ikiastrro.Core.Engines.PlanetNature;

public enum SchoolFunctional { Yogakaraka, Benefic, Neutral, Malefic, Mixed, NotStated }

/// <param name="Why">The houses owned and the rule that gave the verdict.</param>
public sealed record SchoolFunctionalReading(string SchoolCode, SchoolFunctional Verdict, string Why);

/// <summary>
/// Functional (house-lordship) nature of one planet for one Lagna, read the way each school writes it down. The three
/// schools disagree on one point that matters — what a kendra lordship does (docs/research/domain/
/// planet-benefic-malefic-perspectives.md §2) — so each is reported separately and none is blended into another.
/// PVR is Table 30 (<see cref="LagnaFunctionalNature"/>); BPHS ch.34 and Raman are read from the rule rows.
/// </summary>
public static class SchoolFunctionalNature
{
    /// <summary>BPHS ch.34 (Poddar/SJC translation): house strength in ascending order — a planet owning two houses gives the
    /// result of the stronger and stalls the other.</summary>
    private static readonly int[] BphsStrengthOrder = { 2, 12, 5, 9, 3, 6, 11, 8, 1, 4, 7, 10 };

    public static IReadOnlyList<SchoolFunctionalReading> ReadAll(
        ZodiacName lagna, PlanetName planet, NaturalNatureReading natural, int occupiedHouse, bool? moonWaxing, SchoolRuleBook rules)
    {
        var list = new List<SchoolFunctionalReading> { Pvr(lagna, planet, moonWaxing) };
        var owned = OwnedHouses(lagna, planet);
        if (rules.Schools.Any(s => s.Code == SchoolCodes.Bphs)) list.Add(Bphs(planet, owned, natural, rules));
        if (rules.Schools.Any(s => s.Code == SchoolCodes.Raman)) list.Add(Raman(planet, owned, natural, occupiedHouse, rules));
        return list;
    }

    public static int[] OwnedHouses(ZodiacName lagna, PlanetName planet) =>
        planet is PlanetName.Rahu or PlanetName.Ketu
            ? Array.Empty<int>()
            : Enumerable.Range(0, 12).Select(i => (ZodiacName)i)
                .Where(sign => HouseEngine.GetSignLord(sign) == planet.ToString())
                .Select(sign => AstroMath.CountFromSignToSign(lagna, sign)).OrderBy(h => h).ToArray();

    public static SchoolFunctionalReading Pvr(ZodiacName lagna, PlanetName planet, bool? moonWaxing)
    {
        if (planet is PlanetName.Rahu or PlanetName.Ketu)
            return new(SchoolCodes.Pvr, SchoolFunctional.NotStated, "Nodes own no sign; PVR Table 30 lists the seven planets only");
        var r = LagnaFunctionalNature.For(lagna, planet, moonWaxing);
        var verdict = r.Nature switch
        {
            FunctionalNature.Yogakaraka => SchoolFunctional.Yogakaraka,
            FunctionalNature.Benefic => SchoolFunctional.Benefic,
            FunctionalNature.Malefic => SchoolFunctional.Malefic,
            _ => SchoolFunctional.Neutral,
        };
        return new(SchoolCodes.Pvr, verdict, r.Rationale);
    }

    private static SchoolFunctionalReading NotStated(string school, string why) => new(school, SchoolFunctional.NotStated, why);

    private static SchoolFunctional Map(string result) => result switch
    {
        "BENEFIC" => SchoolFunctional.Benefic,
        "MALEFIC" => SchoolFunctional.Malefic,
        _ => SchoolFunctional.Neutral,
    };

    private static string Houses(IEnumerable<int> h) => string.Join(" & ", h.Select(x => $"H{x}"));

    // ---- BPHS ch.34
    private static SchoolFunctionalReading Bphs(PlanetName planet, int[] owned, NaturalNatureReading natural, SchoolRuleBook rules)
    {
        const string S = SchoolCodes.Bphs;
        if (owned.Length == 0) return NotStated(S, "Owns no sign");
        var name = planet.ToString();
        var cls = NaturalNatureReader.RuleClass(natural) ?? "ANY";
        var label = Houses(owned);

        // Sun and Moon own one sign each; BPHS says neither suffers the 8th-lordship blemish.
        if (planet is PlanetName.Sun or PlanetName.Moon && owned.SequenceEqual(new[] { 8 }))
            return new(S, SchoolFunctional.Neutral, $"Rules {label}; Sun and Moon do not suffer 8th lordship (BPHS ch.34)");

        var kendra = owned.Intersect(new[] { 4, 7, 10 }).Any();
        var kona = owned.Intersect(new[] { 5, 9 }).Any();
        if (kendra && kona)
            return new(S, SchoolFunctional.Yogakaraka, $"Rules {label}: a kendra and a kona lord (BPHS ch.34 — yogakaraka)");

        if (owned.Length == 2 && owned.Contains(8))
        {
            var other = owned.Single(h => h != 8);
            if (other is 3 or 7 or 11)
                return new(S, SchoolFunctional.Malefic, $"Rules {label}: the 8th with the {Ord(other)} — extremely harmful (BPHS ch.34)");
            if (other is 5 or 9)
                return new(S, SchoolFunctional.Benefic, $"Rules {label}: the 8th with a kona — auspicious (BPHS ch.34)");
        }

        if (owned.Length == 1)
        {
            var rule = rules.Find(S, "LORDSHIP", cls, name, owned[0]);
            return rule is null ? NotStated(S, $"Rules {label}; BPHS states nothing for it")
                : new(S, Map(rule.Result), $"Rules {label}: {rule.Note ?? rule.Result.ToLowerInvariant()}");
        }

        var predominant = owned.OrderBy(h => Array.IndexOf(BphsStrengthOrder, h)).Last();
        var lesser = owned.Single(h => h != predominant);
        var winner = rules.Find(S, "LORDSHIP", cls, name, predominant);
        return winner is null ? NotStated(S, $"Rules {label}; BPHS states nothing for H{predominant}")
            : new(S, Map(winner.Result),
                $"Rules {label}: H{predominant} is the stronger house in BPHS's order, so its result stalls H{lesser}'s — {winner.Note ?? winner.Result.ToLowerInvariant()}");
    }

    // ---- Raman, How to Judge a Horoscope ch.1
    private static SchoolFunctionalReading Raman(
        PlanetName planet, int[] owned, NaturalNatureReading natural, int occupiedHouse, SchoolRuleBook rules)
    {
        const string S = SchoolCodes.Raman;
        if (owned.Length == 0) return NotStated(S, "Owns no sign");
        var name = planet.ToString();
        var cls = NaturalNatureReader.RuleClass(natural) ?? "ANY";
        var label = Houses(owned);

        var kendra = owned.Intersect(new[] { 4, 7, 10 }).Any();
        var kona = owned.Intersect(new[] { 5, 9 }).Any();
        if (kendra && kona)
            return new(S, SchoolFunctional.Yogakaraka, $"Rules {label}: owns a kendra and a trikona — yogakaraka (Raman)");

        var parts = new List<(int House, SchoolFunctional Verdict, string Text)>();
        foreach (var h in owned)
        {
            var rule = rules.Find(S, "LORDSHIP", cls, name, h);
            if (rule is null) continue;
            var verdict = Map(rule.Result);
            var text = rule.Note ?? rule.Result.ToLowerInvariant();
            // Raman: a kendradhipati (or a natural malefic made benefic by a kendra) stops acting so once it sits in that kendra.
            if (h is 4 or 7 or 10 && occupiedHouse == h && verdict != SchoolFunctional.Neutral)
            {
                verdict = SchoolFunctional.Neutral;
                text += $"; but it sits in H{h}, the kendra it owns, so the effect ceases (Raman)";
            }
            parts.Add((h, verdict, text));
        }
        if (parts.Count == 0) return NotStated(S, $"Rules {label}; Raman states nothing for it");

        var distinct = parts.Select(p => p.Verdict).Distinct().ToList();
        if (distinct.Count == 1)
            return new(S, distinct[0], $"Rules {label}: " + string.Join("; ", parts.Select(p => $"H{p.House} {p.Text}")));
        return new(S, SchoolFunctional.Mixed,
            $"Rules {label} and Raman gives no rule for combining them: " + string.Join("; ", parts.Select(p => $"H{p.House} {p.Verdict.ToString().ToLowerInvariant()} ({p.Text})")));
    }

    private static string Ord(int n) => n switch { 1 => "1st", 2 => "2nd", 3 => "3rd", _ => $"{n}th" };
}
