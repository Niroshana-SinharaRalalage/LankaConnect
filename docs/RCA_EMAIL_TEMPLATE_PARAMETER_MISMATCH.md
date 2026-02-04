# Root Cause Analysis: Email Template Parameter Mismatch

**Phase**: 6A.87 (Hybrid Email Migration)
**Date**: 2026-02-03
**Author**: Claude (System Analysis)
**Status**: ✅ IMPLEMENTED (2026-02-03)

## Implementation Summary

**All 5 critical issues have been fixed:**
1. ✅ SignupCommitmentEmailParams - 3 template names corrected to include "list"
2. ✅ RegistrationCancellationEmailParams - Template name corrected to include "event-"
3. ✅ EventCancellationEmailParams - Template name corrected to include "-notifications"
4. ✅ RefundEmailParams - Template name, StripeRefundId, and currency formatting fixed
5. ✅ FreeEventRegistrationEmailParams & TicketConfirmationEmailParams - Added EventDateTime combined field

**Additional fixes:**
- ✅ Currency formatting fixed (explicit US culture to avoid `¤` symbol)
- ✅ RefundCompletedEventHandler updated for new signature

---

## Executive Summary

After comprehensive audit of 16 TypedEmailParams classes and 24+ email template migrations, **FIVE critical mismatches** were identified that cause email templates to display raw placeholders (e.g., `{{EventDateTime}}`) instead of actual values.

---

## Root Causes Identified

### 🔴 CRITICAL Issue #1: Template Name Mismatch - SignupCommitmentEmailParams

**File**: `src/LankaConnect.Shared/Email/Contracts/SignupCommitmentEmailParams.cs`

| What Params Uses (WRONG) | What Templates Expect (CORRECT) |
|--------------------------|--------------------------------|
| `template-signup-commitment-confirmation` ❌ | `template-signup-list-commitment-confirmation` ✅ |
| `template-signup-commitment-update` ❌ | `template-signup-list-commitment-update` ✅ |
| `template-signup-commitment-cancellation` ❌ | `template-signup-list-commitment-cancellation` ✅ |

**Impact**: Missing word "list" means NO template matches → email fails silently or uses wrong template.

**Evidence** (EmailTemplateNames.cs lines 55-69):
```csharp
public const string SignupCommitmentConfirmation = "template-signup-list-commitment-confirmation";
public const string SignupCommitmentUpdate = "template-signup-list-commitment-update";
public const string SignupCommitmentCancellation = "template-signup-list-commitment-cancellation";
```

---

### 🔴 CRITICAL Issue #2: Template Name Mismatch - RegistrationCancellationEmailParams

**File**: `src/LankaConnect.Shared/Email/Contracts/RegistrationCancellationEmailParams.cs`

| What Params Uses (WRONG) | What EmailTemplateNames Has (CORRECT) |
|--------------------------|--------------------------------------|
| `template-registration-cancellation` ❌ | `template-event-registration-cancellation` ✅ |

**Impact**: Missing "event-" prefix causes template lookup failure.

**Evidence** (EmailTemplateNames.cs line 75):
```csharp
public const string RegistrationCancellation = "template-event-registration-cancellation";
```

---

### 🔴 CRITICAL Issue #3: Template Name Mismatch - EventCancellationEmailParams

**File**: `src/LankaConnect.Shared/Email/Contracts/EventCancellationEmailParams.cs`

| What Params Uses (WRONG) | What EmailTemplateNames Has (CORRECT) |
|--------------------------|--------------------------------------|
| `template-event-cancellation` ❌ | `template-event-cancellation-notifications` ✅ |

**Impact**: Missing "-notifications" suffix causes template lookup failure.

**Evidence** (EmailTemplateNames.cs line 118):
```csharp
public const string EventCancellation = "template-event-cancellation-notifications";
```

---

### 🔴 CRITICAL Issue #4: Template Name Mismatch - RefundEmailParams

**File**: `src/LankaConnect.Shared/Email/Contracts/RefundEmailParams.cs`

| What Params Uses | What EmailTemplateNames Has |
|------------------|----------------------------|
| `template-refund-request-created` ❌ | `template-refund-requested` ✅ |
| `template-refund-completed` ✅ | `template-refund-completed` ✅ |

**Impact**: "refund-request-created" vs "refund-requested" - one of two templates won't match.

**Evidence** (EmailTemplateNames.cs lines 93-100):
```csharp
public const string RefundRequested = "template-refund-requested";
public const string RefundCompleted = "template-refund-completed";
```

---

### 🟡 HIGH Issue #5: Placeholder Naming Convention Chaos

**Root Cause**: Two incompatible date/time conventions exist across templates:

| Convention | Templates Using It | Params Classes Providing |
|------------|-------------------|-------------------------|
| `{{EventDateTime}}` (combined) | Signup list commitments, Refund templates, Newsletter (Phase 6A.96 standardized) | `SignupCommitmentEmailParams`, `RefundEmailParams` |
| `{{EventStartDate}} at {{EventStartTime}}` (separate) | Free/Paid registration, Event reminder, Event cancellation, Event details | `FreeEventRegistrationEmailParams`, `TicketConfirmationEmailParams`, `EventReminderEmailParams` |

