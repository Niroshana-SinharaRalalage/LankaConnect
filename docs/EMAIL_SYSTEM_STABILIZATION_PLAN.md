# LankaConnect Email System Stabilization Plan
**Date**: 2026-01-12
**Status**: Production-Ready Implementation Plan
**Priority**: P0 - CRITICAL BUSINESS SYSTEM

---

## Executive Summary

### Critical Status
The LankaConnect email system is **functional but fragile**. Core flows work, but the system requires systematic stabilization to become production-grade. Email is the **primary communication medium** - failures directly impact user experience and business operations.

### Immediate Risks
1. **P0-BLOCKING**: 7 handlers have hardcoded production URLs - staging deploys break emails
2. **P0-INCOMPLETE**: Event cancellation emails miss 90% of recipients (only sends to confirmed registrations)
3. **P0-BROKEN**: Event reminder job exists but is **not registered in Hangfire** - zero reminders sent
4. **P1**: Inconsistent patterns across 13 handlers - maintenance nightmare

### Success Criteria
- ✅ **Zero hardcoded URLs** - All URLs from configuration
- ✅ **Complete recipient coverage** - Event cancellations reach all notification recipients
- ✅ **Automated reminders** - 3-tier reminder system (7 days, 2 days, 1 day) fully operational
- ✅ **90%+ test coverage** - TDD approach with zero compilation errors
- ✅ **Observable system** - Comprehensive logging with correlation IDs
- ✅ **Deployment confidence** - Each phase independently verifiable

---

## Phase Breakdown (8 Phases, 6-8 Days)

### Phase 1: Foundation - URL Centralization (P0)
**Duration**: 4-6 hours
**Risk**: Low
**Impact**: Eliminates production deployment blocker

#### 1.1 Create URL Helper Service (2 hours)
**Files to Create**:
- `src/LankaConnect.Application/Common/Interfaces/IEmailUrlHelper.cs`
- `src/LankaConnect.Application/Common/Services/EmailUrlHelper.cs`
- `tests/LankaConnect.Application.Tests/Common/Services/EmailUrlHelperTests.cs`

**Interface Design**:
```csharp
namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Centralized URL generation for email templates.
/// All URLs read from IConfiguration to support environment-specific deployments.
/// </summary>
public interface IEmailUrlHelper
{
    /// <summary>
    /// Gets the event details page URL (e.g., https://lankaconnect.com/events/{eventId})
    /// </summary>
    string GetEventDetailsUrl(Guid eventId);

    /// <summary>
    /// Gets the event management URL for organizers (e.g., https://lankaconnect.com/events/{eventId}/manage)
    /// </summary>
    string GetEventManageUrl(Guid eventId);

    /// <summary>
    /// Gets the email verification URL with token (e.g., https://lankaconnect.com/verify-email?token={token})
    /// </summary>
    string GetEmailVerificationUrl(string token);

    /// <summary>
    /// Gets the user profile/events page (e.g., https://lankaconnect.com/profile/my-events)
    /// </summary>
    string GetMyEventsUrl();

    /// <summary>
    /// Gets the newsletter confirmation URL with token
    /// </summary>
    string GetNewsletterConfirmUrl(string token);

    /// <summary>
    /// Gets the newsletter unsubscribe URL with token
    /// </summary>
    string GetNewsletterUnsubscribeUrl(string token);
}
```

**Implementation**:
```csharp
namespace LankaConnect.Application.Common.Services;

public class EmailUrlHelper : IEmailUrlHelper
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailUrlHelper> _logger;

    public EmailUrlHelper(IConfiguration configuration, ILogger<EmailUrlHelper> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string GetEventDetailsUrl(Guid eventId)
    {
        var baseUrl = GetConfigValue("ApplicationUrls:FrontendBaseUrl");
        var path = GetConfigValue("ApplicationUrls:EventDetailsPath", "/events/{eventId}");
        var url = $"{baseUrl}{path.Replace("{eventId}", eventId.ToString())}";

        _logger.LogDebug("[EmailUrlHelper] Generated EventDetailsUrl: {Url}", url);
        return url;
    }

    public string GetEventManageUrl(Guid eventId)
    {
        var baseUrl = GetConfigValue("ApplicationUrls:FrontendBaseUrl");
        var path = GetConfigValue("ApplicationUrls:EventManagePath", "/events/{eventId}/manage");
        return $"{baseUrl}{path.Replace("{eventId}", eventId.ToString())}";
    }

    public string GetEmailVerificationUrl(string token)
    {
        var baseUrl = GetConfigValue("ApplicationUrls:FrontendBaseUrl");
        var path = GetConfigValue("ApplicationUrls:EmailVerificationPath", "/verify-email");
        return $"{baseUrl}{path}?token={Uri.EscapeDataString(token)}";
    }

    public string GetMyEventsUrl()
    {
        var baseUrl = GetConfigValue("ApplicationUrls:FrontendBaseUrl");
        var path = GetConfigValue("ApplicationUrls:MyEventsPath", "/profile/my-events");
        return $"{baseUrl}{path}";
    }

    public string GetNewsletterConfirmUrl(string token)
    {
        var baseUrl = GetConfigValue("ApplicationUrls:FrontendBaseUrl");
        var path = GetConfigValue("ApplicationUrls:NewsletterConfirmPath", "/api/newsletter/confirm");
        return $"{baseUrl}{path}?token={Uri.EscapeDataString(token)}";
    }

    public string GetNewsletterUnsubscribeUrl(string token)
    {
        var baseUrl = GetConfigValue("ApplicationUrls:FrontendBaseUrl");
        var path = GetConfigValue("ApplicationUrls:NewsletterUnsubscribePath", "/api/newsletter/unsubscribe");
        return $"{baseUrl}{path}?token={Uri.EscapeDataString(token)}";
    }

    private string GetConfigValue(string key, string? defaultValue = null)
    {
        var value = _configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            if (defaultValue != null)
            {
                _logger.LogWarning(
                    "[EmailUrlHelper] Configuration key {Key} not found, using default: {Default}",
                    key, defaultValue);
                return defaultValue;
            }

            throw new InvalidOperationException(
                $"Configuration key '{key}' is required but not found in appsettings");
        }
        return value;
    }
}
```

**Unit Tests** (15 tests):
```csharp
public class EmailUrlHelperTests
{
    [Fact]
    public void GetEventDetailsUrl_WithValidEventId_ReturnsCorrectUrl()
    {
        // Arrange
        var config = CreateMockConfiguration(
            ("ApplicationUrls:FrontendBaseUrl", "https://staging.lankaconnect.com"),
            ("ApplicationUrls:EventDetailsPath", "/events/{eventId}"));
        var helper = new EmailUrlHelper(config, Mock.Of<ILogger<EmailUrlHelper>>());
        var eventId = Guid.NewGuid();

        // Act
        var url = helper.GetEventDetailsUrl(eventId);

        // Assert
        url.Should().Be($"https://staging.lankaconnect.com/events/{eventId}");
    }

    [Fact]
    public void GetEventDetailsUrl_MissingConfig_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateMockConfiguration(); // Empty config
        var helper = new EmailUrlHelper(config, Mock.Of<ILogger<EmailUrlHelper>>());

        // Act & Assert
        var act = () => helper.GetEventDetailsUrl(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ApplicationUrls:FrontendBaseUrl*");
    }

    [Fact]
    public void GetEmailVerificationUrl_WithToken_EscapesSpecialCharacters()
    {
        // Arrange
        var config = CreateMockConfiguration(
            ("ApplicationUrls:FrontendBaseUrl", "https://lankaconnect.com"),
            ("ApplicationUrls:EmailVerificationPath", "/verify-email"));
        var helper = new EmailUrlHelper(config, Mock.Of<ILogger<EmailUrlHelper>>());
        var token = "abc+def/ghi=123"; // Token with special chars

        // Act
        var url = helper.GetEmailVerificationUrl(token);

        // Assert
        url.Should().Contain("token=abc%2Bdef%2Fghi%3D123");
    }

    // ... 12 more tests covering all methods, edge cases, defaults
}
```

**DI Registration**:
```csharp
// In src/LankaConnect.Application/DependencyInjection.cs
services.AddScoped<IEmailUrlHelper, EmailUrlHelper>();
```

#### 1.2 Update Configuration Files (30 minutes)
**Files to Modify**:
- `src/LankaConnect.API/appsettings.json`
- `src/LankaConnect.API/appsettings.Staging.json`
- `src/LankaConnect.API/appsettings.Production.json`

**Add Missing Paths**:
```json
"ApplicationUrls": {
  "FrontendBaseUrl": "https://lankaconnect.com",
  "EmailVerificationPath": "/verify-email",
  "UnsubscribePath": "/unsubscribe",
  "EventDetailsPath": "/events/{eventId}",
  "EventManagePath": "/events/{eventId}/manage",  // NEW
  "MyEventsPath": "/profile/my-events",           // NEW
  "NewsletterConfirmPath": "/api/newsletter/confirm",
  "NewsletterUnsubscribePath": "/api/newsletter/unsubscribe"
}
```

