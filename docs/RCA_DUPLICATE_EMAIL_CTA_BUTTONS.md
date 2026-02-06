# RCA: Duplicate Email CTA Buttons

**Date**: 2026-02-04
**Issue**: Some email templates have THREE redundant buttons
**Severity**: Medium (UX issue, not functionality breaking)
**Status**: Analysis Complete

---

## 1. Problem Statement

User reported that some email templates have THREE CTA buttons that are redundant:

1. **"View Event & Register"** - links to `{{EventDetailsUrl}}`
2. **"View Event Details"** - links to `{{EventDetailsUrl}}`
3. **"View Sign-Up Lists"** - links to `{{EventDetailsUrl}}#sign-ups`

The first two buttons (1 and 2) point to the **SAME URL**, making one redundant.

---

## 2. Root Cause Analysis

### 2.1 Timeline of Events

| Migration Date | Migration Name | Templates Affected | Buttons Added |
|----------------|----------------|-------------------|---------------|
| 2025-12-21 | `SeedEventPublishedTemplate_Phase6A39` | `event-published` (now `template-new-event-publication`) | "View Event & Register" |
| 2026-01-16 | `Phase6A61_Update_EventDetailsTemplate_WithAllFields` | `event-details` (now `template-event-details-publication`) | "View Event & Register" (copied from event-published) |
| 2026-02-04 | `Phase6A87Plus_RebuildSignupCommitmentTemplates` | Signup commitment templates (3) | "View Event Details" + "View Sign-Up Lists" |
| 2026-02-04 | `Issue39_AddEventDetailsLinkToRegistrationEmails` | Registration confirmation templates (2) | "View Event Details" |
| 2026-02-04 | `ComprehensiveEmailLinkFix_AddViewEventDetailsButtons` | 11 templates | "View Event Details" + "View Sign-Up Lists" (on some) |
| 2026-02-04 | `Phase6A87Plus_FixSignupCommitmentButtonText` | Signup commitment templates (3) | Changed "View Event & Register" to "View Event Details" |

### 2.2 Root Cause

The duplication occurred because:

1. **Original "event-published" template** (Phase 6A.39) used "View Event & Register" as the CTA - this was CORRECT for that context (inviting users to register)

2. **event-details template** (Phase 6A.61) copied the HTML from event-published, inheriting "View Event & Register" button

3. **ComprehensiveEmailLinkFix** (2026-02-04) added "View Event Details" and "View Sign-Up Lists" buttons to templates that didn't have them, but did NOT check if templates already had "View Event & Register"

4. **Phase6A87Plus_FixSignupCommitmentButtonText** attempted to fix signup templates by replacing "View Event & Register" with "View Event Details" but:
   - Only targeted signup commitment templates
   - Did NOT target event-details or event-published templates

---

## 3. Complete Inventory of Templates with CTA Buttons

### 3.1 Templates with "View Event & Register" Button

| Template Name | Should Keep? | Reasoning |
|--------------|-------------|-----------|
| `template-new-event-publication` | **YES** | This email invites people to register for a new event - "View Event & Register" is the correct CTA |
| `template-event-details-publication` | **NO** | This is a manual notification - "View Event Details" is more appropriate |

### 3.2 Templates with "View Event Details" Button

| Template Name | Current State | Notes |
|--------------|--------------|-------|
| `template-free-event-registration-confirmation` | Correct | Added by Issue39 migration |
| `template-paid-event-registration-confirmation-with-ticket` | Correct | Added by Issue39 migration |
| `template-signup-list-commitment-confirmation` | Correct | Rebuilt by Phase6A87Plus |
| `template-signup-list-commitment-update` | Correct | Rebuilt by Phase6A87Plus |
| `template-signup-list-commitment-cancellation` | Correct | Rebuilt by Phase6A87Plus |
| `template-event-reminder` | Correct | "View Event Details" is appropriate |
| `template-event-approval` | Correct | For organizers viewing their approved event |
| `template-attendees-added-confirmation` | Correct | Users viewing their updated registration |

### 3.3 Templates Potentially with DUPLICATE Buttons

Based on migration analysis, these templates may have BOTH "View Event & Register" AND "View Event Details":

| Template Name | Likely Has Duplicate? | Fix Required |
|--------------|---------------------|--------------|
| `template-event-details-publication` | **YES** | Remove "View Event & Register", keep "View Event Details" |
| `template-new-event-publication` | Possibly | Check - may have had "View Event Details" added |

---

## 4. Recommended Fix Plan

### 4.1 Templates to Modify

#### Template 1: `template-new-event-publication`

**Action**: KEEP "View Event & Register" as primary CTA, REMOVE "View Event Details" if present

**Reasoning**: This email is specifically sent to notify subscribers about a NEW event and invite them to register. The call-to-action should encourage registration, not just viewing.

**Button Configuration**:
- Primary CTA: "View Event & Register" -> `{{EventDetailsUrl}}`
- Secondary CTA: "View Sign-Up Lists" -> `{{EventDetailsUrl}}#sign-ups` (if event has sign-ups)

#### Template 2: `template-event-details-publication`

**Action**: REMOVE "View Event & Register", keep "View Event Details"

