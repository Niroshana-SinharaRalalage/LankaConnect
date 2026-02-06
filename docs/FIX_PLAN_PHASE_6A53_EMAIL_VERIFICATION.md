# Fix Plan: Phase 6A.53 Member Email Verification System

**Date**: 2025-12-28
**Status**: Ready for Implementation
**Severity**: P0 - Critical Production Issues
**Estimated Timeline**: 4-6 hours
**Environment**: Azure Staging (all testing)

---

## Executive Summary

This plan addresses three critical production issues identified in Phase 6A.53:

1. **P0 - Missing UserName Parameter**: Template shows literal `{{UserName}}` in emails
2. **P0 - Inconsistent Branding**: Blue theme instead of LankaConnect gradient
3. **P0 - Broken Verification URLs**: 404 errors due to missing ApplicationUrls configuration

All fixes follow TDD methodology, reuse existing patterns, and include comprehensive verification steps.

---

## Pre-Implementation Research

### 1. Existing Email Template Patterns (Phase 6A.34)

**Brand Gradient Found**:
```html
<!-- File: src/LankaConnect.Infrastructure/Data/Migrations/20251220143225_UpdateRegistrationTemplateWithBranding_Phase6A34.cs -->
.header {
    background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%);
    color: white;
    padding: 30px 20px;
    text-align: center;
}
```

**Color Palette**:
- Maroon: `#8B1538` (0%)
- Saffron/Orange: `#FF6600` (50%)
- Green: `#2d5016` (100%)

**Footer Pattern**:
```html
<div class="footer">
    <img src="https://lankaconnectstrgaccount.blob.core.windows.net/assets/lankaconnect-logo.png"
         alt="LankaConnect" class="footer-logo">
    <p class="footer-brand">LankaConnect</p>
    <p class="footer-tagline">Sri Lankan Community Hub</p>
    <p class="footer-copyright">&copy; 2025 LankaConnect. All rights reserved.</p>
</div>
```

### 2. User Name Parameter Pattern

**User Entity** (src/LankaConnect.Domain/Users/User.cs):
```csharp
public string FirstName { get; private set; }  // Line 13
public string LastName { get; private set; }   // Line 14
public string FullName => $"{FirstName} {LastName}";  // Line 70
```

**Event Handler Pattern** (other handlers retrieve user data):
```csharp
// Handlers have access to IUserRepository
var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
var userName = user.FullName; // or construct from FirstName/LastName
```

### 3. Configuration Patterns

**Development** (appsettings.json lines 128-133):
```json
"ApplicationUrls": {
  "FrontendBaseUrl": "https://lankaconnect.com",
  "EmailVerificationPath": "/verify-email",
  "UnsubscribePath": "/unsubscribe",
  "EventDetailsPath": "/events/{eventId}"
}
```

**Staging/Production**: MISSING (must be added)

---

## Architecture Decisions

### ADR-001: Domain Event Enhancement vs Handler Database Query

**Decision**: Enhance domain event to include FirstName and LastName

**Rationale**:
- Domain events should be self-contained for event handlers
- Avoids database queries in event handlers (better performance)
- Follows existing pattern in other domain events
- Better for event sourcing/audit trail

**Alternative Rejected**:
- Query database in event handler (adds latency, coupling)

### ADR-002: Template Update Strategy

**Decision**: Create new EF Core migration to update template styling

**Rationale**:
- Templates stored in database (email_templates table)
- Migrations provide version control and rollback capability
- Follows existing pattern (Phase 6A.34 template update)
- Environment-agnostic (works in all environments)

**Alternative Rejected**:
- Manual SQL updates (not repeatable, error-prone)

### ADR-003: Configuration Deployment Strategy

**Decision**: Update appsettings.Staging.json and appsettings.Production.json directly

**Rationale**:
- ApplicationUrls values are environment-specific but not secrets
- Safe to commit to version control
- GitHub Actions deployment will pick up changes automatically
- Consistent with existing EmailSettings pattern

**Alternative Rejected**:
- Environment variables (overcomplicated for static config)

---

## Fix Implementation Plan

### Fix #1: Add UserName Parameter to Email

**Priority**: P0
**Dependencies**: None
**Estimated Time**: 90 minutes

#### Files to Change

1. **Domain Event** (src/LankaConnect.Domain/Users/DomainEvents/MemberVerificationRequestedEvent.cs)
   - **Lines to modify**: 10-29
   - **Change**: Add FirstName and LastName properties

2. **User Entity** (src/LankaConnect.Domain/Users/User.cs)
   - **Lines to modify**: Find GenerateEmailVerificationToken method (estimate ~200-250)
   - **Change**: Update to raise event with FirstName and LastName

3. **Event Handler** (src/LankaConnect.Application/Users/EventHandlers/MemberVerificationRequestedEventHandler.cs)
   - **Lines to modify**: 46-51
   - **Change**: Add UserName parameter from event data

#### Exact Code Changes

**Step 1.1: Update Domain Event**

**File**: `src/LankaConnect.Domain/Users/DomainEvents/MemberVerificationRequestedEvent.cs`

**BEFORE** (lines 10-29):
```csharp
public sealed class MemberVerificationRequestedEvent : IDomainEvent
{
    public DateTime OccurredAt { get; }
    public Guid UserId { get; }
    public string Email { get; }
    public string VerificationToken { get; }
    public DateTimeOffset RequestedAt { get; }

    public MemberVerificationRequestedEvent(
        Guid userId,
        string email,
        string verificationToken,
        DateTimeOffset requestedAt)
    {
        OccurredAt = DateTime.UtcNow;
        UserId = userId;
        Email = email;
        VerificationToken = verificationToken;
        RequestedAt = requestedAt;
    }
}
```