#### 1.3 Refactor 7 Event Handlers (2-3 hours)
**Files to Modify**:
1. `src/LankaConnect.Application/Users/EventHandlers/MemberVerificationRequestedEventHandler.cs`
2. `src/LankaConnect.Application/Events/EventHandlers/EventApprovedEventHandler.cs`
3. `src/LankaConnect.Application/Events/EventHandlers/RegistrationConfirmedEventHandler.cs`
4. `src/LankaConnect.Application/Events/EventHandlers/EventPublishedEventHandler.cs`
5. `src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs`
6. `src/LankaConnect.Application/Events/EventHandlers/AnonymousRegistrationConfirmedEventHandler.cs`
7. `src/LankaConnect.Application/Events/EventHandlers/CommitmentCancelledEventHandler.cs`

**Refactor Pattern** (Example: EventApprovedEventHandler):
```csharp
// BEFORE (Line 45):
{ "EventUrl", $"https://lankaconnect.com/events/{domainEvent.EventId}" }

// AFTER:
// 1. Add IEmailUrlHelper to constructor
private readonly IEmailUrlHelper _urlHelper;

public EventApprovedEventHandler(
    IEventRepository eventRepository,
    IUserRepository userRepository,
    IEmailService emailService,
    IEmailUrlHelper urlHelper,  // NEW
    ILogger<EventApprovedEventHandler> logger)
{
    // ... assign _urlHelper
}

// 2. Replace hardcoded URL
{ "EventUrl", _urlHelper.GetEventDetailsUrl(domainEvent.EventId) }
{ "ManageEventUrl", _urlHelper.GetEventManageUrl(domainEvent.EventId) }
```

**Testing**:
- ✅ Build succeeds (0 errors, 0 warnings)
- ✅ All existing unit tests pass
- ✅ Add integration test verifying staging URLs used in staging environment

#### 1.4 Deployment Checklist
- [ ] Code review approved
- [ ] All tests pass (unit + integration)
- [ ] Build succeeds
- [ ] Deploy to staging
- [ ] Verify staging emails use staging URLs (manual test)
- [ ] Check logs for URL generation
- [ ] Deploy to production
- [ ] Monitor logs for 24 hours

**Success Metrics**:
- ✅ Zero hardcoded production URLs in codebase
- ✅ Email URLs dynamically generated from configuration
- ✅ Staging deploys send staging URLs in emails

---

### Phase 2: Event Reminder System Fix (P0)
**Duration**: 6-8 hours
**Risk**: Medium (database changes)
**Impact**: Restores critical automated notification system

#### 2.1 Create Reminder Tracking Table (1 hour)
**Migration**: `20260113000001_Phase_EmailStabilization_P2_EventReminderTracking.cs`

**Schema Design**:
```sql
CREATE TABLE event_reminders_sent (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    reminder_type VARCHAR(20) NOT NULL,  -- 'OneWeek', 'TwoDays', 'OneDay'
    sent_at TIMESTAMP NOT NULL DEFAULT NOW(),
    recipient_count INT NOT NULL,
    successful_sends INT NOT NULL DEFAULT 0,
    failed_sends INT NOT NULL DEFAULT 0,

    CONSTRAINT unique_event_reminder UNIQUE(event_id, reminder_type),

    INDEX idx_event_reminders_sent_event_id (event_id),
    INDEX idx_event_reminders_sent_type (reminder_type),
    INDEX idx_event_reminders_sent_date (sent_at)
);

COMMENT ON TABLE event_reminders_sent IS 'Phase 2: Tracks which reminders have been sent for each event to prevent duplicates';
COMMENT ON COLUMN event_reminders_sent.reminder_type IS 'OneWeek (7 days before), TwoDays (2 days before), OneDay (1 day before)';
```

**Migration Code**:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "event_reminders_sent",
        columns: table => new
        {
            id = table.Column<Guid>(nullable: false, defaultValueSql: "gen_random_uuid()"),
            event_id = table.Column<Guid>(nullable: false),
            reminder_type = table.Column<string>(maxLength: 20, nullable: false),
            sent_at = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
            recipient_count = table.Column<int>(nullable: false),
            successful_sends = table.Column<int>(nullable: false, defaultValue: 0),
            failed_sends = table.Column<int>(nullable: false, defaultValue: 0)
        },
        constraints: table =>
        {
            table.PrimaryKey("pk_event_reminders_sent", x => x.id);
            table.ForeignKey(
                name: "fk_event_reminders_sent_events_event_id",
                column: x => x.event_id,
                principalTable: "events",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
            table.UniqueConstraint("unique_event_reminder", x => new { x.event_id, x.reminder_type });
        });

    migrationBuilder.CreateIndex(
        name: "idx_event_reminders_sent_event_id",
        table: "event_reminders_sent",
        column: "event_id");

    migrationBuilder.CreateIndex(
        name: "idx_event_reminders_sent_type",
        table: "event_reminders_sent",
        column: "reminder_type");

    migrationBuilder.CreateIndex(
        name: "idx_event_reminders_sent_date",
        table: "event_reminders_sent",
        column: "sent_at");
}
```

#### 2.2 Create Domain Entity & Repository (2 hours)
**Files to Create**:
- `src/LankaConnect.Domain/Events/Entities/EventReminderSent.cs`
- `src/LankaConnect.Domain/Events/Enums/ReminderType.cs`
- `src/LankaConnect.Application/Common/Interfaces/IEventReminderRepository.cs`
- `src/LankaConnect.Infrastructure/Data/Repositories/EventReminderRepository.cs`

**Domain Entity**:
```csharp
namespace LankaConnect.Domain.Events.Entities;

public class EventReminderSent : Entity<Guid>
{
    public Guid EventId { get; private set; }
    public ReminderType ReminderType { get; private set; }
    public DateTime SentAt { get; private set; }
    public int RecipientCount { get; private set; }
    public int SuccessfulSends { get; private set; }
    public int FailedSends { get; private set; }

    private EventReminderSent() { } // EF Core

    public static EventReminderSent Create(
        Guid eventId,
        ReminderType reminderType,
        int recipientCount)
    {
        return new EventReminderSent
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            ReminderType = reminderType,
            SentAt = DateTime.UtcNow,
            RecipientCount = recipientCount,
            SuccessfulSends = 0,
            FailedSends = 0
        };
    }

    public void UpdateSendResults(int successful, int failed)
    {
        SuccessfulSends = successful;
        FailedSends = failed;
    }
}

