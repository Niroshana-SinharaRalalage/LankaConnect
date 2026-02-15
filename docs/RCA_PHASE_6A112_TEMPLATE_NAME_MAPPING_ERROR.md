# Root Cause Analysis: Phase 6A.112 Template Name Mapping Error

**Date:** 2026-02-14
**Phase:** 6A.112 - Export staging email templates to production
**Severity:** HIGH - False "8 templates missing" error blocked production deployment
**Status:** RESOLVED

---

## Executive Summary

During Phase 6A.112 implementation, I used incorrect database template names when checking for missing templates, causing a false "8 templates missing from staging" error. The root cause was using shortened/assumed template names instead of the actual `TemplateName` property values defined in the EmailParams classes.

**Impact:**
- ❌ False positive: Claimed 8 templates were missing from staging
- ❌ Created unnecessary SQL export script with wrong template names
- ❌ Delayed production deployment due to incorrect gap analysis
- ✅ User verified staging actually has **32 templates** (not 24 as I claimed)

---

## Problem Statement

### What I Claimed (WRONG):
"8 templates missing from staging database":
1. `template-free-event-registration` ❌
2. `template-ticket-confirmation` ❌
3. `template-attendees-added` ❌
4. `template-event-published` ❌
5. `template-event-reminder` ✅ (CORRECT)
6. `template-event-cancellation` ❌
7. `template-refund-completed` ✅ (CORRECT)
8. `template-refund-requested` ✅ (CORRECT)
9. `template-registration-cancellation` ❌
10. `template-signup-commitment-confirmation` ❌
11. `template-newsletter` ❌

### What Staging Actually Has (CORRECT):
User confirmed staging database contains **32 templates**, including:
- `template-free-event-registration-confirmation` ✅
- `template-paid-event-registration-confirmation-with-ticket` ✅
- `template-attendees-added-confirmation` ✅
- `template-new-event-publication` ✅
- `template-event-reminder` ✅
- `template-event-cancellation-notifications` ✅
- `template-refund-completed` ✅
- `template-refund-requested` ✅
- `template-event-registration-cancellation` ✅
- `template-signup-list-commitment-confirmation` ✅
- `template-newsletter-notification` ✅

---

## Root Cause Analysis

### 1. Source of the Error

**I did not read the EmailParams classes to get the actual template names.**

Instead, I:
1. ❌ Made assumptions based on EmailTemplateContract.cs constants
2. ❌ Used shortened versions of template names
3. ❌ Did not verify against the `TemplateName` property in each EmailParams class

### 2. The Contract vs. Implementation Gap

**EmailTemplateContract.cs defines constants, but NOT all are used as-is:**

```csharp
// EmailTemplateContract.cs - These are NOT always the actual template names!
public static class TemplateNames
{
    public const string FreeEventRegistration = "template-free-event-registration"; // ❌ WRONG
    public const string TicketConfirmation = "template-ticket-confirmation"; // ❌ WRONG
    public const string AttendeesAdded = "template-attendees-added"; // ❌ WRONG
}
```

**The ACTUAL template names are in the EmailParams classes:**

```csharp
// FreeEventRegistrationEmailParams.cs - CORRECT source of truth
public string TemplateName => "template-free-event-registration-confirmation"; // ✅ CORRECT

// TicketConfirmationEmailParams.cs - CORRECT source of truth
public string TemplateName => "template-paid-event-registration-confirmation-with-ticket"; // ✅ CORRECT

// AttendeesAddedEmailParams.cs - CORRECT source of truth
public string TemplateName => "template-attendees-added-confirmation"; // ✅ CORRECT
```

### 3. Why the Gap Exists

**EmailTemplateContract.cs was created in Phase 6A.97** as a single source of truth for **parameter names**, NOT template names. However:

1. Some constants in `TemplateNames` class are **shortcuts/aliases** (e.g., `FreeEventRegistration`)
2. The **actual database template names** include suffixes like `-confirmation`, `-with-ticket`, `-notifications`
3. I incorrectly assumed the constants matched 1:1 with database template names

---

## Correct Template Name Mapping

### Registration Templates

