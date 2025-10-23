# Email & Notifications System - Visual Architecture Guide

## Quick Reference Diagrams

### 1. Clean Architecture Layers Overview

```
┌────────────────────────────────────────────────────────────────────────┐
│                          API LAYER (Presentation)                      │
│  Controllers: EmailController, AuthController (email verification)    │
│  - Receives HTTP requests                                              │
│  - Returns HTTP responses                                              │
│  - Delegates to Application via MediatR                                │
└────────────────────────────────────────────────────────────────────────┘
                                    ↓ depends on
┌────────────────────────────────────────────────────────────────────────┐
│                         APPLICATION LAYER (Use Cases)                  │
│  Commands: SendEmailVerification, SendPasswordReset,                   │
│           SendTransactionalEmail, ProcessEmailQueue                    │
│  Queries: GetEmailHistory, GetEmailStatus, SearchEmails                │
│  Interfaces: IEmailService, IEmailTemplateService                      │
│  - Orchestrates domain logic                                           │
│  - Defines infrastructure contracts                                    │
│  - Uses MediatR for CQRS                                               │
└────────────────────────────────────────────────────────────────────────┘
                                    ↓ depends on
┌────────────────────────────────────────────────────────────────────────┐
│                         DOMAIN LAYER (Business Logic)                  │
│  Aggregates: EmailMessage ✓, User ✓, EmailTemplate ✓                  │
│  Value Objects: EmailVerificationToken, PasswordResetToken             │
│  Domain Events: UserRegisteredEvent, EmailVerificationSentEvent        │
│  - Pure business logic                                                 │
│  - No infrastructure dependencies                                      │
│  - Result pattern for all operations                                   │
└────────────────────────────────────────────────────────────────────────┘
                                    ↑ implements
┌────────────────────────────────────────────────────────────────────────┐
│                      INFRASTRUCTURE LAYER (Technical)                  │
│  Services: SmtpEmailService, RazorTemplateEngine                       │
│  Background Jobs: EmailQueueProcessor (IHostedService)                 │
│  External Systems: MailHog SMTP, PostgreSQL, RazorEngine               │
│  - Concrete implementations                                            │
│  - External dependencies                                               │
│  - Database access                                                     │
└────────────────────────────────────────────────────────────────────────┘
```

---

### 2. Complete Email Verification Flow (Step-by-Step)

```
USER ACTION                    SYSTEM COMPONENT                   DATABASE/SMTP
═══════════════════════════════════════════════════════════════════════════════

1. POST /api/auth/register
   {email, password, name}
                            →  AuthController
                                   ↓
                               RegisterUserCommand
                                   ↓
                            →  RegisterUserHandler
                                   ↓
                               User.Create()
                                   ↓
                               RaiseDomainEvent(
                                 UserCreatedEvent)
                                                          →  [Users] INSERT
                                   ↓
2. Event Published          →  UserCreatedEventHandler
                                   ↓
                               SendEmailVerificationCommand
                                   ↓
                            →  SendEmailVerificationHandler
                                   ↓
                               EmailVerificationToken.Create()
                                 • Token: GUID (32 chars)
                                 • ExpiresAt: +24 hours
                                   ↓
                               User.SetEmailVerificationToken()
                                                          →  [Users] UPDATE
                                   ↓
                               IEmailTemplateService
                                 .RenderTemplateAsync(
                                   "email-verification",
                                   {UserName, VerificationLink})
                                   ↓
                               EmailMessage.Create()
                                 • From: noreply@lankaconnect.com
                                 • To: user@example.com
                                 • Subject: "Verify Your Email"
                                 • Status: Pending
                                   ↓
                               EmailMessage.MarkAsQueued()
                                                          →  [EmailMessages] INSERT
                                   ↓
                               RaiseDomainEvent(
                                 EmailVerificationSentEvent)
                                   ↓
3. Background Job          →  EmailQueueProcessor
   (runs every 30s)            (IHostedService)
                                   ↓
                               GetQueuedEmailsAsync(50)
                                                          ←  [EmailMessages] SELECT
                                   ↓
                               For each email:
                                 EmailMessage.MarkAsSending()
                                                          →  [EmailMessages] UPDATE
                                   ↓
                               IEmailService.SendEmailAsync()
                                   ↓
                            →  SmtpEmailService
                                   ↓
                               MailKit.SmtpClient
                                 • Connect: localhost:1025
                                 • Send email
                                                          →  MailHog SMTP
                                   ↓
                               EmailMessage.MarkAsSent()
                                                          →  [EmailMessages] UPDATE
                                   ↓
4. User receives email      ←  MailHog delivers
   Opens inbox
   Clicks verification link
                            ←
5. GET /api/auth/verify-email
   ?token=xxx&userId=yyy
                            →  AuthController
                                   ↓
                               VerifyEmailCommand
                                   ↓
                            →  VerifyEmailHandler
                                   ↓
                               User.IsEmailVerificationTokenValid()
                                 • Check token matches
                                 • Check not expired
                                   ↓
                               User.VerifyEmail()
                                 • IsEmailVerified = true
                                 • Clear token
                                   ↓
                               RaiseDomainEvent(
                                 UserEmailVerifiedEvent)
                                                          →  [Users] UPDATE
                                   ↓
6. Response: 200 OK
   {message: "Email verified"}
                            ←  AuthController

════════════════════════════════════════════════════════════════════════════════
RESULT: User account activated ✓
```