**AFTER**:
```csharp
public sealed class MemberVerificationRequestedEvent : IDomainEvent
{
    public DateTime OccurredAt { get; }
    public Guid UserId { get; }
    public string Email { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string VerificationToken { get; }
    public DateTimeOffset RequestedAt { get; }

    public MemberVerificationRequestedEvent(
        Guid userId,
        string email,
        string firstName,
        string lastName,
        string verificationToken,
        DateTimeOffset requestedAt)
    {
        OccurredAt = DateTime.UtcNow;
        UserId = userId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        VerificationToken = verificationToken;
        RequestedAt = requestedAt;
    }
}
```

**Step 1.2: Update User Entity (Find GenerateEmailVerificationToken method)**

**File**: `src/LankaConnect.Domain/Users/User.cs`

Search for the method that raises `MemberVerificationRequestedEvent`. Update the event instantiation to include `FirstName` and `LastName`:

**BEFORE**:
```csharp
new MemberVerificationRequestedEvent(
    Id,
    Email.Value,
    verificationToken,
    DateTime.UtcNow)
```

**AFTER**:
```csharp
new MemberVerificationRequestedEvent(
    Id,
    Email.Value,
    FirstName,
    LastName,
    verificationToken,
    DateTime.UtcNow)
```

**Step 1.3: Update Event Handler**

**File**: `src/LankaConnect.Application/Users/EventHandlers/MemberVerificationRequestedEventHandler.cs`

**BEFORE** (lines 46-51):
```csharp
var parameters = new Dictionary<string, object>
{
    { "Email", domainEvent.Email },
    { "VerificationUrl", verificationUrl },
    { "ExpirationHours", 24 }
};
```

**AFTER**:
```csharp
var userName = $"{domainEvent.FirstName} {domainEvent.LastName}";

var parameters = new Dictionary<string, object>
{
    { "UserName", userName },
    { "Email", domainEvent.Email },
    { "VerificationUrl", verificationUrl },
    { "ExpirationHours", 24 }
};
```

#### Tests to Write First (TDD)

**File**: `tests/LankaConnect.Application.Tests/Users/EventHandlers/MemberVerificationRequestedEventHandlerTests.cs` (NEW)

