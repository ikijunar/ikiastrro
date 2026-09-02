using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Jaimini;

/// <summary>
/// Jaimini 8-karaka (Ashta) assignment from the D1 chart. Ranks the 8 grahas
/// (Sun..Saturn, Rahu) by longitude WITHIN their sign, descending; Rahu's key is
/// (30 - itsDegreeInSign) because it is always retrograde. Highest -> AK, lowest -> DK.
/// Ketu is not ranked.
/// </summary>
public static class CharaKarakaCalculator
{
    private static readonly PlanetName[] Ranked =
    {
        PlanetName.Sun, PlanetName.Moon, PlanetName.Mars, PlanetName.Mercury,
        PlanetName.Jupiter, PlanetName.Venus, PlanetName.Saturn, PlanetName.Rahu
    };

    public static IReadOnlyDictionary<PlanetName, CharaKaraka> Assign(
        IReadOnlyDictionary<PlanetName, double> degreeInSignByPlanet)
    {
        double Key(PlanetName p)
        {
            var deg = degreeInSignByPlanet[p] % 30.0;
            if (deg < 0) deg += 30.0;
            return p == PlanetName.Rahu ? 30.0 - deg : deg;
        }

        var order = Ranked
            .OrderByDescending(Key)
            .ThenBy(p => Array.IndexOf(Ranked, p))   // stable tie-break (never fires on real data)
            .ToArray();

        var result = new Dictionary<PlanetName, CharaKaraka>();
        for (var i = 0; i < order.Length; i++)
            result[order[i]] = (CharaKaraka)i;
        return result;
    }
}
