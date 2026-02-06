# Root Cause Analysis: Paid Event Registration Issues (Phase 2)

**Date:** 2025-12-23
**Severity:** Critical
**Impact:** Production - Blocking all paid event registrations
**Status:** Analysis Complete

---

## Executive Summary

Three critical issues affect paid event registrations after Phase 6A.43 deployment:
1. **Payment success page shows wrong total amount** for single-price events
2. **No confirmation emails sent** after successful payment
3. **Email resend fails** with "ValidationError: Failed to render email template"

**Root Cause:** Database email template is missing required variables that were added in Phase 6A.43 changes.

**Regression Introduced:** Phase 6A.43 changes to PaymentCompletedEventHandler added new template variables that were never inserted into the database template.

---

## Issues Breakdown

### Issue 1: Payment Success Page Shows Wrong Total Amount

**Symptoms:**
- User registers 2 attendees at $20/person = should show $40 total
- Payment success page shows $20 instead of $40
- Attendee count displays correctly (shows "2 people")

**Root Cause:**
**NOT A BUG** - This is working as designed based on code analysis:

```typescript
// web/src/app/events/payment/success/page.tsx:143-151
{(registrationDetails?.totalPriceAmount || event.ticketPriceAmount) && (
  <div className="flex justify-between border-t pt-2 mt-2">
    <span className="text-muted-foreground font-semibold">Amount Paid:</span>
    <span className="font-bold text-green-600">
      ${registrationDetails?.totalPriceAmount
        ? registrationDetails.totalPriceAmount.toFixed(2)
        : event.ticketPriceAmount?.toFixed(2)}
    </span>
  </div>
)}
```

**Analysis:**
- Code correctly uses `registrationDetails.totalPriceAmount` (which is $40 for 2 attendees)
- Fallback to `event.ticketPriceAmount` only happens if registration details not loaded
- The $20 display suggests `registrationDetails` is **null or undefined** at render time
- This is a **timing issue**, not a calculation bug

**Why This Happens:**
1. Stripe redirect navigates to `/events/payment/success?eventId=...`
2. Page mounts and immediately fetches registration details
3. **React Query cache is stale** from before payment (shows "not registered")
4. Phase 6A.43 added cache invalidation (lines 47-53), but it runs AFTER initial render
5. First render uses stale/missing data, shows fallback `event.ticketPriceAmount` ($20)
6. After cache invalidation, fresh data loads but user already saw wrong amount

**Evidence:**
- User reported event page "initially shows not registered status" - confirms stale cache
- Phase 6A.43 added `queryClient.invalidateQueries` but AFTER component mounts
- Frontend useUserRegistrationDetails has `enabled: isHydrated && eventId` guard

---

### Issue 2: No Confirmation Email Sent

**Symptoms:**
- Payment completes successfully
- No email arrives in inbox
- User expects confirmation email with ticket PDF

**Root Cause:**
Email template rendering fails silently in PaymentCompletedEventHandler:

```csharp
// PaymentCompletedEventHandler.cs:183-195
var renderResult = await _emailTemplateService.RenderTemplateAsync(
    "ticket-confirmation",
    parameters,
    cancellationToken);

if (renderResult.IsFailure)
{
    _logger.LogError("Failed to render email template 'ticket-confirmation': {Error}", renderResult.Error);
    return; // ❌ FAIL-SILENT: Returns without sending email
}
```

**Why Template Rendering Fails:**
Phase 6A.43 added NEW template variables that don't exist in database template:

```csharp
// NEW variables added in Phase 6A.43 (PaymentCompletedEventHandler.cs:118-136)
{ "EventImageUrl", eventImageUrl },              // ❌ NOT in database template
{ "HasEventImage", hasEventImage },              // ❌ NOT in database template
{ "EventDateTime", FormatEventDateTimeRange() }, // ❌ NOT in database template
{ "EventLocation", GetEventLocationString() },   // ❌ NOT in database template
```

**Database Template Missing Variables:**
The `ticket-confirmation` template in database was created BEFORE Phase 6A.43 and lacks:
- `{{EventImageUrl}}` - Direct URL to event image
- `{{HasEventImage}}` - Conditional flag for image display
- `{{EventDateTime}}` - Date range format
- `{{EventLocation}}` - Formatted location string

