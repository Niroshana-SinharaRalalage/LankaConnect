using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Modules.Identity.Domain.Enums;
using LankaConnect.Modules.Identity.Application.Queries;
using LankaConnect.Modules.Identity.Contracts;
using Moq;

namespace LankaConnect.Modules.Identity.Application.Tests.Queries;

/// <summary>
/// Unit tests for <see cref="IdentityQueries"/> -- the cross-module Contracts
/// adapter over the legacy User repository. Wave 4.6.b (2026-06-24).
/// </summary>
public sealed class IdentityQueriesTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly IdentityQueries _sut;

    public IdentityQueriesTests()
    {
        _sut = new IdentityQueries(_userRepo.Object);
    }

    private static User CreateSampleUser(string? email = null, UserRole role = UserRole.GeneralUser)
    {
        var emailValue = email ?? "test@example.com";
        var result = User.Create(
            email: Email.Create(emailValue).Value,
            firstName: "Test",
            lastName: "User",
            role: role);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsNull_WhenRepositoryReturnsNull()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _sut.GetUserByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByIdAsync_ProjectsToSummaryDto()
    {
        var user = CreateSampleUser();
        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _sut.GetUserByIdAsync(user.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be("test@example.com");
        result.DisplayName.Should().Be("Test User");
        result.Role.Should().Be(UserRoleDto.GeneralUser);
    }

    [Fact]
    public async Task GetUserDetailAsync_ProjectsFullDetail()
    {
        var user = CreateSampleUser(role: UserRole.Admin);
        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _sut.GetUserDetailAsync(user.Id);

        result.Should().NotBeNull();
        result!.Role.Should().Be(UserRoleDto.Admin);
        result.Status.Should().Be(UserStatusDto.Active);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsNull_WhenEmailInvalid()
    {
        var result = await _sut.GetByEmailAsync("not-an-email");

        result.Should().BeNull();
        _userRepo.Verify(r => r.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdsAsync_FiltersMissingUsers()
    {
        var u1 = CreateSampleUser("a@example.com");
        var u2 = CreateSampleUser("b@example.com");
        _userRepo.Setup(r => r.GetByIdAsync(u1.Id, It.IsAny<CancellationToken>())).ReturnsAsync(u1);
        _userRepo.Setup(r => r.GetByIdAsync(u2.Id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await _sut.GetByIdsAsync(new[] { u1.Id, u2.Id });

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(u1.Id);
    }

    [Fact]
    public async Task GetByIdsAsync_ReturnsEmpty_ForEmptyInput()
    {
        var result = await _sut.GetByIdsAsync(Array.Empty<Guid>());

        result.Should().BeEmpty();
        _userRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchByNameAsync_MapsResults()
    {
        var users = new List<User> { CreateSampleUser("a@x.com"), CreateSampleUser("b@x.com") };
        _userRepo.Setup(r => r.SearchByNameAsync("Test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await _sut.SearchByNameAsync("Test");

        result.Should().HaveCount(2);
        result.All(r => r.DisplayName == "Test User").Should().BeTrue();
    }

    [Fact]
    public async Task GetPagedAsync_TranslatesRoleFilter_DtoToDomain()
    {
        _userRepo.Setup(r => r.GetPagedAsync(1, 25, null, UserRole.Admin, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<User>(), 0));

        var (items, total) = await _sut.GetPagedAsync(1, 25, null, UserRoleDto.Admin, true);

        items.Should().BeEmpty();
        total.Should().Be(0);
        _userRepo.Verify(r => r.GetPagedAsync(1, 25, null, UserRole.Admin, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CountAsync_PassesThrough()
    {
        _userRepo.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(42);

        var result = await _sut.CountAsync();

        result.Should().Be(42);
    }

    [Fact]
    public async Task GetUserCountsByRoleAsync_TranslatesEnumKeys_DomainToDto()
    {
        var domainMap = new Dictionary<UserRole, int>
        {
            { UserRole.Admin, 5 },
            { UserRole.GeneralUser, 100 },
        };
        _userRepo.Setup(r => r.GetUserCountsByRoleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(domainMap);

        var result = await _sut.GetUserCountsByRoleAsync();

        result[UserRoleDto.Admin].Should().Be(5);
        result[UserRoleDto.GeneralUser].Should().Be(100);
    }
}
