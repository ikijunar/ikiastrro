using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.Strength;

/// <summary>
/// PVR-first Shadbala calculator. PVR defines the six strength sources and refers detailed
/// arithmetic to Raman's Graha and Bhava Balas; this class keeps every intermediate component
/// visible so later JHora reconciliation can change one convention without hiding the delta.
/// </summary>
public static class ShadbalaCalculator
{
    private static readonly PlanetName[] ClassicalPlanets =
    {
        PlanetName.Sun, PlanetName.Moon, PlanetName.Mars, PlanetName.Mercury,
        PlanetName.Jupiter, PlanetName.Venus, PlanetName.Saturn
    };

    private static readonly IReadOnlyDictionary<PlanetName, double> DeepExaltation =
        new Dictionary<PlanetName, double>
        {
            [PlanetName.Sun] = 10,
            [PlanetName.Moon] = 33,
            [PlanetName.Mars] = 298,
            [PlanetName.Mercury] = 165,
            [PlanetName.Jupiter] = 95,
            [PlanetName.Venus] = 327,
            [PlanetName.Saturn] = 200
        };

    private static readonly IReadOnlyDictionary<PlanetName, double> Naisargika =
        new Dictionary<PlanetName, double>
        {
            [PlanetName.Sun] = 60.00,
            [PlanetName.Moon] = 51.43,
            [PlanetName.Venus] = 42.86,
            [PlanetName.Jupiter] = 34.29,
            [PlanetName.Mercury] = 25.71,
            [PlanetName.Mars] = 17.14,
            [PlanetName.Saturn] = 8.57
        };

    private static readonly IReadOnlyDictionary<PlanetName, int> DigBalaHouses =
        new Dictionary<PlanetName, int>
        {
            [PlanetName.Sun] = 10, [PlanetName.Mars] = 10,
            [PlanetName.Moon] = 4, [PlanetName.Venus] = 4,
            [PlanetName.Mercury] = 1, [PlanetName.Jupiter] = 1,
            [PlanetName.Saturn] = 7
        };

