# Excel Export MIME Type Override - Technical Deep Dive

**Date**: 2026-01-12
**Topic**: Content-Type Header Override Causing ZIP Corruption
**Severity**: High - User-facing export feature broken

---

## Executive Summary

The Excel export endpoint returns a ZIP file with Content-Type `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` instead of `application/zip`, causing browsers and clients to treat it as a single Excel file. This triggers decompression that exposes the internal XML structure instead of the contained .xlsx files.

---

## The Problem: Content-Type Mismatch

### What Code Sets
**File**: `ExportEventAttendeesQueryHandler.cs` (line 156)
```csharp
contentType = "application/zip";
```

### What HTTP Response Shows
```http
HTTP/1.1 200 OK
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
Content-Disposition: attachment; filename="event-...-signup-lists-excel-....zip"
```

### Why This Breaks Downloads

1. **Browser sees**: `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
2. **Browser thinks**: "This is an Excel file"
3. **Browser does**: Treats ZIP as Excel, extracts inner XML structure
4. **User gets**: Broken file with `xl/workbook.xml` instead of `.xlsx` files

---

## ASP.NET Core File Result Behavior

### How `File()` Method Works

**File**: `EventsController.cs` (line 1963-1967)
```csharp
return File(
    result.Value.FileContent,      // byte[] data
    result.Value.ContentType,       // "application/zip"
    result.Value.FileName           // "event-{guid}-signup-lists-excel-{timestamp}.zip"
);
```

### MIME Type Detection Pipeline

ASP.NET Core's `FileContentResult` goes through multiple MIME type detection layers:

1. **Explicit ContentType**: Uses provided `result.Value.ContentType`
2. **Filename Extension Detection**: Checks filename for known extensions
3. **FileExtensionContentTypeProvider**: Maps extensions to MIME types
4. **StaticFileOptions**: Custom MIME type mappings
5. **IIS/Kestrel Middleware**: Final MIME type override

**Problem**: Even though we provide `"application/zip"`, the pipeline can override it based on filename.

---

## Root Cause: Filename Contains "excel"

### Evidence

**Filename**: `event-0458806b-8672-4ad5-a7cb-f5346f1b282a-signup-lists-excel-20260112-153045.zip`

**Key Issue**: The word **"excel"** in the filename may trigger MIME type detection.

### How ASP.NET Core Detects MIME Type

```csharp
// Simplified pseudo-code of ASP.NET Core behavior
public string DetermineMimeType(byte[] content, string fileName, string explicitMimeType)
{
    // Check if filename contains hints
    if (fileName.Contains("excel") || fileName.EndsWith(".xlsx"))
    {
        // Override to Excel MIME type
        return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    }

    // Check file extension
    var extension = Path.GetExtension(fileName);
    if (extension == ".xlsx")
    {
        return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    }

    // Use explicit MIME type as fallback
    return explicitMimeType;
}
```

**This is NOT actual ASP.NET Core code**, but illustrates the behavior we're observing.

---

## ZIP vs XLSX: File Format Confusion

### Why This Causes Corruption

#### XLSX File Structure
XLSX files are ZIP archives with specific XML structure:
```
my-workbook.xlsx (ZIP archive)
  ├── xl/
  │   ├── workbook.xml
  │   └── worksheets/
  │       └── sheet1.xml
  ├── _rels/
  │   └── .rels
  └── [Content_Types].xml
```

#### Our Excel Export ZIP Structure
```
export.zip (ZIP archive)
  ├── Food-and-Drinks.xlsx (ZIP archive - nested!)
  │   ├── xl/
  │   │   ├── workbook.xml
  │   │   └── worksheets/
  │   └── _rels/
  └── API-Test-Sign-Up-List.xlsx (ZIP archive - nested!)
      ├── xl/
      ├── _rels/
      └── [Content_Types].xml
```

#### What Happens When Content-Type is Wrong

1. **Browser receives**: `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
2. **Browser assumes**: This is a single XLSX file (ZIP with XML inside)
3. **Browser extracts**: Outer ZIP layer (our export.zip)
4. **Browser sees**: First XLSX file inside (Food-and-Drinks.xlsx)
5. **Browser extracts again**: Inner XLSX ZIP structure
6. **User gets**: XML files (`xl/workbook.xml`) instead of `.xlsx` files

**This is double-decompression**:
```
export.zip → (decompress as XLSX) → Food-and-Drinks.xlsx → (decompress as XLSX) → xl/workbook.xml
```

---

