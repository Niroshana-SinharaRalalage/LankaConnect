# Epic 1 Phase 2: Social Login Architecture Diagrams

**Date:** 2025-11-01
**Architect:** System Architecture Designer

---

## 1. System Context Diagram (C4 Level 1)

```
┌─────────────────────────────────────────────────────────────────────┐
│                           LankaConnect User                          │
│                    (External Customer/Business)                      │
└───────────────────────────┬─────────────────────────────────────────┘
                            │
                            │ Authenticates via
                            │ social providers
                            ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    LankaConnect Application                          │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                  Authentication System                       │   │
│  │  - Local JWT (Email/Password)                               │   │
│  │  - Microsoft Entra External ID Federation                   │   │
│  │  - Social Logins: Facebook, Google, Apple                   │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                     Domain Model                             │   │
│  │  - User Aggregate                                           │   │
│  │  - ExternalLogin Value Objects                              │   │
│  └─────────────────────────────────────────────────────────────┘   │
└──────────────────┬────────────────────────────┬────────────────────┘
                   │                            │
                   │                            │
                   ↓                            ↓
┌──────────────────────────────┐  ┌──────────────────────────────────┐
│  Azure Entra External ID     │  │     PostgreSQL Database          │
│  (Identity Federation)       │  │  - User profiles                 │
│  - Microsoft Login           │  │  - External logins junction      │
│  - Facebook Federation       │  │  - Refresh tokens                │
│  - Google Federation         │  │                                  │
│  - Apple Federation          │  │                                  │
└──────────────────────────────┘  └──────────────────────────────────┘
```

---

## 2. Container Diagram (C4 Level 2)

```
┌─────────────────────────────────────────────────────────────────────┐
│                      Frontend Application                            │
│                   (React / Angular / Vue)                            │
└───────┬──────────────────────────────────────────┬──────────────────┘
        │                                          │
        │ POST /api/auth/login/entra              │ POST /api/auth/link-provider
        │ (Entra access token)                    │ DELETE /api/auth/unlink-provider
        ↓                                          ↓
┌─────────────────────────────────────────────────────────────────────┐
│                     LankaConnect API (ASP.NET Core)                  │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                   API Layer (Controllers)                    │   │
│  │  - AuthController                                           │   │
│  │    * LoginWithEntra()                                       │   │
│  │    * LinkExternalProvider()                                 │   │
│  │    * UnlinkExternalProvider()                               │   │
│  │    * GetLinkedProviders()                                   │   │
│  └───────────────────────┬─────────────────────────────────────┘   │
│                          │ MediatR                                  │
│                          ↓                                          │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │            Application Layer (CQRS Handlers)                 │   │
│  │  Commands:                                                   │   │
│  │    - LoginWithEntraCommandHandler                           │   │
│  │    - LinkExternalProviderCommandHandler                     │   │
│  │    - UnlinkExternalProviderCommandHandler                   │   │
│  │  Queries:                                                    │   │
│  │    - GetLinkedProvidersQueryHandler                         │   │
│  └───────────────────────┬─────────────────────────────────────┘   │
│                          │                                          │
│                          ↓                                          │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                    Domain Layer                              │   │
│  │  - User Aggregate                                           │   │
│  │    * LinkExternalProvider()                                 │   │
│  │    * UnlinkExternalProvider()                               │   │
│  │  - ExternalLogin Value Object                               │   │
│  │  - FederatedProvider Enum                                   │   │
│  │  - Domain Events                                            │   │
│  └───────────────────────┬─────────────────────────────────────┘   │
│                          │                                          │
│                          ↓                                          │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │              Infrastructure Layer                            │   │
│  │  - UserRepository (PostgreSQL)                              │   │
│  │  - EntraExternalIdService (Token validation)                │   │
│  │  - JwtTokenService (LankaConnect tokens)                    │   │
│  └────────┬────────────────────────────────────┬────────────────┘   │
└───────────┼────────────────────────────────────┼────────────────────┘
            │                                    │
            ↓                                    ↓
┌──────────────────────────┐      ┌─────────────────────────────────┐
│  Azure Entra External ID │      │   PostgreSQL Database           │
│  - OIDC token validation │      │   Schema: users                 │
│  - User info endpoint    │      │   - users table                 │
│  - idp claim parsing     │      │   - external_logins table       │
└──────────────────────────┘      └─────────────────────────────────┘
```

