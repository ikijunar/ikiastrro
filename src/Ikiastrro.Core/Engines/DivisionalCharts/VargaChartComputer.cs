using Ikiastrro.Core.Pipeline;
using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Engines.Karakas;

namespace Ikiastrro.Core.Engines.DivisionalCharts;

/// <summary>
/// Computes ANY divisional chart from an injected division factor + IVargaSignRule.
/// Replaces the per-varga D2/D6/D9/D10/D11 ChartComputers: same shape (real
/// longitude + varga longitude + Whole-Sign house from the varga Lagna), only the
/// sign rule and the factor vary. Shared with ChartAnalyzer via ChartAnalysisInput.
/// The ChartType field on the returned input is left blank - VargaCalculator sets it.
/// </summary>
public static class VargaChartComputer
{
    public static ChartAnalysisInput Compute(
        BirthDetails birthDetails, int divisionFactor, IVargaSignRule rule,
        IReadOnlyList<SpecialPointSeed>? seeds = null)
    {
        var localMoment = BirthMomentFactory.Create(birthDetails);
        var positions = SwissEphemerisProvider.GetSiderealPositions(localMoment, birthDetails.Latitude, birthDetails.Longitude);

        var lagnaSign = rule.SignFor(positions.AscendantLongitude);
        var lagnaVargaLon = AstroMath.GetVargaLongitude(positions.AscendantLongitude, divisionFactor);

        var planetPositions = new List<PlanetPosition>
        {
            new PlanetPosition
            {
                Planet = "Ascendant",
                Sign = lagnaSign.ToString(),
                NirayanaLongitudeDegrees = positions.AscendantLongitude,
                VargaLongitudeDegrees = lagnaVargaLon,
                DegreesInSign = AstroMath.FormatDegreesMinutesSeconds(lagnaVargaLon % 30),
                HouseNumber = 1
            }
        };

        foreach (var planet in PlanetNames.All9)
        {
            var realLon = positions.PlanetLongitudes[planet];
            var vargaSign = rule.SignFor(realLon);
            var vargaLon = AstroMath.GetVargaLongitude(realLon, divisionFactor);
            planetPositions.Add(new PlanetPosition
            {
                Planet = planet.ToString(),
                Sign = vargaSign.ToString(),
                NirayanaLongitudeDegrees = realLon,
                EclipticLatitudeDegrees = positions.PlanetLatitudes[planet],
                SpeedLongitudeDegPerDay = positions.PlanetSpeeds[planet],
                VargaLongitudeDegrees = vargaLon,
                DegreesInSign = AstroMath.FormatDegreesMinutesSeconds(vargaLon % 30),
                HouseNumber = AstroMath.CountFromSignToSign(lagnaSign, vargaSign),
                IsRetrograde = positions.PlanetSpeeds[planet] < 0
            });
        }

        var specialPoints = SpecialPointProjector.Project(
            seeds ?? Array.Empty<SpecialPointSeed>(), rule, divisionFactor, lagnaSign);
        return new ChartAnalysisInput(ChartType: "", lagnaSign, planetPositions) { SpecialPoints = specialPoints };
    }
}
