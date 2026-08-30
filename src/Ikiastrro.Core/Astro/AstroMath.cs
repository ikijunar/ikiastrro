namespace Ikiastrro.Core.Astro;

/// <summary>
/// Pure, deterministic classical-astrology math on a sidereal (nirayana) longitude — sign, degree
/// formatting, nakshatra/pada, navamsa sign, and Whole Sign house counting. Replaces the equivalent
/// VedAstro.Library helper methods (GetZodiacSignAtLongitude, GetConstellationAtLongitude,
/// GetNavamsaSignNameFromLongitude, CountFromSignToSign) with our own implementation now that the
/// underlying longitudes come from SwissEphemerisProvider instead. No external dependency needed for
/// any of this — it's all fixed-arithmetic classical formulas.
/// </summary>
public static class AstroMath
{
    private const double DegreesPerSign = 30.0;
    private const double DegreesPerNakshatra = 360.0 / 27;   // 13°20'
    private const double DegreesPerPada = 360.0 / 108;        // 3°20'
    private const double DegreesPerNavamsa = 360.0 / 108;     // 3°20' (each sign's 30° / 9)

    /// <summary>Normalizes any longitude into [0, 360).</summary>
    public static double Normalize(double longitudeDegrees)
    {
        var d = longitudeDegrees % 360.0;
        return d < 0 ? d + 360.0 : d;
    }

    public static ZodiacName GetSignAtLongitude(double siderealLongitude)
    {
        var normalized = Normalize(siderealLongitude);
        var signIndex = (int)(normalized / DegreesPerSign);
        return (ZodiacName)Math.Min(signIndex, 11); // guard against 360.0 rounding to index 12
    }

    /// <summary>Degrees elapsed within the current sign, 0-30.</summary>
    public static double GetDegreesInSign(double siderealLongitude) =>
        Normalize(siderealLongitude) % DegreesPerSign;

    /// <summary>Formats a 0-30° (or any) degree value as "D°M'S\"".</summary>
    public static string FormatDegreesMinutesSeconds(double degreesValue)
    {
        var totalSeconds = (int)Math.Round(degreesValue * 3600.0);
        var degrees = totalSeconds / 3600;
        var minutes = (totalSeconds % 3600) / 60;
        var seconds = totalSeconds % 60;
        return $"{degrees}°{minutes}'{seconds}\"";
    }

    public static (ConstellationName Nakshatra, int Pada) GetNakshatraAndPada(double siderealLongitude)
    {
        var normalized = Normalize(siderealLongitude);
        var nakshatraIndex = (int)(normalized / DegreesPerNakshatra);
        nakshatraIndex = Math.Min(nakshatraIndex, 26); // guard against rounding to index 27

        var overallPadaIndex = (int)(normalized / DegreesPerPada);
        var pada = (overallPadaIndex % 4) + 1;

        return ((ConstellationName)nakshatraIndex, pada);
    }

    /// <summary>
    /// Nakshatra index (0-26) and the fraction (0-1) already elapsed within it — the basis for
    /// Vimshottari Dasha's "balance of first dasha" calculation (VimshottariDashaCalculator).
    /// </summary>
    public static (int NakshatraIndex, double FractionElapsed) GetNakshatraIndexAndFractionElapsed(double siderealLongitude)
    {
        var normalized = Normalize(siderealLongitude);
        var nakshatraIndex = Math.Min((int)(normalized / DegreesPerNakshatra), 26); // guard against rounding to index 27
        var fractionElapsed = (normalized % DegreesPerNakshatra) / DegreesPerNakshatra;
        return (nakshatraIndex, fractionElapsed);
    }

    /// <summary>
    /// Navamsa (D9) sign from a sidereal longitude. Classical rule: movable signs count navamsas
    /// starting from themselves, fixed signs from the 9th sign from themselves, dual signs from the
    /// 5th sign from themselves. Equivalent to the shortcut formula below (verified against known
    /// cases: Aries seg0 -> Aries; Taurus seg0 -> Capricorn; Gemini seg0 -> Libra).
    /// </summary>
    public static ZodiacName GetNavamsaSign(double siderealLongitude)
    {
        var normalized = Normalize(siderealLongitude);
        var signIndex = (int)(normalized / DegreesPerSign);
        var degreesInSign = normalized % DegreesPerSign;
        var segmentIndex = (int)(degreesInSign / DegreesPerNavamsa);
        segmentIndex = Math.Min(segmentIndex, 8); // guard against rounding to index 9

        var navamsaIndex = (signIndex * 9 + segmentIndex) % 12;
        return (ZodiacName)navamsaIndex;
    }

