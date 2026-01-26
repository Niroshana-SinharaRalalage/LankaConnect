# Email Template Parameter Analysis - Phase 6A.83 Part 3
## Database Truth vs Handler Reality

**Generated**: 2026-01-25
**Database**: lankaconnect-staging-db (Azure PostgreSQL)
**Total Templates**: 18 active templates

---

## CRITICAL FINDINGS

### Templates With BOTH Old and New Parameter Names:
Many templates accept duplicate parameters because they weren't cleaned up during refactoring:

1. **Organizer Parameters**: Templates have BOTH `OrganizerContactName` AND `OrganizerName`
2. **Signup Items**: Templates have BOTH `ItemDescription` AND `ItemName`
3. **Payment Amount**: Templates have BOTH `AmountPaid` AND `TotalAmount`
4. **Unsubscribe Links**: Templates have BOTH `UnsubscribeLink` AND `UnsubscribeUrl`

**Problem**: Handlers send ONE set of parameters, templates expect BOTH, causing literal `{{ParameterName}}` to appear.

---

## TEMPLATE-BY-TEMPLATE ANALYSIS

### 1. template-event-reminder ❌ BROKEN
**Handler**: EventReminderJob.cs (Lines 218-233, 405-420)

**Template Expects** (17 parameters):
- AttendeeName, ContactEmail, EventDescription, EventDetailsUrl, EventLocation
- EventStartDate, EventStartTime, EventTitle, Location
- **OrganizerContactEmail, OrganizerContactName, OrganizerContactPhone**
- ReminderMessage, ReminderTimeframe
- **TicketCode, TicketExpiryDate** ⚠️ MISSING
- UserName

**Handler Sends**:
- ✅ UserName, EventTitle, EventStartDate, EventStartTime, EventLocation
- ✅ EventDetailsUrl, ReminderTimeframe, ReminderMessage
- ❌ **OrganizerContactName** (sends as OrganizerName - WRONG!)
- ❌ **OrganizerContactEmail** (sends as OrganizerEmail - WRONG!)
- ❌ **OrganizerContactPhone** (sends as OrganizerPhone - WRONG!)
- ❌ **TicketCode** - NOT SENT
- ❌ **TicketExpiryDate** - NOT SENT

**FIX REQUIRED**: Revert organizer parameters to OrganizerContact* names, add TicketCode and TicketExpiryDate

---

### 2. template-event-cancellation-notifications ❌ BROKEN
**Handler**: EventCancellationEmailJob.cs (Lines 208-219)

**Template Expects** (14 parameters):
- CancellationReason, ContactEmail, DashboardUrl, EventDate, EventLocation
- EventStartDate, EventStartTime, EventTitle
- **OrganizerContactEmail, OrganizerContactName, OrganizerContactPhone**
- OrganizerEmail ⚠️ (duplicate - template has BOTH)
- RefundInfo, UserName

**Handler Sends**:
- ✅ EventTitle, EventStartDate, EventStartTime, EventLocation
- ✅ CancellationReason, RefundInfo, DashboardUrl, UserName (per-recipient)
- ❌ **OrganizerEmail** (sends this) but template ALSO expects **OrganizerContactEmail**
- ❌ Missing **OrganizerContactName**, **OrganizerContactPhone**

**FIX REQUIRED**: Add OrganizerContactName, OrganizerContactEmail, OrganizerContactPhone (in addition to OrganizerEmail)

---

### 3. template-paid-event-registration-confirmation-with-ticket ❌ BROKEN
**Handler**: PaymentCompletedEventHandler.cs (Lines 169-257)

**Template Expects** (22 parameters):
- **AmountPaid**, Attendees, ContactEmail, ContactPhone, EventDateTime
- EventLocation, EventStartDate, EventStartTime, EventTitle, OrderNumber
- **OrganizerContactEmail, OrganizerContactName, OrganizerContactPhone**
- PaymentDate, PaymentIntentId, Quantity
- **TicketCode, TicketExpiryDate**, TicketType, TicketUrl
- **TotalAmount**, UserName