---

## 3. Authentication Flow Sequence Diagram

### 3.1 Social Login (New User Auto-Provisioning)

```
User          Frontend       Entra External ID    LankaConnect API         Domain         Database
 │                │                 │                     │                  │               │
 │  Click "Login │                 │                     │                  │               │
 │  with Facebook"                 │                     │                  │               │
 ├───────────────>│                 │                     │                  │               │
 │                │ Redirect to     │                     │                  │               │
 │                │ Entra + Facebook│                     │                  │               │
 │                ├────────────────>│                     │                  │               │
 │                │                 │ Redirect to         │                  │               │
 │                │                 │ Facebook OAuth      │                  │               │
 │<───────────────┼─────────────────┤                     │                  │               │
 │ Facebook Login │                 │                     │                  │               │
 │ Dialog         │                 │                     │                  │               │
 ├────────────────┼─────────────────>│                     │                  │               │
 │                │                 │ Issue Entra token   │                  │               │
 │                │                 │ with idp:"facebook" │                  │               │
 │<───────────────┼─────────────────┤                     │                  │               │
 │                │ Entra token     │                     │                  │               │
 │                │ (access_token)  │                     │                  │               │
 │                │<────────────────┤                     │                  │               │
 │                │                 │                     │                  │               │
 │                │ POST /api/auth/login/entra           │                  │               │
 │                │ { accessToken: "eyJ..." }            │                  │               │
 │                ├─────────────────┼─────────────────────>│                  │               │
 │                │                 │                     │ Validate token   │               │
 │                │                 │                     │ (OIDC signature) │               │
 │                │                 │<────────────────────┤                  │               │
 │                │                 │ User info +         │                  │               │
 │                │                 │ idp claim           │                  │               │
 │                │                 │─────────────────────>│                  │               │
 │                │                 │                     │ Parse idp claim  │               │
 │                │                 │                     │ → Facebook       │               │
 │                │                 │                     │                  │               │
 │                │                 │                     │ Find user by     │               │
 │                │                 │                     │ external login   │               │
 │                │                 │                     ├─────────────────────────────────>│
 │                │                 │                     │                  │ SELECT * FROM│
 │                │                 │                     │                  │ external_logins
 │                │                 │                     │                  │ WHERE provider=1
 │                │                 │                     │                  │ AND external_id
 │                │                 │                     │<─────────────────────────────────┤
 │                │                 │                     │ User not found   │               │
 │                │                 │                     │                  │               │
 │                │                 │                     │ CreateFromExternal              │
 │                │                 │                     │ Provider()       │               │
 │                │                 │                     ├─────────────────>│               │
 │                │                 │                     │                  │ Validate      │
 │                │                 │                     │                  │ business rules│
 │                │                 │                     │                  │               │
 │                │                 │                     │                  │ Create User   │
 │                │                 │                     │                  │ + ExternalLogin
 │                │                 │                     │<─────────────────┤               │
 │                │                 │                     │ User aggregate   │               │
 │                │                 │                     │                  │               │
 │                │                 │                     │ Save to database │               │
 │                │                 │                     ├─────────────────────────────────>│
 │                │                 │                     │                  │ INSERT users  │
 │                │                 │                     │                  │ INSERT        │
 │                │                 │                     │                  │ external_logins
 │                │                 │                     │<─────────────────────────────────┤
 │                │                 │                     │                  │               │
 │                │                 │                     │ Generate JWT     │               │
 │                │                 │                     │ tokens           │               │
 │                │                 │                     │                  │               │
 │                │<────────────────┼─────────────────────┤                  │               │
 │                │ 200 OK          │                     │                  │               │
 │                │ { accessToken,  │                     │                  │               │
 │                │   refreshToken, │                     │                  │               │
 │                │   provider:"Facebook" }              │                  │               │
 │<───────────────┤                 │                     │                  │               │
 │ Logged in      │                 │                     │                  │               │
```

---

### 3.2 Account Linking Flow (Existing User)

