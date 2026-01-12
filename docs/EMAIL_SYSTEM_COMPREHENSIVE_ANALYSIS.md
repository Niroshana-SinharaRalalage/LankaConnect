# LankaConnect Email System - Comprehensive Analysis
**Date**: 2026-01-12
**Analyst**: Senior Software Engineer (via Claude Code)
**Status**: Production System Analysis

---

## Executive Summary

### Critical Findings
1. **❌ CRITICAL**: Hardcoded URLs in email templates and handlers (production blocker)
2. **❌ CRITICAL**: Event reminder system completely broken (EventReminderJob not executing)
3. **⚠️ HIGH**: Inconsistent email dispatching patterns across 13 different handlers
4. **⚠️ HIGH**: Template placeholder mismatches causing emails to show raw variables
5. **⚠️ MEDIUM**: No centralized email service - logic duplicated across handlers
6. **✅ GOOD**: Background job infrastructure (Hangfire) properly set up
7. **✅ GOOD**: Email template branding unified (orange/rose gradient)

### Quick Wins (Immediate Action Required)
1. **Fix hardcoded staging URL** in appsettings.Staging.json ✅ COMPLETED (2026-01-12)
2. **Fix EventReminderJob** - Not executing at all (investigate Hangfire registration)
3. **Create centralized URL helper** - Move all URL building to configuration
4. **Audit template placeholders** - Find and fix all MetroArea vs MetroAreasText mismatches

### System Health Score: **6/10** (Fair - Core functionality works, but fragile)

---

## 1. CURRENT STATE MATRIX

### Email Flow Status (11 Required Scenarios)

| # | Email Type | Status | Implementation | Template | Issues | Priority |
|---|------------|--------|----------------|----------|--------|----------|
| 1 | Member Registration Confirmation | ✅ Working | MemberVerificationRequestedEventHandler.cs:27 | member-email-verification | Hardcoded URLs | P1 |
| 2 | Newsletter Subscription Confirmation | ✅ Working | SubscribeToNewsletterCommandHandler.cs:145 | newsletter-confirmation | Config-based URLs ✅ | P2 |
| 3 | Event Organizer Approval | ✅ Working | EventApprovedEventHandler.cs:31 | event-approved | Hardcoded URLs | P1 |
| 4 | Event Announcements (Manual) | ❌ Not Implemented | N/A | N/A | Phase 6A.61 pending | P2 |
| 5 | Event Registration Confirmation | ✅ Working | RegistrationConfirmedEventHandler.cs:44 | registration-confirmed | Hardcoded URLs | P1 |
| 6 | Event Registration Cancellation | ✅ Working | RegistrationCancelledEventHandler.cs:70 | registration-cancellation | Template name fixed ✅ | P2 |
| 7 | Sign-up Commitment Confirmation | ❌ Not Implemented | N/A | signup-commitment-confirmation | Phase 6A.60 pending | P2 |
| 8 | Sign-up Commitment Update/Cancel | ❌ Not Implemented | N/A | N/A | Phase 6A.60 pending | P3 |
| 9 | Event Cancellation (Organizer) | ⚠️ Partial | EventCancelledEventHandler.cs:54 | N/A (inline HTML) | Missing template, incomplete recipients | P1 |
| 10 | Newsletter Broadcasts (Manual) | ⚠️ Partial | Newsletter system exists | Multiple templates | Phase 6A.61 integration | P2 |
| 11 | Automatic Event Reminders | ❌ BROKEN | EventReminderJob.cs:35 | event-reminder | **NOT EXECUTING** | P0 |

**Legend**:
- ✅ Working: Fully implemented and deployed
- ⚠️ Partial: Implemented but with known issues
- ❌ Not Implemented: Missing or broken
- P0: Critical (system broken)
- P1: High (production risk)
- P2: Medium (planned work)
- P3: Low (nice to have)

---

## 2. ARCHITECTURE ASSESSMENT

### Current Email Dispatching Patterns

#### Pattern 1: Direct Email Service (13 handlers)
**Files**:
- `MemberVerificationRequestedEventHandler.cs`
- `EventApprovedEventHandler.cs`
- `RegistrationConfirmedEventHandler.cs`
- `RegistrationCancelledEventHandler.cs`
- `EventCancelledEventHandler.cs`
- `EventPublishedEventHandler.cs`
- `PaymentCompletedEventHandler.cs`
- `CommitmentCancelledEventHandler.cs`
- `AnonymousRegistrationConfirmedEventHandler.cs`
- `SubscribeToNewsletterCommandHandler.cs`
- `ConfirmNewsletterSubscriptionCommandHandler.cs`
- `SendEmailVerificationCommandHandler.cs`
- `SendWelcomeEmailCommandHandler.cs`

