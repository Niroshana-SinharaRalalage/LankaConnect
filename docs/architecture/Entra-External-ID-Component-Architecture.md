# Entra External ID Integration - Component Architecture

**Status:** Design Complete
**Date:** 2025-10-28
**Related ADR:** ADR-002-Entra-External-ID-Integration

---

## Overview

This document provides detailed component architecture diagrams and interaction flows for Microsoft Entra External ID integration with LankaConnect's Clean Architecture system.

---

## Architecture Overview Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         PRESENTATION LAYER                               │
│                     (ASP.NET Core API Controllers)                       │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  AuthController                                                           │
│  ├── POST /api/auth/register         (Local users only)                 │
│  ├── POST /api/auth/login            (Local JWT authentication)         │
│  ├── POST /api/auth/login/entra      (NEW: Entra External ID)           │
│  ├── POST /api/auth/refresh          (Both providers)                   │
│  ├── POST /api/auth/logout           (Both providers)                   │
│  ├── POST /api/auth/verify-email     (Local only)                       │
│  └── POST /api/auth/reset-password   (Local only)                       │
│                                                                           │
└─────────────────────────────────────────────────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        APPLICATION LAYER                                 │
│                    (CQRS with MediatR Handlers)                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  Commands (Local Authentication):                                        │
│  ├── RegisterUserCommand           → RegisterUserHandler                │
│  ├── LoginUserCommand              → LoginUserHandler                   │
│  ├── RefreshTokenCommand           → RefreshTokenHandler                │
│  └── LogoutUserCommand             → LogoutUserHandler                  │
│                                                                           │
│  Commands (NEW - Entra Authentication):                                  │
│  ├── LoginWithEntraCommand         → LoginWithEntraHandler (NEW)        │
│  └── SyncEntraUserCommand          → SyncEntraUserHandler (NEW)         │
│                                                                           │
│  Interfaces:                                                              │
│  ├── IJwtTokenService              (Existing - generates LankaConnect JWT)│
│  ├── IPasswordHashingService       (Existing - BCrypt for local users)  │
│  └── IExternalAuthenticationService (NEW - validates Entra tokens)      │
│                                                                           │
└─────────────────────────────────────────────────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                          DOMAIN LAYER                                    │
│                    (Business Logic & Entities)                          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  User Aggregate:                                                          │
│  ├── Email (ValueObject)                                                 │
│  ├── IdentityProvider (Enum: Local | EntraExternal)                     │
│  ├── ExternalProviderId (string? - Entra OID)                           │
│  ├── PasswordHash (string? - nullable for external users)               │
│  └── Business Rules:                                                      │
│      ├── Local users MUST have PasswordHash                              │
│      ├── External users MUST have ExternalProviderId                     │
│      ├── Password operations only for Local users                        │
│      └── Email verification auto-completed for external users            │
│                                                                           │
│  Domain Events:                                                           │
│  ├── UserCreatedEvent                 (Local users)                      │
│  ├── UserCreatedFromExternalProviderEvent (NEW - External users)        │
│  ├── UserLoggedInEvent                (Both providers)                   │
│  ├── UserPasswordChangedEvent         (Local only)                       │
│  └── UserProviderLinkedEvent          (NEW - Account conversion)        │
│                                                                           │
│  Repository Interfaces:                                                   │
│  └── IUserRepository                                                      │
│      ├── GetByEmailAsync(email)                                          │
│      ├── GetByExternalProviderIdAsync(provider, id) (NEW)               │
│      └── ExistsWithExternalProviderIdAsync(provider, id) (NEW)          │
│                                                                           │
└─────────────────────────────────────────────────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      INFRASTRUCTURE LAYER                                │
│                  (External Services & Data Access)                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  Security Services:                                                       │
│  ├── JwtTokenService               (Existing - generates access/refresh)│
│  ├── PasswordHashingService        (Existing - BCrypt)                  │
│  └── EntraExternalIdService        (NEW - validates Entra tokens)       │
│                                                                           │
│  Data Access:                                                             │
│  ├── ApplicationDbContext          (EF Core)                             │
│  ├── UserRepository                (Implements IUserRepository)          │
│  └── UserConfiguration             (EF Core entity config)               │
│                                                                           │
│  External Dependencies:                                                   │
│  ├── PostgreSQL Database           (User profiles & local auth)          │
│  ├── Redis Cache                   (Token blacklist)                     │
│  └── Microsoft Entra External ID   (OAuth2/OIDC provider)               │
│      ├── Token validation endpoint: /.well-known/openid-configuration   │
│      ├── JWKS endpoint: /discovery/v2.0/keys                             │
│      └── UserInfo endpoint: /openid/v2.0/userinfo                        │
│                                                                           │
└─────────────────────────────────────────────────────────────────────────┘
                                    ▼
                    ┌─────────────────────────────────┐
                    │   Microsoft Entra External ID    │
                    │   (Identity Provider)            │
                    │   TenantId: 369a3c47...          │
                    │   ClientId: 957e9865...          │
                    └─────────────────────────────────┘
