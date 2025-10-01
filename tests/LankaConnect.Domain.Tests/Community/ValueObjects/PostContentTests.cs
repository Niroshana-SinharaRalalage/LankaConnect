using LankaConnect.Domain.Community.ValueObjects;

namespace LankaConnect.Domain.Tests.Community.ValueObjects;

public class PostContentTests
{
    [Fact]
    public void Create_WithValidContent_ShouldReturnSuccess()
    {
        var content = "This is a helpful forum post with useful information for the community.";
        
        var result = PostContent.Create(content);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(content, result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithNullOrWhitespace_ShouldReturnFailure(string content)
    {
        var result = PostContent.Create(content);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Content is required", result.Errors);
    }

    [Fact]
    public void Create_WithTooLongContent_ShouldReturnFailure()
    {
        var content = new string('a', 10001);
        
        var result = PostContent.Create(content);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Content cannot exceed 10000 characters", result.Errors);
    }

    [Fact]
    public void Create_WithMaxLength_ShouldReturnSuccess()
    {
        var content = new string('a', 10000);
        
        var result = PostContent.Create(content);
        
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        var content1 = PostContent.Create("Same content").Value;
        var content2 = PostContent.Create("Same content").Value;
        
        Assert.Equal(content1, content2);
    }

    [Fact]
    public void ToString_ShouldReturnContentValue()
    {
        var contentValue = "Post content here";
        var content = PostContent.Create(contentValue).Value;
        
        Assert.Equal(contentValue, content.ToString());
    }
}