using Dapper;
using VedicHoroGen.Core.Models;

namespace VedicHoroGen.Data;

/// <summary>vw_Chart_HouseNakshatraSpan (migration 034) — read-only.</summary>
public class HouseNakshatraSpanRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    public HouseNakshatraSpanRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public IReadOnlyList<HouseNakshatraSpanRow> GetByChartResultId(int chartResultId)
    {
        const string sql = "SELECT * FROM dbo.vw_Chart_HouseNakshatraSpan WHERE ChartResultId = @ChartResultId " +
                           "ORDER BY HouseNumber, PadaStartDegree";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<HouseNakshatraSpanRow>(sql, new { ChartResultId = chartResultId }).ToList();
    }
}