```

---

## Authentication Flow Diagrams

### Flow 1: Local Authentication (Existing - Unchanged)

```
┌──────────┐          ┌──────────────┐          ┌─────────────────┐          ┌──────────┐
│  Client  │          │ AuthController│          │LoginUserHandler │          │   User   │
│          │          │               │          │                 │          │ (Domain) │
└─────┬────┘          └──────┬───────┘          └────────┬────────┘          └────┬─────┘
      │                      │                            │                        │
      │ POST /auth/login     │                            │                        │
      │ {email, password}    │                            │                        │
      ├─────────────────────>│                            │                        │
      │                      │                            │                        │
      │                      │  LoginUserCommand          │                        │
      │                      ├───────────────────────────>│                        │
      │                      │                            │                        │
      │                      │                            │ GetByEmailAsync        │
      │                      │                            ├───────────────────────>│
      │                      │                            │                        │
      │                      │                            │ User (Local provider)  │
      │                      │                            │<───────────────────────┤
      │                      │                            │                        │
      │                      │                            │ Verify BCrypt password │
      │                      │                            │ (PasswordHashingService)│
      │                      │                            │                        │
      │                      │                            │ user.RecordSuccessfulLogin()
      │                      │                            │                        │
      │                      │                            │ Generate JWT tokens    │
      │                      │                            │ (JwtTokenService)      │
      │                      │                            │                        │
      │                      │ LoginUserResponse          │                        │
      │                      │<───────────────────────────┤                        │
      │                      │                            │                        │
      │  200 OK              │                            │                        │
      │  {accessToken, user} │                            │                        │
      │<─────────────────────┤                            │                        │
      │  Set-Cookie: refreshToken                         │                        │
      │                      │                            │                        │
