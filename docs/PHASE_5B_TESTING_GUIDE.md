# Phase 5B Testing Guide: Preferred Metro Areas

## 🎯 Overview

This guide explains how to test the **PreferredMetroAreasSection** component in your local development environment. The component is integrated on the profile page and communicates with the Azure staging backend API.

---

## 📋 Test Scenarios

### **1. Initial Load & View Mode (No Edit)**

**What to Test**: Component displays current user's metro area selections
**Steps**:
1. Navigate to `/profile` page (logged in user)
2. Scroll to "Preferred Metro Areas" section
3. Observe the display

**Expected Behavior**:
- ✅ Section card displays with MapPin icon and title
- ✅ If user has 0 metros selected: Shows "No metro areas selected"
- ✅ If user has metros selected: Shows them as badges (e.g., "Cleveland, OH")
- ✅ Edit button is visible in top-right corner
- ✅ No checkboxes visible (view mode)

**How to Test**:
```
1. Open browser DevTools (F12)
2. Go to Profile page
3. Scroll to "Preferred Metro Areas" section
4. Verify layout matches expected design
5. Check console for any errors (red text in console)
```

---

### **2. Enter Edit Mode**

**What to Test**: Clicking Edit button transitions to edit mode
**Steps**:
1. On profile page, find "Preferred Metro Areas" section
2. Click the "Edit" button

**Expected Behavior**:
- ✅ Edit button disappears
- ✅ Checkboxes appear for all metro areas
- ✅ Previously selected metros are checked
- ✅ "Save" and "Cancel" buttons appear
- ✅ Selection counter shows "X of 10 selected"
- ✅ Metro areas are grouped: Ohio metros, Other US metros, State-level

**How to Test**:
```
1. Click "Edit" button
2. Verify checkboxes appear
3. Count which metros are pre-checked
4. Check the counter displays correct number
5. Verify grouping (Ohio first, then others)
```

**Visual Locations**:
- Ohio metros: Cleveland, Columbus, Cincinnati
- Other US metros: Las Vegas, Denver, Austin, etc.
- State-level: All of Ohio, All of Nevada, etc.

---

### **3. Select/Deselect Metro Areas**

**What to Test**: Can check and uncheck metro area checkboxes
**Steps**:
1. In edit mode, click a checkbox for an unselected metro
2. Then uncheck a selected one

**Expected Behavior**:
- ✅ Checkbox toggle works (checked → unchecked and vice versa)
- ✅ Counter updates: "X of 10 selected" (increases/decreases)
- ✅ Validation error clears when deselecting
- ✅ No error shown for normal selections (< 10)

**How to Test**:
```
1. Click checkbox for "Las Vegas, NV"
2. Verify it gets checked
3. Watch the counter increment
4. Click it again
5. Verify it unchecks and counter decrements
6. Try 5 more metros - counter should update each time
```

---

### **4. Max Limit Validation (10 metros)**

**What to Test**: Cannot select more than 10 metro areas
**Steps**:
1. In edit mode, select 10 metro areas
2. Try to select an 11th one

**Expected Behavior**:
- ✅ After selecting 10th metro: Counter shows "10 of 10 selected"
- ✅ 11th checkbox click DOES NOT check it
- ✅ Red error message appears: "You cannot select more than 10 metro areas"
- ✅ Can still deselect metros to go below 10
- ✅ Error clears when deselecting one to get back to 9 or below

**How to Test**:
```
1. Select these metros in order:
   - Cleveland, OH
   - Columbus, OH
   - Cincinnati, OH
   - Las Vegas, NV
   - Denver, CO
   - Austin, TX
   - Phoenix, AZ
   - San Diego, CA
   - Portland, OR
   - Seattle, WA
2. Counter should show "10 of 10 selected"
3. Try clicking "Miami, FL"
4. Notice:
   - Checkbox does NOT get checked
   - Red error message appears
5. Now uncheck "Cleveland, OH"
6. Counter shows "9 of 10 selected"
7. Error message disappears
```

---

### **5. Save Changes**

**What to Test**: Save button persists changes to database
**Steps**:
1. In edit mode, select/deselect some metros
2. Click the "Save" button
3. Wait for API response

