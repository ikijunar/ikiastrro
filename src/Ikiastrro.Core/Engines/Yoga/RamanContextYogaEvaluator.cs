using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.Engines.Strength;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.Yoga;

public sealed record ContextualYogaResult(
    string YogaCode, bool? Present, string EvaluationStatus,
    string SourceRefCode, string SourceVariantCode, string SourceLocator, string? Notes = null);

/// <summary>Raman combinations 26–31; D9-dependent rules remain explicitly unevaluated without D9.</summary>
public static class RamanContextYogaEvaluator
{
    public static IReadOnlyList<ContextualYogaResult> Evaluate(ChartAnalysisInput d1, ChartAnalysisInput? d9 = null)
        => new[]
        {
            Row("YOGA_PUSHKALA", 26, 56, 68, Pushkala(d1)),
            Row("YOGA_LAKSHMI", 27, 58, 70, Lakshmi(d1)),
            d9 is null ? MissingD9("YOGA_GAURI", 28, 60, 72) : Row("YOGA_GAURI", 28, 60, 72, Gauri(d1, d9)),
            d9 is null ? MissingD9("YOGA_BHARATHI", 29, 62, 74) : Row("YOGA_BHARATHI", 29, 62, 74, Bharathi(d1, d9)),
            Row("YOGA_CHAPA", 30, 63, 75, Chapa(d1)),
            Row("YOGA_SREENATHA", 31, 64, 76, Sreenatha(d1))
        };

    private static bool Pushkala(ChartAnalysisInput chart)
    {
        var lagnaLord = Lord(chart, 1);
        var moon = Find(chart, PlanetName.Moon);
        var lagnaLordPosition = Find(chart, lagnaLord);
        if (moon is null || lagnaLordPosition?.Sign != moon.Sign) return false;

        var moonDispositor = Enum.Parse<PlanetName>(HouseEngine.GetSignLord(Enum.Parse<ZodiacName>(moon.Sign)));
        var dispositorPosition = Find(chart, moonDispositor);
        if (dispositorPosition is null || !Aspects(moonDispositor, dispositorPosition.Sign, chart.AscendantSign.ToString())) return false;
        var inKendra = dispositorPosition.HouseNumber is 1 or 4 or 7 or 10;
        var intimateFriend = Dignity(chart, moonDispositor, dispositorPosition).CompoundRelationshipCode == "ADHIMITRA";
        return (inKendra || intimateFriend) && chart.Planets.Any(x => x.HouseNumber == 1
            && Enum.TryParse<PlanetName>(x.Planet, out var planet) && Strong(chart, planet, x));
    }

    private static bool Lakshmi(ChartAnalysisInput chart)
    {
        var lagnaLord = Lord(chart, 1); var ninthLord = Lord(chart, 9);
        var lagna = Find(chart, lagnaLord); var ninth = Find(chart, ninthLord);
        return Strong(chart, lagnaLord, lagna) && Strong(chart, ninthLord, ninth)
            && ninth?.HouseNumber is 1 or 4 or 5 or 7 or 9 or 10;
    }

    private static bool Gauri(ChartAnalysisInput d1, ChartAnalysisInput d9)
    {
        var tenthLord = Lord(d1, 10);
        var tenthLordD9 = Find(d9, tenthLord); if (tenthLordD9 is null) return false;
        var navamsaLord = Enum.Parse<PlanetName>(HouseEngine.GetSignLord(Enum.Parse<ZodiacName>(tenthLordD9.Sign)));
        var navamsaLordD1 = Find(d1, navamsaLord); var lagnaLordD1 = Find(d1, Lord(d1, 1));
        return navamsaLordD1?.HouseNumber == 10
            && Dignity(d1, navamsaLord, navamsaLordD1).DignityTypeCode == "EXALTED"
            && lagnaLordD1?.Sign == navamsaLordD1.Sign;
    }

