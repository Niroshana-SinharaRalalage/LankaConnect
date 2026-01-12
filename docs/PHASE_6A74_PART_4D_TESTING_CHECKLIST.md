# Phase 6A.74 Part 4D - Event Management Integration Testing Checklist

**Date**: 2026-01-12
**Feature**: Event-Specific Newsletter Functionality
**Environment**: Azure Staging
**Deployment Status**: ✅ SUCCESS (Run #20930804933)

---

## 🎯 Deployment Verification

### Frontend Deployment
- ✅ **Status**: SUCCESS
- ✅ **Workflow**: Deploy UI to Azure Staging (#20930804933)
- ✅ **Duration**: 4m5s
- ✅ **Trigger**: Push to develop (commit 24bbb421)
- ✅ **UI URL**: https://lankaconnect-ui-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/
- ✅ **Health Check**: 200 OK (verified at 18:51:37 UTC)

### Backend Deployment
- ✅ **Status**: Healthy
- ✅ **API URL**: https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api
- ✅ **Health Check**: 200 OK - Service version 1.0.0 (verified at 18:52:01 UTC)

### Build Status
- ✅ **TypeScript Compilation**: 0 errors (local build)
- ⚠️ **Pre-existing Warnings**: FeedAuthor and MetroArea type issues (unrelated to Newsletter feature)
- ✅ **Next.js Build**: All 25 routes compiled successfully

---

## 🧪 Manual Testing Checklist

### Part 1: Event Management Page - Communications Tab

#### 1.1 Tab Visibility
- [ ] Log in as EventOrganizer role
- [ ] Navigate to any event you own
- [ ] Click "Manage Event" or go to `/events/[id]/manage`
- [ ] Verify "Communications" tab appears after "Signup Lists" tab
- [ ] Verify Mail icon displays next to "Communications" label

#### 1.2 Tab Content - Empty State
- [ ] Click "Communications" tab
- [ ] Verify header displays: "Event Newsletters"
- [ ] Verify event title displays: "For: [Event Name]"
- [ ] Verify blue info banner displays with explanation
- [ ] Verify "Send Newsletter" button appears in header
- [ ] If no newsletters exist, verify empty state message: "No newsletters found for this event"

#### 1.3 Tab Content - With Newsletters
- [ ] If newsletters exist for the event, verify they display in the list
- [ ] Verify only newsletters linked to THIS event appear (not all newsletters)
- [ ] Verify NewsletterList component displays correctly
- [ ] Verify status badges show correctly (Draft/Active/Inactive/Sent)

---

### Part 2: Create Event-Specific Newsletter

#### 2.1 Open Create Form
- [ ] Click "Send Newsletter" button in Communications tab
- [ ] Verify modal opens with NewsletterForm
- [ ] Verify modal has backdrop overlay
- [ ] Verify modal is scrollable if content exceeds viewport

#### 2.2 Event Pre-filling
- [ ] ✅ **CRITICAL**: Verify "Link to Event" dropdown is PRE-FILLED with current event
- [ ] Verify event dropdown shows correct event title
- [ ] Verify event dropdown is still editable (can change to different event or "No event linkage")

#### 2.3 Form Fields
- [ ] Verify "Newsletter Title" input appears (required, max 200 chars)
- [ ] Verify "Newsletter Content" textarea appears (required, max 5000 chars)
- [ ] Verify "Email Groups" multi-select appears
- [ ] Verify "Include Newsletter Subscribers" checkbox appears (default checked)
- [ ] Verify "Link to Event" dropdown shows current event pre-filled
- [ ] Verify "Location Targeting" section appears/hides correctly based on conditions

#### 2.4 Form Validation
- [ ] Try submitting empty form → Verify validation errors appear
- [ ] Enter title < 5 chars → Verify error: "Title must be at least 5 characters"
- [ ] Enter title > 200 chars → Verify error: "Title must be less than 200 characters"
- [ ] Enter description < 20 chars → Verify error: "Description must be at least 20 characters"
- [ ] Uncheck "Include Newsletter Subscribers" AND don't select email groups → Verify error
- [ ] Fill valid data and submit → Verify newsletter creates successfully

#### 2.5 Successful Creation
- [ ] Submit valid form
- [ ] Verify modal closes
- [ ] Verify newsletter appears in the list
- [ ] Verify newsletter has "Draft" status badge
- [ ] Verify newsletter is linked to the event (check via dashboard Newsletters tab)

---

### Part 3: Newsletter Actions from Communications Tab

#### 3.1 Edit Newsletter
- [ ] Click "Edit" button on a draft newsletter
- [ ] Verify modal opens with form pre-filled
- [ ] Verify event dropdown still shows the linked event
- [ ] Modify title and description
- [ ] Click "Update Newsletter"
- [ ] Verify changes saved and modal closes
- [ ] Verify updated newsletter displays in list

#### 3.2 Publish Newsletter
- [ ] Click "Publish" button on a draft newsletter
- [ ] Verify confirmation (if any)
- [ ] Verify newsletter status changes to "Active"
- [ ] Verify "Active" badge displays in indigo color
- [ ] Verify expiry date appears ("Expires on [date]")

#### 3.3 Send Newsletter Email
- [ ] Click "Send Email" button on an active newsletter
- [ ] Verify confirmation (if any)
- [ ] Verify newsletter status changes to "Sent"
- [ ] Verify "Sent" badge displays in emerald color
- [ ] Verify sent date appears ("Sent on [date]")
- [ ] Verify "Send Email" button disappears (can't send twice)

#### 3.4 Delete Newsletter
- [ ] Click "Delete" button on a draft newsletter
- [ ] Verify confirmation dialog appears
- [ ] Confirm deletion
- [ ] Verify newsletter removed from list
- [ ] Verify cannot delete published/sent newsletters

---

### Part 4: Integration Testing

#### 4.1 Dashboard Integration
- [ ] Navigate to Dashboard → Newsletters tab
- [ ] Verify all newsletters display (not just event-specific ones)
- [ ] Find the newsletter created from event page
- [ ] Verify it shows the linked event in the card

#### 4.2 Cross-Navigation
- [ ] From Dashboard Newsletters tab, click on a newsletter linked to an event
- [ ] Navigate to that event's management page
- [ ] Go to Communications tab
- [ ] Verify the newsletter appears in the event-specific list

#### 4.3 Filtering Verification
- [ ] Create newsletters for Event A from its Communications tab
- [ ] Create newsletters for Event B from its Communications tab
- [ ] Navigate to Event A's Communications tab
- [ ] Verify ONLY Event A's newsletters appear
- [ ] Navigate to Event B's Communications tab
- [ ] Verify ONLY Event B's newsletters appear

---

### Part 5: Edge Cases & Error Handling

#### 5.1 No Events Available
- [ ] Create a new EventOrganizer account with no events
- [ ] Navigate to Dashboard → Newsletters tab
- [ ] Try creating newsletter
- [ ] Verify "Link to Event" dropdown shows only "No event linkage" option

#### 5.2 Event Deleted While Viewing
- [ ] Open event management page Communications tab
- [ ] Have another user/session delete the event
- [ ] Try to create newsletter
- [ ] Verify appropriate error handling

#### 5.3 Network Errors
- [ ] Disconnect internet
- [ ] Try to create newsletter
- [ ] Verify loading states and error messages display
- [ ] Reconnect internet
- [ ] Verify retry works

#### 5.4 Concurrent Operations
- [ ] Open Communications tab in two browser tabs
- [ ] Create newsletter in Tab 1
- [ ] Verify Tab 2 updates (React Query cache invalidation)

---

### Part 6: UI/UX Quality Checks

#### 6.1 Responsive Design
- [ ] Test on mobile viewport (375px width)
- [ ] Test on tablet viewport (768px width)
- [ ] Test on desktop viewport (1920px width)
- [ ] Verify modal displays correctly on all viewports
- [ ] Verify buttons and text remain readable

#### 6.2 Brand Consistency
- [ ] Verify orange (#FF7900) used for primary actions
- [ ] Verify rose (#8B1538) used for headings
- [ ] Verify status badge colors match existing patterns
- [ ] Verify fonts and spacing consistent with rest of app

#### 6.3 Accessibility
- [ ] Tab through form fields with keyboard
- [ ] Verify focus indicators visible
- [ ] Verify labels associated with inputs
- [ ] Verify error messages announced properly
- [ ] Test with screen reader (optional but recommended)

#### 6.4 Loading States
- [ ] Verify spinner or skeleton displays while fetching newsletters
- [ ] Verify "Saving..." text appears on form submission
- [ ] Verify "Publishing..." text appears during publish
- [ ] Verify "Sending..." text appears during email send

---

## 🔍 API Endpoint Testing (Optional Backend Verification)

### Newsletter Endpoints
- [ ] GET `/api/newsletters?eventId=[id]` - Fetch event-specific newsletters
- [ ] POST `/api/newsletters` - Create newsletter with eventId
- [ ] PUT `/api/newsletters/[id]` - Update newsletter
- [ ] POST `/api/newsletters/[id]/publish` - Publish newsletter
- [ ] POST `/api/newsletters/[id]/send` - Send newsletter email
- [ ] DELETE `/api/newsletters/[id]` - Delete draft newsletter

### Test Payload Example
```json
{
  "title": "Test Event Newsletter",
  "description": "This is a test newsletter for event-specific functionality.",
  "emailGroupIds": [],
  "includeNewsletterSubscribers": true,
  "eventId": "event-uuid-here",
  "targetAllLocations": true,
  "metroAreaIds": []
}
```

### Expected Response Codes
- ✅ 200 OK - Successful operation
- ✅ 201 Created - Newsletter created
- ✅ 202 Accepted - Email job enqueued
- ✅ 204 No Content - Delete successful
- ❌ 400 Bad Request - Validation error
- ❌ 403 Forbidden - Not authorized (non-owner)
- ❌ 404 Not Found - Newsletter/Event not found

---

## 📊 Test Results Summary

### Critical Features (Must Pass)
- [ ] Communications tab appears in event management page
- [ ] Event dropdown pre-fills correctly when creating from event page
- [ ] Newsletters filter correctly by eventId
- [ ] Create, edit, publish, send, delete operations work
- [ ] Modal form displays and closes correctly

### Important Features (Should Pass)
- [ ] Empty state displays when no newsletters
- [ ] Status badges show correct colors
- [ ] Loading states display appropriately
- [ ] Error handling works for network issues
- [ ] React Query cache invalidates correctly

### Nice-to-Have Features (Can Defer)
- [ ] Responsive design perfect on all devices
- [ ] Accessibility fully compliant
- [ ] Cross-tab synchronization instant
- [ ] Performance optimized for large lists

---

## ✅ Sign-Off Criteria

**Phase 6A.74 Part 4D is considered COMPLETE when:**
1. ✅ All Critical Features pass testing
2. ✅ 90%+ of Important Features pass testing
3. ✅ No blocking bugs found
4. ✅ TypeScript builds with 0 errors
5. ✅ Documentation updated (PROGRESS_TRACKER.md)
6. ✅ Changes committed and deployed to staging

**Current Status**: ✅ DEPLOYED TO STAGING - AWAITING MANUAL QA

---

## 🐛 Issues Found During Testing

### Issue Template
```markdown
**Issue #**: [Number]
**Severity**: Critical | High | Medium | Low
**Component**: [Component Name]
**Steps to Reproduce**:
1.
2.
3.

**Expected Behavior**:

**Actual Behavior**:

**Screenshots**: [If applicable]

**Workaround**: [If known]

**Fix Required**: Yes | No
```

---

## 📝 Notes for QA Team

1. **Focus Areas**: Event dropdown pre-filling is the most critical feature to test
2. **Test Data**: Create at least 3 test events with different configurations
3. **Browser Testing**: Test on Chrome, Firefox, Safari, Edge
4. **Performance**: Monitor for any slowdowns with 10+ newsletters
5. **Console Errors**: Keep browser console open and report any errors

---

## 🚀 Next Steps After QA Approval

1. Mark Part 4D as **PRODUCTION READY** in PROGRESS_TRACKER.md
2. Merge develop branch to master (if applicable)
3. Deploy to production using production deployment workflow
4. Monitor production for 24 hours
5. Create Phase 6A.74 Summary document with all parts complete

---

**Tester Name**: ___________________
**Test Date**: ___________________
**Test Result**: PASS | FAIL | BLOCKED
**Notes**: _________________________
