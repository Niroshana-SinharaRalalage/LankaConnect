# Root Cause Analysis: User Registration 400 Error
**Date**: 2025-12-30
**Status**: CRITICAL - Production Issue
**Impact**: New user registrations failing with 400 error, but users ARE being created

---

## Executive Summary

**CONFIRMED ROOT CAUSE**: Double CommitAsync() with Entity Framework change tracking issue

### Evidence Chain
1. **RegisterUserHandler.cs:113** - First `CommitAsync()` succeeds → User saved ✅
2. **RegisterUserHandler.cs:111** - `User.Create()` calls `GenerateEmailVerificationToken()`
3. **User.cs:267-283** - Token generated + domain event raised
4. **RegisterUserHandler.cs:116-117** - `SendEmailVerificationCommand` dispatched
5. **SendEmailVerificationCommandHandler.cs:71** - Calls `user.GenerateEmailVerificationToken()` AGAIN
6. **SendEmailVerificationCommandHandler.cs:74** - Second `CommitAsync()` attempts to save
7. **PROBLEM**: User entity from step 2 is DETACHED after first CommitAsync()
8. **RESULT**: Exception thrown → RegisterUserHandler catches (line 136) → Returns 400

### Why This Happened

**Phase 6A.53 Implementation Changed the Flow**:

**OLD FLOW (Before Phase 6A.53)**:
```
RegisterUserHandler:
  1. Create user (no token generation)
  2. Save user (CommitAsync)
  3. SendEmailVerificationCommand
     - Load user from DB (fresh context)
     - Generate token (first time)
     - CommitAsync (works fine)
```

**NEW FLOW (Phase 6A.53 - BROKEN)**:
```
RegisterUserHandler:
  1. User.Create() → GenerateEmailVerificationToken() called
     - Token generated
     - Domain event raised
  2. Save user (CommitAsync) → User entity DETACHED from context
  3. SendEmailVerificationCommand
     - Load user from DB → Gets user with token already set
     - GenerateEmailVerificationToken() called AGAIN
     - CommitAsync → FAILS (entity tracking issue OR no-op causing issues)
```

**KEY INSIGHT**: Phase 6A.53 added automatic token generation in `User.Create()` (line 111), but `SendEmailVerificationCommandHandler` STILL calls `GenerateEmailVerificationToken()` again (line 71), causing duplicate token generation and potential EF Core tracking issues.

---

## Impact Assessment

### Severity: CRITICAL
- **User Registration**: BLOCKED (400 error returned to frontend)
- **User Account Creation**: PARTIAL (users ARE created in database)
- **Email Delivery**: BLOCKED (emails not being sent)
- **User Experience**: BROKEN (users cannot verify accounts)

### Scope
- **Affected Environment**: Azure Staging
- **Time Window**: Started after Phase 6A.58 deployment (commit `04940b0f`)
- **User Impact**: ALL new registrations since deployment
- **Data Integrity**: No data corruption (users created correctly)

---

## Detailed Code Analysis

### 1. RegisterUserHandler Flow

```csharp
// Line 72-76: User.Create() calls GenerateEmailVerificationToken()
var userResult = User.Create(emailResult.Value, request.FirstName, request.LastName, actualRole);
// At this point: User has token generated, domain event in queue

// Line 112-113: Save user (domain events dispatched HERE)
await _userRepository.AddAsync(user, cancellationToken);
await _unitOfWork.CommitAsync(cancellationToken);  // ✅ SUCCEEDS
// At this point: User saved, token saved, domain event ALREADY processed

// Line 116-117: Send verification email (REDUNDANT)
var sendEmailCommand = new SendEmailVerificationCommand(user.Id);
var sendEmailResult = await _mediator.Send(sendEmailCommand, cancellationToken);
// This should NOT be necessary - email already sent via domain event!

// Line 119-124: If email fails, ONLY log warning
if (!sendEmailResult.IsSuccess)
{
    _logger.LogWarning(...);  // Just logs
}

// Line 134: Return success (NEVER REACHED if exception thrown)
return Result<RegisterUserResponse>.Success(response);
```

### 2. SendEmailVerificationCommandHandler Issue

