# Epic 1 Phase 2: Social Login Architecture - Executive Summary

**Date:** 2025-11-01
**Architect:** System Architecture Designer
**For:** LankaConnect Development Team

---

## Quick Answers to Your Questions

### 1. Domain Model Design

**Decision:** Keep `IdentityProvider` enum with only TWO values (Local, EntraExternal)

**Why?** Azure Entra External ID **federates** all social providers. The authentication flow is:
- User → Social Provider (Facebook/Google/Apple) → **Entra External ID** → LankaConnect

Entra is the actual identity provider. Facebook/Google/Apple are **upstream federation sources**.

```csharp
// ✅ CORRECT
public enum IdentityProvider
{
    Local = 0,           // Email/password
    EntraExternal = 1    // All social providers go through Entra
}

// ❌ WRONG (Don't do this)
public enum IdentityProvider
{
    Local = 0,
    Facebook = 2,   // Wrong - Facebook isn't the IdP, Entra is
    Google = 3,     // Wrong - Google isn't the IdP, Entra is
    Apple = 4       // Wrong - Apple isn't the IdP, Entra is
}
```

**NEW Enum for Social Providers:**
```csharp
public enum FederatedProvider
{
    Microsoft = 0,   // Direct Entra login
    Facebook = 1,    // Federated via Entra
    Google = 2,      // Federated via Entra
    Apple = 3        // Federated via Entra
}
```

This maps to the `idp` claim in Entra tokens:
- `"idp": "facebook.com"` → `FederatedProvider.Facebook`
- `"idp": "google.com"` → `FederatedProvider.Google`
- `"idp": "appleid.apple.com"` → `FederatedProvider.Apple`

---

### 2. Multi-Provider Support

**Decision:** Replace single `ExternalProviderId` string with `ExternalLogin` value object collection

```csharp
// BEFORE (Phase 1)
public class User
{
    public string? ExternalProviderId { get; private set; }  // Single provider
}

// AFTER (Phase 2)
public class User
{
    private readonly List<ExternalLogin> _externalLogins = new();
    public IReadOnlyCollection<ExternalLogin> ExternalLogins => _externalLogins.AsReadOnly();
}

public class ExternalLogin : ValueObject
{
    public FederatedProvider Provider { get; private set; }     // Facebook/Google/Apple
    public string ExternalProviderId { get; private set; }      // Entra OID claim
    public string ProviderEmail { get; private set; }           // Social provider email
    public DateTime LinkedAt { get; private set; }              // When linked
}
```

**Benefits:**
- One user can link Facebook + Google + Apple simultaneously
- Clean relational model with junction table
- DDD value object pattern (immutable, equality by value)
- Easy to query: "Show me all users who linked Facebook"

---

### 3. Authentication Flow

**Decision:** Reuse and enhance `LoginWithEntraCommand` - NO separate commands needed

**Why?** All social logins flow through the same Entra endpoint. The `idp` claim tells us which provider was used.

```csharp
public class LoginWithEntraCommandHandler
{
    public async Task<Result<LoginWithEntraResponse>> Handle(...)
    {
        // 1. Validate Entra token (works for ALL federated providers)
        var userInfo = await _entraService.GetUserInfoAsync(request.AccessToken);

        // 2. Extract federated provider from 'idp' claim (NEW in Phase 2)
        var federatedProvider = FederatedProviderExtensions.FromIdpClaimValue(
            userInfo.IdentityProvider);  // "facebook.com" → FederatedProvider.Facebook

        // 3. Find user by social login (NEW repository method)
        var user = await _userRepository.GetByExternalLoginAsync(
            federatedProvider,
            userInfo.ObjectId,
            cancellationToken);

        // 4. Auto-provision if new user (enhanced factory method)
        if (user == null)
        {
            user = User.CreateFromExternalProvider(
                IdentityProvider.EntraExternal,
                userInfo.ObjectId,
                email,
                userInfo.FirstName,
                userInfo.LastName,
                federatedProvider,  // NEW parameter
                userInfo.Email,
                UserRole.User);
        }

        // 5. Generate LankaConnect JWT tokens (same as Phase 1)
        // ...
    }
}
```

