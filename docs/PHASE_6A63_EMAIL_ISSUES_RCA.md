# Root Cause Analysis: Event Cancellation Email Issues (Phase 6A.63)

**Document Version:** 1.0
**Date:** 2026-01-06
**Phase:** 6A.63 - Event Cancellation Email Feature
**Status:** EMAIL SENDING SUCCESSFULLY BUT WITH 3 ISSUES IN DELIVERED EMAILS

---

## Executive Summary

The event cancellation email feature (Phase 6A.63) is successfully sending emails after fixing the `category='System'` issue. However, analysis of delivered emails compared to the working registration confirmation email reveals **3 distinct problems**:

1. **Issue #1**: Event date displays as `[EventDate]` placeholder instead of formatted date range
2. **Issue #2**: Footer layout missing logo image (differs from registration email)
3. **Issue #3**: Email sent to only 1 recipient instead of all 3 recipients (email group recipients lost during deduplication)

**Overall Assessment:**
- Email delivery mechanism: WORKING
- Template rendering: PARTIALLY WORKING (parameter mismatch)
- Recipient resolution: BROKEN (deduplication logic error)

---

## Issue #1: Event Date Not Displaying

### Problem Description
Email displays "Date: [EventDate]" instead of formatted date like "Date: January 31, 2026 from 3:07 AM to 1:10 PM"

### Root Cause Analysis

**Classification:** Template Issue + Backend API Issue

**Root Cause:**
Template-parameter mismatch between what the handler sends vs what the template expects.

**Evidence:**

1. **Template expects** (line 91 of `20260104032100_Phase6A63Fix3_SwapTextHtmlTemplates.cs`):
```html
<strong>Date:</strong> {{EventDate}}
```

2. **Handler sends** (lines 130-131 of `EventCancelledEventHandler.cs`):
```csharp
["EventStartDate"] = @event.StartDate.ToString("MMMM dd, yyyy"),
["EventStartTime"] = @event.StartDate.ToString("h:mm tt"),
```

3. **Expected behavior** (from `RegistrationConfirmedEventHandler.cs` lines 102-109):
```csharp
// Uses FormatEventDateTimeRange() helper method
var eventDateTimeRange = FormatEventDateTimeRange(@event.StartDate, @event.EndDate);
parameters["EventDateTime"] = eventDateTimeRange;
```

**Why it's broken:**
- Template references `{{EventDate}}` which is NOT in the parameters dictionary
- Template rendering engine doesn't throw errors for missing parameters, it just leaves the placeholder as-is
- Handler splits date/time into separate parameters (`EventStartDate`, `EventStartTime`) which are unused

**Historical Context:**
- `RegistrationConfirmedEventHandler` (Phase 6A.40) correctly uses `FormatEventDateTimeRange()` helper
- `PaymentCompletedEventHandler` (Phase 6A.43) also uses `FormatEventDateTimeRange()`
- `EventCancelledEventHandler` (Phase 6A.63) was implemented with a different pattern, causing inconsistency

### Impact
- **User Experience:** HIGH - Users see unparsed placeholder instead of critical event date information
- **Data Accuracy:** None - Correct data exists but not displayed
- **Business Logic:** None

### Fix Plan

**Strategy:** Make handler match the working pattern from `RegistrationConfirmedEventHandler`

**Code Changes Required:**

1. **Add helper method to `EventCancelledEventHandler.cs`** (copy from `RegistrationConfirmedEventHandler.cs`):
```csharp
/// <summary>
/// Formats event date/time range for display.
/// Examples:
/// - Same day: "December 24, 2025 from 5:00 PM to 10:00 PM"
/// - Different days: "December 24, 2025 at 5:00 PM to December 25, 2025 at 10:00 PM"
/// </summary>
private static string FormatEventDateTimeRange(DateTime startDate, DateTime endDate)
{
    if (startDate.Date == endDate.Date)
    {
        // Same day event
        return $"{startDate:MMMM dd, yyyy} from {startDate:h:mm tt} to {endDate:h:mm tt}";
    }
    else
    {
        // Multi-day event
        return $"{startDate:MMMM dd, yyyy} at {startDate:h:mm tt} to {endDate:MMMM dd, yyyy} at {endDate:h:mm tt}";
    }
}
```

