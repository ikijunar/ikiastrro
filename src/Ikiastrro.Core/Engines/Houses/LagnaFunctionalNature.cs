using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Engines.Houses;

/// <summary>Functional nature of a planet for a Lagna — Benefic / Malefic / Neutral / Yogakaraka —
/// read straight from P.V.R. Narasimha Rao, "Vedic Astrology: An Integrated Approach"
/// (SRC_PVR_INTEGRATED), §13.2 and Table 30 "Functional nature of planets" (printed p.166-167).
/// This is the single source of the functional-nature verdict used across the app (CLI and web).
/// PVR's rule: lords of trines are functional benefics; lords of the 3rd, 6th and 11th functional
/// malefics; a quadrant lord is a functional malefic if a natural benefic and neutral if a natural
/// malefic; lords of the 2nd, 8th and 12th neutral (the 8th most malefic); a planet owning a quadrant
/// and a trine is a yogakaraka. For planets owning two rasis PVR says to "judiciously combine the two
/// indications", and Table 30 is that combination worked out for every Lagna — so the table is the
/// verdict here and the rule is not re-derived (a mechanical re-derivation disagrees with the book,
/// e.g. Jupiter in Cancer, 6th + 9th lord, which the table lists as benefic).
/// The Moon is not listed for Aries, Libra and Capricorn: PVR makes it depend on phase — a waxing
/// Moon is a natural benefic and becomes a functional malefic with quadrant ownership, a waning Moon
/// a natural malefic made functionally neutral by it. Pass <c>moonWaxing</c> for those Lagnas; left
/// null, the waxing reading is returned and the rationale says so.
/// Rahu/Ketu are out of scope (no sign rulership, not in Table 30).</summary>
public static class LagnaFunctionalNature
{
    private sealed record Row(PlanetName? Yogakaraka, PlanetName[] Benefics, PlanetName[] Neutrals, PlanetName[] Malefics);

    private const PlanetName Su = PlanetName.Sun, Mo = PlanetName.Moon, Ma = PlanetName.Mars, Me = PlanetName.Mercury,
        Ju = PlanetName.Jupiter, Ve = PlanetName.Venus, Sa = PlanetName.Saturn;

    /// <summary>PVR Table 30, p.167, one row per Lagna. The Moon is absent from the Aries, Libra and
    /// Capricorn rows exactly as printed.</summary>
    private static readonly Dictionary<ZodiacName, Row> Table30 = new()
    {
        [ZodiacName.Aries]       = new(null, new[] { Su, Ma, Ju }, Array.Empty<PlanetName>(), new[] { Me, Ve, Sa }),
        [ZodiacName.Taurus]      = new(Sa,   new[] { Su, Me, Sa }, new[] { Ma },              new[] { Mo, Ju, Ve }),
        [ZodiacName.Gemini]      = new(null, new[] { Ve },         new[] { Mo, Me, Sa },      new[] { Su, Ma, Ju }),
        [ZodiacName.Cancer]      = new(Ma,   new[] { Mo, Ma, Ju }, new[] { Su, Sa },          new[] { Me, Ve }),
        [ZodiacName.Leo]         = new(Ma,   new[] { Su, Ma, Ju }, new[] { Mo },              new[] { Me, Ve, Sa }),
        [ZodiacName.Virgo]       = new(null, new[] { Me, Ve },     new[] { Su, Sa },          new[] { Mo, Ma, Ju }),
        [ZodiacName.Libra]       = new(Sa,   new[] { Me, Ve, Sa }, Array.Empty<PlanetName>(), new[] { Su, Ma, Ju }),
        [ZodiacName.Scorpio]     = new(null, new[] { Mo, Ju },     new[] { Su, Ma },          new[] { Me, Ve, Sa }),
        [ZodiacName.Sagittarius] = new(null, new[] { Su, Ma },     new[] { Mo, Me, Ju },      new[] { Ve, Sa }),
        [ZodiacName.Capricornus] = new(Ve,   new[] { Ve, Me, Sa }, new[] { Su },              new[] { Ma, Ju }),
        [ZodiacName.Aquarius]    = new(Ve,   new[] { Ve, Sa },     new[] { Su, Me },          new[] { Mo, Ma, Ju }),
        [ZodiacName.Pisces]      = new(null, new[] { Mo, Ma },     new[] { Ju },              new[] { Su, Me, Ve, Sa }),
    };

    private static readonly PlanetName[] NaturalBenefics = { PlanetName.Jupiter, PlanetName.Venus, PlanetName.Mercury };
    private static readonly int[] Kendras  = { 4, 7, 10 };   // the 1st is a quadrant too but never makes a yogakaraka on its own
    private static readonly int[] Trikonas = { 5, 9 };
    private static readonly int[] Dusthanas = { 6, 8, 12 };
    private static readonly int[] Trishadayas = { 3, 6, 11 };

    /// <summary>The planet Table 30 lists as yogakaraka for a Lagna, if any.</summary>
    public static PlanetName? YogakarakaFor(ZodiacName lagnaSign) => Table30[lagnaSign].Yogakaraka;

