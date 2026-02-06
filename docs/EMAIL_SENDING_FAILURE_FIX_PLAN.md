# Email Sending Failure - Fix Plan

**Date**: December 23, 2025
**Priority**: CRITICAL
**Target Resolution Time**: < 2 hours

---

## Fix Strategy

Based on Root Cause Analysis, we have **3 fix options**:

1. **Option A: Execute Migration** (RECOMMENDED) - Apply Phase 6A.43 migration to staging
2. **Option B: Rollback Code** (FALLBACK) - Revert DI changes to use filesystem templates
3. **Option C: Hybrid Fix** (COMPLEX) - Add fallback logic to handle missing templates

---

## PHASE 1: INVESTIGATION (15 minutes)

**CRITICAL: Must complete these checks BEFORE applying any fix**

### Step 1.1: Verify Deployment State

```bash
# Check Azure Container App deployment history
az containerapp revision list \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query "[].{Name:name, Created:properties.createdTime, Active:properties.active}" \
  --output table

# Get current revision details
az containerapp revision show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --revision <latest-revision-name> \
  --query "properties.template.containers[0].image"
```

**Expected Result**: Confirm deployment includes commit 2bda1cfb (Phase 6A.43)

### Step 1.2: Check Database Migration Status

```sql
-- Connect to staging database
psql -h <staging-db-host> -U <username> -d lankaconnect_staging

-- Check if Phase 6A.43 migration was applied
SELECT migration_id, product_version
FROM public."__EFMigrationsHistory"
WHERE migration_id LIKE '%Phase6A43%'
OR migration_id LIKE '%20251223%'
ORDER BY migration_id DESC;

-- Expected: Should see migration '20251223144022_UpdateAttendeesAgeCategoryAndGender_Phase6A43'
-- If MISSING: Migration was never run (confirms hypothesis)
```

### Step 1.3: Verify Email Templates Table State

```sql
-- Check table structure
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'communications'
  AND table_name = 'email_templates'
ORDER BY ordinal_position;

-- Check template rows
SELECT id, name, subject_template, is_active, created_at, updated_at
FROM communications.email_templates
WHERE name IN ('registration-confirmation', 'ticket-confirmation');

-- Expected Results:
-- CASE 1: Table doesn't exist → Need full migration
-- CASE 2: Table exists but no rows → Need data seeding
-- CASE 3: Table exists with old format → Need schema update
-- CASE 4: Table exists with correct schema → EF Core issue
```

### Step 1.4: Check Application Logs

```bash
# Get recent logs from Azure Container App
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --follow false \
  --tail 500 \
  | grep -i "email\|template\|failed to render"

# Look for specific errors:
# - "Email template 'registration-confirmation' not found"
# - "Email template 'ticket-confirmation' not found"
# - "Failed to materialize EmailSubject"
# - EF Core relationship errors
```

**DECISION TREE**:

```
Investigation Results → Fix Selection

CASE 1: Migration NOT in __EFMigrationsHistory
        ↓
        Option A: Execute Migration (RECOMMENDED)

CASE 2: Migration IN __EFMigrationsHistory BUT templates missing
        ↓
        Option A: Reseed templates manually

CASE 3: Migration IN __EFMigrationsHistory AND templates exist
        ↓
        CASE 3a: SubjectTemplate column wrong type
                ↓
                Option A: Fix schema and reseed

        CASE 3b: EF Core materialization error in logs
                ↓
                Option C: Fix EF Core configuration

CASE 4: Code NOT deployed (still using filesystem templates)
        ↓
        No fix needed - Deploy Phase 6A.43 properly
```

---

## PHASE 2: FIX IMPLEMENTATION

### OPTION A: Execute Migration (RECOMMENDED)

**Prerequisites**:
- Investigation confirms migration is missing
- Database backup taken
- Deployment window available (5-10 minutes downtime)

#### Step A.1: Locate Migration File

