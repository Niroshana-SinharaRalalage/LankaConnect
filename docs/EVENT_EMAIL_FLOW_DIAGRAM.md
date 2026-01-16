# Event Notification Email Flow - Complete System Diagram

**Date**: 2026-01-16
**Purpose**: Visual representation of email sending flow with failure point marked

---

## COMPLETE EMAIL SENDING FLOW

```
┌────────────────────────────────────────────────────────────────────────┐
│                         FRONTEND (React/TypeScript)                     │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Event Communications Page                                              │
│  ┌──────────────────────────────────┐                                  │
│  │ Event: "Sri Lankan New Year"     │                                  │
│  │                                   │                                  │
│  │ Recipients: 5 (Email Groups: 3,  │                                  │
│  │              Subscribers: 2)      │                                  │
│  │                                   │                                  │
│  │  [Send an Email] ← User clicks   │                                  │
│  └──────────────────────────────────┘                                  │
│                    │                                                     │
│                    │ POST /api/events/{id}/notification                │
│                    ↓                                                     │
└────────────────────────────────────────────────────────────────────────┘
                     │
                     │
┌────────────────────┼────────────────────────────────────────────────────┐
│                    ↓         API LAYER (ASP.NET Core)                   │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  EventsController.SendEventNotification()                               │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │ 1. Log incoming request                                           │ │
│  │ 2. Create SendEventNotificationCommand                            │ │
│  │ 3. Send to MediatR: await Mediator.Send(command)                 │ │
│  │ 4. Return 200 OK immediately                                      │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                    │                                                     │
│                    │                                                     │
│                    ↓                                                     │
└────────────────────────────────────────────────────────────────────────┘
                     │
                     │
┌────────────────────┼────────────────────────────────────────────────────┐
│                    ↓      APPLICATION LAYER (MediatR)                   │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  SendEventNotificationCommandHandler.Handle()                           │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │ 1. ✅ Validate event exists                                       │ │
│  │ 2. ✅ Validate user is organizer                                  │ │
│  │ 3. ✅ Validate event status (Active/Published)                    │ │
│  │ 4. ✅ Create EventNotificationHistory record                      │ │
│  │ 5. ✅ Save to database                                            │ │
│  │ 6. ✅ Enqueue Hangfire job:                                       │ │
│  │       _backgroundJobClient.Enqueue<EventNotificationEmailJob>(   │ │
│  │           job => job.ExecuteAsync(history.Id))                    │ │
│  │ 7. ✅ Return success                                              │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                    │                                                     │
│                    │ Job queued successfully                             │
│                    ↓                                                     │
└────────────────────────────────────────────────────────────────────────┘
                     │
                     │
┌────────────────────┼────────────────────────────────────────────────────┐
│                    ↓      BACKGROUND PROCESSING (Hangfire)              │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  EventNotificationEmailJob.ExecuteAsync(historyId)                      │
│                                                                         │
│  STEP 1: Load Data                                                      │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │ ✅ Load EventNotificationHistory from DB                          │ │
│  │ ✅ Load Event from DB                                             │ │
│  │ ✅ Validate both exist                                            │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                    │                                                     │
│                    ↓                                                     │
│  STEP 2: Resolve Recipients                                             │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │ ✅ Call EventNotificationRecipientService                         │ │
│  │                                                                    │ │
│  │    ┌────────────────────────────────────────┐                    │ │
│  │    │ A. Email Groups (3 recipients)        │                    │ │
│  │    │    - Fetch groups by IDs               │                    │ │
│  │    │    - Extract all emails from groups    │                    │ │
│  │    └────────────────────────────────────────┘                    │ │
│  │    ┌────────────────────────────────────────┐                    │ │
│  │    │ B. Newsletter Subscribers (2 recipients)│                   │ │
│  │    │    - Metro area subscribers             │                    │ │
│  │    │    - State-level subscribers            │                    │ │
│  │    │    - All-locations subscribers          │                    │ │
│  │    │    (3-level geo-spatial matching)       │                    │ │
│  │    └────────────────────────────────────────┘                    │ │
│  │    ┌────────────────────────────────────────┐                    │ │
│  │    │ C. Confirmed Registrations (0)         │                    │ │
│  │    │    - Filter by confirmed status         │                    │ │
│  │    │    - Filter by non-null UserId          │                    │ │
│  │    │    - Fetch user emails in bulk          │                    │ │
│  │    └────────────────────────────────────────┘                    │ │
│  │                                                                    │ │
│  │ ✅ Result: 5 unique recipients                                    │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                    │                                                     │
│                    ↓                                                     │
│  STEP 3: Build Template Data                                            │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │ ✅ BuildTemplateData(@event)                                      │ │
│  │    - EventTitle: "Sri Lankan New Year"                            │ │
│  │    - EventDate: "2024-04-14 10:00 AM"                             │ │
│  │    - EventLocation: "Cleveland, OH"                               │ │
│  │    - EventDetailsUrl: "https://..."                               │ │
│  │    - IsFreeEvent: true                                            │ │
│  │    - PricingDetails: "Free"                                       │ │
│  │    - HasOrganizerContact: true                                    │ │
│  │    - OrganizerName: "John Doe"                                    │ │
│  │    - OrganizerEmail: "john@example.com"                           │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                    │                                                     │
│                    ↓                                                     │
│  STEP 4: Update History with Recipient Count                            │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │ ✅ history.UpdateSendStatistics(5, 0, 0)                          │ │
│  │ ✅ Save to database                                               │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                    │                                                     │
│                    ↓                                                     │
│  STEP 5: Send Emails (LOOP: 5 recipients)                               │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │ foreach (var email in recipients)                                 │ │
│  │ {                                                                  │ │
│  │     Log: "Sending email 1/5 to: user1@example.com"                │ │
│  │                                                                    │ │
│  │     ❌ FAILURE POINT:                                             │ │
│  │     result = await _emailService.SendTemplatedEmailAsync(         │ │
│  │         "event-details",  ← Template name hardcoded              │ │
│  │         email,                                                     │ │
│  │         templateData,                                              │ │
│  │         cancellationToken)                                         │ │
│  │                                                                    │ │
│  │     ↓ Goes to EmailService...                                     │ │
│  │ }                                                                  │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                    │                                                     │
│                    ↓                                                     │
└────────────────────────────────────────────────────────────────────────┘
                     │
                     │
┌────────────────────┼────────────────────────────────────────────────────┐
│                    ↓      INFRASTRUCTURE LAYER (Email Service)          │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  EmailService.SendTemplatedEmailAsync()                                 │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │ Step 1: Load Template from Database                               │ │
│  │ ┌────────────────────────────────────────────────────────────┐   │ │
│  │ │ var template = await _emailTemplateRepository                │   │ │
│  │ │     .GetByNameAsync("event-details", cancellationToken);     │   │ │
│  │ │                                                               │   │ │
│  │ │ ↓ Query: SELECT * FROM communications.email_templates       │   │ │
│  │ │          WHERE name = 'event-details'                        │   │ │
│  │ │                                                               │   │ │
│  │ │ ❌ CRITICAL FAILURE: Returns NULL                            │   │ │
│  │ │                                                               │   │ │
│  │ │ if (template == null)                                         │   │ │
│  │ │ {                                                             │   │ │
│  │ │     Log: "❌ Email template 'event-details' not found"      │   │ │
│  │ │     return Result.Failure(                                    │   │ │
│  │ │         "Email template 'event-details' not found");         │   │ │
│  │ │ }                                                             │   │ │
│  │ └────────────────────────────────────────────────────────────┘   │ │
│  │                                                                    │ │
│  │ Step 2-4: NEVER REACHED (template is null)                        │ │
│  │ ┌────────────────────────────────────────────────────────────┐   │ │
│  │ │ ⚠️ NOT EXECUTED:                                             │   │ │
│  │ │ - Render template with parameters                            │   │ │
│  │ │ - Create email message DTO                                   │   │ │
│  │ │ - Send via Azure Communication Services                      │   │ │
│  │ └────────────────────────────────────────────────────────────┘   │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                    │                                                     │
│                    │ Returns: Result.Failure("Template not found")      │
│                    ↓                                                     │
└────────────────────────────────────────────────────────────────────────┘
                     │
                     │
┌────────────────────┼────────────────────────────────────────────────────┐
│                    ↓      BACK TO BACKGROUND JOB                        │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  EventNotificationEmailJob (continued)                                  │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │ if (result.IsSuccess)  // FALSE                                   │ │
│  │ {                                                                  │ │
│  │     successCount++;  // NOT EXECUTED                              │ │
│  │ }                                                                  │ │
│  │ else                                                               │ │
│  │ {                                                                  │ │
│  │     failedCount++;  // ✅ EXECUTED: failedCount = 1               │ │
│  │     Log: "❌ Email 1/5 FAILED - Error: Email template            │ │
│  │           'event-details' not found"                              │ │
│  │ }                                                                  │ │
│  │                                                                    │ │
│  │ // This repeats for ALL 5 recipients                              │ │
│  │ // Result: successCount = 0, failedCount = 5                      │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                    │                                                     │
│                    ↓                                                     │
│  STEP 6: Update Final Statistics                                        │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │ ✅ history.UpdateSendStatistics(5, 0, 5)                          │ │
│  │    - RecipientCount: 5                                            │ │
│  │    - SuccessfulSends: 0                                           │ │
│  │    - FailedSends: 5                                               │ │
│  │ ✅ Save to database                                               │ │
│  │ ✅ Log: "Completed. Success: 0, Failed: 5"                        │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                    │                                                     │
│                    │ Job completes (but all emails failed)              │
│                    ↓                                                     │
└────────────────────────────────────────────────────────────────────────┘
                     │
                     │
┌────────────────────┼────────────────────────────────────────────────────┐
│                    ↓      UI SHOWS RESULT                                │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Event Communications Page (after job completes)                        │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │ ❌ Notification History:                                          │ │
│  │                                                                    │ │
│  │    2024-04-14 10:00 AM                                            │ │
│  │    5 recipients, 0 sent, 5 failed                                 │ │
│  │                                                                    │ │
│  │    ❌ All emails failed to send                                   │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└────────────────────────────────────────────────────────────────────────┘
```

