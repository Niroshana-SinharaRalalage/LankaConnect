# ADR-002: Fix Authentication Parameter Security Vulnerability

**Status**: PROPOSED
**Date**: 2025-11-04
**Decision Makers**: System Architect, Security Team
**Priority**: CRITICAL (Security Vulnerability)

## Context

Six API endpoints in `EventsController.cs` accept `[FromQuery] Guid userId` parameters on `[Authorize]`-protected endpoints, creating a **critical security vulnerability** where authenticated users can impersonate other users by manipulating the query parameter.

### Affected Endpoints

| Line | Method | Route | Vulnerability |
|------|--------|-------|---------------|
| 348 | `CancelRsvp` | `DELETE /api/Events/{id}/rsvp` | Can cancel any user's RSVP |
| 387 | `GetMyRsvps` | `GET /api/Events/my-rsvps` | Can view any user's RSVPs |
| 405 | `GetUpcomingEvents` | `GET /api/Events/upcoming` | Can view any user's events |
| 681 | `AddToWaitingList` | `POST /api/Events/{id}/waiting-list` | Can add any user to list |
| 699 | `RemoveFromWaitingList` | `DELETE /api/Events/{id}/waiting-list` | Can remove any user from list |
| 717 | `PromoteFromWaitingList` | `POST /api/Events/{id}/waiting-list/promote` | Can promote any user |

### Security Impact

**Severity**: HIGH (CVSS 8.1)
- **OWASP**: A01:2021 - Broken Access Control
- **CWE**: CWE-639 - Authorization Bypass Through User-Controlled Key
- **Attack Vector**: Authenticated attacker can manipulate userId to access/modify other users' data
- **Exploitability**: EASY (simple query parameter change)

**Example Attack**:
```bash
# Attacker (userId=111) cancels victim's RSVP (userId=222)
curl -X DELETE "https://api.lankaconnect.com/api/Events/abc123/rsvp?userId=222" \
  -H "Authorization: Bearer <attacker-token>"
```

## Decision

**REMOVE all `[FromQuery] Guid userId` parameters from authenticated endpoints and extract userId from authenticated JWT token claims using `User.GetUserId()` extension method.**

### Rationale

1. **Security First**: User identity MUST come from cryptographically verified JWT token, not user-supplied input
2. **Principle of Least Privilege**: Users should only access their own resources
3. **REST Best Practices**: Authentication data belongs in headers (JWT), not query parameters
4. **Audit Trail**: All actions must be tied to authenticated identity for compliance
5. **Defense in Depth**: Never trust client-supplied identity claims

### Existing Infrastructure

The codebase already provides secure user ID extraction via `ClaimsPrincipalExtensions`:

```csharp
// C:\Work\LankaConnect\src\LankaConnect.API\Extensions\ClaimsPrincipalExtensions.cs
public static Guid GetUserId(this ClaimsPrincipal user)
{
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrWhiteSpace(userIdClaim))
        throw new InvalidOperationException("User ID claim not found");
    return Guid.Parse(userIdClaim);
}
```

## Implementation Pattern

### BEFORE (Vulnerable):
```csharp
[HttpPost("{id:guid}/waiting-list")]
[Authorize]
public async Task<IActionResult> AddToWaitingList(Guid id, [FromQuery] Guid userId)
{
    Logger.LogInformation("Adding user {UserId} to waiting list for event {EventId}", userId, id);
    var command = new AddToWaitingListCommand(id, userId);
    var result = await Mediator.Send(command);
    return HandleResult(result);
}
```

### AFTER (Secure):
```csharp
[HttpPost("{id:guid}/waiting-list")]
[Authorize]
public async Task<IActionResult> AddToWaitingList(Guid id)
{
    var userId = User.GetUserId(); // Extract from authenticated JWT token
    Logger.LogInformation("Adding user {UserId} to waiting list for event {EventId}", userId, id);
    var command = new AddToWaitingListCommand(id, userId);
    var result = await Mediator.Send(command);
    return HandleResult(result);
}
```

## Changes Required

### 1. Controller Method Signatures

**File**: `C:\Work\LankaConnect\src\LankaConnect.API\Controllers\EventsController.cs`