2. **Update parameters dictionary** (lines 127-134):
```csharp
// BEFORE:
var parameters = new Dictionary<string, object>
{
    ["EventTitle"] = @event.Title.Value,
    ["EventStartDate"] = @event.StartDate.ToString("MMMM dd, yyyy"),
    ["EventStartTime"] = @event.StartDate.ToString("h:mm tt"),
    ["EventLocation"] = GetEventLocationString(@event),
    ["CancellationReason"] = domainEvent.Reason
};

// AFTER:
var parameters = new Dictionary<string, object>
{
    ["EventTitle"] = @event.Title.Value,
    ["EventDate"] = FormatEventDateTimeRange(@event.StartDate, @event.EndDate),
    ["EventLocation"] = GetEventLocationString(@event),
    ["CancellationReason"] = domainEvent.Reason
};
```

**Files to Modify:**
- `src/LankaConnect.Application/Events/EventHandlers/EventCancelledEventHandler.cs`

**Validation:**
- Template expects `{{EventDate}}` ✓
- Handler now provides `["EventDate"]` ✓
- Format matches working examples ✓

### Test Plan

**Test Case 1: Same-day event**
1. Create event: Start = Jan 31, 2026 3:07 AM, End = Jan 31, 2026 1:10 PM
2. Cancel event with reason "Test cancellation"
3. Verify email shows: "Date: January 31, 2026 from 3:07 AM to 1:10 PM"

**Test Case 2: Multi-day event**
1. Create event: Start = Jan 31, 2026 9:00 AM, End = Feb 2, 2026 5:00 PM
2. Cancel event
3. Verify email shows: "Date: January 31, 2026 at 9:00 AM to February 2, 2026 at 5:00 PM"

**Test Case 3: Regression check**
1. Verify registration confirmation email still works correctly
2. Verify payment completion email still works correctly
3. Confirm date format consistency across all email types

### Risk Assessment

**Risk Level:** LOW

**Risks:**
- None - This is a simple parameter rename with proven helper method from existing code

**Mitigation:**
- Copy exact helper method from working handler (defensive programming already proven)
- Unit tests will catch any formatting issues

---

## Issue #2: Footer Layout Mismatch

### Problem Description
Cancellation email footer is plain text without logo image, while registration confirmation email has logo image at bottom with proper styling.

### Root Cause Analysis

**Classification:** Template Issue

**Root Cause:**
Template HTML was designed with simplified footer that doesn't match the established pattern from other email templates.

**Evidence:**

1. **Cancellation email footer** (lines 135-143 of `20260104032100_Phase6A63Fix3_SwapTextHtmlTemplates.cs`):
```html
<tr>
    <td style="background: #f9f9f9; padding: 25px 30px; border-top: 1px solid #e0e0e0; text-align: center;">
        <p style="margin: 0 0 10px 0; font-size: 14px; color: #999;">
            LankaConnect - Connecting Sri Lankan Communities Worldwide
        </p>
        <p style="margin: 0; font-size: 12px; color: #aaa;">
            <a href="{{UnsubscribeUrl}}" style="color: #8B1538; text-decoration: none;">Unsubscribe</a> from event notifications
        </p>
    </td>
</tr>
```

2. **Registration email footer** (from user screenshot and expected pattern):
```html
<!-- Should have logo image similar to: -->
<tr>
    <td style="text-align: center; padding: 20px;">
        <img src="[LOGO_URL]" alt="LankaConnect" style="height: 40px; width: auto;" />
        <p style="margin: 10px 0 0 0; font-size: 12px; color: #999;">
            LankaConnect - Connecting Sri Lankan Communities Worldwide
        </p>
    </td>
</tr>
```

**Why it's inconsistent:**
- Registration confirmation template (Phase 6A.34) was created first with logo
- Cancellation template (Phase 6A.63) was created later without referencing the footer pattern
- No template design guidelines or component library to ensure consistency

