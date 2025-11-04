# Code Changes Reference: Security Fix Implementation

**File**: `C:\Work\LankaConnect\src\LankaConnect.API\Controllers\EventsController.cs`
**Changes**: 6 method signatures
**Time Estimate**: 30 minutes for code changes
**Risk**: LOW (simple refactoring)

---

## Change 1: CancelRsvp (Line 343-356)

### BEFORE (Vulnerable):
```csharp
/// <summary>
/// Cancel RSVP to an event (Authenticated users)
/// </summary>
[HttpDelete("{id:guid}/rsvp")]
[Authorize]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> CancelRsvp(Guid id, [FromQuery] Guid userId)
{
    Logger.LogInformation("User {UserId} cancelling RSVP to event {EventId}", userId, id);

    var command = new CancelRsvpCommand(id, userId);
    var result = await Mediator.Send(command);

    return HandleResult(result);
}
```

### AFTER (Secure):
```csharp
/// <summary>
/// Cancel RSVP to an event (Authenticated users)
/// </summary>
[HttpDelete("{id:guid}/rsvp")]
[Authorize]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> CancelRsvp(Guid id)
{
    var userId = User.GetUserId(); // Extract from authenticated JWT token
    Logger.LogInformation("User {UserId} cancelling RSVP to event {EventId}", userId, id);

    var command = new CancelRsvpCommand(id, userId);
    var result = await Mediator.Send(command);

    return HandleResult(result);
}
```

**Changes**:
- ✅ Removed `[FromQuery] Guid userId` parameter
- ✅ Added `var userId = User.GetUserId();` on first line of method body

---

## Change 2: GetMyRsvps (Line 382-395)

### BEFORE (Vulnerable):
```csharp
/// <summary>
/// Get user's RSVPs (Authenticated users)
/// </summary>
[HttpGet("my-rsvps")]
[Authorize]
[ProducesResponseType(typeof(IReadOnlyList<RsvpDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> GetMyRsvps([FromQuery] Guid userId)
{
    Logger.LogInformation("Getting RSVPs for user: {UserId}", userId);

    var query = new GetUserRsvpsQuery(userId);
    var result = await Mediator.Send(query);

    return HandleResult(result);
}
```

### AFTER (Secure):
```csharp
/// <summary>
/// Get user's RSVPs (Authenticated users)
/// </summary>
[HttpGet("my-rsvps")]
[Authorize]
[ProducesResponseType(typeof(IReadOnlyList<RsvpDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> GetMyRsvps()
{
    var userId = User.GetUserId(); // Extract from authenticated JWT token
    Logger.LogInformation("Getting RSVPs for user: {UserId}", userId);

    var query = new GetUserRsvpsQuery(userId);
    var result = await Mediator.Send(query);

    return HandleResult(result);
}
```

**Changes**:
- ✅ Removed `[FromQuery] Guid userId` parameter
- ✅ Added `var userId = User.GetUserId();` on first line of method body

---

## Change 3: GetUpcomingEvents (Line 398-413)

### BEFORE (Vulnerable):
```csharp
/// <summary>
/// Get upcoming events for user (Authenticated users)
/// </summary>
[HttpGet("upcoming")]
[Authorize]
[ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> GetUpcomingEvents([FromQuery] Guid userId)
{
    Logger.LogInformation("Getting upcoming events for user: {UserId}", userId);

    var query = new GetUpcomingEventsForUserQuery(userId);
    var result = await Mediator.Send(query);

    return HandleResult(result);
}
```

### AFTER (Secure):
```csharp
/// <summary>
/// Get upcoming events for user (Authenticated users)
/// </summary>
[HttpGet("upcoming")]
[Authorize]
[ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> GetUpcomingEvents()
{
    var userId = User.GetUserId(); // Extract from authenticated JWT token
    Logger.LogInformation("Getting upcoming events for user: {UserId}", userId);

    var query = new GetUpcomingEventsForUserQuery(userId);
    var result = await Mediator.Send(query);

    return HandleResult(result);
}
```

**Changes**:
- ✅ Removed `[FromQuery] Guid userId` parameter
- ✅ Added `var userId = User.GetUserId();` on first line of method body

---

## Change 4: AddToWaitingList (Line 673-689)