**Flow Diagram:**
```
User clicks "Login with Facebook"
  ↓
Frontend redirects to Entra External ID
  ↓
Entra redirects to Facebook OAuth
  ↓
Facebook authenticates user
  ↓
Entra issues token with claims:
  - oid: "entra-user-id-12345"
  - email: "user@facebook.com"
  - idp: "facebook.com"  ← This tells us it's Facebook
  ↓
Frontend POST /api/auth/login/entra with Entra token
  ↓
LoginWithEntraCommandHandler validates token
  ↓
Extracts idp claim → FederatedProvider.Facebook
  ↓
Finds or creates user with Facebook external login
  ↓
Returns LankaConnect JWT tokens
```

---

### 4. Linking/Unlinking Social Accounts

**New Commands:**

```csharp
// Link a social provider to existing account
POST /api/auth/link-provider
{
  "entraAccessToken": "eyJ..."  // Token from social login
}

// Handler
public class LinkExternalProviderCommandHandler
{
    public async Task<Result> Handle(...)
    {
        // Security: Verify current user
        if (currentUserId != request.UserId)
            return Unauthorized();

        // Validate social token
        var userInfo = await _entraService.GetUserInfoAsync(request.EntraAccessToken);
        var provider = FromIdpClaimValue(userInfo.IdentityProvider);

        // Prevent hijacking: Check if already linked to ANOTHER user
        var existingUser = await _repo.GetByExternalLoginAsync(provider, userInfo.ObjectId);
        if (existingUser != null && existingUser.Id != currentUserId)
            return Failure("Already linked to another account");

        // Link provider
        user.LinkExternalProvider(provider, userInfo.ObjectId, userInfo.Email);
        await _unitOfWork.CommitAsync();
    }
}

// Unlink a social provider
DELETE /api/auth/unlink-provider/Facebook

// Handler
public class UnlinkExternalProviderCommandHandler
{
    public async Task<Result> Handle(...)
    {
        // Business rule validation in domain
        var result = user.UnlinkExternalProvider(
            FederatedProvider.Facebook,
            externalProviderId);

        // Domain prevents unlinking last auth method
        if (result.IsFailure)
            return result;  // "Cannot unlink last authentication method"
    }
}
```

**Domain Business Rules:**
```csharp
public class User
{
    public Result UnlinkExternalProvider(FederatedProvider provider, string externalId)
    {
        var login = _externalLogins.Find(el => el.Provider == provider);
        if (login == null)
            return Failure("Provider not linked");

        // Business Rule: Must keep at least one authentication method
        bool hasPassword = !string.IsNullOrEmpty(PasswordHash);
        bool hasOtherProviders = _externalLogins.Count > 1;

        if (!hasPassword && !hasOtherProviders)
            return Failure("Cannot unlink your only authentication method. Add a password or link another provider first.");

        _externalLogins.Remove(login);
        RaiseDomainEvent(new ExternalProviderUnlinkedEvent(...));
        return Success();
    }
}
```

---

### 5. Database Schema

**Recommendation:** Junction table for `external_logins`

```sql
CREATE TABLE "users"."external_logins" (
    "id" SERIAL PRIMARY KEY,
    "user_id" UUID NOT NULL,
    "provider" INT NOT NULL,  -- FederatedProvider enum (0=MS, 1=FB, 2=Google, 3=Apple)
    "external_provider_id" VARCHAR(255) NOT NULL,  -- Entra OID
    "provider_email" VARCHAR(255) NOT NULL,
    "linked_at" TIMESTAMP NOT NULL,

    CONSTRAINT "fk_external_logins_users"
        FOREIGN KEY ("user_id") REFERENCES "users"."users"("id")
        ON DELETE CASCADE,

    -- Prevent duplicate linking
    CONSTRAINT "uk_external_logins_provider_external_id"
        UNIQUE ("provider", "external_provider_id")
);

-- Index for user lookup
CREATE INDEX "ix_external_logins_user_id"
ON "users"."external_logins"("user_id");

-- Index for authentication (find user by social login)
CREATE INDEX "ix_external_logins_provider_external_id"
ON "users"."external_logins"("provider", "external_provider_id");
```

