namespace FabOS.WebServer.Services.Interfaces;

public interface IAbnLookupService
{
    Task<AbnSearchResult?> SearchByAbnAsync(string abn);
    Task<List<AbnSearchResult>> SearchByNameAsync(string name, string? state = null, string? postcode = null);
    bool ValidateAbn(string abn);
}

public class AbnSearchResult
{
    public string Abn { get; set; } = "";
    public string AbnStatus { get; set; } = "";
    public bool IsCurrentIndicator { get; set; }
    public string EntityTypeCode { get; set; } = "";
    public string EntityTypeName { get; set; } = "";

    // Business Names
    public string OrganisationName { get; set; } = "";
    public string FamilyName { get; set; } = "";
    public string GivenName { get; set; } = "";
    public List<string> TradingNames { get; set; } = new();

    // Address
    public string State { get; set; } = "";
    public string Postcode { get; set; } = "";
    public string AddressLine1 { get; set; } = "";
    public string AddressLine2 { get; set; } = "";
    public string Suburb { get; set; } = "";

    // Dates
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    // ACN if company
    public string? Acn { get; set; }

    // Computed display name
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(OrganisationName))
                return OrganisationName;
            if (!string.IsNullOrEmpty(FamilyName) || !string.IsNullOrEmpty(GivenName))
                return $"{GivenName} {FamilyName}".Trim();
            return TradingNames.FirstOrDefault() ?? "";
        }
    }

    public string FormattedAddress
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(AddressLine1)) parts.Add(AddressLine1);
            if (!string.IsNullOrEmpty(AddressLine2)) parts.Add(AddressLine2);
            if (!string.IsNullOrEmpty(Suburb)) parts.Add(Suburb);
            if (!string.IsNullOrEmpty(State)) parts.Add(State);
            if (!string.IsNullOrEmpty(Postcode)) parts.Add(Postcode);
            return string.Join(", ", parts);
        }
    }
}