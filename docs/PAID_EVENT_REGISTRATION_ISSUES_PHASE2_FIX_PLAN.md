# Fix Plan: Paid Event Registration Issues (Phase 2)

**Date:** 2025-12-23
**Related RCA:** `PAID_EVENT_REGISTRATION_ISSUES_PHASE2_RCA.md`
**Priority:** P0 - Critical Production Issue

---

## Overview

This document outlines the fix plan for three critical issues affecting paid event registrations:
1. Payment success page shows wrong total amount (cache timing)
2. No confirmation emails sent (template rendering failure)
3. Email resend fails with 400 error (template variable mismatch)

**Total Issues:** 6 (3 primary, 3 secondary)
**Estimated Time:** 4-6 hours
**Risk Level:** Medium (requires database + code changes)

---

## Fix Priority Matrix

| Issue | Severity | Impact | Priority | Est. Time |
|-------|----------|--------|----------|-----------|
| Fix 1: Update database email template | Critical | High | P0 | 2 hours |
| Fix 2: Align ResendTicketEmailCommandHandler params | High | Medium | P1 | 1 hour |
| Fix 3: Payment success page cache timing | Medium | Medium | P2 | 1 hour |
| Fix 4: Add template validation | Medium | Low | P3 | 2 hours |
| Fix 5: Improve error visibility | Low | Low | P4 | 1 hour |
| Fix 6: Add monitoring/alerts | Low | Low | P5 | 2 hours |

---

## Fix 1: Update Database Email Template (P0 - Critical)

### Problem
Database template `ticket-confirmation` is missing variables added in Phase 6A.43:
- `{{EventDateTime}}` (date range format)
- `{{EventImageUrl}}` (direct image URL)
- `{{HasEventImage}}` (conditional flag)
- `{{EventLocation}}` (formatted location)

### Solution
Update database template to match Phase 6A.43 expectations.

### Implementation Steps

#### Step 1: Extract current template structure from script
```javascript
// File: scripts/apply-template-v2.js
// This contains the CORRECT template layout updated in Phase 6A.42/43
```

**Action:**
1. Read `scripts/apply-template-v2.js` to get HTML template
2. Extract subject, HTML body, and text body
3. Verify it uses Phase 6A.43 variables

#### Step 2: Create database update script
```sql
-- File: scripts/update-ticket-confirmation-template.sql

UPDATE email_templates
SET
  subject_template = 'Your Ticket for {{EventTitle}} - Registration Confirmed',

  html_template = '<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Registration Confirmed - LankaConnect</title>
</head>
<body style="font-family: ''Segoe UI'', Arial, sans-serif; line-height: 1.6; color: #333333; margin: 0; padding: 0; background-color: #f3f4f6;">
    <!-- Full template from apply-template-v2.js -->
    <!-- Includes: EventImageUrl, HasEventImage, EventDateTime, EventLocation, Attendees -->
</body>
</html>',

  text_template = 'Registration Confirmed!

Hi {{UserName}},

Thank you for registering for {{EventTitle}}.

Event Details:
- Event: {{EventTitle}}
- When: {{EventDateTime}}
- Where: {{EventLocation}}

{{#HasAttendeeDetails}}
Attendees:
{{Attendees}}
{{/HasAttendeeDetails}}

{{#HasTicket}}
Ticket Code: {{TicketCode}}
Valid Until: {{TicketExpiryDate}}
{{/HasTicket}}

{{#HasContactInfo}}
Contact Information:
Email: {{ContactEmail}}
{{/HasContactInfo}}

Amount Paid: {{AmountPaid}}
Payment Date: {{PaymentDate}}

Your ticket is attached to this email as a PDF.

See you at the event!

Best regards,
LankaConnect Team',

  updated_at = NOW()

WHERE name = 'ticket-confirmation';
```

#### Step 3: Verify template variables alignment

