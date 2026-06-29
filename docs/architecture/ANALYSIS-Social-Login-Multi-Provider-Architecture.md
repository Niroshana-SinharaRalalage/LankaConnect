# ADR-003: Social Login Multi-Provider Architecture (Epic 1 Phase 2)

**Status:** Proposed
**Date:** 2025-11-01
**Author:** System Architecture Designer
**Supersedes:** ADR-002 (Microsoft Entra External ID Integration)
**Epic:** Epic 1 Phase 2 - Social Login (Facebook, Google, Apple)

---

## Context and Problem Statement

LankaConnect has successfully implemented Microsoft Entra External ID authentication (Epic 1 Phase 1) with:
- `IdentityProvider` enum (Local = 0, EntraExternal = 1)
- Single `ExternalProviderId` string for storing Entra OID claims
- `User.CreateFromExternalProvider()` factory method
- Auto-provisioning via `LoginWithEntraCommand`

**Epic 1 Phase 2 Requirements:**
1. Add **Facebook, Google, and Apple** as social login providers
2. Azure Entra External ID **federates** these providers (not direct integration)
3. Support **multiple social accounts linked to one user** (account linking)
4. Enable **unlinking** social providers from accounts
5. Prevent **account hijacking** during linking operations
6. Handle users with **no remaining authentication methods** after unlinking

**Key Constraint:** Azure Entra External ID acts as the **federation hub**, so all social logins flow through Entra with an `idp` claim identifying the source provider.

---

## Decision Drivers

1. **Entra Federation Architecture** - All social providers federate through Entra External ID
2. **Multi-Provider Per User** - Users can link Facebook + Google + Apple to one account
3. **Account Linking Security** - Prevent unauthorized linking/unlinking
4. **Clean Architecture Compliance** - DDD aggregates, domain events, CQRS
5. **Backward Compatibility** - Preserve existing Entra users from Phase 1
6. **Data Integrity** - Prevent orphaned users with no authentication method

---

## Recommended Architecture

### 1. Domain Model Design (DDD Patterns)

#### **Option A: Keep Single IdentityProvider Enum + Track Federation Source (RECOMMENDED)**

**Rationale:** Since Azure Entra External ID federates all social providers, the authentication **still goes through Entra**. The `idp` claim in Entra tokens tells us which social provider was used.

```csharp
// Domain/Users/Enums/IdentityProvider.cs
public enum IdentityProvider
{
    Local = 0,           // Email/password in LankaConnect database
    EntraExternal = 1    // All Entra-federated providers (Microsoft, Facebook, Google, Apple)
}

// Extensions remain the same - no changes needed
public static class IdentityProviderExtensions
{
    public static bool IsExternalProvider(this IdentityProvider provider)
        => provider == IdentityProvider.EntraExternal;

    public static bool RequiresPasswordHash(this IdentityProvider provider)
        => provider == IdentityProvider.Local;
}
```

**Key Insight:** We don't need `Facebook = 2, Google = 3, Apple = 4` because **Entra is the actual identity provider**. The social provider is just the **upstream federation source**.

---

#### **ExternalLogin Value Object Collection (NEW)**

To support multiple linked social accounts, introduce a **value object collection** instead of a single `ExternalProviderId` string:

```csharp
// Domain/Users/ValueObjects/ExternalLogin.cs
namespace LankaConnect.Domain.Users.ValueObjects;

/// <summary>
/// Represents a linked external authentication provider account.
/// Supports multiple social logins per user (Facebook, Google, Apple via Entra federation).
/// </summary>
public class ExternalLogin : ValueObject
{
    /// <summary>
    /// The social provider that was federated through Entra External ID.
    /// Extracted from the 'idp' claim in Entra tokens.
    /// Examples: "facebook.com", "google.com", "apple.com", "microsoft.com"
    /// </summary>
    public FederatedProvider Provider { get; private set; }

    /// <summary>
    /// The unique user identifier from Entra External ID (OID claim).
    /// This is Entra's internal ID, not the social provider's native ID.
    /// </summary>
    public string ExternalProviderId { get; private set; }

    /// <summary>
    /// When this external login was first linked to the user account.
    /// </summary>
    public DateTime LinkedAt { get; private set; }

    /// <summary>
    /// Email address from the social provider (for audit trail).
    /// May differ from User.Email if user changed their primary email.
    /// </summary>
    public string ProviderEmail { get; private set; }

    // EF Core constructor
    private ExternalLogin()
    {
        Provider = FederatedProvider.Microsoft;
        ExternalProviderId = null!;
        ProviderEmail = null!;
    }

    private ExternalLogin(
        FederatedProvider provider,
        string externalProviderId,
        string providerEmail,
        DateTime linkedAt)
    {
        Provider = provider;
        ExternalProviderId = externalProviderId;
        ProviderEmail = providerEmail;
        LinkedAt = linkedAt;
    }

    public static Result<ExternalLogin> Create(
        FederatedProvider provider,
        string? externalProviderId,
        string? providerEmail)
    {
        if (string.IsNullOrWhiteSpace(externalProviderId))
            return Result<ExternalLogin>.Failure("External provider ID is required");

        if (string.IsNullOrWhiteSpace(providerEmail))
            return Result<ExternalLogin>.Failure("Provider email is required");

        return Result<ExternalLogin>.Success(new ExternalLogin(
            provider,
            externalProviderId.Trim(),
            providerEmail.Trim(),
            DateTime.UtcNow));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Provider;
        yield return ExternalProviderId;
    }
}

/// <summary>
/// Social providers federated through Azure Entra External ID.
/// Maps to 'idp' claim values in Entra tokens.
/// </summary>
public enum FederatedProvider
{
    Microsoft = 0,   // idp: "live.com" or "login.microsoftonline.com"
    Facebook = 1,    // idp: "facebook.com"
    Google = 2,      // idp: "google.com"
    Apple = 3        // idp: "appleid.apple.com"
}

public static class FederatedProviderExtensions
{
    public static string ToIdpClaimValue(this FederatedProvider provider)
    {
        return provider switch
        {
            FederatedProvider.Microsoft => "login.microsoftonline.com",
            FederatedProvider.Facebook => "facebook.com",
            FederatedProvider.Google => "google.com",
            FederatedProvider.Apple => "appleid.apple.com",
            _ => throw new ArgumentOutOfRangeException(nameof(provider))
        };
    }

    public static FederatedProvider FromIdpClaimValue(string idpClaim)
    {
        return idpClaim.ToLowerInvariant() switch
        {
            "login.microsoftonline.com" => FederatedProvider.Microsoft,
            "live.com" => FederatedProvider.Microsoft,
            "facebook.com" => FederatedProvider.Facebook,
            "google.com" => FederatedProvider.Google,
            "appleid.apple.com" => FederatedProvider.Apple,
            _ => throw new ArgumentException($"Unknown federated provider: {idpClaim}", nameof(idpClaim))
        };
    }

    public static string ToDisplayName(this FederatedProvider provider)
    {
        return provider switch
        {
            FederatedProvider.Microsoft => "Microsoft",
            FederatedProvider.Facebook => "Facebook",
            FederatedProvider.Google => "Google",
            FederatedProvider.Apple => "Apple",
            _ => provider.ToString()
        };
    }
}
```

