# Root Cause Analysis: Email Template Parameter Mismatch
## Phase 6A.83 - Production Email Rendering Failures

**Date**: 2026-01-26
**Classification**: Backend API - Email Template System
**Severity**: HIGH - Affects all production emails
**Status**: Analysis Complete - Fix Plan Ready

---

## EXECUTIVE SUMMARY

Production emails are displaying literal Handlebars parameters (e.g., `{{OrganizerContactName}}`, `{{TicketCode}}`) instead of actual values because of incomplete parameter name refactoring between email handlers and database-stored email templates.

**Impact**:
- 15 of 18 email templates affected
- User-reported issues in EventReminder, PaymentCompleted, and EventCancellation emails
- All production users receiving malformed emails since refactoring started

**Root Cause**: Template refactoring created duplicate parameter names in templates (both old and new), but handlers were only partially updated to use new names, creating a mismatch.

---

## 1. ROOT CAUSE ANALYSIS

### 1.1 Primary Root Cause

**Incomplete Parameter Name Refactoring Between Handlers and Templates**

During Phase 6A (early iterations), email templates were refactored to use "cleaner" parameter names:
- `OrganizerContactName` → `OrganizerName`
- `OrganizerContactEmail` → `OrganizerEmail`
- `OrganizerContactPhone` → `OrganizerPhone`
- `ItemDescription` → `ItemName`

**Critical Mistake**: The OLD parameter names were NEVER removed from database templates. Instead, templates now have BOTH old and new names:

```handlebars
<!-- Template in Database -->
<p>Contact: {{OrganizerContactName}}</p>  <!-- OLD name -->
<p>Email: {{OrganizerEmail}}</p>          <!-- NEW name -->
```

When handlers send ONLY one set of names, the other set appears literally:

**Example 1 - EventReminderJob (sends NEW names)**:
```csharp
// Handler sends:
{ "OrganizerName", "John Smith" }
{ "OrganizerEmail", "john@example.com" }

// Template expects BOTH:
{{OrganizerContactName}}  ❌ Shows literally (not sent)
{{OrganizerEmail}}        ✅ Renders correctly
```

**Example 2 - PaymentCompletedEventHandler (missing parameters)**:
```csharp
// Handler sends:
{ "TotalAmount", "$50.00" }

// Template expects:
{{AmountPaid}}     ❌ Shows literally (not sent)
{{TotalAmount}}    ✅ Renders correctly
{{TicketCode}}     ❌ Shows literally (not sent)
```

### 1.2 Contributing Factors

1. **No Template-Handler Contract Validation**
   - No automated checks to verify handler parameters match template requirements
   - Templates stored in database, handlers in C# code - easy to diverge

2. **Incomplete Refactoring Rollout**
   - Phase 6A.39-6A.82 gradually updated handlers to new parameter names
   - Templates kept old names for "backward compatibility"
   - No cleanup phase to remove old parameter names

3. **Lack of Integration Tests**
   - Unit tests mock email service, never render actual templates
   - Integration tests don't verify rendered email content
   - No end-to-end tests catching literal `{{}}` in emails

4. **Database Templates Modified Independently**
   - Templates updated via SQL migrations
   - Handlers updated via C# code
   - No single source of truth for parameter contracts

5. **Missing Documentation**
   - No parameter naming conventions documented
   - No template parameter registry
   - Handlers don't reference which template they use

### 1.3 Why This Wasn't Caught Earlier

1. **Unit Tests Pass**: All tests mock `IEmailTemplateService`, never rendering real templates
2. **Staging Environment**: Templates in staging DB may differ from production DB
3. **Manual Testing Gap**: QA likely tested "email sent successfully" but didn't inspect rendered HTML
4. **Gradual Degradation**: Issue appeared incrementally as handlers were refactored
5. **Template Complexity**: 18 templates × 10-20 parameters each = 300+ parameter mappings to verify

---

## 2. EVIDENCE

### 2.1 User Reports

