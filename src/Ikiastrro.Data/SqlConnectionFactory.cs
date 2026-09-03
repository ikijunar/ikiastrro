using Microsoft.Data.SqlClient;

namespace Ikiastrro.Data;

public class SqlConnectionFactory
{
    private const string DefaultDb = "ikiastrro";

    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString) => _connectionString = connectionString;

    /// <summary>
    /// Resolves the connection string, in order:
    /// 1. <paramref name="connectionString"/> if given (Web passes ConnectionStrings:Ikiastrro);
    /// 2. env var <c>IKIASTRRO_CONNECTION</c> (stage/uat/prod);
    /// 3. a Windows-Auth string against <c>localhost</c>, catalog =
    ///    <paramref name="dbNameOverride"/> (CLI <c>--db</c>) ?? env <c>IKIASTRRO_DB</c> ?? <c>ikiastrro</c>.
    /// No environment token ever appears in a schema object name — only the catalog / server
    /// differs per environment (see INFRASTRUCTURE.md).
    /// </summary>
    public static SqlConnectionFactory Create(string? connectionString = null, string? dbNameOverride = null)
    {
        if (!string.IsNullOrWhiteSpace(connectionString))
            return new SqlConnectionFactory(connectionString);

        var env = Environment.GetEnvironmentVariable("IKIASTRRO_CONNECTION");
        if (!string.IsNullOrWhiteSpace(env))
            return new SqlConnectionFactory(env);

        var db = dbNameOverride
                 ?? Environment.GetEnvironmentVariable("IKIASTRRO_DB")
                 ?? DefaultDb;
        return new SqlConnectionFactory(
            $"Server=localhost;Database={db};Integrated Security=True;TrustServerCertificate=True;");
    }

    /// <summary>Back-compat: the historical default (Windows Auth, localhost, <c>ikiastrro</c>).</summary>
    public static SqlConnectionFactory CreateDefault() => Create();

    public SqlConnection CreateOpenConnection()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