**EF Core Configuration:**
```csharp
builder.OwnsMany(u => u.ExternalLogins, login =>
{
    login.WithOwner().HasForeignKey("UserId");
    login.ToTable("external_logins", "users");

    login.Property(l => l.Provider)
        .HasConversion<int>()
        .IsRequired();

    login.Property(l => l.ExternalProviderId)
        .HasMaxLength(255)
        .IsRequired();

    login.HasIndex(l => new { l.Provider, l.ExternalProviderId })
        .IsUnique();
});

builder.Navigation(u => u.ExternalLogins).AutoInclude();
```

**Migration Strategy:**
```sql
-- Migrate Phase 1 users to junction table
INSERT INTO external_logins (user_id, provider, external_provider_id, provider_email, linked_at)
SELECT
    id,
    0,  -- FederatedProvider.Microsoft (Phase 1 default)
    external_provider_id,
    email,
    created_at
FROM users
WHERE identity_provider = 1 AND external_provider_id IS NOT NULL;

-- Drop deprecated column (after verification)
-- ALTER TABLE users DROP COLUMN external_provider_id;
```

---

### 6. Command/Query Structure (CQRS)

**Commands:**
```csharp
// EXISTING (Enhanced)
LoginWithEntraCommand
  - AccessToken: string
  - IpAddress: string?
  → Returns: LoginWithEntraResponse (includes FederatedProvider)

// NEW
LinkExternalProviderCommand
  - UserId: Guid
  - EntraAccessToken: string
  → Returns: LinkExternalProviderResponse

UnlinkExternalProviderCommand
  - UserId: Guid
  - Provider: FederatedProvider
  - ExternalProviderId: string
  → Returns: Result (success/failure)
```

**Queries:**
```csharp
// NEW
GetLinkedProvidersQuery
  - UserId: Guid
  → Returns: List<LinkedProviderDto>
    {
      Provider: FederatedProvider,
      DisplayName: string,
      Email: string,
      LinkedAt: DateTime
    }
  → Returns: HasPassword: bool
```

**Domain Events:**
```csharp
// UPDATED (Phase 1)
UserCreatedFromExternalProviderEvent
  - UserId
  - Email
  - FullName
  - IdentityProvider (EntraExternal)
  - FederatedProvider (NEW: Microsoft/Facebook/Google/Apple)
  - ExternalProviderId

// NEW (Phase 2)
ExternalProviderLinkedEvent
  - UserId
  - Email
  - Provider: FederatedProvider
  - ExternalProviderId
  - ProviderEmail

ExternalProviderUnlinkedEvent
  - UserId
  - Email
  - Provider: FederatedProvider
  - ExternalProviderId
```

---

### 7. Security Considerations

#### **Prevent Account Hijacking During Linking**

**Attack Scenario:**
1. Attacker creates Facebook account with victim's email: `victim@example.com`
2. Attacker tries to link this Facebook account to victim's LankaConnect account
3. Attacker gains access to victim's account

**Mitigation:**
```csharp
// In LinkExternalProviderCommandHandler
var existingUser = await _repo.GetByExternalLoginAsync(provider, externalProviderId);

if (existingUser != null && existingUser.Id != currentUserId)
{
    return Result.Failure("This social account is already linked to another user");
}
```

**Additional Security:**
- Require active session (authenticated user) before linking
- Validate `currentUserId` from JWT matches `command.UserId`
- Log all link/unlink operations for audit trail

#### **Prevent Unlinking Last Authentication Method**

**Attack Scenario:**
1. User has only Facebook login (no password)
2. User accidentally unlinks Facebook
3. User is locked out of account

**Mitigation:**
```csharp
public Result UnlinkExternalProvider(...)
{
    bool hasPassword = !string.IsNullOrEmpty(PasswordHash);
    bool hasOtherProviders = _externalLogins.Count > 1;

    if (!hasPassword && !hasOtherProviders)
        return Failure("Cannot unlink your only authentication method");

    // Safe to unlink
}
```

#### **Email Verification Trust**

Entra External ID pre-verifies emails for all federated providers:
```csharp
user.IsEmailVerified = true;  // Safe - Entra validates social provider emails
```

---

## Architecture Decision Summary

