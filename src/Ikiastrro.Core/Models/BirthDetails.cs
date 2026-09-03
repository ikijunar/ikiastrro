namespace Ikiastrro.Core.Models;

/// <summary>
/// The standard input record for a person's birth details.
/// This table never changes shape as new chart/calculation types are added later —
/// it stores only what was given, not computed output.
/// </summary>
public class BirthDetails
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Calendar date of birth.</summary>
    public DateOnly DateOfBirth { get; set; }

    /// <summary>Time of birth as recorded (e.g. hospital-recorded delivery time).</summary>
    public TimeOnly TimeOfBirth { get; set; }

    public string PlaceCity { get; set; } = string.Empty;

    public string PlaceCountry { get; set; } = string.Empty;

    /// <summary>Derived from Place of Birth via geocoding.</summary>
    public double Latitude { get; set; }

    /// <summary>Derived from Place of Birth via geocoding.</summary>
    public double Longitude { get; set; }

    /// <summary>
    /// UTC offset in force at birth date/time at this location (e.g. "+05:30" for IST).
    /// Resolved from lat/long + date via IANA timezone data, not just current-day offset,
    /// so historical DST/offset rules are respected.
    /// </summary>
    public string UtcOffset { get; set; } = string.Empty;

    /// <summary>IANA timezone id used to resolve UtcOffset (e.g. "Asia/Kolkata"). Kept for auditability.</summary>
    public string? IanaTimeZoneId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The time used for chart calculation. Kept as its own property (rather than having every
    /// calculator/view read TimeOfBirth directly) so a future per-person time adjustment can be
    /// reintroduced without touching every call site again — see BirthMomentFactory, IChartCalculator,
    /// and the chart view components.
    /// </summary>
    public TimeOnly EffectiveTimeOfBirth => TimeOfBirth;
}