```csharp
using Xunit;
using Moq;
using FluentAssertions;
using LankaConnect.Application.Users.EventHandlers;
using LankaConnect.Domain.Users.DomainEvents;
using LankaConnect.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Tests.Users.EventHandlers;

public class MemberVerificationRequestedEventHandlerTests
{
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<MemberVerificationRequestedEventHandler>> _loggerMock;
    private readonly Mock<IApplicationUrlsService> _urlsServiceMock;
    private readonly MemberVerificationRequestedEventHandler _handler;

    public MemberVerificationRequestedEventHandlerTests()
    {
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<MemberVerificationRequestedEventHandler>>();
        _urlsServiceMock = new Mock<IApplicationUrlsService>();
        _handler = new MemberVerificationRequestedEventHandler(
            _emailServiceMock.Object,
            _loggerMock.Object,
            _urlsServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldIncludeUserNameParameter()
    {
        // Arrange
        var domainEvent = new MemberVerificationRequestedEvent(
            userId: Guid.NewGuid(),
            email: "test@example.com",
            firstName: "John",
            lastName: "Doe",
            verificationToken: "token123",
            requestedAt: DateTimeOffset.UtcNow);

        var notification = new DomainEventNotification<MemberVerificationRequestedEvent>(domainEvent);

        _urlsServiceMock
            .Setup(x => x.GetEmailVerificationUrl("token123"))
            .Returns("https://lankaconnect.com/verify-email?token=token123");

        _emailServiceMock
            .Setup(x => x.SendTemplatedEmailAsync(
                "member-email-verification",
                "test@example.com",
                It.Is<Dictionary<string, object>>(p =>
                    p.ContainsKey("UserName") &&
                    p["UserName"].ToString() == "John Doe"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _emailServiceMock.Verify(
            x => x.SendTemplatedEmailAsync(
                "member-email-verification",
                "test@example.com",
                It.Is<Dictionary<string, object>>(p =>
                    p.ContainsKey("UserName") &&
                    p["UserName"].ToString() == "John Doe" &&
                    p.ContainsKey("VerificationUrl") &&
                    p.ContainsKey("ExpirationHours")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldConstructFullNameFromFirstAndLastName()
    {
        // Arrange
        var domainEvent = new MemberVerificationRequestedEvent(
            userId: Guid.NewGuid(),
            email: "test@example.com",
            firstName: "Jane",
            lastName: "Smith",
            verificationToken: "token456",
            requestedAt: DateTimeOffset.UtcNow);

        var notification = new DomainEventNotification<MemberVerificationRequestedEvent>(domainEvent);

        _urlsServiceMock
            .Setup(x => x.GetEmailVerificationUrl(It.IsAny<string>()))
            .Returns("https://lankaconnect.com/verify-email?token=token456");

        _emailServiceMock
            .Setup(x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _emailServiceMock.Verify(
            x => x.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<Dictionary<string, object>>(p =>
                    p["UserName"].ToString() == "Jane Smith"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

#### Verification Steps

1. **Unit Test Verification**:
   ```bash
   cd c:\Work\LankaConnect
   dotnet test tests/LankaConnect.Application.Tests/Users/EventHandlers/MemberVerificationRequestedEventHandlerTests.cs
   ```
   - ✅ All tests pass

2. **Build Verification**:
   ```bash
   dotnet build
   ```
   - ✅ 0 errors, 0 warnings

3. **API Test** (Azure Staging):
   ```bash
   # POST to /api/auth/register endpoint
   curl -X POST https://lankaconnect-staging.azurewebsites.net/api/auth/register \
     -H "Content-Type: application/json" \
     -d '{
       "email": "test_fix1@example.com",
       "password": "Test123!@#",
       "firstName": "Test",
       "lastName": "User",
       "selectedRole": "GeneralUser"
     }'
   ```
   - ✅ 200 OK response
   - ✅ Check Azure logs for: `[Phase 6A.53] Sending member-email-verification to test_fix1@example.com`

4. **Email Verification** (Check Azure Communication Services logs):
   ```sql
   -- Query Azure Communication Services email logs
   -- Verify email content shows "Hi Test User," not "Hi {{UserName}},"
   ```
   - ✅ Email received with "Hi Test User,"

5. **Database Verification** (Azure SQL):
   ```sql
   SELECT "Id", "Email", "FirstName", "LastName", "EmailVerificationToken"
   FROM users."Users"
   WHERE "Email" = 'test_fix1@example.com';
   ```
   - ✅ User record exists with verification token

#### Rollback Plan

If tests fail or issues occur:

1. **Revert code changes**:
   ```bash
   git checkout src/LankaConnect.Domain/Users/DomainEvents/MemberVerificationRequestedEvent.cs
   git checkout src/LankaConnect.Domain/Users/User.cs
   git checkout src/LankaConnect.Application/Users/EventHandlers/MemberVerificationRequestedEventHandler.cs
   ```

2. **Remove test file**:
   ```bash
   git clean -fd tests/LankaConnect.Application.Tests/Users/EventHandlers/
   ```

3. **Verify build**:
   ```bash
   dotnet build
   ```

---

### Fix #2: Update Template Branding

**Priority**: P0
**Dependencies**: None (can be done in parallel with Fix #1)
**Estimated Time**: 60 minutes

#### Files to Change

1. **New Migration File** (create): `src/LankaConnect.Infrastructure/Data/Migrations/[TIMESTAMP]_UpdateMemberEmailVerificationTemplateBranding_Phase6A53.cs`

#### Exact Code Changes

**Step 2.1: Create Migration**

**File**: `src/LankaConnect.Infrastructure/Data/Migrations/20251228210000_UpdateMemberEmailVerificationTemplateBranding_Phase6A53.cs` (NEW)

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 6A.53 Fix: Update member-email-verification template with LankaConnect branding
    /// - Gradient header: Maroon (#8B1538) → Saffron (#FF6600) → Green (#2d5016)
    /// - Branded footer with logo and tagline
    /// - Consistent with Phase 6A.34 branding standards
    /// </summary>
    public partial class UpdateMemberEmailVerificationTemplateBranding_Phase6A53 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET
                    ""html_template"" = '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f5f5f5; }
        .container { max-width: 600px; margin: 0 auto; background: white; }
        .header { background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); color: white; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 28px; font-weight: bold; }
        .content { padding: 30px 20px; background: #ffffff; }
        .verify-button { display: inline-block; background: linear-gradient(135deg, #8B1538 0%, #FF6600 100%); color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; margin: 20px 0; font-weight: bold; box-shadow: 0 4px 6px rgba(139, 21, 56, 0.3); }
        .verify-button:hover { box-shadow: 0 6px 8px rgba(139, 21, 56, 0.4); }
        .info-box { background: #fff8f5; padding: 20px; margin: 20px 0; border-left: 4px solid #FF6600; border-radius: 0 8px 8px 0; }
        .footer { background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center; }
        .footer-logo { width: 60px; height: 60px; margin-bottom: 10px; }
        .footer-brand { color: white; font-size: 18px; font-weight: bold; margin: 5px 0; }
        .footer-tagline { color: rgba(255,255,255,0.9); font-size: 12px; margin: 5px 0; }
        .footer-copyright { color: rgba(255,255,255,0.8); font-size: 11px; margin-top: 15px; }
        .highlight { color: #FF6600; font-weight: bold; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Welcome to LankaConnect!</h1>
        </div>
        <div class=""content"">
            <p>Hi <span class=""highlight"">{{UserName}}</span>,</p>
            <p>Thank you for signing up! To complete your registration, please verify your email address:</p>
            <p style=""text-align: center;"">
                <a href=""{{VerificationUrl}}"" class=""verify-button"">Verify Email Address</a>
            </p>
            <div class=""info-box"">
                <p style=""margin: 0; color: #666; font-size: 14px;"">
                    <strong>⏱️ This link will expire in {{ExpirationHours}} hours.</strong>
                </p>
                <p style=""margin: 8px 0 0 0; color: #666; font-size: 14px;"">
                    If you didn''t create this account, please ignore this email.
                </p>
            </div>
            <p>We look forward to connecting you with the Sri Lankan community!</p>
        </div>
        <div class=""footer"">
            <img src=""https://lankaconnectstrgaccount.blob.core.windows.net/assets/lankaconnect-logo.png"" alt=""LankaConnect"" class=""footer-logo"">
            <p class=""footer-brand"">LankaConnect</p>
            <p class=""footer-tagline"">Sri Lankan Community Hub</p>
            <p class=""footer-copyright"">&copy; 2025 LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    ""text_template"" = 'Hi {{UserName}},

Welcome to LankaConnect!

To complete your registration, please verify your email address by clicking the link below:

{{VerificationUrl}}

This link will expire in {{ExpirationHours}} hours.

If you didn''t create this account, please ignore this email.

We look forward to connecting you with the Sri Lankan community!

---
LankaConnect - Sri Lankan Community Hub
© 2025 LankaConnect. All rights reserved.',
                    ""updated_at"" = NOW()
                WHERE ""name"" = 'member-email-verification';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to original blue theme
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET
                    ""html_template"" = '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #2563eb; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }
        .content { padding: 20px; background: #f9fafb; }
        .verify-button { display: inline-block; background: #2563eb; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; margin: 20px 0; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Welcome to LankaConnect!</h1>
        </div>
        <div class=""content"">
            <p>Hi {{UserName}},</p>
            <p>Thank you for signing up! To complete your registration, please verify your email address:</p>
            <p style=""text-align: center;"">
                <a href=""{{VerificationUrl}}"" class=""verify-button"">Verify Email Address</a>
            </p>
            <p style=""color: #666; font-size: 14px;"">This link will expire in {{ExpirationHours}} hours.</p>
            <p style=""color: #666; font-size: 14px;"">If you didn''t create this account, please ignore this email.</p>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    ""text_template"" = 'Hi {{UserName}},

Welcome to LankaConnect!

To complete your registration, please verify your email address by clicking the link below:

{{VerificationUrl}}

This link will expire in {{ExpirationHours}} hours.

If you didn''t create this account, please ignore this email.

© 2025 LankaConnect
Questions? Reply to this email or visit our support page.',
                    ""updated_at"" = NOW()
                WHERE ""name"" = 'member-email-verification';
            ");
        }
    }
}
```