    /// <summary>
    /// D2 (Hora) sign — classical two-sign Parasara rule (PyJHora _hora_traditional_parasara_chart):
    /// the sign's first/second 15° half maps to the Sun's Hora (Leo) or the Moon's Hora (Cancer).
    /// Odd signs: 0-15°→Leo, 15-30°→Cancer. Even signs: reversed. So D2 is always Leo or Cancer.
    /// Verified: Aries 10°→Leo, Aries 20°→Cancer, Taurus 10°→Cancer, Taurus 20°→Leo.
    /// </summary>
    public static ZodiacName GetHoraSign(double siderealLongitude)
    {
        var normalized = Normalize(siderealLongitude);
        var signIndex = (int)(normalized / DegreesPerSign);
        var degreesInSign = normalized % DegreesPerSign;
        var half = degreesInSign < 15.0 ? 0 : 1;          // 0 = first half, 1 = second half
        var isOddSign = signIndex % 2 == 0;               // 1-indexed odd == 0-indexed even
        var sunHora = (isOddSign && half == 0) || (!isOddSign && half == 1);
        return sunHora ? ZodiacName.Leo : ZodiacName.Cancer;
    }

    /// <summary>
    /// D6 (Shashtamsa) sign — Traditional Parasara (PyJHora shashthamsa_chart method 1). Sign split
    /// into six 5° parts. Odd signs: parts map to Aries..Virgo. Even signs: parts map to
    /// Libra..Pisces. The source sign itself does not shift the result. Verified: Aries 2°→Aries,
    /// Aries 27°→Virgo, Taurus 2°→Libra, Taurus 27°→Pisces.
    /// </summary>
    public static ZodiacName GetShashtamsaSign(double siderealLongitude)
    {
        var normalized = Normalize(siderealLongitude);
        var signIndex = (int)(normalized / DegreesPerSign);
        var degreesInSign = normalized % DegreesPerSign;
        var part = Math.Min((int)(degreesInSign / 5.0), 5);           // 0..5
        var isOddSign = signIndex % 2 == 0;
        var target = isOddSign ? part : (part + 6) % 12;
        return (ZodiacName)target;
    }

    /// <summary>
    /// D10 (Dasamsa) sign — Traditional Parasara (PyJHora dasamsa_chart method 1). Sign split into
    /// ten 3° parts. Odd signs: count parts forward from the sign itself. Even signs: count forward
    /// from the 9th sign from it (signIndex + 8). Verified: Aries 1°→Aries, Aries 28°→Capricornus,
    /// Taurus 1°→Capricornus, Taurus 28°→Libra.
    /// </summary>
    public static ZodiacName GetDasamsaSign(double siderealLongitude)
    {
        var normalized = Normalize(siderealLongitude);
        var signIndex = (int)(normalized / DegreesPerSign);
        var degreesInSign = normalized % DegreesPerSign;
        var part = Math.Min((int)(degreesInSign / 3.0), 9);           // 0..9
        var isOddSign = signIndex % 2 == 0;
        var target = isOddSign ? (signIndex + part) % 12 : (signIndex + part + 8) % 12;
        return (ZodiacName)target;
    }

    /// <summary>
    /// D11 (Rudramsa) sign — Traditional Parasara / Sanjay Rath (PyJHora rudramsa_chart method 1,
    /// the shipped default). Sign split into eleven parts of 30/11°. Target = (12 - signIndex + part)
    /// mod 12 for every sign. Verified: Aries 1°→Aries, Aries 29°→Aquarius, Taurus 1°→Pisces,
    /// Taurus 29°→Capricornus.
    /// </summary>
    public static ZodiacName GetRudramsaSign(double siderealLongitude)
    {
        var normalized = Normalize(siderealLongitude);
        var signIndex = (int)(normalized / DegreesPerSign);
        var degreesInSign = normalized % DegreesPerSign;
        var part = Math.Min((int)(degreesInSign / (DegreesPerSign / 11.0)), 10);   // 0..10
        var target = (12 - signIndex + part) % 12;
        return (ZodiacName)target;
    }

    /// <summary>
    /// Whole Sign house distance from <paramref name="fromSign"/> to <paramref name="toSign"/>,
    /// 1-12 (same sign = house 1). Shared by D1/D9 house counting and Lagna/Sun/Moon reckoning.
    /// </summary>
    public static int CountFromSignToSign(ZodiacName fromSign, ZodiacName toSign) =>
        (((int)toSign - (int)fromSign + 12) % 12) + 1;

    /// <summary>
    /// Nakshatra-lord cycle (Vimshottari Dasha order), Aswini's lord first — nakshatraIndex % 9
    /// indexes directly into this. Single source of truth for VimshottariDashaCalculator's dasha
    /// sequencing AND ChartAnalyzer's per-planet NakshatraLordPlanet (2026-08-28) — both are the
    /// same classical 9-lord cycle, just consumed differently.
    /// </summary>
    public static readonly IReadOnlyList<PlanetName> NakshatraLordOrder = new[]
    {
        PlanetName.Ketu, PlanetName.Venus, PlanetName.Sun, PlanetName.Moon, PlanetName.Mars,
        PlanetName.Rahu, PlanetName.Jupiter, PlanetName.Saturn, PlanetName.Mercury
    };

