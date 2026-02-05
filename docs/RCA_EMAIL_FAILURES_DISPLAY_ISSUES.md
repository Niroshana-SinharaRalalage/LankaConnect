# Root Cause Analysis: Email Failures Display Issues

**Date**: 2026-02-05
**Status**: Analysis Complete
**Severity**: Medium
**Phase Reference**: Phase 6A.87/6A.89 Email Metrics Dashboard

---

## Executive Summary

This RCA documents four related issues with the Email Failures display in the Admin Email Metrics Dashboard. The root causes span multiple architectural layers including **UI design decisions**, **intentional privacy protection**, **missing backend feature**, and **architectural design trade-offs**.

---

## Issues Reported

| # | Issue | Current Behavior | Expected Behavior |
|---|-------|------------------|-------------------|
| 1 | Limited failure count | Shows only last 4 failures | Should show at least 1 month of data |
| 2 | Masked email addresses | Shows `l***@g***.com` | Admin view should show full email |
| 3 | Truncated error messages | Text truncated with `truncate` CSS, no tooltip | Should show full error (tooltip or scroll) |
| 4 | Data lost after restart | "Historical failure details unavailable" | Failures should persist |

---

## Architecture Overview

### Components Analyzed

```
Frontend (React/Next.js):
  c:\Work\LankaConnect\web\src\presentation\components\features\admin\email-metrics\EmailMetricsTab.tsx
  c:\Work\LankaConnect\web\src\infrastructure\api\types\email-metrics.types.ts
  c:\Work\LankaConnect\web\src\infrastructure\api\repositories\email-metrics.repository.ts
  c:\Work\LankaConnect\web\src\presentation\hooks\useEmailMetrics.ts

Backend API (.NET):
  c:\Work\LankaConnect\src\LankaConnect.API\Controllers\EmailMetricsController.cs

Backend Service:
  c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Email\Services\DatabaseEmailMetrics.cs
  c:\Work\LankaConnect\src\LankaConnect.Shared\Email\Observability\IEmailMetrics.cs

Database Entity:
  c:\Work\LankaConnect\src\LankaConnect.Domain\Communications\Entities\EmailMetricRecord.cs
  c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Configurations\EmailMetricRecordConfiguration.cs
```

### Data Flow

```
[Email Failure Occurs]
        |
        v
[RecordFailedEmail()] --> ConcurrentBag<EmailFailureRecord> (IN-MEMORY)
        |                          |
        v                          v
[RecordEmailSent(success=false)] --> _pendingUpdates dict --> FlushToDatabase (30s)
                                                                    |
                                                                    v
                                                [EmailMetricRecord table - AGGREGATE COUNTS ONLY]
```

---

## Issue 1: Only Showing Last 4 Failures

### Root Cause: **Backend Design - In-Memory Limit**

**Classification**: Backend / Architecture Design

**Code Location**: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Email\Services\DatabaseEmailMetrics.cs`

```csharp
// Line 52
private const int MaxFailureRecords = 100;

// Lines 266-275: RecordFailedEmail method
public void RecordFailedEmail(...)
{
    // Trim old records if limit exceeded
    while (_failedEmails.Count >= MaxFailureRecords)
    {
        _failedEmails.TryTake(out _);  // FIFO removal from ConcurrentBag
    }

    _failedEmails.Add(new EmailFailureRecord { ... });
}
```

**Analysis**:
- `MaxFailureRecords = 100` limits in-memory storage
- `ConcurrentBag.TryTake()` does NOT guarantee FIFO order - it may remove ANY item
- If the system has processed many emails, older failures are evicted unpredictably
- The "4 failures" observation suggests only 4 failures occurred since last container restart, OR the bag retained only 4 after eviction

**Why Only 4 Visible**:
1. Container recently restarted (no historical data loaded)
2. Only 4 failures occurred since restart
3. ConcurrentBag random eviction left only 4

### Recommended Fix

**Option A (Quick Fix)**: Increase limit and fix ordering
```csharp
private const int MaxFailureRecords = 1000;

