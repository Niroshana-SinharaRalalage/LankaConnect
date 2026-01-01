# CSV Export Root Cause Analysis

**Date**: 2025-12-25
**Phase**: Investigation
**Status**: Analysis Complete

## Executive Summary

Two distinct CSV export issues have been identified through screenshot evidence:

1. **Attendees CSV Export**: All data compressed into single cells with formatting issues
2. **Signups CSV Export**: Empty file (headers only, no data rows)

Both issues stem from different root causes and require different solutions.

---

## Issue 1: Attendees CSV Export - Data Formatting Problems

### Evidence from Screenshot

```
Row 1: "RegistrationId,MainAttendee,AdditionalAttendee Navya Sin 3 2 1 "1 Male 2 Female' niroshhin 1.86E+10 943 Penny NotRequir 0 USD — — ######## Confirmed\r\n"
```

**Observed Problems**:
- Headers concatenated with data in single cell
- Special characters rendered incorrectly (â€" instead of em dash)
- Phone numbers in scientific notation (1.86E+10)
- No column separation visible in Excel
- Carriage return visible (\r\n)

### Root Cause Analysis

#### 1. Backend CSV Generation (CORRECT)

**File**: `src/LankaConnect.Infrastructure/Services/Export/CsvExportService.cs`

```csharp
public byte[] ExportEventAttendees(EventAttendeesResponse attendees)
{
    using var memoryStream = new MemoryStream();
    using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
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

**ANALYSIS**: Backend uses CsvHelper library with proper configuration:
- ✅ UTF-8 encoding specified
- ✅ CultureInfo.InvariantCulture for consistent formatting
- ✅ HasHeaderRecord = true
- ✅ Automatic field quoting by CsvHelper
- ✅ Proper StreamWriter flushing

#### 2. API Response (CORRECT)

**File**: `src/LankaConnect.API/Controllers/EventsController.cs` (Line 1901-1905)

```csharp
return File(
    result.Value.FileContent,
    result.Value.ContentType,  // "text/csv"
    result.Value.FileName
);
```

**ANALYSIS**:
- ✅ Content-Type: "text/csv" is correct
- ✅ Binary content returned as byte array
- ✅ File name properly set

#### 3. Frontend Download Handler (POTENTIAL ISSUE)

**File**: `web/src/presentation/components/features/events/AttendeeManagementTab.tsx` (Lines 170-191)

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

**ANALYSIS**:
- ⚠️ **MISSING**: No explicit MIME type specification when creating Blob
- ⚠️ **MISSING**: No BOM (Byte Order Mark) for UTF-8 Excel compatibility
- ⚠️ Blob created from API response assumes correct MIME type

#### 4. API Client Blob Handling (POTENTIAL ISSUE)

**File**: `web/src/infrastructure/api/repositories/events.repository.ts` (Lines 864-869)

```typescript
async exportEventAttendees(eventId: string, format: 'excel' | 'csv' = 'excel'): Promise<Blob> {
  return await apiClient.get<Blob>(
    `${this.basePath}/${eventId}/export?format=${format}`,
    { responseType: 'blob' }
  );
}
```

**ANALYSIS**:
- ✅ Correctly specifies responseType: 'blob'
- ⚠️ Axios creates generic Blob without explicit MIME type from Content-Type header

### Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Backend: CsvExportService.cs                                 │
│    - CsvHelper generates proper CSV with UTF-8                  │
│    - Returns byte[] with correct delimiters                     │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. API Controller: EventsController.cs                          │
│    - Returns File(byte[], "text/csv", filename)                 │
│    - HTTP Response: Content-Type: text/csv                      │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. Frontend: apiClient.get<Blob>({ responseType: 'blob' })     │
│    ⚠️ ISSUE: Axios creates Blob without preserving MIME type   │
│    - Blob type may be empty or incorrect                        │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. Download Handler: AttendeeManagementTab.tsx                  │
│    ⚠️ ISSUE: URL.createObjectURL(blob) without explicit type   │
│    ⚠️ ISSUE: No UTF-8 BOM for Excel compatibility              │
│    - Excel may misinterpret CSV encoding                        │
└─────────────────────────────────────────────────────────────────┘
```

### Specific Issues Identified

#### A. UTF-8 BOM Missing
**Problem**: Excel requires UTF-8 BOM (0xEF, 0xBB, 0xBF) to correctly detect UTF-8 encoding.

**Evidence**:
- Special characters showing as "â€"" (UTF-8 bytes interpreted as Windows-1252)
- This is classic UTF-8 encoding detection failure in Excel

**Root Cause**: Backend does not prepend BOM to CSV byte array.

#### B. Phone Number Scientific Notation
**Problem**: Excel interprets large numbers (1862943043) as scientific notation (1.86E+10).

**Evidence**: Screenshot shows "1.86E+10" instead of phone number.

**Root Cause**:
- Phone number stored as numeric value without quotes
- CsvHelper should quote string fields, but may need explicit configuration

**Code Reference** (`CsvExportService.cs` Line 36):
```csharp
Phone = a.ContactPhone,  // May need explicit string formatting
```

#### C. Em Dash Character Encoding
**Problem**: Em dash (—) rendered as "â€"" in Excel.

**Evidence**: Address field shows incorrect characters.

**Root Cause**: UTF-8 encoding not detected by Excel (related to missing BOM).

**Code Reference** (`CsvExportService.cs` Line 37):
```csharp
Address = a.ContactAddress ?? "—",  // UTF-8 em dash (U+2014)
```

#### D. Blob MIME Type Not Preserved
**Problem**: Blob created without explicit MIME type.

**Root Cause**: Axios responseType: 'blob' creates generic Blob, not preserving Content-Type header.

**Expected**: `new Blob([data], { type: 'text/csv;charset=utf-8' })`
**Actual**: `new Blob([data])` (generic blob)

---

## Issue 2: Signups CSV Export - No Data

### Evidence from Screenshot

```
Row 1: "Category Item Desc User ID Quantity Committed At"
Row 2: (empty)
Row 3: (empty)
```

**Observed Problems**:
- Headers present and correctly formatted
- No data rows follow
- File downloads successfully but contains no content

### Root Cause Analysis

#### 1. Client-Side CSV Generation (DIFFERENT FROM ATTENDEES)

**File**: `web/src/presentation/components/features/events/SignUpListsTab.tsx` (Lines 29-57)

```typescript
const handleDownloadCSV = () => {
  if (!signUpLists || signUpLists.length === 0) {
    alert('No sign-up lists to download');
    return;
  }

  // Build CSV content
  let csvContent = 'Category,Item Description,User ID,Quantity,Committed At\n';

  signUpLists.forEach((list) => {
    (list.commitments || []).forEach((commitment) => {
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

**CRITICAL ISSUE IDENTIFIED**:

#### A. Wrong Data Structure Used
**Problem**: Code iterates over `list.commitments` but SignUpListDto has DIFFERENT structure.

**Data Model** (`SignUpListDto.cs` Lines 1-26):
```csharp
public class SignUpListDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Legacy fields (for Open/Predefined sign-ups)
    public List<SignUpCommitmentDto> Commitments { get; set; } = new();

    // New category-based fields
    public List<SignUpItemDto> Items { get; set; } = new();  // ← THIS IS USED NOW
}

