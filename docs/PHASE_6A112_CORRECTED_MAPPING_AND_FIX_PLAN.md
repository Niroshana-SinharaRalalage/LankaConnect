# Phase 6A.112 Corrected Template Mapping & Fix Plan

**Date:** 2026-02-14
**Status:** READY FOR EXECUTION
**Prerequisites:** RCA completed, corrected SQL script generated

---

## Quick Reference: What I Got Wrong vs. Reality

| My Wrong Assumption | Actual Database Template Name | Status in Staging |
|---------------------|------------------------------|-------------------|
| `template-free-event-registration` ❌ | `template-free-event-registration-confirmation` | ✅ EXISTS |
| `template-ticket-confirmation` ❌ | `template-paid-event-registration-confirmation-with-ticket` | ✅ EXISTS |
| `template-attendees-added` ❌ | `template-attendees-added-confirmation` | ✅ EXISTS |
| `template-event-published` ❌ | `template-new-event-publication` | ✅ EXISTS |
| `template-event-reminder` ✅ | `template-event-reminder` | ✅ EXISTS |
| `template-event-cancellation` ❌ | `template-event-cancellation-notifications` | ✅ EXISTS |
| `template-refund-completed` ✅ | `template-refund-completed` | ✅ EXISTS |
| `template-refund-requested` ✅ | `template-refund-requested` | ✅ EXISTS |
| `template-registration-cancellation` ❌ | `template-event-registration-cancellation` | ✅ EXISTS |
| `template-signup-commitment-confirmation` ❌ | `template-signup-list-commitment-confirmation` | ✅ EXISTS |
| `template-newsletter` ❌ | `template-newsletter-notification` | ✅ EXISTS |

**Conclusion:** I got **3/11 correct** (27% accuracy). User confirmed staging has **32 templates total**.

---

## Corrected Complete Mapping Table

### EmailParams Class → Database Template Name

| # | EmailParams Class | TemplateName Property | Database Template Name |
|---|-------------------|----------------------|------------------------|
| 1 | `FreeEventRegistrationEmailParams.cs` | `"template-free-event-registration-confirmation"` | `template-free-event-registration-confirmation` |
| 2 | `TicketConfirmationEmailParams.cs` | `"template-paid-event-registration-confirmation-with-ticket"` | `template-paid-event-registration-confirmation-with-ticket` |
| 3 | `RegistrationCancellationEmailParams.cs` | `"template-event-registration-cancellation"` | `template-event-registration-cancellation` |
| 4 | `PreliminaryRegistrationPaymentEmailParams.cs` | `"template-preliminary-registration-payment-pending"` | `template-preliminary-registration-payment-pending` |
| 5 | `EventReminderEmailParams.cs` | `"template-event-reminder"` | `template-event-reminder` |
| 6 | `EventCancellationEmailParams.cs` | `"template-event-cancellation-notifications"` | `template-event-cancellation-notifications` |
| 7 | `AttendeesAddedEmailParams.cs` | `"template-attendees-added-confirmation"` | `template-attendees-added-confirmation` |
| 8 | `EventPublishedEmailParams.cs` | `EmailTemplateContract.TemplateNames.NewEventPublication` | `template-new-event-publication` |
| 9 | `EventDetailsEmailParams.cs` | `"template-event-details-publication"` | `template-event-details-publication` |
| 10 | `EventApprovalEmailParams.cs` | `"template-event-approval"` | `template-event-approval` |
| 11 | `RefundEmailParams.cs` (AsRequest) | `"template-refund-requested"` | `template-refund-requested` |
| 12 | `RefundEmailParams.cs` (AsCompleted) | `"template-refund-completed"` | `template-refund-completed` |
| 13 | `SignupCommitmentEmailParams.cs` (AsConfirmation) | `"template-signup-list-commitment-confirmation"` | `template-signup-list-commitment-confirmation` |
| 14 | `SignupCommitmentEmailParams.cs` (AsUpdate) | `"template-signup-list-commitment-update"` | `template-signup-list-commitment-update` |
| 15 | `SignupCommitmentEmailParams.cs` (AsCancellation) | `"template-signup-list-commitment-cancellation"` | `template-signup-list-commitment-cancellation` |
| 16 | `NewsletterEmailParams.cs` | `EmailTemplateContract.TemplateNames.NewsletterNotification` | `template-newsletter-notification` |
| 17 | `NewsletterSubscriptionEmailParams.cs` | `EmailTemplateContract.TemplateNames.NewsletterSubscriptionConfirmation` | `template-newsletter-subscription-confirmation` |
| 18 | `FormResponseEmailParams.cs` (AsConfirmation) | `EmailTemplateContract.TemplateNames.FormResponseConfirmation` | `template-form-response-confirmation` |
| 19 | `FormResponseEmailParams.cs` (AsUpdate) | `EmailTemplateContract.TemplateNames.FormResponseUpdate` | `template-form-response-update` |
| 20 | `FormResponseEmailParams.cs` (AsCancellation) | `EmailTemplateContract.TemplateNames.FormResponseCancellation` | `template-form-response-cancellation` |
| 21 | `PasswordResetEmailParams.cs` | `EmailTemplateContract.TemplateNames.PasswordReset` | `template-password-reset` |
| 22 | `PasswordChangedEmailParams.cs` | `EmailTemplateContract.TemplateNames.PasswordChangeConfirmation` | `template-password-change-confirmation` |
| 23 | `WelcomeEmailParams.cs` | `"template-welcome"` | `template-welcome` |
| 24 | `AccountActivatedEmailParams.cs` | `"template-account-activated-by-admin"` | `template-account-activated-by-admin` |
| 25 | `AccountDeactivatedEmailParams.cs` | `"template-account-deactivated-by-admin"` | `template-account-deactivated-by-admin` |
| 26 | `AdminUserEmailParams.cs` (Locked) | `"template-account-locked-by-admin"` | `template-account-locked-by-admin` |
| 27 | `AdminUserEmailParams.cs` (Unlocked) | `"template-account-unlocked-by-admin"` | `template-account-unlocked-by-admin` |
| 28 | `OrganizerRoleApprovalEmailParams.cs` | `EmailTemplateContract.TemplateNames.OrganizerRoleApproval` | `template-organizer-role-approval` |
| 29 | `SupportTicketEmailParams.cs` | `"template-support-ticket-confirmation"` | `template-support-ticket-confirmation` |
| 30 | `SupportTicketReplyEmailParams.cs` | `EmailTemplateContract.TemplateNames.SupportTicketReply` | `template-support-ticket-reply` |
| 31 | N/A (Custom organizer email) | N/A | `OrganizerCustomEmail` |