    private static bool Bharathi(ChartAnalysisInput d1, ChartAnalysisInput d9)
    {
        var ninthLordPosition = Find(d1, Lord(d1, 9)); if (ninthLordPosition is null) return false;
        foreach (var house in new[] { 2, 5, 11 })
        {
            var houseLordD9 = Find(d9, Lord(d1, house)); if (houseLordD9 is null) continue;
            var navamsaLord = Enum.Parse<PlanetName>(HouseEngine.GetSignLord(Enum.Parse<ZodiacName>(houseLordD9.Sign)));
            var navamsaLordD1 = Find(d1, navamsaLord);
            if (navamsaLordD1 is not null
                && Dignity(d1, navamsaLord, navamsaLordD1).DignityTypeCode == "EXALTED"
                && navamsaLordD1.Sign == ninthLordPosition.Sign) return true;
        }
        return false;
    }

    private static bool Chapa(ChartAnalysisInput chart)
    {
        var lagnaLord = Lord(chart, 1); var fourthLord = Lord(chart, 4); var tenthLord = Lord(chart, 10);
        var lagna = Find(chart, lagnaLord); var fourth = Find(chart, fourthLord); var tenth = Find(chart, tenthLord);
        return lagna is not null && Dignity(chart, lagnaLord, lagna).DignityTypeCode == "EXALTED"
            && fourth?.HouseNumber == 10 && tenth?.HouseNumber == 4;
    }

    private static bool Sreenatha(ChartAnalysisInput chart)
    {
        var seventhLord = Lord(chart, 7); var seventh = Find(chart, seventhLord);
        var tenth = Find(chart, Lord(chart, 10)); var ninth = Find(chart, Lord(chart, 9));
        return seventh?.HouseNumber == 10
            && Dignity(chart, seventhLord, seventh).DignityTypeCode == "EXALTED"
            && tenth is not null && tenth.Sign == ninth?.Sign;
    }

    private static PlanetName Lord(ChartAnalysisInput chart, int house)
        => Enum.Parse<PlanetName>(HouseEngine.GetSignLord(HouseEngine.GetHouseSign(chart.AscendantSign, house)));
    private static PlanetPosition? Find(ChartAnalysisInput chart, PlanetName planet)
        => chart.Planets.SingleOrDefault(x => x.Planet == planet.ToString());
    private static bool Strong(ChartAnalysisInput chart, PlanetName planet, PlanetPosition? position)
        => position is not null && Dignity(chart, planet, position).DignityTypeCode is "OWN" or "MOOLATRIKONA" or "EXALTED";
    private static PvrDignityResult Dignity(ChartAnalysisInput chart, PlanetName planet, PlanetPosition position)
    {
        var signs = chart.Planets.Where(x => Enum.TryParse<PlanetName>(x.Planet, out _))
            .ToDictionary(x => x.Planet, x => Enum.Parse<ZodiacName>(x.Sign));
        var longitude = position.VargaLongitudeDegrees ?? position.NirayanaLongitudeDegrees;
        var degree = longitude is null ? 15d : ((longitude.Value % 30d) + 30d) % 30d;
        return PvrDignityEvaluator.Evaluate(planet, Enum.Parse<ZodiacName>(position.Sign), degree, signs);
    }
    private static bool Aspects(PlanetName planet, string from, string target)
    {
        var distance = (((int)Enum.Parse<ZodiacName>(target) - (int)Enum.Parse<ZodiacName>(from) + 12) % 12) + 1;
        return distance == 7 || planet == PlanetName.Mars && distance is 4 or 8
            || planet == PlanetName.Jupiter && distance is 5 or 9
            || planet == PlanetName.Saturn && distance is 3 or 10;
    }
    private static ContextualYogaResult Row(string code, int number, int printed, int scan, bool present)
        => new(code, present, "EVALUATED", "SRC_RAMAN_300_COMBINATIONS", $"RAMAN_300_{number:000}",
            $"combination {number}; printed p.{printed}; scan p.{scan}");
    private static ContextualYogaResult MissingD9(string code, int number, int printed, int scan)
        => new(code, null, "NOT_EVALUATED", "SRC_RAMAN_300_COMBINATIONS", $"RAMAN_300_{number:000}",
            $"combination {number}; printed p.{printed}; scan p.{scan}", "D9 placement is required.");
}
