using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Services.Contracts;
using Services.Models.VietMap;
using System.Text.Json;

namespace Services.Implementations
{
    public class VietMapService : IVietMapService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<VietMapService> _logger;

        public VietMapService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<VietMapService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["VietMap:ApiKey"] ?? throw new InvalidOperationException("VietMap API key not configured");
            _logger = logger;
        }
        public async Task<RouteResult?> GetRouteAsync(
            double originLat,
            double originLng,
            double destLat,
            double destLng,
            string vehicle = "car")
        {
            try
            {
                var baseUrl = "https://maps.vietmap.vn/api/route/v3";
                var queryParams = new List<KeyValuePair<string, string>>
                {
                    new("apikey", _apiKey),
                    new("points_encoded", "true"),
                    new("vehicle", vehicle),
                    new("point", $"{originLat},{originLng}"),
                    new("point", $"{destLat},{destLng}")
                };
                var queryString = string.Join("&", queryParams.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
                var url = $"{baseUrl}?{queryString}";

                _logger.LogDebug("Calling VietMap Route API: {Url}", url.Replace(_apiKey, "***"));

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<VietMapResponse>(json);

                if (result?.Code != "OK" || result.Paths == null || !result.Paths.Any())
                {
                    _logger.LogWarning("VietMap API returned error: {Message}", result?.Messages?.FirstOrDefault());
                    return null;
                }

                var path = result.Paths[0];
                return new RouteResult
                {
                    Distance = path.Distance / 1000.0, // Convert to km
                    Duration = path.Time / 1000.0, // Convert to seconds
                    DurationMinutes = path.Time / (1000.0 * 60.0)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling VietMap API");
                return null;
            }
        }
        public async Task<double?> CalculateDistanceAsync(double originLat, double originLng, double destLat, double destLng)
        {
            try
            {
                var routeResult = await GetRouteAsync(originLat, originLng, destLat, destLng, "car");

                if (routeResult == null)
                {
                    _logger.LogWarning("Failed to get route from VietMap API for distance calculation");
                    return null;
                }

                _logger.LogDebug("Distance calculated: {Distance} km", routeResult.Distance);
                return routeResult.Distance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating distance using VietMap API");
                return null;
            }
        }
    }
}
