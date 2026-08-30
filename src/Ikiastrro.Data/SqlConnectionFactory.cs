using Microsoft.Data.SqlClient;

namespace Ikiastrro.Data;

public class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>Default connection: Windows Auth against the local default instance (RAMMYPS/localhost).
    /// Database <c>ikiastrro_betav1</c> (renamed from <c>vedic_horo_gen</c> 2026-08-30 — run
    /// <c>db/00_rename_db_to_ikiastrro_betav1.sql</c> once, or <c>db/ikiastrro_betav1.sql</c> for a fresh build).</summary>
    public static SqlConnectionFactory CreateDefault() =>
        new("Server=localhost;Database=ikiastrro_betav1;Integrated Security=True;TrustServerCertificate=True;");

    public SqlConnection CreateOpenConnection()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
