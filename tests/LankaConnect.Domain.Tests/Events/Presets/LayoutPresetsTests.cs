using FluentAssertions;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Presets;

namespace LankaConnect.Domain.Tests.Events.Presets;

/// <summary>
/// Slice 6 Chunk S6.1: static preset factory — 8 industry-standard layouts.
/// These tests double as the architect's acceptance criteria: every preset the
/// plan enumerates must land with the promised name, layout type, and capacity.
/// </summary>
public class LayoutPresetsTests
{
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void All_Should_Contain_Exactly_Eight_Presets()
    {
        LayoutPresets.All.Should().HaveCount(8);
    }

    [Fact]
    public void All_Preset_Ids_Should_Be_Unique_And_NonEmpty()
    {
        var ids = LayoutPresets.All.Select(p => p.Id).ToList();
        ids.Should().OnlyContain(id => !string.IsNullOrWhiteSpace(id));
        ids.Distinct().Should().HaveCount(ids.Count);
    }

    [Theory]
    [InlineData(LayoutPresets.TheaterClassicId, "Theater Classic", LayoutType.Theater, 200)]
    [InlineData(LayoutPresets.TheaterWithBalconyId, "Theater with Balcony", LayoutType.Theater, 420)]
    [InlineData(LayoutPresets.TheaterWithAislesId, "Theater with Aisles", LayoutType.Theater, 240)]
    [InlineData(LayoutPresets.TheaterCurvedId, "Theater Curved", LayoutType.Theater, 160)]
    [InlineData(LayoutPresets.BanquetRound8Id, "Banquet · 15 Round Tables × 8", LayoutType.Banquet, 120)]
    [InlineData(LayoutPresets.BanquetRound10Id, "Banquet · 15 Round Tables × 10", LayoutType.Banquet, 150)]
    [InlineData(LayoutPresets.BanquetMixedId, "Banquet Mixed", LayoutType.Banquet, 120)]
    [InlineData(LayoutPresets.ConferenceRoomId, "Conference Room", LayoutType.Mixed, 68)]
    public void FindMetadata_Should_Return_Expected_Metadata(
        string id, string expectedName, LayoutType expectedType, int expectedCapacity)
    {
        var metadata = LayoutPresets.FindMetadata(id);

        metadata.Should().NotBeNull();
        metadata!.Name.Should().Be(expectedName);
        metadata.LayoutType.Should().Be(expectedType);
        metadata.TotalCapacity.Should().Be(expectedCapacity);
        metadata.Description.Should().NotBeNullOrWhiteSpace();
        metadata.ThumbnailUrl.Should().StartWith("/layouts/presets/");
    }

    [Fact]
    public void FindMetadata_Should_Return_Null_For_Unknown_Id()
    {
        LayoutPresets.FindMetadata("not-a-real-preset").Should().BeNull();
    }

