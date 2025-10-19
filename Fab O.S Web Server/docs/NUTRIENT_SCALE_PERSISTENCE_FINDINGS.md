# Nutrient Web SDK - Measurement Scale Persistence Investigation

**Date**: October 18, 2025
**Component**: Nutrient POC (Proof of Concept)
**Issue**: Measurement calibration scale does not persist through save/reload cycles
**Status**: ✅ SOLUTION FOUND AND IMPLEMENTED - Scales persist automatically when using measurementValueConfiguration callback

---

## Executive Summary

**BREAKTHROUGH**: Investigation into Nutrient Web SDK (formerly PSPDFKit) version 2024.8.2 revealed that **measurement scale configuration IS automatically persisted in the PDF document** and can be retrieved using the `measurementValueConfiguration` callback during PDF load.

**Key Finding**: According to official Nutrient documentation: *"The configuration for scale and precision is stored in a document, and it persists when you close and reopen the document on any device."*

**Root Cause**: Our initial implementation was missing the `measurementValueConfiguration` callback, which receives document scales from the PDF and ensures they persist across sessions.

**Solution**: Add the `measurementValueConfiguration: (documentScales) => documentScales` callback to PSPDFKit.load() configuration to enable automatic scale persistence.

---

## Timeline

### October 18, 2025 - Initial Investigation

**User Request**: Add save/close/reload functionality to test if measurement calibration (e.g., 1:50 scale) persists when closing and reopening PDFs.

**Implementation Completed**:
- Added "Save & Close PDF" button
- Added "Reload with Saved Data" button
- Implemented `exportAndClose()` JavaScript function
- Implemented `loadPdfWithData()` JavaScript function
- Added Instant JSON storage in C# component

### October 18, 2025 - Problem Discovery

**Test Results**: User reported "Save didn't work" when testing scale persistence.

**Initial Analysis** (Incorrect): Focused on annotation persistence rather than scale configuration persistence.

**User Clarification**: "But we should be foxued on the scale set not the annotation data, Please review this?"

**Correction**: Redirected focus from individual measurement annotations to global scale calibration configuration.

### October 18, 2025 - Documentation Review (Initial)

**Sources Reviewed**:
1. Nutrient Web SDK API Documentation (exportInstantJSON)
2. Nutrient guides on opening documents
3. Nutrient ASP.NET integration guide
4. Nutrient GitHub example repository
5. Web search for 2024 product updates

**Key Discovery**: Q2 2024 update mentioned "persisting page settings" but not specifically measurement scale configuration.

### October 18, 2025 - BREAKTHROUGH DISCOVERY

**User Contribution**: User found official Nutrient documentation page "Configure measurements in a PDF using JavaScript"

**Critical Quote from Documentation**:
> "The configuration for scale and precision is stored in a document, and it persists when you close and reopen the document on any device."

