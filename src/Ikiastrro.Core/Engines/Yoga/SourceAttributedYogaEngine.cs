using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.Yoga;

public sealed record SourceAttributedYogaResult(
    string YogaCode, string Category, bool Present, bool Cancelled, string? Notes,
    string SourceRefCode, string SourceVariantCode, string SourceLocator);

/// <summary>
/// Detects source-specific yoga variants without silently merging Raman and PVR.
/// Initial slice: common solar, lunar and Pancha Mahapurusha yogas.
/// </summary>
public sealed class SourceAttributedYogaEngine
{
    private const string Raman = "SRC_RAMAN_300_COMBINATIONS";
    private const string Pvr = "SRC_PVR_INTEGRATED";

    public IReadOnlyList<SourceAttributedYogaResult> Detect(ChartBundle bundle)
    {
        var d1 = bundle.Charts.SingleOrDefault(c => c.ChartType == "D1");
        return d1 is null ? Array.Empty<SourceAttributedYogaResult>() : Detect(d1);
    }

    public IReadOnlyList<SourceAttributedYogaResult> Detect(ChartAnalysisInput chart)
    {
        var rows = new List<SourceAttributedYogaResult>();
        AddPair(rows, "YOGA_GAJAKESARI", "Chandra", "RAMAN_300_001", "combination 1; printed p.13; scan p.25",
            "PVR_CH11_GAJAKESARI", "ch.11 §11.6", Relative(chart, PlanetName.Jupiter, PlanetName.Moon, 1, 4, 7, 10));
        AddPair(rows, "YOGA_SUNAPHA", "Chandra", "RAMAN_300_002", "combination 2; printed p.16; scan p.28",
            "PVR_CH11_SUNAPHA", "ch.11 §11.3.1", HasPlanetFrom(chart, PlanetName.Moon, 2, PlanetName.Sun));
        AddPair(rows, "YOGA_ANAPHA", "Chandra", "RAMAN_300_003", "combination 3; printed p.19; scan p.31",
            "PVR_CH11_ANAPHA", "ch.11 §11.3.2", HasPlanetFrom(chart, PlanetName.Moon, 12, PlanetName.Sun));
        AddPair(rows, "YOGA_DURADHARA", "Chandra", "RAMAN_300_004", "combination 4; printed p.20; scan p.32",
            "PVR_CH11_DURADHARA", "ch.11 §11.3.3", HasPlanetFrom(chart, PlanetName.Moon, 2, PlanetName.Sun) && HasPlanetFrom(chart, PlanetName.Moon, 12, PlanetName.Sun));
        AddPair(rows, "YOGA_KEMADRUMA", "Chandra", "RAMAN_300_005", "combination 5; printed p.22; scan p.34",
            "PVR_CH11_KEMADRUMA", "ch.11 §11.3.4", !HasPlanetFrom(chart, PlanetName.Moon, 2, PlanetName.Sun) && !HasPlanetFrom(chart, PlanetName.Moon, 12, PlanetName.Sun));
        AddPair(rows, "YOGA_CHANDRA_MANGALA", "Chandra", "RAMAN_300_006", "combination 6; printed p.24; scan p.36",
            "PVR_CH11_CHANDRA_MANGALA", "ch.11 §11.3.5", SameSign(chart, PlanetName.Moon, PlanetName.Mars));

        AddSolar(rows, chart, "YOGA_VESI", 2, "RAMAN_300_016", "combination 16; printed p.40; scan p.52", "PVR_CH11_VESI", "ch.11 §11.2.1");
        AddSolar(rows, chart, "YOGA_VASI", 12, "RAMAN_300_017", "combination 17; printed p.42; scan p.54", "PVR_CH11_VOSI", "ch.11 §11.2.2");
        AddPair(rows, "YOGA_UBHAYACHARI", "Ravi", "RAMAN_300_018", "combination 18; printed p.42; scan p.54",
            "PVR_CH11_UBHAYACHARA", "ch.11 §11.2.3", HasPlanetFrom(chart, PlanetName.Sun, 2, PlanetName.Moon) && HasPlanetFrom(chart, PlanetName.Sun, 12, PlanetName.Moon));
        AddBudhaAditya(rows, chart);

        AddMahapurusha(rows, chart, PlanetName.Jupiter, "YOGA_HAMSA", 19, 43, 55, "§11.4.5");
        AddMahapurusha(rows, chart, PlanetName.Venus, "YOGA_MALAVYA", 20, 48, 60, "§11.4.4");
        AddMahapurusha(rows, chart, PlanetName.Saturn, "YOGA_SASA", 21, 49, 61, "§11.4.3");
        AddMahapurusha(rows, chart, PlanetName.Mars, "YOGA_RUCHAKA", 22, 51, 63, "§11.4.1");
        AddMahapurusha(rows, chart, PlanetName.Mercury, "YOGA_BHADRA", 23, 53, 65, "§11.4.2");
        return rows;
    }