**Pattern**:
```csharp
public async Task Handle(DomainEvent notification, CancellationToken cancellationToken)
{
    // 1. Fetch data (user, event, etc.)
    // 2. Build email parameters dictionary
    // 3. Call _emailService.SendTemplatedEmailAsync()
    // 4. Log result
}
```

**Issues**:
- ❌ Duplicated parameter building logic
- ❌ Inconsistent error handling
- ❌ Hardcoded URLs in 9 out of 13 handlers
- ❌ No URL helper abstraction
- ❌ Template names as magic strings

#### Pattern 2: Background Job (2 jobs)
**Files**:
- `EventCancellationEmailJob.cs` - Background email sending for event cancellations
- `EventReminderJob.cs` - **BROKEN** Automatic reminders

**Pattern**:
```csharp
public class EventCancellationEmailJob
{
    public async Task ExecuteAsync(Guid eventId, List<string> recipientEmails)
    {
        // Batch process emails asynchronously
        // Updates progress counters
        // Handles partial failures
    }
}
```

**Issues with EventReminderJob**:
- ❌ **NOT REGISTERED in Hangfire** - No recurring job schedule
- ❌ Original schedule: Hourly execution
- ❌ Requested change: 1 week, 2 days, 1 day before event
- ❌ Change never completed - job registration removed
- ❌ Zero reminders being sent

#### Pattern 3: Inline HTML Generation (1 handler - DEPRECATED)
**File**: `EventCancelledEventHandler.cs:142-160`

**Pattern**:
```csharp
private string GenerateEventCancelledHtml(Dictionary<string, object> parameters)
{
    // Manually builds HTML string
    // NOT using templates
    // INCONSISTENT with rest of system
}
```

**Status**: ⚠️ SHOULD BE MIGRATED TO TEMPLATE SYSTEM (Phase 6A.63)

### Email Service Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Event Handlers (13)                      │
│  Registration, Cancellation, Approval, Newsletter, etc.     │
└────────────────────┬────────────────────────────────────────┘
                     │ Direct calls
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              IEmailService (EmailService.cs)                │
│  - SendTemplatedEmailAsync(templateName, params)            │
│  - SendEmail(to, subject, body)                             │
└────────────────────┬────────────────────────────────────────┘
                     │
         ┌───────────┴───────────┐
         ▼                       ▼
