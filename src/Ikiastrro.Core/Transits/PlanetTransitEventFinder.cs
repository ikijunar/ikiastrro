using Ikiastrro.Core.Astro;

namespace Ikiastrro.Core.Transits;

/// <summary>One sign-boundary crossing for one planet, at the exact moment it happened.</summary>
public record PlanetTransitEvent(PlanetName Planet, DateTime EventDateTimeUtc, ZodiacName Sign, bool IsRetrograde);

/// <summary>
/// Finds every sign-boundary crossing for the slow planets (Saturn, Jupiter, Rahu) over an arbitrary
/// UTC date range, by walking day-by-day and bisecting each detected crossing down to within 1 minute.
/// Backs tbl_PlanetSignTransitEvents (see docs/vedic-reference-tables.md, point 2).
///
/// Daily stepping is safe here specifically because these three are the slowest-moving bodies — none
/// of them can cross more than one 30° sign boundary within a single day (Rahu's fastest daily motion
/// is ~0.053°, Jupiter's ~0.2°, Saturn's ~0.09°), so a day-over-day sign-index change always reflects
/// exactly one crossing, never a missed multi-crossing. Latitude/longitude passed to
/// SwissEphemerisProvider are irrelevant here (0,0) -- they only affect the Ascendant/houses, which
/// this never reads, only PlanetLongitudes/PlanetSpeeds.
/// </summary>
public static class PlanetTransitEventFinder
{
    public static readonly IReadOnlyList<PlanetName> TrackedPlanets = new[]
    {
        PlanetName.Saturn, PlanetName.Jupiter, PlanetName.Rahu
    };

    private static SiderealPositions GetPositions(DateTime utc) =>
        SwissEphemerisProvider.GetSiderealPositions(new DateTimeOffset(utc, TimeSpan.Zero), 0, 0);

    /// <summary>
    /// Walks [startUtc, endUtc], returning every crossing event for every tracked planet, in the
    /// chronological order they were detected (not separated by planet -- callers needing IsReentry
    /// should filter to one planet and re-sort by EventDateTimeUtc first).
    /// </summary>
    public static IReadOnlyList<PlanetTransitEvent> FindCrossings(DateTime startUtc, DateTime endUtc, Action<DateTime>? onDayProcessed = null)
    {
        var events = new List<PlanetTransitEvent>();

        var current = startUtc;
        var currentPositions = GetPositions(current);
        var previousSigns = TrackedPlanets.ToDictionary(p => p, p => AstroMath.GetSignAtLongitude(currentPositions.PlanetLongitudes[p]));

        while (current < endUtc)
        {
            var next = current.AddDays(1) > endUtc ? endUtc : current.AddDays(1);
            var nextPositions = GetPositions(next);

            foreach (var planet in TrackedPlanets)
            {
                var nextSign = AstroMath.GetSignAtLongitude(nextPositions.PlanetLongitudes[planet]);
                if (nextSign != previousSigns[planet])
                {
                    var lo = current;
                    var hi = next;
                    var loSign = previousSigns[planet];
                    while ((hi - lo) > TimeSpan.FromMinutes(1))
                    {
                        var mid = lo + TimeSpan.FromTicks((hi - lo).Ticks / 2);
                        var midSign = AstroMath.GetSignAtLongitude(GetPositions(mid).PlanetLongitudes[planet]);
                        if (midSign == loSign) lo = mid; else hi = mid;
                    }

                    var eventPositions = GetPositions(hi);
                    var isRetrograde = eventPositions.PlanetSpeeds[planet] < 0;
                    events.Add(new PlanetTransitEvent(planet, hi, nextSign, isRetrograde));
                }
                previousSigns[planet] = nextSign;
            }

            onDayProcessed?.Invoke(current);
            current = next;
        }

        return events;
    }

    /// <summary>
    /// Marks IsReentry = true for any event whose Sign matches the sign from two events earlier for
    /// the SAME planet -- the "double-dip" pattern (enter B, retrograde back to A, re-enter B) shows
    /// up as A,B,A,B in a planet's own chronological sequence; events[i].Sign == events[i-2].Sign
    /// flags both the backward return and the forward re-entry. Requires events already filtered to
    /// one planet and sorted by EventDateTimeUtc.
    /// </summary>
    public static IReadOnlyList<(PlanetTransitEvent Event, bool IsReentry)> MarkReentries(IReadOnlyList<PlanetTransitEvent> onePlanetEventsChronological)
    {
        var result = new List<(PlanetTransitEvent, bool)>();
        for (var i = 0; i < onePlanetEventsChronological.Count; i++)
        {
            var isReentry = i >= 2 && onePlanetEventsChronological[i].Sign == onePlanetEventsChronological[i - 2].Sign;
            result.Add((onePlanetEventsChronological[i], isReentry));
        }
        return result;
    }
}