---

### 3. Password Reset Flow (Detailed)

```
USER ACTION                    SYSTEM COMPONENT                   DATABASE/SMTP
═══════════════════════════════════════════════════════════════════════════════

1. User forgets password
   Clicks "Forgot Password"
                            ↓
2. POST /api/auth/forgot-password
   {email: "user@example.com"}
                            →  AuthController
                                   ↓
                               SendPasswordResetCommand
                                   ↓
                            →  SendPasswordResetHandler
                                   ↓
                               Find User by email
                                                          ←  [Users] SELECT
                                   ↓
                               PasswordResetToken.Create()
                                 • Token: GUID
                                 • ExpiresAt: +1 hour
                                 • IsUsed: false
                                   ↓
                               User.SetPasswordResetToken()
                                                          →  [Users] UPDATE
                                   ↓
                               IEmailTemplateService
                                 .RenderTemplateAsync(
                                   "password-reset",
                                   {UserName, ResetLink})
                                   ↓
                               EmailMessage.Create()
                                 • Subject: "Reset Your Password"
                                 • Priority: HIGH (1)
                                   ↓
                               EmailMessage.MarkAsQueued()
                                                          →  [EmailMessages] INSERT
                                   ↓
                               RaiseDomainEvent(
                                 PasswordResetRequestedEvent)
                                   ↓
3. Background Job          →  EmailQueueProcessor
                                   ↓
                               Process & send via SMTP
                                                          →  MailHog/SMTP
                                   ↓
4. User receives email      ←  Email delivered
   Clicks reset link
   (expires in 1 hour)
                            ↓
5. GET /reset-password
   ?token=xxx&userId=yyy
                            →  Frontend SPA
                                   ↓
                               Display "Set New Password" form
                            ←
6. User enters new password
   POST /api/auth/reset-password
   {userId, token, newPassword}
                            →  AuthController
                                   ↓
                               ResetPasswordCommand
                                   ↓
                            →  ResetPasswordHandler
                                   ↓
                               User.IsPasswordResetTokenValid()
                                 • Check token matches
                                 • Check not expired
                                 • Check not used
                                   ↓
                               Hash new password
                                   ↓
                               User.ChangePassword(hash)
                                 • PasswordHash = new hash
                                 • Clear PasswordResetToken
                                 • Reset FailedLoginAttempts
                                   ↓
                               RaiseDomainEvent(
                                 PasswordResetCompletedEvent)
                                                          →  [Users] UPDATE
                                   ↓
7. Response: 200 OK
   {message: "Password reset"}
                            ←  AuthController

════════════════════════════════════════════════════════════════════════════════
RESULT: Password changed ✓
```

---

### 4. Email State Machine

