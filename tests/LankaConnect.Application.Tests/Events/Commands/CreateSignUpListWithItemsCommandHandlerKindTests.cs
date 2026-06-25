using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.CreateSignUpListWithItems;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Phase 7D.1: Tests for the Kind-branch introduced into
/// CreateSignUpListWithItemsCommandHandler. The legacy Items path is already
/// exercised through integration paths elsewhere — these tests focus on the
/// Kind=Volunteers branch and back-compat default.
/// </summary>
public class CreateSignUpListWithItemsCommandHandlerKindTests
{
    private readonly Mock<IEventRepository> _eventRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<CreateSignUpListWithItemsCommandHandler>> _logger = new();

    private CreateSignUpListWithItemsCommandHandler BuildHandler() =>
        new(_eventRepository.Object, _unitOfWork.Object, _logger.Object);

    private static Event NewEvent()
    {
        var title = EventTitle.Create("Kind Branch Test Event").Value;
        var description = EventDescription.Create("Covers SignUpKind routing in the handler").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);
        var address = Address.Create("1 Test St", "Colombo", "WP", "00100", "Sri Lanka").Value;
        var location = EventLocation.Create(address).Value;
        var price = Money.Create(0m, Currency.USD).Value;
        return Event.Create(title, description, startDate, endDate, Guid.NewGuid(), 100, location, ticketPrice: price).Value;
    }

    [Fact]
    public async Task Handle_KindVolunteers_AllSlotItems_CreatesVolunteerList()
    {
        var @event = NewEvent();
        var command = new CreateSignUpListWithItemsCommand(
            EventId: @event.Id,
            Category: "Cleanup Crew",
            Description: "Post-event cleanup volunteers",
            HasMandatoryItems: true,
            HasPreferredItems: false,
            HasSuggestedItems: false,
            Items: new List<SignUpItemDto>
            {
                new("Sweepers", SignUpItemType.Slot, SignUpItemCategory.Mandatory, AvailableSlots: 4),
                new("Trash haulers", SignUpItemType.Slot, SignUpItemCategory.Mandatory, AvailableSlots: 2),
            },
            HasOpenItems: false,
            Kind: SignUpKind.Volunteers);

        _eventRepository.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);
        _unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue($"expected success but got: {result.Error}");
        @event.SignUpLists.Should().ContainSingle();
        var list = @event.SignUpLists.Single();
        list.Kind.Should().Be(SignUpKind.Volunteers);
        list.Items.Should().OnlyContain(i => i.ItemType == SignUpItemType.Slot);
        list.Items.First(i => i.ItemDescription == "Sweepers").AvailableSlots.Should().Be(4);
    }

    [Fact]
    public async Task Handle_KindVolunteers_WithQuantityItem_ReturnsFailure()
    {
        var @event = NewEvent();
        var command = new CreateSignUpListWithItemsCommand(
            EventId: @event.Id,
            Category: "Mixed Up",
            Description: "Should fail — quantity item in volunteer list",
            HasMandatoryItems: true,
            HasPreferredItems: false,
            HasSuggestedItems: false,
            Items: new List<SignUpItemDto>
            {
                new("Volunteers", SignUpItemType.Slot, SignUpItemCategory.Mandatory, AvailableSlots: 3),
                new("Rice bags", SignUpItemType.Quantity, SignUpItemCategory.Mandatory, TargetQuantity: 10),
            },
            HasOpenItems: false,
            Kind: SignUpKind.Volunteers);

        _eventRepository.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("slot-based");
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_KindDefaultsToItems_UsesLegacyFactory()
    {
        // Positional record default means omitting Kind keeps existing call sites working.
        var @event = NewEvent();
        var command = new CreateSignUpListWithItemsCommand(
            EventId: @event.Id,
            Category: "Food",
            Description: "Back-compat items flow",
            HasMandatoryItems: true,
            HasPreferredItems: false,
            HasSuggestedItems: false,
            Items: new List<SignUpItemDto>
            {
                new("Rice", SignUpItemType.Quantity, SignUpItemCategory.Mandatory, TargetQuantity: 5),
            });

        _eventRepository.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>())).ReturnsAsync(@event);
        _unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await BuildHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue($"expected success but got: {result.Error}");
        @event.SignUpLists.Single().Kind.Should().Be(SignUpKind.Items);
    }
}