public enum ReminderType
{
    OneWeek = 1,
    TwoDays = 2,
    OneDay = 3
}
```

**Repository Interface**:
```csharp
public interface IEventReminderRepository
{
    Task<bool> HasReminderBeenSentAsync(Guid eventId, ReminderType reminderType, CancellationToken ct);
    Task<EventReminderSent> RecordReminderSentAsync(Guid eventId, ReminderType reminderType, int recipientCount, CancellationToken ct);
    Task UpdateReminderResultsAsync(Guid reminderId, int successful, int failed, CancellationToken ct);
}
```

#### 2.3 Refactor EventReminderJob (2-3 hours)
**File**: `src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs`

**Key Changes**:
1. Add IEventReminderRepository dependency
2. Add IEmailUrlHelper dependency
3. Check reminder tracking before sending
4. Record reminder sent after processing
5. Replace hardcoded URL (line 160) with URL helper

**Updated Code** (Key sections):
```csharp
private async Task SendRemindersForWindowAsync(
    DateTime now,
    int startHours,
    int endHours,
    ReminderType reminderType,
    string reminderTimeframe,
    string reminderMessage,
    CancellationToken cancellationToken)
{
    var windowStart = now.AddHours(startHours);
    var windowEnd = now.AddHours(endHours);

    _logger.LogInformation(
        "[Phase 2] EventReminderJob: Checking {ReminderType} reminder window ({Start} to {End})",
        reminderType, windowStart, windowEnd);

    var upcomingEvents = await _eventRepository.GetEventsStartingInTimeWindowAsync(
        windowStart,
        windowEnd,
        new[] { EventStatus.Published, EventStatus.Active });

    if (upcomingEvents.Count == 0)
    {
        _logger.LogInformation("[Phase 2] No events found in {ReminderType} reminder window", reminderType);
        return;
    }

    foreach (var @event in upcomingEvents)
    {
        try
        {
            // CHECK: Has this reminder already been sent?
            var alreadySent = await _eventReminderRepository.HasReminderBeenSentAsync(
                @event.Id, reminderType, cancellationToken);

            if (alreadySent)
            {
                _logger.LogInformation(
                    "[Phase 2] Reminder {ReminderType} already sent for event {EventId}, skipping",
                    reminderType, @event.Id);
                continue;
            }

            var registrations = @event.Registrations;
            _logger.LogInformation(
                "[Phase 2] Sending {ReminderType} reminders for event {EventId} ({Title}) to {Count} attendees",
                reminderType, @event.Id, @event.Title.Value, registrations.Count);

            var successCount = 0;
            var failCount = 0;

            // RECORD: Mark reminder as being sent
            var reminderRecord = await _eventReminderRepository.RecordReminderSentAsync(
                @event.Id, reminderType, registrations.Count, cancellationToken);

            foreach (var registration in registrations)
            {
                try
                {
                    // ... existing email logic ...

                    var parameters = new Dictionary<string, object>
                    {
                        { "AttendeeName", toName },
                        { "EventTitle", @event.Title.Value },
                        { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                        { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                        { "Location", @event.Location?.Address.ToString() ?? "Location TBD" },
                        { "Quantity", registration.Quantity },
                        { "HoursUntilEvent", hoursUntilEvent },
                        { "ReminderTimeframe", reminderTimeframe },
                        { "ReminderMessage", reminderMessage },
                        // FIXED: Use URL helper instead of hardcoded URL
                        { "EventDetailsUrl", _urlHelper.GetEventDetailsUrl(@event.Id) },
                        { "HasOrganizerContact", @event.HasOrganizerContact() },
                        { "OrganizerContactName", @event.OrganizerContactName ?? "" },
                        { "OrganizerContactEmail", @event.OrganizerContactEmail ?? "" },
                        { "OrganizerContactPhone", @event.OrganizerContactPhone ?? "" }
                    };

                    var result = await _emailService.SendTemplatedEmailAsync(
                        "event-reminder",
                        toEmail,
                        parameters,
                        cancellationToken);

                    if (result.IsSuccess)
                        successCount++;
                    else
                        failCount++;
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logger.LogError(ex, "[Phase 2] Error sending reminder for event {EventId}", @event.Id);
                }
            }

            // UPDATE: Record final send results
            await _eventReminderRepository.UpdateReminderResultsAsync(
                reminderRecord.Id, successCount, failCount, cancellationToken);

            _logger.LogInformation(
                "[Phase 2] {ReminderType} reminders for event {EventId}: Success={Success}, Failed={Failed}",
                reminderType, @event.Id, successCount, failCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 2] Error processing {ReminderType} reminders for event {EventId}",
                reminderType, @event.Id);
        }
    }
}

// Update ExecuteAsync to use ReminderType enum
public async Task ExecuteAsync()
{
    _logger.LogInformation("[Phase 2] EventReminderJob: Starting execution at {Time}", DateTime.UtcNow);

    try
    {
        var now = DateTime.UtcNow;

        // Phase 2: Send 3 types of reminders with tracking
        await SendRemindersForWindowAsync(
            now, 167, 169, ReminderType.OneWeek,
            "in 1 week", "Your event is coming up next week. Mark your calendar!", CancellationToken.None);

        await SendRemindersForWindowAsync(
            now, 47, 49, ReminderType.TwoDays,
            "in 2 days", "Your event is just 2 days away. Don't forget!", CancellationToken.None);

        await SendRemindersForWindowAsync(
            now, 23, 25, ReminderType.OneDay,
            "tomorrow", "Your event is tomorrow! We look forward to seeing you there.", CancellationToken.None);

        _logger.LogInformation("[Phase 2] EventReminderJob: Completed execution at {Time}", DateTime.UtcNow);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[Phase 2] EventReminderJob: Fatal error during execution");
    }
}
```

#### 2.4 Register Hangfire Job (30 minutes)
**File**: Find or create `src/LankaConnect.API/RecurringJobsConfiguration.cs` or `Startup.cs`

**Registration Code**:
```csharp
using Hangfire;

public static class RecurringJobsConfiguration
{
    public static void ConfigureRecurringJobs()
    {
        // Phase 2: Event reminder job - runs hourly, sends 3-tier reminders
        RecurringJob.AddOrUpdate<EventReminderJob>(
            "event-reminders",
            job => job.ExecuteAsync(),
            Cron.Hourly,
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });
    }
}

// In Program.cs or Startup.cs (after app.UseHangfireDashboard()):
RecurringJobsConfiguration.ConfigureRecurringJobs();
```

**Verification**:
- Navigate to `/hangfire` dashboard
- Check "Recurring Jobs" tab
- Verify "event-reminders" job is registered
- Trigger manually to test

#### 2.5 Testing Strategy
**Unit Tests** (10 tests):
- Repository: HasReminderBeenSentAsync returns false for new events
- Repository: RecordReminderSentAsync creates tracking record
- Job: Skips events with already-sent reminders
- Job: Records reminder after successful send
- Job: Updates success/fail counts correctly

**Integration Tests** (5 tests):
- Create event in 7-day window, verify OneWeek reminder sent
- Create event in 2-day window, verify TwoDays reminder sent
- Send reminder, run job again, verify no duplicate sent
- Verify tracking record in database after job runs

**Manual Tests**:
- Create test events in staging with start dates in 7 days, 2 days, 1 day
- Wait for hourly job execution OR trigger manually
- Verify emails received
- Check `event_reminders_sent` table for tracking records

#### 2.6 Deployment Checklist
- [ ] Migration reviewed and tested locally
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Build succeeds (0 errors, 0 warnings)
- [ ] Code review approved
- [ ] Deploy migration to staging database
- [ ] Verify table created: `SELECT * FROM event_reminders_sent;`
- [ ] Deploy application code to staging
- [ ] Verify Hangfire job registered in dashboard
- [ ] Create test events in staging
- [ ] Trigger job manually or wait for hourly execution
- [ ] Verify emails sent successfully
- [ ] Verify tracking records created
- [ ] Monitor logs for 24 hours
- [ ] Deploy to production
- [ ] Monitor production logs for 48 hours

**Success Metrics**:
- ✅ Hangfire job registered and running hourly
- ✅ 3-tier reminders (7 days, 2 days, 1 day) sending correctly
- ✅ No duplicate reminders sent (tracking prevents)
- ✅ Email URLs use environment-specific configuration

---

### Phase 3: Event Cancellation Recipient Fix (P0)
**Duration**: 4-5 hours
**Risk**: Medium (complex recipient logic)
**Impact**: Ensures all stakeholders receive cancellation notifications

#### 3.1 Problem Analysis
**Current State** (EventCancelledEventHandler.cs lines 54-68):
```csharp
// ❌ BROKEN: Only sends to confirmed registrations
var registrations = await _registrationRepository.GetByEventAsync(domainEvent.EventId, cancellationToken);
var confirmedRegistrations = registrations
    .Where(r => r.Status == RegistrationStatus.Confirmed)
    .ToList();

if (!confirmedRegistrations.Any())
{
    _logger.LogInformation("No confirmed registrations found for Event {EventId}, skipping email notifications",
        domainEvent.EventId);
    return;  // ❌ EXITS EARLY, MISSING EMAIL GROUPS & NEWSLETTER SUBSCRIBERS
}
```

**Missing Recipients**:
1. ❌ Email groups (targeted distribution lists)
2. ❌ Newsletter subscribers (metro area, state, all locations)
3. ✅ Confirmed registrations (currently implemented)

**Root Cause**: User reported cancelling events with zero registrations, expected emails to email groups and newsletter subscribers, but zero emails sent.

#### 3.2 Solution: Recipient Consolidation
**Service to Reuse**: `IEventNotificationRecipientService`
**Reference**: `EventPublishedEventHandler.cs` lines 48-129 (successful implementation)

**Consolidation Pattern**:
```csharp
// 1. Get confirmed registration emails
var registrations = await _registrationRepository.GetByEventAsync(domainEvent.EventId, cancellationToken);
var confirmedRegistrations = registrations
    .Where(r => r.Status == RegistrationStatus.Confirmed)
    .ToList();

var registrationEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var registration in confirmedRegistrations)
{
    var user = await _userRepository.GetByIdAsync(registration.UserId, cancellationToken);
    if (user != null)
    {
        registrationEmails.Add(user.Email.Value);
    }
}

_logger.LogInformation("[Phase 3] Found {Count} confirmed registrations for Event {EventId}",
    registrationEmails.Count, domainEvent.EventId);

// 2. Get email groups + newsletter subscribers
var notificationRecipients = await _recipientService.ResolveRecipientsAsync(
    domainEvent.EventId,
    cancellationToken);

_logger.LogInformation(
    "[Phase 3] Resolved {Count} notification recipients for Event {EventId}. " +
    "Breakdown: EmailGroups={EmailGroupCount}, Metro={MetroCount}, State={StateCount}, AllLocations={AllLocationsCount}",
    notificationRecipients.EmailAddresses.Count, domainEvent.EventId,
    notificationRecipients.Breakdown.EmailGroupCount,
    notificationRecipients.Breakdown.MetroAreaSubscribers,
    notificationRecipients.Breakdown.StateLevelSubscribers,
    notificationRecipients.Breakdown.AllLocationsSubscribers);

