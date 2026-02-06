# Root Cause Analysis: Paid Event Registration Issues

**Date**: 2025-12-23
**Phase**: 6A.43 Post-Deployment Analysis
**Environment**: Azure Staging
**Severity**: High - Customer-facing issues affecting paid registrations

## Executive Summary

After Phase 6A.43 deployment to Azure staging, four critical issues were identified in the paid event registration flow:

1. Payment success page shows generic "Payment Successful!" without amount or attendee count
2. Email template uses OLD format with unrendered variables ({{EventStartDate}}, {{AttendeeCount}})
3. Registration state shows "not registered" after payment until page refresh
4. Email missing attendee demographic details (Age Category and Gender)

**Root Causes Identified**:
- **Issue 1**: Frontend missing attendee count display logic
- **Issue 2**: Database template correct BUT PaymentCompletedEventHandler using wrong parameter names
- **Issue 3**: React Query cache not invalidated after Stripe redirect
- **Issue 4**: Design decision - template intentionally excludes demographics for privacy/simplicity

---

## Issue 1: Payment Success Page Missing Payment Details

### Symptoms
- Page shows "Payment Successful!" header
- Event details displayed (title, date, time)
- NO amount paid shown
- NO attendee count shown

### Root Cause Analysis

**Location**: `web/src/app/events/payment/success/page.tsx`

**Finding**: The payment success page DOES fetch and display amount paid:

```typescript
// Lines 129-138
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

**However**, attendee count display is missing:

```typescript
// Lines 140-145 - This section exists
{registrationDetails?.quantity && registrationDetails.quantity > 1 && (
  <div className="flex justify-between">
    <span className="text-muted-foreground">Attendees:</span>
    <span className="font-medium">{registrationDetails.quantity} person(s)</span>
  </div>
)}
```

**Actual Problem**: The condition `registrationDetails.quantity > 1` means:
- Single attendee (quantity=1): NOT shown ❌
- Multiple attendees (quantity>1): Shown ✅

### Why This Happened
Phase 6A.24 comment says "Show attendee count if available" but the implementation only shows for multi-attendee registrations.

### Impact
- Users who paid for 1 ticket see NO attendee count confirmation
- Creates confusion about what they paid for
- UX inconsistency between single and multi-attendee registrations

### Fix Required
Change condition from `> 1` to `>= 1` or remove the quantity check entirely.

---

## Issue 2: Email Shows OLD Format with Unrendered Variables

### Symptoms
Email received shows:
- "Your Event Ticket" (blue header) instead of "Registration Confirmed!" (gradient)
- Variables like `{{EventStartDate}}`, `{{EventStartTime}}`, `{{AttendeeCount}}` NOT replaced
- Old template design instead of new gradient design from Phase 6A.43

### Root Cause Analysis - THE CRITICAL DISCOVERY

**Database Template**: ✅ CORRECT - Updated on 2025-12-23 15:19:33 with new design
```javascript
// Template verification (from apply-ticket-template.js output):
{
  "name": "ticket-confirmation",
  "updated_at": "2025-12-23T15:19:33.804Z",
  "html_length": 13834,
  "has_850_width": true,
  "has_gradient": true,
  "has_datetime_range": true,          // Uses {{EventDateTime}}
  "has_conditional_attendees": true,   // Uses {{HasAttendeeDetails}}
  "has_payment_section": true,         // Uses {{AmountPaid}}
  "has_ticket_section": true,          // Uses {{TicketCode}}
  "has_attendee_count": false,         // ❌ Does NOT use {{AttendeeCount}}
  "has_start_date": false,             // ❌ Does NOT use {{EventStartDate}}
  "has_age_category": false,           // ❌ Does NOT use {{AgeCategory}}
  "has_gender": false                  // ❌ Does NOT use {{Gender}}
}
```

**Email Handler Code**: ❌ MISMATCH - Passing WRONG variable names

**Location**: `src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs`

```csharp
// Lines 118-136 - Parameters being passed
var parameters = new Dictionary<string, object>
{
    { "UserName", recipientName },
    { "EventTitle", @event.Title.Value },

    // ✅ CORRECT: Template uses {{EventDateTime}}
    { "EventDateTime", FormatEventDateTimeRange(@event.StartDate, @event.EndDate) },

    // ✅ CORRECT: Template uses {{EventLocation}}
    { "EventLocation", GetEventLocationString(@event) },

    { "RegistrationDate", domainEvent.PaymentCompletedAt.ToString("MMMM dd, yyyy h:mm tt") },

    // ✅ CORRECT: Template uses {{Attendees}} (formatted HTML)
    { "Attendees", attendeeDetailsHtml.ToString().TrimEnd() },
    { "HasAttendeeDetails", hasAttendeeDetails },

    // ✅ CORRECT: Payment and ticket details
    { "AmountPaid", domainEvent.AmountPaid.ToString("C") },
    { "PaymentIntentId", domainEvent.PaymentIntentId },
    { "PaymentDate", domainEvent.PaymentCompletedAt.ToString("MMMM dd, yyyy h:mm tt") }
};
```

**THE SMOKING GUN**: User says they received email with `{{EventStartDate}}`, `{{EventStartTime}}`, `{{AttendeeCount}}`.

**This means**:
1. The NEW database template does NOT contain these variables ✅
2. The OLD template DID contain these variables ❌
3. User received email from OLD template before deployment

**CRITICAL QUESTION**: Where is the old template being loaded from?

### Template Rendering Flow

**Phase 6A.34 Fix**: `AzureEmailService.SendTemplatedEmailAsync` was updated to render directly from database:

```csharp
// Lines 121-140 in AzureEmailService.cs
// Get template from database (Phase 6A.34 Fix: Use database template directly for rendering)
var template = await _emailTemplateRepository.GetByNameAsync(templateName, cancellationToken);

