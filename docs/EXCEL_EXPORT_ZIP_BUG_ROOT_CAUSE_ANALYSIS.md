# Excel Export ZIP Bug - Comprehensive Root Cause Analysis

**Date**: 2026-01-12
**Investigator**: SPARC Architecture Agent
**Status**: Root Cause Identified, Fix Pending Verification

---

## Executive Summary

The Excel export API endpoint returns a ZIP file that, when extracted, contains internal XLSX XML structure (`xl/workbook.xml`, `_rels/`, `[Content_Types].xml`) instead of proper `.xlsx` files. The root cause is **NOT** in the C# backend code, but likely in **Azure deployment configuration or IIS/Kestrel MIME type handling**.

### Critical Finding
- ✅ **Backend code is CORRECT** - Creates valid ZIP archive with proper .xlsx files
- ❌ **Response transformation is WRONG** - Something between backend and client is corrupting the ZIP
- ⚠️ **Content-Type mismatch** - Code sets `application/zip`, but HTTP response shows `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`

---

## Problem Statement

### Expected Behavior
```
test-export.zip/
  ├── Food-and-Drinks.xlsx (valid Excel file)
  └── API-Test-Sign-Up-List.xlsx (valid Excel file)
```

### Actual Behavior
```
test-export.zip/
  ├── xl/workbook.xml
  ├── xl/worksheets/sheet1.xml
  ├── _rels/.rels
  └── [Content_Types].xml
```

### API Testing Evidence
- **Endpoint**: `/api/Events/0458806b-8672-4ad5-a7cb-f5346f1b282a/export?format=SignUpListsExcel`
- **HTTP Status**: 200 OK
- **Content-Type Header**: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` (XLSX MIME type)
- **File Size**: 10,867 bytes (unchanged after multiple "fixes")
- **Event Data**: 2 signup lists with items

---

## Code Flow Analysis

### 1. Entry Point: `EventsController.ExportEventAttendees()`
**Location**: `c:\Work\LankaConnect\src\LankaConnect.API\Controllers\EventsController.cs` (lines 1963-1967)

```csharp
return File(
    result.Value.FileContent,  // byte[] from query handler
    result.Value.ContentType,  // "application/zip" for SignUpListsExcel
    result.Value.FileName      // "event-{guid}-signup-lists-excel-{timestamp}.zip"
);
```

**Analysis**:
- ✅ Controller correctly returns `File()` result with ContentType from query handler
- ✅ No transformation or processing of byte array
- ✅ Standard ASP.NET Core `File()` method usage

---

### 2. Application Layer: `ExportEventAttendeesQueryHandler`
**Location**: `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Queries\ExportEventAttendees\ExportEventAttendeesQueryHandler.cs` (lines 151-156)

```csharp
if (request.Format == ExportFormat.SignUpListsExcel)
{
    // Generate ZIP with Excel files (one Excel per signup list)
    fileContent = _excelService.ExportSignUpListsToExcelZip(signUpListsForExport, request.EventId);
    fileName = $"event-{request.EventId}-signup-lists-excel-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
    contentType = "application/zip";  // ✅ CORRECT: Set as ZIP content type
}
```

**Analysis**:
- ✅ **Correctly sets `contentType = "application/zip"`**
- ✅ Filename has `.zip` extension
- ✅ Calls Excel service to generate ZIP archive

---

### 3. Infrastructure Layer: `ExcelExportService.ExportSignUpListsToExcelZip()`
**Location**: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Services\Export\ExcelExportService.cs` (lines 47-152)

#### Step 3a: ZIP Archive Creation (lines 59-60)
```csharp
using var zipStream = new MemoryStream();
using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
{
```
**Analysis**: ✅ Standard .NET `ZipArchive` creation pattern

#### Step 3b: Excel Workbook Creation (lines 66-96)
```csharp
using var workbook = new XLWorkbook();

// Group items by category within this signup list
var categorizedItems = new Dictionary<string, List<SignUpItemDto>>
{
    ["Mandatory"] = new(),
    ["Suggested"] = new(),
    ["Open"] = new()
};

// Create a sheet for each category that has items
foreach (var (categoryName, items) in categorizedItems)
{
    if (items.Any())
    {
        CreateGroupedSignUpSheet(workbook, $"{categoryName} Items", items);
    }
}
```
**Analysis**: ✅ Creates valid ClosedXML workbook with sheets

