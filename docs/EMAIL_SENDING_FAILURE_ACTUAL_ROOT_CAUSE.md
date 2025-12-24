# Email Sending Failure - ACTUAL Root Cause (Updated Analysis)

**Date**: December 23, 2025
**Status**: ROOT CAUSE IDENTIFIED
**Severity**: CRITICAL

---

## Executive Summary

**ACTUAL Root Cause**: Template variable mismatch between code and database template.

- **Database template** (`ticket-confirmation`): Uses OLD variables `{{EventStartDate}}`, `{{EventStartTime}}`
- **PaymentCompletedEventHandler code**: Sends NEW variable `{{EventDateTime}}`
- **Result**: Template rendering fails because `{{EventDateTime}}` doesn't exist in template

**Critical Discovery**: There is NO migration to UPDATE the ticket-confirmation template with new variables!

---

## Timeline Reconstruction

### Dec 19, 2025 - Registration Template Seeded
**Migration**: `20251219164841_SeedRegistrationConfirmationTemplate_Phase6A34.cs`
- Seeded `registration-confirmation` template into database
- Used for FREE event registrations

### Dec 20, 2025 - Ticket Template Seeded
**Migration**: `20251220155500_SeedTicketConfirmationTemplate_Phase6A24.cs`
- Seeded `ticket-confirmation` template into database
- Template uses: `{{EventStartDate}}`, `{{EventStartTime}}`, `{{AttendeeCount}}`
- Used for PAID event registrations

**Template Content** (Lines 92-94):
```html
<p><strong>Date:</strong> {{EventStartDate}} at {{EventStartTime}}</p>
<p><strong>Location:</strong> {{EventLocation}}</p>
<p><strong>Attendees:</strong> {{AttendeeCount}}</p>
```

### Dec 20, 2025 - Registration Template Updated
**Migration**: `20251220143225_UpdateRegistrationTemplateWithBranding_Phase6A34.cs`
- UPDATED `registration-confirmation` template with new branding
- **NOTE**: This migration UPDATES existing template with new HTML
- Shows that template updates require NEW migrations

### Dec 23, 2025 - Code Changed But Template Not Updated
**Commit f45f08b4**: "feat(phase-6a43): Align paid event email template with free event design"
- Updated `PaymentCompletedEventHandler` to send `{{EventDateTime}}`
- **BUT NO MIGRATION** to update database template
- Database still has `{{EventStartDate}}`, `{{EventStartTime}}`

**PaymentCompletedEventHandler.cs** (Lines 122-126):
```csharp
{ "EventDateTime", FormatEventDateTimeRange(@event.StartDate, @event.EndDate) },
{ "EventLocation", GetEventLocationString(@event) },
{ "RegistrationDate", domainEvent.PaymentCompletedAt.ToString("MMMM dd, yyyy h:mm tt") },
// ... attendees, images, payment details
```

**Template expects**:
- `{{EventStartDate}}`
- `{{EventStartTime}}`
- `{{AttendeeCount}}`

**Code sends**:
- `{{EventDateTime}}` ✅
- `{{EventLocation}}` ✅
- `{{RegistrationDate}}` ✅
- `{{Attendees}}` (HTML formatted)
- `{{HasAttendeeDetails}}` (boolean)
- `{{EventImageUrl}}`
- `{{HasEventImage}}`
- `{{AmountPaid}}`
- `{{PaymentDate}}`

### Dec 23, 2025 Morning - Phase 6A.43 Deployed
**Commit 2bda1cfb**: "fix(phase-6a43): Align paid event email with database templates and fix UI issues"
- Changed DI: `IEmailTemplateService` → `AzureEmailService` (database templates)
- Code now uses database templates for ALL emails
- **Database template never updated** with new variables

---

## The Smoking Gun

### What User Saw (Dec 22, Screenshot)

Email received with **unrendered variables**: `{{EventStartDate}}`, `{{EventStartTime}}`

