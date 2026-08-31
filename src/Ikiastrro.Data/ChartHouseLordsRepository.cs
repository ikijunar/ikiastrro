using Dapper;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>tbl_Chart_HouseLords — shared across every chart type (D1, D9, and any future divisional chart), rows discriminated by ChartResultId/ChartType.</summary>
public class ChartHouseLordsRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ChartHouseLordsRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void InsertAll(IEnumerable<ChartHouseLord> rows)
    {
        const string sql = """
            INSERT INTO dbo.tbl_Chart_HouseLords
                (ChartResultId, BirthDetailId, ChartType, HouseNumber, HouseSign, LordPlanet,
                 LordPlacedInHouseFromLagna, LordPlacedInHouseFromSun, LordPlacedInHouseFromMoon,
                 LordPlacedInSign, LordDignityStatus, ComputedAt,
                 HouseSignId, LordPlanetId, LordPlacedInSignId)
            VALUES
                (@ChartResultId, @BirthDetailId, @ChartType, @HouseNumber, @HouseSign, @LordPlanet,
                 @LordPlacedInHouseFromLagna, @LordPlacedInHouseFromSun, @LordPlacedInHouseFromMoon,
                 @LordPlacedInSign, @LordDignityStatus, @ComputedAt,
                 @HouseSignId, @LordPlanetId, @LordPlacedInSignId)
            """;

        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, rows);
    }

    public IReadOnlyList<ChartHouseLord> GetByChartResultId(int chartResultId)
    {
        const string sql = "SELECT * FROM dbo.tbl_Chart_HouseLords WHERE ChartResultId = @ChartResultId ORDER BY HouseNumber";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<ChartHouseLord>(sql, new { ChartResultId = chartResultId }).ToList();
    }

    /// <summary>Every HouseLord row for one person, all chart types — for the Web workspace's one-shot load.</summary>
    public IReadOnlyList<ChartHouseLord> GetByBirthDetailId(int birthDetailId)
    {
        const string sql = "SELECT * FROM dbo.tbl_Chart_HouseLords WHERE BirthDetailId = @BirthDetailId ORDER BY Id";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<ChartHouseLord>(sql, new { BirthDetailId = birthDetailId }).ToList();
    }

    /// <summary>Deletes every row (every chart type) for one person — used by BirthDetailDeletionService.</summary>
    public void DeleteByBirthDetailId(int birthDetailId)
    {
        const string sql = "DELETE FROM dbo.tbl_Chart_HouseLords WHERE BirthDetailId = @BirthDetailId";
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, new { BirthDetailId = birthDetailId });
    }

    /// <summary>Deletes just one ChartResult's rows (chart types share this table, discriminated by ChartResultId) — used by ChartGenerationService.RecomputeAnalytics to re-derive rows for a ChartResult that already has them.</summary>
    public void DeleteByChartResultId(int chartResultId)
    {
        const string sql = "DELETE FROM dbo.tbl_Chart_HouseLords WHERE ChartResultId = @ChartResultId";
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, new { ChartResultId = chartResultId });
    }
}
