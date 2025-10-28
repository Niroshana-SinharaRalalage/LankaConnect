# Entra External ID Integration - Quick Reference Guide

**Purpose:** Fast lookup for key decisions, code snippets, and implementation details
**Date:** 2025-10-28

---

## Architecture Questions Answered

### Q1: Should we add EntraExternalId property to User entity?

**Answer:** Yes, but renamed to `ExternalProviderId` (provider-agnostic).

```csharp
public class User : BaseEntity
{
    public IdentityProvider IdentityProvider { get; private set; }  // Enum: Local | EntraExternal
    public string? ExternalProviderId { get; private set; }          // Entra OID claim
    public string? PasswordHash { get; private set; }                // Nullable for external users
}
```

---

### Q2: How to handle users who registered with local JWT vs Entra?

**Answer:** Use `IdentityProvider` enum to distinguish.

```csharp
// Query by provider
var localUser = await _userRepository.GetByEmailAsync(email, IdentityProvider.Local);
var entraUser = await _userRepository.GetByEmailAsync(email, IdentityProvider.EntraExternal);

// Check provider before authentication
if (user.IdentityProvider == IdentityProvider.Local)
{
    // Use password authentication
}
else
{
    // Use Entra token validation
}
```

---

### Q3: Should PasswordHash become nullable for Entra users?

**Answer:** Yes, enforced by domain rules.

```csharp
public Result SetPassword(string passwordHash)
{
    if (IdentityProvider != IdentityProvider.Local)
        return Result.Failure("Cannot set password for external users");

    PasswordHash = passwordHash;
    return Result.Success();
}
```

**Database Constraint:**
```sql
ALTER TABLE "Users"
ADD CONSTRAINT "CK_Users_PasswordHash_Required_For_Local"
    CHECK (
        ("IdentityProvider" = 0 AND "PasswordHash" IS NOT NULL)
        OR ("IdentityProvider" != 0 AND "PasswordHash" IS NULL)
    );
```

---

### Q4: Where should Entra integration live in Clean Architecture?

**Answer:** Infrastructure layer as `IExternalAuthenticationService`.

```
src/LankaConnect.Infrastructure/
  Security/
    Services/
      ├── JwtTokenService.cs           (Existing)
      ├── PasswordHashingService.cs    (Existing)
      └── EntraExternalIdService.cs    (NEW - implements IExternalAuthenticationService)
    Interfaces/
      └── IExternalAuthenticationService.cs  (NEW)
```

---

### Q5: How to sync Entra users to PostgreSQL?

**Answer:** Auto-provision on first login via `CreateFromExternalProvider()`.

```csharp
public async Task<Result<LoginUserResponse>> Handle(LoginWithEntraCommand request, CancellationToken ct)
{
    // 1. Validate Entra token
    var validationResult = await _entraService.ValidateTokenAsync(request.AccessToken, ct);

    // 2. Get or create user
    var existingUser = await _userRepository.GetByExternalProviderIdAsync(
        IdentityProvider.EntraExternal,
        validationResult.Value.ProviderId,
        ct);

    User user;
    if (existingUser == null)
    {
        // Auto-provision
        var userResult = User.CreateFromExternalProvider(
            email, firstName, lastName,
            IdentityProvider.EntraExternal,
            validationResult.Value.ProviderId);

        await _userRepository.AddAsync(userResult.Value, ct);
        user = userResult.Value;
    }
    else
    {
        user = existingUser;
    }

    // 3. Generate LankaConnect JWT
    var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user);
    var refreshToken = await _jwtTokenService.GenerateRefreshTokenAsync();

    return Result<LoginUserResponse>.Success(new LoginUserResponse(...));
}
```

---

### Q6: Should RegisterUserCommand still exist for Entra users?

**Answer:** No, Entra users are auto-provisioned on login. RegisterUserCommand creates Local users only.