```
Authenticated     Frontend       LankaConnect API      Domain         Database
User (JWT)
 │                   │                    │               │              │
 │ Click "Link      │                    │               │              │
 │ Google Account"  │                    │               │              │
 ├─────────────────>│                    │               │              │
 │                  │ Redirect to Entra  │               │              │
 │                  │ for Google auth    │               │              │
 │<─────────────────┤                    │               │              │
 │ Google OAuth     │                    │               │              │
 │ (via Entra)      │                    │               │              │
 ├─────────────────>│                    │               │              │
 │                  │ Receive Entra token│               │              │
 │                  │ with idp:"google"  │               │              │
 │<─────────────────┤                    │               │              │
 │                  │                    │               │              │
 │                  │ POST /api/auth/link-provider      │              │
 │                  │ Authorization: Bearer <JWT>       │              │
 │                  │ { entraAccessToken: "eyJ..." }    │              │
 │                  ├───────────────────>│               │              │
 │                  │                    │ Verify JWT    │              │
 │                  │                    │ (current user)│              │
 │                  │                    │               │              │
 │                  │                    │ Validate Entra│              │
 │                  │                    │ token         │              │
 │                  │                    │               │              │
 │                  │                    │ Parse idp →   │              │
 │                  │                    │ Google        │              │
 │                  │                    │               │              │
 │                  │                    │ Check if Google             │
 │                  │                    │ already linked│              │
 │                  │                    │ to ANOTHER user             │
 │                  │                    ├──────────────────────────────>
 │                  │                    │               │ SELECT user_id
 │                  │                    │               │ FROM external_logins
 │                  │                    │               │ WHERE provider=2
 │                  │                    │<──────────────────────────────┤
 │                  │                    │ Not found     │              │
 │                  │                    │               │              │
 │                  │                    │ Load current  │              │
 │                  │                    │ user          │              │
 │                  │                    ├──────────────────────────────>
 │                  │                    │<──────────────────────────────┤
 │                  │                    │ User aggregate│              │
 │                  │                    │               │              │
 │                  │                    │ user.Link     │              │
 │                  │                    │ ExternalProvider()           │
 │                  │                    ├──────────────>│              │
 │                  │                    │               │ Validate:    │
 │                  │                    │               │ Not duplicate│
 │                  │                    │               │              │
 │                  │                    │               │ Add to       │
 │                  │                    │               │ _externalLogins
 │                  │                    │               │              │
 │                  │                    │               │ Raise domain │
 │                  │                    │               │ event        │
 │                  │                    │<──────────────┤              │
 │                  │                    │               │              │
 │                  │                    │ Save changes  │              │
 │                  │                    ├──────────────────────────────>
 │                  │                    │               │ INSERT       │
 │                  │                    │               │ external_logins
 │                  │                    │<──────────────────────────────┤
 │                  │                    │               │              │
 │                  │<───────────────────┤               │              │
 │                  │ 200 OK             │               │              │
 │                  │ { provider: "Google", linked: true }            │
 │<─────────────────┤                    │               │              │
 │ Success message  │                    │               │              │
```

---