// 3. Consolidate all recipients (deduplicated, case-insensitive)
var allRecipients = registrationEmails
    .Concat(notificationRecipients.EmailAddresses)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);

if (!allRecipients.Any())
{
    _logger.LogInformation("[Phase 3] No recipients found for Event {EventId}, skipping cancellation emails",
        domainEvent.EventId);
    return;  // ✅ NOW ONLY EXITS IF NO RECIPIENTS AT ALL
}

_logger.LogInformation(
    "[Phase 3] Sending cancellation emails to {TotalCount} unique recipients for Event {EventId}. " +
    "Breakdown: Registrations={RegCount}, EmailGroups={EmailGroupCount}, Newsletter={NewsletterCount}",
    allRecipients.Count, domainEvent.EventId,
    registrationEmails.Count,
    notificationRecipients.Breakdown.EmailGroupCount,
    notificationRecipients.Breakdown.MetroAreaSubscribers +
    notificationRecipients.Breakdown.StateLevelSubscribers +
    notificationRecipients.Breakdown.AllLocationsSubscribers);
```

#### 3.3 Create Email Template (1 hour)
**Migration**: `20260113000002_Phase_EmailStabilization_P3_EventCancellationTemplate.cs`

**Template Details**:
- Name: `event-cancelled-notification`
- Category: `Transactional`
- Branding: Orange (#f97316) to Rose (#e11d48) gradient

**Template Variables**:
- `{{EventTitle}}` - Event name
- `{{EventStartDate}}` - Original event date
- `{{EventStartTime}}` - Original event time
- `{{EventLocation}}` - Event location
- `{{CancellationReason}}` - Organizer's reason

**HTML Template** (Production-Ready):
```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Event Cancelled - LankaConnect</title>
</head>
<body style="margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f4f4f4;">
    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color: #f4f4f4;">
        <tr>
            <td align="center" style="padding: 40px 20px;">
                <table role="presentation" width="650" cellspacing="0" cellpadding="0" border="0" style="max-width: 650px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);">
                    <!-- Header with Brand Gradient -->
                    <tr>
                        <td style="background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;">
                            <h1 style="margin: 0; font-size: 28px; font-weight: bold; color: white;">Event Cancelled</h1>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style="padding: 40px 30px; background: #ffffff;">
                            <p style="font-size: 16px; margin: 0 0 20px 0; color: #333;">Dear LankaConnect Community,</p>

                            <p style="margin: 0 0 25px 0; color: #555; line-height: 1.6;">
                                We regret to inform you that the following event has been <strong>cancelled</strong>:
                            </p>

                            <!-- Event Details Box -->
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin: 25px 0; background: #fff8f5; border-left: 4px solid #FF6600; border-radius: 0 8px 8px 0;">
                                <tr>
                                    <td style="padding: 20px;">
                                        <h2 style="margin: 0 0 15px 0; color: #8B1538; font-size: 20px;">{{EventTitle}}</h2>
                                        <p style="margin: 5px 0; color: #666; font-size: 14px;"><strong>Date:</strong> {{EventStartDate}} at {{EventStartTime}}</p>
                                        <p style="margin: 5px 0; color: #666; font-size: 14px;"><strong>Location:</strong> {{EventLocation}}</p>
                                    </td>
                                </tr>
                            </table>

                            <!-- Cancellation Reason -->
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin: 25px 0; background: #fef2f2; border-left: 4px solid #DC2626; border-radius: 0 8px 8px 0;">
                                <tr>
                                    <td style="padding: 20px;">
                                        <p style="margin: 0 0 10px 0; font-weight: bold; color: #DC2626; font-size: 14px;">Cancellation Reason:</p>
                                        <p style="margin: 0; color: #666; font-size: 14px; line-height: 1.6;">{{CancellationReason}}</p>
                                    </td>
                                </tr>
                            </table>

                            <p style="margin: 20px 0 0 0; color: #555; line-height: 1.6;">
                                We apologize for any inconvenience this may cause. If you had registered for this event, your registration has been automatically cancelled.
                            </p>

                            <p style="margin: 15px 0 0 0; color: #555; line-height: 1.6;">
                                Thank you for your understanding.
                            </p>
                        </td>
                    </tr>

                    <!-- Footer with Brand Gradient -->
                    <tr>
                        <td style="background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;">
                            <p style="color: white; font-size: 20px; font-weight: bold; margin: 0 0 5px 0;">LankaConnect</p>
                            <p style="color: rgba(255,255,255,0.9); font-size: 14px; margin: 0 0 10px 0;">Sri Lankan Community Hub</p>
                            <p style="color: rgba(255,255,255,0.8); font-size: 12px; margin: 0;">&copy; 2026 LankaConnect. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
```

#### 3.4 Update EventCancelledEventHandler (2-3 hours)
**File**: `src/LankaConnect.Application/Events/EventHandlers/EventCancelledEventHandler.cs`

**Changes**:
1. Add `IEventNotificationRecipientService` dependency
2. Add `IUserRepository` dependency
3. Add `IEmailUrlHelper` dependency
4. Replace registration-only logic with full consolidation (lines 54-120)
5. Replace inline HTML generation with template
6. Remove `GenerateEventCancelledHtml()` method (lines 142-160)

**Updated Handle Method**:
```csharp
public async Task Handle(EventCancelledEvent domainEvent, CancellationToken cancellationToken)
{
    _logger.LogInformation(
        "[Phase 3] Handling EventCancelledEvent for Event {EventId}. Reason: {Reason}",
        domainEvent.EventId, domainEvent.Reason);

    try
    {
        var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
        if (@event == null)
        {
            _logger.LogWarning("[Phase 3] Event {EventId} not found, cannot send cancellation emails", domainEvent.EventId);
            return;
        }

        // 1. Get confirmed registration emails
        var registrations = await _registrationRepository.GetByEventAsync(domainEvent.EventId, cancellationToken);
        var confirmedRegistrations = registrations
            .Where(r => r.Status == RegistrationStatus.Confirmed)
            .ToList();

        var registrationEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var registration in confirmedRegistrations)
        {
            if (registration.UserId.HasValue)
            {
                var user = await _userRepository.GetByIdAsync(registration.UserId.Value, cancellationToken);
                if (user != null)
                {
                    registrationEmails.Add(user.Email.Value);
                }
            }
            else if (registration.Contact != null && !string.IsNullOrWhiteSpace(registration.Contact.Email))
            {
                registrationEmails.Add(registration.Contact.Email);
            }
            else if (registration.AttendeeInfo != null)
            {
                registrationEmails.Add(registration.AttendeeInfo.Email.Value);
            }
        }

        _logger.LogInformation("[Phase 3] Found {Count} confirmed registrations for Event {EventId}",
            registrationEmails.Count, domainEvent.EventId);

        // 2. Get email groups + newsletter subscribers
        var notificationRecipients = await _recipientService.ResolveRecipientsAsync(
            domainEvent.EventId,
            cancellationToken);

        _logger.LogInformation(
            "[Phase 3] Resolved {Count} notification recipients for Event {EventId}. " +
            "Breakdown: EmailGroups={EmailGroupCount}, Metro={MetroCount}, State={StateCount}, AllLocations={AllLocationsCount}",
            notificationRecipients.EmailAddresses.Count, domainEvent.EventId,
            notificationRecipients.Breakdown.EmailGroupCount,
            notificationRecipients.Breakdown.MetroAreaSubscribers,
            notificationRecipients.Breakdown.StateLevelSubscribers,
            notificationRecipients.Breakdown.AllLocationsSubscribers);

        // 3. Consolidate all recipients (deduplicated, case-insensitive)
        var allRecipients = registrationEmails
            .Concat(notificationRecipients.EmailAddresses)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!allRecipients.Any())
        {
            _logger.LogInformation("[Phase 3] No recipients found for Event {EventId}, skipping cancellation emails",
                domainEvent.EventId);
            return;
        }

        _logger.LogInformation(
            "[Phase 3] Sending cancellation emails to {TotalCount} unique recipients for Event {EventId}. " +
            "Breakdown: Registrations={RegCount}, Notification={NotificationCount}",
            allRecipients.Count, domainEvent.EventId,
            registrationEmails.Count,
            notificationRecipients.EmailAddresses.Count);

        // 4. Prepare template parameters
        var parameters = new Dictionary<string, object>
        {
            { "EventTitle", @event.Title.Value },
            { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
            { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
            { "EventLocation", GetEventLocationString(@event) },
            { "CancellationReason", domainEvent.Reason }
        };

        // 5. Send templated email to each recipient
        var successCount = 0;
        var failCount = 0;

        foreach (var email in allRecipients)
        {
            try
            {
                var result = await _emailService.SendTemplatedEmailAsync(
                    "event-cancelled-notification",
                    email,
                    parameters,
                    cancellationToken);

                if (result.IsSuccess)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                    _logger.LogWarning(
                        "[Phase 3] Failed to send event cancellation email to {Email} for event {EventId}: {Errors}",
                        email, domainEvent.EventId, string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogError(ex,
                    "[Phase 3] Error sending cancellation email to {Email} for event {EventId}",
                    email, domainEvent.EventId);
            }
        }

        _logger.LogInformation(
            "[Phase 3] Event cancellation emails completed for event {EventId}. Success: {SuccessCount}, Failed: {FailCount}",
            domainEvent.EventId, successCount, failCount);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "[Phase 3] Fatal error handling EventCancelledEvent for Event {EventId}",
            domainEvent.EventId);
    }
}

