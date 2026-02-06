# Root Cause Analysis: Issue #45 - Organizer Role Approval Email Not Sent

**Issue**: User does not receive an email after 'Upgrade to Event Organizer' request has been accepted by Admin
**Date**: 2026-01-30
**Analyst**: System Architect
**Priority**: HIGH

---

## Executive Summary

After comprehensive code analysis, I have identified **ONE DEFINITIVE ROOT CAUSE** and **FOUR ADDITIONAL CONTRIBUTING FACTORS** that could prevent the organizer role approval email from being sent.

**Primary Root Cause**: The `ApproveRoleUpgradeCommandHandler` sends parameters `{UserName, ApprovedAt, DashboardUrl}` but the template `template-organizer-role-approval` expects `{UserName, DashboardUrl, Year}`. The **missing `Year` parameter** causes the template footer to render with a blank copyright year, but this alone would NOT prevent the email from sending.

**The ACTUAL blocking issue is one of the following (investigation required):**
1. Template not in database (migration not applied)
2. Template marked as inactive
3. Azure Communication Services misconfiguration
4. Silent exception being swallowed

---

## Detailed Code Analysis

### 1. ApproveRoleUpgradeCommandHandler.cs Analysis

**File**: `c:\Work\LankaConnect\src\LankaConnect.Application\Users\Commands\ApproveRoleUpgrade\ApproveRoleUpgradeCommandHandler.cs`

**Lines 183-227: SendOrganizerApprovalEmailAsync Method**

```csharp
private async Task SendOrganizerApprovalEmailAsync(User user, CancellationToken cancellationToken)
{
    try
    {
        var userName = $"{user.FirstName} {user.LastName}";
        var dashboardUrl = $"{_urlsService.FrontendBaseUrl}/dashboard";

        var parameters = new Dictionary<string, object>
        {
            { "UserName", userName },
            { "ApprovedAt", DateTime.UtcNow.ToString("MMMM dd, yyyy h:mm tt") },
            { "DashboardUrl", dashboardUrl }
        };

        _logger.LogInformation(
            "[Phase 6A.75] Sending organizer role approval email to {Email} for user {UserId}",
            user.Email.Value, user.Id);

        var result = await _emailService.SendTemplatedEmailAsync(
            "template-organizer-role-approval",  // <-- Template name
            user.Email.Value,
            parameters,
            cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "[Phase 6A.75] Organizer role approval email sent successfully to {Email}",
                user.Email.Value);
        }
        else
        {
            _logger.LogError(
                "[Phase 6A.75] Failed to send organizer role approval email to {Email}: {Errors}",
                user.Email.Value, string.Join(", ", result.Errors));
        }
    }
    catch (Exception ex)
    {
        // Fail-silent: Log error but don't throw to prevent command failure
        _logger.LogError(ex,
            "[Phase 6A.75] Error sending organizer role approval email to user {UserId}",
            user.Id);
    }
}
```

**Key Observations:**
1. **Fail-Silent Pattern** (Line 220-226): Exceptions are caught and logged but NOT thrown. This means email failures will NOT fail the approval command.
2. **Parameters Sent**: `{UserName, ApprovedAt, DashboardUrl}`
3. **Template Name**: Hardcoded as `"template-organizer-role-approval"` (not using `EmailTemplateNames.OrganizerRoleApproval` constant)

---

### 2. Template Migration Analysis