**Expected Behavior**:
- ✅ Save button becomes disabled while saving
- ✅ Component shows loading indicator (or disabled state)
- ✅ After 2-3 seconds: Success message appears "Preferred metro areas saved successfully"
- ✅ Edit mode exits automatically
- ✅ View mode displays newly saved metros as badges
- ✅ Success message auto-dismisses after 2 seconds

**How to Test**:
```
1. Select 3 metros: Cleveland, Denver, Las Vegas
2. Click "Save" button
3. Watch for:
   - Button becomes disabled/grayed out
   - Could be loading spinner or dimmed appearance
4. Wait 2-3 seconds for response
5. Verify:
   - Success message appears
   - Edit mode exits
   - Badges now show: "Cleveland", "Denver", "Las Vegas"
   - Success message disappears after 2 seconds
```

**Timing**:
- First click to API call: ~100-200ms
- API response (Azure staging): ~500-2000ms (depends on network)
- Total: 2-3 seconds

---

### **6. Cancel Without Saving**

**What to Test**: Cancel button reverts changes without saving
**Steps**:
1. In edit mode, select/deselect metros (change current selection)
2. Click "Cancel" button

**Expected Behavior**:
- ✅ Edit mode exits immediately
- ✅ No API call is made (no loading state)
- ✅ View mode shows ORIGINAL metros (before your changes)
- ✅ Your changes are discarded

**How to Test**:
```
1. Current selection: "Cleveland" only
2. Click "Edit"
3. Uncheck "Cleveland"
4. Check "Denver", "Las Vegas"
5. Click "Cancel"
6. Expected: Reverts to showing only "Cleveland"
7. Verify no success/error message appears
```

---

### **7. Error Handling**

**What to Test**: API errors are handled gracefully
**Steps**:
1. In edit mode, select some metros
2. Simulate network issue or API error
3. Click "Save"

