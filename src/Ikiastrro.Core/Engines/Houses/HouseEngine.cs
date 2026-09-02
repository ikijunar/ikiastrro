using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.Houses;

/// <summary>
/// Whole-sign house geometry: house→sign from the Ascendant, sign→lord, and the per-house
/// lordship rows (tbl_Chart_HouseLords). Extracted verbatim from ChartAnalyzer (2026-09-02
/// engine reorg) — no rule changed; the sign/lord tables moved here from ClassicalDignity
/// because both House and Dignity need "who lords this sign" and House is the lower layer.
/// </summary>
public static class HouseEngine
{
    /// <summary>
    /// Classical sign rulership (planet → the signs it owns). Moved here verbatim from
    /// ClassicalDignity/DignityEngine in the 2026-09-02 engine reorg: it is the backing table for
    /// GetSignLord, which is house geometry's lower-layer primitive. DignityEngine still reads it
    /// for its own-sign / Swakshetra checks (single source of truth — deliberately not duplicated).
    /// </summary>
    internal static readonly Dictionary<string, ZodiacName[]> OwnSigns = new()
    {
        ["Sun"] = new[] { ZodiacName.Leo },
        ["Moon"] = new[] { ZodiacName.Cancer },
        ["Mars"] = new[] { ZodiacName.Aries, ZodiacName.Scorpio },
        ["Mercury"] = new[] { ZodiacName.Gemini, ZodiacName.Virgo },
        ["Jupiter"] = new[] { ZodiacName.Sagittarius, ZodiacName.Pisces },
        ["Venus"] = new[] { ZodiacName.Taurus, ZodiacName.Libra },
        ["Saturn"] = new[] { ZodiacName.Capricornus, ZodiacName.Aquarius }
    };

    /// <summary>Zodiacal order the whole-sign house count walks. Shared with DignityEngine's
    /// SignDistance (Tatkalika Maitri) — same table, moved here with GetHouseSign.</summary>
    internal static readonly ZodiacName[] SignOrder =
    {
        ZodiacName.Aries, ZodiacName.Taurus, ZodiacName.Gemini, ZodiacName.Cancer,
        ZodiacName.Leo, ZodiacName.Virgo, ZodiacName.Libra, ZodiacName.Scorpio,
        ZodiacName.Sagittarius, ZodiacName.Capricornus, ZodiacName.Aquarius, ZodiacName.Pisces
    };

    /// <summary>Ruler of a sign — reverse lookup of OwnSigns.</summary>
    public static string GetSignLord(ZodiacName sign) =>
        OwnSigns.First(kv => kv.Value.Contains(sign)).Key;

    /// <summary>The sign occupying a given house (1-12), Whole Sign counted from the Ascendant sign.</summary>
    public static ZodiacName GetHouseSign(ZodiacName ascendantSign, int houseNumber)
    {
        var ascendantIndex = Array.IndexOf(SignOrder, ascendantSign);
        return SignOrder[(ascendantIndex + houseNumber - 1) % 12];
    }

    public static List<ChartHouseLord> BuildHouseLords(
        ChartAnalysisInput input,
        IReadOnlyDictionary<string, ChartKeyDetail> placementByPlanet)
    {
        var houseLords = new List<ChartHouseLord>();
        for (var houseNumber = 1; houseNumber <= 12; houseNumber++)
        {
            var houseSign = GetHouseSign(input.AscendantSign, houseNumber);
            var lordPlanet = GetSignLord(houseSign);
            var lordPlacement = placementByPlanet[lordPlanet];

            houseLords.Add(new ChartHouseLord
            {
                HouseNumber = houseNumber,
                HouseSign = houseSign.ToString(),
                HouseSignId = AstroIds.SignId(houseSign),
                LordPlanet = lordPlanet,
                LordPlanetId = AstroIds.PlanetId(Enum.Parse<PlanetName>(lordPlanet)),
                LordPlacedInHouseFromLagna = lordPlacement.HouseNumberFromLagna,
                LordPlacedInHouseFromSun = lordPlacement.HouseNumberFromSun,
                LordPlacedInHouseFromMoon = lordPlacement.HouseNumberFromMoon,
                LordPlacedInSign = lordPlacement.Sign,
                LordPlacedInSignId = AstroIds.SignId(Enum.Parse<ZodiacName>(lordPlacement.Sign)),
                LordDignityStatus = lordPlacement.DignityStatus
            });
        }
        return houseLords;
    }
}
