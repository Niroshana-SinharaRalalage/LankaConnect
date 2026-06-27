using LankaConnect.Modules.Identity.Contracts; // W4.6.a: ICurrentUserService moved here
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Application.Tests.TestHelpers;
using LankaConnect.Modules.Identity.Application.Commands.Users.AdminDowngradeUser;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Domain.Support;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Modules.Identity.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Email = LankaConnect.Domain.Shared.ValueObjects.Email;

namespace LankaConnect.Modules.Identity.Application.Tests.Commands.Users;

/// <summary>
/// Phase 6A.106: TDD tests for AdminDowngradeUserCommandHandler
/// Tests written FIRST following Red-Green-Refactor cycle.
/// </summary>
public class AdminDowngradeUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IAdminAuditLogRepository> _auditLogRepository;
    private readonly Mock<ICurrentUserService> _currentUserService;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IEventRepository> _eventRepository;
    // TODO Phase 6A.106 Hotfix: Re-add when IBillingRepository implementation exists
    // private readonly Mock<IBillingRepository> _billingRepository;
    private readonly AdminDowngradeUserCommandHandler _handler;

    public AdminDowngradeUserCommandHandlerTests()
    {
        _userRepository = LankaConnect.Application.Tests.TestHelpers.MockRepository.CreateUserRepository();
        _auditLogRepository = new Mock<IAdminAuditLogRepository>();
        _currentUserService = new Mock<ICurrentUserService>();
        _unitOfWork = LankaConnect.Application.Tests.TestHelpers.MockRepository.CreateUnitOfWork();
        _eventRepository = new Mock<IEventRepository>();
        // TODO Phase 6A.106 Hotfix: Re-add when IBillingRepository implementation exists
        // _billingRepository = new Mock<IBillingRepository>();

        _handler = new AdminDowngradeUserCommandHandler(
            _userRepository.Object,
            _auditLogRepository.Object,
            _currentUserService.Object,
            _unitOfWork.Object,
            _eventRepository.Object,
            // TODO Phase 6A.106 Hotfix: Re-add when IBillingRepository implementation exists
            // _billingRepository.Object,
            NullLogger<AdminDowngradeUserCommandHandler>.Instance);
    }

    #region Happy Path Tests

    [Fact]
    public async Task Handle_AdminManagerDowngradesAdmin_ShouldSucceed()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var adminUser = CreateUser(userId: adminUserId, role: UserRole.AdminManager);
        var targetUser = CreateUser(userId: targetUserId, role: UserRole.Admin);

        _currentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _userRepository.Setup(x => x.GetByIdAsync(adminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _userRepository.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var command = new AdminDowngradeUserCommand(targetUserId, "Testing downgrade", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        targetUser.Role.Should().Be(UserRole.GeneralUser);
        _auditLogRepository.Verify(x => x.AddAsync(It.IsAny<AdminAuditLog>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AdminDowngradesEventOrganizer_ShouldSucceed()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var adminUser = CreateUser(userId: adminUserId, role: UserRole.Admin);
        var targetUser = CreateUser(userId: targetUserId, role: UserRole.EventOrganizer);

        _currentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _userRepository.Setup(x => x.GetByIdAsync(adminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _userRepository.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var command = new AdminDowngradeUserCommand(targetUserId, "Policy violation", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        targetUser.Role.Should().Be(UserRole.GeneralUser);
        _auditLogRepository.Verify(x => x.AddAsync(
            It.Is<AdminAuditLog>(log => log.Action == AdminAuditActions.UserRoleDowngraded),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AdminDowngradesBusinessOwner_ShouldSucceed()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var adminUser = CreateUser(userId: adminUserId, role: UserRole.Admin);
        var targetUser = CreateUser(userId: targetUserId, role: UserRole.BusinessOwner);

        _currentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _userRepository.Setup(x => x.GetByIdAsync(adminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _userRepository.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var command = new AdminDowngradeUserCommand(targetUserId, "Business closed", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        targetUser.Role.Should().Be(UserRole.GeneralUser);
    }

    #endregion

    #region Role Hierarchy Protection Tests

    [Fact]
    public async Task Handle_AdminTriesToDowngradeAdmin_ShouldFail()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var adminUser = CreateUser(userId: adminUserId, role: UserRole.Admin);
        var targetUser = CreateUser(userId: targetUserId, role: UserRole.Admin);

        _currentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _userRepository.Setup(x => x.GetByIdAsync(adminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _userRepository.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var command = new AdminDowngradeUserCommand(targetUserId, "Testing", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot perform actions on users with equal or higher role");
        _auditLogRepository.Verify(x => x.AddAsync(It.IsAny<AdminAuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AnyoneTriesToDowngradeAdminManager_ShouldFail()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var adminUser = CreateUser(userId: adminUserId, role: UserRole.Admin);
        var targetUser = CreateUser(userId: targetUserId, role: UserRole.AdminManager);

        _currentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _userRepository.Setup(x => x.GetByIdAsync(adminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _userRepository.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var command = new AdminDowngradeUserCommand(targetUserId, "Testing", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot perform actions on users with equal or higher role");
    }

    #endregion

    #region Self-Prevention Tests

    [Fact]
    public async Task Handle_AdminTriesToDowngradeSelf_ShouldFail()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();

        var adminUser = CreateUser(userId: adminUserId, role: UserRole.Admin);

        _currentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _userRepository.Setup(x => x.GetByIdAsync(adminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);

        var command = new AdminDowngradeUserCommand(adminUserId, "Testing", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot downgrade your own account");
        _auditLogRepository.Verify(x => x.AddAsync(It.IsAny<AdminAuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Domain Validation Tests

    [Fact]
    public async Task Handle_UserAlreadyGeneralUser_ShouldFail()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var adminUser = CreateUser(userId: adminUserId, role: UserRole.Admin);
        var targetUser = CreateUser(userId: targetUserId, role: UserRole.GeneralUser);

        _currentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _userRepository.Setup(x => x.GetByIdAsync(adminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _userRepository.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var command = new AdminDowngradeUserCommand(targetUserId, "Testing", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("User is already a General User");
        _auditLogRepository.Verify(x => x.AddAsync(It.IsAny<AdminAuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region User Not Found Tests

    [Fact]
    public async Task Handle_TargetUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var adminUser = CreateUser(userId: adminUserId, role: UserRole.Admin);

        _currentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _userRepository.Setup(x => x.GetByIdAsync(adminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _userRepository.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new AdminDowngradeUserCommand(targetUserId, "Testing", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("User not found");
        _auditLogRepository.Verify(x => x.AddAsync(It.IsAny<AdminAuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AdminUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        _currentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _userRepository.Setup(x => x.GetByIdAsync(adminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new AdminDowngradeUserCommand(targetUserId, "Testing", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Admin user not found");
        _auditLogRepository.Verify(x => x.AddAsync(It.IsAny<AdminAuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Permission Tests

    [Fact]
    public async Task Handle_NonAdminTriesDowngrade_ShouldFail()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var adminUser = CreateUser(userId: adminUserId, role: UserRole.GeneralUser);
        var targetUser = CreateUser(userId: targetUserId, role: UserRole.EventOrganizer);

        _currentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _userRepository.Setup(x => x.GetByIdAsync(adminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _userRepository.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var command = new AdminDowngradeUserCommand(targetUserId, "Testing", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Insufficient permissions");
        _auditLogRepository.Verify(x => x.AddAsync(It.IsAny<AdminAuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Audit Log Tests

    [Fact]
    public async Task Handle_SuccessfulDowngrade_ShouldCreateAuditLog()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var adminUser = CreateUser(userId: adminUserId, role: UserRole.Admin);
        var targetUser = CreateUser(userId: targetUserId, role: UserRole.EventOrganizer);

        _currentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _userRepository.Setup(x => x.GetByIdAsync(adminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _userRepository.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var command = new AdminDowngradeUserCommand(targetUserId, "Testing downgrade", "192.168.1.1", "TestAgent/1.0");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogRepository.Verify(x => x.AddAsync(
            It.Is<AdminAuditLog>(log =>
                log.AdminUserId == adminUserId &&
                log.Action == AdminAuditActions.UserRoleDowngraded &&
                log.TargetUserId == targetUserId &&
                log.IpAddress == "192.168.1.1" &&
                log.UserAgent == "TestAgent/1.0"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Domain Event Tests

    [Fact]
    public async Task Handle_SuccessfulDowngrade_ShouldRaiseUserRoleChangedEvent()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var adminUser = CreateUser(userId: adminUserId, role: UserRole.Admin);
        var targetUser = CreateUser(userId: targetUserId, role: UserRole.EventOrganizer);

        _currentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _userRepository.Setup(x => x.GetByIdAsync(adminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _userRepository.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var command = new AdminDowngradeUserCommand(targetUserId, "Testing", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var roleChangedEvent = targetUser.DomainEvents.OfType<UserRoleChangedEvent>().Single();
        roleChangedEvent.UserId.Should().Be(targetUserId);
        roleChangedEvent.OldRole.Should().Be(UserRole.EventOrganizer);
        roleChangedEvent.NewRole.Should().Be(UserRole.GeneralUser);
    }

    #endregion

    #region Pending Upgrade Cleanup Tests

    [Fact]
    public async Task Handle_UserWithPendingUpgrade_ShouldClearPendingUpgrade()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var adminUser = CreateUser(userId: adminUserId, role: UserRole.Admin);
        var targetUser = CreateUser(userId: targetUserId, role: UserRole.EventOrganizer);

        // Simulate a pending upgrade request
        var upgradeMethods = typeof(User).GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var pendingUpgradeProperty = typeof(User).GetProperty("PendingUpgradeRole");
        var upgradeRequestedAtProperty = typeof(User).GetProperty("UpgradeRequestedAt");

        pendingUpgradeProperty?.SetValue(targetUser, UserRole.BusinessOwner);
        upgradeRequestedAtProperty?.SetValue(targetUser, DateTime.UtcNow);

        _currentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _userRepository.Setup(x => x.GetByIdAsync(adminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _userRepository.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var command = new AdminDowngradeUserCommand(targetUserId, "Downgrade overrides pending upgrade", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        targetUser.PendingUpgradeRole.Should().BeNull();
        targetUser.UpgradeRequestedAt.Should().BeNull();
    }

    #endregion

    #region Repository Commit Tests

    [Fact]
    public async Task Handle_SuccessfulDowngrade_ShouldCommitChanges()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var adminUser = CreateUser(userId: adminUserId, role: UserRole.Admin);
        var targetUser = CreateUser(userId: targetUserId, role: UserRole.EventOrganizer);

        _currentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _userRepository.Setup(x => x.GetByIdAsync(adminUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _userRepository.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var command = new AdminDowngradeUserCommand(targetUserId, "Testing", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task Handle_WithCancellationRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var cancellationToken = new CancellationToken(true);

        _currentUserService.Setup(x => x.UserId).Returns(adminUserId);
        _userRepository.Setup(x => x.GetByIdAsync(adminUserId, It.IsAny<CancellationToken>()))
            .Returns((Guid id, CancellationToken ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult<User?>(null);
            });

        var command = new AdminDowngradeUserCommand(targetUserId, "Testing", null, null);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _handler.Handle(command, cancellationToken));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method to create a User entity for testing
    /// </summary>
    private User CreateUser(Guid userId, UserRole role = UserRole.GeneralUser, string? emailAddress = null)
    {
        var email = Email.Create(emailAddress ?? $"user{userId:N}@test.com").Value;
        var user = User.Create(email, "Test", "User").Value;

        // Use reflection to set Id and Role since they're private setters
        var idProperty = typeof(User).GetProperty("Id");
        idProperty?.SetValue(user, userId);

        var roleProperty = typeof(User).GetProperty("Role");
        roleProperty?.SetValue(user, role);

        return user;
    }

    #endregion
}
