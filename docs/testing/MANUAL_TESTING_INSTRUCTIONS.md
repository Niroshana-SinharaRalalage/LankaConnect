# Manual Testing Instructions - Event Management UI
**Session 11 Continuation**
**Date**: 2025-11-26

## Quick Start

### 1. Start the Development Server
```bash
cd C:\Work\LankaConnect\web
npm run dev
```
- Server will start on http://localhost:3000
- Verify console shows no errors

### 2. Open Your Browser
- Navigate to http://localhost:3000
- Open DevTools (F12) for console monitoring

---

## 🎯 Critical Test Flow (5 Minutes)

### Quick Smoke Test
This validates the most important functionality:

1. **Navigate to Events** → http://localhost:3000/events
   - ✅ Should see grid of events
   - ✅ Should see filters at top
   - ✅ Each card shows title, date, location, price

2. **Click Any Event Card**
   - ✅ Should navigate to `/events/{id}`
   - ✅ Should see event details page
   - ✅ Should see "Register" or "Join Waitlist" button

3. **Test RSVP (Anonymous)**
   - ✅ Click "Register for Free" (on free event)
   - ✅ Should redirect to `/login?redirect=/events/{id}`

4. **Test Navigation**
   - ✅ Click "Back to Events"
   - ✅ Should return to events list

**Result**: If all 4 steps pass → ✅ Core functionality working

---

## 📋 Detailed Test Scenarios

### Test 1: Events List Page

**Steps**:
1. Navigate to http://localhost:3000/events
2. Observe the page load

**Verify**:
- [ ] Page loads without errors
- [ ] "Discover Events" heading displays
- [ ] Filters section displays (Event Type, Event Date, Location)
- [ ] Events display in grid layout (3 columns on desktop)
- [ ] Each event card shows:
  - [ ] Event image or gradient background
  - [ ] Category badge (top right)
  - [ ] Event title
  - [ ] Date and time with calendar icon
  - [ ] Location with map pin icon
  - [ ] Capacity with users icon
  - [ ] Price with dollar icon
  - [ ] "View Details" button
- [ ] Hover over card → shadow and translate effect
- [ ] Click anywhere on card → navigates to event detail

**Sample Events to Test**:
- Tech Meetup (Free): http://localhost:3000/events/d914cc72-ce7e-45e9-9c6e-f7b07bd2405c
- Professionals Mixer ($20): http://localhost:3000/events/68f675f1-327f-42a9-be9e-f66148d826c3
- Cooking Class ($55): http://localhost:3000/events/44a20e7b-d6dd-48c9-8925-db75846067bb

---

### Test 2: Event Detail Page - Free Event

**Test Event**: Tech Meetup (Free)
**URL**: http://localhost:3000/events/d914cc72-ce7e-45e9-9c6e-f7b07bd2405c

**Steps**:
1. Navigate to the URL above
2. Wait for page to load

**Verify Hero Section**:
- [ ] Hero image or gradient displays
- [ ] Category badge shows "Business"
- [ ] Full-width hero section

**Verify Event Information**:
- [ ] Title: "Sri Lankan Tech Professionals Meetup"
- [ ] Full description displays
- [ ] Date/Time: November 21, 2025 with calendar icon
- [ ] Location: "Ohio State University Campus, Columbus, Ohio" with map icon
- [ ] Capacity: "0 / 70 registered" with users icon (may vary if registrations exist)
- [ ] Price: "Free Event" with dollar icon

**Verify Registration Section**:
- [ ] Card titled "Event Registration"
- [ ] Quantity selector displays (default: 1)
- [ ] Increment (+) and decrement (-) buttons work
- [ ] Total shows "Free Event"
- [ ] "Register for Free" button displays

**Verify Other Sections**:
- [ ] "Back to Events" button at top
- [ ] "Sign-Up Lists" section at bottom (may show "No sign-up lists")
- [ ] Footer displays

---

### Test 3: Event Detail Page - Paid Event

**Test Event**: Professionals Mixer ($20)
**URL**: http://localhost:3000/events/68f675f1-327f-42a9-be9e-f66148d826c3

**Steps**:
1. Navigate to the URL above
2. Observe differences from free event

