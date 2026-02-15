# Phase 6A.113: Template Name Comparison Table

**Date:** 2026-02-14
**Purpose:** Visual comparison of EmailTemplateContract constants vs. actual database template names

---

## Complete Template Name Mapping

| # | Contract Constant | Contract Value | EmailParams Class | EmailParams Returns | Database Name | Status | Fix Required |
|---|------------------|----------------|-------------------|---------------------|---------------|--------|--------------|
| 1 | `PasswordReset` | `template-password-reset` | `PasswordResetEmailParams` | `"template-password-reset"` | `template-password-reset` | ✅ MATCH | None |
| 2 | `PasswordChangeConfirmation` | `template-password-change-confirmation` | `PasswordChangedEmailParams` | `"template-password-change-confirmation"` | `template-password-change-confirmation` | ✅ MATCH | None |
| 3 | `EmailVerification` | `template-email-verification` ❌ | `EmailVerificationEmailParams` | `"template-membership-email-verification"` | `template-membership-email-verification` | ❌ MISMATCH | Update constant → `template-membership-email-verification` |
| 4 | `WelcomeEmail` | `template-welcome-email` ❌ | `WelcomeEmailParams` | `"template-welcome"` | `template-welcome` | ❌ MISMATCH | Update constant → `template-welcome` |
| 5 | `NewEventPublication` | `template-new-event-publication` | `EventPublishedEmailParams` | `EmailTemplateContract.TemplateNames.NewEventPublication` | `template-new-event-publication` | ✅ MATCH | None |
| 6 | `EventDetailsPublication` | `template-event-details-publication` | `EventDetailsEmailParams` | `EmailTemplateContract.TemplateNames.EventDetailsPublication` | `template-event-details-publication` | ✅ MATCH | None |
| 7 | `PaidEventRegistration` | `template-paid-event-registration` ❌ | `TicketConfirmationEmailParams` | `"template-paid-event-registration-confirmation-with-ticket"` | `template-paid-event-registration-confirmation-with-ticket` | ❌ MISMATCH | Update constant → `template-paid-event-registration-confirmation-with-ticket` |
| 8 | `FreeEventRegistration` | `template-free-event-registration` ❌ | `FreeEventRegistrationEmailParams` | `"template-free-event-registration-confirmation"` | `template-free-event-registration-confirmation` | ❌ MISMATCH | Update constant → `template-free-event-registration-confirmation` |
| 9 | `EventRegistrationCancellation` | `template-event-registration-cancellation` | `RegistrationCancellationEmailParams` | `"template-event-registration-cancellation"` | `template-event-registration-cancellation` | ✅ MATCH | None |
| 10 | `TicketConfirmation` | `template-ticket-confirmation` ❌ | _(None)_ | N/A | _(Does not exist)_ | ❌ UNUSED | Delete constant |
| 11 | `RefundRequested` | `template-refund-requested` | `RefundEmailParams` (AsRequest) | `"template-refund-requested"` | `template-refund-requested` | ✅ MATCH | None |
| 12 | `RefundCompleted` | `template-refund-completed` | `RefundEmailParams` (AsCompleted) | `"template-refund-completed"` | `template-refund-completed` | ✅ MATCH | None |
| 13 | `EventApproved` | `template-event-approved` ❌ | `EventApprovalEmailParams` | `EmailTemplateContract.TemplateNames.EventApproved` | `template-event-approval` | ❌ MISMATCH | Rename constant → `EventApproval`, update value → `template-event-approval` |
| 14 | `EventRejected` | `template-event-rejected` | `EventRejectedEmailParams` | `EmailTemplateContract.TemplateNames.EventRejected` | `template-event-rejected` | ✅ MATCH | None |
| 15 | `EventPostponed` | `template-event-postponed` | `EventPostponedEmailParams` | `EmailTemplateContract.TemplateNames.EventPostponed` | `template-event-postponed` | ✅ MATCH | None |
| 16 | `EventCancellation` | `template-event-cancellation` ❌ | `EventCancellationEmailParams` | `"template-event-cancellation-notifications"` | `template-event-cancellation-notifications` | ❌ MISMATCH | Update constant → `template-event-cancellation-notifications` |
| 17 | `EventReminder` | `template-event-reminder` | `EventReminderEmailParams` | `"template-event-reminder"` | `template-event-reminder` | ✅ MATCH | None |
| 18 | `EventReminder24Hr` | `template-event-reminder-24hr` ❌ | _(None)_ | N/A | _(Does not exist)_ | ❌ UNUSED | Delete constant |
| 19 | `AttendeesAdded` | `template-attendees-added` ❌ | `AttendeesAddedEmailParams` | `"template-attendees-added-confirmation"` | `template-attendees-added-confirmation` | ❌ MISMATCH | Update constant → `template-attendees-added-confirmation` |
| 20 | `SignupCommitmentConfirmation` | `template-signup-list-commitment-confirmation` | `SignupCommitmentEmailParams` (AsConfirmation) | `"template-signup-list-commitment-confirmation"` | `template-signup-list-commitment-confirmation` | ✅ MATCH | None |
| 21 | `SignupCommitmentUpdate` | `template-signup-list-commitment-update` | `SignupCommitmentEmailParams` (AsUpdate) | `"template-signup-list-commitment-update"` | `template-signup-list-commitment-update` | ✅ MATCH | None |
| 22 | `SignupCommitmentCancellation` | `template-signup-list-commitment-cancellation` | `SignupCommitmentEmailParams` (AsCancellation) | `"template-signup-list-commitment-cancellation"` | `template-signup-list-commitment-cancellation` | ✅ MATCH | None |
| 23 | `SupportTicketReceived` | `template-support-ticket-received` ❌ | `SupportTicketEmailParams` | `"template-support-ticket-confirmation"` | `template-support-ticket-confirmation` | ❌ MISMATCH | Rename constant → `SupportTicketConfirmation`, update value → `template-support-ticket-confirmation` |
| 24 | `SupportTicketReply` | `template-support-ticket-reply` | `SupportTicketReplyEmailParams` | `EmailTemplateContract.TemplateNames.SupportTicketReply` | `template-support-ticket-reply` | ✅ MATCH | None |
| 25 | `AdminUserActivation` | `template-admin-user-activation` ❌ | `AccountActivatedEmailParams` | `EmailTemplateContract.TemplateNames.AdminUserActivation` | `template-account-activated-by-admin` | ❌ MISMATCH | Rename constant → `AccountActivatedByAdmin`, update value → `template-account-activated-by-admin` |
| 26 | `AdminUserDeactivation` | `template-admin-user-deactivation` ❌ | `AccountDeactivatedEmailParams` | `EmailTemplateContract.TemplateNames.AdminUserDeactivation` | `template-account-deactivated-by-admin` | ❌ MISMATCH | Rename constant → `AccountDeactivatedByAdmin`, update value → `template-account-deactivated-by-admin` |
| 27 | `OrganizerRoleApproval` | `template-organizer-role-approval` | `OrganizerRoleApprovalEmailParams` | `EmailTemplateContract.TemplateNames.OrganizerRoleApproval` | `template-organizer-role-approval` | ✅ MATCH | None |
| 28 | `NewsletterSubscriptionConfirmation` | `template-newsletter-subscription-confirmation` | `NewsletterSubscriptionEmailParams` | `EmailTemplateContract.TemplateNames.NewsletterSubscriptionConfirmation` | `template-newsletter-subscription-confirmation` | ✅ MATCH | None |
| 29 | `NewsletterNotification` | `template-newsletter-notification` | `NewsletterEmailParams` | `EmailTemplateContract.TemplateNames.NewsletterNotification` | `template-newsletter-notification` | ✅ MATCH | None |
| 30 | `FormResponseConfirmation` | `template-form-response-confirmation` | `FormResponseEmailParams` (AsConfirmation) | `EmailTemplateContract.TemplateNames.FormResponseConfirmation` | `template-form-response-confirmation` | ✅ MATCH | None |
| 31 | `FormResponseUpdate` | `template-form-response-update` | `FormResponseEmailParams` (AsUpdate) | `EmailTemplateContract.TemplateNames.FormResponseUpdate` | `template-form-response-update` | ✅ MATCH | None |
| 32 | `FormResponseCancellation` | `template-form-response-cancellation` | `FormResponseEmailParams` (AsCancellation) | `EmailTemplateContract.TemplateNames.FormResponseCancellation` | `template-form-response-cancellation` | ✅ MATCH | None |
| 33 | `PreliminaryRegistrationPayment` | `template-preliminary-registration-payment-pending` | `PreliminaryRegistrationPaymentEmailParams` | `"template-preliminary-registration-payment-pending"` | `template-preliminary-registration-payment-pending` | ✅ MATCH | None |

