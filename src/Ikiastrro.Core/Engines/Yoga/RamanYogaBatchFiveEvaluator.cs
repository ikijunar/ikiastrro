using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.Engines.Strength;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.Yoga;

/// <summary>Raman combinations 51–60. Textual alternatives and qualified readings remain distinct.</summary>
public static class RamanYogaBatchFiveEvaluator
{
    private static readonly PlanetName[] Benefics = [PlanetName.Moon, PlanetName.Mercury, PlanetName.Jupiter, PlanetName.Venus];
    private static readonly PlanetName[] Malefics = [PlanetName.Sun, PlanetName.Mars, PlanetName.Saturn];
    private static readonly IReadOnlyDictionary<PlanetName, (ZodiacName Sign, double Degree)> DeepExaltation =
        new Dictionary<PlanetName, (ZodiacName, double)>
        {
            [PlanetName.Sun] = (ZodiacName.Aries, 10), [PlanetName.Moon] = (ZodiacName.Taurus, 3),
            [PlanetName.Mars] = (ZodiacName.Capricornus, 28), [PlanetName.Mercury] = (ZodiacName.Virgo, 15),
            [PlanetName.Jupiter] = (ZodiacName.Cancer, 5), [PlanetName.Venus] = (ZodiacName.Pisces, 27),
            [PlanetName.Saturn] = (ZodiacName.Libra, 20)
        };

    public static IReadOnlyList<ContextualYogaResult> Evaluate(ChartAnalysisInput d1, ChartAnalysisInput? d9 = null)
    {
        var rows = new List<ContextualYogaResult>
        {
            Variant("YOGA_HARIHARA_BRAHMA", "RAMAN_300_051_A", 76, 88, HariharaA(d1), "First alternative in Raman's definition."),
            Variant("YOGA_HARIHARA_BRAHMA", "RAMAN_300_051_B", 76, 88, HariharaB(d1), "Second alternative in Raman's definition."),
            Variant("YOGA_HARIHARA_BRAHMA", "RAMAN_300_051_C", 76, 88, HariharaC(d1), "Third alternative in Raman's definition."),
            Variant("YOGA_KUSUMA", "RAMAN_300_052_PRIMARY", 77, 89, KusumaPrimary(d1), "Uses the stated definition: Sun eighth from Moon; Raman's remarks contain a conflicting second-house gloss."),
            Variant("YOGA_KUSUMA", "RAMAN_300_052_RAO", 77, 89, KusumaRao(d1), "Alternative attributed by Raman to Prof. Rao; weak Moon is represented by debilitation."),
            Variant("YOGA_MATSYA", "RAMAN_300_053_STRICT", 79, 91, MatsyaStrict(d1), "Full stated definition."),
            Variant("YOGA_MATSYA", "RAMAN_300_053_RELAXED", 79, 91, MatsyaRelaxed(d1), "Raman's practical relaxation from the remarks."),
            d9 is null ? MissingD9("YOGA_KURMA", "RAMAN_300_054_NAVAMSA", 80, 92) : Variant("YOGA_KURMA", "RAMAN_300_054_NAVAMSA", 80, 92, KurmaNavamsa(d1, d9), "First alternative; Raman records reservations about its interpretation."),
            Variant("YOGA_KURMA", "RAMAN_300_054_RASI", 80, 92, KurmaRasi(d1), "Second alternative; Raman records reservations about its interpretation."),
            Row("YOGA_DEVENDRA", 55, 82, 94, Devendra(d1)),
            Row("YOGA_MAKUTA", 56, 83, 95, Makuta(d1)),
            d9 is null ? MissingD9("YOGA_CHANDIKA", "RAMAN_300_057", 84, 96) : Row("YOGA_CHANDIKA", 57, 84, 96, Chandika(d1, d9)),
            ExactDegreeRow("YOGA_JAYA", 58, 85, 97, d1, Jaya),
            ExactDegreeRow("YOGA_VIDYUT", 59, 87, 99, d1, Vidyut),
            Row("YOGA_GANDHARVA", 60, 88, 100, Gandharva(d1))
        };
        return rows;
    }