// Phase 6A.34 Fix: Render template directly from database content
var subject = RenderTemplateContent(template.SubjectTemplate.Value, parameters);
var htmlBody = RenderTemplateContent(template.HtmlTemplate ?? string.Empty, parameters);
var textBody = RenderTemplateContent(template.TextTemplate, parameters);
```

**However**: The `PaymentCompletedEventHandler` calls a DIFFERENT method:

```csharp
// Lines 186-189 in PaymentCompletedEventHandler.cs
var renderResult = await _emailTemplateService.RenderTemplateAsync(
    "ticket-confirmation",
    parameters,
    cancellationToken);
```

**IEmailTemplateService Implementation**: `RazorEmailTemplateService.cs`

```csharp
// Lines 189-216 - Template loading logic
public async Task<bool> TemplateExistsAsync(string templateName, ...)
{
    var templatePath = GetTemplatePath(templateName);      // .cshtml
    var subjectPath = GetSubjectPath(templateName);        // -subject.txt
    var textPath = GetTextBodyPath(templateName);          // -text.txt
    var htmlPath = GetHtmlBodyPath(templateName);          // -html.html

    // At minimum, we need either the main template file or separate files
    var templateExists = File.Exists(templatePath) ||
                       (File.Exists(subjectPath) && (File.Exists(textPath) || File.Exists(htmlPath)));
}

// Lines 302-316 - File-based template loading
if (File.Exists(subjectPath))
{
    subject = await RenderTemplateFileAsync(subjectPath, data, cancellationToken);
}

