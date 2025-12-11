# TDD Implementation Guide: Authentication Parameter Security Fix

## Overview

This guide follows Test-Driven Development (TDD) with Domain-Driven Design (DDD) principles to fix the authentication parameter security vulnerability in 6 endpoints.

**Estimated Time**: 4-6 hours
**Priority**: CRITICAL (Security Vulnerability)
**Pre-requisite**: ADR-002 approved

## TDD Workflow: Red-Green-Refactor

```
┌─────────────────────────────────────────────────────────┐
│                    TDD CYCLE                            │
├─────────────────────────────────────────────────────────┤
│  RED   → Write failing test (expected secure behavior) │
│  GREEN → Implement minimum code to pass test           │
│  REFACTOR → Clean up code while keeping tests green    │
└─────────────────────────────────────────────────────────┘
```

## Phase 1: RED - Write Failing Tests (1-2 hours)

### Test File Structure

```
tests/
├── LankaConnect.API.Tests/
│   └── Controllers/
│       └── EventsControllerSecurityTests.cs (NEW)
├── LankaConnect.API.IntegrationTests/
│   └── Events/
│       └── WaitingListSecurityTests.cs (NEW)
└── LankaConnect.Application.Tests/
    └── Events/
        └── Commands/
            └── AddToWaitingListCommandHandlerTests.cs (VERIFY)
```

### 1.1 Unit Tests (RED Phase)

**File**: `C:\Work\LankaConnect\tests\LankaConnect.API.Tests\Controllers\EventsControllerSecurityTests.cs`

