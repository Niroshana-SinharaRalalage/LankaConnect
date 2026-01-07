# CSV Export Single Row Issue - Root Cause Analysis
**Date**: 2026-01-06
**Issue**: Event ID `0458806b-8672-4ad5-a7cb-f5346f1b282a` CSV export shows all data in cell A1 instead of proper rows
**Status**: IN PROGRESS - Deep RCA

---

## Problem Statement

### Issue Description
CSV export for event attendees downloads successfully but displays incorrectly in Excel:
- **Expected**: Data in proper rows and columns
- **Actual**: All data appears in a single cell (A1) as one long string
- **Evidence**: Screenshot shows `RegistrationId,MainAttendee,AdditionalAttendees,...` all in cell A1

### User Impact
- Organizers cannot view/analyze attendee data in Excel
- Export feature appears broken despite passing all unit tests
- Manual workaround required (open in text editor, re-import)

### Reproduction
1. Navigate to Event Management > Event ID `0458806b-8672-4ad5-a7cb-f5346f1b282a`
2. Click "Export Attendees" > "CSV format"
3. Download file
4. Open in Microsoft Excel (Windows/Mac)
5. Observe: All data in cell A1

---

## Technical Context

### Current Implementation

#### Backend CSV Generation
**File**: `src/LankaConnect.Infrastructure/Services/Export/CsvExportService.cs`
**Lines**: 14-74

```csharp
public byte[] ExportEventAttendees(EventAttendeesResponse attendees)
{
    // Phase 6A.66: Build CSV manually exactly like signup list export
    // UTF-8 BOM for Excel compatibility
    var csv = new StringBuilder();
    csv.Append('\uFEFF'); // UTF-8 BOM

    // Header row
    csv.Append("RegistrationId,MainAttendee,AdditionalAttendees,TotalAttendees,Adults,Children,MaleCount,FemaleCount,GenderDistribution,Email,Phone,Address,PaymentStatus,TotalAmount,Currency,TicketCode,QRCode,RegistrationDate,Status\r\n");

    // Data rows
    foreach (var a in attendees.Attendees)
    {
        var mainAttendee = a.Attendees.FirstOrDefault()?.Name ?? "Unknown";
        var additionalAttendees = a.TotalAttendees > 1
            ? string.Join(", ", a.Attendees.Skip(1).Select(att => att.Name))
            : "";
        var maleCount = a.Attendees.Count(att => att.Gender == Domain.Events.Enums.Gender.Male);
        var femaleCount = a.Attendees.Count(att => att.Gender == Domain.Events.Enums.Gender.Female);
        var genderDistribution = GetGenderDistribution(a.Attendees);

        // Build row - quote fields that need it
        csv.Append($"\"{a.RegistrationId}\",");
        csv.Append($"\"{mainAttendee}\",");
        csv.Append($"\"{additionalAttendees}\",");
        csv.Append($"{a.TotalAttendees},");
        csv.Append($"{a.AdultCount},");
        csv.Append($"{a.ChildCount},");
        csv.Append($"{maleCount},");
        csv.Append($"{femaleCount},");
        csv.Append($"\"{genderDistribution}\",");
        csv.Append($"\"{a.ContactEmail}\",");
        csv.Append($"\"{a.ContactPhone ?? ""}\",");
        csv.Append($"\"{a.ContactAddress ?? ""}\",");
        csv.Append($"\"{a.PaymentStatus}\",");
        csv.Append($"\"{a.TotalAmount?.ToString("F2") ?? ""}\",");
        csv.Append($"\"{a.Currency ?? ""}\",");
        csv.Append($"\"{a.TicketCode ?? ""}\",");
        csv.Append($"\"{a.QrCodeData ?? ""}\",");
        csv.Append($"\"{a.CreatedAt:yyyy-MM-dd HH:mm:ss}\",");
        csv.Append($"\"{a.Status}\"");
        csv.Append("\r\n"); // Windows line ending
    }

    return Encoding.UTF8.GetBytes(csv.ToString());
}
```

