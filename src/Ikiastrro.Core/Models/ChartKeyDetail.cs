namespace Ikiastrro.Core.Models;

/// <summary>
/// One row of tbl_Chart_KeyDetails — one planet's flattened position + classical dignity, for ANY
/// chart type (D1, D9, and any future divisional chart share this one table, discriminated by
/// ChartType/ChartResultId — see ChartAnalyzer).
/// </summary>
public class ChartKeyDetail
{
    public int Id { get; set; }
    public int ChartResultId { get; set; }

    public string Planet { get; set; } = string.Empty;
    /// <summary>FK to tbl_Planets. Null for the Ascendant/Lagna row.</summary>
    public int? PlanetId { get; set; }
    public string Sign { get; set; } = string.Empty;
    /// <summary>FK to tbl_SignAttributes.</summary>
    public int? SignId { get; set; }

    /// <summary>
    /// Degrees elapsed within <see cref="Sign"/>. Only meaningful for D1 — a varga sign is a discrete
    /// bucket, not a continuous 30° span, so this (and MoolatrikonaSign/Range's degree-range check) is
    /// null for every other chart type. NirayanaLongitudeDegrees below stays populated regardless — it's
    /// the same real ecliptic longitude no matter which divisional chart is reading it.
    /// </summary>
    public string? DegreesInSignDisplay { get; set; }
    public decimal? DegreesInSignDecimal { get; set; }
    public double NirayanaLongitudeDegrees { get; set; }

    /// <summary>The planet's longitude in THIS chart's own 360-degree space:
    /// (NirayanaLongitudeDegrees x N) mod 360. Equals NirayanaLongitudeDegrees
    /// for D1. Persisted (migration 12) so Vargottama and varga conjunction
    /// tightness are pure SQL. DegreesInSignDecimal = VargaLongitudeDegrees % 30.</summary>
    public double VargaLongitudeDegrees { get; set; }

    /// <summary>Row discriminator: 'Graha' (a planet or the Ascendant) or one of the special
    /// points 'SpecialLagna' / 'Arudha' / 'Upagraha'. Non-'Graha' rows carry only position
    /// (Sign / longitudes / house); dignity, nakshatra, combustion, aspects, CharaKaraka are NULL.</summary>
    public string PointKind { get; set; } = "Graha";

    /// <summary>Jaimini Chara Karaka label ('AK'..'DK') for this graha in this person's D1 —
    /// the same label on every chart type. NULL for Ketu, the Ascendant, and special points.</summary>
    public string? CharaKaraka { get; set; }

    /// <summary>
    /// Ecliptic latitude in degrees (Swiss Ephemeris xx[1]) — the real out-of-plane position.
    /// Populated for D1 and every varga (a real-body fact), like NirayanaLongitudeDegrees. Null for
    /// the Ascendant. Sun/Rahu/Ketu sit on (or ~on) the ecliptic so this is ≈ 0 for them.
    /// </summary>
    public double? EclipticLatitudeDegrees { get; set; }

    /// <summary>
    /// Daily motion in longitude, degrees per day (Swiss Ephemeris xx[3]); negative = retrograde —
    /// the raw value IsRetrograde is derived from, kept for Cheshta-avastha / combustion-orb use.
    /// Populated for D1 and every varga; null for the Ascendant.
    /// </summary>
    public double? SpeedLongitudeDegPerDay { get; set; }

    public string? Nakshatra { get; set; }
    public int? NakshatraPada { get; set; }

    /// <summary>
    /// The classical ruling planet (Vimshottari lord) of the nakshatra at NirayanaLongitudeDegrees.
    /// Unlike Nakshatra/NakshatraPada above, this is NOT gated to D1 — it's derived purely from the
    /// real longitude every chart type carries, so it's populated for D1 and D9 alike (2026-08-28).
    /// Null only for the Ascendant/Lagna row is NOT the case — Lagna's nakshatra lord is a real,
    /// meaningful classical value and is populated too.
    /// </summary>
    public string? NakshatraLordPlanet { get; set; }

    public int? NakshatraLordPlanetId { get; set; }

    /// <summary>FK to tbl_Nakshatras — the nakshatra at NirayanaLongitudeDegrees. Populated for D1 and every varga (real-longitude fact), same as NakshatraLordPlanet.</summary>
    public byte? NakshatraId { get; set; }

    /// <summary>FK to tbl_NakshatraPadas (Id = overall pada slot + 1). D1 only — matches NakshatraPada's gating.</summary>
    public int? NakshatraPadaId { get; set; }

    /// <summary>KP nakshatra sub-lord (Vimshottari level 2) planet name, e.g. "Venus". Populated for D1 and every varga, like NakshatraLordPlanet. Name string, not an FK (consistent with SignLordPlanet).</summary>
    public string? NakshatraSubLordPlanet { get; set; }

    public int? NakshatraSubLordPlanetId { get; set; }

    /// <summary>True if retrograde (Vakri) at this moment — from Swiss Ephemeris's own motion speed. Null for the Ascendant (no retrograde concept). Same real value for D1 and D9 (2026-08-28).</summary>
    public bool? IsRetrograde { get; set; }

    /// <summary>Classical combustion (Asta) — within orb of the Sun, evaluated within THIS row's own chart type (D1 uses real longitude, D9 uses Navamsa-remapped longitude — see CombustionEngine/VargaLongitudeDegrees). Only meaningful for Moon/Mars/Mercury/Jupiter/Venus/Saturn; null for Sun/Rahu/Ketu/Ascendant. Computed separately per chart type since 2026-08-28 — a D9 row is no longer guaranteed to match its D1 counterpart.</summary>
    public bool? IsCombust { get; set; }

    /// <summary>Actual angular separation from the Sun within this row's own chart type (D1: real degrees; D9: Navamsa-space degrees), 0-180°, for the 6 planets combustion applies to. Null otherwise.</summary>
    public decimal? DistanceFromSunDegrees { get; set; }

    /// <summary>The orb threshold actually used to decide IsCombust (direct vs. retrograde-narrowed) — kept for audit/debugging, not just the boolean result.</summary>
    public decimal? CombustionOrbUsedDegrees { get; set; }

    /// <summary>Whole Sign house, counted from the Ascendant (Lagna) — the default/original reckoning.</summary>
    public int HouseNumberFromLagna { get; set; }
    /// <summary>Whole Sign house, counted from the Sun's sign (Surya Lagna).</summary>
    public int HouseNumberFromSun { get; set; }
    /// <summary>Whole Sign house, counted from the Moon's sign (Chandra Lagna) — the most commonly used alternate reckoning.</summary>
    public int HouseNumberFromMoon { get; set; }

    public string? OwnSigns { get; set; }
    public string? ExaltationSign { get; set; }
    public string? DebilitationSign { get; set; }
    public string? MoolatrikonaSign { get; set; }
    public string? MoolatrikonaRange { get; set; }
    public string? SignLordPlanet { get; set; }
    public int? SignLordPlanetId { get; set; }
    public string? DignityStatus { get; set; }

    /// <summary>Which planet(s) cast a classical aspect (Drishti) onto this planet, e.g. "Mars (8th), Saturn (3rd)". Null if none.</summary>
    public string? AspectingPlanets { get; set; }
}
