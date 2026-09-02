using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Calculators;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.SpecialPoints;

/// <summary>
/// The Arudha pada of each of the 12 houses (Parashara). For house H with whole-sign lord L:
/// let n = signs counted from H's sign to L's sign (inclusive, 1..12); the pada is n signs
/// on from L's sign (inclusive). If the pada lands on H's own sign or the 7th from it, take
/// the 10th sign from the pada. The pada is placed at the natal Lagna's degree-in-sign so it
/// has a longitude for varga projection. A1 is emitted under the code "AL".
/// </summary>
public static class ArudhaCalculator
{
    public static IReadOnlyList<SpecialPointSeed> Compute(ChartAnalysisInput d1)
    {
        var lagnaSign = d1.AscendantSign;
        var asc = d1.Planets.First(p => p.Planet == "Ascendant");
        var lagnaDegInSign = (asc.NirayanaLongitudeDegrees ?? 0) % 30.0;

        var planetSign = d1.Planets
            .Where(p => p.Planet != "Ascendant")
            .ToDictionary(p => Enum.Parse<PlanetName>(p.Planet), p => Enum.Parse<ZodiacName>(p.Sign));

        int SignIndex(ZodiacName z) => (int)z;
        ZodiacName Add(ZodiacName z, int n) => (ZodiacName)(((SignIndex(z) + n) % 12 + 12) % 12);
        int CountInclusive(ZodiacName from, ZodiacName to) => ((SignIndex(to) - SignIndex(from)) % 12 + 12) % 12 + 1;

        var seeds = new List<SpecialPointSeed>();
        for (var house = 1; house <= 12; house++)
        {
            var houseSign = Add(lagnaSign, house - 1);
            var lord = Enum.Parse<PlanetName>(ClassicalDignity.GetSignLord(houseSign));
            var lordSign = planetSign[lord];
            var n = CountInclusive(houseSign, lordSign);
            var pada = Add(lordSign, n - 1);
            // exception: pada == house's own sign (1st) or the 7th from it
            var fromHouse = CountInclusive(houseSign, pada);   // 1..12
            if (fromHouse == 1 || fromHouse == 7) pada = Add(pada, 9);   // 10th sign inclusive
            var longitude = SignIndex(pada) * 30.0 + lagnaDegInSign;
            var code = house == 1 ? "AL" : $"A{house}";
            seeds.Add(new SpecialPointSeed(code, "Arudha", AstroMath.Normalize(longitude)));
        }
        return seeds;
    }
}
