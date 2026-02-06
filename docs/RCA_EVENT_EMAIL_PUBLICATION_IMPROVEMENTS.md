# Root Cause Analysis: Event Email Publication Improvements

**Date**: 2026-02-05
**Phase**: Phase 6A.98
**Status**: RCA Complete - Awaiting Approval

---

## Executive Summary

This RCA analyzes 4 requirements related to event email publication and registration UI. The analysis identifies the exact files, code locations, and categorizes each issue.

---

## Requirement 1: Change Button Text "Publish Event" to "Send Email"

### Category
**UI Issue** - Frontend text change only

### Root Cause
The button text "Publish Event" is hardcoded in the `EventNewslettersTab.tsx` component. This text is misleading as the button actually sends an email notification, not publishes the event itself.

### Affected Files
| File | Line | Current Code |
|------|------|--------------|
| [EventNewslettersTab.tsx:170](../web/src/presentation/components/features/newsletters/EventNewslettersTab.tsx#L170) | 170 | `{sendEmailMutation.isPending ? 'Sending...' : 'Publish Event'}` |

### Fix Plan
1. Change button text from `'Publish Event'` to `'Send Email'`
2. Keep the `'Sending...'` text as is (already correct)

### Impact Assessment
- **Low Risk** - Text-only change
- **No Backend Changes**
- **No Database Changes**
- **No Test Changes Required** (cosmetic change)

---

## Requirement 2: Dynamic Email Subject Based on Event Age

### Category
**Backend API Issue** - Email subject logic needs to be dynamic

### Root Cause
The email subject is hardcoded in the database template as:
```
New Event: {{EventTitle}} in {{EventCity}}, {{EventState}}
```

The `Event` entity already has a `PublishedAt` property (tracked since Phase 6A.46) that records when an event was first published. However, this property is NOT used when generating the email subject.

### Current Flow
1. User clicks "Publish Event" (now "Send Email") button
2. `EventNotificationEmailJob.ExecuteAsync()` is called
3. Email service loads template `template-event-details-publication` from database
4. Subject template is rendered with static "New Event:" prefix
5. **PublishedAt is NEVER checked** to determine event age

### Business Logic Required
```
IF event.PublishedAt is NULL OR event.PublishedAt is within 7 days from now:
    Subject prefix = "New Event:"
ELSE:
    Subject prefix = "Upcoming Event:"
```

### Affected Files
| File | Line | Description |
|------|------|-------------|
| [EventNotificationEmailJob.cs](../src/LankaConnect.Application/Events/BackgroundJobs/EventNotificationEmailJob.cs) | 315-337 | `BuildTemplateData()` method - needs to add dynamic subject prefix |
| Database: `email_templates` table | N/A | Template subject uses `{{SubjectPrefix}}` placeholder |

### Fix Plan

#### Option A: Dynamic Template Parameter (Recommended)
1. **Backend Change**: Modify `EventNotificationEmailJob.BuildTemplateData()`:
   ```csharp
   // Determine if event is "New" (within 7 days of publish) or "Upcoming"
   var isNewEvent = @event.PublishedAt == null ||
                    (DateTime.UtcNow - @event.PublishedAt.Value).TotalDays <= 7;

   data["SubjectPrefix"] = isNewEvent ? "New Event:" : "Upcoming Event:";
   ```

2. **Database Migration**: Update email template subject:
   ```sql
   UPDATE email_templates
   SET subject_template = '{{SubjectPrefix}} {{EventTitle}} in {{EventCity}}, {{EventState}}'
   WHERE name = 'template-event-details-publication';
   ```

#### Option B: Separate Templates
Create two templates: `template-new-event-publication` and `template-upcoming-event-publication`
- **Not Recommended**: Duplicates template content, harder to maintain

### Impact Assessment
- **Medium Risk** - Backend logic change + Database migration
- **Requires Migration**: Yes
- **Requires Testing**: Yes (unit test for age calculation logic)

---

## Requirement 3: Convert Toast Notification to Inline Message

### Category
**UI Issue** - Frontend notification display pattern change

### Root Cause
The success notification for email publication uses a global toast:
```typescript
// Line 90-93 in EventNewslettersTab.tsx
toast.success(
  'Event publication email queued! Check the history below for delivery status.',
  { duration: 4000 }
);
```

This toast appears at the top of the screen and can be easily missed. The user wants an inline message in the "Publication History" area instead.

### Existing Pattern
The "Event Reminders" section already has an inline processing message pattern (lines 265-278) that shows a loading state in the history area. We should follow this pattern.

### Affected Files
| File | Line | Change Required |
|------|------|-----------------|
| [EventNewslettersTab.tsx:83](../web/src/presentation/components/features/newsletters/EventNewslettersTab.tsx#L83) | N/A | Add `showPublicationProcessing` state |
| [EventNewslettersTab.tsx:87-97](../web/src/presentation/components/features/newsletters/EventNewslettersTab.tsx#L87-L97) | 87-97 | Replace toast with inline state management |
| [EventNewslettersTab.tsx:195-223](../web/src/presentation/components/features/newsletters/EventNewslettersTab.tsx#L195-L223) | 195-223 | Add inline success message in Publication History |

### Fix Plan
1. Add new state: `const [showPublicationProcessing, setShowPublicationProcessing] = useState(false);`
2. Add new state: `const [publicationMessage, setPublicationMessage] = useState<{type: 'success' | 'error', text: string} | null>(null);`
3. Modify `handleSendEmail`:
   ```typescript
   const handleSendEmail = async () => {
     setShowPublicationProcessing(true);
     setPublicationMessage(null);
     try {
       await sendEmailMutation.mutateAsync(eventId);
       setPublicationMessage({
         type: 'success',
         text: 'Email queued successfully! Check delivery status below.'
       });
     } catch (error: any) {
       setPublicationMessage({
         type: 'error',
         text: error?.message || 'Failed to send email'
       });
     } finally {
       setShowPublicationProcessing(false);
       setTimeout(() => setPublicationMessage(null), 5000);
     }
   };
   ```
4. Add inline message component in Publication History area (similar to reminder processing message pattern)

### Impact Assessment
- **Low Risk** - UI state management change
- **No Backend Changes**
- **No Database Changes**
- **Follows Existing Pattern** (reminder section)

---

## Requirement 4: Remove Note Text from Edit Registration Modal

### Category
**UI Issue** - Remove informational text

### Root Cause
The Edit Registration modal displays a note for paid registrations:
```tsx
// Lines 258-264 in EditRegistrationModal.tsx
{isPaidRegistration && (
  <span className="block mt-1 text-amber-600 dark:text-amber-400 font-medium">
    Note: Attendee details can be edited, but count is fixed.
    {canAddMoreToPaid && onAddAttendeesClick && (
      <> Use &quot;Add More Attendees&quot; below to add more.</>
    )}
  </span>
)}
```

The user wants this note text removed entirely.

### Affected Files
| File | Line | Change Required |
|------|------|-----------------|
| [EditRegistrationModal.tsx:258-265](../web/src/presentation/components/features/events/EditRegistrationModal.tsx#L258-L265) | 258-265 | Remove the entire conditional block |

### Fix Plan
1. Remove lines 258-265 (the entire `{isPaidRegistration && (...)}` block)

### Impact Assessment
- **Low Risk** - Removing informational text only
- **No Backend Changes**
- **No Database Changes**
- **No Logic Changes**

---

## Summary Table

| # | Requirement | Category | Files | Risk | DB Migration |
|---|-------------|----------|-------|------|--------------|
| 1 | Button text change | UI | EventNewslettersTab.tsx | Low | No |
| 2 | Dynamic email subject | Backend | EventNotificationEmailJob.cs, DB Migration | Medium | Yes |
| 3 | Toast to inline message | UI | EventNewslettersTab.tsx | Low | No |
| 4 | Remove note text | UI | EditRegistrationModal.tsx | Low | No |

---

## Implementation Order

Recommended order based on dependencies and risk:

1. **Requirement 4** (Remove note text) - Simplest, no dependencies
2. **Requirement 1** (Button text) - Simple UI change
3. **Requirement 3** (Inline message) - UI pattern change, follows existing pattern
4. **Requirement 2** (Dynamic subject) - Most complex, requires backend + migration

---

## Test Plan

### Requirement 1 & 4
- Visual inspection only (cosmetic changes)

### Requirement 2
- **Unit Test**: `EventNotificationEmailJob` - test SubjectPrefix logic
  - Event with PublishedAt = null → "New Event:"
  - Event with PublishedAt = 3 days ago → "New Event:"
  - Event with PublishedAt = 8 days ago → "Upcoming Event:"
  - Event with PublishedAt = 30 days ago → "Upcoming Event:"

### Requirement 3
- Manual test: Click "Send Email" and verify inline message appears in Publication History area
- Verify message auto-dismisses after 5 seconds

---

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Email template migration could fail | Test migration in staging first |
| Inline message could break existing UI | Follow existing pattern (reminder section) |
| Subject prefix logic edge cases | Comprehensive unit tests |

---

## Approval Required

Please review this RCA and approve before implementation begins.

**Questions for User:**
1. Is 7 days the correct threshold for "New" vs "Upcoming"? (Or should it be different?)
2. Should the inline success message auto-dismiss, or stay visible until user dismisses?
