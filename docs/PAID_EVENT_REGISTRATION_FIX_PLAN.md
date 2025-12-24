# Fix Plan: Paid Event Registration Issues

**Date**: 2025-12-23
**Reference**: PAID_EVENT_REGISTRATION_ISSUES_RCA.md
**Target**: Phase 6A.44
**Environment**: Azure Staging → Production

## Overview

This document provides a detailed implementation plan for fixing the four issues identified in paid event registration flow after Phase 6A.43 deployment.

---

## Fix 1: Email Template Rendering - Use Database Template

**Priority**: CRITICAL
**Issue**: PaymentCompletedEventHandler uses filesystem templates (old format) instead of database templates (new format)
**Effort**: 2 hours
**Risk**: Medium (requires careful testing of template rendering)

### Current State

```csharp
// PaymentCompletedEventHandler.cs (Lines 186-189)
var renderResult = await _emailTemplateService.RenderTemplateAsync(
    "ticket-confirmation",
    parameters,
    cancellationToken);
```

- Uses `IEmailTemplateService` → `RazorEmailTemplateService`
- Reads from filesystem: `ticket-confirmation-html.html`
- Filesystem template is outdated (contains {{EventStartDate}}, {{AttendeeCount}})

### Target State

Match the approach used in `RegistrationConfirmedEventHandler` (free events):

```csharp
// Use AzureEmailService directly to render from database
var renderResult = await _emailTemplateService.RenderTemplateAsync(
    "ticket-confirmation",
    parameters,
    cancellationToken);
```

BUT ensure IEmailTemplateService implementation uses database, not filesystem.

### Implementation Steps

#### Step 1.1: Update AzureEmailService to Implement IEmailTemplateService

**File**: `src/LankaConnect.Infrastructure/Email/Services/AzureEmailService.cs`

**Current**: AzureEmailService has `SendTemplatedEmailAsync` that reads from database

**Change**: Add `RenderTemplateAsync` method to implement IEmailTemplateService interface

```csharp
// Add this method to AzureEmailService
public async Task<Result<RenderedEmailTemplate>> RenderTemplateAsync(
    string templateName,
    Dictionary<string, object> parameters,
    CancellationToken cancellationToken = default)
{
    try
    {
        // Get template from database
        var template = await _emailTemplateRepository.GetByNameAsync(templateName, cancellationToken);
        if (template == null)
        {
            return Result<RenderedEmailTemplate>.Failure($"Email template '{templateName}' not found");
        }

        if (!template.IsActive)
        {
            return Result<RenderedEmailTemplate>.Failure($"Email template '{templateName}' is not active");
        }

        // Render template directly from database content
        var subject = RenderTemplateContent(template.SubjectTemplate.Value, parameters);
        var htmlBody = RenderTemplateContent(template.HtmlTemplate ?? string.Empty, parameters);
        var textBody = RenderTemplateContent(template.TextTemplate, parameters);

        return Result<RenderedEmailTemplate>.Success(new RenderedEmailTemplate
        {
            Subject = subject,
            HtmlBody = htmlBody,
            PlainTextBody = textBody
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to render template '{TemplateName}' from database", templateName);
        return Result<RenderedEmailTemplate>.Failure($"Failed to render template: {ex.Message}");
    }
}
```

#### Step 1.2: Update DI Registration

**File**: `src/LankaConnect.API/Program.cs` or dependency injection setup

**Current**:
```csharp
services.AddScoped<IEmailTemplateService, RazorEmailTemplateService>();
```

**Change**:
```csharp
// Use AzureEmailService for both IEmailService and IEmailTemplateService
services.AddScoped<AzureEmailService>();
services.AddScoped<IEmailService>(sp => sp.GetRequiredService<AzureEmailService>());
services.AddScoped<IEmailTemplateService>(sp => sp.GetRequiredService<AzureEmailService>());
```

**Rationale**: Single source of truth for email templates (database)

#### Step 1.3: Add Method Signatures to IEmailTemplateService

**File**: `src/LankaConnect.Application/Common/Interfaces/IEmailTemplateService.cs`

Verify interface has all required methods:
- ✅ `RenderTemplateAsync` - Already exists
- ✅ `GetAvailableTemplatesAsync` - Already exists
- ✅ `GetTemplateInfoAsync` - Already exists
- ✅ `ValidateTemplateParametersAsync` - Already exists