    private static bool HariharaA(ChartAnalysisInput c)
    {
        var second = Find(c, Lord(c, 2));
        return second is not null && HasAny(c, Benefics, RelativeSign(second.Sign, 8)) && HasAny(c, Benefics, RelativeSign(second.Sign, 12));
    }
    private static bool HariharaB(ChartAnalysisInput c)
    {
        var seventh = Find(c, Lord(c, 7));
        return seventh is not null && AtRelative(c, PlanetName.Jupiter, seventh.Sign, 4)
            && AtRelative(c, PlanetName.Moon, seventh.Sign, 9) && AtRelative(c, PlanetName.Mercury, seventh.Sign, 8);
    }
    private static bool HariharaC(ChartAnalysisInput c)
    {
        var lagna = Find(c, Lord(c, 1));
        return lagna is not null && AtRelative(c, PlanetName.Sun, lagna.Sign, 4)
            && AtRelative(c, PlanetName.Venus, lagna.Sign, 10) && AtRelative(c, PlanetName.Mars, lagna.Sign, 11);
    }
    private static bool KusumaPrimary(ChartAnalysisInput c)
    {
        var moon = Find(c, PlanetName.Moon);
        return Find(c, PlanetName.Jupiter)?.HouseNumber == 1 && moon?.HouseNumber == 7
            && Find(c, PlanetName.Sun) is { } sun && Distance(moon.Sign, sun.Sign) == 8;
    }
    private static bool KusumaRao(ChartAnalysisInput c)
    {
        var venus = Find(c, PlanetName.Venus); var moon = Find(c, PlanetName.Moon);
        return venus?.HouseNumber is 1 or 4 or 7 or 10 && IsFixed(venus.Sign)
            && moon?.HouseNumber is 1 or 5 or 9 && Dignity(c, PlanetName.Moon).DignityTypeCode == "DEBILITATED"
            && Find(c, PlanetName.Sun)?.HouseNumber == 10;
    }
    private static bool MatsyaStrict(ChartAnalysisInput c) => HasAny(c, Malefics, HouseSign(c, 1))
        && HasAny(c, Malefics, HouseSign(c, 9)) && HasAny(c, Malefics, HouseSign(c, 5))
        && HasAny(c, Benefics, HouseSign(c, 5)) && HasAny(c, Malefics, HouseSign(c, 4))
        && HasAny(c, Malefics, HouseSign(c, 8));
    private static bool MatsyaRelaxed(ChartAnalysisInput c) => HasAny(c, Malefics, HouseSign(c, 1))
        && HasAny(c, Malefics, HouseSign(c, 9)) && HasAny(c, Malefics, HouseSign(c, 5)) && HasAny(c, Benefics, HouseSign(c, 5));
    private static bool KurmaNavamsa(ChartAnalysisInput d1, ChartAnalysisInput d9) => Benefics.All(p =>
    {
        var rasi = Find(d1, p); var navamsa = Find(d9, p);
        return rasi?.HouseNumber is 5 or 6 or 7 && navamsa is not null && Favoured(d9, p);
    });
    private static bool KurmaRasi(ChartAnalysisInput c) => Benefics.All(p => Find(c, p)?.HouseNumber is 1 or 3 or 11 && Favoured(c, p));
    private static bool Devendra(ChartAnalysisInput c)
    {
        if (!IsFixed(c.AscendantSign.ToString())) return false;
        var l1 = Lord(c, 1); var l11 = Lord(c, 11); var l2 = Lord(c, 2); var l10 = Lord(c, 10);
        return Find(c, l1)?.HouseNumber == 11 && Find(c, l11)?.HouseNumber == 1
            && Find(c, l2)?.HouseNumber == 10 && Find(c, l10)?.HouseNumber == 2
            && new[] { l1, l11, l2, l10 }.Distinct().All(p => Strong(c, p));
    }
    private static bool Makuta(ChartAnalysisInput c)
    {
        var ninth = Find(c, Lord(c, 9)); var jupiter = Find(c, PlanetName.Jupiter);
        return ninth is not null && jupiter is not null && Distance(ninth.Sign, jupiter.Sign) == 9
            && HasAny(c, Benefics, RelativeSign(jupiter.Sign, 9)) && Find(c, PlanetName.Saturn)?.HouseNumber == 10;
    }
    private static bool Chandika(ChartAnalysisInput d1, ChartAnalysisInput d9)
    {
        if (!IsFixed(d1.AscendantSign.ToString())) return false;
        var sixth = Lord(d1, 6); var ninth = Lord(d1, 9);
        var sixthD9 = Find(d9, sixth); var ninthD9 = Find(d9, ninth); if (sixthD9 is null || ninthD9 is null) return false;
        var a = Enum.Parse<PlanetName>(HouseEngine.GetSignLord(Enum.Parse<ZodiacName>(sixthD9.Sign)));
        var b = Enum.Parse<PlanetName>(HouseEngine.GetSignLord(Enum.Parse<ZodiacName>(ninthD9.Sign)));
        var sun = Find(d1, PlanetName.Sun); var sixthD1 = Find(d1, sixth);
        return sun is not null && Find(d1, a)?.Sign == sun.Sign && Find(d1, b)?.Sign == sun.Sign
            && sixthD1 is not null && Aspects(sixth, sixthD1.Sign, d1.AscendantSign.ToString());
    }
    private static bool Jaya(ChartAnalysisInput c) => Dignity(c, Lord(c, 6)).DignityTypeCode == "DEBILITATED" && DeeplyExalted(c, Lord(c, 10));
    private static bool Vidyut(ChartAnalysisInput c)
    {
        var eleventh = Lord(c, 11); var lord = Find(c, eleventh); var venus = Find(c, PlanetName.Venus); var lagna = Find(c, Lord(c, 1));
        return lord is not null && venus?.Sign == lord.Sign && lagna is not null && DeeplyExalted(c, eleventh)
            && Distance(lagna.Sign, lord.Sign) is 1 or 4 or 7 or 10;
    }
    private static bool Gandharva(ChartAnalysisInput c)
    {
        var l10 = Find(c, Lord(c, 10)); var l1 = Find(c, Lord(c, 1)); var jupiter = Find(c, PlanetName.Jupiter);
        return l10?.HouseNumber is 3 or 7 or 11 && l1 is not null && jupiter is not null
            && (l1.Sign == jupiter.Sign || Aspects(PlanetName.Jupiter, jupiter.Sign, l1.Sign))
            && Strong(c, PlanetName.Sun) && Find(c, PlanetName.Moon)?.HouseNumber == 9;
    }