```csharp
// RegisterUserHandler (unchanged - only for Local users)
public async Task<Result<RegisterUserResponse>> Handle(RegisterUserCommand request, CancellationToken ct)
{
    var user = User.Create(email, firstName, lastName).Value;  // IdentityProvider = Local
    user.SetPassword(hashedPassword);
    await _userRepository.AddAsync(user, ct);
    // Send verification email
}

// LoginWithEntraHandler (NEW - auto-provisions Entra users)
public async Task<Result<LoginUserResponse>> Handle(LoginWithEntraCommand request, CancellationToken ct)
{
    // Auto-provision if user doesn't exist
    if (existingUser == null)
    {
        var user = User.CreateFromExternalProvider(...).Value;  // IdentityProvider = EntraExternal
        await _userRepository.AddAsync(user, ct);
    }
}
```

---

### Q7: How to handle LoginUserCommand when using Entra?

**Answer:** Create separate endpoint `/api/auth/login/entra`.

```csharp
// Local authentication (existing)
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginUserCommand request)
{
    // Validates email/password
    // Only works for Local users
}

// Entra authentication (NEW)
[HttpPost("login/entra")]
public async Task<IActionResult> LoginWithEntra([FromBody] LoginWithEntraCommand request)
{
    // Validates Entra access token
    // Auto-provisions user if needed
    // Returns LankaConnect JWT
}
```

**Frontend Flow:**
```javascript
// Check which provider user has
const response = await fetch('/api/auth/check-provider?email=user@example.com');
const { provider } = await response.json();

if (provider === 'Local') {
    // Show password form, POST to /api/auth/login
} else if (provider === 'EntraExternal') {
    // Redirect to Entra, callback to /api/auth/login/entra
}
```

---

### Q8: Domain Event Implications

**Answer:** Different events for different providers.

| Event | Local Users | Entra Users |
|-------|-------------|-------------|
| `UserCreatedEvent` | ✅ Raised | ❌ Not raised |
| `UserCreatedFromExternalProviderEvent` | ❌ Not raised | ✅ Raised |
| `UserLoggedInEvent` | ✅ Raised | ✅ Raised |
| `UserPasswordChangedEvent` | ✅ Raised | ❌ Not raised |
| `UserEmailVerifiedEvent` | ✅ Raised | ❌ Not raised (pre-verified) |
| `UserAccountLockedEvent` | ✅ Raised | ❌ Not raised (handled by Entra) |

```csharp
// Domain event handler for external users
public class UserCreatedFromExternalProviderEventHandler
    : INotificationHandler<UserCreatedFromExternalProviderEvent>
{
    public async Task Handle(UserCreatedFromExternalProviderEvent notification, CancellationToken ct)
    {
        // Send welcome email (no verification needed)
        await _emailService.SendWelcomeEmailAsync(notification.Email);

        // Track analytics
        await _analyticsService.TrackExternalUserRegistrationAsync(
            notification.UserId, notification.Provider);
    }
}
```

---

### Q9: Should we create a new EntraAuthenticationService or extend JwtTokenService?

**Answer:** Create new `EntraExternalIdService` implementing `IExternalAuthenticationService`. Keep `JwtTokenService` unchanged.

**Reason:** Single Responsibility Principle - Entra handles token validation, JWT service generates LankaConnect tokens.

```csharp
// Infrastructure layer
public interface IExternalAuthenticationService
{
    Task<Result<ExternalAuthenticationResult>> ValidateTokenAsync(string accessToken, CancellationToken ct);
    Task<Result<UserClaims>> GetUserClaimsAsync(string accessToken, CancellationToken ct);
}

public class EntraExternalIdService : IExternalAuthenticationService
{
    public async Task<Result<ExternalAuthenticationResult>> ValidateTokenAsync(...)
    {
        // 1. Fetch OIDC configuration
        // 2. Download JWKS
        // 3. Verify JWT signature
        // 4. Validate claims
        // 5. Return user data
    }
}

// JwtTokenService (unchanged - still generates LankaConnect tokens)
public class JwtTokenService : IJwtTokenService
{
    public Task<Result<string>> GenerateAccessTokenAsync(User user)
    {
        // Generate 15-minute access token
    }
}
```

