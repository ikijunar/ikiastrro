using Dapper;
using VedicHoroGen.Core.Models;

namespace VedicHoroGen.Data;

/// <summary>tbl_Dim_LagnaFunctionalNature (migration 031) — read-only reference mirror.</summary>
public class LagnaFunctionalNatureRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    public LagnaFunctionalNatureRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    /// <summary>All 7 classical planets' rows for one Lagna sign (tbl_SignAttributes.Id).</summary>
    public IReadOnlyList<LagnaFunctionalNatureRow> GetForLagna(byte lagnaSignId)
    {
        const string sql = "SELECT * FROM dbo.tbl_Dim_LagnaFunctionalNature WHERE LagnaSignId = @LagnaSignId ORDER BY PlanetId";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<LagnaFunctionalNatureRow>(sql, new { LagnaSignId = lagnaSignId }).ToList();
    }
}