**Key Points**:
- ✅ UTF-8 BOM (`\uFEFF`) for Excel compatibility
- ✅ Explicit `\r\n` (CRLF) line endings
- ✅ Manual CSV building (not using CsvHelper)
- ✅ Returns `Encoding.UTF8.GetBytes()`

#### HTTP Response (Query Handler)
**File**: `src/LankaConnect.Application/Events/Queries/ExportEventAttendees/ExportEventAttendeesQueryHandler.cs`
**Lines**: 105-108

```csharp
fileContent = _csvService.ExportEventAttendees(attendeesResponse);
fileName = $"event-{request.EventId}-attendees-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
contentType = "text/csv; charset=utf-8";
```

**Key Points**:
- ✅ Content-Type includes `charset=utf-8`
- ✅ Filename has `.csv` extension
- ✅ Byte array passed directly to controller

#### Controller Response
**File**: `src/LankaConnect.API/Controllers/EventsController.cs`
**Lines**: 1935-1939

```csharp
return File(
    result.Value.FileContent,
    result.Value.ContentType,
    result.Value.FileName
);
```

**Key Points**:
- ✅ Uses ASP.NET Core `File()` helper
- ✅ Sets proper Content-Type header
- ✅ Sets Content-Disposition with filename

#### Frontend Download (React)
**File**: `web/src/presentation/components/features/events/AttendeeManagementTab.tsx`
**Lines**: 170-191

```typescript
const handleExport = async (format: 'excel' | 'csv') => {
  try {
    setIsExporting(true);
    const blob = await exportMutation.mutateAsync({ eventId, format });

    // Create download link
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `event-${eventId}-attendees.${format === 'excel' ? 'xlsx' : 'csv'}`;
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  } catch (err) {
    console.error('Export failed:', err);
    alert('Failed to export attendees. Please try again.');
  } finally {
    setIsExporting(false);
  }
};
```

**Key Points**:
- ✅ Creates blob from API response
- ✅ Uses `URL.createObjectURL()` for download
- ✅ Sets proper filename with `.csv` extension
- ✅ Triggers download via `link.click()`

#### Working Reference (Signup List Export)
**File**: `web/src/presentation/components/features/events/SignUpListsTab.tsx`
**Lines**: 29-91

```typescript
const handleDownloadCSV = () => {
    // Build CSV content with UTF-8 BOM for Excel compatibility
    const BOM = '\uFEFF';
    let csvContent = BOM + 'Category,Item Description,User ID,Quantity,Committed At\n';

    signUpLists.forEach((list) => {
      (list.items || []).forEach((item) => {
        (item.commitments || []).forEach((commitment) => {
          const userId = commitment.userId || '';
          const quantity = commitment.quantity || 0;
          const committedAt = commitment.committedAt
            ? new Date(commitment.committedAt).toLocaleString()
            : '';

          csvContent += `"${list.category}","${item.itemDescription}","${userId}",${quantity},"${committedAt}"\n`;
          rowCount++;
        });
      });
    });

    // Create download link with proper MIME type
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);

    link.setAttribute('href', url);
    link.setAttribute('download', `event-${eventId}-signups.csv`);
    link.style.visibility = 'hidden';

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};
```

**Key Differences**:
1. **Line Endings**: Signup list uses `\n` (LF), Attendees uses `\r\n` (CRLF)
2. **Generation**: Signup list is CLIENT-SIDE, Attendees is SERVER-SIDE
3. **Blob Creation**: Signup list creates blob in browser, Attendees receives blob from API
4. **Content-Type**: Signup list uses `text/csv;charset=utf-8;`, Attendees uses API response type

---

## Test Coverage

### Unit Tests (ALL PASSING)
**File**: `tests/LankaConnect.Infrastructure.Tests/Services/Export/CsvExportServiceLineEndingTests.cs`

**Tests**:
1. `ExportEventAttendees_Should_UseWindowsLineEndings_ForExcelCompatibility` ✅
   - Verifies `\r\n` present in output
   - Confirms no standalone `\n` without `\r`
   - Validates row separation

2. `ExportEventAttendees_Should_StartWithUtf8Bom` ✅
   - Confirms UTF-8 BOM (0xEF 0xBB 0xBF) at byte level
   - Ensures Excel can detect UTF-8 encoding