```

---

### Flow 2: Entra External ID Authentication (NEW)

```
┌──────────┐    ┌─────────────┐    ┌───────────────────┐    ┌──────────────────┐    ┌──────────┐    ┌──────────┐
│  Client  │    │    Entra    │    │  AuthController   │    │LoginWithEntraHandler│    │   User   │    │PostgreSQL│
│          │    │ External ID │    │                   │    │                   │    │ (Domain) │    │ Database │
└────┬─────┘    └──────┬──────┘    └─────────┬─────────┘    └─────────┬─────────┘    └────┬─────┘    └────┬─────┘
     │                 │                      │                         │                   │               │
     │ 1. Redirect to Entra login             │                         │                   │               │
     │────────────────>│                      │                         │                   │               │
     │                 │                      │                         │                   │               │
     │ 2. User authenticates                  │                         │                   │               │
     │    (email/password or social login)    │                         │                   │               │
     │<────────────────│                      │                         │                   │               │
     │                 │                      │                         │                   │               │
     │ 3. Entra callback with access token    │                         │                   │               │
     │<────────────────│                      │                         │                   │               │
     │                 │                      │                         │                   │               │
     │ 4. POST /auth/login/entra              │                         │                   │               │
     │    {accessToken: "eyJ..."}             │                         │                   │               │
     ├───────────────────────────────────────>│                         │                   │               │
     │                 │                      │                         │                   │               │
     │                 │                      │ 5. LoginWithEntraCommand│                   │               │
     │                 │                      ├────────────────────────>│                   │               │
     │                 │                      │                         │                   │               │
     │                 │                      │                         │ 6. Validate token │               │
     │                 │                      │                         │    (EntraExternalIdService)       │
     │                 │<──────────────────────────────────────────────────────────────────┤               │
     │                 │     GET /.well-known/openid-configuration       │                                  │
     │                 │     Verify JWT signature with JWKS              │                                  │
     │                 ├────────────────────────────────────────────────>│                                  │
     │                 │                      │                         │                   │               │
     │                 │     Token valid: {oid, email, name}            │                   │               │
     │                 │<────────────────────────────────────────────────┤                   │               │
     │                 │                      │                         │                   │               │
     │                 │                      │                         │ 7. Get or create user             │
     │                 │                      │                         │    GetByExternalProviderIdAsync   │
     │                 │                      │                         ├──────────────────>│               │
     │                 │                      │                         │                   │               │
     │                 │                      │                         │                   │ SELECT WHERE  │
     │                 │                      │                         │                   │ ExternalProviderId=oid
     │                 │                      │                         │                   ├──────────────>│
     │                 │                      │                         │                   │               │
     │                 │                      │                         │                   │ User not found│
     │                 │                      │                         │                   │<──────────────┤
     │                 │                      │                         │                   │               │
     │                 │                      │                         │ 8. Auto-provision user            │
     │                 │                      │                         │    User.CreateFromExternalProvider()
     │                 │                      │                         │                   │               │
     │                 │                      │                         │    IdentityProvider = EntraExternal
     │                 │                      │                         │    ExternalProviderId = oid       │
     │                 │                      │                         │    PasswordHash = null            │
     │                 │                      │                         │    IsEmailVerified = true         │
     │                 │                      │                         │                   │               │
     │                 │                      │                         │                   │ INSERT new user
     │                 │                      │                         │                   ├──────────────>│
     │                 │                      │                         │                   │               │
     │                 │                      │                         │ 9. user.RecordSuccessfulLogin()   │
     │                 │                      │                         │                   │               │
     │                 │                      │                         │ 10. Generate LankaConnect JWT     │
     │                 │                      │                         │     (JwtTokenService)             │
     │                 │                      │                         │                   │               │
     │                 │                      │ 11. LoginUserResponse   │                   │               │
     │                 │                      │<────────────────────────┤                   │               │
     │                 │                      │                         │                   │               │
     │ 12. 200 OK       │                     │                         │                   │               │
     │     {accessToken, user}                │                         │                   │               │
     │<────────────────────────────────────────┤                         │                   │               │
     │     Set-Cookie: refreshToken           │                         │                   │               │
     │                 │                      │                         │                   │               │
     │ 13. Subsequent requests use LankaConnect JWT                      │                   │               │
     │     Authorization: Bearer eyJ...       │                         │                   │               │
     │────────────────────────────────────────>│                         │                   │               │
     │                 │                      │                         │                   │               │
```

**Key Points:**
1. Entra handles authentication (steps 1-3)
2. LankaConnect validates Entra token (step 6)
3. User auto-provisioned on first login (step 8)
4. LankaConnect issues its own JWT for API access (step 10)
5. Subsequent requests use LankaConnect JWT, not Entra token

---

### Flow 3: Dual Authentication Mode (During Migration)

```
┌──────────┐          ┌──────────────┐          ┌─────────────────┐
│  Client  │          │ AuthController│          │ Application Layer│
│          │          │               │          │                 │
└─────┬────┘          └──────┬───────┘          └────────┬────────┘
      │                      │                            │
      │ User enters email    │                            │
      │ to check auth method │                            │
      ├─────────────────────>│                            │
      │                      │                            │
      │                      │ GET /auth/check-provider   │
      │                      │ ?email=user@example.com    │
      │                      ├───────────────────────────>│
      │                      │                            │
      │                      │                            │ Query database:
      │                      │                            │ SELECT IdentityProvider
      │                      │                            │ WHERE Email = ?
      │                      │                            │
      │                      │ {provider: "Local"}        │
      │                      │<───────────────────────────┤
      │                      │                            │
      │ Show password form   │                            │
      │<─────────────────────┤                            │
      │                      │                            │
      │ POST /auth/login     │                            │
      │ {email, password}    │                            │
      ├─────────────────────>│                            │
      │                      │                            │
      │                      │  ... Local authentication flow ...
      │                      │                            │
      │                      │                            │
      │ Alt: If provider = "EntraExternal"                │
      │ Show "Login with Microsoft" button                │
      │<─────────────────────┤                            │
      │                      │                            │
      │ Redirect to Entra    │                            │
      ├─────────────────────>│                            │
      │                      │                            │
      │                      │  ... Entra authentication flow ...
      │                      │                            │
