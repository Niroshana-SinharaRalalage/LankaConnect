using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using LankaConnect.Infrastructure.Email.Services;
using LankaConnect.Infrastructure.Email.Configuration;
using System.Text;

namespace LankaConnect.Infrastructure.Tests.Email.Services;

public class RazorEmailTemplateServiceTests
{
    private readonly Mock<IOptions<EmailSettings>> _emailSettings;
    private readonly Mock<IMemoryCache> _cache;
    private readonly Mock<ILogger<RazorEmailTemplateService>> _logger;
    private readonly RazorEmailTemplateService _templateService;
    private readonly EmailSettings _settings;
    private readonly string _templateBasePath;

    public RazorEmailTemplateServiceTests()
    {
        _emailSettings = new Mock<IOptions<EmailSettings>>();
        _cache = new Mock<IMemoryCache>();
        _logger = new Mock<ILogger<RazorEmailTemplateService>>();

        _templateBasePath = Path.Combine(Path.GetTempPath(), "EmailTemplateTests");
        Directory.CreateDirectory(_templateBasePath);

        _settings = new EmailSettings
        {
            TemplateBasePath = _templateBasePath,
            CacheTemplates = true,
            TemplateCacheExpiryInMinutes = 60
        };

        _emailSettings.Setup(x => x.Value).Returns(_settings);

        _templateService = new RazorEmailTemplateService(
            _emailSettings.Object,
            _cache.Object,
            _logger.Object);
    }

