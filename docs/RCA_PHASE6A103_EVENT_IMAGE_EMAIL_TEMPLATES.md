# RCA: Phase 6A.103 - Event Images Not Showing in Email Templates

**Date**: 2026-02-11
**Severity**: Medium (cosmetic/feature missing -- emails still send, just without images)
**Issue Classification**: Database Migration Issue + Minor Template HTML Discrepancy
**Status**: Root Cause Identified -- Fix Required

---

## 1. Executive Summary

Event images are not appearing in 5 email templates (event-details-publication, new-event-publication, event-reminder, event-cancellation-notifications, event-approval) even though Phase 6A.103 was "deployed." The migration file exists, the application handler code correctly passes image URLs, and the EmailParams classes have the correct `ToDictionary()` entries. However, **the migration was never actually executed** because the migration file is missing its required `.Designer.cs` companion file, making it invisible to EF Core's migration runner.

---

## 2. Root Cause Analysis

### PRIMARY ROOT CAUSE: Missing `.Designer.cs` File

**The Phase 6A.103 migration was hand-crafted but NEVER scaffolded by `dotnet ef migrations add`.**

Evidence:

| Migration | `.cs` file | `.Designer.cs` file | Will EF Core execute? |
|-----------|-----------|---------------------|----------------------|
| Phase6A102 | EXISTS at `Data/Migrations/20260210231302_Phase6A102_SyncProductionFromStaging.cs` | EXISTS at `Data/Migrations/20260210231302_Phase6A102_SyncProductionFromStaging.Designer.cs` | YES |
| Phase6A103 | EXISTS at `Data/Migrations/20260211100000_Phase6A103_AddEventImageToEmailTemplates.cs` | **MISSING** | **NO** |

EF Core discovers migrations via the `[Migration("...")]` attribute in the `.Designer.cs` file. Without this file:
- `dotnet ef migrations list` will NOT show Phase6A103
- `dotnet ef database update` will NOT execute Phase6A103
- The `__EFMigrationsHistory` table will NOT contain Phase6A103

**File**: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Migrations\20260211100000_Phase6A103_AddEventImageToEmailTemplates.cs` (line 1-111)

### SECONDARY ISSUE: Image HTML Discrepancy (Minor)

The Phase 6A.103 migration injects an image block that is **missing the `onerror` graceful fallback** that the working registration confirmation templates have.

**Working pattern** (from free-event-registration-confirmation, line 10075-10085 of Phase6A102):
```html
<img
    src="{{EventImageUrl}}"
    alt="{{EventTitle}}"
    width="860"
    style="width: 100%; max-height: 300px; object-fit: cover; display: block"
    onerror="
        this.style.display = 'none';
        this.parentElement.style.height = '0';
        this.parentElement.style.overflow = 'hidden';
    "
/>
```

**Phase 6A.103 pattern** (line 45-50 of migration file):
```html
<img
    src="{{EventImageUrl}}"
    alt="{{EventTitle}}"
    width="860"
    style="width: 100%; max-height: 300px; object-fit: cover; display: block"