```bash
# Find the migration file
cd src/LankaConnect.Infrastructure/Data/Migrations
ls -la *Phase6A43*

# Expected file: 20251223144022_UpdateAttendeesAgeCategoryAndGender_Phase6A43.cs
```

#### Step A.2: Review Migration Content

```bash
# Read the migration to verify it seeds templates
cat 20251223144022_UpdateAttendeesAgeCategoryAndGender_Phase6A43.cs

# Check for:
# 1. Schema changes to email_templates table
# 2. INSERT statements for registration-confirmation template
# 3. INSERT statements for ticket-confirmation template
# 4. subject_template column updates
```

#### Step A.3: Execute Migration on Staging

**Option A.3.1: Via EF Core CLI** (Safest)

```bash
# Set connection string
export ConnectionStrings__DefaultConnection="Host=<staging-db>;Database=lankaconnect_staging;Username=<user>;Password=<pass>"

# Navigate to Infrastructure project
cd src/LankaConnect.Infrastructure

# Apply pending migrations
dotnet ef database update --project . --startup-project ../LankaConnect.API --verbose

# Verify migration applied
dotnet ef migrations list --project . --startup-project ../LankaConnect.API
```

**Option A.3.2: Via SQL Script** (Faster for hotfix)

```bash
# Generate SQL script from migration
dotnet ef migrations script <previous-migration> 20251223144022_UpdateAttendeesAgeCategoryAndGender_Phase6A43 \
  --project src/LankaConnect.Infrastructure \
  --startup-project src/LankaConnect.API \
  --output migration-phase6a43.sql

# Review script
cat migration-phase6a43.sql

# Execute on staging database
psql -h <staging-db-host> -U <username> -d lankaconnect_staging -f migration-phase6a43.sql
```

#### Step A.4: Verify Templates Created

```sql
-- Verify templates exist with correct schema
SELECT
    name,
    subject_template,
    LEFT(html_template, 100) as html_preview,
    is_active,
    type,
    category
FROM communications.email_templates
WHERE name IN ('registration-confirmation', 'ticket-confirmation');

-- Expected Results:
-- name: registration-confirmation
-- subject_template: "Your Event Registration Confirmation - {{EventTitle}}"
-- html_template: Should contain {{EventDateTime}}, {{EventLocation}}, {{HasEventImage}}
-- is_active: true

-- name: ticket-confirmation
-- subject_template: "Your Event Ticket - {{EventTitle}}"
-- html_template: Should contain {{EventDateTime}}, {{PaymentDate}}, {{AmountPaid}}
-- is_active: true
```

#### Step A.5: Test Email Sending

```bash
# Test free event registration
curl -X POST https://<staging-api>/api/events/<event-id>/rsvp \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{
    "attendees": [{"name": "Test User", "ageCategory": "Adult", "gender": "Male"}],
    "contactEmail": "test@example.com"
  }'

# Check logs for email sending
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --follow true \
  --tail 100 \
  | grep "Email sent successfully"

# Expected: "Email sent successfully to test@example.com"
```

**Rollback Plan for Option A**:
```bash
# If migration causes issues, revert to previous migration
dotnet ef database update <previous-migration-id> \
  --project src/LankaConnect.Infrastructure \
  --startup-project src/LankaConnect.API
```

---

### OPTION B: Rollback Code (FALLBACK)

**Use When**:
- Migration execution fails
- Database schema issues prevent migration
- Need immediate fix while investigating root cause

#### Step B.1: Revert DependencyInjection.cs

```bash
# Revert to commit before Phase 6A.43
git diff 73638ab7 2bda1cfb src/LankaConnect.Infrastructure/DependencyInjection.cs

# Create rollback commit
git checkout 73638ab7 -- src/LankaConnect.Infrastructure/DependencyInjection.cs

# Verify change
git diff --cached
```

**Changes to Revert** (lines 207-212):

```csharp
// REMOVE Phase 6A.43 changes:
services.AddScoped<IEmailTemplateService>(provider =>
    provider.GetRequiredService<IEmailService>() as IEmailTemplateService
    ?? throw new InvalidOperationException("IEmailService must implement IEmailTemplateService"));

// RESTORE original registration:
services.AddScoped<IEmailTemplateService, RazorEmailTemplateService>();
```