**The Problem**:
- `FreeEventRegistrationEmailParams.ToDictionary()` outputs `EventStartDate` and `EventStartTime` separately
- But Phase 6A.96 migration updated templates to expect `{{EventDateTime}}`
- **Result**: Templates show `{{EventDateTime}}` literally because params provide different keys!

**Evidence from FreeEventRegistrationEmailParams.ToDictionary() (lines 184-190)**:
```csharp
{ "EventStartDate", EmailDateTimeHelper.FormatEventDate(EventStartDate, TimeZoneId) },
{ "EventStartTime", EventStartTime },
// ⚠️ NO "EventDateTime" KEY PROVIDED!
```

**Evidence from Phase 6A.96 migration** (20260203134739):
Templates were updated to use `{{EventDateTime}}` uniformly.

---

## Complete Mismatch Table

| Params Class | Template Name in Params | Correct Template Name | Status |
|-------------|------------------------|----------------------|--------|
| SignupCommitmentEmailParams | `template-signup-commitment-confirmation` | `template-signup-list-commitment-confirmation` | ❌ MISMATCH |
| SignupCommitmentEmailParams | `template-signup-commitment-update` | `template-signup-list-commitment-update` | ❌ MISMATCH |
| SignupCommitmentEmailParams | `template-signup-commitment-cancellation` | `template-signup-list-commitment-cancellation` | ❌ MISMATCH |
| RegistrationCancellationEmailParams | `template-registration-cancellation` | `template-event-registration-cancellation` | ❌ MISMATCH |
| EventCancellationEmailParams | `template-event-cancellation` | `template-event-cancellation-notifications` | ❌ MISMATCH |
| RefundEmailParams | `template-refund-request-created` | `template-refund-requested` | ❌ MISMATCH |
| RefundEmailParams | `template-refund-completed` | `template-refund-completed` | ✅ CORRECT |
| FreeEventRegistrationEmailParams | `template-free-event-registration-confirmation` | `template-free-event-registration-confirmation` | ✅ CORRECT |
| TicketConfirmationEmailParams | `template-paid-event-registration-confirmation-with-ticket` | `template-paid-event-registration-confirmation-with-ticket` | ✅ CORRECT |
| EventReminderEmailParams | `template-event-reminder` | `template-event-reminder` | ✅ CORRECT |
| AttendeesAddedEmailParams | `template-attendees-added-confirmation` | `template-attendees-added-confirmation` | ✅ CORRECT |
| AdminUserEmailParams | Various admin templates | Various admin templates | ✅ CORRECT |
| SupportTicketEmailParams | Various support templates | Various support templates | ✅ CORRECT |

---

## Placeholder Parameter Audit

### Classes with Date/Time Placeholder Issues

| Params Class | ToDictionary() Outputs | Template Expects | Issue |
|-------------|----------------------|-----------------|-------|
| FreeEventRegistrationEmailParams | `EventStartDate`, `EventStartTime` | `{{EventDateTime}}` (Phase 6A.96) | ⚠️ MISMATCH |
| TicketConfirmationEmailParams | `EventStartDate`, `EventStartTime` | `{{EventDateTime}}` (Phase 6A.96) | ⚠️ MISMATCH |
| EventReminderEmailParams | `EventStartDate`, `EventStartTime` | `{{EventStartDate}}`, `{{EventStartTime}}` | ✅ OK |
| SignupCommitmentEmailParams | `EventDateTime` | `{{EventDateTime}}` | ✅ OK (but template name wrong!) |
| AttendeesAddedEmailParams | `EventStartDate`, `EventStartTime` | `{{EventStartDate}}`, `{{EventStartTime}}` | ✅ OK |
| EventCancellationEmailParams | `EventStartDate`, `EventStartTime` | Varies by migration | ⚠️ NEEDS VERIFICATION |
| RefundEmailParams | `EventStartDate` only | `{{EventDateTime}}` | ⚠️ MISMATCH |
| RegistrationCancellationEmailParams | `EventStartDate` only | Varies by migration | ⚠️ NEEDS VERIFICATION |

---

## Proposed Fix Plan

### Phase 1: Fix Template Name Mismatches (Critical)

**Fix 1.1**: Update SignupCommitmentEmailParams.cs
```csharp
// Change line 18 from:
_templateName = "template-signup-commitment-confirmation";
// To:
_templateName = "template-signup-list-commitment-confirmation";

// Change line 120 from:
_templateName = "template-signup-commitment-update";
// To:
_templateName = "template-signup-list-commitment-update";

// Change line 129 from:
_templateName = "template-signup-commitment-cancellation";
// To:
_templateName = "template-signup-list-commitment-cancellation";
```

**Fix 1.2**: Update RegistrationCancellationEmailParams.cs
```csharp
// Change line 20 from:
public string TemplateName => "template-registration-cancellation";
// To:
public string TemplateName => "template-event-registration-cancellation";
```

