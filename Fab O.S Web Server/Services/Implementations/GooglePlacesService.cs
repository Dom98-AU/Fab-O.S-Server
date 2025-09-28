using System.Text.Json;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations;

public class GooglePlacesService : IGooglePlacesService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GooglePlacesService> _logger;
    private readonly string _apiKey;

    public GooglePlacesService(HttpClient httpClient, IConfiguration configuration, ILogger<GooglePlacesService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _apiKey = _configuration["GoogleMaps:ApiKey"] ?? "";
    }

    public async Task<List<PlaceAutocompletePrediction>> AutocompleteAsync(string input, string sessionToken = null)
    {
        var predictions = new List<PlaceAutocompletePrediction>();

        try
        {
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(_apiKey))
            {
                return predictions;
            }

            var url = $"https://maps.googleapis.com/maps/api/place/autocomplete/json?" +
                     $"input={Uri.EscapeDataString(input)}" +
                     $"&key={_apiKey}" +
                     $"&components=country:au" +
                     $"&types=address";

            if (!string.IsNullOrEmpty(sessionToken))
            {
                url += $"&sessiontoken={sessionToken}";
            }

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("status", out var status) &&
                    status.GetString() == "OK" &&
                    doc.RootElement.TryGetProperty("predictions", out var predictionsElement))
                {
                    foreach (var prediction in predictionsElement.EnumerateArray())
                    {
                        predictions.Add(new PlaceAutocompletePrediction
                        {
                            PlaceId = prediction.GetProperty("place_id").GetString() ?? "",
                            Description = prediction.GetProperty("description").GetString() ?? "",
                            MainText = prediction.TryGetProperty("structured_formatting", out var formatting) &&
                                      formatting.TryGetProperty("main_text", out var mainText)
                                      ? mainText.GetString() ?? "" : "",
                            SecondaryText = prediction.TryGetProperty("structured_formatting", out var formatting2) &&
                                          formatting2.TryGetProperty("secondary_text", out var secondaryText)
                                          ? secondaryText.GetString() ?? "" : ""
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Google Places Autocomplete API");
        }

        return predictions;
    }

    public async Task<PlaceDetails> GetPlaceDetailsAsync(string placeId, string sessionToken = null)
    {
        var details = new PlaceDetails { PlaceId = placeId };

        try
        {
            if (string.IsNullOrWhiteSpace(placeId) || string.IsNullOrWhiteSpace(_apiKey))
            {
                return details;
            }

            var url = $"https://maps.googleapis.com/maps/api/place/details/json?" +
                     $"place_id={placeId}" +
                     $"&key={_apiKey}" +
                     $"&fields=formatted_address,address_components,geometry";

            if (!string.IsNullOrEmpty(sessionToken))
            {
                url += $"&sessiontoken={sessionToken}";
            }

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("status", out var status) &&
                    status.GetString() == "OK" &&
                    doc.RootElement.TryGetProperty("result", out var result))
                {
                    details.FormattedAddress = result.TryGetProperty("formatted_address", out var formatted)
                        ? formatted.GetString() ?? "" : "";

                    if (result.TryGetProperty("geometry", out var geometry) &&
                        geometry.TryGetProperty("location", out var location))
                    {
                        if (location.TryGetProperty("lat", out var lat))
                            details.Latitude = lat.GetDouble();
                        if (location.TryGetProperty("lng", out var lng))
                            details.Longitude = lng.GetDouble();
                    }

                    if (result.TryGetProperty("address_components", out var components))
                    {
                        foreach (var component in components.EnumerateArray())
                        {
                            if (!component.TryGetProperty("types", out var types))
                                continue;

                            var typesList = types.EnumerateArray().Select(t => t.GetString()).ToList();
                            var value = component.TryGetProperty("long_name", out var longName)
                                ? longName.GetString() ?? "" : "";
                            var shortValue = component.TryGetProperty("short_name", out var shortName)
                                ? shortName.GetString() ?? "" : "";

                            if (typesList.Contains("street_number"))
                                details.StreetNumber = value;
                            else if (typesList.Contains("route"))
                                details.Route = value;
                            else if (typesList.Contains("locality"))
                                details.Locality = value;
                            else if (typesList.Contains("administrative_area_level_1"))
                                details.State = shortValue;
                            else if (typesList.Contains("postal_code"))
                                details.PostalCode = value;
                            else if (typesList.Contains("country"))
                                details.Country = value;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Google Places Details API");
        }

        return details;
    }
}