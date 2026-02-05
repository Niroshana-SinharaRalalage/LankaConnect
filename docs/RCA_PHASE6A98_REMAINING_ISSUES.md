# Root Cause Analysis: Phase 6A.98 Remaining Issues

**Date**: 2026-02-05
**Phase**: Phase 6A.98 (Follow-up)
**Status**: RCA Complete - Awaiting Approval

---

## Executive Summary

Two issues were reported after the initial Phase 6A.98 deployment:
1. Reminder functionality still uses toast notifications instead of inline messages
2. Email subject still shows "New Event:" instead of "Upcoming Event:" for older events

---

## Issue 1: Toast Messages Still Appear for Event Reminders

### Category
**UI Issue** - Feature Gap (Incomplete implementation)

### Root Cause
The initial Phase 6A.98 implementation only converted the **publication email** toast to an inline message. The **reminder functionality** still has 4 toast calls that were NOT converted.

### Evidence
File: `web/src/presentation/components/features/newsletters/EventNewslettersTab.tsx`

```typescript
// Line 118-121: Toast for no registrations
toast.success('No registrations found for this event. Reminder not sent.', { duration: 4000 });

// Line 124-127: Toast for duplicate reminder (THE ONE IN SCREENSHOT)
toast('All attendees have already received this reminder. To avoid duplicate emails, the reminder was not sent again.', {
  icon: 'ℹ️',
  duration: 5000,
});

// Line 129-132: Toast for success
toast.success(`Reminder queued for ${result.recipientCount} attendee${result.recipientCount === 1 ? '' : 's'}!`, { duration: 4000 });

// Line 135: Toast for error
toast.error(error?.message || 'Failed to send reminder');
```

### Fix Plan
1. Add new state variables for reminder processing message (similar to publication):
   ```typescript
   const [reminderMessage, setReminderMessage] = useState<{ type: 'success' | 'error' | 'info'; text: string } | null>(null);
   ```

2. Modify `handleSendReminder` to use inline messages instead of toasts

3. Add inline message display in "Reminder Send History" area (currently has the processing spinner but no result message)

### Files to Modify
| File | Change |
|------|--------|
| `EventNewslettersTab.tsx` | Replace 4 toast calls with inline message state |

### Impact Assessment
- **Low Risk** - UI state management change
- **No Backend Changes**
- **Follows Existing Pattern** (publication messages)

---

## Issue 2: Email Subject Still Shows "New Event:" Instead of "Upcoming Event:"

### Category
**Database Issue** OR **Migration Not Applied**

### Root Cause Analysis

**Possible Cause 1: Migration Not Applied**
The migration `20260205200000_Phase6A98_DynamicEmailSubjectPrefix.cs` may not have been applied to the database. The migration updates:
```sql
UPDATE communications.email_templates
SET subject_template = '{{SubjectPrefix}} {{EventTitle}} in {{EventCity}}, {{EventState}}'
WHERE name = 'template-event-details-publication';
```

**Evidence Supporting This Theory:**
- The email screenshot shows: `New Event: Monthly Dana January 2026 in Aurora, OH`
- This event was published in early January (event date shows January 2026)
- If the migration was applied correctly and SubjectPrefix is being passed, it should show "Upcoming Event:"

**Possible Cause 2: Migration Applied But Template Not Using {{SubjectPrefix}}**
The template might still have the hardcoded "New Event:" subject.

### Verification Steps Needed
1. Check if migration exists in `__EFMigrationsHistory` table
2. Query current `subject_template` value for `template-event-details-publication`
3. Check container startup logs for migration application

### Backend Code Status
The backend code IS correct. In `EventNotificationEmailJob.cs` (lines 313-317):
```csharp
// Phase 6A.98: Determine if event is "New" (within 7 days of publish) or "Upcoming"
var isNewEvent = @event.PublishedAt == null ||
                 (DateTime.UtcNow - @event.PublishedAt.Value).TotalDays <= 7;
var subjectPrefix = isNewEvent ? "New Event:" : "Upcoming Event:";
```

And the SubjectPrefix is added to template data (line 337):
```csharp
{ "SubjectPrefix", subjectPrefix }  // Phase 6A.98: Dynamic subject prefix
```

### Fix Plan
**If migration not applied:**
1. Manually apply the SQL to update the template subject:
   ```sql
   UPDATE communications.email_templates
   SET subject_template = '{{SubjectPrefix}} {{EventTitle}} in {{EventCity}}, {{EventState}}'
   WHERE name = 'template-event-details-publication';
   ```

**If migration applied but issue persists:**
1. Verify the template rendering is using {{SubjectPrefix}} correctly
2. Add debug logging to see what SubjectPrefix value is being passed

### Impact Assessment
- **Medium Risk** - Database update required
- **No Code Changes** (backend code is correct)
- **Requires DB Verification**

---

## Summary Table

| # | Issue | Category | Root Cause | Status |
|---|-------|----------|------------|--------|
| 1 | Reminder toast messages | UI Feature Gap | handleSendReminder uses toast, not inline | Needs fix |
| 2 | Email subject still "New Event:" | DB Migration Issue | Migration may not be applied | Needs verification |

---

## Recommended Next Steps

1. **Issue 1**: Implement inline messages for reminder functionality (UI change only)

2. **Issue 2**:
   - First: Manually verify database state via SQL query
   - If migration not applied: Apply SQL directly to staging database
   - If already applied: Debug template rendering

---

## Questions Before Implementation

1. For **Issue 1**: Should I proceed with converting all 4 reminder toasts to inline messages?

2. For **Issue 2**: Do you want me to apply the SQL directly to fix the template subject, or should we investigate further first?
