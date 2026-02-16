# Phase 6A.116 & 6A.117: Email Testing Guide

**Date**: 2026-02-16
**Status**: All 9 issues fixed and deployed to staging
**Migrations Applied**: Phase6A116 & Phase6A117 (verified at 18:20:10 UTC)

---

## Overview

This guide provides step-by-step instructions to verify all 9 email fixes are working correctly in staging.

### Issues Fixed

**P0 Critical (Email System Breaking):**
- Issue #3: Anonymous user token authentication
- Issue #4: Email placeholders showing as raw text
- Issue #5: HTML line breaks escaped
- Issue #8: Email edit button 404 errors
- Issue #9: Signup buttons not working

**P1 High Priority (UX Issues):**
- Issue #10: "Feel free to reply" text
- Issue #11: Empty PICKUP/DELIVERY card (confirmation)
- Issue #12: Both issues in update template

---

## Prerequisites

- Access to staging environment: https://lankaconnect.app
- Email account to receive test emails
- Two different browsers (for token auth testing)
- Event with signup lists and forms configured

---

## Test Suite

### Test 1: Form Response Confirmation Email (Issues #4, #5, #8, #9)

**Steps:**
1. Go to https://lankaconnect.app
2. Find an event with an active form
3. Fill out and submit the form response
4. Check your email inbox

**Verify:**
- [ ] **Issue #4**: Email shows your actual name (not `{{UserName}}`)
- [ ] **Issue #4**: Email shows event title (not `{{EventTitle}}`)
- [ ] **Issue #5**: Form answers appear on separate lines (not literal `<br/>`)
- [ ] **Issue #8**: "Edit Your Response" button exists
- [ ] **Issue #8**: Button URL is correct: `https://lankaconnect.app/events/{id}/forms/{id}`
- [ ] **Issue #9**: If event has signup lists, "View Signup Lists" button appears
- [ ] **Issue #9**: If event has signup forms, "View Signup Forms" button appears
- [ ] **Issue #9**: Buttons are clickable (have `href` attribute)

**Expected Email Content:**
```
Hi [Your Name],

Thank you for submitting your response for [Event Title]!

Response Summary:
Question 1: Answer 1
Question 2: Answer 2
[Each answer on new line - NO <br/> visible]

[Edit Your Response Button - clickable]
[View Signup Lists Button - if applicable]
```

---

### Test 2: Form Response Update Email (Issues #4, #5, #8, #9)

**Steps:**
1. Edit your existing form response
2. Update at least one answer
3. Submit the update
4. Check your email inbox

**Verify:**
- [ ] **Issue #4**: All placeholders replaced with actual values
- [ ] **Issue #5**: Updated answers show line breaks correctly
- [ ] **Issue #8**: Edit button URL correct (no duplicate `/events/` path)
- [ ] **Issue #9**: Signup buttons present if event has lists/forms

---

### Test 3: Anonymous User Token Authentication (Issue #3)

**Steps:**
1. Log out or use incognito mode
2. Submit a form response as anonymous user
3. Check confirmation email
4. Copy the "Edit Your Response" button URL
5. **Open URL in a DIFFERENT browser** (or different incognito window)
6. Verify form loads with your existing data

**Verify:**
- [ ] **Issue #3**: Form loads successfully (no 400 error)
- [ ] **Issue #3**: Form shows your previous answers
- [ ] **Issue #3**: You can update the response
- [ ] **Issue #3**: Update triggers email with updated data

**Technical Detail:**
The fix allows token authentication via:
- X-Access-Token header (frontend sends this)
- Query string `?token=...` (backward compatible)

---

### Test 4: Signup List Commitment Confirmation Email (Issue #11)

**Steps:**
1. Find an event with signup lists
2. Commit to bring an item
3. Check confirmation email

**Verify:**
- [ ] **Issue #11**: NO extra whitespace before footer
- [ ] **Issue #11**: NO empty "PICKUP/DELIVERY CARD" section
- [ ] Email layout looks clean and professional
- [ ] Footer has NO large gap above it

**Before Fix:**
```
[Thank you message]

[HUGE EMPTY SPACE - empty card was here]

[Footer]
```

**After Fix:**
```
[Thank you message]

[Footer - clean layout]
```

---

### Test 5: Signup List Commitment Update Email (Issues #10, #12)

**Steps:**
1. Update your signup list commitment
2. Change quantity or item details
3. Check update email

**Verify:**
- [ ] **Issue #10**: NO "If you have questions, feel free to reply to this email" text
- [ ] **Issue #12**: NO empty PICKUP/DELIVERY card
- [ ] **Issue #12**: Clean footer layout (no extra spacing)
- [ ] Email ends with: "Thank you for your continued support!" (period at end, no extra text)

**Text Removed:**
```html
<!-- This text was REMOVED entirely -->
<br />If you have questions, feel free to reply to this email.
```

**Rationale:** Automated emails should not encourage replies. Organizer contact card already provides proper contact methods.