#### Step B.2: Verify Filesystem Templates Exist

```bash
# Check if old templates exist
ls -la src/LankaConnect.Infrastructure/EmailTemplates/

# Expected files:
# - registration-confirmation-subject.txt
# - registration-confirmation-html.html
# - registration-confirmation-text.txt
# - ticket-confirmation-subject.txt
# - ticket-confirmation-html.html
# - ticket-confirmation-text.txt
```

#### Step B.3: Update Template Variables (Critical)

**Problem**: Old templates use `{{EventStartDate}}`, `{{EventStartTime}}`
**Current Code**: Sends `{{EventDateTime}}`

**Fix Required**:

```html
<!-- registration-confirmation-html.html -->
<!-- BEFORE -->
<p>Date: {{EventStartDate}}</p>
<p>Time: {{EventStartTime}}</p>

<!-- AFTER (update to match current parameters) -->
<p>{{EventDateTime}}</p>
```

**Files to Update**:
1. `registration-confirmation-html.html` → Add `{{EventDateTime}}` parameter
2. `registration-confirmation-subject.txt` → Verify variables
3. `ticket-confirmation-html.html` → Add `{{EventDateTime}}`, `{{AmountPaid}}`, `{{PaymentDate}}`

#### Step B.4: Rebuild and Redeploy

```bash
# Build project
dotnet build src/LankaConnect.API/LankaConnect.API.csproj --configuration Release

# Run tests
dotnet test tests/LankaConnect.Infrastructure.Tests/

# Deploy to staging
# (deployment commands specific to your CI/CD pipeline)
```

#### Step B.5: Verify Rollback Success

```bash
# Test email sending
curl -X POST https://<staging-api>/api/events/<event-id>/rsvp \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{"attendees": [...], "contactEmail": "test@example.com"}'

# Check logs
az containerapp logs show --follow true | grep "Email sent successfully"
```

**Drawback**: Loses Phase 6A.43 benefits (unified template system)

---

### OPTION C: Hybrid Fix with Fallback (COMPLEX)

**Use When**:
- Migration exists but templates intermittently fail
- Need graceful degradation
- Time for proper fix but need immediate mitigation

#### Step C.1: Add Fallback Logic to AzureEmailService

```csharp
// AzureEmailService.cs - Add fallback to filesystem templates
public async Task<Result<RenderedEmailTemplate>> RenderTemplateAsync(
    string templateName,
    Dictionary<string, object> parameters,
    CancellationToken cancellationToken = default)
{
    try
    {
        // Try database templates first
        var template = await _emailTemplateRepository.GetByNameAsync(templateName, cancellationToken);

        if (template == null)
        {
            _logger.LogWarning(
                "Email template '{TemplateName}' not found in database - FALLING BACK to filesystem",
                templateName);

            // FALLBACK: Use RazorEmailTemplateService
            var razorService = new RazorEmailTemplateService(_logger);
            return await razorService.RenderTemplateAsync(templateName, parameters, cancellationToken);
        }

        // Rest of existing code...
        var subject = RenderTemplateContent(template.SubjectTemplate.Value, parameters);
        var htmlBody = RenderTemplateContent(template.HtmlTemplate ?? string.Empty, parameters);
        var textBody = RenderTemplateContent(template.TextTemplate, parameters);

        return Result<RenderedEmailTemplate>.Success(new RenderedEmailTemplate
        {
            Subject = subject,
            HtmlBody = htmlBody,
            PlainTextBody = textBody
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to render template '{TemplateName}' - attempting fallback", templateName);

        // FALLBACK on any error
        try
        {
            var razorService = new RazorEmailTemplateService(_logger);
            return await razorService.RenderTemplateAsync(templateName, parameters, cancellationToken);
        }
        catch (Exception fallbackEx)
        {
            _logger.LogError(fallbackEx, "Fallback template rendering also failed for '{TemplateName}'", templateName);
            return Result<RenderedEmailTemplate>.Failure($"Failed to render template: {ex.Message}");
        }
    }
}
```