**Expected Behavior**:
- ✅ Loading state shows (2-3 seconds)
- ✅ After timeout/error: Error message appears in red
- ✅ Error message shows: "Failed to update preferred metro areas"
- ✅ Edit mode stays active (doesn't exit)
- ✅ Checkboxes remain in current state for retry
- ✅ Can retry by clicking "Save" again

**How to Test Network Error**:
```
Option 1 - Disconnect Network:
1. Open DevTools (F12)
2. Go to Network tab
3. Click the throttle dropdown (shows "No throttling")
4. Select "Offline"
5. Try to save
6. Error message should appear
7. Go back to Normal to reconnect

Option 2 - Use DevTools Network Tab:
1. Open DevTools
2. Go to Network tab
3. Check "Offline" checkbox at bottom
4. Try to save
5. Watch for failed request
6. Error message should appear

Option 3 - API Endpoint Check (Advanced):
1. Open DevTools → Network tab
2. Try to save with some selections
3. Look for PUT request to:
   https://lankaconnect-api-staging.../api/users/{id}/preferred-metro-areas
4. If it's red/failed = Network error
5. If it's red with "401" = Auth error (re-login)
6. If it's red with "500" = Server error
```

---

### **8. Privacy: Clear All Metros**

**What to Test**: Can opt-out by clearing all selections
**Steps**:
1. User has some metros selected
2. In edit mode, uncheck all metros
3. Counter shows "0 of 10 selected"
4. Click "Save"

**Expected Behavior**:
- ✅ All metros become unchecked
- ✅ Counter shows "0 of 10 selected"
- ✅ Save works (API accepts empty array)
- ✅ After save: View mode shows "No metro areas selected"
- ✅ User has successfully opted out of location filtering

**How to Test**:
```
1. Current selection: "Cleveland", "Denver", "Las Vegas"
2. Click "Edit"
3. Uncheck all three metros
4. Counter should show "0 of 10 selected"
5. Click "Save"
6. Verify success message
7. Verify view mode now shows "No metro areas selected"
```

---

### **9. Responsive Design**

**What to Test**: Component works on mobile, tablet, desktop
**Steps**:
1. Open profile page on different screen sizes

**Expected Behavior**:
- ✅ **Mobile** (< 640px):
  - Single column of checkboxes
  - Metro names readable
  - Buttons stack vertically or fit side-by-side
- ✅ **Tablet** (640px - 1024px):
  - Two columns of checkboxes
  - Grid looks balanced
- ✅ **Desktop** (> 1024px):
  - Three columns of checkboxes
  - Full use of available space

**How to Test in DevTools**:
```
1. Open DevTools (F12)
2. Click responsive design mode button (Ctrl+Shift+M)
3. Test different presets:
   - iPhone 12 (390px)
   - iPad (768px)
   - Desktop (1920px)
4. Rotate between portrait/landscape
5. Verify layout looks correct each time
```

---

### **10. Accessibility Features**

**What to Test**: Component is keyboard and screen-reader friendly
**Steps**:
1. Navigate using only Tab key
2. Use keyboard to interact
3. Use screen reader (if available)

**Expected Behavior**:
- ✅ Tab through all interactive elements (Edit, checkboxes, Save, Cancel)
- ✅ Each checkbox has a label (can click label to toggle)
- ✅ Labels are associated with checkboxes (ARIA)
- ✅ Card has `role="region"` with label for screen readers
- ✅ Can use Space or Enter to check/uncheck
- ✅ Error messages are announced

**How to Test Keyboard Navigation**:
```
1. Click in the component area
2. Press Tab repeatedly
3. Verify focus moves to: Edit → (if editing) Checkboxes → Save/Cancel
4. Press Enter or Space on Edit button
5. Press Space/Enter on checkboxes to toggle
6. Press Tab on Save/Cancel buttons
7. All should work without mouse
```

**How to Test with Screen Reader (Windows)**:
```
1. Press Windows key + Enter to enable Narrator
2. Navigate to profile page
3. Listen to announcements
4. Should hear:
   - "Preferred Metro Areas region"
   - Checkbox names and states
   - Button names
```

**How to Test with Screen Reader (Mac)**:
```
1. System Preferences → Accessibility → VoiceOver
2. Enable VoiceOver
3. Navigate with Command keys
4. Should hear region and element descriptions
```

---

## 🔍 Technical Testing Details

### **API Calls to Monitor**

**In DevTools Network Tab**, you'll see these requests:

1. **Initial Profile Load** (GET):
   ```
   GET /api/users/{userId}
   Returns: UserProfile including preferredMetroAreas: ["cleveland-oh", "columbus-oh"]
   ```

2. **Save Metro Areas** (PUT):
   ```
   PUT /api/users/{userId}/preferred-metro-areas
   Body: { "metroAreaIds": ["denver-co", "las-vegas-nv"] }
   Response: Updated UserProfile
   ```

**How to Inspect**:
```
1. Open DevTools (F12)
2. Go to "Network" tab
3. Filter by "XHR" or "Fetch" to see API calls
4. Click a request to see:
   - Request body (what was sent)
   - Response body (what came back)
   - Status code (200 = success, 4xx = client error, 5xx = server error)
5. Check timing: Duration column shows how long it took
```

---

### **Console Logging**

**Check for Error Messages**:
```
1. Open DevTools (F12)
2. Go to "Console" tab
3. Look for red error messages
4. Good signs: No red errors after saving
5. Bad signs:
   - "Failed to save preferred metro areas"
   - "Network request failed"
   - "Unexpected token" (JSON parse error)
```

---

## ✅ Complete Test Checklist

Use this checklist to systematically test all features:

### **View Mode**
- [ ] Component renders with "No metro areas selected" when empty
- [ ] Component shows selected metros as badges
- [ ] Edit button is visible
- [ ] Correct number of metros shown

### **Edit Mode Entry**
- [ ] Edit button is clickable
- [ ] Checkboxes appear after clicking Edit
- [ ] Previous selections are pre-checked
- [ ] Counter shows correct initial count
- [ ] Metro areas are grouped properly
- [ ] Save and Cancel buttons appear

### **Selection Functionality**
- [ ] Can check unchecked metros
- [ ] Can uncheck checked metros
- [ ] Counter updates on each change
- [ ] Validation error clears when below 10
- [ ] No error shown for normal selections

### **Max Limit (10 metros)**
- [ ] Can select exactly 10 metros
- [ ] Counter shows "10 of 10 selected"
- [ ] 11th metro does NOT get checked
- [ ] Error message appears when trying 11th
- [ ] Error message is red and visible
- [ ] Deselecting clears the error

### **Save Functionality**
- [ ] Save button is clickable
- [ ] API request is sent (check Network tab)
- [ ] Request body has correct metroAreaIds
- [ ] Response is successful (200 status)
- [ ] Success message appears
- [ ] Edit mode exits
- [ ] View mode shows new selections
- [ ] Success message auto-dismisses in 2 seconds

### **Cancel Functionality**
- [ ] Cancel button is clickable
- [ ] No API request is made
- [ ] Edit mode exits immediately
- [ ] Original metros are shown (not changed ones)
- [ ] Validation errors are cleared

### **Error Scenarios**
- [ ] Network error is handled gracefully
- [ ] Error message is displayed clearly
- [ ] Edit mode stays active on error
- [ ] Can retry without refreshing page
- [ ] Console has helpful error logs

### **Privacy Feature**
- [ ] Can select "0 of 10" (all unchecked)
- [ ] Save works with empty selection
- [ ] View mode shows "No metro areas selected"
- [ ] User successfully opted out

### **Responsive Design**
- [ ] Mobile (390px): Single column, readable
- [ ] Tablet (768px): Two columns, balanced
- [ ] Desktop (1920px): Three columns, full use

### **Accessibility**
- [ ] Tab navigation works through all elements
- [ ] Can toggle checkboxes with Space/Enter
- [ ] Labels are clickable and associated
- [ ] Region is labeled for screen readers
- [ ] Error messages are readable

---

## 🐛 Troubleshooting

### **Component Not Visible**
```
Problem: Section doesn't appear on profile page
Solution:
1. Are you logged in? (Must have valid user)
2. Check console (F12 → Console) for errors
3. Try refreshing page
4. Check if you're on /profile route
```

### **Checkboxes Not Working**
```
Problem: Clicking checkboxes does nothing
Solution:
1. Check console for JavaScript errors
2. Verify component is in edit mode
3. Try clicking the metro name label (not just checkbox)
4. Refresh and try again
5. Check if API is responding (Network tab)
```

### **Save Not Working**
```
Problem: Save button doesn't do anything
Solution:
1. Open Network tab (F12 → Network)
2. Try to save
3. Look for red request to /preferred-metro-areas
4. If red (failed):
   a. Check API URL in browser
   b. Verify Azure staging API is running
   c. Check console for error message
5. If green (success) but no success message:
   a. Check state management in console
   b. Refresh and try again
```

### **Wrong Metro Areas Shown**
```
Problem: Badges/checkboxes show incorrect metros
Solution:
1. Check profile data in store (open DevTools console)
2. Verify your API response (Network tab)
3. Confirm you're looking at your own profile
4. Try logging out and back in
5. Clear browser cache and refresh
```

---

## 🚀 Performance Testing

### **Monitor Performance**:
```
1. Open DevTools (F12)
2. Go to "Performance" tab
3. Click record button
4. Perform actions:
   - Click Edit
   - Select 5 metros
   - Click Save
5. Stop recording
6. Look for:
   - Smooth animations (no big red bars)
   - Save should complete in <3 seconds
   - No memory leaks
```

### **Expected Timings**:
- Click Edit → Instant (~0ms)
- Select metro → Instant (~10-50ms)
- Click Save → 2-3 seconds total
  - Actual API call: ~500-2000ms
  - UI update: ~100ms
- Success message appears → 0-500ms after API response

---

## 📞 Need Help?

If something doesn't work as expected:

1. **Check Console** (F12 → Console) for error messages
2. **Check Network Tab** (F12 → Network) for API requests
3. **Check Local vs Staging**: Verify you're using staging API
4. **Clear Cache**: Ctrl+Shift+Delete → Clear all history
5. **Hard Refresh**: Ctrl+Shift+R (not just F5)
6. **Check Auth**: Are you logged in with valid token?
7. **Check Backend**: Is Azure staging API running?

---

## ✨ Summary

**What Users Can Test**:
- ✅ View current metro area selections
- ✅ Enter/exit edit mode
- ✅ Select and deselect metros
- ✅ Validation (max 10 limit)
- ✅ Save changes with success feedback
- ✅ Cancel changes without saving
- ✅ Opt-out by clearing all
- ✅ Error handling and recovery
- ✅ Works on mobile/tablet/desktop
- ✅ Keyboard and accessibility support

**All features have been implemented, tested, and are production-ready!**
