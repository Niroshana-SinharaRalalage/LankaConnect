using LankaConnect.Domain.Community.ValueObjects;

namespace LankaConnect.Domain.Tests.Community.ValueObjects;

public class ForumTitleTests
{
    [Fact]
    public void Create_WithValidTitle_ShouldReturnSuccess()
    {
        var title = "Jobs and Career Opportunities";
        
        var result = ForumTitle.Create(title);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(title, result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithNullOrWhitespace_ShouldReturnFailure(string title)
    {
        var result = ForumTitle.Create(title);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Title is required", result.Errors);
    }

    [Fact]
    public void Create_WithTooLongTitle_ShouldReturnFailure()
    {
        var title = new string('a', 101);
        
        var result = ForumTitle.Create(title);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Title cannot exceed 100 characters", result.Errors);
    }

    [Fact]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        var title1 = ForumTitle.Create("Same Title").Value;
        var title2 = ForumTitle.Create("Same Title").Value;
        
        Assert.Equal(title1, title2);
    }

    [Fact]
    public void ToString_ShouldReturnTitleValue()
    {
        var titleValue = "Forum Title";
        var title = ForumTitle.Create(titleValue).Value;
        
        Assert.Equal(titleValue, title.ToString());
    }
}