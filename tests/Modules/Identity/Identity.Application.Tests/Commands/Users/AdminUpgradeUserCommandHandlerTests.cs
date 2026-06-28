using LankaConnect.Modules.Identity.Contracts; // W4.6.a: ICurrentUserService moved here
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Application.Tests.TestHelpers;
using LankaConnect.Modules.Identity.Application.Commands.Users.AdminUpgradeUser;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Notifications.Domain;
using LankaConnect.Modules.Notifications.Domain.Enums;
using LankaConnect.Domain.Support;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Modules.Identity.Domain.Enums;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Email = LankaConnect.Domain.Shared.ValueObjects.Email;

namespace LankaConnect.Modules.Identity.Application.Tests.Commands.Users;

/// <summary>
/// Phase 6A.139 Slice 2: TDD tests for AdminUpgradeUserCommandHandler.
/// Mirrors AdminDowngradeUserCommandHandlerTests in the inverse direction.
/// Adds notification + email parity (mirrors ApproveRoleUpgradeCommandHandler side effects).
/// </summary>
public class AdminUpgradeUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<INotificationRepository> _notificationRepository;
    private readonly Mock<IAdminAuditLogRepository> _auditLogRepository;
    private readonly Mock<ICurrentUserService> _currentUserService;
    private readonly Mock<ITypedEmailService> _typedEmailService;
    private readonly Mock<IApplicationUrlsService> _urlsService;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly AdminUpgradeUserCommandHandler _handler;

    public AdminUpgradeUserCommandHandlerTests()
    {
        _userRepository = LankaConnect.Application.Tests.TestHelpers.MockRepository.CreateUserRepository();
        _notificationRepository = new Mock<INotificationRepository>();
        _auditLogRepository = new Mock<IAdminAuditLogRepository>();
        _currentUserService = new Mock<ICurrentUserService>();
        _typedEmailService = new Mock<ITypedEmailService>();
        _urlsService = new Mock<IApplicationUrlsService>();
        _unitOfWork = LankaConnect.Application.Tests.TestHelpers.MockRepository.CreateUnitOfWork();

        _urlsService.Setup(x => x.FrontendBaseUrl).Returns("https://staging.example.com");

        _typedEmailService
            .Setup(x => x.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok("corr-1", 10));

        _handler = new AdminUpgradeUserCommandHandler(
            _userRepository.Object,
            _notificationRepository.Object,
            _auditLogRepository.Object,
            _currentUserService.Object,
            _typedEmailService.Object,
            _urlsService.Object,
            _unitOfWork.Object,
            NullLogger<AdminUpgradeUserCommandHandler>.Instance);
    }

    #region Happy Path

    [Fact]
    public async Task Handle_AdminUpgradesGeneralUser_ShouldSucceedAndChangeRole()
    {
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var admin = CreateUser(adminId, UserRole.Admin);
        var target = CreateUser(targetId, UserRole.GeneralUser);
        SetupCurrentAdmin(adminId, admin);
        SetupTarget(targetId, target);

        var command = new AdminUpgradeUserCommand(targetId, "Approved by admin", "1.2.3.4", "UA/1.0");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.Role.Should().Be(UserRole.EventOrganizer);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AdminManagerUpgradesGeneralUser_ShouldSucceed()
    {
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var admin = CreateUser(adminId, UserRole.AdminManager);
        var target = CreateUser(targetId, UserRole.GeneralUser);
        SetupCurrentAdmin(adminId, admin);
        SetupTarget(targetId, target);

        var command = new AdminUpgradeUserCommand(targetId, "Approved by admin manager", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.Role.Should().Be(UserRole.EventOrganizer);
    }

    #endregion

    #region Auth + Self-Action Guards

    [Fact]
    public async Task Handle_NonAdminCallsHandler_ShouldFail()
    {
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var admin = CreateUser(adminId, UserRole.GeneralUser);
        var target = CreateUser(targetId, UserRole.GeneralUser);
        SetupCurrentAdmin(adminId, admin);
        SetupTarget(targetId, target);

        var command = new AdminUpgradeUserCommand(targetId, "x", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Insufficient permissions");
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AdminTriesToUpgradeSelf_ShouldFail()
    {
        var adminId = Guid.NewGuid();
        var admin = CreateUser(adminId, UserRole.Admin);
        SetupCurrentAdmin(adminId, admin);

        var command = new AdminUpgradeUserCommand(adminId, "x", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot upgrade your own account");
    }

    #endregion

    #region Not-Found Cases

    [Fact]
    public async Task Handle_AdminUserNotFound_ShouldFail()
    {
        var adminId = Guid.NewGuid();
        _currentUserService.Setup(x => x.UserId).Returns(adminId);
        _userRepository.Setup(x => x.GetByIdAsync(adminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new AdminUpgradeUserCommand(Guid.NewGuid(), "x", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Admin user not found");
    }

    [Fact]
    public async Task Handle_TargetUserNotFound_ShouldFail()
    {
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var admin = CreateUser(adminId, UserRole.Admin);
        SetupCurrentAdmin(adminId, admin);
        _userRepository.Setup(x => x.GetByIdAsync(targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var command = new AdminUpgradeUserCommand(targetId, "x", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("User not found");
    }

    #endregion

    #region Domain Validation

    [Fact]
    public async Task Handle_TargetAlreadyEventOrganizer_ShouldFail()
    {
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var admin = CreateUser(adminId, UserRole.Admin);
        var target = CreateUser(targetId, UserRole.EventOrganizer);
        SetupCurrentAdmin(adminId, admin);
        SetupTarget(targetId, target);

        var command = new AdminUpgradeUserCommand(targetId, "x", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already an Event Organizer");
        _auditLogRepository.Verify(x => x.AddAsync(It.IsAny<AdminAuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TargetIsBusinessOwner_ShouldFail_BecauseOnlyGeneralUsersUpgradable()
    {
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var admin = CreateUser(adminId, UserRole.Admin);
        var target = CreateUser(targetId, UserRole.BusinessOwner);
        SetupCurrentAdmin(adminId, admin);
        SetupTarget(targetId, target);

        var command = new AdminUpgradeUserCommand(targetId, "x", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Only General Users can be upgraded");
    }

    #endregion

    #region Audit Log

    [Fact]
    public async Task Handle_SuccessfulUpgrade_ShouldWriteAuditLogWithUserRoleUpgradedAction()
    {
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var admin = CreateUser(adminId, UserRole.Admin);
        var target = CreateUser(targetId, UserRole.GeneralUser);
        SetupCurrentAdmin(adminId, admin);
        SetupTarget(targetId, target);

        var command = new AdminUpgradeUserCommand(targetId, "Verified offline", "1.1.1.1", "TestAgent");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _auditLogRepository.Verify(x => x.AddAsync(
            It.Is<AdminAuditLog>(log =>
                log.AdminUserId == adminId &&
                log.Action == AdminAuditActions.UserRoleUpgraded &&
                log.TargetUserId == targetId &&
                log.IpAddress == "1.1.1.1" &&
                log.UserAgent == "TestAgent"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TargetHadPendingUpgradeRequest_ShouldRecordShortCircuitInAuditDetails()
    {
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var admin = CreateUser(adminId, UserRole.Admin);
        var target = CreateUser(targetId, UserRole.GeneralUser);

        // Simulate pending upgrade
        typeof(User).GetProperty(nameof(User.PendingUpgradeRole))!.SetValue(target, (UserRole?)UserRole.EventOrganizer);
        typeof(User).GetProperty(nameof(User.UpgradeRequestedAt))!.SetValue(target, (DateTime?)DateTime.UtcNow);

        SetupCurrentAdmin(adminId, admin);
        SetupTarget(targetId, target);

        AdminAuditLog? capturedAudit = null;
        _auditLogRepository
            .Setup(x => x.AddAsync(It.IsAny<AdminAuditLog>(), It.IsAny<CancellationToken>()))
            .Callback<AdminAuditLog, CancellationToken>((log, _) => capturedAudit = log)
            .Returns(Task.CompletedTask);

        var command = new AdminUpgradeUserCommand(targetId, "Direct admin upgrade", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.PendingUpgradeRole.Should().BeNull();
        target.UpgradeRequestedAt.Should().BeNull();
        capturedAudit.Should().NotBeNull();
        capturedAudit!.Details.Should().Contain("ShortCircuitedPendingRequest");
        capturedAudit.Details.Should().Contain("true");
    }

    #endregion

    #region Domain Event

    [Fact]
    public async Task Handle_SuccessfulUpgrade_ShouldRaiseUserRoleChangedEvent()
    {
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var admin = CreateUser(adminId, UserRole.Admin);
        var target = CreateUser(targetId, UserRole.GeneralUser);
        SetupCurrentAdmin(adminId, admin);
        SetupTarget(targetId, target);

        var command = new AdminUpgradeUserCommand(targetId, "x", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var roleEvent = target.DomainEvents.OfType<UserRoleChangedEvent>().Single();
        roleEvent.OldRole.Should().Be(UserRole.GeneralUser);
        roleEvent.NewRole.Should().Be(UserRole.EventOrganizer);
    }

    #endregion

    #region Notification + Email Side Effects

    [Fact]
    public async Task Handle_SuccessfulUpgrade_ShouldCreateInAppNotification()
    {
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var admin = CreateUser(adminId, UserRole.Admin);
        var target = CreateUser(targetId, UserRole.GeneralUser);
        SetupCurrentAdmin(adminId, admin);
        SetupTarget(targetId, target);

        var command = new AdminUpgradeUserCommand(targetId, "x", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _notificationRepository.Verify(x => x.AddAsync(
            It.Is<Notification>(n =>
                n.UserId == targetId &&
                n.Type == NotificationType.RoleUpgradeApproved),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SuccessfulUpgrade_ShouldSendOrganizerApprovalEmail()
    {
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var admin = CreateUser(adminId, UserRole.Admin);
        var target = CreateUser(targetId, UserRole.GeneralUser);
        SetupCurrentAdmin(adminId, admin);
        SetupTarget(targetId, target);

        var command = new AdminUpgradeUserCommand(targetId, "x", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _typedEmailService.Verify(x => x.SendEmailAsync(
            It.Is<IEmailParameters>(p => p is OrganizerRoleApprovalEmailParams),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailServiceThrows_ShouldNotFailCommand_FailSilent()
    {
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var admin = CreateUser(adminId, UserRole.Admin);
        var target = CreateUser(targetId, UserRole.GeneralUser);
        SetupCurrentAdmin(adminId, admin);
        SetupTarget(targetId, target);

        _typedEmailService
            .Setup(x => x.SendEmailAsync(It.IsAny<IEmailParameters>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("ACS down"));

        var command = new AdminUpgradeUserCommand(targetId, "x", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        // Role change committed even if email send blew up
        result.IsSuccess.Should().BeTrue();
        target.Role.Should().Be(UserRole.EventOrganizer);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task Handle_CancellationRequested_ShouldThrowOperationCanceledException()
    {
        var adminId = Guid.NewGuid();
        var token = new CancellationToken(true);
        _currentUserService.Setup(x => x.UserId).Returns(adminId);
        _userRepository
            .Setup(x => x.GetByIdAsync(adminId, It.IsAny<CancellationToken>()))
            .Returns((Guid _, CancellationToken ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult<User?>(null);
            });

        var command = new AdminUpgradeUserCommand(Guid.NewGuid(), "x", null, null);

        await Assert.ThrowsAsync<OperationCanceledException>(() => _handler.Handle(command, token));
    }

    #endregion

    #region Helpers

    private void SetupCurrentAdmin(Guid adminId, User admin)
    {
        _currentUserService.Setup(x => x.UserId).Returns(adminId);
        _userRepository.Setup(x => x.GetByIdAsync(adminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);
    }

    private void SetupTarget(Guid targetId, User target)
    {
        _userRepository.Setup(x => x.GetByIdAsync(targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
    }

    private static User CreateUser(Guid userId, UserRole role = UserRole.GeneralUser, string? emailAddress = null)
    {
        var email = Email.Create(emailAddress ?? $"user{userId:N}@test.com").Value;
        var user = User.Create(email, "Test", "User").Value;
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);
        typeof(User).GetProperty(nameof(User.Role))!.SetValue(user, role);
        return user;
    }

    #endregion
}
