namespace VedicHoroGen.Core.Geocoding;

public record ResolvedPlace(double Latitude, double Longitude, string IanaTimeZoneId, TimeSpan UtcOffset);

/// <summary>
/// Resolves "City, Country" -> latitude/longitude/UTC-offset for a given birth date.
/// Kept behind an interface so the resolution strategy can be swapped later
/// (e.g. a paid geocoder) without touching callers.
/// </summary>
public interface IPlaceResolver
{
    Task<ResolvedPlace> ResolveAsync(string city, string country, DateOnly onDate);
}
