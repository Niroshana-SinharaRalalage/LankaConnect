# Phase 6A.3: Backend Authorization - Complete ✅

**Date Completed**: 2025-11-11
**Status**: ✅ Complete
**Build Status**: Backend 0 errors
**Last Updated**: 2025-11-12

---

## Overview

Phase 6A.3 implements comprehensive policy-based authorization for the LankaConnect backend, ensuring users can only access features appropriate to their role and subscription status.

---

## Authorization Architecture

### Authorization Policies

**File**: [src/LankaConnect.API/Policies/AuthorizationPolicies.cs](../../src/LankaConnect.API/Policies/AuthorizationPolicies.cs)

**Implemented Policies**:

1. **CanCreateEvents** - Check if user can create events
   ```csharp
   services.AddAuthorizationBuilder()
       .AddPolicy("CanCreateEvents", policy =>
           policy.RequireAssertion(context =>
           {
               var user = context.User;
               if (!user.Identity?.IsAuthenticated ?? true) return false;

               var role = user.FindFirst("role")?.Value ?? "";
               if (!Enum.TryParse<UserRole>(role, out var userRole))
                   return false;

               // Only EventOrganizer and above can create events
               return userRole.CanCreateEvents();
           }));
   ```

2. **CanCreateBusinessProfile** - Check if user can create business profile
   ```csharp
   .AddPolicy("CanCreateBusinessProfile", policy =>
       policy.RequireAssertion(context =>
       {
           var role = context.User.FindFirst("role")?.Value ?? "";
           return Enum.TryParse<UserRole>(role, out var userRole) &&
                  userRole.CanCreateBusinessProfile();
       }));
   ```

3. **CanCreatePosts** - Check if user can create forum posts
   ```csharp
   .AddPolicy("CanCreatePosts", policy =>
       policy.RequireAssertion(context =>
       {
           var role = context.User.FindFirst("role")?.Value ?? "";
           return Enum.TryParse<UserRole>(role, out var userRole) &&
                  userRole.CanCreatePosts();
       }));
   ```

4. **CanManageUsers** - Check if user is admin
   ```csharp
   .AddPolicy("CanManageUsers", policy =>
       policy.RequireAssertion(context =>
       {
           var role = context.User.FindFirst("role")?.Value ?? "";
           return Enum.TryParse<UserRole>(role, out var userRole) &&
                  userRole.CanManageUsers();
       }));
   ```

5. **CanModerateContent** - Check if user is admin
   ```csharp
   .AddPolicy("CanModerateContent", policy =>
       policy.RequireAssertion(context =>
       {
           var role = context.User.FindFirst("role")?.Value ?? "";
           return Enum.TryParse<UserRole>(role, out var userRole) &&
                  userRole.CanModerateContent();
       }));
   ```

6. **IsEventOrganizer** - Check for exact EventOrganizer role
   ```csharp
   .AddPolicy("IsEventOrganizer", policy =>
       policy.RequireAssertion(context =>
       {
           var role = context.User.FindFirst("role")?.Value ?? "";
           return Enum.TryParse<UserRole>(role, out var userRole) &&
                  userRole.IsEventOrganizer();
       }));
   ```

### Policy Application

**In Controllers**: Use `[Authorize(Policy = "CanCreateEvents")]` attribute

```csharp
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    [Authorize(Policy = "CanCreateEvents")]
    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventCommand command)
    {
        // Only authorized users can reach this
        // ...
    }

    [Authorize(Policy = "CanManageUsers")]
    [HttpPost("approve/{userId}")]
    public async Task<IActionResult> ApproveUser(Guid userId)
    {
        // Only admins can reach this
        // ...
    }
}
```

---

## Subscription Status Authorization

### Event Creation with Subscription Check

**File**: [src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommandHandler.cs](../../src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommandHandler.cs)

**Authorization Flow**:
1. Check role has `CanCreateEvents()` permission
2. Check subscription status is Active or Trialing
3. Reject if:
   - Role doesn't support event creation
   - SubscriptionStatus is Expired, PastDue, Canceled, or None

```csharp
public class CreateEventCommandHandler : ICommandHandler<CreateEventCommand>
{
    public async Task<Result> Handle(CreateEventCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        // Check role permission
        if (!user.Role.CanCreateEvents())
            return Result.Failure("User role cannot create events");

        // Check subscription status
        if (!user.SubscriptionStatus.IsActive())
            return Result.Failure("Subscription required to create events");

        // Proceed with event creation
        // ...
    }
}
```

---

## Claims-Based Authorization

### JWT Token Claims

**Created during login**: [AuthController.cs](../../src/LankaConnect.API/Controllers/AuthController.cs)

```csharp
var claims = new[]
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim(ClaimTypes.Email, user.Email),
    new Claim(ClaimTypes.Name, user.FullName),
    new Claim("role", user.Role.ToString()),
    new Claim("subscriptionStatus", user.SubscriptionStatus.ToString()),
    new Claim("freeTrialEndsAt", user.FreeTrialEndsAt?.ToString("O") ?? ""),
};
```

### Authorization Filters

**Custom Authorization Filter**: [Services/AuthorizationService.cs](../../src/LankaConnect.Application/Common/Services/AuthorizationService.cs)

