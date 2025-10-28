# Entra External ID Integration - Domain Model Design

**Status:** Detailed Design
**Date:** 2025-10-28
**Related ADR:** ADR-002-Entra-External-ID-Integration

---

## Overview

This document provides detailed domain model design for integrating Microsoft Entra External ID with LankaConnect's existing authentication system while preserving Clean Architecture and DDD principles.

---

## Domain Layer Changes

### 1. Identity Provider Value Object

**File:** `src/LankaConnect.Domain/Users/Enums/IdentityProvider.cs`

```csharp
namespace LankaConnect.Domain.Users.Enums;

/// <summary>
/// Represents the authentication provider for a user account
/// </summary>
public enum IdentityProvider
{
    /// <summary>
    /// Local authentication with email/password stored in LankaConnect database
    /// </summary>
    Local = 0,

    /// <summary>
    /// Microsoft Entra External ID (formerly Azure AD B2C) authentication
    /// Supports social logins and enterprise federation
    /// </summary>
    EntraExternal = 1

    // Future: Google = 2, Facebook = 3, Apple = 4 (if not federated through Entra)
}
```

**Design Rationale:**
- Simple enum keeps domain model clean
- Extensible for future providers
- `Local = 0` ensures backward compatibility with existing users
- Explicit documentation for each provider

---

### 2. Enhanced User Aggregate

**File:** `src/LankaConnect.Domain/Users/User.cs`

#### **New Properties**

```csharp
public class User : BaseEntity
{
    // EXISTING properties (unchanged)
    public Email Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public PhoneNumber? PhoneNumber { get; private set; }
    public string? Bio { get; private set; }
    public bool IsActive { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // NEW: Identity provider tracking
    /// <summary>
    /// The authentication provider used to create and authenticate this user
    /// </summary>
    public IdentityProvider IdentityProvider { get; private set; }

    /// <summary>
    /// External identity provider's unique identifier for this user (e.g., Entra OID claim)
    /// Required for external users, null for local users
    /// </summary>
    public string? ExternalProviderId { get; private set; }

    // MODIFIED: Nullable for external provider users
    /// <summary>
    /// BCrypt hashed password. Required for Local users, null for external provider users
    /// </summary>
    public string? PasswordHash { get; private set; }

    // EXISTING: Authentication-related properties (only for Local users)
    public string? EmailVerificationToken { get; private set; }
    public DateTime? EmailVerificationTokenExpiresAt { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiresAt { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? AccountLockedUntil { get; private set; }

    // EXISTING: Refresh tokens (used by both Local and External users)
    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    // NEW: Computed properties
    public bool IsExternalUser => IdentityProvider != IdentityProvider.Local;
    public bool RequiresPasswordAuthentication => IdentityProvider == IdentityProvider.Local;
    public bool SupportsPasswordReset => IdentityProvider == IdentityProvider.Local;
    public bool SupportsEmailVerification => IdentityProvider == IdentityProvider.Local;
}
```

---

#### **Modified Factory Methods**

```csharp
// EXISTING: Local user creation (modified to set IdentityProvider)
public static Result<User> Create(
    Email? email,
    string firstName,
    string lastName,
    UserRole role = UserRole.User)
{
    if (email == null)
        return Result<User>.Failure("Email is required");

    if (string.IsNullOrWhiteSpace(firstName))
        return Result<User>.Failure("First name is required");

    if (string.IsNullOrWhiteSpace(lastName))
        return Result<User>.Failure("Last name is required");

    var user = new User(email, firstName.Trim(), lastName.Trim(), role);

    // NEW: Explicitly set identity provider
    user.IdentityProvider = IdentityProvider.Local;
    user.ExternalProviderId = null;

    // Raise domain event
    user.RaiseDomainEvent(new UserCreatedEvent(user.Id, email.Value, user.FullName));

    return Result<User>.Success(user);
}

// NEW: External provider user creation
public static Result<User> CreateFromExternalProvider(
    Email? email,
    string firstName,
    string lastName,
    IdentityProvider provider,
    string externalProviderId,
    UserRole role = UserRole.User)
{
    // Validation
    if (email == null)
        return Result<User>.Failure("Email is required");

    if (string.IsNullOrWhiteSpace(firstName))
        return Result<User>.Failure("First name is required");

    if (string.IsNullOrWhiteSpace(lastName))
        return Result<User>.Failure("Last name is required");

    if (provider == IdentityProvider.Local)
        return Result<User>.Failure(
            "Use Create() method for local users. CreateFromExternalProvider is only for external identity providers.");

    if (string.IsNullOrWhiteSpace(externalProviderId))
        return Result<User>.Failure("External provider ID is required for external users");

    // Create user
    var user = new User(email, firstName.Trim(), lastName.Trim(), role)
    {
        IdentityProvider = provider,
        ExternalProviderId = externalProviderId.Trim(),
        IsEmailVerified = true, // External providers handle email verification
        PasswordHash = null,    // No local password for external users
        EmailVerificationToken = null,
        EmailVerificationTokenExpiresAt = null,
        PasswordResetToken = null,
        PasswordResetTokenExpiresAt = null,
        FailedLoginAttempts = 0,
        AccountLockedUntil = null
    };

    // Raise specialized domain event
    user.RaiseDomainEvent(new UserCreatedFromExternalProviderEvent(
        user.Id,
        email.Value,
        user.FullName,
        provider,
        externalProviderId));

    return Result<User>.Success(user);
}
```

