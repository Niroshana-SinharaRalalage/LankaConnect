using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Domain.Tests.Events.Entities;

/// <summary>
/// Slice 2+3A regression + feature coverage for the expanded VenueLayout aggregate:
/// canvas, tables, decorations, mixed-mode TotalCapacity, and
/// <c>Event.EnableAssignedSeating</c> / <c>DisableAssignedSeating</c>.
/// </summary>
public class VenueLayoutSeatingExpansionTests
{
    private readonly Guid _userId = Guid.NewGuid();

    private VenueLayout NewLayout(LayoutType type = LayoutType.Theater) =>
        VenueLayout.Create("Hall", type, _userId).Value;

    #region Canvas

    [Fact]
    public void Create_WithoutCanvas_Should_Use_Default()
    {
        var layout = NewLayout();

        layout.Canvas.Should().NotBeNull();
        layout.Canvas.Width.Should().Be(1200);
        layout.Canvas.Height.Should().Be(800);
        layout.Canvas.BackgroundColor.Should().Be("#ffffff");
    }

    [Fact]
    public void Create_WithExplicitCanvas_Should_Persist_It()
    {
        var canvas = CanvasConfig.Create(1800, 1200, 0.5, "#202020").Value;

        var layout = VenueLayout.Create("Big Hall", LayoutType.Banquet, _userId, canvas: canvas).Value;

        layout.Canvas.Width.Should().Be(1800);
        layout.Canvas.Scale.Should().Be(0.5);
    }

    [Fact]
    public void UpdateCanvas_Should_Replace_Canvas_And_MarkUpdated()
    {
        var layout = NewLayout();
        var newCanvas = CanvasConfig.Create(2000, 2000, 2.0, "#000000").Value;

        var result = layout.UpdateCanvas(newCanvas);

        result.IsSuccess.Should().BeTrue();
        layout.Canvas.Width.Should().Be(2000);
        layout.UpdatedAt.Should().NotBeNull();
    }

    #endregion

    #region Tables

    [Fact]
    public void AddTable_Should_Add_Empty_Table()
    {
        var layout = NewLayout(LayoutType.Banquet);

        var result = layout.AddTable("T1", TableShape.Round, capacity: 8, sortOrder: 0);

        result.IsSuccess.Should().BeTrue();
        layout.Tables.Should().HaveCount(1);
        layout.Tables[0].Seats.Should().BeEmpty();
    }

    [Fact]
    public void AddTable_DuplicateLabel_Should_Fail()
    {
        var layout = NewLayout(LayoutType.Banquet);
        layout.AddTable("T1", TableShape.Round, 8, 0);

        var duplicate = layout.AddTable("T1", TableShape.Round, 10, 1);

        duplicate.IsSuccess.Should().BeFalse();
        duplicate.Error.Should().Contain("already exists");
    }

