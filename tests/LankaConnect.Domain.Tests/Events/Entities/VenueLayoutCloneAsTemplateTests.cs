using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Domain.Tests.Events.Entities;

/// <summary>
/// Slice 8 S8.9b: <see cref="VenueLayout.CloneAsTemplate"/> static factory.
/// Per architect Option A → Option B (faithful clone): produces a new
/// per-user template (<c>EventId == null</c>, <c>IsTemplate == true</c>) that
/// mirrors the source's structure with fresh server-side IDs and preserves
/// per-seat <c>IsEnabled</c> / <c>IsAccessible</c> flags. Tier mappings live
/// on the <c>TicketTier</c> aggregate (different aggregate, owned by the
/// event) and are deliberately dropped — templates are tier-free by design.
/// </summary>
public class VenueLayoutCloneAsTemplateTests
{
    private readonly Guid _sourceOwnerId = Guid.NewGuid();
    private readonly Guid _newOwnerId = Guid.NewGuid();
    private readonly Guid _eventId = Guid.NewGuid();

    /// <summary>
    /// Builds a moderately complex source layout: 1 zone with 2×3 seats
    /// (one disabled, one accessible), 1 round table (capacity 4, one seat
    /// disabled), and 1 stage decoration. Mirrors the kinds of edits the
    /// canvas editor + Slice 5 Chunk 7 toggles produce in production.
    /// </summary>
    private VenueLayout BuildRichSource()
    {
        var canvas = CanvasConfig.Create(1600, 1000, 1.5, "#fafafa").Value;
        var layout = VenueLayout.Create(
            "Source Hall", LayoutType.Theater, _sourceOwnerId, _eventId,
            isTemplate: false, canvas: canvas).Value;

        var stage = layout.AddDecoration(
            DecorationKind.Stage, "Main Stage", sortOrder: 0,
            geometry: "{\"x\":100,\"y\":50,\"width\":600,\"height\":80,\"rotation\":0}",
            properties: "{\"colour\":\"#222\"}");
        stage.IsSuccess.Should().BeTrue();

        var zone = layout.AddZone(
            "Front Section", "#3b82f6", sortOrder: 0,
            shape: ZoneShape.Rect,
            geometry: "{\"x\":100,\"y\":200,\"width\":600,\"height\":300,\"rotation\":0}").Value;
        var seatGen = layout.GenerateTheaterSeats(zone.Id, rows: 2, seatsPerRow: 3);
        seatGen.IsSuccess.Should().BeTrue();

        // Disable A2 + flag A3 accessible — mirrors Slice 5 Chunk 7 toggles.
        var seatA2 = zone.Seats.Single(s => s.Row == "A" && s.Number == 2);
        var seatA3 = zone.Seats.Single(s => s.Row == "A" && s.Number == 3);
        layout.DisableSeat(seatA2.Id).IsSuccess.Should().BeTrue();
        layout.SetSeatAccessible(seatA3.Id, true).IsSuccess.Should().BeTrue();

        var table = layout.GenerateRoundTable(
            "T1", capacity: 4, sortOrder: 0,
            geometry: "{\"centerX\":1000,\"centerY\":400,\"radius\":50}").Value;
        // Disable seat 2 around the round table.
        var t1Seat2 = table.Seats.Single(s => s.Number == 2);
        layout.DisableSeat(t1Seat2.Id).IsSuccess.Should().BeTrue();

        return layout;
    }

    [Fact]
    public void CloneAsTemplate_WithNullSource_Should_Fail()
    {
        var result = VenueLayout.CloneAsTemplate(null!, "New Template", _newOwnerId);

        result.IsSuccess.Should().BeFalse();
        result.Error.ToLowerInvariant().Should().Contain("source");
    }

