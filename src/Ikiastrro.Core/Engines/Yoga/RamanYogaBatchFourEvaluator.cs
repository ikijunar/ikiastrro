using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.Engines.Strength;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.Yoga;

/// <summary>Raman combinations 44–50, retaining the two source variants of Kalanidhi.</summary>
public static class RamanYogaBatchFourEvaluator
{
    public static IReadOnlyList<ContextualYogaResult> Evaluate(ChartAnalysisInput d1, ChartAnalysisInput? d9 = null)
    {
        var rows = new List<ContextualYogaResult>
        {
            Row("YOGA_SANKHA", 44, 65, 77, Sankha(d1)),
            Row("YOGA_BHERI", 45, 68, 80, Bheri(d1)),
            d9 is null ? MissingD9("YOGA_MRIDANGA", 46, 68, 80) : Row("YOGA_MRIDANGA", 46, 68, 80, Mridanga(d1, d9)),
            Row("YOGA_PARIJATHA", 47, 70, 82, Parijatha(d1)),
            Row("YOGA_GAJA", 48, 71, 83, Gaja(d1)),
            Variant("YOGA_KALANIDHI", "RAMAN_300_049_CLASSICAL", 73, 85, KalanidhiClassical(d1),
                "Classical definition stated first by Raman."),
            Variant("YOGA_KALANIDHI", "RAMAN_300_049_OBSERVED", 73, 85, KalanidhiObserved(d1),
                "Raman's broader observational extension, retained separately."),
            Row("YOGA_AMSAVATARA", 50, 75, 87, Amsavatara(d1))
        };
        return rows;
    }

    private static bool Sankha(ChartAnalysisInput chart)
    {
        var fifth = Lord(chart, 5); var sixth = Lord(chart, 6);
        var a = Find(chart, fifth); var b = Find(chart, sixth);
        return fifth != sixth && a is not null && b is not null
            && IsKendraDistance(a.Sign, b.Sign) && Strong(chart, Lord(chart, 1));
    }

    private static bool Bheri(ChartAnalysisInput chart)
    {
        var lagna = Find(chart, Lord(chart, 1)); var venus = Find(chart, PlanetName.Venus); var jupiter = Find(chart, PlanetName.Jupiter);
        return lagna is not null && venus is not null && jupiter is not null
            && IsKendraDistance(lagna.Sign, venus.Sign) && IsKendraDistance(venus.Sign, jupiter.Sign)
            && IsKendraDistance(jupiter.Sign, lagna.Sign) && Strong(chart, Lord(chart, 9));
    }

    private static bool Mridanga(ChartAnalysisInput d1, ChartAnalysisInput d9)
    {
        if (!Strong(d1, Lord(d1, 1))) return false;
        foreach (var position in d1.Planets.Where(IsClassical))
        {
            var planet = Enum.Parse<PlanetName>(position.Planet);
            if (Dignity(d1, planet).DignityTypeCode != "EXALTED") continue;
            var d9Position = Find(d9, planet); if (d9Position is null) continue;
            var d9Lord = Enum.Parse<PlanetName>(HouseEngine.GetSignLord(Enum.Parse<ZodiacName>(d9Position.Sign)));
            var lordPosition = Find(d1, d9Lord); if (lordPosition is null || lordPosition.HouseNumber is not (1 or 4 or 5 or 7 or 9 or 10)) continue;
            var dignity = Dignity(d1, d9Lord);
            if (dignity.DignityTypeCode is "OWN" or "MOOLATRIKONA" or "EXALTED" || dignity.RelationshipScore is > 0) return true;
        }
        return false;
    }

    private static bool Parijatha(ChartAnalysisInput chart)
    {
        var lagnaLordPosition = Find(chart, Lord(chart, 1)); if (lagnaLordPosition is null) return false;
        var firstDispositor = Enum.Parse<PlanetName>(HouseEngine.GetSignLord(Enum.Parse<ZodiacName>(lagnaLordPosition.Sign)));
        var firstPosition = Find(chart, firstDispositor); if (firstPosition is null) return false;
        var secondDispositor = Enum.Parse<PlanetName>(HouseEngine.GetSignLord(Enum.Parse<ZodiacName>(firstPosition.Sign)));
        var secondPosition = Find(chart, secondDispositor); if (secondPosition is null) return false;
        return secondPosition.HouseNumber is 1 or 4 or 5 or 7 or 9 or 10 || Strong(chart, secondDispositor);
    }

    private static bool Gaja(ChartAnalysisInput chart)
    {
        var seventhLord = Lord(chart, 7); var eleventhLord = Lord(chart, 11);
        var seventh = Find(chart, seventhLord); var moon = Find(chart, PlanetName.Moon); var eleventh = Find(chart, eleventhLord);
        return seventh?.HouseNumber == 11 && moon?.Sign == seventh.Sign && eleventh is not null
            && Aspects(eleventhLord, eleventh.Sign, seventh.Sign);
    }