    public static FunctionalNatureResult For(ZodiacName lagnaSign, PlanetName planet, bool? moonWaxing = null)
    {
        if (planet is PlanetName.Rahu or PlanetName.Ketu)
            throw new ArgumentOutOfRangeException(nameof(planet), "Functional nature is defined for the 7 classical planets only.");

        var ruledHouses = Enumerable.Range(0, 12)
            .Select(i => (ZodiacName)i)
            .Where(sign => HouseEngine.GetSignLord(sign) == planet.ToString())
            .Select(sign => AstroMath.CountFromSignToSign(lagnaSign, sign))
            .OrderBy(h => h)
            .ToArray();

        if (ruledHouses.Length == 0)
            throw new InvalidOperationException(
                $"No sign rulership found for {planet} — HouseEngine.GetSignLord contract drift?");

        var marakaHouses = ruledHouses.Intersect(new[] { 2, 7 }).ToArray();
        var dusthanaHouses = ruledHouses.Intersect(Dusthanas).ToArray();
        var trishadayaHouses = ruledHouses.Intersect(Trishadayas).ToArray();
        var baadhaka = BaadhakaCalculator.For(lagnaSign);
        var isBaadhakaLord = baadhaka.Baadhaka.Equals(planet.ToString(), StringComparison.Ordinal);
        var ownsKendra = ruledHouses.Intersect(Kendras).Any();
        var ownsTrikona = ruledHouses.Intersect(Trikonas).Any();
        var houses = string.Join(" & ", ruledHouses);

        var row = Table30[lagnaSign];
        FunctionalNature nature;
        string why;
        var kendradhipatiDosha = false;

        if (row.Yogakaraka == planet)
        {
            nature = FunctionalNature.Yogakaraka;
            why = $"Rules a quadrant and a trine ({houses}) — Yogakaraka (PVR Table 30)";
        }
        else if (row.Benefics.Contains(planet))
        {
            nature = FunctionalNature.Benefic;
            why = $"Rules {houses} — functional benefic (PVR Table 30)";
        }
        else if (row.Neutrals.Contains(planet))
        {
            nature = FunctionalNature.Neutral;
            why = $"Rules {houses} — functionally neutral (PVR Table 30)";
        }
        else if (row.Malefics.Contains(planet))
        {
            nature = FunctionalNature.Malefic;
            why = $"Rules {houses} — functional malefic (PVR Table 30)";
        }
        else if (planet == PlanetName.Moon)
        {
            // Aries / Libra / Capricornus: PVR leaves the Moon out of Table 30 — it depends on phase.
            var waxing = moonWaxing ?? true;
            nature = waxing ? FunctionalNature.Malefic : FunctionalNature.Neutral;
            why = waxing
                ? $"Rules {houses} — waxing Moon is a natural benefic, so quadrant ownership makes it a functional malefic (PVR §13.2)"
                : $"Rules {houses} — waning Moon is a natural malefic, so quadrant ownership makes it functionally neutral (PVR §13.2)";
            if (moonWaxing is null) why += "; phase not supplied, waxing assumed";
            kendradhipatiDosha = waxing;
        }
        else
            throw new InvalidOperationException($"{planet} is missing from PVR Table 30 for {lagnaSign}.");

        // PVR §13.2: a natural benefic owning a quadrant (and no trine) is turned functionally malefic.
        if (nature == FunctionalNature.Malefic && ownsKendra && !ownsTrikona && NaturalBenefics.Contains(planet))
            kendradhipatiDosha = true;

        var modifiers = new List<string>();
        if (marakaHouses.Length > 0)
            modifiers.Add($"māraka lord ({string.Join(" & ", marakaHouses)})");
        if (dusthanaHouses.Length > 0)
            modifiers.Add($"dusthāna lord ({string.Join(" & ", dusthanaHouses)})");
        if (trishadayaHouses.Length > 0)
            modifiers.Add($"triṣaḍāya lord ({string.Join(" & ", trishadayaHouses)})");
        if (isBaadhakaLord)
            modifiers.Add($"bādhaka lord ({baadhaka.SthaanaOffset})");

        if (modifiers.Count > 0)
            why += $"; additionally {string.Join(", ", modifiers)}";

        return new FunctionalNatureResult(
            nature, ruledHouses, marakaHouses, dusthanaHouses, trishadayaHouses,
            isBaadhakaLord, baadhaka.SthaanaOffset, kendradhipatiDosha, why);
    }
}

/// <summary>Functional (Lagna-relative) nature classes. Distinct from natural benefic/malefic.</summary>
public enum FunctionalNature { Benefic, Malefic, Neutral, Yogakaraka }

/// <param name="RuledHouses">1-based house numbers this planet rules from the given Lagna (1 or 2 entries).</param>
/// <param name="MarakaHouses">Owned māraka houses (2 and/or 7), independent of Nature.</param>
/// <param name="DusthanaHouses">Owned dusthāna houses (6, 8 and/or 12).</param>
/// <param name="TrishadayaHouses">Owned triṣaḍāya houses (3, 6 and/or 11).</param>
/// <param name="IsBaadhakaLord">Whether this planet rules the Lagna's modality-derived bādhaka house.</param>
/// <param name="BaadhakaHouse">The bādhaka house: 11 movable, 9 fixed, 7 dual.</param>
/// <param name="KendradhipatiDosha">Natural benefic made functionally malefic by owning a quadrant (PVR §13.2).</param>
public record FunctionalNatureResult(
    FunctionalNature Nature,
    int[] RuledHouses,
    int[] MarakaHouses,
    int[] DusthanaHouses,
    int[] TrishadayaHouses,
    bool IsBaadhakaLord,
    int BaadhakaHouse,
    bool KendradhipatiDosha,
    string Rationale)
{
    public bool IsMaraka => MarakaHouses.Length > 0;
}