---

#### **Modified Authentication Methods**

```csharp
// MODIFIED: SetPassword - validate identity provider
public Result SetPassword(string passwordHash)
{
    // NEW: Business rule - external users cannot have local passwords
    if (IdentityProvider != IdentityProvider.Local)
        return Result.Failure(
            $"Cannot set password for {IdentityProvider} users. Passwords are managed by the external provider.");

    if (string.IsNullOrWhiteSpace(passwordHash))
        return Result.Failure("Password hash is required");

    PasswordHash = passwordHash;
    MarkAsUpdated();
    return Result.Success();
}

// MODIFIED: ChangePassword - validate identity provider
public Result ChangePassword(string newPasswordHash)
{
    // NEW: Business rule enforcement
    if (IdentityProvider != IdentityProvider.Local)
        return Result.Failure(
            $"Cannot change password for {IdentityProvider} users. Passwords are managed by the external provider.");

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

// MODIFIED: SetPasswordResetToken - validate identity provider
public Result SetPasswordResetToken(string token, DateTime expiresAt)
{
    // NEW: Business rule enforcement
    if (IdentityProvider != IdentityProvider.Local)
        return Result.Failure(
            $"Password reset is not supported for {IdentityProvider} users. Use your identity provider's password reset flow.");

    if (string.IsNullOrWhiteSpace(token))
        return Result.Failure("Reset token is required");

    if (expiresAt <= DateTime.UtcNow)
        return Result.Failure("Expiration date must be in the future");

    PasswordResetToken = token;
    PasswordResetTokenExpiresAt = expiresAt;
    MarkAsUpdated();
    return Result.Success();
}

// MODIFIED: SetEmailVerificationToken - validate identity provider
public Result SetEmailVerificationToken(string token, DateTime expiresAt)
{
    // NEW: Business rule enforcement
    if (IdentityProvider != IdentityProvider.Local)
        return Result.Failure(
            $"Email verification is not supported for {IdentityProvider} users. Email is pre-verified by the external provider.");

    if (string.IsNullOrWhiteSpace(token))
        return Result.Failure("Verification token is required");

    if (expiresAt <= DateTime.UtcNow)
        return Result.Failure("Expiration date must be in the future");

    EmailVerificationToken = token;
    EmailVerificationTokenExpiresAt = expiresAt;
    MarkAsUpdated();
    return Result.Success();
}

// MODIFIED: VerifyEmail - validate identity provider
public Result VerifyEmail()
{
    // NEW: Business rule - external users are pre-verified
    if (IdentityProvider != IdentityProvider.Local)
        return Result.Failure(
            $"Email verification is not applicable for {IdentityProvider} users. Email is verified by the external provider.");

    if (IsEmailVerified)
        return Result.Failure("Email is already verified");

    IsEmailVerified = true;
    EmailVerificationToken = null;
    EmailVerificationTokenExpiresAt = null;
    MarkAsUpdated();

    RaiseDomainEvent(new UserEmailVerifiedEvent(Id, Email.Value));
    return Result.Success();
}

// MODIFIED: RecordFailedLoginAttempt - only for local users
public Result RecordFailedLoginAttempt()
{
    // NEW: Business rule - failed login tracking only for local users
    if (IdentityProvider != IdentityProvider.Local)
        return Result.Failure(
            $"Failed login tracking is not supported for {IdentityProvider} users. Authentication is handled by the external provider.");

    FailedLoginAttempts++;

    // Lock account after 5 failed attempts for 30 minutes
    if (FailedLoginAttempts >= 5)
    {
        AccountLockedUntil = DateTime.UtcNow.AddMinutes(30);
        RaiseDomainEvent(new UserAccountLockedEvent(Id, Email.Value, AccountLockedUntil.Value));
    }

    MarkAsUpdated();
    return Result.Success();
}

// MODIFIED: RecordSuccessfulLogin - works for all users
public Result RecordSuccessfulLogin()
{
    // Only clear local auth state for local users
    if (IdentityProvider == IdentityProvider.Local)
    {
        FailedLoginAttempts = 0;
        AccountLockedUntil = null;
    }

    LastLoginAt = DateTime.UtcNow;
    MarkAsUpdated();

    RaiseDomainEvent(new UserLoggedInEvent(Id, Email.Value, LastLoginAt.Value));
    return Result.Success();
}

// MODIFIED: IsAccountLocked - only applicable to local users
public bool IsAccountLocked
{
    get
    {
        // External users are never locked locally (handled by provider)
        if (IdentityProvider != IdentityProvider.Local)
            return false;

        return AccountLockedUntil.HasValue && AccountLockedUntil.Value > DateTime.UtcNow;
    }
}
```