---

## Summary Statistics

- **Total Constants:** 33
- **✅ Matching:** 21 (63.6%)
- **❌ Mismatches:** 10 (30.3%)
- **❌ Unused:** 2 (6.1%)

---

## Fix Actions Required

### Constant Value Updates (10)

| Line # in EmailTemplateContract.cs | Constant Name | Current Value | New Value |
|-----------------------------------|--------------|---------------|-----------|
| ~31 | `PasswordReset` | ✅ CORRECT | No change |
| ~32 | `PasswordChangeConfirmation` | ✅ CORRECT | No change |
| ~33 | `EmailVerification` | `template-email-verification` | `template-membership-email-verification` |
| ~34 | `WelcomeEmail` | `template-welcome-email` | `template-welcome` |
| ~41 | `PaidEventRegistration` | `template-paid-event-registration` | `template-paid-event-registration-confirmation-with-ticket` |
| ~42 | `FreeEventRegistration` | `template-free-event-registration` | `template-free-event-registration-confirmation` |
| ~52 | `EventApproved` | `template-event-approved` | `template-event-approval` |
| ~54 | `EventCancellation` | `template-event-cancellation` | `template-event-cancellation-notifications` |
| ~57 | `AttendeesAdded` | `template-attendees-added` | `template-attendees-added-confirmation` |
| ~65 | `SupportTicketReceived` | `template-support-ticket-received` | `template-support-ticket-confirmation` |
| ~69 | `AdminUserActivation` | `template-admin-user-activation` | `template-account-activated-by-admin` |
| ~70 | `AdminUserDeactivation` | `template-admin-user-deactivation` | `template-account-deactivated-by-admin` |

