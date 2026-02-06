# Root Cause Analysis: Email Verification Not Sent for User Registration

**Date:** 2026-01-26
**Analyst:** Claude (Senior Software Engineer Mode)
**Affected User:** vsinharage@gmail.com
**Severity:** HIGH (Critical Feature Broken)
**Issue Status:** IDENTIFIED - Awaiting Fix

---

## Executive Summary

**Problem:** New user registered with email `vsinharage@gmail.com`, but no email verification email was sent.

**Root Cause:** Email template name mismatch between application code and database. The application code references `template-membership-email-verification` (post-Phase 6A.76 naming convention), but the database still contains `member-email-verification` (old naming convention).

**Classification:** Backend API Issue - Configuration/Migration Inconsistency

**Impact:** ALL new user registrations since deployment are affected. Users cannot verify their email addresses, preventing them from fully utilizing the platform (if email verification is enforced in future phases).

---

## 1. Problem Classification

**Category:** Backend API Issue - Email Service Integration Failure

**Sub-category:** Database Migration Inconsistency

**Nature:** Template name mismatch causing email template lookup failure

---

## 2. Root Cause

### Primary Root Cause

The email verification system fails silently because:

1. **Code expects:** `template-membership-email-verification` (defined in `EmailTemplateNames.cs` line 47)
2. **Database contains:** `member-email-verification` (created by migration `20251228200000_Phase6A53_EnsureMemberEmailVerificationTemplate.cs`)
3. **Migration issue:** Phase 6A.76 migration (`20260123013633_Phase6A76_RenameAndAddEmailTemplates.cs`) was supposed to rename the template from `member-email-verification` to `template-membership-email-verification`, but this migration may not have been applied in the Azure staging database.

### Evidence Trail

1. **Application Code (`EmailTemplateNames.cs`):**
   ```csharp
   public const string MemberEmailVerification = "template-membership-email-verification";
   ```

2. **Event Handler (`MemberVerificationRequestedEventHandler.cs` line 74):**
   ```csharp
   var result = await _emailService.SendTemplatedEmailAsync(
       EmailTemplateNames.MemberEmailVerification,  // = "template-membership-email-verification"
       domainEvent.Email,
       parameters,
       cancellationToken);
   ```

3. **Template Creation Migration (`20251228200000_Phase6A53_EnsureMemberEmailVerificationTemplate.cs` line 37):**
   ```sql
   INSERT INTO communications.email_templates (name, ...)
   VALUES ('member-email-verification', ...)  -- OLD NAME
   ```

4. **Template Rename Migration (`20260123013633_Phase6A76_RenameAndAddEmailTemplates.cs` lines 62-63):**
   ```sql
   UPDATE communications.email_templates
   SET name = 'template-membership-email-verification', updated_at = NOW()
   WHERE name = 'member-email-verification';
   ```

### Why It Fails Silently

The email event handler uses a **fail-silent pattern** (architectural decision to prevent transaction rollback):

**File:** `c:\Work\LankaConnect\src\LankaConnect.Application\Users\EventHandlers\MemberVerificationRequestedEventHandler.cs` (lines 102-110)

```csharp
catch (Exception ex)
{
    stopwatch.Stop();
    // FAIL-SILENT: Log but don't throw (ARCHITECT-REQUIRED)
    _logger.LogError(ex,
        "MemberVerificationRequested FAILED: Exception occurred - UserId={UserId}, Email={Email}, Duration={ElapsedMs}ms",
        domainEvent.UserId, domainEvent.Email, stopwatch.ElapsedMilliseconds);
    // Do NOT re-throw - prevents transaction rollback
}
```

This means:
- User registration completes successfully
- Email template lookup fails (template not found)
- Error is logged but not surfaced to the user
- User receives a "success" response but never gets the email

---

## 3. Current Implementation Analysis

### Registration Flow (Working Correctly)

**Frontend:**
- File: `c:\Work\LankaConnect\web\src\presentation\components\features\auth\RegisterForm.tsx`
- Line 51-58: Calls `authRepository.register()`
- Line 61: Redirects to `/login?registered=true` on success

**Backend API:**
- File: `c:\Work\LankaConnect\src\LankaConnect.API\Controllers\AuthController.cs`
- Line 55-78: `POST /api/Auth/register` endpoint
- Line 63: Calls `_mediator.Send(request)` to invoke `RegisterUserHandler`

**Application Layer:**
- File: `c:\Work\LankaConnect\src\LankaConnect.Application\Auth\Commands\RegisterUser\RegisterUserHandler.cs`
- Line 111: Creates user with `User.Create()`
- Line 186: Adds user to repository
- Line 191: **CRITICAL** - `await _unitOfWork.CommitAsync()` triggers domain event dispatch

