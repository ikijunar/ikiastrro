namespace Ikiastrro.Core.Models;

/// <summary>
/// One graha's participation in a <see cref="ChartMultiGrahaConjunction"/> — its per-planet degree /
/// longitude / dignity / retrograde / combust, recorded <em>once</em> (not duplicated across the pair
/// rows that planet appears in). <see cref="DignityStatus"/> matches the planet's
/// tbl_Chart_KeyDetails row for the same chart. See db/22_add_multigraha_conjunction.sql.
/// </summary>
public class ChartMultiGrahaConjunctionMember
{
    public int Id { get; set; }
    public int MultiGrahaConjunctionId { get; set; }
    public int PlanetId { get; set; }

    /// <summary>Degrees within this chart's sign; D1 = the real degree-in-sign, varga = the varga-space value.</summary>
    public decimal? DegreesInSign { get; set; }

    /// <summary>Real sidereal longitude, populated for every chart type.</summary>
    public double? NirayanaLongitude { get; set; }

    /// <summary>The planet's longitude in this chart's own 360° space (equals NirayanaLongitude for D1).</summary>
    public decimal? VargaLongitude { get; set; }

    /// <summary>D1 only: <c>|memberLon − mean(memberLon)|</c> across the group. Null for a varga chart.</summary>
    public decimal? OrbFromGroupCenterDegrees { get; set; }

    public string? DignityStatus { get; set; }
    public bool? IsRetrograde { get; set; }
    public bool? IsCombust { get; set; }
}
