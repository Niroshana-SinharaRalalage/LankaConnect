using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Tests.Events;

public class RegistrationTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var quantity = 2;
        
        var result = Registration.Create(eventId, userId, quantity);
        
        Assert.True(result.IsSuccess);
        var registration = result.Value;
        Assert.Equal(eventId, registration.EventId);
        Assert.Equal(userId, registration.UserId);
        Assert.Equal(quantity, registration.Quantity);
        Assert.Equal(RegistrationStatus.Confirmed, registration.Status);
        Assert.NotEqual(Guid.Empty, registration.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidQuantity_ShouldReturnFailure(int quantity)
    {
        var result = Registration.Create(Guid.NewGuid(), Guid.NewGuid(), quantity);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Quantity must be greater than 0", result.Errors);
    }

    [Fact]
    public void Create_WithEmptyEventId_ShouldReturnFailure()
    {
        var result = Registration.Create(Guid.Empty, Guid.NewGuid(), 1);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Event ID is required", result.Errors);
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldReturnFailure()
    {
        var result = Registration.Create(Guid.NewGuid(), Guid.Empty, 1);
        
        Assert.True(result.IsFailure);
        Assert.Contains("User ID is required", result.Errors);
    }

    [Fact]
    public void Cancel_WhenConfirmed_ShouldChangeToCancelled()
    {
        var registration = Registration.Create(Guid.NewGuid(), Guid.NewGuid(), 1).Value;
        
        registration.Cancel();
        
        Assert.Equal(RegistrationStatus.Cancelled, registration.Status);
        Assert.NotNull(registration.UpdatedAt);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldNotChangeStatus()
    {
        var registration = Registration.Create(Guid.NewGuid(), Guid.NewGuid(), 1).Value;
        registration.Cancel();
        
        registration.Cancel();
        
        Assert.Equal(RegistrationStatus.Cancelled, registration.Status);
    }

    [Fact]
    public void Confirm_WhenCancelled_ShouldChangeToConfirmed()
    {
        var registration = Registration.Create(Guid.NewGuid(), Guid.NewGuid(), 1).Value;
        registration.Cancel();
        
        registration.Confirm();
        
        Assert.Equal(RegistrationStatus.Confirmed, registration.Status);
        Assert.NotNull(registration.UpdatedAt);
    }
}