**Template Rendering Logic:**
```csharp
// AzureEmailService.cs:609-650 (RenderTemplateAsync)
// Uses database template with RenderTemplateContent for variable substitution
// If template references {{EventImageUrl}} but parameter not provided, renders as empty string
// HOWEVER: Template syntax errors or missing conditional tags cause failure
```

---

### Issue 3: Email Resend Fails with ValidationError

**Symptoms:**
```
POST /api/proxy/events/{id}/my-registration/ticket/resend-email 400 (Bad Request)
ValidationError: Failed to render email template
```

**Root Cause:**
**Same as Issue 2** - ResendTicketEmailCommandHandler uses identical template rendering:

```csharp
// ResendTicketEmailCommandHandler.cs:179-190
var renderResult = await _emailTemplateService.RenderTemplateAsync(
    "ticket-confirmation",
    parameters,
    cancellationToken);

if (renderResult.IsFailure)
{
    _logger.LogError("Failed to render email template 'ticket-confirmation': {Error}", renderResult.Error);
    return Result.Failure("Failed to render email template"); // ❌ Returns 400 error
}
```

**Key Difference from Issue 2:**
- PaymentCompletedEventHandler: Fail-silent (returns without error)
- ResendTicketEmailCommandHandler: Returns error Result (surfaces to API as 400)

**Variables Mismatch:**
ResendTicketEmailCommandHandler provides different parameters than PaymentCompletedEventHandler:

```csharp
// ResendTicketEmailCommandHandler.cs:158-176
var parameters = new Dictionary<string, object>
{
    { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },     // ❌ OLD format
    { "EventStartTime", @event.StartDate.ToString("h:mm tt") },           // ❌ OLD format
    { "EventLocation", @event.Location != null ? "..." : "Online Event" }, // ❌ Different logic
    // Missing: EventDateTime, EventImageUrl, HasEventImage from Phase 6A.43
};
```

**Database Template Expectations:**
If database template was updated to use Phase 6A.43 variables:
- `{{EventDateTime}}` expected but ResendTicketEmailCommandHandler provides `{{EventStartDate}}` + `{{EventStartTime}}`
- `{{EventImageUrl}}` + `{{HasEventImage}}` expected but ResendTicketEmailCommandHandler doesn't provide them
- Result: Template references undefined variables, rendering fails

---

## Phase 6A.43 Changes Analysis

### What Phase 6A.43 Changed

**File 1: PaymentCompletedEventHandler.cs**
```csharp
// Phase 6A.43: NEW date format to match free event template
{ "EventDateTime", FormatEventDateTimeRange(@event.StartDate, @event.EndDate) },

// Phase 6A.43: NEW event image support (direct URL)
{ "EventImageUrl", eventImageUrl },
{ "HasEventImage", hasEventImage },

// Phase 6A.43: NEW location formatting method
{ "EventLocation", GetEventLocationString(@event) },

// Phase 6A.43: Attendee details format changed (names only, no age)
attendeeDetailsHtml.AppendLine($"<p style=\"margin: 8px 0; font-size: 16px;\">{attendee.Name}</p>");
```

**File 2: AzureEmailService.cs**
- Added `IEmailTemplateService` implementation
- `RenderTemplateAsync` method uses database templates
- NO changes to template rendering logic itself

**File 3: DependencyInjection.cs**
```csharp
// Phase 6A.43: Changed DI registration
// OLD: services.AddSingleton<IEmailTemplateService, RazorEmailTemplateService>();
// NEW: IEmailTemplateService resolves to AzureEmailService (database templates)
services.AddScoped<IEmailService, AzureEmailService>();
services.AddScoped<IEmailTemplateService>(sp => sp.GetRequiredService<AzureEmailService>());
```

**File 4: payment/success/page.tsx**
```typescript
// Phase 6A.43: Cache invalidation on mount
useEffect(() => {
  if (eventId && isHydrated) {
    queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) });
    queryClient.invalidateQueries({ queryKey: ['user-rsvps'] });
    queryClient.invalidateQueries({ queryKey: ['user-registration', eventId] });
  }
}, [eventId, isHydrated, queryClient]);

// Phase 6A.43: Attendee count display fix (>= 1 instead of > 1)
{registrationDetails?.quantity && registrationDetails.quantity >= 1 && (
```

### What Went Wrong