**Handler Sends**:
- ✅ UserName, EventTitle, EventStartDate, EventStartTime, EventLocation
- ✅ TotalAmount, Quantity, TicketType, OrderNumber, PaymentIntentId, PaymentDate
- ✅ TicketUrl, EventDetailsUrl, SignUpListsUrl
- ❌ **AmountPaid** - Template has BOTH AmountPaid AND TotalAmount (send both!)
- ❌ **TicketCode** - NOT SENT (critical for ticket display!)
- ❌ **TicketExpiryDate** - NOT SENT
- ❌ **OrganizerContactName**, **OrganizerContactEmail**, **OrganizerContactPhone** - NOT SENT

**FIX REQUIRED**: Add AmountPaid, TicketCode, TicketExpiryDate, OrganizerContact* parameters

---

### 4. template-free-event-registration-confirmation ⚠️ PARTIALLY FIXED
**Handler**: RegistrationConfirmedEventHandler.cs (Lines 127-159) + AnonymousRegistrationConfirmedEventHandler.cs

**Template Expects** (16 parameters):
- Attendees, ContactEmail, ContactPhone, EventDateTime, EventDescription
- EventDetailsUrl, EventLocation, EventStartDate, EventStartTime, EventTitle
- **OrganizerContactEmail, OrganizerContactName, OrganizerContactPhone**
- **OrganizerEmail, OrganizerName** (template has BOTH old and new!)
- UserName

**Handler Sends** (RegistrationConfirmedEventHandler):
- ✅ UserName, EventTitle, EventStartDate, EventStartTime (just fixed)
- ✅ EventLocation, Attendees, ContactEmail, ContactPhone
- ✅ EventDetailsUrl, SignUpListsUrl (just added)
- ❌ **OrganizerContactName**, **OrganizerContactEmail**, **OrganizerContactPhone** - NOT SENT
- ❌ Template also has OrganizerName, OrganizerEmail (old names) - send these too?

**FIX REQUIRED**: Add OrganizerContact* parameters

---

### 5. template-new-event-publication ❌ BROKEN
**Handler**: EventPublishedEventHandler.cs (Lines 131-145)

**Template Expects** (17 parameters):
- ContactEmail, EventCity, EventDateTime, EventDescription, EventDetailsUrl
- EventLocation, EventStartDate, EventStartTime, EventState, EventTitle, EventUrl
- **OrganizerContactEmail, OrganizerContactName, OrganizerContactPhone**
- **OrganizerName** (template has BOTH!)
- SignUpListsUrl, TicketPrice

**Handler Sends** (Phase 6A.82):
- ✅ Most event parameters
- ❌ **Organizer Name** (sent) but template ALSO expects **OrganizerContactName**
- ❌ **OrganizerEmail** (sent) but template expects **OrganizerContactEmail**
- ❌ **OrganizerPhone** (sent) but template expects **OrganizerContactPhone**

**FIX REQUIRED**: Send OrganizerContact* instead of Organizer*

---

### 6. template-event-details-publication ❌ BROKEN
**Handler**: EventNotificationEmailJob.cs (Lines 295-351)

**Template Expects** (18 parameters):
- ContactEmail, EventCity, EventDateTime, EventDescription, EventDetailsUrl
- EventLocation, EventStartDate, EventStartTime, EventState, EventTitle, EventUrl
- **OrganizerContactEmail, OrganizerContactName, OrganizerContactPhone**
- **OrganizerName** (template has BOTH!)
- SignUpListsUrl, TicketPrice, UserName

**Handler Sends**:
- ✅ Most event parameters, UserName (per-recipient - just fixed)
- ❌ **OrganizerName** (sent) but template ALSO expects **OrganizerContactName**
- ❌ **OrganizerEmail** (sent) but template expects **OrganizerContactEmail**
- ❌ **OrganizerPhone** (sent) but template expects **OrganizerContactPhone**

**FIX REQUIRED**: Send OrganizerContact* instead of Organizer*

---

### 7. template-signup-list-commitment-confirmation ⚠️ NEEDS ORGANIZER FIX
**Handler**: UserCommittedToSignUpEventHandler.cs

**Template Expects** (17 parameters):
- ContactEmail, EventDateTime, EventLocation, EventStartDate, EventStartTime
- EventTitle, EventUrl, **ItemDescription, ItemName** (BOTH!), ManageCommitmentUrl
- Notes, **OrganizerContactEmail, OrganizerContactName, OrganizerContactPhone**
- Quantity, SignUpListsUrl, UserName