3. `ExportEventAttendees_Should_HaveCorrectByteSequenceForLineEndings` ✅
   - Byte-level verification of CRLF (0x0D 0x0A)
   - Ensures no standalone LF bytes

4. `ExportEventAttendees_WithMultipleRows_Should_SeparateEachRowWithCrlf` ✅
   - Tests with 5 rows of data
   - Validates each row properly separated

**Status**: All tests passing, but issue persists in production.

---

## Analysis Matrix

### Issue Type Hypotheses

| Hypothesis | Likelihood | Evidence | Status |
|-----------|-----------|----------|---------|
| 1. Backend serialization corrupts CRLF | 🔴 HIGH | Tests pass but real data fails | INVESTIGATING |
| 2. HTTP transmission strips/alters line endings | 🟡 MEDIUM | Possible middleware interference | INVESTIGATING |
| 3. Frontend blob handling corrupts data | 🟡 MEDIUM | Different from working signup list | INVESTIGATING |
| 4. Content-Type header interpretation | 🟡 MEDIUM | Excel behavior depends on MIME type | INVESTIGATING |
| 5. Platform-specific Excel bug (Windows vs Mac) | 🟢 LOW | Would affect more users | INVESTIGATING |
| 6. Character encoding issue (UTF-8 vs Windows-1252) | 🔴 HIGH | Excel encoding detection is fragile | INVESTIGATING |

### Critical Questions

#### Q1: Is the CSV correctly formatted at the backend?
**Test**: Need to capture RAW HTTP response bytes

**Action Required**:
```bash
# Network trace to capture actual bytes transmitted
curl -H "Authorization: Bearer {token}" \
  "https://staging.lankaconnect.com/api/events/0458806b-8672-4ad5-a7cb-f5346f1b282a/export?format=csv" \
  -o attendees.csv --trace-ascii trace.txt

# Hex dump to see actual byte sequence
xxd attendees.csv | head -50
```

**Expected**:
```
0000000: efbb bf52 6567 6973 7472 6174 696f 6e49  ...RegistrationI
0000010: 642c 4d61 696e 4174 7465 6e64 6565 2c2e  d,MainAttendee,.
...
000000X: 0d0a                                      ..  (CRLF)
```

#### Q2: Is the frontend blob correctly handling the data?
**Test**: Browser console inspection

**Action Required**:
```javascript
// In browser console during export
const blob = await exportMutation.mutateAsync({ eventId, format: 'csv' });
const text = await blob.text();
console.log('Blob size:', blob.size);
console.log('Blob type:', blob.type);
console.log('First 500 chars:', text.substring(0, 500));
console.log('CRLF count:', (text.match(/\r\n/g) || []).length);
console.log('LF count:', (text.match(/\n/g) || []).length);
```

**Expected**:
- Blob type: `text/csv; charset=utf-8`
- CRLF count: Number of rows
- LF count: Same as CRLF count (no standalone LF)

#### Q3: What does Excel receive vs what we send?
**Test**: Save file, open in hex editor

**Action Required**:
1. Download CSV from browser
2. Open in hex editor (HxD, xxd, or VS Code hex extension)
3. Verify bytes match backend output
4. Check for BOM (EF BB BF)
5. Check for CRLF (0D 0A)

#### Q4: Does the issue affect Excel only or other CSV viewers?
**Test**: Open in multiple applications

**Action Required**:
1. Excel (Windows)
2. Excel (Mac)
3. Google Sheets (import CSV)
4. LibreOffice Calc
5. Text editor (Notepad++, VS Code)
6. `cat` command in terminal

**Expected**: If only Excel fails, it's an Excel interpretation issue.

#### Q5: Is this a content-type vs file extension issue?
**Test**: Try different MIME types

**Action Required**:
1. Test with `text/csv`
2. Test with `text/csv; charset=utf-8`
3. Test with `application/csv`
4. Test with `text/plain`

**Expected**: Excel behavior may change based on Content-Type.

---

## Differential Diagnosis

### Why Signup List Works vs Attendees Fails