```
┌────────────────────────────────────────────────────────────────────────┐
│                   EmailMessage Aggregate State Machine                 │
└────────────────────────────────────────────────────────────────────────┘

    ┌─────────────┐
    │   PENDING   │  ← Initial state when EmailMessage.Create()
    └──────┬──────┘
           │ MarkAsQueued()
           ↓
    ┌─────────────┐
    │   QUEUED    │  ← Picked up by EmailQueueProcessor
    └──────┬──────┘
           │ MarkAsSending()
           ↓
    ┌─────────────┐
    │   SENDING   │  ← SMTP client is sending
    └──────┬──────┘
           │
           ├──→ SUCCESS: MarkAsSent()
           │       ↓
           │    ┌─────────────┐
           │    │    SENT     │  ← Email accepted by SMTP
           │    └──────┬──────┘
           │           │ MarkAsDelivered()
           │           ↓
           │    ┌─────────────┐
           │    │  DELIVERED  │  ← Final success state
           │    └─────────────┘
           │
           └──→ FAILURE: MarkAsFailed(error, nextRetryAt)
                   ↓
            ┌─────────────┐
            │   FAILED    │
            └──────┬──────┘
                   │
                   ├──→ CanRetry() = true
                   │      Retry()
                   │      ↓
                   │   ┌─────────────┐
                   │   │   QUEUED    │  ← Back to queue
                   │   └─────────────┘
                   │
                   └──→ CanRetry() = false (max retries exceeded)
                          ↓
                       ┌─────────────────────┐
                       │  FAILED (PERMANENT) │  ← Final failure state
                       └─────────────────────┘

RETRY LOGIC:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• MaxRetries: 3
• RetryCount: Incremented on each MarkAsFailed()
• NextRetryAt: Exponential backoff (2^RetryCount * BaseDelay)
  - Retry 1: Now + 30 seconds
  - Retry 2: Now + 60 seconds
  - Retry 3: Now + 120 seconds
• CanRetry(): RetryCount <= MaxRetries && NextRetryAt <= Now
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

### 5. Component Dependency Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                         External Systems                             │
└──────────────────────────────────────────────────────────────────────┘
        ↑                    ↑                    ↑
        │                    │                    │
   MailKit SMTP         RazorEngine          PostgreSQL
   (MailHog)            (Templating)         (Database)
        ↑                    ↑                    ↑
        │                    │                    │
┌──────────────────────────────────────────────────────────────────────┐
│                      Infrastructure Layer                            │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐   │
│  │ SmtpEmailService │  │RazorTemplateEngine│  │ Repositories     │   │
│  │ implements       │  │ implements        │  │ EmailMessage     │   │
│  │ IEmailService    │  │IEmailTemplateServ │  │ User             │   │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘   │
│                                                                       │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │  EmailQueueProcessor (IHostedService)                        │   │
│  │  • Runs every 30 seconds                                     │   │
│  │  • Sends ProcessEmailQueueCommand via MediatR                │   │
│  └──────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────┘
        ↑                    ↑                    ↑
        │ injects            │ injects            │ injects
        │                    │                    │
┌──────────────────────────────────────────────────────────────────────┐
│                      Application Layer                               │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐   │
│  │  Command         │  │  Query           │  │  Interfaces      │   │
│  │  Handlers        │  │  Handlers        │  │                  │   │
│  │  • SendEmail     │  │  • GetHistory    │  │  IEmailService   │   │
│  │  • ProcessQueue  │  │  • GetStatus     │  │  IEmailTemplate  │   │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘   │
└──────────────────────────────────────────────────────────────────────┘
        ↑                    ↑
        │ uses               │ uses
        │                    │
┌──────────────────────────────────────────────────────────────────────┐
│                         Domain Layer                                 │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐   │
│  │  Aggregates      │  │  Value Objects   │  │  Domain Events   │   │
│  │  • EmailMessage  │  │  • EmailVerif    │  │  • UserCreated   │   │
│  │  • User          │  │    Token         │  │  • EmailSent     │   │
│  │  • EmailTemplate │  │  • PasswordReset │  │  • PasswordReset │   │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘   │
└──────────────────────────────────────────────────────────────────────┘
        ↑
        │ uses (MediatR commands)
        │
┌──────────────────────────────────────────────────────────────────────┐
│                         API Layer                                    │
│  ┌──────────────────┐  ┌──────────────────┐                          │
│  │ EmailController  │  │ AuthController   │                          │
│  │ • GetHistory     │  │ • Register       │                          │
│  │ • GetStatus      │  │ • VerifyEmail    │                          │
│  │ • Resend         │  │ • ForgotPassword │                          │
│  └──────────────────┘  └──────────────────┘                          │
└──────────────────────────────────────────────────────────────────────┘
```

