# Phase 6A.112: Comprehensive Email Template Update - Implementation Plan

**Date**: 2026-02-14
**Status**: Planning Complete - Ready for Implementation

---

## Overview

ONE migration to update ALL email templates with three improvements:
1. **Issue 2**: Professional styling for 3 form response templates
2. **Issue 3**: Add "View Signup Forms" button to 11 event email templates
3. **DoNotReply Fix**: Remove "feel free to reply" text from all templates

---

## Templates to Update

### Group 1: Form Response Templates (Issue 2) - 3 templates
✅ **Already prepared** in `Template_Correction/staging/`

1. `template-form-response-confirmation`
2. `template-form-response-update`
3. `template-form-response-cancellation`

**Changes**:
- Professional Phase 6A.96 styling
- Gradient header/footer
- Responsive design
- "View Signup Lists" button (conditional)
- "View Signup Forms" button (cancellation only, conditional)
- **Pluralized**: "forms" not "form" ✅

---

### Group 2: Event Email Templates (Issue 3) - 11 templates
⏳ **Need to download from staging and modify**

**Templates with HasSignUpLists (need HasSignupForms added)**:

1. **template-free-event-registration-confirmation**
   - EmailParams: `FreeEventRegistrationEmailParams`
   - Handler: `RegistrationConfirmedEventHandler` (for free events)

2. **template-paid-event-registration-confirmation-with-ticket**
   - EmailParams: `TicketConfirmationEmailParams`
   - Handler: `PaymentCompletedEventHandler`

3. **template-attendees-added**
   - EmailParams: `AttendeesAddedEmailParams`
   - Handler: `AttendeesAddedEventHandler`

4. **template-event-publication** (when event becomes Active)
   - EmailParams: `EventPublishedEmailParams`
   - Handler: `EventPublishedEventHandler`

5. **template-new-event-publication** (when event is first created)
   - EmailParams: `EventDetailsEmailParams`
   - Handler: (used in multiple places)

6. **template-event-reminder**
   - EmailParams: `EventReminderEmailParams`
   - Handler: (background service)

7. **template-event-cancellation-notifications**
   - EmailParams: `EventCancellationEmailParams`
   - Handler: (event cancellation flow)

8. **template-event-registration-cancellation**
   - EmailParams: `RegistrationCancellationEmailParams`
   - Handler: `RegistrationCancelledEventHandler`

9. **template-refund-requested**
   - EmailParams: `RefundEmailParams`
   - Handler: `RefundRequestedEventHandler`

10. **template-refund-completed**
    - EmailParams: `RefundEmailParams` (same as above)
    - Handler: `RefundCompletedEventHandler`

11. **template-signup-list-commitment-confirmation**
    - EmailParams: `SignupCommitmentEmailParams`
    - Handler: `UserCommittedToSignUpEventHandler`

**Note**: `NewsletterEmailParams` has `HasSignUpLists` property but may not need signup forms button (no event context)

---

### Group 3: Templates with "Feel Free to Reply" Text - ~8 templates
⏳ **Need to identify exact templates and remove text**

**Search Results** from Phase6A102 migration showed occurrences in:
- Account activation/welcome emails
- Event reminder emails
- Registration confirmation emails
- Newsletter subscription emails
- Refund emails
- Signup commitment emails

**Common patterns to remove**:
- "If you have questions, feel free to reply to this email."
- "Welcome back! If you have questions, feel free to reply to this email."
- "feel free to reply to this email.<br />We're here to help!"

---

## Implementation Strategy

### Step 1: Download Current Templates from Staging
```bash
# Use Azure Data Studio or psql to export current HTML
SELECT name, html_template
FROM communications.email_templates
WHERE name IN (
    'template-free-event-registration-confirmation',
    'template-paid-event-registration-confirmation-with-ticket',
    'template-attendees-added',
    'template-event-publication',
    'template-new-event-publication',
    'template-event-reminder',
    'template-event-cancellation-notifications',
    'template-event-registration-cancellation',
    'template-refund-requested',
    'template-refund-completed',
    'template-signup-list-commitment-confirmation'
)
ORDER BY name;
```