---

### Q10: Support both authentication methods during transition?

**Answer:** Yes, dual authentication mode with provider detection.

**Database Schema:**
```sql
-- Before: Email is globally unique
UNIQUE (Email)

-- After: Email is unique per provider
UNIQUE (Email, IdentityProvider)
```

**User can exist with same email but different providers:**
```
user@example.com | IdentityProvider.Local        | has PasswordHash
user@example.com | IdentityProvider.EntraExternal | has ExternalProviderId
```

**Frontend checks provider:**
```csharp
[HttpGet("check-provider")]
public async Task<IActionResult> CheckProvider([FromQuery] string email)
{
    var user = await _userRepository.GetByEmailAsync(Email.Create(email).Value);

    if (user == null)
        return Ok(new { exists = false });

    return Ok(new
    {
        exists = true,
        provider = user.IdentityProvider.ToString(),
        requiresPassword = user.IdentityProvider == IdentityProvider.Local
    });
}
```

---

### Q11: How to identify if user is "local JWT" vs "Entra External ID"?

**Answer:** Query `IdentityProvider` column.

```csharp
// Get user by email (returns first match)
var user = await _context.Users
    .FirstOrDefaultAsync(u => u.Email == email);

// Check provider
if (user.IdentityProvider == IdentityProvider.Local)
{
    // Local authentication
    var passwordValid = _passwordHashingService.VerifyPassword(password, user.PasswordHash);
}
else if (user.IdentityProvider == IdentityProvider.EntraExternal)
{
    // Redirect to Entra
    return Redirect($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize...");
}
```

---

### Q12: Database schema changes needed?

**Answer:** 3 column additions, 3 constraints, 3 indexes.

```sql
-- Columns
ALTER TABLE "Users"
ADD COLUMN "IdentityProvider" INT NOT NULL DEFAULT 0,
ADD COLUMN "ExternalProviderId" VARCHAR(255) NULL;

ALTER TABLE "Users"
ALTER COLUMN "PasswordHash" DROP NOT NULL;

-- Constraints
ALTER TABLE "Users"
ADD CONSTRAINT "CK_Users_IdentityProvider_Valid"
    CHECK ("IdentityProvider" IN (0, 1)),
ADD CONSTRAINT "CK_Users_PasswordHash_Required_For_Local"
    CHECK (("IdentityProvider" = 0 AND "PasswordHash" IS NOT NULL)
        OR ("IdentityProvider" != 0 AND "PasswordHash" IS NULL)),
ADD CONSTRAINT "CK_Users_ExternalProviderId_Required_For_External"
    CHECK (("IdentityProvider" = 0 AND "ExternalProviderId" IS NULL)
        OR ("IdentityProvider" != 0 AND "ExternalProviderId" IS NOT NULL));

-- Indexes
CREATE INDEX "IX_Users_IdentityProvider_ExternalProviderId"
    ON "Users" ("IdentityProvider", "ExternalProviderId")
    WHERE "ExternalProviderId" IS NOT NULL;

CREATE UNIQUE INDEX "IX_Users_ExternalProviderId_Unique"
    ON "Users" ("ExternalProviderId")
    WHERE "ExternalProviderId" IS NOT NULL;

CREATE UNIQUE INDEX "IX_Users_Email_IdentityProvider_Unique"
    ON "Users" ("Email", "IdentityProvider");
```

---

### Q13: How to write integration tests for Entra authentication?

**Answer:** Mock Entra service with test tokens.