User provided screenshots showing literal parameters in production emails:
- `{{OrganizerContactName}}` in event reminder emails
- `{{TicketCode}}` in payment confirmation emails
- `{{OrganizerContactPhone}}` in event cancellation emails

### 2.2 Database Analysis

Extracted template parameters from production database (`template_parameters.json`):

**template-event-reminder** (17 parameters):
```json
[
  "OrganizerContactEmail",     // OLD name
  "OrganizerContactName",      // OLD name
  "OrganizerContactPhone",     // OLD name
  "TicketCode",
  "TicketExpiryDate",
  ...
]
```

**EventReminderJob.cs** (handler sends):
```csharp
parameters["OrganizerName"] = ...;        // NEW name ❌
parameters["OrganizerEmail"] = ...;       // NEW name ❌
parameters["OrganizerPhone"] = ...;       // NEW name ❌
// Missing: TicketCode, TicketExpiryDate  ❌
```

**Result**: Template shows `{{OrganizerContactName}}` literally.

### 2.3 Code Analysis

15 handlers with mismatches identified in `TEMPLATE_PARAMETER_ANALYSIS.md`:

**HIGH SEVERITY** (User-facing, frequent usage):
1. EventReminderJob - 5 parameters wrong
2. PaymentCompletedEventHandler - 6 parameters missing
3. EventCancellationEmailJob - 3 parameters wrong
4. EventPublishedEventHandler - 3 parameters wrong
5. EventNotificationEmailJob - 3 parameters wrong

**MEDIUM SEVERITY** (Less frequent):
6. RegistrationConfirmedEventHandler - 3 parameters missing
7. Signup handlers (3 files) - 4 parameters missing each
8. RegistrationCancelledEventHandler - 3 parameters missing

---

## 3. IMPACT ASSESSMENT

### 3.1 User Impact

**Affected Users**: ALL users receiving ANY event-related emails
- Event attendees (reminders, confirmations)
- Event organizers (approval, publication notifications)
- Newsletter subscribers (partially fixed in 6A.83 Part 2)

**User Experience**:
- Emails look unprofessional/broken
- Critical information missing (ticket codes, organizer contacts)
- User confusion/distrust in platform

### 3.2 Business Impact

**Trust/Reputation**:
- Users may perceive platform as low-quality or buggy
- Organizers may lose confidence in event management features
- Potential churn from paid event registrations

**Operational**:
- Support tickets from confused users
- Manual intervention needed (resending tickets, providing organizer contact)
- Lost revenue if users abandon paid registrations

### 3.3 Technical Debt

- 300+ parameter mappings to audit/fix
- 15 handler files to update
- No automated testing to prevent recurrence
- Maintenance burden from duplicate parameter names

---

## 4. TIMELINE

| Date | Event |
|------|-------|
| Phase 6A.39-6A.50 | Template refactoring begins - new parameter names introduced |
| Phase 6A.51-6A.70 | Handlers gradually updated to new names, old names kept in templates |
| Phase 6A.71-6A.82 | Additional handler updates, organizer contact parameters added |
| Phase 6A.83 Part 1 | Fixed 3 handlers (CommitmentUpdated, CommitmentCancelled, RegistrationCancelled) |
| Phase 6A.83 Part 2 | Fixed NewsletterEmailJob |
| Phase 6A.83 Part 3 | Fixed SubscribeToNewsletter, ResetPassword handlers |
| 2026-01-25 | User reports seeing `{{OrganizerContactName}}` in production emails |
| 2026-01-25 | Database extraction confirms 15 handlers with mismatches |
| 2026-01-26 | RCA created, fix plan ready |

---

## 5. FIX STRATEGY

### 5.1 Strategic Options

#### Option A: Clean Up Templates (Remove Duplicate Parameters)
**Approach**: Remove old parameter names from templates, keep only new names.

**Pros**:
- Single source of truth for parameter names
- Cleaner template code
- Future-proof

