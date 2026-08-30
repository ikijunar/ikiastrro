using SwissEphNet;

namespace Ikiastrro.Core.Astro;

/// <summary>
/// Sidereal (nirayana, Lahiri) longitudes for the Ascendant and all 9 planets at one moment/place,
/// plus each planet's ecliptic latitude (deg) and daily motion speed in longitude (deg/day —
/// negative means retrograde). The Ascendant has no latitude/speed/retrograde concept (it's a
/// house-circle point, not an orbiting body), so PlanetLatitudes/PlanetSpeeds only cover the 9
/// planets, same set as PlanetLongitudes.
/// </summary>
public record SiderealPositions(
    double AscendantLongitude,
    IReadOnlyDictionary<PlanetName, double> PlanetLongitudes,
    IReadOnlyDictionary<PlanetName, double> PlanetLatitudes,
    IReadOnlyDictionary<PlanetName, double> PlanetSpeeds);

/// <summary>
/// Replaces VedAstro.Library as the raw astronomical engine. Wraps SwissEphNet — a direct managed C#
/// port of Astrodienst's Swiss Ephemeris (the same precision source behind JHora, Parashara's Light,
/// and Jyotish Dashboard) — using its Moshier analytical mode (SEFLG_MOSEPH): no external ephemeris
/// data files to bundle or configure, ~1 arcsecond accuracy, more than sufficient given this project's
/// own cross-check against Prokerala/AstroSage already targets sub-arcminute agreement.
///
/// Sidereal longitudes are requested directly via SEFLG_SIDEREAL + SE_SIDM_LAHIRI (Swiss Ephemeris's
/// own authoritative Lahiri ayanamsha implementation) — unlike VedAstro.Library, there is no separate
/// ayanamsha-correction step needed; Swiss Ephemeris subtracts the ayanamsha internally before
/// returning xx[0].
///
/// Rahu is the MEAN lunar node (SE_MEAN_NODE) — verified against this project's established test chart
/// (22 Apr 1981, Chennai): mean node lands within 0.6 arcmin of both Prokerala and AstroSage, while the
/// astronomically "truer" oscillating true node (SE_TRUE_NODE) is off by ~6.8 arcmin, confirming
/// mainstream Vedic tools key off the mean node, not the true node, for Rahu/Ketu. Ketu is derived as
/// Rahu + 180° — Swiss Ephemeris has no separate Ketu body, same as every other engine, since it's not
/// a real celestial body.
/// </summary>
public static class SwissEphemerisProvider
{
    private const int SeSidmLahiri = 1;

    public static SiderealPositions GetSiderealPositions(DateTimeOffset localMoment, double latitude, double longitude)
    {
        using var sweph = new SwissEph();
        sweph.swe_set_sid_mode(SeSidmLahiri, 0, 0);

        var utc = localMoment.ToUniversalTime();
        var utHours = utc.Hour + utc.Minute / 60.0 + utc.Second / 3600.0;
        var jd = sweph.swe_julday(utc.Year, utc.Month, utc.Day, utHours, SwissEph.SE_GREG_CAL);

        const int flags = SwissEph.SEFLG_SIDEREAL | SwissEph.SEFLG_MOSEPH | SwissEph.SEFLG_SPEED;

        // xx[0] = longitude, xx[1] = ecliptic latitude (deg), xx[3] = daily motion speed in
        // longitude (deg/day) — negative means retrograde. Returned together since callers need
        // the longitude and the latitude/speed come free from the same swe_calc_ut call
        // (SEFLG_SPEED is already set).
        (double Longitude, double Latitude, double Speed) GetPosition(int body, string label)
        {
            var xx = new double[6];
            var serr = "";
            var result = sweph.swe_calc_ut(jd, body, flags, xx, ref serr);
            if (result < 0)
            {
                throw new InvalidOperationException($"Swiss Ephemeris calculation failed for {label}: {serr}");
            }
            return (xx[0], xx[1], xx[3]);
        }

        var planetLongitudes = new Dictionary<PlanetName, double>();
        var planetLatitudes = new Dictionary<PlanetName, double>();
        var planetSpeeds = new Dictionary<PlanetName, double>();

        void SetPosition(PlanetName planet, int body, string label)
        {
            var (longitude, latitude, speed) = GetPosition(body, label);
            planetLongitudes[planet] = longitude;
            planetLatitudes[planet] = latitude;
            planetSpeeds[planet] = speed;
        }

        SetPosition(PlanetName.Sun, SwissEph.SE_SUN, "Sun");
        SetPosition(PlanetName.Moon, SwissEph.SE_MOON, "Moon");
        SetPosition(PlanetName.Mars, SwissEph.SE_MARS, "Mars");
        SetPosition(PlanetName.Mercury, SwissEph.SE_MERCURY, "Mercury");
        SetPosition(PlanetName.Jupiter, SwissEph.SE_JUPITER, "Jupiter");
        SetPosition(PlanetName.Venus, SwissEph.SE_VENUS, "Venus");
        SetPosition(PlanetName.Saturn, SwissEph.SE_SATURN, "Saturn");

        var (rahuLongitude, rahuLatitude, rahuSpeed) = GetPosition(SwissEph.SE_MEAN_NODE, "Rahu");
        planetLongitudes[PlanetName.Rahu] = rahuLongitude;
        planetLatitudes[PlanetName.Rahu] = rahuLatitude;
        planetSpeeds[PlanetName.Rahu] = rahuSpeed;
        planetLongitudes[PlanetName.Ketu] = AstroMath.Normalize(rahuLongitude + 180);
        // Ketu is a computed point 180° from Rahu, not a separately-tracked body — it moves exactly
        // as Rahu does, so its retrograde status is Rahu's speed sign, unchanged by the 180° offset.
        // The mean node lies on the ecliptic (latitude ~0); Ketu takes the opposite-signed latitude.
        planetLatitudes[PlanetName.Ketu] = -rahuLatitude;
        planetSpeeds[PlanetName.Ketu] = rahuSpeed;

        var cusps = new double[13];
        var ascmc = new double[10];
        var houseErr = "";
        var houseResult = sweph.swe_houses_ex(jd, SwissEph.SEFLG_SIDEREAL | SwissEph.SEFLG_MOSEPH, latitude, longitude, 'W', cusps, ascmc);
        if (houseResult < 0)
        {
            throw new InvalidOperationException($"Swiss Ephemeris house/ascendant calculation failed: {houseErr}");
        }
        var ascendantLongitude = ascmc[0];

        return new SiderealPositions(ascendantLongitude, planetLongitudes, planetLatitudes, planetSpeeds);
    }
}