```

---

## Component Interaction Diagram

### EntraExternalIdService (NEW Infrastructure Service)

```
┌────────────────────────────────────────────────────────────────────────┐
│              EntraExternalIdService (Infrastructure)                    │
├────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  + ValidateTokenAsync(accessToken) : Result<ExternalAuthenticationResult>
│      ├─ 1. Fetch OIDC configuration from Entra                         │
│      │     GET https://login.microsoftonline.com/{tenant}/.well-known/ │
│      │                                           openid-configuration    │
│      ├─ 2. Download JWKS (public keys)                                 │
│      │     GET {jwks_uri}                                               │
│      ├─ 3. Validate JWT signature                                      │
│      │     using Microsoft.IdentityModel.Tokens                         │
│      ├─ 4. Verify claims:                                               │
│      │     - iss (issuer matches Entra)                                 │
│      │     - aud (audience matches ClientId)                            │
│      │     - exp (not expired)                                          │
│      │     - nbf (not before time)                                      │
│      └─ 5. Extract user claims (oid, email, name)                      │
│                                                                          │
│  + GetUserClaimsAsync(accessToken) : Result<UserClaims>                │
│      └─ Call Entra UserInfo endpoint with access token                 │
│         GET https://graph.microsoft.com/v1.0/me                         │
│                                                                          │
│  Dependencies:                                                           │
│  ├─ HttpClient (for API calls)                                          │
│  ├─ IOptions<EntraExternalIdOptions> (configuration)                   │
│  ├─ ILogger<EntraExternalIdService> (logging)                          │
│  └─ IDistributedCache (cache JWKS, OIDC config)                        │
│                                                                          │
└────────────────────────────────────────────────────────────────────────┘
```

### Configuration Model

```csharp
public class EntraExternalIdOptions
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Instance { get; set; } = "https://login.microsoftonline.com/";
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public bool ValidateTokens { get; set; } = true;
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);

    // Computed properties
    public string Authority => $"{Instance}{TenantId}";
    public string MetadataAddress => $"{Authority}/.well-known/openid-configuration";
}
```

---

## Layer Responsibilities

### Presentation Layer (API Controllers)

**Responsibilities:**
- Receive HTTP requests
- Validate request models (data annotations)
- Map requests to commands
- Handle HTTP-specific concerns (cookies, headers)
- Return appropriate HTTP status codes

**Does NOT:**
- Perform business logic
- Access database directly
- Validate Entra tokens
- Hash passwords

---

### Application Layer (Command Handlers)

**Responsibilities:**
- Orchestrate use cases
- Coordinate between domain and infrastructure
- Handle cross-cutting concerns (logging, transactions)
- Raise domain events
- Transform domain results to DTOs

**Does NOT:**
- Contain business rules (delegated to domain)
- Access database directly (uses repositories)
- Implement authentication logic (uses services)

---

### Domain Layer (Entities & Business Rules)

**Responsibilities:**
- Enforce business invariants
- Define domain events
- Encapsulate business logic
- Maintain aggregate consistency
- Provide domain-specific validation

**Does NOT:**
- Know about databases
- Know about HTTP
- Know about Entra tokens
- Know about infrastructure

---

### Infrastructure Layer (Services & Repositories)

**Responsibilities:**
- Implement repository interfaces
- Handle external API calls (Entra)
- Manage database access (EF Core)
- Implement caching strategies
- Configure external services

**Does NOT:**
- Contain business rules
- Expose implementation details to upper layers

---

## Dependency Injection Configuration

```csharp
// Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Database
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

    // Repositories
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IUnitOfWork, UnitOfWork>();

    // Existing authentication services
    services.AddScoped<IJwtTokenService, JwtTokenService>();
    services.AddScoped<IPasswordHashingService, PasswordHashingService>();

    // NEW: External authentication services
    services.AddScoped<IExternalAuthenticationService, EntraExternalIdService>();

    // Configure Entra External ID options
    services.Configure<EntraExternalIdOptions>(
        configuration.GetSection("EntraExternalId"));

    // Add HTTP client for Entra API calls with resilience
    services.AddHttpClient<IExternalAuthenticationService, EntraExternalIdService>()
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip
        })
        .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
        ));

    // Caching (Redis for production, InMemory for development)
    if (configuration.GetValue<bool>("UseRedis"))
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "LankaConnect:";
        });
    }
    else
    {
        services.AddDistributedMemoryCache();
    }

    return services;
}
```

---

## Security Considerations

### Token Validation Flow

```
┌────────────────────────────────────────────────────────────────────┐
│                     Entra Token Validation                          │
├────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  1. Parse JWT header                                                │
│     ├─ Extract "kid" (key ID)                                       │
│     └─ Extract "alg" (algorithm - must be RS256)                    │
│                                                                      │
│  2. Fetch JWKS from Entra (cached for 24 hours)                    │
│     ├─ GET /.well-known/openid-configuration                        │
│     ├─ Extract "jwks_uri"                                           │
│     └─ GET {jwks_uri}                                               │
│                                                                      │
│  3. Find matching public key by "kid"                               │
│     └─ If not found, refresh JWKS cache and retry                   │
│                                                                      │
│  4. Verify JWT signature                                            │
│     ├─ Use RSA public key from JWKS                                 │
│     └─ Reject if signature invalid                                  │
│                                                                      │
│  5. Validate standard claims                                        │
│     ├─ "iss" = https://login.microsoftonline.com/{tenantId}/v2.0   │
│     ├─ "aud" = {ClientId}                                           │
│     ├─ "exp" > CurrentTime (with 5min clock skew)                   │
│     ├─ "nbf" <= CurrentTime                                         │
│     └─ "iat" is reasonable (not too old)                            │
│                                                                      │
│  6. Extract user claims                                             │
│     ├─ "oid" (object ID - unique user identifier)                   │
│     ├─ "email" (user email address)                                 │
│     ├─ "given_name" (first name)                                    │
│     ├─ "family_name" (last name)                                    │
│     └─ "email_verified" (email verification status)                 │
│                                                                      │
│  7. Return validated user data to application layer                 │
│                                                                      │
└────────────────────────────────────────────────────────────────────┘
```

### Threat Mitigation

| Threat | Mitigation Strategy |
|--------|---------------------|
| Token replay attacks | Short-lived Entra tokens (1 hour), LankaConnect refresh tokens |
| Man-in-the-middle | HTTPS only, HSTS headers, certificate pinning |
| Token forgery | RSA signature verification with Entra JWKS |
| Account takeover | Entra handles MFA, account lockout policies |
| SQL injection | EF Core parameterized queries, unique constraints |
| Duplicate accounts | Unique index on (Email, IdentityProvider) |
| Brute force | Entra handles rate limiting for authentication |

---

## Performance Optimization

### Caching Strategy

```csharp
public class EntraExternalIdService : IExternalAuthenticationService
{
    private const string JwksCacheKey = "entra:jwks";
    private const string OidcConfigCacheKey = "entra:oidc-config";