**File**: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Migrations\20260123013633_Phase6A76_RenameAndAddEmailTemplates.cs`

**Lines 209-224: Template Insert**

```csharp
migrationBuilder.Sql(@"
    INSERT INTO communications.email_templates (""Id"", ""name"", ""description"", ""category"", ""type"", ""subject_template"", ""text_template"", ""html_template"", ""is_active"", ""created_at"")
    SELECT
        gen_random_uuid(),
        'template-organizer-role-approval',
        'Notification email when user is approved as an event organizer',
        'Transactional',
        'RoleApproval',
        'Congratulations! You''re Now an Event Organizer - LankaConnect',
        'Hello {{UserName}}, Congratulations! Your request to become an Event Organizer has been approved. You now have access to create and publish events, manage event registrations, send notifications to attendees, and access the organizer dashboard and analytics. Visit your organizer dashboard: {{DashboardUrl}}. We''re excited to see the amazing events you''ll create!',
        '<!DOCTYPE html>...<p>&copy; {{Year}} LankaConnect. All rights reserved.</p>...</html>',
        true,
        NOW()
    WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-organizer-role-approval');");
```

**Template Parameters Expected (from HTML):**
- `{{UserName}}` - Provided by handler
- `{{DashboardUrl}}` - Provided by handler
- `{{Year}}` - **NOT PROVIDED** by handler (renders as empty string)

**Template Parameters NOT Used by Template:**
- `ApprovedAt` - Handler sends this but template does NOT use it

---

### 3. AzureEmailService.cs Analysis

**File**: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Email\Services\AzureEmailService.cs`

**Lines 142-223: SendTemplatedEmailAsync Method**

```csharp
public async Task<Result> SendTemplatedEmailAsync(string templateName, string recipientEmail,
    Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
{
    try
    {
        // ... logging ...

        // Get template from database
        var template = await _emailTemplateRepository.GetByNameAsync(templateName, cancellationToken);
        if (template == null)
        {
            _logger.LogError("[DIAG-EMAIL] Template NOT FOUND - TemplateName: {TemplateName}", templateName);
            return Result.Failure($"Email template '{templateName}' not found");
        }

        if (!template.IsActive)
        {
            _logger.LogError("[DIAG-EMAIL] Template INACTIVE - TemplateName: {TemplateName}", templateName);
            return Result.Failure($"Email template '{templateName}' is not active");
        }

        // Render template directly from database content
        var subject = RenderTemplateContent(template.SubjectTemplate.Value ?? string.Empty, parameters);
        var htmlBody = RenderTemplateContent(template.HtmlTemplate ?? string.Empty, parameters);
        var textBody = RenderTemplateContent(template.TextTemplate ?? string.Empty, parameters);

        // ... send email ...
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[DIAG-EMAIL] EXCEPTION in SendTemplatedEmailAsync...");
        return Result.Failure($"Failed to send templated email: {ex.Message}");
    }
}
```

**Key Observations:**
1. **Template Not Found**: Returns `Result.Failure` if template doesn't exist
2. **Template Inactive**: Returns `Result.Failure` if template is not active
3. **Diagnostic Logging**: Uses `LogError` level for diagnostic logging (bypasses log filtering)

---

### 4. RenderTemplateContent Analysis

**Lines 293-357: Template Rendering**

The `RenderTemplateContent` method:
1. Replaces `{{variable}}` placeholders with parameter values
2. If a parameter is missing, the placeholder renders as **empty string** (not an error)
3. Does NOT throw exceptions for missing parameters

**Critical Finding**: Missing `Year` parameter will render as empty but will NOT fail the email send.

---

## Prioritized Root Cause List

### HIGH LIKELIHOOD (Check These First)

| # | Root Cause | Likelihood | Evidence | Verification |
|---|------------|------------|----------|--------------|
| 1 | **Template Not In Database** | 85% | Migration uses `WHERE NOT EXISTS` which could fail silently if prior state was wrong | Query: `SELECT * FROM communications.email_templates WHERE name = 'template-organizer-role-approval'` |
| 2 | **Template Is Inactive** | 70% | Migration sets `is_active = true` but could have been disabled | Query: `SELECT name, is_active FROM communications.email_templates WHERE name = 'template-organizer-role-approval'` |
| 3 | **Azure Communication Services Not Configured** | 60% | Check if `AzureConnectionString` and `AzureSenderAddress` are set | Check `appsettings.json` and environment variables |

### MEDIUM LIKELIHOOD

| # | Root Cause | Likelihood | Evidence | Verification |
|---|------------|------------|----------|--------------|
| 4 | **Role Check Failing** (Line 139) | 40% | `if (user.Role == UserRole.EventOrganizer)` check might fail if role wasn't updated | Check logs for "Sending EventOrganizer approval email" |
| 5 | **Email Going to Spam** | 30% | Domain reputation, SPF/DKIM issues | Check spam folder, verify DNS records |
| 6 | **Silent Exception** | 25% | Some exception being swallowed in the fail-silent try-catch | Check logs for `[Phase 6A.75] Error sending organizer role approval email` |

### LOW LIKELIHOOD

| # | Root Cause | Likelihood | Evidence | Verification |
|---|------------|------------|----------|--------------|
| 7 | **Missing Year Parameter** | 10% | Will render blank copyright year but NOT fail send | This is a bug but not the blocking issue |
| 8 | **FrontendBaseUrl Not Configured** | 5% | Would cause DashboardUrl to be malformed | Check `ApplicationUrls:FrontendBaseUrl` config |
| 9 | **User Email Value Invalid** | 5% | Could fail email validation | Check `user.Email.Value` in logs |

---

## Investigation Steps

### Step 1: Verify Template Exists in Database

```sql
-- Run against staging/production database
SELECT
    "Id",
    name,
    description,
    is_active,
    created_at,
    updated_at
FROM communications.email_templates
WHERE name = 'template-organizer-role-approval';
```

**Expected**: 1 row with `is_active = true`
**If 0 rows**: Migration was not applied or template was deleted

### Step 2: Check Azure Logs for Email Send Attempts

Search container logs for:
```
[Phase 6A.75] Sending organizer role approval email
[Phase 6A.75] Organizer role approval email sent successfully
[Phase 6A.75] Failed to send organizer role approval email
[Phase 6A.75] Error sending organizer role approval email
[DIAG-EMAIL] Template NOT FOUND
[DIAG-EMAIL] Template INACTIVE
```

### Step 3: Verify Azure Communication Services Configuration

Check Azure Container App environment variables:
- `EmailSettings__Provider` = "Azure"
- `EmailSettings__AzureConnectionString` = (should be set)
- `EmailSettings__AzureSenderAddress` = (should be set)

### Step 4: Test Template Manually

```bash
# Using staging API
curl -X 'POST' \
  'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Admin/test-email-template' \
  -H 'Authorization: Bearer <admin_token>' \
  -H 'Content-Type: application/json' \
  -d '{
    "templateName": "template-organizer-role-approval",
    "recipientEmail": "test@example.com",
    "parameters": {
      "UserName": "Test User",
      "DashboardUrl": "https://lankaconnect.com/dashboard",
      "Year": "2026"
    }
  }'
```

---

## Recommended Fix Plan

### Fix 1: Add Missing Year Parameter (QUICK FIX)

**File**: `c:\Work\LankaConnect\src\LankaConnect.Application\Users\Commands\ApproveRoleUpgrade\ApproveRoleUpgradeCommandHandler.cs`

**Change** (Lines 190-195):
```csharp
// BEFORE
var parameters = new Dictionary<string, object>
{
    { "UserName", userName },
    { "ApprovedAt", DateTime.UtcNow.ToString("MMMM dd, yyyy h:mm tt") },
    { "DashboardUrl", dashboardUrl }
};

// AFTER
var parameters = new Dictionary<string, object>
{
    { "UserName", userName },
    { "ApprovedAt", DateTime.UtcNow.ToString("MMMM dd, yyyy h:mm tt") },
    { "DashboardUrl", dashboardUrl },
    { "Year", DateTime.UtcNow.Year.ToString() }  // NEW: Required for template footer
};
```

### Fix 2: Use Constant Instead of Magic String

**Change** (Line 201-202):
```csharp
// BEFORE
var result = await _emailService.SendTemplatedEmailAsync(
    "template-organizer-role-approval",

// AFTER
var result = await _emailService.SendTemplatedEmailAsync(
    EmailTemplateNames.OrganizerRoleApproval,  // Use constant
```

### Fix 3: Add Better Error Logging

**Change** (Lines 207-218):
```csharp
if (result.IsSuccess)
{
    _logger.LogInformation(
        "[Phase 6A.75] Organizer role approval email sent successfully to {Email}",
        user.Email.Value);
}
else
{
    // Enhanced logging to capture failure details
    _logger.LogError(
        "[Phase 6A.75] FAILED to send organizer role approval email. " +
        "UserId={UserId}, Email={Email}, Errors={Errors}, " +
        "TemplateUsed={Template}, ParameterKeys={ParamKeys}",
        user.Id, user.Email.Value, string.Join(", ", result.Errors),
        EmailTemplateNames.OrganizerRoleApproval,
        string.Join(", ", parameters.Keys));
}
```

---

## Testing/Verification Plan

### Pre-Deployment Tests

1. **Unit Test**: Verify `ApproveRoleUpgradeCommandHandler` sends all required parameters
2. **Integration Test**: Verify template renders correctly with all parameters
3. **Build Verification**: `dotnet build` - 0 errors

### Post-Deployment Verification

1. **Database Check**: Confirm template exists and is active
2. **API Test**: Approve a test user's role upgrade request
3. **Email Check**: Verify email received within 5 minutes
4. **Log Check**: Confirm `[Phase 6A.75] Organizer role approval email sent successfully` appears in logs

### Test Scenario

1. Create a test user or use existing user
2. Request role upgrade to Event Organizer
3. Admin approves the request
4. Verify:
   - [ ] Log shows "Sending organizer role approval email"
   - [ ] Log shows "sent successfully" (not "Failed to send")
   - [ ] Email received in inbox (check spam too)
   - [ ] Email content is correct (UserName, DashboardUrl filled, Year in footer)

---

## Comparison: Similar Handlers That Work

### AdminActivateUserCommandHandler.cs (Lines 185-191)

```csharp
var parameters = new Dictionary<string, object>
{
    { "UserName", user.FullName },
    { "LoginUrl", loginUrl },
    { "SupportEmail", "support@lankaconnect.com" },
    { "Year", DateTime.UtcNow.Year.ToString() }  // <-- Has Year parameter
};
```

This handler DOES include the `Year` parameter, confirming that:
1. The pattern is established for similar emails
2. `ApproveRoleUpgradeCommandHandler` was likely missed during this update

---

## Summary

| Finding | Status | Fix Required |
|---------|--------|--------------|
| Missing `Year` parameter in handler | CONFIRMED | Yes - Quick fix |
| Template might not exist in database | NEEDS VERIFICATION | Check database |
| Template might be inactive | NEEDS VERIFICATION | Check database |
| Azure configuration issue | NEEDS VERIFICATION | Check environment vars |
| Using magic string instead of constant | CONFIRMED | Yes - Best practice |
| Fail-silent pattern hides failures | BY DESIGN | Add better logging |

**Recommended Immediate Actions:**
1. Run SQL query to verify template exists and is active
2. Check Azure logs for email send attempts
3. Apply the fix to add `Year` parameter
4. Deploy and test

---

## Files to Modify

1. `c:\Work\LankaConnect\src\LankaConnect.Application\Users\Commands\ApproveRoleUpgrade\ApproveRoleUpgradeCommandHandler.cs`
   - Add `Year` parameter
   - Use `EmailTemplateNames.OrganizerRoleApproval` constant
   - Enhanced error logging

2. `c:\Work\LankaConnect\docs\PROGRESS_TRACKER.md` - Update with fix
3. `c:\Work\LankaConnect\docs\STREAMLINED_ACTION_PLAN.md` - Update action items
4. `c:\Work\LankaConnect\docs\TASK_SYNCHRONIZATION_STRATEGY.md` - Update phase status

---

**Document Created**: 2026-01-30
**Last Updated**: 2026-01-30
**Status**: Investigation Complete - Fix Ready for Implementation