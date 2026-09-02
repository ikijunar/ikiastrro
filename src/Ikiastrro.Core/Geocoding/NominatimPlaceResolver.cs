using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GeoTimeZone;
using TimeZoneConverter;

namespace Ikiastrro.Core.Geocoding;

/// <summary>
/// Resolves place name -> lat/long via OpenStreetMap's free Nominatim geocoder (no API key required),
/// then resolves lat/long + birth date -> IANA timezone -> UTC offset entirely offline via
/// GeoTimeZone + TimeZoneConverter (respects historical DST/offset rules, not just today's offset).
///
/// Replaces the originally-planned VedAstroPlaceResolver: VedAstro's GeoLocation.FromName chains
/// through 4 providers (its own hosted API -> Azure Maps -> Google -> local file) and every provider
/// swallows its own failures, silently returning (0,0) instead of throwing when none resolve —
/// confirmed unreliable in testing (see ikiastrro.md). Nominatim is a single, well-known,
/// directly-testable free service with no hidden fallback chain.
///
/// Usage policy: max ~1 request/sec, requires a descriptive User-Agent (set below).
/// https://operations.osmfoundation.org/policies/nominatim/
/// </summary>
public class NominatimPlaceResolver : IPlaceResolver
{
    private static readonly HttpClient HttpClient = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { BaseAddress = new Uri("https://nominatim.openstreetmap.org/") };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ikiastrro/1.0 (local development tool)");
        return client;
    }

    public async Task<ResolvedPlace> ResolveAsync(string city, string country, DateOnly onDate)
    {
        var query = $"{city}, {country}";
        var requestUri = $"search?q={Uri.EscapeDataString(query)}&format=json&limit=1";

        List<NominatimResult>? results;
        try
        {
            results = await HttpClient.GetFromJsonAsync<List<NominatimResult>>(requestUri);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Could not reach the geocoding service to resolve \"{query}\". " +
                "Enter latitude/longitude/UTC-offset manually instead.", ex);
        }

        if (results is null || results.Count == 0)
        {
            throw new InvalidOperationException(
                $"No location found for \"{query}\". Check spelling, or enter latitude/longitude/UTC-offset manually.");
        }

        var latitude = double.Parse(results[0].Lat, System.Globalization.CultureInfo.InvariantCulture);
        var longitude = double.Parse(results[0].Lon, System.Globalization.CultureInfo.InvariantCulture);

        var ianaTimeZoneId = TimeZoneLookup.GetTimeZone(latitude, longitude).Result;
        var timeZoneInfo = TZConvert.GetTimeZoneInfo(ianaTimeZoneId);

        // Use noon on the birth date to avoid ambiguous/nonexistent-time issues right at a DST transition instant.
        var referenceDate = onDate.ToDateTime(new TimeOnly(12, 0));
        var utcOffset = timeZoneInfo.GetUtcOffset(referenceDate);

        return new ResolvedPlace(latitude, longitude, ianaTimeZoneId, utcOffset);
    }

    private class NominatimResult
    {
        [JsonPropertyName("lat")]
        public string Lat { get; set; } = string.Empty;

        [JsonPropertyName("lon")]
        public string Lon { get; set; } = string.Empty;

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;
    }
}