## 4. Domain Model Class Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         User (Aggregate Root)                    │
├─────────────────────────────────────────────────────────────────┤
│ - Id: Guid                                                       │
│ - Email: Email (Value Object)                                   │
│ - FirstName: string                                             │
│ - LastName: string                                              │
│ - IdentityProvider: IdentityProvider (enum)                     │
│ - PasswordHash: string? (null for external users)               │
│ - _externalLogins: List<ExternalLogin>                          │
│ - IsEmailVerified: bool                                         │
│ - Role: UserRole                                                │
│ - CreatedAt: DateTime                                           │
│ - UpdatedAt: DateTime?                                          │
├─────────────────────────────────────────────────────────────────┤
│ + Create(email, firstName, lastName): Result<User>              │
│ + CreateFromExternalProvider(...): Result<User>                 │
│ + LinkExternalProvider(provider, id, email): Result             │
│ + UnlinkExternalProvider(provider, id): Result                  │
│ + HasLinkedProvider(provider): bool                             │
│ + GetExternalLogin(provider, id): ExternalLogin?                │
│ + SetPassword(hash): Result                                     │
│ + ChangePassword(hash): Result                                  │
│ + RecordSuccessfulLogin(): Result                               │
│ + CanAuthenticate: bool { get; }                                │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         │ Contains 0..*
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│              ExternalLogin (Value Object)                        │
├─────────────────────────────────────────────────────────────────┤
│ - Provider: FederatedProvider                                   │
│ - ExternalProviderId: string (Entra OID)                        │
│ - ProviderEmail: string                                         │
│ - LinkedAt: DateTime                                            │
├─────────────────────────────────────────────────────────────────┤
│ + Create(provider, id, email): Result<ExternalLogin>            │
│ + Equals(other): bool (by Provider + ExternalProviderId)        │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────┐    ┌──────────────────────────────┐
│ IdentityProvider (enum)     │    │ FederatedProvider (enum)     │
├─────────────────────────────┤    ├──────────────────────────────┤
│ Local = 0                   │    │ Microsoft = 0                │
│ EntraExternal = 1           │    │ Facebook = 1                 │
└─────────────────────────────┘    │ Google = 2                   │
                                   │ Apple = 3                    │
                                   └──────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    Domain Events                                 │
├─────────────────────────────────────────────────────────────────┤
│ UserCreatedFromExternalProviderEvent                            │
│   - UserId, Email, FullName, IdentityProvider,                  │
│     FederatedProvider, ExternalProviderId                       │
│                                                                  │
│ ExternalProviderLinkedEvent                                     │
│   - UserId, Email, Provider, ExternalProviderId, ProviderEmail  │
│                                                                  │
│ ExternalProviderUnlinkedEvent                                   │
│   - UserId, Email, Provider, ExternalProviderId                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 5. Database Schema (Entity Relationship Diagram)

```
┌─────────────────────────────────────────────────────────────────┐
│                           users.users                            │
├─────────────────────────────────────────────────────────────────┤
│ PK  id                        UUID                               │
│     email                     VARCHAR(255)  UNIQUE               │
│     first_name                VARCHAR(100)                       │
│     last_name                 VARCHAR(100)                       │
│     identity_provider         INT (0=Local, 1=EntraExternal)    │
│     password_hash             VARCHAR(255)  NULL                 │
│     is_email_verified         BOOLEAN                            │
│     role                      INT                                │
│     created_at                TIMESTAMP                          │
│     updated_at                TIMESTAMP NULL                     │
│     (DEPRECATED)              external_provider_id VARCHAR(255)  │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     │ 1
                     │
                     │ has
                     │
                     │ 0..*
                     ↓
┌─────────────────────────────────────────────────────────────────┐
│                   users.external_logins                          │
├─────────────────────────────────────────────────────────────────┤
│ PK  id                        SERIAL                             │
│ FK  user_id                   UUID → users.id (CASCADE DELETE)   │
│     provider                  INT (0=MS, 1=FB, 2=Google, 3=Apple)│
│     external_provider_id      VARCHAR(255) (Entra OID)           │
│     provider_email            VARCHAR(255)                       │
│     linked_at                 TIMESTAMP                          │
│     created_at                TIMESTAMP                          │
├─────────────────────────────────────────────────────────────────┤
│ UK  (provider, external_provider_id)  -- Prevent duplicate links │
│ IX  (user_id)                         -- User lookup             │
│ IX  (provider, external_provider_id)  -- Authentication lookup   │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│              Example Data: Multi-Provider User                   │
├─────────────────────────────────────────────────────────────────┤
│ users:                                                           │
│   id: 123e4567-e89b-12d3-a456-426614174000                      │
│   email: john@example.com                                       │
│   identity_provider: 1 (EntraExternal)                          │
│   password_hash: NULL                                           │
│                                                                  │
│ external_logins (3 linked providers):                           │
│   1. provider: 0 (Microsoft), external_id: "entra-ms-123"       │
│   2. provider: 1 (Facebook),  external_id: "entra-fb-456"       │
│   3. provider: 2 (Google),    external_id: "entra-google-789"   │
└─────────────────────────────────────────────────────────────────┘
```

