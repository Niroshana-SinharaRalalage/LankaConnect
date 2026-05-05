using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Domain.Tests.Events.Entities;

/// <summary>
/// Slice 8 S8.10: <see cref="VenueLayout.CloneFromTemplate"/> static factory.
/// Mirror of <see cref="VenueLayout.CloneAsTemplate"/> but in the opposite
/// direction — clones a per-user template (<c>IsTemplate == true</c>,
/// <c>EventId == null</c>) into a fresh event-attached layout
/// (<c>IsTemplate == false</c>, <c>EventId == targetEventId</c>) so an
/// organizer can apply a saved template to a new event. Per-seat
/// <c>IsEnabled</c> / <c>IsAccessible</c> flags round-trip; tier mappings are
/// not cloned (templates carry none, and the target event's tiers are owned
/// by the <c>TicketTier</c> aggregate, not the layout).
/// </summary>
public class VenueLayoutCloneFromTemplateTests
{
    private readonly Guid _templateOwnerId = Guid.NewGuid();
    private readonly Guid _newOwnerId = Guid.NewGuid();
    private readonly Guid _targetEventId = Guid.NewGuid();

    /// <summary>
    /// Builds a representative template: 1 zone with 4 seats (1 disabled, 1
    /// accessible), 1 round table (capacity 4, 1 disabled seat), 1 stage
    /// decoration. Mirrors the saved-template shape produced by S8.9b.
    /// </summary>
    private VenueLayout BuildSourceTemplate()
    {
        var canvas = CanvasConfig.Create(1600, 1000, 1.5, "#fafafa").Value;
        var template = VenueLayout.Create(
            "Theater Template", LayoutType.Theater, _templateOwnerId,
            eventId: null, isTemplate: true, canvas: canvas).Value;

        template.AddDecoration(
            DecorationKind.Stage, "Main Stage", sortOrder: 0,
            geometry: "{\"x\":100,\"y\":50,\"width\":600,\"height\":80,\"rotation\":0}",
            properties: "{\"colour\":\"#222\"}").IsSuccess.Should().BeTrue();

        var zone = template.AddZone(
            "Front Section", "#3b82f6", sortOrder: 0,
            shape: ZoneShape.Rect,
            geometry: "{\"x\":100,\"y\":200,\"width\":600,\"height\":300,\"rotation\":0}").Value;
        template.GenerateTheaterSeats(zone.Id, rows: 2, seatsPerRow: 2).IsSuccess.Should().BeTrue();

        var seatA2 = zone.Seats.Single(s => s.Row == "A" && s.Number == 2);
        var seatB1 = zone.Seats.Single(s => s.Row == "B" && s.Number == 1);
        template.DisableSeat(seatA2.Id).IsSuccess.Should().BeTrue();
        template.SetSeatAccessible(seatB1.Id, true).IsSuccess.Should().BeTrue();

        var table = template.GenerateRoundTable(
            "T1", capacity: 4, sortOrder: 0,
            geometry: "{\"centerX\":1000,\"centerY\":400,\"radius\":50}").Value;
        var t1Seat3 = table.Seats.Single(s => s.Number == 3);
        template.DisableSeat(t1Seat3.Id).IsSuccess.Should().BeTrue();

        return template;
    }

    [Fact]
    public void CloneFromTemplate_WithNullSource_Should_Fail()
    {
        var result = VenueLayout.CloneFromTemplate(null!, _targetEventId, "Applied", _newOwnerId);
        result.IsSuccess.Should().BeFalse();
        result.Error.ToLowerInvariant().Should().Contain("template");
    }

    [Fact]
    public void CloneFromTemplate_When_Source_Is_Not_A_Template_Should_Fail()
    {
        // Event-attached layouts shouldn't go through this factory — the
        // organizer should use save-as-template first, then apply.
        var notATemplate = VenueLayout.Create(
            "Already attached", LayoutType.Theater, _templateOwnerId,
            eventId: Guid.NewGuid()).Value;

        var result = VenueLayout.CloneFromTemplate(notATemplate, _targetEventId, "Applied", _newOwnerId);
        result.IsSuccess.Should().BeFalse();
        result.Error.ToLowerInvariant().Should().Contain("template");
    }

    [Fact]
    public void CloneFromTemplate_With_Empty_TargetEventId_Should_Fail()
    {
        var template = BuildSourceTemplate();
        var result = VenueLayout.CloneFromTemplate(template, Guid.Empty, "Applied", _newOwnerId);
        result.IsSuccess.Should().BeFalse();
        result.Error.ToLowerInvariant().Should().Contain("event");
    }

