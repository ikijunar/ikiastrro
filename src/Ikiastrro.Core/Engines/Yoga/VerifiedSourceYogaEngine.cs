using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.Engines.Strength;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.Yoga;

/// <summary>
/// Authoritative source-qualified layer over geometric yoga formations.
/// Consumers should use this type, not SourceAttributedYogaEngine directly.
/// </summary>
public sealed class VerifiedSourceYogaEngine
{
    private readonly SourceAttributedYogaEngine _formations = new();

    public IReadOnlyList<SourceAttributedYogaResult> Detect(ChartBundle bundle)
    {
        var d1 = bundle.Charts.SingleOrDefault(c => c.ChartType == "D1");
        return d1 is null ? Array.Empty<SourceAttributedYogaResult>() : Detect(d1);
    }

    public IReadOnlyList<SourceAttributedYogaResult> Detect(ChartAnalysisInput chart)
    {
        var rows = _formations.Detect(chart).ToList();
        Replace(rows, "PVR_CH11_GAJAKESARI", row => row with
        {
            Present = row.Present && PvrGajakesariQualified(chart),
            Notes = "PVR qualification: benefic influence on Jupiter; Jupiter not debilitated, combust, or in an enemy sign."
        });

        foreach (var item in new[]
        {
            (PlanetName.Jupiter, "RAMAN_300_019", "PVR_CH11_HAMSA"),
            (PlanetName.Venus, "RAMAN_300_020", "PVR_CH11_MALAVYA"),
            (PlanetName.Saturn, "RAMAN_300_021", "PVR_CH11_SASA"),
            (PlanetName.Mars, "RAMAN_300_022", "PVR_CH11_RUCHAKA"),
            (PlanetName.Mercury, "RAMAN_300_023", "PVR_CH11_BHADRA")
        })
        {
            var present = IsMahapurusha(chart, item.Item1);
            Replace(rows, item.Item2, row => row with { Present = present });
            Replace(rows, item.Item3, row => row with { Present = present });
        }

        var adhi = NaturalBeneficsFill(chart, PlanetName.Moon, 6, 7, 8);
        rows.Add(new("YOGA_ADHI", "Chandra", adhi, false, null,
            "SRC_RAMAN_300_COMBINATIONS", "RAMAN_300_007", "combination 7; printed p.25; scan p.37"));
        rows.Add(new("YOGA_ADHI", "Chandra", adhi, false, null,
            "SRC_PVR_INTEGRATED", "PVR_CH11_ADHI", "ch.11 §11.3.6"));

        rows.Add(Raman("YOGA_CHATUSSAGARA", 8, 28, 40, HousesOccupied(chart, 1, 4, 7, 10)));
        rows.Add(Raman("YOGA_VASUMATHI", 9, 29, 41,
            BeneficInRelativeHouses(chart, chart.AscendantSign.ToString(), 3, 6, 10, 11)
            || BeneficInRelativeHouses(chart, Find(chart, PlanetName.Moon)?.Sign, 3, 6, 10, 11)));
        rows.Add(Raman("YOGA_RAJALAKSHANA", 10, 31, 43,
            new[] { PlanetName.Jupiter, PlanetName.Venus, PlanetName.Mercury, PlanetName.Moon }
                .All(x => Find(chart, x)?.HouseNumber is 1 or 4 or 7 or 10)));
        rows.Add(Raman("YOGA_VANCHANA_CHORA_BHEETHI", 11, 32, 44, Vanchana(chart)));
        rows.Add(Raman("YOGA_SAKATA_LUNAR", 12, 34, 46,
            Relative(chart, PlanetName.Moon, PlanetName.Jupiter, 6, 8, 12),
            "Distinct from PVR's Nabhasa Sakata distribution yoga."));

        var ramanAmala = BeneficInRelativeHouses(chart, chart.AscendantSign.ToString(), 10)
            || BeneficInRelativeHouses(chart, Find(chart, PlanetName.Moon)?.Sign, 10);
        rows.Add(Raman("YOGA_AMALA", 13, 36, 48, ramanAmala));
        rows.Add(new("YOGA_AMALA", "Popular", PvrAmala(chart), false, null,
            "SRC_PVR_INTEGRATED", "PVR_CH11_AMALA", "ch.11 §11.6"));

        rows.Add(Raman("YOGA_PARVATA", 14, 38, 50,
            BeneficInKendra(chart) && HousesVacantOrBenefic(chart, 6, 8)));
        rows.Add(new("YOGA_PARVATA", "Popular",
            BeneficsOnlyInKendras(chart) && HousesVacantOrBenefic(chart, 7, 8), false,
            "PVR uses 7th and 8th; Raman uses 6th and 8th.",
            "SRC_PVR_INTEGRATED", "PVR_CH11_PARVATA", "ch.11 §11.6"));
        rows.Add(Raman("YOGA_KAHALA", 15, 39, 51, Kahala(chart)));
        return rows;
    }