┌──────────────────┐    ┌──────────────────┐
│ Email Queue      │    │  Azure Email     │
│ (EmailMessage)   │    │  Service (SMTP)  │
│ Background       │    │  Direct send     │
│ Processing       │    │                  │
└──────────────────┘    └──────────────────┘
```

**Key Components**:
1. **EmailService** (`src/LankaConnect.Infrastructure/Email/Services/EmailService.cs`)
   - Fetches templates from database
   - Replaces placeholders with parameters
   - Queues or sends email
   - Handles both Azure Communication Services and SMTP

2. **EmailQueueProcessor** (`src/LankaConnect.Infrastructure/Email/Services/EmailQueueProcessor.cs`)
   - Background job (Hangfire)
   - Processes queued emails
   - Batch processing with retry logic

3. **Email Templates** (Database: `communications.email_templates`)
   - Stored as HTML + text in database
   - Branding: Orange (#f97316) to rose (#e11d48) gradient
   - Categories: Transactional, Marketing, System

---

## 3. HARDCODED URLs ANALYSIS

### Critical Issue: Production Deployment Risk

**Problem**: URLs hardcoded in handlers require code changes for production deployment.

### Hardcoded URL Inventory

| Handler | Line | Hardcoded URL | Should Be |
|---------|------|---------------|-----------|
| MemberVerificationRequestedEventHandler.cs | 42 | `"https://lankaconnect.com/verify-email"` | `_config["ApplicationUrls:FrontendBaseUrl"] + _config["ApplicationUrls:EmailVerificationPath"]` |
| EventApprovedEventHandler.cs | 45 | `"https://lankaconnect.com/events/{eventId}"` | Config-based |
| RegistrationConfirmedEventHandler.cs | 67 | `"https://lankaconnect.com/events/{eventId}"` | Config-based |
| EventPublishedEventHandler.cs | 98 | `"https://lankaconnect.com/events/{eventId}"` | Config-based |
| PaymentCompletedEventHandler.cs | 56 | `"https://lankaconnect.com/events/{eventId}"` | Config-based |
| AnonymousRegistrationConfirmedEventHandler.cs | 78 | `"https://lankaconnect.com/events/{eventId}"` | Config-based |
| CommitmentCancelledEventHandler.cs | 89 | `"https://lankaconnect.com/events/{eventId}"` | Config-based |

**Recently Fixed** ✅:
- ✅ SubscribeToNewsletterCommandHandler.cs - Now uses `_configuration["ApplicationUrls:FrontendBaseUrl"]`
- ✅ NewsletterController.cs - Uses configuration for redirects

### Configuration Files

**Current State** (`appsettings.Staging.json`):
```json
"ApplicationUrls": {
  "ApiBaseUrl": "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io",
  "FrontendBaseUrl": "https://lankaconnect-ui-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io",
  "EmailVerificationPath": "/verify-email",
  "NewsletterConfirmPath": "/api/newsletter/confirm",
  "NewsletterUnsubscribePath": "/api/newsletter/unsubscribe",
  "UnsubscribePath": "/unsubscribe",
  "EventDetailsPath": "/events/{eventId}"
}
```

**Missing Paths** (need to add):
- `EventManagePath`: `/events/{eventId}/manage`
- `EventSignupPath`: `/events/{eventId}/signup`
- `MyEventsPath`: `/profile/my-events`

---

## 4. TEMPLATE CONSISTENCY ANALYSIS

### Template Database Schema
```sql
communications.email_templates (
    id UUID PRIMARY KEY,
    template_name VARCHAR(100) UNIQUE NOT NULL,
    subject TEXT NOT NULL,
    html_content TEXT NOT NULL,
    text_content TEXT,
    category VARCHAR(50),  -- 'Transactional', 'Marketing', 'System'
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP,
    updated_at TIMESTAMP
)
```

### Template Inventory (From Migrations)

| Template Name | Status | Used By | Branding | Issues |
|---------------|--------|---------|----------|--------|
| `member-email-verification` | ✅ Active | MemberVerificationRequestedEventHandler | Orange/Rose ✅ | None |
| `newsletter-confirmation` | ✅ Active | SubscribeToNewsletterCommandHandler | Orange/Rose ✅ | ✅ MetroAreasText fixed |
| `registration-cancellation` | ✅ Active | RegistrationCancelledEventHandler | Orange/Rose ✅ | ✅ Template name fixed |
| `registration-confirmed` | ✅ Active | RegistrationConfirmedEventHandler | Orange/Rose ✅ | None |
| `event-approved` | ✅ Active | EventApprovedEventHandler | Orange/Rose ✅ | None |
| `event-published` | ✅ Active | EventPublishedEventHandler | Orange/Rose ✅ | None |
| `event-reminder` | ✅ Active | EventReminderJob (BROKEN) | Orange/Rose ✅ | Job not executing |
| `payment-completed` | ✅ Active | PaymentCompletedEventHandler | Orange/Rose ✅ | None |
| `signup-commitment-confirmation` | ✅ Active | NOT IMPLEMENTED | Orange/Rose ✅ | Phase 6A.60 pending |
| `event-cancelled-notification` | ❌ MISSING | EventCancelledEventHandler | N/A | Uses inline HTML |

### Template Placeholder Issues

**Known Mismatches**:
1. ✅ **FIXED**: `MetroArea` vs `MetroAreasText` in newsletter-confirmation
2. ⚠️ **POTENTIAL**: Need to audit all templates for placeholder consistency

**Recommendation**: Create automated test to validate template placeholders match handler parameters.

---

## 5. EVENT REMINDER SYSTEM - ROOT CAUSE ANALYSIS

### Critical Failure: EventReminderJob Not Executing

**File**: `src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs`

**Original Implementation**:
```csharp
// Hangfire registration (MISSING NOW)
RecurringJob.AddOrUpdate<EventReminderJob>(
    "event-reminders",
    job => job.SendEventReminders(),
    Cron.Hourly  // Sends reminders 24 hours before event
);
```

**Requested Change** (User requirement):
> "I wanted to change it to send the reminder One Week before the event, two days before the event and one day before the event."

**What Happened**:
1. ❌ Original hourly job was removed/disabled
2. ❌ New multi-frequency job was NEVER implemented
3. ❌ No Hangfire registration exists for any reminder schedule
4. ❌ Zero reminders being sent

**Current Job Code** (`EventReminderJob.cs:35-120`):
```csharp
public async Task SendEventReminders()
{
    var upcomingEvents = await _eventRepository.GetUpcomingEventsAsync(
        DateTime.UtcNow.AddHours(23),  // 23-hour window
        DateTime.UtcNow.AddHours(25),  // 25-hour window
        cancellationToken);

    // Only handles 24-hour reminders
    // Does NOT handle 1-week or 2-day reminders
}
```

**Root Cause**:
- Job logic still hardcoded to 24-hour window
- Never refactored to handle multiple reminder intervals
- Hangfire registration never re-added after removal

### Fix Required (EventReminderJob)

**1. Refactor job to support multiple intervals**:
```csharp
public async Task SendEventReminders(ReminderInterval interval)
{
    var (startWindow, endWindow) = interval switch
    {
        ReminderInterval.OneWeek => (DateTime.UtcNow.AddDays(6.9), DateTime.UtcNow.AddDays(7.1)),
        ReminderInterval.TwoDays => (DateTime.UtcNow.AddDays(1.9), DateTime.UtcNow.AddDays(2.1)),
        ReminderInterval.OneDay => (DateTime.UtcNow.AddDays(0.9), DateTime.UtcNow.AddDays(1.1)),
        _ => throw new ArgumentException($"Unknown interval: {interval}")
    };

    var events = await _eventRepository.GetUpcomingEventsAsync(startWindow, endWindow, cancellationToken);

    foreach (var @event in events)
    {
        // Check if reminder already sent for this interval
        var alreadySent = await _reminderRepository.HasReminderBeenSentAsync(@event.Id, interval);
        if (alreadySent) continue;

        // Send reminder email
        await SendReminderEmail(@event, interval);

        // Record reminder sent
        await _reminderRepository.RecordReminderSentAsync(@event.Id, interval);
    }
}
```

**2. Register three recurring jobs**:
```csharp
// In Startup.cs or RecurringJobsConfiguration.cs
RecurringJob.AddOrUpdate<EventReminderJob>(
    "event-reminders-1-week",
    job => job.SendEventReminders(ReminderInterval.OneWeek),
    Cron.Daily(9)  // 9 AM daily
);

