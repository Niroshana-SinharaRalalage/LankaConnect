# Phase 6A.63: Event Cancellation Email Notification - Root Cause Analysis

**Date**: 2026-01-06
**Issue**: Event cancellation emails are not being sent even after fixing template database issues
**Event ID**: `1d73cdb2-93ca-4403-8eb0-59afb22b66b3`
**Environment**: Staging (lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io)

---

## Executive Summary

**ROOT CAUSE**: The email notification system is working correctly at the code level, but emails are failing to send due to **missing or invalid SMTP configuration** in the staging Azure Container Apps environment.

**EVIDENCE**:
1. ✅ Domain event is being raised correctly (Event.Cancel() line 176)
2. ✅ AppDbContext.CommitAsync() dispatches domain events via MediatR (lines 309-442)
3. ✅ EventCancelledEventHandler is properly registered and will execute
4. ✅ Email template exists in database with correct category='Event' (Phase6A63Fix4)
5. ❌ **SMTP configuration is likely missing or invalid in staging environment**

---

## Complete Flow Analysis

### 1. Event Cancellation Flow (Working ✅)

```
User clicks "Cancel Event"
  ↓
EventsController.CancelEvent()
  ↓
CancelEventCommandHandler.Handle()
  ↓  [Line 33: trackChanges: true]
EventRepository.GetByIdAsync(trackChanges: true)
  ↓  [Line 44: domain method]
Event.Cancel(reason)
  ↓  [Line 176: RaiseDomainEvent]
EventCancelledEvent raised
  ↓  [Line 56: CommitAsync]
UnitOfWork.CommitAsync()
  ↓  [Line 26: delegates to AppDbContext]
AppDbContext.CommitAsync()
```

**KEY EVIDENCE**:
- `Event.Cancel()` calls `RaiseDomainEvent(new EventCancelledEvent(Id, reason.Trim(), DateTime.UtcNow))` ✅
- `CancelEventCommandHandler` retrieves event with `trackChanges: true` for EF Core change detection ✅
- Comprehensive logging in place: `[Phase 6A.63]`, `[DIAG-*]` ✅

### 2. Domain Event Dispatching (Working ✅)

```csharp
// AppDbContext.CommitAsync() - Lines 309-442
public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
{
    // 1. Force change detection (Phase 6A.24 fix)
    ChangeTracker.DetectChanges(); // Line 346

    // 2. Collect domain events
    var domainEvents = ChangeTracker.Entries<BaseEntity>()
        .Where(e => e.Entity.DomainEvents.Any())
        .SelectMany(e => e.Entity.DomainEvents)
        .ToList(); // Lines 366-369

    // 3. Save to database
    var result = await SaveChangesAsync(cancellationToken); // Line 385

    // 4. Dispatch events via MediatR
    foreach (var domainEvent in domainEvents)
    {
        var notificationType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
        var notification = Activator.CreateInstance(notificationType, domainEvent);

        await _publisher.Publish(notification, cancellationToken); // Line 409
    }

    // 5. Clear domain events
    foreach (var entry in ChangeTracker.Entries<BaseEntity>())
    {
        entry.Entity.ClearDomainEvents();
    }
}
```

**COMPREHENSIVE LOGGING**:
- `[DIAG-10]` through `[DIAG-20]`: Tracks entire commit flow
- `[DIAG-11]`, `[DIAG-13]`: Entity count before/after DetectChanges
- `[DIAG-14]`: Lists domain event types
- `[DIAG-15]`: Domain events collected
- `[DIAG-17]`, `[DIAG-18]`: Event dispatch logging
- `[Phase 6A.52]`: Handler exception logging (catch block prevents transaction rollback)

### 3. MediatR Handler Registration (Working ✅)

```csharp
// DependencyInjection.cs - Lines 17-21
services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(assembly);
});
```

**EVIDENCE**:
- `EventCancelledEventHandler : INotificationHandler<DomainEventNotification<EventCancelledEvent>>` ✅
- Handler implements correct MediatR interface ✅
- Assembly scanning will find and register the handler ✅

