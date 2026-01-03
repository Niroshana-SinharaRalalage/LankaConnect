# Phase 6A.XX - CSV Line Break Investigation

## Issue Summary
CSV export displays all data in a single row in Excel, despite adding `charset=utf-8` to Content-Type header.

## Evidence
- Deployment e733abd7 successfully deployed with `charset=utf-8`
- Screenshot shows all data in row 1, columns A-Z
- Excel export works correctly
- Issue persists after charset fix

## Root Cause Analysis

### Incorrect Diagnosis
The charset fix was based on RFC 4180 standards, but this was NOT the actual problem.

### Actual Root Cause
**CsvHelper is writing Unix line endings (`\n`) instead of Windows line endings (`\r\n`)**

#### Evidence:
1. **CsvConfiguration missing NewLine property**:
   ```csharp
   // Current code (line 27-30 in CsvExportService.cs)
   using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
   {
       HasHeaderRecord = true
       // NewLine property is MISSING - defaults to \n
   });
   ```

2. **CsvHelper 33.0.1 defaults**:
   - Default `NewLine = "\n"` (LF only)
   - Does NOT use `Environment.NewLine`
   - Does NOT respect StreamWriter's NewLine property

3. **Why Excel fails**:
   - Excel on Windows expects `\r\n` (CRLF) for row separation
   - When it sees only `\n` (LF), it treats entire file as one row
   - Content-Type header is ignored for double-clicked files
   - BOM is present (correct) but line endings are wrong

## Solution

### Fix 1: Set NewLine in CsvConfiguration (RECOMMENDED)
```csharp
using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
{
    HasHeaderRecord = true,
    NewLine = "\r\n"  // Explicit CRLF for Excel compatibility
});
```

### Fix 2: Set StreamWriter NewLine (ALTERNATIVE)
```csharp
using var writer = new StreamWriter(memoryStream, utf8WithBom)
{
    NewLine = "\r\n"
};
```

### Fix 3: Both (SAFEST)
Set both CsvConfiguration.NewLine and StreamWriter.NewLine to ensure consistency.

## Why Previous Fix Failed

1. **charset=utf-8 in Content-Type**:
   - Only affects HTTP response header
   - Browser respects it during download
   - Excel IGNORES it when opening downloaded file
   - File is handled by Windows file association, not browser

2. **UTF-8 BOM was already correct**:
   - BOM (0xEF 0xBB 0xBF) was already being written
   - This helps Excel detect UTF-8 encoding
   - But doesn't fix line ending issue

3. **Line endings vs character encoding**:
   - Character encoding: UTF-8 (already correct)
   - Line endings: Unix `\n` (WRONG for Excel)
   - These are separate issues

## Testing Plan

### Byte-Level Verification
1. Generate CSV file
2. Check hex dump for line ending bytes:
   - Should see: `0D 0A` (CRLF) between rows
   - Currently seeing: `0A` (LF only)

### Excel Verification
1. Download CSV from API
2. Double-click to open in Excel
3. Verify rows are separated correctly
4. Check data displays in proper rows/columns

## Implementation Status
- [x] Investigation complete
- [ ] Fix implemented
- [ ] Tests created
- [ ] Deployed to staging
- [ ] Verified in production

## Related Issues
- Phase 6A.63: CSV export implementation
- Phase 6A.59: Parameter interpolation fix (separate issue)
- Deployment e733abd7: charset fix (didn't solve the problem)