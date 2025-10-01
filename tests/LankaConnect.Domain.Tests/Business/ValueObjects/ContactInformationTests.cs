using LankaConnect.Domain.Business.ValueObjects;

namespace LankaConnect.Domain.Tests.Business.ValueObjects;

public class ContactInformationTests
{
    [Fact]
    public void Create_WithValidEmail_ShouldReturnSuccess()
    {
        var result = ContactInformation.Create(email: "test@example.com");

        Assert.True(result.IsSuccess);
        var contact = result.Value;
        Assert.NotNull(contact.Email);
        Assert.Equal("test@example.com", contact.Email.Value);
        Assert.Null(contact.PhoneNumber);
        Assert.Null(contact.Website);
    }

    [Fact]
    public void Create_WithValidPhoneNumber_ShouldReturnSuccess()
    {
        var result = ContactInformation.Create(phoneNumber: "+94-77-123-4567");

        Assert.True(result.IsSuccess);
        var contact = result.Value;
        Assert.NotNull(contact.PhoneNumber);
        Assert.Equal("+94-77-123-4567", contact.PhoneNumber.Value);
        Assert.Null(contact.Email);
        Assert.Null(contact.Website);
    }

    [Fact]
    public void Create_WithValidWebsite_ShouldReturnSuccess()
    {
        var result = ContactInformation.Create(website: "https://example.com");

        Assert.True(result.IsSuccess);
        var contact = result.Value;
        Assert.Equal("https://example.com", contact.Website);
        Assert.Null(contact.Email);
        Assert.Null(contact.PhoneNumber);
    }

    [Fact]
    public void Create_WithValidSocialMedia_ShouldReturnSuccess()
    {
        var result = ContactInformation.Create(
            facebookPage: "https://facebook.com/business",
            instagramHandle: "@business_insta",
            twitterHandle: "@business_tw");

        Assert.True(result.IsSuccess);
        var contact = result.Value;
        Assert.Equal("https://facebook.com/business", contact.FacebookPage);
        Assert.Equal("@business_insta", contact.InstagramHandle);
        Assert.Equal("@business_tw", contact.TwitterHandle);
    }

    [Fact]
    public void Create_WithAllContactMethods_ShouldReturnSuccess()
    {
        var result = ContactInformation.Create(
            email: "contact@business.com",
            phoneNumber: "+94-11-123-4567",
            website: "https://business.com",
            facebookPage: "https://facebook.com/business",
            instagramHandle: "@business",
            twitterHandle: "@businesspage");

        Assert.True(result.IsSuccess);
        var contact = result.Value;
        Assert.NotNull(contact.Email);
        Assert.NotNull(contact.PhoneNumber);
        Assert.NotNull(contact.Website);
        Assert.NotNull(contact.FacebookPage);
        Assert.NotNull(contact.InstagramHandle);
        Assert.NotNull(contact.TwitterHandle);
    }

    [Fact]
    public void Create_WithNoContactMethods_ShouldReturnFailure()
    {
        var result = ContactInformation.Create();

        Assert.True(result.IsFailure);
        Assert.Contains("At least one contact method is required", result.Errors);
    }

    [Fact]
    public void Create_WithAllEmptyStrings_ShouldReturnFailure()
    {
        var result = ContactInformation.Create(
            email: "",
            phoneNumber: "",
            website: "",
            facebookPage: "",
            instagramHandle: "",
            twitterHandle: "");

        Assert.True(result.IsFailure);
        Assert.Contains("At least one contact method is required", result.Errors);
    }

