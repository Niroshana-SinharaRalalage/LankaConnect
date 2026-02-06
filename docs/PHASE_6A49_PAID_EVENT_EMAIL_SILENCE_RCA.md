# Phase 6A.49: Paid Event Email Complete Failure - Root Cause Analysis

**Date**: 2025-12-25
**Severity**: P0 - CRITICAL
**Issue**: Paid event registration emails completely stopped sending between Dec 22-23
**Status**: 🔍 INVESTIGATION IN PROGRESS

---

## Executive Summary

Paid event registration confirmation emails (with ticket PDFs) completely stopped being sent between December 22-23, 2025. Free event emails ARE working after Phase 6A.41 fixes. This document provides a systematic root cause analysis with ranked hypotheses and diagnostic procedures.

**Key Facts**:
- ✅ Azure Communication Services verified working (test email sent successfully)
- ✅ Stripe webhooks verified working (screenshot of successful payments)
- ✅ Free event emails working (registration-confirmation template)
- ✅ Event reminder emails working (for previously registered events)
- ✅ Database template (ticket-confirmation) verified correct format
- ✅ Phase 6A.43 code deployed (PaymentCompletedEventHandler uses database templates)
- ❌ Paid event emails NOT working
- ❌ Resend email button fails with "ValidationError: Failed to render email template"

**Timeline**:
- **Dec 22**: Emails were sending (but with unrendered variables like `{{EventStartDate}}`)
- **Dec 23 morning**: Emails STOPPED completely for paid events
- **Dec 23**: Phase 6A.43 deployed (template rendering fix)
- **Dec 25**: Investigation initiated

---

## Section 1: Root Cause Hypotheses (Ranked by Likelihood)

### Hypothesis 1: Template Rendering Exception in RenderTemplateContent (HIGHEST PROBABILITY: 85%)

**Theory**: The `RenderTemplateContent` method in `AzureEmailService.cs` is throwing an exception when processing the ticket-confirmation template, causing the entire email send to fail silently.

**Evidence Supporting**:
1. **Resend button error**: "Failed to render email template" - direct evidence of template rendering failure
2. **Code location**: Lines 227-280 of `AzureEmailService.cs` - complex conditional parsing logic
3. **Recent changes**: Phase 6A.43 introduced this rendering path for paid events
4. **Fail-silent pattern**: PaymentCompletedEventHandler catches ALL exceptions (line 236-242) and logs but doesn't throw
5. **Nested conditionals**: Template supports `{{#HasEventImage}}...{{/HasEventImage}}` - complex parsing logic that could fail

**Likely Failure Scenarios**:
- **Scenario A**: Nested conditional tags causing parsing errors
  - Template has: `{{#HasTicket}}...{{#HasEventImage}}...{{/HasEventImage}}...{{/HasTicket}}`
  - RenderTemplateContent doesn't handle nested conditionals (lines 234-270 only process top-level)
  - This would cause malformed HTML or infinite loop

- **Scenario B**: Missing required parameter causing null reference
  - Template expects parameter that's not in dictionary
  - `param.Value?.ToString()` (line 276) could throw if parameter is null and has no ToString override
  - Or conditional check `param.Value switch` (lines 237-243) could throw on unexpected type

- **Scenario C**: Template database content corruption
  - Template HTML contains malformed conditional syntax
  - Opening tag without closing tag: `{{#HasTicket}}...` (no `{{/HasTicket}}`)
  - This causes `endIndex = -1` at line 253, breaking loop at line 253

**How to Verify**:
```bash
# Check Azure Container App logs for specific error
az containerapp logs show --name <app-name> --resource-group <rg> \
  --follow --filter "PaymentCompletedEventHandler" --since 2h

# Look for:
# - "Failed to render email template 'ticket-confirmation': <ERROR>"
# - Exception stack traces in PaymentCompletedEventHandler
# - Null reference exceptions in RenderTemplateContent
```

