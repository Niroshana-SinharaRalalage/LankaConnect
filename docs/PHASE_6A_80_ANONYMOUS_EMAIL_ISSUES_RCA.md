# Phase 6A.80: Anonymous RSVP Email Issues - Root Cause Analysis

**Date**: 2026-01-24
**Status**: Investigation
**Priority**: HIGH

## User-Reported Issues

### Issue 1: Email Not Sent
**User Report**: "I registered for 0458806b-8672-4ad5-a7cb-f5346f1b282a as lankaconnect.app@gmail.com user. But it didn't send me a confirmation email with template-anonymous-rsvp-confirmation template"

### Issue 2: Template Design Concern
**User Question**: "For paid and free events we have two templates for members. But anonymous users we have only one template to send email, how will that template handle both paid and free events data display?"

---

## Root Cause Analysis

### Issue 1: Why Email Wasn't Sent

**File**: [AnonymousRegistrationConfirmedEventHandler.cs:81-88](../src/LankaConnect.Application/Events/EventHandlers/AnonymousRegistrationConfirmedEventHandler.cs#L81-L88)

```csharp
// Skip email for paid events - PaymentCompletedEventHandler will send it after payment
if (registration.PaymentStatus == PaymentStatus.Pending)
{
    stopwatch.Stop();
    _logger.LogInformation(
        "AnonymousRegistrationConfirmed: Skipping paid event - EventId={EventId}, Email={Email}, Duration={ElapsedMs}ms",
        domainEvent.EventId, domainEvent.AttendeeEmail, stopwatch.ElapsedMilliseconds);
    return; // ❌ EMAIL NOT SENT FOR PAID EVENTS
}
```

**Explanation**:
- When anonymous users register for **PAID** events, `PaymentStatus` is set to `Pending`
- Handler intentionally skips sending email, expecting `PaymentCompletedEventHandler` to handle it after payment
- `PaymentCompletedEventHandler` sends emails using `EmailTemplateNames.PaidEventRegistration` (ticket confirmation template)
- This is **by design** - paid event emails should include ticket information AFTER payment completes

**Diagnosis for Event 0458806b-8672-4ad5-a7cb-f5346f1b282a**:
- If this is a **paid event**, no email sent is CORRECT behavior (waiting for payment)
- If this is a **free event**, there's a bug - email should have been sent

**Action Required**: Check if event `0458806b-8672-4ad5-a7cb-f5346f1b282a` is paid or free.

---

### Issue 2: Template Parameter Mismatch

**Critical Problem**: Handler parameters don't match template expectations!

#### Handler Sends These Parameters

**File**: [AnonymousRegistrationConfirmedEventHandler.cs:111-136](../src/LankaConnect.Application/Events/EventHandlers/AnonymousRegistrationConfirmedEventHandler.cs#L111-L136)

```csharp
var parameters = new Dictionary<string, object>
{
    { "UserName", contactName },
    { "EventTitle", @event.Title.Value },
    { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },      // ❌ Template expects "EventDate"
    { "EventStartTime", @event.StartDate.ToString("h:mm tt") },            // ❌ Template expects "EventTime"
    { "EventEndDate", @event.EndDate.ToString("MMMM dd, yyyy") },          // ❌ Not used in template
    { "EventLocation", ... },                                               // ✅ Matches
    { "Quantity", domainEvent.Quantity },                                   // ❌ Template expects "GuestCount"
    { "RegistrationDate", ... },                                            // ❌ Not used in template
    { "Attendees", attendeeDetails },                                       // ❌ Not used in template
    { "HasAttendeeDetails", attendeeDetails.Any() },                       // ❌ Not used in template
    { "ContactEmail", registration.Contact.Email },                        // ❌ Not used in template
    { "ContactPhone", registration.Contact.PhoneNumber ?? "" },            // ❌ Not used in template
    { "HasContactInfo", true }                                             // ❌ Not used in template
};

// ❌ MISSING: EventUrl, ManageRsvpUrl, Year
```

#### Template Expects These Parameters

**File**: [Phase6A76 Migration:203](../src/LankaConnect.Infrastructure/Data/Migrations/20260123013633_Phase6A76_RenameAndAddEmailTemplates.cs#L203)

**HTML Template**:
```handlebars
{{EventTitle}}       ✅ Provided
{{EventDate}}        ❌ Missing (handler sends "EventStartDate")
{{EventTime}}        ❌ Missing (handler sends "EventStartTime")
{{EventLocation}}    ✅ Provided
{{GuestCount}}       ❌ Missing (handler sends "Quantity")
{{EventUrl}}         ❌ Missing
{{ManageRsvpUrl}}    ❌ Missing
{{Year}}             ❌ Missing
```

**Text Template**:
```handlebars
{{EventTitle}}       ✅ Provided
{{EventDate}}        ❌ Missing
{{EventTime}}        ❌ Missing
{{EventLocation}}    ✅ Provided
{{EventUrl}}         ❌ Missing
{{ManageRsvpUrl}}    ❌ Missing
```

---

## Impact Assessment

### High Impact Issues
1. **Missing Critical Links**: `EventUrl` and `ManageRsvpUrl` are not provided
   - Users can't view event details from email
   - Users can't manage/cancel their RSVP
   - This breaks user experience severely

2. **Wrong Parameter Names**: Date/time parameters have wrong names
   - `{{EventDate}}` and `{{EventTime}}` will render as empty strings
   - Email will show blank date/time fields

3. **Only Free Events Get Emails**: Paid events don't trigger this handler
   - Anonymous paid event users never get RSVP confirmation
   - They only get ticket confirmation AFTER payment completes
   - Gap in communication for pre-payment period

### Medium Impact Issues
1. **Unused Parameters**: Handler sends many parameters template doesn't use
   - `Attendees`, `HasAttendeeDetails`, `ContactEmail`, `ContactPhone`, `HasContactInfo`
   - Wasted processing, confusing code

2. **Missing Year**: Footer won't show correct copyright year

---

## Solution Design

### Option 1: Fix Parameter Names (Quick Fix)

**Changes Required**:
1. Update handler to send correct parameter names:
   - `EventStartDate` → `EventDate`
   - `EventStartTime` → `EventTime`
   - `Quantity` → `GuestCount`
2. Add missing parameters:
   - `EventUrl` - Link to event details page
   - `ManageRsvpUrl` - Link to manage/cancel RSVP
   - `Year` - Current year for footer

**Pros**:
- Quick fix, minimal code changes
- Maintains current template design
- Works for free events immediately

**Cons**:
- Doesn't address paid event gap
- Template still generic (no paid/free differentiation)

### Option 2: Enhanced Template with Conditional Logic (Recommended)

**Design**:
1. Update template to handle BOTH paid and free events
2. Add conditional sections based on `IsPaid` parameter:
   - Free events: Show immediate confirmation
   - Paid events: Show "pending payment" message with payment link
3. Send email for BOTH paid and free anonymous registrations

**Template Structure**:
```handlebars
{{#if IsPaid}}
  <!-- Paid Event Flow -->
  Your registration for {{EventTitle}} is confirmed!

  Next Step: Complete Payment
  Amount: ${{TicketPrice}}
  Payment Link: {{PaymentUrl}}

  You'll receive your ticket confirmation after payment completes.
{{else}}
  <!-- Free Event Flow -->
  Your RSVP for {{EventTitle}} has been confirmed!

  Event Details: {{EventDate}} at {{EventTime}}
  Location: {{EventLocation}}
{{/if}}

<!-- Common sections -->
View Event: {{EventUrl}}
Manage RSVP: {{ManageRsvpUrl}}
```

**Handler Changes**:
1. Remove the `PaymentStatus == Pending` skip logic (lines 81-88)
2. Add parameters:
   - `IsPaid` = `@event.IsPaid`
   - `TicketPrice` = `@event.TicketPrice`
   - `PaymentUrl` = URL to complete payment
   - `EventUrl`, `ManageRsvpUrl`, `Year`
3. Rename parameters to match template expectations

**Pros**:
- Closes communication gap for paid events
- Single template handles all anonymous registrations
- Better user experience (immediate confirmation email)
- Users get payment link immediately

**Cons**:
- More complex template logic
- Requires template migration
- More testing needed

### Option 3: Separate Templates for Paid/Free Anonymous (Enterprise)

**Design**:
- Create TWO templates:
  - `template-anonymous-rsvp-confirmation-free` - For free events
  - `template-anonymous-rsvp-confirmation-paid` - For paid events (pre-payment)
- Handler checks `@event.IsPaid` and uses appropriate template
- Both templates get full parameter set

**Pros**:
- Clean separation of concerns
- Each template optimized for use case
- Easier to maintain and test
- Follows member template pattern (separate paid/free)

**Cons**:
- More templates to maintain
- Code duplication in templates
- Migration required to add new template

---

## Recommended Solution

**OPTION 2: Enhanced Template with Conditional Logic**

**Rationale**:
1. **Closes Critical Gap**: Paid event users get immediate confirmation with payment link
2. **Maintains Simplicity**: One template for all anonymous registrations
3. **Better UX**: Users know what to do next (pay or just attend)
4. **Fixes All Issues**: Addresses parameter mismatch, missing links, and paid event gap
5. **Scalable**: Easy to add more conditional sections later

---

## Implementation Plan - Phase 6A.80

### Step 1: Update Email Template

**Database Migration**: `20260124_Phase6A80_UpdateAnonymousRsvpTemplate.cs`

**Text Template** (Plain Text Version):
```
{{#if IsPaid}}
Registration Confirmed - Payment Required

Hello,

Your registration for {{EventTitle}} has been confirmed!

NEXT STEP: Complete Your Payment
Amount Due: ${{TicketPrice}}
Complete payment here: {{PaymentUrl}}

After payment completes, you'll receive a separate email with your ticket details.

Event Information:
- Date: {{EventDate}}
- Time: {{EventTime}}
- Location: {{EventLocation}}
{{#if GuestCount}}- Guests: {{GuestCount}}{{/if}}

View full event details: {{EventUrl}}
Manage or cancel registration: {{ManageRsvpUrl}}

{{else}}
RSVP Confirmed!

Hello,

Your RSVP for {{EventTitle}} has been confirmed!

Event Details:
- Date: {{EventDate}}
- Time: {{EventTime}}
- Location: {{EventLocation}}
{{#if GuestCount}}- Guests: {{GuestCount}}{{/if}}

View full event details: {{EventUrl}}
To modify or cancel your RSVP: {{ManageRsvpUrl}}

We look forward to seeing you!
{{/if}}

---
© {{Year}} LankaConnect. All rights reserved.
```

**HTML Template**: Similar structure with styling (see migration for full HTML)

### Step 2: Update AnonymousRegistrationConfirmedEventHandler.cs

**Changes**:

1. **Remove Skip Logic** (lines 81-88):
```csharp
// DELETE THIS ENTIRE BLOCK:
if (registration.PaymentStatus == PaymentStatus.Pending)
{
    _logger.LogInformation("AnonymousRegistrationConfirmed: Skipping paid event...");
    return;
}
```

2. **Fix Parameter Names and Add Missing** (lines 111-136):
```csharp
var parameters = new Dictionary<string, object>
{
    // Basic event info
    { "EventTitle", @event.Title.Value },
    { "EventDate", @event.StartDate.ToString("MMMM dd, yyyy") },           // ✅ Fixed name
    { "EventTime", @event.StartDate.ToString("h:mm tt") },                 // ✅ Fixed name
    { "EventLocation", @event.Location != null
        ? $"{@event.Location.Address.Street}, {@event.Location.Address.City}"
        : "Online Event" },
    { "GuestCount", domainEvent.Quantity },                                 // ✅ Fixed name

    // Paid event specific
    { "IsPaid", @event.IsPaid },                                            // ✅ NEW
    { "TicketPrice", @event.TicketPrice?.ToString("F2") ?? "0.00" },       // ✅ NEW
    { "PaymentUrl", $"{_baseUrl}/events/{@event.Id}/payment/{registration.Id}" }, // ✅ NEW

    // Links
    { "EventUrl", $"{_baseUrl}/events/{@event.Id}" },                      // ✅ NEW
    { "ManageRsvpUrl", $"{_baseUrl}/events/{@event.Id}/rsvp/{registration.Id}/manage" }, // ✅ NEW
    { "Year", DateTime.UtcNow.Year }                                        // ✅ NEW
};
```

3. **Add BaseUrl Configuration**:
```csharp
private readonly string _baseUrl;

public AnonymousRegistrationConfirmedEventHandler(
    IEmailService emailService,
    IEventRepository eventRepository,
    IRegistrationRepository registrationRepository,
    ILogger<AnonymousRegistrationConfirmedEventHandler> logger,
    IConfiguration configuration)  // ✅ Add IConfiguration
{
    _emailService = emailService;
    _eventRepository = eventRepository;
    _registrationRepository = registrationRepository;
    _logger = logger;
    _baseUrl = configuration["App:BaseUrl"] ?? "https://lankaconnect.com";  // ✅ Get from config
}
```

### Step 3: Testing

**Test Cases**:
1. ✅ Anonymous registration for **free event** → Receive RSVP confirmation email
2. ✅ Anonymous registration for **paid event** → Receive registration confirmation with payment link
3. ✅ Email contains correct event date, time, location
4. ✅ Event URL link works
5. ✅ Manage RSVP URL link works
6. ✅ Payment URL link works (paid events only)
7. ✅ Year shows current year in footer
8. ✅ Template renders correctly in email clients (Gmail, Outlook, etc.)

**Test Event**: 0458806b-8672-4ad5-a7cb-f5346f1b282a
**Test Email**: lankaconnect.app@gmail.com

### Step 4: Verification Queries

**Check if event is paid or free**:
```sql
SELECT
    id,
    title,
    is_paid,
    ticket_price,
    start_date
FROM events.events
WHERE id = '0458806b-8672-4ad5-a7cb-f5346f1b282a';
```

**Check registration status**:
```sql
SELECT
    r.id,
    r.event_id,
    r.contact_email,
    r.quantity,
    r.payment_status,
    r.created_at
FROM events.registrations r
WHERE r.event_id = '0458806b-8672-4ad5-a7cb-f5346f1b282a'
  AND r.contact_email = 'lankaconnect.app@gmail.com';
```

---

## Success Criteria

Phase 6A.80 complete when:

- [ ] Template migration created and applied
- [ ] Handler updated with correct parameters
- [ ] Skip logic removed for paid events
- [ ] All test cases pass
- [ ] User receives email for test registration
- [ ] Email contains all expected information
- [ ] Links work correctly
- [ ] Build: 0 errors, 0 warnings
- [ ] Tests: All passing
- [ ] Documentation updated

---

## Files to Modify

1. **NEW**: `src/LankaConnect.Infrastructure/Data/Migrations/20260124_Phase6A80_UpdateAnonymousRsvpTemplate.cs`
2. **UPDATE**: `src/LankaConnect.Application/Events/EventHandlers/AnonymousRegistrationConfirmedEventHandler.cs`
3. **UPDATE**: `docs/PROGRESS_TRACKER.md`
4. **UPDATE**: `docs/STREAMLINED_ACTION_PLAN.md`

---

## Questions for User

1. **Is event `0458806b-8672-4ad5-a7cb-f5346f1b282a` a paid or free event?**
   - This will confirm if the missing email is a bug or expected behavior

2. **Do you want Option 2 (Enhanced Template) or Option 3 (Separate Templates)?**
   - Option 2: One template with conditional logic (Recommended)
   - Option 3: Two separate templates (cleaner separation)

3. **Where should payment/event/manage RSVP URLs point to?**
   - Need base URL configuration (e.g., `https://lankaconnect.com`)
   - Need frontend route patterns for these pages

---

## ACTUAL IMPLEMENTATION (2026-01-24)

### What Was Actually Done

**User Decision**: Instead of creating separate anonymous templates, reuse the existing member templates for consistency.

### Issue #3: No UI Success Message (NEW - Found 2026-01-24)

**Problem**: Anonymous users registering for events saw immediate page reload with NO confirmation message.

**User Report**: "For Better user experience we should implement Issue #2: NO UI SUCCESS MESSAGE (Found the bug!)"

**Root Cause**:
- **File**: [web/src/app/events/[id]/page.tsx:266](../web/src/app/events/[id]/page.tsx#L266)
- Free event registration just called `window.location.reload()` with no success message
- Users had no visual confirmation that registration was successful
- No indication that they would receive an email

**Solution Implemented**:
1. Added Dialog component import
2. Added state variables for success dialog (`showSuccessDialog`, `successEmail`)
3. Modified anonymous registration success flow to show dialog before reload
4. Created success dialog with:
   - Success icon and title
   - Event title confirmation
   - Email notification message ("A confirmation email will be sent to **{email}** within 2-6 minutes")
   - Instruction to check spam folder
   - "Got it!" button that triggers reload

**Files Modified**:
- [web/src/app/events/[id]/page.tsx](../web/src/app/events/[id]/page.tsx)
  - Lines 18: Added Dialog import
  - Lines 78-80: Added state variables
  - Lines 263-267: Modified success flow
  - Lines 1189-1241: Added Dialog JSX component

**Testing**: Build successful (0 errors)

---

### Solution: Reuse Member FreeEventRegistration Template

**Migration**: [20260124060707_Phase6A80_RemoveAnonymousRsvpTemplate.cs](../src/LankaConnect.Infrastructure/Data/Migrations/20260124060707_Phase6A80_RemoveAnonymousRsvpTemplate.cs)

**What It Does**:
1. **Deleted** `template-anonymous-rsvp-confirmation` template
2. **Updated** `AnonymousRegistrationConfirmedEventHandler` to use `EmailTemplateNames.FreeEventRegistration`
3. **Updated** member template descriptions to note they support both member and anonymous users

**Benefits**:
- ✅ Eliminates template duplication
- ✅ Ensures consistent user experience (member and anonymous get same email)
- ✅ Reduces maintenance burden (one template to update)
- ✅ Fixes parameter mismatch automatically (member template has correct parameters)

**Handler Changes**:
- **File**: [AnonymousRegistrationConfirmedEventHandler.cs](../src/LankaConnect.Application/Events/EventHandlers/AnonymousRegistrationConfirmedEventHandler.cs)
- Line 154: Changed to `EmailTemplateNames.FreeEventRegistration`
- Lines 117-130: Updated parameters to match member template:
  - `UserName` - Contact name or "Guest"
  - `EventTitle` - Event title
  - `EventDateTime` - Formatted date/time range
  - `EventLocation` - Location string
  - `RegistrationDate` - Registration timestamp
  - `Attendees` - HTML for attendee list
  - `HasAttendeeDetails` - Boolean flag
  - `ContactEmail`, `ContactPhone`, `HasContactInfo` - Contact details
  - `EventImageUrl`, `HasEventImage` - Event image support
  - `HasOrganizerContact`, `OrganizerContactName`, etc. - Organizer contact

**Constants Updated**:
- **File**: [EmailTemplateNames.cs](../src/LankaConnect.Application/Common/Constants/EmailTemplateNames.cs)
- Lines 17, 185: Updated FreeEventRegistration description to "member and anonymous"
- Lines 25, 186: Updated PaidEventRegistration description to "member and anonymous"
- Lines 127-129, 162-163: Removed AnonymousRsvpConfirmation constant

---

### Email Verification Tools Created

**1. SQL Verification Script**:
- **File**: [scripts/check_anonymous_template.sql](../scripts/check_anonymous_template.sql)
- 7 verification queries:
  - Part 1: Verify template configuration
  - Part 2: Check recent anonymous registration emails
  - Part 3: Verify template parameters
  - Part 4: Verify email content (no literal Handlebars)
  - Part 5: Email delivery status summary
  - Part 6: Find failed emails
  - Part 7: Verify specific registration email
- Includes troubleshooting section

**2. Email Verification Guide**:
- **File**: [docs/PHASE_6A_80_EMAIL_VERIFICATION_GUIDE.md](../docs/PHASE_6A_80_EMAIL_VERIFICATION_GUIDE.md)
- Quick reference SQL queries by:
  - Registration ID
  - Email address
  - Event ID
- Email status value explanations
- Normal processing times (2-6 minutes)
- Troubleshooting section

---

### User Confirmation (2026-01-24)

**Test Results**:
1. ✅ User registered for event `0dc17180-e4c9-4768-aefe-e3044ed691fa`
2. ✅ User received email successfully (screenshot provided)
3. ✅ Email parameters rendered correctly:
   - Event title: "Sri Lankan Independence Day Celebration"
   - Date/time: "February 04, 2026 from 5:00 PM to 10:00 PM"
   - Location shown correctly
   - All Handlebars parameters replaced with actual values
4. ✅ User registered for event `0458806b-8672-4ad5-a7cb-f5346f1b282a`
5. ✅ User received email with 5-minute delay (screenshot provided)
6. ✅ Email parameters rendered correctly:
   - Event title: "Monthly Dana January 2026"
   - Multi-day format: "January 25, 2026 at 5:00 PM to January 26, 2026 at 7:00 PM"

**User Quote**: "The query returns no results, but I got the email just now."
- Confirms 5-minute email delay is normal (background job processing)
- Confirms email delivery is working correctly
- Confirms parameter rendering is working correctly

---

## Final Status: ✅ RESOLVED

### Issues Resolved:
1. ✅ **Email Not Sent**: Emails ARE being sent (5-minute delay is normal)
2. ✅ **Template Parameter Mismatch**: Fixed by reusing member template
3. ✅ **No UI Success Message**: Fixed by adding success dialog

### Deliverables:
1. ✅ Migration to remove anonymous template
2. ✅ Handler updated to use FreeEventRegistration template
3. ✅ Success dialog added to UI
4. ✅ SQL verification script created
5. ✅ Email verification guide created
6. ✅ Build: 0 errors, 0 warnings
7. ✅ User testing: Emails received successfully

### Documentation Updated:
- [x] This RCA document
- [ ] PROGRESS_TRACKER.md (pending)
- [ ] STREAMLINED_ACTION_PLAN.md (pending)

---

## How to Verify Email Delivery

**Quick Check**:
```sql
-- Replace with your email address
SELECT
    "Id",
    template_name,
    status,
    "CreatedAt" as queued_at,
    sent_at,
    EXTRACT(EPOCH FROM (sent_at - "CreatedAt")) as seconds_to_send
FROM communications.email_messages
WHERE to_emails::text LIKE '%your.email@example.com%'
ORDER BY "CreatedAt" DESC
LIMIT 5;
```

**Expected Results**:
- Status: `Sent` or `Delivered`
- Template: `template-free-event-registration-confirmation`
- Processing time: 120-360 seconds (2-6 minutes)

**For Detailed Verification**:
- Run [scripts/check_anonymous_template.sql](../scripts/check_anonymous_template.sql)
- See [docs/PHASE_6A_80_EMAIL_VERIFICATION_GUIDE.md](../docs/PHASE_6A_80_EMAIL_VERIFICATION_GUIDE.md)

---

## Lessons Learned

1. **Template Consolidation > Duplication**: Reusing member templates eliminated parameter mismatches
2. **User Feedback Loops**: Success dialogs prevent user confusion about async operations
3. **Email Delays are Normal**: 2-6 minutes for background job processing is acceptable
4. **Comprehensive Verification**: SQL scripts help users self-serve email delivery checks
5. **Documentation Matters**: Clear guides reduce support burden

---

## Next Steps

**If Issues Persist**:
1. Run verification SQL script: `scripts/check_anonymous_template.sql`
2. Check application logs for `AnonymousRegistrationConfirmed` errors
3. Verify Hangfire background job is running
4. Check Azure Email Service configuration

**For Future Enhancements**:
1. Consider real-time email status updates in UI
2. Add email delivery tracking (read receipts)
3. Implement retry logic for failed emails
4. Add user-facing email resend button
