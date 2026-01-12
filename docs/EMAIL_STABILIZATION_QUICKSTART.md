# Email System Stabilization - Quick Start Guide
**Date**: 2026-01-12
**For**: Immediate Implementation
**Reference**: [EMAIL_SYSTEM_STABILIZATION_PLAN.md](./EMAIL_SYSTEM_STABILIZATION_PLAN.md)

---

## 🚨 CRITICAL: Start Here

This guide provides the **fastest path** to stabilize the LankaConnect email system. Follow these steps in order.

---

## Day 1: Phase 1 (6A.70) - URL Centralization (4-6 hours)

### Step 1.1: Create URL Helper Service (2 hours)

**Create File**: `src/LankaConnect.Application/Common/Interfaces/IEmailUrlHelper.cs`
```csharp
namespace LankaConnect.Application.Common.Interfaces;

public interface IEmailUrlHelper
{
    string GetEventDetailsUrl(Guid eventId);
    string GetEventManageUrl(Guid eventId);
    string GetEmailVerificationUrl(string token);
    string GetMyEventsUrl();
    string GetNewsletterConfirmUrl(string token);
    string GetNewsletterUnsubscribeUrl(string token);
}
```

**Create File**: `src/LankaConnect.Application/Common/Services/EmailUrlHelper.cs`
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
                _logger.LogWarning("[EmailUrlHelper] Config key {Key} not found, using default: {Default}", key, defaultValue);
                return defaultValue;
            }
            throw new InvalidOperationException($"Configuration key '{key}' is required but not found in appsettings");
        }
        return value;
    }
}
```

**Register in DI** (`src/LankaConnect.Application/DependencyInjection.cs`):
```csharp
services.AddScoped<IEmailUrlHelper, EmailUrlHelper>();
```

### Step 1.2: Update Configuration (30 minutes)

**Edit**: `src/LankaConnect.API/appsettings.json`
```json
"ApplicationUrls": {
  "FrontendBaseUrl": "https://lankaconnect.com",
  "EmailVerificationPath": "/verify-email",
  "UnsubscribePath": "/unsubscribe",
  "EventDetailsPath": "/events/{eventId}",
  "EventManagePath": "/events/{eventId}/manage",
  "MyEventsPath": "/profile/my-events",
  "NewsletterConfirmPath": "/api/newsletter/confirm",
  "NewsletterUnsubscribePath": "/api/newsletter/unsubscribe"
}
```

**Copy to Staging** (`appsettings.Staging.json`):
```json
"ApplicationUrls": {
  "FrontendBaseUrl": "https://lankaconnect-ui-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io",
  "EmailVerificationPath": "/verify-email",
  "UnsubscribePath": "/unsubscribe",
  "EventDetailsPath": "/events/{eventId}",
  "EventManagePath": "/events/{eventId}/manage",
  "MyEventsPath": "/profile/my-events",
  "NewsletterConfirmPath": "/api/newsletter/confirm",
  "NewsletterUnsubscribePath": "/api/newsletter/unsubscribe"
}
```

### Step 1.3: Refactor 7 Handlers (2 hours)

**Search for hardcoded URLs**:
```bash
grep -r "https://lankaconnect.com" --include="*EventHandler.cs" src/
```

**Refactor Pattern** (Example: `EventApprovedEventHandler.cs`):
```csharp
// 1. Add constructor parameter
private readonly IEmailUrlHelper _urlHelper;

public EventApprovedEventHandler(
    IEventRepository eventRepository,
    IUserRepository userRepository,
    IEmailService emailService,
    IEmailUrlHelper urlHelper,  // ADD THIS
    ILogger<EventApprovedEventHandler> logger)
{
    _eventRepository = eventRepository;
    _userRepository = userRepository;
    _emailService = emailService;
    _urlHelper = urlHelper;  // ADD THIS
    _logger = logger;
}

// 2. Replace hardcoded URL (around line 45)
// BEFORE:
{ "EventUrl", $"https://lankaconnect.com/events/{domainEvent.EventId}" }

