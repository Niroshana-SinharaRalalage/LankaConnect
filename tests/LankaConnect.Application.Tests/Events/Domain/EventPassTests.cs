using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using Moq;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// TDD London School: Event Pass Feature Tests
/// Following outside-in approach with mocks for behavior verification
/// Tests focus on interactions and collaborations between objects
/// </summary>
public class EventPassTests
{
    #region Test Helpers

    private Event CreateTestEvent()
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Event Description").Value;
        var organizerId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = DateTime.UtcNow.AddDays(8);

        var eventResult = Event.Create(title, description, startDate, endDate, organizerId, 100);
        return eventResult.Value;
    }

    private EventPass CreateTestEventPass(string name = "Adult Pass", decimal price = 20m, int quantity = 100)
    {
        var passName = PassName.Create(name).Value;
        var passDescription = PassDescription.Create($"{name} - Test description").Value;
        var passPrice = Money.Create(price, Currency.USD).Value;

        var passResult = EventPass.Create(passName, passDescription, passPrice, quantity);
        return passResult.Value;
    }

    #endregion

    #region EventPass Entity Creation Tests (RED Phase)

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var passName = PassName.Create("Adult Pass").Value;
        var description = PassDescription.Create("General admission for adults").Value;
        var price = Money.Create(20m, Currency.USD).Value;
        var quantity = 100;

        // Act
        var result = EventPass.Create(passName, description, price, quantity);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(passName);
        result.Value.Description.Should().Be(description);
        result.Value.Price.Should().Be(price);
        result.Value.AvailableQuantity.Should().Be(quantity);
        result.Value.TotalQuantity.Should().Be(quantity);
    }

    [Fact]
    public void Create_WithZeroQuantity_ShouldFail()
    {
        // Arrange
        var passName = PassName.Create("Adult Pass").Value;
        var description = PassDescription.Create("General admission").Value;
        var price = Money.Create(20m, Currency.USD).Value;

        // Act
        var result = EventPass.Create(passName, description, price, 0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Quantity must be greater than 0");
    }

    [Fact]
    public void Create_WithNegativeQuantity_ShouldFail()
    {
        // Arrange
        var passName = PassName.Create("Adult Pass").Value;
        var description = PassDescription.Create("General admission").Value;
        var price = Money.Create(20m, Currency.USD).Value;

        // Act
        var result = EventPass.Create(passName, description, price, -10);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Quantity must be greater than 0");
    }

    [Fact]
    public void Create_WithNullName_ShouldFail()
    {
        // Arrange
        var description = PassDescription.Create("General admission").Value;
        var price = Money.Create(20m, Currency.USD).Value;

        // Act
        var result = EventPass.Create(null!, description, price, 100);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Pass name is required");
    }

    #endregion

    #region Pass Reservation Tests (Behavior Verification)

    [Fact]
    public void ReservePass_WithAvailableQuantity_ShouldDecreaseAvailability()
    {
        // Arrange
        var eventPass = CreateTestEventPass(quantity: 10);
        var quantityToReserve = 3;

        // Act
        var result = eventPass.Reserve(quantityToReserve);

        // Assert
        result.IsSuccess.Should().BeTrue();
        eventPass.AvailableQuantity.Should().Be(7); // 10 - 3
        eventPass.ReservedQuantity.Should().Be(3);
    }

    [Fact]
    public void ReservePass_ExceedingAvailableQuantity_ShouldFail()
    {
        // Arrange
        var eventPass = CreateTestEventPass(quantity: 5);

        // Act
        var result = eventPass.Reserve(10);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Insufficient passes available");
        eventPass.AvailableQuantity.Should().Be(5);
    }

    [Fact]
    public void ReservePass_MultipleReservations_ShouldTrackCorrectly()
    {
        // Arrange
        var eventPass = CreateTestEventPass(quantity: 20);

        // Act
        eventPass.Reserve(5);
        eventPass.Reserve(3);
        var result = eventPass.Reserve(7);

        // Assert
        result.IsSuccess.Should().BeTrue();
        eventPass.AvailableQuantity.Should().Be(5); // 20 - 5 - 3 - 7
        eventPass.ReservedQuantity.Should().Be(15);
    }

    #endregion

    #region PassPurchase Entity Tests (Interaction Testing)

    [Fact]
    public void CreatePassPurchase_WithValidData_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var eventPass = CreateTestEventPass();
        var quantity = 2;

        // Act
        var result = PassPurchase.Create(userId, eventId, eventPass.Id, quantity, eventPass.Price);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.EventId.Should().Be(eventId);
        result.Value.EventPassId.Should().Be(eventPass.Id);
        result.Value.Quantity.Should().Be(quantity);
        result.Value.Status.Should().Be(PassPurchaseStatus.Pending);
        result.Value.QRCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreatePassPurchase_ShouldGenerateUniqueQRCode()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var passId = Guid.NewGuid();
        var price = Money.Create(20m, Currency.USD).Value;

        // Act
        var purchase1 = PassPurchase.Create(userId, eventId, passId, 1, price).Value;
        var purchase2 = PassPurchase.Create(userId, eventId, passId, 1, price).Value;

        // Assert
        purchase1.QRCode.Should().NotBe(purchase2.QRCode);
    }

    #endregion

    #region PassPurchase Status Workflow Tests (Behavior Verification)

    [Fact]
    public void ConfirmPurchase_FromPendingStatus_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var passId = Guid.NewGuid();
        var price = Money.Create(20m, Currency.USD).Value;
        var purchase = PassPurchase.Create(userId, eventId, passId, 2, price).Value;

        // Act
        var result = purchase.Confirm();

        // Assert
        result.IsSuccess.Should().BeTrue();
        purchase.Status.Should().Be(PassPurchaseStatus.Confirmed);
        purchase.ConfirmedAt.Should().NotBeNull();
        purchase.ConfirmedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ConfirmPurchase_AlreadyConfirmed_ShouldFail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var passId = Guid.NewGuid();
        var price = Money.Create(20m, Currency.USD).Value;
        var purchase = PassPurchase.Create(userId, eventId, passId, 2, price).Value;
        purchase.Confirm();

        // Act
        var result = purchase.Confirm();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Purchase is already confirmed");
    }

    [Fact]
    public void CancelPurchase_FromPendingStatus_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var passId = Guid.NewGuid();
        var price = Money.Create(20m, Currency.USD).Value;
        var purchase = PassPurchase.Create(userId, eventId, passId, 2, price).Value;

        // Act
        var result = purchase.Cancel();

        // Assert
        result.IsSuccess.Should().BeTrue();
        purchase.Status.Should().Be(PassPurchaseStatus.Cancelled);
        purchase.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void CancelPurchase_FromConfirmedStatus_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var passId = Guid.NewGuid();
        var price = Money.Create(20m, Currency.USD).Value;
        var purchase = PassPurchase.Create(userId, eventId, passId, 2, price).Value;
        purchase.Confirm();

        // Act
        var result = purchase.Cancel();

        // Assert
        result.IsSuccess.Should().BeTrue();
        purchase.Status.Should().Be(PassPurchaseStatus.Cancelled);
    }

    #endregion

    #region Event Aggregate Integration Tests (Contract Testing)

    [Fact]
    public void AddPass_ToEvent_ShouldSucceed()
    {
        // Arrange
        var @event = CreateTestEvent();
        var eventPass = CreateTestEventPass();

        // Act
        var result = @event.AddPass(eventPass);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.Passes.Should().HaveCount(1);
        @event.Passes.First().Should().Be(eventPass);
    }

    [Fact]
    public void AddMultiplePasses_ToEvent_ShouldTrackAll()
    {
        // Arrange
        var @event = CreateTestEvent();
        var adultPass = CreateTestEventPass("Adult Pass", 20m);
        var childPass = CreateTestEventPass("Child Pass", 10m);
        var foodPass = CreateTestEventPass("Food Ticket", 15m);

        // Act
        @event.AddPass(adultPass);
        @event.AddPass(childPass);
        var result = @event.AddPass(foodPass);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.Passes.Should().HaveCount(3);
        @event.Passes.Should().Contain(adultPass);
        @event.Passes.Should().Contain(childPass);
        @event.Passes.Should().Contain(foodPass);
    }

    [Fact]
    public void RemovePass_FromEvent_ShouldSucceed()
    {
        // Arrange
        var @event = CreateTestEvent();
        var eventPass = CreateTestEventPass();
        @event.AddPass(eventPass);

        // Act
        var result = @event.RemovePass(eventPass.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.Passes.Should().BeEmpty();
    }

    [Fact]
    public void RemovePass_WithExistingPurchases_ShouldFail()
    {
        // Arrange
        var @event = CreateTestEvent();
        var eventPass = CreateTestEventPass();
        @event.AddPass(eventPass);
        eventPass.Reserve(5); // Simulate purchases

        // Act
        var result = @event.RemovePass(eventPass.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Cannot remove pass with existing reservations");
    }

    #endregion

    #region Domain Event Tests (Behavior Verification)

    [Fact]
    public void ConfirmPassPurchase_ShouldRaisePassPurchasedEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var passId = Guid.NewGuid();
        var price = Money.Create(20m, Currency.USD).Value;
        var purchase = PassPurchase.Create(userId, eventId, passId, 2, price).Value;
        purchase.ClearDomainEvents(); // Clear creation events

        // Act
        purchase.Confirm();

        // Assert
        var domainEvents = purchase.DomainEvents;
        domainEvents.Should().HaveCount(1);
        domainEvents.First().Should().BeOfType<PassPurchasedEvent>();

        var passEvent = (PassPurchasedEvent)domainEvents.First();
        passEvent.PurchaseId.Should().Be(purchase.Id);
        passEvent.UserId.Should().Be(userId);
        passEvent.EventId.Should().Be(eventId);
    }

    [Fact]
    public void CancelPassPurchase_ShouldRaisePassCancelledEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var passId = Guid.NewGuid();
        var price = Money.Create(20m, Currency.USD).Value;
        var purchase = PassPurchase.Create(userId, eventId, passId, 2, price).Value;
        purchase.ClearDomainEvents(); // Clear creation events

        // Act
        purchase.Cancel();

        // Assert
        var domainEvents = purchase.DomainEvents;
        domainEvents.Should().HaveCount(1);
        domainEvents.First().Should().BeOfType<PassCancelledEvent>();

        var cancelEvent = (PassCancelledEvent)domainEvents.First();
        cancelEvent.PurchaseId.Should().Be(purchase.Id);
        cancelEvent.UserId.Should().Be(userId);
    }

    #endregion

    #region PassConfiguration Value Object Tests

    [Fact]
    public void CreatePassConfiguration_WithValidData_ShouldSucceed()
    {
        // Arrange & Act
        var result = PassConfiguration.Create(
            maxPassesPerUser: 5,
            allowMultiplePassTypes: true,
            refundable: true,
            transferable: false
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MaxPassesPerUser.Should().Be(5);
        result.Value.AllowMultiplePassTypes.Should().BeTrue();
        result.Value.IsRefundable.Should().BeTrue();
        result.Value.IsTransferable.Should().BeFalse();
    }

    [Fact]
    public void PassConfiguration_WithZeroMaxPasses_ShouldFail()
    {
        // Act
        var result = PassConfiguration.Create(
            maxPassesPerUser: 0,
            allowMultiplePassTypes: true,
            refundable: true,
            transferable: false
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("MaxPassesPerUser must be greater than 0");
    }

    #endregion
}
