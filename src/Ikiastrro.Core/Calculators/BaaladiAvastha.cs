using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Calculators;

/// <summary>
/// Baaladi (Baladi) Avastha — the "age" of a planet, from its degree within its sign:
/// Baala (infant) / Kumara (child) / Yuva (youth) / Vriddha (old) / Mrita (dead).
/// In odd signs the bands run 0-6-12-18-24-30 in that order; in even signs they run in
/// reverse. Each state carries a classical effect fraction (how much of the planet's result
/// manifests). Meaningful only for a continuous rasi (D1) longitude — a varga sign is a
/// discrete bucket, so this is D1-only, matching DegreesInSignDecimal.
///
/// Data-driven: the bands and fractions come from tbl_Rule_BaaladiState (a given RuleSetId),
/// never hardcoded here — a different classical convention is a new RuleSetId.
/// </summary>
public static class BaaladiAvastha
{
    /// <summary>Odd signs (1st, 3rd, ... = Aries, Gemini, Leo, Libra, Sagittarius, Aquarius) — bands run forward.
    /// Even signs — bands run reversed. ZodiacName is 0-based (Aries = 0), so even enum value = odd sign.</summary>
    public static bool IsOddSign(ZodiacName sign) => (int)sign % 2 == 0;

    /// <summary>Returns the matching Baaladi rule row for a planet at <paramref name="degreeInSign"/>
    /// (0-30) in <paramref name="sign"/>. Throws if the rules don't cover the degree (0-30 must be
    /// fully partitioned).</summary>
    public static BaaladiRuleRow For(ZodiacName sign, decimal degreeInSign, IReadOnlyList<BaaladiRuleRow> rules)
    {
        if (degreeInSign < 0 || degreeInSign >= 30)
            degreeInSign = ((degreeInSign % 30) + 30) % 30;

        var odd = IsOddSign(sign);
        foreach (var r in rules)
        {
            var from = odd ? r.OddSignFromDegree : r.EvenSignFromDegree;
            var to = odd ? r.OddSignToDegree : r.EvenSignToDegree;
            if (degreeInSign >= from && degreeInSign < to)
                return r;
        }

        throw new InvalidOperationException(
            $"No tbl_Rule_BaaladiState band covers {degreeInSign:0.###}° in a {(odd ? "odd" : "even")} sign — rule rows must partition 0-30.");
    }
}