---

## ROOT CAUSE VISUALIZATION

```
┌───────────────────────────────────────────────────────────────────────┐
│                        THE MISSING PIECE                               │
├───────────────────────────────────────────────────────────────────────┤
│                                                                        │
│  Migration File (IN CODE)                  Database (IN PRODUCTION)   │
│  ┌──────────────────────────┐              ┌─────────────────────┐   │
│  │ ✅ EXISTS                │              │ ❌ MISSING          │   │
│  │                           │              │                      │   │
│  │ File: 20260113020400_    │  ╳╳╳╳╳╳╳╳╳  │ Table: email_       │   │
│  │ Phase6A61_AddEvent       │   NOT        │ templates            │   │
│  │ DetailsTemplate.cs       │  APPLIED     │                      │   │
│  │                           │              │ Query:               │   │
│  │ Contains SQL INSERT:     │              │ SELECT * FROM ...    │   │
│  │                           │              │ WHERE name =         │   │
│  │ INSERT INTO              │              │   'event-details'    │   │
│  │ email_templates ...      │              │                      │   │
│  │ VALUES (                 │              │ Result: 0 rows ❌   │   │
│  │   'event-details',       │              │                      │   │
│  │   'Transactional',       │              │                      │   │
│  │   '<html>...</html>',    │              │                      │   │
│  │   true                   │              │                      │   │
│  │ )                        │              │                      │   │
│  └──────────────────────────┘              └─────────────────────┘   │
│           ↓                                          ↑                 │
│           │                                          │                 │
│           │  Why didn't this migration run?          │                 │
│           │                                          │                 │
│           └──────────────────────────────────────────┘                 │
│                                                                        │
│  REASON: Dual Migration Strategy Conflict                              │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ 1. GitHub Actions tries to run migrations                        │ │
│  │    Status: ✅ Succeeds in CI/CD logs                            │ │
│  │                                                                   │ │
│  │ 2. Container startup tries to run migrations                     │ │
│  │    Status: ❌ Fails silently (exception suppressed)             │ │
│  │                                                                   │ │
│  │ 3. Result: Migration appears to run but doesn't actually insert  │ │
│  │    the template into the database                                │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                        │
└───────────────────────────────────────────────────────────────────────┘
```