// Replace ConcurrentBag with ConcurrentQueue for FIFO behavior
private readonly ConcurrentQueue<EmailFailureRecord> _failedEmails = new();
```

**Option B (Proper Fix)**: Persist failure details to database
- Create new table: `communications.email_failure_details`
- Store: timestamp, template_name, recipient_email, error_message, handler_name
- Add retention policy: auto-delete records older than 30 days
- Load from database on startup, query for dashboard display

---

## Issue 2: Email Addresses are Masked

### Root Cause: **Intentional Privacy Protection in Backend**

**Classification**: Backend / Intentional Design

**Code Location**: `c:\Work\LankaConnect\src\LankaConnect.API\Controllers\EmailMetricsController.cs`

```csharp
// Lines 294-308: MaskEmail method
private static string MaskEmail(string email)
{
    if (string.IsNullOrEmpty(email) || !email.Contains('@'))
        return "***@***.***";

    var parts = email.Split('@');
    var localPart = parts[0];
    var domainPart = parts[1];

    var maskedLocal = localPart.Length > 2
        ? localPart[..2] + new string('*', Math.Min(localPart.Length - 2, 3))
        : new string('*', localPart.Length);

    return $"{maskedLocal}@{domainPart}";
}
```

**Usage**: Line 162 in GetFailures endpoint:
```csharp
RecipientEmail = MaskEmail(f.RecipientEmail),  // Always masks
```

**Analysis**:
- The masking is **intentionally applied** for privacy protection
- The comment `// Masked for privacy` in the DTO confirms this was a deliberate decision
- However, admin users debugging email failures need to see full addresses

### Recommended Fix

**Option A (Recommended)**: Add parameter to control masking for admin
```csharp
[HttpGet("failures")]
[Authorize(Policy = "RequireAdmin")]
public IActionResult GetFailures([FromQuery] int limit = 100, [FromQuery] bool unmasked = false)
{
    // Verify admin role before allowing unmasked
    var isAdmin = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

    var failures = _emailMetrics.GetFailedEmails()
        .OrderByDescending(f => f.Timestamp)
        .Take(limit)
        .Select(f => new EmailFailureDto
        {
            RecipientEmail = (unmasked && isAdmin) ? f.RecipientEmail : MaskEmail(f.RecipientEmail),
            // ...
        })
        .ToList();

    return Ok(response);
}
```

**Option B**: Add separate admin-only endpoint with full details
```csharp
[HttpGet("failures/admin")]
[Authorize(Policy = "RequireSuperAdmin")]
public IActionResult GetFailuresAdmin([FromQuery] int limit = 100)
{
    // Returns unmasked emails for super admins only
}
```

---

## Issue 3: Error Messages Truncated

### Root Cause: **UI CSS Truncation Without Expand Option**

**Classification**: UI / Missing Feature

**Code Location**: `c:\Work\LankaConnect\web\src\presentation\components\features\admin\email-metrics\EmailMetricsTab.tsx`

```tsx
// Lines 625-642: FailureRow component
function FailureRow({ failure }: { failure: EmailFailureDto }) {
  return (
    <tr className="hover:bg-gray-50">
      {/* ... */}
      <td className="px-6 py-4 text-sm text-red-600 max-w-xs truncate">
        {failure.errorMessage}
      </td>
    </tr>
  );
}
```

**Analysis**:
- `max-w-xs truncate` CSS class limits width and adds ellipsis
- No tooltip or expand mechanism exists
- Error messages are often long and detailed, making this very limiting for debugging

### Recommended Fix

**Option A (Quick Fix)**: Add tooltip on hover
```tsx
function FailureRow({ failure }: { failure: EmailFailureDto }) {
  return (
    <tr className="hover:bg-gray-50">
      {/* ... */}
      <td
        className="px-6 py-4 text-sm text-red-600 max-w-xs truncate cursor-help"
        title={failure.errorMessage}  // Native browser tooltip
      >
        {failure.errorMessage}
      </td>
    </tr>
  );
}
```

