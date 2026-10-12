using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Engines.Houses;

/// <summary>Whether the birth Moon is waxing (Shukla paksha) — the input PVR §13.2 needs for the Moon's
/// functional nature in Aries, Libra and Capricorn Lagnas (<see cref="LagnaFunctionalNature.For"/>).
/// Waxing means the Moon is 0-180° ahead of the Sun. Always read from the natal (D1) longitudes: a
/// varga chart's Moon sign says nothing about the phase.</summary>
public static class MoonPhase
{
    /// <summary>Exact: the Moon's elongation from the Sun is under 180°.</summary>
    public static bool IsWaxing(double sunLongitude, double moonLongitude) =>
        AstroMath.Normalize(moonLongitude - sunLongitude) < 180;

    /// <summary>Whole-sign reading for callers that only hold signs, matching
    /// <see cref="ArgalaCalculator.IsNaturalMalefic"/>: waning once the Moon is more than six signs ahead.</summary>
    public static bool IsWaxing(ZodiacName sunSign, ZodiacName moonSign) =>
        AstroMath.CountFromSignToSign(sunSign, moonSign) <= 6;

    /// <summary>Phase from a chart's grahas using exact longitudes; null when the Sun or Moon row is missing
    /// (the caller then leaves the flag unset and the engine says the phase was assumed).</summary>
    public static bool? Of(IEnumerable<ChartKeyDetail> grahas)
    {
        double? sun = null, moon = null;
        foreach (var g in grahas)
        {
            if (g.PointKind != "Graha") continue;
            if (g.Planet == nameof(PlanetName.Sun)) sun = g.NirayanaLongitudeDegrees;
            else if (g.Planet == nameof(PlanetName.Moon)) moon = g.NirayanaLongitudeDegrees;
        }
        return sun is { } s && moon is { } m ? IsWaxing(s, m) : null;
    }

    /// <summary>Phase from graha signs alone (whole-sign); null when the Sun or Moon is absent.</summary>
    public static bool? Of(IReadOnlyDictionary<PlanetName, ZodiacName> placements) =>
        placements.TryGetValue(PlanetName.Sun, out var sun) && placements.TryGetValue(PlanetName.Moon, out var moon)
            ? IsWaxing(sun, moon)
            : null;
}