    private static void AddBudhaAditya(List<SourceAttributedYogaResult> rows, ChartAnalysisInput chart)
    {
        var together = SameSign(chart, PlanetName.Sun, PlanetName.Mercury);
        var separation = Separation(chart, PlanetName.Sun, PlanetName.Mercury);
        rows.Add(Row("YOGA_BUDHA_ADITYA", "Ravi", together && separation is > 10, Raman,
            "RAMAN_300_024", "combination 24; printed p.54; scan p.66",
            separation is null ? "Longitude unavailable; Raman's >10° qualification cannot be established." : $"Sun–Mercury separation {separation:F2}°; Raman requires >10°."));
        rows.Add(Row("YOGA_BUDHA_ADITYA", "Ravi", together, Pvr, "PVR_CH11_BUDHA_ADITYA", "ch.11 §11.2.4",
            together && separation is <= 14 ? "Present, but Mercury is combust; PVR says combustion reduces benefic power." : null));
    }

    private static void AddSolar(List<SourceAttributedYogaResult> rows, ChartAnalysisInput chart, string code, int house,
        string ramanVariant, string ramanLocator, string pvrVariant, string pvrLocator)
        => AddPair(rows, code, "Ravi", ramanVariant, ramanLocator, pvrVariant, pvrLocator,
            HasPlanetFrom(chart, PlanetName.Sun, house, PlanetName.Moon));

    private static void AddMahapurusha(List<SourceAttributedYogaResult> rows, ChartAnalysisInput chart,
        PlanetName planet, string code, int number, int printedPage, int scanPage, string pvrSection)
    {
        var position = Find(chart, planet);
        var present = position is not null && position.HouseNumber is 1 or 4 or 7 or 10 && OwnOrExalted(planet, position.Sign);
        AddPair(rows, code, "Mahapurusha", $"RAMAN_300_{number:000}", $"combination {number}; printed p.{printedPage}; scan p.{scanPage}",
            $"PVR_CH11_{code[5..]}", $"ch.11 {pvrSection}", present);
    }

    private static bool OwnOrExalted(PlanetName planet, string sign) => planet switch
    {
        PlanetName.Mars => sign is "Aries" or "Scorpio" or "Capricornus",
        PlanetName.Mercury => sign is "Gemini" or "Virgo",
        PlanetName.Jupiter => sign is "Sagittarius" or "Pisces" or "Cancer",
        PlanetName.Venus => sign is "Taurus" or "Libra" or "Pisces",
        PlanetName.Saturn => sign is "Capricornus" or "Aquarius" or "Libra",
        _ => false
    };

    private static void AddPair(List<SourceAttributedYogaResult> rows, string code, string category,
        string ramanVariant, string ramanLocator, string pvrVariant, string pvrLocator, bool present)
    {
        rows.Add(Row(code, category, present, Raman, ramanVariant, ramanLocator));
        rows.Add(Row(code, category, present, Pvr, pvrVariant, pvrLocator));
    }

    private static SourceAttributedYogaResult Row(string code, string category, bool present, string source,
        string variant, string locator, string? notes = null) => new(code, category, present, false, notes, source, variant, locator);

    private static bool SameSign(ChartAnalysisInput chart, PlanetName a, PlanetName b)
        => Find(chart, a)?.Sign is { } sign && Find(chart, b)?.Sign == sign;

    private static bool Relative(ChartAnalysisInput chart, PlanetName target, PlanetName reference, params int[] houses)
    {
        var t = Find(chart, target); var r = Find(chart, reference);
        return t is not null && r is not null && houses.Contains(Distance(r.Sign, t.Sign));
    }

    private static bool HasPlanetFrom(ChartAnalysisInput chart, PlanetName reference, int house, params PlanetName[] excluded)
    {
        var r = Find(chart, reference); if (r is null) return false;
        return chart.Planets.Where(p => Enum.TryParse<PlanetName>(p.Planet, out var x) && x is not PlanetName.Rahu and not PlanetName.Ketu)
            .Where(p => !excluded.Contains(Enum.Parse<PlanetName>(p.Planet))).Any(p => Distance(r.Sign, p.Sign) == house);
    }

    private static PlanetPosition? Find(ChartAnalysisInput chart, PlanetName planet) => chart.Planets.SingleOrDefault(p => p.Planet == planet.ToString());
    private static int Distance(string from, string to) => (((int)Enum.Parse<ZodiacName>(to) - (int)Enum.Parse<ZodiacName>(from) + 12) % 12) + 1;
    private static double? Separation(ChartAnalysisInput chart, PlanetName a, PlanetName b)
    {
        var x = Find(chart, a)?.NirayanaLongitudeDegrees; var y = Find(chart, b)?.NirayanaLongitudeDegrees;
        if (x is null || y is null) return null;
        var d = Math.Abs(x.Value - y.Value) % 360d; return Math.Min(d, 360d - d);
    }
}