    [Fact]
    public void AddTable_WithUnknownZoneId_Should_Fail()
    {
        var layout = NewLayout(LayoutType.Banquet);

        var result = layout.AddTable("T1", TableShape.Round, 8, 0, zoneId: Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("zone");
    }

    [Fact]
    public void AddTable_WithExistingZoneId_Should_Succeed()
    {
        var layout = NewLayout(LayoutType.Mixed);
        var zone = layout.AddZone("VIP", "#ff0", 0).Value;

        var result = layout.AddTable("T1", TableShape.Round, 8, 0, zoneId: zone.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.VenueZoneId.Should().Be(zone.Id);
    }

    [Fact]
    public void GenerateRoundTable_Should_Produce_Table_With_Seats()
    {
        var layout = NewLayout(LayoutType.Banquet);

        var result = layout.GenerateRoundTable("T1", capacity: 10, sortOrder: 0);

        result.IsSuccess.Should().BeTrue();
        result.Value.Seats.Should().HaveCount(10);
    }

    [Fact]
    public void GenerateRectTable_OnRoundShape_Should_Fail()
    {
        var layout = NewLayout(LayoutType.Banquet);

        var result = layout.GenerateRectTable("Head", TableShape.Round, 8, 0);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void GenerateRectTable_OnSquare_Should_Produce_Seats_On_All_Sides()
    {
        var layout = NewLayout(LayoutType.Banquet);

        var result = layout.GenerateRectTable("Sq1", TableShape.Square, 8, 0);

        result.IsSuccess.Should().BeTrue();
        result.Value.Seats.Should().HaveCount(8);
    }

    [Fact]
    public void UpdateTable_Should_Modify_Properties()
    {
        var layout = NewLayout(LayoutType.Banquet);
        var table = layout.GenerateRoundTable("T1", 8, 0).Value;

        var result = layout.UpdateTable(table.Id, "VIP-Head", TableShape.Round, 10, 1,
            zoneId: null, geometry: null);

        result.IsSuccess.Should().BeTrue();
        layout.Tables[0].Label.Should().Be("VIP-Head");
        layout.Tables[0].Capacity.Should().Be(10);
    }

    [Fact]
    public void RemoveTable_Should_Remove_Table_From_Collection()
    {
        var layout = NewLayout(LayoutType.Banquet);
        var table = layout.GenerateRoundTable("T1", 8, 0).Value;

        var result = layout.RemoveTable(table.Id);

        result.IsSuccess.Should().BeTrue();
        layout.Tables.Should().BeEmpty();
    }

    [Fact]
    public void GetTable_Should_Return_Null_For_Missing_Id()
    {
        var layout = NewLayout();

        layout.GetTable(Guid.NewGuid()).Should().BeNull();
    }

    #endregion

    #region TotalCapacity across zones + tables

    [Fact]
    public void TotalCapacity_Should_Sum_Zones_And_Tables()
    {
        var layout = NewLayout(LayoutType.Mixed);

        // Zone with 10 theater seats.
        var zone = layout.AddZone("Orchestra", "#f00", 0).Value;
        layout.GenerateTheaterSeats(zone.Id, rows: 1, seatsPerRow: 10);

        // Table with 8 round seats.
        layout.GenerateRoundTable("T1", capacity: 8, sortOrder: 0);

        layout.TotalCapacity.Should().Be(18);
    }

    #endregion

    #region Decorations

    [Fact]
    public void AddDecoration_Should_Add_Item()
    {
        var layout = NewLayout();

        var result = layout.AddDecoration(DecorationKind.Stage, null, 0);

        result.IsSuccess.Should().BeTrue();
        layout.Decorations.Should().HaveCount(1);
    }

    [Fact]
    public void AddDecoration_Text_WithoutLabel_Should_Fail()
    {
        var layout = NewLayout();

        var result = layout.AddDecoration(DecorationKind.Text, null, 0);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void RemoveDecoration_Should_Remove_Item()
    {
        var layout = NewLayout();
        var decoration = layout.AddDecoration(DecorationKind.Stage, null, 0).Value;

        var result = layout.RemoveDecoration(decoration.Id);

        result.IsSuccess.Should().BeTrue();
        layout.Decorations.Should().BeEmpty();
    }

    [Fact]
    public void UpdateDecoration_Should_Modify_Properties()
    {
        var layout = NewLayout();
        var decoration = layout.AddDecoration(DecorationKind.Stage, null, 0).Value;

        var result = layout.UpdateDecoration(decoration.Id, DecorationKind.DanceFloor, "Dance Area", 1,
            geometry: "{\"x\":0,\"y\":0}", properties: null);

        result.IsSuccess.Should().BeTrue();
        decoration.Kind.Should().Be(DecorationKind.DanceFloor);
        decoration.Label.Should().Be("Dance Area");
    }

    #endregion

    #region Event.EnableAssignedSeating / DisableAssignedSeating

    private Event BuildTieredEvent()
    {
        var title = EventTitle.Create("Annual Gala").Value;
        var description = EventDescription.Create("Gala event with seating").Value;

        var evt = Event.Create(
            title,
            description,
            startDate: DateTime.UtcNow.AddDays(30),
            endDate: DateTime.UtcNow.AddDays(30).AddHours(3),
            organizerId: _userId,
            capacity: 500
        ).Value;

        evt.SetTicketingMode(TicketingMode.Tiered);
        return evt;
    }

    [Fact]
    public void EnableAssignedSeating_WithEmptyLayoutId_Should_Throw()
    {
        var evt = BuildTieredEvent();

        Action act = () => evt.EnableAssignedSeating(Guid.Empty);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*persisted VenueLayout*");
    }

    [Fact]
    public void EnableAssignedSeating_OnNonTieredEvent_Should_Fail()
    {
        var title = EventTitle.Create("Simple Event").Value;
        var description = EventDescription.Create("Flat-price event").Value;
        var evt = Event.Create(title, description,
            DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(3),
            _userId, 100).Value;

        var result = evt.EnableAssignedSeating(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("tiered");
    }

    [Fact]
    public void EnableAssignedSeating_Should_Set_Mode_And_LayoutId()
    {
        var evt = BuildTieredEvent();
        var layoutId = Guid.NewGuid();

        var result = evt.EnableAssignedSeating(layoutId);

        result.IsSuccess.Should().BeTrue();
        evt.SeatingMode.Should().Be(SeatingMode.AssignedSeating);
        evt.VenueLayoutId.Should().Be(layoutId);
        evt.HasAssignedSeating.Should().BeTrue();
    }

    [Fact]
    public void DisableAssignedSeating_Should_Reset_To_GA()
    {
        var evt = BuildTieredEvent();
        evt.EnableAssignedSeating(Guid.NewGuid());

        var result = evt.DisableAssignedSeating();

        result.IsSuccess.Should().BeTrue();
        evt.SeatingMode.Should().Be(SeatingMode.GeneralAdmission);
        evt.VenueLayoutId.Should().BeNull();
    }

    [Fact]
    public void DisableAssignedSeating_When_Already_GA_Should_Be_Idempotent()
    {
        var evt = BuildTieredEvent();

        var result = evt.DisableAssignedSeating();

        result.IsSuccess.Should().BeTrue();
        evt.SeatingMode.Should().Be(SeatingMode.GeneralAdmission);
    }

    #endregion

    #region Slice 9.1 Event.CheckLayoutPublishReadiness

    [Fact]
    public void CheckLayoutPublishReadiness_GAEvent_NoLayout_Returns_Success()
    {
        // GA event with no layout — publish-ready (no readiness check applies).
        var title = EventTitle.Create("GA Event").Value;
        var description = EventDescription.Create("General admission only").Value;
        var evt = Event.Create(title, description,
            DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(3),
            _userId, 100).Value;

        var result = evt.CheckLayoutPublishReadiness(layout: null);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CheckLayoutPublishReadiness_GAEvent_LayoutSupplied_Returns_Failure()
    {
        // Defence in depth: caller mistakenly passes a layout for a non-seated event.
        var title = EventTitle.Create("GA Event").Value;
        var description = EventDescription.Create("General admission only").Value;
        var evt = Event.Create(title, description,
            DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(30).AddHours(3),
            _userId, 100).Value;
        var layout = NewLayout();

        var result = evt.CheckLayoutPublishReadiness(layout);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("no venue layout");
    }

    [Fact]
    public void CheckLayoutPublishReadiness_SeatedEvent_LayoutNull_Returns_Failure()
    {
        // Seated event but caller failed to load the layout — fail loudly.
        var evt = BuildTieredEvent();
        evt.EnableAssignedSeating(Guid.NewGuid());

        var result = evt.CheckLayoutPublishReadiness(layout: null);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("but none was supplied");
    }

    [Fact]
    public void CheckLayoutPublishReadiness_LayoutIdMismatch_Returns_Failure()
    {
        // Defence in depth: caller hands the wrong layout aggregate.
        var evt = BuildTieredEvent();
        evt.EnableAssignedSeating(Guid.NewGuid());          // event references some layout id
        var differentLayout = NewLayout();                  // distinct id

        var result = evt.CheckLayoutPublishReadiness(differentLayout);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Layout id mismatch");
    }

    [Fact]
    public void CheckLayoutPublishReadiness_LayoutWithUnmappedZone_Returns_Failure_StrictValidation()
    {
        // Slice 9.1: publish-readiness uses ValidateForEvent(requireTierMapping=true).
        // A zone with no tier_assignment must fail at publish even though it's allowed
        // at apply-preset time.
        var evt = BuildTieredEvent();
        var layout = NewLayout();
        layout.AddZone("Main Floor", "#3b82f6", 1);
        evt.EnableAssignedSeating(layout.Id);

        var result = evt.CheckLayoutPublishReadiness(layout);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("must be mapped to a ticket tier");
    }

    #endregion
}