**Option B (Better UX)**: Add expandable detail view
```tsx
function FailureRow({ failure, onShowDetails }: { failure: EmailFailureDto; onShowDetails: () => void }) {
  return (
    <tr className="hover:bg-gray-50">
      {/* ... */}
      <td className="px-6 py-4 text-sm text-red-600">
        <div className="flex items-center gap-2">
          <span className="max-w-xs truncate">{failure.errorMessage}</span>
          <button
            onClick={onShowDetails}
            className="text-gray-500 hover:text-gray-700"
          >
            <Info className="w-4 h-4" />
          </button>
        </div>
      </td>
    </tr>
  );
}
```

**Option C (Best)**: Modal with full details
- Add click handler to open modal with full error details
- Include: template, recipient, handler, full error message, stack trace if available
- Allow copy-to-clipboard for sharing in tickets

---

## Issue 4: Error Details Gone After Restart

### Root Cause: **Architectural Design - In-Memory Storage Without Persistence**

**Classification**: Architecture / Feature Missing (Database Persistence)

**Code Location**: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Email\Services\DatabaseEmailMetrics.cs`

**Current Persistence Strategy**:
```csharp
// PERSISTED to database (EmailMetricRecord table):
- Aggregate counts per day/template: TotalSent, Successful, Failed, ValidationFailures
- Flushed every 30 seconds via FlushToDatabaseAsync()