#### Step C.2: Update DependencyInjection.cs

```csharp
// Keep Phase 6A.43 changes but add RazorEmailTemplateService registration
services.AddScoped<IEmailTemplateService>(provider =>
    provider.GetRequiredService<IEmailService>() as IEmailTemplateService
    ?? throw new InvalidOperationException("IEmailService must implement IEmailTemplateService"));

// Add Razor service for fallback (internal use only)
services.AddScoped<RazorEmailTemplateService>();
```

**Drawback**: Adds complexity, harder to debug, masks underlying issue

---

## PHASE 3: VERIFICATION (15 minutes)

### Step 3.1: Smoke Tests

```bash
# Test 1: Free Event Registration
POST /api/events/{free-event-id}/rsvp
{
  "attendees": [{"name": "Test Free", "ageCategory": "Adult", "gender": "Male"}],
  "contactEmail": "test-free@example.com"
}

# Test 2: Paid Event Registration + Payment
POST /api/events/{paid-event-id}/rsvp
{
  "attendees": [{"name": "Test Paid", "ageCategory": "Adult", "gender": "Male"}],
  "contactEmail": "test-paid@example.com"
}
# Complete Stripe payment flow
# Verify email sent after payment completion

# Test 3: Group Registration (Multiple Attendees)
POST /api/events/{event-id}/rsvp
{
  "attendees": [
    {"name": "Adult 1", "ageCategory": "Adult", "gender": "Male"},
    {"name": "Child 1", "ageCategory": "Child", "gender": "Female"}
  ],
  "contactEmail": "test-group@example.com"
}
```

### Step 3.2: Verify Email Content

**Checklist**:
- [ ] Subject line renders correctly (no {{variables}} showing)
- [ ] Event title displays
- [ ] Event date/time shows formatted correctly
- [ ] Event location displays (or "Online Event")
- [ ] Attendee names listed correctly
- [ ] Event image displays (if exists)
- [ ] Contact information shows (if provided)
- [ ] For paid events: Payment amount, date, ticket code

### Step 3.3: Check Database State

```sql
-- Verify email messages created
SELECT
    id,
    subject,
    status,
    sent_at,
    failed_reason,
    created_at
FROM communications.email_messages
WHERE created_at > NOW() - INTERVAL '1 hour'
ORDER BY created_at DESC
LIMIT 20;

-- Expected: status = 'Sent', sent_at populated, failed_reason = NULL
```

### Step 3.4: Monitor Application Logs

```bash
# Monitor logs for 10 minutes
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --follow true \
  | grep -E "Email|Template|Registration|Payment"

# Look for:
# ✅ "Email sent successfully to ..."
# ✅ "Template 'registration-confirmation' rendered from database successfully"
# ❌ "Failed to render email template"
# ❌ "Email template 'X' not found"
```

---

## PHASE 4: ROLLBACK PLAN (If Fix Fails)

### Immediate Rollback

```bash
# Revert to last known good deployment
az containerapp revision copy \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --from-revision <previous-working-revision>

# OR: Manual code rollback
git revert 2bda1cfb  # Revert Phase 6A.43 commit
git push origin develop

# Trigger deployment
# (use your CI/CD pipeline)
```

### Database Rollback (If Migration Applied)

```bash
# Revert to previous migration
dotnet ef database update <previous-migration-id> \
  --project src/LankaConnect.Infrastructure \
  --startup-project src/LankaConnect.API \
  --connection "<staging-connection-string>"

# Verify rollback
dotnet ef migrations list --project src/LankaConnect.Infrastructure
```

---

## PHASE 5: POST-FIX ACTIONS

### Step 5.1: Update Documentation

