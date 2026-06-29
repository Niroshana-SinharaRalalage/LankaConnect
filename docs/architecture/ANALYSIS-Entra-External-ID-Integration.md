# ADR-002: Microsoft Entra External ID Integration for Hybrid Authentication

**Status:** Proposed
**Date:** 2025-10-28
**Author:** System Architecture Designer
**Supersedes:** ADR-001 (Email Verification Automation)

---

## Context and Problem Statement

LankaConnect currently uses **local JWT authentication** with BCrypt password hashing, storing credentials in PostgreSQL. The system requires enterprise-grade authentication with:

1. **External user support** (customers) via Microsoft Entra External ID
2. **Social login** (Facebook, Google, Apple) through Entra federation
3. **Dual authentication mode** during migration (local JWT + Entra)
4. **Preservation of existing user data** and domain model integrity
5. **Clean Architecture compliance** with DDD principles
6. **Zero-tolerance TDD** approach for incremental implementation

**Key Constraints:**
- Existing User aggregate must remain the single source of truth
- PostgreSQL stores user profile data; Entra stores credentials
- Email verification and password reset flows already implemented
- Must support gradual migration from local JWT to Entra

---

## Decision Drivers

1. **Clean Architecture Principles** - Dependency inversion, layer separation
2. **Domain-Driven Design** - Preserve User aggregate, domain events
3. **Zero Compilation Errors** - Incremental TDD with passing tests at every step
4. **Security Best Practices** - OAuth2/OIDC standards, token validation
5. **Migration Safety** - Support both authentication methods during transition
6. **Developer Experience** - Testable, mockable interfaces

---

## Decision

### 1. **Identity Provider Abstraction Pattern**

We will introduce an **Identity Provider (IdP) abstraction** in the Domain layer to support multiple authentication sources:

```csharp
// Domain Layer - New ValueObject
public enum IdentityProvider
{
    Local = 0,      // BCrypt + JWT (existing)
    EntraExternal = 1  // Microsoft Entra External ID
}

// Domain Layer - User Entity Enhancement
public class User : BaseEntity
{
    // NEW: Identity provider tracking
    public IdentityProvider IdentityProvider { get; private set; }
    public string? ExternalProviderId { get; private set; }  // Entra OID/sub claim

    // EXISTING: Nullable for Entra users
    public string? PasswordHash { get; private set; }

    // ... existing properties ...
}
```

**Rationale:** This preserves the User aggregate as the single source of truth while allowing polymorphic authentication sources.

---

### 2. **Authentication Service Architecture**

We will create a **Strategy Pattern** for authentication in the Infrastructure layer:

```
Infrastructure/
  Security/
    Services/
      ├── JwtTokenService.cs (EXISTING - unchanged)
      ├── PasswordHashingService.cs (EXISTING - unchanged)
      ├── EntraExternalIdService.cs (NEW)
      └── AuthenticationServiceFactory.cs (NEW - Strategy selector)
    Interfaces/
      └── IExternalAuthenticationService.cs (NEW)
```

**IExternalAuthenticationService Interface:**
```csharp
public interface IExternalAuthenticationService
{
    Task<Result<ExternalAuthenticationResult>> ValidateTokenAsync(
        string accessToken, CancellationToken cancellationToken);

    Task<Result<UserClaims>> GetUserClaimsAsync(
        string accessToken, CancellationToken cancellationToken);
}

public record ExternalAuthenticationResult(
    string ProviderId,         // Entra OID
    string Email,
    string? FirstName,
    string? LastName,
    bool IsEmailVerified,
    IDictionary<string, string> AdditionalClaims);
```

---

### 3. **User Entity Refactoring Strategy**

**Phase 1 - Add Identity Provider Support (TDD):**

