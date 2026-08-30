using Dapper;
using VedicHoroGen.Core.Dasha;

namespace VedicHoroGen.Data;

/// <summary>
/// tbl_Chart_DashaPeriods — one row per Vimshottari period at any level (Maha/Antar/Pratyantar),
/// self-referencing via ParentDashaPeriodId. Not one of the 4 shared chart-analytical repositories
/// (ChartKeyDetailsRepository etc.) — Dasha has its own dedicated table/service per the
/// architecture decision recorded in methods_prodmag.md (2026-08-27).
/// </summary>
public class DashaPeriodsRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public DashaPeriodsRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Flattens and inserts a full Dasha tree for one ChartResult, assigning ParentDashaPeriodId as
    /// it walks down. Deletes any existing periods for this ChartResultId first, so recomputing a
    /// person's Dasha (e.g. after a birth-time correction) never leaves stale rows behind — see
    /// tvf_Chart_LifeWeeks's ChartResultId scoping, which specifically guards against that.
    /// </summary>
    public void InsertTree(int chartResultId, int birthDetailId, IReadOnlyList<DashaPeriod> roots)
    {
        using var connection = _connectionFactory.CreateOpenConnection();

        const string deleteSql = "DELETE FROM dbo.tbl_Chart_DashaPeriods WHERE ChartResultId = @ChartResultId";
        connection.Execute(deleteSql, new { ChartResultId = chartResultId });

        const string insertSql = """
            INSERT INTO dbo.tbl_Chart_DashaPeriods
                (ChartResultId, BirthDetailId, ParentDashaPeriodId, LevelNumber, SequenceInParent,
                 Lord, StartDate, EndDate, StartDayOffset, EndDayOffset)
            OUTPUT INSERTED.Id
            VALUES
                (@ChartResultId, @BirthDetailId, @ParentDashaPeriodId, @LevelNumber, @SequenceInParent,
                 @Lord, @StartDate, @EndDate, @StartDayOffset, @EndDayOffset)
            """;

        void InsertRecursive(DashaPeriod period, int? parentId)
        {
            var newId = connection.ExecuteScalar<int>(insertSql, new
            {
                ChartResultId = chartResultId,
                BirthDetailId = birthDetailId,
                ParentDashaPeriodId = parentId,
                period.LevelNumber,
                period.SequenceInParent,
                Lord = period.Lord.ToString(),
                StartDate = period.StartDate.DateTime,   // local wall-clock, DATETIME2 column has no offset — matches how BirthDetails stores local time-of-day
                EndDate = period.EndDate.DateTime,
                period.StartDayOffset,
                period.EndDayOffset
            });

            foreach (var child in period.Children)
                InsertRecursive(child, newId);
        }

        foreach (var root in roots)
            InsertRecursive(root, null);
    }

    /// <summary>Full tree for one person, reconstructed from a flat, StartDayOffset-ordered query. Empty if Dasha hasn't been computed for them yet.</summary>
    public IReadOnlyList<DashaPeriodRecord> GetTreeByBirthDetailId(int birthDetailId)
    {
        const string sql = """
            SELECT Id, ParentDashaPeriodId, LevelNumber, SequenceInParent, Lord, StartDate, EndDate, StartDayOffset, EndDayOffset
            FROM dbo.tbl_Chart_DashaPeriods
            WHERE BirthDetailId = @BirthDetailId
            ORDER BY StartDayOffset
            """;
        using var connection = _connectionFactory.CreateOpenConnection();
        var flatRows = connection.Query<DashaPeriodRecord>(sql, new { BirthDetailId = birthDetailId }).ToList();
        return DashaPeriodRecord.BuildTree(flatRows);
    }

    /// <summary>
    /// One row per week (1-4000) via tvf_Chart_LifeWeeks — the active Maha/Antar/Pratyantar lord
    /// for each week of this person's assumed lifespan. Backs the life-in-weeks grid. Lords come
    /// back null for weeks beyond whatever this person's Dasha has been computed for (shouldn't
    /// happen in practice — VimshottariDashaCalculator's default 120-year coverage comfortably
    /// exceeds the 4000-week/~76.9-year window — but the TVF is written to degrade to nulls
    /// rather than error if it ever did).
    /// </summary>
    public IReadOnlyList<LifeWeek> GetLifeWeeks(int birthDetailId)
    {
        const string sql = "SELECT * FROM dbo.tvf_Chart_LifeWeeks(@BirthDetailId) ORDER BY WeekNumber";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<LifeWeek>(sql, new { BirthDetailId = birthDetailId }).ToList();
    }

    /// <summary>Deletes every Dasha period row for one person — used by BirthDetailDeletionService.</summary>
    public void DeleteByBirthDetailId(int birthDetailId)
    {
        const string sql = "DELETE FROM dbo.tbl_Chart_DashaPeriods WHERE BirthDetailId = @BirthDetailId";
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, new { BirthDetailId = birthDetailId });
    }
}
