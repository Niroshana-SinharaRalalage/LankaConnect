using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Tests.Events.Entities;

public class VenueLayoutTests
{
    private readonly Guid _userId = Guid.NewGuid();

    #region Create Tests

    [Fact]
    public void Create_WithValidData_Should_Return_Success()
    {
        var result = VenueLayout.Create("Main Hall", LayoutType.Theater, _userId);

        result.IsSuccess.Should().BeTrue();
        var layout = result.Value;
        layout.Name.Should().Be("Main Hall");
        layout.LayoutType.Should().Be(LayoutType.Theater);
        layout.CreatedByUserId.Should().Be(_userId);
        layout.EventId.Should().BeNull();
        layout.IsTemplate.Should().BeFalse();
        layout.Zones.Should().BeEmpty();
        layout.TotalCapacity.Should().Be(0);
    }

    [Fact]
    public void Create_WithEventId_Should_Set_EventId()
    {
        var eventId = Guid.NewGuid();
        var result = VenueLayout.Create("Hall A", LayoutType.Banquet, _userId, eventId);

        result.IsSuccess.Should().BeTrue();
        result.Value.EventId.Should().Be(eventId);
    }

    [Fact]
    public void Create_AsTemplate_Should_Set_IsTemplate()
    {
        var result = VenueLayout.Create("Template 1", LayoutType.Theater, _userId, isTemplate: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsTemplate.Should().BeTrue();
    }

    [Fact]
    public void Create_AsTemplate_WithEventId_Should_Fail()
    {
        var result = VenueLayout.Create("Template 1", LayoutType.Theater, _userId, Guid.NewGuid(), isTemplate: true);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Templates cannot be assigned");
    }

    [Fact]
    public void Create_WithEmptyName_Should_Fail()
    {
        var result = VenueLayout.Create("", LayoutType.Theater, _userId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("name is required");
    }

    [Fact]
    public void Create_WithTooLongName_Should_Fail()
    {
        var result = VenueLayout.Create(new string('x', 201), LayoutType.Theater, _userId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("200 characters");
    }

    [Fact]
    public void Create_WithEmptyUserId_Should_Fail()
    {
        var result = VenueLayout.Create("Hall", LayoutType.Theater, Guid.Empty);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Creator user ID");
    }

    [Fact]
    public void Create_Should_Trim_Name()
    {
        var result = VenueLayout.Create("  Main Hall  ", LayoutType.Theater, _userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Main Hall");
    }

    #endregion

    #region Zone Management Tests

    [Fact]
    public void AddZone_Should_Add_Zone_To_Layout()
    {
        var layout = CreateValidLayout();

        var result = layout.AddZone("VIP Section", "#FF0000", 1);

        result.IsSuccess.Should().BeTrue();
        layout.Zones.Should().HaveCount(1);
        layout.Zones[0].Name.Should().Be("VIP Section");
    }

    [Fact]
    public void AddZone_WithDuplicateName_Should_Fail()
    {
        var layout = CreateValidLayout();
        layout.AddZone("VIP", "#FF0000", 1);

        var result = layout.AddZone("VIP", "#00FF00", 2);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public void AddZone_WithDuplicateName_CaseInsensitive_Should_Fail()
    {
        var layout = CreateValidLayout();
        layout.AddZone("VIP", "#FF0000", 1);

        var result = layout.AddZone("vip", "#00FF00", 2);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public void UpdateZone_Should_Update_Properties()
    {
        var layout = CreateValidLayout();
        var zone = layout.AddZone("Old Name", "#000000", 1).Value;

        var result = layout.UpdateZone(zone.Id, "New Name", "#FFFFFF", 2);

        result.IsSuccess.Should().BeTrue();
        layout.Zones[0].Name.Should().Be("New Name");
        layout.Zones[0].Color.Should().Be("#FFFFFF");
        layout.Zones[0].SortOrder.Should().Be(2);
    }

    [Fact]
    public void UpdateZone_WithNonExistentId_Should_Fail()
    {
        var layout = CreateValidLayout();

        var result = layout.UpdateZone(Guid.NewGuid(), "Name", "#FFF", 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public void UpdateZone_WithDuplicateName_Of_OtherZone_Should_Fail()
    {
        var layout = CreateValidLayout();
        layout.AddZone("Zone A", "#FF0000", 1);
        var zoneB = layout.AddZone("Zone B", "#00FF00", 2).Value;

        var result = layout.UpdateZone(zoneB.Id, "Zone A", "#00FF00", 2);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public void RemoveZone_Should_Remove_Zone()
    {
        var layout = CreateValidLayout();
        var zone = layout.AddZone("VIP", "#FF0000", 1).Value;

        var result = layout.RemoveZone(zone.Id);

        result.IsSuccess.Should().BeTrue();
        layout.Zones.Should().BeEmpty();
    }

    [Fact]
    public void RemoveZone_WithNonExistentId_Should_Fail()
    {
        var layout = CreateValidLayout();

        var result = layout.RemoveZone(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public void GetZone_Should_Return_Zone_By_Id()
    {
        var layout = CreateValidLayout();
        var zone = layout.AddZone("VIP", "#FF0000", 1).Value;

        var found = layout.GetZone(zone.Id);

        found.Should().NotBeNull();
        found!.Name.Should().Be("VIP");
    }

    [Fact]
    public void GetZone_WithNonExistentId_Should_Return_Null()
    {
        var layout = CreateValidLayout();

        var found = layout.GetZone(Guid.NewGuid());

        found.Should().BeNull();
    }

    #endregion

    #region Theater Seat Generation Tests

    [Fact]
    public void GenerateTheaterSeats_Should_Create_Correct_Seats()
    {
        var layout = CreateValidLayout();
        var zone = layout.AddZone("Main Floor", "#FF0000", 1).Value;

        var result = layout.GenerateTheaterSeats(zone.Id, rows: 3, seatsPerRow: 4);

        result.IsSuccess.Should().BeTrue();
        zone.Seats.Should().HaveCount(12);
        zone.EnabledSeatCount.Should().Be(12);
        layout.TotalCapacity.Should().Be(12);
    }

    [Fact]
    public void GenerateTheaterSeats_OnPopulatedZone_Should_Fail()
    {
        // Slice 9.5 contract: refusing to regenerate seats on an already-populated
        // zone is a defence-in-depth check (the canvas-editor UI also gates the
        // seat-gen panel on totalSeatCount === 0). Removing existing seats via
        // the domain orphans them due to the optional XOR-with-VenueTableId FK,
        // which then violates the Postgres ck_seats_zone_xor_table CHECK at
        // SaveChanges. Refusing fast keeps the error precise + clean.
        var layout = CreateValidLayout();
        var zone = layout.AddZone("Main Floor", "#FF0000", 1).Value;
        layout.GenerateTheaterSeats(zone.Id, rows: 3, seatsPerRow: 4); // populate first

        var result = layout.GenerateTheaterSeats(zone.Id, rows: 5, seatsPerRow: 5);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already has");
        zone.Seats.Should().HaveCount(12); // original seats preserved
    }

    [Fact]
    public void GenerateTheaterSeats_Should_Label_Correctly()
    {
        var layout = CreateValidLayout();
        var zone = layout.AddZone("Floor", "#FF0000", 1).Value;

        layout.GenerateTheaterSeats(zone.Id, rows: 2, seatsPerRow: 3);

        zone.Seats[0].Label.Should().Be("A1");
        zone.Seats[1].Label.Should().Be("A2");
        zone.Seats[2].Label.Should().Be("A3");
        zone.Seats[3].Label.Should().Be("B1");
        zone.Seats[4].Label.Should().Be("B2");
        zone.Seats[5].Label.Should().Be("B3");
    }

    [Fact]
    public void GenerateTheaterSeats_WithCustomStartRow_Should_Start_From_Given_Label()
    {
        var layout = CreateValidLayout();
        var zone = layout.AddZone("Balcony", "#FF0000", 1).Value;

        layout.GenerateTheaterSeats(zone.Id, rows: 2, seatsPerRow: 2, startRowLabel: "C");

        zone.Seats[0].Label.Should().Be("C1");
        zone.Seats[1].Label.Should().Be("C2");
        zone.Seats[2].Label.Should().Be("D1");
        zone.Seats[3].Label.Should().Be("D2");
    }

    [Fact]
    public void GenerateTheaterSeats_Should_Refuse_Regeneration_On_Populated_Zone()
    {
        // Slice 9.5 contract update: regenerating seats on a populated zone is
        // refused. Background: the optional XOR-with-VenueTableId FK means EF
        // Core orphans removed seats by setting VenueZoneId=null, which violates
        // the Postgres ck_seats_zone_xor_table CHECK at SaveChanges. Domain-level
        // refusal is defence-in-depth; the canvas-editor UI also gates the seat-
        // gen panel on totalSeatCount === 0.
        var layout = CreateValidLayout();
        var zone = layout.AddZone("Floor", "#FF0000", 1).Value;
        layout.GenerateTheaterSeats(zone.Id, rows: 5, seatsPerRow: 5); // 25 seats
        zone.Seats.Should().HaveCount(25);

        var result = layout.GenerateTheaterSeats(zone.Id, rows: 2, seatsPerRow: 3);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already has");
        zone.Seats.Should().HaveCount(25); // original seats preserved (not 6, not 0)
    }

    [Fact]
    public void GenerateTheaterSeats_WithInvalidZoneId_Should_Fail()
    {
        var layout = CreateValidLayout();

        var result = layout.GenerateTheaterSeats(Guid.NewGuid(), rows: 2, seatsPerRow: 3);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public void GenerateTheaterSeats_WithZeroRows_Should_Fail()
    {
        var layout = CreateValidLayout();
        var zone = layout.AddZone("Floor", "#FF0000", 1).Value;

        var result = layout.GenerateTheaterSeats(zone.Id, rows: 0, seatsPerRow: 3);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("rows");
    }

    [Fact]
    public void GenerateTheaterSeats_WithTooManyRows_Should_Fail()
    {
        var layout = CreateValidLayout();
        var zone = layout.AddZone("Floor", "#FF0000", 1).Value;

        var result = layout.GenerateTheaterSeats(zone.Id, rows: 101, seatsPerRow: 3);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("100");
    }

    [Fact]
    public void GenerateTheaterSeats_WithZeroSeatsPerRow_Should_Fail()
    {
        var layout = CreateValidLayout();
        var zone = layout.AddZone("Floor", "#FF0000", 1).Value;

        var result = layout.GenerateTheaterSeats(zone.Id, rows: 2, seatsPerRow: 0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Seats per row");
    }

    #endregion

    #region Banquet Seat Generation Tests

    [Fact]
    public void GenerateBanquetSeats_Should_Create_Correct_Seats()
    {
        var layout = CreateValidLayout(LayoutType.Banquet);
        var zone = layout.AddZone("Dining Hall", "#00FF00", 1).Value;

        var result = layout.GenerateBanquetSeats(zone.Id, tables: 3, seatsPerTable: 8);

        result.IsSuccess.Should().BeTrue();
        zone.Seats.Should().HaveCount(24);
    }

    [Fact]
    public void GenerateBanquetSeats_Should_Label_Correctly()
    {
        var layout = CreateValidLayout(LayoutType.Banquet);
        var zone = layout.AddZone("Hall", "#00FF00", 1).Value;

        layout.GenerateBanquetSeats(zone.Id, tables: 2, seatsPerTable: 3);

        zone.Seats[0].Label.Should().Be("T1-S1");
        zone.Seats[1].Label.Should().Be("T1-S2");
        zone.Seats[2].Label.Should().Be("T1-S3");
        zone.Seats[3].Label.Should().Be("T2-S1");
        zone.Seats[4].Label.Should().Be("T2-S2");
        zone.Seats[5].Label.Should().Be("T2-S3");
    }

    [Fact]
    public void GenerateBanquetSeats_WithCustomStartTable_Should_Start_Correctly()
    {
        var layout = CreateValidLayout(LayoutType.Banquet);
        var zone = layout.AddZone("Hall", "#00FF00", 1).Value;

        layout.GenerateBanquetSeats(zone.Id, tables: 2, seatsPerTable: 2, startTableNumber: 5);

        zone.Seats[0].Label.Should().Be("T5-S1");
        zone.Seats[2].Label.Should().Be("T6-S1");
    }

    [Fact]
    public void GenerateBanquetSeats_WithTooManyTables_Should_Fail()
    {
        var layout = CreateValidLayout(LayoutType.Banquet);
        var zone = layout.AddZone("Hall", "#00FF00", 1).Value;

        var result = layout.GenerateBanquetSeats(zone.Id, tables: 201, seatsPerTable: 8);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("200");
    }

    [Fact]
    public void GenerateBanquetSeats_WithTooManySeatsPerTable_Should_Fail()
    {
        var layout = CreateValidLayout(LayoutType.Banquet);
        var zone = layout.AddZone("Hall", "#00FF00", 1).Value;

        var result = layout.GenerateBanquetSeats(zone.Id, tables: 5, seatsPerTable: 21);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("20");
    }

    #endregion

    #region Seat Management Tests

    [Fact]
    public void DisableSeat_Should_Disable_Seat_And_Update_Capacity()
    {
        var layout = CreateValidLayout();
        var zone = layout.AddZone("Floor", "#FF0000", 1).Value;
        layout.GenerateTheaterSeats(zone.Id, rows: 2, seatsPerRow: 3);
        var seatId = zone.Seats[0].Id;
        layout.TotalCapacity.Should().Be(6);

        var result = layout.DisableSeat(seatId);

        result.IsSuccess.Should().BeTrue();
        zone.Seats[0].IsEnabled.Should().BeFalse();
        zone.EnabledSeatCount.Should().Be(5);
        layout.TotalCapacity.Should().Be(5);
    }

    [Fact]
    public void EnableSeat_Should_Enable_Seat()
    {
        var layout = CreateValidLayout();
        var zone = layout.AddZone("Floor", "#FF0000", 1).Value;
        layout.GenerateTheaterSeats(zone.Id, rows: 1, seatsPerRow: 2);
        var seatId = zone.Seats[0].Id;
        layout.DisableSeat(seatId);

        var result = layout.EnableSeat(seatId);

        result.IsSuccess.Should().BeTrue();
        zone.Seats[0].IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void SetSeatAccessible_Should_Set_ADA_Flag()
    {
        var layout = CreateValidLayout();
        var zone = layout.AddZone("Floor", "#FF0000", 1).Value;
        layout.GenerateTheaterSeats(zone.Id, rows: 1, seatsPerRow: 2);
        var seatId = zone.Seats[0].Id;

        var result = layout.SetSeatAccessible(seatId, true);

        result.IsSuccess.Should().BeTrue();
        zone.Seats[0].IsAccessible.Should().BeTrue();
    }

    [Fact]
    public void DisableSeat_WithNonExistentId_Should_Fail()
    {
        var layout = CreateValidLayout();

        var result = layout.DisableSeat(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    #endregion

    #region Event Assignment Tests

    [Fact]
    public void AssignToEvent_Should_Set_EventId()
    {
        var layout = CreateValidLayout();
        var eventId = Guid.NewGuid();

        var result = layout.AssignToEvent(eventId);

        result.IsSuccess.Should().BeTrue();
        layout.EventId.Should().Be(eventId);
    }

    [Fact]
    public void AssignToEvent_WithEmptyId_Should_Fail()
    {
        var layout = CreateValidLayout();

        var result = layout.AssignToEvent(Guid.Empty);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Event ID");
    }

    [Fact]
    public void AssignToEvent_WhenTemplate_Should_Fail()
    {
        var layout = VenueLayout.Create("Template", LayoutType.Theater, _userId, isTemplate: true).Value;

        var result = layout.AssignToEvent(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("template");
    }

    [Fact]
    public void AssignToEvent_WhenAlreadyAssignedToDifferentEvent_Should_Fail()
    {
        var layout = CreateValidLayout();
        layout.AssignToEvent(Guid.NewGuid());

        var result = layout.AssignToEvent(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already assigned");
    }

    [Fact]
    public void AssignToEvent_WhenAlreadyAssignedToSameEvent_Should_Succeed()
    {
        var layout = CreateValidLayout();
        var eventId = Guid.NewGuid();
        layout.AssignToEvent(eventId);

        var result = layout.AssignToEvent(eventId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void DetachFromEvent_Should_Clear_EventId()
    {
        var layout = CreateValidLayout();
        layout.AssignToEvent(Guid.NewGuid());

        var result = layout.DetachFromEvent();

        result.IsSuccess.Should().BeTrue();
        layout.EventId.Should().BeNull();
    }

    [Fact]
    public void DetachFromEvent_WhenNotAssigned_Should_Fail()
    {
        var layout = CreateValidLayout();

        var result = layout.DetachFromEvent();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not assigned");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void ValidateForEvent_WithNoZones_Should_Fail()
    {
        var layout = CreateValidLayout();
        var tiers = CreateTestTiers();

        var result = layout.ValidateForEvent(tiers);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("at least one zone");
    }

    [Fact]
    public void ValidateForEvent_WithUnmappedZone_Should_Fail()
    {
        var layout = CreateValidLayout();
        layout.AddZone("VIP", "#FF0000", 1);
        var tiers = CreateTestTiers();
        // Note: No tier.AssignToZone() call — zone is unmapped

        var result = layout.ValidateForEvent(tiers);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("must be mapped");
    }

    [Fact]
    public void ValidateForEvent_WithValidMapping_Should_Succeed()
    {
        var layout = CreateValidLayout();
        var tiers = CreateTestTiers();
        var zone = layout.AddZone("VIP Section", "#FF0000", 1).Value;
        tiers[0].AssignToZone(zone.Id);
        layout.GenerateTheaterSeats(zone.Id, rows: 2, seatsPerRow: 5); // 10 seats, tier capacity is 30

        var result = layout.ValidateForEvent(tiers);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateForEvent_WithZoneSeatCountExceedingTierCapacity_Should_Fail()
    {
        var layout = CreateValidLayout();
        var tiers = CreateTestTiers(); // Capacity: 30 for first tier
        var zone = layout.AddZone("VIP", "#FF0000", 1).Value;
        tiers[0].AssignToZone(zone.Id);
        layout.GenerateTheaterSeats(zone.Id, rows: 10, seatsPerRow: 10); // 100 seats, tier cap is 30

        var result = layout.ValidateForEvent(tiers);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("enabled seats");
    }

    // ────── Slice 9.1: requireTierMapping flag ──────

    [Fact]
    public void ValidateForEvent_RequireTierMappingFalse_UnmappedZone_Should_Succeed()
    {
        // Apply-preset / apply-template path: zones arrive without tier_assignments.
        // The permissive flag must allow this.
        var layout = CreateValidLayout();
        layout.AddZone("Main Floor", "#3b82f6", 1);
        var tiers = CreateTestTiers();

        var result = layout.ValidateForEvent(tiers, requireTierMapping: false);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateForEvent_RequireTierMappingFalse_StillEnforcesCapacityForMappedZones()
    {
        // Even in permissive mode, a zone that IS mapped must respect the tier capacity.
        // This guarantees apply-preset never accepts an inconsistent state.
        var layout = CreateValidLayout();
        var tiers = CreateTestTiers();
        var zone = layout.AddZone("VIP", "#FF0000", 1).Value;
        tiers[0].AssignToZone(zone.Id);
        layout.GenerateTheaterSeats(zone.Id, rows: 10, seatsPerRow: 10); // 100 seats vs cap 30

        var result = layout.ValidateForEvent(tiers, requireTierMapping: false);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("enabled seats");
    }

    [Fact]
    public void ValidateForEvent_BanquetLayoutWithTablesOnlyNoZones_Should_Succeed()
    {
        // Slice 9.4 follow-up fix: banquet presets (round tables, square tables, etc.)
        // legitimately have NO zones — seats live directly on the tables. The original
        // "≥1 zone" check rejected every banquet apply-preset request. Validation must
        // accept zones OR tables.
        var layout = CreateValidLayout();
        var tiers = CreateTestTiers();
        var addTableResult = layout.GenerateRoundTable("Table 1", capacity: 8, sortOrder: 1);
        addTableResult.IsSuccess.Should().BeTrue();

        var result = layout.ValidateForEvent(tiers, requireTierMapping: false);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateForEvent_EmptyLayout_NoZonesAndNoTables_Should_Fail()
    {
        // Regression guard for the truly-empty case — previously the only failure
        // path. Now updated message to match new contract.
        var layout = CreateValidLayout();
        var tiers = CreateTestTiers();

        var result = layout.ValidateForEvent(tiers);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("at least one zone or table");
    }

    [Fact]
    public void ValidateForEvent_RequireTierMappingTrue_IsDefault_PreservesExistingBehaviour()
    {
        // Default-true keeps every existing caller's strict behaviour. Regression guard.
        var layout = CreateValidLayout();
        layout.AddZone("VIP", "#FF0000", 1);
        var tiers = CreateTestTiers();

        // No flag passed — defaults to requireTierMapping=true.
        var result = layout.ValidateForEvent(tiers);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("must be mapped");
    }

    #endregion

    #region BuildPublishReadinessReport Tests (Slice S4)

    [Fact]
    public void BuildPublishReadinessReport_EmptyLayout_Should_Block_With_LayoutEmptyCode()
    {
        var layout = CreateValidLayout();
        var tiers = CreateTestTiers();

        var report = layout.BuildPublishReadinessReport(tiers);

        report.IsPublishReady.Should().BeFalse();
        report.Blockers.Should().ContainSingle(b => b.Code == PublishReadinessCode.LayoutEmpty);
    }

    [Fact]
    public void BuildPublishReadinessReport_ZoneWithSeatsButNoTier_Should_Block_With_ZoneUnmapped()
    {
        var layout = CreateValidLayout();
        var tiers = CreateTestTiers();
        var zone = layout.AddZone("Main Floor", "#3b82f6", 1).Value;
        layout.GenerateTheaterSeats(zone.Id, rows: 2, seatsPerRow: 5);

        var report = layout.BuildPublishReadinessReport(tiers);

        report.IsPublishReady.Should().BeFalse();
        var blocker = report.Blockers.Should().ContainSingle(b =>
            b.Code == PublishReadinessCode.ZoneUnmapped).Subject;
        blocker.ShapeId.Should().Be(zone.Id);
        blocker.ShapeName.Should().Be("Main Floor");
    }

    [Fact]
    public void BuildPublishReadinessReport_EmptyZoneNoTier_Should_Warn_Not_Block()
    {
        var layout = CreateValidLayout();
        var tiers = CreateTestTiers();
        layout.AddZone("Empty Zone", "#3b82f6", 1);

        var report = layout.BuildPublishReadinessReport(tiers);

        report.Blockers.Should().NotContain(b => b.Code == PublishReadinessCode.ZoneUnmapped);
        report.Warnings.Should().Contain(w => w.Code == PublishReadinessCode.ZoneEmptyAndUnmapped);
    }

    [Fact]
    public void BuildPublishReadinessReport_ZoneOverCapacity_Should_Block()
    {
        var layout = CreateValidLayout();
        var tiers = CreateTestTiers(); // first tier capacity = 30
        var zone = layout.AddZone("VIP", "#FF0000", 1).Value;
        tiers[0].AssignToZone(zone.Id);
        layout.GenerateTheaterSeats(zone.Id, rows: 10, seatsPerRow: 10); // 100 > 30

        var report = layout.BuildPublishReadinessReport(tiers);

        report.IsPublishReady.Should().BeFalse();
        report.Blockers.Should().Contain(b =>
            b.Code == PublishReadinessCode.ZoneOverCapacity);
    }

    [Fact]
    public void BuildPublishReadinessReport_TierWithoutMapping_Should_Warn_With_TierWithoutMapping()
    {
        var layout = CreateValidLayout();
        var tiers = CreateTestTiers();
        // Zone exists, mapped to tier 0; tier 1 has no mapping
        var zone = layout.AddZone("VIP", "#FF0000", 1).Value;
        tiers[0].AssignToZone(zone.Id);
        layout.GenerateTheaterSeats(zone.Id, rows: 1, seatsPerRow: 5);

        var report = layout.BuildPublishReadinessReport(tiers);

        report.Warnings.Should().Contain(w =>
            w.Code == PublishReadinessCode.TierWithoutMapping
            && w.TierId == tiers[1].Id);
        // Layout itself is publish-ready — no blockers, only the tier-coverage warning.
        report.IsPublishReady.Should().BeTrue();
    }

    [Fact]
    public void BuildPublishReadinessReport_TierTotalOverCapacity_Should_Block()
    {
        var layout = CreateValidLayout();
        var tiers = CreateTestTiers(); // tier 0 capacity = 30
        // Two zones each at 20 seats, both mapped to tier 0 — 40 total > 30 capacity
        var zoneA = layout.AddZone("VIP A", "#FF0000", 1).Value;
        var zoneB = layout.AddZone("VIP B", "#FFAA00", 2).Value;
        tiers[0].AssignToZone(zoneA.Id);
        tiers[0].AssignToZone(zoneB.Id);
        layout.GenerateTheaterSeats(zoneA.Id, rows: 2, seatsPerRow: 10); // 20
        layout.GenerateTheaterSeats(zoneB.Id, rows: 2, seatsPerRow: 10); // 20

        var report = layout.BuildPublishReadinessReport(tiers);

        report.IsPublishReady.Should().BeFalse();
        report.Blockers.Should().Contain(b =>
            b.Code == PublishReadinessCode.TierTotalOverCapacity
            && b.TierId == tiers[0].Id);
    }

    [Fact]
    public void BuildPublishReadinessReport_TableWithSeatsButNoTier_Should_Block_With_TableUnmapped()
    {
        var layout = CreateValidLayout();
        var tiers = CreateTestTiers();
        var table = layout.GenerateRoundTable("Table 1", capacity: 8, sortOrder: 1).Value;

        var report = layout.BuildPublishReadinessReport(tiers);

        var blocker = report.Blockers.Should().ContainSingle(b =>
            b.Code == PublishReadinessCode.TableUnmapped).Subject;
        blocker.ShapeId.Should().Be(table.Id);
    }

    [Fact]
    public void BuildPublishReadinessReport_FullyMappedHappyPath_Should_Be_PublishReady()
    {
        var layout = CreateValidLayout();
        var tiers = CreateTestTiers();
        var zone = layout.AddZone("VIP", "#FF0000", 1).Value;
        var table = layout.GenerateRoundTable("Banquet 1", capacity: 8, sortOrder: 2).Value;
        tiers[0].AssignToZone(zone.Id);
        tiers[1].AssignToTable(table.Id);
        layout.GenerateTheaterSeats(zone.Id, rows: 2, seatsPerRow: 10); // 20 ≤ 30

        var report = layout.BuildPublishReadinessReport(tiers);

        report.IsPublishReady.Should().BeTrue();
        report.Blockers.Should().BeEmpty();
        report.Warnings.Should().BeEmpty();
        report.TierSummary.Should().HaveCount(2);
        report.TierSummary.Should().ContainSingle(s =>
            s.TierId == tiers[0].Id
            && s.MappedZones.Count == 1
            && s.TotalEnabledSeats == 20);
        report.TierSummary.Should().ContainSingle(s =>
            s.TierId == tiers[1].Id
            && s.MappedTables.Count == 1
            && s.TotalEnabledSeats == 8);
    }

    [Fact]
    public void BuildPublishReadinessReport_EnumeratesAllIssues_NotShortCircuit()
    {
        // Architect requirement: report must list every issue at once, unlike
        // ValidateForEvent which short-circuits on the first failure.
        var layout = CreateValidLayout();
        var tiers = CreateTestTiers();

        var zoneA = layout.AddZone("Unmapped A", "#FF0000", 1).Value;
        layout.GenerateTheaterSeats(zoneA.Id, rows: 1, seatsPerRow: 3);
        var zoneB = layout.AddZone("Unmapped B", "#00FF00", 2).Value;
        layout.GenerateTheaterSeats(zoneB.Id, rows: 1, seatsPerRow: 3);
        layout.GenerateRoundTable("Unmapped Table", capacity: 4, sortOrder: 3);

        var report = layout.BuildPublishReadinessReport(tiers);

        report.Blockers.Count(b => b.Code == PublishReadinessCode.ZoneUnmapped).Should().Be(2);
        report.Blockers.Should().Contain(b => b.Code == PublishReadinessCode.TableUnmapped);
        // Both tiers have no mappings → 2 TierWithoutMapping warnings.
        report.Warnings.Count(w => w.Code == PublishReadinessCode.TierWithoutMapping).Should().Be(2);
    }

    #endregion

    #region UpdateName Tests

    [Fact]
    public void UpdateName_WithValidName_Should_Succeed()
    {
        var layout = CreateValidLayout();

        var result = layout.UpdateName("New Name");

        result.IsSuccess.Should().BeTrue();
        layout.Name.Should().Be("New Name");
    }

    [Fact]
    public void UpdateName_WithEmptyName_Should_Fail()
    {
        var layout = CreateValidLayout();

        var result = layout.UpdateName("");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void UpdateName_Should_Trim()
    {
        var layout = CreateValidLayout();

        layout.UpdateName("  Trimmed  ");

        layout.Name.Should().Be("Trimmed");
    }

    #endregion

    #region Helpers

    private VenueLayout CreateValidLayout(LayoutType type = LayoutType.Theater)
    {
        return VenueLayout.Create("Test Layout", type, _userId).Value;
    }

    private IReadOnlyList<TicketTier> CreateTestTiers()
    {
        var adultPrice = Money.Create(150.00m, Currency.USD).Value;
        var tier1 = TicketTier.Create(Guid.NewGuid(), "VIP", "VIP section", adultPrice, null, null, 30, 10, 1).Value;
        var tier2 = TicketTier.Create(Guid.NewGuid(), "Basic", "Standard", Money.Create(50.00m, Currency.USD).Value, null, null, 50, 10, 2).Value;
        return new List<TicketTier> { tier1, tier2 };
    }

    #endregion
}
