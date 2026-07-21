using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.SharedKernel.Money;

namespace LankaConnect.Domain.Tests.Events.Entities;

/// <summary>
/// Tests for Ticket.CreateTiered() â€” master and individual tiered tickets
/// </summary>
public class TicketTieredTests
{
    private readonly Guid _registrationId = Guid.NewGuid();
    private readonly Guid _eventId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _eventEndDate = DateTime.UtcNow.AddDays(7);

    #region CreateTiered â€” Master Ticket Tests

    [Fact]
    public void CreateTiered_Master_WithValidData_Should_Succeed()
    {
        var result = Ticket.CreateTiered(
            _registrationId, _eventId, _userId, _eventEndDate,
            "VIP", TicketCategory.Master,
            attendeeIndex: null,
            attendeeNames: "John Doe, Jane Doe");

        result.IsSuccess.Should().BeTrue();
        var ticket = result.Value;
        ticket.TicketTierName.Should().Be("VIP");
        ticket.TicketCategory.Should().Be(TicketCategory.Master);
        ticket.AttendeeIndex.Should().BeNull();
        ticket.AttendeeNames.Should().Be("John Doe, Jane Doe");
        ticket.IsValid.Should().BeTrue();
        ticket.TicketCode.Should().StartWith("LC-");
    }

    [Fact]
    public void CreateTiered_Master_WithoutAttendeeNames_Should_Fail()
    {
        var result = Ticket.CreateTiered(
            _registrationId, _eventId, _userId, _eventEndDate,
            "VIP", TicketCategory.Master,
            attendeeNames: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Attendee names are required for master tickets");
    }

    #endregion

    #region CreateTiered â€” Individual Ticket Tests

    [Fact]
    public void CreateTiered_Individual_WithValidData_Should_Succeed()
    {
        var result = Ticket.CreateTiered(
            _registrationId, _eventId, _userId, _eventEndDate,
            "VIP", TicketCategory.Individual,
            attendeeIndex: 0);

        result.IsSuccess.Should().BeTrue();
        var ticket = result.Value;
        ticket.TicketTierName.Should().Be("VIP");
        ticket.TicketCategory.Should().Be(TicketCategory.Individual);
        ticket.AttendeeIndex.Should().Be(0);
        ticket.AttendeeNames.Should().BeNull();
    }

    [Fact]
    public void CreateTiered_Individual_WithoutAttendeeIndex_Should_Fail()
    {
        var result = Ticket.CreateTiered(
            _registrationId, _eventId, _userId, _eventEndDate,
            "VIP", TicketCategory.Individual,
            attendeeIndex: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Attendee index is required for individual tickets");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void CreateTiered_WithEmptyTierName_Should_Fail()
    {
        var result = Ticket.CreateTiered(
            _registrationId, _eventId, _userId, _eventEndDate,
            "", TicketCategory.Master,
            attendeeNames: "John Doe");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Ticket tier name is required");
    }

    [Fact]
    public void CreateTiered_WithStandardCategory_Should_Fail()
    {
        var result = Ticket.CreateTiered(
            _registrationId, _eventId, _userId, _eventEndDate,
            "VIP", TicketCategory.Standard);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Use Create() for standard tickets");
    }

    [Fact]
    public void CreateTiered_WithEmptyRegistrationId_Should_Fail()
    {
        var result = Ticket.CreateTiered(
            Guid.Empty, _eventId, _userId, _eventEndDate,
            "VIP", TicketCategory.Master,
            attendeeNames: "John Doe");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Registration ID is required");
    }

    [Fact]
    public void CreateTiered_WithEmptyEventId_Should_Fail()
    {
        var result = Ticket.CreateTiered(
            _registrationId, Guid.Empty, _userId, _eventEndDate,
            "VIP", TicketCategory.Master,
            attendeeNames: "John Doe");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event ID is required");
    }

    #endregion

    #region Legacy Create Still Works

    [Fact]
    public void Create_Legacy_ShouldHaveStandardCategory()
    {
        var result = Ticket.Create(_registrationId, _eventId, _userId, _eventEndDate);

        result.IsSuccess.Should().BeTrue();
        result.Value.TicketCategory.Should().Be(TicketCategory.Standard);
        result.Value.TicketTierName.Should().BeNull();
        result.Value.AttendeeIndex.Should().BeNull();
        result.Value.AttendeeNames.Should().BeNull();
    }

    #endregion
}
