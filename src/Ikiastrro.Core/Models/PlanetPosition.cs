namespace Ikiastrro.Core.Models;

/// <summary>One planet's placement within a computed chart (D1, D9, or any future varga).</summary>
public class PlanetPosition
{
    public string Planet { get; set; } = string.Empty;

    public string Sign { get; set; } = string.Empty;

    /// <summary>Degrees elapsed within the sign, e.g. "7°14'30\"".</summary>
    public string DegreesInSign { get; set; } = string.Empty;

    /// <summary>Sidereal (nirayana) longitude in degrees, 0-360 — the real ecliptic position, populated for D1 and every varga chart alike (same physical planet regardless of which divisional chart is reading it).</summary>
    public double? NirayanaLongitudeDegrees { get; set; }

    /// <summary>
    /// Ecliptic latitude in degrees (xx[1] from Swiss Ephemeris) — the real out-of-plane position.
    /// A real-body value, the same for D1 and every varga. Null for the Ascendant (an ecliptic
    /// point, no latitude concept).
    /// </summary>
    public double? EclipticLatitudeDegrees { get; set; }

    /// <summary>
    /// Daily motion in longitude, degrees per day (xx[3] from Swiss Ephemeris); negative means
    /// retrograde (this is the raw value <see cref="IsRetrograde"/> is derived from). A real-body
    /// value, the same for D1 and every varga. Null for the Ascendant.
    /// </summary>
    public double? SpeedLongitudeDegPerDay { get; set; }

    /// <summary>
    /// The planet's longitude remapped into this varga's own 0-360° space (see
    /// <see cref="Astro.AstroMath.GetVargaLongitude"/>), e.g. Navamsa total longitude for D9. Null for
    /// D1 (there's no remapping — <see cref="NirayanaLongitudeDegrees"/> already IS the varga
    /// longitude). Used by ChartAnalyzer to evaluate combustion relative to this varga's own zodiac
    /// rather than borrowing the D1 (real) distance to the Sun (2026-08-28 fix).
    /// </summary>
    public double? VargaLongitudeDegrees { get; set; }

    public string? Nakshatra { get; set; }

    public int? NakshatraPada { get; set; }

    /// <summary>House number (1-12) the planet occupies, using Whole Sign counted from the Ascendant.</summary>
    public int HouseNumber { get; set; }

    /// <summary>
    /// True if the planet's apparent motion is retrograde (Vakri) at this moment — from Swiss
    /// Ephemeris's own daily motion speed, not a separate calculation. Null for the Ascendant (a
    /// house-circle point, not an orbiting body — no retrograde concept applies).
    /// </summary>
    public bool? IsRetrograde { get; set; }
}
