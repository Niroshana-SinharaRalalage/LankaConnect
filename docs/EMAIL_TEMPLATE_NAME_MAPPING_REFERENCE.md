# Email Template Name Mapping Reference

**Purpose:** Single source of truth for mapping EmailParams classes to database template names

**Created:** 2026-02-14 (Phase 6A.112 RCA)

**CRITICAL:** Always refer to this document when querying email templates in the database!

---

## Why This Document Exists

**Problem:** `EmailTemplateContract.cs` contains template name constants that are **shortcuts/aliases**, NOT actual database template names.

**Example of the Gap:**
```csharp
// EmailTemplateContract.cs
public const string FreeEventRegistration = "template-free-event-registration"; // ❌ NOT database name

// FreeEventRegistrationEmailParams.cs
public string TemplateName => "template-free-event-registration-confirmation"; // ✅ ACTUAL database name
```

**Solution:** This document provides the authoritative mapping from EmailParams classes to database template names.

---

## Complete Template Name Mapping

### Registration Templates (4 templates)

| EmailParams Class | File Location | TemplateName Property | Database Template Name | Notes |
|-------------------|--------------|----------------------|------------------------|-------|
| `FreeEventRegistrationEmailParams` | `Contracts/FreeEventRegistrationEmailParams.cs` | `"template-free-event-registration-confirmation"` | `template-free-event-registration-confirmation` | Free event registration confirmation |
| `TicketConfirmationEmailParams` | `Contracts/TicketConfirmationEmailParams.cs` | `"template-paid-event-registration-confirmation-with-ticket"` | `template-paid-event-registration-confirmation-with-ticket` | Paid event registration with ticket |
| `RegistrationCancellationEmailParams` | `Contracts/RegistrationCancellationEmailParams.cs` | `"template-event-registration-cancellation"` | `template-event-registration-cancellation` | User cancels registration |
| `PreliminaryRegistrationPaymentEmailParams` | `Contracts/PreliminaryRegistrationPaymentEmailParams.cs` | `"template-preliminary-registration-payment-pending"` | `template-preliminary-registration-payment-pending` | Payment pending for preliminary registration |

### Event Management Templates (6 templates)

| EmailParams Class | File Location | TemplateName Property | Database Template Name | Notes |
|-------------------|--------------|----------------------|------------------------|-------|
| `EventReminderEmailParams` | `Contracts/EventReminderEmailParams.cs` | `"template-event-reminder"` | `template-event-reminder` | Event reminder (24hr, 1 week, etc.) |
| `EventCancellationEmailParams` | `Contracts/EventCancellationEmailParams.cs` | `"template-event-cancellation-notifications"` | `template-event-cancellation-notifications` | Event cancelled by organizer |
| `AttendeesAddedEmailParams` | `Contracts/AttendeesAddedEmailParams.cs` | `"template-attendees-added-confirmation"` | `template-attendees-added-confirmation` | Add-only attendees confirmation |
| `EventPublishedEmailParams` | `Contracts/EventPublishedEmailParams.cs` | `EmailTemplateContract.TemplateNames.NewEventPublication` | `template-new-event-publication` | Event published notification |
| `EventDetailsEmailParams` | `Contracts/EventDetailsEmailParams.cs` | `"template-event-details-publication"` | `template-event-details-publication` | Event details updated |
| `EventApprovalEmailParams` | `Contracts/EventApprovalEmailParams.cs` | `"template-event-approval"` | `template-event-approval` | Event approved by admin |

### Refund Templates (2 templates)

| EmailParams Class | File Location | TemplateName Property | Database Template Name | Notes |
|-------------------|--------------|----------------------|------------------------|-------|
| `RefundEmailParams` (AsRequest) | `Contracts/RefundEmailParams.cs` | `"template-refund-requested"` | `template-refund-requested` | Refund request submitted |
| `RefundEmailParams` (AsCompleted) | `Contracts/RefundEmailParams.cs` | `"template-refund-completed"` | `template-refund-completed` | Refund completed |

### Signup Commitment Templates (3 templates)

| EmailParams Class | File Location | TemplateName Property | Database Template Name | Notes |
|-------------------|--------------|----------------------|------------------------|-------|
| `SignupCommitmentEmailParams` (AsConfirmation) | `Contracts/SignupCommitmentEmailParams.cs` | `"template-signup-list-commitment-confirmation"` | `template-signup-list-commitment-confirmation` | User commits to signup list |
| `SignupCommitmentEmailParams` (AsUpdate) | `Contracts/SignupCommitmentEmailParams.cs` | `"template-signup-list-commitment-update"` | `template-signup-list-commitment-update` | User updates commitment |
| `SignupCommitmentEmailParams` (AsCancellation) | `Contracts/SignupCommitmentEmailParams.cs` | `"template-signup-list-commitment-cancellation"` | `template-signup-list-commitment-cancellation` | User cancels commitment |

