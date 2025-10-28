# Entra External ID Integration - Implementation Roadmap

**Status:** Ready for Execution
**Date:** 2025-10-28
**Related ADR:** ADR-002-Entra-External-ID-Integration
**Methodology:** Test-Driven Development (TDD) with Zero-Tolerance for Compilation Errors

---

## Overview

This roadmap provides a **step-by-step, incremental implementation plan** for integrating Microsoft Entra External ID with LankaConnect, following strict TDD principles and Clean Architecture guidelines.

**Core Principles:**
1. **Red-Green-Refactor** cycle for every change
2. **Zero compilation errors** at any checkpoint
3. **All existing tests pass** after each step
4. **Incremental commits** with clear messages
5. **Backward compatibility** maintained throughout

---

## Implementation Phases

### Phase 1: Domain Layer Foundation (Week 1)
**Goal:** Add identity provider support to User aggregate with 100% passing tests
**Estimated Time:** 8-12 hours
**Test Coverage Target:** 95%+

### Phase 2: Infrastructure Layer (Week 2)
**Goal:** Implement Entra token validation service and repository methods
**Estimated Time:** 12-16 hours
**Test Coverage Target:** 90%+

### Phase 3: Application Layer (Week 3)
**Goal:** Create Entra login command handler and auto-provisioning logic
**Estimated Time:** 10-14 hours
**Test Coverage Target:** 95%+

### Phase 4: Presentation Layer (Week 4)
**Goal:** Add Entra authentication API endpoints
**Estimated Time:** 8-10 hours
**Test Coverage Target:** 90%+

### Phase 5: Integration & Migration (Week 5)
**Goal:** Database migration, E2E testing, production deployment
**Estimated Time:** 12-16 hours
**Test Coverage Target:** E2E scenarios 100% passing

---

## Phase 1: Domain Layer Foundation

### Step 1.1: Add IdentityProvider Enum
**Status:** ✅ No Breaking Changes
**TDD:** Not applicable (enum definition)

```csharp
// File: src/LankaConnect.Domain/Users/Enums/IdentityProvider.cs
namespace LankaConnect.Domain.Users.Enums;

public enum IdentityProvider
{
    Local = 0,
    EntraExternal = 1
}
```

**Commit Message:**
```
feat(domain): Add IdentityProvider enum for multi-provider auth

- Add Local (0) and EntraExternal (1) providers
- Prepares for Entra External ID integration
- No breaking changes

Related: ADR-002-Entra-External-ID-Integration
```

**Verification:**
```bash
dotnet build src/LankaConnect.Domain
# Should compile successfully
```

---

### Step 1.2: Add Properties to User Entity
**Status:** ⚠️ Breaking Change - Requires Tests First

#### TDD Cycle 1: Write Failing Tests

**File:** `tests/LankaConnect.Domain.Tests/Users/UserExternalProviderTests.cs`