| Line | Method | Change |
|------|--------|--------|
| 348 | `CancelRsvp` | Remove `[FromQuery] Guid userId`, add `var userId = User.GetUserId();` |
| 387 | `GetMyRsvps` | Remove `[FromQuery] Guid userId`, add `var userId = User.GetUserId();` |
| 405 | `GetUpcomingEvents` | Remove `[FromQuery] Guid userId`, add `var userId = User.GetUserId();` |
| 681 | `AddToWaitingList` | Remove `[FromQuery] Guid userId`, add `var userId = User.GetUserId();` |
| 699 | `RemoveFromWaitingList` | Remove `[FromQuery] Guid userId`, add `var userId = User.GetUserId();` |
| 717 | `PromoteFromWaitingList` | Remove `[FromQuery] Guid userId`, add `var userId = User.GetUserId();` |

### 2. Swagger Documentation Updates

**File**: `C:\Work\LankaConnect\src\LankaConnect.API\Program.cs`

No changes required - Swagger will automatically update once method signatures are fixed.

### 3. Request DTOs (No Changes)

The following DTOs remain unchanged (used for RSVP quantity, not identity):
- `RsvpRequest(Guid UserId, int Quantity)` - Line 809
- `UpdateRsvpRequest(Guid UserId, int NewQuantity)` - Line 810

**Note**: These DTOs accept userId in request body for admin/organizer use cases (future feature). For user-facing endpoints, always use `User.GetUserId()`.

## Testing Strategy

### Test-Driven Development (TDD) Approach

#### Red Phase: Write Failing Tests
```csharp
// Test: Cannot manipulate userId to access other user's data
[Fact]
public async Task AddToWaitingList_Should_Use_Authenticated_UserId_Not_Query_Parameter()
{
    // Arrange
    var authenticatedUserId = Guid.NewGuid();
    var differentUserId = Guid.NewGuid();
    var eventId = Guid.NewGuid();

    // Mock User.GetUserId() to return authenticatedUserId
    _mockUser.Setup(u => u.GetUserId()).Returns(authenticatedUserId);

    // Act
    await _controller.AddToWaitingList(eventId);

    // Assert
    _mockMediator.Verify(m => m.Send(
        It.Is<AddToWaitingListCommand>(cmd =>
            cmd.EventId == eventId &&
            cmd.UserId == authenticatedUserId), // Must use authenticated user ID
        It.IsAny<CancellationToken>()),
        Times.Once);
}
```

#### Green Phase: Implement Fix
Apply the changes from "Implementation Pattern" above.

#### Refactor Phase: Cleanup
Remove any dead code and ensure consistency across all endpoints.

### Unit Tests Required

**File**: `C:\Work\LankaConnect\tests\LankaConnect.API.Tests\Controllers\EventsControllerTests.cs`

```csharp
public class EventsControllerSecurityTests
{
    [Theory]
    [InlineData("AddToWaitingList")]
    [InlineData("RemoveFromWaitingList")]
    [InlineData("PromoteFromWaitingList")]
    [InlineData("CancelRsvp")]
    [InlineData("GetMyRsvps")]
    [InlineData("GetUpcomingEvents")]
    public async Task Endpoint_Should_Use_Authenticated_UserId(string methodName)
    {
        // Test: All affected endpoints extract userId from authenticated claims
    }

    [Fact]
    public async Task AddToWaitingList_Without_Authentication_Should_Return_401()
    {
        // Test: Unauthenticated requests fail
    }

    [Fact]
    public async Task AddToWaitingList_Should_Not_Accept_UserId_Query_Parameter()
    {
        // Test: Method signature doesn't have [FromQuery] Guid userId
    }
}
```

### Integration Tests Required

**File**: `C:\Work\LankaConnect\tests\LankaConnect.API.IntegrationTests\EventsEndpointTests.cs`

```csharp
[Collection("IntegrationTests")]
public class WaitingListEndpointSecurityTests : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task AddToWaitingList_Should_Use_JWT_UserId_Not_Query_Parameter()
    {
        // Arrange
        var user1Token = await GetJwtToken(user1Id);
        var user2Token = await GetJwtToken(user2Id);

        // Act: User1 tries to add User2 to waiting list (should fail)
        var response = await _client.PostAsync(
            $"/api/Events/{eventId}/waiting-list?userId={user2Id}",
            new StringContent(""),
            headers: new { Authorization = $"Bearer {user1Token}" }
        );

        // Assert: User1 was added (from token), not User2
        var waitingList = await GetWaitingList(eventId);
        Assert.Contains(waitingList, entry => entry.UserId == user1Id);
        Assert.DoesNotContain(waitingList, entry => entry.UserId == user2Id);
    }

    [Fact]
    public async Task WaitingList_Endpoints_Should_Appear_In_Swagger()
    {
        // Act: Generate swagger.json
        var swagger = await _client.GetFromJsonAsync<OpenApiDocument>("/swagger/v1/swagger.json");

        // Assert: All 6 fixed endpoints appear in Swagger
        Assert.Contains(swagger.Paths, p => p.Key == "/api/Events/{id}/waiting-list");
    }
}
```