#### Signup List Export (WORKING)
```typescript
// CLIENT-SIDE generation
csvContent += `"${data}","${data}"\n`;  // LF only
const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
URL.createObjectURL(blob);
```

#### Attendees Export (FAILING)
```csharp
// SERVER-SIDE generation
csv.Append("\r\n");  // CRLF
return Encoding.UTF8.GetBytes(csv.ToString());
```

**Key Difference**: Signup list uses `\n` (LF) and works. Attendees uses `\r\n` (CRLF) and fails.

**Hypothesis**: Excel on user's platform expects LF, not CRLF, OR there's corruption during transmission.

---

## Root Cause Hypotheses (Prioritized)

### Hypothesis 1: HTTP Response Encoding Corruption (CRITICAL)
**Likelihood**: 🔴 HIGH

**Theory**:
- ASP.NET Core `File()` method may alter bytes during transmission
- Possible middleware (compression, CORS, etc.) corrupting line endings
- HTTP chunked transfer encoding affecting CRLF sequences

**Evidence**:
- Tests pass with in-memory byte array
- Production fails with HTTP transmission
- Similar issue documented in ASP.NET Core GitHub issues

**Test**:
```csharp
// Add logging in controller BEFORE returning File()
var hexDump = BitConverter.ToString(result.Value.FileContent.Take(100).ToArray());
Logger.LogInformation("CSV bytes before File(): {Hex}", hexDump);
```

**Fix** (if confirmed):
```csharp
// Option 1: Force binary transfer
return File(result.Value.FileContent, "application/octet-stream", result.Value.FileName);

// Option 2: Disable response buffering
Response.Headers.Add("Content-Transfer-Encoding", "binary");
return File(result.Value.FileContent, result.Value.ContentType, result.Value.FileName);
```

### Hypothesis 2: Content-Type Header Triggers Excel to Interpret as Single Cell (CRITICAL)
**Likelihood**: 🔴 HIGH

**Theory**:
- Excel uses Content-Type to decide parsing mode
- `text/csv; charset=utf-8` may trigger different parser than `text/csv`
- Excel may ignore CRLF when charset is specified in header

**Evidence**:
- Signup list (client-side) works with `text/csv;charset=utf-8;`
- Attendees (server-side) fails with `text/csv; charset=utf-8`
- Subtle difference: Semicolon spacing

**Test**:
```csharp
// Try WITHOUT charset
contentType = "text/csv";
```

**Fix** (if confirmed):
```csharp
// Match signup list format EXACTLY
contentType = "text/csv;charset=utf-8;";  // Note trailing semicolon
```

### Hypothesis 3: UTF-8 BOM + CRLF Interaction (MEDIUM)
**Likelihood**: 🟡 MEDIUM

**Theory**:
- UTF-8 BOM (`\uFEFF`) may interfere with CRLF interpretation
- Excel may not recognize line endings after BOM
- Some Excel versions sensitive to BOM position

**Evidence**:
- Signup list has BOM and works (but uses LF, not CRLF)
- Attendees has BOM and fails (uses CRLF)
- BOM + CRLF combination is the unique factor

**Test**:
```csharp
// Remove BOM temporarily
var csv = new StringBuilder();
// csv.Append('\uFEFF');  // COMMENT OUT
csv.Append("RegistrationId,MainAttendee...");
```

**Fix** (if confirmed):
```csharp
// Option 1: Remove BOM entirely
// Option 2: Use LF instead of CRLF (match signup list)
csv.Append("\n");
```

### Hypothesis 4: Excel's Encoding Detection Failure (MEDIUM)
**Likelihood**: 🟡 MEDIUM

**Theory**:
- Excel fails to detect UTF-8 encoding despite BOM and Content-Type
- Excel interprets bytes as Windows-1252 (default encoding)
- CRLF bytes (0x0D 0x0A) in Windows-1252 may render as special characters instead of line breaks

**Evidence**:
- User sees literal text instead of line breaks (common with encoding mismatch)
- Excel's auto-detection is notoriously fragile
- Issue would not appear in text editors (they handle UTF-8 correctly)

**Test**:
```
# Open file in Excel with explicit encoding:
1. Excel > File > Open
2. Select "CSV" file type
3. Choose "Delimited" > Next
4. Select "UTF-8" encoding
5. Choose "Comma" delimiter
```

