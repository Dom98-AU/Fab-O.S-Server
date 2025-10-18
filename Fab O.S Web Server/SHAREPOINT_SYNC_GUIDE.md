# SharePoint Folder Sync Guide

## Overview

This guide explains how to synchronize existing takeoffs with SharePoint folder structure. This is useful when:
- You have existing takeoffs created before SharePoint integration was enabled
- You want to ensure all takeoffs have their folder structure in SharePoint
- You need to rebuild SharePoint folders after a migration

## API Endpoints

The SharePoint Sync service provides three REST API endpoints:

### 1. Sync All Takeoffs
**Endpoint:** `POST /api/sharepointsync/sync-all`

Synchronizes ALL existing takeoffs in the database with SharePoint.

**Example using cURL:**
```bash
curl -X POST https://localhost:5223/api/sharepointsync/sync-all
```

**Example using PowerShell:**
```powershell
Invoke-RestMethod -Uri "https://localhost:5223/api/sharepointsync/sync-all" -Method Post
```

**Response Example:**
```json
{
  "success": true,
  "takeoffsProcessed": 15,
  "revisionsProcessed": 23,
  "packagesProcessed": 87,
  "foldersCreated": 110,
  "foldersAlreadyExisted": 0,
  "errors": 0,
  "errorMessages": [],
  "createdFolders": [
    "Takeoffs/TK-2025-001/A",
    "Takeoffs/TK-2025-001/A/PKG-001",
    "Takeoffs/TK-2025-001/A/PKG-002",
    ...
  ],
  "duration": "00:00:45.123"
}
```

### 2. Sync Single Takeoff
**Endpoint:** `POST /api/sharepointsync/sync-takeoff/{takeoffId}`

Synchronizes a specific takeoff and all its packages.

**Example:**
```bash
curl -X POST https://localhost:5223/api/sharepointsync/sync-takeoff/5
```

**Example using PowerShell:**
```powershell
Invoke-RestMethod -Uri "https://localhost:5223/api/sharepointsync/sync-takeoff/5" -Method Post
```

### 3. Sync Single Package
**Endpoint:** `POST /api/sharepointsync/sync-package/{packageId}`

Synchronizes a specific package only.

**Example:**
```bash
curl -X POST https://localhost:5223/api/sharepointsync/sync-package/42
```

**Example using PowerShell:**
```powershell
Invoke-RestMethod -Uri "https://localhost:5223/api/sharepointsync/sync-package/42" -Method Post
```

### 4. Get Sync Status
**Endpoint:** `GET /api/sharepointsync/sync-status`

Returns information about available sync endpoints.

**Example:**
```bash
curl https://localhost:5223/api/sharepointsync/sync-status
```

## How It Works

### Folder Structure Created

The sync process creates this 4-level hierarchy:

```
Takeoffs/
  ├── TK-2025-001/
  │   ├── A/
  │   │   ├── PKG-001/
  │   │   ├── PKG-002/
  │   │   └── PKG-003/
  │   └── B/
  │       ├── PKG-001/
  │       └── PKG-002/
  └── TK-2025-002/
      └── A/
          ├── PKG-001/
          └── PKG-002/
```

### Process Flow

1. **Query Database:** Fetches all active takeoffs with their revisions and packages
2. **Check Existing:** For each folder, checks if it already exists in SharePoint
3. **Create Missing:** Creates only the folders that don't exist
4. **Skip Existing:** If a folder already exists, it's counted but not recreated
5. **Error Handling:** Logs errors but continues processing other items
6. **Report Results:** Returns detailed statistics about what was created

### What Gets Synced

- ✅ Takeoff folders: `Takeoffs/{TakeoffNumber}/`
- ✅ Revision folders: `Takeoffs/{TakeoffNumber}/{RevisionCode}/`
- ✅ Package folders: `Takeoffs/{TakeoffNumber}/{RevisionCode}/PKG-{PackageNumber}/`
- ❌ Files (only folder structure is created, not files)

## Testing the Sync

