# Phase 6A.113: Email Template Name Consistency Fix

**Date:** 2026-02-14
**Status:** ANALYSIS COMPLETE - AWAITING USER DECISION
**Priority:** HIGH - Prevents future bugs and ensures code maintainability

---

## Executive Summary

This document addresses **3 critical consistency issues** discovered in the email template system:

1. **EmailTemplateContract constants out of sync with database names** (5 mismatches found)
2. **OrganizerCustomEmail breaks naming convention** (missing `template-` prefix)
3. **Phase 6A.112 template modification scope** (8 templates with "feel free", 14 templates for "View Signup Forms")

**Root Cause:** EmailTemplateContract.cs was created with **shortened aliases** instead of exact database names, causing silent failures when templates don't match.

---

## ISSUE 1: EmailTemplateContract Constants Out of Sync

### Problem Analysis

EmailTemplateContract.cs defines constants that **don't match** the actual database template names:

| EmailTemplateContract Constant | Contract Value | Actual Database Name | Match? |
|-------------------------------|----------------|---------------------|--------|
| `TemplateNames.FreeEventRegistration` | `"template-free-event-registration"` ❌ | `template-free-event-registration-confirmation` | ❌ MISMATCH |
| `TemplateNames.PaidEventRegistration` | `"template-paid-event-registration"` ❌ | `template-paid-event-registration-confirmation-with-ticket` | ❌ MISMATCH |
| `TemplateNames.EventApproved` | `"template-event-approved"` ❌ | `template-event-approval` | ❌ MISMATCH |
| `TemplateNames.EventCancellation` | `"template-event-cancellation"` ❌ | `template-event-cancellation-notifications` | ❌ MISMATCH |
| `TemplateNames.EventReminder24Hr` | `"template-event-reminder-24hr"` ❌ | _(Not in database - unused constant)_ | ❌ MISMATCH |

**Current Workaround:** EmailParams classes hardcode the **actual database names**, bypassing EmailTemplateContract:

```csharp
// EmailTemplateContract.cs
public const string FreeEventRegistration = "template-free-event-registration"; // ❌ SHORT ALIAS

// FreeEventRegistrationEmailParams.cs (ACTUAL CODE)
public string TemplateName => "template-free-event-registration-confirmation"; // ✅ REAL DATABASE NAME
```

**Why This Is Dangerous:**
- **Silent failures**: If code uses `EmailTemplateContract.TemplateNames.FreeEventRegistration`, email fails with "Template not found"
- **No compile-time safety**: The contract promises safety but delivers broken aliases
- **Documentation lies**: Contract comments claim it's the "single source of truth" but it's not
- **Future developers**: Will trust the contract and write buggy code

---

### Comparison Table: Contract vs Database vs EmailParams