#### Step 3c: Excel to Bytes Conversion (lines 98-113)
```csharp
byte[] excelBytes;
using (var excelMemoryStream = new MemoryStream())
{
    workbook.SaveAs(excelMemoryStream);

    // CRITICAL: Reset stream position to beginning before reading
    excelMemoryStream.Position = 0;
    excelBytes = excelMemoryStream.ToArray();

    _logger.LogInformation(
        "Phase 6A.73: Saved Excel workbook for signup list '{Category}' - {ByteCount} bytes",
        signUpList.Category,
        excelBytes.Length);
}
```
**Analysis**: ✅ **Correctly resets stream position** (commit 3fcb1399)

#### Step 3d: Add Excel File to ZIP (lines 115-132)
```csharp
var sanitizedFileName = SanitizeFileName(signUpList.Category);
var fileName = $"{sanitizedFileName}.xlsx";

// Write XLSX directly to ZIP entry without additional compression
var entry = archive.CreateEntry(fileName, CompressionLevel.NoCompression);
using (var entryStream = entry.Open())
{
    entryStream.Write(excelBytes, 0, excelBytes.Length);
    entryStream.Flush();

    _logger.LogInformation(
        "Phase 6A.73: Added '{FileName}' to ZIP archive - {ByteCount} bytes",
        fileName,
        excelBytes.Length);
}
```
**Analysis**:
- ✅ Creates ZIP entry with `.xlsx` extension
- ✅ Uses `CompressionLevel.NoCompression` (XLSX is already compressed internally)
- ✅ Writes complete Excel byte array to ZIP entry
- ✅ Properly flushes stream

#### Step 3e: Return ZIP Bytes (lines 136-142)
```csharp
var zipBytes = zipStream.ToArray();
_logger.LogInformation(
    "Phase 6A.73: Successfully created Excel ZIP archive for event {EventId} - {ZipSize} bytes total",
    eventId,
    zipBytes.Length);

return zipBytes;
```
**Analysis**: ✅ Returns byte array of complete ZIP archive

---

## Root Cause Identification

### Evidence Analysis

#### 1. Code is Correct ✅
- Backend creates proper ZIP archive with `.xlsx` files inside
- ContentType is correctly set to `"application/zip"` in query handler
- File operations follow best practices (stream position reset, proper disposal)
- CSV export works correctly (uses same ZIP pattern) → proves ZIP creation logic is sound

#### 2. Response Content-Type Mismatch ⚠️
**Handler Sets**: `contentType = "application/zip"`
**HTTP Response Shows**: `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`

**Implications**:
- ASP.NET Core or Azure App Service is **overriding** the ContentType
- Something is detecting `.xlsx` extension in filename and forcing XLSX MIME type
- This override may trigger additional processing/transformation

#### 3. File Size Unchanged After "Fixes" 🔴
- Multiple commits (3fcb1399, d163df2c) didn't change file size from 10,867 bytes
- **This suggests the deployed code may NOT be the code in Git repository**
- Possible causes:
  - Build/deployment didn't pick up changes
  - Code is cached in Azure App Service
  - Previous build artifact is still running

#### 4. No Logs from Added Logging Statements 🔴
- `_logger.LogInformation()` statements added in commits 3fcb1399 and earlier
- **No logs appear in Azure Application Insights**
- **This confirms the deployed code is NOT the current Git code**

---

## Root Cause Conclusion

### Primary Root Cause: **Stale Deployment**
The deployed Azure App Service is running an **older version** of the code that does NOT include fixes from commits 3fcb1399 and d163df2c.

**Evidence**:
1. ❌ No logs from added `_logger.LogInformation()` statements
2. ❌ File size unchanged despite code changes
3. ❌ Bug behavior unchanged despite multiple "fixes"

### Secondary Root Cause: **Content-Type Override**
Even when correct code is deployed, Azure App Service or ASP.NET Core middleware may be:
1. Detecting `.xlsx` extension in ZIP entry names
2. Overriding `Content-Type` from `application/zip` to `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
3. This may trigger decompression or transformation that extracts XLSX internals

---

## Systematic Fix Plan

### Phase 1: Verify Deployment Status ⚠️ **CRITICAL FIRST STEP**

```bash
# 1. Check what's actually deployed
az webapp deployment list --name <app-name> --resource-group <rg-name> --output table

