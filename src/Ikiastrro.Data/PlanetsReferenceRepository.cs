using Dapper;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>dbo.tbl_Planets (migration 021) — static graha reference. Read-only.</summary>
public class PlanetsReferenceRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    public PlanetsReferenceRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public IReadOnlyList<PlanetReference> GetAll()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<PlanetReference>("SELECT * FROM dbo.tbl_Planets ORDER BY Id").ToList();
    }
}
