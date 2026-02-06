# Phase 6A.83 Part 4 - Email Template Fix Implementation Guide
## Quick Reference for Developers

**Date**: 2026-01-26
**Related**: [PHASE_6A83_ROOT_CAUSE_ANALYSIS.md](./PHASE_6A83_ROOT_CAUSE_ANALYSIS.md)

---

## QUICK START

### The Problem
Production emails show literal `{{OrganizerContactName}}`, `{{TicketCode}}` instead of actual values.

### The Solution
Update handlers to send BOTH old and new parameter names to match database templates.

### Implementation Pattern
```csharp
// BEFORE (broken - shows {{OrganizerContactName}} literally)
parameters["OrganizerName"] = @event.OrganizerName;

// AFTER (fixed - renders correctly)
parameters["OrganizerContactName"] = @event.OrganizerContactName ?? "Event Organizer";
parameters["OrganizerContactEmail"] = @event.OrganizerContactEmail;
parameters["OrganizerContactPhone"] = @event.OrganizerContactPhone;
```

---

## HIGH PRIORITY FIXES (Do First)

### Fix #1: EventReminderJob.cs ⭐

**File**: `src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs`

**Location**: Lines 218-233

**Change**:
```csharp
// REPLACE lines 222-228:
if (@event.HasOrganizerContact())
{
    parameters["HasOrganizerContact"] = true;
    parameters["OrganizerContactName"] = @event.OrganizerContactName ?? "Event Organizer";

    if (!string.IsNullOrWhiteSpace(@event.OrganizerContactEmail))
        parameters["OrganizerContactEmail"] = @event.OrganizerContactEmail;

    if (!string.IsNullOrWhiteSpace(@event.OrganizerContactPhone))
        parameters["OrganizerContactPhone"] = @event.OrganizerContactPhone;
}

// ADD after line 215 (before parameters dict closes):
// Add ticket parameters if this is a paid registration with ticket
if (registration.TicketId.HasValue)
{
    var ticket = await _ticketRepository.GetByIdAsync(registration.TicketId.Value, cancellationToken);
    if (ticket != null)
    {
        parameters["TicketCode"] = ticket.TicketCode;
        parameters["TicketExpiryDate"] = ticket.ExpiryDate.ToString("MMMM dd, yyyy");
    }
}
```

**Dependencies**: Add `ITicketRepository` to constructor

**Test**:
```bash
curl -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/test/trigger-reminder?eventId={guid}" \
  -H "Authorization: Bearer {token}"
```

**Verify**: Check email shows organizer name/contact + ticket code (no `{{}}`)

---

### Fix #2: PaymentCompletedEventHandler.cs ⭐⭐⭐ CRITICAL

**File**: `src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs`

**Location**: Lines 169-254

**Change**:
```csharp
// ADD after line 181 (after TotalAmount):
{ "AmountPaid", domainEvent.AmountPaid.ToString("C", CultureInfo.GetCultureInfo("en-US")) },

// ADD after line 225 (after TicketUrl):
// Add organizer contact parameters
if (@event.HasOrganizerContact())
{
    parameters["OrganizerContactName"] = @event.OrganizerContactName ?? "Event Organizer";

    if (!string.IsNullOrWhiteSpace(@event.OrganizerContactEmail))
        parameters["OrganizerContactEmail"] = @event.OrganizerContactEmail;

    if (!string.IsNullOrWhiteSpace(@event.OrganizerContactPhone))
        parameters["OrganizerContactPhone"] = @event.OrganizerContactPhone;
}
else
{
    parameters["OrganizerContactName"] = "Event Organizer";
}
```

**Test**:
```bash
# 1. Register for paid event
POST /api/events/{id}/register

# 2. Complete payment
POST /api/payments/complete

# 3. Check email
```

**Verify**: Email shows ticket code, amount paid, organizer contact (no `{{}}`)

---

### Fix #3: EventCancellationEmailJob.cs ⭐

**File**: `src/LankaConnect.Application/Events/BackgroundJobs/EventCancellationEmailJob.cs`

**Location**: Lines 208-219

**Change**:
```csharp
// REPLACE line 219 (OrganizerEmail parameter):
{ "OrganizerEmail", @event.OrganizerContactEmail ?? "support@lankaconnect.com" },
{ "OrganizerContactEmail", @event.OrganizerContactEmail ?? "support@lankaconnect.com" },
{ "OrganizerContactName", @event.OrganizerContactName ?? "Event Organizer" },
{ "OrganizerContactPhone", @event.OrganizerContactPhone ?? "" },
```

**Test**:
```bash
POST /api/events/{id}/cancel
```

**Verify**: Email shows organizer contact info (no `{{}}`)

---

### Fix #4: EventPublishedEventHandler.cs ⭐

**File**: `src/LankaConnect.Application/Events/EventHandlers/EventPublishedEventHandler.cs`

**Location**: Lines 131-145