```csharp
using Xunit;
using Moq;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using LankaConnect.API.Controllers;
using LankaConnect.API.Extensions;
using LankaConnect.Application.Events.Commands.AddToWaitingList;
using LankaConnect.Application.Events.Commands.RemoveFromWaitingList;
using LankaConnect.Application.Events.Commands.PromoteFromWaitingList;
using LankaConnect.Application.Events.Commands.CancelRsvp;
using LankaConnect.Application.Events.Queries.GetUserRsvps;
using LankaConnect.Application.Events.Queries.GetUpcomingEventsForUser;
using LankaConnect.Domain.Common;

namespace LankaConnect.API.Tests.Controllers;

/// <summary>
/// Security tests for authentication parameter vulnerability
/// Tests that endpoints extract userId from JWT claims, not query parameters
/// </summary>
public class EventsControllerSecurityTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<EventsController>> _loggerMock;
    private readonly EventsController _controller;
    private readonly Guid _authenticatedUserId;
    private readonly Guid _attackerUserId;

    public EventsControllerSecurityTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<EventsController>>();
        _authenticatedUserId = Guid.NewGuid();
        _attackerUserId = Guid.NewGuid();

        // Setup controller with authenticated user context
        _controller = new EventsController(_mediatorMock.Object, _loggerMock.Object);
        SetupAuthenticatedUser(_controller, _authenticatedUserId);
    }

    #region Waiting List Endpoints

    [Fact]
    public async Task AddToWaitingList_Should_Use_JWT_UserId_Not_Query_Parameter()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<AddToWaitingListCommand>(), default))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.AddToWaitingList(eventId);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<AddToWaitingListCommand>(cmd =>
                cmd.EventId == eventId &&
                cmd.UserId == _authenticatedUserId), // Must use JWT user ID
            default),
            Times.Once,
            "AddToWaitingList must extract userId from JWT token, not accept as parameter");

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task RemoveFromWaitingList_Should_Use_JWT_UserId_Not_Query_Parameter()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveFromWaitingListCommand>(), default))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.RemoveFromWaitingList(eventId);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<RemoveFromWaitingListCommand>(cmd =>
                cmd.EventId == eventId &&
                cmd.UserId == _authenticatedUserId),
            default),
            Times.Once,
            "RemoveFromWaitingList must extract userId from JWT token");

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task PromoteFromWaitingList_Should_Use_JWT_UserId_Not_Query_Parameter()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<PromoteFromWaitingListCommand>(), default))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.PromoteFromWaitingList(eventId);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<PromoteFromWaitingListCommand>(cmd =>
                cmd.EventId == eventId &&
                cmd.UserId == _authenticatedUserId),
            default),
            Times.Once,
            "PromoteFromWaitingList must extract userId from JWT token");

        Assert.IsType<OkResult>(result);
    }

    #endregion

    #region RSVP Endpoints

    [Fact]
    public async Task CancelRsvp_Should_Use_JWT_UserId_Not_Query_Parameter()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<CancelRsvpCommand>(), default))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.CancelRsvp(eventId);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<CancelRsvpCommand>(cmd =>
                cmd.EventId == eventId &&
                cmd.UserId == _authenticatedUserId),
            default),
            Times.Once,
            "CancelRsvp must extract userId from JWT token");

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task GetMyRsvps_Should_Use_JWT_UserId_Not_Query_Parameter()
    {
        // Arrange
        var expectedRsvps = new List<RsvpDto>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUserRsvpsQuery>(), default))
            .ReturnsAsync(Result<IReadOnlyList<RsvpDto>>.Success(expectedRsvps));

        // Act
        var result = await _controller.GetMyRsvps();

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetUserRsvpsQuery>(query =>
                query.UserId == _authenticatedUserId),
            default),
            Times.Once,
            "GetMyRsvps must extract userId from JWT token");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetUpcomingEvents_Should_Use_JWT_UserId_Not_Query_Parameter()
    {
        // Arrange
        var expectedEvents = new List<EventDto>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUpcomingEventsForUserQuery>(), default))
            .ReturnsAsync(Result<IReadOnlyList<EventDto>>.Success(expectedEvents));

        // Act
        var result = await _controller.GetUpcomingEvents();

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetUpcomingEventsForUserQuery>(query =>
                query.UserId == _authenticatedUserId),
            default),
            Times.Once,
            "GetUpcomingEvents must extract userId from JWT token");

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region Security Boundary Tests

    [Fact]
    public void AddToWaitingList_Should_Not_Have_UserId_Query_Parameter()
    {
        // Arrange
        var method = typeof(EventsController).GetMethod(nameof(EventsController.AddToWaitingList));

        // Assert
        var parameters = method!.GetParameters();
        Assert.Single(parameters); // Only eventId parameter
        Assert.Equal("id", parameters[0].Name);
        Assert.Equal(typeof(Guid), parameters[0].ParameterType);
        Assert.False(parameters.Any(p => p.Name == "userId"),
            "AddToWaitingList must not accept userId as parameter (security vulnerability)");
    }

    [Theory]
    [InlineData(nameof(EventsController.RemoveFromWaitingList))]
    [InlineData(nameof(EventsController.PromoteFromWaitingList))]
    [InlineData(nameof(EventsController.CancelRsvp))]
    public void WaitingList_And_RSVP_Methods_Should_Not_Accept_UserId_Parameter(string methodName)
    {
        // Arrange
        var method = typeof(EventsController).GetMethod(methodName);

        // Assert
        var parameters = method!.GetParameters();
        Assert.False(parameters.Any(p => p.Name == "userId"),
            $"{methodName} must not accept userId as parameter (security vulnerability)");
    }

    [Theory]
    [InlineData(nameof(EventsController.GetMyRsvps))]
    [InlineData(nameof(EventsController.GetUpcomingEvents))]
    public void User_Query_Methods_Should_Not_Accept_UserId_Parameter(string methodName)
    {
        // Arrange
        var method = typeof(EventsController).GetMethod(methodName);

        // Assert
        var parameters = method!.GetParameters();
        Assert.Empty(parameters); // No parameters at all
        Assert.False(parameters.Any(p => p.Name == "userId"),
            $"{methodName} must not accept userId as parameter (security vulnerability)");
    }

    [Fact]
    public void AddToWaitingList_Should_Require_Authorization()
    {
        // Arrange
        var method = typeof(EventsController).GetMethod(nameof(EventsController.AddToWaitingList));

        // Assert
        var authorizeAttribute = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false);
        Assert.NotEmpty(authorizeAttribute);
    }

    #endregion

    #region Helper Methods

    private static void SetupAuthenticatedUser(ControllerBase controller, Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "test@example.com")
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    #endregion
}
```