/// <summary>
/// Safely extracts event location string with defensive null handling.
/// Copied from EventPublishedEventHandler for consistency.
/// </summary>
private static string GetEventLocationString(Event @event)
{
    if (@event.Location?.Address == null)
        return "Online Event";

    var street = @event.Location.Address.Street;
    var city = @event.Location.Address.City;
    var state = @event.Location.Address.State;

    if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city))
        return "Online Event";

    var parts = new List<string>();
    if (!string.IsNullOrWhiteSpace(street)) parts.Add(street);
    if (!string.IsNullOrWhiteSpace(city)) parts.Add(city);
    if (!string.IsNullOrWhiteSpace(state)) parts.Add(state);

    return string.Join(", ", parts);
}
```

#### 3.5 Testing Strategy (5 Test Scenarios)
**Test Scenario 1: Confirmed Registrations Only**
- Create event without email groups/newsletter targeting
- Register 2 users with confirmed status
- Cancel event with reason
- **Expected**: 2 users receive cancellation email
- **Verify**: Email uses template with orange/rose gradient

**Test Scenario 2: Email Groups Only (No Registrations)** ⭐ **CRITICAL BUG FIX TEST**
- Create event with 1 email group (5 recipients)
- Do NOT register any users
- Cancel event with reason
- **Expected**: 5 email group recipients receive cancellation email
- **Verify**: Previously would send ZERO emails (bug), now sends to all 5
- **Verify**: Logs show `[Phase 3]` entries with correct breakdown

**Test Scenario 3: Newsletter Subscribers Only** ⭐ **CRITICAL BUG FIX TEST**
- Create event in metro area with newsletter subscribers
- Do NOT register users or add email groups
- Cancel event
- **Expected**: Metro area newsletter subscribers receive cancellation email
- **Verify**: Previously would send ZERO emails (bug), now sends to subscribers

**Test Scenario 4: Full Consolidation (All Recipient Types)**
- Create event with:
  - Email group: 3 recipients
  - Location: Metro area with 5 newsletter subscribers
  - Register 2 users with confirmed status
  - 1 overlapping email between registration and email group
- Cancel event
- **Expected**: 9 unique recipients (3+5+2-1 duplicate)
- **Verify**: Logs show correct breakdown
- **Verify**: No duplicate emails sent

**Test Scenario 5: Zero Recipients**
- Create event with no email groups, no location, no registrations
- Cancel event
- **Expected**: No emails sent, log: "No recipients found for Event {EventId}"
- **Verify**: No errors, graceful exit

#### 3.6 Deployment Checklist
- [ ] Migration reviewed and tested locally
- [ ] Email template verified (spelling, grammar, formatting)
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Build succeeds (0 errors, 0 warnings)
- [ ] Code review approved
- [ ] Deploy migration to staging database
- [ ] Verify template created: `SELECT * FROM communications.email_templates WHERE template_name = 'event-cancelled-notification';`
- [ ] Deploy application code to staging
- [ ] Test Scenario 1: Registrations only
- [ ] Test Scenario 2: Email groups only (CRITICAL - tests bug fix)
- [ ] Test Scenario 3: Newsletter subscribers only (CRITICAL - tests bug fix)
- [ ] Test Scenario 4: Full consolidation
- [ ] Test Scenario 5: Zero recipients
- [ ] Verify email template rendering in Gmail, Outlook
- [ ] Check staging logs for `[Phase 3]` entries
- [ ] Deploy to production
- [ ] Monitor production logs for 48 hours

**Success Metrics**:
- ✅ Event cancellation emails reach ALL stakeholders (registrations + email groups + newsletter subscribers)
- ✅ Zero emails sent to cancelled events with zero recipients (graceful exit)
- ✅ Template-based emails (no inline HTML)
- ✅ Comprehensive logging with recipient breakdowns

---

### Phase 4: Email Template Constants (P2)
**Duration**: 2 hours
**Risk**: Low
**Impact**: Code maintainability, prevents typos

#### 4.1 Create Template Constants Class
**File**: `src/LankaConnect.Application/Common/Constants/EmailTemplateNames.cs`

```csharp
namespace LankaConnect.Application.Common.Constants;

/// <summary>
/// Centralized constants for email template names.
/// Prevents magic strings and provides compile-time safety.
/// </summary>
public static class EmailTemplateNames
{
    // Member Management
    public const string MemberEmailVerification = "member-email-verification";

    // Event Management
    public const string EventApproved = "event-approved";
    public const string EventPublished = "event-published";
    public const string EventCancelledNotification = "event-cancelled-notification";
    public const string EventReminder = "event-reminder";

    // Event Registration
    public const string RegistrationConfirmed = "registration-confirmed";
    public const string RegistrationCancellation = "registration-cancellation";
    public const string PaymentCompleted = "payment-completed";

    // Sign-Up System
    public const string SignupCommitmentConfirmation = "signup-commitment-confirmation";

    // Newsletter
    public const string NewsletterConfirmation = "newsletter-confirmation";

    // Future Templates (Phase 6A.60, 6A.61)
    // public const string OrganizerCustomMessage = "organizer-custom-message";
}
```

#### 4.2 Refactor Handlers (15 handlers)
**Pattern**:
```csharp
// BEFORE:
await _emailService.SendTemplatedEmailAsync("event-reminder", email, parameters, ct);

// AFTER:
using LankaConnect.Application.Common.Constants;

await _emailService.SendTemplatedEmailAsync(EmailTemplateNames.EventReminder, email, parameters, ct);
```

**Files to Update**:
1. EventApprovedEventHandler.cs
2. EventPublishedEventHandler.cs
3. EventCancelledEventHandler.cs
4. EventReminderJob.cs
5. RegistrationConfirmedEventHandler.cs
6. RegistrationCancelledEventHandler.cs
7. PaymentCompletedEventHandler.cs
8. MemberVerificationRequestedEventHandler.cs
9. SubscribeToNewsletterCommandHandler.cs
10. ConfirmNewsletterSubscriptionCommandHandler.cs
11. SendEmailVerificationCommandHandler.cs
12. SendWelcomeEmailCommandHandler.cs
13. AnonymousRegistrationConfirmedEventHandler.cs
14. CommitmentCancelledEventHandler.cs
15. Any other handlers using magic strings

#### 4.3 Testing
- ✅ Build succeeds (compile-time verification)
- ✅ All existing unit tests pass (no behavior change)
- ✅ Grep for remaining magic strings: `grep -r '".*-.*-.*"' --include="*.cs" | grep SendTemplatedEmailAsync`

#### 4.4 Deployment
- [ ] Code review
- [ ] Build succeeds
- [ ] Deploy to staging
- [ ] Smoke test: Send test emails for each template
- [ ] Deploy to production
- [ ] Monitor logs for template not found errors

**Success Metrics**:
- ✅ Zero magic strings for template names in handlers
- ✅ Compile-time safety (typos cause compilation errors)
- ✅ Easier refactoring (rename template = update one constant)

---

### Phase 5: Centralized Email Orchestration (P2)
**Duration**: 2 days
**Risk**: Medium (major refactor)
**Impact**: Maintainability, consistency, testability

#### 5.1 Create Email Orchestration Service (1 day)
**Files to Create**:
- `src/LankaConnect.Application/Common/Interfaces/IEmailOrchestrationService.cs`
- `src/LankaConnect.Application/Common/Services/EmailOrchestrationService.cs`
- `tests/LankaConnect.Application.Tests/Common/Services/EmailOrchestrationServiceTests.cs`

**Interface Design**:
```csharp
/// <summary>
/// Centralized email orchestration for all system emails.
/// Handles data fetching, parameter building, URL generation, and email sending.
/// </summary>
public interface IEmailOrchestrationService
{
    Task<Result> SendMemberVerificationEmailAsync(Guid userId, CancellationToken ct);
    Task<Result> SendEventApprovalEmailAsync(Guid eventId, CancellationToken ct);
    Task<Result> SendEventPublishedEmailAsync(Guid eventId, CancellationToken ct);
    Task<Result> SendEventCancellationEmailsAsync(Guid eventId, string reason, CancellationToken ct);
    Task<Result> SendRegistrationConfirmationEmailAsync(Guid registrationId, CancellationToken ct);
    Task<Result> SendRegistrationCancellationEmailAsync(Guid registrationId, CancellationToken ct);
    Task<Result> SendPaymentCompletedEmailAsync(Guid registrationId, CancellationToken ct);
    Task<Result> SendEventReminderEmailsAsync(Guid eventId, ReminderType reminderType, CancellationToken ct);
    Task<Result> SendNewsletterConfirmationEmailAsync(string email, string token, CancellationToken ct);
}
```

**Implementation** (Example: SendRegistrationConfirmationEmailAsync):
```csharp
public class EmailOrchestrationService : IEmailOrchestrationService
{
    private readonly IEmailService _emailService;
    private readonly IEmailUrlHelper _urlHelper;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<EmailOrchestrationService> _logger;

