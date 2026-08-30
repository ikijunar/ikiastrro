namespace Ikiastrro.Core.Geocoding;

/// <summary>
/// Fallback resolver for when automatic geocoding fails or is unavailable (e.g. offline) —
/// the caller (CLI) supplies latitude/longitude/UTC-offset directly.
/// </summary>
public class ManualPlaceResolver : IPlaceResolver
{
    private readonly double _latitude;
    private readonly double _longitude;
    private readonly TimeSpan _utcOffset;
    private readonly string? _ianaTimeZoneId;

    public ManualPlaceResolver(double latitude, double longitude, TimeSpan utcOffset, string? ianaTimeZoneId = null)
    {
        _latitude = latitude;
        _longitude = longitude;
        _utcOffset = utcOffset;
        _ianaTimeZoneId = ianaTimeZoneId;
    }

    public Task<ResolvedPlace> ResolveAsync(string city, string country, DateOnly onDate) =>
        Task.FromResult(new ResolvedPlace(_latitude, _longitude, _ianaTimeZoneId ?? "Manual", _utcOffset));
}