```csharp
using FluentAssertions;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using Xunit;

namespace LankaConnect.Domain.Tests.Users;

public class UserExternalProviderTests
{
    [Fact]
    public void Create_LocalUser_ShouldSetProviderToLocal()
    {
        // Arrange
        var email = Email.Create("test@local.com").Value;

        // Act
        var result = User.Create(email, "John", "Doe");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IdentityProvider.Should().Be(IdentityProvider.Local);
        result.Value.ExternalProviderId.Should().BeNull();
        result.Value.PasswordHash.Should().BeNull(); // Not set yet
        result.Value.IsEmailVerified.Should().BeFalse();
    }

    [Fact]
    public void CreateFromExternalProvider_WithValidData_ShouldSucceed()
    {
        // Arrange
        var email = Email.Create("test@entra.com").Value;
        var providerId = "entra-oid-12345-67890";

        // Act
        var result = User.CreateFromExternalProvider(
            email, "Jane", "Smith",
            IdentityProvider.EntraExternal,
            providerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IdentityProvider.Should().Be(IdentityProvider.EntraExternal);
        result.Value.ExternalProviderId.Should().Be(providerId);
        result.Value.PasswordHash.Should().BeNull();
        result.Value.IsEmailVerified.Should().BeTrue(); // Auto-verified
    }

    [Fact]
    public void CreateFromExternalProvider_WithLocalProvider_ShouldFail()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;

        // Act
        var result = User.CreateFromExternalProvider(
            email, "Test", "User",
            IdentityProvider.Local,
            "some-id");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("external identity provider");
    }

    [Fact]
    public void CreateFromExternalProvider_WithoutExternalId_ShouldFail()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;

        // Act
        var result = User.CreateFromExternalProvider(
            email, "Test", "User",
            IdentityProvider.EntraExternal,
            "");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("External provider ID is required");
    }

    [Fact]
    public void CreateFromExternalProvider_ShouldRaiseDomainEvent()
    {
        // Arrange
        var email = Email.Create("test@entra.com").Value;
        var providerId = "entra-oid-event-test";

        // Act
        var result = User.CreateFromExternalProvider(
            email, "Event", "Test",
            IdentityProvider.EntraExternal,
            providerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var domainEvents = result.Value.GetDomainEvents();
        domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserCreatedFromExternalProviderEvent>();
    }
}
```

**Run Tests (Should FAIL):**
```bash
cd tests/LankaConnect.Domain.Tests
dotnet test --filter "FullyQualifiedName~UserExternalProviderTests"
# Expected: All tests fail (methods don't exist yet)
```

#### TDD Cycle 2: Make Tests Pass

**File:** `src/LankaConnect.Domain/Users/User.cs`

```csharp
// ADD these properties
public IdentityProvider IdentityProvider { get; private set; }
public string? ExternalProviderId { get; private set; }

// MODIFY existing Create method
public static Result<User> Create(Email? email, string firstName, string lastName, UserRole role = UserRole.User)
{
    if (email == null)
        return Result<User>.Failure("Email is required");

    if (string.IsNullOrWhiteSpace(firstName))
        return Result<User>.Failure("First name is required");

    if (string.IsNullOrWhiteSpace(lastName))
        return Result<User>.Failure("Last name is required");

    var user = new User(email, firstName.Trim(), lastName.Trim(), role);

    // NEW: Set identity provider
    user.IdentityProvider = IdentityProvider.Local;
    user.ExternalProviderId = null;

    user.RaiseDomainEvent(new UserCreatedEvent(user.Id, email.Value, user.FullName));

    return Result<User>.Success(user);
}

// NEW method
public static Result<User> CreateFromExternalProvider(
    Email? email,
    string firstName,
    string lastName,
    IdentityProvider provider,
    string externalProviderId,
    UserRole role = UserRole.User)
{
    if (email == null)
        return Result<User>.Failure("Email is required");

    if (string.IsNullOrWhiteSpace(firstName))
        return Result<User>.Failure("First name is required");

    if (string.IsNullOrWhiteSpace(lastName))
        return Result<User>.Failure("Last name is required");

    if (provider == IdentityProvider.Local)
        return Result<User>.Failure("Use Create() method for local users. CreateFromExternalProvider is only for external identity providers.");

    if (string.IsNullOrWhiteSpace(externalProviderId))
        return Result<User>.Failure("External provider ID is required for external users");

    var user = new User(email, firstName.Trim(), lastName.Trim(), role)
    {
        IdentityProvider = provider,
        ExternalProviderId = externalProviderId.Trim(),
        IsEmailVerified = true,
        PasswordHash = null,
        EmailVerificationToken = null,
        EmailVerificationTokenExpiresAt = null,
        PasswordResetToken = null,
        PasswordResetTokenExpiresAt = null,
        FailedLoginAttempts = 0,
        AccountLockedUntil = null
    };

    user.RaiseDomainEvent(new UserCreatedFromExternalProviderEvent(
        user.Id, email.Value, user.FullName, provider, externalProviderId));

    return Result<User>.Success(user);
}
```

