using FluentAssertions;
using LankaConnect.Application.Events.Queries.GetUserTemplates;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Queries;

/// <summary>
/// Slice 8 S8.10 — list-my-templates query handler. Thin wrapper over the
/// repo + DTO mapper; tests guard input validation, repository wiring, and
/// the empty-list case (which is the common "no templates yet" path).
/// </summary>
public class GetUserTemplatesQueryHandlerTests
{
    private readonly Mock<IVenueLayoutRepository> _mockRepo = new();
    private readonly GetUserTemplatesQueryHandler _sut;

    public GetUserTemplatesQueryHandlerTests()
    {
        _sut = new GetUserTemplatesQueryHandler(
            _mockRepo.Object,
            Mock.Of<ILogger<GetUserTemplatesQueryHandler>>());
    }

    private static VenueLayout CreateTemplate(Guid ownerId, string name)
    {
        return VenueLayout.Create(
            name, LayoutType.Theater, ownerId,
            eventId: null, isTemplate: true).Value;
    }

    [Fact]
    public async Task Handle_Should_Reject_Empty_UserId()
    {
        var result = await _sut.Handle(new GetUserTemplatesQuery(Guid.Empty), CancellationToken.None);
        result.IsFailure.Should().BeTrue();
        _mockRepo.Verify(r => r.GetTemplatesByUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_Empty_List_When_User_Has_No_Templates()
    {
        var userId = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetTemplatesByUserAsync(userId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((IReadOnlyList<VenueLayout>)Array.Empty<VenueLayout>());

        var result = await _sut.Handle(new GetUserTemplatesQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Map_Each_Template_To_Dto()
    {
        var userId = Guid.NewGuid();
        var t1 = CreateTemplate(userId, "Template A");
        var t2 = CreateTemplate(userId, "Template B");
        _mockRepo.Setup(r => r.GetTemplatesByUserAsync(userId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((IReadOnlyList<VenueLayout>)new[] { t1, t2 });

        var result = await _sut.Handle(new GetUserTemplatesQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(d => d.Name).Should().BeEquivalentTo(new[] { "Template A", "Template B" });
        result.Value.All(d => d.IsTemplate).Should().BeTrue();
        result.Value.All(d => d.EventId == null).Should().BeTrue();
        result.Value.All(d => d.CreatedByUserId == userId).Should().BeTrue();
    }
}