**PaymentCompletedEventHandler.cs provides:**
```csharp
{ "UserName", recipientName },
{ "EventTitle", @event.Title.Value },
{ "EventDateTime", FormatEventDateTimeRange(@event.StartDate, @event.EndDate) }, // ✅ Phase 6A.43
{ "EventLocation", GetEventLocationString(@event) },                             // ✅ Phase 6A.43
{ "RegistrationDate", domainEvent.PaymentCompletedAt.ToString(...) },
{ "Attendees", attendeeDetailsHtml.ToString().TrimEnd() },
{ "HasAttendeeDetails", hasAttendeeDetails },
{ "EventImageUrl", eventImageUrl },                                              // ✅ Phase 6A.43
{ "HasEventImage", hasEventImage },                                              // ✅ Phase 6A.43
{ "AmountPaid", domainEvent.AmountPaid.ToString("C") },
{ "PaymentIntentId", domainEvent.PaymentIntentId },
{ "PaymentDate", domainEvent.PaymentCompletedAt.ToString(...) },
{ "HasTicket", true },
{ "TicketCode", ticketResult.Value.TicketCode },
{ "TicketExpiryDate", @event.EndDate.AddDays(1).ToString(...) },
{ "ContactEmail", registration.Contact?.Email ?? "" },
{ "ContactPhone", registration.Contact?.PhoneNumber ?? "" },
{ "HasContactInfo", registration.Contact != null }
```

**Template must use ONLY these variables** (no extras).

#### Step 4: Create Node.js script to apply template
```javascript
// File: scripts/apply-ticket-confirmation-template.js

const { Client } = require('pg');

const client = new Client({
  host: 'lankaconnect-staging-db.postgres.database.azure.com',
  database: 'LankaConnectDB',
  user: 'adminuser',
  password: '1qaz!QAZ',
  ssl: { rejectUnauthorized: false }
});

// Full template content from apply-template-v2.js
const subjectTemplate = 'Your Ticket for {{EventTitle}} - Registration Confirmed';
const htmlTemplate = `<!-- Full HTML from apply-template-v2.js -->`;
const textTemplate = `Full text template`;

async function updateTemplate() {
  try {
    await client.connect();

    // Update ticket-confirmation template
    const result = await client.query(`
      UPDATE email_templates
      SET
        subject_template = $1,
        html_template = $2,
        text_template = $3,
        updated_at = NOW()
      WHERE name = 'ticket-confirmation'
      RETURNING id, name, updated_at
    `, [subjectTemplate, htmlTemplate, textTemplate]);

    if (result.rowCount === 0) {
      console.error('❌ Template not found: ticket-confirmation');
      process.exit(1);
    }

    console.log('✅ Updated template:', result.rows[0]);

    // Verify template exists and is active
    const verify = await client.query(`
      SELECT name, is_active, created_at, updated_at
      FROM email_templates
      WHERE name = 'ticket-confirmation'
    `);

    console.log('📋 Template status:', verify.rows[0]);

  } catch (error) {
    console.error('❌ Error:', error);
    process.exit(1);
  } finally {
    await client.end();
  }
}

updateTemplate();
```

#### Step 5: Test template rendering

**Test script:**
```javascript
// File: scripts/test-ticket-template-rendering.js

const { Client } = require('pg');

// Test parameters matching PaymentCompletedEventHandler
const testParams = {
  UserName: 'John Doe',
  EventTitle: 'Sri Lankan New Year Celebration 2026',
  EventDateTime: 'December 24, 2025 from 5:00 PM to 10:00 PM',
  EventLocation: '123 Main St, Colombo',
  RegistrationDate: 'December 23, 2025 2:30 PM',
  Attendees: '<p style="margin: 8px 0; font-size: 16px;">John Doe</p><p style="margin: 8px 0; font-size: 16px;">Jane Doe</p>',
  HasAttendeeDetails: true,
  EventImageUrl: 'https://example.com/image.jpg',
  HasEventImage: true,
  AmountPaid: '$40.00',
  PaymentIntentId: 'pi_test123',
  PaymentDate: 'December 23, 2025 2:30 PM',
  HasTicket: true,
  TicketCode: 'TKT-ABC123',
  TicketExpiryDate: 'December 25, 2025',
  ContactEmail: 'john@example.com',
  ContactPhone: '+1234567890',
  HasContactInfo: true
};

async function testTemplate() {
  const client = new Client({ /* connection */ });
  await client.connect();

  // Fetch template
  const result = await client.query(`
    SELECT subject_template, html_template, text_template
    FROM email_templates
    WHERE name = 'ticket-confirmation'
  `);

  if (result.rows.length === 0) {
    console.error('❌ Template not found');
    process.exit(1);
  }

  const template = result.rows[0];

  // Render template (simple variable substitution)
  let rendered = template.html_template;
  for (const [key, value] of Object.entries(testParams)) {
    const placeholder = `{{${key}}}`;
    rendered = rendered.replace(new RegExp(placeholder, 'g'), value);
  }

  console.log('✅ Template rendered successfully');
  console.log('Subject:', renderSubject(template.subject_template, testParams));

  await client.end();
}

function renderSubject(template, params) {
  let result = template;
  for (const [key, value] of Object.entries(params)) {
    result = result.replace(`{{${key}}}`, value);
  }
  return result;
}

testTemplate();
```

