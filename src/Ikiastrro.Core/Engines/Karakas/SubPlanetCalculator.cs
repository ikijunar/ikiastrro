using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Engines.Karakas;

/// <summary>All eleven upagrahas from versioned PVR rules, before varga projection.</summary>
public static class SubPlanetCalculator
{
    public static IReadOnlyList<SpecialPointSeed> Compute(BirthDetails birth, SunTimes sun,
        double sunLongitude, SubPlanetRuleSet rules, AyanamsaDefinition? ayanamsa = null) =>
        Compute(sun, sunLongitude, rules, instant => SwissEphemerisProvider
            .GetSiderealPositions(instant, birth.Latitude, birth.Longitude, ayanamsa).AscendantLongitude);

    // Pure calculation seam: the caller supplies the sidereal rising Ascendant.
    public static IReadOnlyList<SpecialPointSeed> Compute(SunTimes sun, double sunLongitude,
        SubPlanetRuleSet rules, Func<DateTimeOffset, double> ascendantAt)
    {
        rules.Validate();
        if (!double.IsFinite(sunLongitude)) throw new ArgumentOutOfRangeException(nameof(sunLongitude));
        var values = new Dictionary<string, double>();
        foreach (var r in rules.SunRules.OrderBy(r => r.SequenceNo))
        {
            var input = r.InputCode is null ? sunLongitude : values[r.InputCode];
            values.Add(r.Code, AstroMath.Normalize(r.Operation == "ADD" ? input + r.OffsetDegrees!.Value : 360 - input));
        }
        var start = sun.IsNightBirth ? sun.Sunset : sun.Sunrise;
        var end = sun.IsNightBirth ? sun.NextSunrise : sun.Sunset;
        if (end <= start) throw new ArgumentException("The solar arc must have positive duration.", nameof(sun));
        var parts = rules.Parts.Where(p => p.DayNight == (sun.IsNightBirth ? "NIGHT" : "DAY") &&
            p.Weekday == (int)sun.Sunrise.DayOfWeek).ToArray();
        foreach (var r in rules.TimeRules)
        {
            var part = parts.Single(p => p.RulingPlanet == r.RulingPlanet).PartNumber - 1;
            var instant = start + (end - start) * ((part + r.PartFraction) / r.DivisionCount);
            var longitude = ascendantAt(instant);
            if (!double.IsFinite(longitude)) throw new InvalidOperationException("Non-finite rising Ascendant.");
            values.Add(r.Code, AstroMath.Normalize(longitude));
        }
        return values.Select(p => new SpecialPointSeed(p.Key, "Upagraha", p.Value)).ToArray();
    }
}
