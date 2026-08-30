using Ikiastrro.Core.Astro;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Calculators;

/// <summary>
/// D11 (Rudramsa) computation — gains/income. Traditional Parasara / Sanjay Rath via
/// AstroMath.GetRudramsaSign ((12 - signIndex + part) mod 12). Mirrors D9ChartComputer: real
/// longitude + varga longitude (x11, combustion-within-varga only) + Whole-Sign house from the
/// varga Lagna.
/// </summary>
public static class D11RudramsaChartComputer
{
    private const int Divisions = 11;

    public static ChartAnalysisInput Compute(BirthDetails birthDetails)
    {
        var localMoment = BirthMomentFactory.Create(birthDetails);
        var positions = SwissEphemerisProvider.GetSiderealPositions(localMoment, birthDetails.Latitude, birthDetails.Longitude);

        var lagnaSign = AstroMath.GetRudramsaSign(positions.AscendantLongitude);

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
            var vargaSign = AstroMath.GetRudramsaSign(nirayanaLongitude);
            planetPositions.Add(new PlanetPosition
            {
                Planet = planet.ToString(),
                Sign = vargaSign.ToString(),
                NirayanaLongitudeDegrees = nirayanaLongitude,
                EclipticLatitudeDegrees = positions.PlanetLatitudes[planet],
                SpeedLongitudeDegPerDay = positions.PlanetSpeeds[planet],
                VargaLongitudeDegrees = AstroMath.GetVargaLongitude(nirayanaLongitude, Divisions),
                HouseNumber = AstroMath.CountFromSignToSign(lagnaSign, vargaSign),
                IsRetrograde = positions.PlanetSpeeds[planet] < 0
            });
        }

        return new ChartAnalysisInput("D11", lagnaSign, planetPositions);
    }
}