AzureEmailService needs to implement these methods for database templates.

#### Step 1.4: Testing

**Test Cases**:
1. ✅ Paid event registration → Payment success → Email sent
2. ✅ Email uses NEW gradient design (not old blue header)
3. ✅ Variables properly rendered: {{EventDateTime}}, {{AmountPaid}}, {{TicketCode}}
4. ✅ No unrendered variables in email ({{EventStartDate}}, {{AttendeeCount}})
5. ✅ Attendee names displayed correctly
6. ✅ Payment details shown correctly

**Regression Testing**:
1. ✅ Free event registration still works (uses same service)
2. ✅ Other email templates still work

### Deployment Checklist

- [ ] Code changes merged to develop branch
- [ ] Unit tests added for database template rendering
- [ ] Integration tests pass
- [ ] Deploy to Azure staging
- [ ] Manual test: Complete paid registration and verify email
- [ ] Monitor logs for template rendering errors
- [ ] Deploy to production

### Rollback Plan

If database template rendering fails:
1. Revert DI registration to use RazorEmailTemplateService
2. Deploy filesystem templates to Azure App Service
3. Investigate database template issue separately

---

## Fix 2: Payment Success Page - Show Attendee Count for All Quantities

**Priority**: HIGH
**Issue**: Attendee count only shown when quantity > 1, single attendee (quantity=1) not shown
**Effort**: 15 minutes
**Risk**: Low (simple conditional change)

### Current State

```typescript
// web/src/app/events/payment/success/page.tsx (Lines 140-145)
{registrationDetails?.quantity && registrationDetails.quantity > 1 && (
  <div className="flex justify-between">
    <span className="text-muted-foreground">Attendees:</span>
    <span className="font-medium">{registrationDetails.quantity} person(s)</span>
  </div>
)}
```

**Problem**: Single attendee registrations don't show attendee count.

### Target State

```typescript
{registrationDetails?.quantity && registrationDetails.quantity >= 1 && (
  <div className="flex justify-between">
    <span className="text-muted-foreground">Attendees:</span>
    <span className="font-medium">
      {registrationDetails.quantity} {registrationDetails.quantity === 1 ? 'person' : 'people'}
    </span>
  </div>
)}
```

**Better**: Use singular/plural correctly.

### Implementation

**File**: `web/src/app/events/payment/success/page.tsx`

**Change**:
```typescript
{/* Show attendee count for all registrations */}
{registrationDetails?.quantity && (
  <div className="flex justify-between">
    <span className="text-muted-foreground">Attendees:</span>
    <span className="font-medium">
      {registrationDetails.quantity} {registrationDetails.quantity === 1 ? 'person' : 'people'}
    </span>
  </div>
)}
```

### Testing

**Test Cases**:
1. ✅ Single attendee (quantity=1): Shows "1 person"
2. ✅ Two attendees (quantity=2): Shows "2 people"
3. ✅ Three+ attendees: Shows "X people"
4. ✅ No registration details: Section not shown

### Deployment

- [ ] Code change
- [ ] Test in local dev
- [ ] Deploy to staging
- [ ] Manual test payment success page
- [ ] Deploy to production

---

## Fix 3: Registration State Cache - Invalidate After Payment Redirect

**Priority**: HIGH
**Issue**: After Stripe redirect, event detail page shows "not registered" until manual refresh
**Effort**: 30 minutes
**Risk**: Medium (React Query cache management)

### Current State

**Payment Flow**:
1. User submits RSVP → Stripe Checkout redirect
2. Payment completed → Redirect to `/events/payment/success?eventId={id}`
3. User clicks "View Event Details" → Navigate to `/events/{id}`
4. Event detail page loads with STALE cache (from before payment)
5. Shows "not registered" state ❌
6. Manual refresh → Refetch → Shows "registered" ✅

### Root Cause

React Query cache persists across the Stripe redirect, but the cache was populated BEFORE payment completed.

### Target State

When navigating from success page to event detail, invalidate relevant caches to force refetch.

### Implementation

#### Option A: Invalidate Cache on Success Page Mount

**File**: `web/src/app/events/payment/success/page.tsx`

