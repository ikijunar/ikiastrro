namespace Ikiastrro.Core.Dasha;

/// <summary>
/// A Dasha period as read back from tbl_Chart_DashaPeriods — flat (Dapper-friendly), with
/// Id/ParentDashaPeriodId so <see cref="BuildTree"/> can reconstruct the Maha -> Antar ->
/// Pratyantar hierarchy from a plain query result. This is the shape both the CLI and the Web UI
/// render from; VimshottariDashaCalculator's own DashaPeriod (the write-side, freshly-computed
/// tree) is a separate, simpler type since it never needs an Id or a flat-to-tree step.
/// </summary>
public class DashaPeriodRecord
{
    public int Id { get; set; }
    public int? ParentDashaPeriodId { get; set; }
    public int LevelNumber { get; set; }
    public int SequenceInParent { get; set; }
    public string Lord { get; set; } = string.Empty;
    /// <summary>FK to tbl_Planets for Lord.</summary>
    public int LordId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int StartDayOffset { get; set; }
    public int EndDayOffset { get; set; }
    public List<DashaPeriodRecord> Children { get; } = new();

    /// <summary>
    /// Reconstructs the 3-level tree from a flat list (e.g. a plain SELECT ordered by
    /// StartDayOffset), grouping each row under its parent by Id/ParentDashaPeriodId.
    /// </summary>
    public static List<DashaPeriodRecord> BuildTree(IReadOnlyList<DashaPeriodRecord> flatRows)
    {
        var byId = flatRows.ToDictionary(r => r.Id);
        var roots = new List<DashaPeriodRecord>();
        foreach (var row in flatRows)
        {
            if (row.ParentDashaPeriodId is int parentId && byId.TryGetValue(parentId, out var parent))
                parent.Children.Add(row);
            else
                roots.Add(row);
        }
        return roots;
    }
}
