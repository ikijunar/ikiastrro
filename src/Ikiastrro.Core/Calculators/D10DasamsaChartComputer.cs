using Ikiastrro.Core.Astro;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Calculators;

/// <summary>
/// D10 (Dasamsa) computation — career/profession. Traditional Parasara via AstroMath.GetDasamsaSign
/// (odd signs count from the sign itself, even signs from the 9th sign). Mirrors D9ChartComputer:
/// real longitude + varga longitude (x10, combustion-within-varga only) + Whole-Sign house from the
/// varga Lagna.
/// </summary>
public static class D10DasamsaChartComputer
{
    private const int Divisions = 10;

    public static ChartAnalysisInput Compute(BirthDetails birthDetails)
    {
        var localMoment = BirthMomentFactory.Create(birthDetails);
        var positions = SwissEphemerisProvider.GetSiderealPositions(localMoment, birthDetails.Latitude, birthDetails.Longitude);

        var lagnaSign = AstroMath.GetDasamsaSign(positions.AscendantLongitude);

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
            var vargaSign = AstroMath.GetDasamsaSign(nirayanaLongitude);
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

        return new ChartAnalysisInput("D10", lagnaSign, planetPositions);
    }
}
