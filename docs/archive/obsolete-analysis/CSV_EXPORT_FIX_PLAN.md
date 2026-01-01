# CSV Export Fix Implementation Plan

**Date**: 2025-12-25
**Phase**: Phase 6A.48 (CSV Export Fixes)
**Dependencies**: See CSV_EXPORT_RCA.md
**Priority**: CRITICAL (Signups), HIGH (Attendees)

## Overview

This document provides a comprehensive fix plan for two CSV export issues identified in Phase 6A.45's Attendee Management feature.

---

## Fix 1: Signups CSV Export (CRITICAL)

### Problem
CSV file contains headers only, no data rows. Root cause: Incorrect data structure traversal.

### Solution: Correct Data Structure Iteration

#### File to Modify
`web/src/presentation/components/features/events/SignUpListsTab.tsx`

#### Current Implementation (Lines 29-57)
```typescript
const handleDownloadCSV = () => {
  if (!signUpLists || signUpLists.length === 0) {
    alert('No sign-up lists to download');
    return;
  }

  // Build CSV content
  let csvContent = 'Category,Item Description,User ID,Quantity,Committed At\n';

  signUpLists.forEach((list) => {
    (list.commitments || []).forEach((commitment) => {  // ❌ WRONG
      csvContent += `"${list.category}","${commitment.itemDescription}","${commitment.userId}",${commitment.quantity},"${commitment.committedAt}"\n`;
    });
  });

  // Create download link
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

#### Corrected Implementation
```typescript
const handleDownloadCSV = () => {
  if (!signUpLists || signUpLists.length === 0) {
    alert('No sign-up lists to download');
    return;
  }

  // Build CSV content with UTF-8 BOM for Excel compatibility
  const BOM = '\uFEFF';
  let csvContent = BOM + 'Category,Item Description,User ID,Quantity,Committed At\n';

  let rowCount = 0;

  signUpLists.forEach((list) => {
    // ✅ CORRECTED: Iterate through Items[], then nested Commitments[]
    (list.items || []).forEach((item) => {
      (item.commitments || []).forEach((commitment) => {
        // Format phone numbers and dates properly
        const userId = commitment.userId || '';
        const quantity = commitment.quantity || 0;
        const committedAt = commitment.committedAt
          ? new Date(commitment.committedAt).toLocaleString()
          : '';

        csvContent += `"${list.category}","${item.itemDescription}","${userId}",${quantity},"${committedAt}"\n`;
        rowCount++;
      });
    });

    // ✅ BACKWARD COMPATIBILITY: Also check legacy commitments[] if Items[] is empty
    if ((list.items || []).length === 0 && (list.commitments || []).length > 0) {
      (list.commitments || []).forEach((commitment) => {
        const userId = commitment.userId || '';
        const quantity = commitment.quantity || 0;
        const committedAt = commitment.committedAt
          ? new Date(commitment.committedAt).toLocaleString()
          : '';

        csvContent += `"${list.category}","${commitment.itemDescription}","${userId}",${quantity},"${committedAt}"\n`;
        rowCount++;
      });
    }
  });

  // Validate data exists before download
  if (rowCount === 0) {
    alert('No commitments found to export');
    return;
  }

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

#### Key Changes
1. ✅ **Correct nesting**: `list.items[].commitments[]` instead of `list.commitments[]`
2. ✅ **UTF-8 BOM**: Prepend `\uFEFF` for Excel UTF-8 detection
3. ✅ **Backward compatibility**: Support both legacy and new data structures
4. ✅ **Row count validation**: Alert if no data to export
5. ✅ **Date formatting**: Convert ISO dates to readable format

---

## Fix 2: Attendees CSV Export - UTF-8 BOM

### Problem
Excel misinterprets UTF-8 encoding, showing special characters incorrectly (â€" instead of —).

### Solution: Add UTF-8 BOM to Backend CSV Generation

#### File to Modify
`src/LankaConnect.Infrastructure/Services/Export/CsvExportService.cs`

#### Current Implementation (Lines 16-54)
```csharp
public byte[] ExportEventAttendees(EventAttendeesResponse attendees)
{
    using var memoryStream = new MemoryStream();
    using var writer = new StreamWriter(memoryStream, Encoding.UTF8);  // ❌ No BOM
    using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true
    });

    // Define flattened records for CSV export
    var records = attendees.Attendees.Select(a => new
    {
        RegistrationId = a.RegistrationId,
        MainAttendee = a.MainAttendeeName,
        AdditionalAttendees = a.AdditionalAttendees,
        // ... more fields
    });

    csv.WriteRecords(records);
    writer.Flush();
    return memoryStream.ToArray();
}
```

#### Corrected Implementation
```csharp
public byte[] ExportEventAttendees(EventAttendeesResponse attendees)
{
    using var memoryStream = new MemoryStream();

    // ✅ FIXED: Write UTF-8 BOM for Excel compatibility
    var utf8WithBom = new UTF8Encoding(true); // true = include BOM
    using var writer = new StreamWriter(memoryStream, utf8WithBom);

    using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        ShouldQuote = args => true  // ✅ Force quote all fields (handles phone numbers)
    });

    // Define flattened records for CSV export
    var records = attendees.Attendees.Select(a => new
    {
        RegistrationId = a.RegistrationId.ToString(),
        MainAttendee = a.MainAttendeeName,
        AdditionalAttendees = a.AdditionalAttendees,
        TotalAttendees = a.TotalAttendees,
        Adults = a.AdultCount,
        Children = a.ChildCount,
        GenderDistribution = a.GenderDistribution,
        Email = a.ContactEmail,
        Phone = FormatPhoneNumber(a.ContactPhone),  // ✅ Format as string with prefix
        Address = a.ContactAddress ?? string.Empty,  // ✅ Use empty string instead of em dash
        PaymentStatus = a.PaymentStatus.ToString(),
        TotalAmount = a.TotalAmount?.ToString("F2") ?? string.Empty,
        Currency = a.Currency ?? string.Empty,
        TicketCode = a.TicketCode ?? string.Empty,
        QRCode = a.QrCodeData ?? string.Empty,
        RegistrationDate = a.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        Status = a.Status.ToString()
    });

    csv.WriteRecords(records);
    writer.Flush();

    return memoryStream.ToArray();
}

/// <summary>
/// Format phone number as string to prevent Excel scientific notation
/// </summary>
private static string FormatPhoneNumber(string? phone)
{
    if (string.IsNullOrWhiteSpace(phone))
        return string.Empty;

    // Prepend with single quote to force Excel to treat as text
    // Alternative: Could use tab character prefix: "\t" + phone
    return "'" + phone;
}
```

#### Key Changes
1. ✅ **UTF8Encoding(true)**: Includes BOM (0xEF 0xBB 0xBF) at start of file
2. ✅ **ShouldQuote = args => true**: Forces quotes around ALL fields
3. ✅ **Phone number formatting**: Prepend apostrophe to force text interpretation
4. ✅ **Empty strings instead of em dash**: Avoid encoding issues
5. ✅ **GUID to string conversion**: Ensure proper formatting

---

## Fix 3: Frontend Blob MIME Type Validation (OPTIONAL ENHANCEMENT)

### Problem
Axios creates generic Blob without preserving Content-Type from HTTP response headers.

### Solution: Extract Content-Type and Create Typed Blob

#### File to Modify
`web/src/infrastructure/api/client/api-client.ts`

#### Add After Line 299
```typescript
/**
 * GET request with blob response and proper MIME type preservation
 * Extracts Content-Type from response headers
 */
public async getBlob(url: string, config?: AxiosRequestConfig): Promise<{ blob: Blob; filename?: string }> {
  const response = await this.axiosInstance.get(url, {
    ...config,
    responseType: 'blob'
  });

  // Extract Content-Type from response headers
  const contentType = response.headers['content-type'] || 'application/octet-stream';

  // Extract filename from Content-Disposition header if present
  const contentDisposition = response.headers['content-disposition'];
  let filename: string | undefined;

  if (contentDisposition) {
    const filenameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
    if (filenameMatch && filenameMatch[1]) {
      filename = filenameMatch[1].replace(/['"]/g, '');
    }
  }

  // Create properly typed Blob
  const blob = new Blob([response.data], { type: contentType });

  return { blob, filename };
}
```

#### Update Repository Method
`web/src/infrastructure/api/repositories/events.repository.ts` (Lines 864-869)

```typescript
async exportEventAttendees(eventId: string, format: 'excel' | 'csv' = 'excel'): Promise<Blob> {
  const { blob } = await apiClient.getBlob(
    `${this.basePath}/${eventId}/export?format=${format}`
  );
  return blob;
}
```

---

## Testing Checklist

### Signups CSV Export Testing

#### Test Case 1: New Category-Based Sign-Ups
**Setup**:
1. Create event with sign-up list
2. Add items with Mandatory/Preferred/Suggested categories
3. Have users commit to items

**Expected CSV Output**:
```
Category,Item Description,User ID,Quantity,Committed At
"Main Dishes","Chicken Biryani","user-123",2,"2025-12-25 10:30:00"
"Beverages","Soft Drinks","user-456",5,"2025-12-25 11:15:00"
```

**Validation**:
- [ ] Headers present
- [ ] Data rows present (at least 1)
- [ ] All columns populated correctly
- [ ] Date formatted as readable string
- [ ] Opens correctly in Excel
- [ ] UTF-8 characters display correctly

#### Test Case 2: Legacy Open Sign-Ups
**Setup**:
1. Use event with old-style open sign-up lists
2. Commitments at list level (not item level)

**Expected CSV Output**:
```
Category,Item Description,User ID,Quantity,Committed At
"Potluck","Vegetable Salad","user-789",1,"2025-12-24 14:00:00"
```

**Validation**:
- [ ] Legacy structure supported
- [ ] Data exports correctly
- [ ] No duplicate rows

#### Test Case 3: Empty Sign-Up Lists
**Setup**:
1. Event with sign-up lists but no commitments

**Expected Behavior**:
- [ ] Alert: "No commitments found to export"
- [ ] No CSV download triggered

#### Test Case 4: Mixed Structure
**Setup**:
1. Event with both new category-based items AND legacy commitments

**Expected CSV Output**:
- [ ] Both types included in export
- [ ] No duplicates
- [ ] Correct row count

### Attendees CSV Export Testing

#### Test Case 1: Special Characters
**Setup**:
1. Registration with address containing: em dash (—), accented characters (é, ñ)

**Expected Result**:
- [ ] Opens in Excel with correct character display
- [ ] No mojibake (â€" or similar)
- [ ] UTF-8 BOM present in file (check hex editor: starts with EF BB BF)

#### Test Case 2: Phone Numbers
**Setup**:
1. Registration with 10-digit phone: "8629430943"
2. Registration with international: "+94112345678"

**Expected CSV Cell Values**:
```
'8629430943       (with leading apostrophe, displays as text)
'+94112345678     (preserves plus sign)
```

**Validation**:
- [ ] No scientific notation in Excel
- [ ] Phone displays as text (left-aligned in Excel)
- [ ] Leading zeros preserved (if applicable)

#### Test Case 3: Multiple Attendees
**Setup**:
1. Registration with 5 attendees (3 adults, 2 children)
2. Mixed genders

**Expected Output**:
```
MainAttendee,AdditionalAttendees,TotalAttendees,Adults,Children,GenderDistribution
"John Doe","Jane Doe, Tim Smith, Sarah Connor, Mike Ross",5,3,2,"3 Male, 2 Female"
```

**Validation**:
- [ ] Main attendee in separate column
- [ ] Additional attendees comma-separated in quotes
- [ ] Counts accurate
- [ ] Gender distribution formatted correctly

#### Test Case 4: Empty Fields
**Setup**:
1. Registration with missing address, phone

**Expected Output**:
```
Phone,Address
"",""
```

**Validation**:
- [ ] Empty strings, not "—" or null
- [ ] No encoding issues

---

## CSV Standards Compliance (RFC 4180)

### Required Standards
1. ✅ **Line breaks**: CRLF (\r\n) for Windows compatibility
2. ✅ **Field delimiters**: Comma (,)
3. ✅ **Quote character**: Double quote (")
4. ✅ **Escape quotes**: Double double-quotes ("")
5. ✅ **Header row**: First row contains field names
6. ✅ **Encoding**: UTF-8 with BOM for Excel compatibility

### CsvHelper Configuration Verification
```csharp
new CsvConfiguration(CultureInfo.InvariantCulture)
{
    HasHeaderRecord = true,           // ✅ RFC 4180
    Delimiter = ",",                   // ✅ Default, can specify explicitly
    Quote = '"',                       // ✅ Default
    Encoding = Encoding.UTF8,          // ✅ With BOM
    ShouldQuote = args => true,        // ✅ NEW: Quote all fields
    NewLine = "\r\n"                   // ✅ Windows line endings
}
```

---

## Implementation Phases

### Phase 6A.48A: Signups CSV Fix (CRITICAL)
**Priority**: P0 - Blocking
**Estimated Effort**: 30 minutes
**Files Changed**: 1

**Tasks**:
1. Update `SignUpListsTab.tsx` with corrected data iteration
2. Add UTF-8 BOM
3. Add row count validation
4. Test with sample data

**Acceptance Criteria**:
- [ ] CSV contains data rows (not just headers)
- [ ] All commitments exported correctly
- [ ] Backward compatibility with legacy structure
- [ ] UTF-8 characters display correctly in Excel

### Phase 6A.48B: Attendees CSV Encoding Fix (HIGH)
**Priority**: P1 - Important
**Estimated Effort**: 20 minutes
**Files Changed**: 1

**Tasks**:
1. Update `CsvExportService.cs` to use UTF8Encoding(true)
2. Add ShouldQuote configuration
3. Implement FormatPhoneNumber helper
4. Replace em dashes with empty strings
5. Test with special characters

**Acceptance Criteria**:
- [ ] UTF-8 BOM present in generated CSV
- [ ] Special characters (—, é, ñ) display correctly in Excel
- [ ] Phone numbers display as text (no scientific notation)
- [ ] All fields properly quoted

### Phase 6A.48C: Blob MIME Type Enhancement (OPTIONAL)
**Priority**: P2 - Nice to have
**Estimated Effort**: 45 minutes
**Files Changed**: 2

**Tasks**:
1. Add `getBlob()` method to ApiClient
2. Update repository to use new method
3. Extract Content-Type from response headers
4. Create typed Blob
5. Test with both CSV and Excel exports

**Acceptance Criteria**:
- [ ] Blob has correct MIME type
- [ ] Content-Type preserved from HTTP response
- [ ] Filename extracted from Content-Disposition
- [ ] No regression in existing functionality

---

## Deployment Plan

### Pre-Deployment
1. Review RCA document (CSV_EXPORT_RCA.md)
2. Code review all changes
3. Run full test suite
4. Test CSV exports in Excel, Google Sheets, LibreOffice

### Deployment Steps
1. **Backend Changes** (Phase 6A.48B):
   - Build and test locally
   - Deploy to staging environment
   - Verify UTF-8 BOM in downloaded files (hex editor check)
   - Test in Excel on Windows

2. **Frontend Changes** (Phase 6A.48A):
   - Build and test locally
   - Deploy to staging environment
   - Test both new and legacy sign-up lists
   - Verify row count validation

3. **Optional Enhancement** (Phase 6A.48C):
   - Can be deployed separately after A/B phases
   - Low risk, additive change

### Post-Deployment Validation
1. Download CSV from production
2. Open in Excel - verify no encoding issues
3. Check phone numbers display as text
4. Verify sign-up export has data rows
5. Monitor error logs for exceptions

---

## Rollback Plan

### If Issues Detected
1. **Frontend Only**: Revert `SignUpListsTab.tsx` commit
2. **Backend Only**: Revert `CsvExportService.cs` commit
3. **Both**: Revert entire Phase 6A.48 branch

### Rollback Safety
- Changes are isolated to export functions
- No database schema changes
- No impact on registration or sign-up core functionality
- User data not affected

---

## Documentation Updates Required

### User-Facing
- Update help documentation for CSV export
- Add note about Excel compatibility
- Provide sample CSV format

### Developer-Facing
- Update API documentation for export endpoint
- Document CSV format standards
- Add comments explaining UTF-8 BOM necessity

---

## Future Enhancements (Post-Phase 6A.48)

### Potential Improvements
1. **Configurable CSV Delimiters**: Support semicolon for European Excel
2. **Excel Direct Export**: Use EPPlus/NPOI for native .xlsx format (already implemented)
3. **Streaming Large Exports**: For events with 1000+ registrations
4. **Background Job Export**: Email CSV to organizer for very large datasets
5. **Custom Column Selection**: Let organizers choose which fields to export
6. **Export Templates**: Save and reuse column configurations

---

## References

### Standards
- RFC 4180: Common Format and MIME Type for CSV Files
- UTF-8 BOM: Unicode Standard Section 16.8

### Libraries
- CsvHelper: https://joshclose.github.io/CsvHelper/
- Axios Blob Handling: https://axios-http.com/docs/req_config

### Related Documents
- CSV_EXPORT_RCA.md (Root Cause Analysis)
- PHASE_6A45_DEPLOYMENT_PLAN.md (Attendee Management)
- Master Requirements Specification.md

---

**Plan Completed**: 2025-12-25
**Ready for Implementation**: Yes
**Reviewed By**: System Architecture Designer Agent