**Fix 1.3**: Update EventCancellationEmailParams.cs
```csharp
// Change line 19 from:
public string TemplateName => "template-event-cancellation";
// To:
public string TemplateName => "template-event-cancellation-notifications";
```

**Fix 1.4**: Update RefundEmailParams.cs
```csharp
// Change line 137 from:
_templateName = "template-refund-request-created";
// To:
_templateName = "template-refund-requested";
```

### Phase 2: Standardize Date/Time Parameters

**Option A (Recommended)**: Add `EventDateTime` combined field to all params that need it.

For FreeEventRegistrationEmailParams, TicketConfirmationEmailParams:
```csharp
public Dictionary<string, object> ToDictionary()
{
    var dict = new Dictionary<string, object>
    {
        // Keep existing separate fields for backward compatibility
        { "EventStartDate", EmailDateTimeHelper.FormatEventDate(EventStartDate, TimeZoneId) },
        { "EventStartTime", EventStartTime },

        // ADD combined field for Phase 6A.96 standardized templates
        { "EventDateTime", $"{EmailDateTimeHelper.FormatEventDate(EventStartDate, TimeZoneId)} at {EventStartTime}" },
        // ... rest of dictionary
    };
}
```

**Option B (Alternative)**: Update templates to use separate `{{EventStartDate}}` and `{{EventStartTime}}`.

This requires a new migration to update templates affected by Phase 6A.96.

### Phase 3: Verification

1. Run unit tests for all TypedEmailParams classes
2. Deploy to staging
3. Test each email type manually
4. Verify no `{{placeholder}}` appears in rendered emails

---

## Files Requiring Changes

### Params Classes (4 files):
1. `src/LankaConnect.Shared/Email/Contracts/SignupCommitmentEmailParams.cs`
2. `src/LankaConnect.Shared/Email/Contracts/RegistrationCancellationEmailParams.cs`
3. `src/LankaConnect.Shared/Email/Contracts/EventCancellationEmailParams.cs`
4. `src/LankaConnect.Shared/Email/Contracts/RefundEmailParams.cs`

### For Placeholder Standardization (Phase 2):
5. `src/LankaConnect.Shared/Email/Contracts/FreeEventRegistrationEmailParams.cs`
6. `src/LankaConnect.Shared/Email/Contracts/TicketConfirmationEmailParams.cs`

---

## Risk Assessment

| Fix | Risk Level | Mitigation |
|-----|-----------|------------|
| Template name corrections | LOW | Simple string changes, unit tests exist |
| Adding EventDateTime field | MEDIUM | Need to ensure backward compatibility |
| Changing templates via migration | HIGH | Could break existing emails if deployed mid-send |

---

## Approval Checklist

Before implementing, user must approve:

- [ ] Template name fixes (Phase 1) - straightforward corrections
- [ ] Placeholder standardization approach (Phase 2) - Option A or B?
- [ ] Deployment strategy - immediate or phased rollout?
- [ ] Testing plan - which emails to test first?

---

## Related Documents

- [EMAIL_TEMPLATE_PARAMETER_MANIFEST.md](./EMAIL_TEMPLATE_PARAMETER_MANIFEST.md) - Full parameter listing
- [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md) - Phase 6A.87 status
- [STREAMLINED_ACTION_PLAN.md](./STREAMLINED_ACTION_PLAN.md) - Action items

---

## Appendix: Audit Evidence

### Files Audited:
1. SignupCommitmentEmailParams.cs - ❌ 3 template name issues
2. FreeEventRegistrationEmailParams.cs - ⚠️ Missing EventDateTime
3. TicketConfirmationEmailParams.cs - ⚠️ Missing EventDateTime
4. EventReminderEmailParams.cs - ✅ OK
5. EventCancellationEmailParams.cs - ❌ Template name issue
6. RefundEmailParams.cs - ❌ 1 template name issue
7. RegistrationCancellationEmailParams.cs - ❌ Template name issue
8. AttendeesAddedEmailParams.cs - ✅ OK
9. AdminUserEmailParams.cs - ✅ OK
10. SupportTicketEmailParams.cs - ✅ OK
11. EmailVerificationEmailParams.cs - ✅ OK
12. PasswordResetEmailParams.cs - ✅ OK
13. PasswordChangedEmailParams.cs - ✅ OK
14. UserEmailParams.cs - (Not event-related)
15. OrganizerEmailParams.cs - (Not event-related)
16. EventEmailParams.cs - (Base class)

### Migrations Audited:
- Phase6A34 - Uses `{{EventStartDate}} at {{EventStartTime}}`
- Phase6A54 - Uses `{{EventDateTime}}`
- Phase6A76 - Template renames
- Phase6A92 - Refund templates
- Phase6A96 - Standardization (introduced `{{EventDateTime}}` widely)
- Phase6A87 - Newsletter fix

---

**END OF RCA DOCUMENT**

**AWAITING USER APPROVAL BEFORE IMPLEMENTING FIXES**