---

#### **Updated User Aggregate**

```csharp
// Domain/Users/User.cs
public class User : BaseEntity
{
    // EXISTING - No changes
    public IdentityProvider IdentityProvider { get; private set; }
    public string? PasswordHash { get; private set; }

    // DEPRECATED - Remove in Phase 2 migration
    // Will be migrated to ExternalLogins collection
    public string? ExternalProviderId { get; private set; }

    // NEW - Collection of linked social accounts
    private readonly List<ExternalLogin> _externalLogins = new();
    public IReadOnlyCollection<ExternalLogin> ExternalLogins => _externalLogins.AsReadOnly();

    // ... existing properties ...

    /// <summary>
    /// Links a new social provider to this user account.
    /// Business Rules:
    /// - Cannot link the same provider twice (same Provider + ExternalProviderId)
    /// - External users can have multiple linked providers
    /// - Local users can link providers while keeping password auth
    /// </summary>
    public Result LinkExternalProvider(
        FederatedProvider provider,
        string externalProviderId,
        string providerEmail)
    {
        var loginResult = ExternalLogin.Create(provider, externalProviderId, providerEmail);
        if (loginResult.IsFailure)
            return Result.Failure(loginResult.Errors);

        var externalLogin = loginResult.Value;

        // Business Rule: Prevent duplicate provider linking
        if (_externalLogins.Any(el => el.Provider == provider && el.ExternalProviderId == externalProviderId))
            return Result.Failure($"{provider.ToDisplayName()} account is already linked to this user");

        _externalLogins.Add(externalLogin);
        MarkAsUpdated();

        RaiseDomainEvent(new ExternalProviderLinkedEvent(
            Id,
            Email.Value,
            provider,
            externalProviderId,
            providerEmail));

        return Result.Success();
    }

    /// <summary>
    /// Unlinks a social provider from this user account.
    /// Business Rules:
    /// - Cannot unlink the last authentication method (must keep password OR at least one provider)
    /// - Local users must keep password authentication
    /// </summary>
    public Result UnlinkExternalProvider(FederatedProvider provider, string externalProviderId)
    {
        var externalLogin = _externalLogins.FirstOrDefault(el =>
            el.Provider == provider && el.ExternalProviderId == externalProviderId);

        if (externalLogin == null)
            return Result.Failure($"{provider.ToDisplayName()} account is not linked to this user");

        // Business Rule: Prevent removing last authentication method
        bool hasPassword = !string.IsNullOrEmpty(PasswordHash);
        bool hasOtherProviders = _externalLogins.Count > 1;

        if (!hasPassword && !hasOtherProviders)
            return Result.Failure("Cannot unlink the last authentication method. Please add a password or link another provider first.");

        _externalLogins.Remove(externalLogin);
        MarkAsUpdated();

        RaiseDomainEvent(new ExternalProviderUnlinkedEvent(
            Id,
            Email.Value,
            provider,
            externalProviderId));

        return Result.Success();
    }

    /// <summary>
    /// Checks if a specific social provider is linked to this user.
    /// </summary>
    public bool HasLinkedProvider(FederatedProvider provider)
    {
        return _externalLogins.Any(el => el.Provider == provider);
    }

    /// <summary>
    /// Gets the external login for a specific provider and external ID.
    /// Used for authentication lookup.
    /// </summary>
    public ExternalLogin? GetExternalLogin(FederatedProvider provider, string externalProviderId)
    {
        return _externalLogins.FirstOrDefault(el =>
            el.Provider == provider && el.ExternalProviderId == externalProviderId);
    }

    /// <summary>
    /// Creates a user from an external provider (Phase 1 compatibility + Phase 2 enhancement).
    /// Auto-creates the first ExternalLogin in the collection.
    /// </summary>
    public static Result<User> CreateFromExternalProvider(
        IdentityProvider identityProvider,
        string? externalProviderId,
        Email? email,
        string firstName,
        string lastName,
        FederatedProvider federatedProvider,
        string providerEmail,
        UserRole role = UserRole.User)
    {
        if (identityProvider != IdentityProvider.EntraExternal)
            return Result<User>.Failure("Only EntraExternal identity provider is supported for external users");

        if (email == null)
            return Result<User>.Failure("Email is required");

        if (string.IsNullOrWhiteSpace(externalProviderId))
            return Result<User>.Failure("External provider ID is required");

        if (string.IsNullOrWhiteSpace(firstName))
            return Result<User>.Failure("First name is required");

        if (string.IsNullOrWhiteSpace(lastName))
            return Result<User>.Failure("Last name is required");

        var user = new User(email, firstName.Trim(), lastName.Trim(), role)
        {
            IdentityProvider = identityProvider,
            IsEmailVerified = true,
            PasswordHash = null
        };

        // Create first external login
        var loginResult = ExternalLogin.Create(federatedProvider, externalProviderId, providerEmail);
        if (loginResult.IsFailure)
            return Result<User>.Failure(loginResult.Errors);

        user._externalLogins.Add(loginResult.Value);

        user.RaiseDomainEvent(new UserCreatedFromExternalProviderEvent(
            user.Id,
            email.Value,
            user.FullName,
            identityProvider,
            federatedProvider,
            externalProviderId));

        return Result<User>.Success(user);
    }

    /// <summary>
    /// Determines if user can authenticate (has password OR external provider).
    /// </summary>
    public bool CanAuthenticate => !string.IsNullOrEmpty(PasswordHash) || _externalLogins.Any();
}
```

