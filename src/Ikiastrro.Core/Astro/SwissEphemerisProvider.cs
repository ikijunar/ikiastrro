using Ikiastrro.Core.Calculators;
using Ikiastrro.Core.Models;
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
    IReadOnlyDictionary<PlanetName, double> PlanetSpeeds,
    double AyanamshaDegrees,
    double LocalSiderealTimeHours);

/// <summary>
/// The three sunrise/sunset instants that frame the Vedic day the birth falls in, plus a
/// night-birth flag. All are local-offset <see cref="DateTimeOffset"/>s built from
/// <c>BirthDetails.UtcOffset</c>.
///
/// <see cref="Sunrise"/> opens that Vedic day, <see cref="Sunset"/> splits it from its
/// night, <see cref="NextSunrise"/> closes it. So the <b>day arc</b> is
/// [<see cref="Sunrise"/>, <see cref="Sunset"/>] and the <b>night arc</b> is
/// [<see cref="Sunset"/>, <see cref="NextSunrise"/>] — Gulika / Maandi (Task 5) divide
/// whichever arc the birth is in into eight.
///
/// For a "night birth" — birth after midnight but before that calendar day's sunrise, as
/// 1_Ramakrishnan's 05:30 vs 05:56 sunrise is — the Vedic day began at the PREVIOUS
/// calendar day's sunrise, so <see cref="Sunrise"/> / <see cref="Sunset"/> are that prior
/// day's pair and <see cref="NextSunrise"/> is the birth calendar date's sunrise. This
/// matches JHora, which prints "Sunrise: 5:56:39 (April 21)" for this 22-Apr-1981 birth.
/// Hora Lagna's reference sunrise is always <see cref="Sunrise"/>.
/// </summary>
public record SunTimes(
    DateTimeOffset Sunrise,
    DateTimeOffset Sunset,
    DateTimeOffset NextSunrise,
    bool IsNightBirth);

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

        // ascmc[2] = ARMC (Right Ascension of the MC) in degrees = the local apparent
        // sidereal time; /15 -> hours, normalised to [0,24).
        var lst = (ascmc[2] / 15.0) % 24.0;
        if (lst < 0) lst += 24.0;
        var ayanamsha = sweph.swe_get_ayanamsa_ut(jd);

        return new SiderealPositions(
            ascendantLongitude, planetLongitudes, planetLatitudes, planetSpeeds, ayanamsha, lst);
    }

    /// <summary>Convenience overload for callers (e.g. ChartGenerationService in the
    /// Data layer) that hold a BirthDetails but cannot reach the internal
    /// BirthMomentFactory.</summary>
    public static SiderealPositions GetSiderealPositions(BirthDetails birthDetails) =>
        GetSiderealPositions(
            BirthMomentFactory.Create(birthDetails),
            birthDetails.Latitude,
            birthDetails.Longitude);

    /// <summary>
    /// Sunrise / sunset for a person's birth date &amp; place — see <see cref="SunTimes"/>.
    ///
    /// Path taken: SwissEphNet 2.8.0.2 DOES expose <c>swe_rise_trans</c> plus the
    /// <c>SE_CALC_RISE</c> / <c>SE_CALC_SET</c> / <c>SE_BIT_DISC_CENTER</c> /
    /// <c>SE_BIT_NO_REFRACTION</c> constants (verified by reflecting the shipped
    /// <c>SwissEphNet.dll</c>), so the manual hour-angle fallback in the brief is not
    /// needed. Signature used:
    /// <c>int swe_rise_trans(double tjd_ut, int ipl, string starname, int epheflag,
    /// int rsmi, double[] geopos, double atpress, double attemp, ref double tret,
    /// ref string serr)</c>.
    ///
    /// Rise/set flag combination: <c>SE_BIT_DISC_CENTER | SE_BIT_NO_REFRACTION</c> — i.e.
    /// geometric centre of the Sun's disc crossing the true horizon, no atmospheric
    /// refraction. This is JHora / Parashara's Light's default and lands within ~2s of the
    /// golden record's <c>5:56:39</c> sunrise / <c>18:18:53</c> sunset (Chennai,
    /// 22 Apr 1981). The residual is a fixed early bias: SwissEphNet ships no <c>.se1</c>
    /// files, so <c>swe_rise_trans</c> falls back to the Moshier analytic theory rather than
    /// the full Swiss Ephemeris JHora uses, and delta-T handling differs slightly. Toggling
    /// either bit off shifts sunrise by ~1–2 min (refraction) or ~2.5 min (disc), which is a
    /// real break; the ~2s Moshier gap is not. <c>verify-jaimini</c> asserts a 5s tolerance.
    /// </summary>
    public static SunTimes GetSunTimes(BirthDetails birthDetails)
    {
        var moment = BirthMomentFactory.Create(birthDetails);   // local-offset
        var offset = moment.Offset;
        using var sweph = new SwissEph();

        var geopos = new[] { birthDetails.Longitude, birthDetails.Latitude, 0.0 };

        double JdOf(DateTimeOffset t)
        {
            var u = t.ToUniversalTime();
            return sweph.swe_julday(u.Year, u.Month, u.Day,
                u.Hour + u.Minute / 60.0 + u.Second / 3600.0, SwissEph.SE_GREG_CAL);
        }

        DateTimeOffset LocalOf(double jdUt)
        {
            // SwissEphNet's swe_revjul takes ref (not out) parameters.
            int y = 0, mo = 0, d = 0;
            double h = 0;
            sweph.swe_revjul(jdUt, SwissEph.SE_GREG_CAL, ref y, ref mo, ref d, ref h);
            var whole = (int)h;
            var min = (int)((h - whole) * 60);
            var sec = (int)Math.Round(((h - whole) * 60 - min) * 60);
            var utc = new DateTimeOffset(y, mo, d, whole, min, 0, TimeSpan.Zero).AddSeconds(sec);
            return utc.ToOffset(offset);
        }

        double NextEvent(double fromJd, int rsmi)
        {
            double tret = 0;
            string serr = "";
            var rc = sweph.swe_rise_trans(fromJd, SwissEph.SE_SUN, null,
                SwissEph.SEFLG_MOSEPH, rsmi, geopos, 0.0, 0.0, ref tret, ref serr);
            if (rc < 0) throw new InvalidOperationException($"swe_rise_trans failed: {serr}");
            return tret;
        }

        const int riseFlag = SwissEph.SE_CALC_RISE | SwissEph.SE_BIT_DISC_CENTER | SwissEph.SE_BIT_NO_REFRACTION;
        const int setFlag = SwissEph.SE_CALC_SET | SwissEph.SE_BIT_DISC_CENTER | SwissEph.SE_BIT_NO_REFRACTION;

        // Search anchor: local midnight of the birth calendar date, expressed in UT.
        var localMidnight = new DateTimeOffset(
            birthDetails.DateOfBirth.ToDateTime(TimeOnly.MinValue), offset);
        var midnightJd = JdOf(localMidnight);

        var birthDateSunriseJd = NextEvent(midnightJd, riseFlag);
        var birthDateSunrise = LocalOf(birthDateSunriseJd);
        var isNight = moment < birthDateSunrise;   // birth precedes the day's sunrise -> night arc

        // The sunrise that OPENS the Vedic day containing the birth (and the sunset that
        // splits that day from its night). For a night birth that day opened at the
        // PREVIOUS calendar day's sunrise — exactly what JHora prints as "(April 21)".
        var arcSunriseJd = isNight ? NextEvent(midnightJd - 1.0, riseFlag) : birthDateSunriseJd;
        var arcSunsetJd = NextEvent(arcSunriseJd, setFlag);
        var sunrise = LocalOf(arcSunriseJd);
        var sunset = LocalOf(arcSunsetJd);
        // The sunrise that closes this Vedic day / opens the next. For a night birth that is
        // the birth calendar date's own sunrise; for a day birth it is the following day's.
        var nextSunrise = LocalOf(NextEvent(arcSunsetJd, riseFlag));

        return new SunTimes(sunrise, sunset, nextSunrise, isNight);
    }
}
