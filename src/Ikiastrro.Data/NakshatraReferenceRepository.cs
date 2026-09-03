using Dapper;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>dbo.tbl_Nakshatras + tbl_NakshatraPadas (migration 022) + tbl_NakshatraSubLords
/// (migration 033) — static nakshatra reference. Read-only.</summary>
public class NakshatraReferenceRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    public NakshatraReferenceRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public IReadOnlyList<NakshatraReference> GetAll()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<NakshatraReference>("SELECT * FROM dbo.tbl_Nakshatras ORDER BY Id").ToList();
    }

    public IReadOnlyList<NakshatraPadaReference> GetPadas()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<NakshatraPadaReference>(
            "SELECT * FROM dbo.tbl_NakshatraPadas ORDER BY NakshatraId, PadaNumber").ToList();
    }

    public IReadOnlyList<NakshatraSubLordReference> GetSubLords()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<NakshatraSubLordReference>(
            "SELECT * FROM dbo.tbl_NakshatraSubLords ORDER BY NakshatraId, SubSequenceNumber").ToList();
    }
}
