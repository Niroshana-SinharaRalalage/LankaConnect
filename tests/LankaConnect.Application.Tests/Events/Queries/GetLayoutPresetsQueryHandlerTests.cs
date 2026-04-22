using FluentAssertions;
using LankaConnect.Application.Events.Queries.GetLayoutPresets;
using LankaConnect.Domain.Events.Presets;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Queries;

/// <summary>
/// Slice 6 Chunk S6.2: GetLayoutPresets projects the static preset list onto DTOs.
/// </summary>
public class GetLayoutPresetsQueryHandlerTests
{
    private readonly GetLayoutPresetsQueryHandler _sut;

    public GetLayoutPresetsQueryHandlerTests()
    {
        _sut = new GetLayoutPresetsQueryHandler(Mock.Of<ILogger<GetLayoutPresetsQueryHandler>>());
    }

    [Fact]
    public async Task Handle_Should_Return_All_Eight_Presets()
    {
        var result = await _sut.Handle(new GetLayoutPresetsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(LayoutPresets.All.Count);
        result.Value.Should().HaveCount(8);
    }

    [Fact]
    public async Task Handle_Should_Preserve_Preset_Order_From_Domain_Source()
    {
        var result = await _sut.Handle(new GetLayoutPresetsQuery(), CancellationToken.None);

        result.Value.Select(p => p.Id).Should().Equal(
            LayoutPresets.All.Select(p => p.Id));
    }

    [Fact]
    public async Task Handle_Should_Project_Every_Field_From_Domain_Metadata()
    {
        var result = await _sut.Handle(new GetLayoutPresetsQuery(), CancellationToken.None);

        foreach (var dto in result.Value)
        {
            var meta = LayoutPresets.FindMetadata(dto.Id);
            meta.Should().NotBeNull();
            dto.Name.Should().Be(meta!.Name);
            dto.Description.Should().Be(meta.Description);
            dto.LayoutType.Should().Be(meta.LayoutType);
            dto.TotalCapacity.Should().Be(meta.TotalCapacity);
            dto.ThumbnailUrl.Should().Be(meta.ThumbnailUrl);
        }
    }
}