```csharp
// Test helper
public class MockEntraExternalIdService : IExternalAuthenticationService
{
    public Task<Result<ExternalAuthenticationResult>> ValidateTokenAsync(string accessToken, CancellationToken ct)
    {
        // Test token format: "test-token:{oid}:{email}"
        var parts = accessToken.Split(':');

        if (parts[0] != "test-token")
            return Task.FromResult(Result<ExternalAuthenticationResult>.Failure("Invalid token"));

        var result = new ExternalAuthenticationResult(
            ProviderId: parts[1],
            Email: parts[2],
            FirstName: "Test",
            LastName: "User",
            IsEmailVerified: true,
            AdditionalClaims: new Dictionary<string, string>());

        return Task.FromResult(Result<ExternalAuthenticationResult>.Success(result));
    }
}

// Integration test
public class EntraAuthenticationTests : IClassFixture<WebApplicationFactory>
{
    [Fact]
    public async Task LoginWithEntra_NewUser_ShouldAutoProvision()
    {
        // Arrange
        var testToken = "test-token:entra-oid-123:newuser@example.com";

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login/entra",
            new { AccessToken = testToken });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.Value == "newuser@example.com");

        user.Should().NotBeNull();
        user.IdentityProvider.Should().Be(IdentityProvider.EntraExternal);
        user.ExternalProviderId.Should().Be("entra-oid-123");
    }
}
```

---

### Q14: Mock Entra tokens for unit tests?

**Answer:** Create JWT with test signing key.

```csharp
public static class EntraTestHelpers
{
    private static readonly SecurityKey TestSigningKey =
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-key-for-unit-tests-only-32-chars"));

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

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "https://login.microsoftonline.com/test-tenant/v2.0",
            Audience = "test-client-id",
            SigningCredentials = new SigningCredentials(
                TestSigningKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

// Usage in tests
[Fact]
public async Task ValidateTokenAsync_WithMockToken_ShouldSucceed()
{
    var mockToken = EntraTestHelpers.CreateMockEntraAccessToken(
        oid: "test-oid-123",
        email: "test@example.com");

    var result = await _entraService.ValidateTokenAsync(mockToken, CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    result.Value.ProviderId.Should().Be("test-oid-123");
    result.Value.Email.Should().Be("test@example.com");
}
```

---

## Code Snippets

### User Entity Factory Methods

```csharp
// Local user creation
public static Result<User> Create(
    Email email,
    string firstName,
    string lastName,
    UserRole role = UserRole.User)
{
    var user = new User(email, firstName, lastName, role);
    user.IdentityProvider = IdentityProvider.Local;
    user.ExternalProviderId = null;
    user.RaiseDomainEvent(new UserCreatedEvent(user.Id, email.Value, user.FullName));
    return Result<User>.Success(user);
}

// External provider user creation
public static Result<User> CreateFromExternalProvider(
    Email email,
    string firstName,
    string lastName,
    IdentityProvider provider,
    string externalProviderId,
    UserRole role = UserRole.User)
{
    if (provider == IdentityProvider.Local)
        return Result<User>.Failure("Use Create() for local users");

    var user = new User(email, firstName, lastName, role)
    {
        IdentityProvider = provider,
        ExternalProviderId = externalProviderId,
        IsEmailVerified = true,
        PasswordHash = null
    };

    user.RaiseDomainEvent(new UserCreatedFromExternalProviderEvent(
        user.Id, email.Value, user.FullName, provider, externalProviderId));

    return Result<User>.Success(user);
}
```

---

### Repository Methods

```csharp
// Get by external provider ID
public async Task<User?> GetByExternalProviderIdAsync(
    IdentityProvider provider,
    string externalProviderId,
    CancellationToken cancellationToken = default)
{
    return await _context.Users
        .FirstOrDefaultAsync(u =>
            u.IdentityProvider == provider &&
            u.ExternalProviderId == externalProviderId,
            cancellationToken);
}

// Check if external ID exists
public async Task<bool> ExistsWithExternalProviderIdAsync(
    IdentityProvider provider,
    string externalProviderId,
    CancellationToken cancellationToken = default)
{
    return await _context.Users
        .AnyAsync(u =>
            u.IdentityProvider == provider &&
            u.ExternalProviderId == externalProviderId,
            cancellationToken);
}
```