    public async Task<Result<ExternalAuthenticationResult>> ValidateTokenAsync(
        string accessToken, CancellationToken cancellationToken)
    {
        // 1. Check cache for OIDC configuration (24 hour cache)
        var oidcConfig = await _cache.GetStringAsync(OidcConfigCacheKey, cancellationToken);

        if (string.IsNullOrEmpty(oidcConfig))
        {
            // Fetch from Entra and cache
            oidcConfig = await FetchOidcConfigurationAsync(cancellationToken);
            await _cache.SetStringAsync(
                OidcConfigCacheKey,
                oidcConfig,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                },
                cancellationToken);
        }

        // 2. Check cache for JWKS (24 hour cache)
        var jwks = await _cache.GetStringAsync(JwksCacheKey, cancellationToken);

        if (string.IsNullOrEmpty(jwks))
        {
            var config = JsonSerializer.Deserialize<OidcConfiguration>(oidcConfig);
            jwks = await FetchJwksAsync(config.JwksUri, cancellationToken);

            await _cache.SetStringAsync(
                JwksCacheKey,
                jwks,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                },
                cancellationToken);
        }

        // 3. Validate token with cached keys
        var validationResult = ValidateJwtToken(accessToken, jwks);

        return validationResult;
    }
}
```

**Cache Hit Rates:**
- OIDC configuration: ~99% (refreshes daily)
- JWKS: ~99% (refreshes daily or on key rotation)
- Token validation: No caching (security risk)

---

## Monitoring and Observability

### Metrics to Track

```csharp
// Custom metrics for Application Insights / Prometheus
public class AuthenticationMetrics
{
    // Login success/failure rates
    public Counter LocalLoginAttempts { get; set; }
    public Counter EntraLoginAttempts { get; set; }
    public Counter LoginSuccesses { get; set; }
    public Counter LoginFailures { get; set; }

