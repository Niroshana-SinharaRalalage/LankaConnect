# Phase 6A.113 Quick Reference

**Date:** 2026-02-14
**Status:** AWAITING USER DECISION

---

## TL;DR - What Needs Your Decision

You have **3 critical email template consistency issues** to fix:

### Issue 1: EmailTemplateContract Constants Don't Match Database
**Problem:** 12 out of 33 constants in `EmailTemplateContract.cs` have wrong values
**Example:** Constant says `"template-free-event-registration"` but database has `"template-free-event-registration-confirmation"`

**Your Choice:**
- **Option A (RECOMMENDED):** Update code constants to match database ✅
- **Option B:** Rename 12 database templates to match code ❌

**Your preference:** "I think it is always better to have the same name" → **OPTION A**

---

### Issue 2: OrganizerCustomEmail Breaks Naming Convention
**Problem:** 31 templates use `template-*`, but 1 uses `OrganizerCustomEmail`

**Fix:** Rename `OrganizerCustomEmail` → `template-organizer-custom-email`

**Your preference:** "We should correct this... Need to follow consistency" → **AGREED**

---

### Issue 3: Template Content Modifications (Phase 6A.112)
**Problem:** 8 templates have "feel free" text, 14 templates need "View Signup Forms" button

**Your Choice:**
- **Option 1 (Narrow):** Fix 3 event templates only
- **Option 2 (Comprehensive):** Fix all 8 templates while we're at it

**Recommendation:** Option 2 (comprehensive cleanup)

---

## My Recommendation Summary

1. ✅ **Issue 1:** Option A (update code to match database)
2. ✅ **Issue 2:** Rename to `template-organizer-custom-email`
3. ✅ **Issue 3:** Comprehensive cleanup (all 8 templates)

**Why this approach wins:**
- Code matches database reality (no risky database renames)
- One-time comprehensive cleanup (don't revisit later)
- Adds validation test to prevent future drift
- Lower risk, faster execution

---

## What I've Delivered

### 📄 Analysis Document
**File:** `docs/PHASE_6A113_EMAIL_TEMPLATE_NAME_CONSISTENCY_FIX.md`
- Complete comparison table (32 constants analyzed)
- Root cause analysis
- Detailed fix plans for all 3 issues
- Success criteria

### 📜 Export Scripts (3 files)
1. `scripts/phase6a113_export_feel_free_templates.sql` - Export 3 event templates
2. `scripts/phase6a113_export_all_feel_free_templates.sql` - Export all 8 templates
3. `scripts/phase6a113_export_signup_forms_button_templates.sql` - Export 14 templates for button

### 🧪 Validation Test Skeleton
Included in main document - prevents future template name drift

---

## Detailed Findings

### EmailTemplateContract Mismatches (12 total)

| Constant Name | Current Value ❌ | Should Be ✅ |
|--------------|-----------------|-------------|
| `FreeEventRegistration` | `template-free-event-registration` | `template-free-event-registration-confirmation` |
| `PaidEventRegistration` | `template-paid-event-registration` | `template-paid-event-registration-confirmation-with-ticket` |
| `EmailVerification` | `template-email-verification` | `template-membership-email-verification` |
| `WelcomeEmail` | `template-welcome-email` | `template-welcome` |
| `EventApproved` | `template-event-approved` | `template-event-approval` |
| `EventCancellation` | `template-event-cancellation` | `template-event-cancellation-notifications` |
| `AttendeesAdded` | `template-attendees-added` | `template-attendees-added-confirmation` |
| `SupportTicketReceived` | `template-support-ticket-received` | `template-support-ticket-confirmation` |
| `AdminUserActivation` | `template-admin-user-activation` | `template-account-activated-by-admin` |
| `AdminUserDeactivation` | `template-admin-user-deactivation` | `template-account-deactivated-by-admin` |
| `TicketConfirmation` | `template-ticket-confirmation` | _(unused - delete)_ |
| `EventReminder24Hr` | `template-event-reminder-24hr` | _(unused - delete)_ |

---

### "Feel Free" Templates (8 total)

| Template Name | Event-Related? | In Narrow Scope? |
|--------------|----------------|------------------|
| `template-account-activated-by-admin` | ❌ No | ❌ |
| `template-account-unlocked-by-admin` | ❌ No | ❌ |
| `template-free-event-registration-confirmation` | ✅ Yes | ✅ |
| `template-membership-email-verification` | ❌ No | ❌ |
| `template-paid-event-registration-confirmation-with-ticket` | ✅ Yes | ✅ |
| `template-password-change-confirmation` | ❌ No | ❌ |
| `template-password-reset` | ❌ No | ❌ |
| `template-signup-list-commitment-confirmation` | ✅ Yes | ✅ |

**Narrow Scope:** 3 templates (event-related only)
**Comprehensive Scope:** 8 templates (all templates)

---

### "View Signup Forms" Button Templates (14 total)

**Priority Breakdown:**
- **HIGH (7):** registration-confirmation (free + paid), attendees-added, event-reminder, preliminary-registration, signup-commitment-confirmation
- **MEDIUM (5):** registration-cancellation, event-cancellation, commitment-update, event-postponed, new-event-publication
- **LOW (2):** event-approval, event-rejected

---

## Next Steps (Awaiting Your Decision)

**Please confirm:**
1. Issue 1 → Option A (update code to match database)?
2. Issue 2 → Rename to `template-organizer-custom-email`?
3. Issue 3 → Comprehensive cleanup (all 8 templates)?

**Once you confirm, I will:**
1. Update `EmailTemplateContract.cs` with 12 corrected constants
2. Update all `*EmailParams.cs` classes to use contract constants
3. Create EF Core migration for `OrganizerCustomEmail` rename
4. Create validation test to prevent future drift
5. Generate template modification migration for "feel free" and button

**Estimated time:** 2-3 hours for complete implementation + testing

---

## Files Reference

All deliverables saved in:
- **Analysis:** `docs/PHASE_6A113_EMAIL_TEMPLATE_NAME_CONSISTENCY_FIX.md`
- **Scripts:** `scripts/phase6a113_*.sql`
- **Quick Ref:** `docs/PHASE_6A113_QUICK_REFERENCE.md` (this file)
