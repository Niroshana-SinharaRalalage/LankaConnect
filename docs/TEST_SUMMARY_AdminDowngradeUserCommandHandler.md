# Test Summary: AdminDowngradeUserCommandHandler

## Overview
**Phase:** 6A.106
**Date Created:** 2026-02-12
**Test File:** `tests/LankaConnect.Application.Tests/Users/Commands/AdminDowngradeUserCommandHandlerTests.cs`
**Total Lines:** 522
**Total Tests:** 15
**Test Framework:** xUnit with FluentAssertions and Moq
**TDD Status:** RED (Tests written first - handler not yet implemented)

## Test Coverage Summary

### 1. Happy Path Tests (3 tests)
- ✅ **AdminManagerDowngradesAdmin** - AdminManager successfully downgrades Admin to GeneralUser
- ✅ **AdminDowngradesEventOrganizer** - Admin successfully downgrades EventOrganizer to GeneralUser
- ✅ **AdminDowngradesBusinessOwner** - Admin successfully downgrades BusinessOwner to GeneralUser

### 2. Role Hierarchy Protection Tests (2 tests)
- ✅ **AdminTriesToDowngradeAdmin** - Fails with "Cannot perform actions on users with equal or higher role"
- ✅ **AnyoneTriesToDowngradeAdminManager** - Fails when trying to downgrade AdminManager (highest role)

### 3. Self-Prevention Tests (1 test)
- ✅ **AdminTriesToDowngradeSelf** - Fails with "Cannot downgrade your own account"

### 4. Domain Validation Tests (1 test)
- ✅ **UserAlreadyGeneralUser** - Fails with "User is already a General User"

### 5. User Not Found Tests (2 tests)
- ✅ **TargetUserNotFound** - Returns "User not found" when target doesn't exist
- ✅ **AdminUserNotFound** - Returns "Admin user not found" when admin doesn't exist

### 6. Permission Tests (1 test)
- ✅ **NonAdminTriesDowngrade** - Fails with "Insufficient permissions" for non-admin users

### 7. Audit Log Tests (1 test)
- ✅ **SuccessfulDowngrade_ShouldCreateAuditLog** - Verifies audit log created with correct action, IP, UserAgent

### 8. Domain Event Tests (1 test)
- ✅ **SuccessfulDowngrade_ShouldRaiseUserRoleChangedEvent** - Verifies UserRoleChangedEvent raised with correct old/new roles

### 9. Pending Upgrade Cleanup Tests (1 test)
- ✅ **UserWithPendingUpgrade_ShouldClearPendingUpgrade** - Verifies PendingUpgradeRole and UpgradeRequestedAt cleared on downgrade

### 10. Repository Commit Tests (1 test)
- ✅ **SuccessfulDowngrade_ShouldCommitChanges** - Verifies UnitOfWork.CommitAsync called

### 11. Cancellation Token Tests (1 test)
- ✅ **WithCancellationRequested_ShouldThrowOperationCanceledException** - Honors cancellation token

## Test Architecture

### Mocked Dependencies
```csharp
Mock<IUserRepository>              // GetByIdAsync for admin + target user
Mock<IAdminAuditLogRepository>     // AddAsync for audit logging
Mock<ICurrentUserService>          // UserId property
Mock<IUnitOfWork>                  // CommitAsync transaction
NullLogger<Handler>                // Logging without setup
```

### Test Helper Methods
- `CreateUser(Guid userId, UserRole role, string? email)` - Creates test User entities with reflection to set Id and Role

### Pattern Followed
All tests follow the **Arrange-Act-Assert** pattern:
1. **Arrange**: Setup mocks, create test data
2. **Act**: Call `_handler.Handle(command, cancellationToken)`
3. **Assert**: Verify result + mock interactions using FluentAssertions

## Key Design Decisions