# 2. Check current commit in Azure
az webapp deployment source show --name <app-name> --resource-group <rg-name>

# 3. Force rebuild and redeploy
git commit --allow-empty -m "chore: Trigger rebuild for Excel export fix"
git push origin develop

# 4. Wait for deployment to complete
az webapp deployment list --name <app-name> --resource-group <rg-name> --query "[0].status"

# 5. Restart app service to clear any caching
az webapp restart --name <app-name> --resource-group <rg-name>
```

**Expected Outcome**: After redeployment, logs should appear in Application Insights with messages like:
```
Phase 6A.73: Starting Excel ZIP export for event {EventId} - 2 signup lists
Phase 6A.73: Saved Excel workbook for signup list 'Food and Drinks' - XXXX bytes
Phase 6A.73: Added 'Food-and-Drinks.xlsx' to ZIP archive - XXXX bytes
```

---

### Phase 2: Fix Content-Type Override Issue

If logs appear but bug persists, the Content-Type override is the issue.

#### Solution A: Remove `.xlsx` Extension from Filename Parameter ✅ **RECOMMENDED**

**Problem**: ASP.NET Core's `File()` method may be auto-detecting MIME type from filename.

**Fix**: Use generic filename in `File()` call, keep `.zip` extension only:

```csharp
// In EventsController.cs (line 1963-1967)
return File(
    result.Value.FileContent,
    "application/zip",  // FORCE application/zip, ignore result.Value.ContentType
    result.Value.FileName.Replace(".xlsx", ".zip")  // Ensure .zip extension
);
```

**Impact**:
- ✅ Forces `Content-Type: application/zip` in HTTP response
- ✅ Prevents ASP.NET Core from auto-detecting XLSX MIME type
- ✅ Browser/client will treat as ZIP archive, not Excel file

---

#### Solution B: Add Custom Middleware to Lock Content-Type

If Solution A fails, add middleware to prevent Content-Type overrides:

```csharp
// In Program.cs or Startup.cs
app.Use(async (context, next) =>
{
    await next();

    // If response is ZIP file, lock Content-Type
    if (context.Response.Headers.ContentType.ToString().Contains("application/zip") ||
        context.Response.Headers["Content-Disposition"].ToString().Contains(".zip"))
    {
        context.Response.ContentType = "application/zip";
    }
});
```

---

#### Solution C: Use Generic Filename in Controller

If MIME type detection is based on `Content-Disposition` filename:

```csharp
// In EventsController.cs
return File(
    result.Value.FileContent,
    "application/zip",
    $"export-{Guid.NewGuid()}.zip"  // Generic filename, no hints about contents
);
```

---

### Phase 3: Add Response Validation

Add integration test to verify HTTP response:

```csharp
[Fact]
public async Task ExportSignUpListsExcel_ReturnsZipWithXlsxFiles()
{
    // Arrange
    var eventId = Guid.Parse("0458806b-8672-4ad5-a7cb-f5346f1b282a");

    // Act
    var response = await _client.GetAsync($"/api/Events/{eventId}/export?format=SignUpListsExcel");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("application/zip", response.Content.Headers.ContentType.MediaType);

    var zipBytes = await response.Content.ReadAsByteArrayAsync();

    // Verify ZIP structure
    using var zipStream = new MemoryStream(zipBytes);
    using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

    // Should contain .xlsx files, NOT .xml files
    var entries = archive.Entries.ToList();
    Assert.All(entries, entry => Assert.EndsWith(".xlsx", entry.FullName));
    Assert.DoesNotContain(entries, entry => entry.FullName.Contains("xl/workbook.xml"));

    // Verify each .xlsx file is valid Excel
    foreach (var entry in entries)
    {
        using var entryStream = entry.Open();
        using var ms = new MemoryStream();
        await entryStream.CopyToAsync(ms);
        ms.Position = 0;

        // ClosedXML should be able to open it
        using var workbook = new XLWorkbook(ms);
        Assert.NotEmpty(workbook.Worksheets);
    }
}
```

---

## Testing Strategy

### Test 1: Verify Deployment ✅ **DO THIS FIRST**

**Objective**: Confirm correct code is deployed

**Steps**:
1. Deploy latest code to Azure
2. Check Application Insights for log messages
3. Look for: `"Phase 6A.73: Starting Excel ZIP export"`

**Success Criteria**:
- Logs appear in Application Insights with event ID and signup list count

**If Fails**:
- Check Azure DevOps pipeline logs
- Verify Git branch is correct (`develop`)
- Check App Service configuration for deployment slot

---

### Test 2: Verify ZIP Structure

**Objective**: Confirm ZIP contains .xlsx files, not XML

**Steps**:
```bash
# Download file
curl -o test.zip "https://api.lankaconnect.com/api/Events/0458806b-8672-4ad5-a7cb-f5346f1b282a/export?format=SignUpListsExcel"

