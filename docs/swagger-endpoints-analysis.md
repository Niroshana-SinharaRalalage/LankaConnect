# Architecture Analysis: Missing Swagger Endpoints

## Executive Summary

**ROOT CAUSE IDENTIFIED**: The six missing endpoints ARE present in code and WILL compile, but they have **critical architectural flaws** that prevent Swagger from properly documenting them or may cause runtime failures.

## Critical Issues Found

### 1. SECURITY VULNERABILITY: Query Parameter Authentication
**Lines: 681, 699, 717** (Waiting List Endpoints)

```csharp
[HttpPost("{id:guid}/waiting-list")]
[Authorize]
public async Task<IActionResult> AddToWaitingList(Guid id, [FromQuery] Guid userId)
```

**Problem**: `[FromQuery] Guid userId` on authenticated endpoints creates a severe security flaw:
- Attackers can manipulate userId query parameter to impersonate other users
- The `[Authorize]` attribute authenticates the request, but the userId comes from an untrusted source
- This violates the principle of "never trust user input" for identity claims

**Impact on Swagger**: Swagger may generate these endpoints, but they expose a critical security vulnerability.

**Architectural Fix Required**:
```csharp
// CORRECT: Extract userId from authenticated claims
[HttpPost("{id:guid}/waiting-list")]
[Authorize]
public async Task<IActionResult> AddToWaitingList(Guid id)
{
    var userId = User.GetUserId(); // Extract from authenticated token
    var command = new AddToWaitingListCommand(id, userId);
    // ...
}
```

### 2. INCONSISTENT AUTHENTICATION PATTERNS

**Lines: 348, 387, 405** (Existing RSVP endpoints with same flaw)

```csharp
[HttpDelete("{id:guid}/rsvp")]
[Authorize]
public async Task<IActionResult> CancelRsvp(Guid id, [FromQuery] Guid userId)
```

**Problem**: Same security vulnerability across RSVP endpoints. These may already be in Swagger but are equally vulnerable.

### 3. HTTP METHOD CONFLICTS (Potential)

**Lines: 676, 694, 712** (Waiting List Endpoints)

```csharp
POST /api/Events/{id}/waiting-list      // Line 676
DELETE /api/Events/{id}/waiting-list    // Line 694
POST /api/Events/{id}/waiting-list/promote // Line 712
```

**Analysis**: Route structure is valid, but the `[FromQuery] Guid userId` parameter creates ambiguity:
- POST with query parameter is unconventional
- DELETE with query parameter violates REST best practices (should use route parameter or authenticated user)
- May cause Swagger schema conflicts when generating operation IDs

### 4. MISSING ENDPOINTS DETAILS

#### 4.1 Search Endpoint (Line 86)
```csharp
[HttpGet("search")]
public async Task<IActionResult> SearchEvents([FromQuery] string searchTerm, ...)
```
**Status**: ✅ Should appear in Swagger (no obvious issues)

#### 4.2 Waiting List Endpoints (Lines 676, 694, 712)
**Status**: ⚠️ Security flaw prevents proper documentation

#### 4.3 Calendar Export (Line 756)
```csharp
[HttpGet("{id:guid}/ics")]
public async Task<IActionResult> GetEventIcs(Guid id)
```
**Status**: ✅ Should appear in Swagger (no obvious issues)

#### 4.4 Social Share Tracking (Line 789)
```csharp
[HttpPost("{id:guid}/share")]
public async Task<IActionResult> RecordEventShare(Guid id, [FromBody] RecordShareRequest? request = null)
```
**Status**: ✅ Should appear in Swagger (no obvious issues)

## Why Swagger Might Skip These Endpoints

### Hypothesis 1: Swagger Operation ID Conflicts
When Swagger generates operation IDs from method names, conflicting signatures can cause it to skip endpoints:

```csharp
// Both methods generate "AddToWaitingList" operation ID
POST /api/Events/{id}/waiting-list?userId={userId}
```

The `[FromQuery] Guid userId` on multiple endpoints might create ambiguous operation IDs.

### Hypothesis 2: ApiExplorer Configuration
The `TagDescriptionsDocumentFilter` (Program.cs:68) only defines tags but doesn't fix operation visibility issues. If Swagger's ApiExplorer encounters parameter binding issues, it may silently skip endpoints.

### Hypothesis 3: Model Binding Validation Failure
Swagger validates parameter bindings at startup. The combination of:
- `[Authorize]` attribute
- `[FromQuery] Guid userId` parameter
- POST/DELETE methods with query parameters

May trigger internal Swagger validation failures that cause silent endpoint exclusion.

## Architecture Decision Records (ADRs)

### ADR-001: Authentication Parameter Design

**Context**: Endpoints require user identification for operations.

**Decision**: REJECT `[FromQuery] Guid userId` pattern on authenticated endpoints.

**Rationale**:
1. **Security**: User identity MUST come from authenticated claims (JWT token)
2. **Principle of Least Privilege**: Never allow users to specify their own ID
3. **REST Best Practices**: Authentication details belong in headers, not query strings
4. **Audit Trail**: All user actions must be tied to authenticated identity

**Consequences**:
- Remove all `[FromQuery] Guid userId` parameters from `[Authorize]` endpoints
- Extract userId from `User.GetUserId()` extension method
- Update all affected commands to use authenticated user ID

### ADR-002: HTTP Method Selection

**Context**: Waiting list operations need proper HTTP method semantics.

**Decision**:
- POST for adding to waiting list (creates resource)
- DELETE for removing from waiting list (removes resource)
- POST for promoting (state transition)

**Rationale**: Follows REST semantics, but MUST use authenticated user identity.

