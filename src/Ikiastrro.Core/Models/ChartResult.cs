namespace Ikiastrro.Core.Models;

/// <summary>
/// One computed chart/calculation artifact for a BirthDetails record.
/// Adding a new chart type or calculation flow later (D2, Vimshottari Dasha, Ashtakavarga, ...)
/// means inserting new rows here with a new ChartType — never a schema change.
/// </summary>
public class ChartResult
{
    public int Id { get; set; }

    public int BirthDetailId { get; set; }

    /// <summary>FK to tbl_Rule_Sets — the rule-set version this chart was computed under.</summary>
    public int RuleSetId { get; set; } = 1;
    /// <summary>FK to tbl_Dim_ChartType. Null when CalculationKind != "PositionChart" (e.g. VimshottariDasha).</summary>
    public int? ChartTypeId { get; set; }
    /// <summary>"PositionChart" | "VimshottariDasha" — separates divisional charts from the Dasha run.</summary>
    public string CalculationKind { get; set; } = "PositionChart";

    /// <summary>e.g. "D1", "D9". Later: "D2", "VimshottariDasha", "Ashtakavarga", etc.</summary>
    public string ChartType { get; set; } = string.Empty;

    /// <summary>Ayanamsha used for this computation, e.g. "Lahiri".</summary>
    public string Ayanamsha { get; set; } = "Lahiri";

    /// <summary>House system used for this computation, e.g. "WholeSign".</summary>
    public string HouseSystem { get; set; } = "WholeSign";

    /// <summary>Calculation engine + version that produced this result, for reproducibility.</summary>
    public string EngineVersion { get; set; } = string.Empty;

    /// <summary>tbl_Rule_VargaScheme.MethodCode for this chart type (e.g.
    /// "ParasaraTraditional"). Null for D1 and non-position calculations.</summary>
    public string? VargaMethod { get; set; }

    /// <summary>Numeric Lahiri ayanamsha at the birth moment, degrees. One value
    /// per person; the same across all of that person's chart rows.</summary>
    public double? AyanamshaDegrees { get; set; }

    /// <summary>Local apparent sidereal time at the birth moment, hours [0,24).</summary>
    public double? SiderealTimeHours { get; set; }

    /// <summary>Full computed result as JSON — a FROZEN AUDIT SNAPSHOT, never read
    /// back as the source of truth. Every field it holds also has a typed column
    /// (tbl_Chart_KeyDetails / tbl_ChartResults).</summary>
    public string ResultJson { get; set; } = string.Empty;

    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}
