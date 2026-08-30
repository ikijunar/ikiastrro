using Dapper;
using VedicHoroGen.Core.Astro;
using VedicHoroGen.Core.Transits;

namespace VedicHoroGen.Data;

/// <summary>
/// tbl_PlanetSignTransitEvents -- one row per actual sign-boundary crossing for Saturn/Jupiter/Rahu
/// (Ketu is always derived from Rahu via vw_KetuSignTransitEvents, never stored here). PlanetId/SignId
/// map straight off the enum values: tbl_Planets.Id = (int)PlanetName + 1 (Sun=1..Ketu=9), and
/// tbl_SignAttributes.Id = (int)ZodiacName + 1 (Aries=1..Pisces=12) -- both tables were seeded in
/// that exact order specifically so no string lookup is needed here.
/// </summary>
public class PlanetSignTransitEventsRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public PlanetSignTransitEventsRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>Row count for one planet -- used to make the backfill idempotent (skip if already populated).</summary>
    public int CountByPlanet(PlanetName planet)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM dbo.tbl_PlanetSignTransitEvents WHERE PlanetId = @PlanetId",
            new { PlanetId = (int)planet + 1 });
    }

    public void InsertAll(IEnumerable<(PlanetTransitEvent Event, bool IsReentry)> events)
    {
        const string sql = """
            INSERT INTO dbo.tbl_PlanetSignTransitEvents (PlanetId, EventDateTimeUtc, SignId, MotionDirection, IsReentry)
            VALUES (@PlanetId, @EventDateTimeUtc, @SignId, @MotionDirection, @IsReentry)
            """;
        using var connection = _connectionFactory.CreateOpenConnection();
        var rows = events.Select(e => new
        {
            PlanetId = (int)e.Event.Planet + 1,
            e.Event.EventDateTimeUtc,
            SignId = (int)e.Event.Sign + 1,
            MotionDirection = e.Event.IsRetrograde ? "Retrograde" : "Direct",
            e.IsReentry
        });
        connection.Execute(sql, rows);
    }
}
