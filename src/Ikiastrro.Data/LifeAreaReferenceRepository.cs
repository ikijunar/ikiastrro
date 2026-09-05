using Dapper;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>dbo.tbl_Dim_LifeArea (migration 30) — the 20 PVR Table 11 life-area reference. Read-only.</summary>
public class LifeAreaReferenceRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    public LifeAreaReferenceRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public IReadOnlyList<LifeAreaRow> GetAll()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<LifeAreaRow>(
            "SELECT Id, AreaCode, AreaName, Description, PlaneOfExistence, WorkspaceGroupCode, SortOrder " +
            "FROM dbo.tbl_Dim_LifeArea ORDER BY SortOrder").ToList();
    }
}