#### Tests to Write First (TDD)

**File**: `tests/LankaConnect.IntegrationTests/Migrations/EmailTemplateBrandingTests.cs` (NEW)

```csharp
using Xunit;
using FluentAssertions;
using LankaConnect.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.IntegrationTests.Migrations;

public class EmailTemplateBrandingTests : DatabaseIntegrationTestBase
{
    [Fact]
    public async Task MemberEmailVerificationTemplate_ShouldHaveBrandGradient()
    {
        // Arrange & Act
        var template = await DbContext.EmailTemplates
            .FirstOrDefaultAsync(t => t.Name == "member-email-verification");

        // Assert
        template.Should().NotBeNull();
        template!.HtmlTemplate.Should().Contain("linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%)");
        template.HtmlTemplate.Should().NotContain("#2563eb"); // Should NOT have blue theme
    }

    [Fact]
    public async Task MemberEmailVerificationTemplate_ShouldHaveBrandedFooter()
    {
        // Arrange & Act
        var template = await DbContext.EmailTemplates
            .FirstOrDefaultAsync(t => t.Name == "member-email-verification");

        // Assert
        template.Should().NotBeNull();
        template!.HtmlTemplate.Should().Contain("lankaconnect-logo.png");
        template.HtmlTemplate.Should().Contain("LankaConnect");
        template.HtmlTemplate.Should().Contain("Sri Lankan Community Hub");
        template.HtmlTemplate.Should().Contain("© 2025 LankaConnect");
    }

    [Fact]
    public async Task MemberEmailVerificationTemplate_ShouldMatchRegistrationTemplateBranding()
    {
        // Arrange
        var verificationTemplate = await DbContext.EmailTemplates
            .FirstOrDefaultAsync(t => t.Name == "member-email-verification");

        var registrationTemplate = await DbContext.EmailTemplates
            .FirstOrDefaultAsync(t => t.Name == "registration-confirmation");

        // Assert - Both should use same gradient
        verificationTemplate.Should().NotBeNull();
        registrationTemplate.Should().NotBeNull();

        var gradient = "linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%)";
        verificationTemplate!.HtmlTemplate.Should().Contain(gradient);
        registrationTemplate!.HtmlTemplate.Should().Contain(gradient);
    }
}
```

#### Verification Steps

1. **Create Migration**:
   ```bash
   cd c:\Work\LankaConnect\src\LankaConnect.Infrastructure
   dotnet ef migrations add UpdateMemberEmailVerificationTemplateBranding_Phase6A53 --context LankaConnectDbContext --output-dir Data/Migrations
   ```
   - ✅ Migration file created

2. **Build Verification**:
   ```bash
   cd c:\Work\LankaConnect
   dotnet build
   ```
   - ✅ 0 errors

3. **Integration Test**:
   ```bash
   dotnet test tests/LankaConnect.IntegrationTests/Migrations/EmailTemplateBrandingTests.cs
   ```
   - ✅ All tests pass

