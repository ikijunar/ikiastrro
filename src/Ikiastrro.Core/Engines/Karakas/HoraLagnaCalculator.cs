using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Engines.Karakas;

/// <summary>
/// Hora Lagna. Classical rule (PyJHora <c>special_ascendant</c>, lagna_rate_factor 0.5):
/// take the Sun's sidereal longitude at the Vedic day's opening sunrise
/// (<see cref="SunTimes.Sunrise"/> — the PREVIOUS calendar day's
/// sunrise for a night birth) and add 0.5° per minute of clock time-of-day elapsed since
/// that sunrise. For a night birth the time-of-day difference is negative (birth 05:30 is
/// before sunrise 05:56), which is correct — HL runs slightly behind the Sun.
///
/// Verified against docs/artifacts/reference-charts/Rammy_Jagannatha.txt: 23 Pi 55' 08" (Pisces; Navamsa Aquarius).
/// </summary>
public static class HoraLagnaCalculator
{
    public static SpecialPointSeed Compute(BirthDetails bd, SunTimes sun)
    {
        var reference = sun.Sunrise;
        var sunLonAtSunrise = SwissEphemerisProvider
            .GetSiderealPositions(reference, bd.Latitude, bd.Longitude)
            .PlanetLongitudes[PlanetName.Sun];

        var birth = BirthMomentFactory.Create(bd);
        var elapsedMinutes = (birth.TimeOfDay - reference.TimeOfDay).TotalMinutes;

        var hl = AstroMath.Normalize(sunLonAtSunrise + elapsedMinutes * 0.5);
        return new SpecialPointSeed("HL", "SpecialLagna", hl);
    }
}