### 1. Domain Method Testing
Tests verify the domain method `targetUser.DowngradeToGeneralUserByAdmin()` is called, which encapsulates business rules:
- Cannot downgrade AdminManager
- Cannot downgrade if already GeneralUser
- Clears pending upgrade requests
- Raises UserRoleChangedEvent

### 2. Audit Trail Verification
All successful operations verify:
- Audit log created with `AdminAuditActions.UserRoleDowngraded`
- IP address and User Agent captured
- Target user details included in audit details JSON

### 3. Security Testing
- Role hierarchy protection (Admin cannot touch AdminManager or other Admins)
- Self-prevention (cannot downgrade own account)
- Permission checks (only Admin/AdminManager can downgrade)

### 4. Data Integrity
- Pending upgrade requests are cleared on downgrade
- Domain events raised for event sourcing/notifications
- Transaction committed via UnitOfWork

## Expected Implementation Pattern

Based on `AdminDeactivateUserCommandHandler`, the handler should:

```csharp
public class AdminDowngradeUserCommandHandler : ICommandHandler<AdminDowngradeUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminDowngradeUserCommandHandler> _logger;

    public async Task<Result> Handle(AdminDowngradeUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Get admin user
        // 2. Validate admin role (Admin or AdminManager)
        // 3. Get target user
        // 4. Self-prevention check
        // 5. Role hierarchy check
        // 6. Call targetUser.DowngradeToGeneralUserByAdmin()
        // 7. Create audit log
        // 8. Commit transaction
        // 9. Return Result.Success()
    }
}
```

## Next Steps (TDD Cycle)

### RED Phase ✅ (Current)
- Tests written and will fail (handler doesn't exist yet)

### GREEN Phase (Next)
1. Create `AdminDowngradeUserCommand.cs` (command model)
2. Create `AdminDowngradeUserCommandHandler.cs` (handler implementation)
3. Run tests - should turn GREEN
4. Verify 100% test pass rate

### REFACTOR Phase (After Green)
1. Review code for duplication
2. Extract common patterns
3. Improve logging messages
4. Keep tests green during refactoring

## Test Execution

```bash
# Run only AdminDowngradeUserCommandHandler tests
dotnet test --filter "FullyQualifiedName~AdminDowngradeUserCommandHandlerTests"

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportsDirectory=./coverage
```

## Coverage Goals
- **Target:** 100% line coverage
- **Target:** 100% branch coverage
- **All edge cases covered:** Yes
- **All failure paths tested:** Yes
- **All success paths tested:** Yes

## Dependencies on Domain

### Domain Method (Already Implemented)
```csharp
// User.cs line 924
public Result DowngradeToGeneralUserByAdmin()
{
    if (Role == UserRole.AdminManager)
        return Result.Failure("AdminManager role cannot be downgraded");

    if (Role == UserRole.GeneralUser)
        return Result.Failure("User is already a General User");

    var oldRole = Role;
    Role = UserRole.GeneralUser;

    // Clear pending upgrade if any
    if (PendingUpgradeRole.HasValue)
    {
        PendingUpgradeRole = null;
        UpgradeRequestedAt = null;
    }

    MarkAsUpdated();
    RaiseDomainEvent(new UserRoleChangedEvent(Id, Email.Value, oldRole, UserRole.GeneralUser));

    return Result.Success();
}
```

### Domain Event (Already Implemented)
```csharp
// UserRoleChangedEvent.cs
public record UserRoleChangedEvent(Guid UserId, string Email, UserRole OldRole, UserRole NewRole) : DomainEvent;
```

### Audit Action (Already Implemented)
```csharp
// AdminAuditLog.cs line 171
public const string UserRoleDowngraded = "USER_ROLE_DOWNGRADED";
```

## File Location
**Full Path:** `C:\Work\LankaConnect\tests\LankaConnect.Application.Tests\Users\Commands\AdminDowngradeUserCommandHandlerTests.cs`

## Status: READY FOR IMPLEMENTATION
All tests are written. Next step: Implement handler to make tests pass (GREEN phase).