---

### 6. File Structure Tree

```
LankaConnect/
├── src/
│   ├── LankaConnect.Domain/
│   │   ├── Communications/
│   │   │   ├── Entities/
│   │   │   │   ├── EmailMessage.cs              ✅ EXISTS (38 tests)
│   │   │   │   └── EmailTemplate.cs             ✅ EXISTS
│   │   │   ├── ValueObjects/
│   │   │   │   ├── EmailVerificationToken.cs    🆕 NEW
│   │   │   │   ├── PasswordResetToken.cs        🆕 NEW
│   │   │   │   ├── TemplateVariable.cs          🆕 NEW
│   │   │   │   └── EmailSubject.cs              ✅ EXISTS
│   │   │   └── Enums/
│   │   │       ├── EmailStatus.cs               ✅ EXISTS
│   │   │       └── EmailType.cs                 ✅ EXISTS
│   │   ├── Users/
│   │   │   └── User.cs                          ✅ EXISTS (token support)
│   │   └── Events/
│   │       ├── UserRegisteredEvent.cs           🆕 NEW
│   │       ├── EmailVerificationSentEvent.cs    🆕 NEW
│   │       ├── PasswordResetRequestedEvent.cs   🆕 NEW
│   │       └── UserEmailVerifiedEvent.cs        ✅ EXISTS
│   │
│   ├── LankaConnect.Application/
│   │   ├── Communications/
│   │   │   ├── Commands/
│   │   │   │   ├── SendEmailVerification/       ✅ EXISTS
│   │   │   │   ├── VerifyEmail/                 ✅ EXISTS
│   │   │   │   ├── SendPasswordReset/           ✅ EXISTS
│   │   │   │   ├── ResetPassword/               ✅ EXISTS
│   │   │   │   ├── SendTransactionalEmail/      🆕 NEW
│   │   │   │   └── ProcessEmailQueue/           🆕 NEW
│   │   │   └── Queries/
│   │   │       ├── GetEmailHistory/             🆕 NEW
│   │   │       ├── GetEmailStatus/              ✅ EXISTS
│   │   │       └── SearchEmails/                🆕 NEW
│   │   └── Common/
│   │       └── Interfaces/
│   │           ├── IEmailService.cs             ✅ EXISTS
│   │           ├── IEmailTemplateService.cs     ✅ EXISTS
│   │           ├── IEmailMessageRepository.cs   ✅ EXISTS
│   │           └── IEmailTemplateRepository.cs  ✅ EXISTS
│   │
│   ├── LankaConnect.Infrastructure/
│   │   └── Communications/
│   │       ├── EmailService/
│   │       │   ├── SmtpEmailService.cs          🆕 NEW (MailKit)
│   │       │   ├── SmtpSettings.cs              🆕 NEW
│   │       │   └── EmailServiceExtensions.cs    🆕 NEW
│   │       ├── TemplateEngine/
│   │       │   ├── RazorTemplateEngine.cs       🆕 NEW (RazorEngineCore)
│   │       │   ├── TemplateCache.cs             🆕 NEW
│   │       │   └── TemplateEngineExtensions.cs  🆕 NEW
│   │       ├── BackgroundJobs/
│   │       │   ├── EmailQueueProcessor.cs       🆕 NEW (IHostedService)
│   │       │   └── EmailQueueProcessorSettings.cs 🆕 NEW
│   │       └── Templates/
│   │           ├── EmailVerification.cshtml     🆕 NEW
│   │           ├── PasswordReset.cshtml         🆕 NEW
│   │           └── TransactionalBase.cshtml     🆕 NEW
│   │
│   └── LankaConnect.API/
│       └── Controllers/
│           ├── EmailController.cs               🆕 NEW
│           └── AuthController.cs                ✅ EXISTS (enhance)
│
└── tests/
    ├── LankaConnect.Domain.Tests/
    │   └── Communications/
    │       └── ValueObjects/
    │           ├── EmailVerificationTokenTests.cs 🆕 NEW (~6 tests)
    │           └── PasswordResetTokenTests.cs     🆕 NEW (~6 tests)
    │
    ├── LankaConnect.Application.Tests/
    │   └── Communications/
    │       ├── Commands/
    │       │   ├── SendTransactionalEmailTests.cs 🆕 NEW (~5 tests)
    │       │   └── ProcessEmailQueueTests.cs      🆕 NEW (~5 tests)
    │       └── Queries/
    │           └── GetEmailHistoryTests.cs        🆕 NEW (~5 tests)
    │
    ├── LankaConnect.Infrastructure.Tests/
    │   └── Communications/
    │       ├── SmtpEmailServiceTests.cs           🆕 NEW (~10 tests)
    │       ├── RazorTemplateEngineTests.cs        🆕 NEW (~8 tests)
    │       └── EmailQueueProcessorTests.cs        🆕 NEW (~5 tests)
    │
    └── LankaConnect.API.Tests/
        └── Controllers/
            ├── EmailControllerTests.cs            🆕 NEW (~8 tests)
            └── AuthControllerTests.cs             ✅ EXISTS (add ~5 tests)

LEGEND:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ EXISTS     - File already exists, may need enhancement
🆕 NEW        - File needs to be created
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

EXISTING FOUNDATION: 38 tests for EmailMessage aggregate ✓
NEW TESTS NEEDED: ~52 tests across all layers
TOTAL FINAL TESTS: ~90 tests
```

