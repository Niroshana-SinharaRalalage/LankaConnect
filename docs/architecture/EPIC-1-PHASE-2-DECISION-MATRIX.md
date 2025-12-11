# Epic 1 Phase 2: Architectural Decision Matrix

**Date:** 2025-11-01
**Purpose:** Decision comparison and team discussion guide

---

## Decision 1: How to Model Social Providers in Domain Layer?

### Option A: Keep Single IdentityProvider Enum (RECOMMENDED)

```csharp
public enum IdentityProvider { Local = 0, EntraExternal = 1 }
public enum FederatedProvider { Microsoft = 0, Facebook = 1, Google = 2, Apple = 3 }
```

| Criteria | Rating | Reasoning |
|----------|--------|-----------|
| **Architectural Correctness** | ⭐⭐⭐⭐⭐ | Entra IS the identity provider; social platforms are federation sources |
| **DDD Alignment** | ⭐⭐⭐⭐⭐ | Clear ubiquitous language: "authenticated via Entra, federated from Facebook" |
| **Schema Simplicity** | ⭐⭐⭐⭐⭐ | Single `identity_provider` column, no schema changes |
| **Query Performance** | ⭐⭐⭐⭐⭐ | Single index on `identity_provider` column |
| **Future Extensibility** | ⭐⭐⭐⭐⭐ | Add new federated providers without schema migrations |
| **Backward Compatibility** | ⭐⭐⭐⭐⭐ | Phase 1 Entra users remain valid |
| **Code Complexity** | ⭐⭐⭐⭐ | Requires mapping `idp` claim to `FederatedProvider` enum |

**Pros:**
- ✅ Architecturally accurate (Entra is the IdP)
- ✅ No breaking changes to existing code
- ✅ Easy to add LinkedIn, Twitter, etc. later
- ✅ Clear separation: authentication (Entra) vs federation source (Facebook)

**Cons:**
- ⚠️ Requires parsing `idp` claim from Entra tokens
- ⚠️ Two enums to maintain (IdentityProvider + FederatedProvider)

---

### Option B: Expand IdentityProvider Enum (REJECTED)

```csharp
public enum IdentityProvider {
    Local = 0,
    EntraExternal = 1,
    Facebook = 2,  // Misleading - not direct IdP
    Google = 3,
    Apple = 4
}
```

| Criteria | Rating | Reasoning |
|----------|--------|-----------|
| **Architectural Correctness** | ⭐⭐ | Misleading - Facebook isn't the IdP, Entra is |
| **DDD Alignment** | ⭐⭐ | Violates ubiquitous language - conflates authentication and federation |
| **Schema Simplicity** | ⭐⭐⭐⭐ | Single enum column |
| **Query Performance** | ⭐⭐⭐ | Multiple index entries needed |
| **Future Extensibility** | ⭐⭐ | Schema migrations for each new provider |
| **Backward Compatibility** | ⭐⭐⭐ | Requires data migration for Phase 1 users |
| **Code Complexity** | ⭐⭐⭐⭐ | Simple enum mapping |

**Pros:**
- ✅ Single enum (simpler on surface)
- ✅ No need to parse `idp` claim

**Cons:**
- ❌ Architecturally incorrect (Entra is the IdP, not Facebook)
- ❌ Breaks Single Responsibility Principle
- ❌ Harder to extend (LinkedIn, Twitter, etc.)
- ❌ Confusing for developers ("Why is Facebook an identity provider when we use Entra?")

---

### Option C: Separate User Tables (FacebookUser, GoogleUser) (REJECTED)

```csharp
public class FacebookUser : User { }
public class GoogleUser : User { }
```

| Criteria | Rating | Reasoning |
|----------|--------|-----------|
| **Architectural Correctness** | ⭐ | Violates DRY, breaks User aggregate |
| **DDD Alignment** | ⭐ | Multiple aggregates for same business concept |
| **Schema Simplicity** | ⭐ | Separate tables, complex joins |
| **Query Performance** | ⭐⭐ | UNION queries needed for "all users" |
| **Future Extensibility** | ⭐ | New table for each provider |
| **Backward Compatibility** | ⭐ | Major refactor of existing code |
| **Code Complexity** | ⭐ | Polymorphism overhead, duplicated logic |

**Pros:**
- ✅ Type safety per provider (minimal benefit)

**Cons:**
- ❌ Massive code duplication
- ❌ Breaks User aggregate pattern
- ❌ Complex querying (UNION all tables)
- ❌ Nightmare for multi-provider users