    [Fact]
    public void Create_WithAllWhitespaceStrings_ShouldReturnFailure()
    {
        var result = ContactInformation.Create(
            email: "   ",
            phoneNumber: "   ",
            website: "   ",
            facebookPage: "   ",
            instagramHandle: "   ",
            twitterHandle: "   ");

        Assert.True(result.IsFailure);
        Assert.Contains("At least one contact method is required", result.Errors);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@domain.com")]
    [InlineData("email@")]
    [InlineData("email@domain")]
    public void Create_WithInvalidEmail_ShouldReturnFailure(string invalidEmail)
    {
        var result = ContactInformation.Create(email: invalidEmail);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid email", result.Errors.First());
    }

    [Theory]
    [InlineData("invalid-phone")]
    [InlineData("123")]
    [InlineData("++94-77-123-4567")]
    public void Create_WithInvalidPhoneNumber_ShouldReturnFailure(string invalidPhone)
    {
        var result = ContactInformation.Create(phoneNumber: invalidPhone);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid phone number", result.Errors.First());
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("file://local.txt")]
    public void Create_WithInvalidWebsite_ShouldReturnFailure(string invalidWebsite)
    {
        var result = ContactInformation.Create(website: invalidWebsite);

        Assert.True(result.IsFailure);
        Assert.Contains("Website must be a valid URL", result.Errors);
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com")]
    [InlineData("https://sub.example.com/path")]
    public void Create_WithValidWebsiteUrls_ShouldReturnSuccess(string validWebsite)
    {
        var result = ContactInformation.Create(website: validWebsite);

        Assert.True(result.IsSuccess);
        Assert.Equal(validWebsite, result.Value.Website);
    }

    [Fact]
    public void Create_WithTooLongFacebookPage_ShouldReturnFailure()
    {
        var longFacebookPage = new string('a', 101);

        var result = ContactInformation.Create(facebookPage: longFacebookPage);

        Assert.True(result.IsFailure);
        Assert.Contains("Facebook page URL cannot exceed 100 characters", result.Errors);
    }

    [Fact]
    public void Create_WithMaxLengthFacebookPage_ShouldReturnSuccess()
    {
        var maxLengthFacebookPage = new string('a', 100);

        var result = ContactInformation.Create(facebookPage: maxLengthFacebookPage);

        Assert.True(result.IsSuccess);
        Assert.Equal(maxLengthFacebookPage, result.Value.FacebookPage);
    }

    [Theory]
    [InlineData("valid_username")]
    [InlineData("user.name")]
    [InlineData("user123")]
    [InlineData("@valid_username")]
    public void Create_WithValidInstagramHandle_ShouldReturnSuccess(string validHandle)
    {
        var result = ContactInformation.Create(instagramHandle: validHandle);

        Assert.True(result.IsSuccess);
        Assert.Equal(validHandle, result.Value.InstagramHandle);
    }

    [Theory]
    [InlineData("invalid-handle!")]
    [InlineData("handle with spaces")]
    [InlineData("handle@domain")]
    public void Create_WithInvalidInstagramHandle_ShouldReturnFailure(string invalidHandle)
    {
        var result = ContactInformation.Create(instagramHandle: invalidHandle);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid Instagram handle format", result.Errors);
    }

    [Fact]
    public void Create_WithTooLongInstagramHandle_ShouldReturnFailure()
    {
        var longHandle = new string('a', 31);

        var result = ContactInformation.Create(instagramHandle: longHandle);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid Instagram handle format", result.Errors);
    }

    [Theory]
    [InlineData("valid_username")]
    [InlineData("username123")]
    [InlineData("user_name")]
    [InlineData("@validusername")]
    public void Create_WithValidTwitterHandle_ShouldReturnSuccess(string validHandle)
    {
        var result = ContactInformation.Create(twitterHandle: validHandle);

        Assert.True(result.IsSuccess);
        Assert.Equal(validHandle, result.Value.TwitterHandle);
    }

    [Theory]
    [InlineData("invalid-handle!")]
    [InlineData("handle with spaces")]
    [InlineData("handle@domain")]
    [InlineData("handle.name")]
    public void Create_WithInvalidTwitterHandle_ShouldReturnFailure(string invalidHandle)
    {
        var result = ContactInformation.Create(twitterHandle: invalidHandle);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid Twitter handle format", result.Errors);
    }

    [Fact]
    public void Create_WithTooLongTwitterHandle_ShouldReturnFailure()
    {
        var longHandle = new string('a', 16);

        var result = ContactInformation.Create(twitterHandle: longHandle);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid Twitter handle format", result.Errors);
    }

    [Fact]
    public void Create_ShouldTrimWhitespaceFromInputs()
    {
        var result = ContactInformation.Create(
            website: "  https://example.com  ",
            facebookPage: "  https://facebook.com/page  ",
            instagramHandle: "  @instagram  ",
            twitterHandle: "  @twitter  ");

        Assert.True(result.IsSuccess);
        var contact = result.Value;
        Assert.Equal("https://example.com", contact.Website);
        Assert.Equal("https://facebook.com/page", contact.FacebookPage);
        Assert.Equal("@instagram", contact.InstagramHandle);
        Assert.Equal("@twitter", contact.TwitterHandle);
    }

    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        var contact1 = ContactInformation.Create(
            email: "test@example.com",
            phoneNumber: "+94-77-123-4567").Value;
        var contact2 = ContactInformation.Create(
            email: "test@example.com",
            phoneNumber: "+94-77-123-4567").Value;

        Assert.Equal(contact1, contact2);
    }

    [Fact]
    public void Equality_WithDifferentEmails_ShouldNotBeEqual()
    {
        var contact1 = ContactInformation.Create(email: "test1@example.com").Value;
        var contact2 = ContactInformation.Create(email: "test2@example.com").Value;

        Assert.NotEqual(contact1, contact2);
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldBeEqual()
    {
        var contact1 = ContactInformation.Create(email: "test@example.com").Value;
        var contact2 = ContactInformation.Create(email: "test@example.com").Value;

        Assert.Equal(contact1.GetHashCode(), contact2.GetHashCode());
    }

    [Fact]
    public void ToString_WithMultipleContactMethods_ShouldFormatCorrectly()
    {
        var contact = ContactInformation.Create(
            email: "test@example.com",
            phoneNumber: "+94-77-123-4567",
            website: "https://example.com").Value;

        var result = contact.ToString();

        Assert.Contains("Email:", result);
        Assert.Contains("Phone:", result);
        Assert.Contains("Website:", result);
        Assert.Contains("test@example.com", result);
        Assert.Contains("+94-77-123-4567", result);
        Assert.Contains("https://example.com", result);
    }

    [Fact]
    public void ToString_WithSocialMedia_ShouldIncludeSocialHandles()
    {
        var contact = ContactInformation.Create(
            facebookPage: "https://facebook.com/page",
            instagramHandle: "@instagram",
            twitterHandle: "@twitter").Value;

        var result = contact.ToString();

        Assert.Contains("Facebook:", result);
        Assert.Contains("Instagram:", result);
        Assert.Contains("Twitter:", result);
    }
}