---

## THE FIX VISUALIZATION

```
┌───────────────────────────────────────────────────────────────────────┐
│                         EMERGENCY FIX                                  │
├───────────────────────────────────────────────────────────────────────┤
│                                                                        │
│  SQL Script                                Database                   │
│  ┌────────────────────────┐               ┌──────────────────────┐   │
│  │ EMERGENCY_FIX_event_   │               │ Table: email_        │   │
│  │ details_template.sql   │   ────────►   │ templates            │   │
│  │                         │   INSERT      │                      │   │
│  │ INSERT INTO            │               │ ✅ Template exists   │   │
│  │ email_templates        │               │                      │   │
│  │ VALUES (               │               │ id: <uuid>           │   │
│  │   'event-details',     │               │ name: event-details  │   │
│  │   'Transactional',     │               │ is_active: true      │   │
│  │   '<html>...</html>',  │               │ category: Events     │   │
│  │   true                 │               │ html_template: ...   │   │
│  │ )                      │               │ text_template: ...   │   │
│  │ WHERE NOT EXISTS ...   │               │ subject_template: .. │   │
│  └────────────────────────┘               └──────────────────────┘   │
│           │                                         │                 │
│           │                                         ↓                 │
│           │                                                            │
│           │                                Email Service               │
│           │                                ┌──────────────────────┐   │
│           │                                │ ✅ Template found    │   │
│           │                                │ ✅ Emails send OK    │   │
│           └────────────────────────────────│ ✅ Success: 5        │   │
│                                             │ ✅ Failed: 0         │   │
│                                             └──────────────────────┘   │
│                                                                        │
│  Time to Fix: 5 minutes                                                │
│  Downtime: ZERO                                                        │
│  Risk: VERY LOW (idempotent INSERT with WHERE NOT EXISTS)             │
│                                                                        │
└───────────────────────────────────────────────────────────────────────┘
```