    private static bool DeeplyExalted(ChartAnalysisInput c, PlanetName p)
    {
        var position = Find(c, p); if (position is null || !DeepExaltation.TryGetValue(p, out var point)) return false;
        var longitude = position.VargaLongitudeDegrees ?? position.NirayanaLongitudeDegrees;
        return longitude is not null && Enum.Parse<ZodiacName>(position.Sign) == point.Sign
            && Math.Abs((((longitude.Value % 30) + 30) % 30) - point.Degree) < 0.000001;
    }
    private static bool HasExactDegreeData(ChartAnalysisInput c, int house) => (Find(c, Lord(c, house))?.VargaLongitudeDegrees ?? Find(c, Lord(c, house))?.NirayanaLongitudeDegrees) is not null;
    private static ContextualYogaResult ExactDegreeRow(string code, int number, int printed, int scan, ChartAnalysisInput c, Func<ChartAnalysisInput, bool> predicate)
        => !HasExactDegreeData(c, number == 58 ? 10 : 11)
            ? new(code, null, "NOT_EVALUATED", "SRC_RAMAN_300_COMBINATIONS", $"RAMAN_300_{number:000}", $"combination {number}; printed p.{printed}; scan p.{scan}", "Exact longitude is required to test deep exaltation.")
            : Row(code, number, printed, scan, predicate(c));
    private static bool Favoured(ChartAnalysisInput c, PlanetName p) { var d = Dignity(c, p); return d.DignityTypeCode is "OWN" or "MOOLATRIKONA" or "EXALTED" || d.RelationshipScore > 0; }
    private static bool Strong(ChartAnalysisInput c, PlanetName p) => Dignity(c, p).DignityTypeCode is "OWN" or "MOOLATRIKONA" or "EXALTED";
    private static PvrDignityResult Dignity(ChartAnalysisInput c, PlanetName p)
    {
        var x = Find(c, p); if (x is null) return new("MISSING", 0, "", null, null, 0);
        var lon = x.VargaLongitudeDegrees ?? x.NirayanaLongitudeDegrees;
        return PvrDignityEvaluator.Evaluate(p, Enum.Parse<ZodiacName>(x.Sign), lon is null ? 15 : ((lon.Value % 30) + 30) % 30);
    }
    private static bool AtRelative(ChartAnalysisInput c, PlanetName p, string origin, int distance) => Find(c, p) is { } x && Distance(origin, x.Sign) == distance;
    private static bool HasAny(ChartAnalysisInput c, IEnumerable<PlanetName> planets, string sign) => planets.Any(p => Find(c, p)?.Sign == sign);
    private static bool IsFixed(string sign) => Enum.Parse<ZodiacName>(sign) is ZodiacName.Taurus or ZodiacName.Leo or ZodiacName.Scorpio or ZodiacName.Aquarius;
    private static string HouseSign(ChartAnalysisInput c, int house) => HouseEngine.GetHouseSign(c.AscendantSign, house).ToString();
    private static string RelativeSign(string sign, int distance) => ((ZodiacName)(((int)Enum.Parse<ZodiacName>(sign) + distance - 1) % 12)).ToString();
    private static int Distance(string from, string to) => (((int)Enum.Parse<ZodiacName>(to) - (int)Enum.Parse<ZodiacName>(from) + 12) % 12) + 1;
    private static bool Aspects(PlanetName p, string from, string target) { var d = Distance(from, target); return d == 7 || p == PlanetName.Mars && d is 4 or 8 || p == PlanetName.Jupiter && d is 5 or 9 || p == PlanetName.Saturn && d is 3 or 10; }
    private static PlanetName Lord(ChartAnalysisInput c, int house) => Enum.Parse<PlanetName>(HouseEngine.GetSignLord(HouseEngine.GetHouseSign(c.AscendantSign, house)));
    private static PlanetPosition? Find(ChartAnalysisInput c, PlanetName p) => c.Planets.SingleOrDefault(x => x.Planet == p.ToString());
    private static ContextualYogaResult Row(string code, int number, int printed, int scan, bool present) => Variant(code, $"RAMAN_300_{number:000}", printed, scan, present, null);
    private static ContextualYogaResult Variant(string code, string variant, int printed, int scan, bool present, string? notes) => new(code, present, "EVALUATED", "SRC_RAMAN_300_COMBINATIONS", variant, $"combination {variant.Substring(10, 3)}; printed p.{printed}; scan p.{scan}", notes);
    private static ContextualYogaResult MissingD9(string code, string variant, int printed, int scan) => new(code, null, "NOT_EVALUATED", "SRC_RAMAN_300_COMBINATIONS", variant, $"combination {variant.Substring(10, 3)}; printed p.{printed}; scan p.{scan}", "D9 placement is required.");
}