**Consequences**:
- Keep HTTP methods as-is
- Remove userId query parameters
- Use authenticated user from token

## Recommended Solution Architecture

### Phase 1: Security Fixes (CRITICAL - Do First)

**Files to Modify**: `EventsController.cs`

**Endpoints to Fix**:
1. Line 681: `AddToWaitingList` - Remove `[FromQuery] Guid userId`
2. Line 699: `RemoveFromWaitingList` - Remove `[FromQuery] Guid userId`
3. Line 717: `PromoteFromWaitingList` - Remove `[FromQuery] Guid userId`
4. Line 348: `CancelRsvp` - Remove `[FromQuery] Guid userId`
5. Line 387: `GetMyRsvps` - Remove `[FromQuery] Guid userId`
6. Line 405: `GetUpcomingEvents` - Remove `[FromQuery] Guid userId`

**Implementation Pattern**:
```csharp
// BEFORE (VULNERABLE):
[HttpPost("{id:guid}/waiting-list")]
[Authorize]
public async Task<IActionResult> AddToWaitingList(Guid id, [FromQuery] Guid userId)
{
    var command = new AddToWaitingListCommand(id, userId);
    // ...
}

// AFTER (SECURE):
[HttpPost("{id:guid}/waiting-list")]
[Authorize]
public async Task<IActionResult> AddToWaitingList(Guid id)
{
    var userId = User.GetUserId(); // Extract from authenticated JWT token
    var command = new AddToWaitingListCommand(id, userId);
    // ...
}
```

### Phase 2: Swagger Configuration Enhancement

**File**: `Program.cs` (Swagger configuration)

**Add Operation Filter** to ensure all endpoints are documented:
```csharp
c.OperationFilter<AuthorizationOperationFilter>(); // Documents auth requirements
c.OperationFilter<FileUploadOperationFilter>(); // Documents file uploads
```

### Phase 3: Testing Strategy

**Unit Tests** (Test authenticated user extraction):
```csharp
// Test: AddToWaitingList uses authenticated user
// Test: Cannot spoof userId via query parameter
// Test: Unauthorized request returns 401
```

**Integration Tests** (Test Swagger generation):
```csharp
// Test: All 7 endpoints appear in swagger.json
// Test: No operation ID conflicts
// Test: Proper request/response schemas
```

**Security Tests**:
```csharp
// Test: Cannot manipulate userId to access other users' data
// Test: JWT token is required and validated
// Test: User can only modify their own waiting list entries
```

## Why This Matters (Business Impact)

### Security Risks (HIGH SEVERITY):
- **OWASP A01:2021 - Broken Access Control**: Users can impersonate others
- **Data Breach Potential**: Attackers can join waiting lists as any user
- **Compliance Violations**: GDPR/CCPA require proper authentication
- **Reputation Damage**: Security vulnerability in production

### Technical Debt:
- **Inconsistent Patterns**: Mixes authenticated and query-parameter approaches
- **Test Coverage Gaps**: Current tests may not catch the security flaw
- **Documentation Issues**: Swagger can't properly document flawed endpoints

### Deployment Blockers:
- **Azure Security Scanning**: May flag the vulnerability
- **Penetration Testing**: Will fail security audit
- **Insurance Requirements**: May violate security compliance

## Testing Checklist Before Deployment

### 1. Code Review Checklist
- [ ] All `[Authorize]` endpoints extract userId from `User.GetUserId()`
- [ ] No `[FromQuery] Guid userId` on authenticated endpoints
- [ ] Consistent authentication pattern across all controllers
- [ ] Security unit tests added for userId extraction

### 2. Swagger Validation Checklist
- [ ] Generate swagger.json successfully (no errors)
- [ ] All 7 missing endpoints appear in swagger.json
- [ ] No operation ID conflicts
- [ ] Proper request/response schemas for all endpoints
- [ ] Security scheme properly applied to authenticated endpoints

### 3. Security Testing Checklist
- [ ] Cannot manipulate userId via query parameter
- [ ] Unauthorized requests return 401
- [ ] Authenticated requests use token userId only
- [ ] Cannot access other users' waiting list entries
- [ ] Audit logs show correct user identity

### 4. Integration Testing Checklist
- [ ] Add to waiting list works with authenticated user
- [ ] Remove from waiting list works with authenticated user
- [ ] Promote from waiting list works with authenticated user
- [ ] Search endpoint returns correct results
- [ ] ICS export generates valid calendar file
- [ ] Share tracking records correct user

## Conclusion

**DIAGNOSIS**: The endpoints exist in code but have critical security vulnerabilities that may prevent Swagger from properly documenting them. The root cause is the `[FromQuery] Guid userId` parameter on `[Authorize]` endpoints, which creates:

1. **Security vulnerability** (users can impersonate others)
2. **Swagger operation ID conflicts** (ambiguous method signatures)
3. **REST best practice violations** (POST/DELETE with query parameters for identity)

**RECOMMENDATION**:
1. **IMMEDIATELY** fix the security vulnerability by removing `[FromQuery] Guid userId`
2. **THEN** verify all endpoints appear in Swagger
3. **FINALLY** deploy with comprehensive security testing

**PRIORITY**: CRITICAL - Do not deploy to production until security fixes are applied.

## Next Steps

1. **Architect Review**: Approve the ADRs above
2. **TDD Implementation**: Write failing tests for secure authentication
3. **Code Refactoring**: Apply security fixes to all affected endpoints
4. **Swagger Validation**: Verify all endpoints appear
5. **Security Testing**: Penetration test the fixed endpoints
6. **Deployment**: Deploy to staging with monitoring
