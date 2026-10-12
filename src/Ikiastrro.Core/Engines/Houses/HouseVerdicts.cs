using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.LifeMatters;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Engines.Houses;

/// <summary>One house of one chart read two ways: Raman's benefic/malefic count
/// (<see cref="HouseBeneficMaleficCalculator"/>) and PVR step 5's influences on it
/// (<see cref="TargetInfluences"/>).</summary>
public sealed record HouseVerdictRow(int House, ZodiacName Sign, HouseBeneficMaleficResult Raman, TargetInfluenceReading Influences);

/// <summary>The planets that act on one house by Argala (the pairs that hold or are contested) and
/// the planets that obstruct them (Virodhargala). Supplied by the caller because the Argala facts
/// are stored, and read, outside Core.</summary>
public sealed record HouseArgalaPlanets(IReadOnlyList<PlanetName> Argala, IReadOnlyList<PlanetName> Virodhargala)
{
    public static readonly HouseArgalaPlanets None = new([], []);
}

/// <summary>
/// "What acts on each house" for one chart: every house read by Raman's calculator and by PVR
/// step 5. Pure Core, so the Web page and any analytics writer read a house the same way. Argala
/// arrives per house through <paramref name="argalaForHouse"/>; the Data project builds it from
/// the stored Argala facts (<c>ArgalaFacts.HouseVerdicts</c>), the same facts and verdicts Key
/// Inference uses. Computed live; nothing stored.
/// </summary>
public static class HouseVerdicts
{
    /// <param name="grahas">The chart's grahas (the Ascendant row is ignored).</param>
    /// <param name="argalaForHouse">Argala planets for a house number (1-12).</param>
    /// <returns>Empty when the chart lacks a graha's placement.</returns>
    public static IReadOnlyList<HouseVerdictRow> For(
        string ascendantSign, IReadOnlyList<ChartKeyDetail> grahas, Func<int, HouseArgalaPlanets> argalaForHouse)
    {
        var placements = new Dictionary<PlanetName, ZodiacName>();
        foreach (var g in grahas)
            if (Enum.TryParse<PlanetName>(g.Planet, out var planet) && Enum.TryParse<ZodiacName>(g.Sign, out var sign))
                placements[planet] = sign;
        if (placements.Count < 9) return [];

        var lagna = Enum.Parse<ZodiacName>(ascendantSign);
        var raman = HouseBeneficMaleficCalculator.ComputeAll(lagna, grahas);
        var moonWaxing = MoonPhase.Of(grahas);
        return raman.Select(r =>
        {
            var argala = argalaForHouse(r.HouseNumber);
            var influences = TargetInfluences.Read(lagna, placements, r.Sign,
                argalaPlanets: argala.Argala, virodhargalaPlanets: argala.Virodhargala, moonWaxing: moonWaxing);
            return new HouseVerdictRow(r.HouseNumber, r.Sign, r, influences);
        }).ToList();
    }
}
