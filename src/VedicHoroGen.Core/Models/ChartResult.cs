namespace VedicHoroGen.Core.Models;

/// <summary>
/// One computed chart/calculation artifact for a BirthDetails record.
/// Adding a new chart type or calculation flow later (D2, Vimshottari Dasha, Ashtakavarga, ...)
/// means inserting new rows here with a new ChartType — never a schema change.
/// </summary>
public class ChartResult
{
    public int Id { get; set; }

    public int BirthDetailId { get; set; }

    /// <summary>e.g. "D1", "D9". Later: "D2", "VimshottariDasha", "Ashtakavarga", etc.</summary>
    public string ChartType { get; set; } = string.Empty;

    /// <summary>Ayanamsha used for this computation, e.g. "Lahiri".</summary>
    public string Ayanamsha { get; set; } = "Lahiri";

    /// <summary>House system used for this computation, e.g. "WholeSign".</summary>
    public string HouseSystem { get; set; } = "WholeSign";

    /// <summary>Calculation engine + version that produced this result, for reproducibility.</summary>
    public string EngineVersion { get; set; } = string.Empty;

    /// <summary>Full computed result, serialized as JSON (planet positions, houses, etc. per ChartType).</summary>
    public string ResultJson { get; set; } = string.Empty;

    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}