---

#### **NEW: External Provider Management Methods**

```csharp
/// <summary>
/// Links an external provider to an existing local user (account conversion)
/// </summary>
public Result LinkExternalProvider(IdentityProvider provider, string externalProviderId)
{
    if (provider == IdentityProvider.Local)
        return Result.Failure("Cannot link Local provider");

    if (string.IsNullOrWhiteSpace(externalProviderId))
        return Result.Failure("External provider ID is required");

    if (IdentityProvider != IdentityProvider.Local)
        return Result.Failure(
            $"User already uses {IdentityProvider}. Cannot link multiple external providers.");

    // Convert local user to external provider user
    IdentityProvider = provider;
    ExternalProviderId = externalProviderId.Trim();

    // Clear local authentication state
    PasswordHash = null;
    EmailVerificationToken = null;
    EmailVerificationTokenExpiresAt = null;
    PasswordResetToken = null;
    PasswordResetTokenExpiresAt = null;
    FailedLoginAttempts = 0;
    AccountLockedUntil = null;
    IsEmailVerified = true; // External provider verified

    MarkAsUpdated();
    RaiseDomainEvent(new UserProviderLinkedEvent(Id, Email.Value, provider, externalProviderId));
    return Result.Success();
}

/// <summary>
/// Synchronizes user profile data from external provider
/// </summary>
public Result SyncFromExternalProvider(string? firstName, string? lastName, bool? isEmailVerified)
{
    if (IdentityProvider == IdentityProvider.Local)
        return Result.Failure("Sync is only applicable for external provider users");

    bool hasChanges = false;

    if (!string.IsNullOrWhiteSpace(firstName) && FirstName != firstName.Trim())
    {
        FirstName = firstName.Trim();
        hasChanges = true;
    }

    if (!string.IsNullOrWhiteSpace(lastName) && LastName != lastName.Trim())
    {
        LastName = lastName.Trim();
        hasChanges = true;
    }

    if (isEmailVerified.HasValue && IsEmailVerified != isEmailVerified.Value)
    {
        IsEmailVerified = isEmailVerified.Value;
        hasChanges = true;
    }

    if (hasChanges)
    {
        MarkAsUpdated();
        RaiseDomainEvent(new UserProfileSyncedEvent(Id, Email.Value, IdentityProvider));
    }

    return Result.Success();
}
```

---

### 3. New Domain Events

**File:** `src/LankaConnect.Domain/Events/UserCreatedFromExternalProviderEvent.cs`

```csharp
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Raised when a user is created from an external identity provider (auto-provisioned)
/// </summary>
public record UserCreatedFromExternalProviderEvent(
    Guid UserId,
    string Email,
    string FullName,
    IdentityProvider Provider,
    string ExternalProviderId) : DomainEvent;
```

**File:** `src/LankaConnect.Domain/Events/UserProviderLinkedEvent.cs`

```csharp
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Raised when an existing local user links an external identity provider
/// </summary>
public record UserProviderLinkedEvent(
    Guid UserId,
    string Email,
    IdentityProvider Provider,
    string ExternalProviderId) : DomainEvent;
```