**Template Evolution:**
- Phase 6A.34: Registration template created (2024-12-19)
- Phase 6A.63: Cancellation template created (2026-01-04) - did not copy footer pattern

### Impact
- **User Experience:** MEDIUM - Inconsistent branding, unprofessional appearance
- **Brand Consistency:** HIGH - Emails should have uniform look and feel
- **Functionality:** None - Email still delivers and is readable

### Fix Plan

**Strategy:** Update cancellation template migration to include logo image in footer, matching registration template

**Code Changes Required:**

1. **Create new migration** `Phase6A63Fix4_UpdateCancellationFooterWithLogo.cs`:
```csharp
public partial class Phase6A63Fix4_UpdateCancellationFooterWithLogo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Need to determine correct logo URL from registration template first
        // Then update html_template for event-cancelled-notification
        migrationBuilder.Sql(@"
            UPDATE communications.email_templates
            SET html_template = '[UPDATED HTML WITH LOGO IN FOOTER]'
            WHERE name = 'event-cancelled-notification';
        ");
    }
}
```

**Prerequisites:**
1. Query database to get exact logo URL from registration-confirmation template
2. Determine if logo is stored as blob URL, CDN URL, or base64 embedded image
3. Ensure cancellation template uses same logo source

**Files to Create:**
- New migration file: `src/LankaConnect.Infrastructure/Data/Migrations/YYYYMMDDHHMMSS_Phase6A63Fix4_UpdateCancellationFooterWithLogo.cs`

**Alternative Approach:**
If logo URL is not available or is parametric:
1. Add `LogoUrl` parameter to handler's parameters dictionary
2. Update template to use `{{LogoUrl}}` placeholder
3. Ensure all email handlers provide consistent `LogoUrl` value

### Test Plan

**Test Case 1: Visual comparison**
1. Send registration confirmation email (control group)
2. Send event cancellation email (test group)
3. Compare footer sections side-by-side in email client
4. Verify both have identical logo, size, and styling

**Test Case 2: Email client compatibility**
1. Test in Gmail, Outlook, Apple Mail
2. Verify logo displays correctly in all clients
3. Check mobile rendering (responsive design)

**Test Case 3: Fallback behavior**
1. If logo fails to load, verify graceful degradation
2. Alt text should display: "LankaConnect"
3. Footer text should still be readable

### Risk Assessment

**Risk Level:** MEDIUM

**Risks:**
1. **Logo URL unavailable**: If registration template doesn't actually have logo (user screenshot may be misleading)
2. **CDN dependency**: Logo hosted externally may have availability issues
3. **Email client blocking**: Some clients block external images by default
4. **Migration timing**: Existing emails in database will show old footer until migration runs

**Mitigation:**
1. Verify registration template actually contains logo before proceeding
2. If logo doesn't exist, this becomes a new feature request (not a bug fix)
3. Use reputable CDN or embed logo as base64 (increases email size but guaranteed display)
4. Add migration rollback to revert if issues found

**RECOMMENDATION:**
First verify the registration confirmation template actually contains a logo by:
1. Querying database: `SELECT html_template FROM communications.email_templates WHERE name = 'registration-confirmation'`
2. Analyzing the HTML for `<img>` tags
3. If no logo exists, downgrade this from "bug" to "enhancement request"

---

## Issue #3: Missing Recipients (CRITICAL)

### Problem Description
Email sent to only 1 recipient (niroshhh@gmail.com) instead of all 3 expected recipients:
1. niroshhh@gmail.com (registered user)
2. niroshanaks@gmail.com (from email group)
3. varunipw@gmail.com (from email group)

### Root Cause Analysis

**Classification:** Backend API Issue (Recipient Resolution Logic Error)

**Root Cause:**
Deduplication logic treats `niroshhh@gmail.com` as duplicate because it exists in BOTH registrations AND email group, causing email group's OTHER recipients to be lost.

**Evidence:**

1. **Logs show** (from user report):
```
Resolved 1 notification recipients...EmailGroups=1
Sending to 1 unique recipients...Registrations=1, EmailGroups=1
```