---

## Decision 2: How to Store Multiple Linked Providers?

### Option A: ExternalLogin Value Object Collection (RECOMMENDED)

```csharp
public class User {
    private readonly List<ExternalLogin> _externalLogins = new();
}
public class ExternalLogin : ValueObject {
    public FederatedProvider Provider { get; }
    public string ExternalProviderId { get; }
}
```

**Database:** Junction table `external_logins`

| Criteria | Rating | Reasoning |
|----------|--------|-----------|
| **DDD Alignment** | ⭐⭐⭐⭐⭐ | Value object collection pattern |
| **Relational Integrity** | ⭐⭐⭐⭐⭐ | Foreign keys, constraints enforced |
| **Query Performance** | ⭐⭐⭐⭐⭐ | Indexed lookups, efficient joins |
| **Type Safety** | ⭐⭐⭐⭐⭐ | Strongly typed domain model |
| **EF Core Support** | ⭐⭐⭐⭐⭐ | Native `OwnsMany` support |
| **Multi-Provider Support** | ⭐⭐⭐⭐⭐ | Unlimited linked providers |
| **Code Complexity** | ⭐⭐⭐⭐ | Standard EF Core configuration |

**Pros:**
- ✅ Clean relational model
- ✅ Enforces unique constraints (one Facebook account per user)
- ✅ Fast authentication lookups via indexes
- ✅ Easy to query: "Show users who linked Facebook"
- ✅ DDD value object pattern

**Cons:**
- ⚠️ Requires junction table migration
- ⚠️ Slightly more complex EF Core configuration

---

### Option B: JSON Column for ExternalLogins (REJECTED)

```csharp
public class User {
    public string ExternalLoginsJson { get; set; }  // JSON array
}
```

| Criteria | Rating | Reasoning |
|----------|--------|-----------|
| **DDD Alignment** | ⭐⭐ | Primitive obsession anti-pattern |
| **Relational Integrity** | ⭐ | No foreign keys, no constraints |
| **Query Performance** | ⭐ | Full JSON column scans |
| **Type Safety** | ⭐⭐ | Serialization/deserialization overhead |
| **EF Core Support** | ⭐⭐⭐ | JSON column support varies by database |
| **Multi-Provider Support** | ⭐⭐⭐ | Manual array management |
| **Code Complexity** | ⭐⭐⭐ | JSON serialization logic needed |

**Pros:**
- ✅ No junction table needed
- ✅ Atomic updates (single row)

**Cons:**
- ❌ Cannot query "users with Facebook login" efficiently
- ❌ No database-level constraints
- ❌ JSON parsing overhead
- ❌ Violates DDD value object pattern
- ❌ Harder to debug (opaque JSON blob)

---

### Option C: Single ExternalProviderId String (Phase 1 Approach) (REJECTED)

```csharp
public class User {
    public string? ExternalProviderId { get; set; }  // Single provider only
}
```

| Criteria | Rating | Reasoning |
|----------|--------|-----------|
| **DDD Alignment** | ⭐⭐⭐ | Simple primitive property |
| **Relational Integrity** | ⭐⭐⭐⭐ | Single column, simple |
| **Query Performance** | ⭐⭐⭐⭐ | Indexed column |
| **Type Safety** | ⭐⭐⭐ | String primitive |
| **EF Core Support** | ⭐⭐⭐⭐⭐ | Native support |
| **Multi-Provider Support** | ⭐ | **CANNOT support multiple providers** |
| **Code Complexity** | ⭐⭐⭐⭐⭐ | Simplest approach |

**Pros:**
- ✅ Simple implementation
- ✅ Fast queries

**Cons:**
- ❌ **CANNOT link multiple social providers** (deal-breaker for Phase 2)
- ❌ Users forced to choose one provider only
- ❌ No way to identify which provider (Facebook vs Google)

---

## Decision 3: How to Handle Account Linking?

### Option A: Explicit User Consent Required (RECOMMENDED)

**Flow:**
1. User logs into existing account
2. User navigates to Settings → Linked Accounts
3. User clicks "Link Facebook"
4. POST `/api/auth/link-provider` with Entra token
5. Backend validates user identity + checks for hijacking

