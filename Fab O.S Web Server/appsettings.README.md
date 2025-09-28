# Configuration Setup

## Important: Secrets Management

This application uses sensitive configuration values that should NOT be committed to version control.

### Local Development Setup

1. Copy your secrets from the team's secure location into `appsettings.Development.json`
2. This file is gitignored and will not be pushed to GitHub
3. The application will automatically use `appsettings.Development.json` in development mode

### Configuration Files

- `appsettings.json` - Template file with placeholders (safe to commit)
- `appsettings.Development.json` - Local development secrets (gitignored)
- `appsettings.Production.json` - Production secrets (gitignored)

### Required Configuration Values

The following values need to be set in your local `appsettings.Development.json`:

- **ConnectionStrings:DefaultConnection** - SQL Server connection string
- **AzureDocumentIntelligence:Endpoint** - Azure Form Recognizer endpoint
- **AzureDocumentIntelligence:ApiKey** - Azure Form Recognizer API key
- **JwtSettings:SecretKey** - JWT signing key
- **GoogleMaps:ApiKey** - Google Maps API key
- **AbnLookup:ApiKey** - ABN Lookup API key
- **SharePoint** settings - If using SharePoint integration

### Security Notes

- Never commit real API keys or passwords to Git
- Use environment variables or Azure Key Vault in production
- Rotate keys regularly
- Keep `appsettings.Development.json` secure on your local machine