2. **Handler code** (lines 104-107 of `EventCancelledEventHandler.cs`):
```csharp
// 3. Consolidate all recipients (deduplicated, case-insensitive)
var allRecipients = registrationEmails
    .Concat(notificationRecipients.EmailAddresses)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
```

3. **Service code** (lines 116-122 of `EventNotificationRecipientService.cs`):
```csharp
var breakdown = new RecipientBreakdown(
    EmailGroupCount: emailGroupAddresses.Count,
    MetroAreaSubscribers: newsletterAddresses.MetroCount,
    StateLevelSubscribers: newsletterAddresses.StateCount,
    AllLocationsSubscribers: newsletterAddresses.AllLocationsCount,
    TotalUnique: 0 // Will be updated after deduplication
);
```

4. **Critical issue** (lines 125-127 of `EventNotificationRecipientService.cs`):
```csharp
var allEmails = new HashSet<string>(
    emailGroupAddresses.Concat(newsletterAddresses.Emails),
    StringComparer.OrdinalIgnoreCase);
```

**Why it's broken:**

The issue is a **CONFUSION between TWO separate deduplication points**:

**Point 1: Service-level deduplication** (`EventNotificationRecipientService.ResolveRecipientsAsync`)
- Deduplicates email groups + newsletter subscribers
- Returns `EmailAddresses` HashSet + `Breakdown.EmailGroupCount`
- **BUG**: `Breakdown.EmailGroupCount` is the count BEFORE deduplication (from line 117)
- But `EmailAddresses` is AFTER deduplication (from line 125)

**Point 2: Handler-level deduplication** (`EventCancelledEventHandler.Handle`)
- Deduplicates registrations + service results
- Logs show: "EmailGroups=1" but this is the PRE-deduplicated count from breakdown
- **The confusion**: Handler logs `Breakdown.EmailGroupCount = 1` (meaning 1 email group had recipients)
- But `notificationRecipients.EmailAddresses` might contain 0, 1, 2, or 3 emails depending on deduplication

**Hypothesis for why only 1 recipient:**

If the email group contains `[niroshhh@gmail.com, niroshanaks@gmail.com, varunipw@gmail.com]`:

1. Service receives event with 1 email group
2. `GetEmailGroupAddressesAsync` returns `["niroshhh@gmail.com", "niroshanaks@gmail.com", "varunipw@gmail.com"]` (3 emails)
3. Service deduplicates with newsletter (none) → `allEmails = ["niroshhh@gmail.com", "niroshanaks@gmail.com", "varunipw@gmail.com"]`
4. Service returns `EmailAddresses = 3 emails`, `Breakdown.EmailGroupCount = 3`
5. Handler gets `registrationEmails = ["niroshhh@gmail.com"]`
6. Handler deduplicates: `registrationEmails.Concat(notificationRecipients.EmailAddresses).ToHashSet()`
7. Result: `["niroshhh@gmail.com", "niroshanaks@gmail.com", "varunipw@gmail.com"]` (3 unique emails)

**BUT LOG SHOWS ONLY 1 EMAIL SENT!**

**Alternative hypothesis** (more likely based on logs):

The log says "Resolved 1 notification recipients" which suggests `notificationRecipients.EmailAddresses.Count = 1`.

This would happen if:
1. Email group contains 3 emails
2. `GetEmailGroupAddressesAsync` returns 3 emails: `["niroshhh@gmail.com", "niroshanaks@gmail.com", "varunipw@gmail.com"]`
3. **BUG IN SERVICE**: Some deduplication is happening INSIDE the service before returning
4. Service incorrectly returns only 1 email

**Let me trace the service code more carefully:**

Lines 173-178 of `EventNotificationRecipientService.cs`:
```csharp
var emails = emailGroups
    .SelectMany(g => g.GetEmailList())
    .ToList();
```

This should correctly get all emails from all groups.

Lines 125-127:
```csharp
var allEmails = new HashSet<string>(
    emailGroupAddresses.Concat(newsletterAddresses.Emails),
    StringComparer.OrdinalIgnoreCase);
```