### Acceptance Criteria
- [ ] Database template uses all Phase 6A.43 variables
- [ ] Template renders without errors for test parameters
- [ ] HTML includes event image section with `{{#HasEventImage}}` conditional
- [ ] Date shows as range format: "December 24, 2025 from 5:00 PM to 10:00 PM"
- [ ] Attendees show names only (no age) in HTML paragraph format
- [ ] Text template includes all required sections

### Rollback Plan
```sql
-- Backup current template before update
CREATE TABLE email_templates_backup_20251223 AS
SELECT * FROM email_templates WHERE name = 'ticket-confirmation';

-- Rollback if needed
UPDATE email_templates
SET
  subject_template = (SELECT subject_template FROM email_templates_backup_20251223),
  html_template = (SELECT html_template FROM email_templates_backup_20251223),
  text_template = (SELECT text_template FROM email_templates_backup_20251223)
WHERE name = 'ticket-confirmation';
```

---

## Fix 2: Align ResendTicketEmailCommandHandler Parameters (P1 - High)

### Problem
ResendTicketEmailCommandHandler provides different parameters than PaymentCompletedEventHandler:
- Uses `EventStartDate` + `EventStartTime` instead of `EventDateTime`
- Missing `EventImageUrl` and `HasEventImage`
- Different `EventLocation` formatting logic

### Solution
Update ResendTicketEmailCommandHandler to provide identical parameters.

### Implementation

**File:** `src/LankaConnect.Application/Events/Commands/ResendTicketEmail/ResendTicketEmailCommandHandler.cs`

