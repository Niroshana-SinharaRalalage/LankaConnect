using LankaConnect.Modules.Communications.Domain.Repositories;
using LankaConnect.Modules.Communications.Domain.Entities;
using LankaConnect.Modules.Communications.Domain;
using LankaConnect.Modules.Communications.Application.Queries;
using LankaConnect.Modules.Communications.Contracts;
using Moq;

namespace LankaConnect.Modules.Communications.Application.Tests.Queries;

/// <summary>
/// Wave 5.4.b (2026-06-13) — unit tests for <see cref="EmailGroupQueries"/>,
/// the Contracts-side adapter that wraps <see cref="IEmailGroupRepository"/>.
/// Moq mocks the repository; no DB access. Asserts wire-shape correctness of
/// the Domain -> Contracts projection (the failing case is silent ABI drift
/// for cross-module consumers in Wave 5.4.d).
/// </summary>
public sealed class EmailGroupQueriesTests
{
    [Fact]
    public async Task GetByIdsAsync_EmptyInput_ReturnsEmpty_WithoutCallingRepository()
    {
        var repo = new Mock<IEmailGroupRepository>();
        var sut = new EmailGroupQueries(repo.Object);

        var result = await sut.GetByIdsAsync(Array.Empty<Guid>());

        result.Should().BeEmpty();
        repo.Verify(
            r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdsAsync_ReturnsMappedSummaries()
    {
        var ownerId = Guid.NewGuid();
        var group = EmailGroup.Create(
            name: "Newsletter Subs",
            ownerId: ownerId,
            emailAddresses: "a@x.com, b@x.com, c@x.com",
            description: "Active list").Value;

        var repo = new Mock<IEmailGroupRepository>();
        repo
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailGroup> { group });

        var sut = new EmailGroupQueries(repo.Object);

        var result = await sut.GetByIdsAsync(new[] { group.Id });

        result.Should().HaveCount(1);
        var dto = result[0];
        dto.Id.Should().Be(group.Id);
        dto.Name.Should().Be("Newsletter Subs");
        dto.OwnerId.Should().Be(ownerId);
        dto.EmailCount.Should().Be(3);
        dto.IsActive.Should().BeTrue();
        dto.Description.Should().Be("Active list");
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        var repo = new Mock<IEmailGroupRepository>();
        repo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailGroup?)null);

        var sut = new EmailGroupQueries(repo.Object);

        var result = await sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithMatch_ReturnsSummary()
    {
        var group = EmailGroup.Create(
            name: "VIPs",
            ownerId: Guid.NewGuid(),
            emailAddresses: "vip@x.com").Value;

        var repo = new Mock<IEmailGroupRepository>();
        repo
            .Setup(r => r.GetByIdAsync(group.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var sut = new EmailGroupQueries(repo.Object);

        var result = await sut.GetByIdAsync(group.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(group.Id);
        result.Name.Should().Be("VIPs");
        result.EmailCount.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdWithEmailsAsync_ReturnsDetailWithEmailsSplit()
    {
        var group = EmailGroup.Create(
            name: "Big List",
            ownerId: Guid.NewGuid(),
            emailAddresses: "a@x.com,b@x.com,c@x.com,d@x.com").Value;

        var repo = new Mock<IEmailGroupRepository>();
        repo
            .Setup(r => r.GetByIdAsync(group.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var sut = new EmailGroupQueries(repo.Object);

        var result = await sut.GetByIdWithEmailsAsync(group.Id);

        result.Should().NotBeNull();
        result!.Emails.Should().HaveCount(4);
        result.Emails.Should().Contain(new[] { "a@x.com", "b@x.com", "c@x.com", "d@x.com" });
        result.EmailCount.Should().Be(4);
    }

    [Fact]
    public async Task GetByOwnerAsync_ReturnsAllOwnerGroups()
    {
        var ownerId = Guid.NewGuid();
        var g1 = EmailGroup.Create("Group A", ownerId, "a@x.com").Value;
        var g2 = EmailGroup.Create("Group B", ownerId, "b@x.com,c@x.com").Value;

        var repo = new Mock<IEmailGroupRepository>();
        repo
            .Setup(r => r.GetByOwnerAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailGroup> { g1, g2 });

        var sut = new EmailGroupQueries(repo.Object);

        var result = await sut.GetByOwnerAsync(ownerId);

        result.Should().HaveCount(2);
        result.Select(d => d.Name).Should().BeEquivalentTo(new[] { "Group A", "Group B" });
        result.Single(d => d.Name == "Group B").EmailCount.Should().Be(2);
    }
}
