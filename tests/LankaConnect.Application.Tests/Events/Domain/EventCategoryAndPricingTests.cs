using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Unit tests for Event Category and TicketPrice properties (Epic 2 Phase 2)
/// Tests category classification and ticket pricing support for events
/// </summary>
public class EventCategoryAndPricingTests
{
    private Event CreateValidEvent(EventCategory? category = null, Money? ticketPrice = null)
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);
        var organizerId = Guid.NewGuid();

        return Event.Create(
            title,
            description,
            startDate,
            endDate,
            organizerId,
            capacity: 100,
            location: null,
            category: category ?? EventCategory.Community,
            ticketPrice: ticketPrice
        ).Value;
    }

    #region Category Tests

    [Fact]
    public void Create_WithValidCategory_ShouldSetCategory()
    {
        // Arrange & Act
        var @event = CreateValidEvent(category: EventCategory.Religious);

        // Assert
        @event.Category.Should().Be(EventCategory.Religious);
    }

    [Theory]
    [InlineData(EventCategory.Religious)]
    [InlineData(EventCategory.Cultural)]
    [InlineData(EventCategory.Community)]
    [InlineData(EventCategory.Educational)]
    [InlineData(EventCategory.Social)]
    [InlineData(EventCategory.Business)]
    [InlineData(EventCategory.Charity)]
    [InlineData(EventCategory.Entertainment)]
    public void Create_WithAllEventCategories_ShouldSucceed(EventCategory category)
    {
        // Arrange
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);

        // Act
        var result = Event.Create(
            title,
            description,
            startDate,
            endDate,
            Guid.NewGuid(),
            100,
            location: null,
            category: category,
            ticketPrice: null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Category.Should().Be(category);
    }

    [Fact]
    public void Create_WithDefaultCategory_ShouldSetCommunityCategory()
    {
        // Arrange
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);

        // Act - Create event without specifying category (should default to Community)
        var result = Event.Create(
            title,
            description,
            startDate,
            endDate,
            Guid.NewGuid(),
            100);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Category.Should().Be(EventCategory.Community);
    }

    #endregion

    #region TicketPrice Tests

    [Fact]
    public void Create_WithNullTicketPrice_ShouldCreateFreeEvent()
    {
        // Arrange & Act
        var @event = CreateValidEvent(ticketPrice: null);

        // Assert
        @event.TicketPrice.Should().BeNull();
        @event.IsFree().Should().BeTrue();
    }

    [Fact]
    public void Create_WithValidTicketPrice_ShouldSetTicketPrice()
    {
        // Arrange
        var ticketPrice = Money.Create(25.00m, Currency.USD).Value;

        // Act
        var @event = CreateValidEvent(ticketPrice: ticketPrice);

        // Assert
        @event.TicketPrice.Should().Be(ticketPrice);
        @event.IsFree().Should().BeFalse();
    }

    [Fact]
    public void Create_WithZeroTicketPrice_ShouldCreateFreeEvent()
    {
        // Arrange
        var ticketPrice = Money.Zero(Currency.USD);

        // Act
        var @event = CreateValidEvent(ticketPrice: ticketPrice);

        // Assert
        @event.TicketPrice.Should().NotBeNull();
        @event.TicketPrice!.IsZero.Should().BeTrue();
        @event.IsFree().Should().BeTrue();
    }

    [Theory]
    [InlineData(10.00, Currency.USD)]
    [InlineData(1500.00, Currency.LKR)]
    [InlineData(15.00, Currency.GBP)]
    [InlineData(20.00, Currency.EUR)]
    [InlineData(25.00, Currency.CAD)]
    [InlineData(30.00, Currency.AUD)]
    public void Create_WithDifferentCurrencies_ShouldSucceed(decimal amount, Currency currency)
    {
        // Arrange
        var ticketPrice = Money.Create(amount, currency).Value;
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);

        // Act
        var result = Event.Create(
            title,
            description,
            startDate,
            endDate,
            Guid.NewGuid(),
            100,
            location: null,
            category: EventCategory.Community,
            ticketPrice: ticketPrice);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TicketPrice.Should().Be(ticketPrice);
        result.Value.TicketPrice!.Amount.Should().Be(amount);
        result.Value.TicketPrice!.Currency.Should().Be(currency);
    }

    [Fact]
    public void IsFree_WithNullTicketPrice_ShouldReturnTrue()
    {
        // Arrange
        var @event = CreateValidEvent(ticketPrice: null);

        // Act
        var isFree = @event.IsFree();

        // Assert
        isFree.Should().BeTrue();
    }

    [Fact]
    public void IsFree_WithZeroTicketPrice_ShouldReturnTrue()
    {
        // Arrange
        var @event = CreateValidEvent(ticketPrice: Money.Zero(Currency.USD));

        // Act
        var isFree = @event.IsFree();

        // Assert
        isFree.Should().BeTrue();
    }

    [Fact]
    public void IsFree_WithNonZeroTicketPrice_ShouldReturnFalse()
    {
        // Arrange
        var ticketPrice = Money.Create(25.00m, Currency.USD).Value;
        var @event = CreateValidEvent(ticketPrice: ticketPrice);

        // Act
        var isFree = @event.IsFree();

        // Assert
        isFree.Should().BeFalse();
    }

    #endregion

    #region Combined Tests

    [Fact]
    public void Create_WithCategoryAndPrice_ShouldSetBothProperties()
    {
        // Arrange
        var category = EventCategory.Business;
        var ticketPrice = Money.Create(50.00m, Currency.USD).Value;
        var title = EventTitle.Create("Business Conference").Value;
        var description = EventDescription.Create("Annual business conference").Value;
        var startDate = DateTime.UtcNow.AddDays(30);
        var endDate = startDate.AddHours(8);

        // Act
        var result = Event.Create(
            title,
            description,
            startDate,
            endDate,
            Guid.NewGuid(),
            500,
            location: null,
            category: category,
            ticketPrice: ticketPrice);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Category.Should().Be(EventCategory.Business);
        result.Value.TicketPrice.Should().Be(ticketPrice);
        result.Value.IsFree().Should().BeFalse();
    }

    [Fact]
    public void Create_FreeCharityEvent_ShouldHaveCorrectProperties()
    {
        // Arrange
        var category = EventCategory.Charity;
        var title = EventTitle.Create("Community Food Drive").Value;
        var description = EventDescription.Create("Free community food distribution").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(4);

        // Act
        var result = Event.Create(
            title,
            description,
            startDate,
            endDate,
            Guid.NewGuid(),
            200,
            location: null,
            category: category,
            ticketPrice: null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Category.Should().Be(EventCategory.Charity);
        result.Value.TicketPrice.Should().BeNull();
        result.Value.IsFree().Should().BeTrue();
    }

    [Fact]
    public void Create_PaidEntertainmentEvent_ShouldHaveCorrectProperties()
    {
        // Arrange
        var category = EventCategory.Entertainment;
        var ticketPrice = Money.Create(75.00m, Currency.USD).Value;
        var title = EventTitle.Create("Music Concert").Value;
        var description = EventDescription.Create("Live music performance").Value;
        var startDate = DateTime.UtcNow.AddDays(14);
        var endDate = startDate.AddHours(3);

        // Act
        var result = Event.Create(
            title,
            description,
            startDate,
            endDate,
            Guid.NewGuid(),
            1000,
            location: null,
            category: category,
            ticketPrice: ticketPrice);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Category.Should().Be(EventCategory.Entertainment);
        result.Value.TicketPrice!.Amount.Should().Be(75.00m);
        result.Value.TicketPrice!.Currency.Should().Be(Currency.USD);
        result.Value.IsFree().Should().BeFalse();
    }

    #endregion
}