```csharp
public interface IAuthorizationService
{
    Task<bool> CanCreateEventsAsync(Guid userId);
    Task<bool> CanCreateBusinessProfileAsync(Guid userId);
    Task<bool> CanCreatePostsAsync(Guid userId);
    Task<bool> CanManageUsersAsync(Guid userId);
}

public class AuthorizationService : IAuthorizationService
{
    private readonly IUserRepository _userRepository;

    public async Task<bool> CanCreateEventsAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        return user?.Role.CanCreateEvents() ?? false &&
               user?.SubscriptionStatus.IsActive() ?? false;
    }

    // ... other methods
}
```

---

## Role-Based Access Control

### Complete Authorization Matrix

| Feature | GeneralUser | EventOrganizer | BusinessOwner | Admin | AdminManager |
|---------|:----------:|:-------------:|:-------------|:----:|:-----------:|
| Browse Events | ✅ | ✅ | ✅ | ✅ | ✅ |
| Register for Event | ✅ | ✅ | ✅ | ✅ | ✅ |
| Create Event | ❌ | ✅* | ❌ | ✅ | ✅ |
| Create Post | ❌ | ✅* | ❌ | ✅ | ✅ |
| Create Business | ❌ | ❌ | ✅* | ✅ | ✅ |
| Approve Users | ❌ | ❌ | ❌ | ✅ | ✅ |
| Manage Admins | ❌ | ❌ | ❌ | ❌ | ✅ |

*Requires active subscription

---

## Middleware Authorization

### Authorization Middleware Setup

**File**: [src/LankaConnect.API/Program.cs](../../src/LankaConnect.API/Program.cs)

```csharp
// Add authorization services
services.AddAuthorization();

// Add authentication scheme (JWT Bearer)
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

// Register authorization policies
app.AddAuthorizationPolicies();

// Add middleware in pipeline
app.UseAuthentication();
app.UseAuthorization();
```

---

## API Endpoint Authorization

### Event Endpoints

```csharp
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    [Authorize(Policy = "CanCreateEvents")]
    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventCommand command)
    // Only EventOrganizer and above can create

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEvent(Guid id)
    // Anyone can view event details

    [Authorize]
    [HttpPost("{id}/register")]
    public async Task<IActionResult> RegisterForEvent(Guid id)
    // Any authenticated user can register
}
```

### Admin Endpoints

```csharp
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    [Authorize(Policy = "CanManageUsers")]
    [HttpPost("users/{userId}/approve")]
    public async Task<IActionResult> ApproveUser(Guid userId)
    // Only admin and admin manager

    [Authorize(Policy = "CanModerateContent")]
    [HttpPost("content/{contentId}/remove")]
    public async Task<IActionResult> RemoveContent(Guid contentId)
    // Only admin and admin manager
}
```

---

## Error Handling

### Unauthorized Response

When user lacks required policy:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "The user does not have permission to access this resource."
}
```

### Unauthenticated Response

When no token provided or invalid:
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Authorization required."
}
```

---

## Security Best Practices

1. **Role Enum Validation**: Parse role claims safely with TryParse
2. **Subscription Status Check**: Always verify subscription before feature access
3. **User Context Isolation**: `ICurrentUserService` ensures users access only their own data
4. **Fail-Safe Defaults**: Deny access by default unless explicitly authorized
5. **Audit Logging**: Log all authorization decisions (implementation in Phase 2)
6. **Token Expiration**: JWT tokens expire after 24 hours
7. **HTTPS Only**: Tokens transmitted only over HTTPS

---

## Testing Performed

1. **Backend Build**: Successful with 0 errors
2. **Policy Application**: Verified policies block unauthorized access
3. **Subscription Enforcement**: Event creation blocked for expired subscriptions
4. **Role Validation**: All 6 roles properly evaluated
5. **Token Parsing**: Claims extracted correctly from JWT

---

## Integration Points

1. **With Phase 6A.0 (Roles)**: Authorization uses UserRole enum
2. **With Phase 6A.1 (Subscription)**: Authorization checks subscription status
3. **With Phase 6A.2 (Dashboard)**: Frontend respects same rules
4. **With Phase 6A.5 (Approvals)**: Admin endpoints protected by policies

---

## Future Enhancements

### Phase 2 (Recommended)
- [ ] **Audit Logging** - Track all authorization decisions
- [ ] **Rate Limiting** - Prevent abuse of authorized endpoints
- [ ] **Dynamic Policies** - Load policies from database
- [ ] **Resource-Level Authorization** - Owner-based access control
- [ ] **Claim Validation** - Additional claim-based checks

### Phase 3 (Advanced)
- [ ] **OAuth2** - External identity providers
- [ ] **SAML** - Enterprise authentication
- [ ] **Permission Management** - Admin UI for roles and permissions
- [ ] **API Keys** - Service-to-service authorization

---

## Build Status

**Backend Build**: ✅ **0 errors** (47.44s compile time)
- Authorization middleware verified
- Policy definitions compiled
- No breaking changes

---

## Related Documentation

- See [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md) for complete phase registry
- See [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md) for overall project status