```csharp
// Domain Layer - User.cs modifications
public class User : BaseEntity
{
    // NEW properties
    public IdentityProvider IdentityProvider { get; private set; }
    public string? ExternalProviderId { get; private set; }

    // MODIFIED: Nullable for Entra users
    public string? PasswordHash { get; private set; }

    // NEW factory method for Entra users
    public static Result<User> CreateFromExternalProvider(
        Email email,
        string firstName,
        string lastName,
        IdentityProvider provider,
        string externalProviderId,
        UserRole role = UserRole.User)
    {
        if (provider == IdentityProvider.Local)
            return Result<User>.Failure("Use standard Create method for local users");

        if (string.IsNullOrWhiteSpace(externalProviderId))
            return Result<User>.Failure("External provider ID is required");

        var user = new User(email, firstName, lastName, role)
        {
            IdentityProvider = provider,
            ExternalProviderId = externalProviderId,
            IsEmailVerified = true,  // Entra handles email verification
            PasswordHash = null      // No local password for external users
        };

        user.RaiseDomainEvent(new UserCreatedFromExternalProviderEvent(
            user.Id, email.Value, user.FullName, provider, externalProviderId));

        return Result<User>.Success(user);
    }

    // NEW business rule validation
    public Result SetPassword(string passwordHash)
    {
        if (IdentityProvider != IdentityProvider.Local)
            return Result.Failure("Cannot set password for external provider users");

        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure("Password hash is required");

        PasswordHash = passwordHash;
        MarkAsUpdated();
        return Result.Success();
    }

    // NEW business rules
    public bool IsExternalUser => IdentityProvider != IdentityProvider.Local;
    public bool RequiresPasswordAuthentication => IdentityProvider == IdentityProvider.Local;
}
```

**Business Rules:**
- Local users MUST have PasswordHash
- External users MUST have ExternalProviderId
- External users CANNOT have PasswordHash
- Password reset is only valid for local users

---

### 4. **Application Layer Commands**

**New Commands:**

```csharp
// Application/Auth/Commands/LoginWithEntra/
public record LoginWithEntraCommand(
    string AccessToken,
    string? IpAddress = null) : IRequest<Result<LoginUserResponse>>;

public class LoginWithEntraHandler : IRequestHandler<LoginWithEntraCommand, Result<LoginUserResponse>>
{
    private readonly IExternalAuthenticationService _entraService;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public async Task<Result<LoginUserResponse>> Handle(
        LoginWithEntraCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate Entra access token
        var validationResult = await _entraService.ValidateTokenAsync(
            request.AccessToken, cancellationToken);

        if (!validationResult.IsSuccess)
            return Result<LoginUserResponse>.Failure("Invalid Entra token");

        var authResult = validationResult.Value;

        // 2. Get or create user in PostgreSQL
        var email = Email.Create(authResult.Email);
        var existingUser = await _userRepository.GetByEmailAsync(email.Value, cancellationToken);

        User user;
        if (existingUser == null)
        {
            // Auto-provision user from Entra
            var userResult = User.CreateFromExternalProvider(
                email.Value,
                authResult.FirstName ?? "",
                authResult.LastName ?? "",
                IdentityProvider.EntraExternal,
                authResult.ProviderId);

            user = userResult.Value;
            await _userRepository.AddAsync(user, cancellationToken);
        }
        else
        {
            // Sync Entra user with existing local user
            if (existingUser.IdentityProvider == IdentityProvider.Local)
            {
                return Result<LoginUserResponse>.Failure(
                    "Account exists with local authentication. Please use password login.");
            }
            user = existingUser;
        }

        // 3. Generate LankaConnect JWT tokens
        var accessTokenResult = await _jwtTokenService.GenerateAccessTokenAsync(user);
        var refreshTokenResult = await _jwtTokenService.GenerateRefreshTokenAsync();

        // 4. Record login
        user.RecordSuccessfulLogin();
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<LoginUserResponse>.Success(new LoginUserResponse(...));
    }
}
```

**Modified Commands:**
- `RegisterUserCommand` - Add `IdentityProvider` parameter (defaults to Local)
- `LoginUserCommand` - Check `IdentityProvider` before password validation

---

### 5. **Database Schema Changes**

**Migration: AddIdentityProviderSupport**

```sql
-- Add new columns to Users table
ALTER TABLE "Users"
ADD COLUMN "IdentityProvider" INT NOT NULL DEFAULT 0,
ADD COLUMN "ExternalProviderId" VARCHAR(255) NULL;

-- Make PasswordHash nullable (already nullable in current schema)
-- No change needed if already nullable

-- Add index for external provider lookups
CREATE INDEX "IX_Users_IdentityProvider_ExternalProviderId"
ON "Users" ("IdentityProvider", "ExternalProviderId");

-- Add unique constraint for external users
CREATE UNIQUE INDEX "IX_Users_ExternalProviderId_Unique"
ON "Users" ("ExternalProviderId")
WHERE "ExternalProviderId" IS NOT NULL;

-- Update existing users to Local identity provider
UPDATE "Users"
SET "IdentityProvider" = 0, "ExternalProviderId" = NULL
WHERE "IdentityProvider" IS NULL;
```