### 1.2 Integration Tests (RED Phase)

**File**: `C:\Work\LankaConnect\tests\LankaConnect.API.IntegrationTests\Events\WaitingListSecurityTests.cs`

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using LankaConnect.API;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.API.IntegrationTests.Events;

/// <summary>
/// Integration tests for waiting list security
/// Tests end-to-end authorization and user ID extraction from JWT
/// </summary>
[Collection("IntegrationTests")]
public class WaitingListSecurityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public WaitingListSecurityTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task AddToWaitingList_Without_Authentication_Should_Return_401()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync(
            $"/api/Events/{eventId}/waiting-list",
            new StringContent(""));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddToWaitingList_Should_Use_JWT_UserId_From_Token()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var eventId = await CreateTestEvent();

        // Get JWT token for user1
        var user1Token = await GetJwtTokenForUser(user1Id);

        // Act: User1 tries to add themselves to waiting list
        // Even if they pass user2Id in query, it should use user1Id from token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);
        var response = await _client.PostAsync(
            $"/api/Events/{eventId}/waiting-list",
            new StringContent(""));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify: User1 is on waiting list (from token), not user2
        var waitingList = await GetWaitingList(eventId);
        Assert.Contains(waitingList, entry => entry.UserId == user1Id);
        Assert.DoesNotContain(waitingList, entry => entry.UserId == user2Id);
    }

    [Fact]
    public async Task Cannot_Manipulate_UserId_To_Cancel_Other_Users_Rsvp()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var eventId = await CreateTestEvent();

        // User2 creates an RSVP
        var user2Token = await GetJwtTokenForUser(user2Id);
        await CreateRsvp(eventId, user2Token);

        // Act: User1 tries to cancel User2's RSVP by manipulating query parameter
        var user1Token = await GetJwtTokenForUser(user1Id);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);
        var response = await _client.DeleteAsync(
            $"/api/Events/{eventId}/rsvp?userId={user2Id}"); // Attack attempt

        // Assert: Should fail or cancel user1's RSVP (not user2's)
        var user2Rsvp = await GetUserRsvp(eventId, user2Token);
        Assert.NotNull(user2Rsvp); // User2's RSVP should still exist
    }

    [Fact]
    public async Task GetMyRsvps_Should_Return_Only_Authenticated_Users_Rsvps()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var event1 = await CreateTestEvent();
        var event2 = await CreateTestEvent();

        // User1 RSVPs to event1
        var user1Token = await GetJwtTokenForUser(user1Id);
        await CreateRsvp(event1, user1Token);

        // User2 RSVPs to event2
        var user2Token = await GetJwtTokenForUser(user2Id);
        await CreateRsvp(event2, user2Token);

        // Act: User1 tries to get User2's RSVPs by manipulating query parameter
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);
        var response = await _client.GetAsync($"/api/Events/my-rsvps?userId={user2Id}"); // Attack attempt

        // Assert: Should return only User1's RSVPs (from token)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rsvps = await response.Content.ReadFromJsonAsync<List<RsvpDto>>();
        Assert.NotNull(rsvps);
        Assert.All(rsvps, rsvp => Assert.Equal(user1Id, rsvp.UserId)); // Only user1's RSVPs
        Assert.DoesNotContain(rsvps, rsvp => rsvp.UserId == user2Id); // No user2 RSVPs
    }

    [Fact]
    public async Task Swagger_Should_Include_All_Fixed_Endpoints()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var swagger = await response.Content.ReadAsStringAsync();

        // All 6 fixed endpoints + search/ics/share should be in Swagger
        Assert.Contains("/api/Events/{id}/waiting-list", swagger);
        Assert.Contains("/api/Events/{id}/waiting-list/promote", swagger);
        Assert.Contains("/api/Events/my-rsvps", swagger);
        Assert.Contains("/api/Events/upcoming", swagger);
        Assert.Contains("/api/Events/search", swagger);
        Assert.Contains("/api/Events/{id}/ics", swagger);
        Assert.Contains("/api/Events/{id}/share", swagger);
    }

    #region Helper Methods

    private async Task<Guid> CreateTestEvent()
    {
        // Implementation: Create test event and return ID
        throw new NotImplementedException();
    }

    private async Task<string> GetJwtTokenForUser(Guid userId)
    {
        // Implementation: Generate JWT token for test user
        throw new NotImplementedException();
    }

    private async Task CreateRsvp(Guid eventId, string token)
    {
        // Implementation: Create RSVP for event
        throw new NotImplementedException();
    }

    private async Task<List<WaitingListEntryDto>> GetWaitingList(Guid eventId)
    {
        // Implementation: Get waiting list for event
        throw new NotImplementedException();
    }

    private async Task<RsvpDto?> GetUserRsvp(Guid eventId, string token)
    {
        // Implementation: Get user's RSVP for event
        throw new NotImplementedException();
    }

    #endregion
}
```

### 1.3 Run Tests (Expect Failures)

```bash
cd C:\Work\LankaConnect
dotnet test tests/LankaConnect.API.Tests/LankaConnect.API.Tests.csproj --filter "FullyQualifiedName~EventsControllerSecurityTests"
```

**Expected Output**:
```
Starting test execution...
[FAIL] AddToWaitingList_Should_Use_JWT_UserId_Not_Query_Parameter
  Expected: method signature without userId parameter
  Actual: method has [FromQuery] Guid userId parameter

