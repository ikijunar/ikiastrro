using Dapper;
using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Transits;

namespace Ikiastrro.Data;

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

    /// <summary>Sidereal sign of a slow planet as of a date, with the entry date and the next
    /// crossing after it. Ketu (PlanetName.Ketu → PlanetId 9) is resolved inside tvf_PlanetSignAtDate
    /// from Rahu's events; the "next change" query applies the same 8↔9 remap. Returns null if no
    /// crossing was recorded on or before <paramref name="asOfUtc"/> (person predates 1930, or the
    /// transit table is not backfilled).</summary>
    public PlanetTransitSnapshot? GetSnapshot(PlanetName planet, DateTime asOfUtc)
    {
        var planetId = (int)planet + 1;                    // Sun=1 … Ketu=9
        var eventsPlanetId = planetId == 9 ? 8 : planetId; // Ketu's rows live under Rahu
        using var connection = _connectionFactory.CreateOpenConnection();

        var current = connection.QuerySingleOrDefault<CurrentRow>(
            "SELECT TOP (1) CAST(SignId AS tinyint) AS SignId, EventDateTimeUtc, MotionDirection FROM dbo.tvf_PlanetSignAtDate(@PlanetId, @AsOf)",
            new { PlanetId = planetId, AsOf = asOfUtc });
        if (current is null) return null;

        var next = connection.ExecuteScalar<DateTime?>(
            "SELECT MIN(EventDateTimeUtc) FROM dbo.tbl_PlanetSignTransitEvents " +
            "WHERE PlanetId = @P AND EventDateTimeUtc > @AsOf",
            new { P = eventsPlanetId, AsOf = asOfUtc });

        return new PlanetTransitSnapshot(planet, current.SignId, current.EventDateTimeUtc,
            current.MotionDirection, next);
    }

    private sealed record CurrentRow(byte SignId, DateTime EventDateTimeUtc, string MotionDirection);

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