| Criteria | Rating | Reasoning |
|----------|--------|-----------|
| **Security** | ⭐⭐⭐⭐⭐ | Prevents account hijacking |
| **User Control** | ⭐⭐⭐⭐⭐ | User explicitly chooses which providers to link |
| **OWASP Compliance** | ⭐⭐⭐⭐⭐ | Follows account linking best practices |
| **User Experience** | ⭐⭐⭐⭐ | Requires extra step, but safer |
| **Code Complexity** | ⭐⭐⭐⭐ | Separate command handler needed |

**Security Checks:**
```csharp
// Check 1: Verify current user (prevent unauthorized linking)
if (currentUserId != command.UserId)
    return Unauthorized();

// Check 2: Check if provider already linked to ANOTHER user
var existingUser = await GetByExternalLoginAsync(provider, externalId);
if (existingUser != null && existingUser.Id != currentUserId)
    return Failure("Already linked to another account");
```

**Pros:**
- ✅ Most secure approach
- ✅ Prevents hijacking attacks
- ✅ User in full control
- ✅ Clear audit trail

**Cons:**
- ⚠️ Requires extra UI (Settings page)
- ⚠️ Extra step for users (not auto-linked)

---

### Option B: Auto-Link on Email Match (REJECTED)

**Flow:**
1. User logs in with Facebook
2. Email matches existing account
3. Backend automatically links Facebook to account

| Criteria | Rating | Reasoning |
|----------|--------|-----------|
| **Security** | ⭐⭐ | **VULNERABLE to account hijacking** |
| **User Control** | ⭐⭐ | No user consent |
| **OWASP Compliance** | ⭐ | Violates account linking best practices |
| **User Experience** | ⭐⭐⭐⭐⭐ | Seamless, no extra steps |
| **Code Complexity** | ⭐⭐⭐⭐ | Simpler (no separate command) |

**Security Risk:**
```
Attack Scenario:
1. Attacker creates Facebook account with victim@example.com
2. Attacker logs in with Facebook
3. Backend auto-links to victim's existing account
4. Attacker gains access to victim's data
```

**Pros:**
- ✅ Better UX (seamless linking)

**Cons:**
- ❌ **CRITICAL SECURITY FLAW** - account hijacking
- ❌ User has no control
- ❌ OWASP violation
- ❌ No audit trail for linking

---

## Decision 4: How to Prevent Account Lockouts?

### Option A: Domain-Enforced Business Rule (RECOMMENDED)

```csharp
public Result UnlinkExternalProvider(FederatedProvider provider, string externalId)
{
    bool hasPassword = !string.IsNullOrEmpty(PasswordHash);
    bool hasOtherProviders = _externalLogins.Count > 1;

    if (!hasPassword && !hasOtherProviders)
        return Failure("Cannot unlink your only authentication method");

    // Safe to unlink
}
```

| Criteria | Rating | Reasoning |
|----------|--------|-----------|
| **Data Integrity** | ⭐⭐⭐⭐⭐ | Always enforced at domain level |
| **Testability** | ⭐⭐⭐⭐⭐ | Pure domain logic, easy to test |
| **DDD Alignment** | ⭐⭐⭐⭐⭐ | Business rule in aggregate |
| **Code Location** | ⭐⭐⭐⭐⭐ | Domain layer (correct place) |
| **Bypass Risk** | ⭐⭐⭐⭐⭐ | Cannot be bypassed |

**Pros:**
- ✅ Enforced in domain layer (impossible to bypass)
- ✅ Easy to test (unit tests)
- ✅ Clear business rule
- ✅ Prevents user lockouts

**Cons:**
- None (this is the correct approach)

---

### Option B: Application Layer Validation (REJECTED)

```csharp
// In UnlinkExternalProviderCommandHandler
if (user.ExternalLogins.Count == 1 && string.IsNullOrEmpty(user.PasswordHash))
    return Failure("Cannot unlink");
```

| Criteria | Rating | Reasoning |
|----------|--------|-----------|
| **Data Integrity** | ⭐⭐⭐ | Can be bypassed if rule not duplicated everywhere |
| **Testability** | ⭐⭐⭐ | Requires mocking application layer |
| **DDD Alignment** | ⭐⭐ | Business rule in wrong layer |
| **Code Location** | ⭐⭐ | Application layer (should be domain) |
| **Bypass Risk** | ⭐⭐ | Could be bypassed by other commands |

**Cons:**
- ❌ Business rule in wrong layer
- ❌ Could be forgotten in future commands
- ❌ Violates DDD principles

---

### Option C: Database Constraint (REJECTED)