---

### Command Handler

```csharp
public class LoginWithEntraHandler : IRequestHandler<LoginWithEntraCommand, Result<LoginUserResponse>>
{
    private readonly IExternalAuthenticationService _entraService;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<LoginUserResponse>> Handle(
        LoginWithEntraCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate Entra token
        var validationResult = await _entraService.ValidateTokenAsync(
            request.AccessToken, cancellationToken);

        if (!validationResult.IsSuccess)
            return Result<LoginUserResponse>.Failure("Invalid Entra token");

        var authResult = validationResult.Value;

        // 2. Get or create user
        var email = Email.Create(authResult.Email).Value;
        var existingUser = await _userRepository.GetByExternalProviderIdAsync(
            IdentityProvider.EntraExternal,
            authResult.ProviderId,
            cancellationToken);

        User user;
        if (existingUser == null)
        {
            // Auto-provision
            var userResult = User.CreateFromExternalProvider(
                email,
                authResult.FirstName ?? "",
                authResult.LastName ?? "",
                IdentityProvider.EntraExternal,
                authResult.ProviderId);

            await _userRepository.AddAsync(userResult.Value, cancellationToken);
            user = userResult.Value;
        }
        else
        {
            user = existingUser;
        }

        // 3. Generate LankaConnect JWT
        var accessTokenResult = await _jwtTokenService.GenerateAccessTokenAsync(user);
        var refreshTokenResult = await _jwtTokenService.GenerateRefreshTokenAsync();

        var refreshToken = RefreshToken.Create(
            refreshTokenResult.Value,
            DateTime.UtcNow.AddDays(7),
            request.IpAddress).Value;

        user.AddRefreshToken(refreshToken);
        user.RecordSuccessfulLogin();

        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<LoginUserResponse>.Success(new LoginUserResponse(
            user.Id,
            user.Email.Value,
            user.FullName,
            user.Role,
            accessTokenResult.Value,
            refreshTokenResult.Value,
            DateTime.UtcNow.AddMinutes(15)));
    }
}
```

---

### API Controller

```csharp
[HttpPost("login/entra")]
[ProducesResponseType(typeof(LoginUserResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> LoginWithEntra(
    [FromBody] LoginWithEntraCommand request,
    CancellationToken cancellationToken)
{
    try
    {
        var ipAddress = GetClientIpAddress();
        var loginRequest = request with { IpAddress = ipAddress };

        var result = await _mediator.Send(loginRequest, cancellationToken);

        if (!result.IsSuccess)
            return Unauthorized(new { error = result.Error });

        SetRefreshTokenCookie(result.Value.RefreshToken);

        _logger.LogInformation(
            "User {UserId} authenticated via Entra External ID",
            result.Value.UserId);

        return Ok(new
        {
            user = new
            {
                result.Value.UserId,
                result.Value.Email,
                result.Value.FullName,
                result.Value.Role
            },
            result.Value.AccessToken,
            result.Value.TokenExpiresAt,
            authMethod = "entra-external"
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during Entra login");
        return StatusCode(500, new { error = "An error occurred during login" });
    }
}
```

---

## Implementation Checklist

### Phase 1: Domain Layer ✅
- [ ] Add `IdentityProvider` enum
- [ ] Add properties to User entity
- [ ] Implement `CreateFromExternalProvider()`
- [ ] Add `UserCreatedFromExternalProviderEvent`
- [ ] Enforce password management rules
- [ ] Enforce email verification rules
- [ ] Enforce account locking rules
- [ ] **25+ passing tests**

### Phase 2: Infrastructure Layer ✅
- [ ] Create database migration
- [ ] Apply migration to local/staging
- [ ] Update EF Core configuration
- [ ] Implement repository methods
- [ ] Create `IExternalAuthenticationService`
- [ ] Implement `EntraExternalIdService`
- [ ] **15+ integration tests**