**Change**:
```csharp
// REVERT organizer parameters to OrganizerContact* names:
{ "OrganizerContactName", @event.OrganizerContactName ?? "Event Organizer" },
{ "OrganizerContactEmail", @event.OrganizerContactEmail ?? "support@lankaconnect.com" },
{ "OrganizerContactPhone", @event.OrganizerContactPhone ?? "" },
```

**Note**: This was changed in Phase 6A.82 to use `OrganizerName`, needs to revert back.

**Test**:
```bash
POST /api/events/{id}/publish
```

---

### Fix #5: EventNotificationEmailJob.cs ⭐

**File**: `src/LankaConnect.Application/Events/BackgroundJobs/EventNotificationEmailJob.cs`

**Location**: Around line 310 (in parameter building)

**Change**:
```csharp
// REVERT organizer parameters to OrganizerContact* names:
{ "OrganizerContactName", @event.OrganizerContactName ?? "Event Organizer" },
{ "OrganizerContactEmail", @event.OrganizerContactEmail ?? "support@lankaconnect.com" },
{ "OrganizerContactPhone", @event.OrganizerContactPhone ?? "" },
```

---

## MEDIUM PRIORITY FIXES (Do Second)

### Fix #6: RegistrationConfirmedEventHandler.cs

**Files**:
- `src/LankaConnect.Application/Events/EventHandlers/RegistrationConfirmedEventHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/AnonymousRegistrationConfirmedEventHandler.cs`

**Change** (add to both):
```csharp
// Add after EventDetailsUrl parameter (around line 150):
if (@event.HasOrganizerContact())
{
    parameters["OrganizerContactName"] = @event.OrganizerContactName ?? "Event Organizer";
    parameters["OrganizerContactEmail"] = @event.OrganizerContactEmail;
    parameters["OrganizerContactPhone"] = @event.OrganizerContactPhone;
}
```

---

### Fix #7: Signup List Handlers (3 files)

**Files**:
- `UserCommittedToSignUpEventHandler.cs`
- `CommitmentUpdatedEventHandler.cs`
- `CommitmentCancelledEventHandler.cs`

**Change** (add to all 3):
```csharp
// After ItemName parameter:
{ "ItemDescription", item.Name }, // Template has BOTH ItemName and ItemDescription

// Add organizer contact:
if (@event.HasOrganizerContact())
{
    parameters["OrganizerContactName"] = @event.OrganizerContactName ?? "Event Organizer";
    parameters["OrganizerContactEmail"] = @event.OrganizerContactEmail;
    parameters["OrganizerContactPhone"] = @event.OrganizerContactPhone;
}
```

---

### Fix #8: RegistrationCancelledEventHandler.cs

**Change**:
```csharp
// Add organizer contact parameters:
if (@event.HasOrganizerContact())
{
    parameters["OrganizerContactName"] = @event.OrganizerContactName ?? "Event Organizer";
    parameters["OrganizerContactEmail"] = @event.OrganizerContactEmail;
    parameters["OrganizerContactPhone"] = @event.OrganizerContactPhone;
}
```

---

### Fix #9: SubscribeToNewsletterCommandHandler.cs

**File**: `src/LankaConnect.Application/Communications/Commands/SubscribeToNewsletter/SubscribeToNewsletterCommandHandler.cs`

**Change**:
```csharp
// Line 215: Add duplicate parameter for template compatibility
var unsubscribeUrl = _emailUrlHelper.BuildNewsletterUnsubscribeUrl(subscriber.Id);
parameters["UnsubscribeUrl"] = unsubscribeUrl;
parameters["UnsubscribeLink"] = unsubscribeUrl; // Template expects both
```

---

## TESTING WORKFLOW

### 1. Local Testing
```bash
# Run unit tests
dotnet test --filter "FullyQualifiedName~[HandlerName]"

# Run all email tests
dotnet test --filter "Category=Email"
```

### 2. Staging Deployment
```bash
# Commit fix
git add [file].cs
git commit -m "fix(phase-6a83): Fix email template parameters for [HandlerName]"

# Push to develop
git push origin develop

# Wait for GitHub Actions deploy-staging.yml (~5 min)
```

### 3. Staging Verification
```bash
# Login to get token
curl -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"niroshhh@gmail.com","password":"12!@qwASzx","rememberMe":true,"ipAddress":"string"}'

# Trigger email scenario (example: event reminder)
curl -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/test/trigger-reminder?eventId={guid}" \
  -H "Authorization: Bearer {token}"

# Check MailHog or staging inbox for email
```

### 4. SQL Verification
```sql
-- Connect to Azure PostgreSQL staging DB
-- Check recent emails for literal {{}} (indicates still broken)
SELECT
    id,
    recipient,
    subject,
    template_name,
    created_at,
    CASE
        WHEN body_html LIKE '%{{%' THEN 'BROKEN - Has literal {{}}'
        ELSE 'OK'
    END as status
FROM communications.email_messages
WHERE template_name = 'template-event-reminder'
  AND created_at > NOW() - INTERVAL '1 hour'
ORDER BY created_at DESC;

-- Check specific parameters rendered
SELECT
    id,
    recipient,
    parameters::json->>'OrganizerContactName' as organizer_name,
    parameters::json->>'TicketCode' as ticket_code,
    created_at
FROM communications.email_messages
WHERE template_name = 'template-event-reminder'
  AND created_at > NOW() - INTERVAL '1 hour';
```

