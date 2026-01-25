# Email Template to Handler Mapping - Complete Reference

## Purpose
Systematic verification of ALL 19 email templates to ensure handlers send all required parameters.
Prevents literal Handlebars like `{{ParameterName}}` from appearing in emails.

## Verification Status

| # | Template Name | Handler/Job | Status | Notes |
|---|---------------|-------------|--------|-------|
| 1 | template-free-event-registration-confirmation | RegistrationConfirmedEventHandler, AnonymousRegistrationConfirmedEventHandler | ✅ VERIFIED | Phase 6A.80 - Fixed |
| 2 | template-paid-event-registration-confirmation-with-ticket | PaymentCompletedEventHandler, ResendTicketEmailCommandHandler | ⏳ PENDING | Need to verify |
| 3 | template-event-reminder | EventReminderJob | ⏳ PENDING | Need to verify |
| 4 | template-membership-email-verification | MemberVerificationRequestedEventHandler | ⏳ PENDING | Need to verify |
| 5 | template-signup-list-commitment-confirmation | UserCommittedToSignUpEventHandler | ⏳ PENDING | Need to verify |
| 6 | template-signup-list-commitment-update | CommitmentUpdatedEventHandler | ⏳ PENDING | Need to verify |
| 7 | template-signup-list-commitment-cancellation | CommitmentCancelledEmailHandler | ⏳ PENDING | Need to verify |
| 8 | template-event-registration-cancellation | RegistrationCancelledEventHandler | ⏳ PENDING | Need to verify |
| 9 | template-new-event-publication | EventPublishedEventHandler | ✅ FIXED | Phase 6A.82 - Added organizer params |
| 10 | template-event-details-publication | EventNotificationEmailJob | ✅ VERIFIED | Lines 295-351 complete |
| 11 | template-event-cancellation-notifications | EventCancellationEmailJob | ⏳ PENDING | Need to verify |
| 12 | template-event-approval | EventApprovedEventHandler | ⏳ PENDING | Need to verify |
| 13 | template-newsletter-notification | NewsletterEmailJob | ⏳ PENDING | Need to verify |
| 14 | template-newsletter-subscription-confirmation | SubscribeToNewsletterCommandHandler | ⏳ PENDING | Need to verify |
| 15 | template-password-reset | SendPasswordResetCommandHandler | ⏳ PENDING | Need to verify |
| 16 | template-password-change-confirmation | ResetPasswordCommandHandler | ⏳ PENDING | Need to verify |
| 17 | template-welcome | SendWelcomeEmailCommandHandler | ⏳ PENDING | Need to verify |
| 18 | template-organizer-role-approval | ApproveRoleUpgradeCommandHandler | ⏳ PENDING | Need to verify |
| 19 | OrganizerCustomEmail | SendBusinessNotificationCommandHandler | ⏳ PENDING | Need to verify |

---

## Template Details

### 1. template-free-event-registration-confirmation

**Used By**:
- [RegistrationConfirmedEventHandler.cs](../src/LankaConnect.Application/Events/EventHandlers/RegistrationConfirmedEventHandler.cs) (Member registrations)
- [AnonymousRegistrationConfirmedEventHandler.cs](../src/LankaConnect.Application/Events/EventHandlers/AnonymousRegistrationConfirmedEventHandler.cs) (Anonymous registrations)

**Template Parameters Expected**: (Run SQL to get from database)
- UserName
- EventTitle
- EventDateTime
- EventLocation
- RegistrationDate
- HasAttendeeDetails
- Attendees
- ContactEmail, ContactPhone, HasContactInfo
- EventImageUrl, HasEventImage
- HasOrganizerContact, OrganizerContactName, OrganizerContactEmail, OrganizerContactPhone
- EventDetailsUrl

**Handler Parameters Sent**: ✅ VERIFIED in Phase 6A.80

**Status**: ✅ Complete

---

### 2. template-paid-event-registration-confirmation-with-ticket