---

### 2. Database Schema Design

#### **Option A: Junction Table for Multiple Providers (RECOMMENDED)**

**Rationale:** Clean relational design, supports multiple providers per user, efficient indexing.

```sql
-- Migration: AddSocialLoginSupport (Phase 2)

-- Create external_logins junction table
CREATE TABLE "users"."external_logins" (
    "id" SERIAL PRIMARY KEY,
    "user_id" UUID NOT NULL,
    "provider" INT NOT NULL,  -- FederatedProvider enum (0=Microsoft, 1=Facebook, 2=Google, 3=Apple)
    "external_provider_id" VARCHAR(255) NOT NULL,  -- Entra OID claim
    "provider_email" VARCHAR(255) NOT NULL,
    "linked_at" TIMESTAMP NOT NULL DEFAULT NOW(),
    "created_at" TIMESTAMP NOT NULL DEFAULT NOW(),

    -- Foreign key to users table
    CONSTRAINT "fk_external_logins_users"
        FOREIGN KEY ("user_id") REFERENCES "users"."users"("id")
        ON DELETE CASCADE,

    -- Unique constraint: One provider per external ID (prevent duplicate linking)
    CONSTRAINT "uk_external_logins_provider_external_id"
        UNIQUE ("provider", "external_provider_id")
);

-- Index for user lookup by social provider
CREATE INDEX "ix_external_logins_user_id"
ON "users"."external_logins"("user_id");

-- Index for authentication lookup (find user by social login)
CREATE INDEX "ix_external_logins_provider_external_id"
ON "users"."external_logins"("provider", "external_provider_id");

-- Migrate existing Phase 1 Entra users to junction table
INSERT INTO "users"."external_logins" ("user_id", "provider", "external_provider_id", "provider_email", "linked_at")
SELECT
    "id" AS "user_id",
    0 AS "provider",  -- FederatedProvider.Microsoft
    "external_provider_id",
    "email" AS "provider_email",  -- Use user's email as provider email
    "created_at" AS "linked_at"
FROM "users"."users"
WHERE "identity_provider" = 1 AND "external_provider_id" IS NOT NULL;

-- Drop deprecated column (after migration verification)
-- ALTER TABLE "users"."users" DROP COLUMN "external_provider_id";
```

**EF Core Configuration:**

```csharp
// Infrastructure/Data/Configurations/UserConfiguration.cs
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // ... existing configuration ...

        // Configure ExternalLogins collection
        builder.OwnsMany(u => u.ExternalLogins, login =>
        {
            login.WithOwner().HasForeignKey("UserId");
            login.ToTable("external_logins", "users");

            login.Property<int>("Id")
                .ValueGeneratedOnAdd();

            login.HasKey("Id");

            login.Property(l => l.Provider)
                .HasColumnName("provider")
                .HasConversion<int>()
                .IsRequired();

            login.Property(l => l.ExternalProviderId)
                .HasColumnName("external_provider_id")
                .HasMaxLength(255)
                .IsRequired();

            login.Property(l => l.ProviderEmail)
                .HasColumnName("provider_email")
                .HasMaxLength(255)
                .IsRequired();

            login.Property(l => l.LinkedAt)
                .HasColumnName("linked_at")
                .IsRequired();

            // Unique constraint: one provider per external ID
            login.HasIndex(l => new { l.Provider, l.ExternalProviderId })
                .IsUnique()
                .HasDatabaseName("uk_external_logins_provider_external_id");

            // Index for user lookup
            login.HasIndex("UserId")
                .HasDatabaseName("ix_external_logins_user_id");
        });

        // Auto-include ExternalLogins when loading User
        builder.Navigation(u => u.ExternalLogins).AutoInclude();

        // DEPRECATED: Keep for backward compatibility during migration
        builder.Property(u => u.ExternalProviderId)
            .HasMaxLength(255)
            .IsRequired(false);
    }
}
```

