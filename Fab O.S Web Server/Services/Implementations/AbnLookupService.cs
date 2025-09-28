using System.Xml.Linq;
using System.Text;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations;

public class AbnLookupService : IAbnLookupService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AbnLookupService> _logger;
    private readonly string _guid;
    private readonly string _baseUrl;

    public AbnLookupService(HttpClient httpClient, IConfiguration configuration, ILogger<AbnLookupService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _guid = _configuration["AbnLookup:Guid"] ?? "";
        _baseUrl = _configuration["AbnLookup:BaseUrl"] ?? "https://abr.business.gov.au/abrxmlsearch/AbrXmlSearch.asmx/ABRSearchByABN";
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<AbnSearchResult?> SearchByAbnAsync(string abn)
    {
        try
        {
            // Clean the ABN
            abn = abn.Replace(" ", "").Replace("-", "");

            if (!ValidateAbn(abn))
            {
                _logger.LogWarning($"Invalid ABN format: {abn}");
                return null;
            }

            // Use the simpler GET request approach for ABN lookup
            var url = $"https://abr.business.gov.au/abrxmlsearch/AbrXmlSearch.asmx/ABRSearchByABN?searchString={abn}&includeHistoricalDetails=N&authenticationGuid={_guid}";

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var responseXml = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"ABN lookup response received for {abn}");
                return ParseAbnResponse(responseXml);
            }

            _logger.LogError($"ABN Lookup failed with status: {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogError($"Response content: {content}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching ABN");
            return null;
        }
    }

    public async Task<List<AbnSearchResult>> SearchByNameAsync(string name, string? state = null, string? postcode = null)
    {
        var results = new List<AbnSearchResult>();

        try
        {
            // Use URL encoding for special characters
            name = Uri.EscapeDataString(name);

            // Build the query string for name search
            var url = $"https://abr.business.gov.au/abrxmlsearch/AbrXmlSearch.asmx/ABRSearchByNameSimpleProtocol?" +
                     $"name={name}" +
                     $"&postcode={postcode ?? ""}" +
                     $"&legalName=Y&tradingName=Y" +
                     $"&NSW={(state == null || state == "NSW" ? "Y" : "N")}" +
                     $"&SA={(state == null || state == "SA" ? "Y" : "N")}" +
                     $"&ACT={(state == null || state == "ACT" ? "Y" : "N")}" +
                     $"&VIC={(state == null || state == "VIC" ? "Y" : "N")}" +
                     $"&WA={(state == null || state == "WA" ? "Y" : "N")}" +
                     $"&NT={(state == null || state == "NT" ? "Y" : "N")}" +
                     $"&QLD={(state == null || state == "QLD" ? "Y" : "N")}" +
                     $"&TAS={(state == null || state == "TAS" ? "Y" : "N")}" +
                     $"&authenticationGuid={_guid}";

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var responseXml = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Name search response received for '{name}'");
                results = ParseNameSearchResponse(responseXml);
            }
            else
            {
                _logger.LogError($"ABN Name search failed with status: {response.StatusCode}");
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Response content: {content}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching ABN by name");
        }

        return results;
    }

    public bool ValidateAbn(string abn)
    {
        // Remove spaces and hyphens
        abn = abn.Replace(" ", "").Replace("-", "");

        // ABN must be 11 digits
        if (abn.Length != 11 || !abn.All(char.IsDigit))
            return false;

        // Apply the ABN checksum algorithm
        var weights = new[] { 10, 1, 3, 5, 7, 9, 11, 13, 15, 17, 19 };
        var digits = abn.Select(c => int.Parse(c.ToString())).ToArray();

        // Subtract 1 from first digit
        digits[0] -= 1;

        // Calculate weighted sum
        var sum = 0;
        for (int i = 0; i < 11; i++)
        {
            sum += digits[i] * weights[i];
        }

        // Check if divisible by 89
        return sum % 89 == 0;
    }

    private AbnSearchResult? ParseAbnResponse(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);

            // The response is a simple XML document, not SOAP
            var abnResponse = doc.Root;
            if (abnResponse == null) return null;

            // Look for business entity elements
            var businessEntity = abnResponse.Descendants().Where(e =>
                e.Name.LocalName == "businessEntity" ||
                e.Name.LocalName == "businessEntity201408" ||
                e.Name.LocalName == "businessEntity201205").FirstOrDefault();

            if (businessEntity == null)
            {
                // Try to get it directly from root if it's the response element
                if (abnResponse.Name.LocalName.Contains("ABRPayloadSearchResults"))
                {
                    businessEntity = abnResponse.Descendants().FirstOrDefault(e => e.Name.LocalName.StartsWith("businessEntity"));
                }
            }

            if (businessEntity == null)
            {
                _logger.LogWarning($"No business entity found in response");
                return null;
            }

            var result = new AbnSearchResult
            {
                Abn = businessEntity.Descendants().FirstOrDefault(e => e.Name.LocalName == "identifierValue")?.Value ?? "",
                AbnStatus = businessEntity.Descendants().FirstOrDefault(e => e.Name.LocalName == "identifierStatus")?.Value ?? "",
                IsCurrentIndicator = businessEntity.Descendants().FirstOrDefault(e => e.Name.LocalName == "isCurrentIndicator")?.Value == "Y",
                EntityTypeCode = businessEntity.Descendants().FirstOrDefault(e => e.Name.LocalName == "entityTypeCode")?.Value ?? "",
                EntityTypeName = businessEntity.Descendants().FirstOrDefault(e => e.Name.LocalName == "entityDescription")?.Value ?? ""
            };

            // Parse main name using local name matching
            var mainName = businessEntity.Descendants().FirstOrDefault(e => e.Name.LocalName == "mainName");
            if (mainName != null)
            {
                result.OrganisationName = mainName.Descendants().FirstOrDefault(e => e.Name.LocalName == "organisationName")?.Value ?? "";
                result.FamilyName = mainName.Descendants().FirstOrDefault(e => e.Name.LocalName == "familyName")?.Value ?? "";
                result.GivenName = mainName.Descendants().FirstOrDefault(e => e.Name.LocalName == "givenName")?.Value ?? "";
                var effectiveFromStr = mainName.Descendants().FirstOrDefault(e => e.Name.LocalName == "effectiveFrom")?.Value;
                result.EffectiveFrom = ParseDate(effectiveFromStr);
            }

            // Parse trading names
            var tradingNames = businessEntity.Descendants().Where(e => e.Name.LocalName == "mainTradingName");
            foreach (var tn in tradingNames)
            {
                var name = tn.Descendants().FirstOrDefault(e => e.Name.LocalName == "organisationName")?.Value;
                if (!string.IsNullOrEmpty(name))
                    result.TradingNames.Add(name);
            }

            // Parse business address
            var address = businessEntity.Descendants().FirstOrDefault(e => e.Name.LocalName == "mainBusinessPhysicalAddress");
            if (address != null)
            {
                result.State = address.Descendants().FirstOrDefault(e => e.Name.LocalName == "stateCode")?.Value ?? "";
                result.Postcode = address.Descendants().FirstOrDefault(e => e.Name.LocalName == "postcode")?.Value ?? "";
                result.AddressLine1 = address.Descendants().FirstOrDefault(e => e.Name.LocalName == "addressLine1")?.Value ?? "";
                result.AddressLine2 = address.Descendants().FirstOrDefault(e => e.Name.LocalName == "addressLine2")?.Value ?? "";
                result.Suburb = address.Descendants().FirstOrDefault(e => e.Name.LocalName == "suburb")?.Value ?? "";
            }

            // Parse ACN if exists
            var asicNumber = businessEntity.Descendants().FirstOrDefault(e => e.Name.LocalName == "ASICNumber");
            if (asicNumber != null)
            {
                result.Acn = asicNumber.Descendants().FirstOrDefault(e => e.Name.LocalName == "identifierValue")?.Value;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing ABN response");
            return null;
        }
    }

    private List<AbnSearchResult> ParseNameSearchResponse(string xml)
    {
        var results = new List<AbnSearchResult>();

        try
        {
            var doc = XDocument.Parse(xml);

            // The response is a simple XML document
            var searchResultsList = doc.Descendants().Where(e => e.Name.LocalName == "searchResultsList").FirstOrDefault();
            if (searchResultsList == null)
            {
                _logger.LogWarning("No searchResultsList found in name search response");
                return results;
            }

            var searchResults = searchResultsList.Elements().Where(e => e.Name.LocalName == "searchResultsRecord");

            foreach (var record in searchResults)
            {
                var result = new AbnSearchResult();

                // Parse ABN info using local names
                var abnElement = record.Descendants().FirstOrDefault(e => e.Name.LocalName == "ABN");
                if (abnElement != null)
                {
                    result.Abn = abnElement.Descendants().FirstOrDefault(e => e.Name.LocalName == "identifierValue")?.Value ?? "";
                    result.AbnStatus = abnElement.Descendants().FirstOrDefault(e => e.Name.LocalName == "identifierStatus")?.Value ?? "";
                }

                result.IsCurrentIndicator = record.Descendants().FirstOrDefault(e => e.Name.LocalName == "isCurrentIndicator")?.Value == "Y";

                // Parse names
                var mainName = record.Descendants().FirstOrDefault(e => e.Name.LocalName == "mainName");
                if (mainName != null)
                {
                    result.OrganisationName = mainName.Descendants().FirstOrDefault(e => e.Name.LocalName == "organisationName")?.Value ?? "";
                    result.FamilyName = mainName.Descendants().FirstOrDefault(e => e.Name.LocalName == "familyName")?.Value ?? "";
                    result.GivenName = mainName.Descendants().FirstOrDefault(e => e.Name.LocalName == "givenName")?.Value ?? "";
                }

                // Parse trading name if no main name
                var tradingName = record.Descendants().FirstOrDefault(e => e.Name.LocalName == "mainTradingName");
                if (tradingName != null)
                {
                    var tn = tradingName.Descendants().FirstOrDefault(e => e.Name.LocalName == "organisationName")?.Value;
                    if (!string.IsNullOrEmpty(tn))
                        result.TradingNames.Add(tn);
                }

                // Parse business address
                var address = record.Descendants().FirstOrDefault(e => e.Name.LocalName == "mainBusinessPhysicalAddress");
                if (address != null)
                {
                    result.State = address.Descendants().FirstOrDefault(e => e.Name.LocalName == "stateCode")?.Value ?? "";
                    result.Postcode = address.Descendants().FirstOrDefault(e => e.Name.LocalName == "postcode")?.Value ?? "";
                }

                results.Add(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing name search response");
        }

        return results;
    }

    private DateTime? ParseDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr)) return null;
        if (DateTime.TryParse(dateStr, out var date))
            return date;
        return null;
    }
}