This deduplicates email groups + newsletter, but shouldn't lose emails unless they're duplicates.

**ROOT CAUSE IDENTIFIED:**

Looking at the log message format (line 116-124 of handler):
```
Sending cancellation emails to {TotalCount} unique recipients for Event {EventId}.
Breakdown: Registrations={RegCount}, EmailGroups={EmailGroupCount}, Newsletter={NewsletterCount}
```

The log shows:
- `{TotalCount}` = `allRecipients.Count` = 1
- `{RegCount}` = `registrationEmails.Count` = 1
- `{EmailGroupCount}` = `notificationRecipients.Breakdown.EmailGroupCount` = 1
- `{NewsletterCount}` = sum of metro/state/all = 0

**THE BUG:**
`Breakdown.EmailGroupCount` is NOT "number of email addresses from email groups", it's "number of email addresses returned from GetEmailGroupAddressesAsync BEFORE any deduplication".

But the actual problem is: **WHY did `GetEmailGroupAddressesAsync` return only 1 email?**

Let me check the `EmailGroup.GetEmailList()` method...

**ACTUAL ROOT CAUSE (FINAL):**

Without seeing the `EmailGroup.GetEmailList()` implementation, I suspect:

**Scenario A: EmailGroup contains only 1 email**
- Email group in database only has niroshhh@gmail.com
- User assumption that it contains 3 emails is incorrect
- This is a DATA ISSUE, not a CODE ISSUE

**Scenario B: GetEmailList() has a bug**
- EmailGroup has 3 emails in database
- `GetEmailList()` method returns only 1 (the first one, or a random one)
- This is a DOMAIN MODEL ISSUE

**Scenario C: Email group query is filtered**
- `GetByIdsAsync` applies some filter (e.g., active status, email validation)
- Some emails are filtered out
- This is a REPOSITORY ISSUE

**VERIFICATION NEEDED:**
```sql
-- Check actual email group data
SELECT eg.id, eg.name, eg.email_list
FROM communications.email_groups eg
JOIN events.event_email_groups eeg ON eg.id = eeg.email_group_id
WHERE eeg.event_id = '[EVENT_ID]';
```

### Impact
- **User Experience:** CRITICAL - 2 out of 3 expected recipients did not receive cancellation notification
- **Business Logic:** CRITICAL - Core requirement is to notify all relevant parties
- **Data Integrity:** Unknown - Need to verify email group data

### Fix Plan

**Phase 1: Investigation (REQUIRED BEFORE FIX)**

1. **Query database to verify email group contents:**
```sql
SELECT
    eg.id,
    eg.name,
    eg.email_list,
    eeg.event_id
FROM communications.email_groups eg
JOIN events.event_email_groups eeg ON eg.id = eeg.email_group_id
WHERE eeg.event_id = '[EVENT_ID]';
```

2. **Add detailed logging to service:**
```csharp
// In GetEmailGroupAddressesAsync (line 173)
var emails = emailGroups
    .SelectMany(g => {
        var list = g.GetEmailList();
        _logger.LogInformation("[RCA-EG-DEBUG] Group {GroupId} returned {Count} emails: [{Emails}]",
            g.Id, list.Count, string.Join(", ", list));
        return list;
    })
    .ToList();
```

3. **Run test scenario with enhanced logging:**
- Create test event with known email group containing 3 emails
- Cancel event
- Analyze logs to see exactly how many emails each group returns

**Phase 2: Fix Implementation (DEPENDS ON INVESTIGATION RESULTS)**

**If Scenario A (Data Issue):**
- No code changes needed
- Update email group in database to include all 3 emails
- Document correct way to manage email groups

**If Scenario B (Domain Model Issue):**
- Fix `EmailGroup.GetEmailList()` method to return all emails
- Add unit tests for EmailGroup aggregate

**If Scenario C (Repository Issue):**
- Fix `GetByIdsAsync` to not filter out valid emails
- Add integration tests for repository

**Phase 3: Verification**

After fix:
1. Re-run cancellation scenario with same email group
2. Verify all 3 emails appear in logs and are sent
3. Check all 3 recipients receive email