**Cons**:
- Requires modifying 15+ database templates
- Risk of breaking currently-working handlers
- Requires careful SQL migration
- Harder to rollback if issues found

**Risk Level**: MEDIUM-HIGH

---

#### Option B: Update Handlers to Send All Parameters ✅ RECOMMENDED
**Approach**: Update handlers to send BOTH old and new parameter names.

**Pros**:
- No template changes needed (safer)
- Backward compatible with any template version
- Easy to implement incrementally
- Easy to rollback (just revert handler changes)
- Can verify each fix in staging before production

**Cons**:
- Temporary duplication in handler code
- Technical debt (duplicate parameters)
- Slightly larger email payloads

**Risk Level**: LOW

---

### 5.2 Selected Strategy: **Option B**

**Rationale**:
1. **Safety First**: Templates are in production database - harder to test/rollback
2. **Incremental Rollout**: Fix handlers one-by-one, verify each in staging
3. **Backward Compatibility**: Works with any template version (old/new parameter names)
4. **User Impact**: Fastest path to fix user-facing issues
5. **Cleanup Later**: Once all handlers fixed, can clean up templates in Phase 6B

**Implementation Pattern**:
```csharp
// Send BOTH old and new parameter names
parameters["OrganizerName"] = organizerName;           // New name
parameters["OrganizerContactName"] = organizerName;    // Old name (template expects this)
parameters["OrganizerEmail"] = organizerEmail;         // New name
parameters["OrganizerContactEmail"] = organizerEmail;  // Old name (template expects this)
```

---

## 6. PRIORITIZED FIX PLAN

### 6.1 HIGH PRIORITY (User-Reported, Frequent Usage)

#### Fix #1: EventReminderJob.cs
**File**: `src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs`
**Lines**: 218-233, 405-420
**Frequency**: Daily (24hr, 1hr before event)
**User Impact**: HIGH - all event attendees

**Current Issues**:
- Sends `OrganizerName` instead of `OrganizerContactName`
- Sends `OrganizerEmail` instead of `OrganizerContactEmail`
- Sends `OrganizerPhone` instead of `OrganizerContactPhone`
- Missing `TicketCode`, `TicketExpiryDate`

**Fix**:
```csharp
// REVERT: Use OrganizerContact* parameters (lines 222-228)
parameters["OrganizerContactName"] = @event.OrganizerContactName ?? "Event Organizer";
parameters["OrganizerContactEmail"] = @event.OrganizerContactEmail;
parameters["OrganizerContactPhone"] = @event.OrganizerContactPhone;

// ADD: Ticket parameters (if paid event with ticket)
if (registration.TicketId.HasValue)
{
    var ticket = await _ticketRepository.GetByIdAsync(registration.TicketId.Value);
    if (ticket != null)
    {
        parameters["TicketCode"] = ticket.TicketCode;
        parameters["TicketExpiryDate"] = ticket.ExpiryDate.ToString("MMMM dd, yyyy");
    }
}
```

**Testing**:
```bash
# 1. Trigger reminder job in staging
POST /api/test/trigger-event-reminder?eventId={guid}

# 2. Check MailHog for rendered email
# 3. Verify NO literal {{}} parameters appear
# 4. Verify organizer contact displays correctly
```

---

#### Fix #2: PaymentCompletedEventHandler.cs ⚠️ CRITICAL
**File**: `src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs`
**Lines**: 169-254
**Frequency**: Every paid registration
**User Impact**: CRITICAL - affects revenue/conversions

**Current Issues**:
- Missing `AmountPaid` (template has both `AmountPaid` and `TotalAmount`)
- Missing `TicketCode` (shows literally in email!)
- Missing `TicketExpiryDate`
- Missing `OrganizerContactName`, `OrganizerContactEmail`, `OrganizerContactPhone`