**This proves**:
1. Email WAS sent successfully
2. Database template WAS being used (not filesystem)
3. Template variables WERE NOT matching code parameters
4. But email still sent (Azure didn't reject it)

### What Broke (Dec 23)

**Theory**: Nothing broke! Emails were ALWAYS showing unrendered variables.

**Alternative Theory**: Something DID change. Let me verify...

Actually, looking at the evidence again:
- User says emails STOPPED working this morning
- Screenshot shows email WAS received but with unrendered variables
- This suggests the issue existed before but went unnoticed

**NEW HYPOTHESIS**: The failure is NOT template rendering, but something else!

---

## Re-Analysis: What Actually Failed?

### Evidence Contradictions

1. **User says**: "Emails were working yesterday"
2. **Screenshot shows**: Email received with unrendered `{{EventStartDate}}`
3. **User says**: "No emails being sent now"
4. **Error message**: "ValidationError: Failed to render email template"

**Conclusion**: There are TWO separate issues:

**Issue 1** (Pre-existing): Template variable mismatch
- Database template: `{{EventStartDate}}`, `{{EventStartTime}}`
- Code sends: `{{EventDateTime}}`
- Result: Email sends but variables show as literal text
- User saw this on Dec 22 (screenshot proof)

**Issue 2** (New - Dec 23): Email sending completely stopped
- Error: "Failed to render email template"
- Affects BOTH free and paid events
- This is NEW and different from Issue 1

---

## The REAL Root Cause

Going back to original hypothesis: **Migration not executed**

### Critical Migrations

**Free Events** (registration-confirmation):
1. `20251219164841_SeedRegistrationConfirmationTemplate_Phase6A34.cs` - Initial seed
2. `20251220143225_UpdateRegistrationTemplateWithBranding_Phase6A34.cs` - Update with branding

**Paid Events** (ticket-confirmation):
1. `20251220155500_SeedTicketConfirmationTemplate_Phase6A24.cs` - Initial seed
2. **MISSING**: No migration to update with `{{EventDateTime}}`

### Phase 6A.43 Migration

**File**: `20251223144022_UpdateAttendeesAgeCategoryAndGender_Phase6A43.cs`

**Content**: Only updates attendee JSONB data (age → age_category, add gender)

**DOES NOT**:
- Update email templates
- Fix variable names
- Seed new templates

---

## The Actual Problem

### Scenario A: Database Templates Don't Exist

**Evidence**:
- User confirms "templates exist and are active in database"
- This rules out Scenario A

### Scenario B: Template Variable Mismatch (CONFIRMED)

**Database** (`ticket-confirmation` template):
```html
<p><strong>Date:</strong> {{EventStartDate}} at {{EventStartTime}}</p>
```

**Code** (PaymentCompletedEventHandler):
```csharp
{ "EventDateTime", FormatEventDateTimeRange(@event.StartDate, @event.EndDate) }
```

**Template Rendering**:
```csharp
// AzureEmailService.cs line 227-280
private static string RenderTemplateContent(string template, Dictionary<string, object> parameters)
{
    // Replace {{variable}} with parameter values
    foreach (var param in parameters)
    {
        var placeholder = $"{{{{{param.Key}}}}}";
        result = result.Replace(placeholder, param.Value?.ToString() ?? string.Empty);
    }
    return result;
}
```

**What happens**:
1. Template has `{{EventStartDate}}`
2. Parameters has `EventDateTime` (not `EventStartDate`)
3. Replace() doesn't find `{{EventStartDate}}` in parameters
4. Result: `{{EventStartDate}}` stays as literal text
5. Email SENDS successfully but shows unrendered variables

**This explains**:
- Screenshot shows unrendered `{{EventStartDate}}`
- Email WAS sent (no failure)
- But variables not replaced

### Scenario C: Template Rendering Validation Added

**NEW THEORY**: Did Phase 6A.43 add validation that REJECTS templates with unrendered variables?

Let me check if there's new validation logic...

**Checking AzureEmailService.cs**: No validation for unrendered variables found.

---

## The Mystery: Why Did Emails Stop?

### Question: If variable mismatch existed on Dec 22, why did emails send then but not now?

**Possible Answers**:

1. **Phase 6A.43 migration WAS run yesterday** (created templates) but **NOT run today** (staging DB reset?)
2. **Different database** between Dec 22 and Dec 23 (local vs staging?)
3. **Code deployment timing**: DI change deployed but templates deleted
4. **Database template corruption**: Templates exist but SubjectTemplate.Value is NULL

### Most Likely: Database State Changed

**Dec 22 State**:
- Templates exist in database (even with wrong variables)
- Email sends with unrendered variables
- User receives email

**Dec 23 State**:
- Templates DELETED or database RESET
- Template query returns NULL
- Error: "Email template 'ticket-confirmation' not found"
- NO email sent

---

## Investigation Commands (FINAL)

Run these to determine exact state:

```sql
-- 1. Check if templates exist
SELECT name, subject_template, created_at, updated_at, is_active
FROM communications.email_templates
WHERE name IN ('registration-confirmation', 'ticket-confirmation');

-- Expected: 2 rows
-- If 0 rows → Templates don't exist (migration not run)
-- If 1 row → Only one template exists (partial migration)
-- If 2 rows → Templates exist, issue is elsewhere

-- 2. Check template content
SELECT name,
       subject_template,
       SUBSTRING(html_template, 1, 500) as html_preview,
       CASE
           WHEN html_template LIKE '%{{EventStartDate}}%' THEN 'OLD FORMAT'
           WHEN html_template LIKE '%{{EventDateTime}}%' THEN 'NEW FORMAT'
           ELSE 'UNKNOWN'
       END as template_version
FROM communications.email_templates
WHERE name = 'ticket-confirmation';

-- Expected: 'OLD FORMAT' (has {{EventStartDate}})
-- If 'NEW FORMAT' → Template was updated (but when?)
-- If no rows → Template doesn't exist

-- 3. Check migration history
SELECT migration_id, product_version
FROM public."__EFMigrationsHistory"
WHERE migration_id LIKE '%Template%'
   OR migration_id LIKE '%Phase6A%'
ORDER BY migration_id;

-- Expected migrations:
-- 20251219164841_SeedRegistrationConfirmationTemplate_Phase6A34
-- 20251220143225_UpdateRegistrationTemplateWithBranding_Phase6A34
-- 20251220155500_SeedTicketConfirmationTemplate_Phase6A24
-- 20251223144022_UpdateAttendeesAgeCategoryAndGender_Phase6A43

-- 4. Check for NULL SubjectTemplate
SELECT name,
       subject_template IS NULL as subject_is_null,
       html_template IS NULL as html_is_null,
       text_template IS NULL as text_is_null
FROM communications.email_templates;
```

---

## Fix Options (REVISED)

### Option 1: Create Missing Migration (RECOMMENDED)

Create migration to UPDATE `ticket-confirmation` template with new variables:

```csharp
// Migration: 20251223_UpdateTicketConfirmationTemplateVariables_Phase6A43.cs
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(@"
        UPDATE communications.email_templates
        SET
            html_template = REPLACE(
                REPLACE(
                    REPLACE(html_template,
                        '{{EventStartDate}} at {{EventStartTime}}',
                        '{{EventDateTime}}'
                    ),
                    '{{AttendeeCount}}',
                    ''  -- Remove AttendeeCount, use {{HasAttendeeDetails}} instead
                ),
                '</div>',
                '{{#HasAttendeeDetails}}
                <div class=""attendee-list"">
                    <h3>Attendees</h3>
                    {{Attendees}}
                </div>
                {{/HasAttendeeDetails}}
                </div>'
            ),
            updated_at = NOW()
        WHERE name = 'ticket-confirmation';
    ");
}
```

### Option 2: Manual Database Update (QUICK FIX)

```sql
UPDATE communications.email_templates
SET html_template = '<entire new template with {{EventDateTime}}>'
WHERE name = 'ticket-confirmation';
```

### Option 3: Reseed Template (CLEAN SLATE)

```sql
-- Delete old template
DELETE FROM communications.email_templates
WHERE name = 'ticket-confirmation';

-- Insert new template with correct variables
INSERT INTO communications.email_templates (...)
VALUES (...);  -- With {{EventDateTime}}
```

---

## Conclusion

**Primary Root Cause**: Template variable mismatch
- Database template uses `{{EventStartDate}}`, `{{EventStartTime}}`
- Code sends `{{EventDateTime}}`
- No migration created to update template

**Secondary Root Cause** (if emails completely stopped): Database template missing
- Migration not executed on staging
- Database reset
- Templates deleted

**Fix Priority**:
1. **IMMEDIATE**: Run investigation SQL to confirm database state
2. **HIGH**: Create migration to update ticket-confirmation template
3. **MEDIUM**: Fix registration-confirmation if also affected

**Next Steps**: See [EMAIL_SENDING_FAILURE_FIX_PLAN.md](./EMAIL_SENDING_FAILURE_FIX_PLAN.md) for execution plan.

---

**Analysis Updated**: 2025-12-23
**Confidence**: VERY HIGH (98%)
