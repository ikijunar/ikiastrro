using Dapper;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

public class ChartResultsRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ChartResultsRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>Inserts the record and returns it with Id populated (needed by callers that write child rows, e.g. D1 key-details).</summary>
    public ChartResult Insert(ChartResult chartResult)
    {
        const string sql = """
            INSERT INTO dbo.tbl_ChartResults
                (BirthDetailId, ChartType, ChartTypeId, CalculationKind, RuleSetId,
                 Ayanamsha, HouseSystem, EngineVersion, VargaMethod, AyanamshaDegrees, SiderealTimeHours, ResultJson, ComputedAt)
            OUTPUT INSERTED.Id
            VALUES
                (@BirthDetailId, @ChartType, @ChartTypeId, @CalculationKind, @RuleSetId,
                 @Ayanamsha, @HouseSystem, @EngineVersion, @VargaMethod, @AyanamshaDegrees, @SiderealTimeHours, @ResultJson, @ComputedAt)
            """;

        using var connection = _connectionFactory.CreateOpenConnection();
        chartResult.Id = connection.ExecuteScalar<int>(sql, chartResult);
        return chartResult;
    }

    public void InsertAll(IEnumerable<ChartResult> chartResults)
    {
        foreach (var chartResult in chartResults)
        {
            Insert(chartResult);
        }
    }

    public IReadOnlyList<ChartResult> GetByBirthDetailId(int birthDetailId)
    {
        const string sql = "SELECT * FROM dbo.tbl_ChartResults WHERE BirthDetailId = @BirthDetailId ORDER BY ChartType";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<ChartResult>(sql, new { BirthDetailId = birthDetailId }).ToList();
    }

    /// <summary>Deletes every chart result (every chart type) for one person — used by BirthDetailDeletionService. Call after the 4 analytical tables are cleared (they FK-reference this table).</summary>
    public void DeleteByBirthDetailId(int birthDetailId)
    {
        const string sql = "DELETE FROM dbo.tbl_ChartResults WHERE BirthDetailId = @BirthDetailId";
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, new { BirthDetailId = birthDetailId });
    }

    /// <summary>
    /// Deletes just one chart type's result(s) for one person — used when recomputing Vimshottari
    /// Dasha (e.g. after a birth-time correction) so the old ChartResults row is replaced rather
    /// than left orphaned once its tbl_Chart_DashaPeriods rows are cleared out from under it.
    /// Unlike DeleteByBirthDetailId, this leaves every other chart type (D1, D9, ...) untouched.
    /// </summary>
    public void DeleteByBirthDetailIdAndChartType(int birthDetailId, string chartType)
    {
        const string sql = "DELETE FROM dbo.tbl_ChartResults WHERE BirthDetailId = @BirthDetailId AND ChartType = @ChartType";
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, new { BirthDetailId = birthDetailId, ChartType = chartType });
    }
}