    public async Task<Result> SendRegistrationConfirmationEmailAsync(
        Guid registrationId,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "[EmailOrchestration] Starting registration confirmation email. RegistrationId={RegistrationId}",
                registrationId);

            // 1. Fetch data
            var registration = await _registrationRepository.GetByIdAsync(registrationId, ct);
            if (registration == null)
            {
                _logger.LogWarning("[EmailOrchestration] Registration {RegistrationId} not found", registrationId);
                return Result.Failure("Registration not found");
            }

            var @event = await _eventRepository.GetByIdAsync(registration.EventId, ct);
            if (@event == null)
            {
                _logger.LogWarning("[EmailOrchestration] Event {EventId} not found for registration {RegistrationId}",
                    registration.EventId, registrationId);
                return Result.Failure("Event not found");
            }

            // 2. Determine recipient email
            string? toEmail = null;
            string toName = "Event Attendee";

            if (registration.UserId.HasValue)
            {
                var user = await _userRepository.GetByIdAsync(registration.UserId.Value, ct);
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
                {
                    toName = firstAttendee.Name;
                }
            }
            else if (registration.AttendeeInfo != null)
            {
                toEmail = registration.AttendeeInfo.Email.Value;
                toName = registration.AttendeeInfo.Name;
            }

            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("[EmailOrchestration] No email found for registration {RegistrationId}", registrationId);
                return Result.Failure("No email address found");
            }

            // 3. Build parameters
            var parameters = new Dictionary<string, object>
            {
                { "AttendeeName", toName },
                { "EventTitle", @event.Title.Value },
                { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                { "EventLocation", GetEventLocationString(@event) },
                { "Quantity", registration.Quantity },
                { "TotalPrice", registration.TotalPrice },
                { "EventUrl", _urlHelper.GetEventDetailsUrl(@event.Id) },
                { "MyEventsUrl", _urlHelper.GetMyEventsUrl() }
            };

            // 4. Send email
            var result = await _emailService.SendTemplatedEmailAsync(
                EmailTemplateNames.RegistrationConfirmed,
                toEmail,
                parameters,
                ct);

            // 5. Log result
            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[EmailOrchestration] Registration confirmation email sent successfully. " +
                    "RegistrationId={RegistrationId}, Email={Email}",
                    registrationId, toEmail);
            }
            else
            {
                _logger.LogWarning(
                    "[EmailOrchestration] Failed to send registration confirmation email. " +
                    "RegistrationId={RegistrationId}, Errors={Errors}",
                    registrationId, string.Join(", ", result.Errors));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[EmailOrchestration] Error sending registration confirmation email. RegistrationId={RegistrationId}",
                registrationId);
            return Result.Failure($"Email send failed: {ex.Message}");
        }
    }

    private static string GetEventLocationString(Event @event)
    {
        if (@event.Location?.Address == null)
            return "Online Event";

        var street = @event.Location.Address.Street;
        var city = @event.Location.Address.City;
        var state = @event.Location.Address.State;

        if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city))
            return "Online Event";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(street)) parts.Add(street);
        if (!string.IsNullOrWhiteSpace(city)) parts.Add(city);
        if (!string.IsNullOrWhiteSpace(state)) parts.Add(state);

        return string.Join(", ", parts);
    }
}
```

#### 5.2 Refactor Event Handlers (1 day)
**Pattern**:
```csharp
// BEFORE (EventApprovedEventHandler.cs):
public async Task Handle(EventApprovedEvent notification, CancellationToken cancellationToken)
{
    // 50 lines of data fetching, parameter building, email sending
}

// AFTER:
public async Task Handle(EventApprovedEvent notification, CancellationToken cancellationToken)
{
    var result = await _emailOrchestrationService.SendEventApprovalEmailAsync(
        notification.EventId,
        cancellationToken);

    if (!result.IsSuccess)
    {
        _logger.LogWarning(
            "Failed to send event approval email for Event {EventId}: {Errors}",
            notification.EventId, string.Join(", ", result.Errors));
    }
}
```

**Benefits**:
- ✅ Handlers reduced from 50-100 lines to 5-10 lines
- ✅ All email logic testable in isolation
- ✅ Consistent error handling
- ✅ Single place to add metrics, rate limiting, retry logic

#### 5.3 Testing (50 tests)
**Unit Tests** (40 tests):
- 10 tests per email type (8 types = 80 tests, prioritize top 5)
- Test data fetching (entity not found)
- Test parameter building (correct values)
- Test email sending (success/failure)
- Test logging (correct messages)

**Integration Tests** (10 tests):
- End-to-end email sending with real database
- Verify template fetching
- Verify URL generation
- Verify email queuing

#### 5.4 Deployment
- [ ] All tests pass (90%+ coverage)
- [ ] Code review approved
- [ ] Build succeeds
- [ ] Deploy to staging
- [ ] Smoke test: Trigger each email type
- [ ] Verify emails sent successfully
- [ ] Check logs for `[EmailOrchestration]` entries
- [ ] Deploy to production
- [ ] Monitor for 48 hours

**Success Metrics**:
- ✅ All email logic centralized in orchestration service
- ✅ Event handlers simplified (5-10 lines each)
- ✅ 90%+ test coverage on orchestration service
- ✅ Consistent logging format across all emails

---

### Phase 6: Signup Commitment Emails (P1)
**Duration**: 4 hours
**Risk**: Low
**Impact**: Completes missing email flow

**See**: `docs/EMAIL_SYSTEM_REMAINING_WORK_PLAN.md` Phase 6A.60 for full implementation plan

**Key Changes**:
1. Create `SignupCommitmentConfirmedEvent` domain event
2. Modify `SignUpItem.AddCommitment()` to raise event
3. Create `SignupCommitmentConfirmedEventHandler`
4. Update `CommitToSignUpItemCommandHandler` to pass new parameters

**Testing**: TDD approach with 16 tests (4 domain + 10 handler + 2 integration)

**Template**: Already exists (`signup-commitment-confirmation`)

---

### Phase 7: Manual Event Email Sending (P1)
**Duration**: 17 hours
**Risk**: High (complex feature)
**Impact**: Organizer communication tool

**See**: `docs/EMAIL_SYSTEM_REMAINING_WORK_PLAN.md` Phase 6A.61 for full implementation plan

**Key Features**:
- Recipient filtering (All, PaidOnly, FreeOnly, Specific)
- Email preview before sending
- Batch processing (50 emails per chunk, 4 concurrent)
- Idempotency (SHA256 hash prevents duplicates)
- Audit trail (complete history + per-recipient delivery status)
- Authorization (only event organizer can send)

**Database Changes**:
- `event_email_history` table
- `event_email_recipients` table

**Testing**: 30+ tests covering domain, application, and integration layers

---

### Phase 8: Observability & Monitoring (P3)
**Duration**: 1 day
**Risk**: Low
**Impact**: Production debugging, performance tracking

#### 8.1 Correlation ID Propagation (4 hours)
**Goal**: Track email flows end-to-end with unique IDs

**Implementation**:
```csharp
// In EmailOrchestrationService:
public async Task<Result> SendRegistrationConfirmationEmailAsync(Guid registrationId, CancellationToken ct)
{
    var correlationId = Guid.NewGuid();

    using (_logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = correlationId,
        ["RegistrationId"] = registrationId,
        ["EmailType"] = "RegistrationConfirmation"
    }))
    {
        _logger.LogInformation("[EmailOrchestration] Starting email send. CorrelationId={CorrelationId}", correlationId);

        // ... email logic ...

        _logger.LogInformation("[EmailOrchestration] Email send completed. CorrelationId={CorrelationId}", correlationId);
    }
}
```

**Benefits**:
- ✅ Trace single email send across multiple services
- ✅ Grep logs by correlation ID for debugging
- ✅ Performance analysis (time between start/end)

#### 8.2 Email Delivery Metrics (2 hours)
**Metrics to Track**:
- Email send rate (emails/minute)
- Email delivery success rate (%)
- Email send latency (p50, p95, p99)
- Template-specific failure rates
- Queue backlog depth

**Implementation**:
```csharp
// In EmailService or EmailOrchestrationService:
_metrics.IncrementCounter("email.sent.total", 1, new[]
{
    new KeyValuePair<string, object?>("template", templateName),
    new KeyValuePair<string, object?>("result", result.IsSuccess ? "success" : "failure")
});