// NOT PERSISTED (in-memory only):
- EmailFailureRecord details (ConcurrentBag<EmailFailureRecord> _failedEmails)
- ValidationFailureRecord details (ConcurrentBag<ValidationFailureRecord> _validationFailures)
```

**Evidence from Code**:
```csharp
// Lines 337-369: LoadMetricsFromDatabaseAsync
private async Task LoadMetricsFromDatabaseAsync(CancellationToken cancellationToken)
{
    // Loads ONLY aggregate metrics from EmailMetricRecord table
    // Does NOT load failure details (they don't exist in database)
    var records = await ((DbContext)dbContext).Set<EmailMetricRecord>()
        .Where(m => m.MetricDate >= startDate)
        .ToListAsync(cancellationToken);

    // Aggregates into _templateMetrics (counts only)
    foreach (var record in records) { /* aggregates */ }

    // _failedEmails and _validationFailures remain EMPTY after restart
}
```

**UI Handling of This Case** (implemented in Phase 6A.89):
```tsx
// Lines 275-285: EmailMetricsTab.tsx
) : failures?.failures.length === 0 && failures?.totalCount > 0 ? (
  /* Phase 6A.89 Fix: Show message when failures exist but details lost */
  <div className="p-8 text-center text-amber-600">
    <AlertTriangle className="w-12 h-12 mx-auto mb-3" />
    <p className="font-medium">Historical failure details unavailable</p>
    <p className="text-sm text-gray-500 mt-2">
      {failures.totalCount} failure{failures.totalCount !== 1 ? 's' : ''} recorded,
      but details were cleared after server restart.
    </p>
  </div>
```

### Analysis

The current system has a **hybrid persistence model**:
- **Durable**: Aggregate counts (total failures, success rate) survive restarts
- **Volatile**: Failure details (error messages, emails) are lost on restart

This was a **conscious architectural decision** documented in Phase 6A.89, but it limits debugging capability.

### Recommended Fix

**Create Database Table for Failure Details**:

```sql
-- Migration: AddEmailFailureDetailsTable
CREATE TABLE communications.email_failure_details (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    template_name VARCHAR(100) NOT NULL,
    recipient_email VARCHAR(255),  -- Consider encryption for GDPR
    error_message TEXT NOT NULL,
    handler_name VARCHAR(200),
    correlation_id VARCHAR(100),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    -- Auto-expire after 30 days for data retention
    expires_at TIMESTAMPTZ GENERATED ALWAYS AS (created_at + INTERVAL '30 days') STORED
);

-- Index for dashboard queries
CREATE INDEX IX_EmailFailureDetails_Timestamp ON communications.email_failure_details (timestamp DESC);
CREATE INDEX IX_EmailFailureDetails_Template ON communications.email_failure_details (template_name);

-- Scheduled job to delete expired records (add to maintenance)
-- DELETE FROM communications.email_failure_details WHERE expires_at < NOW();
```

**Update DatabaseEmailMetrics.cs**:
```csharp
public void RecordFailedEmail(string correlationId, string templateName,
    string recipientEmail, string errorMessage, string handlerName)
{
    // Add to in-memory for immediate display
    _failedEmails.Add(new EmailFailureRecord { ... });

    // Queue for database persistence
    _pendingFailureDetails.Enqueue(new EmailFailureDetail { ... });
}

// Add to FlushToDatabaseAsync:
private async Task FlushFailureDetailsToDatabaseAsync()
{
    while (_pendingFailureDetails.TryDequeue(out var detail))
    {
        // Insert to database
    }
}

// Load on startup:
private async Task LoadFailureDetailsFromDatabaseAsync()
{
    var recentFailures = await dbContext.Set<EmailFailureDetail>()
        .Where(f => f.Timestamp >= DateTime.UtcNow.AddMonths(-1))
        .OrderByDescending(f => f.Timestamp)
        .Take(MaxFailureRecords)
        .ToListAsync();

    foreach (var failure in recentFailures)
    {
        _failedEmails.Add(MapToRecord(failure));
    }
}
```

---

## Summary of Classifications

| Issue | Classification | Layer | Fix Complexity |
|-------|----------------|-------|----------------|
| 1. Limited failures (4) | Backend Architecture | `DatabaseEmailMetrics.cs` | Medium |
| 2. Masked emails | Intentional Privacy | `EmailMetricsController.cs` | Low |
| 3. Truncated errors | UI Missing Feature | `EmailMetricsTab.tsx` | Low |
| 4. Data lost on restart | Architecture Design | Multiple layers | High |

---

## Recommended Implementation Order

1. **Quick Win (Low effort)**: Fix Issue 3 - Add tooltip to error messages
2. **Medium effort**: Fix Issue 2 - Add admin unmasked option
3. **Medium effort**: Fix Issue 1 - Increase limit, use ConcurrentQueue
4. **High effort**: Fix Issue 4 - Add database persistence for failure details

---

## Files to Modify

### Issue 1 Fix (Backend)
- `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Email\Services\DatabaseEmailMetrics.cs`

### Issue 2 Fix (Backend API)
- `c:\Work\LankaConnect\src\LankaConnect.API\Controllers\EmailMetricsController.cs`

### Issue 3 Fix (Frontend)
- `c:\Work\LankaConnect\web\src\presentation\components\features\admin\email-metrics\EmailMetricsTab.tsx`

### Issue 4 Fix (Full Stack)
- Create migration for new table
- `c:\Work\LankaConnect\src\LankaConnect.Domain\Communications\Entities\EmailFailureDetail.cs` (new)
- `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Configurations\EmailFailureDetailConfiguration.cs` (new)
- `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Email\Services\DatabaseEmailMetrics.cs`
- `c:\Work\LankaConnect\src\LankaConnect.API\Controllers\EmailMetricsController.cs`

---

## GDPR Considerations

For Issue 4 (persisting email addresses to database):
- Consider encrypting `recipient_email` column at rest
- Implement 30-day auto-expiry for data retention compliance
- Add audit logging for access to unmasked email addresses
- Document data processing in privacy policy

---

## References

- Phase 6A.87: Initial Email Metrics Dashboard implementation
- Phase 6A.89: Database-backed metrics with hybrid persistence
- Related PR: Email metrics survival across container restarts
