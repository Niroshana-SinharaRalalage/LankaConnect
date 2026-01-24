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
