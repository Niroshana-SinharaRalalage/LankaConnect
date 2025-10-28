# Microsoft Entra External ID Integration - Architecture Summary

**Project:** LankaConnect
**Status:** Design Complete - Ready for Implementation
**Date:** 2025-10-28
**Author:** System Architecture Designer

---

## Executive Summary

This document provides a comprehensive architectural overview for integrating Microsoft Entra External ID (formerly Azure AD B2C) with LankaConnect's existing local JWT authentication system. The design follows **Clean Architecture** and **Domain-Driven Design** principles while maintaining **zero-tolerance for compilation errors** through strict TDD methodology.

---

## Documentation Index

### 1. Architecture Decision Record (ADR)
**File:** [`ADR-002-Entra-External-ID-Integration.md`](./ADR-002-Entra-External-ID-Integration.md)

**Purpose:** Primary decision document outlining the chosen architecture strategy, alternatives considered, and consequences.

**Key Decisions:**
- Identity Provider abstraction pattern
- Strategy pattern for authentication services
- Dual authentication mode during migration
- Domain model preservation approach

**Contents:**
- Context and problem statement
- Decision drivers and rationale
- Detailed technical decisions (8 major sections)
- Implementation phases (5 weeks)
- Alternatives considered
- Risk assessment and mitigation

---

### 2. Domain Model Design
**File:** [`Entra-External-ID-Domain-Model-Design.md`](./Entra-External-ID-Domain-Model-Design.md)

**Purpose:** Detailed design for User aggregate refactoring and domain layer changes.

**Key Components:**
- `IdentityProvider` enum (Local, EntraExternal)
- Enhanced User entity with provider tracking
- New factory method: `CreateFromExternalProvider()`
- Business rule enforcement (password, email, account locking)
- Domain events for external providers
- Repository interface extensions

**Business Rules:**
- Local users MUST have PasswordHash
- External users MUST have ExternalProviderId
- External users cannot have local passwords
- External users are pre-verified

**TDD Test Cases:** 25+ domain tests specified

---

### 3. Database Migration Strategy
**File:** [`Entra-External-ID-Database-Migration-Strategy.md`](./Entra-External-ID-Database-Migration-Strategy.md)

**Purpose:** Complete database schema changes, migration scripts, and validation procedures.

**Key Changes:**
- Add `IdentityProvider` column (INT, default 0)
- Add `ExternalProviderId` column (VARCHAR(255), nullable)
- Make `PasswordHash` nullable
- Add check constraints for data integrity
- Create indexes for performance

**Safety Features:**
- Idempotent migration scripts
- Rollback procedures
- Data validation functions
- Performance impact analysis
- Zero-downtime deployment strategy

**Estimated Migration Time:** 2-5 seconds

---

### 4. Component Architecture
**File:** [`Entra-External-ID-Component-Architecture.md`](./Entra-External-ID-Component-Architecture.md)

**Purpose:** Detailed component diagrams, interaction flows, and layer responsibilities.

**Key Components:**
- **Presentation Layer:** `/api/auth/login/entra` endpoint
- **Application Layer:** `LoginWithEntraCommand` handler
- **Domain Layer:** User aggregate with provider abstraction
- **Infrastructure Layer:** `EntraExternalIdService` for token validation

**Authentication Flows:**
1. Local authentication (existing - unchanged)
2. Entra External ID authentication (new)
3. Dual authentication mode (migration period)

**Security Considerations:**
- JWT signature verification with JWKS
- Token validation flow (6 steps)
- Caching strategy for performance
- Threat mitigation strategies

---

### 5. Implementation Roadmap
**File:** [`Entra-External-ID-Implementation-Roadmap.md`](./Entra-External-ID-Implementation-Roadmap.md)

**Purpose:** Step-by-step TDD implementation plan with zero-tolerance for errors.

