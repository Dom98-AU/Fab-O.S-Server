namespace FabOS.WebServer.Services.Interfaces;

public interface IGooglePlacesService
{
    Task<List<PlaceAutocompletePrediction>> AutocompleteAsync(string input, string sessionToken = null);
    Task<PlaceDetails> GetPlaceDetailsAsync(string placeId, string sessionToken = null);
}

public class PlaceAutocompletePrediction
{
    public string PlaceId { get; set; } = "";
    public string Description { get; set; } = "";
    public string MainText { get; set; } = "";
    public string SecondaryText { get; set; } = "";
}

public class PlaceDetails
{
    public string PlaceId { get; set; } = "";
    public string FormattedAddress { get; set; } = "";
    public string StreetNumber { get; set; } = "";
    public string Route { get; set; } = "";
    public string Locality { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Country { get; set; } = "";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}