**File:** `src/LankaConnect.Domain/Events/UserProfileSyncedEvent.cs`

```csharp
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Raised when user profile data is synchronized from external provider
/// </summary>
public record UserProfileSyncedEvent(
    Guid UserId,
    string Email,
    IdentityProvider Provider) : DomainEvent;
```

---

### 4. Repository Interface Extension

**File:** `src/LankaConnect.Domain/Users/IUserRepository.cs`

```csharp
// ADD these methods to existing interface

/// <summary>
/// Gets a user by their external provider ID
/// </summary>
Task<User?> GetByExternalProviderIdAsync(
    IdentityProvider provider,
    string externalProviderId,
    CancellationToken cancellationToken = default);

/// <summary>
/// Checks if an external provider ID is already linked to a user
/// </summary>
Task<bool> ExistsWithExternalProviderIdAsync(
    IdentityProvider provider,
    string externalProviderId,
    CancellationToken cancellationToken = default);

/// <summary>
/// Gets all users using a specific identity provider
/// </summary>
Task<IEnumerable<User>> GetByIdentityProviderAsync(
    IdentityProvider provider,
    CancellationToken cancellationToken = default);
```

---

## Domain Invariants and Business Rules

### Invariants (Always True)

1. **User must have exactly one identity provider**
   - `IdentityProvider` is required (non-nullable)
   - Cannot have multiple providers simultaneously

2. **Local users must have PasswordHash**
   - `IF IdentityProvider == Local THEN PasswordHash != null`

3. **External users must have ExternalProviderId**
   - `IF IdentityProvider != Local THEN ExternalProviderId != null`

4. **External users cannot have PasswordHash**
   - `IF IdentityProvider != Local THEN PasswordHash == null`

5. **External users are pre-verified**
   - `IF IdentityProvider != Local THEN IsEmailVerified == true`

6. **ExternalProviderId is unique per provider**
   - `(IdentityProvider, ExternalProviderId)` is unique constraint

### Business Rules

| Operation | Local Users | External Users |
|-----------|-------------|----------------|
| Set Password | ✅ Allowed | ❌ Rejected |
| Change Password | ✅ Allowed | ❌ Rejected |
| Request Password Reset | ✅ Allowed | ❌ Rejected |
| Email Verification | ✅ Allowed | ❌ Not needed (pre-verified) |
| Failed Login Tracking | ✅ Tracked | ❌ Handled by provider |
| Account Locking | ✅ Enforced | ❌ Handled by provider |
| Profile Sync | ❌ Not applicable | ✅ From provider |

---

## Domain Validation

### TDD Test Cases (To Be Implemented)

#### **User Creation Tests**

```csharp
[Fact] public void Create_LocalUser_ShouldSetProviderToLocal()
[Fact] public void CreateFromExternalProvider_WithValidData_ShouldSucceed()
[Fact] public void CreateFromExternalProvider_WithLocalProvider_ShouldFail()
[Fact] public void CreateFromExternalProvider_WithoutExternalId_ShouldFail()
[Fact] public void CreateFromExternalProvider_ShouldSetEmailVerifiedToTrue()
[Fact] public void CreateFromExternalProvider_ShouldSetPasswordHashToNull()
```

#### **Password Management Tests**

```csharp
[Fact] public void SetPassword_ForLocalUser_ShouldSucceed()
[Fact] public void SetPassword_ForExternalUser_ShouldFail()
[Fact] public void ChangePassword_ForLocalUser_ShouldSucceed()
[Fact] public void ChangePassword_ForExternalUser_ShouldFail()
[Fact] public void SetPasswordResetToken_ForLocalUser_ShouldSucceed()
[Fact] public void SetPasswordResetToken_ForExternalUser_ShouldFail()
```

#### **Email Verification Tests**

```csharp
[Fact] public void SetEmailVerificationToken_ForLocalUser_ShouldSucceed()
[Fact] public void SetEmailVerificationToken_ForExternalUser_ShouldFail()
[Fact] public void VerifyEmail_ForLocalUser_ShouldSucceed()
[Fact] public void VerifyEmail_ForExternalUser_ShouldFail()
[Fact] public void CreateFromExternalProvider_ShouldNotRequireEmailVerification()
```

#### **Account Locking Tests**

```csharp
[Fact] public void RecordFailedLoginAttempt_ForLocalUser_ShouldIncrement()
[Fact] public void RecordFailedLoginAttempt_ForExternalUser_ShouldFail()
[Fact] public void IsAccountLocked_ForLocalUser_ShouldReturnTrue_WhenLocked()
[Fact] public void IsAccountLocked_ForExternalUser_ShouldAlwaysReturnFalse()
```