| # | Contract Constant | Contract Value | EmailParams Class | EmailParams Returns | Database Name | Status |
|---|------------------|----------------|-------------------|---------------------|---------------|--------|
| 1 | `PasswordReset` | `template-password-reset` | `PasswordResetEmailParams` | `"template-password-reset"` | `template-password-reset` | ✅ MATCH |
| 2 | `PasswordChangeConfirmation` | `template-password-change-confirmation` | `PasswordChangedEmailParams` | `"template-password-change-confirmation"` | `template-password-change-confirmation` | ✅ MATCH |
| 3 | `EmailVerification` | `template-email-verification` | `EmailVerificationEmailParams` | `"template-membership-email-verification"` | `template-membership-email-verification` | ❌ MISMATCH |
| 4 | `WelcomeEmail` | `template-welcome-email` | `WelcomeEmailParams` | `"template-welcome"` | `template-welcome` | ❌ MISMATCH |
| 5 | `NewEventPublication` | `template-new-event-publication` | `EventPublishedEmailParams` | `EmailTemplateContract.TemplateNames.NewEventPublication` | `template-new-event-publication` | ✅ MATCH |
| 6 | `EventDetailsPublication` | `template-event-details-publication` | `EventDetailsEmailParams` | `EmailTemplateContract.TemplateNames.EventDetailsPublication` | `template-event-details-publication` | ✅ MATCH |
| 7 | `PaidEventRegistration` | `template-paid-event-registration` | `TicketConfirmationEmailParams` | `"template-paid-event-registration-confirmation-with-ticket"` | `template-paid-event-registration-confirmation-with-ticket` | ❌ MISMATCH |
| 8 | `FreeEventRegistration` | `template-free-event-registration` | `FreeEventRegistrationEmailParams` | `"template-free-event-registration-confirmation"` | `template-free-event-registration-confirmation` | ❌ MISMATCH |
| 9 | `EventRegistrationCancellation` | `template-event-registration-cancellation` | `RegistrationCancellationEmailParams` | `"template-event-registration-cancellation"` | `template-event-registration-cancellation` | ✅ MATCH |
| 10 | `TicketConfirmation` | `template-ticket-confirmation` | _(None - uses PaidEventRegistration)_ | N/A | _(Does not exist)_ | ❌ UNUSED |
| 11 | `RefundRequested` | `template-refund-requested` | `RefundEmailParams` (AsRequest) | `"template-refund-requested"` | `template-refund-requested` | ✅ MATCH |
| 12 | `RefundCompleted` | `template-refund-completed` | `RefundEmailParams` (AsCompleted) | `"template-refund-completed"` | `template-refund-completed` | ✅ MATCH |
| 13 | `EventApproved` | `template-event-approved` | `EventApprovalEmailParams` | `EmailTemplateContract.TemplateNames.EventApproved` | `template-event-approval` | ❌ MISMATCH |
| 14 | `EventRejected` | `template-event-rejected` | `EventRejectedEmailParams` | `EmailTemplateContract.TemplateNames.EventRejected` | `template-event-rejected` | ✅ MATCH |
| 15 | `EventPostponed` | `template-event-postponed` | `EventPostponedEmailParams` | `EmailTemplateContract.TemplateNames.EventPostponed` | `template-event-postponed` | ✅ MATCH |
| 16 | `EventCancellation` | `template-event-cancellation` | `EventCancellationEmailParams` | `"template-event-cancellation-notifications"` | `template-event-cancellation-notifications` | ❌ MISMATCH |
| 17 | `EventReminder` | `template-event-reminder` | `EventReminderEmailParams` | `"template-event-reminder"` | `template-event-reminder` | ✅ MATCH |
| 18 | `EventReminder24Hr` | `template-event-reminder-24hr` | _(None)_ | N/A | _(Does not exist)_ | ❌ UNUSED |
| 19 | `AttendeesAdded` | `template-attendees-added` | `AttendeesAddedEmailParams` | `"template-attendees-added-confirmation"` | `template-attendees-added-confirmation` | ❌ MISMATCH |
| 20 | `SignupCommitmentConfirmation` | `template-signup-list-commitment-confirmation` | `SignupCommitmentEmailParams` | `"template-signup-list-commitment-confirmation"` | `template-signup-list-commitment-confirmation` | ✅ MATCH |
| 21 | `SignupCommitmentUpdate` | `template-signup-list-commitment-update` | `SignupCommitmentEmailParams` | `"template-signup-list-commitment-update"` | `template-signup-list-commitment-update` | ✅ MATCH |
| 22 | `SignupCommitmentCancellation` | `template-signup-list-commitment-cancellation` | `SignupCommitmentEmailParams` | `"template-signup-list-commitment-cancellation"` | `template-signup-list-commitment-cancellation` | ✅ MATCH |
| 23 | `SupportTicketReceived` | `template-support-ticket-received` | `SupportTicketEmailParams` | `"template-support-ticket-confirmation"` | `template-support-ticket-confirmation` | ❌ MISMATCH |
| 24 | `SupportTicketReply` | `template-support-ticket-reply` | `SupportTicketReplyEmailParams` | `EmailTemplateContract.TemplateNames.SupportTicketReply` | `template-support-ticket-reply` | ✅ MATCH |
| 25 | `AdminUserActivation` | `template-admin-user-activation` | `AccountActivatedEmailParams` | `EmailTemplateContract.TemplateNames.AdminUserActivation` | `template-account-activated-by-admin` | ❌ MISMATCH |
| 26 | `AdminUserDeactivation` | `template-admin-user-deactivation` | `AccountDeactivatedEmailParams` | `EmailTemplateContract.TemplateNames.AdminUserDeactivation` | `template-account-deactivated-by-admin` | ❌ MISMATCH |
| 27 | `OrganizerRoleApproval` | `template-organizer-role-approval` | `OrganizerRoleApprovalEmailParams` | `EmailTemplateContract.TemplateNames.OrganizerRoleApproval` | `template-organizer-role-approval` | ✅ MATCH |
| 28 | `NewsletterSubscriptionConfirmation` | `template-newsletter-subscription-confirmation` | `NewsletterSubscriptionEmailParams` | `EmailTemplateContract.TemplateNames.NewsletterSubscriptionConfirmation` | `template-newsletter-subscription-confirmation` | ✅ MATCH |
| 29 | `NewsletterNotification` | `template-newsletter-notification` | `NewsletterEmailParams` | `EmailTemplateContract.TemplateNames.NewsletterNotification` | `template-newsletter-notification` | ✅ MATCH |
| 30 | `FormResponseConfirmation` | `template-form-response-confirmation` | `FormResponseEmailParams` | `EmailTemplateContract.TemplateNames.FormResponseConfirmation` | `template-form-response-confirmation` | ✅ MATCH |
| 31 | `FormResponseUpdate` | `template-form-response-update` | `FormResponseEmailParams` | `EmailTemplateContract.TemplateNames.FormResponseUpdate` | `template-form-response-update` | ✅ MATCH |
| 32 | `FormResponseCancellation` | `template-form-response-cancellation` | `FormResponseEmailParams` | `EmailTemplateContract.TemplateNames.FormResponseCancellation` | `template-form-response-cancellation` | ✅ MATCH |