### Step 2: Modify 11 Event Templates

**For each template, add after "View Signup Lists" button**:

```html
<!-- VIEW SIGNUP FORMS BUTTON (conditional) -->
{{#HasSignupForms}}
<table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin: 8px 0 20px">
    <tr>
        <td align="center" style="padding: 8px 0 0">
            This event has signup forms available
        </td>
    </tr>
    <tr>
        <td align="center" style="padding: 8px 0 0">
            <!--[if mso]>
                <v:roundrect
                    xmlns:v="urn:schemas-microsoft-com:vml"
                    xmlns:w="urn:schemas-microsoft-com:office:word"
                    href="{{SignupFormsUrl}}"
                    style="height: 48px; v-text-anchor: middle; width: 260px;"
                    arcsize="20%"
                    stroke="true"
                    strokecolor="#ea580c"
                    strokeweight="2px"
                >
                    <v:fill type="solid" color="#ffffff" />
                    <w:anchorlock />
                    <center style="color: #ea580c; font-family: &quot;Segoe UI&quot;, Arial, sans-serif; font-size: 15px; font-weight: bold;">
                        View Signup Forms
                    </center>
                </v:roundrect>
            <![endif]-->
            <!--[if !mso]><!-->
            <a href="{{SignupFormsUrl}}" style="display: inline-block; background: #ffffff; color: #ea580c; text-decoration: none; font-family: &quot;Segoe UI&quot;, Arial, sans-serif; font-weight: 600; font-size: 15px; padding: 11px 44px; border-radius: 10px; border: 2px solid #ea580c; letter-spacing: 0.2px; min-width: 200px; text-align: center;">View Signup Forms</a>
            <!--<![endif]-->
        </td>
    </tr>
</table>
{{/HasSignupForms}}
```

### Step 3: Remove "Feel Free to Reply" Text

**Search and replace patterns**:
- Find: `If you have questions, feel free to reply to this email.`
- Replace: `We're here to help!` (or similar positive closing)

- Find: `Welcome back! If you have questions, feel free to reply to this email.`
- Replace: `Welcome back!`

- Find: `feel free to reply to this email.<br />We're here to help!`
- Replace: `We're here to help!`

### Step 4: Update Migration File

The migration `20260214211455_Phase6A112_UpdateFormResponseEmailTemplatesWithProfessionalStyling.cs` will:

1. Read 3 form response templates from `Template_Correction/staging/`
2. Read 11 modified event templates from `Template_Correction/event_templates/`
3. Use SQL REPLACE() for "feel free to reply" text removal (lightweight)
4. Execute 14 UPDATE statements total

### Step 5: Update EmailParams Classes (11 files)

**Pattern to add to each**:

```csharp
// Add properties
public bool HasSignupForms { get; set; } = false;
public string SignupFormsUrl { get; set; } = string.Empty;

// Add fluent method
public XxxEmailParams WithSignupFormsUrl(string url)
{
    HasSignupForms = !string.IsNullOrWhiteSpace(url);
    SignupFormsUrl = url ?? string.Empty;
    return this;
}

// Update ToDictionary()
{ EmailTemplateContract.Common.HasSignupForms, HasSignupForms },
{ EmailTemplateContract.Common.SignupFormsUrl, SignupFormsUrl },
```

### Step 6: Update Email Handlers (8-11 files)

**Pattern to add after `WithSignUpListsUrl()`**:

```csharp
// Check if event has active signup forms
var forms = await _eventFormRepository.GetByEventIdAsync(@event.Id);
var activeForms = forms.Where(f => f.Status == FormStatus.Active || f.Status == FormStatus.Draft).ToList();

if (activeForms.Any())
{
    emailParams.WithSignupFormsUrl($"{eventUrl}#signup-forms");
}
```

