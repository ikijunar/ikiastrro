using Dapper;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>
/// tbl_Chart_MultiGrahaConjunction + tbl_Chart_MultiGrahaConjunctionMember — the multi-graha
/// conjunction (Graha Saṃyoga) group layer, shared across every chart type (rows discriminated by
/// ChartResultId). Mirrors <see cref="ChartConjunctionsRepository"/>. Members cascade-delete with
/// their group (FK <c>ON DELETE CASCADE</c>), so the delete methods only touch the group table.
/// </summary>
public class ChartMultiGrahaConjunctionRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ChartMultiGrahaConjunctionRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Inserts each group (returning its Id) then all its members. <c>ChartResultId</c> must already be
    /// stamped on every group. Skips silently when <paramref name="groups"/> is empty.
    /// </summary>
    public void InsertAll(IEnumerable<ChartMultiGrahaConjunction> groups)
    {
        const string groupSql = """
            INSERT INTO dbo.tbl_Chart_MultiGrahaConjunction
                (ChartResultId, SignId, HouseNumberFromLagna, PlanetCount, MemberKey, LongitudeSpanDegrees)
            OUTPUT INSERTED.Id
            VALUES
                (@ChartResultId, @SignId, @HouseNumberFromLagna, @PlanetCount, @MemberKey, @LongitudeSpanDegrees)
            """;
        const string memberSql = """
            INSERT INTO dbo.tbl_Chart_MultiGrahaConjunctionMember
                (MultiGrahaConjunctionId, PlanetId, DegreesInSign, NirayanaLongitude, VargaLongitude,
                 OrbFromGroupCenterDegrees, DignityStatus, IsRetrograde, IsCombust)
            VALUES
                (@MultiGrahaConjunctionId, @PlanetId, @DegreesInSign, @NirayanaLongitude, @VargaLongitude,
                 @OrbFromGroupCenterDegrees, @DignityStatus, @IsRetrograde, @IsCombust)
            """;

        using var connection = _connectionFactory.CreateOpenConnection();
        foreach (var group in groups)
        {
            group.Id = connection.ExecuteScalar<int>(groupSql, group);
            foreach (var m in group.Members) m.MultiGrahaConjunctionId = group.Id;
            if (group.Members.Count > 0) connection.Execute(memberSql, group.Members);
        }
    }

    /// <summary>
    /// Stamps <c>tbl_Chart_Conjunctions.MultiGrahaConjunctionId</c> for one chart by matching each
    /// pair row's <c>(ChartResultId, SignId)</c> to its group. Run after both are inserted.
    /// </summary>
    public void LinkPairRows(int chartResultId)
    {
        const string sql = """
            UPDATE c
               SET c.MultiGrahaConjunctionId = g.Id
              FROM dbo.tbl_Chart_Conjunctions c
              JOIN dbo.tbl_Chart_MultiGrahaConjunction g
                ON g.ChartResultId = c.ChartResultId AND g.SignId = c.SignId
             WHERE c.ChartResultId = @ChartResultId AND c.MultiGrahaConjunctionId IS NULL
            """;
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, new { ChartResultId = chartResultId });
    }

    /// <summary>Every group (with its members) for one person, all chart types — for the Web workspace's one-shot load.</summary>
    public IReadOnlyList<ChartMultiGrahaConjunction> GetByBirthDetailId(int birthDetailId)
    {
        const string groupSql = """
            SELECT * FROM dbo.tbl_Chart_MultiGrahaConjunction
            WHERE ChartResultId IN (SELECT Id FROM dbo.tbl_ChartResults WHERE BirthDetailId = @BirthDetailId)
            ORDER BY Id
            """;
        const string memberSql = """
            SELECT m.* FROM dbo.tbl_Chart_MultiGrahaConjunctionMember m
            JOIN dbo.tbl_Chart_MultiGrahaConjunction g ON g.Id = m.MultiGrahaConjunctionId
            WHERE g.ChartResultId IN (SELECT Id FROM dbo.tbl_ChartResults WHERE BirthDetailId = @BirthDetailId)
            ORDER BY m.MultiGrahaConjunctionId, m.PlanetId
            """;
        using var connection = _connectionFactory.CreateOpenConnection();
        var groups = connection.Query<ChartMultiGrahaConjunction>(groupSql, new { BirthDetailId = birthDetailId }).ToList();
        var membersByGroup = connection.Query<ChartMultiGrahaConjunctionMember>(memberSql, new { BirthDetailId = birthDetailId })
            .GroupBy(m => m.MultiGrahaConjunctionId)
            .ToDictionary(g => g.Key, g => g.ToList());
        foreach (var group in groups)
            if (membersByGroup.TryGetValue(group.Id, out var members))
                group.Members = members;
        return groups;
    }

    /// <summary>Deletes one ChartResult's groups (members cascade) — used by ChartGenerationService.RecomputeAnalytics.</summary>
    public void DeleteByChartResultId(int chartResultId)
    {
        const string sql = "DELETE FROM dbo.tbl_Chart_MultiGrahaConjunction WHERE ChartResultId = @ChartResultId";
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, new { ChartResultId = chartResultId });
    }

    /// <summary>
    /// Deletes every group (every chart type) for one person — members cascade. Call after
    /// tbl_Chart_Conjunctions is cleared for the person (its rows FK-reference this table).
    /// </summary>
    public void DeleteByBirthDetailId(int birthDetailId)
    {
        const string sql = """
            DELETE FROM dbo.tbl_Chart_MultiGrahaConjunction
            WHERE ChartResultId IN (SELECT Id FROM dbo.tbl_ChartResults WHERE BirthDetailId = @BirthDetailId)
            """;
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, new { BirthDetailId = birthDetailId });
    }
}
