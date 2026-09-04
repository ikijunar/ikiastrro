namespace Ikiastrro.Core.Models;

/// <summary>
/// One multi-graha conjunction (Graha Saṃyoga) — the maximal set of ≥ 2 grahas sharing a rāśi in a
/// given chart. Covers the 2-planet case with no special-casing; <see cref="PlanetCount"/>
/// discriminates the pair / triple / quadruple / larger-cluster hierarchy. Members hang off this via
/// <see cref="ChartMultiGrahaConjunctionMember"/>; each existing pair row in tbl_Chart_Conjunctions
/// links back through <c>MultiGrahaConjunctionId</c>. See db/22_add_multigraha_conjunction.sql.
/// </summary>
public class ChartMultiGrahaConjunction
{
    public int Id { get; set; }
    public int ChartResultId { get; set; }
    public int SignId { get; set; }
    public int HouseNumberFromLagna { get; set; }
    public int PlanetCount { get; set; }

    /// <summary>Ascending PlanetId CSV, e.g. "1,3,4,6" — canonical, for idempotent upsert and yoga-subset matching.</summary>
    public string MemberKey { get; set; } = string.Empty;

    /// <summary>
    /// D1 only: <c>max − min</c> of the members' real (Nirāyana) longitude, in degrees, range [0,30).
    /// Null for a varga chart — a varga sign is a discrete bucket, so real-longitude spread is
    /// meaningless there (same reasoning as <see cref="ChartConjunction.DegreeSeparation"/>).
    /// </summary>
    public decimal? LongitudeSpanDegrees { get; set; }

    /// <summary>Canonical member order = ascending PlanetId. Not persisted on the group row itself.</summary>
    public List<ChartMultiGrahaConjunctionMember> Members { get; set; } = new();
}
