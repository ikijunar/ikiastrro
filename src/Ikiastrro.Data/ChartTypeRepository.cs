using Dapper;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>tbl_Dim_ChartType — the fixed chart-type vocabulary. Six rows today (D1/D2/D6/D9/D10/D11).</summary>
public class ChartTypeRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ChartTypeRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public IReadOnlyList<ChartTypeRow> GetAll()
    {
        const string sql = "SELECT CAST(Id AS INT) AS Id, Code, DisplayName, CAST(DivisionalFactor AS INT) AS DivisionalFactor, Category, CAST(DisplayOrder AS INT) AS DisplayOrder FROM dbo.tbl_Dim_ChartType ORDER BY DisplayOrder";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<ChartTypeRow>(sql).ToList();
    }

    public IReadOnlyDictionary<string, int> CodeToId() =>
        GetAll().ToDictionary(r => r.Code, r => r.Id, StringComparer.OrdinalIgnoreCase);
}
