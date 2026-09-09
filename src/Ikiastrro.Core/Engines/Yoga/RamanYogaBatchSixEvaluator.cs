using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.Engines.Strength;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.Yoga;

/// <summary>Raman combinations 61–70, with explicit D9 and lunar-context gates.</summary>
public static class RamanYogaBatchSixEvaluator
{
    private static readonly PlanetName[] Benefics =
        [PlanetName.Moon, PlanetName.Mercury, PlanetName.Jupiter, PlanetName.Venus];

    public static IReadOnlyList<ContextualYogaResult> Evaluate(
        ChartAnalysisInput d1,
        ChartAnalysisInput? d9 = null,
        bool? isDayBirth = null,
        bool? isWaxingMoon = null,
        bool? isFullMoon = null)
        => new[]
        {
            Row("YOGA_SIVA", 61, 89, 101, Siva(d1)),
            d9 is null ? Missing("YOGA_VISHNU", 62, 90, 102, "D9 placement is required.") : Row("YOGA_VISHNU", 62, 90, 102, Vishnu(d1, d9)),
            Row("YOGA_BRAHMA", 63, 93, 105, Brahma(d1)),
            Row("YOGA_INDRA", 64, 95, 107, Indra(d1)),
            Row("YOGA_RAVI", 65, 95, 107, Ravi(d1)),
            GarudaRow(d1, d9, isDayBirth, isWaxingMoon),
            Row("YOGA_GO", 67, 99, 111, Go(d1)),
            GolaRow(d1, d9, isFullMoon),
            Row("YOGA_THRILOCHANA", 69, 101, 113, Thrilochana(d1)),
            Row("YOGA_KULAVARDHANA", 70, 101, 113, Kulavardhana(d1))
        };

    private static bool Siva(ChartAnalysisInput c) =>
        Find(c, Lord(c, 5))?.HouseNumber == 9
        && Find(c, Lord(c, 9))?.HouseNumber == 10
        && Find(c, Lord(c, 10))?.HouseNumber == 5;

    private static bool Vishnu(ChartAnalysisInput d1, ChartAnalysisInput d9)
    {
        var ninthLord = Lord(d1, 9);
        var ninthInD9 = Find(d9, ninthLord);
        if (ninthInD9 is null) return false;
        var navamsaLord = Enum.Parse<PlanetName>(HouseEngine.GetSignLord(Enum.Parse<ZodiacName>(ninthInD9.Sign)));
        var secondSign = HouseSign(d1, 2);
        return Find(d1, navamsaLord)?.Sign == secondSign
            && Find(d1, Lord(d1, 10))?.Sign == secondSign
            && Find(d1, ninthLord)?.Sign == secondSign;
    }

    private static bool Brahma(ChartAnalysisInput c)
    {
        var ninth = Find(c, Lord(c, 9)); var eleventh = Find(c, Lord(c, 11));
        var jupiter = Find(c, PlanetName.Jupiter); var venus = Find(c, PlanetName.Venus);
        var mercury = Find(c, PlanetName.Mercury); var lagna = Find(c, Lord(c, 1)); var tenth = Find(c, Lord(c, 10));
        return ninth is not null && eleventh is not null && jupiter is not null && venus is not null && mercury is not null
            && IsKendra(ninth.Sign, jupiter.Sign) && IsKendra(eleventh.Sign, venus.Sign)
            && ((lagna is not null && IsKendra(lagna.Sign, mercury.Sign))
                || (tenth is not null && IsKendra(tenth.Sign, mercury.Sign)));
    }

    private static bool Indra(ChartAnalysisInput c)
    {
        var fifth = Lord(c, 5); var eleventh = Lord(c, 11);
        return fifth != eleventh && Find(c, fifth)?.HouseNumber == 11
            && Find(c, eleventh)?.HouseNumber == 5 && Find(c, PlanetName.Moon)?.HouseNumber == 5;
    }

    private static bool Ravi(ChartAnalysisInput c)
    {
        var tenth = Find(c, Lord(c, 10)); var saturn = Find(c, PlanetName.Saturn);
        return Find(c, PlanetName.Sun)?.HouseNumber == 10
            && tenth?.HouseNumber == 3 && saturn?.HouseNumber == 3 && tenth.Sign == saturn.Sign;
    }

    private static ContextualYogaResult GarudaRow(ChartAnalysisInput d1, ChartAnalysisInput? d9, bool? day, bool? waxing)
    {
        const string missing = "D9 placement plus daytime and waxing-Moon context are required.";
        if (d9 is null || day is null || waxing is null) return Missing("YOGA_GARUDA", 66, 97, 109, missing);
        var moonD9 = Find(d9, PlanetName.Moon);
        if (moonD9 is null) return Row("YOGA_GARUDA", 66, 97, 109, false);
        var navamsaLord = Enum.Parse<PlanetName>(HouseEngine.GetSignLord(Enum.Parse<ZodiacName>(moonD9.Sign)));
        return Row("YOGA_GARUDA", 66, 97, 109, day.Value && waxing.Value && Exalted(d1, navamsaLord));
    }