#### **Provider Linking Tests**

```csharp
[Fact] public void LinkExternalProvider_ForLocalUser_ShouldSucceed()
[Fact] public void LinkExternalProvider_ForExternalUser_ShouldFail()
[Fact] public void LinkExternalProvider_ShouldClearLocalAuthState()
[Fact] public void LinkExternalProvider_ShouldRaiseProviderLinkedEvent()
```

#### **Profile Sync Tests**

```csharp
[Fact] public void SyncFromExternalProvider_ForExternalUser_ShouldUpdateProfile()
[Fact] public void SyncFromExternalProvider_ForLocalUser_ShouldFail()
[Fact] public void SyncFromExternalProvider_WhenNoChanges_ShouldNotRaiseEvent()
```

---

## Database Schema Impact

### EF Core Entity Configuration

**File:** `src/LankaConnect.Infrastructure/Data/Configurations/UserConfiguration.cs`

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // ... existing configuration ...

        // Identity Provider configuration
        builder.Property(u => u.IdentityProvider)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(IdentityProvider.Local)
            .HasComment("0=Local, 1=EntraExternal");

        builder.Property(u => u.ExternalProviderId)
            .HasMaxLength(255)
            .IsRequired(false)
            .HasComment("External identity provider's unique ID (e.g., Entra OID)");

        // Password hash is nullable for external users
        builder.Property(u => u.PasswordHash)
            .IsRequired(false)
            .HasComment("BCrypt password hash. Required for Local users, null for external users");

        // Indexes for performance
        builder.HasIndex(u => new { u.IdentityProvider, u.ExternalProviderId })
            .HasDatabaseName("IX_Users_IdentityProvider_ExternalProviderId")
            .HasFilter("[ExternalProviderId] IS NOT NULL");

        builder.HasIndex(u => u.ExternalProviderId)
            .IsUnique()
            .HasDatabaseName("IX_Users_ExternalProviderId_Unique")
            .HasFilter("[ExternalProviderId] IS NOT NULL");

        // Email uniqueness per provider (optional - discuss with team)
        builder.HasIndex(u => new { u.Email, u.IdentityProvider })
            .IsUnique()
            .HasDatabaseName("IX_Users_Email_IdentityProvider_Unique");
    }
}
```

---

## Migration Path

### Existing Users

All existing users in the database will be automatically set to:
- `IdentityProvider = 0` (Local)
- `ExternalProviderId = null`
- `PasswordHash` remains unchanged

### Data Migration SQL

```sql
-- Set default identity provider for existing users
UPDATE Users
SET IdentityProvider = 0, ExternalProviderId = NULL
WHERE IdentityProvider IS NULL;

-- Verify all local users have password hashes
SELECT COUNT(*) FROM Users
WHERE IdentityProvider = 0 AND PasswordHash IS NULL;
-- Should return 0

-- Verify no duplicate external IDs
SELECT ExternalProviderId, COUNT(*)
FROM Users
WHERE ExternalProviderId IS NOT NULL
GROUP BY ExternalProviderId
HAVING COUNT(*) > 1;
-- Should return 0 rows
```

---

## Performance Considerations

### Indexing Strategy

1. **Primary Lookups:**
   - Email + IdentityProvider (unique constraint)
   - ExternalProviderId (unique constraint, filtered index)

2. **Query Patterns:**
   - Get user by email (existing index)
   - Get user by external provider ID (new index)
   - List users by provider (new index)

### Estimated Query Performance

```sql
-- Existing query (no change)
SELECT * FROM Users WHERE Email = 'user@example.com';
-- Uses: IX_Users_Email (existing)

-- New query pattern
SELECT * FROM Users
WHERE IdentityProvider = 1 AND ExternalProviderId = 'entra-oid-12345';
-- Uses: IX_Users_IdentityProvider_ExternalProviderId (new)

