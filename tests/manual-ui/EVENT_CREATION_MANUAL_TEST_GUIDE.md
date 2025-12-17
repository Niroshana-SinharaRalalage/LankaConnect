# Event Creation - Manual UI Testing Guide
**Phase 6 Day 2 - Manual UI Testing**
**Test Environment**: Staging (https://lankaconnect-staging.azurewebsites.net)
**Tester**: _________________
**Date**: _________________

---

## Prerequisites

### Test Account Setup
1. **Login Credentials**:
   - Email: `niroshhh2@gmail.com`
   - Password: `12!@qwASzx`
   - Role: EventOrganizer (required to create events)

2. **Browser Requirements**:
   - ✅ Chrome/Edge (latest version recommended)
   - ✅ Clear browser cache before testing
   - ✅ DevTools console open (check for errors)

3. **Staging Environment**:
   - Frontend: https://lankaconnect-staging.azurewebsites.net
   - API: https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io

---

## Test Scenario 1: Free Event Creation

### Objective
Test creating a free event with no pricing

### Steps
1. **Navigate to Event Creation**
   - [ ] Login with test account credentials
   - [ ] Go to "Create Event" page
   - [ ] Verify page loads without errors (check console)

2. **Fill Event Details**
   - [ ] **Title**: "Manual Test - Free Community Event [YOUR_NAME]"
   - [ ] **Description**: "This is a free event created during manual UI testing."
   - [ ] **Category**: Select "Community"
   - [ ] **Start Date**: Select a future date (e.g., 2025-12-20)
   - [ ] **End Date**: Select same or next day
   - [ ] **Capacity**: Enter "100"

3. **Set Pricing**
   - [ ] **Is Free**: Toggle to "Yes" / Check "Free Event"
   - [ ] Verify pricing fields are hidden/disabled
   - [ ] No price fields should be visible

4. **Set Location**
   - [ ] **Street**: "123 Test Street"
   - [ ] **City**: "Colombo"
   - [ ] **State**: "Western"
   - [ ] **Zip Code**: "00100"
   - [ ] **Country**: "Sri Lanka"

5. **Submit and Verify**
   - [ ] Click "Create Event" button
   - [ ] Verify loading state appears
   - [ ] Verify success message/toast notification
   - [ ] Verify redirect to event details page or event list
   - [ ] **Record Event ID**: _________________

6. **Verify Created Event**
   - [ ] Navigate to event details page
   - [ ] Verify title displays correctly
   - [ ] Verify "Free Event" badge/label is shown
   - [ ] Verify no price information is displayed
   - [ ] Verify location displays correctly
   - [ ] Verify registration button is available

### Expected Result
✅ Event created successfully with isFree=true, no pricing displayed

---

## Test Scenario 2: Single Price Event Creation

### Objective
Test creating a paid event with single (legacy) pricing

### Steps
1. **Navigate to Event Creation**
   - [ ] Go to "Create Event" page
   - [ ] Verify page loads

2. **Fill Event Details**
   - [ ] **Title**: "Manual Test - Single Price Workshop [YOUR_NAME]"
   - [ ] **Description**: "Paid workshop with single ticket price."
   - [ ] **Category**: Select "Educational"
   - [ ] **Start Date**: Future date
   - [ ] **End Date**: Same or next day
   - [ ] **Capacity**: "50"

3. **Set Pricing**
   - [ ] **Is Free**: Toggle to "No" / Uncheck "Free Event"
   - [ ] **Pricing Type**: Select "Single Price" (if dropdown exists)
   - [ ] **Ticket Price**: Enter "25.00"
   - [ ] **Currency**: Select "USD"
   - [ ] Verify price preview displays "$25.00 per person"

4. **Set Location**
   - [ ] Fill location details (any valid address)

5. **Submit and Verify**
   - [ ] Click "Create Event"
   - [ ] Verify success message
   - [ ] **Record Event ID**: _________________

6. **Verify Created Event**
   - [ ] Navigate to event details
   - [ ] Verify price displays as "$25.00 per person" or "USD 25.00"
   - [ ] Verify NOT marked as free
   - [ ] Verify "Register" button shows price
   - [ ] Check console for errors (should be none)

### Expected Result
✅ Event created with single price, displays correctly on details page

---

## Test Scenario 3: Dual Price Event (Adult/Child)

### Objective
Test creating event with age-based dual pricing

### Steps
1. **Navigate to Event Creation**
   - [ ] Go to "Create Event" page

2. **Fill Event Details**
   - [ ] **Title**: "Manual Test - Family Event Dual Pricing [YOUR_NAME]"
   - [ ] **Description**: "Family event with adult and child pricing."
   - [ ] **Category**: Select "Cultural"
   - [ ] **Start Date**: Future date
   - [ ] **End Date**: Same or next day
   - [ ] **Capacity**: "150"

3. **Set Dual Pricing**
   - [ ] **Is Free**: No
   - [ ] **Pricing Type**: Select "Age-Based (Adult/Child)" or "Dual Pricing"
   - [ ] **Adult Price**: Enter "30.00"
   - [ ] **Child Price**: Enter "15.00"
   - [ ] **Child Age Limit**: Enter "12"
   - [ ] **Currency**: Select "USD"
   - [ ] Verify price preview shows both prices

4. **Set Location**
   - [ ] Fill location details

5. **Submit and Verify**
   - [ ] Click "Create Event"
   - [ ] Verify success message
   - [ ] **Record Event ID**: _________________

6. **Verify Created Event**
   - [ ] Navigate to event details
   - [ ] Verify Adult price displays: "$30.00"
   - [ ] Verify Child price displays: "$15.00 (under 12)"
   - [ ] Verify both prices are clearly labeled
   - [ ] Verify registration form allows selecting adult/child attendees

### Expected Result
✅ Event created with dual pricing, both prices display correctly

---

## Test Scenario 4: Group Tiered Pricing (Phase 6D)

### Objective
Test creating event with quantity-based discount tiers

### Steps
1. **Navigate to Event Creation**
   - [ ] Go to "Create Event" page

2. **Fill Event Details**
   - [ ] **Title**: "Manual Test - Corporate Event Group Pricing [YOUR_NAME]"
   - [ ] **Description**: "Corporate event with group discounts."
   - [ ] **Category**: Select "Business"
   - [ ] **Start Date**: Future date
   - [ ] **End Date**: Same or next day
   - [ ] **Capacity**: "200"

3. **Set Group Tiered Pricing**
   - [ ] **Is Free**: No
   - [ ] **Pricing Type**: Select "Group/Quantity-Based" or "Tiered Pricing"

   **Tier 1**:
   - [ ] **Min Attendees**: "1"
   - [ ] **Max Attendees**: "2"
   - [ ] **Price Per Person**: "50.00"
   - [ ] **Currency**: "USD"

   **Tier 2** (Click "Add Tier"):
   - [ ] **Min Attendees**: "3"
   - [ ] **Max Attendees**: "5"
   - [ ] **Price Per Person**: "40.00"
   - [ ] **Currency**: "USD"

   **Tier 3** (Click "Add Tier"):
   - [ ] **Min Attendees**: "6"
   - [ ] **Max Attendees**: Leave blank or "Unlimited"
   - [ ] **Price Per Person**: "35.00"
   - [ ] **Currency**: "USD"

   - [ ] Verify tier preview displays all 3 tiers
   - [ ] Verify price calculation preview shows correctly

4. **Set Location**
   - [ ] Fill location details

5. **Submit and Verify**
   - [ ] Click "Create Event"
   - [ ] Verify success message
   - [ ] **Record Event ID**: _________________

6. **Verify Created Event**
   - [ ] Navigate to event details
   - [ ] Verify all 3 pricing tiers display:
     - 1-2 people: $50.00/person
     - 3-5 people: $40.00/person
     - 6+ people: $35.00/person
   - [ ] Verify pricing table/card is clearly readable
   - [ ] Verify registration form allows selecting number of attendees
   - [ ] Verify price updates dynamically based on quantity

### Expected Result
✅ Event created with 3 pricing tiers, all display and calculate correctly

---

## Test Scenario 5: Event Image Upload

### Objective
Test uploading event cover image

### Steps
1. **Create or Edit Event**
   - [ ] Go to any event creation/edit page
   - [ ] Fill required fields

2. **Upload Image**
   - [ ] Click "Upload Cover Image" or "Add Image"
   - [ ] Select a test image (JPG/PNG, < 5MB recommended)
   - [ ] Verify upload progress indicator
   - [ ] Verify image preview appears

3. **Submit and Verify**
   - [ ] Save/Create event
   - [ ] Navigate to event details
   - [ ] Verify image displays correctly
   - [ ] Verify image loads from Azure Blob Storage (check Network tab)

### Expected Result
✅ Image uploads successfully and displays on event page

---

## Test Scenario 6: Event Video Upload (Phase 6A.12)

### Objective
Test uploading event promotional video

### Steps
1. **Create or Edit Event**
   - [ ] Go to event creation/edit page

2. **Upload Video**
   - [ ] Click "Upload Video" or "Add Video"
   - [ ] Select a test video (MP4, < 50MB recommended)
   - [ ] Verify upload progress
   - [ ] Verify video thumbnail appears

3. **Submit and Verify**
   - [ ] Save event
   - [ ] Navigate to event details
   - [ ] Verify video player displays
   - [ ] Click play to verify video works
   - [ ] Verify video URL is from Azure Blob Storage

### Expected Result
✅ Video uploads and plays correctly on event page

---

## Test Scenario 7: Form Validation Testing

### Objective
Test form validation and error handling

### Steps

**Test 7.1: Required Fields**
- [ ] Try to submit form with empty title → Verify error message
- [ ] Try to submit with empty description → Verify error
- [ ] Try to submit with empty capacity → Verify error
- [ ] Try to submit with empty dates → Verify error

**Test 7.2: Date Validation**
- [ ] Try to set end date before start date → Verify error
- [ ] Try to set past start date → Verify error/warning
- [ ] Set valid date range → Verify success

**Test 7.3: Pricing Validation**
- [ ] For paid event, try negative price → Verify error
- [ ] For paid event, try zero price → Verify error
- [ ] For dual pricing, try child price > adult price → Verify error/warning
- [ ] For group pricing, try overlapping tiers → Verify error
- [ ] For group pricing, try gap in tiers → Verify error

**Test 7.4: Capacity Validation**
- [ ] Try negative capacity → Verify error
- [ ] Try zero capacity → Verify error
- [ ] Try capacity over 10,000 → Verify error/warning

### Expected Result
✅ All validation rules work correctly, clear error messages displayed

---

## Test Scenario 8: Event Listing & Search

### Objective
Test that created events appear correctly in listings

### Steps
1. **Navigate to Event Listing**
   - [ ] Go to "Events" or "Browse Events" page
   - [ ] Verify page loads

2. **Verify Event Cards**
   - [ ] Scroll through events
   - [ ] Find one of your test events
   - [ ] Verify event card shows:
     - [ ] Title
     - [ ] Category badge
     - [ ] Date
     - [ ] Price (or "Free" badge)
     - [ ] Cover image (if uploaded)

3. **Test Filters**
   - [ ] Filter by "Free Events Only" → Verify only free events show
   - [ ] Filter by category → Verify correct events show
   - [ ] Filter by date range → Verify filtering works

4. **Test Search**
   - [ ] Search for your test event by title
   - [ ] Verify it appears in results
   - [ ] Search for non-existent event → Verify "no results" message

### Expected Result
✅ Events display correctly, filters and search work as expected

---

## Test Scenario 9: Event Registration Flow

### Objective
Test registering for different event types

### Steps

**Test 9.1: Register for Free Event**
- [ ] Navigate to free event details
- [ ] Click "Register" / "RSVP"
- [ ] Fill attendee details
- [ ] Submit registration
- [ ] Verify success message
- [ ] Verify registration appears in "My Events"

**Test 9.2: Register for Single Price Event**
- [ ] Navigate to single price event
- [ ] Click "Register"
- [ ] Fill attendee details
- [ ] Verify price summary shows correct amount
- [ ] Proceed to payment (or verify payment redirect)

**Test 9.3: Register for Dual Price Event**
- [ ] Navigate to dual price event
- [ ] Click "Register"
- [ ] Add 2 adults and 1 child
- [ ] Verify price calculation: (2 × $30) + (1 × $15) = $75
- [ ] Verify breakdown shows correctly

**Test 9.4: Register for Group Tiered Event**
- [ ] Navigate to group tiered event
- [ ] Click "Register"
- [ ] Select 4 attendees
- [ ] Verify tier 2 pricing applies: 4 × $40 = $160
- [ ] Change to 6 attendees
- [ ] Verify tier 3 pricing applies: 6 × $35 = $210

### Expected Result
✅ Registration flow works for all pricing types, calculations correct

---

## Test Scenario 10: Mobile Responsiveness

### Objective
Test UI on mobile devices/viewport

### Steps
1. **Open DevTools**
   - [ ] Press F12 to open DevTools
   - [ ] Click "Toggle device toolbar" (Ctrl+Shift+M)
   - [ ] Select "iPhone 12" or "Pixel 5"

2. **Test Event Creation**
   - [ ] Navigate through event creation form
   - [ ] Verify all fields are accessible
   - [ ] Verify buttons are tappable (not too small)
   - [ ] Verify no horizontal scrolling

3. **Test Event Details**
   - [ ] View event details on mobile
   - [ ] Verify layout stacks vertically
   - [ ] Verify images resize properly
   - [ ] Verify pricing information is readable

4. **Test Event Listing**
   - [ ] Browse events on mobile
   - [ ] Verify cards are full-width
   - [ ] Verify touch targets are adequate

### Expected Result
✅ All pages are mobile-friendly, no layout issues

---

## Browser Compatibility Testing

### Objective
Test across different browsers

### Browsers to Test
- [ ] **Chrome** (latest)
  - Create free event → Pass/Fail: _____
  - Create paid event → Pass/Fail: _____
  - Registration flow → Pass/Fail: _____

- [ ] **Firefox** (latest)
  - Create free event → Pass/Fail: _____
  - Create paid event → Pass/Fail: _____
  - Registration flow → Pass/Fail: _____

- [ ] **Safari** (if available)
  - Create free event → Pass/Fail: _____
  - Create paid event → Pass/Fail: _____
  - Registration flow → Pass/Fail: _____

- [ ] **Edge** (latest)
  - Create free event → Pass/Fail: _____
  - Create paid event → Pass/Fail: _____
  - Registration flow → Pass/Fail: _____

---

## Defect Reporting Template

When you find a bug, document it here:

### Bug #1
- **Scenario**: _________________
- **Steps to Reproduce**:
  1. _________________
  2. _________________
  3. _________________
- **Expected Result**: _________________
- **Actual Result**: _________________
- **Severity**: Critical / High / Medium / Low
- **Screenshot**: (paste link or attach)
- **Console Errors**: (if any)

### Bug #2
- **Scenario**: _________________
- **Steps to Reproduce**: _________________
- **Expected Result**: _________________
- **Actual Result**: _________________
- **Severity**: _________________

---

## Testing Checklist Summary

### Critical Path Tests (Must Pass)
- [ ] Scenario 1: Free Event Creation
- [ ] Scenario 2: Single Price Event Creation
- [ ] Scenario 3: Dual Price Event Creation
- [ ] Scenario 4: Group Tiered Pricing
- [ ] Scenario 7: Form Validation
- [ ] Scenario 9: Event Registration Flow

### Enhancement Tests (Should Pass)
- [ ] Scenario 5: Event Image Upload
- [ ] Scenario 6: Event Video Upload
- [ ] Scenario 8: Event Listing & Search
- [ ] Scenario 10: Mobile Responsiveness

### Nice-to-Have Tests
- [ ] Browser Compatibility (all browsers)
- [ ] Performance (page load times)
- [ ] Accessibility (keyboard navigation, screen readers)

---

## Sign-Off

**Tester Name**: _________________
**Date Completed**: _________________
**Total Scenarios Tested**: _____ / 10
**Scenarios Passed**: _____
**Scenarios Failed**: _____
**Critical Bugs Found**: _____

**Overall Assessment**:
- [ ] ✅ Ready for Production
- [ ] ⚠️ Minor Issues (can deploy with known issues)
- [ ] ❌ Critical Issues (must fix before deploy)

**Notes**:
_________________________________________________
_________________________________________________
_________________________________________________