**Fix** (if confirmed):
```csharp
// Force UTF-8 with BOM more aggressively
var utf8Bom = new byte[] { 0xEF, 0xBB, 0xBF };
var csvBytes = Encoding.UTF8.GetBytes(csv.ToString());
var result = new byte[utf8Bom.Length + csvBytes.Length];
Array.Copy(utf8Bom, result, utf8Bom.Length);
Array.Copy(csvBytes, 0, result, utf8Bom.Length, csvBytes.Length);
return result;
```

### Hypothesis 5: Frontend Blob Type Mismatch (MEDIUM)
**Likelihood**: 🟡 MEDIUM

**Theory**:
- Axios receives `text/csv` but creates blob with wrong type
- Blob type doesn't match Content-Type from server
- Browser downloads file with incorrect metadata

**Evidence**:
- Signup list explicitly sets blob type in browser
- Attendees relies on API response Content-Type
- Different blob creation mechanisms

**Test**:
```typescript
// Log actual blob type
const blob = await exportMutation.mutateAsync({ eventId, format: 'csv' });
console.log('Blob type from API:', blob.type);
console.log('Blob size:', blob.size);

// Manually recreate blob with correct type
const text = await blob.text();
const newBlob = new Blob([text], { type: 'text/csv;charset=utf-8;' });
const url = URL.createObjectURL(newBlob);
```

**Fix** (if confirmed):
```typescript
// Force blob type to match signup list
const blob = await exportMutation.mutateAsync({ eventId, format: 'csv' });
const correctedBlob = new Blob([await blob.arrayBuffer()], {
  type: 'text/csv;charset=utf-8;'
});
const url = URL.createObjectURL(correctedBlob);
```

### Hypothesis 6: Line Ending Normalization in HTTP Stack (LOW)
**Likelihood**: 🟢 LOW

**Theory**:
- Reverse proxy (nginx, IIS, Azure App Gateway) normalizing line endings
- HTTP compression (gzip) affecting binary data
- .NET Core middleware altering text responses

**Evidence**:
- Would affect all CSV exports (but signup list works)
- Tests pass locally (no HTTP transmission)
- Issue only in production environment

**Test**:
```bash
# Compare local vs deployed
curl http://localhost:5000/api/events/{id}/export?format=csv -o local.csv
curl https://staging.lankaconnect.com/api/events/{id}/export?format=csv -o remote.csv
diff <(xxd local.csv) <(xxd remote.csv)
```

**Fix** (if confirmed):
```csharp
// In Program.cs or Startup.cs
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/events") &&
        context.Request.Query.ContainsKey("format"))
    {
        context.Response.Headers.Add("Content-Encoding", "identity");
    }
    await next();
});
```

---

## Recommended Investigation Plan

### Phase 1: Capture Raw Data (30 minutes)
**Objective**: Determine WHERE the corruption occurs

**Tasks**:
1. ✅ Backend: Add hex dump logging in `ExportEventAttendeesQueryHandler`
2. ✅ HTTP: Capture raw response with `curl` and `xxd`
3. ✅ Frontend: Log blob in browser console
4. ✅ File: Download and inspect in hex editor
5. ✅ Compare: Diff all 4 stages to find where CRLF disappears

**Success Criteria**: Identify exact point where line endings are lost/corrupted.

### Phase 2: Test Hypotheses (1 hour)
**Objective**: Validate root cause with targeted experiments

**Tasks**:
1. ✅ Test Content-Type variations (without charset, different format)
2. ✅ Test without UTF-8 BOM
3. ✅ Test with LF instead of CRLF (match signup list)
4. ✅ Test frontend blob type override
5. ✅ Test Excel import wizard (manual encoding selection)

**Success Criteria**: One change makes Excel display correctly.

### Phase 3: Implement Durable Fix (1 hour)
**Objective**: Fix root cause, not symptoms

**Tasks**:
1. ✅ Apply validated fix from Phase 2
2. ✅ Update unit tests to match new implementation
3. ✅ Test with actual event data (Event ID `0458806b...`)
4. ✅ Cross-platform testing (Windows Excel, Mac Excel, Google Sheets)
5. ✅ Document decision in ADR