if (File.Exists(htmlPath))
{
    htmlBody = await RenderTemplateFileAsync(htmlPath, data, cancellationToken);
}
```

### THE ROOT CAUSE

**PaymentCompletedEventHandler uses `_emailTemplateService.RenderTemplateAsync`**:
- This calls `RazorEmailTemplateService`
- Which reads from FILESYSTEM (ticket-confirmation-html.html)
- NOT from database (communications.email_templates table)

**WHEREAS `RegistrationConfirmedEventHandler` (for free events) uses**:
- Direct database template loading via AzureEmailService.SendTemplatedEmailAsync
- Parameters match template exactly

### Evidence

1. **Database template**: Updated successfully, no old variables
2. **File-based template**: Likely contains old format with {{EventStartDate}}, {{AttendeeCount}}
3. **Deployment**: Azure App Service likely has old template files in filesystem
4. **Two rendering paths**:
   - Paid events → RazorEmailTemplateService → Filesystem ❌
   - Free events → AzureEmailService → Database ✅

### Why This Happened

Phase 6A.34 fixed AzureEmailService to use database templates, but PaymentCompletedEventHandler was added in Phase 6A.24 using the IEmailTemplateService interface, which points to the FILE-BASED RazorEmailTemplateService.

The template was updated in the DATABASE but not in the FILESYSTEM.

### Impact
- Users receive OLD email format with broken variable placeholders
- Professional appearance damaged
- Template versioning inconsistency between storage mechanisms

### Fix Required
**Option 1 (Quick Fix)**: Update filesystem template files in Azure deployment
**Option 2 (Proper Fix)**: Change PaymentCompletedEventHandler to use database template rendering like free events
**Option 3 (Best Fix)**: Deprecate RazorEmailTemplateService and use database-only templates

---

## Issue 3: Registration State Inconsistency After Payment

### Symptoms
- After payment success, navigate back to event page
- Shows "user not registered" state
- Only after manual page refresh does it show "registered" state

### Root Cause Analysis

**Location**: `web/src/app/events/payment/success/page.tsx` and `web/src/presentation/hooks/useEvents.ts`

**Payment Success Flow**:
1. User completes payment on Stripe Checkout
2. Stripe redirects to `/events/payment/success?eventId={id}`
3. Payment success page loads
4. PaymentCompletedEventHandler processes webhook (asynchronous)
5. User clicks "View Event Details" button
6. Navigates to `/events/{id}` via `router.push()`

**The Problem**: React Query cache is NOT invalidated during this flow.

**Event Detail Page Cache Logic**:

```typescript
// Lines 52-54 in events/[id]/page.tsx
const { data: userRsvp, isLoading: isLoadingRsvp } = useUserRsvpForEvent(
    (user?.userId && isHydrated) ? id : undefined
);

// Lines 59-62
const { data: registrationDetails, isLoading: isLoadingRegistration } = useUserRegistrationDetails(
    (user?.userId && isHydrated) ? id : undefined,
    !!userRsvp // Fetch details whenever userRsvp exists
);
```

**Cache Keys**:
- `['user-rsvps']` - All user RSVPs
- `['user-registration', eventId]` - Specific event registration details

**When ARE these invalidated?**

```typescript
// Lines 426-438 in useEvents.ts - useRsvpToEvent hook
onSuccess: (_data, variables) => {
  // Phase 6A.25 Fix: Invalidate all relevant caches after successful RSVP
  queryClient.invalidateQueries({ queryKey: eventKeys.detail(variables.eventId) });
  queryClient.invalidateQueries({ queryKey: ['user-rsvps'] });
  queryClient.invalidateQueries({ queryKey: ['user-registration', variables.eventId] });
},
```

**CRITICAL**: This `onSuccess` callback runs when `useRsvpToEvent` mutation succeeds.

**Payment Flow**:
1. User submits RSVP → Mutation → Cache invalidated ✅
2. Stripe redirect happens
3. Payment success page loads (NEW page, NEW React Query client instance)
4. User navigates to event detail page
5. Event detail page uses STALE cache from before redirect ❌

**Why This Happens**:
- Stripe checkout happens on external domain (checkout.stripe.com)
- Full page navigation, React state is lost
- When user returns, React Query rehydrates from browser cache
- Browser cache was populated BEFORE payment completed
- No invalidation happened because the success wasn't a mutation result

**Evidence**:

```typescript
// Lines 32-39 in success/page.tsx
const { data: event, isLoading, error } = useEventById(eventId || undefined);