    [Fact]
    public void CloneFromTemplate_With_Empty_NewOwnerId_Should_Fail()
    {
        var template = BuildSourceTemplate();
        var result = VenueLayout.CloneFromTemplate(template, _targetEventId, "Applied", Guid.Empty);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void CloneFromTemplate_With_Empty_Or_Whitespace_Name_Should_Fail()
    {
        var template = BuildSourceTemplate();
        VenueLayout.CloneFromTemplate(template, _targetEventId, "", _newOwnerId).IsSuccess.Should().BeFalse();
        VenueLayout.CloneFromTemplate(template, _targetEventId, "   ", _newOwnerId).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void CloneFromTemplate_Should_Set_Event_Markers()
    {
        var template = BuildSourceTemplate();
        var clone = VenueLayout.CloneFromTemplate(template, _targetEventId, "Applied", _newOwnerId).Value;

        clone.IsTemplate.Should().BeFalse();
        clone.EventId.Should().Be(_targetEventId);
        clone.CreatedByUserId.Should().Be(_newOwnerId);
        clone.Name.Should().Be("Applied");
        clone.LayoutType.Should().Be(LayoutType.Theater);
        clone.Id.Should().NotBe(template.Id);
    }

    [Fact]
    public void CloneFromTemplate_Should_Preserve_Canvas()
    {
        var template = BuildSourceTemplate();
        var clone = VenueLayout.CloneFromTemplate(template, _targetEventId, "Applied", _newOwnerId).Value;

        clone.Canvas.Width.Should().Be(template.Canvas.Width);
        clone.Canvas.Height.Should().Be(template.Canvas.Height);
        clone.Canvas.Scale.Should().Be(template.Canvas.Scale);
        clone.Canvas.BackgroundColor.Should().Be(template.Canvas.BackgroundColor);
    }

    [Fact]
    public void CloneFromTemplate_Should_Clone_Zones_With_Fresh_Ids_And_Same_Shape()
    {
        var template = BuildSourceTemplate();
        var clone = VenueLayout.CloneFromTemplate(template, _targetEventId, "Applied", _newOwnerId).Value;

        clone.Zones.Should().HaveCount(template.Zones.Count);
        for (var i = 0; i < template.Zones.Count; i++)
        {
            var src = template.Zones[i];
            var dst = clone.Zones[i];
            dst.Id.Should().NotBe(src.Id);
            dst.VenueLayoutId.Should().Be(clone.Id);
            dst.Name.Should().Be(src.Name);
            dst.Color.Should().Be(src.Color);
            dst.SortOrder.Should().Be(src.SortOrder);
            dst.Shape.Should().Be(src.Shape);
            dst.Geometry.Should().Be(src.Geometry);
        }
    }

    [Fact]
    public void CloneFromTemplate_Should_Clone_Zone_Seats_With_Flag_Fidelity()
    {
        var template = BuildSourceTemplate();
        var clone = VenueLayout.CloneFromTemplate(template, _targetEventId, "Applied", _newOwnerId).Value;

        var srcZone = template.Zones[0];
        var dstZone = clone.Zones[0];
        dstZone.Seats.Should().HaveCount(srcZone.Seats.Count);

        foreach (var srcSeat in srcZone.Seats)
        {
            var dstSeat = dstZone.Seats.Single(s => s.Row == srcSeat.Row && s.Number == srcSeat.Number);
            dstSeat.Id.Should().NotBe(srcSeat.Id);
            dstSeat.IsEnabled.Should().Be(srcSeat.IsEnabled);
            dstSeat.IsAccessible.Should().Be(srcSeat.IsAccessible);
            dstSeat.Label.Should().Be(srcSeat.Label);
            dstSeat.SortOrder.Should().Be(srcSeat.SortOrder);
        }
    }

    [Fact]
    public void CloneFromTemplate_Should_Clone_Tables_With_Seat_Fidelity()
    {
        var template = BuildSourceTemplate();
        var clone = VenueLayout.CloneFromTemplate(template, _targetEventId, "Applied", _newOwnerId).Value;

        clone.Tables.Should().HaveCount(template.Tables.Count);
        var srcTable = template.Tables[0];
        var dstTable = clone.Tables[0];
        dstTable.Id.Should().NotBe(srcTable.Id);
        dstTable.VenueLayoutId.Should().Be(clone.Id);
        dstTable.Capacity.Should().Be(srcTable.Capacity);
        dstTable.Seats.Should().HaveCount(srcTable.Seats.Count);

        foreach (var srcSeat in srcTable.Seats)
        {
            var dstSeat = dstTable.Seats.Single(s => s.Number == srcSeat.Number);
            dstSeat.IsEnabled.Should().Be(srcSeat.IsEnabled);
        }
    }

    [Fact]
    public void CloneFromTemplate_Should_Clone_Decorations()
    {
        var template = BuildSourceTemplate();
        var clone = VenueLayout.CloneFromTemplate(template, _targetEventId, "Applied", _newOwnerId).Value;

        clone.Decorations.Should().HaveCount(template.Decorations.Count);
        var srcDec = template.Decorations[0];
        var dstDec = clone.Decorations[0];
        dstDec.Id.Should().NotBe(srcDec.Id);
        dstDec.Kind.Should().Be(srcDec.Kind);
        dstDec.Label.Should().Be(srcDec.Label);
        dstDec.Geometry.Should().Be(srcDec.Geometry);
        // Local copy avoids the FluentAssertions `TypeExtensions.Properties(Type)`
        // collision documented in VenueLayoutCloneAsTemplateTests.
        string dstProps = dstDec.Properties;
        string srcProps = srcDec.Properties;
        dstProps.Should().Be(srcProps);
    }

    [Fact]
    public void CloneFromTemplate_Should_Preserve_Total_Capacity()
    {
        var template = BuildSourceTemplate();
        var clone = VenueLayout.CloneFromTemplate(template, _targetEventId, "Applied", _newOwnerId).Value;
        clone.TotalCapacity.Should().Be(template.TotalCapacity);
    }

    [Fact]
    public void CloneFromTemplate_From_Empty_Template_Should_Succeed()
    {
        var canvas = CanvasConfig.Create(800, 600, 1.0, "#ffffff").Value;
        var emptyTemplate = VenueLayout.Create(
            "Empty Template", LayoutType.Theater, _templateOwnerId,
            eventId: null, isTemplate: true, canvas: canvas).Value;

        var clone = VenueLayout.CloneFromTemplate(emptyTemplate, _targetEventId, "Applied", _newOwnerId).Value;

        clone.Zones.Should().BeEmpty();
        clone.Tables.Should().BeEmpty();
        clone.Decorations.Should().BeEmpty();
        clone.IsTemplate.Should().BeFalse();
        clone.EventId.Should().Be(_targetEventId);
    }
}
