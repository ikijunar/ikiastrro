using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Engines.PlanetNature;

/// <summary>A related planet (a sign lord or a star lord) with its natural nature and PVR functional verdict in the same chart.</summary>
public sealed record RelatedPlanetNature(
    string Planet, NaturalNature Natural, SchoolFunctional? Pvr, int? House);

/// <param name="Dignity">The planet's own dignity in this sign (stored DignityStatus).</param>
public sealed record SignReading(string Sign, string? Dignity, RelatedPlanetNature? Lord, bool IsOwnLord);

/// <param name="StarLord">The nakshatra's Vimshottari lord, with its house in this chart.</param>
/// <param name="SubLord">The KP sub-lord (Vimshottari level 2), natural nature only.</param>
public sealed record NakshatraReading(string? Nakshatra, int? Pada, RelatedPlanetNature? StarLord, RelatedPlanetNature? SubLord,
    NakshatraTemperamentInfo? Temperament = null, NakshatraAspectReading? StarAspects = null);

/// <param name="HouseVerdict">The occupied house's final nature: its lord, the planets in it (this planet and its conjunctions) and
/// the planets aspecting it, each by functional nature from this chart's Lagna (<see cref="HouseBeneficMaleficCalculator"/>,
/// Raman HTJH p.14-15). Null only if the house could not be computed.</param>
public sealed record PlanetNatureRow(
    string Planet, int House, string Sign,
    NaturalNatureReading Natural,
    IReadOnlyList<SchoolFunctionalReading> Functional,
    IReadOnlyList<SchoolPlacementReading> Placement,
    SignReading SignInfo,
    NakshatraReading NakshatraInfo,
    HouseBeneficMaleficResult? HouseVerdict = null);

/// <summary>
/// Astro Facts "Benefic / malefic": per planet, in the chart shown (D1 or any varga, with that chart's own Lagna), the planet's
/// natural nature, its functional nature by school, how its house placement reads by school, the nature of its sign lord and of
/// its star lord. Facts side by side — no blended verdict (docs/research/domain/planet-benefic-malefic-perspectives.md §9).
/// </summary>
public static class PlanetBeneficMaleficReader
{
    private static readonly PlanetName[] Order =
    {
        PlanetName.Sun, PlanetName.Moon, PlanetName.Mars, PlanetName.Mercury, PlanetName.Jupiter,
        PlanetName.Venus, PlanetName.Saturn, PlanetName.Rahu, PlanetName.Ketu,
    };

    public static IReadOnlyList<PlanetNatureRow> Read(
        IReadOnlyList<ChartKeyDetail> keyDetails, ZodiacName lagna, bool? moonWaxing, SchoolRuleBook rules)
    {
        var grahas = keyDetails.Where(k => k.PointKind == "Graha" && k.Planet != "Ascendant").ToList();
        var natural = NaturalNatureReader.Read(grahas, moonWaxing);
        var rows = new List<PlanetNatureRow>();

        RelatedPlanetNature? Related(string? planet)
        {
            if (planet is null || !natural.TryGetValue(planet, out var n)) return null;
            SchoolFunctional? pvr = Enum.TryParse<PlanetName>(planet, out var pn)
                ? SchoolFunctionalNature.Pvr(lagna, pn, moonWaxing).Verdict : null;
            var house = grahas.FirstOrDefault(g => g.Planet == planet)?.HouseNumberFromLagna;
            return new(planet, n.Nature, pvr, house);
        }

        var stars = grahas.Where(k => k.Nakshatra is not null).ToDictionary(k => k.Planet, k => (string?)k.Nakshatra);
        var houses = HouseBeneficMaleficCalculator.ComputeAll(lagna, grahas).ToDictionary(h => h.HouseNumber);

        foreach (var planet in Order)
        {
            var g = grahas.FirstOrDefault(k => k.Planet == planet.ToString());
            if (g is null || !natural.TryGetValue(g.Planet, out var nat)) continue;

            var functional = SchoolFunctionalNature.ReadAll(lagna, planet, nat, g.HouseNumberFromLagna, moonWaxing, rules);
            var placement = SchoolPlacement.ReadAll(g.HouseNumberFromLagna, nat, functional[0], rules);
            var sign = new SignReading(g.Sign, g.DignityStatus, Related(g.SignLordPlanet), g.SignLordPlanet == g.Planet);
            var nak = new NakshatraReading(g.Nakshatra, g.NakshatraPada, Related(g.NakshatraLordPlanet),
                g.NakshatraSubLordPlanet is { } sub && natural.TryGetValue(sub, out var sn) ? new(sub, sn.Nature, null, null) : null,
                NakshatraTemperament.Of(g.Nakshatra), g.Nakshatra is null ? null : NakshatraAspects.Read(g.Planet, stars));
            rows.Add(new(g.Planet, g.HouseNumberFromLagna, g.Sign, nat, functional, placement, sign, nak,
                houses.GetValueOrDefault(g.HouseNumberFromLagna)));
        }
        return rows;
    }

    /// <summary>PVR's functional verdict for each classical planet in each chart, from that chart's own Lagna (PVR reads
    /// functional nature "in dasamsa" the same way, Example 54). Keyed chart type → planet.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<PlanetName, SchoolFunctional>> PvrAcrossCharts(
        IEnumerable<(string ChartType, ZodiacName Lagna)> charts, bool? moonWaxing) =>
        charts.ToDictionary(
            c => c.ChartType,
            c => (IReadOnlyDictionary<PlanetName, SchoolFunctional>)Order
                .Where(p => p is not (PlanetName.Rahu or PlanetName.Ketu))
                .ToDictionary(p => p, p => SchoolFunctionalNature.Pvr(c.Lagna, p, moonWaxing).Verdict));
}