### 5. Container Logs Check
```bash
# Check Azure Container Apps logs for errors
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group rg-lankaconnect-staging \
  --follow

# Look for email-related errors:
# - "Failed to send email"
# - "Email template rendering failed"
# - Exception stack traces
```

---

## CHECKLIST (Per Handler Fix)

- [ ] Code changes made
- [ ] Local unit tests pass
- [ ] Committed with descriptive message: `fix(phase-6a83): Fix email template parameters for [HandlerName]`
- [ ] Pushed to develop branch
- [ ] GitHub Actions deploy-staging.yml succeeded
- [ ] Triggered email in staging environment
- [ ] Checked email in MailHog/inbox - NO `{{}}` literal parameters
- [ ] Verified expected data renders correctly
- [ ] SQL query shows parameters sent correctly
- [ ] Container logs show no errors
- [ ] Updated PROGRESS_TRACKER.md

---

## COMMON ISSUES

### Issue: "HasOrganizerContact() method not found"
**Solution**: Method is extension method, ensure `using LankaConnect.Domain.Events.Extensions;` at top of file.

### Issue: "TicketRepository not available in EventReminderJob"
**Solution**: Add to constructor:
```csharp
private readonly ITicketRepository _ticketRepository;

public EventReminderJob(
    // ... existing params
    ITicketRepository ticketRepository)
{
    _ticketRepository = ticketRepository;
}
```

### Issue: Email still shows `{{}}` after fix
**Solutions**:
1. Check parameter name EXACT match (case-sensitive)
2. Verify template in database has that parameter name
3. Check staging DB template != production template
4. Clear email queue and retry

### Issue: "Ticket not found" in EventReminderJob
**Solution**: Only add ticket parameters if ticket exists:
```csharp
if (registration.TicketId.HasValue)
{
    var ticket = await _ticketRepository.GetByIdAsync(registration.TicketId.Value);
    if (ticket != null)  // Check not null!
    {
        parameters["TicketCode"] = ticket.TicketCode;
        // ...
    }
}
```

---

## REFERENCE

### Parameter Name Mapping

| Template Expects | Handler Should Send | Notes |
|-----------------|---------------------|-------|
| OrganizerContactName | OrganizerContactName | Old name, kept for compatibility |
| OrganizerContactEmail | OrganizerContactEmail | Old name, kept for compatibility |
| OrganizerContactPhone | OrganizerContactPhone | Old name, kept for compatibility |
| TicketCode | TicketCode | Must retrieve from TicketRepository |
| TicketExpiryDate | TicketExpiryDate | Usually event.EndDate + 1 day |
| AmountPaid | AmountPaid | Some templates have BOTH AmountPaid and TotalAmount |
| ItemDescription | ItemDescription | Some templates have BOTH ItemDescription and ItemName |
| UnsubscribeLink | UnsubscribeLink | Some templates have BOTH UnsubscribeLink and UnsubscribeUrl |

### Useful Commands

```bash
# Find all handlers that send email
grep -r "SendTemplatedEmailAsync" src/LankaConnect.Application/

# Find all usages of specific template
grep -r "EventTemplateNames.EventReminder" src/

# Check what parameters a handler sends
grep -A 20 "var parameters = new Dictionary" [handler-file].cs
```

---

## ROLLOUT SCHEDULE

### Week 1 (HIGH Priority)
- **Day 1**: Fix #1 (EventReminderJob)
- **Day 2**: Fix #2 (PaymentCompletedEventHandler)
- **Day 3**: Fix #3, #4, #5 (Cancellation, Published, Notification)
- **Day 4**: Test all HIGH priority fixes in staging
- **Day 5**: Deploy to production, monitor

### Week 2 (MEDIUM Priority)
- **Day 1-2**: Fix #6, #7 (Registration, Signup handlers)
- **Day 3**: Fix #8, #9 (Cancellation, Newsletter)
- **Day 4-5**: Test, deploy to production

---

## SUCCESS METRICS

**Immediate** (after each fix deployed):
- [ ] Zero literal `{{}}` in emails for that template
- [ ] All expected data renders correctly
- [ ] No errors in Azure logs

**Overall** (after all fixes):
- [ ] Zero user reports of broken emails
- [ ] All 15 templates verified working
- [ ] SQL query shows zero emails with `{{` in body_html

---

**Last Updated**: 2026-01-26
**Related Documents**:
- [PHASE_6A83_ROOT_CAUSE_ANALYSIS.md](./PHASE_6A83_ROOT_CAUSE_ANALYSIS.md) - Full RCA
- [TEMPLATE_PARAMETER_ANALYSIS.md](./TEMPLATE_PARAMETER_ANALYSIS.md) - Template breakdown
- [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md) - Track implementation progress