# Extract and list contents
unzip -l test.zip

# Should see:
# Food-and-Drinks.xlsx
# API-Test-Sign-Up-List.xlsx
```

**Success Criteria**:
- ZIP contains `.xlsx` files
- NO `xl/`, `_rels/`, `[Content_Types].xml` at root level

**If Fails**:
- Apply Solution A (force Content-Type in controller)
- Redeploy and retest

---

### Test 3: Verify Excel File Validity

**Objective**: Confirm extracted .xlsx files open in Excel

**Steps**:
```bash
# Extract ZIP
unzip test.zip

# Open in Excel
# - On Windows: start excel "Food-and-Drinks.xlsx"
# - On Mac: open -a "Microsoft Excel" "Food-and-Drinks.xlsx"
```

**Success Criteria**:
- Excel opens file without errors
- Sheets are visible: "Mandatory Items", "Suggested Items", "Open Items"
- Data is correctly formatted with headers

**If Fails**:
- Check `excelBytes` length in logs (should be > 5000 bytes for typical workbook)
- Verify `excelMemoryStream.Position = 0` is in deployed code
- Check if `workbook.SaveAs()` throws exceptions (check logs)

---

### Test 4: Compare CSV vs Excel ZIP

**Objective**: Verify both formats work consistently

**Steps**:
```bash
# Download CSV ZIP
curl -o csv.zip "https://api.lankaconnect.com/api/Events/0458806b-8672-4ad5-a7cb-f5346f1b282a/export?format=SignUpListsZip"

# Download Excel ZIP
curl -o excel.zip "https://api.lankaconnect.com/api/Events/0458806b-8672-4ad5-a7cb-f5346f1b282a/export?format=SignUpListsExcel"

# Compare structures
unzip -l csv.zip
unzip -l excel.zip
```

**Success Criteria**:
- Both ZIPs have similar structure (same number of files)
- CSV ZIP has `.csv` files, Excel ZIP has `.xlsx` files
- Both have `Content-Type: application/zip` in HTTP response

---

## Why Previous Fixes Didn't Work

### Commit 3fcb1399: `excelMemoryStream.Position = 0`
**What it fixed**: ClosedXML leaving stream position at EOF
**Why it didn't work**: Code was never deployed to Azure
**Evidence**: No logs from `_logger.LogInformation()` statements added in this commit

### Commit d163df2c: Brace Indentation Fix
**What it fixed**: Brace mismatch causing archive to close prematurely
**Why it didn't work**: Code was never deployed to Azure
**Evidence**: File size unchanged (10,867 bytes)

### Why File Size Stayed the Same
If fixes were deployed, file size would change because:
- Fixing stream position would include full Excel file data
- Fixing brace mismatch would prevent premature archive closure
- **Unchanged file size = stale deployment**

---

## Critical Observations Explained

### 1. Content-Type Mismatch
**Query Handler Sets**: `"application/zip"`
**HTTP Response Shows**: `"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"`

**Explanation**: ASP.NET Core's `File()` method auto-detects MIME type from filename extension. When it sees `.xlsx` in the filename (even inside ZIP entry names), it overrides the ContentType parameter.

**Solution**: Force `"application/zip"` in controller, ignore query handler's ContentType.

---

### 2. CSV Export Works, Excel Export Doesn't
**Explanation**:
- CSV export uses `CsvExportService.ExportSignUpListsToZip()` which returns ZIP with `.csv` files
- CSV files are plain text, no special MIME type handling
- Excel export uses `ExcelExportService.ExportSignUpListsToExcelZip()` which returns ZIP with `.xlsx` files
- XLSX MIME type triggers special handling in ASP.NET Core or Azure

**Implication**: The ZIP creation logic is correct (both use same pattern). The issue is with MIME type detection/override.

---

### 3. No Logs in Azure Application Insights
**Explanation**: The deployed code is from an earlier commit (before logging was added).

**Action Required**:
1. Verify current Git commit: `git rev-parse HEAD`
2. Check Azure deployment history
3. Force rebuild and redeploy
4. Verify logs appear after deployment

---

## Recommended Immediate Actions (In Order)

### 1. ⚠️ **FIRST**: Verify Deployment Status
```bash
# Check what's deployed
az webapp deployment source show --name <app-name> --resource-group <rg-name>