    [Fact]
    public async Task TemplateExistsAsync_WithExistingTemplate_ShouldReturnTrue()
    {
        // Arrange
        var templateName = "welcome-email";
        await CreateTestTemplate(templateName, "Welcome {{userName}}!");

        // Act
        var result = await _templateService.TemplateExistsAsync(templateName);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TemplateExistsAsync_WithNonExistentTemplate_ShouldReturnFalse()
    {
        // Arrange
        var templateName = "non-existent-template";

        // Act
        var result = await _templateService.TemplateExistsAsync(templateName);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RenderTemplateAsync_WithSimpleTemplate_ShouldRenderCorrectly()
    {
        // Arrange
        var templateName = "simple-template";
        var templateContent = "Hello {{userName}}, welcome to {{companyName}}!";
        await CreateTestTemplate(templateName, templateContent);

        var templateData = new Dictionary<string, object>
        {
            ["userName"] = "John Doe",
            ["companyName"] = "LankaConnect"
        };

        // Act
        var (subject, textBody, htmlBody) = await _templateService.RenderTemplateAsync(templateName, templateData);

        // Assert
        subject.Should().Contain("John Doe");
        subject.Should().Contain("LankaConnect");
        textBody.Should().Contain("Hello John Doe, welcome to LankaConnect!");
        htmlBody.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RenderTemplateAsync_WithHtmlTemplate_ShouldRenderBothTextAndHtml()
    {
        // Arrange
        var templateName = "html-template";
        var templateContent = """
            Subject: Welcome {{userName}}
            
            <h1>Welcome {{userName}}!</h1>
            <p>Thank you for joining {{companyName}}.</p>
            <p>Click <a href="{{confirmationUrl}}">here</a> to confirm your email.</p>
            """;
        await CreateTestTemplate(templateName, templateContent);

        var templateData = new Dictionary<string, object>
        {
            ["userName"] = "Jane Smith",
            ["companyName"] = "LankaConnect",
            ["confirmationUrl"] = "https://lankaconnect.com/confirm?token=abc123"
        };

        // Act
        var (subject, textBody, htmlBody) = await _templateService.RenderTemplateAsync(templateName, templateData);

        // Assert
        subject.Should().Be("Welcome Jane Smith");
        textBody.Should().Contain("Jane Smith");
        textBody.Should().NotContain("<h1>");
        htmlBody.Should().Contain("<h1>Welcome Jane Smith!</h1>");
        htmlBody.Should().Contain("https://lankaconnect.com/confirm?token=abc123");
    }

    [Fact]
    public async Task RenderTemplateAsync_WithConditionalContent_ShouldRenderConditionally()
    {
        // Arrange
        var templateName = "conditional-template";
        var templateContent = """
            Subject: Account Update
            
            Hello {{userName}},
            
            {{#if isVerified}}
            Your account is verified and active!
            {{else}}
            Please verify your account by clicking the link below.
            {{/if}}
            
            {{#if hasDiscount}}
            Special offer: Get {{discountPercent}}% off your next purchase!
            {{/if}}
            """;
        await CreateTestTemplate(templateName, templateContent);

        var templateData = new Dictionary<string, object>
        {
            ["userName"] = "Alice Johnson",
            ["isVerified"] = true,
            ["hasDiscount"] = false,
            ["discountPercent"] = 20
        };

        // Act
        var (subject, textBody, htmlBody) = await _templateService.RenderTemplateAsync(templateName, templateData);

        // Assert
        textBody.Should().Contain("Your account is verified and active!");
        textBody.Should().NotContain("Please verify your account");
        textBody.Should().NotContain("Special offer");
    }

    [Fact]
    public async Task RenderTemplateAsync_WithLoopContent_ShouldRenderLoop()
    {
        // Arrange
        var templateName = "loop-template";
        var templateContent = """
            Subject: Order Confirmation
            
            Dear {{customerName}},
            
            Your order contains the following items:
            {{#each items}}
            - {{name}}: ${{price}}
            {{/each}}
            
            Total: ${{total}}
            """;
        await CreateTestTemplate(templateName, templateContent);

        var templateData = new Dictionary<string, object>
        {
            ["customerName"] = "Bob Wilson",
            ["items"] = new List<object>
            {
                new { name = "Sri Lankan Curry", price = 15.99 },
                new { name = "Coconut Rice", price = 8.50 },
                new { name = "Mango Lassi", price = 4.99 }
            },
            ["total"] = 29.48
        };

        // Act
        var (subject, textBody, htmlBody) = await _templateService.RenderTemplateAsync(templateName, templateData);

        // Assert
        subject.Should().Be("Order Confirmation");
        textBody.Should().Contain("Sri Lankan Curry: $15.99");
        textBody.Should().Contain("Coconut Rice: $8.50");
        textBody.Should().Contain("Mango Lassi: $4.99");
        textBody.Should().Contain("Total: $29.48");
    }

    [Fact]
    public async Task RenderTemplateAsync_WithNonExistentTemplate_ShouldThrowException()
    {
        // Arrange
        var templateName = "non-existent";
        var templateData = new Dictionary<string, object>();

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _templateService.RenderTemplateAsync(templateName, templateData));
    }

    [Fact]
    public async Task RenderTemplateAsync_WithMalformedTemplate_ShouldThrowException()
    {
        // Arrange
        var templateName = "malformed-template";
        var templateContent = "Hello {{userName} - missing closing brace";
        await CreateTestTemplate(templateName, templateContent);

        var templateData = new Dictionary<string, object>
        {
            ["userName"] = "Test User"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _templateService.RenderTemplateAsync(templateName, templateData));
        
        exception.Message.Should().Contain("template parsing");
    }

    [Fact]
    public async Task RenderTemplateAsync_WithMissingTemplateData_ShouldHandleGracefully()
    {
        // Arrange
        var templateName = "missing-data-template";
        var templateContent = "Hello {{userName}}, your score is {{score}}.";
        await CreateTestTemplate(templateName, templateContent);

        var templateData = new Dictionary<string, object>
        {
            ["userName"] = "Test User"
            // missing "score" property
        };

        // Act
        var (subject, textBody, htmlBody) = await _templateService.RenderTemplateAsync(templateName, templateData);

        // Assert
        textBody.Should().Contain("Test User");
        // The missing variable should be handled gracefully (empty string or left as is)
        textBody.Should().NotContain("{{score}}"); // Should not have unprocessed tokens
    }

    [Fact]
    public async Task RenderTemplateAsync_WithCachingEnabled_ShouldCacheTemplate()
    {
        // Arrange
        _settings.CacheTemplates = true;
        var templateName = "cached-template";
        var templateContent = "Cached template: {{message}}";
        await CreateTestTemplate(templateName, templateContent);

        var templateData = new Dictionary<string, object>
        {
            ["message"] = "Hello from cache"
        };

        object? cachedValue = null;
        _cache.Setup(x => x.TryGetValue(It.IsAny<string>(), out cachedValue))
              .Returns(false);

        _cache.Setup(x => x.CreateEntry(It.IsAny<string>()))
              .Returns(Mock.Of<ICacheEntry>());

        // Act
        await _templateService.RenderTemplateAsync(templateName, templateData);

        // Assert
        _cache.Verify(x => x.TryGetValue(It.Is<string>(k => k.Contains(templateName)), out cachedValue), Times.Once);
        _cache.Verify(x => x.CreateEntry(It.Is<string>(k => k.Contains(templateName))), Times.Once);
    }

    [Fact]
    public async Task RenderTemplateAsync_WithCachingDisabled_ShouldNotUseCache()
    {
        // Arrange
        _settings.CacheTemplates = false;
        var templateName = "non-cached-template";
        var templateContent = "Non-cached template: {{message}}";
        await CreateTestTemplate(templateName, templateContent);

        var templateData = new Dictionary<string, object>
        {
            ["message"] = "Hello without cache"
        };

        // Act
        await _templateService.RenderTemplateAsync(templateName, templateData);

        // Assert
        _cache.Verify(x => x.TryGetValue(It.IsAny<string>(), out It.Ref<object?>.IsAny), Times.Never);
    }

    [Fact]
    public async Task GetTemplatePathAsync_WithValidTemplateName_ShouldReturnCorrectPath()
    {
        // Arrange
        var templateName = "test-template";

        // Act
        var result = await _templateService.GetTemplatePathAsync(templateName);

        // Assert
        result.Should().EndWith($"{templateName}.cshtml");
        result.Should().StartWith(_templateBasePath);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public async Task TemplateExistsAsync_WithInvalidTemplateName_ShouldReturnFalse(string templateName)
    {
        // Act
        var result = await _templateService.TemplateExistsAsync(templateName);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RenderTemplateAsync_WithSpecialCharacters_ShouldEncodeCorrectly()
    {
        // Arrange
        var templateName = "special-chars-template";
        var templateContent = "Message: {{message}}";
        await CreateTestTemplate(templateName, templateContent);

        var templateData = new Dictionary<string, object>
        {
            ["message"] = "<script>alert('xss')</script> & special chars"
        };

        // Act
        var (subject, textBody, htmlBody) = await _templateService.RenderTemplateAsync(templateName, templateData);

        // Assert
        htmlBody.Should().Contain("&lt;script&gt;"); // Should be HTML encoded
        htmlBody.Should().Contain("&amp;"); // Ampersand should be encoded
        textBody.Should().Contain("<script>alert('xss')</script>"); // Text version should not be encoded
    }

    [Fact]
    public async Task RenderTemplateAsync_WithDateTimeData_ShouldFormatCorrectly()
    {
        // Arrange
        var templateName = "datetime-template";
        var templateContent = "Event date: {{eventDate:yyyy-MM-dd}} at {{eventTime:HH:mm}}";
        await CreateTestTemplate(templateName, templateContent);

        var eventDateTime = new DateTime(2023, 12, 25, 14, 30, 0);
        var templateData = new Dictionary<string, object>
        {
            ["eventDate"] = eventDateTime,
            ["eventTime"] = eventDateTime.TimeOfDay
        };

        // Act
        var (subject, textBody, htmlBody) = await _templateService.RenderTemplateAsync(templateName, templateData);

        // Assert
        textBody.Should().Contain("2023-12-25");
        textBody.Should().Contain("14:30");
    }

    // Helper method to create test templates
    private async Task CreateTestTemplate(string templateName, string content)
    {
        var templatePath = Path.Combine(_templateBasePath, $"{templateName}.cshtml");
        await File.WriteAllTextAsync(templatePath, content);
    }

    // Cleanup after tests
    public void Dispose()
    {
        if (Directory.Exists(_templateBasePath))
        {
            Directory.Delete(_templateBasePath, true);
        }
    }
}