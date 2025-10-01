using LankaConnect.Domain.Users.ValueObjects;

namespace LankaConnect.Domain.Tests.Users.ValueObjects;

public class EmailTests
{
    [Fact]
    public void Create_WithValidEmail_ShouldReturnSuccess()
    {
        var email = "test@example.com";
        
        var result = Email.Create(email);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(email, result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithNullOrWhitespace_ShouldReturnFailure(string email)
    {
        var result = Email.Create(email);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Email is required", result.Errors);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@example.com")]
    [InlineData("test.example.com")]
    [InlineData("test@.com")]
    public void Create_WithInvalidFormat_ShouldReturnFailure(string email)
    {
        var result = Email.Create(email);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Invalid email format", result.Errors);
    }

    [Fact]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        var email1 = Email.Create("test@example.com").Value;
        var email2 = Email.Create("test@example.com").Value;
        
        Assert.Equal(email1, email2);
    }

    [Fact]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        var email1 = Email.Create("test1@example.com").Value;
        var email2 = Email.Create("test2@example.com").Value;
        
        Assert.NotEqual(email1, email2);
    }

    [Fact]
    public void ToString_ShouldReturnEmailValue()
    {
        var emailValue = "test@example.com";
        var email = Email.Create(emailValue).Value;
        
        Assert.Equal(emailValue, email.ToString());
    }
}