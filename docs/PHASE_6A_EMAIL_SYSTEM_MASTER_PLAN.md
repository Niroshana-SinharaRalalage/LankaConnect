# Phase 6A Email System Master Plan
**System Architect: Technical Decision Document**
**Date Created**: 2025-12-27
**Last Updated**: 2025-12-27
**Status**: Active Development
**Phase Coverage**: 6A.49 (Complete) → 6A.57 (Event Reminders)

---

## Document Purpose

This master plan serves as the **single source of truth** for all email system work in Phase 6A. It ensures continuity across sessions and prevents requirements from being forgotten.

**Cross-Reference**: See [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md) for phase number assignments.

---

## Executive Summary

### Current State (as of 2025-12-27)

**Completed:**
- ✅ Phase 0: Email configuration infrastructure (SendGrid, queue processor, domain events)
- ✅ Phase 6A.49: Fixed paid event email flow (EF Core tracking issue)
- ✅ Phase 6A.54: Created 4 new email templates with professional HTML layout
- ✅ Phase 6A.52-56: Logging, tracking fixes, category fix, currency fix

**Existing Templates (6 total):**
1. `ticket-confirmation` - Paid event ticket (has professional HTML layout, PDF attachment)
2. `registration-confirmation` - Free event registration (has professional HTML layout)
3. `member-email-verification` - Email verification (NEW Phase 6A.54, professional HTML)
4. `signup-commitment-confirmation` - Item commitment (NEW Phase 6A.54, professional HTML)
5. `registration-cancellation` - Cancellation (NEW Phase 6A.54, professional HTML)
6. `organizer-custom-message` - Organizer to attendees (NEW Phase 6A.54, professional HTML)

**Existing Background Jobs:**
- ✅ `EventReminderJob` - Sends reminders 24 hours before event (runs hourly via Hangfire)
- ✅ `EventStatusUpdateJob` - Updates event statuses (runs hourly via Hangfire)
- ✅ `ExpiredBadgeCleanupJob` - Cleans up expired badges (runs daily via Hangfire)

**Current Reminder Schedule:**
- ❌ **PROBLEM**: Job runs hourly, checks for events in 23-25 hour window
- ❌ **PROBLEM**: Uses ugly plain text HTML (inline in `GenerateEventReminderHtml()`)
- ❌ **PROBLEM**: Sends only 1 reminder (24 hours before event)

---

## New Requirements from User (2025-12-27)

### Requirement 1: Event Reminder Email Improvements (HIGH PRIORITY)

**Current State Analysis:**
```csharp
// File: EventReminderJob.cs (line 403-410)
recurringJobManager.AddOrUpdate<EventReminderJob>(
    "event-reminder-job",
    job => job.ExecuteAsync(),
    Cron.Hourly, // Runs every hour
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
);

// File: EventReminderJob.cs (lines 37-40)
var reminderWindowStart = now.AddHours(23);
var reminderWindowEnd = now.AddHours(25);
// Finds events starting between 23-25 hours from now
```

**Problems Identified:**
1. Inline HTML generation (lines 166-186) - ugly plain text layout
2. Only sends 1 reminder (24 hours before)
3. No database tracking of which reminders were sent (risk of duplicates)

**User Requirements:**
- ✅ Create professional HTML template matching other email templates (orange/rose gradient)
- ✅ Change reminder schedule:
  - ❌ Remove current 24-hour single reminder
  - ✅ Send 1 week before event (168 hours)
  - ✅ Send 2 days before event (48 hours)
  - ✅ Send 1 day before event (24 hours)

**Assigned Phase Number**: **6A.57** (confirmed available in master index)

---

### Requirement 2: Remaining Email Features (FROM ORIGINAL PLAN)

From [EMAIL_SYSTEM_IMPLEMENTATION_PLAN_ARCHITECT_APPROVED.md](./EMAIL_SYSTEM_IMPLEMENTATION_PLAN_ARCHITECT_APPROVED.md):

#### Phase 6A.53: Member Email Verification (7-9 hours) - PARTIALLY COMPLETE
**Status**: Template created in 6A.54, backend implementation NOT started

**Remaining Work:**
- Database migration for verification columns (IsEmailVerified, EmailVerificationToken, EmailVerificationTokenExpiresAt)
- Domain entity methods (GenerateEmailVerificationToken, VerifyEmail, RegenerateEmailVerificationToken)
- Event handler (MemberVerificationRequestedEventHandler)
- API endpoints (/verify-email?token=X, /resend-verification)
- Frontend verification page
- Unit + integration tests

**Dependencies**: Phase 6A.54 (template) - ✅ COMPLETE

#### Phase 6A.51: Signup Commitment Emails (3-4 hours) - TEMPLATE ONLY
**Status**: Template created in 6A.54, backend implementation NOT started

**Remaining Work:**
- Domain event (SignupCommitmentConfirmedEvent)
- Event handler (SignupCommitmentConfirmedEventHandler)
- Trigger from SignUpItem entity when user commits to bringing item
- Unit + integration tests

**Dependencies**: Phase 6A.54 (template) - ✅ COMPLETE

#### Phase 6A.52: Registration Cancellation Emails (3-4 hours) - TEMPLATE ONLY
**Status**: Template created in 6A.54, backend implementation NOT started

**Remaining Work:**
- Domain event (RegistrationCancelledEvent with PaymentStatus for refund info)
- Event handler (RegistrationCancelledEventHandler)
- Trigger from Registration.Cancel() method
- Unit + integration tests

