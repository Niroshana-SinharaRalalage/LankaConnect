# Phase 6A.82: Disable Automatic Event Publication Emails + Fix Template Parameters

## Executive Summary

**Date**: 2026-01-24
**Status**: ✅ Complete
**Type**: Feature Flag + Bug Fix

### Issues Addressed

1. **Automatic emails conflict with manual "Send Notification" button**
   - Organizers have a dedicated button to control when notification emails are sent
   - Automatic emails on publish were unwanted and redundant

2. **Template parameter rendering issue**
   - `template-new-event-publication` showed literal `{{HasOrganizerContact}}`, `{{OrganizerContactName}}`
   - EventPublishedEventHandler wasn't sending organizer contact parameters

---

## Part 1: Feature Flag Implementation

### Problem

When an organizer publishes an event (changes status from Draft to Published), `EventPublishedEventHandler` automatically sends notification emails to:
- Email groups associated with the event
- Newsletter subscribers matching the event location

However, the application already has a **manual "Send Notification" button** that gives organizers explicit control over when to send these emails.

**User Request**: "We should prevent sending this email automatically. We have a separate button that organizer can manually send an event publication email."

### Solution

Added `EmailSettings:AutomaticNotifications:SendOnEventPublish` feature flag (default: `false`).

**Configuration Structure**:

```json
{
  "EmailSettings": {
    "AutomaticNotifications": {
      "SendOnEventPublish": false
    }
  }
}
```

### Implementation Details

**1. Created Configuration Class**

[src/LankaConnect.Application/Common/Configuration/EmailNotificationSettings.cs](../src/LankaConnect.Application/Common/Configuration/EmailNotificationSettings.cs)

```csharp
public sealed class EmailNotificationSettings
{
    public const string SectionName = "EmailSettings:AutomaticNotifications";

    public bool SendOnEventPublish { get; set; } = false;
}
```

**Why Application layer?**
- Clean Architecture: Application handlers can't reference Infrastructure layer
- Avoids circular dependencies
- Keeps configuration close to where it's used

**2. Updated EventPublishedEventHandler**