**Used By**:
- [PaymentCompletedEventHandler.cs:Line 245-291](../src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs#L245-L291)
- [ResendTicketEmailCommandHandler.cs](../src/LankaConnect.Application/Events/Commands/ResendTicketEmail/ResendTicketEmailCommandHandler.cs)

**Template Parameters Expected**: (Run SQL to get from database)
- TO BE VERIFIED

**Handler Parameters Sent**: (Check PaymentCompletedEventHandler.cs BuildTemplateParameters)
- TO BE CHECKED

**Status**: ⏳ PENDING

---

### 3. template-event-reminder

**Used By**:
- [EventReminderJob.cs](../src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs)

**Template Parameters Expected**: (Run SQL)
- TO BE VERIFIED

**Handler Parameters Sent**: (Check EventReminderJob)
- TO BE CHECKED

**Status**: ⏳ PENDING

---

### 4. template-membership-email-verification

**Used By**:
- [MemberVerificationRequestedEventHandler.cs](../src/LankaConnect.Application/Users/EventHandlers/MemberVerificationRequestedEventHandler.cs)

**Template Parameters Expected**: (Run SQL)
- UserName
- VerificationUrl
- TokenExpiry

**Handler Parameters Sent**: (Check handler)
- TO BE CHECKED

**Status**: ⏳ PENDING

---

### 5. template-signup-list-commitment-confirmation

**Used By**:
- [UserCommittedToSignUpEventHandler.cs](../src/LankaConnect.Application/Events/EventHandlers/UserCommittedToSignUpEventHandler.cs)

**Template Parameters Expected**: (Run SQL)
- UserName
- EventTitle
- EventDateTime
- EventLocation
- EventDetailsUrl
- SignupItem
- CommitmentType

**Handler Parameters Sent**: (Check handler)
- TO BE CHECKED

**Status**: ⏳ PENDING

---

### 6. template-signup-list-commitment-update

**Used By**:
- [CommitmentUpdatedEventHandler.cs](../src/LankaConnect.Application/Events/EventHandlers/CommitmentUpdatedEventHandler.cs)

**Template Parameters Expected**: (Run SQL)
- UserName
- EventTitle
- EventDateTime
- EventLocation
- EventDetailsUrl
- SignupItem
- CommitmentType

**Handler Parameters Sent**: (Check handler)
- TO BE CHECKED

**Status**: ⏳ PENDING

---

### 7. template-signup-list-commitment-cancellation

**Used By**:
- [CommitmentCancelledEmailHandler.cs](../src/LankaConnect.Application/Events/EventHandlers/CommitmentCancelledEmailHandler.cs)

**Template Parameters Expected**: (Run SQL)
- UserName
- EventTitle
- EventDateTime
- EventLocation
- EventDetailsUrl
- SignupItem

**Handler Parameters Sent**: (Check handler)
- TO BE CHECKED

**Status**: ⏳ PENDING

---

### 8. template-event-registration-cancellation

**Used By**:
- [RegistrationCancelledEventHandler.cs](../src/LankaConnect.Application/Events/EventHandlers/RegistrationCancelledEventHandler.cs)

**Template Parameters Expected**: (Run SQL)
- UserName
- EventTitle
- EventDateTime
- RefundAmount
- RefundCurrency

**Handler Parameters Sent**: (Check handler)
- TO BE CHECKED

**Status**: ⏳ PENDING

---

### 9. template-new-event-publication

**Used By**:
- [EventPublishedEventHandler.cs:Lines 114-145](../src/LankaConnect.Application/Events/EventHandlers/EventPublishedEventHandler.cs#L114-L145)

**Template Parameters Expected**: (From Phase 6A.39 migration)
- EventTitle, EventDescription
- EventStartDate, EventStartTime
- EventLocation, EventCity, EventState
- IsFree, IsPaid, TicketPrice
- EventUrl
- HasOrganizerContact, OrganizerName, OrganizerEmail, OrganizerPhone (added in Phase 6A.82)

**Handler Parameters Sent**: ✅ ALL MATCHED

**Status**: ✅ FIXED in Phase 6A.82

---

### 10. template-event-details-publication

**Used By**:
- [EventNotificationEmailJob.cs:Lines 295-351](../src/LankaConnect.Application/Events/BackgroundJobs/EventNotificationEmailJob.cs#L295-L351)

**Template Parameters Expected**: (From Phase 6A.61 migration)
- EventTitle, EventDescription
- EventStartDate, EventStartTime
- EventLocation, EventCity, EventState, EventUrl
- IsFree, IsPaid, TicketPrice
- HasSignUpLists, SignUpListsUrl
- HasOrganizerContact, OrganizerName, OrganizerEmail, OrganizerPhone

**Handler Parameters Sent**: ✅ ALL MATCHED

**Status**: ✅ VERIFIED

---

### 11. template-event-cancellation-notifications

**Used By**:
- [EventCancellationEmailJob.cs](../src/LankaConnect.Application/Events/BackgroundJobs/EventCancellationEmailJob.cs)

**Template Parameters Expected**: (Run SQL)
- EventTitle, EventDateTime
- CancellationReason
- UserName
- RefundInfo
- OrganizerEmail

**Handler Parameters Sent**: (Check job)
- TO BE CHECKED

**Status**: ⏳ PENDING

---

### 12. template-event-approval

**Used By**:
- [EventApprovedEventHandler.cs](../src/LankaConnect.Application/Events/EventHandlers/EventApprovedEventHandler.cs)

**Template Parameters Expected**: (Run SQL)
- OrganizerName
- EventTitle
- EventStartDate, EventStartTime
- EventLocation
- EventDetailsUrl

**Handler Parameters Sent**: (Check handler - uses EmailTemplateNames.EventApproval constant)
- TO BE CHECKED

**Status**: ⏳ PENDING

---

### 13. template-newsletter-notification

**Used By**:
- [NewsletterEmailJob.cs](../src/LankaConnect.Application/Communications/BackgroundJobs/NewsletterEmailJob.cs)

**Template Parameters Expected**: (Run SQL)
- NewsletterTitle
- NewsletterContent
- UnsubscribeUrl

**Handler Parameters Sent**: (Check job)
- TO BE CHECKED

**Status**: ⏳ PENDING

---

### 14. template-newsletter-subscription-confirmation

**Used By**:
- [SubscribeToNewsletterCommandHandler.cs](../src/LankaConnect.Application/Communications/Commands/SubscribeToNewsletter/SubscribeToNewsletterCommandHandler.cs)

**Template Parameters Expected**: (Run SQL)
- UserName
- UnsubscribeUrl

**Handler Parameters Sent**: (Check handler)
- TO BE CHECKED

**Status**: ⏳ PENDING

---

### 15. template-password-reset

**Used By**:
- [SendPasswordResetCommandHandler.cs](../src/LankaConnect.Application/Communications/Commands/SendPasswordReset/SendPasswordResetCommandHandler.cs)

**Template Parameters Expected**: (From Phase 6A.76 migration)
- UserName
- ResetLink
- (other parameters TBD)

**Handler Parameters Sent**: (Check handler)
- TO BE CHECKED

**Status**: ⏳ PENDING

---

### 16. template-password-change-confirmation

**Used By**:
- [ResetPasswordCommandHandler.cs](../src/LankaConnect.Application/Communications/Commands/ResetPassword/ResetPasswordCommandHandler.cs)

**Template Parameters Expected**: (From Phase 6A.76 migration)
- UserName
- ChangedAt

**Handler Parameters Sent**: (Check handler)
- TO BE CHECKED

**Status**: ⏳ PENDING

---

### 17. template-welcome

**Used By**:
- [SendWelcomeEmailCommandHandler.cs](../src/LankaConnect.Application/Communications/Commands/SendWelcomeEmail/SendWelcomeEmailCommandHandler.cs)

**Template Parameters Expected**: (From Phase 6A.76 migration)
- UserName
- DashboardUrl

**Handler Parameters Sent**: (Check handler)
- TO BE CHECKED

**Status**: ⏳ PENDING

---

### 18. template-organizer-role-approval

**Used By**:
- [ApproveRoleUpgradeCommandHandler.cs](../src/LankaConnect.Application/Users/Commands/ApproveRoleUpgrade/ApproveRoleUpgradeCommandHandler.cs)

**Template Parameters Expected**: (From Phase 6A.76 migration)
- UserName
- ApprovedAt
- DashboardUrl

**Handler Parameters Sent**: (Check handler)
- TO BE CHECKED

**Status**: ⏳ PENDING

---

### 19. OrganizerCustomEmail

**Used By**:
- [SendBusinessNotificationCommandHandler.cs](../src/LankaConnect.Application/Communications/Commands/SendBusinessNotification/SendBusinessNotificationCommandHandler.cs) (if exists)

**Template Parameters Expected**: (Run SQL)
- UserName
- EventTitle, EventDateTime, EventLocation, EventDetailsUrl
- CustomSubject, CustomBody
- OrganizerName, OrganizerEmail

**Handler Parameters Sent**: (Check handler)
- TO BE CHECKED

**Status**: ⏳ PENDING

---

## Verification Process

For each template:

1. **Run SQL**: `scripts/verify_all_email_templates.sql` Part 3 to get expected parameters
2. **Find Handler**: Locate handler file from table above
3. **Check Parameters**: Search for `new Dictionary<string, object>` or `BuildTemplateParameters`
4. **Compare**: Ensure ALL template parameters are sent by handler
5. **Fix if Mismatch**: Add missing parameters to handler
6. **Test**: Send test email and verify no literal Handlebars

## Common Issues Found

1. **Organizer Contact**: Templates updated to include organizer fields but handlers not updated
2. **Event Image**: Templates may expect HasEventImage/EventImageUrl but handlers don't send
3. **URL Fields**: Template expects EventDetailsUrl but handler sends EventUrl (or vice versa)
4. **Conditional Fields**: Template has `{{#HasX}}` but handler doesn't set `HasX` to false when absent

## Next Steps

1. Run SQL verification script: `scripts/verify_all_email_templates.sql`
2. For each template marked PENDING, check handler parameters
3. Document mismatches
4. Create fixes for all mismatches
5. Test each template with actual email send
6. Update this document with findings
