using Dapper;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>dbo.tbl_SignAttributes (migration 019) — static rasi reference. Read-only.
/// The snake_case columns type_house_element / type_house_keyattri are SELECT-aliased so
/// Dapper maps them to SignAttributeReference.HouseElement / HouseModality.</summary>
public class SignAttributesRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    public SignAttributesRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public IReadOnlyList<SignAttributeReference> GetAll()
    {
        const string sql = "SELECT *, type_house_element AS HouseElement, type_house_keyattri AS HouseModality " +
                           "FROM dbo.tbl_SignAttributes ORDER BY Id";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<SignAttributeReference>(sql).ToList();
    }
}