---

### 3. Application Layer (CQRS Commands/Queries)

#### **Enhanced EntraUserInfo DTO**

```csharp
// Application/Common/Interfaces/IEntraExternalIdService.cs
public class EntraUserInfo
{
    public string ObjectId { get; set; } = string.Empty;  // Entra OID
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool EmailVerified { get; set; } = true;

    // NEW: Federated provider information from 'idp' claim
    public string? IdentityProvider { get; set; }  // e.g., "facebook.com", "google.com"
}
```

#### **Updated LoginWithEntraCommandHandler**

```csharp
// Application/Auth/Commands/LoginWithEntra/LoginWithEntraCommandHandler.cs
public class LoginWithEntraCommandHandler : IRequestHandler<LoginWithEntraCommand, Result<LoginWithEntraResponse>>
{
    // ... existing dependencies ...

    public async Task<Result<LoginWithEntraResponse>> Handle(
        LoginWithEntraCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate Entra token and extract user info
        var userInfoResult = await _entraService.GetUserInfoAsync(request.AccessToken);
        if (userInfoResult.IsFailure)
            return Result<LoginWithEntraResponse>.Failure(userInfoResult.Errors);

        var entraUserInfo = userInfoResult.Value;

        // 2. Parse federated provider from 'idp' claim (NEW)
        FederatedProvider federatedProvider;
        try
        {
            federatedProvider = string.IsNullOrEmpty(entraUserInfo.IdentityProvider)
                ? FederatedProvider.Microsoft  // Default if no idp claim
                : FederatedProviderExtensions.FromIdpClaimValue(entraUserInfo.IdentityProvider);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Unknown federated provider in idp claim: {Idp}", entraUserInfo.IdentityProvider);
            return Result<LoginWithEntraResponse>.Failure($"Unsupported identity provider: {entraUserInfo.IdentityProvider}");
        }

        // 3. Find user by external login (provider + external ID)
        var existingUser = await _userRepository.GetByExternalLoginAsync(
            federatedProvider,
            entraUserInfo.ObjectId,
            cancellationToken);

        User user;
        bool isNewUser = false;

        if (existingUser == null)
        {
            // 4. Check if email already exists (prevent account hijacking)
            var emailResult = Email.Create(entraUserInfo.Email);
            if (emailResult.IsFailure)
                return Result<LoginWithEntraResponse>.Failure(emailResult.Errors);

            var emailExists = await _userRepository.ExistsWithEmailAsync(emailResult.Value, cancellationToken);
            if (emailExists)
            {
                // Email exists - this is an ACCOUNT LINKING scenario
                // Require explicit user consent (not auto-linking for security)
                _logger.LogWarning("Email {Email} exists, requiring explicit account linking", entraUserInfo.Email);
                return Result<LoginWithEntraResponse>.Failure(
                    $"An account with {entraUserInfo.Email} already exists. Please log in first and link your {federatedProvider.ToDisplayName()} account from settings.");
            }

            // 5. Auto-provision new user
            var createUserResult = User.CreateFromExternalProvider(
                IdentityProvider.EntraExternal,
                entraUserInfo.ObjectId,
                emailResult.Value,
                entraUserInfo.FirstName,
                entraUserInfo.LastName,
                federatedProvider,
                entraUserInfo.Email,
                UserRole.User);

            if (createUserResult.IsFailure)
                return Result<LoginWithEntraResponse>.Failure(createUserResult.Errors);

            user = createUserResult.Value;
            await _userRepository.AddAsync(user, cancellationToken);
            isNewUser = true;

            _logger.LogInformation("Auto-provisioned new user {UserId} from {Provider}", user.Id, federatedProvider);
        }
        else
        {
            user = existingUser;
            _logger.LogInformation("Found existing user {UserId} for {Provider} login", user.Id, federatedProvider);

            // Opportunistic profile sync (unchanged from Phase 1)
            // ...
        }

        // 6. Generate JWT tokens (unchanged from Phase 1)
        // ...

        // 7. Record successful login (unchanged from Phase 1)
        user.RecordSuccessfulLogin();
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<LoginWithEntraResponse>.Success(new LoginWithEntraResponse(
            user.Id,
            user.Email.Value,
            user.FullName,
            user.Role,
            accessTokenResult.Value,
            refreshTokenResult.Value,
            tokenExpiresAt,
            isNewUser,
            federatedProvider));  // NEW: Include which social provider was used
    }
}
```

#### **New Commands: Link/Unlink Social Providers**