**Note**: Some handlers may not need this (e.g., refund emails where event context is less relevant)

### Step 7: Update EmailTemplateContract

Add to `Common` class:

```csharp
public const string HasSignupForms = "HasSignupForms";
public const string SignupFormsUrl = "SignupFormsUrl";
```

---

## File Checklist

### Backend Files to Modify

**EmailParams Classes** (11 files):
- [ ] `AttendeesAddedEmailParams.cs`
- [ ] `EventCancellationEmailParams.cs`
- [ ] `EventDetailsEmailParams.cs`
- [ ] `EventPublishedEmailParams.cs`
- [ ] `EventReminderEmailParams.cs`
- [ ] `FreeEventRegistrationEmailParams.cs`
- [ ] `NewsletterEmailParams.cs` (optional - no event context)
- [ ] `RefundEmailParams.cs`
- [ ] `RegistrationCancellationEmailParams.cs`
- [ ] `SignupCommitmentEmailParams.cs`
- [ ] `TicketConfirmationEmailParams.cs`

**Email Handlers** (8 files - some share EmailParams):
- [ ] `AnonymousRegistrationConfirmedEventHandler.cs`
- [ ] `AttendeesAddedEventHandler.cs`
- [ ] `EventPublishedEventHandler.cs`
- [ ] `PaymentCompletedEventHandler.cs`
- [ ] `RefundCompletedEventHandler.cs`
- [ ] `RefundRequestedEventHandler.cs`
- [ ] `RegistrationCancelledEventHandler.cs`
- [ ] `RegistrationConfirmedEventHandler.cs`

**Contract**:
- [ ] `EmailTemplateContract.cs` - Add HasSignupForms/SignupFormsUrl

**Migration**:
- [ ] `20260214211455_Phase6A112_UpdateFormResponseEmailTemplatesWithProfessionalStyling.cs` - Expand to include all updates

### Template Files

**Form Response Templates** (already done):
- [x] `template-form-response-confirmation-modified.html`
- [x] `template-form-response-update-modified.html`
- [x] `template-form-response-cancellation-modified.html`

**Event Templates** (to download and modify):
- [ ] Download 11 templates from staging
- [ ] Add "View Signup Forms" button to each
- [ ] Remove "feel free to reply" text
- [ ] Save to `Template_Correction/event_templates/`

---

## Testing Checklist

### Backend Testing
- [ ] Build solution: `dotnet build LankaConnect.sln`
- [ ] Run tests: `dotnet test`
- [ ] Migration compiles without errors

### Integration Testing (Staging)
- [ ] Deploy migration to staging
- [ ] Verify all 14 templates updated correctly
- [ ] Test email sending for each type
- [ ] Verify "View Signup Forms" button appears when event has forms
- [ ] Verify button does NOT appear when event has no forms
- [ ] Verify no "feel free to reply" text in any emails

---

## Estimated Effort

- **Download templates**: 30 min
- **Modify 11 templates**: 2-3 hours (repetitive work)
- **Update 11 EmailParams classes**: 1-2 hours
- **Update 8 handlers**: 1-1.5 hours
- **Update migration**: 1 hour
- **Testing**: 2-3 hours

**Total**: 1-2 days of work

---

## Next Steps

1. **Download 11 event templates from staging** ⏳ NEXT
2. Modify each template (add button, remove "feel free")
3. Update EmailParams classes
4. Update email handlers
5. Update EmailTemplateContract
6. Update migration file
7. Test locally (build)
8. Commit and deploy to staging
9. Verify on staging
10. Deploy to production

---

## Success Criteria

✅ All 14 email templates professionally styled
✅ "View Signup Forms" button works correctly (conditional)
✅ No "feel free to reply" text in any template
✅ All EmailParams classes have HasSignupForms/SignupFormsUrl
✅ All handlers populate signup forms URL when applicable
✅ Migration runs successfully on staging
✅ All email types tested and verified