**Handler Sends** (Phase 6A.83 Part 1):
- ✅ ItemName (renamed from ItemDescription), EventDetailsUrl, CommitmentType
- ❌ Template has BOTH ItemDescription AND ItemName - send both!
- ❌ **OrganizerContact*** parameters - NOT SENT

**FIX REQUIRED**: Send ItemDescription in addition to ItemName, add OrganizerContact*

---

### 8. template-signup-list-commitment-update ⚠️ NEEDS ORGANIZER FIX
**Handler**: CommitmentUpdatedEventHandler.cs (Lines 78-91)

**Template Expects** (17 parameters):
- ContactEmail, EventDateTime, EventLocation, EventStartDate, EventStartTime
- EventTitle, EventUrl, **ItemDescription, ItemName** (BOTH!), ManageCommitmentUrl
- Notes, **OrganizerContactEmail, OrganizerContactName, OrganizerContactPhone**
- Quantity, SignUpListsUrl, UserName

**Handler Sends** (Just fixed):
- ✅ ItemName (just renamed), ManageCommitmentUrl (just added), Quantity
- ❌ Template has BOTH ItemDescription AND ItemName - send both!
- ❌ **OrganizerContact*** parameters - NOT SENT

**FIX REQUIRED**: Send ItemDescription in addition to ItemName, add OrganizerContact*

---

### 9. template-signup-list-commitment-cancellation ⚠️ NEEDS ORGANIZER FIX
**Handler**: CommitmentCancelledEmailHandler.cs (Lines 78-90)

**Template Expects** (8 parameters):
- EventDate, EventDetailsUrl, EventLocation, EventTitle
- **ItemDescription, ItemName** (BOTH!), Quantity, UserName

**Handler Sends** (Just fixed):
- ✅ ItemName (just renamed), EventDetailsUrl (just added)
- ❌ Template has BOTH ItemDescription AND ItemName - send both!

**FIX REQUIRED**: Send ItemDescription in addition to ItemName

---

### 10. template-event-registration-cancellation ⚠️ NEEDS ORGANIZER FIX
**Handler**: RegistrationCancelledEventHandler.cs

**Template Expects** (12 parameters):
- CancellationDate, ContactEmail, EventDateTime, EventDetailsUrl
- EventLocation, EventStartDate, EventStartTime, EventTitle
- **OrganizerContactEmail, OrganizerContactName, OrganizerContactPhone**
- UserName

**Handler Sends** (Just fixed Phase 6A.83 Part 1):
- ✅ EventDetailsUrl, EventLocation (just added)
- ❌ **OrganizerContact*** parameters - NOT SENT

**FIX REQUIRED**: Add OrganizerContact* parameters

---

### 11. template-newsletter-notification ✅ CORRECT (Phase 6A.83 Part 2)
**Handler**: NewsletterEmailJob.cs (Lines 157-171)

**Template Expects** (10 parameters):
- DashboardUrl, EventDateTime, EventDescription, EventDetailsUrl
- EventLocation, NewsletterContent, NewsletterTitle
- SignUpListsUrl, UnsubscribeLink, UnsubscribeUrl

**Handler Sends**:
- ✅ ALL CORRECT after Phase 6A.83 Part 2 fixes

---

### 12. template-newsletter-subscription-confirmation ⚠️ HAS BOTH UnsubscribeLink AND UnsubscribeUrl
**Handler**: SubscribeToNewsletterCommandHandler.cs (Line 215)

**Template Expects** (5 parameters):
- ConfirmationLink, Email, MetroAreasText, **UnsubscribeLink, UnsubscribeUrl** (BOTH!)

**Handler Sends** (Just fixed Phase 6A.83 Part 3):
- ✅ UnsubscribeUrl (just fixed)
- ❌ Template ALSO has **UnsubscribeLink** - should send both!

**FIX REQUIRED**: Add UnsubscribeLink parameter (in addition to UnsubscribeUrl)

---

### 13. template-password-change-confirmation ✅ CORRECT (Phase 6A.83 Part 3)
**Handler**: ResetPasswordCommandHandler.cs (Line 158)

**Template Expects** (2 parameters):
- ChangedAt, UserName

**Handler Sends**:
- ✅ ChangedAt (just fixed), UserName

---

### 14. template-password-reset ✅ CORRECT
**Handler**: SendPasswordResetCommandHandler.cs

**Template Expects** (2 parameters):
- ResetLink, UserName