**Implementation Phases:**
1. **Phase 1 (Week 1):** Domain layer foundation - 25+ tests
2. **Phase 2 (Week 2):** Infrastructure layer - Database + Entra service
3. **Phase 3 (Week 3):** Application layer - Command handlers
4. **Phase 4 (Week 4):** Presentation layer - API endpoints
5. **Phase 5 (Week 5):** Integration, testing, deployment

**TDD Methodology:**
- Red-Green-Refactor cycles
- Commit after each passing test suite
- 90%+ test coverage maintained
- All existing tests pass at every checkpoint

**Estimated Total Time:** 50-70 hours

---

## Architecture Overview

### Layer Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                      PRESENTATION LAYER                          │
│  AuthController                                                  │
│  ├── POST /auth/login              (Local JWT)                  │
│  ├── POST /auth/login/entra        (NEW: Entra External ID)     │
│  └── POST /auth/refresh            (Both providers)             │
└─────────────────────────────────────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                     APPLICATION LAYER                            │
│  Commands                                                        │
│  ├── LoginUserCommand → LoginUserHandler         (Existing)    │
│  └── LoginWithEntraCommand → LoginWithEntraHandler  (NEW)       │
│                                                                  │
│  Interfaces                                                      │
│  ├── IJwtTokenService                             (Existing)    │
│  ├── IPasswordHashingService                      (Existing)    │
│  └── IExternalAuthenticationService               (NEW)         │
└─────────────────────────────────────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                       DOMAIN LAYER                               │
│  User Aggregate                                                  │
│  ├── IdentityProvider (Local | EntraExternal)                   │
│  ├── ExternalProviderId (string?)                               │
│  ├── PasswordHash (string? - nullable for external)             │
│  └── Business Rules (password, email, locking)                  │
│                                                                  │
│  Domain Events                                                   │
│  ├── UserCreatedEvent                             (Existing)    │
│  ├── UserCreatedFromExternalProviderEvent         (NEW)         │
│  └── UserProviderLinkedEvent                      (NEW)         │
└─────────────────────────────────────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                    INFRASTRUCTURE LAYER                          │
│  Services                                                        │
│  ├── JwtTokenService                              (Existing)    │
│  ├── PasswordHashingService                       (Existing)    │
│  └── EntraExternalIdService                       (NEW)         │
│                                                                  │
│  Data Access                                                     │
│  ├── ApplicationDbContext (EF Core)                             │
│  ├── UserRepository                                             │
│  └── PostgreSQL Database                                        │
│                                                                  │
│  External Dependencies                                           │
│  └── Microsoft Entra External ID (OAuth2/OIDC)                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Key Architectural Decisions

### 1. Identity Provider Abstraction

**Problem:** Support multiple authentication providers without breaking existing functionality.

**Solution:** Introduce `IdentityProvider` enum in the domain layer, making the User aggregate provider-agnostic.

**Benefits:**
- Single User entity (no separate tables)
- Clear business rules per provider
- Future extensibility (Google, Facebook, etc.)
- Domain remains pure (no infrastructure concerns)

---

### 2. Dual Authentication Mode

**Problem:** Existing users cannot switch to Entra immediately.

**Solution:** Support both authentication methods simultaneously during migration period.

**Implementation:**
```csharp
// Email uniqueness constraint changed from:
UNIQUE (Email)

// To:
UNIQUE (Email, IdentityProvider)
```

**User Identification:**
- Check `IdentityProvider` to determine authentication flow
- Optional `/api/auth/check-provider` endpoint for frontend
- Clear error messages for mismatched providers

---

### 3. Auto-Provisioning Strategy

**Problem:** How to create LankaConnect users from Entra authentication?

**Solution:** Auto-provision users on first login via `CreateFromExternalProvider()`.

**Flow:**
1. User authenticates with Entra
2. Entra returns access token
3. LankaConnect validates token
4. Check if user exists by `ExternalProviderId`
5. If not found, create new user with Entra data
6. Issue LankaConnect JWT for API access

**Business Rules:**
- Email pre-verified (Entra handles verification)
- No password stored locally
- User profile synced from Entra claims

---

### 4. Token Strategy