```csharp
// BEFORE (lines 158-176) - OLD format
var parameters = new Dictionary<string, object>
{
    { "UserName", recipientName },
    { "EventTitle", @event.Title.Value },
    { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },     // ❌ OLD
    { "EventStartTime", @event.StartDate.ToString("h:mm tt") },           // ❌ OLD
    { "EventLocation", @event.Location != null ? $"{@event.Location.Address.Street}, {@event.Location.Address.City}" : "Online Event" }, // ❌ Unsafe
    // ... rest of parameters
};

// AFTER - Aligned with PaymentCompletedEventHandler
var parameters = new Dictionary<string, object>
{
    { "UserName", recipientName },
    { "EventTitle", @event.Title.Value },

    // Phase 6A.43: Use date range format matching PaymentCompletedEventHandler
    { "EventDateTime", FormatEventDateTimeRange(@event.StartDate, @event.EndDate) },

    // Phase 6A.43: Use safe location formatting matching PaymentCompletedEventHandler
    { "EventLocation", GetEventLocationString(@event) },

    { "RegistrationDate", registration.CreatedAt.ToString("MMMM dd, yyyy h:mm tt") },

    // Attendee details - names only (no age)
    { "Attendees", attendeeDetailsHtml.ToString().TrimEnd() },
    { "HasAttendeeDetails", registration.HasDetailedAttendees() },

    // Event image
    { "EventImageUrl", GetEventImageUrl(@event) },
    { "HasEventImage", HasEventImage(@event) },

    // Payment details
    { "IsPaidEvent", true },
    { "AmountPaid", registration.TotalPrice?.Amount.ToString("C") ?? "$0.00" },
    { "PaymentIntentId", registration.StripePaymentIntentId ?? "" },
    { "PaymentDate", DateTime.UtcNow.ToString("MMMM dd, yyyy h:mm tt") },

    // Ticket details
    { "HasTicket", true },
    { "TicketCode", ticket.TicketCode },
    { "TicketExpiryDate", @event.EndDate.AddDays(1).ToString("MMMM dd, yyyy") },

    // Contact information
    { "ContactEmail", registration.Contact?.Email ?? "support@lankaconnect.com" },
    { "ContactPhone", registration.Contact?.PhoneNumber ?? "" },
    { "HasContactInfo", registration.Contact != null }
};

// Add helper methods at bottom of class
private static string FormatEventDateTimeRange(DateTime startDate, DateTime endDate)
{
    if (startDate.Date == endDate.Date)
    {
        // Same day event
        return $"{startDate:MMMM dd, yyyy} from {startDate:h:mm tt} to {endDate:h:mm tt}";
    }
    else
    {
        // Multi-day event
        return $"{startDate:MMMM dd, yyyy} at {startDate:h:mm tt} to {endDate:MMMM dd, yyyy} at {endDate:h:mm tt}";
    }
}

private static string GetEventLocationString(Event @event)
{
    // Defensive null handling matching PaymentCompletedEventHandler
    if (@event.Location?.Address == null)
        return "Online Event";

    var street = @event.Location.Address.Street;
    var city = @event.Location.Address.City;

    if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city))
        return "Online Event";

    if (string.IsNullOrWhiteSpace(street))
        return city!;

    if (string.IsNullOrWhiteSpace(city))
        return street;

    return $"{street}, {city}";
}

private static string GetEventImageUrl(Event @event)
{
    var primaryImage = @event.Images.FirstOrDefault(i => i.IsPrimary);
    return primaryImage?.ImageUrl ?? "";
}

private static bool HasEventImage(Event @event)
{
    var imageUrl = GetEventImageUrl(@event);
    return !string.IsNullOrEmpty(imageUrl);
}
```

### Changes Required
1. Replace `EventStartDate` + `EventStartTime` with `EventDateTime`
2. Add `EventImageUrl` and `HasEventImage` parameters
3. Use `GetEventLocationString` helper (defensive null handling)
4. Add `FormatEventDateTimeRange` helper method
5. Update attendee details formatting to match PaymentCompletedEventHandler

### Acceptance Criteria
- [ ] ResendTicketEmailCommandHandler provides identical parameters to PaymentCompletedEventHandler
- [ ] Email template renders successfully
- [ ] Resend email API returns 200 OK
- [ ] Email received with correct formatting
- [ ] All 3 pricing types tested (Single, AgeDual, GroupTiered)

---

## Fix 3: Payment Success Page Cache Timing (P2 - Medium)

### Problem
Payment success page shows wrong total amount on first render due to stale React Query cache.

### Root Cause
```typescript
// Current implementation (payment/success/page.tsx:47-53)
useEffect(() => {
  if (eventId && isHydrated) {
    queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) });
    queryClient.invalidateQueries({ queryKey: ['user-rsvps'] });
    queryClient.invalidateQueries({ queryKey: ['user-registration', eventId] });
  }
}, [eventId, isHydrated, queryClient]);
```

**Problem:** Cache invalidation runs AFTER component mounts and renders.

### Solution
Prefetch registration data on mount, show loading state until fresh data available.

### Implementation

**File:** `web/src/app/events/payment/success/page.tsx`