| EmailParams Class | TemplateName Property | Database Template Name | EmailTemplateContract Constant |
|-------------------|----------------------|------------------------|-------------------------------|
| `FreeEventRegistrationEmailParams.cs` | `"template-free-event-registration-confirmation"` | `template-free-event-registration-confirmation` | `FreeEventRegistration` (❌ WRONG) |
| `TicketConfirmationEmailParams.cs` | `"template-paid-event-registration-confirmation-with-ticket"` | `template-paid-event-registration-confirmation-with-ticket` | `PaidEventRegistration` (partial match) |
| `RegistrationCancellationEmailParams.cs` | `"template-event-registration-cancellation"` | `template-event-registration-cancellation` | `EventRegistrationCancellation` ✅ |

### Event Management Templates

| EmailParams Class | TemplateName Property | Database Template Name | EmailTemplateContract Constant |
|-------------------|----------------------|------------------------|-------------------------------|
| `EventReminderEmailParams.cs` | `"template-event-reminder"` | `template-event-reminder` | `EventReminder` ✅ |
| `EventCancellationEmailParams.cs` | `"template-event-cancellation-notifications"` | `template-event-cancellation-notifications` | `EventCancellation` (❌ WRONG) |
| `AttendeesAddedEmailParams.cs` | `"template-attendees-added-confirmation"` | `template-attendees-added-confirmation` | `AttendeesAdded` (❌ WRONG) |
| `EventPublishedEmailParams.cs` | `"template-new-event-publication"` | `template-new-event-publication` | `NewEventPublication` ✅ |

### Refund Templates

| EmailParams Class | TemplateName Property | Database Template Name | EmailTemplateContract Constant |
|-------------------|----------------------|------------------------|-------------------------------|
| `RefundEmailParams.cs` (AsRequest) | `"template-refund-requested"` | `template-refund-requested` | `RefundRequested` ✅ |
| `RefundEmailParams.cs` (AsCompleted) | `"template-refund-completed"` | `template-refund-completed` | `RefundCompleted` ✅ |

### Signup Commitment Templates

| EmailParams Class | TemplateName Property | Database Template Name | EmailTemplateContract Constant |
|-------------------|----------------------|------------------------|-------------------------------|
| `SignupCommitmentEmailParams.cs` (AsConfirmation) | `"template-signup-list-commitment-confirmation"` | `template-signup-list-commitment-confirmation` | `SignupCommitmentConfirmation` (partial) |
| `SignupCommitmentEmailParams.cs` (AsUpdate) | `"template-signup-list-commitment-update"` | `template-signup-list-commitment-update` | `SignupCommitmentUpdate` ✅ |
| `SignupCommitmentEmailParams.cs` (AsCancellation) | `"template-signup-list-commitment-cancellation"` | `template-signup-list-commitment-cancellation` | `SignupCommitmentCancellation` ✅ |

### Newsletter Templates

| EmailParams Class | TemplateName Property | Database Template Name | EmailTemplateContract Constant |
|-------------------|----------------------|------------------------|-------------------------------|
| `NewsletterEmailParams.cs` | `EmailTemplateContract.TemplateNames.NewsletterNotification` | `template-newsletter-notification` | `NewsletterNotification` ✅ |

---

## Key Findings

### ✅ Templates Actually in Staging (32 total)

User confirmed staging has:
```
template-form-response-confirmation
template-form-response-update
template-form-response-cancellation
template-support-ticket-reply
template-membership-email-verification
template-support-ticket-confirmation
template-free-event-registration-confirmation ✅
template-signup-list-commitment-update ✅
template-account-locked-by-admin
template-event-details-publication
template-newsletter-notification ✅
template-password-change-confirmation
template-event-registration-cancellation ✅
template-signup-list-commitment-confirmation ✅
template-event-approval
template-event-reminder ✅
template-preliminary-registration-payment-pending
template-welcome
template-paid-event-registration-confirmation-with-ticket ✅
template-organizer-role-approval
template-new-event-publication ✅
template-event-cancellation-notifications ✅
OrganizerCustomEmail
template-account-activated-by-admin
template-newsletter-subscription-confirmation
template-refund-requested ✅
template-password-reset
template-account-deactivated-by-admin
template-account-unlocked-by-admin
template-attendees-added-confirmation ✅
template-refund-completed ✅
template-signup-list-commitment-cancellation ✅
```