```csharp
// Application/Auth/Commands/LinkExternalProvider/LinkExternalProviderCommand.cs
public record LinkExternalProviderCommand(
    Guid UserId,
    string EntraAccessToken) : IRequest<Result<LinkExternalProviderResponse>>;

public class LinkExternalProviderCommandHandler
    : IRequestHandler<LinkExternalProviderCommand, Result<LinkExternalProviderResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEntraExternalIdService _entraService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;  // Security: verify user identity

    public async Task<Result<LinkExternalProviderResponse>> Handle(
        LinkExternalProviderCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Security: Verify the current authenticated user matches the target user
        var currentUserId = _currentUserService.GetUserId();
        if (currentUserId != request.UserId)
            return Result<LinkExternalProviderResponse>.Failure("Unauthorized: Cannot link providers to another user's account");

        // 2. Validate Entra token
        var userInfoResult = await _entraService.GetUserInfoAsync(request.EntraAccessToken);
        if (userInfoResult.IsFailure)
            return Result<LinkExternalProviderResponse>.Failure(userInfoResult.Errors);

        var entraUserInfo = userInfoResult.Value;

        // 3. Parse federated provider
        var federatedProvider = FederatedProviderExtensions.FromIdpClaimValue(
            entraUserInfo.IdentityProvider ?? "login.microsoftonline.com");

        // 4. Load user
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return Result<LinkExternalProviderResponse>.Failure("User not found");

        // 5. Check if provider is already linked to ANOTHER user (prevent account hijacking)
        var existingUserWithProvider = await _userRepository.GetByExternalLoginAsync(
            federatedProvider,
            entraUserInfo.ObjectId,
            cancellationToken);

        if (existingUserWithProvider != null && existingUserWithProvider.Id != user.Id)
            return Result<LinkExternalProviderResponse>.Failure(
                $"This {federatedProvider.ToDisplayName()} account is already linked to another user");

        // 6. Link provider to user
        var linkResult = user.LinkExternalProvider(
            federatedProvider,
            entraUserInfo.ObjectId,
            entraUserInfo.Email);

        if (linkResult.IsFailure)
            return Result<LinkExternalProviderResponse>.Failure(linkResult.Errors);

        // 7. Save changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<LinkExternalProviderResponse>.Success(new LinkExternalProviderResponse(
            user.Id,
            federatedProvider,
            entraUserInfo.Email,
            DateTime.UtcNow));
    }
}

// Application/Auth/Commands/UnlinkExternalProvider/UnlinkExternalProviderCommand.cs
public record UnlinkExternalProviderCommand(
    Guid UserId,
    FederatedProvider Provider,
    string ExternalProviderId) : IRequest<Result>;

public class UnlinkExternalProviderCommandHandler
    : IRequestHandler<UnlinkExternalProviderCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public async Task<Result> Handle(
        UnlinkExternalProviderCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Security: Verify current user
        var currentUserId = _currentUserService.GetUserId();
        if (currentUserId != request.UserId)
            return Result.Failure("Unauthorized");

        // 2. Load user
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return Result.Failure("User not found");

        // 3. Unlink provider (domain validates business rules)
        var unlinkResult = user.UnlinkExternalProvider(request.Provider, request.ExternalProviderId);
        if (unlinkResult.IsFailure)
            return unlinkResult;

        // 4. Save changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
```

#### **New Query: Get User's Linked Providers**

```csharp
// Application/Auth/Queries/GetLinkedProviders/GetLinkedProvidersQuery.cs
public record GetLinkedProvidersQuery(Guid UserId) : IRequest<Result<GetLinkedProvidersResponse>>;

public class GetLinkedProvidersQueryHandler
    : IRequestHandler<GetLinkedProvidersQuery, Result<GetLinkedProvidersResponse>>
{
    private readonly IUserRepository _userRepository;

    public async Task<Result<GetLinkedProvidersResponse>> Handle(
        GetLinkedProvidersQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return Result<GetLinkedProvidersResponse>.Failure("User not found");

        var linkedProviders = user.ExternalLogins.Select(el => new LinkedProviderDto(
            el.Provider,
            el.Provider.ToDisplayName(),
            el.ProviderEmail,
            el.LinkedAt)).ToList();

        var hasPassword = !string.IsNullOrEmpty(user.PasswordHash);

        return Result<GetLinkedProvidersResponse>.Success(new GetLinkedProvidersResponse(
            user.Id,
            linkedProviders,
            hasPassword));
    }
}

public record GetLinkedProvidersResponse(
    Guid UserId,
    List<LinkedProviderDto> LinkedProviders,
    bool HasPassword);

public record LinkedProviderDto(
    FederatedProvider Provider,
    string DisplayName,
    string Email,
    DateTime LinkedAt);
```

---

### 4. Repository Layer Updates

```csharp
// Domain/Users/IUserRepository.cs
public interface IUserRepository : IRepository<User>
{
    // EXISTING
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithEmailAsync(Email email, CancellationToken cancellationToken = default);

    // DEPRECATED (Phase 1 - will remove after migration)
    Task<User?> GetByExternalProviderIdAsync(string externalProviderId, CancellationToken cancellationToken = default);

    // NEW (Phase 2)
    Task<User?> GetByExternalLoginAsync(
        FederatedProvider provider,
        string externalProviderId,
        CancellationToken cancellationToken = default);
}

// Infrastructure/Data/Repositories/UserRepository.cs
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public async Task<User?> GetByExternalLoginAsync(
        FederatedProvider provider,
        string externalProviderId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.ExternalLogins.Any(el =>
                el.Provider == provider &&
                el.ExternalProviderId == externalProviderId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    // DEPRECATED: Keep for backward compatibility during migration
    public async Task<User?> GetByExternalProviderIdAsync(
        string externalProviderId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.ExternalLogins.Any(el => el.ExternalProviderId == externalProviderId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
```

---

### 5. Domain Events