```typescript
function PaymentSuccessContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const eventId = searchParams?.get('eventId');
  const [isRedirecting, setIsRedirecting] = useState(false);
  const [isFetchingFreshData, setIsFetchingFreshData] = useState(true); // ✅ NEW
  const { user, isHydrated } = useAuthStore();
  const queryClient = useQueryClient();

  // Fetch event details
  const { data: event, isLoading, error } = useEventById(eventId || undefined);

  // Fetch registration details
  const { data: registrationDetails, isLoading: isLoadingRegistration, refetch } = useUserRegistrationDetails(
    (user?.userId && isHydrated && eventId) ? eventId : undefined,
    true
  );

  // ✅ FIX: Invalidate cache and refetch BEFORE rendering
  useEffect(() => {
    let mounted = true;

    async function refreshData() {
      if (eventId && isHydrated) {
        // Invalidate stale cache
        await queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) });
        await queryClient.invalidateQueries({ queryKey: ['user-rsvps'] });
        await queryClient.invalidateQueries({ queryKey: ['user-registration', eventId] });

        // Refetch registration details
        if (user?.userId) {
          await refetch();
        }

        // Mark data as fresh
        if (mounted) {
          setIsFetchingFreshData(false);
        }
      }
    }

    refreshData();

    return () => {
      mounted = false;
    };
  }, [eventId, isHydrated, user?.userId, queryClient, refetch]);

  // ✅ Show loading state while fetching fresh data
  if (isLoading || isFetchingFreshData) {
    return (
      <div className="min-h-screen flex flex-col bg-background">
        <Header />
        <main className="flex-1 container mx-auto px-4 py-8">
          <div className="max-w-2xl mx-auto text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto"></div>
            <p className="mt-4 text-muted-foreground">Loading your registration details...</p>
          </div>
        </main>
        <Footer />
      </div>
    );
  }

  return (
    <div className="min-h-screen flex flex-col bg-background">
      {/* Rest of component unchanged */}
    </div>
  );
}
```

### Alternative Solution (Simpler)
Add a minimum delay before rendering to ensure cache invalidation completes:

```typescript
useEffect(() => {
  async function refreshData() {
    if (eventId && isHydrated) {
      // Invalidate cache
      await queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) });
      await queryClient.invalidateQueries({ queryKey: ['user-rsvps'] });
      await queryClient.invalidateQueries({ queryKey: ['user-registration', eventId] });

      // Wait 500ms for refetch to complete
      await new Promise(resolve => setTimeout(resolve, 500));

      setIsFetchingFreshData(false);
    }
  }

  refreshData();
}, [eventId, isHydrated, queryClient]);
```

### Acceptance Criteria
- [ ] Payment success page shows correct total amount on first render
- [ ] No flash of wrong amount (stale cache)
- [ ] Loading state displayed until fresh data available
- [ ] Attendee count displays correctly
- [ ] Works for all 3 pricing types

---

## Fix 4: Add Template Validation (P3 - Medium)

### Problem
No validation that database templates contain required variables.

### Solution
Create template validation service and schema definitions.

### Implementation

**File:** `src/LankaConnect.Application/Common/Interfaces/IEmailTemplateValidator.cs`

```csharp
namespace LankaConnect.Application.Common.Interfaces;

public interface IEmailTemplateValidator
{
    /// <summary>
    /// Validates that template contains all required variables
    /// </summary>
    Result ValidateTemplate(string templateName, string templateContent, List<string> requiredVariables);

    /// <summary>
    /// Extracts all variables from template content
    /// </summary>
    List<string> ExtractVariables(string templateContent);

    /// <summary>
    /// Validates template syntax (conditionals, placeholders)
    /// </summary>
    Result ValidateTemplateSyntax(string templateContent);
}
```

**File:** `src/LankaConnect.Infrastructure/Email/Services/EmailTemplateValidator.cs`