-- Provider listing query
SELECT * FROM Users WHERE IdentityProvider = 1;
-- Uses: IX_Users_IdentityProvider_ExternalProviderId (new, covering)
```

---

## Security Considerations

### Domain-Level Security

1. **Immutable Identity Provider**
   - Once set, `IdentityProvider` can only change via `LinkExternalProvider()`
   - Prevents accidental provider switching

2. **Password Protection**
   - External users cannot have local passwords
   - Domain enforces this at business logic level

3. **Token Validation**
   - Domain layer is agnostic to token validation (handled in Infrastructure)
   - Domain only validates business rules

4. **Audit Trail**
   - All provider changes raise domain events
   - Events captured for compliance/auditing

---

## Next Steps

1. **Implementation Order (TDD):**
   1. Add `IdentityProvider` enum (compile, no tests)
   2. Add properties to User entity (compile error: need tests)
   3. Write failing tests for `CreateFromExternalProvider()`
   4. Implement `CreateFromExternalProvider()` (tests pass)
   5. Write failing tests for password management rules
   6. Update password methods with provider checks (tests pass)
   7. Repeat for all methods

2. **Code Review Checkpoints:**
   - After enum and properties added
   - After factory methods implemented
   - After all authentication methods updated
   - After domain events added

3. **Integration Points:**
   - Infrastructure layer: Repository implementation
   - Application layer: Command handlers
   - Presentation layer: API endpoints

---

## Appendix: Complete User Entity Signature

```csharp
public class User : BaseEntity
{
    // Identity
    public Email Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string FullName => $"{FirstName} {LastName}";

    // Profile
    public PhoneNumber? PhoneNumber { get; private set; }
    public string? Bio { get; private set; }
    public bool IsActive { get; private set; }
    public UserRole Role { get; private set; }

    // Authentication - Provider Info
    public IdentityProvider IdentityProvider { get; private set; }
    public string? ExternalProviderId { get; private set; }
    public string? PasswordHash { get; private set; }

    // Authentication - Email Verification (Local only)
    public bool IsEmailVerified { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public DateTime? EmailVerificationTokenExpiresAt { get; private set; }

    // Authentication - Password Reset (Local only)
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiresAt { get; private set; }

    // Authentication - Account Security (Local only)
    public int FailedLoginAttempts { get; private set; }
    public DateTime? AccountLockedUntil { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    // Refresh Tokens (Both providers)
    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    // Computed Properties
    public bool IsExternalUser => IdentityProvider != IdentityProvider.Local;
    public bool RequiresPasswordAuthentication => IdentityProvider == IdentityProvider.Local;
    public bool SupportsPasswordReset => IdentityProvider == IdentityProvider.Local;
    public bool SupportsEmailVerification => IdentityProvider == IdentityProvider.Local;
    public bool IsAccountLocked => IdentityProvider == IdentityProvider.Local
        && AccountLockedUntil.HasValue
        && AccountLockedUntil.Value > DateTime.UtcNow;

    // Factory Methods
    public static Result<User> Create(Email email, string firstName, string lastName, UserRole role = UserRole.User);
    public static Result<User> CreateFromExternalProvider(Email email, string firstName, string lastName, IdentityProvider provider, string externalProviderId, UserRole role = UserRole.User);

    // Profile Management
    public Result UpdateProfile(string firstName, string lastName, PhoneNumber? phoneNumber, string? bio);
    public Result ChangeEmail(Email newEmail);
    public void Activate();
    public void Deactivate();

    // Password Management (Local only)
    public Result SetPassword(string passwordHash);
    public Result ChangePassword(string newPasswordHash);
    public Result SetPasswordResetToken(string token, DateTime expiresAt);
    public bool IsPasswordResetTokenValid(string token);

    // Email Verification (Local only)
    public Result SetEmailVerificationToken(string token, DateTime expiresAt);
    public Result VerifyEmail();
    public bool IsEmailVerificationTokenValid(string token);

    // Login Management
    public Result RecordFailedLoginAttempt(); // Local only
    public Result RecordSuccessfulLogin(); // Both providers

    // Refresh Token Management (Both providers)
    public Result AddRefreshToken(RefreshToken refreshToken);
    public Result RevokeRefreshToken(string token, string? revokedByIp = null);
    public void RevokeAllRefreshTokens(string? revokedByIp = null);
    public RefreshToken? GetRefreshToken(string token);

    // Role Management
    public Result ChangeRole(UserRole newRole);

    // External Provider Management
    public Result LinkExternalProvider(IdentityProvider provider, string externalProviderId);
    public Result SyncFromExternalProvider(string? firstName, string? lastName, bool? isEmailVerified);
}
```

---

**Review Status:** ✅ Ready for Implementation
**Test Coverage Target:** 95% for User entity
**Estimated Implementation Time:** 8-12 hours (TDD approach)