**Dependencies**: Phase 6A.54 (template) - ✅ COMPLETE

#### Phase 6A.50: Manual "Send Email to Attendees" (11-13 hours) - TEMPLATE ONLY
**Status**: Template created in 6A.54, backend implementation NOT started

**Remaining Work:**
- Command (SendOrganizerEventEmailCommand with validation)
- Command handler with HTML sanitization (HtmlSanitizer NuGet)
- Domain event (OrganizerEventEmailRequestedEvent)
- Event handler (OrganizerEventEmailRequestedEventHandler)
- Rate limiting repository method (GetOrganizerEmailCountTodayAsync - max 5 emails/event/day)
- Recipient resolution repository method (GetEmailRecipientsAsync)
- Frontend SendEmailModal component with recipient filters (All/Checked-In/Pending)
- Unit + integration tests

**Dependencies**: Phase 6A.54 (template) - ✅ COMPLETE

---

## Architectural Decisions

### Decision 1: Event Reminder Template Strategy

**Question**: Single template with variable or 3 separate templates?

**Options Evaluated:**

**Option A: Single Template with {{DaysUntilEvent}} Variable**
```
Template: event-reminder.html
Variables: {{EventTitle}}, {{DateTime}}, {{Location}}, {{DaysUntilEvent}}
Subject: "Reminder: {{EventTitle}} starts in {{DaysUntilEvent}}"
```
**Pros:**
- Less code duplication
- Easier to maintain branding consistency
- Single template to update if design changes

**Cons:**
- Subject line less flexible ("starts in 7 days" vs "starts next week")
- Message content less personalized

**Option B: Three Separate Templates**
```
Templates: event-reminder-7days.html, event-reminder-2days.html, event-reminder-1day.html
Each with unique messaging and urgency level
```
**Pros:**
- More personalized messaging
- Different urgency levels (Week = informational, 1 Day = urgent)
- Flexible subject lines

**Cons:**
- 3x maintenance burden
- Risk of branding inconsistency
- More database rows

**DECISION: Option A - Single Template**

**Rationale:**
1. **Maintainability**: Single source of truth for branding (orange/rose gradient)
2. **Code simplicity**: EventReminderJob uses same template, different parameters
3. **Flexibility**: Template can include conditional content based on {{DaysUntilEvent}}
4. **DRY Principle**: Don't Repeat Yourself - aligns with Clean Architecture

**Template Variables:**
```csharp
{
    "UserName": "John Doe",
    "EventTitle": "Community Potluck Dinner",
    "EventDateTime": "Saturday, February 10, 2025 at 6:00 PM",
    "EventLocation": "Community Center, 123 Main St, Colombo",
    "EventDetailsUrl": "https://lankaconnect.com/events/abc-123",
    "DaysUntilEvent": 7, // or 2, or 1
    "HoursUntilEvent": 168.0, // Precise hours for "Starting in X hours"
    "Quantity": 2, // Number of tickets/attendees
    "ConfirmationNumber": "REG-2025-001234",
    "QRCodeUrl": "https://api.lankaconnect.com/qr/REG-2025-001234.png" // For paid events
}
```

**Template Content Strategy:**
```html
<!-- Different messaging based on days until event -->
{{#if (eq DaysUntilEvent 7)}}
    <p>Just a heads up! Your event is coming up next week.</p>
{{else if (eq DaysUntilEvent 2)}}
    <p>Your event is just 2 days away. Don't forget to mark your calendar!</p>
{{else if (eq DaysUntilEvent 1)}}
    <p><strong>Reminder:</strong> Your event is tomorrow! We look forward to seeing you there.</p>
{{/if}}
```

**NOTE**: Since we're using C# string interpolation (not Handlebars), we'll pass a pre-formatted `ReminderMessage` variable instead.

---

### Decision 2: Event Reminder Scheduling Strategy

**Question**: How to send 3 reminders (7 days, 2 days, 1 day) without duplicates?

**Current Implementation Issues:**
```csharp
// EventReminderJob runs HOURLY
// Checks for events in 23-25 hour window
// NO TRACKING of which reminders were sent
// Risk: Event starting in 24 hours gets reminder EVERY HOUR (24 duplicate emails!)
```

**Options Evaluated:**

**Option A: Multiple Time Windows (Current Approach Enhanced)**
```csharp
// Send 7-day reminder: Check events in 167-169 hour window
// Send 2-day reminder: Check events in 47-49 hour window
// Send 1-day reminder: Check events in 23-25 hour window
// NO database tracking needed (window prevents duplicates)
```
**Pros:**
- Simple implementation
- No database schema changes
- Works with existing hourly job