**Domain Layer:**
- File: `c:\Work\LankaConnect\src\LankaConnect.Domain\Users\User.cs`
- Line 111: `User.Create()` calls `GenerateEmailVerificationToken()`
- Line 267-283: `GenerateEmailVerificationToken()` generates GUID token and raises `MemberVerificationRequestedEvent`

### Email Sending Flow (BROKEN - Template Not Found)

**Event Handler:**
- File: `c:\Work\LankaConnect\src\LankaConnect.Application\Users\EventHandlers\MemberVerificationRequestedEventHandler.cs`
- Line 74: Calls `_emailService.SendTemplatedEmailAsync(EmailTemplateNames.MemberEmailVerification, ...)`
- **Expected:** Lookup template `template-membership-email-verification`
- **Reality:** Template not found in database (still has old name `member-email-verification`)

**Email Service:**
- File: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Email\Services\AzureEmailService.cs`
- Line 97: Retrieves template from database via `_emailTemplateRepository.GetByNameAsync(templateName)`
- Line 99-103: If template not found, returns `Result.Failure($"Email template '{templateName}' not found")`

**Email Configuration:**
- File: `c:\Work\LankaConnect\src\LankaConnect.API\appsettings.json`
- Email provider: **Azure Communication Services**
- Connection string: Configured
- Sender address: `DoNotReply@7689582e-73cc-4552-b2ff-8afd9d1a6814.azurecomm.net`

---

## 4. Gap Analysis

### What's Missing

1. **Database Migration Not Applied:** The Phase 6A.76 migration that renames the template from `member-email-verification` to `template-membership-email-verification` was not applied to the Azure staging database.

2. **Lack of Template Name Validation:** No startup validation to ensure all required email templates exist with correct names.

3. **Silent Failure:** Email sending failure doesn't surface to user (architectural decision, but needs better monitoring).

4. **No Email Sending Monitoring:** No alerts or dashboard to track email sending failures in production/staging.

---

## 5. Impact Assessment

### Severity: HIGH

**Affected Users:** ALL new registrations since the last deployment (potentially dozens of users)

**Business Impact:**
- Users cannot verify their email addresses
- If email verification becomes required in Phase 2+, unverified users will be locked out
- Poor user experience (expecting verification email, never receiving it)
- Potential reputation damage if users report "broken registration"

**Technical Impact:**
- Email verification system completely non-functional
- Database state inconsistency (code expects new template name, database has old name)
- Fail-silent pattern hides the issue from normal monitoring

**Affected Functionality:**
1. User registration email verification
2. Email resend functionality (same template lookup issue)
3. Any future features depending on email verification status

---

## 6. Fix Plan

### 6.1 Immediate Hotfix (Emergency - Deploy Today)

**Goal:** Fix the template name mismatch immediately to restore email verification.

**Option A: Update Database Template Name (RECOMMENDED)**

**Step 1: Create Hotfix Migration**
```bash
cd c:\Work\LankaConnect\src\LankaConnect.Infrastructure
dotnet ef migrations add Phase6AX_Hotfix_EnsureEmailTemplateNameConsistency
```

**Step 2: Migration Content**

File: `src/LankaConnect.Infrastructure/Data/Migrations/YYYYMMDDHHMMSS_Phase6AX_Hotfix_EnsureEmailTemplateNameConsistency.cs`

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    public partial class Phase6AX_Hotfix_EnsureEmailTemplateNameConsistency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // HOTFIX: Ensure template name matches application code
            // This handles cases where Phase6A76 migration wasn't applied
            migrationBuilder.Sql(@"
                -- Update template name if it exists with old name
                UPDATE communications.email_templates
                SET name = 'template-membership-email-verification', updated_at = NOW()
                WHERE name = 'member-email-verification';

                -- Log the change for audit trail
                DO $$
                BEGIN
                    IF FOUND THEN
                        RAISE NOTICE 'Updated member-email-verification to template-membership-email-verification';
                    ELSE
                        RAISE NOTICE 'Template already has correct name or does not exist';
                    END IF;
                END $$;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to old name
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'member-email-verification', updated_at = NOW()
                WHERE name = 'template-membership-email-verification';
            ");
        }
    }
}
```

**Step 3: Deploy Migration**
```bash
# Test migration locally first
dotnet ef database update --project src/LankaConnect.Infrastructure

# Commit migration
git add src/LankaConnect.Infrastructure/Data/Migrations/
git commit -m "hotfix(email): Ensure email template name consistency for verification emails"

# Push to develop (triggers auto-deploy to staging)
git push origin develop
```