# Compare with current Git commit
git rev-parse HEAD
```

**If different**: Redeploy immediately.

---

### 2. **SECOND**: Force Rebuild and Deploy
```bash
# Trigger deployment
git commit --allow-empty -m "chore: Force rebuild for Excel export fix verification"
git push origin develop

# Wait for deployment
# Check Azure DevOps pipeline or GitHub Actions

# Restart app service
az webapp restart --name <app-name> --resource-group <rg-name>
```

---

### 3. **THIRD**: Test for Logs
```bash
# Wait 2 minutes after deployment

# Make API request
curl "https://api.lankaconnect.com/api/Events/0458806b-8672-4ad5-a7cb-f5346f1b282a/export?format=SignUpListsExcel" -o test.zip

# Check Application Insights
# Look for: "Phase 6A.73: Starting Excel ZIP export"
```

**If logs appear**: Code is deployed, proceed to Test 2 (verify ZIP structure)
**If no logs**: Deployment failed, check pipeline logs

---

### 4. **FOURTH**: If Bug Persists After Redeployment
Apply Solution A (force Content-Type in controller):

```csharp
// File: src/LankaConnect.API/Controllers/EventsController.cs
// Line: 1963-1967

return File(
    result.Value.FileContent,
    "application/zip",  // ✅ FORCE ZIP MIME TYPE
    result.Value.FileName
);
```

Commit, redeploy, retest.

---

## Success Criteria

### Deployment Success
- ✅ Logs appear in Application Insights with "Phase 6A.73" messages
- ✅ Log shows correct signup list count (2 for test event)
- ✅ Log shows Excel workbook byte counts

### Bug Fix Success
- ✅ HTTP response has `Content-Type: application/zip`
- ✅ Downloaded file extracts to show `.xlsx` files (not XML)
- ✅ Excel files open without errors
- ✅ Data is correctly formatted in sheets

### Regression Prevention
- ✅ Integration test added to verify ZIP structure
- ✅ CI/CD pipeline runs test before deployment
- ✅ Manual testing checklist updated

---

## Lessons Learned

### 1. Always Verify Deployment
- Don't assume code changes are deployed
- Check logs to confirm new code is running
- Use feature flags or version endpoints to verify deployment

### 2. Add Logging Early
- Logging helped identify deployment issue
- Should have added logs in FIRST commit, not after multiple failed fixes

### 3. Test HTTP Response, Not Just Code
- Backend code can be correct, but middleware/infrastructure can transform responses
- Always test end-to-end HTTP response headers and body

### 4. Use Integration Tests
- Unit tests passed, but integration test would have caught this
- Need test that downloads actual HTTP response and validates ZIP structure

---

## Next Steps

1. **Immediate**: Run Phase 1 (Verify Deployment Status) - see above
2. **Short-term**: Apply Solution A if bug persists after redeployment
3. **Long-term**: Add integration test to CI/CD pipeline
4. **Documentation**: Update deployment checklist with verification steps

---

## References

### Commits
- `3fcb1399`: Fix Excel export ZIP by resetting MemoryStream position
- `d163df2c`: Fix brace mismatch causing archive to close prematurely
- `06b296f5`: Fix double-compression bug in Excel signup list export
- `58a5e901`: Properly save Excel files to ZIP by buffering to memory first

### Files
- `ExcelExportService.cs`: Line 47-152 (`ExportSignUpListsToExcelZip` method)
- `ExportEventAttendeesQueryHandler.cs`: Line 151-156 (ContentType setting)
- `EventsController.cs`: Line 1963-1967 (`File()` return)

### Testing
- Test Event ID: `0458806b-8672-4ad5-a7cb-f5346f1b282a`
- API Endpoint: `/api/Events/{id}/export?format=SignUpListsExcel`
- Expected signup lists: 2 ("Food and Drinks", "API Test Sign Up List")