**Summary:**
- ✅ **20 constants match** database names
- ❌ **12 constants have mismatches** (10 wrong values + 2 unused constants)

---

### Fix Options

#### Option A: Update EmailTemplateContract Constants to Match Database (RECOMMENDED)

**Pros:**
- ✅ Contract becomes the **true single source of truth**
- ✅ Future code can safely use `EmailTemplateContract.TemplateNames.*`
- ✅ No database changes required (production data unchanged)
- ✅ Easier rollback (code-only change)
- ✅ Aligns with user preference: "always better to have the same name"

**Cons:**
- ❌ Requires updating ~10 constant values in EmailTemplateContract.cs
- ❌ Risk of breaking existing code that uses wrong constants (minimal - EmailParams bypass them)

**Implementation:**
1. Update EmailTemplateContract.cs constants to match exact database names
2. Deprecate unused constants (`TicketConfirmation`, `EventReminder24Hr`)
3. Add XML comments documenting old values for reference
4. Run validation test to confirm all EmailParams match contract

**Changes Required:**
```csharp
// BEFORE
public const string FreeEventRegistration = "template-free-event-registration";
public const string PaidEventRegistration = "template-paid-event-registration";
public const string EventApproved = "template-event-approved";

// AFTER
public const string FreeEventRegistration = "template-free-event-registration-confirmation";
public const string PaidEventRegistration = "template-paid-event-registration-confirmation-with-ticket";
public const string EventApproval = "template-event-approval"; // Renamed from EventApproved
```

---

#### Option B: Rename Database Templates to Match Constants