    [Fact]
    public void CloneAsTemplate_WithEmptyName_Should_Fail()
    {
        var source = BuildRichSource();
        var result = VenueLayout.CloneAsTemplate(source, "", _newOwnerId);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void CloneAsTemplate_WithWhitespaceName_Should_Fail()
    {
        var source = BuildRichSource();
        var result = VenueLayout.CloneAsTemplate(source, "   ", _newOwnerId);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void CloneAsTemplate_WithEmptyOwnerId_Should_Fail()
    {
        var source = BuildRichSource();
        var result = VenueLayout.CloneAsTemplate(source, "Copy", Guid.Empty);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void CloneAsTemplate_Should_SetTemplateMarkers()
    {
        var source = BuildRichSource();
        var clone = VenueLayout.CloneAsTemplate(source, "Source Hall (Template)", _newOwnerId).Value;

        clone.IsTemplate.Should().BeTrue();
        clone.EventId.Should().BeNull();
        clone.CreatedByUserId.Should().Be(_newOwnerId);
        clone.Name.Should().Be("Source Hall (Template)");
        clone.LayoutType.Should().Be(LayoutType.Theater);
        clone.Id.Should().NotBe(source.Id); // fresh aggregate ID
    }

    [Fact]
    public void CloneAsTemplate_Should_PreserveCanvas()
    {
        var source = BuildRichSource();
        var clone = VenueLayout.CloneAsTemplate(source, "Copy", _newOwnerId).Value;

        clone.Canvas.Width.Should().Be(source.Canvas.Width);
        clone.Canvas.Height.Should().Be(source.Canvas.Height);
        clone.Canvas.Scale.Should().Be(source.Canvas.Scale);
        clone.Canvas.BackgroundColor.Should().Be(source.Canvas.BackgroundColor);
    }

    [Fact]
    public void CloneAsTemplate_Should_CloneZonesWithFreshIds_AndSameStructure()
    {
        var source = BuildRichSource();
        var clone = VenueLayout.CloneAsTemplate(source, "Copy", _newOwnerId).Value;

        clone.Zones.Should().HaveCount(source.Zones.Count);
        for (var i = 0; i < source.Zones.Count; i++)
        {
            var src = source.Zones[i];
            var dst = clone.Zones[i];
            dst.Id.Should().NotBe(src.Id);             // fresh server-side ID
            dst.VenueLayoutId.Should().Be(clone.Id);   // pointed at new layout
            dst.Name.Should().Be(src.Name);
            dst.Color.Should().Be(src.Color);
            dst.SortOrder.Should().Be(src.SortOrder);
            dst.Shape.Should().Be(src.Shape);
            dst.Geometry.Should().Be(src.Geometry);
        }
    }

    [Fact]
    public void CloneAsTemplate_Should_CloneZoneSeatsWithFreshIds_AndPreserveLabels()
    {
        var source = BuildRichSource();
        var clone = VenueLayout.CloneAsTemplate(source, "Copy", _newOwnerId).Value;

        var srcZone = source.Zones[0];
        var dstZone = clone.Zones[0];
        dstZone.Seats.Should().HaveCount(srcZone.Seats.Count);

        foreach (var srcSeat in srcZone.Seats)
        {
            var dstSeat = dstZone.Seats.Single(s => s.Row == srcSeat.Row && s.Number == srcSeat.Number);
            dstSeat.Id.Should().NotBe(srcSeat.Id);
            dstSeat.VenueZoneId.Should().Be(dstZone.Id);
            dstSeat.Label.Should().Be(srcSeat.Label);
            dstSeat.SortOrder.Should().Be(srcSeat.SortOrder);
        }
    }

    [Fact]
    public void CloneAsTemplate_Should_PreserveDisabledSeatFlags()
    {
        var source = BuildRichSource();
        var clone = VenueLayout.CloneAsTemplate(source, "Copy", _newOwnerId).Value;

        var srcZone = source.Zones[0];
        var dstZone = clone.Zones[0];

        // Source disabled A2; clone must also report A2 as disabled.
        var srcA2 = srcZone.Seats.Single(s => s.Row == "A" && s.Number == 2);
        var dstA2 = dstZone.Seats.Single(s => s.Row == "A" && s.Number == 2);
        srcA2.IsEnabled.Should().BeFalse();
        dstA2.IsEnabled.Should().BeFalse();

        // Other seats stay enabled.
        var srcA1 = srcZone.Seats.Single(s => s.Row == "A" && s.Number == 1);
        var dstA1 = dstZone.Seats.Single(s => s.Row == "A" && s.Number == 1);
        srcA1.IsEnabled.Should().BeTrue();
        dstA1.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void CloneAsTemplate_Should_PreserveAccessibleSeatFlags()
    {
        var source = BuildRichSource();
        var clone = VenueLayout.CloneAsTemplate(source, "Copy", _newOwnerId).Value;

        var dstZone = clone.Zones[0];
        var dstA3 = dstZone.Seats.Single(s => s.Row == "A" && s.Number == 3);
        dstA3.IsAccessible.Should().BeTrue();

        // Other seats stay non-accessible.
        var dstA1 = dstZone.Seats.Single(s => s.Row == "A" && s.Number == 1);
        dstA1.IsAccessible.Should().BeFalse();
    }

    [Fact]
    public void CloneAsTemplate_Should_CloneTablesWithFreshIds_AndPreserveSeats()
    {
        var source = BuildRichSource();
        var clone = VenueLayout.CloneAsTemplate(source, "Copy", _newOwnerId).Value;

        clone.Tables.Should().HaveCount(source.Tables.Count);
        var srcTable = source.Tables[0];
        var dstTable = clone.Tables[0];

        dstTable.Id.Should().NotBe(srcTable.Id);
        dstTable.VenueLayoutId.Should().Be(clone.Id);
        dstTable.Label.Should().Be(srcTable.Label);
        dstTable.Shape.Should().Be(srcTable.Shape);
        dstTable.Capacity.Should().Be(srcTable.Capacity);
        dstTable.SortOrder.Should().Be(srcTable.SortOrder);
        dstTable.Geometry.Should().Be(srcTable.Geometry);

        // Seats: same count, fresh IDs, same labels.
        dstTable.Seats.Should().HaveCount(srcTable.Seats.Count);
        foreach (var srcSeat in srcTable.Seats)
        {
            var dstSeat = dstTable.Seats.Single(s => s.Number == srcSeat.Number);
            dstSeat.Id.Should().NotBe(srcSeat.Id);
            dstSeat.VenueTableId.Should().Be(dstTable.Id);
            dstSeat.Label.Should().Be(srcSeat.Label);
            dstSeat.SortOrder.Should().Be(srcSeat.SortOrder);
        }
    }

    [Fact]
    public void CloneAsTemplate_Should_PreserveDisabledFlagsOnTableSeats()
    {
        var source = BuildRichSource();
        var clone = VenueLayout.CloneAsTemplate(source, "Copy", _newOwnerId).Value;

        var srcTable = source.Tables[0];
        var dstTable = clone.Tables[0];
        var srcSeat2 = srcTable.Seats.Single(s => s.Number == 2);
        var dstSeat2 = dstTable.Seats.Single(s => s.Number == 2);
        srcSeat2.IsEnabled.Should().BeFalse();
        dstSeat2.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void CloneAsTemplate_Should_CloneDecorations()
    {
        var source = BuildRichSource();
        var clone = VenueLayout.CloneAsTemplate(source, "Copy", _newOwnerId).Value;

        clone.Decorations.Should().HaveCount(source.Decorations.Count);
        var srcDec = source.Decorations[0];
        var dstDec = clone.Decorations[0];
        dstDec.Id.Should().NotBe(srcDec.Id);
        dstDec.VenueLayoutId.Should().Be(clone.Id);
        dstDec.Kind.Should().Be(srcDec.Kind);
        dstDec.Label.Should().Be(srcDec.Label);
        dstDec.SortOrder.Should().Be(srcDec.SortOrder);
        dstDec.Geometry.Should().Be(srcDec.Geometry);
        // Local copies to avoid the FluentAssertions `TypeExtensions.Properties(Type)`
        // extension-method ambiguity when `.Properties.Should()` is chained directly.
        string dstProps = dstDec.Properties;
        string srcProps = srcDec.Properties;
        dstProps.Should().Be(srcProps);
    }

    [Fact]
    public void CloneAsTemplate_Should_PreserveTotalCapacity()
    {
        var source = BuildRichSource();
        var clone = VenueLayout.CloneAsTemplate(source, "Copy", _newOwnerId).Value;

        // 2×3 zone seats - 1 disabled = 5 enabled in zone.
        // Round table 4 seats - 1 disabled = 3 enabled at table.
        // Total enabled = 8.
        clone.TotalCapacity.Should().Be(source.TotalCapacity);
        clone.TotalCapacity.Should().Be(8);
    }

    [Fact]
    public void CloneAsTemplate_From_Empty_Source_Should_Succeed_With_No_Children()
    {
        var canvas = CanvasConfig.Create(800, 600, 1.0, "#ffffff").Value;
        var source = VenueLayout.Create(
            "Empty", LayoutType.Theater, _sourceOwnerId, _eventId,
            isTemplate: false, canvas: canvas).Value;

        var clone = VenueLayout.CloneAsTemplate(source, "Empty Copy", _newOwnerId).Value;

        clone.Zones.Should().BeEmpty();
        clone.Tables.Should().BeEmpty();
        clone.Decorations.Should().BeEmpty();
        clone.IsTemplate.Should().BeTrue();
        clone.EventId.Should().BeNull();
    }

    [Fact]
    public void CloneAsTemplate_Should_Be_Independent_Of_Source()
    {
        // Mutating the source after the clone has no effect on the clone —
        // confirms there's no shared reference into source's child collections.
        var source = BuildRichSource();
        var clone = VenueLayout.CloneAsTemplate(source, "Copy", _newOwnerId).Value;
        var cloneZoneCountBefore = clone.Zones.Count;

        // Add a new zone to the source; clone must not pick it up.
        var newZone = source.AddZone("Extra Zone", "#000", sortOrder: 99);
        newZone.IsSuccess.Should().BeTrue();

        clone.Zones.Should().HaveCount(cloneZoneCountBefore);
    }
}