_metrics.RecordValue("email.send.duration_ms", stopwatch.ElapsedMilliseconds, new[]
{
    new KeyValuePair<string, object?>("template", templateName)
});
```

**Dashboard Queries** (Seq or Application Insights):
```sql
-- Email delivery rate
SELECT COUNT(*) as total_emails,
       SUM(CASE WHEN result = 'success' THEN 1 ELSE 0 END) as successful,
       AVG(send_duration_ms) as avg_duration_ms
FROM email_logs
WHERE timestamp > NOW() - INTERVAL '1 hour'
GROUP BY template_name;

-- Failed emails
SELECT template_name, COUNT(*) as failures,
       STRING_AGG(DISTINCT error_message, ', ') as errors
FROM email_logs
WHERE result = 'failure' AND timestamp > NOW() - INTERVAL '24 hours'
GROUP BY template_name
ORDER BY failures DESC;
```

#### 8.3 Alerting Rules (2 hours)
**Critical Alerts**:
- Email delivery success rate < 95% in 1-hour window
- Email send latency > 2 minutes (p95)
- Template placeholder errors > 0
- Event reminder job failure
- Email queue backlog > 500

**Implementation** (Azure Monitor or Seq):
```json
{
  "name": "Email Delivery Rate Alert",
  "condition": {
    "query": "SELECT COUNT(*) as success_rate FROM email_logs WHERE result = 'success' AND timestamp > NOW() - 1 HOUR",
    "threshold": 0.95,
    "operator": "LessThan"
  },
  "actions": [
    {
      "type": "email",
      "recipients": ["ops-team@lankaconnect.com"],
      "subject": "CRITICAL: Email delivery rate below 95%"
    },
    {
      "type": "slack",
      "channel": "#production-alerts"
    }
  ]
}
```

---

## Deployment Strategy

### Staging Deployment Process
1. **Pre-Deployment**
   - Run all unit tests locally
   - Run all integration tests locally
   - Build succeeds (0 errors, 0 warnings)
   - Code review approved
   - Migration scripts reviewed

2. **Database Migration** (if applicable)
   ```bash
   dotnet ef database update --context AppDbContext --connection "<staging-connection>"
   ```
   - Verify tables/columns created
   - Run verification queries
   - Test rollback script

3. **Application Deployment**
   ```bash
   az containerapp update \
     --name lankaconnect-api-staging \
     --resource-group LankaConnect-RG \
     --image <image-tag>
   ```

4. **Verification**
   - Health check endpoint responds (200 OK)
   - Application logs show successful startup
   - Database connection successful
   - Test phase-specific functionality
   - Check logs for phase-specific markers (e.g., `[Phase 3]`)
   - Verify email sending works

5. **Monitoring** (24-48 hours)
   - Check error logs
   - Monitor email delivery rates
   - Verify performance metrics
   - User acceptance testing

### Production Deployment Process
1. **Pre-Deployment Checks**
   - Staging verification complete (all tests passed)
   - No critical issues in staging logs
   - Business sign-off received
   - Rollback plan documented

2. **Deployment Window**
   - Low-traffic hours (e.g., 2 AM UTC)
   - Announce maintenance window (if needed)

3. **Deploy**
   - Database migration (if applicable)
   - Application deployment
   - Smoke tests
   - Health check

4. **Post-Deployment**
   - Monitor logs for 48 hours
   - Check email delivery rates
   - Verify all email types working
   - User feedback monitoring

### Rollback Procedures

**Phase 1 (URL Helper) - No Migration**:
- Disable feature flag (if implemented)
- Redeploy previous version
- No database rollback needed

**Phase 2 (Event Reminders) - With Migration**:
- Disable Hangfire job via dashboard
- Redeploy previous version
- Database rollback:
  ```bash
  dotnet ef database update <previous-migration> --context AppDbContext
  ```

**Phase 3 (Event Cancellation) - With Migration**:
- Disable event cancellation emails (feature flag)
- Redeploy previous version
- Database rollback (remove template)

**Rollback Criteria**:
- Email delivery failure rate > 10%
- Application errors > 5% of requests
- Database performance degradation
- Critical security vulnerability discovered
- User-reported data corruption

---

## Testing Strategy

### Test Pyramid
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

### Unit Tests (60% of tests)
**Coverage Target**: 90%+

**What to Test**:
- URL helper methods (15 tests)
- Email orchestration service methods (40 tests)
- Event handler logic (30 tests)
- Domain entity behavior (20 tests)
- Repository methods (15 tests)

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

### Integration Tests (25% of tests)
**Coverage Target**: Critical flows only

**What to Test**:
- End-to-end email sending with real templates
- Database operations (migrations, tracking)
- URL generation in different environments
- Email queue processing
- Hangfire job execution

**Example**:
```csharp
[Fact]
public async Task EventReminderJob_WithUpcomingEvent_SendsReminderEmail()
{
    // Arrange: Create event in database with start date in 24 hours
    var eventId = await CreateTestEventInDatabase(DateTime.UtcNow.AddHours(24));

    // Act: Run job
    await _eventReminderJob.ExecuteAsync();

    // Assert: Check email was sent
    var emailLog = await _dbContext.EmailLogs
        .Where(x => x.EventId == eventId && x.TemplateName == "event-reminder")
        .FirstOrDefaultAsync();

    emailLog.Should().NotBeNull();
    emailLog!.Status.Should().Be("Sent");
}
```

### Manual Testing (15% of validation)
**Email Rendering Checklist**:
- [ ] Email displays correctly in Gmail web
- [ ] Email displays correctly in Outlook desktop
- [ ] Email displays correctly in Apple Mail (iOS)
- [ ] Email displays correctly in dark mode
- [ ] All links are clickable and functional
- [ ] Unsubscribe link works
- [ ] Email doesn't go to spam folder
- [ ] Images load correctly
- [ ] No broken layouts on mobile
- [ ] Template variables all populated (no `{{variable}}` raw text)

**Functional Testing**:
- [ ] Member registration sends verification email
- [ ] Event approval sends notification to organizer
- [ ] Event registration sends confirmation to attendee
- [ ] Event cancellation sends to all recipients (registrations + email groups + newsletter)
- [ ] Event reminders sent at 7 days, 2 days, 1 day before
- [ ] No duplicate reminders sent
- [ ] Signup commitment sends confirmation
- [ ] All URLs point to correct environment

---

## Risk Assessment & Mitigation

### Phase 1: URL Centralization
**Risks**:
- Missing configuration keys in environment
- URL helper throws exception instead of graceful fallback
- Performance impact from configuration reads

**Mitigation**:
- Default values for optional paths
- Validation at startup (fail fast if critical keys missing)
- Cache configuration values in URL helper
- Comprehensive unit tests for all edge cases

### Phase 2: Event Reminders
**Risks**:
- Duplicate reminders if tracking fails
- Database migration rollback complexity
- Hangfire job not starting after deployment
- Performance impact with large event sets

**Mitigation**:
- Unique constraint on (event_id, reminder_type) prevents duplicates
- Test migration rollback in staging
- Manual verification of Hangfire dashboard after deployment
- Index on event start_date for query performance
- 2-hour window prevents duplicates with hourly job

### Phase 3: Event Cancellation
**Risks**:
- Recipient consolidation logic complexity
- Performance with large email groups + newsletter subscribers
- Template rendering issues
- Email service rate limits

**Mitigation**:
- Comprehensive unit tests for recipient consolidation
- Batch processing (50 emails at a time)
- Test template rendering in multiple email clients
- Monitor email send rates, alert if > 80% of rate limit
- 5 test scenarios covering all edge cases

### Phase 5: Email Orchestration Refactor
**Risks**:
- Breaking existing email flows during refactor
- Regression bugs from handler changes
- Performance overhead from additional abstraction layer

**Mitigation**:
- Incremental refactor (1-2 handlers at a time)
- Comprehensive regression test suite
- A/B testing in staging (old vs new handlers)
- Performance benchmarks before/after refactor
- Feature flags to disable orchestration service if issues

---

## Success Metrics

### P0: Critical Issues (Must Fix)
- ✅ **Zero hardcoded URLs** - All URLs from configuration (Phase 1)
- ✅ **Event reminder system operational** - 3-tier reminders sending (Phase 2)
- ✅ **Complete recipient coverage** - Cancellations reach all stakeholders (Phase 3)

### P1: High Priority (Should Fix)
- ✅ **Template constants** - Zero magic strings in code (Phase 4)
- ✅ **Centralized orchestration** - All email logic in one service (Phase 5)
- ✅ **Signup commitment emails** - Missing flow implemented (Phase 6)

### P2: Medium Priority (Nice to Have)
- ✅ **Manual event emails** - Organizer communication tool (Phase 7)
- ✅ **Observability** - Correlation IDs, metrics, alerts (Phase 8)

### Quality Metrics
- ✅ **90%+ test coverage** - Comprehensive unit + integration tests
- ✅ **0 compilation errors** - TDD approach, zero tolerance
- ✅ **< 5% email failure rate** - Reliable delivery
- ✅ **< 30 second send latency** (p95) - Fast email processing
- ✅ **Zero duplicate reminders** - Tracking prevents re-sends

### Operational Metrics
- ✅ **Independent deployment** - Each phase deployable separately
- ✅ **Rollback capability** - Clear rollback procedures documented
- ✅ **Monitoring in place** - Logs, metrics, alerts operational
- ✅ **Documentation complete** - All phases documented in PROGRESS_TRACKER.md

---

## Timeline Summary

| Phase | Duration | Risk | Status |
|-------|----------|------|--------|
| Phase 1: URL Centralization | 4-6 hours | Low | Not Started |
| Phase 2: Event Reminder Fix | 6-8 hours | Medium | Not Started |
| Phase 3: Event Cancellation Fix | 4-5 hours | Medium | Not Started |
| Phase 4: Template Constants | 2 hours | Low | Not Started |
| Phase 5: Email Orchestration | 2 days | Medium | Not Started |
| Phase 6: Signup Commitment Emails | 4 hours | Low | Not Started |
| Phase 7: Manual Event Emails | 17 hours | High | Not Started |
| Phase 8: Observability | 1 day | Low | Not Started |
| **TOTAL** | **6-8 days** | - | - |

**Recommended Order**:
1. Phase 1 (P0) - Unblocks staging deploys
2. Phase 2 (P0) - Restores critical automated notifications
3. Phase 3 (P0) - Fixes incomplete recipient logic
4. Phase 4 (P2) - Quick win, improves code quality
5. Phase 5 (P2) - Major refactor, wait for confidence from phases 1-4
6. Phase 6 (P1) - New feature, builds on phase 5
7. Phase 7 (P1) - Complex feature, final major addition
8. Phase 8 (P3) - Observability, ongoing improvement

---

## Appendix A: File References

### Configuration Files
- `src/LankaConnect.API/appsettings.json`
- `src/LankaConnect.API/appsettings.Staging.json`
- `src/LankaConnect.API/appsettings.Production.json`

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

### Repositories
- `src/LankaConnect.Domain/Events/IEventRepository.cs`
- `src/LankaConnect.Domain/Users/IUserRepository.cs`
- `src/LankaConnect.Domain/Events/IRegistrationRepository.cs`

---

## Appendix B: Architecture Diagrams

### Before: Current Architecture (Fragile)
```
┌─────────────────────────────────────────────────────────────┐
│                 Event Handlers (13 files)                   │
│  ❌ Hardcoded URLs in 7 handlers                            │
│  ❌ Duplicated parameter building logic                     │
│  ❌ Inconsistent error handling                             │
│  ❌ Magic string template names                             │
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
│ (Background)     │    │  Service (SMTP)  │
└──────────────────┘    └──────────────────┘