**Fix**:
```csharp
// Line 181: ADD AmountPaid (duplicate of TotalAmount for template compatibility)
parameters["AmountPaid"] = domainEvent.AmountPaid.ToString("C", CultureInfo.GetCultureInfo("en-US"));
parameters["TotalAmount"] = domainEvent.AmountPaid.ToString("C", CultureInfo.GetCultureInfo("en-US"));

// Lines 223-225: Already has TicketCode, TicketExpiryDate ✅

// ADD: Organizer contact parameters (after line 225)
if (@event.HasOrganizerContact())
{
    parameters["OrganizerContactName"] = @event.OrganizerContactName ?? "Event Organizer";
    parameters["OrganizerContactEmail"] = @event.OrganizerContactEmail;
    parameters["OrganizerContactPhone"] = @event.OrganizerContactPhone;
}
```

**NOTE**: This handler already sends `TicketCode` and `TicketExpiryDate` (lines 223-225), just missing organizer and AmountPaid.

**Testing**:
```bash
# 1. Register for paid event in staging
POST /api/events/{id}/register
POST /api/payments/complete

# 2. Check email for:
#    - TicketCode renders (not {{}})
#    - AmountPaid displays
#    - Organizer contact displays
```

---

#### Fix #3: EventCancellationEmailJob.cs
**File**: `src/LankaConnect.Application/Events/BackgroundJobs/EventCancellationEmailJob.cs`
**Lines**: 208-219
**Frequency**: When organizer cancels event
**User Impact**: HIGH - critical communication

**Current Issues**:
- Sends `OrganizerEmail` but template ALSO expects `OrganizerContactEmail`
- Missing `OrganizerContactName`, `OrganizerContactPhone`

**Fix**:
```csharp
// Line 219: ADD after OrganizerEmail
parameters["OrganizerEmail"] = @event.OrganizerContactEmail ?? "support@lankaconnect.com";
parameters["OrganizerContactEmail"] = @event.OrganizerContactEmail ?? "support@lankaconnect.com"; // Duplicate for template
parameters["OrganizerContactName"] = @event.OrganizerContactName ?? "Event Organizer";
parameters["OrganizerContactPhone"] = @event.OrganizerContactPhone ?? "";
```

**Testing**:
```bash
# Trigger cancellation in staging
POST /api/events/{id}/cancel

# Verify email shows organizer contact correctly
```

---

#### Fix #4: EventPublishedEventHandler.cs
**File**: `src/LankaConnect.Application/Events/EventHandlers/EventPublishedEventHandler.cs`
**Lines**: 131-145
**Frequency**: Every new published event
**User Impact**: HIGH - first impression for newsletter subscribers

**Current Issues**:
- Sends `OrganizerName`, `OrganizerEmail`, `OrganizerPhone`
- Template expects `OrganizerContactName`, `OrganizerContactEmail`, `OrganizerContactPhone`

**Fix** (Phase 6A.82 already partially fixed, needs revert):
```csharp
// REVERT to OrganizerContact* parameter names
parameters["OrganizerContactName"] = @event.OrganizerContactName ?? "Event Organizer";
parameters["OrganizerContactEmail"] = @event.OrganizerContactEmail;
parameters["OrganizerContactPhone"] = @event.OrganizerContactPhone;
```

**Testing**:
```bash
# Publish event in staging
POST /api/events/{id}/publish

# Check newsletter email rendering
```

---

#### Fix #5: EventNotificationEmailJob.cs
**File**: `src/LankaConnect.Application/Events/BackgroundJobs/EventNotificationEmailJob.cs`
**Lines**: 295-351
**Frequency**: When event details change (updated event notifications)
**User Impact**: HIGH - keeps attendees informed

**Current Issues**:
- Same as Fix #4 (sends `OrganizerName` instead of `OrganizerContactName`)

**Fix**:
```csharp
// Line ~310: REVERT to OrganizerContact* names
parameters["OrganizerContactName"] = @event.OrganizerContactName ?? "Event Organizer";
parameters["OrganizerContactEmail"] = @event.OrganizerContactEmail;
parameters["OrganizerContactPhone"] = @event.OrganizerContactPhone;
```

