using VedicHoroGen.Core.Astro;

namespace VedicHoroGen.Core.Dasha;

/// <summary>
/// One Vimshottari Dasha period at any level (1=Mahadasha, 2=Antardasha, 3=Pratyantardasha).
/// A tree, not a flat list — Children holds the next level down, empty for a level-3 leaf.
/// VimshottariDashaRepository (Data) flattens this into tbl_Chart_DashaPeriods rows, assigning
/// ParentDashaPeriodId as it walks the tree.
/// </summary>
public class DashaPeriod
{
    public int LevelNumber { get; init; }

    /// <summary>1-9 — this period's position in its parent's 9-lord cycle (its true classical
    /// position, not renumbered to start at 1 when the first-emitted period is birth-partial).</summary>
    public int SequenceInParent { get; init; }

    public PlanetName Lord { get; init; }

    public DateTimeOffset StartDate { get; init; }

    public DateTimeOffset EndDate { get; init; }

    /// <summary>Whole days since birth (Day 0 = DOB) — joins to tbl_Dim_LifeCalendar.</summary>
    public int StartDayOffset { get; init; }

    public int EndDayOffset { get; init; }

    public List<DashaPeriod> Children { get; init; } = new();
}