// AFTER:
{ "EventUrl", _urlHelper.GetEventDetailsUrl(domainEvent.EventId) }
{ "ManageEventUrl", _urlHelper.GetEventManageUrl(domainEvent.EventId) }
```

**Files to Update**:
1. `MemberVerificationRequestedEventHandler.cs` (line 42)
2. `EventApprovedEventHandler.cs` (line 45)
3. `RegistrationConfirmedEventHandler.cs` (line 67)
4. `EventPublishedEventHandler.cs` (line 98)
5. `PaymentCompletedEventHandler.cs` (line 56)
6. `AnonymousRegistrationConfirmedEventHandler.cs` (line 78)
7. `CommitmentCancelledEventHandler.cs` (line 89)

### Step 1.4: Test & Deploy (1 hour)

```bash
# Build
dotnet build --no-incremental

# Test
dotnet test

# Deploy to staging
az containerapp update --name lankaconnect-api-staging --resource-group LankaConnect-RG --image <image-tag>

# Verify
curl https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/health
```

**Manual Test**:
- Approve an event in staging
- Check organizer email
- Verify URL points to staging: `https://lankaconnect-ui-staging...`

---

## Day 2-3: Phase 2 (6A.71) - Event Reminder System Fix (6-8 hours)

### Step 2.1: Create Migration (1 hour)

**Create File**: `src/LankaConnect.Infrastructure/Data/Migrations/20260113000001_Phase_EmailStabilization_P2_EventReminderTracking.cs`

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

public partial class Phase_EmailStabilization_P2_EventReminderTracking : Migration
{
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

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "event_reminders_sent");
    }
}
```

**Apply Migration**:
```bash
# Local
dotnet ef database update --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API

# Staging
dotnet ef database update --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API --connection "<staging-connection>"
```

### Step 2.2: Create Domain Entity (1 hour)

**Create File**: `src/LankaConnect.Domain/Events/Enums/ReminderType.cs`
```csharp
namespace LankaConnect.Domain.Events.Enums;

public enum ReminderType
{
    OneWeek = 1,
    TwoDays = 2,
    OneDay = 3
}
```

**Create File**: `src/LankaConnect.Domain/Events/Entities/EventReminderSent.cs`
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

    public static EventReminderSent Create(Guid eventId, ReminderType reminderType, int recipientCount)
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
```

### Step 2.3: Create Repository (1 hour)

**Interface** (`src/LankaConnect.Application/Common/Interfaces/IEventReminderRepository.cs`):
```csharp
public interface IEventReminderRepository
{
    Task<bool> HasReminderBeenSentAsync(Guid eventId, ReminderType reminderType, CancellationToken ct);
    Task<EventReminderSent> RecordReminderSentAsync(Guid eventId, ReminderType reminderType, int recipientCount, CancellationToken ct);
    Task UpdateReminderResultsAsync(Guid reminderId, int successful, int failed, CancellationToken ct);
}
```

**Implementation** (`src/LankaConnect.Infrastructure/Data/Repositories/EventReminderRepository.cs`):
```csharp
public class EventReminderRepository : IEventReminderRepository
{
    private readonly AppDbContext _context;

    public EventReminderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasReminderBeenSentAsync(Guid eventId, ReminderType reminderType, CancellationToken ct)
    {
        return await _context.EventRemindersSent
            .AnyAsync(x => x.EventId == eventId && x.ReminderType == reminderType, ct);
    }

    public async Task<EventReminderSent> RecordReminderSentAsync(Guid eventId, ReminderType reminderType, int recipientCount, CancellationToken ct)
    {
        var reminder = EventReminderSent.Create(eventId, reminderType, recipientCount);
        await _context.EventRemindersSent.AddAsync(reminder, ct);
        await _context.SaveChangesAsync(ct);
        return reminder;
    }

    public async Task UpdateReminderResultsAsync(Guid reminderId, int successful, int failed, CancellationToken ct)
    {
        var reminder = await _context.EventRemindersSent.FindAsync(new object[] { reminderId }, ct);
        if (reminder != null)
        {
            reminder.UpdateSendResults(successful, failed);
            await _context.SaveChangesAsync(ct);
        }
    }
}
```