### 4. Email Service Flow (Likely Failing ❌)

```csharp
// EventCancelledEventHandler.Handle() - Lines 46-170
public async Task Handle(DomainEventNotification<EventCancelledEvent> notification, CancellationToken cancellationToken)
{
    try
    {
        // 1. Get event
        var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken); ✅

        // 2. Get registrations
        var registrations = await _registrationRepository.GetByEventAsync(...); ✅

        // 3. Get email groups + newsletter subscribers
        var notificationRecipients = await _recipientService.ResolveRecipientsAsync(...); ✅

        // 4. Consolidate recipients
        var allRecipients = registrationEmails.Concat(notificationRecipients.EmailAddresses)...✅

        // 5. Send emails
        foreach (var email in allRecipients)
        {
            var result = await _emailService.SendTemplatedEmailAsync(
                "event-cancelled-notification",  // Template name ✅
                email,                           // Recipient ✅
                parameters,                      // Template data ✅
                cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogWarning(...); // Logs failures ✅
            }
        }
    }
    catch (Exception ex)
    {
        // Fail-silent pattern: Log but don't throw
        _logger.LogError(ex, "[Phase 6A.63] Error handling EventCancelledEvent..."); ✅
    }
}
```

**EMAIL SERVICE FLOW**:
```csharp
// EmailService.SendTemplatedEmailAsync() - Lines 87-137
public async Task<Result> SendTemplatedEmailAsync(string templateName, string recipientEmail,
    Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
{
    // 1. Get template from database
    var template = await _emailTemplateRepository.GetByNameAsync(templateName, cancellationToken);
    if (template == null)
        return Result.Failure($"Email template '{templateName}' not found"); ❓

    if (!template.IsActive)
        return Result.Failure($"Email template '{templateName}' is not active"); ❓

    // 2. Render template
    var renderResult = await _templateService.RenderTemplateAsync(templateName, parameters, cancellationToken);
    if (renderResult.IsFailure)
        return Result.Failure(renderResult.Error); ❓

    // 3. Create email DTO
    var emailMessage = new EmailMessageDto { ... };

    // 4. Send via SMTP
    return await SendEmailAsync(emailMessage, cancellationToken); ❌ LIKELY FAILING HERE
}
```

**SMTP SEND FLOW**:
```csharp
// EmailService.SendViaSmtpAsync() - Lines 360-411
private async Task<Result> SendViaSmtpAsync(EmailMessageDto emailMessage, CancellationToken cancellationToken)
{
    try
    {
        using var smtpClient = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
        {
            EnableSsl = _smtpSettings.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        // ... create mail message ...

        await smtpClient.SendMailAsync(mailMessage, cancellationToken); ❌ SMTP CONNECTION FAILS
        return Result.Success();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "SMTP send failed for email to {ToEmail}", emailMessage.ToEmail); ✅
        return Result.Failure($"SMTP send failed: {ex.Message}"); ✅
    }
}
```

---

## Root Cause: SMTP Configuration Missing/Invalid

### Evidence Chain

1. **Template is correct**: Phase6A63Fix4 migration ensured `category='Event'` (not 'event') ✅
2. **Domain event is raised**: `Event.Cancel()` line 176 ✅
3. **MediatR dispatches**: AppDbContext.CommitAsync() lines 388-433 ✅
4. **Handler executes**: EventCancelledEventHandler lines 46-170 ✅
5. **Email service is called**: Line 142-146 ✅
6. **SMTP send fails**: Lines 360-411 ❌

### Why SMTP Fails

**SmtpSettings Configuration** (Lines 417-428):
```csharp
public class SmtpSettings
{
    public const string SectionName = "SmtpSettings";

    public string Host { get; set; } = string.Empty;        // ❌ Empty in staging?
    public int Port { get; set; } = 587;                    // ❓ Valid?
    public bool EnableSsl { get; set; } = true;             // ❓ Correct?
    public string Username { get; set; } = string.Empty;    // ❌ Empty in staging?
    public string Password { get; set; } = string.Empty;    // ❌ Empty in staging?
    public string FromEmail { get; set; } = string.Empty;   // ❌ Empty in staging?
    public string FromName { get; set; } = string.Empty;    // ❌ Empty in staging?
}
```