**EF Core Configuration:**

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // ... existing configuration ...

        builder.Property(u => u.IdentityProvider)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(IdentityProvider.Local);

        builder.Property(u => u.ExternalProviderId)
            .HasMaxLength(255)
            .IsRequired(false);

        builder.HasIndex(u => new { u.IdentityProvider, u.ExternalProviderId })
            .HasDatabaseName("IX_Users_IdentityProvider_ExternalProviderId");

        builder.HasIndex(u => u.ExternalProviderId)
            .IsUnique()
            .HasFilter("[ExternalProviderId] IS NOT NULL")
            .HasDatabaseName("IX_Users_ExternalProviderId_Unique");

        // PasswordHash remains nullable
        builder.Property(u => u.PasswordHash)
            .IsRequired(false);
    }
}
```

---

### 6. **Dual Authentication Mode Strategy**

**During Migration Phase:**

```csharp
// API Controller - Support both login methods
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // EXISTING - Unchanged
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand request)
    {
        // Handles local JWT authentication
    }

    // NEW - Entra External ID login
    [HttpPost("login/entra")]
    public async Task<IActionResult> LoginWithEntra([FromBody] LoginWithEntraCommand request)
    {
        var result = await _mediator.Send(request, cancellationToken);

        if (!result.IsSuccess)
            return Unauthorized(new { error = result.Error });

        SetRefreshTokenCookie(result.Value.RefreshToken);

        return Ok(new
        {
            user = new { ... },
            result.Value.AccessToken,
            result.Value.TokenExpiresAt,
            authMethod = "entra-external"
        });
    }

    // EXISTING - Unchanged
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand request)
    {
        // Only creates local JWT users
        // Entra users are auto-provisioned on first login
    }
}
```

**Frontend Flow:**
1. User clicks "Login with Microsoft" → redirects to Entra External ID
2. Entra callback returns access token → POST `/api/auth/login/entra`
3. Backend validates token, syncs user, returns LankaConnect JWT
4. User clicks "Login with Email/Password" → POST `/api/auth/login` (existing)

**User Identification Logic:**
```csharp
public async Task<User?> GetUserByIdentityAsync(
    Email email,
    IdentityProvider? provider = null)
{
    if (provider.HasValue)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Email == email &&
                u.IdentityProvider == provider.Value);
    }

    return await _context.Users
        .FirstOrDefaultAsync(u => u.Email == email);
}
```

---

### 7. **Domain Events Strategy**

**New Domain Events:**

```csharp
// Domain/Events/UserCreatedFromExternalProviderEvent.cs
public record UserCreatedFromExternalProviderEvent(
    Guid UserId,
    string Email,
    string FullName,
    IdentityProvider Provider,
    string ExternalProviderId) : DomainEvent;
```

**Existing Events - Behavior Changes:**

| Event | Local Users | Entra Users | Notes |
|-------|-------------|-------------|-------|
| `UserCreatedEvent` | ✅ Raised | ❌ Not raised | Use `UserCreatedFromExternalProviderEvent` |
| `UserLoggedInEvent` | ✅ Raised | ✅ Raised | Same behavior |
| `UserPasswordChangedEvent` | ✅ Raised | ❌ Not raised | Password managed by Entra |
| `UserEmailVerifiedEvent` | ✅ Raised | ❌ Not raised | Email pre-verified by Entra |
| `UserAccountLockedEvent` | ✅ Raised | ❌ Not raised | Account locking managed by Entra |

**Event Handler Example:**

```csharp
public class UserCreatedFromExternalProviderEventHandler
    : INotificationHandler<UserCreatedFromExternalProviderEvent>
{
    public async Task Handle(
        UserCreatedFromExternalProviderEvent notification,
        CancellationToken cancellationToken)
    {
        // Send welcome email (no verification needed)
        await _emailService.SendWelcomeEmailAsync(notification.Email);

        // Log analytics event
        await _analyticsService.TrackExternalUserRegistrationAsync(
            notification.UserId, notification.Provider);
    }
}
```

---

### 8. **Testing Strategy**

#### **Unit Tests (Domain Layer)**

```csharp
// Tests/LankaConnect.Domain.Tests/Users/UserTests.cs
public class UserExternalProviderTests
{
    [Fact]
    public void CreateFromExternalProvider_WithValidData_ShouldSucceed()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;
        var providerId = "entra-oid-12345";