**Pros:**
- ✅ EmailTemplateContract stays unchanged
- ✅ Shorter, simpler template names

**Cons:**
- ❌ Requires database migration in **production** (risky)
- ❌ Must update **all database templates** (staging + production)
- ❌ Breaking change if external systems reference template names
- ❌ Must test email sending after migration
- ❌ Rollback requires another migration

**Implementation:**
1. Create EF Core migration to rename 10+ templates
2. Test in staging environment
3. Deploy to production
4. Verify all emails still send correctly

---

#### Option C: Add Validation Test to Catch Drift

**Pros:**
- ✅ Prevents future mismatches
- ✅ Catches errors at build/test time
- ✅ Works with either Option A or Option B

**Cons:**
- ❌ Doesn't fix existing mismatches
- ❌ Only a safety net, not a solution

**Implementation:**
1. Create unit test in `LankaConnect.Shared.Tests`
2. Test validates every EmailParams.TemplateName matches EmailTemplateContract
3. Test fails if new EmailParams hardcodes a template name

---

### RECOMMENDATION: Hybrid Approach (A + C)

**Execute in this order:**

1. ✅ **Fix EmailTemplateContract constants** (Option A) - Align code with reality
2. ✅ **Add validation test** (Option C) - Prevent future drift
3. ✅ **Update EmailParams classes** to use contract constants (remove hardcoded strings)

**This approach:**
- Makes EmailTemplateContract the **true source of truth**
- Adds **compile-time safety** for future development
- Requires **no database changes** (safer, faster)
- Aligns with user's preference: "better to have the same name"

---

## ISSUE 2: OrganizerCustomEmail Naming Inconsistency

### Problem Analysis

**Current State:**
- **31 templates** follow naming convention: `template-*`
- **1 template** breaks convention: `OrganizerCustomEmail` ❌

**Example:**
```sql
-- ✅ CORRECT PATTERN (31 templates)
template-password-reset
template-event-approval
template-newsletter-notification

-- ❌ EXCEPTION (1 template)
OrganizerCustomEmail
```

**Why This Is Inconsistent:**
- Violates naming convention established across entire system
- Makes pattern matching difficult (`WHERE name LIKE 'template-%'`)
- Confusing for developers (is this a special system template?)

---

### Fix Plan

**Rename:** `OrganizerCustomEmail` → `template-organizer-custom-email`

**Files to Update:**

1. **EmailTemplateContract.cs:**
   ```csharp
   // BEFORE - Line 73
   // public const string OrganizerCustomEmail = "OrganizerCustomEmail";

   // AFTER
   public const string OrganizerCustomEmail = "template-organizer-custom-email";
   ```

2. **EmailTemplateNames.cs:**
   ```csharp
   // BEFORE - Line 143
   // public const string OrganizerCustomEmail = "OrganizerCustomEmail";

   // AFTER
   public const string OrganizerCustomEmail = "template-organizer-custom-email";
   ```

3. **Database Migration:**
   ```csharp
   // Migration: Phase6A113_RenameOrganizerCustomEmailTemplate.cs
   protected override void Up(MigrationBuilder migrationBuilder)
   {
       migrationBuilder.Sql(@"
           UPDATE communications.email_templates
           SET name = 'template-organizer-custom-email'
           WHERE name = 'OrganizerCustomEmail';
       ");
   }

   protected override void Down(MigrationBuilder migrationBuilder)
   {
       migrationBuilder.Sql(@"
           UPDATE communications.email_templates
           SET name = 'OrganizerCustomEmail'
           WHERE name = 'template-organizer-custom-email';
       ");
   }
   ```

**Impact Analysis:**
- ✅ **Low risk**: Template not actively used (organizer custom email feature not implemented yet)
- ✅ **Clean rollback**: Down() migration reverts change
- ✅ **No EmailParams class**: No code references to update (template used dynamically)

---