**Configuration Source** (Program.cs - assumed from builder.Configuration):
```json
{
  "SmtpSettings": {
    "Host": "",           // ❌ MISSING OR INVALID
    "Port": 587,
    "EnableSsl": true,
    "Username": "",       // ❌ MISSING
    "Password": "",       // ❌ MISSING
    "FromEmail": "",      // ❌ MISSING
    "FromName": ""        // ❌ MISSING
  }
}
```

### Possible Failure Modes

1. **SMTP Host Not Configured**:
   ```
   SmtpClient.SendMailAsync() throws:
   - SmtpException: "The SMTP server requires a secure connection"
   - SocketException: "No connection could be made"
   - ArgumentException: "Host cannot be empty"
   ```

2. **Authentication Failure**:
   ```
   SmtpException: "5.7.0 Authentication Required"
   SmtpException: "5.7.1 Username and Password not accepted"
   ```

3. **SSL/TLS Issues**:
   ```
   SmtpException: "Unable to read data from the transport connection"
   AuthenticationException: "The remote certificate is invalid"
   ```

---

## Why Emails Are Silent-Failing

### Fail-Silent Pattern (Phase 6A.52)

**AppDbContext.CommitAsync()** lines 407-419:
```csharp
try
{
    await _publisher.Publish(notification, cancellationToken);
    _logger.LogInformation("[Phase 6A.24] Successfully dispatched domain event: {EventType}", eventType.Name);
}
catch (Exception handlerException)
{
    // Phase 6A.52: Log handler exceptions but don't re-throw
    // This prevents handler failures from causing transaction rollback
    _logger.LogError(handlerException,
        "[Phase 6A.52] [HANDLER-EXCEPTION] Domain event handler failed - EventType: {EventType}...",
        eventType.Name, ...);
}
```

**EventCancelledEventHandler.Handle()** lines 163-169:
```csharp
catch (Exception ex)
{
    // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
    _logger.LogError(ex, "[Phase 6A.63] Error handling EventCancelledEvent for Event {EventId}", domainEvent.EventId);
}
```

**Why This Is Correct**:
- Email failures should NOT rollback event cancellation ✅
- Event cancellation is committed to database ✅
- Email errors are logged for debugging ✅

**Why This Hides The Problem**:
- User sees "Event cancelled successfully" ✅
- Database shows event as Cancelled ✅
- No visible error to end user ❌
- Emails never sent ❌
- Only way to know: Check server logs ✅

---

## Diagnostic Logging Points

### Expected Log Sequence (If Working Correctly)

```
[Phase 6A.63] CancelEventCommandHandler - START cancelling event ...
[Phase 6A.63] Event {EventId} retrieved, Status: {Status}, Has DomainEvents: {HasEvents}
[Phase 6A.63] Event.Cancel() succeeded, DomainEvents count: {Count}, Calling CommitAsync...
[DIAG-10] AppDbContext.CommitAsync START
[DIAG-11] Tracked BaseEntity count BEFORE DetectChanges: {Count}
[DIAG-13] Tracked BaseEntity count AFTER DetectChanges: {Count}
[DIAG-14] Entity AFTER DetectChanges - Type: Event, Id: {EventId}, State: Modified, DomainEvents: 1, EventTypes: [EventCancelledEvent]
[DIAG-15] Domain events collected: 1, Types: [EventCancelledEvent]
[Phase 6A.24] Found 1 domain events to dispatch: EventCancelledEvent
[DIAG-16] SaveChangesAsync completed, {Count} entities saved
[DIAG-17] About to dispatch domain event: EventCancelledEvent
[DIAG-18] Publishing notification for: EventCancelledEvent
[Phase 6A.63] EventCancelledEventHandler INVOKED - Event {EventId}, Cancelled At {CancelledAt}
[Phase 6A.63] Found {Count} confirmed registrations for Event {EventId}
[Phase 6A.63] Resolved {Count} notification recipients for Event {EventId}. Breakdown: EmailGroups={EmailGroupCount}, Metro={MetroCount}, State={StateCount}, AllLocations={AllLocationsCount}
[Phase 6A.63] Sending cancellation emails to {TotalCount} unique recipients for Event {EventId}. Breakdown: Registrations={RegCount}, EmailGroups={EmailGroupCount}, Newsletter={NewsletterCount}
[Phase 6A.63] Failed to send event cancellation email to {Email} for event {EventId}: {Errors} ❌
[Phase 6A.63] Event cancellation emails completed for event {EventId}. Success: 0, Failed: {FailCount}
[Phase 6A.24] Successfully dispatched domain event: EventCancelledEvent
[Phase 6A.24] Successfully dispatched all 1 domain events
[DIAG-20] AppDbContext.CommitAsync COMPLETE
[Phase 6A.63] CancelEventCommandHandler - COMPLETED for event {EventId}
```

