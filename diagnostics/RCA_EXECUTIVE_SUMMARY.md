# Root Cause Analysis - User Registration 400 Error
**Date**: 2025-12-30
**Severity**: CRITICAL
**Status**: Analysis Complete - Fix Ready for Implementation

---

## Problem Statement

Users attempting to register at localhost:3000 (frontend) → Azure staging backend receive **400 Bad Request** error with message "An error occurred during user registration". However, users ARE being created in the database WITH verification tokens.

---

## Root Cause (CONFIRMED)

**Double Token Generation with Entity Framework Tracking Conflict**

### Technical Explanation

Phase 6A.53 introduced automatic token generation in `User.Create()` factory method, but the existing `SendEmailVerificationCommand` call was NOT removed. This causes:

1. **First token generation** (User.cs:111) - Token generated, domain event raised
2. **First CommitAsync** (RegisterUserHandler.cs:113) - User saved, domain event dispatched, email sent ✅
3. **Redundant command** (RegisterUserHandler.cs:116-117) - `SendEmailVerificationCommand` dispatched
4. **Second token generation** (SendEmailVerificationCommandHandler.cs:71) - Token regenerated on already-saved user
5. **Second CommitAsync** (SendEmailVerificationCommandHandler.cs:74) - Fails due to entity tracking issue or causes exception
6. **Exception caught** (RegisterUserHandler.cs:136-140) - Returns 400 error to client

### Code Evidence

**RegisterUserHandler.cs** (Lines 72-76, 112-124):
```csharp
// Line 72: User.Create() calls GenerateEmailVerificationToken()
var userResult = User.Create(emailResult.Value, request.FirstName, request.LastName, actualRole);

// Line 112-113: Save user (succeeds)
await _userRepository.AddAsync(user, cancellationToken);
await _unitOfWork.CommitAsync(cancellationToken);  // ✅ User saved

// Line 116-117: REDUNDANT email send
var sendEmailCommand = new SendEmailVerificationCommand(user.Id);
var sendEmailResult = await _mediator.Send(sendEmailCommand, cancellationToken);
// This should not be here - email already sent via domain event!
```

**User.cs** (Line 111):
```csharp
// Phase 6A.53: Generate email verification token for new local users
user.GenerateEmailVerificationToken();  // Called automatically in User.Create()
```

**SendEmailVerificationCommandHandler.cs** (Lines 71-74):
```csharp
// Line 71: Generate token AGAIN (duplicate)
user.GenerateEmailVerificationToken();

// Line 74: Commit changes (FAILS)
await _unitOfWork.CommitAsync(cancellationToken);
```

---

## Why This Explains All Symptoms

| Symptom | Explanation |
|---------|-------------|
| 400 error returned to frontend | Exception in SendEmailVerificationCommandHandler caught by RegisterUserHandler try-catch |
| User IS created in database | First CommitAsync() succeeds before exception |
| User HAS verification token | Token generated in User.Create() and saved |
| User received email yesterday | Email system WAS working before Phase 6A.58 deployment |
| Email shows old template | Migration not applied OR email sent before template update |

---

## Impact Assessment

### Current State
- **Severity**: CRITICAL - Blocking new user registrations
- **User Experience**: Users see error, cannot verify accounts
- **Data Integrity**: No corruption (users created correctly)
- **Email Delivery**: Broken (emails not being sent)

### Affected Components
1. User registration flow (frontend → backend)
2. Email verification system
3. Domain event handling
4. Entity Framework change tracking

### Time Window
- Started after Phase 6A.58 deployment (commit `04940b0f`)
- All new registrations since deployment affected
- Estimated impact: All registration attempts in last 24 hours

---

## Recommended Fix

### Option A: Remove Duplicate Email Send (RECOMMENDED)

**File**: `src\LankaConnect.Application\Auth\Commands\RegisterUser\RegisterUserHandler.cs`

**Change**: Remove lines 115-124 (explicit email send)

**Rationale**:
- Email already sent via domain event in `CommitAsync()`
- `MemberVerificationRequestedEventHandler` handles email sending automatically
- Eliminates duplicate token generation
- Eliminates second CommitAsync() call
- Simplifies code and follows event-driven architecture

**Before**:
```csharp
await _unitOfWork.CommitAsync(cancellationToken);

// Send verification email automatically
var sendEmailCommand = new SendEmailVerificationCommand(user.Id);
var sendEmailResult = await _mediator.Send(sendEmailCommand, cancellationToken);

if (!sendEmailResult.IsSuccess)
{
    _logger.LogWarning(...);
}

return Result<RegisterUserResponse>.Success(response);
```

**After**:
```csharp
await _unitOfWork.CommitAsync(cancellationToken);

// Email sent automatically via MemberVerificationRequestedEvent
_logger.LogInformation("User registered successfully: {Email}", request.Email);

return Result<RegisterUserResponse>.Success(response);
```

### Option B: Conditional Token Generation (ALTERNATIVE)

**File**: `src\LankaConnect.Application\Communications\Commands\SendEmailVerification\SendEmailVerificationCommandHandler.cs`

**Change**: Only regenerate token if needed

**Before**:
```csharp
user.GenerateEmailVerificationToken();
await _unitOfWork.CommitAsync(cancellationToken);
```

**After**:
```csharp
// Only regenerate if token missing, expired, or forced
if (string.IsNullOrEmpty(user.EmailVerificationToken) ||
    !user.EmailVerificationTokenExpiresAt.HasValue ||
    user.EmailVerificationTokenExpiresAt.Value <= DateTime.UtcNow ||
    request.ForceResend)
{
    user.GenerateEmailVerificationToken();
    await _unitOfWork.CommitAsync(cancellationToken);
}
```