**Add**:
```typescript
import { useQueryClient } from '@tanstack/react-query';
import { eventKeys } from '@/presentation/hooks/useEvents';

function PaymentSuccessContent() {
  const queryClient = useQueryClient();
  const eventId = searchParams?.get('eventId');

  // Invalidate caches when payment success page loads
  useEffect(() => {
    if (eventId) {
      // Invalidate all event-related caches
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) });
      queryClient.invalidateQueries({ queryKey: ['user-rsvps'] });
      queryClient.invalidateQueries({ queryKey: ['user-registration', eventId] });

      console.log('[Payment Success] ✅ Invalidated event caches for', eventId);
    }
  }, [eventId, queryClient]);

  // ... rest of component
}
```

**Pros**:
- Simple implementation
- Invalidates cache immediately on success page load
- No changes needed to event detail page

**Cons**:
- Runs on every success page load (even if user doesn't navigate to event)
- Slight performance cost for unnecessary invalidation

#### Option B: Invalidate Cache Before Navigation

**File**: `web/src/app/events/payment/success/page.tsx`

**Modify** `handleViewEvent`:
```typescript
const queryClient = useQueryClient();

const handleViewEvent = () => {
  if (eventId) {
    // Invalidate caches BEFORE navigation
    queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) });
    queryClient.invalidateQueries({ queryKey: ['user-rsvps'] });
    queryClient.invalidateQueries({ queryKey: ['user-registration', eventId] });

    setIsRedirecting(true);
    router.push(`/events/${eventId}`);
  }
};
```

**Pros**:
- Only invalidates when user actually navigates
- More efficient

**Cons**:
- Doesn't help if user navigates via browser back button
- Doesn't help if user navigates via direct URL

#### Option C: Force Refetch on Event Detail Page

**File**: `web/src/app/events/[id]/page.tsx`

**Add** URL parameter detection:
```typescript
const fromPaymentSuccess = searchParams.get('from') === 'payment-success';

// Force refetch if coming from payment success
const { data: userRsvp, isLoading: isLoadingRsvp } = useUserRsvpForEvent(
  (user?.userId && isHydrated) ? id : undefined,
  {
    refetchOnMount: fromPaymentSuccess ? 'always' : true,
  }
);
```

**And update success page navigation**:
```typescript
const handleViewEvent = () => {
  if (eventId) {
    setIsRedirecting(true);
    router.push(`/events/${eventId}?from=payment-success`);
  }
};
```

**Pros**:
- Works for all navigation paths (button, back button, direct URL)
- Explicit about intent
- No unnecessary invalidation

**Cons**:
- Requires URL parameter handling
- More code changes

### Recommended Approach

**Use Option A (Invalidate on Mount)** for simplicity and reliability.

### Implementation Steps

1. Add `useQueryClient` import to success page
2. Add `useEffect` to invalidate caches when `eventId` changes
3. Test navigation flow:
   - Payment success → View Event Details → Should show "Registered" immediately
   - Payment success → Browser back → Should update correctly
   - Payment success → Direct URL navigation → Should update correctly

### Testing

**Test Cases**:
1. ✅ Complete payment → Success page loads → Click "View Event" → Shows "Registered"
2. ✅ Complete payment → Success page loads → Use back button → Shows "Registered"
3. ✅ Complete payment → Close tab → Reopen event URL → Shows "Registered"
4. ✅ Cache invalidation doesn't break other pages

### Deployment

- [ ] Code changes
- [ ] Test payment flow end-to-end
- [ ] Deploy to staging
- [ ] Manual test: Payment → Success → Event Detail (no refresh)
- [ ] Deploy to production

---

## Fix 4: Attendee Demographics in Email (OPTIONAL)

**Priority**: LOW (Enhancement)
**Issue**: Email shows attendee names only, not age category or gender
**Effort**: 1 hour
**Risk**: Low
**Decision Required**: Product owner approval

### Current State

Email shows:
```
Registered Attendees
--------------------
John Doe
Jane Doe
```

No indication of:
- Age category (Adult/Child)
- Gender (Male/Female/Other)

### Discussion

**Is this a bug or design decision?**

**Evidence from code**:
```csharp
// Phase 6A.43: Format attendee details - names only (no age) to match free event template
```

**Design rationale** (inferred):
- Consistency with free event emails
- Privacy concern (sensitive demographics)
- Simplicity for mobile-friendly design

**However**:
- Paid events have different ticket prices for adults vs. children
- User may want confirmation of attendee types they paid for
- Transparency in what was purchased

### Target State (If Approved)

Email shows:
```
Registered Attendees
--------------------
John Doe (Adult, Male)
Jane Doe (Child, Female)
```

### Implementation

#### Step 4.1: Update Attendee HTML Formatting

**File**: `src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs`

**Current** (Lines 98-108):
```csharp
// Phase 6A.43: Format attendee details - names only (no age) to match free event template
var attendeeDetailsHtml = new System.Text.StringBuilder();

if (registration.HasDetailedAttendees() && registration.Attendees.Any())
{
    foreach (var attendee in registration.Attendees)
    {
        // HTML format - names only, matching free event template style
        attendeeDetailsHtml.AppendLine($"<p style=\"margin: 8px 0; font-size: 16px;\">{attendee.Name}</p>");
    }
}
```

**Change**:
```csharp
// Phase 6A.44: Format attendee details with demographics for paid events
var attendeeDetailsHtml = new System.Text.StringBuilder();

if (registration.HasDetailedAttendees() && registration.Attendees.Any())
{
    foreach (var attendee in registration.Attendees)
    {
        var ageCategoryStr = attendee.AgeCategory.ToString(); // "Adult" or "Child"
        var genderStr = attendee.Gender.HasValue ? $", {attendee.Gender.Value}" : "";

        // HTML format with demographics
        attendeeDetailsHtml.AppendLine(
            $"<p style=\"margin: 8px 0; font-size: 16px;\">" +
            $"{attendee.Name} " +
            $"<span style=\"color: #6b7280; font-size: 14px;\">({ageCategoryStr}{genderStr})</span>" +
            $"</p>"
        );
    }
}
```

**Result**:
```html
<p>John Doe <span style="color: #6b7280;">(Adult, Male)</span></p>
<p>Jane Doe <span style="color: #6b7280;">(Child, Female)</span></p>
```

#### Step 4.2: Update Email Template Description

**File**: `scripts/apply-ticket-template.js`

Add explanation in template:
```html
<!-- Registered Attendees Card (conditional) -->
{{#HasAttendeeDetails}}
<table role="presentation">
    <h3>Registered Attendees</h3>
    <p style="font-size: 14px; color: #6b7280; margin-bottom: 12px;">
        The following attendees have been registered for this event:
    </p>
    <div>
        {{Attendees}}
    </div>
</table>
{{/HasAttendeeDetails}}
```

### Testing

**Test Cases**:
1. ✅ Single adult: Shows "John Doe (Adult)"
2. ✅ Adult with gender: Shows "John Doe (Adult, Male)"
3. ✅ Child without gender: Shows "Jane Doe (Child)"
4. ✅ Mixed attendees: All show correct demographics
5. ✅ Email rendering correct in all email clients

### Decision Points

**Questions for Product Owner**:
1. Should we show age category in email confirmation?
2. Should we show gender in email confirmation?
3. Privacy concerns with displaying demographics in email?
4. Alternative: Show demographics only in PDF ticket, not in email?

**Recommendation**: Include age category (Adult/Child) but NOT gender for privacy.

```csharp
// Recommended implementation
var ageCategoryStr = attendee.AgeCategory.ToString();
attendeeDetailsHtml.AppendLine(
    $"<p style=\"margin: 8px 0; font-size: 16px;\">" +
    $"{attendee.Name} " +
    $"<span style=\"color: #6b7280; font-size: 14px;\">({ageCategoryStr})</span>" +
    $"</p>"
);
```

### Deployment

**Only proceed if approved by product owner**:
- [ ] Product owner decision
- [ ] Code changes
- [ ] Update database template
- [ ] Deploy to staging
- [ ] User acceptance testing
- [ ] Deploy to production

---

## Implementation Timeline

### Day 1 (Morning)
- **Fix 1.1-1.2**: Update AzureEmailService and DI registration (1.5 hours)
- **Fix 1.3**: Update IEmailTemplateService implementation (30 minutes)

### Day 1 (Afternoon)
- **Fix 1.4**: Testing email template rendering (1 hour)
- **Fix 2**: Payment success page attendee count (15 minutes)
- **Fix 3**: Registration state cache invalidation (30 minutes)

### Day 2 (Morning)
- Integration testing all fixes together (2 hours)
- Deploy to Azure staging (30 minutes)
- Manual end-to-end testing (1 hour)

### Day 2 (Afternoon)
- Bug fixes from testing (1-2 hours)
- Code review and approval (1 hour)
- Deploy to production (30 minutes)

### Day 3 (If Fix 4 approved)
- **Fix 4**: Attendee demographics in email (1 hour)
- Testing and deployment (1 hour)

**Total Effort**: 2-3 days (depending on Fix 4 approval)

---

## Testing Strategy

### Unit Tests

**New Tests Required**:
1. AzureEmailService.RenderTemplateAsync with database template
2. Template variable replacement with new parameters
3. Conditional section rendering ({{#HasAttendeeDetails}})

### Integration Tests

**Test Scenarios**:
1. Complete paid event registration flow
2. Stripe payment webhook processing
3. Email sending with database template
4. React Query cache invalidation after payment

### Manual Testing Checklist

#### Email Template Verification
- [ ] Register for paid event
- [ ] Complete Stripe payment
- [ ] Check email received
- [ ] Verify email uses NEW gradient design
- [ ] Verify all variables rendered correctly
- [ ] No {{placeholder}} text visible
- [ ] Attendee names displayed
- [ ] Payment details displayed
- [ ] Ticket code displayed

#### Payment Success Page
- [ ] Payment success page loads
- [ ] Event details shown correctly
- [ ] Amount paid shown
- [ ] Attendee count shown (for quantity=1 and quantity>1)
- [ ] "View Event Details" button works

#### Registration State
- [ ] Navigate to event detail page from success page
- [ ] Shows "Registered" state immediately (no refresh needed)
- [ ] RSVP section shows correct attendee list
- [ ] Ticket download button available

#### Regression Testing
- [ ] Free event registration still works
- [ ] Other email templates still work
- [ ] Event creation/editing still works
- [ ] User profile still works

---

## Monitoring and Validation

### Production Monitoring

**After Deployment**:
1. Monitor application logs for template rendering errors
2. Check email delivery success rates
3. Monitor React Query cache performance
4. Track user feedback on email clarity

### Success Metrics

**Week 1 Post-Deployment**:
- ✅ 0 email template rendering errors
- ✅ 100% email variable replacement success
- ✅ 0 user complaints about registration state
- ✅ Reduced support tickets for "payment didn't work"

### Rollback Triggers

**Immediate Rollback If**:
- Email template rendering fails for >5% of emails
- Payment confirmation emails not sent
- Registration state cache invalidation causes performance issues
- Critical bug discovered in production

---

## Risks and Mitigation

### Risk 1: Database Template Rendering Performance

**Risk**: Database query on every email send could be slower than filesystem cache

**Mitigation**:
- Add caching layer for database templates (MemoryCache with 10-minute TTL)
- Monitor email sending latency
- Optimize database query with proper indexes

### Risk 2: DI Registration Breaking Other Services

**Risk**: Changing IEmailTemplateService implementation could break other email features

**Mitigation**:
- Comprehensive regression testing
- Staged rollout (staging first, then production)
- Monitor all email sending after deployment

### Risk 3: Cache Invalidation Side Effects

**Risk**: Aggressive cache invalidation could impact performance

**Mitigation**:
- Only invalidate necessary query keys
- Monitor React Query DevTools in staging
- Add logging for cache invalidation events

### Risk 4: Attendee Demographics Privacy

**Risk**: Showing demographics in email could violate privacy expectations (if Fix 4 implemented)

**Mitigation**:
- Get product owner approval before implementing
- Review privacy policy compliance
- Consider alternative (show in PDF only)

---

## Documentation Updates

**After Deployment**:
1. Update `Email_Notifications_System_Architecture.md` with database template decision
2. Document React Query cache invalidation patterns for payment flows
3. Create ADR for email template storage strategy
4. Update Phase 6A tracking documents

---

## Conclusion

This fix plan addresses all four identified issues with a prioritized, phased approach. The most critical issue (email template rendering) is addressed first, followed by quick wins (attendee count, cache invalidation), and finally an optional enhancement (demographics in email).

**Estimated Total Effort**: 2-3 days
**Risk Level**: Medium (requires careful testing)
**Expected Impact**: High (resolves customer-facing issues)

**Next Steps**:
1. Review this plan with team
2. Get product owner decision on Fix 4
3. Create tracking tasks in project management system
4. Begin implementation on Day 1

---

**Prepared by**: System Architect
**Review Date**: 2025-12-23
**Approval Required**: Tech Lead, Product Owner