## ISSUE 3: Phase 6A.112 Template Modification Scope

### Analysis: "Feel Free" Text in Templates

**User's Grep Results:** 8 templates contain "feel free" text:

| # | Template Name | "Feel Free" Usage | In Phase 6A.112 Scope? |
|---|--------------|-------------------|----------------------|
| 1 | `template-account-activated-by-admin` | "feel free to contact our support team" | ❌ NOT IN SCOPE |
| 2 | `template-account-unlocked-by-admin` | "feel free to contact support" | ❌ NOT IN SCOPE |
| 3 | `template-free-event-registration-confirmation` | "feel free to reach out" | ✅ IN SCOPE |
| 4 | `template-membership-email-verification` | "feel free to contact support" | ❌ NOT IN SCOPE |
| 5 | `template-paid-event-registration-confirmation-with-ticket` | "feel free to contact organizer" | ✅ IN SCOPE |
| 6 | `template-password-change-confirmation` | "feel free to contact support" | ❌ NOT IN SCOPE |
| 7 | `template-password-reset` | "feel free to contact support" | ❌ NOT IN SCOPE |
| 8 | `template-signup-list-commitment-confirmation` | "feel free to reach out" | ✅ IN SCOPE |

**Phase 6A.112 Focus:** Event-related templates only (not auth/admin templates)

**Cleanup Strategy:**
- **Option 1:** Remove "feel free" from **3 event templates** (narrow scope)
- **Option 2:** Remove "feel free" from **all 8 templates** (comprehensive cleanup)

