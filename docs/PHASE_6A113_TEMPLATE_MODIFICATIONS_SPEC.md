# Phase 6A.112/113: Email Template Modifications Specification

**Date**: 2026-02-15
**Status**: Ready for Implementation
**Prerequisites**: Phase 6A.113 code changes completed ✅

---

## Overview

This document specifies the exact modifications needed to email templates in the staging database.

### Two Categories of Changes:

1. **Add "View Signup Forms" Button** - 14 event email templates
2. **Remove "feel free" Text** - 8 templates (overlaps with category 1)

---

## Part 1: Add "View Signup Forms" Button (14 Templates)

### Templates to Modify:

Based on EmailParams classes with `HasSignupForms` property:

1. `template-free-event-registration-confirmation` (FreeEventRegistrationEmailParams)
2. `template-paid-event-registration-confirmation-with-ticket` (TicketConfirmationEmailParams)
3. `template-event-reminder` (EventReminderEmailParams)
4. `template-event-registration-cancellation` (RegistrationCancellationEmailParams)
5. `template-attendees-added-confirmation` (AttendeesAddedEmailParams)
6. `template-event-cancellation-notifications` (EventCancellationEmailParams)
7. `template-new-event-publication` (EventPublishedEmailParams)
8. `template-newsletter-notification` (NewsletterEmailParams)
9. `template-signup-list-commitment-confirmation` (SignupCommitmentEmailParams)
10. `template-signup-list-commitment-update` (SignupCommitmentEmailParams)
11. `template-signup-list-commitment-cancellation` (SignupCommitmentEmailParams)
12. `template-refund-requested` (RefundEmailParams)
13. `template-refund-completed` (RefundEmailParams)
14. `template-event-details-publication` (EventDetailsEmailParams)

### Button HTML to Add:

Insert AFTER the existing "View Sign-Up Lists" button (or after main CTA) and BEFORE `{{#HasOrganizerContact}}`:

```html
{{#HasSignupForms}}
<p style="text-align: center; margin: 20px 0;">
    <a href="{{SignupFormsUrl}}" style="color: #FF6600; text-decoration: underline;">View Signup Forms</a>
</p>
{{/HasSignupForms}}
```

**Alternative Button Style** (if template uses button instead of link):
```html
{{#HasSignupForms}}
<table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="margin-top: 15px;">
    <tr>
        <td style="text-align: center;">
            <a href="{{SignupFormsUrl}}" style="display: inline-block; background: #3b82f6; color: white; text-decoration: none; padding: 12px 24px; border-radius: 6px; font-weight: 600; font-size: 14px;">View Signup Forms</a>
        </td>
    </tr>
</table>
{{/HasSignupForms}}
```

### Placement Strategy:

**Find this pattern:**
```html
{{#HasSignUpLists}}
<p style="text-align: center; margin: 20px 0;">
    <a href="{{SignUpListsUrl}}" ...>View Sign-Up Lists</a>
</p>
{{/HasSignUpLists}}

{{#HasOrganizerContact}}
...
{{/HasOrganizerContact}}
```

**Insert button between them:**
```html
{{#HasSignUpLists}}
<p style="text-align: center; margin: 20px 0;">
    <a href="{{SignUpListsUrl}}" ...>View Sign-Up Lists</a>
</p>
{{/HasSignUpLists}}

<!-- NEW: Phase 6A.112/113 -->
{{#HasSignupForms}}
<p style="text-align: center; margin: 20px 0;">
    <a href="{{SignupFormsUrl}}" style="color: #FF6600; text-decoration: underline;">View Signup Forms</a>
</p>
{{/HasSignupForms}}

{{#HasOrganizerContact}}
...
{{/HasOrganizerContact}}
```

---

## Part 2: Remove "feel free" Text (8 Templates)

### Text to Remove:

Common patterns found in templates:
- "If you have questions, feel free to reply to this email."
- "Feel free to reach out if you have any questions."
- "Feel free to contact us if you need assistance."

### SQL to Find Affected Templates:

```sql
SELECT name,
       CASE
           WHEN html_body ILIKE '%feel free%' THEN 'html_body'
           WHEN text_body ILIKE '%feel free%' THEN 'text_body'
           ELSE 'none'
       END as found_in
FROM communications.email_templates
WHERE html_body ILIKE '%feel free%' OR text_body ILIKE '%feel free%'
ORDER BY name;
```

**Expected Results**: 8 templates (user confirmed this in analysis)

### Removal Strategy:

