using Ikiastrro.Core.Astro;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Calculators;

/// <summary>
/// The actual D1 (Rasi) computation, extracted from D1RasiCalculator so other consumers (e.g. the
/// key-details/dignity writer) can reuse the same computed planet data without recomputing it or
/// parsing it back out of JSON. Raw sidereal longitudes come from SwissEphemerisProvider (Swiss
/// Ephemeris, Moshier mode, Lahiri sidereal) — see that class for why this replaced VedAstro.Library.
/// </summary>
public static class D1ChartComputer
{
    public static ChartAnalysisInput Compute(BirthDetails birthDetails)
    {
        var localMoment = BirthMomentFactory.Create(birthDetails);
        var positions = SwissEphemerisProvider.GetSiderealPositions(localMoment, birthDetails.Latitude, birthDetails.Longitude);

        var ascendantSign = AstroMath.GetSignAtLongitude(positions.AscendantLongitude);
        var (ascendantNakshatra, ascendantPada) = AstroMath.GetNakshatraAndPada(positions.AscendantLongitude);

        var planetPositions = new List<PlanetPosition>
        {
            new PlanetPosition
            {
                Planet = "Ascendant",
                Sign = ascendantSign.ToString(),
                DegreesInSign = AstroMath.FormatDegreesMinutesSeconds(AstroMath.GetDegreesInSign(positions.AscendantLongitude)),
                NirayanaLongitudeDegrees = positions.AscendantLongitude,
                Nakshatra = AstroMath.GetNakshatraName(ascendantNakshatra),
                NakshatraPada = ascendantPada,
                HouseNumber = 1
            }
        };

        foreach (var planet in PlanetNames.All9)
        {
            var nirayanaLongitude = positions.PlanetLongitudes[planet];
            var sign = AstroMath.GetSignAtLongitude(nirayanaLongitude);
            var (nakshatra, pada) = AstroMath.GetNakshatraAndPada(nirayanaLongitude);
            var houseNumber = AstroMath.CountFromSignToSign(ascendantSign, sign);

            planetPositions.Add(new PlanetPosition
            {
                Planet = planet.ToString(),
                Sign = sign.ToString(),
                DegreesInSign = AstroMath.FormatDegreesMinutesSeconds(AstroMath.GetDegreesInSign(nirayanaLongitude)),
                NirayanaLongitudeDegrees = nirayanaLongitude,
                EclipticLatitudeDegrees = positions.PlanetLatitudes[planet],
                SpeedLongitudeDegPerDay = positions.PlanetSpeeds[planet],
                Nakshatra = AstroMath.GetNakshatraName(nakshatra),
                NakshatraPada = pada,
                HouseNumber = houseNumber,
                IsRetrograde = positions.PlanetSpeeds[planet] < 0
            });
        }

        return new ChartAnalysisInput("D1", ascendantSign, planetPositions);
    }
}