---

### 6.2 MEDIUM PRIORITY

#### Fix #6: RegistrationConfirmedEventHandler.cs + AnonymousRegistrationConfirmedEventHandler.cs
**Files**:
- `src/LankaConnect.Application/Events/EventHandlers/RegistrationConfirmedEventHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/AnonymousRegistrationConfirmedEventHandler.cs`

**Current Issues**:
- Missing `OrganizerContactName`, `OrganizerContactEmail`, `OrganizerContactPhone`

**Fix** (add to both handlers):
```csharp
// After line ~150
if (@event.HasOrganizerContact())
{
    parameters["OrganizerContactName"] = @event.OrganizerContactName ?? "Event Organizer";
    parameters["OrganizerContactEmail"] = @event.OrganizerContactEmail;
    parameters["OrganizerContactPhone"] = @event.OrganizerContactPhone;
}
```

---

#### Fix #7: Signup List Handlers (3 files)
**Files**:
- `UserCommittedToSignUpEventHandler.cs`
- `CommitmentUpdatedEventHandler.cs`
- `CommitmentCancelledEventHandler.cs`

**Current Issues**:
- Send `ItemName` but templates have BOTH `ItemName` AND `ItemDescription`
- Missing organizer contact parameters

**Fix** (all 3 handlers):
```csharp
// ADD duplicate parameter for template compatibility
parameters["ItemName"] = item.Name;
parameters["ItemDescription"] = item.Name; // Template has both!

// ADD organizer contact
if (@event.HasOrganizerContact())
{
    parameters["OrganizerContactName"] = @event.OrganizerContactName ?? "Event Organizer";
    parameters["OrganizerContactEmail"] = @event.OrganizerContactEmail;
    parameters["OrganizerContactPhone"] = @event.OrganizerContactPhone;
}
```

---

#### Fix #8: RegistrationCancelledEventHandler.cs
**File**: `src/LankaConnect.Application/Events/EventHandlers/RegistrationCancelledEventHandler.cs`

**Current Issues**:
- Missing `OrganizerContactName`, `OrganizerContactEmail`, `OrganizerContactPhone`

**Fix**:
```csharp
// Add organizer contact parameters
if (@event.HasOrganizerContact())
{
    parameters["OrganizerContactName"] = @event.OrganizerContactName ?? "Event Organizer";
    parameters["OrganizerContactEmail"] = @event.OrganizerContactEmail;
    parameters["OrganizerContactPhone"] = @event.OrganizerContactPhone;
}
```

---

#### Fix #9: SubscribeToNewsletterCommandHandler.cs
**File**: `src/LankaConnect.Application/Communications/Commands/SubscribeToNewsletter/SubscribeToNewsletterCommandHandler.cs`

**Current Issues**:
- Sends `UnsubscribeUrl` but template has BOTH `UnsubscribeUrl` AND `UnsubscribeLink`

**Fix**:
```csharp
// Send both parameter names
var unsubscribeUrl = _emailUrlHelper.BuildNewsletterUnsubscribeUrl(subscriber.Id);
parameters["UnsubscribeUrl"] = unsubscribeUrl;
parameters["UnsubscribeLink"] = unsubscribeUrl; // Duplicate for template
```

---

### 6.3 LOW PRIORITY (Need Verification)

#### Fix #10: MemberVerificationRequestedEventHandler.cs
**Verification Needed**: Check if sends `ExpirationHours` or `TokenExpiry`

#### Fix #11: EventApprovedEventHandler.cs
**Verification Needed**: Verify all 9 template parameters sent correctly

#### Fix #12: Password/Welcome Handlers
**Verification Needed**: Verify parameter naming matches templates

---

## 7. TESTING STRATEGY

### 7.1 Per-Handler Testing (Staging)

**For each fixed handler**:

