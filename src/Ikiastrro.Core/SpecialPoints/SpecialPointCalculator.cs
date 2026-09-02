using Ikiastrro.Core.Calculators;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.SpecialPoints;

/// <summary>Computes every special point's D1 longitude for a person. Task 4 wires the
/// Arudhas; Task 5 adds HL / Gulika / Maandi.</summary>
public static class SpecialPointCalculator
{
    public static IReadOnlyList<SpecialPointSeed> ComputeSeeds(BirthDetails birthDetails)
    {
        var d1 = D1ChartComputer.Compute(birthDetails, Array.Empty<SpecialPointSeed>());
        var seeds = new List<SpecialPointSeed>();
        seeds.AddRange(ArudhaCalculator.Compute(d1));
        return seeds;
    }
}
