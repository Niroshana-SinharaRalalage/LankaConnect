# Phase 6A.15: Dashboard Cancel Registration Button

**Status:** ✅ Complete
**Session:** 30
**Date:** 2025-12-10
**Commit:** 640857d

## Overview

Added cancel registration functionality to the Dashboard's "My Registered Events" section. Users can now cancel their event registration directly from the dashboard without navigating to the event details page.

## User Story

**As a** registered event attendee
**I want to** cancel my registration from the dashboard
**So that** I don't have to navigate to the event details page to cancel

## Implementation Summary

### Frontend Changes

#### 1. EventsList Component (`web/src/presentation/components/features/dashboard/EventsList.tsx`)

**Added:**
- `onCancelClick?: (eventId: string) => Promise<void>` prop
- `cancellingEventId` state to track which event is being cancelled
- `handleCancelClick` function with `e.stopPropagation()` to prevent event card click
- Cancel button UI with loading state

**Key Code (lines 106-120, 212-223):**
```typescript
const handleCancelClick = async (eventId: string, e: React.MouseEvent) => {
  e.stopPropagation(); // Prevent triggering onEventClick

  if (!onCancelClick || cancellingEventId) return;

  setCancellingEventId(eventId);
  try {
    await onCancelClick(eventId);
  } finally {
    setCancellingEventId(null);
  }
};
```

```tsx
{/* Cancel Registration Button - Session 30 */}
{onCancelClick && (
  <button
    onClick={(e) => handleCancelClick(event.id, e)}
    disabled={cancellingEventId === event.id}
    className="px-3 py-1 text-sm font-medium text-red-700 bg-red-50 border border-red-200 rounded hover:bg-red-100 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
    aria-label="Cancel registration"
  >
    {cancellingEventId === event.id ? 'Cancelling...' : 'Cancel Registration'}
  </button>
)}
```

#### 2. Dashboard Page (`web/src/app/(dashboard)/dashboard/page.tsx`)

**Added:**
- `handleCancelRegistration` async function (lines 155-167)
- Wired `onCancelClick={handleCancelRegistration}` to all 3 EventsList instances (Admin, EventOrganizer, GeneralUser)

**Key Code:**
```typescript
// Session 30: Cancel registration handler for dashboard
const handleCancelRegistration = async (eventId: string): Promise<void> => {
  try {
    await eventsRepository.cancelRsvp(eventId);
    // Reload registered events after successful cancellation
    const events = await eventsRepository.getUserRsvps();
    setRegisteredEvents(events);
  } catch (error) {
    console.error('Error cancelling registration:', error);
    throw error; // Re-throw so the component can show error state if needed
  }
};
```

### Testing

#### Test Coverage (`web/tests/unit/presentation/components/features/dashboard/EventsList.test.tsx`)

**Added 6 new tests:**
1. ✅ Should render cancel button for each event when onCancelClick prop is provided
2. ✅ Should not render cancel button when onCancelClick prop is not provided
3. ✅ Should call onCancelClick with correct eventId when cancel button is clicked
4. ✅ Should show loading state on cancel button while cancellation is in progress
5. ✅ Should not trigger onEventClick when cancel button is clicked
6. ✅ Should disable only the clicked cancel button, not all buttons

**Test Results:**
- Total: 15 tests
- Passed: 15 tests
- Failed: 0 tests

## Backend API

**Uses existing endpoint:**
- `DELETE /api/events/{id}/rsvp` (EventsController.cs:490-504)
- Frontend method: `eventsRepository.cancelRsvp(eventId)`

No backend changes required - reused existing cancel registration functionality.

## UX Features

1. **Loading State:** Button shows "Cancelling..." during API call
2. **Disabled State:** Only the clicked button is disabled, not all buttons
3. **Event Propagation:** Button click doesn't trigger event card click
4. **Auto-Refresh:** Registered events list automatically reloads after cancellation
5. **Consistent UI:** Uses same color scheme (red) as event details page cancel button

## Files Changed

1. `web/src/presentation/components/features/dashboard/EventsList.tsx`
2. `web/src/app/(dashboard)/dashboard/page.tsx`
3. `web/tests/unit/presentation/components/features/dashboard/EventsList.test.tsx`

## Testing Instructions

1. Navigate to Dashboard
2. Click "My Registered Events" tab
3. Verify cancel button appears on each registered event
4. Click "Cancel Registration" button
5. Verify button shows "Cancelling..." during API call
6. Verify event is removed from list after cancellation
7. Verify only clicked button is disabled during cancellation

## Related Work

- **Event Details Cancel:** Existing cancel button in `web/src/app/events/[id]/page.tsx`
- **Backend API:** `CancelRsvpCommand` in `src/LankaConnect.Application/Events/Commands/CancelRsvp/`

## Next Steps

None - feature is complete and fully tested.

## References

- Backend API: [EventsController.cs:490-504](../src/LankaConnect.API/Controllers/EventsController.cs)
- Repository: [events.repository.ts:249-251](../web/src/infrastructure/api/repositories/events.repository.ts)
- Commit: 640857d