**Cons:**
- Timing imprecise (events starting at hour 23.5 get reminder, hour 22.5 don't)
- Risk of missed reminders if job fails during specific hour
- No audit trail of sent reminders

**Option B: Database Tracking Table**
```sql
CREATE TABLE communications.event_reminders_sent (
    id uuid PRIMARY KEY,
    event_id uuid NOT NULL,
    registration_id uuid NOT NULL,
    reminder_type varchar(50) NOT NULL, -- '7_days', '2_days', '1_day'
    sent_at timestamp NOT NULL,
    UNIQUE(event_id, registration_id, reminder_type)
);
```
**Pros:**
- Guaranteed no duplicates (database constraint)
- Audit trail for debugging
- Can resend failed reminders
- Precise timing (send exactly once regardless of job schedule)

**Cons:**
- Database schema change
- More complex query logic
- Table grows over time (needs cleanup job)

**Option C: Event Entity Tracking with Timestamps**
```csharp
public class Event {
    public DateTimeOffset? Reminder7DaysSentAt { get; set; }
    public DateTimeOffset? Reminder2DaysSentAt { get; set; }
    public DateTimeOffset? Reminder1DaySentAt { get; set; }
}
```
**Pros:**
- Simple schema (just 3 columns on existing table)
- Easy to query "which reminders haven't been sent"
- No separate cleanup needed

**Cons:**
- Denormalized (what if event has 100 registrations? Same timestamp for all?)
- Can't track per-registration failures
- Event entity bloated with email concerns

**DECISION: Option A - Multiple Time Windows (Enhanced with Safety Checks)**

**Rationale:**
1. **Zero schema changes**: No migration needed, faster implementation
2. **Existing infrastructure**: Hangfire hourly job already configured
3. **Low risk**: 2-hour windows (167-169, 47-49, 23-25) prevent duplicates
4. **Email queue is idempotent**: EmailService already handles duplicate prevention
5. **Logging provides audit trail**: Existing ILogger tracks all sent emails

**Implementation Strategy:**
```csharp
public async Task ExecuteAsync()
{
    var now = DateTime.UtcNow;

    // Check 7-day reminders (167-169 hours)
    await SendRemindersAsync(now.AddHours(167), now.AddHours(169), 7, "7 days");

    // Check 2-day reminders (47-49 hours)
    await SendRemindersAsync(now.AddHours(47), now.AddHours(49), 2, "2 days");

    // Check 1-day reminders (23-25 hours)
    await SendRemindersAsync(now.AddHours(23), now.AddHours(25), 1, "1 day");
}

private async Task SendRemindersAsync(DateTime windowStart, DateTime windowEnd, int daysUntil, string label)
{
    var events = await _eventRepository.GetEventsStartingInTimeWindowAsync(windowStart, windowEnd, ...);

    foreach (var @event in events)
    {
        _logger.LogInformation(
            "Sending {Label} reminder for event {EventId} ({Title})",
            label, @event.Id, @event.Title);

        // Send to all registrations...
    }
}
```

**Safety Mechanisms:**
1. **Window size**: 2-hour windows ensure events only match once per reminder interval
2. **Logging**: Every reminder logged with event ID + registration ID for audit trail
3. **Email queue deduplication**: EmailMessage table has unique constraint on (ToEmail, Subject, ScheduledFor)
4. **Fail-silent**: Exceptions caught per registration, doesn't stop batch

**Future Enhancement** (Phase 6A.58+):
- If audit requirements increase, implement Option B (tracking table)
- Can be added without changing EventReminderJob logic

---

### Decision 3: Email Template Attachment Strategy

**Question**: Should event reminders include PDF ticket attachment (like paid event emails)?

**Analysis:**

**Paid Event Emails (ticket-confirmation template):**
- ✅ Includes PDF ticket with QR code
- Use case: User needs ticket for entry + payment receipt

**Free Event Emails (registration-confirmation template):**
- ❌ No PDF attachment
- Use case: Lightweight confirmation

**Event Reminder Emails:**
- Use case: Remind user of upcoming event
- User already has confirmation email with ticket (if paid)

**Options:**

**Option A: Include PDF ticket in all reminders (paid events only)**
```csharp
if (@event.PricingType == PricingType.Paid)
{
    // Attach PDF ticket to reminder email
}
```
**Pros:**
- User can re-download ticket if lost original email
- Convenient "single email with everything"

**Cons:**
- Larger email size (PDF ~50KB)
- More SendGrid bandwidth usage
- Slower email processing

**Option B: Link to ticket download (no attachment)**
```csharp
var parameters = new Dictionary<string, object>
{
    { "TicketDownloadUrl", $"https://lankaconnect.com/my-tickets/{registration.Id}" }
};
```
**Pros:**
- Lightweight emails
- Faster processing
- Less SendGrid costs

**Cons:**
- User must click link (extra step)
- Requires internet to view ticket

**DECISION: Option B - Link to Ticket Download (No Attachment)**

**Rationale:**
1. **Performance**: Event reminders sent in bulk (could be 1000+ emails), attachments slow processing
2. **Cost**: SendGrid charges per email size, PDF attachments increase costs
3. **UX**: Users already have ticket from confirmation email (sent immediately after registration)
4. **Redundancy**: Reminder is just a reminder, not a ticket delivery mechanism
5. **Flexibility**: Template includes `{{EventDetailsUrl}}` and `{{ManageBookingUrl}}` for ticket access

**Template Content:**
```html
<div class="ticket-info" style="background: #f3f4f6; padding: 16px; border-radius: 8px;">
    <h3>Your Ticket</h3>
    <p>Confirmation #: {{ConfirmationNumber}}</p>
    <p>Attendees: {{Quantity}}</p>
    <a href="{{ManageBookingUrl}}" class="button">View/Download Ticket</a>
</div>
```

---

### Decision 4: Phase Sequencing and Implementation Order

**Question**: What's the optimal order to implement remaining email features?

**Factors Considered:**
1. **User urgency**: Event reminders are user's top priority (mentioned first)
2. **Business impact**: Reminder improvements affect ALL events going forward
3. **Technical dependencies**: Templates already created in Phase 6A.54
4. **Complexity**: Easier tasks first builds momentum
5. **Risk**: High-risk features (rate limiting, HTML sanitization) need more time

**Recommended Sequence:**

| Phase | Feature | Priority | Complexity | Hours | Why This Order? |
|-------|---------|----------|------------|-------|-----------------|
| **6A.57** | Event Reminder Improvements | **P0** | Low-Medium | 3-4 | **USER URGENT REQUEST**, low risk, high impact |
| **6A.51** | Signup Commitment Emails | P1 | Low | 3-4 | Simple domain event + handler, builds momentum |
| **6A.52** | Registration Cancellation Emails | P1 | Low | 3-4 | Simple domain event + handler, similar to 6A.51 |
| **6A.53** | Member Email Verification | P1 | Medium-High | 7-9 | Security-critical, needs GUID tokens + rate limiting |
| **6A.50** | Manual Organizer Emails | P1 | High | 11-13 | Most complex: HTML sanitization, rate limiting, recipient filters |

**Rationale for Order:**

1. **6A.57 First** (Event Reminders):
   - User's immediate concern (ugly emails going out to real users)
   - Fast win (3-4 hours) builds confidence
   - No schema changes (uses existing infrastructure)
   - High visibility (affects all future events)

2. **6A.51 & 6A.52 Next** (Signup + Cancellation):
   - Low complexity (domain event + handler pattern)
   - Templates already created
   - Build momentum with quick completions
   - Unblock user testing of full workflow

3. **6A.53 Third** (Email Verification):
   - Security-critical, needs focused attention
   - GUID token generation + expiry logic
   - Rate limiting (3 emails/hour/user)
   - Frontend verification page

4. **6A.50 Last** (Organizer Emails):
   - Most complex feature (11-13 hours)
   - Requires HtmlSanitizer NuGet package
   - Rate limiting (5 emails/event/day)
   - Recipient resolution logic (All/Checked-In/Pending)
   - Frontend modal with rich text editor
   - Save for when all patterns established

**Estimated Timeline:**

| Week | Phases | Total Hours | Deliverables |
|------|--------|-------------|--------------|
| Week 1 | 6A.57 + 6A.51 + 6A.52 | 10-12 hours | Reminders improved, commitment + cancellation emails working |
| Week 2 | 6A.53 | 7-9 hours | Email verification system complete |
| Week 3 | 6A.50 | 11-13 hours | Organizer email feature complete |
| **Total** | **5 phases** | **28-34 hours** | **Complete email system** |

---

## Tracking Mechanism Design

### Problem Statement
User wants a robust way to remember the full plan without forgetting requirements across sessions.

### Solution: Multi-Level Documentation Strategy

#### Level 1: Master Plan (THIS DOCUMENT)
**File**: `docs/PHASE_6A_EMAIL_SYSTEM_MASTER_PLAN.md`

**Purpose**: Single source of truth for email system technical decisions

**Contents:**
- Architectural decisions with rationale
- Template strategy
- Scheduling strategy
- Phase sequencing
- Acceptance criteria
- Technical specifications

**Update Frequency**: After each architectural decision or phase completion

#### Level 2: Phase Master Index
**File**: `docs/PHASE_6A_MASTER_INDEX.md`

**Purpose**: Registry of all Phase 6A features with status

**Contents:**
- Phase number assignments
- Feature descriptions
- Status (Complete/Blocked/Planned)
- Links to summary documents

**Update Frequency**: Before starting new phase, after completing phase

#### Level 3: Progress Tracker
**File**: `docs/PROGRESS_TRACKER.md`

**Purpose**: Session-by-session development log

**Contents:**
- Current session status
- Work completed
- Build status
- Deployment status
- Historical log

**Update Frequency**: At end of each session

#### Level 4: Phase Summary Documents
**Files**: `docs/PHASE_6A[X]_[FEATURE]_SUMMARY.md`

**Purpose**: Detailed implementation record for completed phases

**Contents:**
- Problem statement
- Solution approach
- Files modified
- Tests written
- Deployment notes
- Lessons learned

**Update Frequency**: Created after phase completion

### Synchronization Protocol

**Before Starting Any Phase:**
1. ✅ Check Phase Master Index for next available phase number
2. ✅ Read Master Plan (this document) for architectural decisions
3. ✅ Update Progress Tracker with current session status
4. ✅ Review any related phase summaries for context

**During Phase Implementation:**
1. ✅ Follow architectural decisions from Master Plan
2. ✅ Log significant decisions/changes in session notes
3. ✅ Use TodoWrite tool to track sub-tasks

**After Phase Completion:**
1. ✅ Create Phase Summary document
2. ✅ Update Phase Master Index with status + link to summary
3. ✅ Update Progress Tracker with completion status
4. ✅ Update Master Plan if architectural decisions changed
5. ✅ Commit all documentation changes with code

**Session Handoff Checklist:**
```
[ ] Master Plan reviewed
[ ] Phase Master Index shows current phase status
[ ] Progress Tracker updated with session summary
[ ] All documentation committed to Git
[ ] Build status: 0 errors, 0 warnings
[ ] Next phase identified and documented
```

---

## Phase 6A.57: Event Reminder Email Improvements

### Priority
**P0 - User Urgent Request**

### Estimated Effort
**3-4 hours**

### Dependencies
- ✅ Phase 6A.54 complete (templates created)
- ✅ Hangfire background jobs configured
- ✅ EventReminderJob infrastructure exists

### Problem Statement

**Current State:**
- Event reminders sent 24 hours before event
- Uses ugly inline HTML (plain text layout)
- No professional branding
- Only 1 reminder per event

**User Requirements:**
- Professional HTML template matching other emails (orange/rose gradient)
- Send 3 reminders: 7 days, 2 days, 1 day before event
- Remove current 24-hour only schedule

### Solution Design

#### Component 1: Email Template
**File**: `src/LankaConnect.Infrastructure/Data/Migrations/YYYYMMDD_Phase6A57_AddEventReminderTemplate.cs`

**Template Name**: `event-reminder`

**Subject Line**: `Reminder: {{EventTitle}} starts in {{ReminderTimeframe}}`
- 7 days: "Reminder: Community Potluck starts in 1 week"
- 2 days: "Reminder: Community Potluck starts in 2 days"
- 1 day: "Reminder: Community Potluck starts tomorrow"

**Template Variables:**
```csharp
{
    "UserName": string,              // "John Doe"
    "EventTitle": string,            // "Community Potluck Dinner"
    "EventDateTime": string,         // "Saturday, Feb 10, 2025 at 6:00 PM"
    "EventLocation": string,         // "Community Center, 123 Main St"
    "EventDetailsUrl": string,       // "https://lankaconnect.com/events/abc-123"
    "ManageBookingUrl": string,      // "https://lankaconnect.com/bookings/xyz-789"
    "ConfirmationNumber": string,    // "REG-2025-001234"
    "Quantity": int,                 // 2
    "ReminderTimeframe": string,     // "1 week" or "2 days" or "tomorrow"
    "DaysUntilEvent": int,           // 7 or 2 or 1
    "HoursUntilEvent": decimal,      // 168.0 or 48.0 or 24.0
    "ReminderMessage": string        // Pre-formatted HTML message based on days
}
```

**HTML Template Structure** (same orange/rose gradient as other templates):
```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <style>
        /* Same CSS as ticket-confirmation and other templates */
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; }
        .header { background: linear-gradient(135deg, #fb923c 0%, #f43f5e 100%); }
        .button { background: linear-gradient(135deg, #fb923c 0%, #f43f5e 100%); }
        /* ... */
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>Event Reminder</h1>
        </div>
        <div class="content">
            <p>Hi {{UserName}},</p>
            {{ReminderMessage}}
            <div class="event-details">
                <h3>{{EventTitle}}</h3>
                <p><strong>When:</strong> {{EventDateTime}}</p>
                <p><strong>Where:</strong> {{EventLocation}}</p>
                <p><strong>Your Tickets:</strong> {{Quantity}}</p>
                <p><strong>Confirmation:</strong> {{ConfirmationNumber}}</p>
            </div>
            <a href="{{EventDetailsUrl}}" class="button">View Event Details</a>
            <a href="{{ManageBookingUrl}}" class="button">Manage Booking</a>
        </div>
        <div class="footer">
            <p><strong>LankaConnect</strong> - Connecting Sri Lankan Communities</p>
            <p>If you need to cancel or modify your registration, visit your <a href="{{ManageBookingUrl}}">booking page</a>.</p>
        </div>
    </div>
</body>
</html>
```

**ReminderMessage Variable Content:**
```csharp
private string GetReminderMessage(int daysUntilEvent)
{
    return daysUntilEvent switch
    {
        7 => "<p>Just a friendly reminder that your event is coming up <strong>next week</strong>!</p>" +
             "<p>We wanted to give you plenty of time to prepare and add it to your calendar.</p>",

        2 => "<p>Your event is just <strong>2 days away</strong>!</p>" +
             "<p>Don't forget to mark your calendar and plan your trip to the venue.</p>",

        1 => "<p><strong>Your event is tomorrow!</strong></p>" +
             "<p>We're looking forward to seeing you there. Here are your event details:</p>",

        _ => "<p>This is a reminder about your upcoming event.</p>"
    };
}
```

#### Component 2: Update EventReminderJob
**File**: `src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs`

**Changes:**

1. **Replace inline HTML generation** with template-based emails:
```csharp
// DELETE lines 166-186 (GenerateEventReminderHtml method)

// REPLACE with:
private async Task<Result> SendReminderEmailAsync(
    Event @event,
    Registration registration,
    string toEmail,
    string toName,
    int daysUntilEvent,
    CancellationToken cancellationToken)
{
    var reminderTimeframe = daysUntilEvent switch
    {
        7 => "1 week",
        2 => "2 days",
        1 => "tomorrow",
        _ => $"{daysUntilEvent} days"
    };

    var parameters = new Dictionary<string, object>
    {
        { "UserName", toName },
        { "EventTitle", @event.Title.Value },
        { "EventDateTime", @event.StartDate.ToString("dddd, MMMM dd, yyyy 'at' h:mm tt") },
        { "EventLocation", @event.Location?.Address.ToString() ?? "Location TBD" },
        { "EventDetailsUrl", $"https://lankaconnect.com/events/{@event.Id}" },
        { "ManageBookingUrl", $"https://lankaconnect.com/bookings/{registration.Id}" },
        { "ConfirmationNumber", registration.ConfirmationCode ?? "N/A" },
        { "Quantity", registration.Quantity },
        { "ReminderTimeframe", reminderTimeframe },
        { "DaysUntilEvent", daysUntilEvent },
        { "HoursUntilEvent", Math.Round((@event.StartDate - DateTime.UtcNow).TotalHours, 1) },
        { "ReminderMessage", GetReminderMessage(daysUntilEvent) }
    };

    return await _emailService.SendTemplatedEmailAsync(
        "event-reminder",
        toEmail,
        parameters,
        cancellationToken);
}
```

2. **Update ExecuteAsync to send 3 reminder types**:
```csharp
public async Task ExecuteAsync()
{
    _logger.LogInformation(
        "[Phase 6A.57] EventReminderJob: Starting execution at {Time}",
        DateTime.UtcNow);

    try
    {
        var now = DateTime.UtcNow;

        // Send 7-day reminders (167-169 hour window)
        await SendRemindersForWindowAsync(
            now.AddHours(167),
            now.AddHours(169),
            daysUntilEvent: 7,
            label: "7-day");

        // Send 2-day reminders (47-49 hour window)
        await SendRemindersForWindowAsync(
            now.AddHours(47),
            now.AddHours(49),
            daysUntilEvent: 2,
            label: "2-day");

        // Send 1-day reminders (23-25 hour window)
        await SendRemindersForWindowAsync(
            now.AddHours(23),
            now.AddHours(25),
            daysUntilEvent: 1,
            label: "1-day");

        _logger.LogInformation(
            "[Phase 6A.57] EventReminderJob: Completed execution at {Time}",
            DateTime.UtcNow);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "[Phase 6A.57] EventReminderJob: Fatal error during execution");
    }
}

private async Task SendRemindersForWindowAsync(
    DateTime windowStart,
    DateTime windowEnd,
    int daysUntilEvent,
    string label)
{
    _logger.LogInformation(
        "[Phase 6A.57] Checking for events requiring {Label} reminders (window: {Start} to {End})",
        label, windowStart, windowEnd);

    var upcomingEvents = await _eventRepository.GetEventsStartingInTimeWindowAsync(
        windowStart,
        windowEnd,
        new[] { EventStatus.Published, EventStatus.Active });

    _logger.LogInformation(
        "[Phase 6A.57] Found {Count} events for {Label} reminders",
        upcomingEvents.Count, label);

    foreach (var @event in upcomingEvents)
    {
        try
        {
            var registrations = @event.Registrations;

            _logger.LogInformation(
                "[Phase 6A.57] Sending {Label} reminders for event {EventId} ({Title}) to {Count} attendees",
                label, @event.Id, @event.Title.Value, registrations.Count);

            foreach (var registration in registrations)
            {
                try
                {
                    // Determine recipient email (authenticated user, anonymous contact, or legacy)
                    string? toEmail = null;
                    string toName = "Event Attendee";

                    if (registration.UserId.HasValue)
                    {
                        var user = await _userRepository.GetByIdAsync(
                            registration.UserId.Value,
                            CancellationToken.None);

                        if (user != null)
                        {
                            toEmail = user.Email.Value;
                            toName = $"{user.FirstName} {user.LastName}";
                        }
                    }
                    else if (registration.Contact != null)
                    {
                        toEmail = registration.Contact.Email;
                        var firstAttendee = registration.Attendees.FirstOrDefault();
                        if (firstAttendee != null)
                            toName = firstAttendee.Name;
                    }
                    else if (registration.AttendeeInfo != null)
                    {
                        toEmail = registration.AttendeeInfo.Email.Value;
                        toName = registration.AttendeeInfo.Name;
                    }

                    if (string.IsNullOrWhiteSpace(toEmail))
                    {
                        _logger.LogWarning(
                            "[Phase 6A.57] No email found for registration {RegistrationId}, skipping",
                            registration.Id);
                        continue;
                    }

                    var result = await SendReminderEmailAsync(
                        @event,
                        registration,
                        toEmail,
                        toName,
                        daysUntilEvent,
                        CancellationToken.None);

                    if (result.IsFailure)
                    {
                        _logger.LogWarning(
                            "[Phase 6A.57] Failed to send {Label} reminder for event {EventId} to {Email}: {Errors}",
                            label, @event.Id, toEmail, string.Join(", ", result.Errors));
                    }
                    else
                    {
                        _logger.LogInformation(
                            "[Phase 6A.57] {Label} reminder sent successfully for event {EventId} to {Email}",
                            label, @event.Id, toEmail);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[Phase 6A.57] Error sending {Label} reminder for registration {RegistrationId}",
                        label, registration.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.57] Error processing {Label} reminders for event {EventId}",
                label, @event.Id);
        }
    }
}
```

#### Component 3: Database Migration
**File**: `src/LankaConnect.Infrastructure/Data/Migrations/YYYYMMDD_Phase6A57_AddEventReminderTemplate.cs`

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Insert event-reminder template
    migrationBuilder.InsertData(
        table: "email_templates",
        schema: "communications",
        columns: new[] { "id", "name", "subject", "body_html", "body_text", "category", "type", "is_active", "created_at", "updated_at" },
        values: new object[]
        {
            Guid.NewGuid(),
            "event-reminder",
            "Reminder: {{EventTitle}} starts in {{ReminderTimeframe}}",
            @"<!-- Full HTML template here -->",
            @"Hi {{UserName}},

{{ReminderMessage}}

Event: {{EventTitle}}
When: {{EventDateTime}}
Where: {{EventLocation}}
Your Tickets: {{Quantity}}
Confirmation: {{ConfirmationNumber}}

View Event Details: {{EventDetailsUrl}}
Manage Booking: {{ManageBookingUrl}}

Best regards,
LankaConnect Team",
            "EventManagement",
            (int)EmailType.EventNotification,
            true,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        });
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DeleteData(
        table: "email_templates",
        schema: "communications",
        keyColumn: "name",
        keyValue: "event-reminder");
}
```

#### Component 4: Update EmailTemplateNames
**File**: `src/LankaConnect.Infrastructure/Email/Configuration/EmailTemplateNames.cs`

```csharp
/// <summary>
/// Event reminder email (sent 7 days, 2 days, and 1 day before event).
/// Variables: {UserName}, {EventTitle}, {EventDateTime}, {EventLocation},
///           {ReminderTimeframe}, {DaysUntilEvent}, {Quantity}, {ConfirmationNumber}
/// Status: ✅ Phase 6A.57 (Professional HTML template)
/// </summary>
public const string EventReminder = "event-reminder";
```

### Testing Requirements

#### Unit Tests
**File**: `tests/LankaConnect.Application.Tests/Events/BackgroundJobs/EventReminderJobTests.cs`

1. **Test: 7-day reminder sent for event starting in 168 hours**
2. **Test: 2-day reminder sent for event starting in 48 hours**
3. **Test: 1-day reminder sent for event starting in 24 hours**
4. **Test: No reminders sent for event starting in 100 hours** (falls between windows)
5. **Test: No duplicate reminders sent if job runs multiple times in same window**
6. **Test: Failed email doesn't stop batch processing**
7. **Test: Correct ReminderMessage generated for each timeframe**

#### Integration Tests
**File**: `tests/LankaConnect.IntegrationTests/Events/EventReminderJobIntegrationTests.cs`

1. **Test: Full workflow - 3 reminders sent for event lifecycle**
   - Create event starting in 8 days
   - Run job, verify 7-day reminder sent
   - Advance time to 3 days before
   - Run job, verify 2-day reminder sent
   - Advance time to 25 hours before
   - Run job, verify 1-day reminder sent

2. **Test: Email template variables populated correctly**
3. **Test: Authenticated user vs anonymous registration handling**
4. **Test: Paid vs free event reminders (same template, different variables)**

#### Manual Testing Checklist
1. [ ] Create test event starting in 7.5 days
2. [ ] Trigger EventReminderJob manually (via Hangfire dashboard)
3. [ ] Verify 7-day reminder email received with professional HTML
4. [ ] Check email uses orange/rose gradient matching other templates
5. [ ] Verify links work (EventDetailsUrl, ManageBookingUrl)
6. [ ] Fast-forward event start date to 2.5 days
7. [ ] Trigger job again, verify 2-day reminder sent
8. [ ] Fast-forward to 24.5 hours, verify 1-day reminder sent
9. [ ] Verify no duplicate reminders sent if job runs multiple times

### Acceptance Criteria

- [ ] `event-reminder` template created in database with professional HTML layout
- [ ] Template uses same orange/rose gradient (#fb923c → #f43f5e) as other templates
- [ ] Template is mobile-responsive (max-width: 600px)
- [ ] Plain text version included for email clients without HTML support
- [ ] EventReminderJob sends 3 reminders (7 days, 2 days, 1 day before event)
- [ ] Time windows prevent duplicate reminders (167-169h, 47-49h, 23-25h)
- [ ] ReminderMessage variable contains personalized content based on days until event
- [ ] All template variables populated correctly (UserName, EventTitle, etc.)
- [ ] Links work correctly (EventDetailsUrl, ManageBookingUrl)
- [ ] Logging includes phase number [Phase 6A.57] for audit trail
- [ ] Fail-silent pattern: Email failures don't stop batch processing
- [ ] Unit tests cover all reminder timeframes and edge cases
- [ ] Integration tests verify full workflow
- [ ] Manual testing on staging environment successful
- [ ] Build: 0 errors, 0 warnings
- [ ] Documentation: EMAIL_TEMPLATE_VARIABLES.md updated
- [ ] Documentation: Phase summary document created

### Rollout Strategy

1. **Deploy to Staging**:
   - Run migration to create template
   - Test with real events in staging database
   - Verify Hangfire job logs show 3 reminder types

2. **Monitor First Week**:
   - Check SendGrid delivery rates
   - Monitor user complaints/feedback
   - Verify no duplicate emails reported

3. **Deploy to Production**:
   - Apply migration during low-traffic window
   - Monitor first 24 hours closely
   - Be ready to disable job via Hangfire dashboard if issues

4. **Success Metrics**:
   - Email delivery rate > 95%
   - No duplicate reminder complaints
   - Positive user feedback on professional layout

---

## Remaining Phases: Quick Reference

### Phase 6A.51: Signup Commitment Emails (3-4 hours)
**Template**: ✅ Created in 6A.54
**Backend**: ❌ Not started
**Frontend Trigger**: SignUpItem entity when user commits to bringing item

**Key Tasks:**
- Create `SignupCommitmentConfirmedEvent` domain event
- Create `SignupCommitmentConfirmedEventHandler`
- Trigger from `SignUpItem.Commit()` or similar method
- Unit tests + integration tests

---

### Phase 6A.52: Registration Cancellation Emails (3-4 hours)
**Template**: ✅ Created in 6A.54
**Backend**: ❌ Not started
**Frontend Trigger**: Registration.Cancel() method

**Key Tasks:**
- Create `RegistrationCancelledEvent` with PaymentStatus property
- Create `RegistrationCancelledEventHandler`
- Update `Registration.Cancel()` to raise event
- Calculate refund amount based on cancellation policy
- Unit tests + integration tests

---

### Phase 6A.53: Member Email Verification (7-9 hours)
**Template**: ✅ Created in 6A.54
**Backend**: ❌ Not started (complex security requirements)
**Frontend**: ❌ Verification page not created

**Key Tasks:**
- Database migration (3 columns + 2 indexes)
- GUID-based token generation (security requirement)
- Token expiry logic (24 hours)
- Rate limiting (3 emails/hour/user)
- Resend verification email with 1-hour cooldown
- Frontend verification page (/verify-email?token=X)
- Unit tests + integration tests + Azure staging verification

---

### Phase 6A.50: Manual Organizer Emails (11-13 hours)
**Template**: ✅ Created in 6A.54
**Backend**: ❌ Not started (most complex feature)
**Frontend**: ❌ SendEmailModal not created

**Key Tasks:**
- Install HtmlSanitizer NuGet package
- Create `SendOrganizerEventEmailCommand` with validation
- Create command handler with HTML sanitization
- Create domain event `OrganizerEventEmailRequestedEvent`
- Create event handler `OrganizerEventEmailRequestedEventHandler`
- Implement rate limiting (5 emails/event/day)
- Implement recipient resolution (All/Checked-In/Pending filters)
- Create repository methods (GetOrganizerEmailCountTodayAsync, GetEmailRecipientsAsync)
- Frontend SendEmailModal with rich text editor + recipient filters
- Unit tests + integration tests + Azure staging verification

---

## Critical Requirements: Security & Quality

### Security Requirements (ALL PHASES)

1. **Email Injection Prevention**:
   - Sanitize all subject lines (remove \r\n\0\t)
   - Sanitize all HTML content (HtmlSanitizer library)
   - Validate all email addresses (regex + DNS check)

2. **Rate Limiting**:
   - Member verification: Max 3 emails/hour/user
   - Organizer emails: Max 5 emails/event/day
   - Event reminders: Natural rate limiting via time windows

3. **Token Security**:
   - Use Guid.NewGuid().ToString("N") for tokens (32 hex chars, unpredictable)
   - NOT Base64 (predictable patterns)
   - One-time use (null token after verification)
   - Expiry enforcement (database index for cleanup)

4. **Fail-Silent Pattern**:
   - Email failures logged but don't throw exceptions
   - Domain event handlers catch all exceptions
   - Failed emails don't block domain operations

### Quality Requirements (ALL PHASES)

1. **Test Coverage**:
   - Unit tests: ≥90% coverage per phase
   - Integration tests: Full workflow end-to-end
   - Manual testing: Azure staging verification

2. **Build Quality**:
   - Zero tolerance: 0 errors, 0 warnings
   - Before commit, after commit, after deployment

3. **Logging Standards**:
   - All email operations logged with phase number: `[Phase 6A.XX]`
   - Include: EventId, RegistrationId, Email (masked), Template, Result
   - Use structured logging (ILogger with parameters)

4. **Documentation Standards**:
   - Update EMAIL_TEMPLATE_VARIABLES.md for all new variables
   - Create phase summary after completion
   - Update all 3 tracking docs (Master Index, Progress Tracker, Action Plan)
   - Commit documentation with code (same PR)

---

## Change History

| Date | Change | Reason |
|------|--------|--------|
| 2025-12-27 | Document created | User request for robust plan tracking |
| 2025-12-27 | Decision 1: Single event reminder template | Maintainability over flexibility |
| 2025-12-27 | Decision 2: Multiple time windows (no DB tracking) | Zero schema changes, existing infrastructure |
| 2025-12-27 | Decision 3: No PDF attachments in reminders | Performance over convenience |
| 2025-12-27 | Decision 4: Phase sequencing (6A.57 first) | User urgency + quick win |
| 2025-12-27 | Phase 6A.57 specification complete | Ready for implementation |

---

## References

### Primary Documents
- [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md) - Phase number registry
- [EMAIL_SYSTEM_IMPLEMENTATION_PLAN_ARCHITECT_APPROVED.md](./EMAIL_SYSTEM_IMPLEMENTATION_PLAN_ARCHITECT_APPROVED.md) - Original plan
- [EMAIL_TEMPLATE_VARIABLES.md](./EMAIL_TEMPLATE_VARIABLES.md) - Template variable reference
- [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md) - Session log

### Code References
- `src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs` - Current implementation
- `src/LankaConnect.API/Program.cs` (lines 403-410) - Hangfire job registration
- `src/LankaConnect.Domain/Communications/Enums/EmailType.cs` - Email type enum
- `src/LankaConnect.Infrastructure/Email/Configuration/EmailTemplateNames.cs` - Template constants

### Architecture References
- Clean Architecture + DDD principles
- Domain Events pattern (MediatR)
- Repository Pattern (no DbContext in application layer)
- Fail-Silent pattern (email operations)

---

**Last Updated**: 2025-12-27
**Next Review**: After Phase 6A.57 completion
**Maintained By**: System Architect