[FAIL] AddToWaitingList_Should_Not_Have_UserId_Query_Parameter
  Expected: 1 parameter (eventId)
  Actual: 2 parameters (eventId, userId)

Total tests: 10
Passed: 0
Failed: 10
Time: 1.2s
```

## Phase 2: GREEN - Implement Fixes (1-2 hours)

### 2.1 Update Controller Methods

**File**: `C:\Work\LankaConnect\src\LankaConnect.API\Controllers\EventsController.cs`

**Change 1: AddToWaitingList (Line 676-688)**
```csharp
// BEFORE:
[HttpPost("{id:guid}/waiting-list")]
[Authorize]
public async Task<IActionResult> AddToWaitingList(Guid id, [FromQuery] Guid userId)
{
    Logger.LogInformation("Adding user {UserId} to waiting list for event {EventId}", userId, id);
    var command = new AddToWaitingListCommand(id, userId);
    var result = await Mediator.Send(command);
    return HandleResult(result);
}

// AFTER:
[HttpPost("{id:guid}/waiting-list")]
[Authorize]
public async Task<IActionResult> AddToWaitingList(Guid id)
{
    var userId = User.GetUserId(); // Extract from JWT token
    Logger.LogInformation("Adding user {UserId} to waiting list for event {EventId}", userId, id);
    var command = new AddToWaitingListCommand(id, userId);
    var result = await Mediator.Send(command);
    return HandleResult(result);
}
```

**Change 2: RemoveFromWaitingList (Line 694-707)**
```csharp
// BEFORE:
[HttpDelete("{id:guid}/waiting-list")]
[Authorize]
public async Task<IActionResult> RemoveFromWaitingList(Guid id, [FromQuery] Guid userId)

// AFTER:
[HttpDelete("{id:guid}/waiting-list")]
[Authorize]
public async Task<IActionResult> RemoveFromWaitingList(Guid id)
{
    var userId = User.GetUserId();
    // ... rest of implementation
}
```

**Change 3: PromoteFromWaitingList (Line 712-725)**
```csharp
// BEFORE:
[HttpPost("{id:guid}/waiting-list/promote")]
[Authorize]
public async Task<IActionResult> PromoteFromWaitingList(Guid id, [FromQuery] Guid userId)