const { data: registrationDetails, isLoading: isLoadingRegistration } = useUserRegistrationDetails(
    (user?.userId && isHydrated && eventId) ? eventId : undefined,
    true // User is registered (they just completed payment)
);
```

The success page DOES fetch registration details with `isUserRegistered=true`, but this creates a NEW query result that's NOT shared with the event detail page's cache.

### Impact
- Confusing UX - user sees "not registered" after paying
- Requires manual refresh to see correct state
- Undermines confidence in payment completion

### Fix Required
When navigating from success page to event detail page, need to:
1. Invalidate React Query cache for event and registration
2. Force refetch on navigation
3. Consider using router.push with `{ scroll: false }` and triggering refetch

Alternative: Use server-side redirect or prefetch pattern.

---

## Issue 4: Email Missing Attendee Demographics (Age/Gender)

### Symptoms
Email doesn't show:
- Age Category (Adult/Child) for each attendee
- Gender status for each attendee

### Root Cause Analysis

**Is this a BUG or a DESIGN DECISION?**

**Evidence**:

1. **Domain Model** (AttendeeDetails.cs):
```csharp
public class AttendeeDetails : ValueObject
{
    public string Name { get; }
    public AgeCategory AgeCategory { get; }  // Adult or Child
    public Gender? Gender { get; }            // Male, Female, Other (optional)
}
```

2. **PaymentCompletedEventHandler** (Lines 98-108):
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

**THE COMMENT SAYS IT ALL**: "names only (no age) to match free event template"

3. **Template Design** (apply-ticket-template.js):
```html
<!-- Registered Attendees Card (conditional) -->
{{#HasAttendeeDetails}}
<table role="presentation">
    <h3>Registered Attendees</h3>
    <div>
        {{Attendees}}  <!-- Just names, no demographics -->
    </div>
</table>
{{/HasAttendeeDetails}}
```

### Design Rationale (Inferred)

**Why names only?**
1. **Consistency**: Free event template shows names only
2. **Privacy**: Age and gender are sensitive demographic data
3. **Simplicity**: Clean email design without clutter
4. **Mobile-friendly**: Less information = better mobile experience

**But attendee data IS collected**:
- Frontend form collects name, age category, gender
- Database stores this data in AttendeeDetails value object
- Used for event organizer reporting and management

### Is This Actually a Problem?

**Arguments FOR including demographics**:
- User paid for specific ticket types (adult vs. child pricing)
- Confirmation should show what was purchased
- Transparency in pricing breakdown

**Arguments AGAINST**:
- Email is just confirmation, not invoice
- Sensitive data in email could be privacy risk
- Organizer has full details in backend

### Impact
- Currently working as designed per Phase 6A.43 comments
- User may want to verify they registered correct attendee types
- Not a bug, but potentially incomplete feature

### Fix Required (If Needed)
If demographics SHOULD be shown:

1. Update attendee HTML formatting:
```csharp
attendeeDetailsHtml.AppendLine(
    $"<p>{attendee.Name} ({attendee.AgeCategory}" +
    (attendee.Gender.HasValue ? $", {attendee.Gender.Value}" : "") +
    ")</p>"
);
```

2. Update template to explain demographics:
```html
<h3>Registered Attendees</h3>
<p style="font-size: 14px; color: #6b7280;">
    Below are the attendees registered for this event:
</p>
{{Attendees}}
```

---

## Summary of Fixes Needed

### Priority 1 (Critical - Customer-Facing)

**Fix 2.1: Email Template Rendering Path**
- **File**: PaymentCompletedEventHandler.cs
- **Change**: Switch from IEmailTemplateService (filesystem) to direct database rendering
- **Impact**: Ensures latest template design is used
- **Effort**: 2 hours (requires testing)

**Fix 1.1: Payment Success Attendee Count**
- **File**: web/src/app/events/payment/success/page.tsx
- **Change**: Show attendee count for quantity >= 1 (not just > 1)
- **Impact**: Better confirmation UX
- **Effort**: 15 minutes

### Priority 2 (Important - UX Issue)

**Fix 3.1: Registration State Cache Invalidation**
- **File**: web/src/app/events/payment/success/page.tsx
- **Change**: Invalidate React Query cache before navigating to event detail
- **Impact**: Correct registration state shown immediately
- **Effort**: 30 minutes

### Priority 3 (Enhancement - Feature Request)

**Fix 4.1: Attendee Demographics in Email (Optional)**
- **File**: PaymentCompletedEventHandler.cs
- **Change**: Include age category and gender in attendee list
- **Impact**: More complete confirmation details
- **Effort**: 1 hour
- **Decision Required**: Product owner approval needed

---

## Recommended Fix Order

1. **Fix 2.1 first** - This is the most critical issue affecting production emails
2. **Fix 1.1 second** - Quick win for better UX
3. **Fix 3.1 third** - Important UX improvement
4. **Fix 4.1 last** - Only if product requirements dictate

---

## Additional Recommendations

### Testing Strategy
1. **Email Template Testing**: Create automated tests comparing database vs. filesystem templates
2. **Payment Flow Testing**: E2E test for Stripe payment → email → registration state
3. **Cache Invalidation Testing**: Verify React Query cache behavior after external redirects

### Monitoring
1. Log which template source is used (database vs. filesystem)
2. Track cache hit/miss rates for registration queries
3. Monitor email delivery and variable rendering success rates

### Documentation
1. Update architecture docs to clarify template storage strategy
2. Document React Query cache invalidation patterns for payment flows
3. Create ADR for email template demographics inclusion decision

---

## Appendix: Technical Details

### Template Rendering Comparison

| Aspect | RazorEmailTemplateService | AzureEmailService Database Rendering |
|--------|---------------------------|--------------------------------------|
| Source | Filesystem files | PostgreSQL database |
| Update Process | Deploy new files | Run SQL script |
| Version Control | Git repository | Database migrations |
| Cache Strategy | MemoryCache with file hash | Database query cache |
| Deployment Risk | File sync required | Single source of truth |
| Current Usage | PaymentCompleted (paid) | RegistrationConfirmed (free) |

### React Query Cache Lifecycle

```
User Flow:                    Cache State:
────────────────────────────  ───────────────────────────
1. RSVP form submit          ['user-rsvps']: STALE
2. Mutation success          ['user-rsvps']: INVALIDATED
3. Stripe redirect           [CACHE PERSISTED TO BROWSER]
4. Payment completed         [BACKEND WEBHOOK]
5. Return to app             ['user-rsvps']: REHYDRATED (stale)
6. Navigate to event         ['user-rsvps']: USED (incorrect!)
7. Manual refresh            ['user-rsvps']: REFETCHED (correct)
```

### Database Template Verification Query

```sql
SELECT
    name,
    updated_at,
    LENGTH(html_template) as html_length,
    html_template LIKE '%EventDateTime%' as uses_datetime,
    html_template LIKE '%EventStartDate%' as uses_old_format,
    html_template LIKE '%AttendeeCount%' as uses_attendee_count
FROM communications.email_templates
WHERE name = 'ticket-confirmation';
```

**Current Result** (2025-12-23 15:19):
- ✅ Uses `{{EventDateTime}}` (new format)
- ❌ Does NOT use `{{EventStartDate}}` (old format)
- ❌ Does NOT use `{{AttendeeCount}}` (not in new design)

---

**Document prepared by**: System Architect
**Review required**: Backend Lead, Frontend Lead, Product Owner
**Next Steps**: Create fix plan and estimate implementation timeline