**Create Domain Event:**

**File:** `src/LankaConnect.Domain/Events/UserCreatedFromExternalProviderEvent.cs`

```csharp
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Domain.Events;

public record UserCreatedFromExternalProviderEvent(
    Guid UserId,
    string Email,
    string FullName,
    IdentityProvider Provider,
    string ExternalProviderId) : DomainEvent;
```

**Run Tests (Should PASS):**
```bash
dotnet test --filter "FullyQualifiedName~UserExternalProviderTests"
# Expected: All 5 tests pass
```

**Commit Message:**
```
feat(domain): Add CreateFromExternalProvider factory method to User

- Add IdentityProvider and ExternalProviderId properties
- Implement CreateFromExternalProvider for Entra users
- Add UserCreatedFromExternalProviderEvent
- External users are pre-verified (no email verification needed)
- 5 passing tests for external provider creation

TDD: Red-Green-Refactor cycle complete
Related: ADR-002-Entra-External-ID-Integration
```

---

### Step 1.3: Add Business Rule Enforcement
**Status:** ⚠️ Requires Tests First

#### TDD Cycle 3: Password Management Rules

**Add Tests:**

```csharp
// File: tests/LankaConnect.Domain.Tests/Users/UserPasswordManagementTests.cs
public class UserPasswordManagementTests
{
    [Fact]
    public void SetPassword_ForLocalUser_ShouldSucceed()
    {
        var user = User.Create(Email.Create("local@test.com").Value, "John", "Doe").Value;
        var result = user.SetPassword("hashed-password-12345");

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("hashed-password-12345");
    }

    [Fact]
    public void SetPassword_ForExternalUser_ShouldFail()
    {
        var user = User.CreateFromExternalProvider(
            Email.Create("entra@test.com").Value,
            "Jane", "Smith",
            IdentityProvider.EntraExternal,
            "entra-oid-123").Value;

        var result = user.SetPassword("hashed-password");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Cannot set password for EntraExternal users");
    }

    [Fact]
    public void ChangePassword_ForLocalUser_ShouldSucceed()
    {
        var user = User.Create(Email.Create("local@test.com").Value, "John", "Doe").Value;
        user.SetPassword("old-hash");

        var result = user.ChangePassword("new-hash");

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("new-hash");
    }

    [Fact]
    public void ChangePassword_ForExternalUser_ShouldFail()
    {
        var user = User.CreateFromExternalProvider(
            Email.Create("entra@test.com").Value,
            "Jane", "Smith",
            IdentityProvider.EntraExternal,
            "entra-oid-123").Value;

        var result = user.ChangePassword("new-hash");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("external provider");
    }

    [Fact]
    public void SetPasswordResetToken_ForLocalUser_ShouldSucceed()
    {
        var user = User.Create(Email.Create("local@test.com").Value, "John", "Doe").Value;
        user.SetPassword("hash");

        var result = user.SetPasswordResetToken("reset-token-123", DateTime.UtcNow.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        user.PasswordResetToken.Should().Be("reset-token-123");
    }

    [Fact]
    public void SetPasswordResetToken_ForExternalUser_ShouldFail()
    {
        var user = User.CreateFromExternalProvider(
            Email.Create("entra@test.com").Value,
            "Jane", "Smith",
            IdentityProvider.EntraExternal,
            "entra-oid-123").Value;

        var result = user.SetPasswordResetToken("token", DateTime.UtcNow.AddHours(1));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not supported for EntraExternal users");
    }
}
```

**Run Tests (Should FAIL):**
```bash
dotnet test --filter "FullyQualifiedName~UserPasswordManagementTests"
# Expected: Tests for external users should fail
```

#### Update Domain Methods:

```csharp
// File: src/LankaConnect.Domain/Users/User.cs

public Result SetPassword(string passwordHash)
{
    if (IdentityProvider != IdentityProvider.Local)
        return Result.Failure($"Cannot set password for {IdentityProvider} users. Passwords are managed by the external provider.");

    if (string.IsNullOrWhiteSpace(passwordHash))
        return Result.Failure("Password hash is required");

    PasswordHash = passwordHash;
    MarkAsUpdated();
    return Result.Success();
}

public Result ChangePassword(string newPasswordHash)
{
    if (IdentityProvider != IdentityProvider.Local)
        return Result.Failure($"Cannot change password for {IdentityProvider} users. Passwords are managed by the external provider.");

    if (string.IsNullOrWhiteSpace(newPasswordHash))
        return Result.Failure("Password hash is required");

    PasswordHash = newPasswordHash;
    PasswordResetToken = null;
    PasswordResetTokenExpiresAt = null;
    FailedLoginAttempts = 0;
    AccountLockedUntil = null;

    MarkAsUpdated();
    RaiseDomainEvent(new UserPasswordChangedEvent(Id, Email.Value));
    return Result.Success();
}

public Result SetPasswordResetToken(string token, DateTime expiresAt)
{
    if (IdentityProvider != IdentityProvider.Local)
        return Result.Failure($"Password reset is not supported for {IdentityProvider} users. Use your identity provider's password reset flow.");

    if (string.IsNullOrWhiteSpace(token))
        return Result.Failure("Reset token is required");

    if (expiresAt <= DateTime.UtcNow)
        return Result.Failure("Expiration date must be in the future");

    PasswordResetToken = token;
    PasswordResetTokenExpiresAt = expiresAt;
    MarkAsUpdated();
    return Result.Success();
}
```

**Run Tests (Should PASS):**
```bash
dotnet test --filter "FullyQualifiedName~UserPasswordManagementTests"
# Expected: All 6 tests pass
```

**Run ALL Domain Tests:**
```bash
cd tests/LankaConnect.Domain.Tests
dotnet test
# Expected: ALL tests pass (existing + new)
```

**Commit Message:**
```
feat(domain): Enforce password management rules by provider

- SetPassword only allowed for Local users
- ChangePassword only allowed for Local users
- SetPasswordResetToken only allowed for Local users
- External provider users get clear error messages
- 6 passing tests for password management rules

TDD: All existing tests still pass
Related: ADR-002-Entra-External-ID-Integration
```

---

### Step 1.4: Add Email Verification Rules

**Follow same TDD pattern:**
1. Write failing tests for `SetEmailVerificationToken` and `VerifyEmail`
2. Update methods with provider checks
3. Verify all tests pass
4. Commit with clear message

**Estimated Time:** 1 hour

---

### Step 1.5: Add Account Locking Rules

**Follow same TDD pattern:**
1. Write failing tests for `RecordFailedLoginAttempt` and `IsAccountLocked`
2. Update methods with provider checks
3. Verify all tests pass
4. Commit with clear message

**Estimated Time:** 1 hour

---

### Phase 1 Completion Checklist

- [ ] IdentityProvider enum added
- [ ] User entity has IdentityProvider and ExternalProviderId properties
- [ ] CreateFromExternalProvider factory method implemented
- [ ] UserCreatedFromExternalProviderEvent added
- [ ] Password management rules enforced (3 methods)
- [ ] Email verification rules enforced (2 methods)
- [ ] Account locking rules enforced (2 methods)
- [ ] **Total Tests:** 25+ passing
- [ ] **Test Coverage:** 95%+
- [ ] **All existing tests:** Still passing
- [ ] **Zero compilation errors**

**Final Verification:**
```bash
cd tests/LankaConnect.Domain.Tests
dotnet test --collect:"XPlat Code Coverage"
# Check coverage report: aim for 95%+

cd ../../src/LankaConnect.Domain
dotnet build
# Should compile without warnings
```

---

## Phase 2: Infrastructure Layer

### Step 2.1: Database Migration
**Estimated Time:** 2 hours