        // Act
        var result = User.CreateFromExternalProvider(
            email, "John", "Doe",
            IdentityProvider.EntraExternal, providerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IdentityProvider.Should().Be(IdentityProvider.EntraExternal);
        result.Value.ExternalProviderId.Should().Be(providerId);
        result.Value.PasswordHash.Should().BeNull();
        result.Value.IsEmailVerified.Should().BeTrue();
    }

    [Fact]
    public void SetPassword_ForExternalUser_ShouldFail()
    {
        // Arrange
        var user = User.CreateFromExternalProvider(
            Email.Create("test@example.com").Value,
            "John", "Doe",
            IdentityProvider.EntraExternal,
            "entra-oid-12345").Value;

        // Act
        var result = user.SetPassword("hashed-password");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("external provider");
    }
}
```

#### **Integration Tests (Infrastructure Layer)**

```csharp
// Tests/LankaConnect.IntegrationTests/Auth/EntraAuthenticationTests.cs
public class EntraExternalIdServiceTests : IClassFixture<WebApplicationFactory>
{
    [Fact]
    public async Task ValidateTokenAsync_WithValidEntraToken_ShouldReturnUserClaims()
    {
        // Arrange
        var service = _factory.Services.GetRequiredService<IExternalAuthenticationService>();
        var validToken = await GetValidEntraAccessTokenAsync(); // Test helper

        // Act
        var result = await service.ValidateTokenAsync(validToken, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().NotBeNullOrEmpty();
        result.Value.ProviderId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithExpiredToken_ShouldFail()
    {
        // Arrange
        var service = _factory.Services.GetRequiredService<IExternalAuthenticationService>();
        var expiredToken = GetExpiredEntraToken();

        // Act
        var result = await service.ValidateTokenAsync(expiredToken, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("expired");
    }
}
```

#### **E2E Tests with Mock Entra**

```csharp
// Use WireMock or similar to mock Entra endpoints
public class EntraAuthenticationFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task LoginWithEntra_NewUser_ShouldAutoProvisionAndReturnTokens()
    {
        // Arrange
        var mockEntraToken = CreateMockEntraAccessToken(
            oid: "new-user-oid",
            email: "newuser@example.com",
            firstName: "Jane",
            lastName: "Smith");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login/entra",
            new { AccessToken = mockEntraToken });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<LoginResponse>();
        content.AccessToken.Should().NotBeNullOrEmpty();
        content.User.Email.Should().Be("newuser@example.com");

        // Verify user created in database
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.Value == "newuser@example.com");

        user.Should().NotBeNull();
        user.IdentityProvider.Should().Be(IdentityProvider.EntraExternal);
        user.ExternalProviderId.Should().Be("new-user-oid");
    }
}
```

#### **Test Helpers**

```csharp
public static class EntraTestHelpers
{
    public static string CreateMockEntraAccessToken(
        string oid,
        string email,
        string? firstName = null,
        string? lastName = null)
    {
        var claims = new[]
        {
            new Claim("oid", oid),
            new Claim("email", email),
            new Claim("given_name", firstName ?? "Test"),
            new Claim("family_name", lastName ?? "User"),
            new Claim("email_verified", "true")
        };

        // Create JWT token for testing
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = TestSigningCredentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
```

---

### 9. **Configuration Management**

**appsettings.json:**

```json
{
  "Jwt": {
    "Key": "your-jwt-secret-key",
    "Issuer": "LankaConnect",
    "Audience": "LankaConnect.API",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "EntraExternalId": {
    "TenantId": "369a3c47-33b7-4baa-98b8-6ddf16a51a31",
    "ClientId": "957e9865-fca0-4236-9276-a8643a7193b5",
    "ClientSecret": "eEX8Q~Fj9Q-XkoD3923G5A~hnpgZT12ms.JwJdBW",
    "Domain": "lankaconnect.onmicrosoft.com",
    "Instance": "https://login.microsoftonline.com/",
    "SignUpSignInPolicyId": "B2C_1_SignUpSignIn",
    "Scopes": "openid profile email User.Read",
    "ValidateTokens": true,
    "ClockSkew": "00:05:00"
  },
  "Authentication": {
    "DefaultProvider": "Local",
    "AllowedProviders": ["Local", "EntraExternal"],
    "AutoProvisionExternalUsers": true
  }
}
```

**Dependency Injection:**

```csharp
// Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // EXISTING - Local authentication
    services.AddScoped<IJwtTokenService, JwtTokenService>();
    services.AddScoped<IPasswordHashingService, PasswordHashingService>();

    // NEW - External authentication
    services.AddScoped<IExternalAuthenticationService, EntraExternalIdService>();

    // Configure Entra External ID
    services.Configure<EntraExternalIdOptions>(
        configuration.GetSection("EntraExternalId"));

    // Add HTTP client for Entra API calls
    services.AddHttpClient<IExternalAuthenticationService, EntraExternalIdService>()
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

    return services;
}
```

---

## Consequences

### Positive Consequences

1. **Clean Separation of Concerns**
   - Domain layer remains authentication-agnostic
   - Infrastructure layer handles external provider integration
   - Testability through interface abstraction

2. **Gradual Migration Path**
   - Dual authentication mode allows phased rollout
   - Existing users continue with local JWT
   - New users can choose Entra or local authentication

3. **Enterprise-Ready Authentication**
   - OAuth2/OIDC compliance
   - Social login support through Entra federation
   - Centralized identity management

4. **Preserved Domain Model**
   - User aggregate remains single source of truth
   - Domain events maintain consistency
   - Business rules enforced at domain level

5. **Testable Architecture**
   - Mock Entra tokens for unit tests
   - Integration tests with test identity provider
   - E2E tests with WireMock

### Negative Consequences

1. **Increased Complexity**
   - Two authentication flows to maintain
   - Additional testing overhead
   - More configuration management

2. **Migration Challenges**
   - Existing users cannot easily switch to Entra
   - Need migration strategy for local → Entra conversion
   - Potential user confusion during transition

3. **External Dependency**
   - Relies on Entra availability
   - Network latency for token validation
   - Requires Microsoft tenant configuration

4. **Data Synchronization**
   - User profile data may drift between Entra and PostgreSQL
   - Need sync strategy for profile updates
   - Entra remains authoritative for credentials

---

## Implementation Roadmap

### Phase 1: Foundation (Week 1)
1. Add `IdentityProvider` enum and `ExternalProviderId` to User entity (TDD)
2. Write domain tests for external user creation
3. Create database migration
4. Update EF Core configuration
5. Make `PasswordHash` nullable with validation rules

**Success Criteria:** All existing tests pass + 15 new User entity tests

### Phase 2: Infrastructure (Week 2)
1. Create `IExternalAuthenticationService` interface
2. Implement `EntraExternalIdService` with token validation
3. Configure Entra HTTP client and options
4. Write integration tests with mock tokens
5. Add logging and error handling

**Success Criteria:** Token validation works with test Entra tenant

### Phase 3: Application Layer (Week 3)
1. Create `LoginWithEntraCommand` and handler
2. Implement auto-provisioning logic
3. Add domain events for external users
4. Write application layer tests
5. Update existing commands to respect `IdentityProvider`

**Success Criteria:** 30+ passing tests for Entra login flow

### Phase 4: API Integration (Week 4)
1. Add `/api/auth/login/entra` endpoint
2. Update API documentation (Swagger)
3. Add E2E tests with WireMock
4. Configure CORS for Entra redirects
5. Implement rate limiting

**Success Criteria:** E2E tests pass with mock Entra

### Phase 5: Migration & Monitoring (Week 5)
1. Deploy to staging environment
2. Run database migration
3. Configure production Entra tenant
4. Add monitoring and alerting
5. Document migration guide for users

**Success Criteria:** Production-ready deployment

---

## Alternatives Considered

### Alternative 1: Replace JWT with Entra Entirely
**Rejected:** Would break existing authentication for all users, requiring forced migration.

### Alternative 2: Separate User Tables (LocalUser, EntraUser)
**Rejected:** Violates DDD principles, creates data duplication, complicates querying.

### Alternative 3: Use Entra as Primary, Fallback to Local
**Rejected:** Increases complexity, unclear which system is authoritative.

### Alternative 4: Duende IdentityServer as Abstraction Layer
**Considered but Deferred:** Adds significant complexity; may revisit for Phase 2 with multiple IdPs.

---

## References

- [Microsoft Entra External ID Documentation](https://learn.microsoft.com/en-us/entra/external-id/)
- [OAuth 2.0 and OpenID Connect Protocols](https://oauth.net/2/)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [LankaConnect ADR-001: Email Verification Automation](./ADR-001-EMAIL-VERIFICATION-AUTOMATION.md)

---

## Approval

- [ ] Domain Expert Review
- [ ] Security Team Review
- [ ] DevOps Team Review
- [ ] Lead Developer Approval

---

**Next Steps:**
1. Review ADR with team
2. Create GitHub issues for each phase
3. Set up test Entra tenant
4. Begin TDD implementation of Phase 1
