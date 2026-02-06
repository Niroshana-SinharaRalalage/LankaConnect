# Root Cause Analysis: Resend Ticket Email Toast to Inline Message

**Date**: 2026-02-05
**Phase**: Phase 6A.100
**Status**: RCA Complete - Awaiting Approval

---

## Executive Summary

User requested converting the "Failed to resend ticket email. Please try again." toast notification to an inline message, consistent with the Phase 6A.98+ changes made to publication and reminder emails.

---

## Issue Analysis

### Category
**UI Issue** - Feature Request (Consistency Improvement)

### Current Behavior
When clicking "Resend Confirmation Email" button on the Your Ticket page:
- **Success**: Button text changes to "Email Sent!" with green checkmark for 5 seconds
- **Error**: A toast notification appears at top of page with error message

### Evidence
File: `web/src/presentation/components/features/events/TicketSection.tsx`

```typescript
// Line 90-116: Current handleResendEmail function
const handleResendEmail = async () => {
  if (isResending) return;

  try {
    setIsResending(true);
    setResendSuccess(false);
    await eventsRepository.resendTicketEmail(eventId);
    setResendSuccess(true);
    // Clear success message after 5 seconds
    setTimeout(() => setResendSuccess(false), 5000);
  } catch (err: unknown) {
    console.error('Failed to resend email:', err);
    // ...error extraction logic...
    toast.error(errorMessage);  // <-- THIS IS THE TOAST
  } finally {
    setIsResending(false);
  }
};
```

### Problem Statement
1. Toast notifications appear at top of screen, far from the button clicked
2. Toast auto-dismisses, user might miss the error message
3. Inconsistent with Phase 6A.98+ changes to publication/reminder emails which now use inline messages

---

## Root Cause

**UI/UX Inconsistency** - The TicketSection component was not updated as part of Phase 6A.98+ when publication and reminder toasts were converted to inline messages.

---

## Backend Analysis

The backend API is functioning correctly. Errors can occur due to:
1. Email service rate limiting (Resend API)
2. Invalid email address
3. Ticket not found
4. Payment not completed

The backend already returns meaningful error messages (Phase 6A.98 enhancement in lines 352-363 of `ResendTicketEmailCommandHandler.cs`).

---

## Fix Plan

### Approach
Convert the `toast.error()` call to an inline message displayed near the "Resend Confirmation Email" button, following the same pattern used in `EventNewslettersTab.tsx` for publication/reminder emails.

### Changes Required

| File | Change |
|------|--------|
| `TicketSection.tsx` | Add `resendMessage` state, replace toast with inline message |

### Implementation Details

1. **Add state variable for inline message**:
   ```typescript
   const [resendMessage, setResendMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);
   ```

2. **Modify handleResendEmail to use inline message**:
   - On success: Set success message (in addition to button state change)
   - On error: Set error message instead of toast

3. **Add inline message display in Actions area**:
   - Display message below the action buttons
   - Green background for success, red for error
   - Auto-dismiss after 5-8 seconds

### Visual Design

The inline message will appear below the action buttons:
```
[Download Ticket]  [Resend Confirmation Email]

┌─────────────────────────────────────────────┐
│ ✓ Confirmation email sent successfully!     │  <- Green for success
│   Check your inbox.                         │
└─────────────────────────────────────────────┘

OR

┌─────────────────────────────────────────────┐
│ ✗ Failed to resend ticket email.            │  <- Red for error
│   Please try again later.                   │
└─────────────────────────────────────────────┘
```

---

## Impact Assessment

- **Low Risk** - UI state management change only
- **No Backend Changes Required**
- **Follows Existing Pattern** (EventNewslettersTab.tsx)
- **Improves UX Consistency** across the application

---

## Files to Modify

1. `web/src/presentation/components/features/events/TicketSection.tsx`
   - Add `resendMessage` state
   - Modify `handleResendEmail` function
   - Add inline message display component

---

## Checklist Before Implementation

- [x] RCA Complete
- [x] Fix plan approved by system-architect
- [ ] User approval for implementation

---

## Questions for User

1. Should I proceed with implementing this inline message change?
2. Should the success message be shown BOTH as button text change AND inline message, or just inline?