```bash
cd src/LankaConnect.Infrastructure

# Create migration
dotnet ef migrations add AddIdentityProviderSupport --context ApplicationDbContext

# Review generated migration
# Verify: Adds IdentityProvider and ExternalProviderId columns
# Verify: Makes PasswordHash nullable
# Verify: Adds check constraints

# Apply to local database
dotnet ef database update

# Run validation queries
```

**Commit:** Database migration for identity provider support

---

### Step 2.2: Repository Implementation
**TDD Steps:**

1. Add tests to `UserRepositoryTests.cs`
2. Implement `GetByExternalProviderIdAsync`
3. Implement `ExistsWithExternalProviderIdAsync`
4. Verify integration tests pass

**Estimated Time:** 3 hours

---

### Step 2.3: Entra External ID Service
**TDD Steps:**

1. Create `IExternalAuthenticationService` interface
2. Write unit tests with mock HTTP responses
3. Implement `EntraExternalIdService`
4. Implement token validation logic
5. Add integration tests with WireMock

**Estimated Time:** 6-8 hours

---

### Phase 2 Completion Checklist

- [ ] Database migration applied successfully
- [ ] EF Core configuration updated
- [ ] Repository methods implemented (2 new methods)
- [ ] EntraExternalIdService implemented
- [ ] Token validation works with test tokens
- [ ] **Total Tests:** 15+ integration tests
- [ ] **Test Coverage:** 90%+
- [ ] **All tests passing**

---

## Phase 3: Application Layer

### Step 3.1: LoginWithEntraCommand
**TDD Steps:**

1. Create command and response DTOs
2. Write failing handler tests
3. Implement `LoginWithEntraHandler`
4. Implement auto-provisioning logic
5. Verify command tests pass

**Estimated Time:** 4-6 hours

---

### Step 3.2: Update Existing Commands
**TDD Steps:**

1. Update `RegisterUserHandler` tests
2. Update `LoginUserHandler` to respect provider
3. Ensure backward compatibility
4. Verify all application tests pass

**Estimated Time:** 3-4 hours

---

### Phase 3 Completion Checklist

- [ ] LoginWithEntraCommand implemented
- [ ] Auto-provisioning logic working
- [ ] Domain events raised correctly
- [ ] Existing commands updated
- [ ] **Total Tests:** 30+ application tests
- [ ] **Test Coverage:** 95%+
- [ ] **All tests passing**

---

## Phase 4: Presentation Layer

### Step 4.1: Add Entra Login Endpoint
**TDD Steps:**

1. Write API integration tests
2. Add `/api/auth/login/entra` endpoint
3. Update Swagger documentation
4. Test with Postman/REST Client

**Estimated Time:** 3-4 hours

---

### Step 4.2: Update Existing Endpoints
**TDD Steps:**

1. Verify `/api/auth/login` still works
2. Verify `/api/auth/register` still works
3. Add `/api/auth/check-provider` endpoint (optional)
4. Update API documentation

**Estimated Time:** 2-3 hours

---

### Phase 4 Completion Checklist

- [ ] `/api/auth/login/entra` endpoint working
- [ ] Entra callback handled correctly
- [ ] Cookies set properly
- [ ] Error responses appropriate
- [ ] Swagger documentation updated
- [ ] **Total Tests:** 10+ E2E tests
- [ ] **Test Coverage:** 90%+
- [ ] **All tests passing**

---

## Phase 5: Integration & Deployment

### Step 5.1: E2E Testing
**Checklist:**

- [ ] Test complete Entra login flow
- [ ] Test auto-provisioning new users
- [ ] Test existing user login (both providers)
- [ ] Test token refresh flow
- [ ] Test error scenarios
- [ ] Performance testing

**Estimated Time:** 4-6 hours

---

### Step 5.2: Production Deployment
**Checklist:**

- [ ] Backup production database
- [ ] Apply database migration
- [ ] Deploy application code
- [ ] Run smoke tests
- [ ] Monitor error logs
- [ ] Verify metrics

**Estimated Time:** 3-4 hours

---

### Step 5.3: Documentation
**Checklist:**