**Indexes:**
```sql
-- User lookup by social login (authentication)
CREATE INDEX ix_external_logins_provider_external_id
ON users.external_logins (provider, external_provider_id);

-- User's linked providers (profile page)
CREATE INDEX ix_external_logins_user_id
ON users.external_logins (user_id);

-- Prevent duplicate linking
CREATE UNIQUE INDEX uk_external_logins_provider_external_id
ON users.external_logins (provider, external_provider_id);
```

---

## 6. Component Interaction Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                         API Layer                                     │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │                     AuthController                              │  │
│  │  - LoginWithEntra()     → LoginWithEntraCommand                │  │
│  │  - LinkExternalProvider() → LinkExternalProviderCommand        │  │
│  │  - UnlinkExternalProvider() → UnlinkExternalProviderCommand    │  │
│  │  - GetLinkedProviders() → GetLinkedProvidersQuery              │  │
│  └────────────────────────┬───────────────────────────────────────┘  │
└───────────────────────────┼──────────────────────────────────────────┘
                            │ MediatR.Send()
                            ↓
┌──────────────────────────────────────────────────────────────────────┐
│                      Application Layer (CQRS)                         │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │         LoginWithEntraCommandHandler                           │  │
│  │  1. Validate Entra token → IEntraExternalIdService            │  │
│  │  2. Parse idp claim → FederatedProvider                        │  │
│  │  3. Find user → IUserRepository.GetByExternalLoginAsync()     │  │
│  │  4. Auto-provision if new → User.CreateFromExternalProvider() │  │
│  │  5. Generate JWT → IJwtTokenService                            │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                       │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │       LinkExternalProviderCommandHandler                       │  │
│  │  1. Verify current user (security check)                       │  │
│  │  2. Validate Entra token → IEntraExternalIdService            │  │
│  │  3. Check hijacking → GetByExternalLoginAsync()                │  │
│  │  4. Link provider → user.LinkExternalProvider()                │  │
│  │  5. Save → IUnitOfWork.CommitAsync()                           │  │
│  └────────────────────────────────────────────────────────────────┘  │
└───────────────────────────┬──────────────────────────────────────────┘
                            │
                            ↓
┌──────────────────────────────────────────────────────────────────────┐
│                        Domain Layer                                   │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │                      User Aggregate                            │  │
│  │                                                                │  │
│  │  Business Rules:                                               │  │
│  │  ✓ External users cannot have PasswordHash                    │  │
│  │  ✓ Cannot link same provider twice                            │  │
│  │  ✓ Cannot unlink last authentication method                   │  │
│  │  ✓ Email must be valid Email value object                     │  │
│  │                                                                │  │
│  │  Domain Events:                                                │  │
│  │  - UserCreatedFromExternalProviderEvent                        │  │
│  │  - ExternalProviderLinkedEvent                                 │  │
│  │  - ExternalProviderUnlinkedEvent                               │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                       │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │                 ExternalLogin Value Object                     │  │
│  │  - Provider: FederatedProvider (enum)                          │  │
│  │  - ExternalProviderId: string (Entra OID)                      │  │
│  │  - ProviderEmail: string                                       │  │
│  │  - LinkedAt: DateTime                                          │  │
│  │                                                                │  │
│  │  Equality: By Provider + ExternalProviderId                    │  │
│  └────────────────────────────────────────────────────────────────┘  │
└───────────────────────────┬──────────────────────────────────────────┘
                            │
                            ↓