## Why CSV Export Works

### CSV Export Structure
```
export.zip
  ├── Food-Drinks-Mandatory.csv (plain text)
  ├── Food-Drinks-Open.csv (plain text)
  └── API-Test-Sign-Up-List-Open.csv (plain text)
```

### No MIME Type Confusion
- CSV files are plain text (`text/csv`)
- No nested ZIP structures
- Browser doesn't try to auto-extract
- `application/zip` MIME type is respected

---

## Systematic Fix Approaches

### Approach 1: Force Content-Type in Controller ✅ **RECOMMENDED**

**Rationale**: Override ASP.NET Core's MIME type detection completely.

**Implementation**:
```csharp
// File: src/LankaConnect.API/Controllers/EventsController.cs
// Line: 1963-1967

return File(
    result.Value.FileContent,
    "application/zip",  // ✅ FORCE application/zip, ignore auto-detection
    result.Value.FileName
);
```

**Pros**:
- ✅ Simple one-line change
- ✅ Directly overrides MIME type detection
- ✅ No changes to infrastructure layer

**Cons**:
- ⚠️ Loses flexibility if other formats are added
- ⚠️ Breaks query handler's ContentType control

**Testing**:
```bash
curl -I "https://api.lankaconnect.com/api/Events/{id}/export?format=SignUpListsExcel"
# Should show: Content-Type: application/zip
```

---

### Approach 2: Remove "excel" from Filename

**Rationale**: Prevent MIME type detection from filename hints.

**Implementation**:
```csharp
// File: src/LankaConnect.Application/Events/Queries/ExportEventAttendees/ExportEventAttendeesQueryHandler.cs
// Line: 155

fileName = $"event-{request.EventId}-signup-lists-archive-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
//                                                  ^^^^^^^ Changed from "excel" to "archive"
```