- [ ] Update API documentation
- [ ] Create migration guide for users
- [ ] Document Entra configuration
- [ ] Update README
- [ ] Create troubleshooting guide

**Estimated Time:** 2-3 hours

---

## Phase 5 Completion Checklist

- [ ] E2E tests passing in staging
- [ ] Production deployment successful
- [ ] Zero downtime migration
- [ ] Monitoring dashboards configured
- [ ] Documentation complete
- [ ] Team trained on new flow

---

## Daily Checklist Template

Use this checklist at the end of each work session:

```markdown
## Daily Checkpoint: [Date]

### Work Completed
- [ ] Phase/Step completed: _______________
- [ ] Tests written: ___ (passing: ___ / failing: ___)
- [ ] Code coverage: ____%

### Code Quality
- [ ] Zero compilation errors
- [ ] All existing tests pass
- [ ] New tests added and passing
- [ ] Code reviewed (self or peer)
- [ ] Committed with clear message

### Blockers
- [ ] Any blockers encountered? ___________
- [ ] Decisions needed? __________________

### Next Session
- [ ] Next step to work on: ______________
- [ ] Estimated time: ___________________
```

---

## Risk Management

### High-Risk Areas

| Risk | Mitigation | Contingency |
|------|-----------|-------------|
| Database migration fails | Test in staging first, have rollback script | Rollback migration, restore backup |
| Entra token validation breaks | Cache JWKS, retry logic, fallback to local | Disable Entra endpoint, revert to local only |
| Existing tests fail | Maintain backward compatibility | Fix breaking changes before proceeding |
| Performance degradation | Index optimization, caching strategy | Add database indexes, tune queries |
| User confusion (dual auth) | Clear UI/UX, migration guide | Add `/check-provider` endpoint |

---

## Success Metrics

### Code Quality Metrics

- **Test Coverage:** > 90% across all layers
- **Build Success Rate:** 100% (zero broken builds)
- **Code Review Approval:** All commits peer-reviewed
- **Technical Debt:** Zero known issues

### Performance Metrics

- **Token Validation:** < 200ms (p95)
- **User Login:** < 500ms (p95)
- **Auto-provisioning:** < 1s (p95)
- **Database Migration:** < 10s

### Business Metrics

- **Zero Downtime:** Migration with no service interruption
- **User Impact:** Zero existing user authentication failures
- **Adoption Rate:** Track Entra vs Local signups
- **Support Tickets:** < 5 Entra-related tickets in first week

---

## Completion Criteria

### Definition of Done

For each phase to be considered complete:

1. ✅ All planned features implemented
2. ✅ All tests passing (unit + integration)
3. ✅ Code coverage targets met
4. ✅ Zero compilation errors/warnings
5. ✅ Code reviewed and approved
6. ✅ Documentation updated
7. ✅ Deployed to staging and tested
8. ✅ Performance benchmarks met
9. ✅ Security review passed
10. ✅ Team demo completed

---

## Appendix: Useful Commands

### Testing Commands

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~UserExternalProviderTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate coverage report
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report"
```

### Database Commands

```bash
# Create migration
dotnet ef migrations add MigrationName --context ApplicationDbContext

# Apply migration
dotnet ef database update

# Generate SQL script
dotnet ef migrations script --idempotent --output migration.sql

# Rollback migration
dotnet ef database update PreviousMigrationName
```

### Git Commands

```bash
# Commit with TDD message template
git commit -m "feat(domain): [description]

- Bullet point changes
- Test results: X passing
- TDD: Red-Green-Refactor complete

Related: ADR-002-Entra-External-ID-Integration"

# Create feature branch
git checkout -b feature/entra-external-id-phase-1

# Squash commits before merge
git rebase -i HEAD~5
```

---

**Roadmap Status:** ✅ Ready for Implementation
**Estimated Total Time:** 50-70 hours (5-7 weeks @ 10 hours/week)
**Next Action:** Begin Phase 1, Step 1.1