    /// <summary>The classical ruling planet (Vimshottari lord) of the nakshatra containing this longitude. Independent of which divisional chart is reading the longitude — same real nakshatra either way.</summary>
    public static PlanetName GetNakshatraLord(double siderealLongitude)
    {
        var (nakshatraIndex, _) = GetNakshatraIndexAndFractionElapsed(siderealLongitude);
        return NakshatraLordOrder[nakshatraIndex % 9];
    }

    /// <summary>
    /// Vimshottari dasha years per planet — the classical 120-year total split. Single source of
    /// truth for VimshottariDashaCalculator AND the KP nakshatra sub-lord division
    /// (GetNakshatraSubLord), the same way NakshatraLordOrder is shared. Ordered map, not positional.
    /// </summary>
    public static readonly IReadOnlyDictionary<PlanetName, int> VimshottariYearsByLord = new Dictionary<PlanetName, int>
    {
        [PlanetName.Ketu] = 7, [PlanetName.Venus] = 20, [PlanetName.Sun] = 6, [PlanetName.Moon] = 10,
        [PlanetName.Mars] = 7, [PlanetName.Rahu] = 18, [PlanetName.Jupiter] = 16, [PlanetName.Saturn] = 19,
        [PlanetName.Mercury] = 17
    };

    /// <summary>
    /// The 27 nakshatra names in sidereal order, spelled to match tbl_Nakshatras.NakshatraName
    /// exactly (migration 021 seed). Indexed by the ConstellationName enum's integer value. This —
    /// not ConstellationName.ToString() — is what gets persisted to tbl_Chart_KeyDetails.Nakshatra,
    /// so the stored value joins cleanly to the reference table.
    /// </summary>
    public static readonly IReadOnlyList<string> NakshatraCanonicalNames = new[]
    {
        "Ashwini", "Bharani", "Krittika", "Rohini", "Mrigashira", "Ardra", "Punarvasu", "Pushya",
        "Ashlesha", "Magha", "Purva Phalguni", "Uttara Phalguni", "Hasta", "Chitra", "Swati",
        "Vishakha", "Anuradha", "Jyeshtha", "Mula", "Purva Ashadha", "Uttara Ashadha", "Shravana",
        "Dhanishta", "Shatabhisha", "Purva Bhadrapada", "Uttara Bhadrapada", "Revati"
    };

    /// <summary>Canonical (reference-table) name for a nakshatra enum value.</summary>
    public static string GetNakshatraName(ConstellationName nakshatra) => NakshatraCanonicalNames[(int)nakshatra];

    /// <summary>Overall pada index 0-107 (each pada = 3°20'), across the whole zodiac. +1 gives tbl_NakshatraPadas.Id.</summary>
    public static int GetOverallPadaIndex(double siderealLongitude) =>
        Math.Min((int)(Normalize(siderealLongitude) / DegreesPerPada), 107);

    /// <summary>
    /// KP nakshatra sub-lord (Vimshottari level 2) at this longitude. Within the 13°20' nakshatra the
    /// nine sub-divisions run in Vimshottari order starting from the nakshatra's own lord, each of
    /// width (years/120) x 13°20'. Same construction as tbl_NakshatraSubLords' seed and
    /// VimshottariDashaCalculator.FindSlot. Verified: 218.72° (Anuradha, +5.39°) -> Venus.
    /// </summary>
    public static PlanetName GetNakshatraSubLord(double siderealLongitude)
    {
        var normalized = Normalize(siderealLongitude);
        var nakshatraIndex = Math.Min((int)(normalized / DegreesPerNakshatra), 26);
        var degreesIntoNakshatra = normalized - nakshatraIndex * DegreesPerNakshatra;
        var startLordIndex = nakshatraIndex % 9;

        var cumulative = 0.0;
        for (var slot = 0; slot < 9; slot++)
        {
            var lord = NakshatraLordOrder[(startLordIndex + slot) % 9];
            var subWidth = VimshottariYearsByLord[lord] / 120.0 * DegreesPerNakshatra;
            if (degreesIntoNakshatra < cumulative + subWidth || slot == 8)
                return lord;
            cumulative += subWidth;
        }
        throw new InvalidOperationException("Unreachable — 9 sub-lord slots span the full nakshatra.");
    }

    /// <summary>
    /// A planet's longitude remapped into a divisional (varga) chart's own 0-360° space —
    /// <c>(realLongitude × divisionalNumber) mod 360</c>, e.g. <paramref name="divisionalNumber"/> = 9
    /// for Navamsa. Linear-equivalent to <see cref="GetNavamsaSign"/>'s segment walk (verified: both
    /// agree on which sign every degree falls into) but keeps the continuous degree instead of
    /// collapsing to a sign bucket, so it can drive varga-relative proximity checks — e.g. combustion
    /// evaluated within D9 rather than borrowing the D1 (real) distance to the Sun (2026-08-28 fix).
    /// </summary>
    public static double GetVargaLongitude(double siderealLongitude, int divisionalNumber) =>
        Normalize(siderealLongitude * divisionalNumber);
}