    /// <summary>Calculates the seven classical planets from the D1 and available varga inputs.</summary>
    public static IReadOnlyList<PlanetaryStrengthResult> Calculate(
        IReadOnlyList<ChartAnalysisInput> charts,
        SiderealPositions positions,
        SunTimes sunTimes)
    {
        var d1 = charts.FirstOrDefault(c => c.ChartType.Equals("D1", StringComparison.OrdinalIgnoreCase))
                  ?? charts.FirstOrDefault()
                  ?? throw new ArgumentException("At least one chart input is required.", nameof(charts));

        var results = new List<PlanetaryStrengthResult>(ClassicalPlanets.Length);
        foreach (var planet in ClassicalPlanets)
        {
            var name = planet.ToString();
            var p = d1.Planets.FirstOrDefault(x => x.Planet.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (p is null || p.NirayanaLongitudeDegrees is null) continue;

            var components = new List<ShadbalaComponentResult>();
            AddSthana(components, planet, p, charts);
            AddDig(components, planet, p);
            AddKala(components, planet, p, positions, sunTimes);
            AddCheshta(components, planet, p, positions);
            AddNaisargika(components, planet);
            AddDrik(components, planet, d1);

            var sthana = Sum(components, "STHANA_BALA");
            var dig = Sum(components, "DIG_BALA");
            var kala = Sum(components, "KALA_BALA");
            var cheshta = Sum(components, "CHESTA_BALA");
            var naisargika = Sum(components, "NAISARGIKA_BALA");
            var drik = Sum(components, "DRIK_BALA");
            var total = sthana + dig + kala + cheshta + naisargika + drik;
            var uchcha = components.First(x => x.SubComponentCode == "UCHCHA_BALA").ValueVirupas;
            var ishta = Math.Sqrt(Math.Max(0, uchcha * cheshta));

            results.Add(new PlanetaryStrengthResult(
                name, components, Round(sthana), Round(dig), Round(kala), Round(cheshta),
                Round(naisargika), Round(drik), Round(total), Round(total / 60.0),
                Round(ishta), Round(60.0 - ishta)));
        }
        return results;
    }

    private static void AddSthana(List<ShadbalaComponentResult> rows, PlanetName planet,
        PlanetPosition p, IReadOnlyList<ChartAnalysisInput> charts)
    {
        var longitude = p.NirayanaLongitudeDegrees!.Value;
        var exalt = DeepExaltation[planet];
        var distanceFromDebilitation = AngularDistance(longitude, (exalt + 180) % 360);
        rows.Add(Row("STHANA_BALA", "UCHCHA_BALA", distanceFromDebilitation / 3.0,
            "DEBILITATION_DISTANCE", "Raman/PVR: distance from the deep-debilitation point."));

        var saptaTypes = new[] { "D1", "D2", "D3", "D7", "D9", "D12", "D30" };
        var saptaDetails = charts.Where(c => saptaTypes.Contains(c.ChartType, StringComparer.OrdinalIgnoreCase))
            .Select(c => (Chart: c, Position: c.Planets.FirstOrDefault(x => x.Planet.Equals(p.Planet, StringComparison.OrdinalIgnoreCase))))
            .Where(x => x.Position is not null)
            .Select(x =>
            {
                var position = x.Position!;
                var chartLongitude = position.VargaLongitudeDegrees ?? position.NirayanaLongitudeDegrees ?? longitude;
                var degreeInSign = ((chartLongitude % 30) + 30) % 30;
                var sign = Enum.TryParse<ZodiacName>(position.Sign, true, out var parsed) ? parsed : ZodiacName.Aries;
                var signs = x.Chart.Planets
                    .Where(v => Enum.TryParse<PlanetName>(v.Planet, out _))
                    .ToDictionary(v => v.Planet, v => Enum.Parse<ZodiacName>(v.Sign), StringComparer.OrdinalIgnoreCase);
                var dignity = PvrDignityEvaluator.Evaluate(planet, sign, degreeInSign, signs);
                return (ChartType: x.Chart.ChartType, Sign: position.Sign, DegreeInSign: degreeInSign, Dignity: dignity);
            }).ToList();
        var dignityPoints = saptaDetails.Sum(x => x.Dignity.SaptavargajaPoints);
        rows.Add(Row("STHANA_BALA", "SAPTAVARGAJA_BALA", dignityPoints,
            "SAPTA_VARGA_DIGNITY", string.Join("; ", saptaDetails.Select(x =>
                $"{x.ChartType}:{x.Sign} {x.DegreeInSign:0.###}° {x.Dignity.DignityTypeCode} " +
                $"lord={x.Dignity.SignLord} rel={x.Dignity.CompoundRelationshipCode ?? "—"} " +
                $"({x.Dignity.SaptavargajaPoints:0.###})"))));

        var odd = SignIndex(p.Sign) % 2 == 0;
        var sexMatches = planet is PlanetName.Sun or PlanetName.Mars or PlanetName.Jupiter or PlanetName.Saturn
            ? odd : !odd;
        rows.Add(Row("STHANA_BALA", "OJHA_YUGMA_RASYAMSA_BALA", sexMatches ? 30 : 0,
            "ODD_EVEN_D1_D9", "Raman/PVR odd-even sign and planetary sex contribution."));

        rows.Add(Row("STHANA_BALA", "KENDRADI_BALA", p.HouseNumber is 1 or 4 or 7 or 10 ? 60 :
            p.HouseNumber is 2 or 5 or 8 or 11 ? 30 : 15,
            "KENDRA_PANAPHARA_APOKLIMA", "Kendra, panaphara, and apoklima house contribution."));
        rows.Add(Row("STHANA_BALA", "DREKKANA_BALA", (int)((longitude % 30) / 10) switch
        {
            0 when planet is PlanetName.Sun or PlanetName.Mars or PlanetName.Jupiter => 15,
            1 when planet is PlanetName.Moon or PlanetName.Venus => 15,
            2 when planet is PlanetName.Mercury or PlanetName.Saturn => 15,
            _ => 0
        }, "DREKKANA_GROUP", "Drekkana sex/group contribution."));
    }

    private static void AddDig(List<ShadbalaComponentResult> rows, PlanetName planet, PlanetPosition p)
    {
        var houseDistance = ((p.HouseNumber - DigBalaHouses[planet]) % 12 + 12) % 12;
        var fraction = Math.Min(houseDistance, 12 - houseDistance) / 6.0;
        rows.Add(Row("DIG_BALA", "DIG_BALA", 60.0 * (1.0 - fraction),
            "FULL_STRENGTH_HOUSE_DISTANCE", "Directional strength measured from the full-strength house."));
    }

    private static void AddKala(List<ShadbalaComponentResult> rows, PlanetName planet, PlanetPosition p,
        SiderealPositions positions, SunTimes sunTimes)
    {
        var dayStrong = planet is PlanetName.Sun or PlanetName.Jupiter or PlanetName.Venus;
        var nathonnata = sunTimes.IsNightBirth == dayStrong ? 0 : 60;
        rows.Add(Row("KALA_BALA", "NATHONNATA_BALA", nathonnata,
            "DAY_NIGHT_ARC", "Temporal strength from the birth day/night arc."));

        var sun = positions.PlanetLongitudes[PlanetName.Sun];
        var moon = positions.PlanetLongitudes[PlanetName.Moon];
        var phase = AngularDistance(sun, moon);
        var paksha = planet == PlanetName.Moon ? Math.Abs(180 - phase) / 3.0 : 0;
        rows.Add(Row("KALA_BALA", "PAKSHA_BALA", 60 - paksha,
            "MOON_PHASE", "Lunar-phase component; additional calendrical components are added in the next slice."));
    }

    private static void AddCheshta(List<ShadbalaComponentResult> rows, PlanetName planet, PlanetPosition p,
        SiderealPositions positions)
    {
        var value = planet is PlanetName.Sun or PlanetName.Moon ? 0 :
            (p.IsRetrograde == true || positions.PlanetSpeeds.GetValueOrDefault(planet) < 0 ? 60 : 30);
        rows.Add(Row("CHESTA_BALA", "CHESTA_BALA", value,
            "MOTION_AND_RETROGRADE", "Motional strength; mean-motion refinements are retained for JHora reconciliation."));
    }

    private static void AddNaisargika(List<ShadbalaComponentResult> rows, PlanetName planet) =>
        rows.Add(Row("NAISARGIKA_BALA", "NAISARGIKA_BALA", Naisargika[planet],
            "FIXED_PLANET_VALUE", "Permanent natural strength."));

    private static void AddDrik(List<ShadbalaComponentResult> rows, PlanetName planet, ChartAnalysisInput d1)
    {
        var target = d1.Planets.FirstOrDefault(p => p.Planet.Equals(planet.ToString(), StringComparison.OrdinalIgnoreCase));
        if (target?.NirayanaLongitudeDegrees is null) return;
        var total = 0.0;
        foreach (var source in d1.Planets.Where(p => !p.Planet.Equals("Ascendant", StringComparison.OrdinalIgnoreCase)
                                                    && !p.Planet.Equals(planet.ToString(), StringComparison.OrdinalIgnoreCase)
                                                    && Enum.TryParse<PlanetName>(p.Planet, out _)))
        {
            var sourcePlanet = Enum.Parse<PlanetName>(source.Planet);
            var angle = AngularDistance(source.NirayanaLongitudeDegrees ?? 0, target.NirayanaLongitudeDegrees.Value);
            var strength = AspectStrength(angle, sourcePlanet);
            total += IsNaturalBenefic(sourcePlanet) ? strength : -strength;
        }
        rows.Add(Row("DRIK_BALA", "DRIK_BALA", total / 4.0,
            "SPUTA_DRISHTI", "Aspect strength from benefic and malefic planetary influence."));
    }

    private static ShadbalaComponentResult Row(string bala, string sub, double value, string method, string narrative) =>
        new(bala, sub, Round(value), method, narrative);

    private static double Sum(IEnumerable<ShadbalaComponentResult> rows, string bala) => rows.Where(x => x.BalaCode == bala).Sum(x => x.ValueVirupas);
    private static double Round(double value) => Math.Round(value, 3, MidpointRounding.AwayFromZero);
    private static int SignIndex(string sign) => Enum.TryParse<ZodiacName>(sign, true, out var z) ? (int)z : 0;
    private static double AngularDistance(double a, double b)
    {
        var d = Math.Abs(((a - b) % 360 + 360) % 360);
        return d > 180 ? 360 - d : d;
    }

    private static bool IsNaturalBenefic(PlanetName planet) => planet is PlanetName.Moon or PlanetName.Mercury or PlanetName.Jupiter or PlanetName.Venus;

    private static double AspectStrength(double angle, PlanetName source)
    {
        var special = source switch
        {
            PlanetName.Mars => new[] { 90d, 210d },
            PlanetName.Jupiter => new[] { 120d, 240d },
            PlanetName.Saturn => new[] { 60d, 270d },
            _ => Array.Empty<double>()
        };
        if (special.Any(x => AngularDistance(angle, x) <= 8)) return 60;
        if (Math.Abs(angle - 180) <= 8) return 60;
        if (angle < 30 || angle > 150) return 0;
        return angle <= 90 ? (angle - 30) : (150 - angle);
    }
}