    private static SourceAttributedYogaResult Raman(string code, int number, int printedPage,
        int scanPage, bool present, string? notes = null)
        => new(code, "Raman", present, false, notes, "SRC_RAMAN_300_COMBINATIONS",
            $"RAMAN_300_{number:000}", $"combination {number}; printed p.{printedPage}; scan p.{scanPage}");

    private static void Replace(List<SourceAttributedYogaResult> rows, string variant,
        Func<SourceAttributedYogaResult, SourceAttributedYogaResult> update)
    {
        var index = rows.FindIndex(x => x.SourceVariantCode == variant);
        if (index >= 0) rows[index] = update(rows[index]);
    }

    private static bool IsMahapurusha(ChartAnalysisInput chart, PlanetName planet)
    {
        var position = Find(chart, planet);
        if (position is null || position.HouseNumber is not (1 or 4 or 7 or 10)) return false;
        var dignity = PvrDignityEvaluator.Evaluate(planet, Enum.Parse<ZodiacName>(position.Sign), Degree(position));
        return dignity.DignityTypeCode is "OWN" or "MOOLATRIKONA" or "EXALTED";
    }

    private static bool PvrGajakesariQualified(ChartAnalysisInput chart)
    {
        var jupiter = Find(chart, PlanetName.Jupiter);
        if (jupiter is null) return false;
        var signs = chart.Planets.Where(x => Enum.TryParse<PlanetName>(x.Planet, out _))
            .ToDictionary(x => x.Planet, x => Enum.Parse<ZodiacName>(x.Sign));
        var dignity = PvrDignityEvaluator.Evaluate(PlanetName.Jupiter,
            Enum.Parse<ZodiacName>(jupiter.Sign), Degree(jupiter), signs);
        if (dignity.DignityTypeCode == "DEBILITATED" || dignity.RelationshipScore is < 0) return false;
        if (Separation(chart, PlanetName.Jupiter, PlanetName.Sun) is <= 11) return false;
        return new[] { PlanetName.Moon, PlanetName.Mercury, PlanetName.Venus }
            .Select(x => Find(chart, x)).Where(x => x is not null)
            .Any(x => Distance(x!.Sign, jupiter.Sign) is 1 or 7);
    }

    private static bool NaturalBeneficsFill(ChartAnalysisInput chart, PlanetName reference, params int[] houses)
    {
        var origin = Find(chart, reference); if (origin is null) return false;
        var occupied = chart.Planets.Where(x => x.Planet is "Mercury" or "Jupiter" or "Venus")
            .Select(x => Distance(origin.Sign, x.Sign)).ToHashSet();
        return occupied.Any(houses.Contains);
    }

    private static bool HousesOccupied(ChartAnalysisInput chart, params int[] houses)
    {
        var occupied = chart.Planets.Where(x => Enum.TryParse<PlanetName>(x.Planet, out _))
            .Select(x => x.HouseNumber).ToHashSet();
        return houses.All(occupied.Contains);
    }

    private static bool BeneficInRelativeHouses(ChartAnalysisInput chart, string? origin, params int[] houses)
        => origin is not null && chart.Planets.Where(x => IsNaturalBenefic(chart, x))
            .Any(x => houses.Contains(Distance(origin, x.Sign)));

    private static bool PvrAmala(ChartAnalysisInput chart)
        => OnlyBeneficsInRelativeHouse(chart, chart.AscendantSign.ToString(), 10)
            || OnlyBeneficsInRelativeHouse(chart, Find(chart, PlanetName.Moon)?.Sign, 10);

    private static bool OnlyBeneficsInRelativeHouse(ChartAnalysisInput chart, string? origin, int house)
    {
        if (origin is null) return false;
        var occupants = chart.Planets.Where(x => Enum.TryParse<PlanetName>(x.Planet, out _)
            && Distance(origin, x.Sign) == house).ToList();
        return occupants.Count > 0 && occupants.All(x => IsNaturalBenefic(chart, x));
    }

    private static bool BeneficsOnlyInKendras(ChartAnalysisInput chart)
    {
        var occupants = chart.Planets.Where(x => x.HouseNumber is 1 or 4 or 7 or 10
            && Enum.TryParse<PlanetName>(x.Planet, out _)).ToList();
        return occupants.Any(x => IsNaturalBenefic(chart, x))
            && occupants.All(x => IsNaturalBenefic(chart, x));
    }

    private static bool BeneficInKendra(ChartAnalysisInput chart)
        => chart.Planets.Any(x => x.HouseNumber is 1 or 4 or 7 or 10 && IsNaturalBenefic(chart, x));

    private static bool HousesVacantOrBenefic(ChartAnalysisInput chart, params int[] houses)
        => chart.Planets.Where(x => houses.Contains(x.HouseNumber) && Enum.TryParse<PlanetName>(x.Planet, out _))
            .All(x => IsNaturalBenefic(chart, x));