**Success Criteria**: CSV opens correctly in Excel with proper rows/columns.

### Phase 4: Verification (30 minutes)
**Objective**: Ensure fix doesn't break anything else

**Tasks**:
1. ✅ Run all unit tests (CSV export + line ending tests)
2. ✅ Test Excel export (should still work)
3. ✅ Test signup list CSV (should still work)
4. ✅ Test on staging environment
5. ✅ Deploy to production

**Success Criteria**: All exports work correctly, no regressions.

---

## Next Steps

### Immediate Actions (DO NOW)
1. **Capture Raw HTTP Response**
   ```bash
   curl -H "Authorization: Bearer {token}" \
     "https://staging.lankaconnect.com/api/events/0458806b-8672-4ad5-a7cb-f5346f1b282a/export?format=csv" \
     -o attendees.csv --trace-ascii trace.txt
   xxd attendees.csv | head -100 > hex_dump.txt
   ```

2. **Log Frontend Blob**
   ```javascript
   // Add to AttendeeManagementTab.tsx
   const blob = await exportMutation.mutateAsync({ eventId, format: 'csv' });
   const text = await blob.text();
   console.log('CRLF check:', text.includes('\r\n'));
   console.log('First 500:', text.substring(0, 500));
   ```

3. **Try Quick Fix (LF instead of CRLF)**
   ```csharp
   // In CsvExportService.cs, change:
   csv.Append("\r\n");  // CRLF
   // TO:
   csv.Append("\n");     // LF (match signup list)
   ```

### If Quick Fix Works
1. Update unit tests to expect LF instead of CRLF
2. Document why LF works better than CRLF for browser downloads
3. Deploy to staging and verify
4. Create ADR documenting decision

### If Quick Fix Fails
1. Proceed with full Phase 1-4 investigation
2. Capture data at every stage of the pipeline
3. Methodically test each hypothesis
4. Report findings in updated RCA

---

## Key Insights

### Why This Is Hard to Debug
1. **Unit tests pass**: In-memory byte array is correct
2. **HTTP transmission**: Something changes between backend and frontend
3. **Excel-specific**: Other apps (text editors, Google Sheets) may work fine
4. **Encoding complexity**: UTF-8 BOM + CRLF + HTTP headers + Excel heuristics = many variables
5. **Platform differences**: Windows vs Mac Excel behave differently

### Why Signup List Works
1. **Client-side generation**: No HTTP transmission to corrupt data
2. **Simple LF**: No CRLF complexity
3. **Explicit blob type**: Browser knows exactly how to handle it

### Critical Difference
**Signup List**: Browser → Blob → Download (all client-side)
**Attendees**: Backend → HTTP → Axios → Blob → Download (crosses network boundary)

**Hypothesis**: The network boundary (HTTP transmission) is where corruption occurs.

---

## Files to Review/Modify

### Backend (C#)
- `src/LankaConnect.Infrastructure/Services/Export/CsvExportService.cs` (CSV generation)
- `src/LankaConnect.Application/Events/Queries/ExportEventAttendees/ExportEventAttendeesQueryHandler.cs` (HTTP response)
- `src/LankaConnect.API/Controllers/EventsController.cs` (File() return)

### Frontend (TypeScript)
- `web/src/presentation/components/features/events/AttendeeManagementTab.tsx` (Export handler)
- `web/src/infrastructure/api/repositories/events.repository.ts` (API client)
- `web/src/infrastructure/api/client/apiClient.ts` (Axios configuration)

### Tests
- `tests/LankaConnect.Infrastructure.Tests/Services/Export/CsvExportServiceLineEndingTests.cs` (Unit tests)

### Documentation
- `docs/CSV_EXPORT_ROOT_CAUSE_ANALYSIS.md` (This file)
- `docs/PHASE_6A_MASTER_INDEX.md` (Phase tracking)
- `docs/PROGRESS_TRACKER.md` (Status updates)

---

## Status: INVESTIGATION IN PROGRESS

**Next Update**: After Phase 1 data capture is complete.
