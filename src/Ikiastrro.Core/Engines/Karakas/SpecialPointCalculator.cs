using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Position;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Engines.Karakas;

/// <summary>Computes every special point's D1 longitude for a person: AL + the 12 Bhava
/// Arudhas (A2–A12), Hora Lagna, Gulika, Maandi. Each is then projected into every varga
/// by SpecialPointProjector.</summary>
public static class SpecialPointCalculator
{
    public static IReadOnlyList<SpecialPointSeed> ComputeSeeds(BirthDetails birthDetails)
    {
        var d1 = D1ChartComputer.Compute(birthDetails, Array.Empty<SpecialPointSeed>());
        var sun = SwissEphemerisProvider.GetSunTimes(birthDetails);

        var seeds = new List<SpecialPointSeed>();
        seeds.AddRange(ArudhaCalculator.Compute(d1));
        seeds.Add(HoraLagnaCalculator.Compute(birthDetails, sun));
        var (gulika, maandi) = UpagrahaCalculator.Compute(birthDetails, sun);
        seeds.Add(gulika);
        seeds.Add(maandi);
        return seeds;
    }
}