**Database Check**:
```sql
-- Verify template syntax is valid
SELECT
  name,
  LENGTH(html_template) as html_length,
  LENGTH(subject_template) as subject_length,
  html_template LIKE '%{{#%' as has_conditionals,
  html_template LIKE '%{{#HasTicket}}%' as has_ticket_conditional,
  html_template LIKE '%{{/HasTicket}}%' as has_closing_ticket,
  -- Count opening vs closing tags
  (LENGTH(html_template) - LENGTH(REPLACE(html_template, '{{#', ''))) / 3 as opening_tags,
  (LENGTH(html_template) - LENGTH(REPLACE(html_template, '{{/', ''))) / 3 as closing_tags
FROM communications.email_templates
WHERE name = 'ticket-confirmation';

-- If opening_tags != closing_tags, you have unbalanced conditionals
```

**Fix If Confirmed**:
1. Add defensive null checking in RenderTemplateContent
2. Add try-catch around parameter.Value operations
3. Add validation for balanced conditional tags
4. Add detailed logging before/after each rendering step

---

### Hypothesis 2: PaymentCompletedEvent Not Being Raised (PROBABILITY: 75%)

**Theory**: The domain event `PaymentCompletedEvent` is not being raised at all, so `PaymentCompletedEventHandler` never executes.

**Evidence Supporting**:
1. **No logs**: User mentioned no specific PaymentCompletedEventHandler logs appearing
2. **Stripe webhooks working**: Payment succeeds but downstream event not triggered
3. **Phase 6A.34 fix**: `DomainEventDispatcher` was modified - could have introduced regression
4. **Event dispatching**: Domain events use MediatR pipeline - any break in chain stops propagation

**Where Event Should Be Raised**:
Check `StripeWebhookHandler` or payment processing code:
```csharp
// Should have something like:
registration.CompletePayment(paymentIntentId, amountPaid);
// Inside CompletePayment method:
AddDomainEvent(new PaymentCompletedEvent(...));
```

**How to Verify**:
```bash
# Check if PaymentCompletedEventHandler is being invoked at all
az containerapp logs show --name <app-name> --resource-group <rg> \
  --filter "PaymentCompletedEventHandler INVOKED" --since 24h

# Should see:
# "[Phase 6A.24] ✅ PaymentCompletedEventHandler INVOKED - Event {EventId}, Registration {RegistrationId}"
```

**Database Check**:
```sql
-- Check if registrations have payment_completed_at set
SELECT
  id,
  event_id,
  user_id,
  payment_status,
  payment_completed_at,
  stripe_payment_intent_id,
  created_at,
  updated_at
FROM events.event_registrations
WHERE event_id = '9e3722f5-c255-4dcc-b167-afef56bc5592'
  AND created_at >= '2025-12-23'
ORDER BY created_at DESC
LIMIT 5;

-- If payment_completed_at IS NULL but payment_status = 'Completed',
-- then PaymentCompletedEvent was never raised
```

**Fix If Confirmed**:
1. Verify `Registration.CompletePayment()` raises domain event
2. Check DomainEventDispatcher is properly registered in DI
3. Verify MediatR pipeline configuration

---

### Hypothesis 3: Email Template Not Found in Database (PROBABILITY: 60%)

**Theory**: The database query for `ticket-confirmation` template is failing or returning null, despite template existing.

**Evidence Supporting**:
1. **Resend error**: "Failed to render email template" suggests template lookup failed
2. **Case sensitivity**: PostgreSQL is case-sensitive - template name mismatch possible
3. **Recent deployments**: Database migrations could have affected templates table

**How to Verify**:
```sql
-- Exact template lookup as code does
SELECT
  id,
  name,
  is_active,
  created_at,
  updated_at,
  LENGTH(html_template) as html_length,
  subject_template
FROM communications.email_templates
WHERE name = 'ticket-confirmation';  -- Exact match

-- Also try case variations
SELECT name, is_active
FROM communications.email_templates
WHERE LOWER(name) LIKE '%ticket%';
```

**Code Check**:
In `AzureEmailService.cs` line 634:
```csharp
var template = await _emailTemplateRepository.GetByNameAsync(templateName, cancellationToken);
if (template == null)
{
    _logger.LogWarning("Email template '{TemplateName}' not found in database", templateName);
    return Result<RenderedEmailTemplate>.Failure($"Email template '{templateName}' not found");
}
```