### BEFORE (Vulnerable):
```csharp
/// <summary>
/// Add user to event waiting list (Authenticated users)
/// </summary>
[HttpPost("{id:guid}/waiting-list")]
[Authorize]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
/// <summary>
/// Add user to event waiting list (Authenticated users)
/// </summary>
[HttpPost("{id:guid}/waiting-list")]
[Authorize]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> AddToWaitingList(Guid id)
{
    var userId = User.GetUserId(); // Extract from authenticated JWT token
    Logger.LogInformation("Adding user {UserId} to waiting list for event {EventId}", userId, id);

    var command = new AddToWaitingListCommand(id, userId);
    var result = await Mediator.Send(command);

    return HandleResult(result);
}
```

**Changes**:
- ✅ Removed `, [FromQuery] Guid userId` from parameter list
- ✅ Added `var userId = User.GetUserId();` on first line of method body

---

## Change 5: RemoveFromWaitingList (Line 691-707)

### BEFORE (Vulnerable):
```csharp
/// <summary>
/// Remove user from event waiting list (Authenticated users)
/// </summary>
[HttpDelete("{id:guid}/waiting-list")]
[Authorize]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> RemoveFromWaitingList(Guid id, [FromQuery] Guid userId)
{
    Logger.LogInformation("Removing user {UserId} from waiting list for event {EventId}", userId, id);

    var command = new RemoveFromWaitingListCommand(id, userId);
    var result = await Mediator.Send(command);

    return HandleResult(result);
}
```

### AFTER (Secure):
```csharp
/// <summary>
/// Remove user from event waiting list (Authenticated users)
/// </summary>
[HttpDelete("{id:guid}/waiting-list")]
[Authorize]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> RemoveFromWaitingList(Guid id)
{
    var userId = User.GetUserId(); // Extract from authenticated JWT token
    Logger.LogInformation("Removing user {UserId} from waiting list for event {EventId}", userId, id);

    var command = new RemoveFromWaitingListCommand(id, userId);
    var result = await Mediator.Send(command);

    return HandleResult(result);
}
```

**Changes**:
- ✅ Removed `, [FromQuery] Guid userId` from parameter list
- ✅ Added `var userId = User.GetUserId();` on first line of method body

---

## Change 6: PromoteFromWaitingList (Line 709-725)

### BEFORE (Vulnerable):
```csharp
/// <summary>
/// Promote user from waiting list to confirmed registration (Authenticated users)
/// </summary>
[HttpPost("{id:guid}/waiting-list/promote")]
[Authorize]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> PromoteFromWaitingList(Guid id, [FromQuery] Guid userId)
{
    Logger.LogInformation("Promoting user {UserId} from waiting list for event {EventId}", userId, id);

    var command = new PromoteFromWaitingListCommand(id, userId);
    var result = await Mediator.Send(command);

    return HandleResult(result);
}
```

### AFTER (Secure):
```csharp
/// <summary>
/// Promote user from waiting list to confirmed registration (Authenticated users)
/// </summary>
[HttpPost("{id:guid}/waiting-list/promote")]
[Authorize]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> PromoteFromWaitingList(Guid id)
{
    var userId = User.GetUserId(); // Extract from authenticated JWT token
    Logger.LogInformation("Promoting user {UserId} from waiting list for event {EventId}", userId, id);

    var command = new PromoteFromWaitingListCommand(id, userId);
    var result = await Mediator.Send(command);

    return HandleResult(result);
}
```

**Changes**:
- ✅ Removed `, [FromQuery] Guid userId` from parameter list
- ✅ Added `var userId = User.GetUserId();` on first line of method body

---

## Summary of Changes

### Pattern Applied to All 6 Methods:

**Step 1**: Remove parameter
```diff
- public async Task<IActionResult> MethodName(Guid id, [FromQuery] Guid userId)
+ public async Task<IActionResult> MethodName(Guid id)
```

**Step 2**: Add userId extraction
```diff
  public async Task<IActionResult> MethodName(Guid id)
  {
+     var userId = User.GetUserId(); // Extract from authenticated JWT token
      Logger.LogInformation("...", userId, id);
```

### Affected Lines (Exact)

| Method | Line Range | Changes |
|--------|------------|---------|
| CancelRsvp | 343-356 | Remove param (line 348), add extraction (after line 349) |
| GetMyRsvps | 382-395 | Remove param (line 387), add extraction (after line 388) |
| GetUpcomingEvents | 398-413 | Remove param (line 405), add extraction (after line 406) |
| AddToWaitingList | 673-689 | Remove param (line 681), add extraction (after line 682) |
| RemoveFromWaitingList | 691-707 | Remove param (line 699), add extraction (after line 700) |
| PromoteFromWaitingList | 709-725 | Remove param (line 717), add extraction (after line 718) |