**Problem:** Should we use Entra tokens directly for API calls?

**Decision:** No - validate Entra token once, issue LankaConnect JWT for subsequent requests.

**Rationale:**
- Shorter Entra token lifespan (1 hour)
- LankaConnect has custom claims (business roles, preferences)
- Reduces external API dependency
- Consistent authorization logic

**Flow:**
```
Entra Token (1 hour) → Validate once → LankaConnect JWT (15 min) + Refresh Token (7 days)
```

---

### 5. Password Management Approach

**Problem:** External users don't have passwords in LankaConnect.

**Solution:** Make `PasswordHash` nullable, enforce at domain level.

**Domain Rules:**
```csharp
public Result SetPassword(string passwordHash)
{
    if (IdentityProvider != IdentityProvider.Local)
        return Result.Failure("Cannot set password for external users");
    // ...
}
```

**Benefits:**
- Compile-time safety (nullable reference types)
- Runtime validation in domain layer
- Clear error messages
- No breaking changes (existing local users unaffected)

---

## Database Schema Changes

### Before Migration

```sql
CREATE TABLE "Users" (
    "Id" UUID PRIMARY KEY,
    "Email" VARCHAR(255) NOT NULL UNIQUE,
    "PasswordHash" VARCHAR(255) NOT NULL,  -- Always required
    -- ... other columns
);
```

### After Migration

```sql
CREATE TABLE "Users" (
    "Id" UUID PRIMARY KEY,
    "Email" VARCHAR(255) NOT NULL,
    "IdentityProvider" INT NOT NULL DEFAULT 0,        -- NEW
    "ExternalProviderId" VARCHAR(255) NULL,           -- NEW
    "PasswordHash" VARCHAR(255) NULL,                 -- Now nullable
    -- ... other columns

    -- Constraints
    CONSTRAINT "CK_Users_PasswordHash_Required_For_Local"
        CHECK (("IdentityProvider" = 0 AND "PasswordHash" IS NOT NULL)
            OR ("IdentityProvider" != 0 AND "PasswordHash" IS NULL)),

    CONSTRAINT "CK_Users_ExternalProviderId_Required_For_External"
        CHECK (("IdentityProvider" = 0 AND "ExternalProviderId" IS NULL)
            OR ("IdentityProvider" != 0 AND "ExternalProviderId" IS NOT NULL))
);

-- New Indexes
CREATE UNIQUE INDEX "IX_Users_Email_IdentityProvider_Unique"
    ON "Users" ("Email", "IdentityProvider");

CREATE UNIQUE INDEX "IX_Users_ExternalProviderId_Unique"
    ON "Users" ("ExternalProviderId")
    WHERE "ExternalProviderId" IS NOT NULL;
```

---

## Testing Strategy

### Test Pyramid

```
           /\
          /E2E\          5% - Full Entra integration (WireMock)
         /------\
        /        \
       /Integration\     25% - Mock Entra service, real DB
      /------------\
     /              \
    /   Unit Tests   \   70% - Domain logic, pure functions
   /------------------\
```

### Test Coverage Targets

| Layer | Coverage Target | Test Count Estimate |
|-------|----------------|---------------------|
| Domain | 95%+ | 25+ tests |
| Application | 95%+ | 30+ tests |
| Infrastructure | 90%+ | 15+ tests |
| Presentation | 90%+ | 10+ tests |
| E2E | 100% scenarios | 5+ tests |

### TDD Workflow

```
1. Write failing test
   ├─ RED: Test fails (expected)
   │
2. Implement minimal code
   ├─ GREEN: Test passes
   │
3. Refactor code
   ├─ REFACTOR: Improve without breaking tests
   │
4. Verify all tests pass
   ├─ Run full test suite
   │
5. Commit with clear message
   └─ "feat(domain): [description] - X tests passing"
```

---

## Security Considerations

### Token Validation