**Register in DI** (`src/LankaConnect.Infrastructure/DependencyInjection.cs`):
```csharp
services.AddScoped<IEventReminderRepository, EventReminderRepository>();
```

### Step 2.4: Update EventReminderJob (2 hours)

**Edit**: `src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs`

**Add Dependencies**:
```csharp
private readonly IEventReminderRepository _eventReminderRepository;
private readonly IEmailUrlHelper _urlHelper;

public EventReminderJob(
    IEventRepository eventRepository,
    IUserRepository userRepository,
    IEmailService emailService,
    IEventReminderRepository eventReminderRepository,  // ADD
    IEmailUrlHelper urlHelper,  // ADD
    ILogger<EventReminderJob> logger)
{
    _eventRepository = eventRepository;
    _userRepository = userRepository;
    _emailService = emailService;
    _eventReminderRepository = eventReminderRepository;  // ADD
    _urlHelper = urlHelper;  // ADD
    _logger = logger;
}
```

**Update ExecuteAsync**:
```csharp
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

**Update SendRemindersForWindowAsync** (add ReminderType parameter, check tracking, use URL helper):
```csharp
private async Task SendRemindersForWindowAsync(
    DateTime now,
    int startHours,
    int endHours,
    ReminderType reminderType,  // ADD
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
        windowStart, windowEnd, new[] { EventStatus.Published, EventStatus.Active });

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
                "[Phase 2] Sending {ReminderType} reminders for event {EventId} to {Count} attendees",
                reminderType, @event.Id, registrations.Count);

            var successCount = 0;
            var failCount = 0;

            // RECORD: Mark reminder as being sent
            var reminderRecord = await _eventReminderRepository.RecordReminderSentAsync(
                @event.Id, reminderType, registrations.Count, cancellationToken);

            foreach (var registration in registrations)
            {
                try
                {
                    // ... existing email recipient logic ...

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
                        // FIXED: Use URL helper
                        { "EventDetailsUrl", _urlHelper.GetEventDetailsUrl(@event.Id) },
                        { "HasOrganizerContact", @event.HasOrganizerContact() },
                        { "OrganizerContactName", @event.OrganizerContactName ?? "" },
                        { "OrganizerContactEmail", @event.OrganizerContactEmail ?? "" },
                        { "OrganizerContactPhone", @event.OrganizerContactPhone ?? "" }
                    };

                    var result = await _emailService.SendTemplatedEmailAsync(
                        "event-reminder", toEmail, parameters, cancellationToken);

                    if (result.IsSuccess)
                        successCount++;
                    else
                        failCount++;
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logger.LogError(ex, "[Phase 2] Error sending reminder");
                }
            }

            // UPDATE: Record final results
            await _eventReminderRepository.UpdateReminderResultsAsync(
                reminderRecord.Id, successCount, failCount, cancellationToken);

            _logger.LogInformation(
                "[Phase 2] {ReminderType} reminders for event {EventId}: Success={Success}, Failed={Failed}",
                reminderType, @event.Id, successCount, failCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 2] Error processing reminders for event {EventId}", @event.Id);
        }
    }
}
```

### Step 2.5: Register Hangfire Job (30 minutes)

**Find or Create**: `src/LankaConnect.API/RecurringJobsConfiguration.cs`

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
```

**Register in Program.cs** (after `app.UseHangfireDashboard()`):
```csharp
RecurringJobsConfiguration.ConfigureRecurringJobs();
```

### Step 2.6: Test & Deploy (1 hour)

```bash
# Build & Test
dotnet build
dotnet test

# Deploy to staging
dotnet ef database update --connection "<staging-connection>"
az containerapp update --name lankaconnect-api-staging --resource-group LankaConnect-RG --image <image-tag>

# Verify Hangfire
# Navigate to: https://lankaconnect-api-staging.../hangfire
# Check "Recurring Jobs" tab
# Verify "event-reminders" job is registered

# Manual Test
# 1. Create test event in staging with start date in 7 days
# 2. Trigger job manually in Hangfire dashboard
# 3. Check email received
# 4. Verify tracking record: SELECT * FROM event_reminders_sent;
```