/>
```

The missing `onerror` handler means if an image URL is broken or unreachable, the template will show a broken image icon instead of gracefully hiding it.

---

## 3. What IS Working Correctly

### 3.1 Application Handler Code (ALL CORRECT)

All 8 handlers/jobs correctly fetch and pass the event image URL:

| Handler/Job | File | Line | Pattern |
|------------|------|------|---------|
| `EventNotificationEmailJob` | `BackgroundJobs/EventNotificationEmailJob.cs` | 152-153, 211 | `@event.Images.FirstOrDefault(i => i.IsPrimary)` then `.WithEventImage(eventImageUrl)` |
| `EventCancellationEmailJob` | `BackgroundJobs/EventCancellationEmailJob.cs` | 246-247, 279 | Same pattern |
| `EventReminderJob` (24h) | `BackgroundJobs/EventReminderJob.cs` | 140-141, 233 | Same pattern |
| `EventReminderJob` (7d) | `BackgroundJobs/EventReminderJob.cs` | 343-344, 446 | Same pattern |
| `EventApprovedEventHandler` | `EventHandlers/EventApprovedEventHandler.cs` | 89-90, 107 | Same pattern |
| `EventPublishedEventHandler` | `EventHandlers/EventPublishedEventHandler.cs` | 126-127, 154 | Same pattern |
| `UserCommittedToSignUpEventHandler` | `EventHandlers/UserCommittedToSignUpEventHandler.cs` | 100-101 | Same pattern |
| `CommitmentUpdatedEventHandler` | `EventHandlers/CommitmentUpdatedEventHandler.cs` | 100-101 | Same pattern |
| `CommitmentCancelledEmailHandler` | `EventHandlers/CommitmentCancelledEmailHandler.cs` | 100-101 | Same pattern |

### 3.2 EmailParams ToDictionary() Methods (ALL CORRECT)

All 5 target EmailParams classes correctly include image parameters in their `ToDictionary()`:

| EmailParams Class | File | Has `HasEventImage`? | Has `EventImageUrl`? | Has `WithEventImage()` fluent? |
|------------------|------|---------------------|---------------------|-------------------------------|
| `EventDetailsEmailParams` | `Shared/Email/Contracts/EventDetailsEmailParams.cs` (line 235-236) | YES | YES | YES (line 268-273) |
| `EventReminderEmailParams` | `Shared/Email/Contracts/EventReminderEmailParams.cs` (line 229-230) | YES | YES | YES (line 347-352) |
| `EventCancellationEmailParams` | `Shared/Email/Contracts/EventCancellationEmailParams.cs` (line 214-215) | YES | YES | YES (line 274-279) |
| `EventApprovalEmailParams` | `Shared/Email/Contracts/EventApprovalEmailParams.cs` (line 138-139) | YES | YES | YES (line 183-188) |
| `SignupCommitmentEmailParams` | `Shared/Email/Contracts/SignupCommitmentEmailParams.cs` (line 241-242) | YES | YES | YES (line 290-295) |

All use `EmailTemplateContract.EventImage.HasEventImage` and `EmailTemplateContract.EventImage.EventImageUrl` constants which resolve to `"HasEventImage"` and `"EventImageUrl"` respectively (verified at `EmailTemplateContract.cs` lines 525, 530).

### 3.3 Migration SQL Logic (CORRECT -- if it were to run)

The SQL in the migration is sound:
- Uses correct table: `communications.email_templates` -- CORRECT
- Uses PostgreSQL `replace()` function -- CORRECT syntax
- Search anchor: `'<!-- BODY CONTENT'` -- CORRECT, this is a substring match that will hit `<!-- BODY CONTENT (720px inner) -->` which IS present in all 5 target templates
- Idempotency guard: `AND html_template NOT LIKE '%HasEventImage%'` -- CORRECT
- WHERE clause template names are ALL correct

### 3.4 Anchor Point Verification (ALL 5 TEMPLATES HAVE IT)

| Template | Anchor Found | Line in Phase6A102 |
|----------|-------------|-------------------|
| `template-event-approval` | `<!-- BODY CONTENT (720px inner) -->` | Line 4750 |
| `template-event-cancellation-notifications` | `<!-- BODY CONTENT (720px inner) -->` | Line 5548 |
| `template-event-details-publication` | `<!-- BODY CONTENT (720px inner) -->` | Line 6800 |
| `template-event-reminder` | `<!-- BODY CONTENT (720px inner) -->` | Line 8829 |
| `template-new-event-publication` | `<!-- BODY CONTENT (720px inner) -->` | Line 11891 |

---

## 4. "Reply to this Email" Audit

**PROBLEM**: Multiple templates contain text like "feel free to reply to this email" but emails are sent from a `DoNotReply@*.azurecomm.net` address (confirmed at `EmailSettings.cs` line 21).

### Templates with Misleading "Reply" Text

| # | Template Name | Line in Phase6A102 | Exact Text |
|---|--------------|-------------------|------------|
| 1 | `template-event-cancellation-notifications` | ~8503 | "We hope to see you at future events! If you have questions, feel free to reply to this email." |
| 2 | `template-event-details-publication` | ~9742 | "We look forward to seeing you there! If you have questions, feel free to reply to this email." |
| 3 | `template-free-event-registration-confirmation` | ~10911 | "We look forward to seeing you at the event! If you have questions, feel free to reply to this email." |
| 4 | `template-paid-event-registration-confirmation-with-ticket` | ~16020 | "We look forward to seeing you at the event! If you have questions, feel free to reply to this email." |
| 5 | `template-password-change-confirmation` | ~16690 | "If you have questions, feel free to reply to this email. We're here to help." |
| 6 | `template-password-reset` | ~17344 | "If you have questions, feel free to reply to this email. We're here to help." |
| 7 | `template-signup-list-commitment-confirmation` | ~22248 | "We appreciate your contribution! If you have questions, feel free to reply to this email." |
| 8 | `template-signup-list-commitment-update` | ~23398 | "Thank you for your continued support! If you have questions, feel free to reply to this email." |
| 9 | `template-support-ticket-reply` (text_template) | ~24246 | "If you have any further questions, please reply to this email or contact us at {{SupportEmail}}" |
| 10 | `template-support-ticket-reply` (html_template) | ~24565 | "If you have any follow-up questions, please reply to this email. We're here to help." |

**Note**: Template #9/#10 (`template-support-ticket-reply`) is a special case -- for support ticket replies, replying to the email MIGHT be intentionally supported. All others should NOT suggest replying.

**Recommended replacement text**: "If you have questions, please contact us at {{SupportEmail}}." or use the `{{#HasOrganizerContact}}` conditional to show the organizer's contact info.

---

## 5. Signup Commitment Templates - Image Support Status

| Template | Has `<!-- BODY CONTENT` anchor? | Anchor variant | Currently has image block? | Params pass image? |
|----------|-------------------------------|----------------|--------------------------|-------------------|
| `template-signup-list-commitment-cancellation` | YES | `<!-- BODY CONTENT -->` (no "720px inner") | NO | YES (handler code at line 101 of `CommitmentCancelledEmailHandler.cs`) |
| `template-signup-list-commitment-confirmation` | YES | `<!-- BODY CONTENT (720px inner) -->` | NO | YES (handler code at line 101 of `UserCommittedToSignUpEventHandler.cs`) |
| `template-signup-list-commitment-update` | YES | `<!-- BODY CONTENT (720px inner) -->` | NO | YES (handler code at line 101 of `CommitmentUpdatedEventHandler.cs`) |

**Finding**: The signup commitment templates have the code-side support (params + handler) but the HTML templates in the database do NOT have the `{{#HasEventImage}}` block. These should be added in the fix.

**IMPORTANT**: The cancellation template uses a different anchor variant (`<!-- BODY CONTENT -->` without the "720px inner" suffix). The `replace()` approach still works because `'<!-- BODY CONTENT'` is a substring of both variants, but this is worth noting for consistency.

---

## 6. Fix Plan

### Step 1: Generate Proper EF Core Migration (CRITICAL)

The existing hand-crafted migration file must be REPLACED with a properly scaffolded one.

```bash
# 1. Delete the broken migration file
rm src/LankaConnect.Infrastructure/Data/Migrations/20260211100000_Phase6A103_AddEventImageToEmailTemplates.cs

# 2. Generate a new migration (EF Core will create BOTH .cs and .Designer.cs)
dotnet ef migrations add Phase6A103_AddEventImageToEmailTemplates \
    --project src/LankaConnect.Infrastructure \
    --startup-project src/LankaConnect.API

# 3. Replace the auto-generated Up() and Down() methods with the SQL from the original file
#    BUT with these corrections (see Step 2)
```

### Step 2: Fix Image HTML to Match Working Pattern

Add the `onerror` graceful fallback to the injected image HTML:

```sql
UPDATE communications.email_templates
SET html_template = replace(
    html_template,
    '<!-- BODY CONTENT',
    '<!-- EVENT IMAGE (conditional + graceful fallback) -->
                        {{#HasEventImage}}
                        <!--[if !mso]><!-->
                        <tr>
                            <td style="font-size: 0; line-height: 0">
                                <!--<![endif]-->
                                <img
                                    src="{{EventImageUrl}}"
                                    alt="{{EventTitle}}"
                                    width="860"
                                    style="width: 100%; max-height: 300px; object-fit: cover; display: block"
                                    onerror="
                                        this.style.display = ''none'';
                                        this.parentElement.style.height = ''0'';
                                        this.parentElement.style.overflow = ''hidden'';
                                    "
                                />
                                <!--[if !mso]><!-->
                            </td>
                        </tr>
                        <!--<![endif]-->
                        {{/HasEventImage}}

                        <!-- BODY CONTENT'),
    updated_at = NOW()
WHERE name IN (
    'template-event-details-publication',
    'template-new-event-publication',
    'template-event-reminder',
    'template-event-cancellation-notifications',
    'template-event-approval',
    'template-signup-list-commitment-cancellation',
    'template-signup-list-commitment-confirmation',
    'template-signup-list-commitment-update'
)
AND html_template NOT LIKE '%HasEventImage%';
```

**Changes from original**:
1. Added `onerror` graceful fallback (matches working templates)
2. Added 3 signup commitment templates to the WHERE clause
3. Total templates updated: 8 (was 5)

### Step 3: Fix "Reply to this Email" Text (Separate Migration)

Create a second migration to replace misleading reply text:

```sql
-- Replace "feel free to reply to this email" with proper contact guidance
-- in all templates EXCEPT template-support-ticket-reply (where replying may be intended)
UPDATE communications.email_templates
SET html_template = replace(
    html_template,
    'feel free to reply to this email',
    'contact the event organizer or reach us at lankaconnect.app@gmail.com'
),
    updated_at = NOW()
WHERE html_template LIKE '%feel free to reply to this email%'
AND name != 'template-support-ticket-reply';

UPDATE communications.email_templates
SET html_template = replace(
    html_template,
    'please reply to this email',
    'please contact us at lankaconnect.app@gmail.com'
),
    updated_at = NOW()
WHERE html_template LIKE '%please reply to this email%'
AND name != 'template-support-ticket-reply';

-- Also fix text_template versions
UPDATE communications.email_templates
SET text_template = replace(
    text_template,
    'feel free to reply to this email',
    'contact the event organizer or reach us at lankaconnect.app@gmail.com'
),
    updated_at = NOW()
WHERE text_template LIKE '%feel free to reply to this email%'
AND name != 'template-support-ticket-reply';
```

### Step 4: Deploy and Verify

```bash
# 1. Run tests
dotnet test

# 2. Apply migration locally
dotnet ef database update --project src/LankaConnect.Infrastructure

# 3. Verify templates were updated
# Query: SELECT name, html_template LIKE '%HasEventImage%' as has_image
#        FROM communications.email_templates
#        WHERE name IN ('template-event-details-publication', ...);

# 4. Push to staging, verify via API test
```

---

## 7. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| New migration conflicts with existing | Low | High | Pull latest develop before generating |
| `replace()` modifies wrong part of template | Very Low | Medium | `NOT LIKE '%HasEventImage%'` guard prevents double-injection; `<!-- BODY CONTENT` anchor is specific |
| Image URLs are broken/missing for some events | Medium | Low | `onerror` handler hides broken images gracefully; `HasEventImage=false` skips the entire block |
| "Reply to this email" fix breaks template rendering | Very Low | Low | Simple string replacement, no Handlebars logic affected |
| Signup commitment templates have wrong anchor variant | Low | None | PostgreSQL `replace()` with `'<!-- BODY CONTENT'` substring matches BOTH variants (`<!-- BODY CONTENT -->` and `<!-- BODY CONTENT (720px inner) -->`) |

---

## 8. Files Referenced in This Analysis

### Migration Files
- `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Migrations\20260211100000_Phase6A103_AddEventImageToEmailTemplates.cs` (broken -- missing Designer.cs)
- `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Migrations\20260210231302_Phase6A102_SyncProductionFromStaging.cs` (reference for template HTML)
- `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Migrations\20260210231302_Phase6A102_SyncProductionFromStaging.Designer.cs` (example of proper migration)

### EmailParams Files
- `c:\Work\LankaConnect\src\LankaConnect.Shared\Email\Contracts\EventDetailsEmailParams.cs`
- `c:\Work\LankaConnect\src\LankaConnect.Shared\Email\Contracts\EventReminderEmailParams.cs`
- `c:\Work\LankaConnect\src\LankaConnect.Shared\Email\Contracts\EventCancellationEmailParams.cs`
- `c:\Work\LankaConnect\src\LankaConnect.Shared\Email\Contracts\EventApprovalEmailParams.cs`
- `c:\Work\LankaConnect\src\LankaConnect.Shared\Email\Contracts\SignupCommitmentEmailParams.cs`
- `c:\Work\LankaConnect\src\LankaConnect.Shared\Email\Contracts\EmailTemplateContract.cs`

### Handler/Job Files
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\BackgroundJobs\EventNotificationEmailJob.cs`
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\BackgroundJobs\EventCancellationEmailJob.cs`
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\BackgroundJobs\EventReminderJob.cs`
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\EventHandlers\EventApprovedEventHandler.cs`
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\EventHandlers\EventPublishedEventHandler.cs`
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\EventHandlers\UserCommittedToSignUpEventHandler.cs`
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\EventHandlers\CommitmentUpdatedEventHandler.cs`
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\EventHandlers\CommitmentCancelledEmailHandler.cs`

### Configuration
- `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Email\Configuration\EmailSettings.cs` (DoNotReply sender address)

---

## 9. Lessons Learned

1. **Never hand-craft EF Core migration files.** Always use `dotnet ef migrations add` to generate the `.cs` and `.Designer.cs` pair. The `.Designer.cs` file contains the `[Migration]` attribute and model snapshot that EF Core requires to discover and execute the migration.

2. **Verify migration registration before deployment.** Run `dotnet ef migrations list` to confirm the migration appears before deploying.

3. **Match existing patterns exactly.** When replicating HTML from one template to another, copy the COMPLETE pattern including error handlers (`onerror`), not just the visual elements.

4. **Audit sender address vs. template copy.** If emails are sent from a DoNotReply address, templates should NEVER suggest "reply to this email."
