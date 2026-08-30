using Dapper;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>tvf_Chart_SadeSatiPeriods (migration 023) — read-only.</summary>
public class SadeSatiRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    public SadeSatiRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public IReadOnlyList<SadeSatiPeriod> GetByBirthDetailId(int birthDetailId)
    {
        const string sql = "SELECT PeriodType, SortOrder, StartDateTimeUtc, EndDateTimeUtc, SaturnSign " +
                           "FROM dbo.tvf_Chart_SadeSatiPeriods(@BirthDetailId) ORDER BY SortOrder, StartDateTimeUtc";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<SadeSatiPeriod>(sql, new { BirthDetailId = birthDetailId }).ToList();
    }
}
