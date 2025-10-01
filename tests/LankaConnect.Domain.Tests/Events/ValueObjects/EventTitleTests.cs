using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Domain.Tests.Events.ValueObjects;

public class EventTitleTests
{
    [Fact]
    public void Create_WithValidTitle_ShouldReturnSuccess()
    {
        var title = "Sri Lankan New Year Celebration";
        
        var result = EventTitle.Create(title);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(title, result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithNullOrWhitespace_ShouldReturnFailure(string title)
    {
        var result = EventTitle.Create(title);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Title is required", result.Errors);
    }

    [Fact]
    public void Create_WithTooLongTitle_ShouldReturnFailure()
    {
        var title = new string('a', 201);
        
        var result = EventTitle.Create(title);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Title cannot exceed 200 characters", result.Errors);
    }

    [Fact]
    public void Create_WithMaxLength_ShouldReturnSuccess()
    {
        var title = new string('a', 200);
        
        var result = EventTitle.Create(title);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(title, result.Value.Value);
    }

    [Fact]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        var title1 = EventTitle.Create("Same Title").Value;
        var title2 = EventTitle.Create("Same Title").Value;
        
        Assert.Equal(title1, title2);
    }

    [Fact]
    public void ToString_ShouldReturnTitleValue()
    {
        var titleValue = "Event Title";
        var title = EventTitle.Create(titleValue).Value;
        
        Assert.Equal(titleValue, title.ToString());
    }
}