RecurringJob.AddOrUpdate<EventReminderJob>(
    "event-reminders-2-days",
    job => job.SendEventReminders(ReminderInterval.TwoDays),
    Cron.Daily(9)
);

RecurringJob.AddOrUpdate<EventReminderJob>(
    "event-reminders-1-day",
    job => job.SendEventReminders(ReminderInterval.OneDay),
    Cron.Daily(9)
);
```

**3. Add reminder tracking table**:
```sql
CREATE TABLE event_reminders_sent (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    reminder_interval VARCHAR(20) NOT NULL, -- 'OneWeek', 'TwoDays', 'OneDay'
    sent_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE(event_id, reminder_interval)
);
```

---

## 6. TECHNICAL DEBT ASSESSMENT

### Critical Issues (P0 - Immediate Action)

| Issue | Impact | Effort | Risk |
|-------|--------|--------|------|
| **Event reminders not sending** | **CRITICAL** - Users not getting reminders | 4-6 hours | High (new table, job refactor) |
| **Hardcoded URLs in 7 handlers** | **HIGH** - Production deployment requires code changes | 2-3 hours | Low (config refactor) |
| **EventCancelledEventHandler incomplete recipients** | **HIGH** - Email groups & newsletter subscribers not notified | 3-4 hours | Medium (recipient consolidation) |

### High Priority Issues (P1 - This Week)

| Issue | Impact | Effort | Risk |
|-------|--------|--------|------|
| **EventCancelledEventHandler uses inline HTML** | HIGH - Inconsistent with template system | 2 hours | Low (template migration) |
| **No centralized URL helper** | MEDIUM - Code duplication, error-prone | 3 hours | Low (refactor) |
| **Template placeholder audit needed** | MEDIUM - Potential email bugs | 2 hours | Low (testing) |

### Medium Priority Issues (P2 - This Sprint)

| Issue | Impact | Effort | Risk |
|-------|--------|--------|------|
| **No centralized email parameter builder** | MEDIUM - Code duplication | 4-6 hours | Low (refactor) |
| **Inconsistent error handling patterns** | MEDIUM - Observability gaps | 3-4 hours | Low (standardization) |
| **Missing email logging correlation IDs** | LOW - Debugging difficulty | 2-3 hours | Low (enhancement) |

### Low Priority Issues (P3 - Future)

| Issue | Impact | Effort | Risk |
|-------|--------|--------|------|
| **Email template versioning** | LOW - Template changes break old emails | 8-10 hours | Medium (architecture) |
| **Email preview endpoint** | LOW - UX improvement | 4-6 hours | Low (new feature) |
| **Email analytics/tracking** | LOW - Business intelligence | 10-15 hours | Medium (new system) |

---

## 7. RECOMMENDED ARCHITECTURE

### Centralized Email Orchestration Service

**New Service**: `IEmailOrchestrationService`

```csharp
public interface IEmailOrchestrationService
{
    Task<Result> SendMemberVerificationEmailAsync(Guid userId, CancellationToken ct);
    Task<Result> SendEventApprovalEmailAsync(Guid eventId, CancellationToken ct);
    Task<Result> SendRegistrationConfirmationEmailAsync(Guid registrationId, CancellationToken ct);
    Task<Result> SendEventCancellationEmailsAsync(Guid eventId, string reason, CancellationToken ct);
    Task<Result> SendEventReminderEmailsAsync(Guid eventId, ReminderInterval interval, CancellationToken ct);
    // ... etc for all 11 email types
}
```

**Benefits**:
1. ✅ Single place to manage all email logic
2. ✅ Easy to add logging, metrics, rate limiting
3. ✅ Testable in isolation
4. ✅ Clear API surface
5. ✅ Consistent error handling

**Implementation**:
```csharp
public class EmailOrchestrationService : IEmailOrchestrationService
{
    private readonly IEmailService _emailService;
    private readonly IEmailParameterBuilder _parameterBuilder;
    private readonly IUrlHelper _urlHelper;
    private readonly ILogger<EmailOrchestrationService> _logger;