---

## COMPONENT INTERACTION SUMMARY

### All Components Working EXCEPT Template Loading

| Component | Status | Evidence |
|-----------|--------|----------|
| Frontend UI | ✅ Working | Button click sends API request |
| API Controller | ✅ Working | Receives request, creates command |
| Command Handler | ✅ Working | Validates, creates history, enqueues job |
| Hangfire | ✅ Working | Job enqueued and executed |
| Background Job | ✅ Working | Loads data, resolves recipients |
| Recipient Service | ✅ Working | Fetches 5 recipients correctly |
| Template Data Builder | ✅ Working | Creates correct parameters |
| **Email Template Repo** | **❌ FAILING** | **Returns null - template doesn't exist** |
| Email Rendering | ⚠️ Not Reached | Skipped due to null template |
| Azure Email Service | ⚠️ Not Reached | Skipped due to null template |
| Statistics Update | ✅ Working | Records 0 success, 5 failures |

### Single Point of Failure

```
      ALL COMPONENTS WORKING
              ↓
              ↓
              ↓
      ┌──────────────────┐
      │ Email Template   │
      │ Repository       │ ← ❌ SINGLE FAILURE POINT
      │ .GetByNameAsync  │
      └──────────────────┘
              ↓
          Returns NULL
              ↓
      ALL DOWNSTREAM FAILS
```

---

## AFTER FIX - SUCCESSFUL FLOW

```
Frontend → API → Command Handler → Hangfire → Background Job
    ↓
Resolve Recipients (5 found)
    ↓
Build Template Data
    ↓
Load Template ← ✅ FOUND: event-details
    ↓
Render Template with Data
    ↓
Send Email #1 → ✅ SUCCESS
Send Email #2 → ✅ SUCCESS
Send Email #3 → ✅ SUCCESS
Send Email #4 → ✅ SUCCESS
Send Email #5 → ✅ SUCCESS
    ↓
Update Statistics: 5 recipients, 5 sent, 0 failed
    ↓
UI Shows: ✅ "5 recipients, 5 sent, 0 failed"
```

---

## KEY TAKEAWAYS

1. **Only ONE component is broken**: Email template repository returns null
2. **Fix is simple**: Insert missing template into database
3. **Impact is immediate**: Emails work as soon as template exists
4. **No code changes needed**: All code is correct
5. **Root cause is infrastructure**: Migration didn't actually run in database