### Security Tests Required

**File**: `C:\Work\LankaConnect\tests\LankaConnect.API.SecurityTests\AuthorizationTests.cs`

```csharp
[Collection("SecurityTests")]
public class WaitingListAuthorizationTests
{
    [Fact]
    public async Task Cannot_Manipulate_UserId_To_Add_Other_User_To_WaitingList()
    {
        // OWASP A01:2021 - Broken Access Control
    }

    [Fact]
    public async Task Cannot_View_Other_Users_RSVPs()
    {
        // Privacy violation test
    }

    [Fact]
    public async Task Audit_Logs_Show_Correct_User_Identity()
    {
        // Compliance test (GDPR/CCPA)
    }
}
```

## Consequences

### Positive

1. **Security**: Eliminates critical authorization bypass vulnerability
2. **Compliance**: Meets GDPR/CCPA requirements for user data protection
3. **Consistency**: Aligns with authentication patterns used elsewhere (e.g., line 133)
4. **Swagger**: Endpoints will appear correctly in swagger.json
5. **Audit Trail**: All actions tied to authenticated user for forensics
6. **Best Practices**: Follows OWASP and REST security guidelines

### Negative

1. **Breaking Change**: Client applications must remove userId query parameters (API versioning recommended)
2. **Testing Overhead**: Requires comprehensive security testing
3. **Migration**: Existing API consumers need updates

### Neutral

1. **Performance**: No impact (same number of operations)
2. **Logging**: Update log messages to show userId is from token (already done in current code)

## Migration Strategy

### API Versioning (Recommended)

**Option 1: Version Headers**
```csharp
[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("2.0")]
public class EventsController : BaseController<EventsController>
```

**Option 2: Immediate Fix (For Security Vulnerabilities)**
Deploy fix immediately without versioning since current implementation is insecure.

### Client Migration

1. **Update API Documentation**: Document that userId is extracted from JWT token
2. **Release Notes**: Notify clients of security fix and breaking change
3. **Monitoring**: Track 400 errors from clients still sending userId query parameter
4. **Deprecation Timeline**: N/A (immediate fix for security vulnerability)

## Verification Checklist

### Pre-Deployment
- [ ] All 6 method signatures updated (removed `[FromQuery] Guid userId`)
- [ ] All 6 methods call `User.GetUserId()` to extract userId
- [ ] Unit tests pass (TDD green phase)
- [ ] Integration tests pass
- [ ] Security tests pass
- [ ] Swagger.json generated successfully
- [ ] All 7 missing endpoints appear in swagger.json (including search, ics, share)
- [ ] Code review approved by security team
- [ ] Static analysis passes (no security warnings)

### Post-Deployment (Staging)
- [ ] Manual testing: Cannot manipulate userId via query parameter
- [ ] Manual testing: Authenticated requests work correctly
- [ ] Manual testing: Unauthenticated requests return 401
- [ ] Penetration testing: Authorization bypass attempts fail
- [ ] Monitoring: No 500 errors related to user ID extraction
- [ ] Swagger UI: All endpoints visible and documented

### Post-Deployment (Production)
- [ ] Rollout to 10% of traffic
- [ ] Monitor error rates for 24 hours
- [ ] Full rollout if error rate < 0.1%
- [ ] Security team sign-off

## References

- **OWASP A01:2021**: [Broken Access Control](https://owasp.org/Top10/A01_2021-Broken_Access_Control/)
- **CWE-639**: [Authorization Bypass Through User-Controlled Key](https://cwe.mitre.org/data/definitions/639.html)
- **REST Security**: [OWASP REST Security Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/REST_Security_Cheat_Sheet.html)
- **JWT Best Practices**: [RFC 8725](https://datatracker.ietf.org/doc/html/rfc8725)

## Related ADRs

- ADR-001: Authentication Parameter Design (parent)
- ADR-003: API Versioning Strategy (future)
- ADR-004: Security Testing Requirements (future)

## Approval

| Role | Name | Date | Status |
|------|------|------|--------|
| System Architect | TBD | 2025-11-04 | PENDING |
| Security Lead | TBD | 2025-11-04 | PENDING |
| Tech Lead | TBD | 2025-11-04 | PENDING |

---

**Next Steps**:
1. Review this ADR with security team
2. Approve implementation plan
3. Create TDD test suite
4. Implement fixes following TDD
5. Deploy to staging with monitoring
6. Security penetration testing
7. Production deployment