```csharp
// Domain/Events/ExternalProviderLinkedEvent.cs
public record ExternalProviderLinkedEvent(
    Guid UserId,
    string Email,
    FederatedProvider Provider,
    string ExternalProviderId,
    string ProviderEmail) : DomainEvent;

// Domain/Events/ExternalProviderUnlinkedEvent.cs
public record ExternalProviderUnlinkedEvent(
    Guid UserId,
    string Email,
    FederatedProvider Provider,
    string ExternalProviderId) : DomainEvent;

// UPDATED: Domain/Events/UserCreatedFromExternalProviderEvent.cs
public record UserCreatedFromExternalProviderEvent(
    Guid UserId,
    string Email,
    string FullName,
    IdentityProvider IdentityProvider,
    FederatedProvider FederatedProvider,  // NEW: Which social provider
    string ExternalProviderId) : DomainEvent;
```

---

### 6. Security Considerations

#### **Account Hijacking Prevention**

**Scenario:** Attacker creates Facebook account with victim's email, attempts to link.

**Mitigation:**
```csharp
// In LinkExternalProviderCommandHandler
var existingUserWithProvider = await _userRepository.GetByExternalLoginAsync(
    federatedProvider, entraUserInfo.ObjectId, cancellationToken);

if (existingUserWithProvider != null && existingUserWithProvider.Id != user.Id)
    return Result.Failure("This social account is already linked to another user");
```

#### **Email Verification Bypass Prevention**

**Scenario:** Attacker uses unverified social email to bypass email verification.

**Mitigation:**
```csharp
// Entra External ID pre-verifies emails for all federated providers
// Trust entraUserInfo.EmailVerified claim
user.IsEmailVerified = true;  // Safe for Entra-federated providers
```

#### **Unlinking Security**

**Scenario:** User tries to unlink last authentication method, locking themselves out.

**Mitigation:**
```csharp
// In User.UnlinkExternalProvider()
bool hasPassword = !string.IsNullOrEmpty(PasswordHash);
bool hasOtherProviders = _externalLogins.Count > 1;

if (!hasPassword && !hasOtherProviders)
    return Result.Failure("Cannot unlink the last authentication method");
```

---

## Trade-Offs Analysis

### Design Decision: Single IdentityProvider Enum vs Separate Enums

| Aspect | Single Enum (RECOMMENDED) | Separate Enums |
|--------|---------------------------|----------------|
| **Correctness** | ✅ Accurate - Entra is the IdP | ❌ Misleading - social providers aren't direct IdPs |
| **Schema Simplicity** | ✅ Simple - one column | ❌ Complex - multiple columns |
| **Query Performance** | ✅ Fast - single index | ⚠️ Slower - multiple index scans |
| **Federation Flexibility** | ✅ Easy to add new federated providers | ❌ Schema changes for each provider |
| **Domain Model Clarity** | ✅ Clear separation: authentication vs federation | ❌ Conflates authentication and federation |

**Winner:** Single IdentityProvider enum with ExternalLogin collection

---

### Design Decision: ExternalLogin Collection vs JSON Column

| Aspect | ExternalLogin Collection (RECOMMENDED) | JSON Column |
|--------|----------------------------------------|-------------|
| **Relational Integrity** | ✅ Foreign keys, constraints | ❌ No constraints |
| **Query Performance** | ✅ Indexed lookups | ❌ Full JSON scans |
| **Type Safety** | ✅ Strongly typed | ❌ Dynamic typing |
| **DDD Compliance** | ✅ Value object collection | ⚠️ Primitive obsession |
| **EF Core Support** | ✅ Native OwnsMany | ⚠️ JSON column support varies |

**Winner:** ExternalLogin value object collection with junction table

---

## Implementation Roadmap

### Phase 2A: Domain Layer (Week 1)

**Day 1-2: Value Objects & Aggregates**
1. Create `FederatedProvider` enum
2. Create `ExternalLogin` value object
3. Update `User` aggregate with `LinkExternalProvider()` / `UnlinkExternalProvider()`
4. Write 20+ domain tests
5. **Success Criteria:** 100% domain test coverage

**Day 3: Domain Events**
1. Create `ExternalProviderLinkedEvent`
2. Create `ExternalProviderUnlinkedEvent`
3. Update `UserCreatedFromExternalProviderEvent`
4. Write domain event tests
5. **Success Criteria:** All events raise correctly

### Phase 2B: Infrastructure Layer (Week 2)

**Day 1-2: Database Migration**
1. Create `external_logins` junction table
2. Migrate existing Phase 1 users to junction table
3. Update EF Core `UserConfiguration`
4. Test migration rollback
5. **Success Criteria:** Migration succeeds with zero data loss

**Day 3: Repository Updates**
1. Implement `GetByExternalLoginAsync`
2. Update existing methods for backward compatibility
3. Write repository integration tests
4. **Success Criteria:** 100% repository test coverage

**Day 4: EntraService Enhancement**
1. Update `EntraUserInfo` DTO with `IdentityProvider` claim
2. Parse `idp` claim from Entra tokens
3. Map `idp` values to `FederatedProvider` enum
4. Write token parsing tests
5. **Success Criteria:** All social providers parse correctly

### Phase 2C: Application Layer (Week 3)

**Day 1-2: LoginWithEntraCommand Enhancement**
1. Update handler to parse `FederatedProvider` from token
2. Call `GetByExternalLoginAsync` instead of deprecated method
3. Update auto-provisioning to use new factory method
4. Write 15+ handler tests (all social providers)
5. **Success Criteria:** All login scenarios pass