4. **Manual Database Check** (Azure Staging after deployment):
   ```sql
   SELECT "name", "html_template"
   FROM communications.email_templates
   WHERE "name" = 'member-email-verification';
   ```
   - ✅ Verify gradient colors: #8B1538, #FF6600, #2d5016
   - ✅ Verify footer logo URL present
   - ✅ No blue color (#2563eb)

5. **Email Visual Verification** (Send test email):
   ```bash
   # Register new test user
   curl -X POST https://lankaconnect-staging.azurewebsites.net/api/auth/register \
     -H "Content-Type: application/json" \
     -d '{
       "email": "test_fix2@example.com",
       "password": "Test123!@#",
       "firstName": "Branding",
       "lastName": "Test",
       "selectedRole": "GeneralUser"
     }'
   ```
   - ✅ Receive email with gradient header
   - ✅ Verify footer with logo and tagline
   - ✅ Compare visually with event registration email

#### Rollback Plan

1. **Revert migration**:
   ```bash
   cd c:\Work\LankaConnect\src\LankaConnect.Infrastructure
   dotnet ef migrations remove --context LankaConnectDbContext
   ```

2. **Or rollback in database** (if already applied):
   ```bash
   dotnet ef database update 20251228200000_Phase6A53_EnsureMemberEmailVerificationTemplate --context LankaConnectDbContext
   ```

---

### Fix #3: Add ApplicationUrls Configuration

**Priority**: P0
**Dependencies**: None (can be done in parallel)
**Estimated Time**: 30 minutes

#### Files to Change

1. **Staging Config**: `src/LankaConnect.API/appsettings.Staging.json`
2. **Production Config**: `src/LankaConnect.API/appsettings.Production.json`

#### Exact Code Changes

**Step 3.1: Update Staging Configuration**

**File**: `src/LankaConnect.API/appsettings.Staging.json`

**Add AFTER line 79** (after EmailSettings closing brace):
```json
  },
  "ApplicationUrls": {
    "FrontendBaseUrl": "https://lankaconnect-staging.vercel.app",
    "EmailVerificationPath": "/verify-email",
    "UnsubscribePath": "/unsubscribe",
    "EventDetailsPath": "/events/{eventId}"
  },
  "AllowedHosts": "*",
```

**Step 3.2: Update Production Configuration**

**File**: `src/LankaConnect.API/appsettings.Production.json`

**Add AFTER line 72** (after EmailSettings closing brace):
```json
  },
  "ApplicationUrls": {
    "FrontendBaseUrl": "https://lankaconnect.com",
    "EmailVerificationPath": "/verify-email",
    "UnsubscribePath": "/unsubscribe",
    "EventDetailsPath": "/events/{eventId}"
  },
  "AllowedHosts": "*",
```

#### Environment URLs Determination

**Staging Frontend**: `https://lankaconnect-staging.vercel.app`
- Based on typical Vercel staging deployment pattern
- **VERIFY**: Check with team for actual staging frontend URL

**Production Frontend**: `https://lankaconnect.com`
- Already configured in development appsettings.json
- Production domain

#### Tests to Write First (TDD)

**File**: `tests/LankaConnect.IntegrationTests/Configuration/ApplicationUrlsConfigurationTests.cs` (NEW)

```csharp
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using LankaConnect.Infrastructure.Email.Configuration;

namespace LankaConnect.IntegrationTests.Configuration;

public class ApplicationUrlsConfigurationTests
{
    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void ApplicationUrls_ShouldBeConfiguredInAllEnvironments(string environment)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: false)
            .Build();

        var services = new ServiceCollection();
        services.Configure<ApplicationUrlsOptions>(configuration.GetSection("ApplicationUrls"));
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<ApplicationUrlsOptions>>().Value;

        // Assert
        options.Should().NotBeNull();
        options.FrontendBaseUrl.Should().NotBeNullOrWhiteSpace();
        options.EmailVerificationPath.Should().NotBeNullOrWhiteSpace();
        options.UnsubscribePath.Should().NotBeNullOrWhiteSpace();
        options.EventDetailsPath.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ApplicationUrls_Staging_ShouldPointToStagingFrontend()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Staging.json", optional: false)
            .Build();

        var services = new ServiceCollection();
        services.Configure<ApplicationUrlsOptions>(configuration.GetSection("ApplicationUrls"));
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<ApplicationUrlsOptions>>().Value;

        // Assert
        options.FrontendBaseUrl.Should().Contain("staging");
        options.FrontendBaseUrl.Should().StartWith("https://");
    }

    [Fact]
    public void ApplicationUrls_Production_ShouldPointToProductionDomain()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Production.json", optional: false)
            .Build();

        var services = new ServiceCollection();
        services.Configure<ApplicationUrlsOptions>(configuration.GetSection("ApplicationUrls"));
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<ApplicationUrlsOptions>>().Value;

        // Assert
        options.FrontendBaseUrl.Should().Be("https://lankaconnect.com");
        options.EmailVerificationPath.Should().Be("/verify-email");
    }
}
```

#### Verification Steps

1. **Configuration Test**:
   ```bash
   dotnet test tests/LankaConnect.IntegrationTests/Configuration/ApplicationUrlsConfigurationTests.cs
   ```
   - ✅ All tests pass

2. **Build Verification**:
   ```bash
   dotnet build --configuration Staging
   dotnet build --configuration Production
   ```
   - ✅ 0 errors in both configurations

3. **API Startup Test** (Azure Staging after deployment):
   ```bash
   # Check Azure App Service logs
   # Look for: "ApplicationUrls configured: FrontendBaseUrl = https://lankaconnect-staging.vercel.app"
   ```
   - ✅ No InvalidOperationException on startup
   - ✅ Configuration loaded successfully

4. **URL Generation Test** (API call):
   ```bash
   # Register test user
   curl -X POST https://lankaconnect-staging.azurewebsites.net/api/auth/register \
     -H "Content-Type: application/json" \
     -d '{
       "email": "test_fix3@example.com",
       "password": "Test123!@#",
       "firstName": "URL",
       "lastName": "Test",
       "selectedRole": "GeneralUser"
     }'

   # Check Azure logs for verification URL
   # Expected: https://lankaconnect-staging.vercel.app/verify-email?token=...
   ```
   - ✅ URL generated with correct domain
   - ✅ No InvalidOperationException

5. **End-to-End Verification** (Click link in email):
   - ✅ Opens frontend at `/verify-email?token=...`
   - ✅ NO 404 error
   - ✅ Token validation page loads

#### Rollback Plan

1. **Revert configuration files**:
   ```bash
   git checkout src/LankaConnect.API/appsettings.Staging.json
   git checkout src/LankaConnect.API/appsettings.Production.json
   ```

2. **Redeploy previous version**:
   ```bash
   # GitHub Actions will automatically deploy previous commit
   git revert HEAD
   git push origin develop
   ```

---

## Combined Testing Strategy

### Unit Tests

**Order of Execution**:
1. MemberVerificationRequestedEventHandlerTests (Fix #1)
2. EmailTemplateBrandingTests (Fix #2)
3. ApplicationUrlsConfigurationTests (Fix #3)

**Command**:
```bash
cd c:\Work\LankaConnect
dotnet test --filter "FullyQualifiedName~MemberVerificationRequestedEventHandlerTests|FullyQualifiedName~EmailTemplateBrandingTests|FullyQualifiedName~ApplicationUrlsConfigurationTests"
```

**Expected Result**: All tests pass (0 failures)

### Integration Tests

**Database Setup** (Azure Staging):
```sql
-- Verify migrations applied
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%Phase6A53%'
ORDER BY "MigrationId" DESC;

-- Expected:
-- 20251228210000_UpdateMemberEmailVerificationTemplateBranding_Phase6A53
-- 20251228200000_Phase6A53_EnsureMemberEmailVerificationTemplate
```

**API Endpoint Tests**:

1. **Registration Flow**:
   ```bash
   # Test registration with new fixes
   curl -X POST https://lankaconnect-staging.azurewebsites.net/api/auth/register \
     -H "Content-Type: application/json" \
     -d '{
       "email": "integration_test@example.com",
       "password": "Test123!@#",
       "firstName": "Integration",
       "lastName": "Test",
       "selectedRole": "GeneralUser"
     }'
   ```

2. **Resend Verification**:
   ```bash
   # Login first to get token
   curl -X POST https://lankaconnect-staging.azurewebsites.net/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{
       "email": "integration_test@example.com",
       "password": "Test123!@#"
     }'

   # Resend verification email
   curl -X POST https://lankaconnect-staging.azurewebsites.net/api/communications/send-email-verification \
     -H "Authorization: Bearer {token}" \
     -H "Content-Type: application/json"
   ```

### Manual Testing Checklist

- [ ] Register new user in staging
- [ ] Receive verification email within 2 minutes
- [ ] Email displays "Hi [FirstName] [LastName]," (no literal {{UserName}})
- [ ] Email has gradient header (maroon → orange → green)
- [ ] Email has footer with logo and "Sri Lankan Community Hub"
- [ ] Click "Verify Email Address" button
- [ ] Redirects to staging frontend `/verify-email` page
- [ ] NO 404 error
- [ ] Token validation succeeds
- [ ] User's IsEmailVerified flag set to true in database
- [ ] Azure logs show no errors

### Smoke Tests After Deployment

**GitHub Actions Verification**:
```yaml
# Verify deployment workflow succeeds
# Check: .github/workflows/azure-webapps-dotnet-core.yml
```

**Post-Deployment Checks**:
```bash
# 1. Health check
curl https://lankaconnect-staging.azurewebsites.net/health

# 2. Swagger UI accessible
open https://lankaconnect-staging.azurewebsites.net/swagger

# 3. Database migrations applied
# Check Azure SQL via Azure Portal Query Editor
```

---

## Deployment Sequence

### Step 1: Code Changes

**Order**:
1. Create test files first (TDD)
2. Update domain event (Fix #1)
3. Update event handler (Fix #1)
4. Create branding migration (Fix #2)
5. Update configuration files (Fix #3)

**Git Commits**:
```bash
# Commit 1: Tests
git add tests/
git commit -m "test(phase-6a53): Add tests for email verification fixes"

# Commit 2: Domain changes
git add src/LankaConnect.Domain/
git commit -m "fix(phase-6a53): Add FirstName/LastName to MemberVerificationRequestedEvent"

# Commit 3: Handler changes
git add src/LankaConnect.Application/
git commit -m "fix(phase-6a53): Include UserName parameter in verification email"

# Commit 4: Migration
git add src/LankaConnect.Infrastructure/Data/Migrations/
git commit -m "fix(phase-6a53): Update email template with LankaConnect branding"

# Commit 5: Configuration
git add src/LankaConnect.API/appsettings.*.json
git commit -m "fix(phase-6a53): Add ApplicationUrls config for staging and production"

# Push all together
git push origin develop
```

### Step 2: GitHub Actions Workflow

**Automatic**:
- GitHub Actions detects push to `develop`
- Builds solution
- Runs all tests
- Deploys to Azure App Service (Staging)
- Applies EF Core migrations automatically

**Verify Workflow**:
```bash
# Check GitHub Actions status
# https://github.com/[your-org]/LankaConnect/actions
```

### Step 3: Post-Deployment Verification

**Azure SQL Migration Check**:
```sql
SELECT TOP 3 "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId" DESC;

-- Should include: 20251228210000_UpdateMemberEmailVerificationTemplateBranding_Phase6A53
```

**Template Verification**:
```sql
SELECT "name",
       CASE
           WHEN "html_template" LIKE '%#8B1538%' THEN 'Branded ✅'
           WHEN "html_template" LIKE '%#2563eb%' THEN 'Old Blue ❌'
           ELSE 'Unknown'
       END AS BrandingStatus
FROM communications.email_templates
WHERE "name" = 'member-email-verification';
```

**Configuration Check** (Azure App Service logs):
```bash
# Look for startup log:
# "ApplicationUrlsOptions configured: FrontendBaseUrl = https://lankaconnect-staging.vercel.app"
```

### Step 4: End-to-End Test

1. Register new user through staging UI
2. Check email (Azure Communication Services)
3. Verify email formatting
4. Click verification link
5. Confirm user verified in database

---

## Risk Assessment

### What Could Break

**Risk #1: Existing Users with Unverified Emails**
- **Impact**: Existing verification tokens might fail if event structure changed
- **Mitigation**: Domain event is backward compatible (added fields, didn't remove)
- **Probability**: Low

**Risk #2: Template Migration Fails**
- **Impact**: Template update doesn't apply, emails still blue
- **Mitigation**: Migration has rollback (Down method)
- **Probability**: Low (tested in integration tests)

**Risk #3: Wrong Frontend URL**
- **Impact**: Verification links point to wrong domain (404)
- **Mitigation**: Configuration tests validate URLs, easy to update
- **Probability**: Medium (if staging URL unknown)

**Risk #4: Breaking Event Handler Contract**
- **Impact**: Email sending fails if other code depends on old event structure
- **Mitigation**: Search codebase for all references to MemberVerificationRequestedEvent
- **Probability**: Very Low (event is new in Phase 6A.53)

### Mitigation Strategies

1. **Deploy During Low-Traffic Window**: Minimize user impact
2. **Monitor Azure Logs**: Watch for exceptions in real-time
3. **Test in Staging First**: Full end-to-end test before production
4. **Gradual Rollout**: Deploy to staging, wait 24 hours, then production
5. **Feature Flag**: Consider adding email template version flag if needed

### Rollback Procedures

**Fast Rollback (Within 15 Minutes)**:
```bash
# Revert all commits
git revert HEAD~5..HEAD
git push origin develop

# GitHub Actions auto-deploys previous version
```

**Database Rollback**:
```bash
# If migration causes issues
cd src/LankaConnect.Infrastructure
dotnet ef database update 20251228200000_Phase6A53_EnsureMemberEmailVerificationTemplate \
  --context LankaConnectDbContext \
  --connection "[Azure SQL Connection String]"
```

**Configuration-Only Rollback**:
```bash
# If just URL is wrong
# Edit appsettings.Staging.json directly in Azure Portal
# Update ApplicationUrls:FrontendBaseUrl
# Restart App Service
```

---

## Documentation Updates

### Files to Update After Completion

1. **PROGRESS_TRACKER.md**:
   ```markdown
   ### Phase 6A.53 - Member Email Verification System (FIXES APPLIED)

   **Status**: ✅ Complete
   **Date Completed**: 2025-12-28

   **Issues Fixed**:
   - ✅ UserName parameter now populated in verification emails
   - ✅ Email template updated with LankaConnect branding
   - ✅ ApplicationUrls configuration added to all environments

   **Files Changed**:
   - Domain: MemberVerificationRequestedEvent (added FirstName/LastName)
   - Application: MemberVerificationRequestedEventHandler (added UserName parameter)
   - Infrastructure: New migration for template branding
   - Configuration: appsettings.Staging.json, appsettings.Production.json

   **See**: FIX_PLAN_PHASE_6A53_EMAIL_VERIFICATION.md
   ```

2. **STREAMLINED_ACTION_PLAN.md**:
   ```markdown
   ## Phase 6A.53 Fixes (2025-12-28)

   **Completed**:
   - [x] Add UserName parameter to verification email template
   - [x] Update template styling with brand gradient
   - [x] Configure ApplicationUrls for staging and production
   - [x] Write comprehensive tests for all fixes
   - [x] Deploy and verify in staging environment

   **Results**: All three critical issues resolved, email verification fully functional.
   ```

3. **TASK_SYNCHRONIZATION_STRATEGY.md**:
   ```markdown
   ### Phase 6A.53 Status Update

   - **Template Parameter Issue**: RESOLVED (UserName parameter added)
   - **Branding Inconsistency**: RESOLVED (Brand gradient applied)
   - **URL Configuration**: RESOLVED (All environments configured)
   - **Production Status**: FUNCTIONAL
   ```

4. **Create Phase Summary**: `docs/PHASE_6A53_FIX_SUMMARY.md`
   ```markdown
   # Phase 6A.53 Fix Summary

   ## Issues Resolved

   ### Issue #1: Missing UserName Parameter
   - Added FirstName and LastName to MemberVerificationRequestedEvent
   - Event handler now constructs UserName from event data
   - Tests ensure parameter is always provided

   ### Issue #2: Template Branding
   - Created migration to update HTML template
   - Applied LankaConnect gradient (maroon → orange → green)
   - Added branded footer with logo and tagline
   - Matches Phase 6A.34 branding standards

   ### Issue #3: Configuration URLs
   - Added ApplicationUrls to appsettings.Staging.json
   - Added ApplicationUrls to appsettings.Production.json
   - Verification links now point to correct frontend domains

   ## Testing
   - Unit tests: 100% pass rate
   - Integration tests: All environments verified
   - End-to-end test: Complete registration flow functional

   ## Deployment
   - GitHub Actions: Successful deployment to staging
   - Database migrations: Applied successfully
   - Zero downtime deployment

   ## Lessons Learned
   - Always include all required data in domain events
   - Establish and enforce template branding standards
   - Validate configuration across all environments before deployment
   - End-to-end testing in staging is essential
   ```

---

## Prevention Recommendations

### 1. Template Parameter Validation

**Action**: Create automated validation for email templates

**Implementation**:
```csharp
// src/LankaConnect.Infrastructure/Email/Services/EmailTemplateValidator.cs
public class EmailTemplateValidator
{
    public Result ValidateTemplateParameters(
        string templateContent,
        Dictionary<string, object> parameters)
    {
        var requiredPlaceholders = ExtractPlaceholders(templateContent);
        var missingParameters = requiredPlaceholders
            .Where(p => !parameters.ContainsKey(p))
            .ToList();

        if (missingParameters.Any())
        {
            return Result.Failure(
                $"Missing required parameters: {string.Join(", ", missingParameters)}");
        }

        return Result.Success();
    }
}
```

### 2. Email Template Design System

**Action**: Create reusable email components

**Files to Create**:
- `src/LankaConnect.Infrastructure/Templates/Email/Components/header.html`
- `src/LankaConnect.Infrastructure/Templates/Email/Components/footer.html`
- `src/LankaConnect.Infrastructure/Templates/Email/Components/styles.css`

**Benefits**:
- Consistent branding across all emails
- Single place to update brand colors
- Easier template maintenance

### 3. Configuration Validation on Startup

**Action**: Add configuration validation

**Implementation**:
```csharp
// src/LankaConnect.API/Program.cs
public static void ValidateConfiguration(IConfiguration configuration)
{
    var requiredSettings = new[]
    {
        "ApplicationUrls:FrontendBaseUrl",
        "ApplicationUrls:EmailVerificationPath",
        "EmailSettings:AzureConnectionString"
    };

    foreach (var setting in requiredSettings)
    {
        if (string.IsNullOrWhiteSpace(configuration[setting]))
        {
            throw new InvalidOperationException(
                $"Required configuration '{setting}' is missing or empty");
        }
    }
}
```

### 4. Environment-Specific Integration Tests

**Action**: Add CI/CD tests for each environment

**GitHub Actions Workflow**:
```yaml
- name: Test Staging Configuration
  run: dotnet test --filter "Category=ConfigurationTest&Environment=Staging"

- name: Test Production Configuration
  run: dotnet test --filter "Category=ConfigurationTest&Environment=Production"
```

### 5. Code Review Checklist for Email Features

**Add to PR Template**:
- [ ] All template placeholders have corresponding parameters in event handler
- [ ] Email template uses LankaConnect branding (gradient header, branded footer)
- [ ] ApplicationUrls configuration updated in all environment files
- [ ] End-to-end test in staging environment passed
- [ ] Email visual preview reviewed (screenshot attached)

---

## Appendix: Reference Links

### Internal Documentation
- RCA Document: `docs/RCA_PHASE_6A53_EMAIL_VERIFICATION_ISSUES.md`
- Phase Summary: `docs/PHASE_6A53_MEMBER_EMAIL_VERIFICATION_SUMMARY.md`
- Email System Plan: `docs/EMAIL_SYSTEM_IMPLEMENTATION_PLAN_FINAL.md`
- Branding Reference: Phase 6A.34 Migration

### Code References
- Domain Event: `src/LankaConnect.Domain/Users/DomainEvents/MemberVerificationRequestedEvent.cs`
- Event Handler: `src/LankaConnect.Application/Users/EventHandlers/MemberVerificationRequestedEventHandler.cs`
- URL Service: `src/LankaConnect.Infrastructure/Email/Services/ApplicationUrlsService.cs`
- Template Migration: `src/LankaConnect.Infrastructure/Data/Migrations/20251228200000_Phase6A53_EnsureMemberEmailVerificationTemplate.cs`

### Azure Resources
- Staging App Service: `lankaconnect-staging.azurewebsites.net`
- Production App Service: `lankaconnect.azurewebsites.net`
- Azure SQL Database: Check Azure Portal for connection details
- Azure Communication Services: Email delivery logs

### GitHub
- Repository: Check GitHub for actual repo URL
- Actions Workflow: `.github/workflows/azure-webapps-dotnet-core.yml`
- Pull Request Template: `.github/pull_request_template.md`

---

## Timeline and Milestones

### Hour 0-1: Test Creation (TDD)
- ✅ Create MemberVerificationRequestedEventHandlerTests
- ✅ Create EmailTemplateBrandingTests
- ✅ Create ApplicationUrlsConfigurationTests
- ✅ All tests RED (expected to fail)

### Hour 1-2: Fix #1 Implementation
- ✅ Update MemberVerificationRequestedEvent
- ✅ Update User.GenerateEmailVerificationToken
- ✅ Update MemberVerificationRequestedEventHandler
- ✅ All Fix #1 tests GREEN

### Hour 2-3: Fix #2 Implementation
- ✅ Create branding migration
- ✅ Build and verify migration
- ✅ All Fix #2 tests GREEN

### Hour 3-3.5: Fix #3 Implementation
- ✅ Update appsettings.Staging.json
- ✅ Update appsettings.Production.json
- ✅ All Fix #3 tests GREEN

### Hour 3.5-4: Integration
- ✅ Run full test suite (all tests GREEN)
- ✅ Build in all configurations (0 errors)
- ✅ Commit all changes
- ✅ Push to develop branch

### Hour 4-5: Deployment and Verification
- ✅ GitHub Actions deploys to staging
- ✅ Database migrations applied
- ✅ End-to-end test in staging (register → email → verify)
- ✅ All smoke tests pass

### Hour 5-6: Documentation and Wrap-Up
- ✅ Update PROGRESS_TRACKER.md
- ✅ Update STREAMLINED_ACTION_PLAN.md
- ✅ Create PHASE_6A53_FIX_SUMMARY.md
- ✅ Update PHASE_6A_MASTER_INDEX.md
- ✅ Production deployment (if staging successful)

---

## Success Criteria

**Fix #1 Complete When**:
- ✅ Unit tests pass (MemberVerificationRequestedEventHandlerTests)
- ✅ Verification email shows "Hi [FirstName] [LastName]," not "Hi {{UserName}},"
- ✅ No template parameter errors in logs

**Fix #2 Complete When**:
- ✅ Integration tests pass (EmailTemplateBrandingTests)
- ✅ Email header has gradient: maroon → orange → green
- ✅ Email footer has logo and "Sri Lankan Community Hub"
- ✅ Visual consistency with event registration emails

**Fix #3 Complete When**:
- ✅ Configuration tests pass (ApplicationUrlsConfigurationTests)
- ✅ Verification link opens frontend (no 404)
- ✅ Staging URL: `https://lankaconnect-staging.vercel.app/verify-email?token=...`
- ✅ Production URL: `https://lankaconnect.com/verify-email?token=...`

**Overall Success When**:
- ✅ All tests pass (unit + integration + end-to-end)
- ✅ Build has 0 errors, 0 warnings
- ✅ Staging deployment successful
- ✅ End-to-end registration flow works (register → email → verify → confirmed)
- ✅ Azure logs show no errors
- ✅ Documentation updated
- ✅ Production deployment successful (after 24hr staging soak)

---

**Document Status**: Ready for Implementation
**Next Action**: Begin TDD test creation (Hour 0)
**Approval Required**: Yes (from senior engineer before production deployment)
**Estimated Completion**: 2025-12-28 EOD (if started immediately)