**Handler Sends**:
- ✅ Assumed correct (need to verify)

---

### 15. template-membership-email-verification ⚠️ NEEDS CHECK
**Handler**: MemberVerificationRequestedEventHandler.cs

**Template Expects** (3 parameters):
- **ExpirationHours**, UserName, VerificationUrl

**Handler Sends**:
- ❌ Need to verify if sends ExpirationHours or TokenExpiry

---

### 16. template-welcome ✅ CORRECT
**Handler**: SendWelcomeEmailCommandHandler.cs

**Template Expects** (5 parameters):
- DashboardUrl, Email, Name, SiteUrl, UserName

**Handler Sends**:
- ✅ Assumed correct (need to verify)

---

### 17. template-organizer-role-approval ✅ CORRECT
**Handler**: ApproveRoleUpgradeCommandHandler.cs

**Template Expects** (2 parameters):
- DashboardUrl, UserName

**Handler Sends**:
- ✅ Assumed correct

---

### 18. template-event-approval ⚠️ NEEDS CHECK
**Handler**: EventApprovedEventHandler.cs

**Template Expects** (9 parameters):
- ApprovedAt, EventDetailsUrl, EventLocation, EventManageUrl
- EventStartDate, EventStartTime, EventTitle, EventUrl, OrganizerName

**Handler Sends**:
- ❌ Need to verify parameters

---

## SUMMARY OF FIXES NEEDED

### HIGH PRIORITY (User Reported Issues):

1. **EventReminderJob.cs**:
   - Revert: OrganizerName → OrganizerContactName
   - Revert: OrganizerEmail → OrganizerContactEmail
   - Revert: OrganizerPhone → OrganizerContactPhone
   - Add: TicketCode, TicketExpiryDate

2. **PaymentCompletedEventHandler.cs**:
   - Add: AmountPaid (in addition to TotalAmount)
   - Add: TicketCode, TicketExpiryDate
   - Add: OrganizerContactName, OrganizerContactEmail, OrganizerContactPhone

3. **EventCancellationEmailJob.cs**:
   - Change: OrganizerEmail → OrganizerContactEmail
   - Add: OrganizerContactName, OrganizerContactPhone

4. **EventPublishedEventHandler.cs**:
   - Revert: OrganizerName → OrganizerContactName
   - Revert: OrganizerEmail → OrganizerContactEmail
   - Revert: OrganizerPhone → OrganizerContactPhone

5. **EventNotificationEmailJob.cs**:
   - Revert: OrganizerName → OrganizerContactName
   - Revert: OrganizerEmail → OrganizerContactEmail
   - Revert: OrganizerPhone → OrganizerContactPhone

### MEDIUM PRIORITY:

6. **RegistrationConfirmedEventHandler.cs + AnonymousRegistrationConfirmedEventHandler.cs**:
   - Add: OrganizerContactName, OrganizerContactEmail, OrganizerContactPhone

7. **Signup Handlers** (3 files):
   - Add: ItemDescription (in addition to ItemName - templates have both!)
   - Add: OrganizerContactName, OrganizerContactEmail, OrganizerContactPhone

8. **RegistrationCancelledEventHandler.cs**:
   - Add: OrganizerContactName, OrganizerContactEmail, OrganizerContactPhone

9. **SubscribeToNewsletterCommandHandler.cs**:
   - Add: UnsubscribeLink (in addition to UnsubscribeUrl - template has both!)

### LOW PRIORITY (Need Verification):

10. **MemberVerificationRequestedEventHandler.cs**: Verify ExpirationHours parameter
11. **EventApprovedEventHandler.cs**: Verify all parameters
12. **Welcome/PasswordReset handlers**: Verify all parameters

---

## ROOT CAUSE

Templates were refactored to use new parameter names (e.g., `OrganizerName` instead of `OrganizerContactName`), but the OLD parameter names were never removed from the templates. This caused:

1. Handlers sending NEW names → OLD names show as literal `{{OrganizerContactName}}`
2. Handlers sending OLD names → NEW names show as literal `{{OrganizerName}}`
3. Some templates have BOTH old and new names, requiring handlers to send BOTH

**Solution**: Either:
- **Option A**: Clean up templates to remove duplicate parameters (requires template changes)
- **Option B**: Update handlers to send ALL expected parameters (easier, safer for now)

We chose **Option B** - update handlers to match current template expectations.