```csharp
using System.Text.RegularExpressions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Infrastructure.Email.Services;

public class EmailTemplateValidator : IEmailTemplateValidator
{
    public Result ValidateTemplate(string templateName, string templateContent, List<string> requiredVariables)
    {
        // Extract variables from template
        var templateVariables = ExtractVariables(templateContent);

        // Check for missing required variables
        var missingVariables = requiredVariables.Except(templateVariables).ToList();

        if (missingVariables.Any())
        {
            return Result.Failure($"Template '{templateName}' is missing required variables: {string.Join(", ", missingVariables)}");
        }

        // Validate syntax
        var syntaxResult = ValidateTemplateSyntax(templateContent);
        if (syntaxResult.IsFailure)
        {
            return syntaxResult;
        }

        return Result.Success();
    }

    public List<string> ExtractVariables(string templateContent)
    {
        var variables = new HashSet<string>();

        // Extract {{variable}} placeholders
        var matches = Regex.Matches(templateContent, @"\{\{([^}#/]+)\}\}");
        foreach (Match match in matches)
        {
            variables.Add(match.Groups[1].Value.Trim());
        }

        // Extract {{#variable}} conditionals
        var conditionals = Regex.Matches(templateContent, @"\{\{#([^}]+)\}\}");
        foreach (Match match in conditionals)
        {
            variables.Add(match.Groups[1].Value.Trim());
        }

        return variables.ToList();
    }

    public Result ValidateTemplateSyntax(string templateContent)
    {
        // Check for unclosed conditionals
        var openTags = Regex.Matches(templateContent, @"\{\{#([^}]+)\}\}");
        var closeTags = Regex.Matches(templateContent, @"\{\{/([^}]+)\}\}");

        if (openTags.Count != closeTags.Count)
        {
            return Result.Failure($"Template has unclosed conditional tags. Found {openTags.Count} opening tags and {closeTags.Count} closing tags.");
        }

        // Validate matching pairs
        var openTagNames = openTags.Select(m => m.Groups[1].Value.Trim()).ToList();
        var closeTagNames = closeTags.Select(m => m.Groups[1].Value.Trim()).ToList();

        foreach (var openTag in openTagNames)
        {
            if (!closeTagNames.Contains(openTag))
            {
                return Result.Failure($"Template has unclosed conditional: {{{{#{openTag}}}}}");
            }
        }

        return Result.Success();
    }
}
```

**Usage in AzureEmailService:**

```csharp
public async Task<Result<RenderedEmailTemplate>> RenderTemplateAsync(
    string templateName,
    Dictionary<string, object> parameters,
    CancellationToken cancellationToken = default)
{
    // Get template from database
    var template = await _emailTemplateRepository.GetByNameAsync(templateName, cancellationToken);
    if (template == null)
    {
        return Result<RenderedEmailTemplate>.Failure($"Email template '{templateName}' not found");
    }

    // ✅ NEW: Validate template before rendering
    var requiredVariables = parameters.Keys.ToList();
    var validationResult = _templateValidator.ValidateTemplate(
        templateName,
        template.HtmlTemplate ?? "",
        requiredVariables);

    if (validationResult.IsFailure)
    {
        _logger.LogError("Template validation failed: {Error}", validationResult.Error);
        return Result<RenderedEmailTemplate>.Failure(validationResult.Error);
    }

    // Render template
    var subject = RenderTemplateContent(template.SubjectTemplate.Value, parameters);
    var htmlBody = RenderTemplateContent(template.HtmlTemplate ?? string.Empty, parameters);
    var textBody = RenderTemplateContent(template.TextTemplate, parameters);

    // ... rest of method
}
```

### Acceptance Criteria
- [ ] Validator extracts all variables from template
- [ ] Validator detects missing required variables
- [ ] Validator detects unclosed conditional tags
- [ ] Validation integrated into RenderTemplateAsync
- [ ] Validation errors logged with details

---

## Fix 5: Improve Error Visibility (P4 - Low)

### Problem
PaymentCompletedEventHandler fails silently (no user notification).

### Solution
Add monitoring and alerting for failed email sends.

### Implementation

**Option 1: Store failed email attempts in database**

```csharp
// PaymentCompletedEventHandler.cs
if (result.IsFailure)
{
    _logger.LogError(
        "Failed to send payment confirmation email to {Email}: {Errors}",
        recipientEmail, string.Join(", ", result.Errors));

    // ✅ NEW: Store failed email attempt for retry
    var failedEmail = FailedEmailAttempt.Create(
        domainEvent.RegistrationId,
        "ticket-confirmation",
        recipientEmail,
        result.Error);

    await _failedEmailRepository.AddAsync(failedEmail, cancellationToken);
}
```

**Option 2: Raise domain event for failed emails**

```csharp
// Raise domain event
RaiseDomainEvent(new EmailSendFailedEvent(
    domainEvent.RegistrationId,
    "ticket-confirmation",
    recipientEmail,
    result.Error,
    DateTime.UtcNow));
```