---

### 7. Configuration Files

**appsettings.Development.json**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "LankaConnect.Infrastructure.Communications": "Debug"
    }
  },
  "SmtpSettings": {
    "Host": "localhost",
    "Port": 1025,
    "Username": null,
    "Password": null,
    "DefaultFromEmail": "noreply@lankaconnect.com",
    "DefaultFromName": "LankaConnect",
    "EnableSsl": false
  },
  "EmailQueueProcessorSettings": {
    "BatchSize": 50,
    "PollingIntervalSeconds": 30,
    "Enabled": true
  }
}
```

**appsettings.Production.json**
```json
{
  "SmtpSettings": {
    "Host": "smtp.sendgrid.net",
    "Port": 587,
    "Username": "apikey",
    "Password": "SG.xxxx",
    "DefaultFromEmail": "noreply@lankaconnect.com",
    "DefaultFromName": "LankaConnect",
    "EnableSsl": true
  },
  "EmailQueueProcessorSettings": {
    "BatchSize": 100,
    "PollingIntervalSeconds": 10,
    "Enabled": true
  }
}
```

---

### 8. NuGet Packages Required

```xml
<!-- Infrastructure Layer -->
<PackageReference Include="MailKit" Version="4.3.0" />
<PackageReference Include="MimeKit" Version="4.3.0" />
<PackageReference Include="RazorEngineCore" Version="2022.8.1" />

<!-- Testing -->
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="xUnit" Version="2.6.2" />
<PackageReference Include="Moq" Version="4.20.69" />
```

---

### 9. Docker Compose for Local Development

```yaml
version: '3.8'
services:
  mailhog:
    image: mailhog/mailhog:latest
    container_name: lankaconnect-mailhog
    ports:
      - "1025:1025"  # SMTP server
      - "8025:8025"  # Web UI
    networks:
      - lankaconnect-network

  postgres:
    image: postgres:15
    container_name: lankaconnect-postgres
    environment:
      POSTGRES_USER: lankaconnect
      POSTGRES_PASSWORD: dev_password
      POSTGRES_DB: lankaconnect_dev
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data
    networks:
      - lankaconnect-network

networks:
  lankaconnect-network:
    driver: bridge

volumes:
  postgres-data:
```

**Start services:**
```bash
docker-compose up -d
```

**Access MailHog UI:**
```
http://localhost:8025
```

---

### 10. Testing Strategy Summary

```
┌────────────────────────────────────────────────────────────────────┐
│                     Testing Pyramid                                │
└────────────────────────────────────────────────────────────────────┘

                           ▲
                          ╱ ╲
                         ╱   ╲
                        ╱     ╲
                       ╱       ╲
                      ╱ E2E (20) ╲   ← API integration tests
                     ╱───────────╲    WebApplicationFactory
                    ╱             ╲   Full request/response
                   ╱               ╲
                  ╱─────────────────╲
                 ╱  Integration (30)  ╲ ← Infrastructure tests
                ╱─────────────────────╲  Real SMTP, DB, Razor
               ╱                       ╲
              ╱─────────────────────────╲
             ╱      Unit Tests (40)      ╲ ← Domain + Application
            ╱─────────────────────────────╲  Mocked dependencies
           ╱                               ╲
          ╱═════════════════════════════════╲