**Log Check**:
```bash
# Look for template not found warnings
az containerapp logs show --name <app-name> --resource-group <rg> \
  --filter "Email template 'ticket-confirmation' not found" --since 24h
```

**Fix If Confirmed**:
1. Verify template name is exactly `ticket-confirmation` (no spaces, correct case)
2. Verify `is_active = true`
3. Re-run template creation migration if needed

---

### Hypothesis 4: Contact Email/Phone Null Causing Parameter Issues (PROBABILITY: 50%)

**Theory**: When Contact is null, the parameters dictionary contains empty strings for ContactEmail/ContactPhone, which breaks template rendering.

**Evidence Supporting**:
1. **Code fix deployed**: Lines 139-150 in PaymentCompletedEventHandler set ContactEmail/ContactPhone to `""` when Contact is null
2. **Template expectations**: Template might have `{{#HasContactInfo}}{{ContactEmail}}{{/HasContactInfo}}` which expects non-empty strings
3. **Recent change**: This code was just added in Phase 6A.43 - could have introduced issue

**Code Analysis**:
```csharp
// Line 139-150 in PaymentCompletedEventHandler.cs
if (registration.Contact != null)
{
    parameters["ContactEmail"] = registration.Contact.Email;
    parameters["ContactPhone"] = registration.Contact.PhoneNumber ?? "";
    parameters["HasContactInfo"] = true;
}
else
{
    parameters["ContactEmail"] = "";  // ⚠️ Could cause issues
    parameters["ContactPhone"] = "";  // ⚠️ Could cause issues
    parameters["HasContactInfo"] = false;
}
```

**Template Rendering Logic**:
```csharp
// Line 237-243 - Conditional evaluation
var isTruthy = param.Value switch
{
    bool b => b,
    string s => !string.IsNullOrEmpty(s),  // ⚠️ Empty string = false
    null => false,
    _ => true
};
```

**Potential Issue**: If template has `{{ContactEmail}}` outside of `{{#HasContactInfo}}` conditional, it will render as empty string, which might break HTML structure.

**How to Verify**:
```sql
-- Check if recent registrations have Contact data
SELECT
  id,
  event_id,
  contact_email,
  contact_phone,
  user_id
FROM events.event_registrations
WHERE event_id = '9e3722f5-c255-4dcc-b167-afef56bc5592'
  AND created_at >= '2025-12-23';
```