### Option 1: Using Swagger UI

1. Navigate to `https://localhost:5223/api-docs`
2. Find the `SharePointSync` section
3. Click on `POST /api/sharepointsync/sync-all`
4. Click "Try it out"
5. Click "Execute"
6. View the response

### Option 2: Using Browser Console

Open browser developer console (F12) and run:

```javascript
fetch('/api/sharepointsync/sync-all', { method: 'POST' })
  .then(r => r.json())
  .then(data => console.log(data));
```

### Option 3: Using Postman

1. Create new POST request
2. URL: `https://localhost:5223/api/sharepointsync/sync-all`
3. Click Send
4. View response

## Monitoring and Logs

The sync process logs detailed information:

```
[Information] Starting SharePoint sync for all takeoffs
[Information] Found 15 takeoffs to sync
[Information] Created takeoff/revision folder: Takeoffs/TK-2025-001/A
[Information] Created package folder: Takeoffs/TK-2025-001/A/PKG-001
[Information] SharePoint sync completed. Takeoffs: 15, Packages: 87, Folders Created: 110
```

Check application logs for detailed progress and any errors.

## Error Handling

### Common Errors

**Error:** "SharePoint not connected"
- **Solution:** Ensure SharePoint setup is complete at `/admin/sharepoint-setup`

**Error:** "Document library 'Takeoff Files' not found"
- **Solution:** Create the document library in SharePoint (see SharePoint Setup guide)

**Error:** "Package X has no takeoff number"
- **Solution:** This package is invalid and will be skipped

### Retry Failed Items

If some items fail, you can:
1. Fix the underlying issue
2. Run sync again - it will skip items that already exist
3. Or sync specific takeoffs using the single takeoff endpoint

## Best Practices

1. **Run During Off-Hours:** Sync all takeoffs during low-traffic times
2. **Start Small:** Test with a single takeoff first using `/sync-takeoff/{id}`
3. **Check Results:** Review the response to ensure expected folders were created
4. **Verify in SharePoint:** Log into SharePoint and verify folders exist
5. **One-Time Operation:** This is typically run once after enabling SharePoint integration

## Security

⚠️ **Important:** These endpoints should be restricted to administrators only.

Consider adding authorization:
```csharp
[Authorize(Roles = "Administrator")]
public class SharePointSyncController : ControllerBase
{
    // ...
}
```

## Performance

- **Small System (< 50 takeoffs):** ~10-30 seconds
- **Medium System (50-500 takeoffs):** ~30-120 seconds
- **Large System (500+ takeoffs):** 2-10 minutes

The process runs asynchronously and won't block other operations.

## Troubleshooting

### Sync appears stuck
- Check application logs for errors
- Verify SharePoint connection is still active
- Try syncing a single takeoff to isolate the issue

### Some folders not created
- Check the `errorMessages` array in the response
- Verify takeoffs have valid TakeoffNumbers
- Ensure packages are linked to valid revisions

### All folders show "already existed"
- This is normal if sync was run before
- Folders won't be recreated if they already exist
- This is safe to run multiple times

## FAQ

**Q: Can I run this multiple times?**
A: Yes, it's safe. Existing folders are detected and skipped.

**Q: Will this copy existing files?**
A: No, this only creates folder structure. Files are uploaded separately.

**Q: What if a takeoff has no packages?**
A: The takeoff and revision folders will still be created.

**Q: Can I schedule this to run automatically?**
A: Yes, you could create a background job, but typically it's run manually once.

**Q: Will this affect performance?**
A: Minimal impact. The sync runs asynchronously and SharePoint calls are batched efficiently.

---

## Quick Start

**To sync all existing takeoffs right now:**

1. Ensure SharePoint is configured and connected
2. Open browser and navigate to: `https://localhost:5223/api-docs`
3. Find `POST /api/sharepointsync/sync-all`
4. Click "Try it out" → "Execute"
5. Wait for completion
6. Review the summary in the response

Done! All your existing takeoffs now have their folder structure in SharePoint. 🎉