**Option 3: Add background job to retry failed emails**

```csharp
// Background job runs every 5 minutes
public class RetryFailedEmailsJob
{
    public async Task ExecuteAsync()
    {
        var failedEmails = await _failedEmailRepository.GetPendingRetries();

        foreach (var failed in failedEmails)
        {
            // Retry email send
            var retryResult = await _emailService.SendEmailAsync(...);

            if (retryResult.IsSuccess)
            {
                failed.MarkAsResent();
            }
            else
            {
                failed.IncrementRetryCount();
            }
        }
    }
}
```

### Acceptance Criteria
- [ ] Failed email attempts logged to database
- [ ] Admin dashboard shows failed emails
- [ ] Background job retries failed emails
- [ ] Max retry count enforced (3 attempts)
- [ ] Alerts sent to dev team for persistent failures

---

## Fix 6: Add Monitoring and Alerts (P5 - Low)

### Problem
No monitoring for email template rendering failures.

### Solution
Add Application Insights logging and alerts.

### Implementation

**Add structured logging:**

```csharp
// AzureEmailService.cs
public async Task<Result<RenderedEmailTemplate>> RenderTemplateAsync(...)
{
    using var activity = _activitySource.StartActivity("EmailTemplateRendering");
    activity?.SetTag("template.name", templateName);
    activity?.SetTag("parameter.count", parameters.Count);

    var stopwatch = Stopwatch.StartNew();

    try
    {
        // Render template
        var result = RenderTemplateContent(...);

        stopwatch.Stop();
        activity?.SetTag("rendering.duration_ms", stopwatch.ElapsedMilliseconds);

        _logger.LogInformation(
            "Template '{TemplateName}' rendered successfully in {Duration}ms with {ParameterCount} parameters",
            templateName, stopwatch.ElapsedMilliseconds, parameters.Count);

        return result;
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

        _logger.LogError(ex,
            "Template '{TemplateName}' rendering failed after {Duration}ms. Parameters: {@Parameters}",
            templateName, stopwatch.ElapsedMilliseconds, parameters.Keys);

        return Result<RenderedEmailTemplate>.Failure($"Template rendering failed: {ex.Message}");
    }
}
```

**Add Azure Monitor alert:**

```
Alert Rule: Email Template Rendering Failures
Condition: customMetrics | where name == "EmailTemplateRenderingFailed" | count > 5 in 5 minutes
Action: Send email to dev-team@lankaconnect.com
Severity: Error
```

### Acceptance Criteria
- [ ] Structured logging for template rendering
- [ ] Azure Monitor dashboard shows template metrics
- [ ] Alerts configured for rendering failures
- [ ] Metrics tracked: success rate, duration, failure count

---

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public async Task RenderTemplateAsync_WithAllRequiredVariables_ShouldSucceed()
{
    // Arrange
    var templateName = "ticket-confirmation";
    var parameters = new Dictionary<string, object>
    {
        { "UserName", "John Doe" },
        { "EventTitle", "Test Event" },
        { "EventDateTime", "December 24, 2025 from 5:00 PM to 10:00 PM" },
        { "EventLocation", "Test Location" },
        { "EventImageUrl", "https://example.com/image.jpg" },
        { "HasEventImage", true },
        // ... all required parameters
    };

    // Act
    var result = await _emailTemplateService.RenderTemplateAsync(templateName, parameters);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Subject.Should().Contain("Test Event");
    result.Value.HtmlBody.Should().Contain("December 24, 2025");
}