BREAKDOWN:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Layer            Test Count    Type           Tools
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Domain           15            Unit           xUnit, FluentAssertions
Application      25            Unit           xUnit, Moq, MediatR
Infrastructure   30            Integration    xUnit, MailHog, TestDb
API              20            E2E            WebApplicationFactory
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
TOTAL            90            Mixed          TDD Zero Tolerance
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

TDD WORKFLOW:
1. RED   → Write failing test first
2. GREEN → Write minimal code to pass
3. REFACTOR → Improve code quality
4. REPEAT → Next test

ZERO TOLERANCE RULE:
✓ All tests must pass at every commit
✓ No compilation errors allowed
✓ 100% code coverage for business logic
```

---

## Implementation Checklist

### Phase 1: Domain Layer (2-3 hours)
- [ ] Create `EmailVerificationToken.cs` value object
- [ ] Create `PasswordResetToken.cs` value object
- [ ] Create `TemplateVariable.cs` value object
- [ ] Create `UserRegisteredEvent.cs` domain event
- [ ] Create `EmailVerificationSentEvent.cs` domain event
- [ ] Create `PasswordResetRequestedEvent.cs` domain event
- [ ] Write 15 unit tests (all passing)

### Phase 2: Application Layer (4-5 hours)
- [ ] Create `SendTransactionalEmailCommand` + Handler + Validator
- [ ] Create `ProcessEmailQueueCommand` + Handler
- [ ] Create `GetEmailHistoryQuery` + Handler + Validator
- [ ] Create `SearchEmailsQuery` + Handler
- [ ] Write 25 unit tests with mocked dependencies (all passing)

### Phase 3: Infrastructure Layer (6-8 hours)
- [ ] Implement `SmtpEmailService.cs` (MailKit)
- [ ] Implement `RazorTemplateEngine.cs` (RazorEngineCore)
- [ ] Implement `EmailQueueProcessor.cs` (IHostedService)
- [ ] Create email templates (Razor .cshtml files)
- [ ] Configure dependency injection in `Program.cs`
- [ ] Write 30 integration tests with MailHog (all passing)

### Phase 4: API Layer (3-4 hours)
- [ ] Create `EmailController.cs` (history, status, resend)
- [ ] Enhance `AuthController.cs` (verification endpoints)
- [ ] Write 20 E2E tests with WebApplicationFactory (all passing)

### Phase 5: Documentation & Deployment (2-3 hours)
- [ ] Seed email templates to database
- [ ] Update configuration files (appsettings.json)
- [ ] Create Docker Compose setup
- [ ] Write deployment guide
- [ ] Run full test suite (90 tests passing)

**Total Estimated Time:** 15-20 hours (with TDD)

---

## Quick Start Commands

```bash
# 1. Start MailHog (local SMTP server)
docker run -d -p 1025:1025 -p 8025:8025 mailhog/mailhog

# 2. Restore dependencies
dotnet restore

# 3. Run migrations
dotnet ef database update --project src/LankaConnect.Infrastructure

# 4. Run all tests
dotnet test

# 5. Start API
dotnet run --project src/LankaConnect.API

# 6. Access MailHog UI
http://localhost:8025
```

---

## Support & Resources

- **Architecture Document:** `docs/architecture/EMAIL_NOTIFICATIONS_ARCHITECTURE.md`
- **Visual Guide:** `docs/architecture/EMAIL_SYSTEM_VISUAL_GUIDE.md` (this file)
- **MailKit Docs:** https://github.com/jstedfast/MailKit
- **RazorEngine Docs:** https://github.com/adoconnection/RazorEngineCore

---

**Architecture Status:** APPROVED ✓
**Implementation Ready:** YES ✓
**Zero Tolerance:** ENFORCED ✓