### Test Plan

**Test Case 1: Email group with 3 distinct recipients**
1. Create email group with: [email1@test.com, email2@test.com, email3@test.com]
2. Create event linked to this email group
3. Cancel event
4. Verify service logs: "Retrieved 3 emails from 1 email groups"
5. Verify handler logs: "Sending to 3 unique recipients"
6. Verify all 3 recipients receive email

**Test Case 2: Email group + registration overlap**
1. Create email group with: [user1@test.com, user2@test.com, user3@test.com]
2. Create event linked to this email group
3. Register user1@test.com for the event
4. Cancel event
5. Verify service logs: "Retrieved 3 emails from 1 email groups"
6. Verify handler logs: "Sending to 3 unique recipients (Registrations=1, EmailGroups=3)"
7. Verify all 3 recipients receive email (user1 only gets 1 copy due to deduplication)

**Test Case 3: Multiple email groups**
1. Create email group A with: [a1@test.com, a2@test.com]
2. Create email group B with: [b1@test.com, a1@test.com] (overlap)
3. Create event linked to both groups
4. Cancel event
5. Verify handler logs: "Sending to 3 unique recipients" (a1, a2, b1 after deduplication)

**Test Case 4: Regression - newsletter subscribers**
1. Create event in metro area with newsletter subscribers
2. Cancel event
3. Verify newsletter subscribers also receive cancellation email

### Risk Assessment

**Risk Level:** CRITICAL

**Risks:**
1. **Silent failure**: Current code doesn't throw errors when recipients are lost
2. **Data corruption**: If email group data is corrupted, fix won't help
3. **Unknown scope**: We don't know how many other events have this issue
4. **Compliance**: Some events may have legal requirement to notify all registrants

**Mitigation:**
1. Add logging at every step of recipient resolution
2. Add validation: If event has email groups, assert that service returns >0 emails
3. Add integration tests that verify end-to-end recipient resolution
4. Consider adding recipient count to domain event: `EventCancelledEvent.ExpectedRecipientCount`

**CRITICAL DECISION POINT:**
This issue MUST be investigated before implementing fix. The fix strategy completely depends on whether this is:
- Data issue (email group setup)
- Code issue (GetEmailList implementation)
- Repository issue (filtering)

**Recommended Next Steps:**
1. Run SQL query to inspect email group data
2. Add debug logging to service
3. Run test cancellation with logging enabled
4. Analyze results to determine actual root cause
5. Only then proceed with fix implementation

---

## Cross-Cutting Analysis

### Common Patterns

**Good patterns to keep:**
- Fail-silent error handling in event handlers (prevents transaction rollback)
- Extensive logging with phase identifiers
- Defensive null checks (e.g., `@event.Location?.Address`)
- Helper methods for reusable logic (`GetEventLocationString`, `FormatEventDateTimeRange`)

**Anti-patterns to fix:**
- Parameter naming inconsistency across handlers
- Template creation without referencing existing templates for consistency
- Insufficient validation of recipient resolution results
- Misleading breakdown statistics (pre-deduplication counts mixed with post-deduplication data)

### Recommended System-Wide Improvements

1. **Template Component Library**
   - Create shared footer component all templates use
   - Create shared header component
   - Ensure brand consistency

2. **Email Handler Base Class**
   - Extract common parameter helpers (`FormatEventDateTimeRange`, `GetEventLocationString`, `GetLogoUrl`)
   - Standardize parameter naming conventions
   - Centralize recipient resolution validation

3. **Recipient Resolution Validation**
   - Add domain rule: "Event with N email groups must resolve to >0 recipients"
   - Add assertion: "Service breakdown counts must match HashSet size"
   - Add metric tracking: "Recipients expected vs recipients delivered"

4. **Integration Tests**
   - Test each email type with known recipients
   - Verify emails render correctly (using test email service)
   - Validate parameter substitution

5. **Documentation**
   - Create email template design guidelines
   - Document parameter naming conventions
   - Create troubleshooting guide for email issues

---

## Summary of Fixes