┌──────────────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                               │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │                    UserRepository                              │  │
│  │  - GetByExternalLoginAsync(provider, id)                       │  │
│  │  - ExistsWithEmailAsync(email)                                 │  │
│  │  - AddAsync(user)                                              │  │
│  │                                                                │  │
│  │  EF Core Query:                                                │  │
│  │    SELECT * FROM users                                         │  │
│  │    WHERE EXISTS (                                              │  │
│  │      SELECT 1 FROM external_logins                             │  │
│  │      WHERE user_id = users.id                                  │  │
│  │        AND provider = @provider                                │  │
│  │        AND external_provider_id = @externalId                  │  │
│  │    )                                                           │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                       │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │              EntraExternalIdService                            │  │
│  │  - ValidateAccessTokenAsync(token)                             │  │
│  │  - GetUserInfoAsync(token)                                     │  │
│  │                                                                │  │
│  │  Returns:                                                      │  │
│  │    EntraUserInfo {                                             │  │
│  │      ObjectId: "entra-oid-12345",                              │  │
│  │      Email: "user@facebook.com",                               │  │
│  │      FirstName: "John",                                        │  │
│  │      LastName: "Doe",                                          │  │
│  │      IdentityProvider: "facebook.com"  ← idp claim             │  │
│  │    }                                                           │  │
│  └────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
```

---

## 7. Security Threat Model

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Threat: Account Hijacking                         │
├─────────────────────────────────────────────────────────────────────┤
│ Attack Vector:                                                       │
│   1. Attacker creates Facebook account with victim@example.com      │
│   2. Attacker tries to link to victim's LankaConnect account         │
│   3. Attacker gains access to victim's data                          │
├─────────────────────────────────────────────────────────────────────┤
│ Mitigation:                                                          │
│   ✓ Check if ExternalProviderId already linked to ANOTHER user      │
│   ✓ Require authenticated session before linking                    │
│   ✓ Verify currentUserId matches command.UserId                     │
│   ✓ Log all link/unlink operations for audit                        │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                   Threat: Account Lockout                            │
├─────────────────────────────────────────────────────────────────────┤
│ Attack Vector:                                                       │
│   1. User has only Facebook login (no password)                      │
│   2. User accidentally unlinks Facebook                              │
│   3. User is locked out of account                                   │
├─────────────────────────────────────────────────────────────────────┤
│ Mitigation:                                                          │
│   ✓ Domain validates: hasPassword OR hasOtherProviders              │
│   ✓ Prevent unlinking last authentication method                    │
│   ✓ UI warns before unlinking last provider                         │
│   ✓ Suggest adding password before unlinking                        │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│              Threat: Token Replay Attack                             │
├─────────────────────────────────────────────────────────────────────┤
│ Attack Vector:                                                       │
│   1. Attacker intercepts Entra access token                          │
│   2. Attacker replays token to link provider                         │
│   3. Attacker gains access via social login                          │
├─────────────────────────────────────────────────────────────────────┤
│ Mitigation:                                                          │
│   ✓ Entra tokens have short expiration (1 hour)                     │
│   ✓ HTTPS enforced for all API calls                                │
│   ✓ Token validation checks signature + expiration                  │
│   ✓ LinkProvider requires active authenticated session              │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 8. Data Flow: idp Claim Parsing

```
┌────────────────────────────────────────────────────────────────────┐
│                 Entra External ID Token Structure                   │
├────────────────────────────────────────────────────────────────────┤
│ {                                                                   │
│   "oid": "b1a2c3d4-e5f6-7890-abcd-ef1234567890",  ← Entra user ID  │
│   "email": "john@facebook.com",                                     │
│   "given_name": "John",                                             │
│   "family_name": "Doe",                                             │
│   "idp": "facebook.com",                          ← Federation src  │
│   "email_verified": true,                                           │
│   "iss": "https://login.microsoftonline.com/...",                   │
│   "aud": "957e9865-fca0-4236-9276-a8643a7193b5",                    │
│   "exp": 1730501234,                                                │
│   "iat": 1730497634                                                 │
│ }                                                                   │
└────────────────────────────────────────────────────────────────────┘
                            │
                            │ Parse in EntraExternalIdService
                            ↓
┌────────────────────────────────────────────────────────────────────┐
│                       EntraUserInfo DTO                             │
├────────────────────────────────────────────────────────────────────┤
│ ObjectId: "b1a2c3d4-e5f6-7890-abcd-ef1234567890"                   │
│ Email: "john@facebook.com"                                          │
│ FirstName: "John"                                                   │
│ LastName: "Doe"                                                     │
│ IdentityProvider: "facebook.com"  ← From idp claim                 │
│ EmailVerified: true                                                 │
└────────────────────────────────────────────────────────────────────┘
                            │
                            │ Map in LoginWithEntraCommandHandler
                            ↓