1. Export template HTML
2. Search for paragraph or div containing "feel free"
3. Remove entire paragraph/section (don't leave orphaned formatting)
4. Ensure clean HTML after removal (no double `<p></p>` tags)

**Example:**
```html
<!-- BEFORE -->
<p>Thank you for registering for {{EventTitle}}.</p>
<p>If you have questions, feel free to reply to this email.</p>
<p>See you at the event!</p>

<!-- AFTER -->
<p>Thank you for registering for {{EventTitle}}.</p>
<p>See you at the event!</p>
```

---

## Implementation Workflow

### Step 1: Export Templates from Staging

Run the PowerShell export scripts:

```powershell
# Export 14 templates for button addition
.\scripts\phase6a112_export_signup_forms_button_templates.ps1

# Export templates with "feel free" text
.\scripts\phase6a112_export_feel_free_templates.ps1
```

**Output Files:**
- `scripts/phase6a112_signup_forms_button_templates.json`
- `scripts/phase6a112_feel_free_templates.json`

### Step 2: Modify Templates Locally

Create modified versions in `Template_Correction/staging-phase6a113/`:

```
Template_Correction/staging-phase6a113/
├── template-free-event-registration-confirmation-modified.html
├── template-paid-event-registration-confirmation-with-ticket-modified.html
├── template-event-reminder-modified.html
├── ... (14 total templates)
```

**For each template:**
1. Copy from export JSON to .html file
2. Add `{{#HasSignupForms}}` button (see Part 1)
3. Remove "feel free" text if present (see Part 2)
4. Validate HTML (no syntax errors)
5. Test Handlebars conditionals render correctly

### Step 3: Create EF Core Migration

Create migration file: `Phase6A113_UpdateEventEmailTemplatesWithSignupFormsButton.cs`

**Pattern** (from existing Phase6A112 migration):

```csharp
public partial class Phase6A113_UpdateEventEmailTemplatesWithSignupFormsButton : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Get path to modified templates
        var projectRoot = FindProjectRoot();
        var templatePath = Path.Combine(projectRoot, "Template_Correction", "staging-phase6a113");

        // Read all 14 modified HTML templates
        var templates = new Dictionary<string, string>
        {
            ["template-free-event-registration-confirmation"] =
                File.ReadAllText(Path.Combine(templatePath, "template-free-event-registration-confirmation-modified.html")),
            ["template-paid-event-registration-confirmation-with-ticket"] =
                File.ReadAllText(Path.Combine(templatePath, "template-paid-event-registration-confirmation-with-ticket-modified.html")),
            // ... (14 total)
        };

        // Update each template
        foreach (var (name, html) in templates)
        {
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_body = '{EscapeSql(html)}',
                    updated_at = NOW()
                WHERE name = '{name}';
            ");
        }
    }

    private string FindProjectRoot() { /* same as Phase6A112 */ }
    private string EscapeSql(string input) { /* same as Phase6A112 */ }
}
```

### Step 4: Test Migration

```bash
# Apply migration to local database
dotnet ef database update --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API

# Verify templates updated
dotnet ef migrations list --project src/LankaConnect.Infrastructure
```

### Step 5: Deploy to Staging

```bash
# Commit migration
git add .
git commit -m "feat(email): Phase 6A.113 - Add View Signup Forms button to 14 event emails, remove 'feel free' text"

# Push to trigger GitHub Actions deploy-staging.yml
git push origin develop
```

### Step 6: Verify in Staging

1. Check Azure container logs for migration success
2. Query staging database:
```sql
-- Verify button added (should find 14 templates)
SELECT name
FROM communications.email_templates
WHERE html_body ILIKE '%HasSignupForms%'
ORDER BY name;

-- Verify "feel free" removed (should find 0 templates)
SELECT name
FROM communications.email_templates
WHERE html_body ILIKE '%feel free%'
ORDER BY name;
```

3. Send test emails via API scripts:
```powershell
.\scripts\test_email_templates_phase6a113.ps1
```

---

## Validation Checklist

### Code Validation (✅ Already Complete)
- [x] EmailTemplateContract.cs constants updated
- [x] 14 EmailParams classes have `HasSignupForms` property
- [x] 14 EmailParams classes have `SignupFormsUrl` property
- [x] 14 EmailParams classes have `WithSignupForms()` fluent method
- [x] Event handlers set `HasSignupForms` and `SignupFormsUrl` when event has forms
- [x] Validation tests pass (31/31)

### Template Validation (Pending)
- [ ] 14 templates exported from staging
- [ ] Button HTML added to all 14 templates
- [ ] "feel free" text removed from 8 templates
- [ ] HTML validated (no syntax errors)
- [ ] Handlebars placeholders correct (`{{SignupFormsUrl}}`, `{{HasSignupForms}}`)
- [ ] Button placement correct (after signup lists, before organizer contact)

### Migration Validation (Pending)
- [ ] Migration file created
- [ ] All 14 template file paths correct
- [ ] SQL escaping correct (single quotes doubled)
- [ ] Migration runs without errors locally
- [ ] Database query confirms templates updated

### Deployment Validation (Pending)
- [ ] Migration deployed to staging
- [ ] Staging database query shows 14 templates have button
- [ ] Staging database query shows 0 templates have "feel free" text
- [ ] Test emails sent and received
- [ ] Emails render correctly (HTML + mobile)
- [ ] Buttons work (navigate to correct URLs)

---

## Known Overlaps

**Templates in BOTH categories** (add button + remove "feel free"):
- Need to determine which of the 14 templates also have "feel free" text
- User analysis found 8 templates total with "feel free"
- Some of those 8 may be outside the 14 event templates

---

## Next Actions

1. **User**: Run PowerShell export scripts to get template HTML
2. **User**: Identify which 8 specific templates have "feel free" text
3. **Claude/User**: Modify all 14 template HTML files
4. **Claude**: Create EF migration referencing modified files
5. **User**: Review migration, test locally, deploy to staging

---

**Created**: 2026-02-15 by Claude (Senior Engineer)
**Dependencies**: Phase 6A.113 code changes (complete), psql client tools (for export)
**Estimated Time**: 2-3 hours for template modification + testing