**Verify Pricing**:
- [ ] Price shows "$20.00 per person" with dollar icon
- [ ] Quantity selector defaults to 1
- [ ] Change quantity to 2 → Total shows "$40.00"
- [ ] Change quantity to 3 → Total shows "$60.00"
- [ ] Button says "Continue to Payment" (not "Register for Free")

**Verify Capacity**:
- [ ] Shows "0 / 60 registered" (or current count)
- [ ] No "Event Full" badge (capacity not reached)

---

### Test 4: RSVP - Anonymous User (Authentication Flow)

**Steps**:
1. Clear all cookies/auth tokens (or use incognito mode)
2. Navigate to any event detail page
3. Click "Register for Free" or "Continue to Payment"

**Verify**:
- [ ] Page redirects to `/login?redirect=/events/{id}`
- [ ] Login page displays
- [ ] Redirect URL is preserved in query string

**After Login** (if you have test credentials):
4. Login with valid credentials
5. After successful login

**Verify**:
- [ ] Automatically redirects back to event detail page
- [ ] "Register" button now works (doesn't redirect to login again)

---

### Test 5: RSVP - Authenticated User

**Prerequisites**: Must be logged in with valid user account

**Steps**:
1. Login to the application
2. Navigate to Tech Meetup: http://localhost:3000/events/d914cc72-ce7e-45e9-9c6e-f7b07bd2405c
3. Set quantity to 2
4. Click "Register for Free"

**Verify**:
- [ ] Button shows loading state ("Processing..." or disabled)
- [ ] No error alerts
- [ ] Success feedback (may vary based on implementation)
- [ ] Capacity count updates (0/70 → 2/70)
- [ ] React Query refetches data

**Check Console**:
- [ ] No error messages in console
- [ ] API call succeeds (check Network tab: POST to `/api/events/{id}/rsvp`)

---

### Test 6: Waitlist Functionality

**Note**: This requires an event at full capacity. You may need to use backend API to set capacity = currentRegistrations.

**Simulated Steps** (if full event available):
1. Navigate to full event
2. Observe UI changes

**Verify**:
- [ ] "Event Full" badge displays (red background)
- [ ] Capacity shows "{capacity} / {capacity} registered" (e.g., "25 / 25")
- [ ] Button changes to "Join Waitlist" (not "Register")

**Test Waitlist Action**:
3. Click "Join Waitlist"

**Verify (if logged in)**:
- [ ] Alert displays: "Successfully joined waitlist! You will be notified when a spot becomes available."
- [ ] Button disables during processing

**Verify (if not logged in)**:
- [ ] Redirects to `/login?redirect=/events/{id}`

---

### Test 7: Quantity Selector Validation

**Steps**:
1. Navigate to any paid event
2. Test quantity selector

**Verify Min Value**:
- [ ] Default quantity is 1
- [ ] Click decrement (-) → should NOT go below 1
- [ ] Try typing 0 → should reset to 1 or show validation

**Verify Increment**:
- [ ] Click increment (+) → quantity increases to 2, 3, 4...
- [ ] Total price updates dynamically (e.g., $20 → $40 → $60)

**Verify Manual Input**:
- [ ] Click in input field
- [ ] Type 5 → should accept
- [ ] Total updates to $100 (5 * $20)
- [ ] Type letters (abc) → should not accept or reset

---

### Test 8: Sign-Up Management Section

**Note**: This requires backend setup (organizer must create sign-up lists).

**Steps**:
1. Navigate to event with sign-up lists (if available)
2. Scroll to "Sign-Up Lists" section

**Verify Display (Anonymous)**:
- [ ] Section title: "Sign-Up Lists"
- [ ] Each sign-up list shows:
  - [ ] Category name
  - [ ] Description
  - [ ] Type: "Predefined Items" or "Open"
  - [ ] Existing commitments (if any)
- [ ] Message: "Please log in to commit to items"

**Verify Commit Action (Authenticated)**:
3. Login first
4. Click "I can bring something"

**Verify**:
- [ ] Form appears with:
  - [ ] Item description input
  - [ ] Quantity input (default: 1)
  - [ ] "Confirm" and "Cancel" buttons
5. Enter: "Vegetable Salad", Quantity: 2
6. Click "Confirm"

**Verify**:
- [ ] Form closes
- [ ] Commitment appears in list immediately (optimistic update)
- [ ] Shows: "Vegetable Salad - Quantity: 2"
- [ ] "Cancel" button appears on your commitment only

**Test Cancel**:
7. Click "Cancel" on your commitment

**Verify**:
- [ ] Confirmation dialog appears
- [ ] After confirming → commitment removed from list

---

### Test 9: Loading and Error States

**Test Loading State**:
1. Navigate to `/events`
2. Open DevTools → Network tab → Throttle to "Slow 3G"
3. Refresh page

**Verify**:
- [ ] Loading skeleton displays (gray placeholder cards)
- [ ] No broken layout during loading
- [ ] Events populate after load completes

**Test Error State - Invalid Event**:
4. Navigate to: http://localhost:3000/events/00000000-0000-0000-0000-000000000000
5. Wait for load

**Verify**:
- [ ] Error message displays: "Event not found" or similar
- [ ] "Back to Events" button works
- [ ] No crash or blank page

**Test Network Error**:
6. Open DevTools → Network tab → Go offline
7. Navigate to any event
8. Observe error handling

**Verify**:
- [ ] User-friendly error message
- [ ] Option to retry
- [ ] No console crash

---

### Test 10: Responsive Design

**Mobile Test (375px)**:
1. Open DevTools (F12) → Toggle device toolbar (Ctrl+Shift+M)
2. Select "iPhone SE" or set width to 375px
3. Navigate to `/events`

**Verify**:
- [ ] Events display in 1 column (full width)
- [ ] Filters stack vertically
- [ ] Cards are touch-friendly (adequate spacing)
- [ ] No horizontal scrolling

4. Click event → navigate to detail page

**Verify**:
- [ ] Information grid stacks vertically
- [ ] "Register" button is full width
- [ ] Text is readable (not too small)

**Tablet Test (768px)**:
5. Set width to 768px
6. Navigate to `/events`

**Verify**:
- [ ] Events display in 2 columns
- [ ] Filters display in row (if space allows)

**Desktop Test (1280px)**:
7. Set width to 1280px
8. Navigate to `/events`

**Verify**:
- [ ] Events display in 3 columns
- [ ] Optimal spacing and layout
- [ ] All elements fit within viewport

---

## 🐛 Known Issues to Watch For

1. **Badge Variant Error**: If you see TypeScript error about Badge variant, it was fixed (use className instead of variant="destructive")

2. **Stripe Placeholder**: Paid event checkout does NOT redirect to Stripe yet (placeholder implementation)

3. **Sign-Up Lists**: Requires backend setup by organizer first

4. **Images**: Events use fallback gradient/emoji (no image upload yet)

---

## ✅ Test Completion Checklist

After completing all tests, check off:

- [ ] All events display correctly on list page
- [ ] Event detail pages load with correct information
- [ ] RSVP works for free events (with auth)
- [ ] Price calculation works for paid events
- [ ] Quantity selector validation works
- [ ] Waitlist appears for full events (if tested)
- [ ] Sign-up management displays (if available)
- [ ] Authentication redirects work properly
- [ ] Loading states display correctly
- [ ] Error handling works (invalid events, network errors)
- [ ] Responsive design works on mobile/tablet/desktop
- [ ] No console errors during testing
- [ ] All navigation works (back buttons, links)

---

## 📊 Report Issues

If you find bugs, document:
1. **What you were doing** (steps to reproduce)
2. **What you expected** (expected behavior)
3. **What actually happened** (actual behavior)
4. **Screenshots** (if UI issue)
5. **Console errors** (if any)

Report findings in:
- `docs/testing/EVENT_MANAGEMENT_TEST_RESULTS.md` (create this file)
- Or update `EVENT_MANAGEMENT_E2E_TEST_PLAN.md` with results

---

## Next Steps After Testing

1. Document all findings
2. Update test results table in E2E test plan
3. Create bug tickets for any issues
4. Update PROGRESS_TRACKER.md with testing status
5. Plan fixes or enhancements