┌────────────────────────────────────────────────────────────────────┐
│              FederatedProvider Enum Mapping                         │
├────────────────────────────────────────────────────────────────────┤
│ "facebook.com"              → FederatedProvider.Facebook (1)        │
│ "google.com"                → FederatedProvider.Google (2)          │
│ "appleid.apple.com"         → FederatedProvider.Apple (3)           │
│ "login.microsoftonline.com" → FederatedProvider.Microsoft (0)       │
│ "live.com"                  → FederatedProvider.Microsoft (0)       │
└────────────────────────────────────────────────────────────────────┘
                            │
                            │ Create domain entity
                            ↓
┌────────────────────────────────────────────────────────────────────┐
│                 ExternalLogin Value Object                          │
├────────────────────────────────────────────────────────────────────┤
│ Provider: FederatedProvider.Facebook (1)                            │
│ ExternalProviderId: "b1a2c3d4-e5f6-7890-abcd-ef1234567890"          │
│ ProviderEmail: "john@facebook.com"                                  │
│ LinkedAt: 2025-11-01T10:30:00Z                                      │
└────────────────────────────────────────────────────────────────────┘
                            │
                            │ Save to database
                            ↓
┌────────────────────────────────────────────────────────────────────┐
│                  external_logins Table Row                          │
├────────────────────────────────────────────────────────────────────┤
│ id: 42                                                              │
│ user_id: 123e4567-e89b-12d3-a456-426614174000                      │
│ provider: 1  (Facebook)                                             │
│ external_provider_id: "b1a2c3d4-e5f6-7890-abcd-ef1234567890"        │
│ provider_email: "john@facebook.com"                                 │
│ linked_at: 2025-11-01 10:30:00                                      │
└────────────────────────────────────────────────────────────────────┘
```

---

## 9. Migration Path: Phase 1 → Phase 2

```
┌────────────────────────────────────────────────────────────────────┐
│                    BEFORE (Phase 1)                                 │
├────────────────────────────────────────────────────────────────────┤
│ users table:                                                        │
│   id: 123                                                           │
│   email: john@example.com                                           │
│   identity_provider: 1 (EntraExternal)                              │
│   external_provider_id: "entra-oid-456"  ← Single provider ID      │
│   password_hash: NULL                                               │
└────────────────────────────────────────────────────────────────────┘
                            │
                            │ Run Migration
                            ↓
┌────────────────────────────────────────────────────────────────────┐
│                    AFTER (Phase 2)                                  │
├────────────────────────────────────────────────────────────────────┤
│ users table:                                                        │
│   id: 123                                                           │
│   email: john@example.com                                           │
│   identity_provider: 1 (EntraExternal)                              │
│   external_provider_id: "entra-oid-456"  ← DEPRECATED (keep temp)  │
│   password_hash: NULL                                               │
│                                                                     │
│ external_logins table (NEW):                                        │
│   id: 1                                                             │
│   user_id: 123                                                      │
│   provider: 0 (Microsoft)  ← Assume Microsoft for Phase 1 users    │
│   external_provider_id: "entra-oid-456"                             │
│   provider_email: john@example.com                                  │
│   linked_at: <user.created_at>                                      │
└────────────────────────────────────────────────────────────────────┘

Migration SQL:
  INSERT INTO external_logins (user_id, provider, external_provider_id, ...)
  SELECT id, 0, external_provider_id, email, created_at
  FROM users
  WHERE identity_provider = 1 AND external_provider_id IS NOT NULL;
```

---

## Summary

These diagrams illustrate:

1. **System Context** - Entra federates all social providers
2. **Container Architecture** - Clean Architecture layers
3. **Authentication Flows** - Social login + account linking
4. **Domain Model** - User aggregate + ExternalLogin value objects
5. **Database Schema** - Junction table for multi-provider support
6. **Component Interactions** - CQRS command handlers
7. **Security Threats** - Mitigation strategies
8. **Data Flow** - idp claim parsing
9. **Migration Path** - Phase 1 → Phase 2 upgrade

**Key Architectural Principles:**
- Entra External ID is the sole identity provider (federation hub)
- FederatedProvider enum tracks upstream social platforms
- ExternalLogin collection enables multi-provider support
- Domain enforces business rules (prevent lockouts, hijacking)
- Clean separation: API → Application → Domain → Infrastructure