### Phase 3: Application Layer ✅
- [ ] Create `LoginWithEntraCommand`
- [ ] Implement `LoginWithEntraHandler`
- [ ] Update `RegisterUserHandler`
- [ ] Update `LoginUserHandler`
- [ ] **30+ application tests**

### Phase 4: Presentation Layer ✅
- [ ] Add `/api/auth/login/entra` endpoint
- [ ] Add `/api/auth/check-provider` endpoint
- [ ] Update Swagger documentation
- [ ] **10+ E2E tests**

### Phase 5: Deployment ✅
- [ ] Deploy to staging
- [ ] Run E2E tests
- [ ] Deploy to production
- [ ] Monitor metrics
- [ ] Update documentation

---

## Testing Quick Reference

### Run Specific Test Suite

```bash
# Domain tests
dotnet test --filter "FullyQualifiedName~LankaConnect.Domain.Tests"

# User-related tests
dotnet test --filter "FullyQualifiedName~UserTests"

# Entra-specific tests
dotnet test --filter "FullyQualifiedName~Entra"

# Integration tests
dotnet test --filter "Category=Integration"

# E2E tests
dotnet test --filter "Category=E2E"
```

### Generate Coverage Report

```bash
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report"
start coverage-report/index.html
```

---

## Configuration Quick Reference

### Development (appsettings.Development.json)

```json
{
  "EntraExternalId": {
    "TenantId": "test-tenant-id",
    "ClientId": "test-client-id",
    "ClientSecret": "test-secret",
    "Domain": "test.onmicrosoft.com"
  },
  "Authentication": {
    "DefaultProvider": "Local",
    "AllowedProviders": ["Local"],
    "AutoProvisionExternalUsers": false
  }
}
```

### Production (appsettings.Production.json)

```json
{
  "EntraExternalId": {
    "TenantId": "369a3c47-33b7-4baa-98b8-6ddf16a51a31",
    "ClientId": "957e9865-fca0-4236-9276-a8643a7193b5",
    "ClientSecret": "***",  // From Azure Key Vault
    "Domain": "lankaconnect.onmicrosoft.com"
  },
  "Authentication": {
    "DefaultProvider": "Local",
    "AllowedProviders": ["Local", "EntraExternal"],
    "AutoProvisionExternalUsers": true
  }
}
```

---

## Troubleshooting

### Issue: Entra token validation fails

**Check:**
1. Token not expired (`exp` claim)
2. Correct `aud` (audience) claim matches ClientId
3. JWKS cache not stale
4. Network connectivity to Entra endpoints

**Fix:**
```csharp
// Clear JWKS cache
await _cache.RemoveAsync("entra:jwks");
```

---

### Issue: User auto-provisioning fails

**Check:**
1. Email claim present in Entra token
2. External provider ID extracted correctly
3. Database constraints not violated
4. User doesn't already exist with different provider

**Fix:**
```csharp
// Query user by external ID before provisioning
var existing = await _userRepository.GetByExternalProviderIdAsync(
    IdentityProvider.EntraExternal,
    externalProviderId);
```

---

### Issue: Tests failing after changes

**Check:**
1. All existing tests still passing
2. Test database up to date
3. Mock services configured correctly
4. Test data conflicts

**Fix:**
```bash
# Reset test database
dotnet ef database drop --context ApplicationDbContext --environment Test
dotnet ef database update --context ApplicationDbContext --environment Test
```

---

## Next Steps

1. ✅ Review all architecture documents
2. ✅ Set up test Entra tenant
3. ✅ Create feature branch: `feature/entra-external-id`
4. Begin implementation: [Phase 1, Step 1.1](./Entra-External-ID-Implementation-Roadmap.md#step-11-add-identityprovider-enum)

---

**Quick Reference Status:** ✅ Complete
**Last Updated:** 2025-10-28