```sql
ALTER TABLE users ADD CONSTRAINT check_has_auth_method
CHECK (password_hash IS NOT NULL OR EXISTS (SELECT 1 FROM external_logins WHERE user_id = users.id));
```

| Criteria | Rating | Reasoning |
|----------|--------|-----------|
| **Data Integrity** | ⭐⭐⭐⭐⭐ | Database-level enforcement |
| **Testability** | ⭐⭐ | Requires database integration tests |
| **DDD Alignment** | ⭐⭐ | Business rule in database, not domain |
| **Code Location** | ⭐ | Database layer (too low-level) |
| **Bypass Risk** | ⭐⭐⭐⭐⭐ | Cannot be bypassed |

**Cons:**
- ❌ Business rule hidden in database
- ❌ Poor error messages (generic SQL error)
- ❌ Not all databases support complex CHECK constraints
- ❌ Harder to test

---

## Decision 5: Migration Strategy for Phase 1 Users

### Option A: Automatic Migration via SQL (RECOMMENDED)

```sql
INSERT INTO external_logins (user_id, provider, external_provider_id, provider_email, linked_at)
SELECT id, 0 /* Microsoft */, external_provider_id, email, created_at
FROM users
WHERE identity_provider = 1 AND external_provider_id IS NOT NULL;
```

| Criteria | Rating | Reasoning |
|----------|--------|-----------|
| **Data Safety** | ⭐⭐⭐⭐⭐ | Transactional SQL migration |
| **Downtime** | ⭐⭐⭐⭐⭐ | Milliseconds (single INSERT) |
| **Rollback** | ⭐⭐⭐⭐⭐ | Simple (delete from external_logins) |
| **Complexity** | ⭐⭐⭐⭐⭐ | Single SQL statement |
| **Testing** | ⭐⭐⭐⭐⭐ | Easy to test on staging |

**Pros:**
- ✅ Fast (milliseconds)
- ✅ Safe (transactional)
- ✅ Easy rollback
- ✅ Zero downtime

**Cons:**
- None

---

### Option B: Application-Level Migration (REJECTED)

```csharp
var users = await GetAllEntraUsersAsync();
foreach (var user in users) {
    user.LinkExternalProvider(...);
    await SaveChangesAsync();
}
```

| Criteria | Rating | Reasoning |
|----------|--------|-----------|
| **Data Safety** | ⭐⭐⭐ | Could fail mid-migration |
| **Downtime** | ⭐⭐ | Minutes (process each user) |
| **Rollback** | ⭐⭐ | Complex (need to track progress) |
| **Complexity** | ⭐⭐ | Requires migration script |
| **Testing** | ⭐⭐⭐ | Harder to test |

**Cons:**
- ❌ Slow (loops through users)
- ❌ Risk of partial migration failure
- ❌ Complex rollback

---

## Recommended Architecture Summary

| Decision Point | Recommendation | Key Reason |
|----------------|----------------|------------|
| **Social Provider Model** | Single `IdentityProvider` enum + `FederatedProvider` enum | Entra is the IdP, social platforms are federation sources |
| **Multi-Provider Storage** | `ExternalLogin` value object collection + junction table | DDD pattern, relational integrity, multi-provider support |
| **Account Linking** | Explicit user consent via `LinkExternalProviderCommand` | Security: prevents account hijacking |
| **Unlinking Protection** | Domain-enforced business rule | Prevents user lockouts, DDD-compliant |
| **Migration Strategy** | Automatic SQL migration | Fast, safe, zero downtime |

---

## Decision Validation Checklist

Before finalizing, verify:

- [ ] **Security Review:** Account hijacking mitigations in place?
- [ ] **DDD Review:** Business rules in domain layer?
- [ ] **Performance Review:** Database indexes optimized?
- [ ] **Testing Review:** Unit + integration + E2E tests planned?
- [ ] **Migration Review:** Rollback plan documented?
- [ ] **OWASP Review:** Account linking best practices followed?
- [ ] **Team Consensus:** All stakeholders agree on approach?

---

## References

- [OWASP Account Linking Security](https://cheatsheetseries.owasp.org/cheatsheets/Third_Party_Identity_Management_Cheat_Sheet.html)
- [Domain-Driven Design: Aggregates](https://martinfowler.com/bliki/DDD_Aggregate.html)
- [Azure Entra External ID Federation](https://learn.microsoft.com/en-us/entra/external-id/identity-providers)
- [ADR-003: Full Technical Details](./ADR-003-Social-Login-Multi-Provider-Architecture.md)