    // Provider distribution
    public Gauge TotalLocalUsers { get; set; }
    public Gauge TotalEntraUsers { get; set; }

    // Performance
    public Histogram EntraTokenValidationDuration { get; set; }
    public Histogram JwtGenerationDuration { get; set; }

    // Errors
    public Counter EntraApiErrors { get; set; }
    public Counter DatabaseErrors { get; set; }
}
```

### Logging Strategy

```csharp
// Structured logging with Serilog
_logger.LogInformation(
    "User {UserId} authenticated via {Provider} from {IpAddress}",
    user.Id, user.IdentityProvider, ipAddress);

_logger.LogWarning(
    "Entra token validation failed: {Error}. Token: {TokenPrefix}...",
    error, accessToken[..20]);

_logger.LogError(exception,
    "Failed to fetch JWKS from Entra. Falling back to cached keys.");
```

---

## Testing Architecture

### Test Pyramid

```
                    /\
                   /  \
                  / E2E \           5% - Full Entra integration
                 /--------\
                /          \
               / Integration \      25% - Mock Entra responses
              /--------------\
             /                \
            /   Unit Tests     \   70% - Domain logic, token parsing
           /--------------------\
```

### Mock Entra Service (Test Doubles)

```csharp
public class MockEntraExternalIdService : IExternalAuthenticationService
{
    public Task<Result<ExternalAuthenticationResult>> ValidateTokenAsync(
        string accessToken, CancellationToken cancellationToken)
    {
        // Parse test token format: "test-token:{oid}:{email}"
        var parts = accessToken.Split(':');

        if (parts.Length != 3 || parts[0] != "test-token")
            return Task.FromResult(Result<ExternalAuthenticationResult>.Failure("Invalid test token"));

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
```

---

## Deployment Considerations

### Environment Configuration

| Environment | Entra Config | Database | Caching |
|-------------|--------------|----------|---------|
| Development | Test tenant | LocalDB/PostgreSQL | InMemory |
| Staging | Production tenant (non-prod app) | Azure PostgreSQL | Redis |
| Production | Production tenant | Azure PostgreSQL | Redis Cluster |

### Rollout Strategy

1. **Week 1:** Deploy infrastructure changes (database migration)
2. **Week 2:** Deploy Entra service (disabled via feature flag)
3. **Week 3:** Enable Entra login for internal testing (10% traffic)
4. **Week 4:** Gradual rollout to all users (100% traffic)
5. **Week 5:** Deprecate local registration (Entra only for new users)

---

**Review Status:** ✅ Architecture Design Complete
**Next Steps:** Implement Phase 1 (Domain Layer) with TDD