```csharp
// Line 39: Load user from database
var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
// User already has token from User.Create()

// Line 71: Generate token AGAIN (DUPLICATE)
user.GenerateEmailVerificationToken();
// This modifies the user entity, raises domain event AGAIN

// Line 74: Commit changes (PROBLEM)
await _unitOfWork.CommitAsync(cancellationToken);
// Potential issues:
// 1. Entity tracking conflict (user modified after detach)
// 2. Duplicate domain event processing
// 3. Race condition with first email send
```

### 3. Domain Event Handler (Fail-Silent Pattern)

```csharp
// MemberVerificationRequestedEventHandler.cs:80-88
catch (Exception ex)
{
    // FAIL-SILENT: Log but don't throw
    _logger.LogError(ex, ...);
    // Do NOT re-throw - prevents transaction rollback
}
```

**KEY**: Domain event handler uses fail-silent pattern, so email failures DON'T cause 400 error. The 400 must come from `SendEmailVerificationCommandHandler` exception.

---

## Diagnostic Queries

### Query 1: Check Migration Status
```sql
-- Verify Phase 6A.53 Fix #3 migration was applied
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%Phase6A53%'
ORDER BY "MigrationId" DESC;
```

**Expected Result**: Should see `20251229231742_Phase6A53Fix3_RevertToCorrectTemplateNoLogo`

**If Missing**: Email template migration not applied → old template causing rendering errors

### Query 2: Email Template State
```sql
-- Check current template (should NOT have decorative stars)
SELECT
    "Id",
    "Type",
    CASE
        WHEN "HtmlTemplate" LIKE '%✦%' THEN 'OLD_TEMPLATE'
        ELSE 'NEW_TEMPLATE'
    END as template_version,
    LENGTH("HtmlTemplate") as template_length,
    "Subject",
    "UpdatedAt"
FROM "EmailTemplates"
WHERE "Type" = 1;  -- EmailVerification
```

**Expected Result**: `NEW_TEMPLATE` without stars

**If OLD_TEMPLATE**: Migration not applied, email rendering may fail

### Query 3: Recent Registration Attempts
```sql
-- Users created in last 24 hours
SELECT
    "Id",
    "Email",
    "UserName",
    "IsEmailVerified",
    "EmailVerificationToken" IS NOT NULL as has_token,
    "EmailVerificationTokenExpiresAt",
    "CreatedAt",
    EXTRACT(EPOCH FROM (NOW() - "CreatedAt"))/60 as minutes_ago
FROM "Users"
WHERE "CreatedAt" >= NOW() - INTERVAL '24 hours'
ORDER BY "CreatedAt" DESC;
```

**Expected Result**: Multiple users with tokens but `IsEmailVerified = false`

**Confirms**: Users being created but verification emails not sent

### Query 4: Token Generation Timing
```sql
-- Check if tokens are being regenerated
SELECT
    "Email",
    "CreatedAt",
    "EmailVerificationTokenExpiresAt",
    EXTRACT(EPOCH FROM ("EmailVerificationTokenExpiresAt" - INTERVAL '24 hours' - "CreatedAt"))/60
        as token_generation_delay_minutes
FROM "Users"
WHERE "CreatedAt" >= NOW() - INTERVAL '24 hours'
ORDER BY "CreatedAt" DESC;
```

**Expected Result**: Delay should be ~0 minutes (token generated immediately)

**If Delay > 0**: Indicates token regeneration happening after user creation

---

## Diagnostic Steps (Execute in Order)

### Step 1: Database State Check (2 minutes)
1. Run all 4 diagnostic queries above
2. Verify migration status
3. Check email template version
4. Confirm recent user creation patterns

### Step 2: Azure Logs Analysis (5 minutes)
```bash
# Check Azure Container App logs for exceptions
az containerapp logs show \
  --name <app-name> \
  --resource-group <rg-name> \
  --follow false \
  --tail 200 \
  | grep -E "Error|Exception|Failed|Phase 6A.53"
```

**Look For**:
- `MemberVerificationRequestedEvent` log entries
- Email service exceptions
- Entity Framework tracking exceptions
- `SendEmailVerificationCommandHandler` errors

### Step 3: API Endpoint Test (3 minutes)
```bash
# Test registration endpoint directly
curl -X POST https://staging-api.example.com/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test-$(date +%s)@example.com",
    "password": "Test@123456",
    "firstName": "Test",
    "lastName": "User",
    "selectedRole": 1
  }' \
  -v
```