### Newsletter Templates (2 templates)

| EmailParams Class | File Location | TemplateName Property | Database Template Name | Notes |
|-------------------|--------------|----------------------|------------------------|-------|
| `NewsletterEmailParams` | `Contracts/NewsletterEmailParams.cs` | `EmailTemplateContract.TemplateNames.NewsletterNotification` | `template-newsletter-notification` | Newsletter notification |
| `NewsletterSubscriptionEmailParams` | `Contracts/NewsletterSubscriptionEmailParams.cs` | `EmailTemplateContract.TemplateNames.NewsletterSubscriptionConfirmation` | `template-newsletter-subscription-confirmation` | Newsletter subscription confirmed |

### Form Response Templates (3 templates)

| EmailParams Class | File Location | TemplateName Property | Database Template Name | Notes |
|-------------------|--------------|----------------------|------------------------|-------|
| `FormResponseEmailParams` (AsConfirmation) | `Contracts/FormResponseEmailParams.cs` | `EmailTemplateContract.TemplateNames.FormResponseConfirmation` | `template-form-response-confirmation` | Form response submitted (Phase 6A.107) |
| `FormResponseEmailParams` (AsUpdate) | `Contracts/FormResponseEmailParams.cs` | `EmailTemplateContract.TemplateNames.FormResponseUpdate` | `template-form-response-update` | Form response updated (Phase 6A.107) |
| `FormResponseEmailParams` (AsCancellation) | `Contracts/FormResponseEmailParams.cs` | `EmailTemplateContract.TemplateNames.FormResponseCancellation` | `template-form-response-cancellation` | Form response cancelled (Phase 6A.107) |

### Admin/User Management Templates (8 templates)

| EmailParams Class | File Location | TemplateName Property | Database Template Name | Notes |
|-------------------|--------------|----------------------|------------------------|-------|
| `PasswordResetEmailParams` | `Contracts/PasswordResetEmailParams.cs` | `EmailTemplateContract.TemplateNames.PasswordReset` | `template-password-reset` | Password reset requested |
| `PasswordChangedEmailParams` | `Contracts/PasswordChangedEmailParams.cs` | `EmailTemplateContract.TemplateNames.PasswordChangeConfirmation` | `template-password-change-confirmation` | Password changed confirmation |
| `WelcomeEmailParams` | `Contracts/WelcomeEmailParams.cs` | `"template-welcome"` | `template-welcome` | Welcome new user |
| `AccountActivatedEmailParams` | `Contracts/AccountActivatedEmailParams.cs` | `"template-account-activated-by-admin"` | `template-account-activated-by-admin` | Admin activated account |
| `AccountDeactivatedEmailParams` | `Contracts/AccountDeactivatedEmailParams.cs` | `"template-account-deactivated-by-admin"` | `template-account-deactivated-by-admin` | Admin deactivated account |
| `AdminUserEmailParams` (Locked) | `Contracts/AdminUserEmailParams.cs` | `"template-account-locked-by-admin"` | `template-account-locked-by-admin` | Admin locked account |
| `AdminUserEmailParams` (Unlocked) | `Contracts/AdminUserEmailParams.cs` | `"template-account-unlocked-by-admin"` | `template-account-unlocked-by-admin` | Admin unlocked account |
| `OrganizerRoleApprovalEmailParams` | `Contracts/OrganizerRoleApprovalEmailParams.cs` | `EmailTemplateContract.TemplateNames.OrganizerRoleApproval` | `template-organizer-role-approval` | Organizer role approved |

### Support Templates (2 templates)

| EmailParams Class | File Location | TemplateName Property | Database Template Name | Notes |
|-------------------|--------------|----------------------|------------------------|-------|
| `SupportTicketEmailParams` | `Contracts/SupportTicketEmailParams.cs` | `"template-support-ticket-confirmation"` | `template-support-ticket-confirmation` | Support ticket received |
| `SupportTicketReplyEmailParams` | `Contracts/SupportTicketReplyEmailParams.cs` | `EmailTemplateContract.TemplateNames.SupportTicketReply` | `template-support-ticket-reply` | Support ticket reply |

### Custom Templates (1 template)

| EmailParams Class | File Location | TemplateName Property | Database Template Name | Notes |
|-------------------|--------------|----------------------|------------------------|-------|
| N/A (Organizer sends custom emails) | N/A | N/A | `OrganizerCustomEmail` | Custom email template for organizers |

---

## Summary Statistics

| Category | Template Count |
|----------|---------------|
| Registration Templates | 4 |
| Event Management Templates | 6 |
| Refund Templates | 2 |
| Signup Commitment Templates | 3 |
| Newsletter Templates | 2 |
| Form Response Templates | 3 |
| Admin/User Management Templates | 8 |
| Support Templates | 2 |
| Custom Templates | 1 |
| **TOTAL** | **31** |

