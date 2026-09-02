using Ikiastrro.Core.Astro;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.SpecialPoints;

/// <summary>
/// Gulika and Maandi — the two Saturn upagrahas. The arc the birth falls in (day arc
/// sunrise→sunset, or night arc sunset→next sunrise) is split into eight equal parts. The
/// part ruled by Saturn — by the classical weekday day-part / night-part ruler sequence —
/// gives the instant; the Ascendant rising then IS the upagraha longitude (full 0–360°,
/// then projected into each varga like a planet). Gulika takes the START of Saturn's part,
/// Maandi its MIDDLE.
///
/// Weekday is the Vedic day's (opens at <see cref="SunTimes.Sunrise"/>),
/// so a pre-dawn birth uses the previous civil day's ruler row — matching JHora.
///
/// Verified against scratch/Rammy_Jagannatha.txt (Tuesday-night birth, Saturn = night part 1):
/// Gulika 7 Li 44' 38" (Navamsa Sg), Maandi 18 Li 07' 01" (Navamsa Pi).
/// </summary>
public static class UpagrahaCalculator
{
    // Ruler of each 1/8 part, index 0 = the part starting at the arc's start, weekday 0 = Sunday.
    // Planet indices: 0=Sun 1=Moon 2=Mars 3=Mercury 4=Jupiter 5=Venus 6=Saturn, -1 = the unruled 8th.
    private static readonly int[][] DayParts =
    {
        new[] { 0, 1, 2, 3, 4, 5, 6, -1 }, new[] { 1, 2, 3, 4, 5, 6, -1, 0 },
        new[] { 2, 3, 4, 5, 6, -1, 0, 1 }, new[] { 3, 4, 5, 6, -1, 0, 1, 2 },
        new[] { 4, 5, 6, -1, 0, 1, 2, 3 }, new[] { 5, 6, -1, 0, 1, 2, 3, 4 },
        new[] { 6, -1, 0, 1, 2, 3, 4, 5 },
    };
    // Night parts start from the lord of the 5th weekday on.
    private static readonly int[][] NightParts =
    {
        new[] { 4, 5, 6, -1, 0, 1, 2, 3 }, new[] { 5, 6, -1, 0, 1, 2, 3, 4 },
        new[] { 6, -1, 0, 1, 2, 3, 4, 5 }, new[] { 0, 1, 2, 3, 4, 5, 6, -1 },
        new[] { 1, 2, 3, 4, 5, 6, -1, 0 }, new[] { 2, 3, 4, 5, 6, -1, 0, 1 },
        new[] { 3, 4, 5, 6, -1, 0, 1, 2 },
    };
    private const int SaturnIndex = 6;

    public static (SpecialPointSeed Gulika, SpecialPointSeed Maandi) Compute(
        BirthDetails bd, SunTimes sun)
    {
        var weekday = (int)sun.Sunrise.DayOfWeek;   // Vedic day opener; Sunday = 0
        var (arcStart, arcEnd, parts) = sun.IsNightBirth
            ? (sun.Sunset, sun.NextSunrise, NightParts[weekday])
            : (sun.Sunrise, sun.Sunset, DayParts[weekday]);

        var onePart = (arcEnd - arcStart) / 8.0;
        var saturnPart = Array.IndexOf(parts, SaturnIndex);

        SpecialPointSeed Rising(double partOffset, string code)
        {
            var instant = arcStart + onePart * partOffset;
            var ascLon = SwissEphemerisProvider
                .GetSiderealPositions(instant, bd.Latitude, bd.Longitude)
                .AscendantLongitude;
            return new SpecialPointSeed(code, "Upagraha", AstroMath.Normalize(ascLon));
        }

        return (Rising(saturnPart, "Gulika"), Rising(saturnPart + 0.5, "Maandi"));
    }
}