    [Fact]
    public void Create_Should_Return_NotFound_For_Unknown_Id()
    {
        var result = LayoutPresets.Create("not-a-real-preset", _userId);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public void Create_Should_Fail_When_UserId_Is_Empty()
    {
        var result = LayoutPresets.Create(LayoutPresets.TheaterClassicId, Guid.Empty);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_With_No_EventId_Should_Produce_Template_Layout()
    {
        var result = LayoutPresets.Create(LayoutPresets.TheaterClassicId, _userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.EventId.Should().BeNull();
        result.Value.IsTemplate.Should().BeTrue();
    }

    [Fact]
    public void Create_With_EventId_Should_Produce_Event_Attached_Layout()
    {
        var eventId = Guid.NewGuid();
        var result = LayoutPresets.Create(LayoutPresets.TheaterClassicId, _userId, eventId);

        result.IsSuccess.Should().BeTrue();
        result.Value.EventId.Should().Be(eventId);
        result.Value.IsTemplate.Should().BeFalse();
    }

    [Fact]
    public void TheaterClassic_Should_Have_One_Zone_With_200_Seats_And_A_Stage()
    {
        var layout = LayoutPresets.Create(LayoutPresets.TheaterClassicId, _userId).Value;

        layout.LayoutType.Should().Be(LayoutType.Theater);
        layout.Zones.Should().HaveCount(1);
        layout.Zones.Single().Seats.Should().HaveCount(200);
        layout.Tables.Should().BeEmpty();
        layout.Decorations.Should().Contain(d => d.Kind == DecorationKind.Stage);
        layout.TotalCapacity.Should().Be(200);
    }

    [Fact]
    public void TheaterWithBalcony_Should_Have_Three_Zones_Totaling_420_Seats()
    {
        var layout = LayoutPresets.Create(LayoutPresets.TheaterWithBalconyId, _userId).Value;

        layout.Zones.Should().HaveCount(3);
        layout.Zones.Select(z => z.Name).Should().BeEquivalentTo(["Orchestra", "Mezzanine", "Balcony"]);
        layout.TotalCapacity.Should().Be(420);
        layout.Decorations.Should().Contain(d => d.Kind == DecorationKind.Stage);
    }

    [Fact]
    public void TheaterWithAisles_Should_Have_Three_Zones_And_Two_Aisle_Decorations()
    {
        var layout = LayoutPresets.Create(LayoutPresets.TheaterWithAislesId, _userId).Value;

        layout.Zones.Should().HaveCount(3);
        layout.TotalCapacity.Should().Be(240);
        layout.Decorations.Count(d => d.Kind == DecorationKind.Aisle).Should().Be(2);
        layout.Decorations.Should().Contain(d => d.Kind == DecorationKind.Stage);
    }

    [Fact]
    public void TheaterCurved_Should_Have_A_Curved_Zone()
    {
        var layout = LayoutPresets.Create(LayoutPresets.TheaterCurvedId, _userId).Value;

        layout.TotalCapacity.Should().Be(160);
        layout.Zones.Should().Contain(z => z.Shape == ZoneShape.Curve);
        layout.Decorations.Should().Contain(d => d.Kind == DecorationKind.Stage);
    }

    [Fact]
    public void BanquetRound8_Should_Have_15_Round_Tables_With_8_Seats_Each()
    {
        var layout = LayoutPresets.Create(LayoutPresets.BanquetRound8Id, _userId).Value;

        layout.LayoutType.Should().Be(LayoutType.Banquet);
        layout.Tables.Should().HaveCount(15);
        layout.Tables.Should().OnlyContain(t => t.Shape == TableShape.Round && t.Capacity == 8);
        layout.TotalCapacity.Should().Be(120);
    }

    [Fact]
    public void BanquetRound10_Should_Have_15_Round_Tables_With_10_Seats_Each()
    {
        var layout = LayoutPresets.Create(LayoutPresets.BanquetRound10Id, _userId).Value;

        layout.Tables.Should().HaveCount(15);
        layout.Tables.Should().OnlyContain(t => t.Shape == TableShape.Round && t.Capacity == 10);
        layout.TotalCapacity.Should().Be(150);
    }

    [Fact]
    public void BanquetMixed_Should_Have_Ten_Round_Plus_Five_Rect_Tables()
    {
        var layout = LayoutPresets.Create(LayoutPresets.BanquetMixedId, _userId).Value;

        layout.Tables.Count(t => t.Shape == TableShape.Round).Should().Be(10);
        layout.Tables.Count(t => t.Shape == TableShape.Rect).Should().Be(5);
        layout.TotalCapacity.Should().Be(120);
        layout.Decorations.Should().Contain(d => d.Kind == DecorationKind.DanceFloor);
    }

    [Fact]
    public void ConferenceRoom_Should_Be_A_Mixed_Layout_With_Tables_And_A_Classroom_Zone()
    {
        var layout = LayoutPresets.Create(LayoutPresets.ConferenceRoomId, _userId).Value;

        layout.LayoutType.Should().Be(LayoutType.Mixed);
        layout.Tables.Should().NotBeEmpty();
        layout.Zones.Should().NotBeEmpty();
        layout.TotalCapacity.Should().Be(68);
    }

    [Fact]
    public void Preset_Metadata_Capacity_Should_Match_Actual_Generated_Capacity_For_Every_Preset()
    {
        foreach (var meta in LayoutPresets.All)
        {
            var result = LayoutPresets.Create(meta.Id, _userId);
            result.IsSuccess.Should().BeTrue($"preset '{meta.Id}' must build successfully");
            result.Value.TotalCapacity.Should().Be(
                meta.TotalCapacity,
                $"preset '{meta.Id}' metadata capacity must match what the factory builds");
        }
    }

    [Fact]
    public void Created_Layout_Name_Should_Match_Preset_Name()
    {
        foreach (var meta in LayoutPresets.All)
        {
            var result = LayoutPresets.Create(meta.Id, _userId);
            result.Value.Name.Should().Be(meta.Name);
        }
    }
}