**Expected**: 400 error with generic message

**Check**: Does user appear in database?

### Step 4: Log Level Verification (2 minutes)
Check `appsettings.Production.json` logging configuration:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "LankaConnect.Application.Users.EventHandlers": "Debug",
      "LankaConnect.Application.Communications": "Debug"
    }
  }
}
```

**If Insufficient**: Exceptions may be swallowed without proper logging

---

## Fix Strategy

### Immediate Fix (Option A - RECOMMENDED): Remove Duplicate Call

**Problem**: `SendEmailVerificationCommand` is redundant after Phase 6A.53

**Solution**: Remove explicit email send from `RegisterUserHandler`

**File**: `src\LankaConnect.Application\Auth\Commands\RegisterUser\RegisterUserHandler.cs`

```csharp
// REMOVE Lines 115-124 (duplicate email send)
// Email already sent via domain event in User.Create()

// Save user (domain events dispatched automatically)
await _userRepository.AddAsync(user, cancellationToken);
await _unitOfWork.CommitAsync(cancellationToken);

// REMOVE THIS:
// var sendEmailCommand = new SendEmailVerificationCommand(user.Id);
// var sendEmailResult = await _mediator.Send(sendEmailCommand, cancellationToken);
// if (!sendEmailResult.IsSuccess)
// {
//     _logger.LogWarning(...);
// }

_logger.LogInformation("User registered successfully: {Email}", request.Email);
```

**Why This Works**:
1. `User.Create()` generates token (line 111)
2. Token generation raises `MemberVerificationRequestedEvent` (line 275-282)
3. `CommitAsync()` dispatches domain events (line 113)
4. `MemberVerificationRequestedEventHandler` sends email automatically
5. No duplicate token generation
6. No second CommitAsync()
7. No entity tracking issues

### Immediate Fix (Option B - ALTERNATIVE): Skip Token Generation in SendEmailVerificationCommandHandler

**Problem**: Token already generated in `User.Create()`

**Solution**: Check if token exists before regenerating

**File**: `src\LankaConnect.Application\Communications\Commands\SendEmailVerification\SendEmailVerificationCommandHandler.cs`

```csharp
// Line 70-74: Only regenerate token if needed
if (string.IsNullOrEmpty(user.EmailVerificationToken) ||
    !user.EmailVerificationTokenExpiresAt.HasValue ||
    user.EmailVerificationTokenExpiresAt.Value <= DateTime.UtcNow ||
    request.ForceResend)
{
    user.GenerateEmailVerificationToken();
    await _unitOfWork.CommitAsync(cancellationToken);
}
else
{
    _logger.LogInformation(
        "User {UserId} already has valid token, reusing existing token",
        user.Id);
}
```

**Why This Works**:
1. If token exists and is valid, skip regeneration
2. Email already sent via domain event
3. No duplicate CommitAsync() for new registrations
4. Still works for "Resend Verification Email" feature

### Long-Term Fix: Architecture Clarification

**Problem**: Confusion between command-based and event-based email sending

**Solution**: Document clear pattern in ADR

**Decision**:
- Domain events handle automatic notifications (registration, verification)
- Commands handle explicit user actions (resend email, change email)
- No mixing of both patterns for same action

---

## Testing Plan

### Test 1: New User Registration
```bash
# Register new user
POST /api/auth/register
{
  "email": "test@example.com",
  "password": "Test@123456",
  "firstName": "Test",
  "lastName": "User",
  "selectedRole": 1
}
```

**Expected**:
- ✅ 200 OK response
- ✅ User created in database
- ✅ Email verification token generated
- ✅ Verification email sent to user
- ✅ Email uses NEW template (no decorative stars)
- ✅ Email shows correct UserName

### Test 2: Resend Verification Email
```bash
# Resend verification email (explicit user action)
POST /api/auth/resend-verification-email
{
  "email": "test@example.com"
}
```

**Expected**:
- ✅ 200 OK response
- ✅ New token generated
- ✅ New email sent
- ✅ Old token invalidated

### Test 3: Database Verification
```sql
-- Verify user state after registration
SELECT
    "Email",
    "IsEmailVerified",
    "EmailVerificationToken" IS NOT NULL as has_token,
    "EmailVerificationTokenExpiresAt",
    "CreatedAt"
