using FluentAssertions;
using LankaConnect.Application.Interfaces;
using LankaConnect.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Services;

public class EmailUrlHelperTests
{
    private readonly IConfiguration _configuration;
    private readonly IEmailUrlHelper _emailUrlHelper;

    public EmailUrlHelperTests()
    {
        var configurationData = new Dictionary<string, string>
        {
            ["ApplicationUrls:ApiBaseUrl"] = "https://api.example.com",
            ["ApplicationUrls:FrontendBaseUrl"] = "https://www.example.com",
            ["ApplicationUrls:EmailVerificationPath"] = "/verify-email",
            ["ApplicationUrls:EventDetailsPath"] = "/events/{eventId}",
            ["ApplicationUrls:EventManagePath"] = "/events/{eventId}/manage",
            ["ApplicationUrls:EventSignupPath"] = "/events/{eventId}/signup",
            ["ApplicationUrls:MyEventsPath"] = "/my-events",
            ["ApplicationUrls:NewsletterConfirmPath"] = "/api/newsletter/confirm",
            ["ApplicationUrls:NewsletterUnsubscribePath"] = "/api/newsletter/unsubscribe",
            ["ApplicationUrls:UnsubscribePath"] = "/unsubscribe"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();

        _emailUrlHelper = new EmailUrlHelper(_configuration);
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new EmailUrlHelper(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Theory]
    [InlineData("abc123")]
    [InlineData("token-with-special-chars!@#")]
    public void BuildEmailVerificationUrl_WithValidToken_ReturnsCorrectUrl(string token)
    {
        // Act
        var result = _emailUrlHelper.BuildEmailVerificationUrl(token);

        // Assert
        result.Should().Be($"https://www.example.com/verify-email?token={Uri.EscapeDataString(token)}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildEmailVerificationUrl_WithInvalidToken_ThrowsArgumentException(string? token)
    {
        // Act
        var act = () => _emailUrlHelper.BuildEmailVerificationUrl(token!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("token")
            .WithMessage("Token cannot be null or whitespace.*");
    }

    [Fact]
    public void BuildEventDetailsUrl_WithValidEventId_ReturnsCorrectUrl()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var result = _emailUrlHelper.BuildEventDetailsUrl(eventId);

        // Assert
        result.Should().Be($"https://www.example.com/events/{eventId}");
    }

    [Fact]
    public void BuildEventDetailsUrl_WithEmptyGuid_ThrowsArgumentException()
    {
        // Act
        var act = () => _emailUrlHelper.BuildEventDetailsUrl(Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("eventId")
            .WithMessage("Event ID cannot be empty.*");
    }

    [Fact]
    public void BuildEventManageUrl_WithValidEventId_ReturnsCorrectUrl()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var result = _emailUrlHelper.BuildEventManageUrl(eventId);

        // Assert
        result.Should().Be($"https://www.example.com/events/{eventId}/manage");
    }

    [Fact]
    public void BuildEventManageUrl_WithEmptyGuid_ThrowsArgumentException()
    {
        // Act
        var act = () => _emailUrlHelper.BuildEventManageUrl(Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("eventId")
            .WithMessage("Event ID cannot be empty.*");
    }

    [Fact]
    public void BuildEventSignupUrl_WithValidEventId_ReturnsCorrectUrl()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var result = _emailUrlHelper.BuildEventSignupUrl(eventId);

        // Assert
        result.Should().Be($"https://www.example.com/events/{eventId}/signup");
    }

    [Fact]
    public void BuildEventSignupUrl_WithEmptyGuid_ThrowsArgumentException()
    {
        // Act
        var act = () => _emailUrlHelper.BuildEventSignupUrl(Guid.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("eventId")
            .WithMessage("Event ID cannot be empty.*");
    }

    [Fact]
    public void BuildMyEventsUrl_ReturnsCorrectUrl()
    {
        // Act
        var result = _emailUrlHelper.BuildMyEventsUrl();

        // Assert
        result.Should().Be("https://www.example.com/my-events");
    }

    [Theory]
    [InlineData("newsletter-token-123")]
    [InlineData("token+with+special=chars")]
    public void BuildNewsletterConfirmUrl_WithValidToken_ReturnsCorrectUrl(string token)
    {
        // Act
        var result = _emailUrlHelper.BuildNewsletterConfirmUrl(token);

        // Assert
        result.Should().Be($"https://api.example.com/api/newsletter/confirm?token={Uri.EscapeDataString(token)}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildNewsletterConfirmUrl_WithInvalidToken_ThrowsArgumentException(string? token)
    {
        // Act
        var act = () => _emailUrlHelper.BuildNewsletterConfirmUrl(token!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("token")
            .WithMessage("Token cannot be null or whitespace.*");
    }

    [Theory]
    [InlineData("unsubscribe-token-456")]
    [InlineData("token/with/slashes")]
    public void BuildNewsletterUnsubscribeUrl_WithValidToken_ReturnsCorrectUrl(string token)
    {
        // Act
        var result = _emailUrlHelper.BuildNewsletterUnsubscribeUrl(token);

        // Assert
        result.Should().Be($"https://api.example.com/api/newsletter/unsubscribe?token={Uri.EscapeDataString(token)}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildNewsletterUnsubscribeUrl_WithInvalidToken_ThrowsArgumentException(string? token)
    {
        // Act
        var act = () => _emailUrlHelper.BuildNewsletterUnsubscribeUrl(token!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("token")
            .WithMessage("Token cannot be null or whitespace.*");
    }

    [Theory]
    [InlineData("general-unsubscribe-token")]
    [InlineData("token&with&ampersands")]
    public void BuildUnsubscribeUrl_WithValidToken_ReturnsCorrectUrl(string token)
    {
        // Act
        var result = _emailUrlHelper.BuildUnsubscribeUrl(token);

        // Assert
        result.Should().Be($"https://www.example.com/unsubscribe?token={Uri.EscapeDataString(token)}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildUnsubscribeUrl_WithInvalidToken_ThrowsArgumentException(string? token)
    {
        // Act
        var act = () => _emailUrlHelper.BuildUnsubscribeUrl(token!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("token")
            .WithMessage("Token cannot be null or whitespace.*");
    }

    [Fact]
    public void BuildEmailVerificationUrl_UrlEncoding_HandlesSpecialCharactersCorrectly()
    {
        // Arrange
        var tokenWithSpecialChars = "token+with/special=chars&more?stuff";

        // Act
        var result = _emailUrlHelper.BuildEmailVerificationUrl(tokenWithSpecialChars);

        // Assert
        result.Should().Contain(Uri.EscapeDataString(tokenWithSpecialChars));
        result.Should().NotContain("+with/special=");
    }

    [Fact]
    public void BuildEmailVerificationUrl_WithTrailingSlashInBaseUrl_HandlesCorrectly()
    {
        // Arrange
        var configurationData = new Dictionary<string, string>
        {
            ["ApplicationUrls:FrontendBaseUrl"] = "https://www.example.com/",
            ["ApplicationUrls:EmailVerificationPath"] = "/verify-email"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();

        var helper = new EmailUrlHelper(config);

        // Act
        var result = helper.BuildEmailVerificationUrl("token123");

        // Assert
        result.Should().Be("https://www.example.com/verify-email?token=token123");
        result.Should().NotContain("//verify-email");
    }

    [Fact]
    public void BuildEventDetailsUrl_WithoutConfiguration_UsesDefaultPath()
    {
        // Arrange
        var configurationData = new Dictionary<string, string>
        {
            ["ApplicationUrls:FrontendBaseUrl"] = "https://www.example.com"
            // EventDetailsPath not configured - should use default
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();

        var helper = new EmailUrlHelper(config);
        var eventId = Guid.NewGuid();

        // Act
        var result = helper.BuildEventDetailsUrl(eventId);

        // Assert
        result.Should().Be($"https://www.example.com/events/{eventId}");
    }

    [Fact]
    public void BuildEmailVerificationUrl_WithMissingFrontendBaseUrl_ThrowsInvalidOperationException()
    {
        // Arrange
        var configurationData = new Dictionary<string, string>
        {
            ["ApplicationUrls:EmailVerificationPath"] = "/verify-email"
            // FrontendBaseUrl missing
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();

        var helper = new EmailUrlHelper(config);

        // Act
        var act = () => helper.BuildEmailVerificationUrl("token");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("ApplicationUrls:FrontendBaseUrl is not configured.");
    }

    [Fact]
    public void BuildNewsletterConfirmUrl_WithMissingApiBaseUrl_ThrowsInvalidOperationException()
    {
        // Arrange
        var configurationData = new Dictionary<string, string>
        {
            ["ApplicationUrls:NewsletterConfirmPath"] = "/api/newsletter/confirm"
            // ApiBaseUrl missing
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();

        var helper = new EmailUrlHelper(config);

        // Act
        var act = () => helper.BuildNewsletterConfirmUrl("token");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("ApplicationUrls:ApiBaseUrl is not configured.");
    }

    [Fact]
    public void AllMethods_WithVariousEventIds_GenerateUniqueUrls()
    {
        // Arrange
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();

        // Act
        var details1 = _emailUrlHelper.BuildEventDetailsUrl(eventId1);
        var details2 = _emailUrlHelper.BuildEventDetailsUrl(eventId2);
        var manage1 = _emailUrlHelper.BuildEventManageUrl(eventId1);
        var signup1 = _emailUrlHelper.BuildEventSignupUrl(eventId1);

        // Assert
        details1.Should().NotBe(details2);
        details1.Should().NotBe(manage1);
        details1.Should().NotBe(signup1);
        manage1.Should().NotBe(signup1);
    }
}
