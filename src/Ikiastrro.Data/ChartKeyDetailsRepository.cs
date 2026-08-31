using Dapper;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>tbl_Chart_KeyDetails — shared across every chart type (D1, D9, and any future divisional chart), rows discriminated by ChartResultId/ChartType.</summary>
public class ChartKeyDetailsRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ChartKeyDetailsRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void InsertAll(IEnumerable<ChartKeyDetail> rows)
    {
        const string sql = """
            INSERT INTO dbo.tbl_Chart_KeyDetails
                (ChartResultId, BirthDetailId, ChartType, Planet, Sign, DegreesInSignDisplay, DegreesInSignDecimal,
                 NirayanaLongitudeDegrees, EclipticLatitudeDegrees, SpeedLongitudeDegPerDay,
                 Nakshatra, NakshatraPada, NakshatraLordPlanet,
                 NakshatraId, NakshatraPadaId, NakshatraSubLordPlanet, IsRetrograde,
                 IsCombust, DistanceFromSunDegrees, CombustionOrbUsedDegrees,
                 HouseNumberFromLagna, HouseNumberFromSun, HouseNumberFromMoon,
                 OwnSigns, ExaltationSign, DebilitationSign, MoolatrikonaSign, MoolatrikonaRange,
                 SignLordPlanet, DignityStatus, AspectingPlanets, ComputedAt,
                 PlanetId, SignId, NakshatraLordPlanetId, NakshatraSubLordPlanetId, SignLordPlanetId)
            VALUES
                (@ChartResultId, @BirthDetailId, @ChartType, @Planet, @Sign, @DegreesInSignDisplay, @DegreesInSignDecimal,
                 @NirayanaLongitudeDegrees, @EclipticLatitudeDegrees, @SpeedLongitudeDegPerDay,
                 @Nakshatra, @NakshatraPada, @NakshatraLordPlanet,
                 @NakshatraId, @NakshatraPadaId, @NakshatraSubLordPlanet, @IsRetrograde,
                 @IsCombust, @DistanceFromSunDegrees, @CombustionOrbUsedDegrees,
                 @HouseNumberFromLagna, @HouseNumberFromSun, @HouseNumberFromMoon,
                 @OwnSigns, @ExaltationSign, @DebilitationSign, @MoolatrikonaSign, @MoolatrikonaRange,
                 @SignLordPlanet, @DignityStatus, @AspectingPlanets, @ComputedAt,
                 @PlanetId, @SignId, @NakshatraLordPlanetId, @NakshatraSubLordPlanetId, @SignLordPlanetId)
            """;

        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, rows);
    }

    public IReadOnlyList<ChartKeyDetail> GetByChartResultId(int chartResultId)
    {
        const string sql = "SELECT * FROM dbo.tbl_Chart_KeyDetails WHERE ChartResultId = @ChartResultId ORDER BY Id";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<ChartKeyDetail>(sql, new { ChartResultId = chartResultId }).ToList();
    }

    /// <summary>Every KeyDetails row for one person, all chart types — for the Web workspace's one-shot load.</summary>
    public IReadOnlyList<ChartKeyDetail> GetByBirthDetailId(int birthDetailId)
    {
        const string sql = "SELECT * FROM dbo.tbl_Chart_KeyDetails WHERE BirthDetailId = @BirthDetailId ORDER BY Id";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<ChartKeyDetail>(sql, new { BirthDetailId = birthDetailId }).ToList();
    }

    /// <summary>Deletes every row (every chart type) for one person — used by BirthDetailDeletionService.</summary>
    public void DeleteByBirthDetailId(int birthDetailId)
    {
        const string sql = "DELETE FROM dbo.tbl_Chart_KeyDetails WHERE BirthDetailId = @BirthDetailId";
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, new { BirthDetailId = birthDetailId });
    }

    /// <summary>
    /// Deletes just one ChartResult's rows (both D1 and D9 share this table, discriminated by
    /// ChartResultId) — used by the CLI's `recompute-keydetails` mode to re-derive rows for a
    /// ChartResult that already has KeyDetails but predates a newly-added column (e.g. IsRetrograde/
    /// IsCombust/NakshatraLordPlanet, 2026-08-28), where the UNIQUE (ChartResultId, Planet) index
    /// would otherwise reject a plain re-insert.
    /// </summary>
    public void DeleteByChartResultId(int chartResultId)
    {
        const string sql = "DELETE FROM dbo.tbl_Chart_KeyDetails WHERE ChartResultId = @ChartResultId";
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, new { ChartResultId = chartResultId });
    }
}