**Additional Documentation Findings**:
- Built-in toolbar UI for setting scales exists (we don't need custom buttons!)
- Scales configured via toolbar are automatically stored in the PDF
- The `measurementValueConfiguration` callback is the key to persistence
- Callback receives `documentScales` parameter containing scales stored in the PDF

**Root Cause Identified**:
Our JavaScript configuration was missing the `measurementValueConfiguration` callback. Without this callback, Nutrient cannot retrieve and restore document scales during PDF load.

**Solution Implemented**:
Added `measurementValueConfiguration: (documentScales) => documentScales` callback to both `loadPdf()` and `loadPdfWithData()` functions in `/wwwroot/js/nutrient-poc.js`

---

## Technical Findings

### What Instant JSON Exports

According to official Nutrient API documentation:

> "Instant JSON can be used to instantiate a viewer with a diff that is applied to the raw PDF. This format can be used to **store annotation and form field value changes** on your server..."

**Includes**:
- ✅ Annotations (following Instant Annotation JSON format specification)
- ✅ Form field values (following Instant Form Field Value JSON format specification)
- ✅ Individual measurement annotations (with embedded scale data per annotation)

**Does NOT Include**:
- ❌ Global measurement scale configuration
- ❌ Viewer instance-level settings
- ❌ Measurement calibration applied via `setMeasurementValueConfiguration()`

### How Measurement Scales Work

**Instance-Level Configuration**:
When you call `instance.setMeasurementValueConfiguration(config)`, it sets a configuration on the **PSPDFKit instance** that affects all new measurements created afterward.

```javascript
const config = {
    scale: [{
        unitFrom: 'in',
        unitTo: 'ft',
        factor: 50
    }],
    precision: 'FOUR_DP'
};
await instance.setMeasurementValueConfiguration(config);
```

**Problem**: This configuration is ephemeral - it exists only during the lifetime of that particular instance and is lost when the instance is unloaded.

### Measurement Scale in Annotations

Individual measurement annotations DO contain scale data:

```javascript
// From console logs of created measurement
{
    measurementScale: {
        scale: [{
            unitFrom: 'in',
            unitTo: 'ft',
            factor: 50
        }],
        precision: 'FOUR_DP'
    }
}
```

However, this is **per-annotation data**, not a global setting that applies to new measurements.

---

## Code Analysis

### Current Implementation (Incomplete)

**File**: `/wwwroot/js/nutrient-poc.js`

**setMeasurementScale function** (Lines 256-296):
```javascript
setMeasurementScale: async function(scale, unitFrom, unitTo) {
    const config = {
        scale: [{
            unitFrom: unitFrom,
            unitTo: unitTo,
            factor: scale
        }],
        precision: 'FOUR_DP'
    };

    if (typeof this.instance.setMeasurementValueConfiguration === 'function') {
        await this.instance.setMeasurementValueConfiguration(config);
        console.log('[Nutrient POC] ✓ Measurement scale set');
        return true;
    }
}
```

**Issue**: Configuration is set but NOT stored for later retrieval.

**exportAndClose function** (Lines 302-332):
```javascript
exportAndClose: async function() {
    const instantJSON = await this.instance.exportInstantJSON();
    await PSPDFKit.unload(this.instance);
    this.instance = null;
    return JSON.stringify(instantJSON);
}
```

**Issue**: Only exports Instant JSON, which doesn't include measurement scale configuration.

**loadPdfWithData function** (Lines 340-424):
```javascript
loadPdfWithData: async function(documentUrl, containerId, instantJsonString) {
    const instantJSON = JSON.parse(instantJsonString);
    const configuration = {
        container: '#' + containerId,
        document: documentUrl,
        instantJSON: instantJSON,
        // ... other config
    };
    this.instance = await PSPDFKit.load(configuration);
}
```

**Issue**: Only restores Instant JSON, measurement scale is not reapplied.

---

## Solution Design

### ~~Approach: Manual Scale Configuration Persistence~~ (DEPRECATED)

~~Since Nutrient Web SDK does not automatically persist measurement scale configuration, we must implement manual tracking and restoration.~~

**UPDATE**: This approach was based on incorrect assumptions. Nutrient DOES automatically persist scales in the PDF document. The solution is much simpler - just add the `measurementValueConfiguration` callback.

### ACTUAL Solution: Use measurementValueConfiguration Callback

**Simple Implementation** - Add this callback to PSPDFKit.load() configuration:

```javascript
measurementValueConfiguration: (documentScales) => {
    console.log('[Nutrient POC] Document scales loaded:', documentScales);
    return documentScales;  // Return scales to preserve them
}
```

This callback:
1. Receives scales stored in the PDF document via the `documentScales` parameter
2. Returns them to Nutrient so they're available for use
3. Automatically persists through save/reload cycles via Instant JSON

### DEPRECATED Implementation Plan (Kept for Historical Reference)

#### 1. Track Scale Configuration (JavaScript)

Add a variable to store the current scale configuration:

```javascript
window.nutrientPoc = {
    instance: null,
    dotNetRef: null,
    licenseKey: null,
    measurementScaleConfig: null,  // NEW: Track scale config
    // ...
};
```

#### 2. Store Configuration When Set

Update `setMeasurementScale()` to store the config:

```javascript
setMeasurementScale: async function(scale, unitFrom, unitTo) {
    const config = {
        scale: [{
            unitFrom: unitFrom,
            unitTo: unitTo,
            factor: scale
        }],
        precision: 'FOUR_DP'
    };

    // Store configuration for later retrieval
    this.measurementScaleConfig = {
        scale: scale,
        unitFrom: unitFrom,
        unitTo: unitTo
    };

    await this.instance.setMeasurementValueConfiguration(config);
    return true;
}
```

#### 3. Export Combined Data Structure

Update `exportAndClose()` to include scale config:

```javascript
exportAndClose: async function() {
    const instantJSON = await this.instance.exportInstantJSON();

    // Create combined data structure
    const exportData = {
        instantJSON: instantJSON,
        measurementScale: this.measurementScaleConfig  // Include scale config
    };

    await PSPDFKit.unload(this.instance);
    this.instance = null;

    return JSON.stringify(exportData);
}
```

**Exported Data Format**:
```json
{
    "instantJSON": {
        "format": "https://pspdfkit.com/instant-json/v1",
        "annotations": [ /* ... */ ],
        "formFieldValues": { /* ... */ }
    },
    "measurementScale": {
        "scale": 50,
        "unitFrom": "in",
        "unitTo": "ft"
    }
}
```

#### 4. Restore Scale on Reload

Update `loadPdfWithData()` to restore scale config:

```javascript
loadPdfWithData: async function(documentUrl, containerId, dataString) {
    // Parse combined data structure
    const data = JSON.parse(dataString);
    const instantJSON = data.instantJSON;
    const measurementScale = data.measurementScale;

    // Load PDF with Instant JSON
    const configuration = {
        container: '#' + containerId,
        document: documentUrl,
        instantJSON: instantJSON,
        // ... other config
    };
    this.instance = await PSPDFKit.load(configuration);

    // Restore measurement scale configuration if it exists
    if (measurementScale) {
        await this.setMeasurementScale(
            measurementScale.scale,
            measurementScale.unitFrom,
            measurementScale.unitTo
        );
        console.log('[Nutrient POC] ✓ Measurement scale restored:', measurementScale);
    }

    return true;
}
```

#### 5. C# Changes

No changes required to `NutrientPocTest.razor` - it already handles the data as a string and passes it through correctly.

---

## Testing Procedure

### Test Workflow

1. **Load PDF**: Click "Load Sample PDF"
2. **Set Scale**: Click "Set Scale (1:50)"
   - Verify console shows: `[Nutrient POC] ✓ Measurement scale set`
3. **Save Without Drawing**: Click "Save & Close PDF" (don't draw any measurements)
   - Verify console shows exported JSON with `measurementScale` property
4. **Reload**: Click "Reload with Saved Data"
   - Verify console shows: `[Nutrient POC] ✓ Measurement scale restored`
5. **Test Scale**: Draw a new measurement line
   - Verify the measurement uses 1:50 scale (not default 1:1)

### Expected Results

- ✅ New measurements after reload should use the 1:50 scale
- ✅ Console should confirm scale restoration
- ✅ Scale should persist even without any annotations being created

---

## References

### Documentation Sources

- **Nutrient Web SDK API**: https://www.nutrient.io/api/web/PSPDFKit.Instance.html#exportInstantJSON
- **Opening Documents**: https://www.nutrient.io/guides/web/open-a-document/
- **ASP.NET Integration**: https://www.nutrient.io/guides/web/open-a-document/aspnet/
- **GitHub Examples**: https://github.com/PSPDFKit/nutrient-web-examples/tree/main/examples/asp-net

### Product Updates

- **Q2 2024 Release**: Mentioned "persisting page settings" feature
- **2023.4 Release**: Added support for multiple measurement scales in PDFs

### Related Files

- `/wwwroot/js/nutrient-poc.js` - JavaScript interop and PDF viewer logic
- `/Components/Pages/NutrientPocTest.razor` - Blazor component UI
- `/docs/TROUBLESHOOTING_PROCESS_MANAGEMENT.md` - Process management guide (created Oct 18, 2025)

---

## Conclusion

The investigation confirmed that Nutrient Web SDK's Instant JSON export feature does not include global measurement scale configuration. This is by design - Instant JSON is specifically for annotations and form field values, not viewer instance configuration.

The solution requires manually tracking and persisting the measurement scale configuration alongside Instant JSON data. The implementation is straightforward and will ensure that calibration scales persist through save/reload cycles.

---

## Next Steps

1. ✅ Investigation complete
2. ✅ Solution implemented (`measurementValueConfiguration` callback added)
3. ✅ **TESTED AND CONFIRMED**: Scale persistence working with built-in toolbar UI
4. ✅ Applied same pattern to production NutrientPdfViewer component
5. ✅ **PRODUCTION SOLUTION**: Base64 encoding implemented to work around SignalR limits

---

## Production Implementation (October 19, 2025)

### The Real Problem: SignalR Message Size Limits

When implementing the solution in production (`NutrientPdfViewer.razor`), we discovered that the actual blocker was not the measurement scale persistence (which works automatically), but **Blazor Server's SignalR message size limitation**.

**Root Cause**:
- Blazor Server uses SignalR for JavaScript interop
- Default SignalR message size limit: **32KB**
- Exported PDF with measurements: **~960KB**
- JavaScript interop failed silently when trying to transfer large byte arrays

**Error Observed**:
```
fail: FabOS.WebServer.Components.Shared.NutrientPdfViewer[0]
      [NutrientPdfViewer] Error in HandleCloseAsync
         at ...NutrientPdfViewer.razor:line 842
```

The modal would close without saving because the PDF export failed.

### Solution Implemented: Base64 Encoding

**Files Modified**:
1. `/wwwroot/js/nutrient-viewer.js` (lines 1236-1281)
2. `/Components/Shared/NutrientPdfViewer.razor` (lines 833-859)

**JavaScript Changes** (`nutrient-viewer.js`):
```javascript
/**
 * Export PDF with embedded annotations for SharePoint upload
 * Returns base64-encoded PDF to avoid SignalR message size limits
 */
exportPDF: async function(containerId) {
    const instanceData = this.instances[containerId];
    if (!instanceData?.instance) {
        return null;
    }

    try {
        // Export PDF with all annotations embedded
        const arrayBuffer = await instanceData.instance.exportPDF();

        // Convert ArrayBuffer to Base64 to avoid SignalR limits
        const uint8Array = new Uint8Array(arrayBuffer);
        let binaryString = '';
        const chunkSize = 8192;

        // Build binary string in chunks to avoid stack overflow
        for (let i = 0; i < uint8Array.length; i += chunkSize) {
            const chunk = uint8Array.subarray(i, Math.min(i + chunkSize, uint8Array.length));
            binaryString += String.fromCharCode.apply(null, chunk);
        }

        const base64 = btoa(binaryString);

        console.log(`[Nutrient Viewer] ✓ PDF exported successfully (${arrayBuffer.byteLength} bytes, base64: ${base64.length} chars)`);
        return base64;

    } catch (error) {
        console.error('[Nutrient Viewer] ❌ Error exporting PDF:', error);
        return null;
    }
}
```

**C# Changes** (`NutrientPdfViewer.razor`):
```csharp
// Export PDF as base64 to avoid SignalR message size limits
var pdfBase64 = await JS.InvokeAsync<string>("nutrientViewer.exportPDF", containerId);
Logger.LogInformation("[NutrientPdfViewer] Exported PDF (base64): {Length} chars", pdfBase64?.Length ?? 0);

// Convert base64 back to bytes
byte[]? pdfBytes = null;
if (!string.IsNullOrEmpty(pdfBase64))
{
    try
    {
        pdfBytes = Convert.FromBase64String(pdfBase64);
        Logger.LogInformation("[NutrientPdfViewer] Converted PDF to bytes: {Length} bytes", pdfBytes.Length);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "[NutrientPdfViewer] Error converting base64 to bytes");
    }
}

// Upload to SharePoint
if (pdfBytes != null && pdfBytes.Length > 0)
{
    using var pdfStream = new MemoryStream(pdfBytes);
    var updatedFile = await sharePointSvc.UpdateFileAsync(drawing.SharePointItemId, pdfStream, "application/pdf");
}
```

**Key Points**:
- Base64 encoding adds 33% overhead (960KB becomes ~1.28MB as string)
- SignalR can handle base64 strings better than large byte arrays
- Chunked encoding prevents stack overflow errors
- SharePoint receives the complete PDF with all embedded measurements and scales

### Test Results (October 19, 2025)

**User Feedback**: "Great, It worked !!!"

**Console Logs Confirming Success**:
```
[Nutrient Viewer] ✓ PDF exported successfully (958606 bytes, base64: 1278142 chars)
[Nutrient Viewer] 📄 Loading PDF from URL: /api/packagedrawings/10/sharepoint-content
[Nutrient Viewer] Document scales loaded: Array [ {…} ]
  ▶ 0: Object { scale: (1) […], precision: "fourDecimalPlaces" }
    ▶ scale: Array [ {…} ]
      ▶ 0: Object { unitFrom: PSPDFKit.MeasurementScaleUnitFrom.INCHES, ... }
         factor: 50  ← 1:50 scale persisted!
```

**Verification**:
- ✅ PDF saved to SharePoint with embedded scales
- ✅ Scales persist after save/reload
- ✅ New measurements automatically use saved 1:50 scale
- ✅ No SignalR errors
- ✅ Modal closes successfully after save

### Alternative Solutions Considered

The solution discussion explored several alternatives:

**1. Cloud Direct Upload to SharePoint** (Future Enhancement)
- JavaScript gets upload session URL from C# via SignalR
- C# calls Graph API `createUploadSession`
- JavaScript uploads PDF directly to SharePoint via HTTP
- Benefits: No SignalR limits, no base64 overhead
- Status: Recommended for future optimization

**2. Increase SignalR Message Size**
```csharp
services.AddServerSideBlazor()
    .AddHubOptions(options => {
        options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB
    });
```
- Simple configuration change
- Acceptable for internal applications
- Still has limits for very large files
- Status: Valid alternative to base64 approach

**3. Multi-Provider Storage Architecture** (Planned)
- Abstract storage layer supporting SharePoint, Google Drive, Dropbox, Azure Blob
- Database as single source of truth for metadata
- Cloud direct upload for all providers
- Status: In progress - see `/docs/CLOUD_STORAGE_ARCHITECTURE.md`

### PSPDFKit/Nutrient Licensing Clarification

**Question**: Does the PDF saving approach affect licensing?

**Answer**: ✅ NO additional licensing required

- Client-side `exportPDF()` is covered by Nutrient Web SDK license
- No server-side processing (Document Engine) involved
- JavaScript API usage is within license scope
- Upload mechanism (SignalR vs HTTP) doesn't affect licensing

**Source**: Nutrient documentation confirms:
> "Nutrient Web SDK enables client-side viewing and conversion of PDF... directly on any browser — no server dependencies or MS Office licenses are required"

### Testing Workflow

**To verify the solution works:**
1. Navigate to package drawing upload page
2. Upload a PDF
3. Open PDF in viewer modal
4. Use Nutrient's built-in measurement tools to set a scale (e.g., 1:50)
5. Draw measurement annotations
6. Click "Save & Close"
7. Reopen the same PDF
8. Check browser console for: `"Document scales loaded: Array [ {…} ]"`
9. Verify `factor: 50` appears in console
10. Create new measurement and confirm it uses 1:50 scale

**Expected Results:**
- Console shows document scales being loaded
- New measurements automatically use the saved 1:50 scale
- No SignalR errors in console or server logs
- PDF saved successfully to SharePoint

---

**Document Version**: 2.0
**Last Updated**: October 19, 2025
**Author**: Claude (AI Assistant)
**Reviewed By**: User (Production tested and confirmed working)
**Status**: ✅ PRODUCTION READY
