using Ikiastrro.Core.Astro;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Calculators;

/// <summary>
/// The actual D9 (Navamsa) computation, extracted from D9NavamsaCalculator (mirrors D1ChartComputer/
/// D1RasiCalculator's split) so other consumers — namely ChartAnalyzer, via the CLI/Web write paths —
/// can reuse the same computed placements without recomputing or parsing them back out of JSON.
///
/// NirayanaLongitudeDegrees is populated here too, from the same underlying rasi (D1) longitude each
/// planet's Navamsa sign is derived from — it's the same real ecliptic position regardless of which
/// divisional chart is reading it, so it stays meaningful (and keeps tbl_Chart_KeyDetails.
/// NirayanaLongitudeDegrees NOT NULL for every chart type). Degree-within-sign, Nakshatra/Pada, and
/// exact display formatting are D1-only concepts (see ChartKeyDetail) and are left unset here.
/// IsRetrograde, by contrast, is populated here too (2026-08-28) — like NirayanaLongitudeDegrees, a
/// planet's real motion doesn't depend on which divisional chart is reading it.
///
/// VargaLongitudeDegrees is ALSO populated here (2026-08-28 combustion fix) — unlike
/// NirayanaLongitudeDegrees/IsRetrograde above, this one is deliberately D9-specific: it remaps the
/// real longitude into Navamsa's own 0-360° space (real × 9, mod 360) so ChartAnalyzer can evaluate
/// combustion relative to D9's own zodiac instead of silently reusing the D1 distance-to-Sun.
/// </summary>
public static class D9ChartComputer
{
    /// <summary>Navamsa divides each 30° sign into 9 parts — the "9" in D9.</summary>
    private const int NavamsaDivisions = 9;

    public static ChartAnalysisInput Compute(BirthDetails birthDetails)
    {
        var localMoment = BirthMomentFactory.Create(birthDetails);
        var positions = SwissEphemerisProvider.GetSiderealPositions(localMoment, birthDetails.Latitude, birthDetails.Longitude);

        var navamsaLagnaSign = AstroMath.GetNavamsaSign(positions.AscendantLongitude);

        var planetPositions = new List<PlanetPosition>
        {
            // Navamsa Lagna listed first, same convention as D1 — every chart point in one list.
            // By definition its house number is 1 (a sign's distance from itself).
            new PlanetPosition
            {
                Planet = "Ascendant",
                Sign = navamsaLagnaSign.ToString(),
                NirayanaLongitudeDegrees = positions.AscendantLongitude,
                VargaLongitudeDegrees = AstroMath.GetVargaLongitude(positions.AscendantLongitude, NavamsaDivisions),
                HouseNumber = 1
            }
        };

        foreach (var planet in PlanetNames.All9)
        {
            var nirayanaLongitude = positions.PlanetLongitudes[planet];
            var navamsaSign = AstroMath.GetNavamsaSign(nirayanaLongitude);
            var houseNumber = AstroMath.CountFromSignToSign(navamsaLagnaSign, navamsaSign);

            planetPositions.Add(new PlanetPosition
            {
                Planet = planet.ToString(),
                Sign = navamsaSign.ToString(),
                NirayanaLongitudeDegrees = nirayanaLongitude,
                VargaLongitudeDegrees = AstroMath.GetVargaLongitude(nirayanaLongitude, NavamsaDivisions),
                HouseNumber = houseNumber,
                // Same real motion regardless of which divisional chart is reading the longitude —
                // a planet doesn't become "less retrograde" because you're looking at its Navamsa.
                IsRetrograde = positions.PlanetSpeeds[planet] < 0
            });
        }

        return new ChartAnalysisInput("D9", navamsaLagnaSign, planetPositions);
    }
}
