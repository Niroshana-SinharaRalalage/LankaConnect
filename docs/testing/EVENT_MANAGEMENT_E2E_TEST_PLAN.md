# Event Management End-to-End Test Plan
**Session 11 Continuation - Event Management UI Testing**
**Date**: 2025-11-26
**Test Environment**: Staging API + Local Dev Server

## Test Environment Setup

### Prerequisites
- ✅ Backend API: https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api
- ✅ Frontend Dev Server: http://localhost:3000
- ✅ Test Data: 24 events available in staging database
- ✅ Build Status: Zero TypeScript errors

### Test Accounts
- **Anonymous User**: No authentication required for viewing events
- **Authenticated User**: Required for RSVP/waitlist/sign-up actions
  - Redirect to `/login` if not authenticated

## Test Scenarios

### 1. Events List Page Navigation ✅ Ready for Testing

**Test ID**: E2E-001
**Feature**: Events List Page with Clickable Cards
**File**: `web/src/app/events/page.tsx`

**Test Steps**:
1. Navigate to http://localhost:3000/events
2. Verify events load from staging API
3. Verify each event card displays:
   - Event image or fallback emoji
   - Event title
   - Category badge
   - Date and time
   - Location (city, state)
   - Capacity (registrations/capacity)
   - Pricing (free or amount)
   - "View Details" button
4. Hover over event card → verify hover effects (shadow, translate-y)
5. Click on any event card → verify navigation to `/events/{id}`

**Expected Results**:
- ✅ 24 events display in grid (3 columns on desktop)
- ✅ Click navigates to event detail page
- ✅ No console errors

**Sample Events for Testing**:
- Free Event: `d914cc72-ce7e-45e9-9c6e-f7b07bd2405c` (Tech Meetup)
- Paid Event: `68f675f1-327f-42a9-be9e-f66148d826c3` (Professionals Mixer - $20)
- Low Capacity: `44a20e7b-d6dd-48c9-8925-db75846067bb` (Cooking Class - 25 capacity)

---

### 2. Event Detail Page - Information Display ✅ Ready for Testing

**Test ID**: E2E-002
**Feature**: Event Detail Page with Complete Information
**File**: `web/src/app/events/[id]/page.tsx`

**Test Steps**:
1. Navigate to `/events/d914cc72-ce7e-45e9-9c6e-f7b07bd2405c` (Free Tech Meetup)
2. Verify hero section displays:
   - Event image or fallback gradient
   - Category badge (Business)
3. Verify event information grid displays:
   - Event title: "Sri Lankan Tech Professionals Meetup"
   - Description with full text
   - Date & Time: Nov 21, 2025 with icon
   - Location: "Ohio State University Campus, Columbus, Ohio" with map icon
   - Capacity: "0 / 70 registered" with users icon
   - Pricing: "Free Event" with dollar icon
4. Verify "Back to Events" button exists
5. Verify loading skeleton appears while fetching