---

## Day 3-4: Phase 3 (6A.72) - Event Cancellation Fix (4-5 hours)

### Quick Summary
1. Create `event-cancelled-notification` email template (1 hour)
2. Add `IEventNotificationRecipientService` and `IUserRepository` to `EventCancelledEventHandler` (30 min)
3. Replace registration-only logic with full consolidation (2 hours)
4. Test with 5 scenarios (1.5 hours)

**Full Implementation**: See [EMAIL_SYSTEM_STABILIZATION_PLAN.md Phase 3](./EMAIL_SYSTEM_STABILIZATION_PLAN.md#phase-3-event-cancellation-recipient-fix-p0)

---

## Week 2+: Remaining Phases

**Day 5**: Phase 4 (6A.73) - Email Template Constants (2 hours)
- Create `EmailTemplateNames` constants class
- Refactor 15 handlers to use constants

**Day 6**: Phase 6 (6A.60) - Signup Commitment Emails (4 hours)
- Create domain event
- Create event handler
- TDD approach with 16 tests

**Days 8-9**: Phase 5 (6A.74) - Email Orchestration (2 days)
- Create `IEmailOrchestrationService`
- Refactor handlers to use orchestration

**Days 10-12**: Phase 7 (6A.61) - Manual Event Emails (17 hours)
- Create database tables
- Implement batch processor
- Add API endpoints

**Ongoing**: Phase 8 (6A.75) - Observability (1 day)
- Add correlation IDs
- Add metrics
- Configure alerts

---

## Emergency Rollback Procedures

### Phase 1 Rollback (No Migration)
```bash
# Disable feature (if flag exists)
# Redeploy previous version
az containerapp update --name lankaconnect-api-staging --resource-group LankaConnect-RG --image <previous-tag>
```

### Phase 2 Rollback (With Migration)
```bash
# 1. Disable Hangfire job via dashboard
# Navigate to /hangfire, click "event-reminders", click "Delete"

# 2. Redeploy previous version
az containerapp update --name lankaconnect-api-staging --resource-group LankaConnect-RG --image <previous-tag>

# 3. Rollback database (if needed)
dotnet ef database update <previous-migration> --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API --connection "<connection>"
```

---

## Success Checklist

After each phase:

- [ ] Build succeeds (0 errors, 0 warnings)
- [ ] All tests pass
- [ ] Code review approved
- [ ] Deployed to staging
- [ ] Manual testing completed
- [ ] Logs checked (no errors)
- [ ] Deployed to production
- [ ] Monitored for 24-48 hours
- [ ] Phase summary document created
- [ ] PHASE_6A_MASTER_INDEX.md updated
- [ ] PROGRESS_TRACKER.md updated

---

## Need Help?

**Documentation**:
- [EMAIL_SYSTEM_STABILIZATION_PLAN.md](./EMAIL_SYSTEM_STABILIZATION_PLAN.md) - Full implementation details
- [EMAIL_STABILIZATION_PHASE_ASSIGNMENTS.md](./EMAIL_STABILIZATION_PHASE_ASSIGNMENTS.md) - Phase numbers and timeline
- [EMAIL_SYSTEM_COMPREHENSIVE_ANALYSIS.md](./EMAIL_SYSTEM_COMPREHENSIVE_ANALYSIS.md) - Current state analysis

**Key Files**:
- EventReminderJob.cs: `src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs`
- EventCancelledEventHandler.cs: `src/LankaConnect.Application/Events/EventHandlers/EventCancelledEventHandler.cs`
- appsettings.json: `src/LankaConnect.API/appsettings.json`

---

**GOOD LUCK! 🚀**

**Document Owner**: System Architect
**Last Updated**: 2026-01-12
**Status**: Ready for Immediate Use

**END OF QUICK START GUIDE**