**Step 4: Verify Fix in Staging**
```bash
# After deployment completes, test registration
curl -X 'POST' \
  'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/register' \
  -H 'Content-Type: application/json' \
  -d '{
    "email": "test-verification@example.com",
    "password": "Test123!@#",
    "firstName": "Test",
    "lastName": "User",
    "selectedRole": "GeneralUser",
    "preferredMetroAreaIds": ["guid-of-metro-area"],
    "agreeToTerms": true
  }'

# Check Azure logs for successful email sending
az containerapp logs show --name lankaconnect-api-staging --resource-group lankaconnect-rg --tail 50 | grep "MemberVerification"
```

---

### 6.2 Medium-Term Fix (Deploy This Week)

**Goal:** Add startup validation to prevent similar issues in the future.

**Step 1: Create Email Template Validator Service**

File: `src/LankaConnect.API/Configuration/EmailTemplateValidator.cs`

```csharp
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LankaConnect.API.Configuration;

/// <summary>
/// Validates that all required email templates exist in the database.
/// Runs at application startup to catch configuration issues early.
/// </summary>
public class EmailTemplateValidator : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailTemplateValidator> _logger;

    public EmailTemplateValidator(IServiceProvider serviceProvider, ILogger<EmailTemplateValidator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailTemplateRepository = scope.ServiceProvider.GetRequiredService<IEmailTemplateRepository>();

        _logger.LogInformation("Starting email template validation...");

        var missingTemplates = new List<string>();
        var allTemplates = EmailTemplateNames.All;

        foreach (var templateName in allTemplates)
        {
            var template = await emailTemplateRepository.GetByNameAsync(templateName, cancellationToken);
            if (template == null)
            {
                missingTemplates.Add(templateName);
                _logger.LogError("CRITICAL: Email template '{TemplateName}' not found in database", templateName);
            }
            else if (!template.IsActive)
            {
                _logger.LogWarning("Email template '{TemplateName}' exists but is not active", templateName);
            }
            else
            {
                _logger.LogInformation("Email template '{TemplateName}' validated successfully", templateName);
            }
        }

        if (missingTemplates.Any())
        {
            var errorMessage = $"CRITICAL: Missing email templates: {string.Join(", ", missingTemplates)}. " +
                             "Application may fail to send emails. Check database migrations.";
            _logger.LogError(errorMessage);

            // In production, throw exception to prevent startup with missing templates
            if (!Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                throw new InvalidOperationException(errorMessage);
            }
        }
        else
        {
            _logger.LogInformation("All {Count} email templates validated successfully", allTemplates.Count);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

**Step 2: Register Validator in Program.cs**

File: `src/LankaConnect.API/Program.cs`

```csharp
// Add after other service registrations
builder.Services.AddHostedService<EmailTemplateValidator>();
```

---

### 6.3 Long-Term Fix (Deploy Next Sprint)

**Goal:** Improve email sending observability and error handling.

**1. Add Email Sending Metrics Dashboard**
- Track email sending success/failure rates
- Alert on email template not found errors
- Monitor Azure Communication Services quota usage

**2. Add User-Facing Error Handling**
- Show warning if email verification fails to send
- Provide "Resend Verification Email" button immediately after registration
- Display troubleshooting tips (check spam folder, etc.)

**3. Add Email Template Management UI**
- Admin panel to view/edit email templates
- Template preview functionality
- Template deployment verification

---

### 6.4 Tests to Add (TDD Approach)

**Test 1: Email Template Existence Validation**

File: `tests/LankaConnect.Application.Tests/Users/EventHandlers/MemberVerificationRequestedEventHandlerTests.cs`

```csharp
[Fact]
public async Task Handle_WhenTemplateNotFound_LogsErrorAndDoesNotThrow()
{
    // Arrange
    var emailService = new Mock<IEmailService>();
    emailService
        .Setup(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result.Failure("Email template 'template-membership-email-verification' not found"));

    var handler = new MemberVerificationRequestedEventHandler(
        emailService.Object,
        _logger.Object,
        _urlsService.Object);

    var domainEvent = new MemberVerificationRequestedEvent(
        Guid.NewGuid(),
        "test@example.com",
        "verification-token",
        DateTimeOffset.UtcNow,
        "John",
        "Doe");

    var notification = new DomainEventNotification<MemberVerificationRequestedEvent>(domainEvent);

    // Act & Assert - Should not throw exception
    await handler.Handle(notification, CancellationToken.None);

    // Verify error was logged
    _logger.Verify(
        x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Email sending failed")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}
```

**Test 2: Email Template Name Constant Matches Database**

File: `tests/LankaConnect.Infrastructure.Tests/Email/EmailTemplateIntegrationTests.cs`

```csharp
[Fact]
public async Task MemberEmailVerificationTemplate_Exists_WithCorrectName()
{
    // Arrange
    using var context = CreateDbContext();
    var repository = new EmailTemplateRepository(context);

    // Act
    var template = await repository.GetByNameAsync(
        EmailTemplateNames.MemberEmailVerification,
        CancellationToken.None);

    // Assert
    template.Should().NotBeNull("email template should exist in database");
    template!.Name.Should().Be("template-membership-email-verification");
    template.IsActive.Should().BeTrue("template should be active");
    template.Category.Value.Should().Be("Authentication");
}
```

---

### 6.5 Configuration Changes

**No configuration changes required** - the issue is purely a database migration inconsistency.

---

### 6.6 Deployment Steps

**Step 1: Create and Test Migration Locally**
```bash
cd c:\Work\LankaConnect\src\LankaConnect.Infrastructure
dotnet ef migrations add Phase6AX_Hotfix_EnsureEmailTemplateNameConsistency
dotnet ef database update
```

**Step 2: Verify Migration Locally**
```bash
# Check template name in local database
# (Requires psql or database tool)
SELECT name, is_active FROM communications.email_templates WHERE name LIKE '%email-verification%';
```

**Step 3: Commit and Push**
```bash
git add .
git commit -m "hotfix(email): Fix email verification template name mismatch

Root Cause: Template name in code (template-membership-email-verification)
does not match database (member-email-verification).

Fix: Create idempotent migration to ensure template name consistency.

Issue: Email verification emails not being sent for new user registrations.
Impact: ALL new users since last deployment affected.

Tests: Added integration test for template name validation.
"

git push origin develop
```

**Step 4: Monitor Deployment**
```bash
# Watch GitHub Actions deploy-staging.yml
# Verify migration applied successfully in Azure logs

az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg \
  --tail 100 --follow true | grep -i "migration"
```

**Step 5: Test Email Verification**
- Register new test user
- Check Azure logs for email sending success
- Verify email received in test inbox

---

## 7. Risk Assessment

### Risks of the Fix

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Migration fails in production | Low | High | Test thoroughly in staging first; migration is idempotent (safe to re-run) |
| Template name update breaks other features | Low | Medium | Only updates one specific template; grep codebase for any hardcoded references |
| Email still doesn't send due to Azure config | Low | High | Verify Azure Communication Services connection in staging before production |
| Users registered during broken period remain unverified | Medium | Low | Create manual script to resend verification emails to affected users |

### Mitigation Strategies

1. **Idempotent Migration:** Migration uses `WHERE name = 'old-name'`, so it only updates if old name exists. Safe to run multiple times.

2. **Staging First:** Deploy to staging, test thoroughly, then deploy to production.

3. **Rollback Plan:** Migration includes `Down()` method to revert template name if needed.

4. **Communication Plan:** Notify affected users that verification emails are working again and provide manual resend option.

---

## 8. Verification Plan

### How to Test the Fix in Staging

**Test 1: New User Registration**
```bash
# 1. Register a new user
curl -X 'POST' \
  'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/register' \
  -H 'Content-Type: application/json' \
  -d '{
    "email": "verification-test-$(date +%s)@example.com",
    "password": "TestPass123!@#",
    "firstName": "Verification",
    "lastName": "Test",
    "selectedRole": "GeneralUser",
    "preferredMetroAreaIds": ["valid-metro-guid"],
    "agreeToTerms": true
  }'

# Expected: 201 Created with user ID

# 2. Check Azure logs for email sending
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg \
  --tail 50 | grep "MemberVerification"

# Expected log output:
# "MemberVerificationRequested COMPLETE: Email sent successfully"
```

**Test 2: Resend Verification Email**
```bash
# 1. Login as existing unverified user
# 2. Call resend verification endpoint
curl -X 'POST' \
  'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/resend-verification' \
  -H 'Authorization: Bearer {access-token}' \
  -H 'Content-Type: application/json' \
  -d '{"userId": "{user-guid}"}'

# Expected: 200 OK with success message
```

**Test 3: Database Verification**
```sql
-- Connect to Azure PostgreSQL database
-- Check template name
SELECT id, name, is_active, category, created_at, updated_at
FROM communications.email_templates
WHERE name = 'template-membership-email-verification';

-- Expected: One row with is_active = true

-- Check email messages sent
SELECT to_email, subject, status, created_at, sent_at, error_message
FROM communications.email_messages
WHERE subject LIKE '%Verify%'
ORDER BY created_at DESC
LIMIT 10;

-- Expected: Recent messages with status = 'Sent'
```

---

## 9. Post-Fix Action Items

### Immediate (After Hotfix Deployment)

- [ ] Verify email verification works for new registrations (test in staging)
- [ ] Deploy hotfix to production
- [ ] Resend verification emails to affected users (vsinharage@gmail.com and others)
- [ ] Monitor Azure logs for 24 hours to ensure no email sending errors

### Short-Term (This Week)

- [ ] Implement EmailTemplateValidator startup service
- [ ] Add integration tests for email template existence
- [ ] Create admin dashboard to view email sending metrics
- [ ] Document email template naming convention in ADR

### Long-Term (Next Sprint)

- [ ] Implement email sending metrics and alerting
- [ ] Add user-facing error handling for email failures
- [ ] Create email template management UI for admins
- [ ] Audit all email templates to ensure naming consistency

---

## 10. Lessons Learned

### What Went Wrong

1. **Migration Not Applied:** Phase 6A.76 migration (template rename) was not applied to Azure staging database, likely due to deployment pipeline issue or manual intervention.

2. **Lack of Startup Validation:** Application starts successfully even with missing email templates, causing silent failures.

3. **Fail-Silent Pattern Hides Issues:** Architectural decision to use fail-silent pattern for email sending prevented transaction rollback but also hid the issue from normal error monitoring.

4. **No Email Sending Monitoring:** No alerts or dashboard to track email sending failures in real-time.

### What Went Right

1. **Clean Architecture Separation:** Issue was contained to infrastructure layer (email service) without affecting domain or application logic.

2. **Comprehensive Logging:** Structured logging provided clear audit trail to identify the issue.

3. **Domain Event Pattern:** Registration succeeded despite email failure, preventing data loss.

### Process Improvements

1. **Mandatory Migration Verification:** After each deployment, verify all pending migrations were applied successfully.

2. **Startup Validation:** Add startup checks for critical dependencies (email templates, external services, etc.).

3. **Email Monitoring:** Implement real-time alerting for email sending failures.

4. **Integration Tests:** Add integration tests that verify database state matches application code expectations.

---

## Appendices

### Appendix A: Affected Code Files

| File Path | Role | Lines |
|-----------|------|-------|
| `src/LankaConnect.Application/Common/Constants/EmailTemplateNames.cs` | Template name constant | 47 |
| `src/LankaConnect.Application/Users/EventHandlers/MemberVerificationRequestedEventHandler.cs` | Event handler | 74 |
| `src/LankaConnect.Infrastructure/Email/Services/AzureEmailService.cs` | Email sending service | 97-103 |
| `src/LankaConnect.Infrastructure/Data/Migrations/20251228200000_Phase6A53_EnsureMemberEmailVerificationTemplate.cs` | Template creation | 37 |
| `src/LankaConnect.Infrastructure/Data/Migrations/20260123013633_Phase6A76_RenameAndAddEmailTemplates.cs` | Template rename | 62-63 |

### Appendix B: Related Database Schema

```sql
-- Email Templates Table
CREATE TABLE communications.email_templates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) UNIQUE NOT NULL,  -- CRITICAL: Must match EmailTemplateNames constants
    description TEXT,
    subject_template TEXT NOT NULL,
    text_template TEXT NOT NULL,
    html_template TEXT NOT NULL,
    type INTEGER NOT NULL,              -- EmailType enum
    category VARCHAR(100) NOT NULL,     -- EmailTemplateCategory
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Email Messages Table (Audit Trail)
CREATE TABLE communications.email_messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    to_email VARCHAR(255) NOT NULL,
    subject TEXT NOT NULL,
    status VARCHAR(50) NOT NULL,        -- Queued, Sent, Failed
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    sent_at TIMESTAMP,
    error_message TEXT
);
```

### Appendix C: Contact Information

| Role | Contact | Availability |
|------|---------|-------------|
| Database Admin | dbadmin@lankaconnect.com | 24/7 |
| Azure Support | azure-support@lankaconnect.com | Business hours |
| DevOps Engineer | devops@lankaconnect.com | 24/7 on-call |

---

**Document Version:** 1.0
**Last Updated:** 2026-01-26
**Next Review:** After hotfix deployment
**Document Owner:** Development Team Lead