---

## Verification Steps

### 1. Code Compilation
```bash
cd C:\Work\LankaConnect\src\LankaConnect.API
dotnet build
```
**Expected**: Build succeeds with 0 errors

### 2. Method Signature Verification
```bash
# Check that [FromQuery] Guid userId is removed
grep -n "\[FromQuery\] Guid userId" Controllers/EventsController.cs
```
**Expected**: No matches found

### 3. User.GetUserId() Usage
```bash
# Check that all methods extract userId from token
grep -A 2 "var userId = User.GetUserId()" Controllers/EventsController.cs | grep -c "var userId"
```
**Expected**: At least 6 matches (one for each fixed method)

### 4. Run Tests
```bash
cd C:\Work\LankaConnect
dotnet test tests/LankaConnect.API.Tests/LankaConnect.API.Tests.csproj --filter "FullyQualifiedName~EventsControllerSecurityTests"
```
**Expected**: All tests pass (10/10)

### 5. Swagger Verification
```bash
cd C:\Work\LankaConnect\src\LankaConnect.API
dotnet run
```
Navigate to: `https://localhost:5001/swagger`

**Expected**: All 7 endpoints visible:
- ✅ GET /api/Events/search
- ✅ POST /api/Events/{id}/waiting-list
- ✅ DELETE /api/Events/{id}/waiting-list
- ✅ POST /api/Events/{id}/waiting-list/promote
- ✅ GET /api/Events/{id}/waiting-list
- ✅ GET /api/Events/{id}/ics
- ✅ POST /api/Events/{id}/share

---

## Rollback Instructions (If Needed)

If issues occur after deployment, revert changes:

```bash
# Revert to previous commit
git revert HEAD

# Or restore from backup
git checkout HEAD~1 -- src/LankaConnect.API/Controllers/EventsController.cs

# Rebuild and redeploy
dotnet build
dotnet publish -c Release
```

---

## Integration with TDD Workflow

**Step 1: RED** - Write tests that expect secure pattern
- Tests verify userId comes from JWT token
- Tests fail with current vulnerable code

**Step 2: GREEN** - Apply code changes above
- Implement all 6 method changes
- Tests pass with secure pattern

**Step 3: REFACTOR** - Clean up
- Run `dotnet format`
- Verify all tests still pass
- Check code coverage ≥ 95%

---

## Additional Notes

### User.GetUserId() Extension Method

Located in: `C:\Work\LankaConnect\src\LankaConnect.API\Extensions\ClaimsPrincipalExtensions.cs`

```csharp
public static Guid GetUserId(this ClaimsPrincipal user)
{
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrWhiteSpace(userIdClaim))
    {
        throw new InvalidOperationException("User ID claim not found");
    }

    if (!Guid.TryParse(userIdClaim, out var userId))
    {
        throw new InvalidOperationException($"Invalid user ID format: {userIdClaim}");
    }

    return userId;
}
```

**Behavior**:
- Extracts `ClaimTypes.NameIdentifier` from JWT token
- Throws exception if claim not found (caught by [Authorize] middleware)
- Validates Guid format
- Returns authenticated user's ID

### Error Handling

**Unauthenticated Request**:
- `[Authorize]` attribute returns 401 before method executes
- `User.GetUserId()` never called

**Authenticated Request, Missing Claim**:
- `User.GetUserId()` throws `InvalidOperationException`
- Returns 500 with error message
- This should never happen with proper JWT token generation

**Authenticated Request, Valid Claim**:
- `User.GetUserId()` returns userId from token
- Method executes normally
- User can only access/modify their own data

---

## Testing Checklist

- [ ] Code compiles without errors
- [ ] No `[FromQuery] Guid userId` found in affected methods
- [ ] All methods call `User.GetUserId()`
- [ ] Unit tests pass (10/10)
- [ ] Integration tests pass
- [ ] Swagger shows all 7 endpoints
- [ ] Manual security test: Cannot manipulate userId
- [ ] Code review approved
- [ ] Ready for deployment

---

**Time to Implement**: 30 minutes (code changes only)
**Total Time with Testing**: 4-6 hours (following TDD guide)
**Risk**: LOW (simple, well-tested refactoring)
**Impact**: HIGH (fixes critical security vulnerability)