| Decision Point | Recommendation | Rationale |
|----------------|----------------|-----------|
| **IdentityProvider Enum** | Keep 2 values (Local, EntraExternal) | Entra federates all social providers |
| **Social Provider Tracking** | New `FederatedProvider` enum | Maps to `idp` claim in Entra tokens |
| **Multi-Provider Storage** | `ExternalLogin` value object collection | DDD pattern, supports multiple linked accounts |
| **Database Schema** | Junction table `external_logins` | Relational integrity, indexed lookups |
| **Authentication Command** | Reuse `LoginWithEntraCommand` | All social logins use same Entra endpoint |
| **Linking Commands** | New `Link/UnlinkExternalProviderCommand` | Explicit user consent required |
| **Business Rules** | Domain-enforced constraints | Prevent lockouts and hijacking |

---

## Implementation Phases

### Phase 2A: Domain Layer (Week 1)
- Create `FederatedProvider` enum
- Create `ExternalLogin` value object
- Update `User` aggregate with linking methods
- Write 20+ domain tests

### Phase 2B: Infrastructure (Week 2)
- Create `external_logins` junction table
- Migrate Phase 1 users
- Update EF Core configuration
- Enhance `EntraUserInfo` DTO with `idp` claim

### Phase 2C: Application Layer (Week 3)
- Update `LoginWithEntraCommandHandler`
- Create `LinkExternalProviderCommand`
- Create `UnlinkExternalProviderCommand`
- Create `GetLinkedProvidersQuery`

### Phase 2D: API & Testing (Week 4)
- Add API endpoints
- E2E tests for all social providers
- Security testing (hijacking, lockout scenarios)

### Phase 2E: Azure Configuration (Week 5)
- Configure Facebook in Entra
- Configure Google in Entra
- Configure Apple in Entra
- Production deployment

---

## Testing Strategy

### Unit Tests (Domain)
```csharp
[Fact]
public void LinkExternalProvider_WithDuplicateProvider_ShouldFail()
[Fact]
public void UnlinkExternalProvider_WhenLastMethod_ShouldFail()
[Fact]
public void User_CanHaveMultipleLinkedProviders()
```

### Integration Tests (Infrastructure)
```csharp
[Theory]
[InlineData("facebook.com", FederatedProvider.Facebook)]
[InlineData("google.com", FederatedProvider.Google)]
public void FromIdpClaimValue_ShouldParseCorrectly(...)
```

### E2E Tests (API)
```csharp
[Fact]
public async Task LoginWithFacebook_NewUser_ShouldAutoProvision()
[Fact]
public async Task LinkGoogleAccount_WhenAlreadyLinkedToAnother_ShouldFail()
[Fact]
public async Task UnlinkProvider_WhenLastMethod_ShouldFail()
```

---

## Migration Path for Existing Users

**Phase 1 Users (Entra only):**
```sql
-- Automatically migrated to junction table
INSERT INTO external_logins (user_id, provider, external_provider_id, provider_email)
SELECT id, 0 /* Microsoft */, external_provider_id, email
FROM users
WHERE identity_provider = 1;
```

**No Breaking Changes:**
- Existing `LoginWithEntraCommand` still works
- Response enhanced with `FederatedProvider` field
- Frontend can ignore new field until Phase 2 UI is ready

---

## Key Takeaways

1. **Don't expand IdentityProvider enum** - Entra is the IdP for all social logins
2. **Use FederatedProvider enum** - Tracks which social platform via `idp` claim
3. **Junction table pattern** - Clean, scalable, performant
4. **Reuse LoginWithEntraCommand** - All social logins flow through same handler
5. **Explicit linking required** - Security: prevent auto-linking on email match
6. **Domain enforces safety** - Cannot unlink last authentication method
7. **Backward compatible** - Phase 1 users migrate seamlessly

---

## References

- Full technical details: [ADR-003-Social-Login-Multi-Provider-Architecture.md](./ADR-003-Social-Login-Multi-Provider-Architecture.md)
- Phase 1 implementation: [ADR-002-Entra-External-ID-Integration.md](./ADR-002-Entra-External-ID-Integration.md)
- Azure Entra Federation: https://learn.microsoft.com/en-us/entra/external-id/identity-providers

---

**Questions?** Review ADR-003 for complete code samples and detailed rationale.