### Constants to Delete (2)

| Line # | Constant Name | Reason |
|--------|--------------|--------|
| ~44 | `TicketConfirmation` | No EmailParams class uses this - merged into `PaidEventRegistration` |
| ~56 | `EventReminder24Hr` | Not in database, no EmailParams class, feature not implemented |

### Constant Renames (3)

| Old Constant Name | New Constant Name | Reason |
|------------------|------------------|--------|
| `EventApproved` | `EventApproval` | Match database naming: `template-event-approval` |
| `SupportTicketReceived` | `SupportTicketConfirmation` | Match database naming: `template-support-ticket-confirmation` |
| `AdminUserActivation` | `AccountActivatedByAdmin` | Match database naming: `template-account-activated-by-admin` |
| `AdminUserDeactivation` | `AccountDeactivatedByAdmin` | Match database naming: `template-account-deactivated-by-admin` |

---

## Special Case: OrganizerCustomEmail

**Current Database Name:** `OrganizerCustomEmail` ❌ (breaks `template-*` convention)
**Should Be:** `template-organizer-custom-email`

**Fix:** Database migration required (Issue 2)

---

## EmailParams Classes That Need Updates

After fixing EmailTemplateContract, these classes need to use contract constants:

| EmailParams Class | Current Code | Should Be |
|------------------|--------------|-----------|
| `FreeEventRegistrationEmailParams` | `"template-free-event-registration-confirmation"` | `EmailTemplateContract.TemplateNames.FreeEventRegistration` |
| `TicketConfirmationEmailParams` | `"template-paid-event-registration-confirmation-with-ticket"` | `EmailTemplateContract.TemplateNames.PaidEventRegistration` |
| `RegistrationCancellationEmailParams` | `"template-event-registration-cancellation"` | `EmailTemplateContract.TemplateNames.EventRegistrationCancellation` |
| `EventReminderEmailParams` | `"template-event-reminder"` | `EmailTemplateContract.TemplateNames.EventReminder` |
| `EventCancellationEmailParams` | `"template-event-cancellation-notifications"` | `EmailTemplateContract.TemplateNames.EventCancellation` |
| `AttendeesAddedEmailParams` | `"template-attendees-added-confirmation"` | `EmailTemplateContract.TemplateNames.AttendeesAdded` |
| `PasswordResetEmailParams` | `"template-password-reset"` | `EmailTemplateContract.TemplateNames.PasswordReset` |
| `PasswordChangedEmailParams` | `"template-password-change-confirmation"` | `EmailTemplateContract.TemplateNames.PasswordChangeConfirmation` |
| `EmailVerificationEmailParams` | `"template-membership-email-verification"` | `EmailTemplateContract.TemplateNames.EmailVerification` |
| `WelcomeEmailParams` | `"template-welcome"` | `EmailTemplateContract.TemplateNames.WelcomeEmail` |
| `PreliminaryRegistrationPaymentEmailParams` | `"template-preliminary-registration-payment-pending"` | `EmailTemplateContract.TemplateNames.PreliminaryRegistrationPayment` |

**Total:** 11 EmailParams classes need hardcoded strings replaced with contract constants

---

## Verification Checklist

After applying fixes:

- [ ] All EmailTemplateContract constants match database template names
- [ ] No hardcoded template name strings in EmailParams classes
- [ ] Validation test passes with 0 failures
- [ ] `OrganizerCustomEmail` renamed to `template-organizer-custom-email`
- [ ] All templates follow `template-*` naming convention
- [ ] Build succeeds with no compilation errors
- [ ] All emails send successfully in staging environment

---

## Related Documents

- **Full Analysis:** `PHASE_6A113_EMAIL_TEMPLATE_NAME_CONSISTENCY_FIX.md`
- **Quick Reference:** `PHASE_6A113_QUICK_REFERENCE.md`
- **Export Scripts:** `scripts/phase6a113_*.sql`