**Total:** 31 EmailParams-mapped templates + 1 additional template in staging = **32 templates**

---

## Fix Plan for Phase 6A.112

### Step 1: Verify Staging Database (Run in Staging)

```sql
-- Run this query to confirm all 32 templates exist
SELECT name FROM communications.email_templates
ORDER BY name;
```

**Expected Output:** 32 rows with template names matching the table above.

### Step 2: Check Production Database (Run in Production)

```sql
-- Run this query to see which templates are missing
SELECT
    t.template_name
FROM (
    VALUES
        ('template-free-event-registration-confirmation'),
        ('template-paid-event-registration-confirmation-with-ticket'),
        ('template-attendees-added-confirmation'),
        ('template-new-event-publication'),
        ('template-event-reminder'),
        ('template-event-cancellation-notifications'),
        ('template-refund-completed'),
        ('template-refund-requested'),
        ('template-event-registration-cancellation'),
        ('template-signup-list-commitment-confirmation'),
        ('template-newsletter-notification')
        -- Add all 32 templates from the mapping table above
) AS t(template_name)
LEFT JOIN communications.email_templates et ON et.name = t.template_name
WHERE et.name IS NULL
ORDER BY t.template_name;
```

**Expected Output:** List of templates missing in production.

### Step 3: Export Templates from Staging

```bash
# Use the corrected SQL script
psql -h <staging-host> -U <user> -d <staging-db> -f scripts/phase6a112_export_staging_templates_CORRECTED.sql > staging_templates_export.json
```

### Step 4: Import Templates to Production

```bash
# Generated INSERT statements from Step 3
psql -h <production-host> -U <user> -d <production-db> -f staging_templates_import.sql
```

### Step 5: Verify Import Success

```sql
-- Run in production database
SELECT
    COUNT(*) AS total_templates,
    COUNT(CASE WHEN name LIKE 'template-%' THEN 1 END) AS system_templates,
    COUNT(CASE WHEN name = 'OrganizerCustomEmail' THEN 1 END) AS custom_templates
FROM communications.email_templates;
```

**Expected Output:** 32+ templates (production may have additional templates not in staging).

---

## Testing After Import

### Test 1: Registration Confirmation Emails

```bash
# Test free event registration email
curl -X POST https://lankaconnect-api.azurewebsites.net/api/events/{eventId}/register \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"attendees": [{"name": "Test User", "email": "test@example.com"}]}'

# Expected: Email sent with template "template-free-event-registration-confirmation"
```