    private static bool IsNaturalBenefic(ChartAnalysisInput chart, PlanetPosition position)
    {
        if (position.Planet is "Jupiter" or "Venus" or "Mercury") return true;
        if (position.Planet != "Moon") return false;
        var sun = Find(chart, PlanetName.Sun);
        if (sun?.NirayanaLongitudeDegrees is null || position.NirayanaLongitudeDegrees is null) return false;
        var elongation = (position.NirayanaLongitudeDegrees.Value - sun.NirayanaLongitudeDegrees.Value + 360d) % 360d;
        return elongation is > 0 and < 180;
    }

    private static bool Relative(ChartAnalysisInput chart, PlanetName target, PlanetName reference, params int[] houses)
    {
        var targetPosition = Find(chart, target); var referencePosition = Find(chart, reference);
        return targetPosition is not null && referencePosition is not null
            && houses.Contains(Distance(referencePosition.Sign, targetPosition.Sign));
    }

    private static bool Vanchana(ChartAnalysisInput chart)
    {
        var gulika = chart.SpecialPoints.SingleOrDefault(x => x.Planet.Equals("Gulika", StringComparison.OrdinalIgnoreCase));
        var maleficInLagna = chart.Planets.Any(x => x.HouseNumber == 1 && IsNaturalMalefic(chart, x));
        var firstAlternative = maleficInLagna && gulika?.HouseNumber is 5 or 9;

        var relevantLords = new[] { 1, 4, 5, 7, 9, 10 }.Select(x => HouseLord(chart, x)).Distinct();
        var secondAlternative = gulika is not null && relevantLords
            .Select(x => Find(chart, x)).Any(x => x?.Sign == gulika.Sign);

        var lagnaLord = Find(chart, HouseLord(chart, 1));
        var thirdAlternative = lagnaLord is not null && new[] { PlanetName.Rahu, PlanetName.Saturn, PlanetName.Ketu }
            .Select(x => Find(chart, x)).Any(x => x?.Sign == lagnaLord.Sign);
        return firstAlternative || secondAlternative || thirdAlternative;
    }

    private static bool Kahala(ChartAnalysisInput chart)
    {
        var fourthLord = HouseLord(chart, 4);
        var ninthLord = HouseLord(chart, 9);
        var lagnaLord = HouseLord(chart, 1);
        var fourth = Find(chart, fourthLord);
        var ninth = Find(chart, ninthLord);
        var lagna = Find(chart, lagnaLord);

        var firstAlternative = fourthLord != ninthLord && fourth is not null && ninth is not null
            && Distance(fourth.Sign, ninth.Sign) is 1 or 4 or 7 or 10
            && Strong(lagnaLord, lagna);

        var tenthLord = HouseLord(chart, 10);
        var tenth = Find(chart, tenthLord);
        var secondAlternative = Strong(fourthLord, fourth) && fourth is not null && tenth is not null
            && (fourth.Sign == tenth.Sign || Aspects(tenthLord, tenth.Sign, fourth.Sign));
        return firstAlternative || secondAlternative;
    }

    private static PlanetName HouseLord(ChartAnalysisInput chart, int house)
        => Enum.Parse<PlanetName>(HouseEngine.GetSignLord(HouseEngine.GetHouseSign(chart.AscendantSign, house)));

    private static bool Strong(PlanetName planet, PlanetPosition? position)
    {
        if (position is null) return false;
        var dignity = PvrDignityEvaluator.Evaluate(planet, Enum.Parse<ZodiacName>(position.Sign), Degree(position));
        return dignity.DignityTypeCode is "OWN" or "MOOLATRIKONA" or "EXALTED";
    }

    private static bool Aspects(PlanetName planet, string from, string target)
    {
        var distance = Distance(from, target);
        return distance == 7
            || planet == PlanetName.Mars && distance is 4 or 8
            || planet == PlanetName.Jupiter && distance is 5 or 9
            || planet == PlanetName.Saturn && distance is 3 or 10;
    }

    private static bool IsNaturalMalefic(ChartAnalysisInput chart, PlanetPosition position)
    {
        if (position.Planet is "Sun" or "Mars" or "Saturn" or "Rahu" or "Ketu") return true;
        if (position.Planet != "Moon") return false;
        return !IsNaturalBenefic(chart, position);
    }

    private static PlanetPosition? Find(ChartAnalysisInput chart, PlanetName planet)
        => chart.Planets.SingleOrDefault(x => x.Planet == planet.ToString());

    private static int Distance(string from, string to)
        => (((int)Enum.Parse<ZodiacName>(to) - (int)Enum.Parse<ZodiacName>(from) + 12) % 12) + 1;

    private static double Degree(PlanetPosition position)
    {
        var longitude = position.VargaLongitudeDegrees ?? position.NirayanaLongitudeDegrees;
        return longitude is null ? 15d : ((longitude.Value % 30d) + 30d) % 30d;
    }

    private static double? Separation(ChartAnalysisInput chart, PlanetName a, PlanetName b)
    {
        var x = Find(chart, a)?.NirayanaLongitudeDegrees; var y = Find(chart, b)?.NirayanaLongitudeDegrees;
        if (x is null || y is null) return null;
        var distance = Math.Abs(x.Value - y.Value) % 360d;
        return Math.Min(distance, 360d - distance);
    }
}