**Note:** User confirmed staging has **32 templates**, which includes `template-membership-email-verification` not yet mapped to an EmailParams class.

---

## Usage Examples

### ❌ WRONG: Using EmailTemplateContract constants

```csharp
// DON'T DO THIS - Constants are shortcuts, not database names!
var templateNames = new[] {
    EmailTemplateContract.TemplateNames.FreeEventRegistration, // "template-free-event-registration" ❌
    EmailTemplateContract.TemplateNames.TicketConfirmation // "template-ticket-confirmation" ❌
};

// Query will FAIL - templates not found!
var templates = await dbContext.EmailTemplates
    .Where(t => templateNames.Contains(t.Name))
    .ToListAsync();
```

### ✅ CORRECT: Using EmailParams TemplateName property

```csharp
// DO THIS - Get actual database template name from EmailParams class
var freeEventParams = new FreeEventRegistrationEmailParams();
var actualTemplateName = freeEventParams.TemplateName; // "template-free-event-registration-confirmation" ✅

// Query will SUCCEED
var template = await dbContext.EmailTemplates
    .FirstOrDefaultAsync(t => t.Name == actualTemplateName);
```

### ✅ CORRECT: Using this reference document

```sql
-- Refer to this document for database queries
SELECT * FROM communications.email_templates
WHERE name IN (
    'template-free-event-registration-confirmation', -- ✅ From reference doc
    'template-paid-event-registration-confirmation-with-ticket', -- ✅ From reference doc
    'template-event-reminder' -- ✅ From reference doc
);
```

---

## Validation Script

Run this script to verify all EmailParams classes match database template names:

```csharp
public class EmailTemplateNameValidationTests
{
    [Fact]
    public void All_EmailParams_TemplateNames_Match_Database()
    {
        // Phase 6A.112 Fix: Ensure EmailParams TemplateName properties match database

        var expectedMappings = new Dictionary<string, string>
        {
            // Registration templates
            { "FreeEventRegistrationEmailParams", "template-free-event-registration-confirmation" },
            { "TicketConfirmationEmailParams", "template-paid-event-registration-confirmation-with-ticket" },
            { "RegistrationCancellationEmailParams", "template-event-registration-cancellation" },

            // Event management templates
            { "EventReminderEmailParams", "template-event-reminder" },
            { "EventCancellationEmailParams", "template-event-cancellation-notifications" },
            { "AttendeesAddedEmailParams", "template-attendees-added-confirmation" },
            { "EventPublishedEmailParams", "template-new-event-publication" },

            // Refund templates
            { "RefundEmailParams.AsRequest", "template-refund-requested" },
            { "RefundEmailParams.AsCompleted", "template-refund-completed" },

            // Signup commitment templates
            { "SignupCommitmentEmailParams.AsConfirmation", "template-signup-list-commitment-confirmation" },
            { "SignupCommitmentEmailParams.AsUpdate", "template-signup-list-commitment-update" },
            { "SignupCommitmentEmailParams.AsCancellation", "template-signup-list-commitment-cancellation" },

            // Newsletter templates
            { "NewsletterEmailParams", "template-newsletter-notification" },
        };

        // Verify each mapping
        foreach (var mapping in expectedMappings)
        {
            var className = mapping.Key;
            var expectedTemplateName = mapping.Value;

            // Instantiate EmailParams class and check TemplateName property
            // (Add actual reflection code here)
        }
    }
}
```

---

## Migration Strategy

### When Adding a New Email Template

1. **Create EmailParams class** with `TemplateName` property
2. **Add to this reference document** in the appropriate category
3. **Create database migration** using the EXACT name from TemplateName
4. **Update validation tests** to include new template

### When Querying Email Templates

1. **Check this reference document** for the correct database template name
2. **Do NOT use** EmailTemplateContract constants directly
3. **Use EmailParams.TemplateName** property in code

---

## Related Documents

- **RCA:** [RCA_PHASE_6A112_TEMPLATE_NAME_MAPPING_ERROR.md](./RCA_PHASE_6A112_TEMPLATE_NAME_MAPPING_ERROR.md)
- **SQL Script:** [phase6a112_export_staging_templates_CORRECTED.sql](../scripts/phase6a112_export_staging_templates_CORRECTED.sql)
- **Contract:** [EmailTemplateContract.cs](../src/LankaConnect.Shared/Email/Contracts/EmailTemplateContract.cs)
- **EmailParams Classes:** `src/LankaConnect.Shared/Email/Contracts/*EmailParams.cs`

---

**Last Updated:** 2026-02-14 (Phase 6A.112)
**Maintained By:** Architecture Team
**Review Frequency:** After each new email template addition