**Entra Token Validation Steps:**
1. Parse JWT header (extract `kid`, `alg`)
2. Fetch JWKS from Entra (cached 24h)
3. Verify signature with RSA public key
4. Validate claims: `iss`, `aud`, `exp`, `nbf`
5. Extract user claims: `oid`, `email`, `name`

**LankaConnect JWT:**
- Short-lived access tokens (15 min)
- Long-lived refresh tokens (7 days)
- Stored in HttpOnly cookies (XSS protection)
- Token blacklist for logout

### Data Protection

| Data | Storage Location | Encryption |
|------|-----------------|------------|
| Credentials | Entra External ID | Managed by Microsoft |
| User profiles | PostgreSQL | TDE (database level) |
| Tokens (access) | Client memory | N/A (short-lived) |
| Tokens (refresh) | PostgreSQL + Redis | Database encryption |

### Threat Mitigation

| Threat | Mitigation |
|--------|-----------|
| Token replay | Short token lifetime, jti claim validation |
| Account enumeration | Generic error messages, rate limiting |
| SQL injection | Parameterized queries (EF Core) |
| XSS attacks | HttpOnly cookies, CSP headers |
| CSRF attacks | SameSite cookies, anti-forgery tokens |

---

## Performance Considerations

### Expected Latencies

| Operation | Target Latency (p95) | Notes |
|-----------|---------------------|-------|
| Token validation | < 200ms | Cached JWKS |
| User auto-provision | < 1s | Database insert |
| JWT generation | < 50ms | In-memory signing |
| Complete login flow | < 500ms | End-to-end |

### Caching Strategy

| Resource | Cache Duration | Invalidation |
|----------|---------------|-------------|
| OIDC config | 24 hours | Manual or on error |
| JWKS | 24 hours | On key rotation |
| User profiles | N/A | Database only |

### Database Indexes

```sql
-- Existing (unchanged)
CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");

-- New indexes for provider queries
CREATE INDEX "IX_Users_IdentityProvider_ExternalProviderId"
    ON "Users" ("IdentityProvider", "ExternalProviderId")
    WHERE "ExternalProviderId" IS NOT NULL;

CREATE UNIQUE INDEX "IX_Users_ExternalProviderId_Unique"
    ON "Users" ("ExternalProviderId")
    WHERE "ExternalProviderId" IS NOT NULL;

-- Composite index for dual auth mode
CREATE UNIQUE INDEX "IX_Users_Email_IdentityProvider_Unique"
    ON "Users" ("Email", "IdentityProvider");
```

**Estimated Storage Impact:** +5-10% for indexes

---

## Migration Approach

### Pre-Migration

1. ✅ Complete all architecture documents
2. ✅ Review ADR with team
3. ✅ Set up test Entra tenant
4. ✅ Create implementation roadmap

### Implementation (5 Weeks)

**Week 1: Domain Layer**
- Add IdentityProvider enum
- Refactor User entity
- Add business rules
- Write 25+ domain tests

**Week 2: Infrastructure Layer**
- Apply database migration
- Implement repository methods
- Create EntraExternalIdService
- Write 15+ integration tests

**Week 3: Application Layer**
- Create LoginWithEntraCommand
- Implement auto-provisioning
- Update existing commands
- Write 30+ application tests

**Week 4: Presentation Layer**
- Add `/api/auth/login/entra` endpoint
- Update API documentation
- Write 10+ E2E tests

**Week 5: Deployment**
- Deploy to staging
- Run E2E tests
- Deploy to production
- Monitor metrics

### Post-Migration

1. Monitor error rates
2. Track provider adoption
3. Gather user feedback
4. Optimize performance
5. Plan deprecation of local registration (optional)

---

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=lankaconnect;...",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Key": "your-secret-key",
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

---

## Success Criteria

### Technical Metrics

- ✅ Zero compilation errors at every checkpoint
- ✅ 90%+ test coverage across all layers
- ✅ All existing tests pass after each change
- ✅ Performance targets met (< 500ms login)
- ✅ Security review passed

### Business Metrics

