using Dapper;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

public class BirthDetailsRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public BirthDetailsRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>Inserts the record and returns it with Id populated.</summary>
    public BirthDetails Insert(BirthDetails birthDetails)
    {
        const string sql = """
            INSERT INTO dbo.tbl_BirthDetails
                (Name, DateOfBirth, TimeOfBirth,
                 PlaceCity, PlaceCountry, Latitude, Longitude, UtcOffset, IanaTimeZoneId, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES
                (@Name, @DateOfBirth, @TimeOfBirth,
                 @PlaceCity, @PlaceCountry, @Latitude, @Longitude, @UtcOffset, @IanaTimeZoneId, @CreatedAt)
            """;

        using var connection = _connectionFactory.CreateOpenConnection();
        var newId = connection.ExecuteScalar<int>(sql, new
        {
            birthDetails.Name,
            DateOfBirth = birthDetails.DateOfBirth.ToDateTime(TimeOnly.MinValue),
            TimeOfBirth = birthDetails.TimeOfBirth.ToTimeSpan(),
            birthDetails.PlaceCity,
            birthDetails.PlaceCountry,
            birthDetails.Latitude,
            birthDetails.Longitude,
            birthDetails.UtcOffset,
            birthDetails.IanaTimeZoneId,
            birthDetails.CreatedAt
        });

        birthDetails.Id = newId;
        return birthDetails;
    }

    public BirthDetails? GetById(int id)
    {
        const string sql = "SELECT * FROM dbo.tbl_BirthDetails WHERE Id = @Id";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.QuerySingleOrDefault<BirthDetailsRow>(sql, new { Id = id })?.ToModel();
    }

    /// <summary>All BirthDetails rows, alphabetical by Name — backs the "Saved Charts" list page.</summary>
    public IReadOnlyList<BirthDetails> GetAll()
    {
        const string sql = "SELECT * FROM dbo.tbl_BirthDetails ORDER BY Name";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<BirthDetailsRow>(sql).Select(row => row.ToModel()).ToList();
    }

    /// <summary>
    /// Up to <paramref name="limit"/> names starting with <paramref name="prefix"/> (case-insensitive),
    /// alphabetical — backs the Name field's autocomplete on the entry form. Empty/whitespace prefix
    /// returns nothing (avoids listing everyone on an empty field).
    /// </summary>
    public IReadOnlyList<BirthDetails> SearchByNamePrefix(string prefix, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return Array.Empty<BirthDetails>();

        const string sql = """
            SELECT TOP (@Limit) * FROM dbo.tbl_BirthDetails
            WHERE Name LIKE @Prefix + '%'
            ORDER BY Name
            """;
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<BirthDetailsRow>(sql, new { Prefix = prefix, Limit = limit })
            .Select(row => row.ToModel()).ToList();
    }

    /// <summary>
    /// True if a BirthDetails row already exists with this Name (case-insensitive, matches the
    /// default column collation). Callers should check this before Insert to surface a friendly
    /// "duplicate name" message — the UX_BirthDetails_Name unique index on the table is the
    /// DB-level backstop for the same rule, in case of a race between the check and the insert.
    /// </summary>
    public bool ExistsByName(string name)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.tbl_BirthDetails WHERE Name = @Name";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.ExecuteScalar<int>(sql, new { Name = name }) > 0;
    }

    /// <summary>
    /// Deletes just this one row. Call last, via BirthDetailDeletionService — every chart result and
    /// analytical row derived from this person must be deleted first (both FK-reference this table).
    /// </summary>
    public void Delete(int id)
    {
        const string sql = "DELETE FROM dbo.tbl_BirthDetails WHERE Id = @Id";
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, new { Id = id });
    }

    /// <summary>Dapper needs concrete TimeOnly/DateOnly mapping help — this row shape bridges that.</summary>
    private class BirthDetailsRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public TimeSpan TimeOfBirth { get; set; }
        public string PlaceCity { get; set; } = string.Empty;
        public string PlaceCountry { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string UtcOffset { get; set; } = string.Empty;
        public string? IanaTimeZoneId { get; set; }
        public DateTime CreatedAt { get; set; }

        public BirthDetails ToModel() => new()
        {
            Id = Id,
            Name = Name,
            DateOfBirth = DateOnly.FromDateTime(DateOfBirth),
            TimeOfBirth = TimeOnly.FromTimeSpan(TimeOfBirth),
            PlaceCity = PlaceCity,
            PlaceCountry = PlaceCountry,
            Latitude = Latitude,
            Longitude = Longitude,
            UtcOffset = UtcOffset,
            IanaTimeZoneId = IanaTimeZoneId,
            CreatedAt = CreatedAt
        };
    }
}