**Pros**:
- ✅ Preserves query handler's ContentType control
- ✅ More semantic filename (doesn't mislead about contents)

**Cons**:
- ⚠️ Less obvious what format is inside ZIP
- ⚠️ May not fix issue if detection is extension-based only

**Testing**:
```bash
# Download file
curl "https://api.lankaconnect.com/api/Events/{id}/export?format=SignUpListsExcel" -o test.zip

# Filename should be:
# event-{guid}-signup-lists-archive-{timestamp}.zip
```

---

### Approach 3: Custom FileResult with Locked MIME Type

**Rationale**: Create custom action result that prevents MIME type override.

**Implementation**:
```csharp
// New file: src/LankaConnect.API/ActionResults/ZipFileResult.cs
public class ZipFileResult : FileContentResult
{
    public ZipFileResult(byte[] fileContents, string fileName)
        : base(fileContents, "application/zip")
    {
        FileDownloadName = fileName;
    }

    public override void ExecuteResult(ActionContext context)
    {
        // Lock Content-Type before execution
        context.HttpContext.Response.Headers["Content-Type"] = "application/zip";
        base.ExecuteResult(context);
    }
}

// In EventsController.cs
return new ZipFileResult(result.Value.FileContent, result.Value.FileName);
```

**Pros**:
- ✅ Most robust solution
- ✅ Reusable for other ZIP exports
- ✅ Explicitly prevents MIME type override

**Cons**:
- ⚠️ More code to maintain
- ⚠️ Overkill for single endpoint

---

### Approach 4: Configure FileExtensionContentTypeProvider

**Rationale**: Globally configure ASP.NET Core to always use `application/zip` for `.zip` files.

**Implementation**:
```csharp
// File: src/LankaConnect.API/Program.cs or Startup.cs

var provider = new FileExtensionContentTypeProvider();

// Ensure .zip always maps to application/zip (override any defaults)
provider.Mappings[".zip"] = "application/zip";

// Remove any xlsx-related mappings for .zip files
provider.Mappings.Remove(".xlsx");

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});
```

**Pros**:
- ✅ Global fix for all ZIP exports
- ✅ Prevents future similar issues

**Cons**:
- ⚠️ Affects all static file serving
- ⚠️ May not fix issue if MIME detection is in different middleware

---

## Recommended Solution

### Primary Fix: Approach 1 (Force Content-Type)

**Why**: Simple, direct, and effective.

```csharp
// In EventsController.cs (line 1963-1967)
return File(
    result.Value.FileContent,
    "application/zip",  // ✅ Always force ZIP MIME type
    result.Value.FileName
);
```

### Secondary Fix: Approach 2 (Rename File)

**Why**: Improves semantic clarity and removes MIME detection hints.

```csharp
// In ExportEventAttendeesQueryHandler.cs (line 155)
fileName = $"event-{request.EventId}-signup-lists-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
// Removed "excel" from filename - cleaner and avoids MIME confusion
```

### Combined Fix (Best Practice)

Apply both approaches for defense-in-depth:

1. **Remove "excel" from filename** (query handler)
2. **Force `application/zip`** (controller)

**Code Changes**:

**File 1**: `ExportEventAttendeesQueryHandler.cs` (line 155)
```csharp
fileName = $"event-{request.EventId}-signup-lists-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
```

**File 2**: `EventsController.cs` (line 1963-1967)
```csharp
return File(
    result.Value.FileContent,
    "application/zip",
    result.Value.FileName
);
```

---

## Testing Strategy

### Test 1: Verify HTTP Headers

```bash
curl -I "https://api.lankaconnect.com/api/Events/0458806b-8672-4ad5-a7cb-f5346f1b282a/export?format=SignUpListsExcel" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Expected**:
```http
HTTP/1.1 200 OK
Content-Type: application/zip
Content-Disposition: attachment; filename="event-...-signup-lists-....zip"
```

**Pass Criteria**: `Content-Type: application/zip` (NOT `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`)

---

### Test 2: Verify ZIP Structure

```bash
# Download file
curl "https://api.lankaconnect.com/api/Events/0458806b-8672-4ad5-a7cb-f5346f1b282a/export?format=SignUpListsExcel" \
  -o test.zip \
  -H "Authorization: Bearer YOUR_TOKEN"

# List contents
unzip -l test.zip
```

**Expected**:
```
Archive:  test.zip
  Length      Date    Time    Name
---------  ---------- -----   ----
     8493  2026-01-12 15:30   Food-and-Drinks.xlsx
     6234  2026-01-12 15:30   API-Test-Sign-Up-List.xlsx
---------                     -------
    14727                     2 files
```

**Pass Criteria**: ZIP contains `.xlsx` files, NOT `xl/` directories

---

### Test 3: Verify Excel File Validity

```bash
# Extract ZIP
unzip test.zip -d extracted/

# Verify XLSX file signature (should be ZIP)
file extracted/Food-and-Drinks.xlsx
# Expected: Microsoft Excel 2007+

# Check ZIP signature (hex dump)
xxd extracted/Food-and-Drinks.xlsx | head -1
# Expected: 00000000: 504b 0304 .... (PK.. = ZIP signature)
```

**Pass Criteria**: XLSX files have ZIP signature (`PK\x03\x04`)

---

### Test 4: Open in Excel

**Manual Test**:
1. Download ZIP from API
2. Extract ZIP
3. Open `Food-and-Drinks.xlsx` in Microsoft Excel

**Expected**:
- ✅ Excel opens file without errors
- ✅ Sheets visible: "Mandatory Items", "Suggested Items", "Open Items"
- ✅ Data is correctly formatted with headers

---

## Edge Cases to Test

### 1. Single Signup List (Only 1 File in ZIP)
**Event ID**: Create test event with 1 signup list
**Expected**: ZIP contains 1 `.xlsx` file

### 2. Many Signup Lists (10+ Files)
**Event ID**: Create test event with 10 signup lists
**Expected**: ZIP contains 10 `.xlsx` files, all valid

### 3. Special Characters in Signup List Name
**Signup List**: "Food & Drinks: Hot/Cold (Mandatory!)"
**Expected**: Filename sanitized: `Food-Drinks-Hot-Cold-Mandatory.xlsx`

### 4. Empty Signup List (No Items)
**Event ID**: Create test event with signup list containing 0 items
**Expected**: Returns error: "No signup lists to export"

---

## Integration Test (Automated)

```csharp
// File: tests/LankaConnect.IntegrationTests/EventExportTests.cs

[Fact]
public async Task ExportSignUpListsExcel_ReturnsZipWithXlsxFiles()
{
    // Arrange
    var eventId = await CreateTestEventWithSignUpLists();

    // Act
    var response = await _client.GetAsync($"/api/Events/{eventId}/export?format=SignUpListsExcel");

    // Assert - HTTP Response
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("application/zip", response.Content.Headers.ContentType?.MediaType);

    var contentDisposition = response.Content.Headers.ContentDisposition;
    Assert.NotNull(contentDisposition);
    Assert.Equal("attachment", contentDisposition.DispositionType);
    Assert.EndsWith(".zip", contentDisposition.FileName);

    // Assert - ZIP Structure
    var zipBytes = await response.Content.ReadAsByteArrayAsync();
    using var zipStream = new MemoryStream(zipBytes);
    using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

    var entries = archive.Entries.ToList();
    Assert.NotEmpty(entries);

    // All entries should be .xlsx files
    Assert.All(entries, entry =>
    {
        Assert.EndsWith(".xlsx", entry.FullName);
        Assert.DoesNotContain("/", entry.FullName); // No subdirectories
    });

    // Assert - Excel File Validity
    foreach (var entry in entries)
    {
        using var entryStream = entry.Open();
        using var ms = new MemoryStream();
        await entryStream.CopyToAsync(ms);
        ms.Position = 0;

        // Verify XLSX signature (PK\x03\x04)
        var signature = new byte[4];
        ms.Read(signature, 0, 4);
        Assert.Equal(0x50, signature[0]); // 'P'
        Assert.Equal(0x4B, signature[1]); // 'K'
        Assert.Equal(0x03, signature[2]);
        Assert.Equal(0x04, signature[3]);

        // Verify ClosedXML can open it
        ms.Position = 0;
        using var workbook = new XLWorkbook(ms);
        Assert.NotEmpty(workbook.Worksheets);
    }
}
```

---

## Monitoring and Alerting

### Application Insights Query

```kusto
// Track Content-Type mismatches
requests
| where url contains "/export"
| where url contains "SignUpListsExcel"
| extend contentType = tostring(customDimensions.ContentType)
| where contentType != "application/zip"
| project
    timestamp,
    url,
    contentType,
    resultCode,
    duration
| order by timestamp desc
```

### Alert Rule

**Condition**: `contentType != "application/zip"` for Excel export
**Action**: Send notification to DevOps team
**Frequency**: Check every 5 minutes

---

## Rollback Considerations

### If Fix Causes Issues

**Symptoms**:
- Other export formats break (CSV, single Excel)
- Browser downloads fail
- File corruption in other features

**Rollback Strategy**:
1. Revert controller change (keep query handler changes)
2. Redeploy
3. Test CSV export to ensure it still works
4. Investigate ASP.NET Core middleware configuration

---

## Documentation Updates

### API Documentation
Update export endpoint documentation:

```yaml
/api/Events/{id}/export:
  get:
    parameters:
      - name: format
        schema:
          enum: [Excel, Csv, SignUpListsZip, SignUpListsExcel]
    responses:
      200:
        description: Exported data
        content:
          # SignUpListsExcel format returns ZIP (not single Excel file)
          application/zip:  # ✅ Updated from application/vnd.openxmlformats...
            schema:
              type: string
              format: binary
            examples:
              signUpListsExcel:
                summary: ZIP archive with multiple Excel files
                value: (binary ZIP data)
```

### User-Facing Documentation
Update export feature guide:

> **Excel Sign-Up List Export**
>
> Format: `SignUpListsExcel`
>
> Returns: ZIP archive (`.zip` file) containing one Excel file (`.xlsx`) per sign-up list.
>
> **Important**: You must extract the ZIP file to access the Excel files inside.
>
> Example:
> ```
> event-...-signup-lists-....zip
>   ├── Food-and-Drinks.xlsx
>   └── Volunteers-Sign-Up.xlsx
> ```

---

## Related Issues

### Similar MIME Type Issues in Codebase

Search for other potential MIME type conflicts:

```bash
# Find all File() calls in controllers
grep -r "return File(" src/LankaConnect.API/Controllers/

# Check for other Excel exports
grep -r "\.xlsx" src/LankaConnect.Application/

# Check for other ZIP exports
grep -r "ZipArchive" src/LankaConnect.Infrastructure/
```

**Action**: Review and apply same fix pattern if similar issues exist.

---

## References

### ASP.NET Core Documentation
- [File Results](https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/actions#file-results)
- [Static Files Middleware](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/static-files)
- [Content Negotiation](https://docs.microsoft.com/en-us/aspnet/core/web-api/advanced/formatting)

### MIME Types
- [IANA Media Types](https://www.iana.org/assignments/media-types/media-types.xhtml)
- `application/zip`: ZIP archive
- `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`: Excel 2007+ (.xlsx)

### ZIP Format
- [ZIP File Format Specification](https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT)
- [XLSX Format (Office Open XML)](https://docs.microsoft.com/en-us/office/open-xml/structure-of-a-spreadsheetml-document)