**Reasoning**: This is for manual event notifications (organizer sending updates). Users may already be registered, so "View Event Details" is more appropriate.

**Button Configuration**:
- Primary CTA: "View Event Details" -> `{{EventDetailsUrl}}`
- Secondary CTA: "View Sign-Up Lists" -> `{{EventDetailsUrl}}#sign-ups` (if event has sign-ups)

### 4.2 All Other Event Templates

**Standard Button Configuration**:
- Primary CTA: "View Event Details" -> `{{EventDetailsUrl}}`
- Optional Secondary: "View Sign-Up Lists" -> `{{EventDetailsUrl}}#sign-ups` (for signup-related emails only)

---

## 5. Migration Strategy

### Phase 1: Audit Current State (Database Query)

```sql
-- Check which templates have "View Event & Register" text
SELECT name,
       html_template LIKE '%View Event &amp; Register%' as has_view_register,
       html_template LIKE '%View Event Details%' as has_view_details,
       html_template LIKE '%View Sign-Up Lists%' as has_signup_lists
FROM communications.email_templates
WHERE category IN ('Event', 'Events', 'Registration', 'Notification')
  AND is_active = true
ORDER BY name;
```

### Phase 2: Create Fix Migration

```csharp
// Migration: Fix_DuplicateCTAButtons
public partial class Fix_DuplicateCTAButtons : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. template-event-details-publication: Remove "View Event & Register", ensure "View Event Details" exists
        migrationBuilder.Sql(@"
            UPDATE communications.email_templates
            SET html_template = REGEXP_REPLACE(
                html_template,
                '<table[^>]*>.*?View Event &amp; Register.*?</table>',
                '',
                'gs'
            ),
            updated_at = NOW()
            WHERE name = 'template-event-details-publication'
              AND html_template LIKE '%View Event &amp; Register%';
        ");

        // 2. template-new-event-publication: Remove "View Event Details" if present (keep "View Event & Register")
        migrationBuilder.Sql(@"
            UPDATE communications.email_templates
            SET html_template = REGEXP_REPLACE(
                html_template,
                '<!-- View Event Details CTA Button -->.*?</table>',
                '',
                'gs'
            ),
            updated_at = NOW()
            WHERE name = 'template-new-event-publication'
              AND html_template LIKE '%<!-- View Event Details CTA Button -->%';
        ");
    }
}
```

### Phase 3: Verification

After migration, verify with:

```sql
SELECT name,
       (LENGTH(html_template) - LENGTH(REPLACE(html_template, 'EventDetailsUrl', ''))) / LENGTH('EventDetailsUrl') as url_count
FROM communications.email_templates
WHERE name IN (
    'template-new-event-publication',
    'template-event-details-publication'
);
```

Each template should have:
- `template-new-event-publication`: 2 URLs (one for button, one for sign-ups link if present)
- `template-event-details-publication`: 2 URLs (one for button, one for sign-ups link if present)

---

## 6. Risk Assessment

### Low Risk
- This is a UX improvement, not a functionality fix
- No data loss possible
- Email delivery unaffected

### Medium Risk
- REGEX_REPLACE on HTML can be tricky - must test thoroughly
- Must ensure we don't accidentally remove ALL buttons

### Mitigation
1. Test migration on staging first
2. Save backup of template HTML before migration
3. Verify with database query after migration
4. Send test emails to verify appearance

---

## 7. Summary Table

| Template | Current Buttons | Recommended Buttons |
|----------|----------------|---------------------|
| `template-new-event-publication` | "View Event & Register" + possibly "View Event Details" + "View Sign-Up Lists" | "View Event & Register" + "View Sign-Up Lists" |
| `template-event-details-publication` | "View Event & Register" + "View Event Details" + "View Sign-Up Lists" | "View Event Details" + "View Sign-Up Lists" |
| All other event templates | "View Event Details" (+ "View Sign-Up Lists" for signup templates) | No change needed |

---

## 8. Files Examined

### Migration Files

- `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Migrations\20251221160725_SeedEventPublishedTemplate_Phase6A39.cs`
- `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Migrations\20260116160323_Phase6A61_Update_EventDetailsTemplate_WithAllFields.cs`
- `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Migrations\20260204100000_Phase6A87Plus_FixSignupCommitmentButtonText.cs`
- `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Migrations\20260204150000_Phase6A87Plus_RebuildSignupCommitmentTemplates.cs`
- `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Migrations\20260204195000_ComprehensiveEmailLinkFix_AddViewEventDetailsButtons.cs`
- `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Migrations\20260204001500_Issue39_AddEventDetailsLinkToRegistrationEmails.cs`

### Constants/Configuration

- `c:\Work\LankaConnect\src\LankaConnect.Application\Common\Constants\EmailTemplateNames.cs`

---

## 9. Next Steps

1. [ ] Run audit query on staging database to confirm current state
2. [ ] Create migration to fix duplicate buttons
3. [ ] Test migration on staging
4. [ ] Send test emails from staging to verify appearance
5. [ ] Deploy to production
6. [ ] Verify production templates

---

**Document Author**: Claude Code (Architecture Agent)
**Reviewed By**: Pending