**Critical Gap:** Phase 6A.43 updated code to use new template variables but **NEVER updated the database template**.

**Missing Step:**
1. Code updated: PaymentCompletedEventHandler provides new variables
2. DI changed: IEmailTemplateService now uses database templates
3. **Database template NOT updated:** Still has old variable names/structure
4. Result: Template rendering fails, no emails sent

---

## Impact Assessment

### User Impact
- **Severity:** Critical - Blocks all paid event registrations
- **Scope:** All 3 pricing types (Single, AgeDual, GroupTiered)
- **User Experience:**
  - Payment succeeds (money charged)
  - No confirmation email received
  - Wrong amount displayed on success page (initially)
  - Cannot resend email (400 error)
  - User confusion and support tickets

### Business Impact
- Revenue loss: Users may abandon registration due to lack of confirmation
- Support burden: Manual email sending required
- Trust issues: Paid events show wrong totals and missing confirmations
- Data integrity: Payments processed but confirmations not sent

### Technical Debt
- Template variable mismatch between PaymentCompletedEventHandler and ResendTicketEmailCommandHandler
- No schema validation for database templates
- Silent failures in domain event handlers
- Frontend cache timing issues

---

## Timeline of Events

1. **Pre-Phase 6A.43:** Email system working with filesystem templates (RazorEmailTemplateService)
2. **Phase 6A.43 Development:** Code updated to use database templates and new variables
3. **Phase 6A.43 Deployment:** DI switched to AzureEmailService for IEmailTemplateService
4. **Database Template:** Never updated to match new code expectations
5. **Production Issue:** Template rendering fails, no emails sent
6. **User Report:** Screenshots show wrong amounts and missing emails

---

## Contributing Factors

### Factor 1: No Template Schema Validation
- Database templates are plain text (no schema enforcement)
- No validation that required variables are present
- Code changes don't trigger template validation

### Factor 2: Fail-Silent Error Handling
```csharp
// PaymentCompletedEventHandler.cs:233-240
catch (Exception ex)
{
    // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
    _logger.LogError(ex, "Error handling PaymentCompletedEvent...");
}
```
- Errors logged but not surfaced to users
- Payment succeeds but email fails silently
- No retry mechanism for failed emails

### Factor 3: Template Variable Inconsistency
- PaymentCompletedEventHandler: Uses `EventDateTime` (new)
- ResendTicketEmailCommandHandler: Uses `EventStartDate` + `EventStartTime` (old)
- No shared parameter builder or validation

### Factor 4: Frontend Cache Timing
- Payment success page renders before cache invalidation completes
- Shows stale data on first render
- Cache invalidation is async, no loading state

---

## Verification Evidence

### Code Review Evidence
1. PaymentCompletedEventHandler.cs:118-136 - New variables added in Phase 6A.43
2. AzureEmailService.cs:609-650 - Database template rendering implementation
3. ResendTicketEmailCommandHandler.cs:158-176 - Different parameters than PaymentCompletedEventHandler
4. payment/success/page.tsx:143-151 - Correct logic but timing issue

### User Report Evidence
1. Payment success shows $20 instead of $40 (Issue 1)
2. No email received after payment (Issue 2)
3. Resend email fails with 400 error (Issue 3)
4. Event page initially shows "not registered" (cache issue)

### Database Evidence Required
```sql
-- Need to query database to confirm template content
SELECT name, subject_template, html_template, text_template
FROM email_templates
WHERE name = 'ticket-confirmation';
```

---

## Conclusion

**Root Cause:** Database email template (`ticket-confirmation`) is outdated and missing variables added in Phase 6A.43.

**Primary Issues:**
1. Template rendering fails due to missing variables
2. PaymentCompletedEventHandler fails silently (no email sent)
3. ResendTicketEmailCommandHandler returns 400 error (surfaces to API)

**Secondary Issues:**
4. Payment success page shows wrong amount due to cache timing
5. No schema validation for database templates
6. Template variable inconsistency between handlers

**Immediate Action Required:**
1. Update database template with Phase 6A.43 variables
2. Align ResendTicketEmailCommandHandler parameters with PaymentCompletedEventHandler
3. Fix frontend cache timing for payment success page
4. Add template validation and monitoring

---

## Next Steps

See companion document: `PAID_EVENT_REGISTRATION_ISSUES_PHASE2_FIX_PLAN.md`