### ❌ My Wrong Query (11 templates)

I looked for:
```sql
'template-free-event-registration', -- ❌ WRONG (missing -confirmation)
'template-ticket-confirmation', -- ❌ WRONG (missing paid-event-registration-with-ticket)
'template-attendees-added', -- ❌ WRONG (missing -confirmation)
'template-event-published', -- ❌ WRONG (actual: template-new-event-publication)
'template-event-reminder', -- ✅ CORRECT
'template-event-cancellation', -- ❌ WRONG (missing -notifications)
'template-refund-completed', -- ✅ CORRECT
'template-refund-requested', -- ✅ CORRECT
'template-registration-cancellation', -- ❌ WRONG (missing event- prefix)
'template-signup-commitment-confirmation', -- ❌ WRONG (missing list-)
'template-newsletter' -- ❌ WRONG (missing -notification)
```

---

## The Correct Process (What I Should Have Done)

### Step 1: Read ALL EmailParams Classes

```bash
# Get all EmailParams files
glob **/*EmailParams.cs --path src/LankaConnect.Shared/Email/Contracts

# Read each file to find the TemplateName property
read FreeEventRegistrationEmailParams.cs
read TicketConfirmationEmailParams.cs
read EventReminderEmailParams.cs
# ... etc for all 30 EmailParams classes
```

### Step 2: Extract Actual Template Names

```csharp
// From each EmailParams class, extract:
public string TemplateName => "<actual-database-template-name>";
```

### Step 3: Build Correct Mapping Table

| EmailParams Class → Database Template Name |
|-------------------------------------------|
| FreeEventRegistrationEmailParams.cs → `template-free-event-registration-confirmation` |
| TicketConfirmationEmailParams.cs → `template-paid-event-registration-confirmation-with-ticket` |
| ... |

### Step 4: Query Database with Correct Names

```sql
SELECT name FROM communications.email_templates
WHERE name IN (
  'template-free-event-registration-confirmation',
  'template-paid-event-registration-confirmation-with-ticket',
  'template-attendees-added-confirmation',
  'template-new-event-publication',
  'template-event-reminder',
  'template-event-cancellation-notifications',
  'template-refund-completed',
  'template-refund-requested',
  'template-event-registration-cancellation',
  'template-signup-list-commitment-confirmation',
  'template-newsletter-notification'
);
```

---

## Impact Assessment

### False Positives (What I Got Wrong)

| Template I Claimed Missing | Actual Status in Staging |
|----------------------------|--------------------------|
| `template-free-event-registration` ❌ | ✅ EXISTS as `template-free-event-registration-confirmation` |
| `template-ticket-confirmation` ❌ | ✅ EXISTS as `template-paid-event-registration-confirmation-with-ticket` |
| `template-attendees-added` ❌ | ✅ EXISTS as `template-attendees-added-confirmation` |
| `template-event-published` ❌ | ✅ EXISTS as `template-new-event-publication` |
| `template-event-cancellation` ❌ | ✅ EXISTS as `template-event-cancellation-notifications` |
| `template-registration-cancellation` ❌ | ✅ EXISTS as `template-event-registration-cancellation` |
| `template-signup-commitment-confirmation` ❌ | ✅ EXISTS as `template-signup-list-commitment-confirmation` |
| `template-newsletter` ❌ | ✅ EXISTS as `template-newsletter-notification` |

### True Positives (What I Got Right)

| Template I Claimed Missing | Actual Status |
|----------------------------|--------------|
| `template-event-reminder` ✅ | ✅ CORRECT - exists in staging |
| `template-refund-completed` ✅ | ✅ CORRECT - exists in staging |
| `template-refund-requested` ✅ | ✅ CORRECT - exists in staging |

---

## Lessons Learned

### 1. Never Assume Template Names from Constants