---

### Test 6: Event Registration Cancellation Email (Issue #10)

**Steps:**
1. Cancel an event registration
2. Check cancellation email

**Verify:**
- [ ] **Issue #10**: NO "feel free to reply" text
- [ ] Email has organizer contact card (if organizer contact provided)
- [ ] Email ends professionally without encouraging email replies

---

### Test 7: Event Reminder Email (Issue #10)

**Steps:**
1. Trigger an event reminder (manually or wait for scheduled reminder)
2. Check reminder email

**Verify:**
- [ ] **Issue #10**: NO "feel free to reply" text
- [ ] Email content focuses on event details
- [ ] Professional tone without soliciting email responses

---

## Automated API Test (Optional)

Use the provided PowerShell script to test Issue #3 programmatically:

```powershell
.\test_issue81_staging.ps1
```

This script will:
1. Login to get JWT token
2. Find event with forms
3. Test GET with X-Access-Token header
4. Test GET with query string token
5. Verify both methods work

---

## Success Criteria

### All Tests Pass When:

**Email Content (Issues #4, #5):**
- ✅ Zero raw placeholders (no `{{UserName}}`, `{{EventTitle}}`, etc.)
- ✅ Line breaks render correctly (no visible `<br/>` tags)
- ✅ All dynamic content shows real values

**Email URLs (Issues #8, #9):**
- ✅ Edit button navigates to correct form page (not 404)
- ✅ Signup buttons present when event has lists/forms
- ✅ All buttons clickable with proper `href` attributes

**Authentication (Issue #3):**
- ✅ Anonymous users can edit from different browser (no 400 error)
- ✅ Token auth works via header AND query string

**Email Layout (Issues #10, #11, #12):**
- ✅ No "feel free to reply" text in any email
- ✅ No empty card sections causing layout gaps
- ✅ Clean, professional footer layout across all templates

---

## Troubleshooting

### If placeholders still show (Issue #4):
1. Check Azure logs for email sending errors
2. Verify FormResponseEmailParams.ToDictionary() uses correct constants
3. Check if email template in database has correct placeholder names

### If line breaks don't render (Issue #5):
1. Verify Phase6A116 migration was applied:
   ```sql
   SELECT * FROM "__EFMigrationsHistory"
   WHERE "MigrationId" LIKE '%Phase6A116%';
   ```
2. Check email_templates table for `{{{ResponseSummary}}}` (triple braces)

### If edit button 404s (Issue #8):
1. Check email source HTML for button URL
2. Verify URL format: `https://lankaconnect.app/events/{id}/forms/{id}`
3. Check for duplicate `/events/` in path

### If token auth fails (Issue #3):
1. Check browser console for X-Access-Token header
2. Verify API endpoint logs show header received
3. Try query string method: `?token={token}`

### If "feel free" text appears (Issue #10):
1. Verify Phase6A117 migration was applied
2. Check template update timestamp in database
3. Verify REPLACE() removed the text

### If empty card appears (Issues #11, #12):
1. Verify Phase6A117 migration was applied
2. Check template for `<!-- PICKUP/DELIVERY CARD -->` comment
3. Verify REGEXP_REPLACE() removed card section

---

## Migration Verification

To verify both migrations were applied:

```bash
# Connect to Azure Container App
az containerapp exec \
  --name lankaconnect-api-staging \
  --resource-group LankaConnect \
  --command '/bin/bash'

# Check migration history
echo $DATABASE_URL | xargs -I {} psql {} -c "
SELECT \"MigrationId\", \"ProductVersion\"
FROM \"__EFMigrationsHistory\"
WHERE \"MigrationId\" LIKE '%Phase6A11%'
ORDER BY \"MigrationId\" DESC;
"
```

**Expected Output:**
```
                    MigrationId                     | ProductVersion
----------------------------------------------------+---------------
 20260216181052_Phase6A117_FixEmailTemplateTextAndLayout | 8.0.19
 20260216033407_Phase6A116_FixEmailTemplateHtmlRendering | 8.0.19
```

---

## Reporting Issues

If any test fails, provide:
1. Screenshot of email showing issue
2. Email source HTML (View → Message Source)
3. Browser console errors (if applicable)
4. Azure Container App logs (if backend issue)

---

## Summary

**Total Tests**: 7 test scenarios covering 9 issues
**Estimated Time**: 20-30 minutes for complete verification
**Critical Path**: Tests 1-3 (P0 issues)
**Nice-to-Have**: Tests 4-7 (P1 UX improvements)

**After All Tests Pass:**
- [ ] Mark Phase 6A.116 & 6A.117 as COMPLETE
- [ ] Update PROGRESS_TRACKER.md with test results
- [ ] Merge PR #82 to main
- [ ] Deploy to production

---

**Document Created**: 2026-02-16
**Last Updated**: 2026-02-16
**Phase**: 6A.116 & 6A.117
**Status**: Ready for User Testing