    private static bool Go(ChartAnalysisInput c)
    {
        var jupiter = Find(c, PlanetName.Jupiter); var secondLord = Find(c, Lord(c, 2));
        return jupiter is not null && secondLord?.Sign == jupiter.Sign
            && Dignity(c, PlanetName.Jupiter).DignityTypeCode == "MOOLATRIKONA"
            && Exalted(c, Lord(c, 1));
    }

    private static ContextualYogaResult GolaRow(ChartAnalysisInput d1, ChartAnalysisInput? d9, bool? fullMoon)
    {
        if (d9 is null || fullMoon is null)
            return Missing("YOGA_GOLA", 68, 100, 112, "D9 placement and full-Moon context are required.");
        var moon = Find(d1, PlanetName.Moon);
        var present = fullMoon.Value && moon?.HouseNumber == 9
            && Find(d1, PlanetName.Jupiter)?.Sign == moon.Sign
            && Find(d1, PlanetName.Venus)?.Sign == moon.Sign
            && Find(d9, PlanetName.Mercury)?.HouseNumber == 1;
        return Row("YOGA_GOLA", 68, 100, 112, present);
    }

    private static bool Thrilochana(ChartAnalysisInput c)
    {
        var sun = Find(c, PlanetName.Sun); var moon = Find(c, PlanetName.Moon); var mars = Find(c, PlanetName.Mars);
        return sun is not null && moon is not null && mars is not null
            && IsTrine(sun.Sign, moon.Sign) && IsTrine(moon.Sign, mars.Sign) && IsTrine(mars.Sign, sun.Sign);
    }

    private static bool Kulavardhana(ChartAnalysisInput c)
    {
        var origins = new[] { c.AscendantSign.ToString(), Find(c, PlanetName.Sun)?.Sign, Find(c, PlanetName.Moon)?.Sign };
        return origins.All(origin => origin is not null && Benefics.Any(p => Find(c, p)?.Sign == RelativeSign(origin, 5)));
    }

    private static bool Exalted(ChartAnalysisInput c, PlanetName p) => Dignity(c, p).DignityTypeCode == "EXALTED";
    private static PvrDignityResult Dignity(ChartAnalysisInput c, PlanetName p)
    {
        var x = Find(c, p); if (x is null) return new("MISSING", 0, "", null, null, 0);
        var longitude = x.VargaLongitudeDegrees ?? x.NirayanaLongitudeDegrees;
        return PvrDignityEvaluator.Evaluate(p, Enum.Parse<ZodiacName>(x.Sign),
            longitude is null ? 15 : ((longitude.Value % 30) + 30) % 30);
    }
    private static bool IsKendra(string origin, string target) => Distance(origin, target) is 1 or 4 or 7 or 10;
    private static bool IsTrine(string origin, string target) => Distance(origin, target) is 1 or 5 or 9;
    private static int Distance(string origin, string target) => (((int)Enum.Parse<ZodiacName>(target) - (int)Enum.Parse<ZodiacName>(origin) + 12) % 12) + 1;
    private static string RelativeSign(string origin, int distance) => ((ZodiacName)(((int)Enum.Parse<ZodiacName>(origin) + distance - 1) % 12)).ToString();
    private static string HouseSign(ChartAnalysisInput c, int house) => HouseEngine.GetHouseSign(c.AscendantSign, house).ToString();
    private static PlanetName Lord(ChartAnalysisInput c, int house) => Enum.Parse<PlanetName>(HouseEngine.GetSignLord(HouseEngine.GetHouseSign(c.AscendantSign, house)));
    private static PlanetPosition? Find(ChartAnalysisInput c, PlanetName p) => c.Planets.SingleOrDefault(x => x.Planet == p.ToString());
    private static ContextualYogaResult Row(string code, int number, int printed, int scan, bool present) =>
        new(code, present, "EVALUATED", "SRC_RAMAN_300_COMBINATIONS", $"RAMAN_300_{number:000}",
            $"combination {number}; printed p.{printed}; scan p.{scan}");
    private static ContextualYogaResult Missing(string code, int number, int printed, int scan, string note) =>
        new(code, null, "NOT_EVALUATED", "SRC_RAMAN_300_COMBINATIONS", $"RAMAN_300_{number:000}",
            $"combination {number}; printed p.{printed}; scan p.{scan}", note);
}