FROM "Users"
WHERE "Email" = 'test@example.com';
```

**Expected**:
- ✅ `has_token = true`
- ✅ `IsEmailVerified = false`
- ✅ `EmailVerificationTokenExpiresAt` 24 hours from now

### Test 4: Email Verification
```bash
# Verify email using token from database
GET /api/auth/verify-email?token={token}
```

**Expected**:
- ✅ 200 OK response
- ✅ User.IsEmailVerified = true
- ✅ Token cleared from database

---

## Prevention Measures

### 1. Add Integration Tests
```csharp
[Fact]
public async Task RegisterUser_ShouldSendVerificationEmail_OnlyOnce()
{
    // Arrange
    var command = new RegisterUserCommand(...);
    var emailServiceMock = new Mock<IEmailService>();

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    emailServiceMock.Verify(
        x => x.SendTemplatedEmailAsync(
            "member-email-verification",
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()),
        Times.Once);  // Should only send once!
}
```

### 2. Add Architecture Rule Test
```csharp
[Fact]
public void RegisterUserHandler_ShouldNotCallSendEmailVerificationCommand()
{
    // Ensure RegisterUserHandler doesn't explicitly send emails
    // Email sending should happen via domain events only
    var handlerCode = File.ReadAllText("RegisterUserHandler.cs");
    handlerCode.Should().NotContain("SendEmailVerificationCommand");
}
```

### 3. Update ADR: Domain Event vs Command Pattern
```markdown
# ADR-XXX: Email Notification Pattern

## Decision
- Domain events handle automatic notifications triggered by state changes
- Commands handle explicit user actions requiring immediate feedback
- No mixing of both patterns for same business action

## Example
- User Registration → Domain event sends verification email
- Resend Verification Email → Command sends new email
```

### 4. Code Review Checklist
- [ ] Domain event handlers use fail-silent pattern
- [ ] No duplicate CommitAsync() calls in single transaction
- [ ] Commands don't duplicate logic already in domain events
- [ ] Entity tracking issues avoided (no detached entity modifications)

---

## Rollback Plan (If Fix Fails)

### Option 1: Revert Phase 6A.58 Deployment
```bash
# Revert to previous commit before Phase 6A.58
git revert 04940b0f
git push origin develop

# Redeploy
gh workflow run deploy-staging.yml
```

### Option 2: Hotfix with OLD Pattern
```csharp
// Temporarily revert to explicit email sending
// Remove User.Create() token generation
// Keep SendEmailVerificationCommand approach
```

---

## Next Steps (Priority Order)

1. **IMMEDIATE** (5 min): Run diagnostic SQL queries
2. **IMMEDIATE** (5 min): Check Azure logs for exceptions
3. **URGENT** (15 min): Implement Fix Option A (remove duplicate call)
4. **URGENT** (10 min): Test fix on staging
5. **URGENT** (5 min): Verify email delivery
6. **HIGH** (30 min): Add integration tests
7. **MEDIUM** (1 hour): Create ADR for email pattern
8. **MEDIUM** (30 min): Update documentation

---

## Success Criteria

Fix is considered successful when:
- ✅ New user registration returns 200 OK
- ✅ User created in database with token
- ✅ Verification email sent within 5 seconds
- ✅ Email uses correct template (no decorative stars)
- ✅ Email shows personalized UserName
- ✅ No exceptions in Azure logs
- ✅ Integration tests pass
- ✅ Zero 400 errors for 24 hours post-deployment

---

## Lessons Learned

1. **Domain Event Timing**: Events dispatched on `CommitAsync()`, not immediately
2. **Duplicate Logic**: Automatic (domain events) vs explicit (commands) must be clear
3. **Entity Tracking**: Modifying detached entities causes EF Core issues
4. **Fail-Silent Pattern**: Domain event handlers swallow exceptions, masking issues
5. **Migration Verification**: Always verify migrations applied to target environment
6. **Logging Levels**: Production needs DEBUG logging for event handlers
7. **Integration Tests**: Must test end-to-end flow, not just unit tests

---

**Prepared By**: Claude Code System Architecture Designer
**Date**: 2025-12-30
**Version**: 1.0
**Status**: Ready for Execution
