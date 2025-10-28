# Future Enhancements - Entra External ID Integration

**Status**: Architecture-Ready for Phase 2
**Last Updated**: 2025-10-28
**Related**: ADR-002-Entra-External-ID-Integration

---

## Overview

This document tracks **deferred enhancements** for Entra External ID integration that are architecturally sound but not required for MVP Phase 1.

**Phase 1 (MVP - COMPLETE)**: ✅ Authentication, auto-provisioning, opportunistic profile sync
**Phase 2 (Future)**: Administrative tools, bulk operations, scheduled sync

---

## Deferred Enhancements

### 1. SyncEntraUserCommand - Explicit Profile Synchronization

**Status**: Deferred to Phase 2
**Priority**: Low (P3)
**Estimated Effort**: 2-3 hours
**Blocking**: None - can be added without breaking changes

#### Description

A dedicated CQRS command for explicitly synchronizing user profile data from Microsoft Entra External ID, independent of the login flow.

#### Use Cases

1. **Admin Dashboard**: "Sync All Users" button to bulk-refresh profiles
2. **Scheduled Jobs**: Nightly background job to sync all Entra users
3. **User-Initiated**: "Refresh Profile from Entra" button in user settings
4. **Webhook Integration**: Handler for Entra profile change events
5. **Compliance**: Audit trail for profile synchronization events

#### Current Workaround