```bash
# 1. Deploy handler fix to Azure staging
git add [handler-file].cs
git commit -m "fix(phase-6a83): Fix email template parameters for [HandlerName]"
git push origin develop

# 2. Wait for GitHub Actions deploy-staging.yml to complete (~5 min)

# 3. Trigger the specific email scenario
# Example for EventReminderJob:
curl -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/test/trigger-event-reminder?eventId={guid}" \
  -H "Authorization: Bearer {token}"

# 4. Check email in MailHog or staging inbox
# 5. Verify NO literal {{}} parameters
# 6. Verify all expected data renders correctly
```

### 7.2 SQL Verification Queries

```sql
-- Check email sends after fix deployed
SELECT
    id,
    recipient,
    subject,
    template_name,
    parameters::json->>'OrganizerContactName' as organizer_name,
    parameters::json->>'TicketCode' as ticket_code,
    status,
    created_at
FROM communications.email_messages
WHERE template_name = 'template-event-reminder'
  AND created_at > NOW() - INTERVAL '1 hour'
ORDER BY created_at DESC
LIMIT 10;

-- Check for literal {{}} in sent emails (indicates bug still present)
SELECT
    id,
    recipient,
    subject,
    template_name,
    created_at
FROM communications.email_messages
WHERE body_html LIKE '%{{%'
  AND created_at > NOW() - INTERVAL '1 hour';
```

### 7.3 Integration Test Additions (Future Prevention)

```csharp
[Fact]
public async Task PaymentCompletedEventHandler_ShouldSendEmailWithoutLiteralParameters()
{
    // Arrange
    var @event = EventTestDataBuilder.CreatePaidEvent();
    var registration = RegistrationTestDataBuilder.CreateRegistration(@event.Id);
    var domainEvent = new PaymentCompletedEvent(registration.Id, 50.00m, DateTime.UtcNow);

    // Act
    await _handler.Handle(domainEvent, CancellationToken.None);

    // Assert
    var sentEmail = _emailCapture.LastSentEmail;
    sentEmail.BodyHtml.Should().NotContain("{{"); // No literal Handlebars
    sentEmail.BodyHtml.Should().Contain("John Smith"); // OrganizerContactName rendered
    sentEmail.BodyHtml.Should().Contain("ABC123"); // TicketCode rendered
}
```

### 7.4 Automated Template Contract Validation (Future)

```csharp
// Tool to extract template parameters and compare with handler
public class TemplateParameterValidator
{
    public async Task<ValidationResult> ValidateHandler(string templateName, string handlerFile)
    {
        // 1. Query database for template parameters
        var templateParams = await _db.EmailTemplates
            .Where(t => t.Name == templateName)
            .Select(t => ExtractHandlebarsParameters(t.BodyHtml))
            .FirstAsync();

        // 2. Parse handler C# code for parameters sent
        var handlerParams = ParseHandlerParameters(handlerFile);

        // 3. Compare
        var missing = templateParams.Except(handlerParams).ToList();
        var extra = handlerParams.Except(templateParams).ToList();

        return new ValidationResult
        {
            TemplateName = templateName,
            HandlerFile = handlerFile,
            MissingParameters = missing,  // Handler doesn't send, template expects
            ExtraParameters = extra        // Handler sends, template doesn't use
        };
    }
}
```

---

## 8. PREVENTION RECOMMENDATIONS

### 8.1 Process Improvements

1. **Template-Handler Contract Registry**
   - Create `/docs/EMAIL_TEMPLATE_CONTRACTS.md`
   - Document each template with required parameters
   - Handlers reference this before implementation

2. **Pre-Deployment Validation**
   - Add GitHub Actions workflow: `validate-email-templates.yml`
   - Runs `TemplateParameterValidator` on every PR
   - Fails CI if handler parameters don't match template

3. **Integration Testing Requirement**
   - ALL email handlers MUST have integration test
   - Test MUST verify rendered HTML (not mocked service)
   - Use MailHog in CI pipeline to capture/validate emails