    public async Task<Result> SendRegistrationConfirmationEmailAsync(
        Guid registrationId,
        CancellationToken ct)
    {
        try
        {
            // 1. Fetch data
            var registration = await _registrationRepository.GetByIdAsync(registrationId, ct);
            if (registration == null) return Result.Failure("Registration not found");

            // 2. Build parameters using helper
            var parameters = _parameterBuilder.BuildRegistrationConfirmationParameters(registration);

            // 3. Add URLs using helper
            parameters["EventUrl"] = _urlHelper.GetEventDetailsUrl(registration.EventId);

            // 4. Send email
            var result = await _emailService.SendTemplatedEmailAsync(
                "registration-confirmed",
                registration.Email,
                parameters,
                ct);

            // 5. Log with correlation ID
            _logger.LogInformation(
                "[EmailOrchestration] Registration confirmation sent. RegistrationId={RegistrationId}, Success={Success}",
                registrationId, result.IsSuccess);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send registration confirmation email. RegistrationId={RegistrationId}", registrationId);
            return Result.Failure($"Email send failed: {ex.Message}");
        }
    }
}
```

### URL Helper Service

**New Service**: `IEmailUrlHelper`

```csharp
public interface IEmailUrlHelper
{
    string GetEventDetailsUrl(Guid eventId);
    string GetEventManageUrl(Guid eventId);
    string GetEmailVerificationUrl(string token);
    string GetNewsletterConfirmUrl(string token);
    string GetNewsletterUnsubscribeUrl(string token);
    string GetMyEventsUrl();
}

public class EmailUrlHelper : IEmailUrlHelper
{
    private readonly IConfiguration _configuration;

