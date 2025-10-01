using LankaConnect.Domain.Users.ValueObjects;

namespace LankaConnect.Domain.Tests.Users.ValueObjects;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("+1-555-123-4567")]
    [InlineData("555-123-4567")]
    [InlineData("(555) 123-4567")]
    [InlineData("555.123.4567")]
    [InlineData("15551234567")]
    [InlineData("+94771234567")]
    public void Create_WithValidPhoneNumber_ShouldReturnSuccess(string phoneNumber)
    {
        var result = PhoneNumber.Create(phoneNumber);
        
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithNullOrWhitespace_ShouldReturnFailure(string phoneNumber)
    {
        var result = PhoneNumber.Create(phoneNumber);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Phone number is required", result.Errors);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("abc-def-ghij")]
    [InlineData("555-123")]
    [InlineData("+1-555")]
    public void Create_WithInvalidFormat_ShouldReturnFailure(string phoneNumber)
    {
        var result = PhoneNumber.Create(phoneNumber);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Invalid phone number format", result.Errors);
    }

    [Fact]
    public void Equality_WithSameNormalizedValue_ShouldBeEqual()
    {
        var phone1 = PhoneNumber.Create("+1-555-123-4567").Value;
        var phone2 = PhoneNumber.Create("15551234567").Value;
        
        Assert.Equal(phone1, phone2);
    }

    [Fact]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        var phone1 = PhoneNumber.Create("555-123-4567").Value;
        var phone2 = PhoneNumber.Create("555-987-6543").Value;
        
        Assert.NotEqual(phone1, phone2);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedPhoneNumber()
    {
        var phone = PhoneNumber.Create("+1-555-123-4567").Value;
        
        Assert.NotEmpty(phone.ToString());
    }
}