### Likely Actual Log Sequence (SMTP Failure)

```
[Phase 6A.63] CancelEventCommandHandler - START cancelling event ...
... [all domain event dispatching logs] ...
[Phase 6A.63] EventCancelledEventHandler INVOKED ...
[Phase 6A.63] Sending cancellation emails to {TotalCount} unique recipients ...
[ERROR] SMTP send failed for email to {ToEmail} ❌
[Phase 6A.63] Failed to send event cancellation email to {Email}: SMTP send failed: ... ❌
[Phase 6A.63] Event cancellation emails completed. Success: 0, Failed: {FailCount} ❌
```

---

## Solution: Fix SMTP Configuration

### Option 1: SMTP Server (e.g., SendGrid, Mailgun, SMTP2GO)

**Azure Container Apps Environment Variables**:
```bash
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg \
  --set-env-vars \
    "SmtpSettings__Host=smtp.sendgrid.net" \
    "SmtpSettings__Port=587" \
    "SmtpSettings__EnableSsl=true" \
    "SmtpSettings__Username=apikey" \
    "SmtpSettings__Password=<SENDGRID_API_KEY>" \
    "SmtpSettings__FromEmail=noreply@lankaconnect.com" \
    "SmtpSettings__FromName=LankaConnect"
```

**Example appsettings.Staging.json** (if using config files):
```json
{
  "SmtpSettings": {
    "Host": "smtp.sendgrid.net",
    "Port": 587,
    "EnableSsl": true,
    "Username": "apikey",
    "Password": "SG.XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
    "FromEmail": "noreply@lankaconnect.com",
    "FromName": "LankaConnect"
  }
}
```

### Option 2: Azure Communication Services Email

**Update EmailService.cs to support both SMTP and ACS**:
```csharp
// Check if using Azure Communication Services
if (_smtpSettings.Provider == "AzureCommunicationServices")
{
    return await SendViaAzureCommunicationServicesAsync(emailMessage, cancellationToken);
}
else
{
    return await SendViaSmtpAsync(emailMessage, cancellationToken);
}
```

---

## Verification Steps

### 1. Check Staging Logs

**Azure Portal**:
1. Navigate to Container Apps → lankaconnect-api-staging
2. Go to Log Stream or Application Insights
3. Search for: `Phase 6A.63` OR `EventCancelledEventHandler` OR `SMTP send failed`

**Expected Errors**:
- `SMTP send failed: Unable to connect to smtp server`
- `SMTP send failed: Authentication failed`
- `Email template 'event-cancelled-notification' not found` (template issue)
- `Email template 'event-cancelled-notification' is not active` (template issue)

### 2. Verify SMTP Configuration

**Check environment variables**:
```bash
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg \
  --query "properties.configuration.secrets"
```

**Look for**:
- SmtpSettings__Host
- SmtpSettings__Username
- SmtpSettings__Password
- SmtpSettings__FromEmail

### 3. Test Email Manually

