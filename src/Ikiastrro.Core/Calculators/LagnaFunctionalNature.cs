using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Calculators;

/// <summary>Parashari functional nature of a planet with respect to a Lagna — Benefic / Malefic /
/// Neutral / Yogakaraka — derived from which houses the planet rules from that Lagna. This is the
/// single source of the functional-nature verdict used across the app (CLI and web).
/// A documented heuristic following the general rules in B.V. Raman, "How to Judge a Horoscope"
/// Vol. 1, p.14-15 (kendra/trikona lordship, kendradhipati dosha, maraka/dusthana). For
/// mixed-lordship planets the catch-all branch below applies. Rahu/Ketu are out of scope
/// (no sign rulership).</summary>
public static class LagnaFunctionalNature
{
    private static readonly HashSet<PlanetName> NaturalBenefics = new()
        { PlanetName.Jupiter, PlanetName.Venus, PlanetName.Mercury, PlanetName.Moon };

    private static readonly int[] Kendras  = { 4, 7, 10 };   // the 1st is a kendra too but never on its own confers yogakaraka
    private static readonly int[] Trikonas = { 5, 9 };       // the 1st is a trikona too, same caveat
    private static readonly int[] Dusthanas3611 = { 3, 6, 11 };

    public static FunctionalNatureResult For(ZodiacName lagnaSign, PlanetName planet)
    {
        if (planet is PlanetName.Rahu or PlanetName.Ketu)
            throw new ArgumentOutOfRangeException(nameof(planet), "Functional nature is defined for the 7 classical planets only.");

        var ruledHouses = Enumerable.Range(0, 12)
            .Select(i => (ZodiacName)i)
            .Where(sign => ClassicalDignity.GetSignLord(sign) == planet.ToString())
            .Select(sign => AstroMath.CountFromSignToSign(lagnaSign, sign))
            .OrderBy(h => h)
            .ToArray();

        if (ruledHouses.Length == 0)
            throw new InvalidOperationException(
                $"No sign rulership found for {planet} — ClassicalDignity.GetSignLord contract drift?");

        var isMaraka = ruledHouses.Contains(2) || ruledHouses.Contains(7);
        var hasKendra  = ruledHouses.Intersect(Kendras).Any();
        var hasTrikona = ruledHouses.Intersect(Trikonas).Any();
        var hasBad     = ruledHouses.Intersect(Dusthanas3611).Any();
        var isNaturalBenefic = NaturalBenefics.Contains(planet);

        FunctionalNature nature;
        var kendradhipatiDosha = false;
        string why;

        if (hasKendra && hasTrikona)
        {
            nature = FunctionalNature.Yogakaraka;
            why = $"Rules a kendra and a trikona ({string.Join(" & ", ruledHouses)}) — Yogakaraka";
        }
        else if (ruledHouses.Contains(1) && !hasBad)
        {
            nature = planet == PlanetName.Moon ? FunctionalNature.Neutral : FunctionalNature.Benefic;
            why = planet == PlanetName.Moon ? "Lagna lord and the Moon — Neutral" : "Lagna lord — Benefic";
        }
        else if ((ruledHouses.Contains(5) || ruledHouses.Contains(9)) && !hasBad)
        {
            nature = FunctionalNature.Benefic;
            why = $"Trikona lord ({string.Join(" & ", ruledHouses)}) — Benefic";
        }
        else if (!hasBad && ruledHouses.All(h => Kendras.Contains(h)) && ruledHouses.Length > 0)
        {
            if (isNaturalBenefic) { nature = FunctionalNature.Malefic; kendradhipatiDosha = true;
                why = $"Natural benefic owning only a kendra ({string.Join(" & ", ruledHouses)}) — kendradhipati dosha"; }
            else { nature = FunctionalNature.Benefic;
                why = $"Natural malefic owning a kendra ({string.Join(" & ", ruledHouses)}) — Benefic"; }
        }
        else if (hasBad)
        {
            nature = FunctionalNature.Malefic;
            why = $"Lord of {string.Join(" & ", ruledHouses.Intersect(Dusthanas3611))} — malefic house lordship";
        }
        else if (ruledHouses.Length > 0 && ruledHouses.All(h => h is 2 or 8 or 12))
        {
            nature = planet is PlanetName.Sun or PlanetName.Moon ? FunctionalNature.Neutral : FunctionalNature.Malefic;
            why = $"Lord of {string.Join(" & ", ruledHouses)}" + (nature == FunctionalNature.Neutral ? " — luminary, Neutral" : " — Malefic");
        }
        else
        {
            nature = FunctionalNature.Malefic;   // default catch-all: mixed kendra + maraka/dusthana, no trikona
            why = $"Mixed lordship ({string.Join(" & ", ruledHouses)}) — Malefic (heuristic default)";
        }

        return new FunctionalNatureResult(nature, ruledHouses, isMaraka, kendradhipatiDosha, why);
    }
}

/// <summary>Functional (Lagna-relative) nature classes. Distinct from natural benefic/malefic.</summary>
public enum FunctionalNature { Benefic, Malefic, Neutral, Yogakaraka }

/// <param name="RuledHouses">1-based house numbers this planet rules from the given Lagna (1 or 2 entries).</param>
/// <param name="IsMaraka">Additionally lord of the 2nd or 7th (independent of Nature).</param>
/// <param name="KendradhipatiDosha">Natural benefic degraded by owning only an angle.</param>
public record FunctionalNatureResult(
    FunctionalNature Nature, int[] RuledHouses, bool IsMaraka, bool KendradhipatiDosha, string Rationale);