- ✅ Zero downtime during migration
- ✅ Zero existing user authentication failures
- ✅ Successful Entra login for test users
- ✅ < 5 support tickets in first week
- ✅ Positive user feedback

### Operational Metrics

- ✅ Monitoring dashboards configured
- ✅ Alerting rules set up
- ✅ Incident response plan documented
- ✅ Rollback procedures tested

---

## Risk Assessment

### High-Impact Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| Database migration failure | Low | High | Test in staging, have rollback script |
| Breaking existing auth | Low | Critical | Maintain backward compatibility, comprehensive tests |
| Entra service outage | Medium | High | Fallback to local auth, clear error messages |
| Performance degradation | Low | Medium | Caching, indexes, load testing |

### Mitigation Strategies

1. **Backward Compatibility:** All existing users continue using local JWT
2. **Gradual Rollout:** Feature flag for Entra login
3. **Monitoring:** Track error rates, latencies, adoption
4. **Rollback Plan:** Documented procedures for each phase

---

## Next Steps

### Immediate Actions (Week 1)

1. **Team Review:** Present ADR and architecture documents
2. **Setup:** Configure test Entra tenant
3. **Branch:** Create `feature/entra-external-id` branch
4. **Begin Implementation:** Start Phase 1, Step 1.1

### Implementation Sequence

```
Phase 1: Domain Layer (Week 1)
  ├─ Step 1.1: Add IdentityProvider enum
  ├─ Step 1.2: Add properties to User entity
  ├─ Step 1.3: Add business rule enforcement
  └─ Step 1.4-1.5: Email verification and account locking rules

Phase 2: Infrastructure Layer (Week 2)
  ├─ Step 2.1: Database migration
  ├─ Step 2.2: Repository implementation
  └─ Step 2.3: Entra External ID service

Phase 3: Application Layer (Week 3)
  ├─ Step 3.1: LoginWithEntraCommand
  └─ Step 3.2: Update existing commands

Phase 4: Presentation Layer (Week 4)
  ├─ Step 4.1: Add Entra login endpoint
  └─ Step 4.2: Update existing endpoints

Phase 5: Integration & Deployment (Week 5)
  ├─ Step 5.1: E2E testing
  ├─ Step 5.2: Production deployment
  └─ Step 5.3: Documentation and monitoring
```

---

## Support and Resources

### Documentation

- [ADR-002: Entra External ID Integration](./ADR-002-Entra-External-ID-Integration.md)
- [Domain Model Design](./Entra-External-ID-Domain-Model-Design.md)
- [Database Migration Strategy](./Entra-External-ID-Database-Migration-Strategy.md)
- [Component Architecture](./Entra-External-ID-Component-Architecture.md)
- [Implementation Roadmap](./Entra-External-ID-Implementation-Roadmap.md)

### External Resources

- [Microsoft Entra External ID Documentation](https://learn.microsoft.com/en-us/entra/external-id/)
- [OAuth 2.0 Specification](https://oauth.net/2/)
- [OpenID Connect Core](https://openid.net/specs/openid-connect-core-1_0.html)
- [Clean Architecture Guide](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

### Team Contacts

- **Architecture Questions:** System Architecture Designer
- **Domain Logic:** Lead Developer
- **Infrastructure:** DevOps Team
- **Security:** Security Team

---

## Conclusion

This architecture provides a **robust, testable, and maintainable** solution for integrating Microsoft Entra External ID with LankaConnect while:

1. ✅ Preserving Clean Architecture principles
2. ✅ Maintaining Domain-Driven Design patterns
3. ✅ Following strict TDD methodology
4. ✅ Ensuring backward compatibility
5. ✅ Supporting gradual migration
6. ✅ Meeting enterprise security standards

The implementation roadmap provides clear, incremental steps with zero-tolerance for compilation errors, ensuring a smooth and safe integration process.

---

**Document Status:** ✅ Complete and Ready for Review
**Last Updated:** 2025-10-28
**Review Required:** Architecture Review Board, Security Team, Lead Developer