┌─────────────────────────────────────────────────────────────┐
│           EventReminderJob (BROKEN)                         │
│  ❌ NOT REGISTERED in Hangfire                              │
│  ❌ Hardcoded 24-hour window                                │
│  ❌ No tracking (duplicates possible)                       │
│  ❌ Hardcoded production URL                                │
└─────────────────────────────────────────────────────────────┘
```

### After: Stabilized Architecture (Production-Ready)
```
┌─────────────────────────────────────────────────────────────┐
│                 Event Handlers (13 files)                   │
│  ✅ Slim handlers (5-10 lines)                              │
│  ✅ No URL generation                                       │
│  ✅ No parameter building                                   │
│  ✅ Consistent error handling                               │
└────────────────────┬────────────────────────────────────────┘
                     │ Calls
                     ▼
┌─────────────────────────────────────────────────────────────┐
│         IEmailOrchestrationService (NEW)                    │
│  ✅ SendEventApprovalEmailAsync()                           │
│  ✅ SendRegistrationConfirmationEmailAsync()                │
│  ✅ SendEventCancellationEmailsAsync()                      │
│  ✅ SendEventReminderEmailsAsync()                          │
│  ✅ Centralized logging, metrics, error handling            │
└────────────────────┬────────────────────────────────────────┘
                     │ Uses
         ┌───────────┴───────────┬───────────────────┐
         ▼                       ▼                   ▼
┌──────────────────┐   ┌──────────────────┐  ┌──────────────────┐
│ IEmailUrlHelper  │   │ IEmailService    │  │ IEventRepository │
│  ✅ Config-based │   │  Template-based  │  │  Data fetching   │
│  ✅ Environment  │   │  Queue or direct │  │                  │
│     aware        │   │                  │  │                  │
└──────────────────┘   └──────────────────┘  └──────────────────┘

┌─────────────────────────────────────────────────────────────┐
│           EventReminderJob (FIXED)                          │
│  ✅ REGISTERED in Hangfire (hourly)                         │
│  ✅ 3-tier reminders (7 days, 2 days, 1 day)                │
│  ✅ Tracking table prevents duplicates                      │
│  ✅ Config-based URLs                                       │
│  ✅ Comprehensive logging                                   │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│           event_reminders_sent (NEW)                        │
│  ✅ Tracks which reminders sent per event                   │
│  ✅ Prevents duplicates with unique constraint              │
│  ✅ Records success/failure counts                          │
└─────────────────────────────────────────────────────────────┘
```

---

## Appendix C: Key Decision Records

### ADR #1: URL Helper vs Configuration Injection
**Decision**: Create dedicated `IEmailUrlHelper` service instead of injecting `IConfiguration` into handlers
**Rationale**:
- ✅ Single responsibility (URL generation)
- ✅ Testable in isolation
- ✅ Encapsulates URL building logic
- ✅ Easy to add logging, validation, caching
- ❌ One more service dependency

**Alternatives Considered**:
- Direct `IConfiguration` injection: Simple but less testable
- Static helper class: No DI, hard to test

### ADR #2: Reminder Tracking Table vs In-Memory Cache
**Decision**: Database table (`event_reminders_sent`) for tracking
**Rationale**:
- ✅ Survives application restarts
- ✅ Queryable for analytics
- ✅ Unique constraint prevents duplicates at DB level
- ✅ Audit trail for debugging
- ❌ Database I/O overhead (minimal, infrequent queries)

**Alternatives Considered**:
- Redis cache: Faster but ephemeral
- Event flag on domain entity: Requires loading full event

### ADR #3: Recipient Consolidation vs Separate Handlers
**Decision**: Single handler with recipient consolidation for event cancellations
**Rationale**:
- ✅ Single source of truth
- ✅ Reuses `IEventNotificationRecipientService`
- ✅ Consistent with `EventPublishedEventHandler` pattern
- ✅ Deduplicates recipients automatically
- ❌ More complex logic

**Alternatives Considered**:
- Separate handlers for each recipient type: Duplicate code, hard to maintain
- Domain service for recipient resolution: Overengineering

### ADR #4: Email Orchestration Service vs Direct Email Service
**Decision**: Create `IEmailOrchestrationService` layer between handlers and email service
**Rationale**:
- ✅ Centralized email logic (data fetching, parameter building)
- ✅ Testable in isolation
- ✅ Consistent error handling
- ✅ Easy to add metrics, rate limiting, retry logic
- ✅ Handlers reduced to 5-10 lines
- ❌ Additional abstraction layer

**Alternatives Considered**:
- Direct email service: Simpler but duplicated logic across handlers
- Domain service per email type: Too many services

### ADR #5: TDD Approach vs Write Tests Later
**Decision**: Test-Driven Development (Red-Green-Refactor) for all phases
**Rationale**:
- ✅ Zero-tolerance for compilation errors
- ✅ Forces design thinking upfront
- ✅ 90%+ test coverage by default
- ✅ Confidence in refactoring
- ✅ Prevents regression bugs
- ❌ Slower initial development

**Alternatives Considered**:
- Write tests after implementation: Faster initially but lower coverage

---

## Contact & Questions

**Document Owner**: System Architect
**Last Updated**: 2026-01-12
**Status**: Ready for Implementation

For questions about this plan:
1. Review comprehensive analysis: `docs/EMAIL_SYSTEM_COMPREHENSIVE_ANALYSIS.md`
2. Review remaining work plan: `docs/EMAIL_SYSTEM_REMAINING_WORK_PLAN.md`
3. Check PHASE_6A_MASTER_INDEX.md for phase numbers
4. Refer to TASK_SYNCHRONIZATION_STRATEGY.md for documentation protocol

---

**END OF STABILIZATION PLAN**