    private static bool KalanidhiClassical(ChartAnalysisInput chart)
    {
        var jupiter = Find(chart, PlanetName.Jupiter); if (jupiter?.HouseNumber is not (2 or 5)) return false;
        var sign = Enum.Parse<ZodiacName>(jupiter.Sign);
        if (sign is not (ZodiacName.Gemini or ZodiacName.Virgo or ZodiacName.Taurus or ZodiacName.Libra)) return false;
        return Influences(chart, PlanetName.Mercury, jupiter.Sign) || Influences(chart, PlanetName.Venus, jupiter.Sign);
    }

    private static bool KalanidhiObserved(ChartAnalysisInput chart)
    {
        var jupiter = Find(chart, PlanetName.Jupiter); if (jupiter?.HouseNumber is not (2 or 5 or 9)) return false;
        return Find(chart, PlanetName.Mercury)?.Sign == jupiter.Sign || Find(chart, PlanetName.Venus)?.Sign == jupiter.Sign;
    }

    private static bool Amsavatara(ChartAnalysisInput chart)
    {
        var movableLagna = chart.AscendantSign is ZodiacName.Aries or ZodiacName.Cancer or ZodiacName.Libra or ZodiacName.Capricornus;
        var venus = Find(chart, PlanetName.Venus); var jupiter = Find(chart, PlanetName.Jupiter); var saturn = Find(chart, PlanetName.Saturn);
        return movableLagna && venus?.HouseNumber is 1 or 4 or 7 or 10 && jupiter?.HouseNumber is 1 or 4 or 7 or 10
            && saturn?.HouseNumber is 1 or 4 or 7 or 10 && Dignity(chart, PlanetName.Saturn).DignityTypeCode == "EXALTED";
    }

    private static bool Influences(ChartAnalysisInput chart, PlanetName planet, string target)
    {
        var position = Find(chart, planet);
        return position is not null && (position.Sign == target || Aspects(planet, position.Sign, target));
    }
    private static bool Aspects(PlanetName planet, string from, string target)
    {
        var d = Distance(from, target);
        return d == 7 || planet == PlanetName.Mars && d is 4 or 8 || planet == PlanetName.Jupiter && d is 5 or 9 || planet == PlanetName.Saturn && d is 3 or 10;
    }
    private static bool IsKendraDistance(string a, string b) => Distance(a, b) is 1 or 4 or 7 or 10;
    private static int Distance(string from, string to) => (((int)Enum.Parse<ZodiacName>(to) - (int)Enum.Parse<ZodiacName>(from) + 12) % 12) + 1;
    private static PlanetName Lord(ChartAnalysisInput chart, int house) => Enum.Parse<PlanetName>(HouseEngine.GetSignLord(HouseEngine.GetHouseSign(chart.AscendantSign, house)));
    private static PlanetPosition? Find(ChartAnalysisInput chart, PlanetName planet) => chart.Planets.SingleOrDefault(x => x.Planet == planet.ToString());
    private static bool Strong(ChartAnalysisInput chart, PlanetName planet) => Dignity(chart, planet).DignityTypeCode is "OWN" or "MOOLATRIKONA" or "EXALTED";
    private static PvrDignityResult Dignity(ChartAnalysisInput chart, PlanetName planet)
    {
        var p = Find(chart, planet); if (p is null) return new("MISSING", 0, "", null, null, 0);
        var lon = p.VargaLongitudeDegrees ?? p.NirayanaLongitudeDegrees;
        return PvrDignityEvaluator.Evaluate(planet, Enum.Parse<ZodiacName>(p.Sign), lon is null ? 15 : ((lon.Value % 30) + 30) % 30);
    }
    private static bool IsClassical(PlanetPosition p) => Enum.TryParse<PlanetName>(p.Planet, out var x) && x is not PlanetName.Rahu and not PlanetName.Ketu;
    private static ContextualYogaResult Row(string code, int number, int printed, int scan, bool present) => Variant(code, $"RAMAN_300_{number:000}", printed, scan, present, null);
    private static ContextualYogaResult Variant(string code, string variant, int printed, int scan, bool present, string? notes)
        => new(code, present, "EVALUATED", "SRC_RAMAN_300_COMBINATIONS", variant, $"combination {variant.Substring(10, 3)}; printed p.{printed}; scan p.{scan}", notes);
    private static ContextualYogaResult MissingD9(string code, int number, int printed, int scan)
        => new(code, null, "NOT_EVALUATED", "SRC_RAMAN_300_COMBINATIONS", $"RAMAN_300_{number:000}", $"combination {number}; printed p.{printed}; scan p.{scan}", "D9 placement is required.");
}