[src/LankaConnect.Application/Events/EventHandlers/EventPublishedEventHandler.cs:70-78](../src/LankaConnect.Application/Events/EventHandlers/EventPublishedEventHandler.cs#L70-L78)

```csharp
// Phase 6A.82: Check if automatic email sending is enabled
if (!_emailNotificationSettings.SendOnEventPublish)
{
    stopwatch.Stop();
    _logger.LogInformation(
        "EventPublished SKIPPED: Automatic email notifications disabled - EventId={EventId}, Duration={ElapsedMs}ms. " +
        "Organizers can manually send notifications using 'Send Notification' button.",
        domainEvent.EventId, stopwatch.ElapsedMilliseconds);
    return; // Exit early - no emails sent
}
```

**3. Registered Configuration**

[src/LankaConnect.Infrastructure/DependencyInjection.cs:247-249](../src/LankaConnect.Infrastructure/DependencyInjection.cs#L247-L249)

```csharp
// Phase 6A.82: Register Email Notification Settings
services.Configure<Application.Common.Configuration.EmailNotificationSettings>(
    configuration.GetSection(Application.Common.Configuration.EmailNotificationSettings.SectionName));
```

**4. Updated All appsettings Files**

- [appsettings.json](../src/LankaConnect.API/appsettings.json#L127-L129)
- [appsettings.Staging.json](../src/LankaConnect.API/appsettings.Staging.json#L84-L86)
- [appsettings.Production.json](../src/LankaConnect.API/appsettings.Production.json#L73-L75)

All set to `SendOnEventPublish: false` by default.

### Behavior Matrix

| SendOnEventPublish | Behavior |
|--------------------|----------|
| `false` (default) | EventPublishedEventHandler logs skip message and returns early. No automatic emails. Organizers must use manual "Send Notification" button. |
| `true` | EventPublishedEventHandler sends notification emails automatically when event is published (legacy behavior). |

---

## Part 2: Template Parameter Fix

### Problem

User screenshot showed email with literal Handlebars syntax:
- `{{HasOrganizerContact}}`
- `{{OrganizerContactName}}`

### Root Cause

**Template**: `template-new-event-publication` includes organizer contact fields.

**Handler Comparison**:

| Handler | Sends Organizer Parameters? |
|---------|----------------------------|
| EventNotificationEmailJob.cs (manual button) | ✅ YES - Lines 335-349 |
| EventPublishedEventHandler.cs (automatic) | ❌ NO - Missing |

### Solution

Added organizer contact parameters to EventPublishedEventHandler to match EventNotificationEmailJob pattern.

**Parameters Added**:

```csharp
// Phase 6A.82: Add organizer contact if opted in (matches EventNotificationEmailJob pattern)
if (@event.HasOrganizerContact())
{
    parameters["HasOrganizerContact"] = true;
    parameters["OrganizerName"] = @event.OrganizerContactName ?? "Event Organizer";

    if (!string.IsNullOrWhiteSpace(@event.OrganizerContactEmail))
        parameters["OrganizerEmail"] = @event.OrganizerContactEmail;

    if (!string.IsNullOrWhiteSpace(@event.OrganizerContactPhone))
        parameters["OrganizerPhone"] = @event.OrganizerContactPhone;
}
else
{
    parameters["HasOrganizerContact"] = false;
}
```

**Now Both Handlers Send**:
- `HasOrganizerContact` (boolean)
- `OrganizerName` (string, defaults to "Event Organizer")
- `OrganizerEmail` (string, optional)
- `OrganizerPhone` (string, optional)

---

## Files Modified

### Part 1: Feature Flag

1. **src/LankaConnect.Infrastructure/Email/Configuration/EmailSettings.cs**
   - Added `AutomaticNotificationSettings` nested class
   - Added `AutomaticNotifications` property to `EmailSettings`

2. **src/LankaConnect.Application/Common/Configuration/EmailNotificationSettings.cs** (NEW)
   - Configuration class for Application layer
   - Defines `SendOnEventPublish` flag

3. **src/LankaConnect.Application/Events/EventHandlers/EventPublishedEventHandler.cs**
   - Added feature flag check before sending emails
   - Injected `IOptions<EmailNotificationSettings>`

4. **src/LankaConnect.Infrastructure/DependencyInjection.cs**
   - Registered `EmailNotificationSettings` configuration

5. **src/LankaConnect.API/appsettings.json**
6. **src/LankaConnect.API/appsettings.Staging.json**
7. **src/LankaConnect.API/appsettings.Production.json**
   - Added `AutomaticNotifications` section with `SendOnEventPublish: false`

### Part 2: Parameter Fix

1. **src/LankaConnect.Application/Events/EventHandlers/EventPublishedEventHandler.cs**
   - Added organizer contact parameters to match EventNotificationEmailJob pattern

---

## Testing

### Build Verification

```bash
cd src/LankaConnect.API
dotnet build --no-incremental
```

**Result**: ✅ Build succeeded. 0 errors, 0 warnings.

### Manual Testing

**Scenario 1: Event Publication (Automatic Disabled)**

1. Organizer publishes event (Draft → Published)
2. **Expected**: No automatic email sent
3. **Expected Log**: "EventPublished SKIPPED: Automatic email notifications disabled"
4. Organizer clicks "Send Notification" button manually
5. **Expected**: Email sent via `EventNotificationEmailJob` with all parameters

**Scenario 2: Organizer Contact Rendering**

1. Create event with organizer contact enabled
2. Set organizer name, email, phone
3. Use manual "Send Notification" button
4. **Expected Email Content**:
   - Shows actual organizer name (NOT `{{OrganizerName}}`)
   - Shows actual contact email (NOT `{{OrganizerEmail}}`)
   - Shows "Organizer:" section (NOT `{{HasOrganizerContact}}`)

---

## Deployment Notes

### Environment Variables

No new environment variables required. Configuration uses existing `appsettings.json` structure.

### Database Changes

None. This is a code-only change.

### Breaking Changes

None. Default behavior is to disable automatic emails (safer default).

### Rollback Plan

If automatic emails need to be re-enabled:

**Option 1: Configuration Change** (Recommended)
```json
{
  "EmailSettings": {
    "AutomaticNotifications": {
      "SendOnEventPublish": true
    }
  }
}
```

**Option 2: Code Rollback**
```bash
git revert a76c9961
git revert 8fe4c6f2
git push origin develop
```

---

## Commits

**Commit 1**: feat(phase-6a82): Add feature flag to disable automatic event publication emails
**Hash**: 8fe4c6f2
**Files**: 7 files changed (Configuration + Handler + appsettings)

**Commit 2**: fix(phase-6a82): Add missing organizer contact parameters to EventPublishedEventHandler
**Hash**: a76c9961
**Files**: 1 file changed (EventPublishedEventHandler.cs)

---

## Future Enhancements

1. **Admin UI Toggle**: Add admin dashboard setting to control `SendOnEventPublish` flag
2. **Per-Event Control**: Allow organizers to choose automatic/manual per event
3. **Scheduled Sending**: Let organizers schedule when notification email goes out
4. **A/B Testing**: Track conversion rates for automatic vs manual sending

---

## Lessons Learned

1. **Manual Controls > Automatic**: When users have explicit control UI, automatic behavior should default to OFF
2. **Parameter Consistency**: All handlers using same template should send same parameters
3. **Feature Flags**: Use configuration flags for behavior that may need runtime toggling
4. **Clean Architecture**: Configuration in Application layer avoids dependency violations

---

## Status

- [x] Feature flag implemented
- [x] Template parameter fix applied
- [x] All appsettings files updated
- [x] Build successful (0 errors)
- [x] Code committed and pushed
- [x] Documentation created

**Next Steps**: Update PROGRESS_TRACKER.md and STREAMLINED_ACTION_PLAN.md