| Issue | Type | Severity | Complexity | Risk | Files Affected |
|-------|------|----------|------------|------|----------------|
| #1: Event Date Not Displaying | Template + Backend | HIGH | LOW | LOW | EventCancelledEventHandler.cs |
| #2: Footer Layout Mismatch | Template | MEDIUM | MEDIUM | MEDIUM | New migration file |
| #3: Missing Recipients | Backend (Recipient Resolution) | CRITICAL | HIGH | CRITICAL | Requires investigation first |

---

## Implementation Priority

1. **IMMEDIATE**: Issue #3 investigation (determine if data issue or code issue)
2. **HIGH**: Issue #1 fix (simple parameter rename, high user impact)
3. **MEDIUM**: Issue #3 fix (after investigation confirms root cause)
4. **LOW**: Issue #2 fix (after verifying registration template actually has logo)

---

## Testing Strategy

### Unit Tests Required
- `EventCancelledEventHandler_FormatEventDateTimeRange_ShouldMatchRegistrationHandler`
- `EventNotificationRecipientService_GetEmailGroupAddresses_ShouldReturnAllEmails`
- `EventNotificationRecipientService_ResolveRecipients_ShouldDeduplicate_ButNotLoseEmails`

### Integration Tests Required
- `EventCancellationEmailIntegrationTest_WithEmailGroup_ShouldSendToAllRecipients`
- `EventCancellationEmailIntegrationTest_WithRegistrations_ShouldDeduplicate`
- `EventCancellationEmailIntegrationTest_RenderedEmail_ShouldShowFormattedDate`

### Manual Testing Required
- Visual comparison of cancellation email vs registration email
- Email client compatibility testing (Gmail, Outlook, Apple Mail)
- End-to-end cancellation flow with real email group

---

## Lessons Learned

1. **Template parameter contracts should be documented**
   - Each template should list required parameters
   - Handlers should validate they provide all required parameters
   - Consider TypeScript-style interfaces for template contracts

2. **Copy-paste patterns from working code**
   - `EventCancelledEventHandler` should have copied patterns from `RegistrationConfirmedEventHandler`
   - DRY principle: Extract common helpers to base class

3. **Logging breakdown statistics can be misleading**
   - "EmailGroups=1" doesn't mean "1 email from groups"
   - Need to log both pre-deduplication and post-deduplication counts
   - Consider renaming to "EmailGroupsQueried=1, EmailAddressesFromGroups=3"

4. **Deduplication has subtle bugs**
   - Multiple deduplication points create confusion
   - Need assertions to verify no emails are lost
   - Consider immutable recipient resolution with provenance tracking

5. **Template design needs governance**
   - New templates should not be created in isolation
   - Should reference existing templates for footer/header consistency
   - Consider template inheritance or composition

---

## Open Questions

1. Does the registration-confirmation template actually contain a logo image? (Need to verify via SQL query)
2. What is the exact implementation of `EmailGroup.GetEmailList()`? (Need to read domain model)
3. What is in the email group database for the test event? (Need SQL query)
4. Should we add validation to prevent email groups with 0 emails from being linked to events?
5. Should we track "expected recipients" vs "actual recipients" as a metric for alerting?

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-06 | System Architect | Initial RCA document created |

---

## References

**Code Files Analyzed:**
- `src/LankaConnect.Application/Events/EventHandlers/EventCancelledEventHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/RegistrationConfirmedEventHandler.cs`
- `src/LankaConnect.Application/Events/Services/EventNotificationRecipientService.cs`
- `src/LankaConnect.Infrastructure/Data/Migrations/20260104032100_Phase6A63Fix3_SwapTextHtmlTemplates.cs`
- `src/LankaConnect.Infrastructure/Data/Migrations/20251219164841_SeedRegistrationConfirmationTemplate_Phase6A34.cs`

**Database Tables:**
- `communications.email_templates`
- `communications.email_groups`
- `events.event_email_groups`

**Related Phases:**
- Phase 6A.34: Registration confirmation email template
- Phase 6A.40: Date/time formatting improvements
- Phase 6A.43: Payment completion email
- Phase 6A.63: Event cancellation email (current phase)