**Day 3-4: Link/Unlink Commands**
1. Create `LinkExternalProviderCommand` + handler
2. Create `UnlinkExternalProviderCommand` + handler
3. Create `GetLinkedProvidersQuery` + handler
4. Write 25+ application tests
5. **Success Criteria:** All security scenarios covered

### Phase 2D: API Layer (Week 4)

**Day 1-2: API Endpoints**
1. Update `POST /api/auth/login/entra` (no breaking changes)
2. Add `POST /api/auth/link-provider`
3. Add `DELETE /api/auth/unlink-provider/{provider}`
4. Add `GET /api/auth/linked-providers`
5. Update Swagger documentation
6. **Success Criteria:** All endpoints documented

**Day 3: E2E Tests**
1. Test Facebook login flow
2. Test Google login flow
3. Test Apple login flow
4. Test account linking scenarios
5. Test account unlinking scenarios
6. **Success Criteria:** 30+ E2E tests passing

### Phase 2E: Azure Configuration (Week 5)

**Day 1-2: Entra Federation Setup**
1. Configure Facebook identity provider in Entra
2. Configure Google identity provider in Entra
3. Configure Apple identity provider in Entra
4. Test `idp` claim values for each provider
5. **Success Criteria:** All providers return correct `idp` claims

**Day 3-4: Deployment**
1. Deploy to staging environment
2. Run database migration
3. Test all social login flows end-to-end
4. Monitor logs and metrics
5. **Success Criteria:** Production-ready

---

## Testing Strategy

### Unit Tests (Domain Layer)

```csharp
public class ExternalLoginTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var result = ExternalLogin.Create(
            FederatedProvider.Facebook,
            "fb-123456",
            "user@facebook.com");

        result.IsSuccess.Should().BeTrue();
        result.Value.Provider.Should().Be(FederatedProvider.Facebook);
        result.Value.ExternalProviderId.Should().Be("fb-123456");
    }

    [Fact]
    public void LinkExternalProvider_WithSameProvider_ShouldFail()
    {
        var user = CreateTestUser();
        user.LinkExternalProvider(FederatedProvider.Facebook, "fb-123", "test@fb.com");

        var result = user.LinkExternalProvider(FederatedProvider.Facebook, "fb-123", "test@fb.com");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("already linked");
    }

    [Fact]
    public void UnlinkExternalProvider_WhenLastMethod_ShouldFail()
    {
        var user = CreateExternalOnlyUser();  // No password

        var result = user.UnlinkExternalProvider(FederatedProvider.Google, "google-123");

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("last authentication method");
    }
}

public class UserMultiProviderTests
{
    [Fact]
    public void User_CanHaveMultipleLinkedProviders()
    {
        var user = CreateTestUser();

        user.LinkExternalProvider(FederatedProvider.Facebook, "fb-123", "user@fb.com");
        user.LinkExternalProvider(FederatedProvider.Google, "google-456", "user@google.com");
        user.LinkExternalProvider(FederatedProvider.Apple, "apple-789", "user@apple.com");

        user.ExternalLogins.Should().HaveCount(3);
        user.HasLinkedProvider(FederatedProvider.Facebook).Should().BeTrue();
        user.HasLinkedProvider(FederatedProvider.Google).Should().BeTrue();
        user.HasLinkedProvider(FederatedProvider.Apple).Should().BeTrue();
    }
}
```

### Integration Tests (Infrastructure Layer)

```csharp
public class EntraIdpClaimParsingTests
{
    [Theory]
    [InlineData("facebook.com", FederatedProvider.Facebook)]
    [InlineData("google.com", FederatedProvider.Google)]
    [InlineData("appleid.apple.com", FederatedProvider.Apple)]
    [InlineData("login.microsoftonline.com", FederatedProvider.Microsoft)]
    public void FromIdpClaimValue_ShouldParseCorrectly(string idpClaim, FederatedProvider expected)
    {
        var result = FederatedProviderExtensions.FromIdpClaimValue(idpClaim);

        result.Should().Be(expected);
    }
}

public class ExternalLoginRepositoryTests : IClassFixture<DbFixture>
{
    [Fact]
    public async Task GetByExternalLoginAsync_WithFacebookLogin_ShouldFindUser()
    {
        // Arrange
        var user = CreateUserWithFacebookLogin();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByExternalLoginAsync(
            FederatedProvider.Facebook,
            "fb-123456",
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }
}
```

### E2E Tests (API Layer)

```csharp
public class SocialLoginFlowTests : IClassFixture<WebApplicationFactory>
{
    [Fact]
    public async Task LoginWithFacebook_NewUser_ShouldAutoProvision()
    {
        // Arrange: Mock Entra token with Facebook idp claim
        var facebookToken = CreateMockEntraToken(
            oid: "fb-new-user",
            email: "newuser@facebook.com",
            firstName: "Jane",
            lastName: "Doe",
            idpClaim: "facebook.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login/entra",
            new { AccessToken = facebookToken });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<LoginWithEntraResponse>();
        content.FederatedProvider.Should().Be(FederatedProvider.Facebook);
    }

    [Fact]
    public async Task LinkGoogleAccount_WhenAlreadyLinkedToAnother_ShouldFail()
    {
        // Arrange
        var user1 = await CreateAuthenticatedUser();
        var user2 = await CreateAuthenticatedUser();

        var googleToken = CreateMockEntraToken(
            oid: "google-123",
            email: "shared@google.com",
            idpClaim: "google.com");

        // Link to user1
        await LinkProviderAsync(user1.Id, googleToken);

        // Act: Try to link same Google account to user2
        var response = await LinkProviderAsync(user2.Id, googleToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.Should().Contain("already linked to another user");
    }

    [Fact]
    public async Task UnlinkProvider_WhenLastMethod_ShouldFail()
    {
        // Arrange: User with only Facebook login (no password)
        var user = await CreateExternalOnlyUser(FederatedProvider.Facebook);

        // Act
        var response = await _client.DeleteAsync(
            $"/api/auth/unlink-provider/{FederatedProvider.Facebook}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.Should().Contain("last authentication method");
    }
}
```