    public EmailUrlHelper(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetEventDetailsUrl(Guid eventId)
    {
        var baseUrl = _configuration["ApplicationUrls:FrontendBaseUrl"];
        var path = _configuration["ApplicationUrls:EventDetailsPath"]?.Replace("{eventId}", eventId.ToString());
        return $"{baseUrl}{path}";
    }

    // ... etc
}
```

---

## 8. IMPLEMENTATION ROADMAP

### Phase 0: Critical Fixes (1-2 days) - **IMMEDIATE**

**Goal**: Fix broken/blocking issues

1. **Fix EventReminderJob** (4-6 hours)
   - [ ] Create `event_reminders_sent` tracking table
   - [ ] Refactor job to support multiple intervals
   - [ ] Register three recurring jobs in Hangfire
   - [ ] Test with staging events
   - [ ] Deploy to staging
   - [ ] Monitor for 24 hours
   - [ ] Deploy to production

2. **Create URL Helper Service** (2-3 hours)
   - [ ] Create `IEmailUrlHelper` interface
   - [ ] Implement `EmailUrlHelper` class
   - [ ] Add all URL methods
   - [ ] Register in DI container
   - [ ] Update 7 handlers to use URL helper
   - [ ] Test with staging
   - [ ] Deploy to production

3. **Complete EventCancelledEventHandler** (3-4 hours)
   - [ ] Create `event-cancelled-notification` template (Phase 6A.63)
   - [ ] Add recipient consolidation logic
   - [ ] Remove inline HTML generation
   - [ ] Test with all recipient types
   - [ ] Deploy to staging and production

**Total**: 9-13 hours (1-2 days)

### Phase 1: Centralization (1 week)

**Goal**: Reduce code duplication, improve maintainability

1. **Create Email Orchestration Service** (2 days)
   - [ ] Define `IEmailOrchestrationService` interface
   - [ ] Implement service for all 11 email types
   - [ ] Add comprehensive logging
   - [ ] Unit test all methods
   - [ ] Integration test with database

2. **Refactor Existing Handlers** (2 days)
   - [ ] Update 13 event handlers to use orchestration service
   - [ ] Remove duplicated logic
   - [ ] Ensure backward compatibility
   - [ ] Run full regression test suite

3. **Create Email Parameter Builder** (1 day)
   - [ ] Define `IEmailParameterBuilder` interface
   - [ ] Implement builders for each email type
   - [ ] Extract common parameter logic
   - [ ] Unit test builders

**Total**: 5 days

### Phase 2: New Features (2 weeks)

**Goal**: Complete missing email flows

1. **Phase 6A.60: Signup Commitment Emails** (3-4 hours)
   - Per EMAIL_SYSTEM_REMAINING_WORK_PLAN.md

2. **Phase 6A.61: Manual Event Email Sending** (15-17 hours)
   - Per EMAIL_SYSTEM_REMAINING_WORK_PLAN.md

3. **Phase 6A.63: Event Cancellation Template** (2-3 hours)
   - Per EMAIL_SYSTEM_REMAINING_WORK_PLAN.md

**Total**: 20-24 hours

### Phase 3: Quality & Observability (1 week)

**Goal**: Production hardening

1. **Email Template Testing** (2 days)
   - [ ] Automated placeholder validation
   - [ ] Cross-browser email rendering tests
   - [ ] Mobile email client tests
   - [ ] Spam filter testing

2. **Logging & Monitoring** (2 days)
   - [ ] Add correlation IDs to all email logs
   - [ ] Create email delivery dashboard
   - [ ] Set up alerts for failure spikes
   - [ ] Add performance metrics

3. **Documentation** (1 day)
   - [ ] Document email architecture
   - [ ] Create runbook for email issues
   - [ ] Document all email templates
   - [ ] Create email testing guide

**Total**: 5 days

---

## 9. QUICK WINS (Do These First)

### 1. Fix Staging URL (✅ COMPLETED - 2026-01-12)
**Time**: 15 minutes
**Impact**: Medium
**Risk**: Low

**Action**: Update `appsettings.Staging.json` to use staging frontend URL instead of production.

**Status**: ✅ Completed in commit ec0e1a59

### 2. Audit Template Placeholders (2 hours)
**Time**: 2 hours
**Impact**: High
**Risk**: Low

**Action**: Query all templates and compare placeholders with handler parameters.

**Query**:
```sql
SELECT template_name, html_content
FROM communications.email_templates
WHERE is_active = true;
```

**Test**: Send test email for each template, verify no raw placeholders visible.

### 3. Add Missing Configuration Paths (30 minutes)
**Time**: 30 minutes
**Impact**: Medium
**Risk**: Low

**Action**: Add missing URL paths to all appsettings files.

**Add to `appsettings.json`**:
```json
"ApplicationUrls": {
  "EventManagePath": "/events/{eventId}/manage",
  "EventSignupPath": "/events/{eventId}/signup",
  "MyEventsPath": "/profile/my-events",
  "ProfilePath": "/profile"
}
```

### 4. Fix EventReminderJob Registration (1 hour)
**Time**: 1 hour
**Impact**: **CRITICAL**
**Risk**: Low

**Action**: Re-register EventReminderJob in Hangfire (quick fix for original 24-hour reminder).

**File**: `src/LankaConnect.API/RecurringJobsConfiguration.cs` or `Startup.cs`

**Add**:
```csharp
RecurringJob.AddOrUpdate<EventReminderJob>(
    "event-reminders-24h",
    job => job.SendEventReminders(),
    Cron.Hourly,  // Check every hour
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
);
```

**Test**: Check Hangfire dashboard, verify job runs hourly.

### 5. Create Centralized Template Name Constants (30 minutes)
**Time**: 30 minutes
**Impact**: Medium
**Risk**: Low

**Action**: Replace magic strings with constants.

**File**: `src/LankaConnect.Application/Common/Constants/EmailTemplateNames.cs`

**Add**:
```csharp
public static class EmailTemplateNames
{
    public const string MemberEmailVerification = "member-email-verification";
    public const string NewsletterConfirmation = "newsletter-confirmation";
    public const string RegistrationCancellation = "registration-cancellation";
    public const string RegistrationConfirmed = "registration-confirmed";
    public const string EventApproved = "event-approved";
    public const string EventPublished = "event-published";
    public const string EventReminder = "event-reminder";
    public const string PaymentCompleted = "payment-completed";
    public const string SignupCommitmentConfirmation = "signup-commitment-confirmation";
    public const string EventCancelledNotification = "event-cancelled-notification";
}
```

**Refactor**: Replace all `"template-name"` strings with constants.

---

## 10. TESTING STRATEGY

### Email System Testing Pyramid

```
         ┌─────────────────┐
         │ Manual Testing  │  5%
         │ (Email Preview) │
         ├─────────────────┤
         │ E2E Tests       │  10%
         │ (Full Flow)     │
         ├─────────────────┤
         │ Integration     │  25%
         │ Tests (DB+Email)│
         ├─────────────────┤
         │ Unit Tests      │  60%
         │ (Handlers+Svcs) │
         └─────────────────┘
```

### 1. Unit Tests (60% of tests)

**What to Test**:
- Email parameter builders
- URL helper methods
- Email orchestration service methods
- Template placeholder replacement
- Recipient consolidation logic

**Example**:
```csharp
[Fact]
public async Task SendRegistrationConfirmationEmail_WithValidData_SendsEmail()
{
    // Arrange
    var registrationId = Guid.NewGuid();
    var mockRegistration = CreateMockRegistration(registrationId);
    _registrationRepositoryMock
        .Setup(x => x.GetByIdAsync(registrationId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(mockRegistration);

    // Act
    var result = await _emailOrchestrationService.SendRegistrationConfirmationEmailAsync(
        registrationId,
        CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    _emailServiceMock.Verify(
        x => x.SendTemplatedEmailAsync(
            EmailTemplateNames.RegistrationConfirmed,
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()),
        Times.Once);
}
```

### 2. Integration Tests (25% of tests)

**What to Test**:
- End-to-end email sending with real templates
- Database template fetching
- Email queue processing
- Hangfire job execution

**Example**:
```csharp
[Fact]
public async Task EventReminderJob_WithUpcomingEvent_SendsReminderEmail()
{
    // Arrange: Create event in database with start date in 24 hours
    var eventId = await CreateTestEventInDatabase(DateTime.UtcNow.AddHours(24));

    // Act: Run job manually
    await _eventReminderJob.SendEventReminders();

    // Assert: Check email was sent
    var emailMessages = await _emailMessageRepository.GetByEventIdAsync(eventId);
    emailMessages.Should().ContainSingle()
        .Which.TemplateName.Should().Be(EmailTemplateNames.EventReminder);
}
```

### 3. E2E Tests (10% of tests)

**What to Test**:
- Complete user flows with email verification
- Event registration + confirmation email
- Event cancellation + cancellation email
- Newsletter subscription + confirmation email

**Example**:
```csharp
[Fact]
public async Task UserRegistersForEvent_ReceivesConfirmationEmail_CanClickLink()
{
    // Arrange: Create event
    var eventId = await CreateTestEvent();

    // Act: User registers for event
    var response = await _httpClient.PostAsJsonAsync(
        $"/api/events/{eventId}/register",
        new { Email = "test@example.com", Name = "Test User" });

    // Assert: Email was sent
    var emailLog = await GetLatestEmailLog();
    emailLog.TemplateName.Should().Be(EmailTemplateNames.RegistrationConfirmed);
    emailLog.ToEmail.Should().Be("test@example.com");

    // Assert: Email contains valid link
    var confirmUrl = ExtractUrlFromEmail(emailLog.HtmlContent, "View Event");
    confirmUrl.Should().StartWith("https://lankaconnect.com/events/");

    // Act: Click link
    var eventResponse = await _httpClient.GetAsync(confirmUrl);
    eventResponse.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

### 4. Manual Testing (5% of validation)

**What to Test**:
- Email rendering in Gmail, Outlook, Apple Mail
- Mobile email client rendering
- Dark mode appearance
- Spam filter testing
- Link functionality

**Checklist**:
- [ ] Email displays correctly in Gmail web
- [ ] Email displays correctly in Outlook desktop
- [ ] Email displays correctly in Apple Mail (iOS)
- [ ] Email displays correctly in dark mode
- [ ] All links are clickable
- [ ] Unsubscribe link works
- [ ] Email doesn't go to spam folder
- [ ] Images load correctly
- [ ] No broken layouts

---

## 11. MONITORING & ALERTS

### Key Metrics to Track

1. **Email Delivery Rate**
   - Target: > 98%
   - Alert if < 95% in 1-hour window

2. **Email Send Latency**
   - Target: < 30 seconds (queued) or < 5 seconds (immediate)
   - Alert if > 2 minutes

3. **Template Placeholder Errors**
   - Target: 0 errors
   - Alert on any occurrence

4. **Event Reminder Job Success Rate**
   - Target: 100% execution
   - Alert if job fails

5. **Email Queue Backlog**
   - Target: < 100 queued emails
   - Alert if > 500 queued

### Logging Standards

**Format**: Structured logging with correlation IDs

**Example**:
```csharp
_logger.LogInformation(
    "[EmailOrchestration] Sending {EmailType} email. " +
    "CorrelationId={CorrelationId}, RecipientEmail={RecipientEmail}, " +
    "TemplateName={TemplateName}, EventId={EventId}",
    "Registration Confirmation",
    correlationId,
    recipientEmail,
    EmailTemplateNames.RegistrationConfirmed,
    eventId);
```

**Required Fields**:
- `[EmailOrchestration]` prefix for all email logs
- `CorrelationId` for tracing
- `EmailType` for categorization
- `TemplateName` for debugging
- `RecipientEmail` (or hashed for privacy)
- `EventId`/`UserId` for context

---

## 12. CONCLUSION

### Summary

The LankaConnect email system is **functional but fragile**. Core email flows work, but the system suffers from:
1. **Hardcoded URLs** requiring code changes for production
2. **Broken event reminder system** (critical user-facing feature)
3. **Inconsistent implementation patterns** across 13 handlers
4. **Lack of centralization** causing code duplication

### Immediate Actions (Next 24 Hours)

1. ✅ **DONE**: Fix staging frontend URL in appsettings.Staging.json
2. **TODO**: Re-register EventReminderJob in Hangfire (1 hour)
3. **TODO**: Create and deploy URL helper service (2-3 hours)
4. **TODO**: Audit template placeholders (2 hours)

### Short-Term Goals (This Week)

1. Complete EventCancelledEventHandler recipient consolidation
2. Migrate EventCancelledEventHandler to template system
3. Implement EventReminderJob with multiple intervals
4. Add missing configuration URL paths

### Long-Term Vision (Next Month)

1. Centralize all email logic in Email Orchestration Service
2. Complete Phase 6A.60 (Signup Commitment Emails)
3. Complete Phase 6A.61 (Manual Event Email Sending)
4. Implement comprehensive email testing suite
5. Add email analytics and monitoring dashboard

---

## APPENDIX A: File References

### Event Handlers (Email Sending)
- `src/LankaConnect.Application/Users/EventHandlers/MemberVerificationRequestedEventHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/EventApprovedEventHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/EventCancelledEventHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/EventPublishedEventHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/RegistrationCancelledEventHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/RegistrationConfirmedEventHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/CommitmentCancelledEventHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/AnonymousRegistrationConfirmedEventHandler.cs`

### Background Jobs
- `src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs`
- `src/LankaConnect.Application/Events/BackgroundJobs/EventCancellationEmailJob.cs`
- `src/LankaConnect.Application/Communications/BackgroundJobs/NewsletterEmailJob.cs`

### Email Services
- `src/LankaConnect.Infrastructure/Email/Services/EmailService.cs`
- `src/LankaConnect.Infrastructure/Email/Services/AzureEmailService.cs`
- `src/LankaConnect.Infrastructure/Email/Services/EmailQueueProcessor.cs`
- `src/LankaConnect.Infrastructure/Email/Services/RazorEmailTemplateService.cs`

### Configuration
- `src/LankaConnect.API/appsettings.json`
- `src/LankaConnect.API/appsettings.Staging.json`
- `src/LankaConnect.API/appsettings.Production.json`

---

**END OF ANALYSIS**