// AFTER:
[HttpPost("{id:guid}/waiting-list/promote")]
[Authorize]
public async Task<IActionResult> PromoteFromWaitingList(Guid id)
{
    var userId = User.GetUserId();
    // ... rest of implementation
}
```

**Change 4: CancelRsvp (Line 348-356)**
```csharp
// BEFORE:
[HttpDelete("{id:guid}/rsvp")]
[Authorize]
public async Task<IActionResult> CancelRsvp(Guid id, [FromQuery] Guid userId)

// AFTER:
[HttpDelete("{id:guid}/rsvp")]
[Authorize]
public async Task<IActionResult> CancelRsvp(Guid id)
{
    var userId = User.GetUserId();
    // ... rest of implementation
}
```

**Change 5: GetMyRsvps (Line 387-395)**
```csharp
// BEFORE:
[HttpGet("my-rsvps")]
[Authorize]
public async Task<IActionResult> GetMyRsvps([FromQuery] Guid userId)

// AFTER:
[HttpGet("my-rsvps")]
[Authorize]
public async Task<IActionResult> GetMyRsvps()
{
    var userId = User.GetUserId();
    // ... rest of implementation
}
```

**Change 6: GetUpcomingEvents (Line 405-413)**
```csharp
// BEFORE:
[HttpGet("upcoming")]
[Authorize]
public async Task<IActionResult> GetUpcomingEvents([FromQuery] Guid userId)

// AFTER:
[HttpGet("upcoming")]
[Authorize]
public async Task<IActionResult> GetUpcomingEvents()
{
    var userId = User.GetUserId();
    // ... rest of implementation
}
```

### 2.2 Run Tests Again (Expect Passes)

```bash
dotnet test tests/LankaConnect.API.Tests/LankaConnect.API.Tests.csproj --filter "FullyQualifiedName~EventsControllerSecurityTests"
```

**Expected Output**:
```
Starting test execution...
[PASS] AddToWaitingList_Should_Use_JWT_UserId_Not_Query_Parameter
[PASS] RemoveFromWaitingList_Should_Use_JWT_UserId_Not_Query_Parameter
[PASS] PromoteFromWaitingList_Should_Use_JWT_UserId_Not_Query_Parameter
[PASS] CancelRsvp_Should_Use_JWT_UserId_Not_Query_Parameter
[PASS] GetMyRsvps_Should_Use_JWT_UserId_Not_Query_Parameter
[PASS] GetUpcomingEvents_Should_Use_JWT_UserId_Not_Query_Parameter
[PASS] AddToWaitingList_Should_Not_Have_UserId_Query_Parameter
[PASS] WaitingList_And_RSVP_Methods_Should_Not_Accept_UserId_Parameter
[PASS] User_Query_Methods_Should_Not_Accept_UserId_Parameter
[PASS] AddToWaitingList_Should_Require_Authorization

Total tests: 10
Passed: 10
Failed: 0
Time: 1.5s
```

## Phase 3: REFACTOR - Clean Up and Optimize (30 mins)

### 3.1 Code Cleanup

- [ ] Remove any commented-out code
- [ ] Ensure consistent logging patterns
- [ ] Add XML documentation comments if missing
- [ ] Run code formatter: `dotnet format`

### 3.2 Test Refactoring

- [ ] Extract common test setup to base class
- [ ] Use test data builders for complex objects
- [ ] Add parameterized tests for similar scenarios

### 3.3 Verify All Tests Pass

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportsDirectory=./coverage
```

## Phase 4: Integration and Swagger Verification (30 mins)

### 4.1 Build and Run Locally

```bash
cd C:\Work\LankaConnect\src\LankaConnect.API
dotnet build
dotnet run
```

### 4.2 Manual Swagger Verification