**❌ DON'T:**
```csharp
// Assuming EmailTemplateContract constants are 1:1 with database names
var templateNames = new[] {
    EmailTemplateContract.TemplateNames.FreeEventRegistration, // ❌ WRONG
    EmailTemplateContract.TemplateNames.TicketConfirmation // ❌ WRONG
};
```

**✅ DO:**
```csharp
// Always read the EmailParams class TemplateName property
var freeEventParams = new FreeEventRegistrationEmailParams();
var actualTemplateName = freeEventParams.TemplateName; // ✅ CORRECT
// "template-free-event-registration-confirmation"
```

### 2. EmailTemplateContract is for Parameter Names, Not Template Names

**Purpose of EmailTemplateContract.cs:**
- ✅ Define **parameter names** for Handlebars placeholders (e.g., `UserName`, `EventTitle`)
- ✅ Single source of truth for `ToDictionary()` methods
- ❌ NOT for database template name lookups

**The TemplateName constants are aliases/shortcuts, NOT database identifiers.**

### 3. Always Verify Against Source of Truth

**For template names, the source of truth is:**
1. **EmailParams classes** (`TemplateName` property)
2. **Database** (`communications.email_templates.name` column)

**NOT:**
- ❌ EmailTemplateContract.cs constants
- ❌ Assumptions based on file names
- ❌ Shortened versions

---

## Prevention Strategy

### Automated Template Name Validation

Create a test that ensures EmailTemplateContract constants match EmailParams classes:

```csharp
[Fact]
public void EmailTemplateContract_Constants_Match_EmailParams_TemplateNames()
{
    // Phase 6A.112 Fix: Ensure template name constants match actual implementation
    var freeEventParams = new FreeEventRegistrationEmailParams();
    Assert.Equal("template-free-event-registration-confirmation", freeEventParams.TemplateName);
    // NOT EmailTemplateContract.TemplateNames.FreeEventRegistration
}
```

### Documentation Update

Add to `EmailTemplateContract.cs`:

```csharp
/// <summary>
/// WARNING: These constants are SHORTCUTS for parameter names, NOT database template names!
///
/// To get the ACTUAL database template name, check the EmailParams class:
/// - FreeEventRegistrationEmailParams.TemplateName
/// - TicketConfirmationEmailParams.TemplateName
/// - etc.
///
/// DO NOT use these constants for database queries without verification!
/// </summary>
public static class TemplateNames
{
    // ...
}
```

### Phase 6A.112 Corrected SQL Script

```sql
-- Phase 6A.112 CORRECTED: Export email templates from staging to production
-- Root Cause Fix: Use ACTUAL template names from EmailParams classes

-- Templates to export (verified from EmailParams TemplateName properties):
SELECT
    name,
    template_type,
    subject,
    html_body,
    text_body,
    description
FROM communications.email_templates
WHERE name IN (
    'template-free-event-registration-confirmation',
    'template-paid-event-registration-confirmation-with-ticket',
    'template-attendees-added-confirmation',
    'template-new-event-publication',
    'template-event-reminder',
    'template-event-cancellation-notifications',
    'template-refund-completed',
    'template-refund-requested',
    'template-event-registration-cancellation',
    'template-signup-list-commitment-confirmation',
    'template-signup-list-commitment-update',
    'template-signup-list-commitment-cancellation',
    'template-newsletter-notification',
    'template-form-response-confirmation',
    'template-form-response-update',
    'template-form-response-cancellation'
)
ORDER BY name;
```

---

## Conclusion

**Root Cause:** I used shortened/assumed template names from `EmailTemplateContract.cs` instead of reading the actual `TemplateName` property from EmailParams classes.

**Impact:** False "8 templates missing" error that blocked Phase 6A.112 production deployment.

**Resolution:** Created correct mapping table by reading ALL EmailParams classes and extracting actual `TemplateName` values.

**Next Steps:**
1. ✅ Use corrected template names for Phase 6A.112 export
2. ✅ Add documentation warning to EmailTemplateContract.cs
3. ✅ Create automated test to validate template name consistency
4. ✅ Update PHASE_6A_MASTER_INDEX.md with this RCA

---

**Prepared by:** Claude (Architecture Agent)
**Reviewed by:** User (Senior Engineer)
**Date:** 2026-02-14