**Expected Results**:
- ✅ All event information displays correctly
- ✅ Icons render with brand colors (#FF7900)
- ✅ No missing data or null values
- ✅ Responsive layout on mobile/desktop

---

### 3. RSVP Functionality - Free Event ✅ Ready for Testing

**Test ID**: E2E-003
**Feature**: RSVP to Free Event
**Endpoint**: `POST /api/events/{id}/rsvp`

**Test Steps**:
1. Navigate to `/events/d914cc72-ce7e-45e9-9c6e-f7b07bd2405c` (Free Tech Meetup)
2. Verify quantity selector displays (default: 1)
3. Change quantity to 3
4. Verify "Total: Free Event" displays
5. **Anonymous User Test**:
   - Click "Register for Free" → should redirect to `/login?redirect=/events/{id}`
6. **Authenticated User Test** (requires login):
   - Login first
   - Navigate back to event
   - Click "Register for Free"
   - Verify success message or optimistic update
   - Verify registration count updates from 0 to 3

**Expected Results**:
- ✅ Anonymous redirects to login
- ✅ Authenticated user can RSVP
- ✅ Capacity updates optimistically
- ✅ React Query invalidates cache

---

### 4. RSVP Functionality - Paid Event ✅ Ready for Testing

**Test ID**: E2E-004
**Feature**: RSVP to Paid Event with Stripe Integration
**Endpoint**: `POST /api/events/{id}/rsvp` (Stripe placeholder)

**Test Steps**:
1. Navigate to `/events/68f675f1-327f-42a9-be9e-f66148d826c3` (Professionals Mixer - $20)
2. Verify quantity selector displays (default: 1)
3. Change quantity to 2
4. Verify "Total: $40.00" displays (20 * 2)
5. **Anonymous User Test**:
   - Click "Continue to Payment" → should redirect to `/login?redirect=/events/{id}`
6. **Authenticated User Test** (requires login):
   - Login first
   - Navigate back to event
   - Click "Continue to Payment"
   - **Note**: Stripe integration is placeholder, verify RSVP mutation is called
   - Check console for mutation success

**Expected Results**:
- ✅ Price calculation is accurate (quantity * price)
- ✅ Anonymous redirects to login
- ✅ Authenticated user triggers RSVP mutation
- ✅ Stripe integration placeholder noted (TODO for future)

---

### 5. Quantity Selector ✅ Ready for Testing

**Test ID**: E2E-005
**Feature**: Quantity Selector with Validation

**Test Steps**:
1. Navigate to any event detail page
2. Verify quantity selector displays with:
   - Label: "Number of Attendees"
   - Default value: 1
   - Min value: 1
   - Increment/decrement buttons
3. Click increment (+) → verify quantity increases to 2
4. Click decrement (-) → verify quantity decreases to 1
5. Try to decrement below 1 → should not allow
6. Manually type 5 in input → verify accepts
7. Manually type 0 → should reset to 1
8. Verify total price updates for paid events

**Expected Results**:
- ✅ Quantity cannot go below 1
- ✅ Total price updates dynamically
- ✅ Input validates numeric values only

---

### 6. Waitlist Functionality ✅ Ready for Testing

**Test ID**: E2E-006
**Feature**: Join Waitlist for Full Event
**Endpoint**: `POST /api/events/{id}/waiting-list`

**Test Steps**:
1. **Create Full Event Scenario**:
   - Find event with low capacity (e.g., Cooking Class - 25 capacity)
   - OR use backend to set event capacity = currentRegistrations
2. Navigate to full event detail page
3. Verify "Event Full" badge displays (red)
4. Verify "Join Waitlist" button displays instead of "Register" button
5. **Anonymous User Test**:
   - Click "Join Waitlist" → should redirect to `/login?redirect=/events/{id}`
6. **Authenticated User Test**:
   - Login first
   - Navigate back to event
   - Click "Join Waitlist"
   - Verify success alert: "Successfully joined waitlist! You will be notified when a spot becomes available."
   - Verify button disables during processing

**Expected Results**:
- ✅ Full event shows "Event Full" badge
- ✅ "Join Waitlist" button appears
- ✅ Anonymous redirects to login
- ✅ Authenticated user joins waitlist successfully
- ✅ Success message displays

---

### 7. Sign-Up Management Integration ✅ Ready for Testing

**Test ID**: E2E-007
**Feature**: Sign-Up Lists with Commitments
**Component**: `SignUpManagementSection`
**Endpoint**: `GET /api/events/{id}/signups`

**Test Steps**:
1. **Setup**: Backend must have sign-up lists created for an event
   - Use backend API to create sign-up list (organizer only)
   - Add predefined items: ["Appetizers", "Main Course", "Desserts"]
2. Navigate to event detail page with sign-up lists
3. Scroll down to "Sign-Up Lists" section
4. Verify section displays:
   - Section title: "Sign-Up Lists"
   - Each sign-up list card with:
     - Category name
     - Description
     - Type: "Predefined Items" or "Open"
     - Suggested items list (if predefined)
     - Existing commitments count
5. **Anonymous User Test**:
   - Verify message: "Please log in to commit to items"
6. **Authenticated User Test**:
   - Login first
   - Click "I can bring something"
   - Verify form appears with:
     - Item description input
     - Quantity input (default: 1)
     - "Confirm" and "Cancel" buttons
   - Enter: "Vegetable Salad", Quantity: 2
   - Click "Confirm"
   - Verify commitment appears in list
   - Verify "Cancel" button appears on own commitment only

**Expected Results**:
- ✅ Sign-up lists display correctly
- ✅ Anonymous cannot commit (login required message)
- ✅ Authenticated can commit with item + quantity
- ✅ Optimistic update shows commitment immediately
- ✅ User can cancel own commitments

---

### 8. Authentication Flow ✅ Ready for Testing

**Test ID**: E2E-008
**Feature**: Auth-Aware Redirects

**Test Steps**:
1. Clear all authentication tokens/cookies
2. Navigate to `/events/d914cc72-ce7e-45e9-9c6e-f7b07bd2405c`
3. Click "Register for Free" → verify redirect to `/login?redirect=/events/d914cc72-ce7e-45e9-9c6e-f7b07bd2405c`
4. Login with valid credentials
5. After successful login → verify redirect back to `/events/d914cc72-ce7e-45e9-9c6e-f7b07bd2405c`
6. Verify "Register for Free" button now works
7. Test same flow with:
   - "Join Waitlist" button
   - "I can bring something" button in sign-ups

**Expected Results**:
- ✅ Anonymous users redirect to login with return URL
- ✅ After login, users return to event page
- ✅ Actions work after authentication
- ✅ No broken auth flows

---

### 9. Loading and Error States ✅ Ready for Testing

**Test ID**: E2E-009
**Feature**: Loading Skeletons and Error Handling

**Test Steps**:
1. **Loading State Test**:
   - Navigate to `/events/{id}` (any event)
   - Verify loading skeleton displays:
     - Hero section skeleton
     - Information grid skeleton
     - "Loading..." text
2. **Error State Test**:
   - Navigate to invalid event ID: `/events/00000000-0000-0000-0000-000000000000`
   - Verify error message displays: "Event not found"
   - Verify "Back to Events" button works
3. **Network Error Test**:
   - Disconnect from internet (or use DevTools offline mode)
   - Navigate to event page
   - Verify error message displays
   - Reconnect and verify retry works

**Expected Results**:
- ✅ Loading skeleton displays during fetch
- ✅ 404 errors show "Event not found"
- ✅ Network errors show user-friendly message
- ✅ Error boundaries prevent crashes

---

### 10. Responsive Design ✅ Ready for Testing

**Test ID**: E2E-010
**Feature**: Mobile and Desktop Responsive Layout

**Test Steps**:
1. Open Chrome DevTools (F12) → Toggle device toolbar
2. Test on mobile (375px width):
   - Verify events list shows 1 column
   - Verify event detail page stacks vertically
   - Verify buttons are full-width on mobile
3. Test on tablet (768px width):
   - Verify events list shows 2 columns
   - Verify event detail page responsive grid
4. Test on desktop (1280px width):
   - Verify events list shows 3 columns
   - Verify event detail page optimal layout

**Expected Results**:
- ✅ All breakpoints render correctly
- ✅ No horizontal scrolling
- ✅ Touch targets are adequate size on mobile

---

## Test Execution Checklist

### Pre-Testing
- [x] Backend API healthy (200 status)
- [x] Frontend dev server running (port 3000)
- [x] Zero TypeScript compilation errors
- [x] Test data available (24 events in staging)

### Testing Phase
- [ ] E2E-001: Events List Navigation
- [ ] E2E-002: Event Detail Information Display
- [ ] E2E-003: RSVP Free Event
- [ ] E2E-004: RSVP Paid Event
- [ ] E2E-005: Quantity Selector
- [ ] E2E-006: Waitlist Functionality
- [ ] E2E-007: Sign-Up Management
- [ ] E2E-008: Authentication Flow
- [ ] E2E-009: Loading/Error States
- [ ] E2E-010: Responsive Design

### Post-Testing
- [ ] Document all findings
- [ ] Report bugs/issues
- [ ] Update PROGRESS_TRACKER.md
- [ ] Create bug tickets if needed
- [ ] Verify fixes

---

## Known Limitations

1. **Stripe Integration**: Placeholder implementation
   - Currently calls RSVP mutation but does not redirect to Stripe Checkout
   - **Next Step**: Integrate with `paymentsRepository.createCheckoutSession()`

2. **Sign-Up Lists**: Requires backend setup
   - Organizers must create sign-up lists via backend API first
   - No UI for organizer to create/manage sign-up lists yet
   - **Next Step**: Build organizer dashboard

3. **Image Upload**: Not implemented
   - Events use fallback gradient or emoji
   - **Next Step**: Integrate with Azure Blob Storage for event images

---

## Test Results Summary (To Be Filled During Testing)

| Test ID | Feature | Status | Notes |
|---------|---------|--------|-------|
| E2E-001 | Events List Navigation | ⏳ Pending | |
| E2E-002 | Event Detail Display | ⏳ Pending | |
| E2E-003 | RSVP Free Event | ⏳ Pending | |
| E2E-004 | RSVP Paid Event | ⏳ Pending | |
| E2E-005 | Quantity Selector | ⏳ Pending | |
| E2E-006 | Waitlist | ⏳ Pending | |
| E2E-007 | Sign-Up Management | ⏳ Pending | |
| E2E-008 | Authentication | ⏳ Pending | |
| E2E-009 | Loading/Error States | ⏳ Pending | |
| E2E-010 | Responsive Design | ⏳ Pending | |

---

## Sample Test Events from Staging API

### Free Events (Good for RSVP Testing)
1. **Tech Meetup** - `d914cc72-ce7e-45e9-9c6e-f7b07bd2405c`
   - Capacity: 70
   - Category: Business
   - Location: Columbus, Ohio

2. **Career Workshop** - `1afb85a0-da5e-45b4-806f-f2a9b612de44`
   - Capacity: 80
   - Category: Educational
   - Location: Cincinnati, Ohio

3. **Summer Picnic** - `873a3cd4-5a50-460d-a85b-942e32f7dc5d`
   - Capacity: 200
   - Category: Social
   - Location: Cleveland, Ohio

### Paid Events (Good for Price Calculation Testing)
1. **Professionals Mixer** - `68f675f1-327f-42a9-be9e-f66148d826c3`
   - Price: $20.00
   - Capacity: 60
   - Category: Business

2. **Cooking Class** - `44a20e7b-d6dd-48c9-8925-db75846067bb`
   - Price: $55.00
   - Capacity: 25 (Low capacity - good for waitlist test)
   - Category: Cultural

3. **Charity Dinner** - `c2afa82f-1ba5-47d8-9b77-a91961000513`
   - Price: $75.00
   - Capacity: 120
   - Category: Charity

---

## Next Steps After Testing

1. ✅ Complete all 10 test scenarios
2. ✅ Document results in this file
3. ✅ Update PROGRESS_TRACKER.md with test status
4. ✅ Report any bugs/issues found
5. ✅ Plan fixes or enhancements based on findings
6. Consider next phase:
   - Extend Stripe integration
   - Build organizer dashboard
   - Add event creation UI
   - Implement image upload