- [ ] Update PROGRESS_TRACKER.md with Phase 6A.43 deployment status
- [ ] Document fix in PHASE_6A43_PAID_EVENT_FIX_SUMMARY.md
- [ ] Create incident report: EMAIL_SENDING_INCIDENT_2025-12-23.md
- [ ] Update deployment checklist to prevent recurrence

### Step 5.2: Notify Affected Users

```sql
-- Find users who registered during outage period
SELECT DISTINCT
    r.contact->>'email' as email,
    e.title as event_title,
    r.created_at as registration_time
FROM events.registrations r
JOIN events.events e ON r.event_id = e.id
WHERE r.created_at BETWEEN '<outage-start>' AND '<outage-end>'
  AND r.status = 'Confirmed'
ORDER BY r.created_at;

-- Manually resend confirmation emails via ResendTicketEmailCommand
```

### Step 5.3: Implement Monitoring

**Add Alerts**:
1. Email sending failure rate > 10%
2. Template not found errors
3. Database query failures on email_templates table

**Add Health Check**:
```csharp
// Check email template availability on startup
public class EmailTemplateHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(...)
    {
        var templates = new[] { "registration-confirmation", "ticket-confirmation" };
        foreach (var template in templates)
        {
            var result = await _emailTemplateRepository.GetByNameAsync(template);
            if (result == null)
                return HealthCheckResult.Unhealthy($"Template '{template}' not found");
        }
        return HealthCheckResult.Healthy();
    }
}
```

### Step 5.4: Deployment Checklist Update

**Add to deployment checklist**:
- [ ] Run `dotnet ef migrations list` to check pending migrations
- [ ] Execute `dotnet ef database update` BEFORE deploying code changes
- [ ] Verify email templates exist in database
- [ ] Test email sending in staging before production deployment
- [ ] Monitor email sending rate for 1 hour post-deployment

---

## Timeline Estimate

| Phase | Option A (Migration) | Option B (Rollback) | Option C (Hybrid) |
|-------|---------------------|---------------------|-------------------|
| Investigation | 15 min | 15 min | 15 min |
| Implementation | 20 min | 30 min | 45 min |
| Verification | 15 min | 15 min | 20 min |
| **Total** | **50 min** | **60 min** | **80 min** |

---

## Risk Assessment

### Option A: Execute Migration

**Risks**:
- Migration might fail due to schema conflicts
- Downtime during migration execution (5-10 min)
- Data corruption if migration has bugs

**Mitigation**:
- Database backup before migration
- Test migration on local database first
- Have rollback script ready

**Success Probability**: 85%

### Option B: Rollback Code

**Risks**:
- Filesystem templates might be outdated
- Loses Phase 6A.43 benefits
- Need another deployment later to reapply Phase 6A.43

**Mitigation**:
- Update filesystem templates before deployment
- Schedule Phase 6A.43 redeployment

**Success Probability**: 95%

### Option C: Hybrid Fix

**Risks**:
- Adds complexity
- Harder to debug issues
- Masks underlying problem

**Mitigation**:
- Add extensive logging
- Monitor fallback usage
- Plan to remove fallback after root cause fixed

**Success Probability**: 70%

---

## Recommended Approach

**RECOMMENDED: Option A (Execute Migration)**

**Reasoning**:
1. Fixes root cause instead of masking it
2. Aligns code and database state
3. Enables Phase 6A.43 benefits
4. Faster long-term solution
5. Investigation likely confirms migration is missing

**Fallback**: If Option A fails within 30 minutes, switch to Option B

**Next Steps**:
1. Execute PHASE 1 investigation (15 min)
2. Based on results, choose Option A or B
3. Execute fix plan
4. Verify success
5. Document and monitor

---

## Contact Information

**Incident Commander**: System Architecture Team
**Database Admin**: Required for migration execution
**DevOps Engineer**: Required for deployment
**Support Team**: Notify when fix is deployed

**Escalation**: If fix not complete in 2 hours, escalate to CTO

---

**Document Version**: 1.0
**Last Updated**: 2025-12-23
**Status**: READY FOR EXECUTION
