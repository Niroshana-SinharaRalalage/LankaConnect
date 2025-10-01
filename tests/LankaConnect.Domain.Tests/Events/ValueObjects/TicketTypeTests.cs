using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Domain.Tests.Events.ValueObjects;

public class TicketTypeTests
{
    [Fact]
    public void CreateFree_ShouldReturnFreeTicketType()
    {
        var name = "General Admission";
        
        var result = TicketType.CreateFree(name, 100);
        
        Assert.True(result.IsSuccess);
        var ticketType = result.Value;
        Assert.Equal(name, ticketType.Name);
        Assert.True(ticketType.IsFree);
        Assert.Null(ticketType.Price);
        Assert.Equal(100, ticketType.MaxAvailable);
        Assert.Equal(1, ticketType.MaxPerUser);
    }

    [Fact]
    public void CreatePaid_WithValidPrice_ShouldReturnPaidTicketType()
    {
        var name = "VIP Access";
        var price = Money.Create(50.00m, Currency.USD).Value;
        
        var result = TicketType.CreatePaid(name, price, 50, 2);
        
        Assert.True(result.IsSuccess);
        var ticketType = result.Value;
        Assert.Equal(name, ticketType.Name);
        Assert.False(ticketType.IsFree);
        Assert.Equal(price, ticketType.Price);
        Assert.Equal(50, ticketType.MaxAvailable);
        Assert.Equal(2, ticketType.MaxPerUser);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateFree_WithInvalidName_ShouldReturnFailure(string name)
    {
        var result = TicketType.CreateFree(name, 100);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Name is required", result.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateFree_WithInvalidMaxAvailable_ShouldReturnFailure(int maxAvailable)
    {
        var result = TicketType.CreateFree("General", maxAvailable);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Max available must be greater than 0", result.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreatePaid_WithInvalidMaxPerUser_ShouldReturnFailure(int maxPerUser)
    {
        var price = Money.Create(25.00m, Currency.USD).Value;
        
        var result = TicketType.CreatePaid("VIP", price, 50, maxPerUser);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Max per user must be greater than 0", result.Errors);
    }

    [Fact]
    public void HasCapacityFor_WithSufficientCapacity_ShouldReturnTrue()
    {
        var ticketType = TicketType.CreateFree("General", 100).Value;
        
        Assert.True(ticketType.HasCapacityFor(25, 50));
        Assert.False(ticketType.HasCapacityFor(51, 50));
    }

    [Fact]
    public void CanUserPurchase_WithinLimit_ShouldReturnTrue()
    {
        var ticketType = TicketType.CreatePaid("VIP", Money.Create(50m, Currency.USD).Value, 100, 2).Value;
        
        Assert.True(ticketType.CanUserPurchase(1, 1));
        Assert.True(ticketType.CanUserPurchase(2, 0));
        Assert.False(ticketType.CanUserPurchase(1, 2));
        Assert.False(ticketType.CanUserPurchase(3, 0));
    }

    [Fact]
    public void Equality_WithSameName_ShouldBeEqual()
    {
        var ticketType1 = TicketType.CreateFree("General", 100).Value;
        var ticketType2 = TicketType.CreateFree("General", 200).Value;
        
        Assert.Equal(ticketType1, ticketType2);
    }

    [Fact]
    public void ToString_ShouldReturnName()
    {
        var name = "VIP Access";
        var ticketType = TicketType.CreateFree(name, 100).Value;
        
        Assert.Equal(name, ticketType.ToString());
    }
}