using Ikiastrro.Core.Astro;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Calculators;

/// <summary>
/// D2 (Hora) computation — classical two-sign Parasara rule via AstroMath.GetHoraSign (every point
/// lands in Leo or Cancer). Mirrors D9ChartComputer's structure: real longitude + varga longitude
/// (x2, for combustion-within-varga only) + Whole-Sign house from the Hora Lagna. Degree-in-sign
/// and nakshatra/pada are D1-only concepts and left unset.
/// </summary>
public static class D2HoraChartComputer
{
    private const int Divisions = 2;

    public static ChartAnalysisInput Compute(BirthDetails birthDetails)
    {
        var localMoment = BirthMomentFactory.Create(birthDetails);
        var positions = SwissEphemerisProvider.GetSiderealPositions(localMoment, birthDetails.Latitude, birthDetails.Longitude);

        var lagnaSign = AstroMath.GetHoraSign(positions.AscendantLongitude);

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
            var vargaSign = AstroMath.GetHoraSign(nirayanaLongitude);
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

        return new ChartAnalysisInput("D2", lagnaSign, planetPositions);
    }
}