4. **Template Versioning**
   - Add `version` column to `communications.email_templates`
   - Handlers specify expected template version
   - Migration required if template parameters change

### 8.2 Technical Improvements

1. **Shared Parameter Contracts (C# Records)**
   ```csharp
   // Define template parameters as strongly-typed contracts
   public record EventReminderEmailParams(
       string UserName,
       string EventTitle,
       string EventStartDate,
       string EventStartTime,
       string OrganizerContactName,
       string OrganizerContactEmail,
       string? OrganizerContactPhone,
       string? TicketCode,
       string? TicketExpiryDate
   );

   // Handler uses contract
   var emailParams = new EventReminderEmailParams(
       UserName: registration.UserName,
       EventTitle: @event.Title,
       // ...compiler enforces all parameters provided
   );
   ```

2. **Template Parameter Extraction Tool**
   - CLI tool: `dotnet run --project EmailTools -- extract-params`
   - Scans all templates in database
   - Generates `/docs/template-parameters.json`
   - Run before each release

3. **Automated Template Cleanup**
   - SQL migration to remove duplicate parameter names
   - Run AFTER all handlers fixed (Phase 6B)
   - Keep only "canonical" parameter names

### 8.3 Documentation Standards

1. **Handler Comment Requirement**
   ```csharp
   /// <summary>
   /// Sends event reminder email to registered attendees.
   /// </summary>
   /// <remarks>
   /// Email Template: template-event-reminder
   /// Template Version: 2.0
   /// Required Parameters: UserName, EventTitle, OrganizerContactName, TicketCode
   /// Optional Parameters: OrganizerContactPhone
   /// </remarks>
   public class EventReminderJob : INotificationHandler<EventReminderDue>
   ```

2. **Template Migration Documentation**
   ```sql
   -- Migration: 20260126_UpdateEventReminderTemplate.sql
   -- Changes:
   --   - Added parameter: TicketExpiryDate
   --   - Removed parameter: OldOrganizerName (replaced by OrganizerContactName)
   -- Affected Handlers:
   --   - EventReminderJob.cs (update required)
   ```

### 8.4 Monitoring & Alerting

1. **Production Email Monitoring**
   ```csharp
   // Log warning if email contains literal {{}}
   if (renderedHtml.Contains("{{"))
   {
       _logger.LogWarning(
           "Email template rendering incomplete - literal parameters detected. " +
           "Template: {Template}, Recipient: {Recipient}",
           templateName, recipient);

       // Send alert to Slack/PagerDuty
       await _alertService.NotifyAsync("Email rendering failure detected");
   }
   ```

2. **Weekly Email Audit Report**
   - Query `communications.email_messages` for emails with `{{` in body
   - Generate report of affected templates
   - Email to engineering team

---

## 9. ROLLOUT PLAN

### 9.1 Phase 6A.83 Part 4 - HIGH Priority Fixes (Week 1)

**Day 1-2**:
- Fix #1: EventReminderJob
- Fix #2: PaymentCompletedEventHandler
- Deploy to staging, test thoroughly

**Day 3-4**:
- Fix #3: EventCancellationEmailJob
- Fix #4: EventPublishedEventHandler
- Fix #5: EventNotificationEmailJob
- Deploy to staging, test

**Day 5**:
- Deploy HIGH priority fixes to production
- Monitor logs for errors
- Verify user reports resolved

### 9.2 Phase 6A.84 - MEDIUM Priority Fixes (Week 2)

**Day 1-3**:
- Fix #6: RegistrationConfirmedEventHandler (both)
- Fix #7: Signup handlers (3 files)
- Fix #8: RegistrationCancelledEventHandler
- Fix #9: SubscribeToNewsletterCommandHandler

**Day 4-5**:
- Deploy to staging, comprehensive testing
- Deploy to production

### 9.3 Phase 6A.85 - Verification & Testing (Week 3)

**Day 1-2**:
- Fix #10-12: Verify low-priority handlers
- Create integration tests for all handlers

**Day 3-5**:
- Implement `TemplateParameterValidator` tool
- Add GitHub Actions validation workflow
- Create `/docs/EMAIL_TEMPLATE_CONTRACTS.md`

### 9.4 Phase 6B - Template Cleanup (Month 2)

**After all handlers verified working**:
1. Create SQL migration to remove duplicate parameters from templates
2. Test migration in staging (verify all emails still render)
3. Deploy to production
4. Remove duplicate parameters from handler code

---

## 10. SUCCESS CRITERIA

### 10.1 Immediate Success (After Phase 6A.83 Part 4)

- [ ] Zero user reports of literal `{{}}` in emails
- [ ] All HIGH priority handlers tested in staging
- [ ] All HIGH priority handlers deployed to production
- [ ] Production logs show no email rendering warnings
- [ ] Manual testing confirms organizer contact, ticket codes render correctly

### 10.2 Short-Term Success (After Phase 6A.85)

- [ ] All 15 affected handlers fixed and tested
- [ ] Integration tests added for all email handlers
- [ ] `TemplateParameterValidator` tool running in CI
- [ ] Email template contract documentation created
- [ ] Zero literal `{{}}` parameters in production emails (verified via SQL query)

### 10.3 Long-Term Success (After Phase 6B)

- [ ] Template duplicate parameters removed (cleanup migration deployed)
- [ ] Email template versioning system implemented
- [ ] Automated email auditing in place (weekly reports)
- [ ] No email template issues in 30 days
- [ ] Pre-deployment validation prevents future parameter mismatches

---

## 11. LESSONS LEARNED

1. **Template-Code Coupling**: Database templates and C# handlers are tightly coupled but have no contract enforcement
2. **Integration Testing Critical**: Unit tests with mocks don't catch template rendering issues
3. **Gradual Refactoring Risk**: Incremental parameter name changes created inconsistent state
4. **Documentation Gap**: No single source of truth for template parameter contracts
5. **Monitoring Blind Spot**: No alerts for malformed emails in production

---

## 12. APPENDIX

### 12.1 Affected Files Summary

**Handlers Requiring Fixes** (15 files):
1. EventReminderJob.cs
2. PaymentCompletedEventHandler.cs
3. EventCancellationEmailJob.cs
4. EventPublishedEventHandler.cs
5. EventNotificationEmailJob.cs
6. RegistrationConfirmedEventHandler.cs
7. AnonymousRegistrationConfirmedEventHandler.cs
8. UserCommittedToSignUpEventHandler.cs
9. CommitmentUpdatedEventHandler.cs
10. CommitmentCancelledEventHandler.cs
11. RegistrationCancelledEventHandler.cs
12. SubscribeToNewsletterCommandHandler.cs
13. MemberVerificationRequestedEventHandler.cs (verify)
14. EventApprovedEventHandler.cs (verify)
15. Welcome/Password handlers (verify)

**Templates Affected** (15 templates):
- template-event-reminder
- template-paid-event-registration-confirmation-with-ticket
- template-event-cancellation-notifications
- template-new-event-publication
- template-event-details-publication
- template-free-event-registration-confirmation
- template-signup-list-commitment-confirmation
- template-signup-list-commitment-update
- template-signup-list-commitment-cancellation
- template-event-registration-cancellation
- template-newsletter-subscription-confirmation
- template-membership-email-verification
- template-event-approval
- template-welcome
- template-password-reset

### 12.2 Reference Documents

- [TEMPLATE_PARAMETER_ANALYSIS.md](./TEMPLATE_PARAMETER_ANALYSIS.md) - Detailed template-by-template breakdown
- [template_parameters.json](../scripts/template_parameters.json) - Database extraction results
- [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md) - Phase 6A.83 progress tracking

---

**RCA Prepared By**: Architecture Agent
**Review Status**: Ready for Implementation
**Next Steps**: Begin Phase 6A.83 Part 4 - HIGH Priority Handler Fixes