[Fact]
public async Task RenderTemplateAsync_WithMissingVariables_ShouldFail()
{
    // Arrange
    var templateName = "ticket-confirmation";
    var parameters = new Dictionary<string, object>
    {
        { "UserName", "John Doe" }
        // Missing EventDateTime, EventLocation, etc.
    };

    // Act
    var result = await _emailTemplateService.RenderTemplateAsync(templateName, parameters);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Contain("missing required variables");
}
```

### Integration Tests

```csharp
[Fact]
public async Task PaymentCompletedEventHandler_ShouldSendEmailWithCorrectParameters()
{
    // Arrange
    var eventId = Guid.NewGuid();
    var registrationId = Guid.NewGuid();
    var domainEvent = new PaymentCompletedEvent(
        eventId, registrationId, userId: Guid.NewGuid(),
        amountPaid: 40.00m, "test@example.com", "pi_test", DateTime.UtcNow);

    // Act
    await _handler.Handle(new DomainEventNotification<PaymentCompletedEvent>(domainEvent));

    // Assert
    _emailServiceMock.Verify(s => s.SendEmailAsync(
        It.Is<EmailMessageDto>(e =>
            e.ToEmail == "test@example.com" &&
            e.Subject.Contains("Ticket") &&
            e.HtmlBody.Contains("$40.00") &&
            e.Attachments.Any(a => a.FileName.Contains(".pdf"))
        ),
        It.IsAny<CancellationToken>()
    ), Times.Once);
}
```

### Manual Testing Checklist

**Single Price Event ($20/person):**
- [ ] Register 1 attendee - Confirm email sent with $20 total
- [ ] Register 2 attendees - Confirm email sent with $40 total
- [ ] Payment success page shows correct amount ($40)
- [ ] Resend email works (200 OK)

**AgeDual Price Event ($50 adult, $30 child):**
- [ ] Register 2 adults - Email shows $100 total
- [ ] Register 1 adult + 1 child - Email shows $80 total
- [ ] Payment success page shows correct amount
- [ ] Resend email works

**GroupTiered Price Event (1-5: $40, 6-10: $35, 11+: $30):**
- [ ] Register 3 attendees - Email shows $120 ($40 × 3)
- [ ] Register 7 attendees - Email shows $245 ($35 × 7)
- [ ] Payment success page shows correct amount
- [ ] Resend email works

---

## Deployment Plan

### Phase 1: Database Template Update (P0)
1. Backup current template
2. Run update script
3. Verify template in database
4. Test template rendering

### Phase 2: Code Changes (P1-P2)
1. Update ResendTicketEmailCommandHandler
2. Fix payment success page cache timing
3. Deploy to staging
4. Run integration tests

### Phase 3: Validation & Monitoring (P3-P5)
1. Add template validation service
2. Add monitoring and alerts
3. Deploy to production
4. Monitor for 24 hours

### Rollback Triggers
- Template rendering failures increase
- Email send rate drops below 90%
- User reports of missing emails
- 400 errors on resend endpoint

### Rollback Steps
1. Restore database template from backup
2. Revert code changes
3. Restart application services
4. Verify email sending works

---

## Success Metrics

**Immediate Success:**
- [ ] 0 template rendering errors in logs
- [ ] Email send success rate > 99%
- [ ] Payment success page shows correct amounts
- [ ] Resend email returns 200 OK

**24-Hour Success:**
- [ ] No user reports of missing emails
- [ ] No reports of wrong amounts displayed
- [ ] Template validation catches all issues
- [ ] Monitoring dashboards show healthy metrics

**Long-Term Success:**
- [ ] Template schema enforced for all templates
- [ ] Failed emails automatically retried
- [ ] Dev team alerted to template issues
- [ ] Documentation updated with parameter requirements

---

## Documentation Updates Required

1. **Email Template Schema:**
   - Document all required variables for ticket-confirmation template
   - Document conditional syntax
   - Add examples for each pricing type

2. **Developer Guide:**
   - How to update email templates
   - How to add new template variables
   - How to test template rendering

3. **Operations Runbook:**
   - How to monitor email sending
   - How to investigate failed emails
   - How to manually resend emails

---

## Conclusion

This fix plan addresses all 6 issues identified in the RCA:
1. Database template updated with Phase 6A.43 variables (P0)
2. ResendTicketEmailCommandHandler aligned with PaymentCompletedEventHandler (P1)
3. Payment success page cache timing fixed (P2)
4. Template validation added (P3)
5. Error visibility improved (P4)
6. Monitoring and alerts added (P5)

**Total Estimated Time:** 4-6 hours
**Risk Level:** Medium
**Recommended Deployment:** Staging first, then production after 24-hour monitoring