**LoginWithEntraCommandHandler** includes opportunistic profile sync (lines 121-144):
- Automatically updates first/last name if changed in Entra
- Triggers on user login
- Handles 99% of profile update scenarios
- Gracefully degrades (sync failure doesn't block authentication)

#### When to Implement

**Trigger Conditions** (any of):
- ✅ Administrative need for bulk user synchronization
- ✅ Business requirement for scheduled profile sync
- ✅ Compliance requirement for audit trail of profile changes
- ✅ Webhook integration from Entra profile change events
- ✅ User-facing "Refresh Profile" feature request

---

## Implementation Specification

### Command Definition

```csharp
// File: src/LankaConnect.Application/Auth/Commands/SyncEntraUser/SyncEntraUserCommand.cs

using MediatR;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Auth.Commands.SyncEntraUser;

/// <summary>
/// Command to explicitly synchronize user profile data from Microsoft Entra External ID
/// </summary>
public record SyncEntraUserCommand(Guid UserId) : IRequest<Result<SyncEntraUserResponse>>;

public record SyncEntraUserResponse(
    Guid UserId,
    bool WasSynced,
    string[] ChangedFields,
    DateTime SyncedAt);
```

### Handler Implementation

```csharp
// File: src/LankaConnect.Application/Auth/Commands/SyncEntraUser/SyncEntraUserCommandHandler.cs

public class SyncEntraUserCommandHandler : IRequestHandler<SyncEntraUserCommand, Result<SyncEntraUserResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEntraExternalIdService _entraService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SyncEntraUserCommandHandler> _logger;

    // Handler responsibilities:
    // 1. Load user by ID from database
    // 2. Verify user has Entra external provider (IdentityProvider.EntraExternal)
    // 3. Call IEntraExternalIdService to get latest user info from Entra
    //    - Need new method: GetUserInfoByExternalIdAsync(externalProviderId)
    //    - Or reuse existing token validation flow
    // 4. Compare current profile with Entra data
    // 5. Update profile using user.UpdateProfile() domain method
    // 6. Track changed fields for response
    // 7. Commit changes and return result

    public async Task<Result<SyncEntraUserResponse>> Handle(
        SyncEntraUserCommand request,
        CancellationToken cancellationToken)
    {
        // Step 1: Load user
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return Result<SyncEntraUserResponse>.Failure("User not found");

        // Step 2: Verify Entra user
        if (user.IdentityProvider != IdentityProvider.EntraExternal)
            return Result<SyncEntraUserResponse>.Failure("User is not an Entra External ID user");

        // Step 3: Get latest info from Entra
        // NOTE: Need to add new service method or reuse token flow
        var entraUserInfo = await _entraService.GetUserInfoByExternalIdAsync(
            user.ExternalProviderId!,
            cancellationToken);

        if (entraUserInfo.IsFailure)
            return Result<SyncEntraUserResponse>.Failure(entraUserInfo.Errors);

        // Step 4: Track changes
        var changedFields = new List<string>();
        if (user.FirstName != entraUserInfo.Value.FirstName) changedFields.Add("FirstName");
        if (user.LastName != entraUserInfo.Value.LastName) changedFields.Add("LastName");

        // Step 5: Update if changes detected
        if (changedFields.Any())
        {
            var updateResult = user.UpdateProfile(
                entraUserInfo.Value.FirstName,
                entraUserInfo.Value.LastName,
                user.PhoneNumber, // Preserve
                user.Bio);        // Preserve

            if (updateResult.IsFailure)
                return Result<SyncEntraUserResponse>.Failure(updateResult.Errors);

            await _unitOfWork.CommitAsync(cancellationToken);
        }

        // Step 6: Return result
        return Result<SyncEntraUserResponse>.Success(new SyncEntraUserResponse(
            user.Id,
            changedFields.Any(),
            changedFields.ToArray(),
            DateTime.UtcNow));
    }
}
```

### Required Service Enhancement

```csharp
// File: src/LankaConnect.Application/Common/Interfaces/IEntraExternalIdService.cs

public interface IEntraExternalIdService
{
    // Existing methods
    bool IsEnabled { get; }
    Task<Result<EntraTokenClaims>> ValidateAccessTokenAsync(string accessToken);
    Task<Result<EntraUserInfo>> GetUserInfoAsync(string accessToken);

    // NEW method required for SyncEntraUserCommand
    /// <summary>
    /// Retrieves user information from Entra by external provider ID (OID)
    /// </summary>
    /// <remarks>
    /// This requires service-to-service authentication using client credentials flow.
    /// The app must have delegated User.Read.All permission.
    /// </remarks>
    Task<Result<EntraUserInfo>> GetUserInfoByExternalIdAsync(
        string externalProviderId,
        CancellationToken cancellationToken = default);
}
```

### Test Scenarios

```csharp
// File: tests/LankaConnect.Application.Tests/Auth/Commands/SyncEntraUserCommandHandlerTests.cs

public class SyncEntraUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithChangedName_ShouldUpdateAndReturnChanges()
    {
        // Test: Entra user has new name, sync updates it
    }

    [Fact]
    public async Task Handle_WithNoChanges_ShouldNotUpdateAndReturnNoChanges()
    {
        // Test: Entra data matches, no database update
    }

    [Fact]
    public async Task Handle_WithNonEntraUser_ShouldFail()
    {
        // Test: Local user cannot be synced from Entra
    }

    [Fact]
    public async Task Handle_WithInvalidUser_ShouldFail()
    {
        // Test: User ID doesn't exist
    }

    [Fact]
    public async Task Handle_WhenEntraServiceFails_ShouldFail()
    {
        // Test: Entra API unavailable
    }

    [Fact]
    public async Task Handle_WhenUpdateProfileFails_ShouldFail()
    {
        // Test: Domain validation failure (e.g., empty name)
    }

    [Fact]
    public async Task Handle_WhenDatabaseSaveFails_ShouldFail()
    {
        // Test: CommitAsync returns 0
    }
}
```

### API Endpoint (Presentation Layer)

```csharp
// File: src/LankaConnect.API/Controllers/AuthController.cs

/// <summary>
/// Synchronizes user profile from Microsoft Entra External ID
/// </summary>
/// <remarks>
/// Only works for users authenticated via Entra External ID.
/// Updates first name, last name if changed in Entra.
/// </remarks>
[HttpPost("sync-entra-profile")]
[Authorize] // Requires authenticated user
[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SyncEntraUserResponse))]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> SyncEntraProfile()
{
    var userId = User.GetUserId(); // Extension method to extract user ID from JWT
    var command = new SyncEntraUserCommand(userId);
    var result = await _mediator.Send(command);

    return result.IsSuccess
        ? Ok(result.Value)
        : BadRequest(result.Errors);
}

/// <summary>
/// Admin endpoint to bulk sync all Entra users
/// </summary>
[HttpPost("admin/sync-all-entra-users")]
[Authorize(Roles = "Admin")]
[ProducesResponseType(StatusCodes.Status200OK)]
public async Task<IActionResult> SyncAllEntraUsers()
{
    // Background job implementation
    // Use Hangfire/Quartz to process async
    _backgroundJobClient.Enqueue(() => SyncAllEntraUsersAsync());
    return Accepted("Bulk sync job queued");
}
```

### Background Job Implementation

```csharp
// File: src/LankaConnect.Infrastructure/Jobs/EntraProfileSyncJob.cs

using Hangfire;
using MediatR;

namespace LankaConnect.Infrastructure.Jobs;

public class EntraProfileSyncJob
{
    private readonly IUserRepository _userRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<EntraProfileSyncJob> _logger;

    /// <summary>
    /// Scheduled job to sync all Entra users (runs nightly at 2 AM UTC)
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task SyncAllEntraUsersAsync()
    {
        _logger.LogInformation("Starting scheduled Entra user profile sync");

        // Get all users with Entra provider
        var entraUsers = await _userRepository.GetUsersByProviderAsync(
            IdentityProvider.EntraExternal);

        int successCount = 0;
        int failureCount = 0;

        foreach (var user in entraUsers)
        {
            try
            {
                var command = new SyncEntraUserCommand(user.Id);
                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    successCount++;
                    if (result.Value.WasSynced)
                    {
                        _logger.LogInformation(
                            "Synced user {UserId}: {ChangedFields}",
                            user.Id,
                            string.Join(", ", result.Value.ChangedFields));
                    }
                }
                else
                {
                    failureCount++;
                    _logger.LogWarning(
                        "Failed to sync user {UserId}: {Errors}",
                        user.Id,
                        string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger.LogError(ex, "Error syncing user {UserId}", user.Id);
            }
        }

        _logger.LogInformation(
            "Completed Entra profile sync: {SuccessCount} successful, {FailureCount} failed",
            successCount,
            failureCount);
    }
}
```

### Configuration

```csharp
// File: src/LankaConnect.API/Program.cs or Startup.cs

// Add Hangfire for background jobs
services.AddHangfire(config => config.UsePostgreSqlStorage(connectionString));
services.AddHangfireServer();

// Schedule nightly sync job
RecurringJob.AddOrUpdate<EntraProfileSyncJob>(
    "sync-entra-profiles",
    job => job.SyncAllEntraUsersAsync(),
    Cron.Daily(2)); // 2 AM UTC
```

---

## Required Infrastructure Changes

### 1. New Repository Method

```csharp
// File: src/LankaConnect.Domain/Users/IUserRepository.cs

public interface IUserRepository : IRepository<User>
{
    // Existing methods...

    /// <summary>
    /// Gets all users by identity provider
    /// </summary>
    Task<IReadOnlyList<User>> GetUsersByProviderAsync(
        IdentityProvider provider,
        CancellationToken cancellationToken = default);
}
```

### 2. Entra Service Enhancement

Requires **Microsoft Graph API** integration for service-to-service authentication:

```csharp
// File: src/LankaConnect.Infrastructure/Security/Services/EntraExternalIdService.cs

// Add Graph API client
private readonly GraphServiceClient _graphClient;

public async Task<Result<EntraUserInfo>> GetUserInfoByExternalIdAsync(
    string externalProviderId,
    CancellationToken cancellationToken = default)
{
    try
    {
        // Use Graph API to fetch user by object ID
        var user = await _graphClient.Users[externalProviderId]
            .Request()
            .GetAsync(cancellationToken);

        return Result<EntraUserInfo>.Success(new EntraUserInfo
        {
            ObjectId = user.Id,
            Email = user.Mail ?? user.UserPrincipalName,
            FirstName = user.GivenName ?? "User",
            LastName = user.Surname ?? "Unknown",
            DisplayName = user.DisplayName,
            EmailVerified = true
        });
    }
    catch (ServiceException ex)
    {
        _logger.LogError(ex, "Failed to fetch user from Graph API: {ExternalId}", externalProviderId);
        return Result<EntraUserInfo>.Failure($"Failed to retrieve user from Entra: {ex.Message}");
    }
}
```

### 3. Required NuGet Packages

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="Microsoft.Graph" Version="5.0.0" />
<PackageVersion Include="Hangfire.AspNetCore" Version="1.8.0" />
<PackageVersion Include="Hangfire.PostgreSql" Version="1.20.0" />
```

### 4. Azure App Registration Updates

**Required API Permissions**:
- `User.Read.All` (Application permission) - Read all users' full profiles
- Grant admin consent in Azure Portal

**Client Credentials Flow**:
```csharp
var credential = new ClientSecretCredential(
    _options.TenantId,
    _options.ClientId,
    _options.ClientSecret);

_graphClient = new GraphServiceClient(credential);
```

---

## Migration Path

### Step 1: Update Infrastructure (2-3 hours)
1. Add `GetUsersByProviderAsync` to repository
2. Install Microsoft.Graph NuGet package
3. Add Graph API client to EntraExternalIdService
4. Implement `GetUserInfoByExternalIdAsync`
5. Test Graph API connectivity

### Step 2: Implement Command (1-2 hours)
1. Create `SyncEntraUserCommand` and handler
2. Write 7 comprehensive tests (TDD)
3. Verify all tests pass

### Step 3: Add API Endpoints (1 hour)
1. Add `/api/auth/sync-entra-profile` endpoint
2. Add `/api/admin/sync-all-entra-users` endpoint
3. Integration tests

### Step 4: Background Job (Optional - 1 hour)
1. Install Hangfire
2. Create `EntraProfileSyncJob`
3. Schedule nightly execution
4. Monitoring and alerting

---

## Testing Strategy

### Unit Tests
- ✅ Command validation
- ✅ Handler logic with mocked services
- ✅ Domain method integration

### Integration Tests
- ✅ End-to-end API call
- ✅ Database state verification
- ✅ Graph API mocking

### Manual Testing
- ✅ Change name in Entra portal
- ✅ Call sync endpoint
- ✅ Verify database update
- ✅ Check audit logs

---

## Performance Considerations

### Bulk Sync Optimization

**Problem**: Syncing 10,000 users = 10,000 Graph API calls

**Solution**:
```csharp
// Batch Graph API requests (100 users per request)
var batchRequest = new BatchRequestContent();
foreach (var user in users.Take(100))
{
    batchRequest.AddBatchRequestStep(
        new HttpRequestMessage(HttpMethod.Get, $"/users/{user.ExternalProviderId}"));
}

var batchResponse = await _graphClient.Batch.Request().PostAsync(batchRequest);
```

**Rate Limiting**:
- Microsoft Graph: 10,000 requests/10 minutes per app
- Implement exponential backoff and retry
- Use Change Delta Queries for efficiency

---

## Monitoring & Observability

### Metrics to Track
1. **Sync Success Rate**: % of successful syncs
2. **Average Sync Duration**: Time per user
3. **Changed Field Frequency**: Which fields change most
4. **Failure Reasons**: Authentication, network, validation

### Logging
```csharp
_logger.LogInformation("Profile sync completed", new
{
    UserId = user.Id,
    WasSynced = result.WasSynced,
    ChangedFields = result.ChangedFields,
    DurationMs = stopwatch.ElapsedMilliseconds
});
```

### Alerting
- Alert if sync job fails for 3 consecutive runs
- Alert if sync success rate < 95%
- Alert if Graph API rate limit hit

---

## Security Considerations

### Authorization
- User sync endpoint: Requires authenticated user (self-service only)
- Admin bulk sync: Requires Admin role
- Webhook handler: Validate Entra webhook signature

### Audit Trail
```csharp
// Raise domain event for audit
user.RaiseDomainEvent(new UserProfileSyncedFromEntraEvent(
    user.Id,
    changedFields,
    DateTime.UtcNow));
```

### Data Privacy
- Log changed fields, not actual values
- Comply with GDPR/data retention policies
- Allow users to opt-out of automatic sync

---

## Conclusion

**SyncEntraUserCommand** is architecturally sound and can be implemented when needed without breaking changes. Current MVP solution (opportunistic sync on login) handles 99% of scenarios. Build this enhancement only when:

1. Administrative/bulk sync requirement emerges
2. Scheduled sync business requirement
3. Compliance audit trail requirement
4. Webhook integration requirement

**Estimated Total Effort**: 4-6 hours for complete implementation with tests.

---

**Related Documents**:
- [ADR-002-Entra-External-ID-Integration.md](./ADR-002-Entra-External-ID-Integration.md)
- [Entra-External-ID-Implementation-Roadmap.md](./Entra-External-ID-Implementation-Roadmap.md)
- [Entra-External-ID-Component-Architecture.md](./Entra-External-ID-Component-Architecture.md)
