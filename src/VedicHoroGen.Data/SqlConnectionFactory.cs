using Microsoft.Data.SqlClient;

namespace VedicHoroGen.Data;

public class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>Default v1 connection: Windows Auth against the local default instance (RAMMYPS/localhost).</summary>
    public static SqlConnectionFactory CreateDefault() =>
        new("Server=localhost;Database=vedic_horo_gen;Integrated Security=True;TrustServerCertificate=True;");

    public SqlConnection CreateOpenConnection()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
