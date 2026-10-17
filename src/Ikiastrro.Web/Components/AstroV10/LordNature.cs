using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.LifeMatters.Promise;
using Ikiastrro.Web.Components.Workspace;

namespace Ikiastrro.Web.Components.AstroV10;

/// <summary>
/// What kind of planet a lord is, kept as three separate facts (never blended into one score): its natural nature (fixed,
/// with the Moon and Mercury read from the chart), its functional nature for this lagna (the reading subhaargala and
/// paapaargala use), and the houses it owns and stands in. <see cref="Dignity"/> is the dignity label of its own row.
/// </summary>
public sealed record LordNature(
    string Planet, bool NaturalMalefic, Ikiastrro.Core.LifeMatters.Promise.Direction Functional,
    IReadOnlyList<int> HousesOwned, int? HouseStood, string? Dignity)
{
    /// <summary>One entry per graha in the chart, keyed by planet name.</summary>
    public static IReadOnlyDictionary<string, LordNature> For(LoadedChart chart)
    {
        var lagna = Enum.Parse<ZodiacName>(chart.AscendantSign);
        var grahas = chart.Grahas.Where(k => k.Planet != "Ascendant").ToList();
        var occupancy = ArgalaFactBuilder.BuildOccupancy(grahas);
        var moonWaxing = MoonPhase.Of(grahas);
        var result = new Dictionary<string, LordNature>();
        foreach (var row in grahas)
        {
            if (!Enum.TryParse<PlanetName>(row.Planet, out var planet)) continue;
            var owned = chart.HouseLords.Where(h => h.LordPlanet == row.Planet).Select(h => h.HouseNumber).OrderBy(n => n).ToList();
            result[row.Planet] = new LordNature(
                row.Planet, ArgalaCalculator.IsNaturalMalefic(planet, occupancy),
                D1PromiseEngine.NatureDirection(lagna, planet, moonWaxing),
                owned, row.HouseNumberFromLagna, row.DignityStatus);
        }
        return result;
    }
}