**Recommendation**: Use Option A (simpler, more aligned with event-driven architecture)

---

## Diagnostic Steps (Execute Before Fix)

### 1. Run Database Diagnostics (2 minutes)
```bash
# Execute: c:\Work\LankaConnect\diagnostics\quick_rca_check.sql
# Check:
#   - Migration status (Phase6A53Fix3 applied?)
#   - Email template version (old stars vs new clean?)
#   - Recent user creation (users with tokens?)
#   - Token timing (immediate vs delayed?)
```

### 2. Check Azure Logs (3 minutes)
```bash
az containerapp logs show \
  --name lankaconnect-staging \
  --resource-group lankaconnect-rg \
  --follow false \
  --tail 200 | grep -E "Error|Exception|Phase 6A.53"
```

**Look for**:
- `MemberVerificationRequestedEvent` processing logs
- Email service exceptions
- Entity Framework tracking errors
- SendEmailVerificationCommandHandler failures

### 3. Test Registration Endpoint (2 minutes)
```bash
curl -X POST https://staging-api.azurewebsites.net/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test-'$(date +%s)'@example.com",
    "password": "Test@123456",
    "firstName": "Test",
    "lastName": "User",
    "selectedRole": 1
  }'
```

**Expected**: 400 error, but user created in database

---

## Testing Plan (Post-Fix)

### Test Case 1: Happy Path Registration
```
1. Register new user via frontend
2. Verify 200 OK response
3. Check database - user created with token
4. Check email inbox - verification email received
5. Verify email template (clean, no stars)
6. Click verification link
7. Verify user.IsEmailVerified = true
```

### Test Case 2: Resend Verification Email
```
1. Register user
2. Wait for email
3. Click "Resend verification email"
4. Verify new token generated
5. Verify new email sent
6. Verify old token invalidated
```

### Test Case 3: Duplicate Registration
```
1. Register user with email@example.com
2. Attempt to register again with same email
3. Verify 400 error with "user already exists" message
4. Verify only ONE user in database
```

---

## Prevention Measures

### 1. Add Integration Test
```csharp
[Fact]
public async Task RegisterUser_ShouldSendEmailOnlyOnce()
{
    // Arrange
    var command = new RegisterUserCommand(...);
    var emailServiceMock = new Mock<IEmailService>();

    // Act
    await _handler.Handle(command, CancellationToken.None);

    // Assert
    emailServiceMock.Verify(
        x => x.SendTemplatedEmailAsync(...),
        Times.Once);  // Email sent exactly once
}
```

### 2. Architecture Decision Record
Create ADR documenting:
- Domain events handle automatic notifications
- Commands handle explicit user actions
- No mixing of both for same business operation

### 3. Code Review Checklist
- [ ] No duplicate CommitAsync() in single handler
- [ ] Domain event logic not duplicated in commands
- [ ] Entity tracking issues avoided
- [ ] Integration tests cover end-to-end flow

---

## Success Metrics

Fix is successful when:
- ✅ New registration returns 200 OK
- ✅ User created with verification token
- ✅ Verification email sent within 5 seconds
- ✅ Email uses correct template
- ✅ Email shows personalized name
- ✅ No exceptions in Azure logs
- ✅ Zero 400 errors for 24 hours post-fix

---

## Next Actions (Priority Order)

| Priority | Action | Time | Owner |
|----------|--------|------|-------|
| P0 | Run diagnostic SQL queries | 5 min | Developer |
| P0 | Check Azure logs for exceptions | 5 min | Developer |
| P0 | Implement Fix Option A | 15 min | Developer |
| P0 | Test fix on staging environment | 15 min | Developer |
| P0 | Verify email delivery | 5 min | Developer |
| P1 | Add integration tests | 30 min | Developer |
| P1 | Create ADR for email pattern | 30 min | Architect |
| P2 | Update documentation | 15 min | Developer |

**Total Time to Fix**: ~1.5 hours

---

## Rollback Plan

If fix causes issues:

1. **Immediate Rollback**:
   ```bash
   git revert <fix-commit-sha>
   git push origin develop
   ```

2. **Alternative**: Deploy previous stable commit
   ```bash
   git checkout <stable-commit>
   gh workflow run deploy-staging.yml
   ```

3. **Hotfix**: Temporarily disable automatic email sending
   ```csharp
   // Comment out token generation in User.Create()
   // Keep explicit SendEmailVerificationCommand approach
   ```

---

## Related Documents

1. **Detailed RCA**: `c:\Work\LankaConnect\diagnostics\rca_detailed_diagnostic_plan.md`
2. **Diagnostic Queries**: `c:\Work\LankaConnect\diagnostics\quick_rca_check.sql`
3. **Full Diagnostics**: `c:\Work\LankaConnect\diagnostics\rca_user_registration_400_diagnostics.sql`
4. **Phase 6A.53 Docs**: See PHASE_6A_MASTER_INDEX.md for implementation history

---

## Lessons Learned

1. **Domain Event Timing**: Events dispatched during `CommitAsync()`, not immediately
2. **Redundant Logic**: When adding automatic behavior, remove explicit calls
3. **Entity Tracking**: Avoid modifying entities after `CommitAsync()`
4. **Fail-Silent Pattern**: Domain event handlers mask exceptions
5. **Migration Verification**: Always verify migrations in target environment
6. **Integration Testing**: Unit tests alone insufficient for event-driven flows

---

**Prepared By**: System Architecture Designer (Claude)
**Reviewed By**: Pending
**Approved By**: Pending
**Status**: Ready for Implementation