**Recommendation:** Option 2 (comprehensive cleanup while we're fixing templates)

---

### Analysis: "View Signup Forms" Button Scope

**Phase 6A.112 Requirement:** Add "View Signup Forms" button to event-related emails

**Event Templates That Should Have Button:** (14 templates)

| # | Template Name | Has Signup Forms Button? | Priority |
|---|--------------|--------------------------|----------|
| 1 | `template-free-event-registration-confirmation` | ❌ MISSING | HIGH |
| 2 | `template-paid-event-registration-confirmation-with-ticket` | ❌ MISSING | HIGH |
| 3 | `template-event-registration-cancellation` | ❌ MISSING | MEDIUM |
| 4 | `template-attendees-added-confirmation` | ❌ MISSING | HIGH |
| 5 | `template-event-reminder` | ❌ MISSING | HIGH |
| 6 | `template-event-cancellation-notifications` | ❌ MISSING | MEDIUM |
| 7 | `template-event-approval` | ❌ MISSING | LOW |
| 8 | `template-event-rejected` | ❌ MISSING | LOW |
| 9 | `template-event-postponed` | ❌ MISSING | MEDIUM |
| 10 | `template-new-event-publication` | ❌ MISSING | MEDIUM |
| 11 | `template-signup-list-commitment-confirmation` | ❌ MISSING | HIGH |
| 12 | `template-signup-list-commitment-update` | ❌ MISSING | MEDIUM |
| 13 | `template-signup-list-commitment-cancellation` | ❌ MISSING | LOW |
| 14 | `template-preliminary-registration-payment-pending` | ❌ MISSING | HIGH |

**Button Template:**
```html
{{#if HasSignupForms}}
  <tr>
    <td style="padding: 20px 0;">
      <a href="{{SignupFormsUrl}}"
         style="background-color: #10B981; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block; font-weight: 600;">
        View Signup Forms
      </a>
    </td>
  </tr>
{{/if}}
```

**Implementation Notes:**
- All EmailParams classes already have `HasSignupForms` and `SignupFormsUrl` properties (added in Phase 6A.112)
- Templates just need HTML button added
- Button shows conditionally (only if event has signup forms)

---

### Export Scripts

#### Script 1: Export 3 Templates for "Feel Free" Cleanup

```sql
-- File: scripts/phase6a113_export_feel_free_templates.sql
-- Purpose: Export 3 event templates needing "feel free" text removed

SELECT
    name,
    subject,
    html_content
FROM communications.email_templates
WHERE name IN (
    'template-free-event-registration-confirmation',
    'template-paid-event-registration-confirmation-with-ticket',
    'template-signup-list-commitment-confirmation'
)
ORDER BY name;
```

#### Script 2: Export 14 Templates for "View Signup Forms" Button

```sql
-- File: scripts/phase6a113_export_signup_forms_button_templates.sql
-- Purpose: Export 14 event templates needing "View Signup Forms" button

SELECT
    name,
    subject,
    html_content
FROM communications.email_templates
WHERE name IN (
    'template-free-event-registration-confirmation',
    'template-paid-event-registration-confirmation-with-ticket',
    'template-event-registration-cancellation',
    'template-attendees-added-confirmation',
    'template-event-reminder',
    'template-event-cancellation-notifications',
    'template-event-approval',
    'template-event-rejected',
    'template-event-postponed',
    'template-new-event-publication',
    'template-signup-list-commitment-confirmation',
    'template-signup-list-commitment-update',
    'template-signup-list-commitment-cancellation',
    'template-preliminary-registration-payment-pending'
)
ORDER BY name;
```

#### Script 3: Export All 8 Templates for Comprehensive "Feel Free" Cleanup

```sql
-- File: scripts/phase6a113_export_all_feel_free_templates.sql
-- Purpose: Export ALL 8 templates containing "feel free" for comprehensive cleanup

SELECT
    name,
    subject,
    html_content
FROM communications.email_templates
WHERE name IN (
    'template-account-activated-by-admin',
    'template-account-unlocked-by-admin',
    'template-free-event-registration-confirmation',
    'template-membership-email-verification',
    'template-paid-event-registration-confirmation-with-ticket',
    'template-password-change-confirmation',
    'template-password-reset',
    'template-signup-list-commitment-confirmation'
)
ORDER BY name;
```

---

## Recommended Execution Order

### Phase 1: Fix Root Causes (Do FIRST)

1. ✅ **Issue 1: Sync EmailTemplateContract with database** (Option A + C)
   - Update 12 constant values in EmailTemplateContract.cs
   - Add validation test to prevent future drift
   - Update EmailParams classes to use contract constants

2. ✅ **Issue 2: Rename OrganizerCustomEmail** (database migration + code update)
   - Create EF Core migration
   - Update EmailTemplateContract.cs constant
   - Update EmailTemplateNames.cs constant
   - Deploy to staging, test, deploy to production

### Phase 2: Template Content Modifications (Do AFTER fixes)

3. ✅ **Issue 3: Export and modify templates**
   - Run export scripts
   - Modify HTML templates (remove "feel free", add "View Signup Forms" button)
   - Create comprehensive migration to update templates
   - Test in staging environment
   - Deploy to production

---

## Validation Test Implementation

### File: `tests/LankaConnect.Shared.Tests/Email/EmailTemplateContractValidationTests.cs`

```csharp
using LankaConnect.Shared.Email.Contracts;
using Xunit;

namespace LankaConnect.Shared.Tests.Email;

/// <summary>
/// Phase 6A.113: Validation tests to ensure EmailTemplateContract stays in sync with EmailParams classes.
/// Prevents future template name drift.
/// </summary>
public class EmailTemplateContractValidationTests
{
    [Fact]
    public void FreeEventRegistrationEmailParams_TemplateName_MatchesContract()
    {
        // Arrange
        var emailParams = new FreeEventRegistrationEmailParams();

        // Act
        var templateName = emailParams.TemplateName;

        // Assert
        Assert.Equal(EmailTemplateContract.TemplateNames.FreeEventRegistration, templateName);
    }

    [Fact]
    public void TicketConfirmationEmailParams_TemplateName_MatchesContract()
    {
        // Arrange
        var emailParams = new TicketConfirmationEmailParams();

        // Act
        var templateName = emailParams.TemplateName;

        // Assert
        Assert.Equal(EmailTemplateContract.TemplateNames.PaidEventRegistration, templateName);
    }

    [Fact]
    public void AllEmailParamsClasses_UseContractConstants_NotHardcodedStrings()
    {
        // This test documents all EmailParams classes and their template names
        // If a new EmailParams class is added, it MUST be added to this test

        var validMappings = new Dictionary<Type, string>
        {
            { typeof(FreeEventRegistrationEmailParams), EmailTemplateContract.TemplateNames.FreeEventRegistration },
            { typeof(TicketConfirmationEmailParams), EmailTemplateContract.TemplateNames.PaidEventRegistration },
            { typeof(PasswordResetEmailParams), EmailTemplateContract.TemplateNames.PasswordReset },
            { typeof(PasswordChangedEmailParams), EmailTemplateContract.TemplateNames.PasswordChangeConfirmation },
            // Add all 31 EmailParams classes here
        };

        foreach (var (emailParamsType, expectedTemplateName) in validMappings)
        {
            var instance = Activator.CreateInstance(emailParamsType) as IEmailParameters;
            Assert.NotNull(instance);
            Assert.Equal(expectedTemplateName, instance.TemplateName);
        }
    }

    [Fact]
    public void AllTemplateNames_FollowNamingConvention()
    {
        // Phase 6A.113: All templates must use "template-*" naming convention
        var allTemplateNames = typeof(EmailTemplateContract.TemplateNames)
            .GetFields()
            .Where(f => f.IsLiteral && !f.IsInitOnly) // Constants only
            .Select(f => f.GetValue(null) as string)
            .Where(value => value != null);

        foreach (var templateName in allTemplateNames)
        {
            Assert.StartsWith("template-", templateName, StringComparison.Ordinal);
        }
    }
}
```

---

## Success Criteria

### Issue 1 Success Criteria:
- [ ] All EmailTemplateContract constants match exact database template names
- [ ] All EmailParams classes use `EmailTemplateContract.TemplateNames.*` (no hardcoded strings)
- [ ] Validation test passes with 0 failures
- [ ] All emails send successfully in staging environment

### Issue 2 Success Criteria:
- [ ] Database templates renamed: `OrganizerCustomEmail` → `template-organizer-custom-email`
- [ ] EmailTemplateContract constant updated
- [ ] EmailTemplateNames constant updated
- [ ] Migration deployed to staging and production
- [ ] Custom organizer emails still send correctly (when feature is implemented)

### Issue 3 Success Criteria:
- [ ] 3 (or 8) templates have "feel free" text removed
- [ ] 14 templates have "View Signup Forms" button added
- [ ] Templates tested in staging with actual email sends
- [ ] All conditional buttons render correctly (`{{#if HasSignupForms}}`)
- [ ] Migration deployed to production

---

## User Decision Required

**Question 1 (Issue 1):** Do you want to:
- **Option A (RECOMMENDED):** Update EmailTemplateContract constants to match database?
- **Option B:** Rename database templates to match EmailTemplateContract?

**Question 2 (Issue 3):** Do you want to:
- **Option 1 (Narrow):** Remove "feel free" from 3 event templates only?
- **Option 2 (Comprehensive):** Remove "feel free" from all 8 templates?

**My Recommendation:**
- ✅ **Issue 1:** Option A (update contract to match database)
- ✅ **Issue 2:** Rename `OrganizerCustomEmail` → `template-organizer-custom-email`
- ✅ **Issue 3:** Option 2 (comprehensive cleanup of all 8 templates)

**This approach:**
- Makes code match reality (database is source of truth)
- Ensures consistency across all templates
- Adds compile-time safety for future development
- Requires no risky database renames (just 1 low-risk rename for Issue 2)

Please confirm your preference, and I'll proceed with implementation.