public class SignUpItemDto
{
    public Guid Id { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public List<SignUpCommitmentDto> Commitments { get; set; } = new();  // ← NESTED HERE
}
```

**ROOT CAUSE**:
1. Code expects: `signUpLists[].commitments[]`
2. Actual structure: `signUpLists[].Items[].Commitments[]` (nested)
3. The `list.commitments` is likely empty array for new category-based sign-ups
4. Code SHOULD iterate: `list.Items[].Commitments[]`

#### B. Incorrect Property Access
**Expected Iteration Pattern**:
```typescript
signUpLists.forEach((list) => {
  list.Items.forEach((item) => {              // ← MISSING
    item.Commitments.forEach((commitment) => {  // ← SHOULD BE NESTED
      csvContent += `"${list.category}","${item.itemDescription}","${commitment.userId}",${commitment.quantity},"${commitment.committedAt}"\n`;
    });
  });
});
```

**Actual Iteration Pattern**:
```typescript
signUpLists.forEach((list) => {
  (list.commitments || []).forEach((commitment) => {  // ← WRONG: commitments is empty
    csvContent += ...
  });
});
```

### Data Structure Evidence

**TypeScript Type Definition** (`events.types.ts` - expected):
```typescript
export interface SignUpListDto {
  id: string;
  category: string;
  description: string;

  // Legacy (may be empty)
  commitments: SignUpCommitmentDto[];

  // Current structure
  items: SignUpItemDto[];  // ← SHOULD USE THIS
}

export interface SignUpItemDto {
  id: string;
  itemDescription: string;
  commitments: SignUpCommitmentDto[];  // ← DATA IS HERE
}
```

---

## Summary of Root Causes

### Attendees CSV Export
1. **Missing UTF-8 BOM** - Excel cannot detect UTF-8 encoding
2. **Phone number formatting** - Not quoted as string, Excel treats as number
3. **Blob MIME type not explicit** - Generic blob without charset
4. **No validation of blob content type** - Frontend doesn't verify MIME type

### Signups CSV Export
1. **Wrong data structure traversal** - Accessing `list.commitments` instead of `list.Items[].Commitments[]`
2. **Legacy vs. New schema mismatch** - Code written for old flat structure, data uses new nested structure
3. **No fallback logic** - No handling for both legacy and new structures
4. **No data validation** - No check if Items[] array is populated

---

## Technical Impact Assessment

### Attendees Export
- **Severity**: Medium-High
- **User Impact**: CSV downloads but Excel cannot parse correctly
- **Data Loss**: No data loss, but unusable format
- **Workaround**: Users can import with custom delimiter settings

### Signups Export
- **Severity**: High
- **User Impact**: CSV downloads with headers only, no data
- **Data Loss**: Complete data loss in export
- **Workaround**: None - feature is non-functional

---

## Next Steps

See companion document: **CSV_EXPORT_FIX_PLAN.md** for detailed implementation plan.

### Priority Order
1. **CRITICAL**: Fix Signups CSV (completely broken)
2. **HIGH**: Fix Attendees CSV encoding (UTF-8 BOM)
3. **MEDIUM**: Fix phone number formatting
4. **LOW**: Improve blob MIME type handling

---

## Code References

### Files Analyzed
1. `src/LankaConnect.Infrastructure/Services/Export/CsvExportService.cs` - Backend CSV generation (Attendees)
2. `src/LankaConnect.API/Controllers/EventsController.cs` - Export endpoint (Lines 1853-1906)
3. `web/src/presentation/components/features/events/AttendeeManagementTab.tsx` - Attendees export UI
4. `web/src/presentation/components/features/events/SignUpListsTab.tsx` - Signups export UI (Lines 29-57)
5. `web/src/infrastructure/api/repositories/events.repository.ts` - API client (Lines 864-869)
6. `src/LankaConnect.Application/Events/Common/SignUpListDto.cs` - Data model
7. `src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs` - Data query

### Key Libraries
- **Backend**: CsvHelper (properly configured)
- **Frontend**: Native Blob API (missing configuration)

---

**Analysis Completed**: 2025-12-25
**Architect**: System Architecture Designer Agent
