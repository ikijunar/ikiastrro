using Ikiastrro.Core.Astro;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Calculators;

/// <summary>
/// D6 (Shashtamsa) computation — health/disease. Traditional Parasara via AstroMath.GetShashtamsaSign
/// (odd signs -> Aries..Virgo, even signs -> Libra..Pisces). Mirrors D9ChartComputer: real longitude
/// + varga longitude (x6, combustion-within-varga only) + Whole-Sign house from the varga Lagna.
/// </summary>
public static class D6ShashtamsaChartComputer
{
    private const int Divisions = 6;

    public static ChartAnalysisInput Compute(BirthDetails birthDetails)
    {
        var localMoment = BirthMomentFactory.Create(birthDetails);
        var positions = SwissEphemerisProvider.GetSiderealPositions(localMoment, birthDetails.Latitude, birthDetails.Longitude);

        var lagnaSign = AstroMath.GetShashtamsaSign(positions.AscendantLongitude);

        var planetPositions = new List<PlanetPosition>
        {
            new PlanetPosition
            {
                Planet = "Ascendant",
                Sign = lagnaSign.ToString(),
                NirayanaLongitudeDegrees = positions.AscendantLongitude,
                VargaLongitudeDegrees = AstroMath.GetVargaLongitude(positions.AscendantLongitude, Divisions),
                HouseNumber = 1
            }
        };

        foreach (var planet in PlanetNames.All9)
        {
            var nirayanaLongitude = positions.PlanetLongitudes[planet];
            var vargaSign = AstroMath.GetShashtamsaSign(nirayanaLongitude);
            planetPositions.Add(new PlanetPosition
            {
                Planet = planet.ToString(),
                Sign = vargaSign.ToString(),
                NirayanaLongitudeDegrees = nirayanaLongitude,
                VargaLongitudeDegrees = AstroMath.GetVargaLongitude(nirayanaLongitude, Divisions),
                HouseNumber = AstroMath.CountFromSignToSign(lagnaSign, vargaSign),
                IsRetrograde = positions.PlanetSpeeds[planet] < 0
            });
        }

        return new ChartAnalysisInput("D6", lagnaSign, planetPositions);
    }
}