**Fix If Confirmed**:
1. Remove ContactEmail/ContactPhone from parameters if Contact is null (don't add at all)
2. Ensure template only references ContactEmail inside `{{#HasContactInfo}}` conditional
3. Update template to handle missing Contact gracefully

---

### Hypothesis 5: Ticket Generation Failing (PROBABILITY: 40%)

**Theory**: The `_ticketService.GenerateTicketAsync()` call is failing, which prevents email from being sent.

**Evidence Supporting**:
1. **Code dependency**: Lines 153-183 in PaymentCompletedEventHandler generate ticket BEFORE sending email
2. **Failure point**: If ticket generation fails, parameters won't include HasTicket=true
3. **Template requirement**: Email template might require ticket information

**Code Flow**:
```csharp
// Line 153-156
var ticketResult = await _ticketService.GenerateTicketAsync(
    registration.Id,
    @event.Id,
    cancellationToken);

// Line 159-183: If ticket generation fails
if (ticketResult.IsSuccess)
{
    // Set HasTicket = true, add ticket code
}
else
{
    _logger.LogWarning("Failed to generate ticket: {Error}", ...);
    parameters["HasTicket"] = false;  // ⚠️ Does template handle this?
}
```

**How to Verify**:
```bash
# Check for ticket generation failures
az containerapp logs show --name <app-name> --resource-group <rg> \
  --filter "Failed to generate ticket" --since 24h
```

```sql
-- Check if tickets were created
SELECT
  t.id,
  t.ticket_code,
  t.registration_id,
  t.event_id,
  t.status,
  t.created_at
FROM events.tickets t
JOIN events.event_registrations r ON t.registration_id = r.id
WHERE r.event_id = '9e3722f5-c255-4dcc-b167-afef56bc5592'
  AND r.created_at >= '2025-12-23'
ORDER BY t.created_at DESC;
```

**Fix If Confirmed**:
1. Send email even if ticket generation fails (degrade gracefully)
2. Update template to handle `HasTicket = false` scenario
3. Investigate ticket generation service issues

---

### Hypothesis 6: Email Subject Rendering Failing (PROBABILITY: 35%)

**Theory**: The subject template is failing to render, which causes the entire email send to fail validation.

**Evidence Supporting**:
1. **Subject required**: EmailMessageDto validation (line 288 in AzureEmailService.cs) requires non-empty subject
2. **Subject rendering**: Line 648 renders subject separately: `var subject = RenderTemplateContent(template.SubjectTemplate.Value, parameters);`
3. **Validation failure**: If subject is empty or malformed, validation fails at line 64

**Code Check**:
```csharp
// Line 288 in AzureEmailService.cs
if (string.IsNullOrWhiteSpace(emailMessage.Subject))
    return Result.Failure("Email subject is required");
```

**How to Verify**:
```sql
-- Check subject template
SELECT
  name,
  subject_template,
  LENGTH(subject_template) as subject_length
FROM communications.email_templates
WHERE name = 'ticket-confirmation';

-- Subject should be simple, like: "Your Ticket for {{EventTitle}}"
-- If it has complex conditionals, it could fail
```

**Fix If Confirmed**:
1. Simplify subject template (no conditionals)
2. Add fallback subject if rendering fails
3. Add detailed logging for subject rendering

---

### Hypothesis 7: Azure Email Service Configuration Changed (PROBABILITY: 25%)

**Theory**: Azure Communication Services settings changed between Dec 22-23, breaking email sending.

**Evidence Against** (Lower Probability):
1. User confirmed test email sent successfully
2. Free events emails working
3. Only paid events affected

**However, Could Still Be**:
- Attachment size limits changed (PDF tickets)
- Rate limiting triggered for transactional emails
- Sender domain verification expired

**How to Verify**:
```bash
# Check Azure Container App environment variables
az containerapp show --name <app-name> --resource-group <rg> \
  --query properties.configuration.secrets

# Verify:
# - Email__Provider = "Azure"
# - Email__AzureConnectionString is set
# - Email__AzureSenderAddress is valid
```

**Azure Portal Check**:
1. Go to Azure Communication Services resource
2. Check "Email" → "Domains" → Verify domain status
3. Check "Email" → "Overview" → Recent send statistics
4. Look for delivery failures or bounces

**Fix If Confirmed**:
1. Re-verify sender domain
2. Check rate limiting quotas
3. Review ACS diagnostic logs

---

### Hypothesis 8: Database Connection Issue in Template Repository (PROBABILITY: 20%)

**Theory**: The `_emailTemplateRepository.GetByNameAsync()` call is timing out or failing due to database connectivity issues.

**Evidence Against**:
1. Other database operations working (registration queries work)
2. Free event emails work (also use template repository)

**However, Could Be**:
- Connection pool exhaustion
- Long-running query blocking template reads
- Database deadlock on template table

**How to Verify**:
```sql
-- Check for locks on template table
SELECT
  pid,
  state,
  query,
  wait_event_type,
  wait_event
FROM pg_stat_activity
WHERE query LIKE '%email_templates%'
  AND state != 'idle';
```

**Fix If Confirmed**:
1. Add query timeout to template repository
2. Add retry logic for template lookups
3. Investigate database performance

---

## Section 2: Systematic Diagnostic Steps

### Step 1: Check Azure Container App Logs (CRITICAL - Do This First)

**What to Look For**:
```bash
# Step 1A: Check if handler is invoked
az containerapp logs show --name lankaconnect-staging-api --resource-group <rg> \
  --filter "PaymentCompletedEventHandler INVOKED" --since 48h

# Expected: Should see multiple log entries like:
# [Phase 6A.24] ✅ PaymentCompletedEventHandler INVOKED - Event {EventId}, Registration {RegistrationId}

# Step 1B: Check for template rendering errors
az containerapp logs show --name lankaconnect-staging-api --resource-group <rg> \
  --filter "Failed to render email template" --since 48h

# Step 1C: Check for ticket generation issues
az containerapp logs show --name lankaconnect-staging-api --resource-group <rg> \
  --filter "Failed to generate ticket" --since 48h

# Step 1D: Check for email sending failures
az containerapp logs show --name lankaconnect-staging-api --resource-group <rg> \
  --filter "Failed to send payment confirmation email" --since 48h

# Step 1E: Check for ANY errors in PaymentCompletedEventHandler
az containerapp logs show --name lankaconnect-staging-api --resource-group <rg> \
  --filter "PaymentCompletedEventHandler" --since 48h | grep -i error
```

**What Each Log Tells You**:
- **No "INVOKED" logs**: Hypothesis 2 confirmed (event not raised)
- **"Failed to render" errors**: Hypothesis 1 confirmed (rendering issue)
- **"Failed to generate ticket"**: Hypothesis 5 confirmed (ticket issue)
- **"Failed to send email"**: Check detailed error message for Azure issues

---

### Step 2: Database Forensics

**Query 1: Check Recent Registrations**:
```sql
SELECT
  r.id,
  r.event_id,
  r.user_id,
  r.payment_status,
  r.payment_completed_at,
  r.stripe_payment_intent_id,
  r.contact_email,
  r.contact_phone,
  r.created_at,
  r.updated_at,
  e.title as event_title
FROM events.event_registrations r
JOIN events.events e ON r.event_id = e.id
WHERE r.event_id = '9e3722f5-c255-4dcc-b167-afef56bc5592'
  AND r.created_at >= '2025-12-23'
ORDER BY r.created_at DESC;

-- LOOK FOR:
-- 1. payment_status = 'Completed' (should be TRUE)
-- 2. payment_completed_at IS NOT NULL (should be TRUE)
-- 3. stripe_payment_intent_id IS NOT NULL (should be TRUE)
-- 4. contact_email - could be NULL
```

**Query 2: Check Email Send Attempts**:
```sql
SELECT
  id,
  sender_email,
  recipient_emails,
  subject,
  status,
  sent_at,
  failed_at,
  error_message,
  created_at
FROM communications.email_messages
WHERE created_at >= '2025-12-23'
  AND subject LIKE '%Ticket%'
ORDER BY created_at DESC
LIMIT 10;

-- LOOK FOR:
-- 1. Are emails being created at all?
-- 2. What is the status? (Queued, Sent, Failed)
-- 3. Check error_message if Failed
```

**Query 3: Verify Template**:
```sql
SELECT
  name,
  is_active,
  LENGTH(subject_template) as subject_len,
  LENGTH(html_template) as html_len,
  subject_template,
  -- Check for unbalanced conditionals
  (LENGTH(html_template) - LENGTH(REPLACE(html_template, '{{#', ''))) / 3 as opening_tags,
  (LENGTH(html_template) - LENGTH(REPLACE(html_template, '{{/', ''))) / 3 as closing_tags,
  updated_at
FROM communications.email_templates
WHERE name = 'ticket-confirmation';

-- CRITICAL CHECKS:
-- 1. is_active = TRUE
-- 2. opening_tags = closing_tags (balanced conditionals)
-- 3. updated_at should be 2025-12-23 (recent Phase 6A.43 update)
```

---

### Step 3: Test Resend Email Button (Controlled Test)

**Why This Helps**: The resend button uses the same code path as PaymentCompletedEventHandler, so any error will be visible in browser console.

**How to Test**:
1. Go to `/my-registrations` page
2. Find the registration for event `9e3722f5-c255-4dcc-b167-afef56bc5592`
3. Click "Resend Email" button
4. Open browser DevTools Console
5. Look for specific error message

**Error Messages to Look For**:
- `"Failed to render email template"` → Hypothesis 1 (template rendering)
- `"Template 'ticket-confirmation' not found"` → Hypothesis 3 (template lookup)
- `"Registration not found"` → Database issue
- `"Payment not completed"` → Payment status issue
- Network 500 error → Check Azure logs for exception

---

### Step 4: Direct Database Template Test

**Run This to Test Template Rendering Logic Manually**:

Create test script: `test-template-rendering.sql`
```sql
-- Simulate RenderTemplateContent logic

-- Get template
SELECT html_template
INTO TEMP template_content
FROM communications.email_templates
WHERE name = 'ticket-confirmation';

-- Try to find all conditional tags
SELECT
  regexp_matches(html_template, '{{#([^}]+)}}', 'g') as opening_tags,
  regexp_matches(html_template, '{{/([^}]+)}}', 'g') as closing_tags
FROM communications.email_templates
WHERE name = 'ticket-confirmation';

-- If counts don't match, template is malformed
```

---

## Section 3: Minimal Reproduction Test

**Goal**: Create smallest possible test to isolate the issue.

**Option A: Direct API Call** (if you have API access):
```bash
# Test template rendering endpoint directly
curl -X POST https://<staging-url>/api/email/test-template \
  -H "Content-Type: application/json" \
  -d '{
    "templateName": "ticket-confirmation",
    "parameters": {
      "UserName": "Test User",
      "EventTitle": "Test Event",
      "EventDateTime": "December 25, 2025 from 5:00 PM to 10:00 PM",
      "EventLocation": "Test Location",
      "RegistrationDate": "December 23, 2025 1:00 PM",
      "Attendees": "<p>Test Attendee</p>",
      "HasAttendeeDetails": true,
      "EventImageUrl": "",
      "HasEventImage": false,
      "AmountPaid": "$50.00",
      "PaymentIntentId": "pi_test123",
      "PaymentDate": "December 23, 2025 1:00 PM",
      "HasTicket": true,
      "TicketCode": "TEST123",
      "TicketExpiryDate": "December 26, 2025",
      "ContactEmail": "test@example.com",
      "ContactPhone": "+1234567890",
      "HasContactInfo": true
    }
  }'
```

**Option B: Unit Test** (add to test project):
```csharp
[Fact]
public async Task PaymentCompletedEventHandler_ShouldSendEmail_WhenValidData()
{
    // Arrange
    var eventId = Guid.Parse("9e3722f5-c255-4dcc-b167-afef56bc5592");
    var registrationId = Guid.NewGuid();

    // Act
    var result = await _handler.Handle(new PaymentCompletedEvent(...));

    // Assert
    Assert.True(result.IsSuccess);
    // Verify email was sent
}
```

---

## Section 4: Fix Plans for Each Hypothesis

### Fix Plan 1: Template Rendering Exception

**If Confirmed**: RenderTemplateContent is throwing exception on nested conditionals or malformed template.

**Immediate Fix (15 minutes)**:
```csharp
// Add to AzureEmailService.cs, line 627
public async Task<Result<RenderedEmailTemplate>> RenderTemplateAsync(
    string templateName,
    Dictionary<string, object> parameters,
    CancellationToken cancellationToken = default)
{
    try
    {
        _logger.LogInformation(
            "[DIAG] Starting template rendering for '{TemplateName}'",
            templateName);

        var template = await _emailTemplateRepository.GetByNameAsync(templateName, cancellationToken);
        if (template == null)
        {
            _logger.LogWarning("[DIAG] Template '{TemplateName}' not found", templateName);
            return Result<RenderedEmailTemplate>.Failure($"Email template '{templateName}' not found");
        }

        _logger.LogInformation("[DIAG] Template found, rendering subject...");

        // ADD DEFENSIVE TRY-CATCH HERE
        string subject, htmlBody, textBody;
        try
        {
            subject = RenderTemplateContent(template.SubjectTemplate.Value, parameters);
            _logger.LogInformation("[DIAG] Subject rendered: {Subject}", subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DIAG] Failed to render subject template");
            return Result<RenderedEmailTemplate>.Failure($"Failed to render subject: {ex.Message}");
        }

        try
        {
            htmlBody = RenderTemplateContent(template.HtmlTemplate ?? string.Empty, parameters);
            _logger.LogInformation("[DIAG] HTML rendered, length: {Length}", htmlBody.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DIAG] Failed to render HTML template");
            return Result<RenderedEmailTemplate>.Failure($"Failed to render HTML: {ex.Message}");
        }

        try
        {
            textBody = RenderTemplateContent(template.TextTemplate, parameters);
            _logger.LogInformation("[DIAG] Text rendered, length: {Length}", textBody.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DIAG] Failed to render text template");
            return Result<RenderedEmailTemplate>.Failure($"Failed to render text: {ex.Message}");
        }

        return Result<RenderedEmailTemplate>.Success(new RenderedEmailTemplate
        {
            Subject = subject,
            HtmlBody = htmlBody,
            PlainTextBody = textBody
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[DIAG] Unexpected error in RenderTemplateAsync");
        return Result<RenderedEmailTemplate>.Failure($"Unexpected error: {ex.Message}");
    }
}
```

**Deploy and Monitor**: Logs will now show exactly where rendering fails.

---

### Fix Plan 2: PaymentCompletedEvent Not Raised

**If Confirmed**: Domain event dispatcher not working.

**Check 1: Verify Event is Added to Domain**:
```csharp
// In Registration.cs or wherever payment is completed
public Result CompletePayment(string paymentIntentId, decimal amountPaid)
{
    // ... payment logic ...

    // THIS LINE MUST EXIST:
    AddDomainEvent(new PaymentCompletedEvent(
        Id,
        EventId,
        UserId,
        amountPaid,
        paymentIntentId,
        DateTime.UtcNow,
        Contact?.Email ?? "",
        Contact?.PhoneNumber));

    return Result.Success();
}
```

**Check 2: Verify SaveChanges Dispatches Events**:
```csharp
// In ApplicationDbContext.cs
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // Dispatch domain events
    await _domainEventDispatcher.DispatchEventsAsync(this, cancellationToken);

    return await base.SaveChangesAsync(cancellationToken);
}
```

**Fix**: Ensure both checks pass. Add logging to confirm event dispatch.

---

### Fix Plan 3: Template Not Found

**If Confirmed**: Database query returning null.

**Fix**:
```sql
-- Verify template exists with exact name
SELECT * FROM communications.email_templates WHERE name = 'ticket-confirmation';

-- If not found, insert it:
INSERT INTO communications.email_templates (
  id, name, description, subject_template, html_template, text_template, is_active
) VALUES (
  gen_random_uuid(),
  'ticket-confirmation',
  'Ticket confirmation email for paid events',
  'Your Ticket for {{EventTitle}}',
  '...',  -- HTML content
  '...',  -- Text content
  true
);
```

---

### Fix Plan 4: Contact Null Causing Issues

**If Confirmed**: Empty ContactEmail/ContactPhone breaking template.

**Fix**:
```csharp
// In PaymentCompletedEventHandler.cs, lines 139-150
// CHANGE THIS:
if (registration.Contact != null)
{
    parameters["ContactEmail"] = registration.Contact.Email;
    parameters["ContactPhone"] = registration.Contact.PhoneNumber ?? "";
    parameters["HasContactInfo"] = true;
}
else
{
    parameters["ContactEmail"] = "";  // ❌ REMOVE
    parameters["ContactPhone"] = "";  // ❌ REMOVE
    parameters["HasContactInfo"] = false;
}

// TO THIS:
if (registration.Contact != null)
{
    parameters["ContactEmail"] = registration.Contact.Email;
    parameters["ContactPhone"] = registration.Contact.PhoneNumber ?? "";
    parameters["HasContactInfo"] = true;
}
else
{
    // DON'T add ContactEmail/ContactPhone parameters at all
    parameters["HasContactInfo"] = false;
}
```

---

## Section 5: Recommended Action Plan

### Immediate Actions (Next 30 Minutes)

1. **Run Azure Log Queries** (5 min):
   - Check if PaymentCompletedEventHandler is being invoked
   - Look for "Failed to render" errors
   - Look for exception stack traces

2. **Run Database Queries** (5 min):
   - Verify recent registrations exist with payment_completed_at
   - Check email_messages table for send attempts
   - Verify ticket-confirmation template exists and is active

3. **Test Resend Button** (5 min):
   - Click resend on user's registration
   - Capture exact error from browser console
   - Capture exact error from Azure logs

4. **Analyze Results** (15 min):
   - Compare findings with hypotheses
   - Identify most likely root cause
   - Prepare targeted fix

### Follow-Up Actions (Next 2 Hours)

**If Hypothesis 1 Confirmed (Template Rendering)**:
1. Deploy defensive logging fix (15 min)
2. Trigger test payment (10 min)
3. Analyze detailed logs (15 min)
4. Deploy permanent fix (30 min)
5. Verify with real payment (15 min)

**If Hypothesis 2 Confirmed (Event Not Raised)**:
1. Check domain event registration in DI (15 min)
2. Add logging to CompletePayment method (15 min)
3. Trigger test payment (10 min)
4. Verify event is dispatched (10 min)
5. Deploy fix (30 min)

**If Hypothesis 3 Confirmed (Template Not Found)**:
1. Re-run template migration (10 min)
2. Verify template in database (5 min)
3. Test resend button (5 min)
4. Deploy if needed (15 min)

---

## Section 6: Success Criteria

**Email Sending Restored When**:
- ✅ Azure logs show "Payment confirmation email sent successfully"
- ✅ User receives email with ticket PDF attached
- ✅ Email uses new gradient design (not old blue header)
- ✅ All template variables properly rendered (no `{{}}` visible)
- ✅ Resend button works without errors

**Root Cause Confirmed When**:
- ✅ Specific error identified in logs
- ✅ Fix deployed successfully
- ✅ 3+ consecutive test payments send emails successfully
- ✅ No regression in free event emails

---

## Section 7: Questions for User

1. **Azure Logs**: Can you run the Step 1 log queries and share the output? This will immediately tell us which hypothesis is correct.

2. **Resend Button**: Can you click the resend button and share:
   - Browser console error (F12 → Console tab)
   - Network tab response (F12 → Network tab)

3. **Database Access**: Can you run the Step 2 database queries?

4. **Timeline Clarification**:
   - When exactly was Phase 6A.43 deployed to staging? (date/time)
   - When was the last successful paid event email sent? (date/time)
   - How many paid registrations have occurred since deployment?

5. **User Registration Details**:
   - Registration ID for event `9e3722f5-c255-4dcc-b167-afef56bc5592`
   - Did user fill out Contact information during registration?

---

## Appendix A: File Locations

**Event Handlers**:
- `src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs` - Lines 43-243
- `src/LankaConnect.Application/Events/EventHandlers/RegistrationConfirmedEventHandler.cs` - Lines 40-158

**Email Services**:
- `src/LankaConnect.Infrastructure/Email/Services/AzureEmailService.cs` - Lines 22-774
- Template rendering: Lines 227-280
- IEmailTemplateService implementation: Lines 615-773

**Command Handlers**:
- `src/LankaConnect.Application/Events/Commands/ResendTicketEmail/ResendTicketEmailCommandHandler.cs` - Lines 45-292

**Database**:
- Template table: `communications.email_templates`
- Registration table: `events.event_registrations`
- Email tracking: `communications.email_messages`

---

## Appendix B: Similar Past Issues

**Phase 6A.35** (Dec 19, 2025): Template discovery failure
- **Symptom**: All emails stopped (100% failure)
- **Root Cause**: Docker permission issue preventing file reads
- **Fix**: Dockerfile user permissions
- **Lesson**: Check container logs for file access errors

**Phase 6A.43** (Dec 23, 2025): Email rendering mismatch
- **Symptom**: Emails sent but with unrendered variables
- **Root Cause**: Two rendering paths (filesystem vs database)
- **Fix**: Unified to database templates
- **Lesson**: This current issue likely related to Phase 6A.43 changes

**Phase 6A.34** (Dec 19, 2025): Domain event dispatch
- **Symptom**: Events not triggering handlers
- **Root Cause**: DomainEventDispatcher not calling DispatchEventsAsync
- **Fix**: Updated SaveChangesAsync
- **Lesson**: Always verify events are dispatched after registration changes

---

**Document Status**: Ready for diagnostic execution
**Next Update**: After Step 1-3 results analyzed
**Assigned To**: System Architect + DevOps
**Priority**: P0 - CRITICAL (blocking paid event revenue)