### Test 2: Event Reminder Emails

```bash
# Trigger event reminder job (24hr before event)
curl -X POST https://lankaconnect-api.azurewebsites.net/api/admin/jobs/event-reminders \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Expected: Email sent with template "template-event-reminder"
```

### Test 3: Refund Emails

```bash
# Request refund for paid registration
curl -X POST https://lankaconnect-api.azurewebsites.net/api/registrations/{regId}/refund \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"reason": "Cannot attend"}'

# Expected: Email sent with template "template-refund-requested"
```

---

## Rollback Plan (If Import Fails)

### Rollback Step 1: Backup Production Templates

```sql
-- Run BEFORE importing - backup production templates
CREATE TABLE communications.email_templates_backup_20260214 AS
SELECT * FROM communications.email_templates;
```

### Rollback Step 2: Restore from Backup

```sql
-- If import fails, restore from backup
DELETE FROM communications.email_templates;

INSERT INTO communications.email_templates
SELECT * FROM communications.email_templates_backup_20260214;
```

---

## Deliverables

### 1. RCA Document ✅
- [x] File: `docs/RCA_PHASE_6A112_TEMPLATE_NAME_MAPPING_ERROR.md`
- [x] Status: COMPLETED
- [x] Root cause identified: Used EmailTemplateContract constants instead of EmailParams TemplateName properties

### 2. Corrected Mapping Table ✅
- [x] File: `docs/EMAIL_TEMPLATE_NAME_MAPPING_REFERENCE.md`
- [x] Status: COMPLETED
- [x] Contains all 31 EmailParams → Database mappings

### 3. Corrected SQL Script ✅
- [x] File: `scripts/phase6a112_export_staging_templates_CORRECTED.sql`
- [x] Status: COMPLETED
- [x] Uses ACTUAL database template names from EmailParams classes

### 4. Fix Plan ✅
- [x] File: `docs/PHASE_6A112_CORRECTED_MAPPING_AND_FIX_PLAN.md` (this document)
- [x] Status: COMPLETED
- [x] Step-by-step instructions for correcting Phase 6A.112

---

## Next Steps

1. **User Review:**
   - [ ] Review RCA document for accuracy
   - [ ] Review corrected mapping table
   - [ ] Approve SQL export script

2. **Execution:**
   - [ ] Run Step 1: Verify staging database (32 templates expected)
   - [ ] Run Step 2: Check production database (identify missing templates)
   - [ ] Run Step 3: Export templates from staging
   - [ ] Run Step 4: Import templates to production
   - [ ] Run Step 5: Verify import success

3. **Testing:**
   - [ ] Test registration confirmation emails
   - [ ] Test event reminder emails
   - [ ] Test refund emails
   - [ ] Check Azure logs for any email sending errors

4. **Documentation:**
   - [ ] Update PROGRESS_TRACKER.md with Phase 6A.112 completion
   - [ ] Update STREAMLINED_ACTION_PLAN.md
   - [ ] Add to PHASE_6A_MASTER_INDEX.md

---

## Lessons Learned Integration

### Update EmailTemplateContract.cs Documentation

Add this warning to the top of the `TemplateNames` class:

```csharp
/// <summary>
/// WARNING: These constants are SHORTCUTS for developer convenience, NOT always database template names!
///
/// CORRECT SOURCE OF TRUTH for database template names:
/// - EmailParams classes (e.g., FreeEventRegistrationEmailParams.TemplateName)
/// - EMAIL_TEMPLATE_NAME_MAPPING_REFERENCE.md
///
/// NEVER use these constants for database queries without verification!
///
/// Example of the gap:
/// - Constant: FreeEventRegistration = "template-free-event-registration" (SHORTCUT)
/// - Actual DB name: "template-free-event-registration-confirmation" (CORRECT)
///
/// See: RCA_PHASE_6A112_TEMPLATE_NAME_MAPPING_ERROR.md
/// </summary>
public static class TemplateNames
{
    // ...
}
```

### Create Automated Validation Test

Add to test suite:

```csharp
[Fact]
public void EmailTemplateContract_WarningDocumentation_Exists()
{
    // Ensure EmailTemplateContract.cs has the warning documentation
    var contractFile = File.ReadAllText("src/LankaConnect.Shared/Email/Contracts/EmailTemplateContract.cs");
    Assert.Contains("WARNING: These constants are SHORTCUTS", contractFile);
}
```

---

**STATUS:** Ready for user review and approval to proceed with execution.

**CRITICAL:** Do NOT proceed with Phase 6A.112 execution until user approves this corrected mapping and fix plan.