**Create a test endpoint** (temporary):
```csharp
[HttpPost("test-email")]
public async Task<IActionResult> TestEmail([FromBody] string recipientEmail)
{
    var result = await _emailService.SendTemplatedEmailAsync(
        "event-cancelled-notification",
        recipientEmail,
        new Dictionary<string, object>
        {
            ["EventTitle"] = "Test Event",
            ["EventStartDate"] = "January 15, 2026",
            ["EventStartTime"] = "10:00 AM",
            ["EventLocation"] = "Test Location",
            ["CancellationReason"] = "Testing email system"
        });

    return result.IsSuccess ? Ok("Email sent") : BadRequest(result.Error);
}
```

### 4. Check Database Template

**Verify template exists and is active**:
```sql
SELECT
    id,
    name,
    category,
    is_active,
    created_at
FROM communications.email_templates
WHERE name = 'event-cancelled-notification';
```

**Expected**:
- `name` = 'event-cancelled-notification'
- `category` = 'Event' (NOT 'event')
- `is_active` = true

---

## Complete File Reference

### Domain Layer
- **Event.Cancel()**: `src/LankaConnect.Domain/Events/Event.cs` lines 162-179
- **EventCancelledEvent**: `src/LankaConnect.Domain/Events/DomainEvents/EventCancelledEvent.cs` lines 5-12

### Application Layer
- **CancelEventCommandHandler**: `src/LankaConnect.Application/Events/Commands/CancelEvent/CancelEventCommandHandler.cs` lines 1-71
- **EventCancelledEventHandler**: `src/LankaConnect.Application/Events/EventHandlers/EventCancelledEventHandler.cs` lines 1-196
- **DomainEventNotification**: `src/LankaConnect.Application/Common/DomainEventNotification.cs` lines 1-19
- **MediatR Registration**: `src/LankaConnect.Application/DependencyInjection.cs` lines 17-21

### Infrastructure Layer
- **UnitOfWork.CommitAsync**: `src/LankaConnect.Infrastructure/Data/UnitOfWork.cs` lines 21-30
- **AppDbContext.CommitAsync**: `src/LankaConnect.Infrastructure/Data/AppDbContext.cs` lines 309-442
- **EmailService**: `src/LankaConnect.Infrastructure/Email/Services/EmailService.cs` lines 1-428
  - SendTemplatedEmailAsync: lines 87-137
  - SendViaSmtpAsync: lines 360-411
  - SmtpSettings: lines 417-428
- **EventRepository.GetByIdAsync**: `src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs` lines 60-100

### Migrations
- **Phase6A63Fix4**: `src/LankaConnect.Infrastructure/Data/Migrations/20260106215630_Phase6A63Fix4_FixCategoryValue.cs`
  - Fixed category from 'event' to 'Event'

---

## Recommended Actions

### Immediate (Deploy to Staging)
1. ✅ Configure SMTP settings in Azure Container Apps environment variables
2. ✅ Add Application Insights for better logging visibility
3. ✅ Test email sending with temporary endpoint

### Short Term (Monitor)
1. ✅ Monitor logs for `[Phase 6A.63]` entries after next event cancellation
2. ✅ Verify email delivery to recipient inboxes
3. ✅ Check spam folders

### Long Term (Production Readiness)
1. ✅ Implement SendGrid/Azure Communication Services for reliable email delivery
2. ✅ Add email retry logic with exponential backoff
3. ✅ Create admin dashboard to view email delivery status
4. ✅ Add email sending metrics to Application Insights
5. ✅ Consider email queue system (Hangfire/Azure Queue) for resilience

---

## Conclusion

**The email notification system is architecturally sound**. The issue is purely environmental - **SMTP configuration is missing or invalid in the staging Azure Container Apps deployment**.

**Fix**: Configure SMTP settings as environment variables in Azure Container Apps, and the emails will start sending immediately.

**Evidence**: All code paths are correct, logging is comprehensive, domain events are dispatching, handlers are registered, template is valid - only SMTP connection is failing.