1. Navigate to: `https://localhost:5001/swagger`
2. Verify all 7 endpoints appear:
   - ✅ GET /api/Events/search
   - ✅ POST /api/Events/{id}/waiting-list
   - ✅ DELETE /api/Events/{id}/waiting-list
   - ✅ POST /api/Events/{id}/waiting-list/promote
   - ✅ GET /api/Events/{id}/waiting-list
   - ✅ GET /api/Events/{id}/ics
   - ✅ POST /api/Events/{id}/share

### 4.3 Manual Security Testing

**Test 1: Cannot manipulate userId**
```bash
# Get JWT token for user1
TOKEN=$(curl -X POST https://localhost:5001/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user1@test.com","password":"Password123!"}' \
  | jq -r '.token')

# Try to add user2 to waiting list (should add user1 instead)
curl -X POST "https://localhost:5001/api/Events/{event-id}/waiting-list?userId=user2-id" \
  -H "Authorization: Bearer $TOKEN"

# Verify user1 was added (from token), not user2
curl -X GET "https://localhost:5001/api/Events/{event-id}/waiting-list" \
  -H "Authorization: Bearer $TOKEN"
```

## Phase 5: Deployment Strategy (30 mins)

### 5.1 Pre-Deployment Checklist

- [ ] All unit tests pass (100% success rate)
- [ ] All integration tests pass
- [ ] Code coverage ≥ 90% for affected methods
- [ ] Security tests pass
- [ ] Swagger.json generated successfully
- [ ] Manual security testing complete
- [ ] Code review approved
- [ ] ADR-002 signed off

### 5.2 Staging Deployment

```bash
# Build for staging
dotnet publish -c Release -o ./publish

# Deploy to Azure staging slot
az webapp deployment source config-zip \
  --resource-group lankaconnect-rg \
  --name lankaconnect-api-staging \
  --src ./publish.zip
```

### 5.3 Smoke Tests (Staging)

```bash
# Test staging swagger
curl https://lankaconnect-api-staging.azurewebsites.net/swagger/v1/swagger.json | jq '.paths | keys'

# Expected: All 7 endpoints present
```

### 5.4 Production Deployment

**Canary Deployment** (10% traffic):
```bash
# Deploy to production with canary
az webapp traffic-routing set \
  --resource-group lankaconnect-rg \
  --name lankaconnect-api \
  --distribution staging=10
```

**Monitor for 24 hours**:
- Error rate < 0.1%
- No security incidents
- No 500 errors related to userId

**Full Rollout**:
```bash
az webapp traffic-routing set \
  --resource-group lankaconnect-rg \
  --name lankaconnect-api \
  --distribution staging=100
```

## Success Metrics

### Code Quality
- ✅ Test coverage: 95%+ (target)
- ✅ All tests passing: 100%
- ✅ Code review approved
- ✅ Static analysis: 0 security warnings

### Security
- ✅ OWASP A01 vulnerability eliminated
- ✅ Cannot manipulate userId to impersonate users
- ✅ All authenticated endpoints use JWT claims
- ✅ Audit trail shows correct user identity

### Functionality
- ✅ All 7 endpoints appear in Swagger
- ✅ Waiting list operations work correctly
- ✅ RSVP operations work correctly
- ✅ User dashboard endpoints work correctly

### Performance
- ✅ No performance degradation (<5ms overhead for GetUserId())
- ✅ Response time: p95 < 200ms
- ✅ Error rate: < 0.1%

## Rollback Plan

If issues occur in production:

```bash
# Immediate rollback to previous version
az webapp deployment slot swap \
  --resource-group lankaconnect-rg \
  --name lankaconnect-api \
  --slot staging \
  --action swap
```

## Post-Deployment Actions

1. **Monitor for 48 hours**:
   - Error rates
   - Security incidents
   - User feedback

2. **Document lessons learned**:
   - Update security checklist
   - Update TDD process documentation
   - Share knowledge with team

3. **Plan preventive measures**:
   - Add static analysis rules to catch similar issues
   - Create security code review checklist
   - Schedule security training

---

**Total Time Estimate**: 4-6 hours
**Risk Level**: LOW (well-tested, security-focused)
**Business Value**: HIGH (eliminates critical vulnerability)
