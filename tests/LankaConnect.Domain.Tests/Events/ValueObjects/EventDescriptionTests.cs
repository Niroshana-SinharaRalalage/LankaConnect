using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Domain.Tests.Events.ValueObjects;

public class EventDescriptionTests
{
    [Fact]
    public void Create_WithValidDescription_ShouldReturnSuccess()
    {
        var description = "Join us for a traditional Sri Lankan New Year celebration with authentic food, music, and cultural activities.";
        
        var result = EventDescription.Create(description);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(description, result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithNullOrWhitespace_ShouldReturnFailure(string description)
    {
        var result = EventDescription.Create(description);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Description is required", result.Errors);
    }

    [Fact]
    public void Create_WithTooLongDescription_ShouldReturnFailure()
    {
        var description = new string('a', 2001);
        
        var result = EventDescription.Create(description);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Description cannot exceed 2000 characters", result.Errors);
    }

    [Fact]
    public void Create_WithMaxLength_ShouldReturnSuccess()
    {
        var description = new string('a', 2000);
        
        var result = EventDescription.Create(description);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(description, result.Value.Value);
    }

    [Fact]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        var desc1 = EventDescription.Create("Same description").Value;
        var desc2 = EventDescription.Create("Same description").Value;
        
        Assert.Equal(desc1, desc2);
    }

    [Fact]
    public void ToString_ShouldReturnDescriptionValue()
    {
        var descValue = "Event description";
        var description = EventDescription.Create(descValue).Value;
        
        Assert.Equal(descValue, description.ToString());
    }
}