---

## API Endpoints

```csharp
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // EXISTING (Phase 1) - Enhanced for Phase 2
    [HttpPost("login/entra")]
    public async Task<IActionResult> LoginWithEntra([FromBody] LoginWithEntraCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return BadRequest(new { errors = result.Errors });

        return Ok(new
        {
            user = new
            {
                id = result.Value.UserId,
                email = result.Value.Email,
                fullName = result.Value.FullName,
                role = result.Value.Role.ToString()
            },
            accessToken = result.Value.AccessToken,
            refreshToken = result.Value.RefreshToken,
            expiresAt = result.Value.TokenExpiresAt,
            isNewUser = result.Value.IsNewUser,
            provider = result.Value.FederatedProvider.ToDisplayName()  // NEW
        });
    }

    // NEW (Phase 2)
    [HttpPost("link-provider")]
    [Authorize]
    public async Task<IActionResult> LinkExternalProvider([FromBody] LinkExternalProviderRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new LinkExternalProviderCommand(userId, request.EntraAccessToken);

        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return BadRequest(new { errors = result.Errors });

        return Ok(new
        {
            provider = result.Value.Provider.ToDisplayName(),
            email = result.Value.Email,
            linkedAt = result.Value.LinkedAt
        });
    }

    // NEW (Phase 2)
    [HttpDelete("unlink-provider/{provider}/{externalProviderId}")]
    [Authorize]
    public async Task<IActionResult> UnlinkExternalProvider(
        FederatedProvider provider,
        string externalProviderId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new UnlinkExternalProviderCommand(userId, provider, externalProviderId);

        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return BadRequest(new { errors = result.Errors });

        return NoContent();
    }

    // NEW (Phase 2)
    [HttpGet("linked-providers")]
    [Authorize]
    public async Task<IActionResult> GetLinkedProviders()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var query = new GetLinkedProvidersQuery(userId);

        var result = await _mediator.Send(query);
        if (result.IsFailure)
            return BadRequest(new { errors = result.Errors });

        return Ok(new
        {
            linkedProviders = result.Value.LinkedProviders.Select(p => new
            {
                provider = p.Provider.ToString(),
                displayName = p.DisplayName,
                email = p.Email,
                linkedAt = p.LinkedAt
            }),
            hasPassword = result.Value.HasPassword
        });
    }
}
```

---

## Consequences

### Positive

1. **Clean Domain Model** - Single IdentityProvider enum accurately reflects Entra federation
2. **Multi-Provider Support** - Users can link multiple social accounts
3. **Security** - Prevents account hijacking and unintended lockouts
4. **Backward Compatible** - Phase 1 Entra users migrate seamlessly
5. **Scalable** - Easy to add new federated providers (LinkedIn, Twitter, etc.)
6. **DDD Compliance** - ExternalLogin as value object collection
7. **Testable** - Mock Entra tokens with different `idp` claims

### Negative

1. **Migration Complexity** - Need to migrate `ExternalProviderId` to junction table
2. **Entra Dependency** - All social logins require Entra External ID configuration
3. **Token Size** - JWT tokens include multiple linked providers (minimal impact)
4. **UI Complexity** - Frontend needs to handle multiple linked accounts

---

## Alternatives Considered

### Alternative 1: Expand IdentityProvider Enum (Facebook = 2, Google = 3)

**Rejected:** Violates Single Responsibility. Entra External ID is the identity provider; social platforms are upstream federation sources.

### Alternative 2: JSON Column for ExternalLogins

**Rejected:** Poor query performance, no type safety, violates DDD principles.

### Alternative 3: Separate Tables (FacebookUsers, GoogleUsers)

**Rejected:** Data duplication, violates DRY, complex querying, breaks User aggregate.

---

## References

- [ADR-002: Microsoft Entra External ID Integration](./ADR-002-Entra-External-ID-Integration.md)
- [Azure Entra External ID - Identity Providers](https://learn.microsoft.com/en-us/entra/external-id/identity-providers)
- [OpenID Connect idp Claim](https://openid.net/specs/openid-connect-core-1_0.html#IDToken)
- [Domain-Driven Design: Value Objects](https://martinfowler.com/bliki/ValueObject.html)
- [OWASP: Account Linking Best Practices](https://cheatsheetseries.owasp.org/cheatsheets/Third_Party_Identity_Management_Cheat_Sheet.html)

---

## Approval

- [ ] Domain Expert Review
- [ ] Security Team Review
- [ ] Lead Developer Approval
- [ ] Product Owner Approval

---

**Next Steps:**
1. Review ADR with team
2. Set up Facebook, Google, Apple in Entra tenant
3. Test `idp` claim values for each provider
4. Begin TDD implementation of Phase 2A (Domain Layer)
