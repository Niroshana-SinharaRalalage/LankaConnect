using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.Services;
using LankaConnect.Domain.Communications.ValueObjects;

namespace LankaConnect.Domain.Tests.Communications.Services;

/// <summary>
/// Comprehensive tests for EmailTemplateCategoryService domain service
/// </summary>
public class EmailTemplateCategoryServiceTests
{
    private readonly EmailTemplateCategoryService _service;

    public EmailTemplateCategoryServiceTests()
    {
        _service = new EmailTemplateCategoryService();
    }

    [Theory]
    [InlineData(EmailType.EmailVerification, "Authentication")]
    [InlineData(EmailType.PasswordReset, "Authentication")]
    [InlineData(EmailType.BusinessNotification, "Business")]
    [InlineData(EmailType.Marketing, "Marketing")]
    [InlineData(EmailType.Newsletter, "Marketing")]
    [InlineData(EmailType.Welcome, "Notification")]
    [InlineData(EmailType.EventNotification, "Notification")]
    [InlineData(EmailType.Transactional, "System")]
    public void DetermineCategory_ShouldReturnCorrectCategory(EmailType emailType, string expectedCategoryValue)
    {
        // Act
        var result = _service.DetermineCategory(emailType);

        // Assert
        Assert.Equal(expectedCategoryValue, result.Value);
    }

    [Fact]
    public void GetEmailTypesForCategory_Authentication_ShouldReturnAuthenticationTypes()
    {
        // Arrange
        var category = EmailTemplateCategory.Authentication;

        // Act
        var emailTypes = _service.GetEmailTypesForCategory(category).ToList();

        // Assert
        Assert.Contains(EmailType.EmailVerification, emailTypes);
        Assert.Contains(EmailType.PasswordReset, emailTypes);
        Assert.Equal(2, emailTypes.Count);
    }

    [Fact]
    public void GetEmailTypesForCategory_Business_ShouldReturnBusinessTypes()
    {
        // Arrange
        var category = EmailTemplateCategory.Business;

        // Act
        var emailTypes = _service.GetEmailTypesForCategory(category).ToList();

        // Assert
        Assert.Contains(EmailType.BusinessNotification, emailTypes);
        Assert.Single(emailTypes);
    }

    [Fact]
    public void GetEmailTypesForCategory_Marketing_ShouldReturnMarketingTypes()
    {
        // Arrange
        var category = EmailTemplateCategory.Marketing;

        // Act
        var emailTypes = _service.GetEmailTypesForCategory(category).ToList();

        // Assert
        Assert.Contains(EmailType.Marketing, emailTypes);
        Assert.Contains(EmailType.Newsletter, emailTypes);
        Assert.Equal(2, emailTypes.Count);
    }

    [Fact]
    public void GetEmailTypesForCategory_Notification_ShouldReturnNotificationTypes()
    {
        // Arrange
        var category = EmailTemplateCategory.Notification;

        // Act
        var emailTypes = _service.GetEmailTypesForCategory(category).ToList();

        // Assert
        Assert.Contains(EmailType.Welcome, emailTypes);
        Assert.Contains(EmailType.EventNotification, emailTypes);
        Assert.Equal(2, emailTypes.Count);
    }

    [Fact]
    public void GetEmailTypesForCategory_System_ShouldReturnSystemTypes()
    {
        // Arrange
        var category = EmailTemplateCategory.System;

        // Act
        var emailTypes = _service.GetEmailTypesForCategory(category).ToList();

        // Assert
        Assert.Contains(EmailType.Transactional, emailTypes);
        Assert.Single(emailTypes);
    }

    [Theory]
    [InlineData(EmailType.EmailVerification, "Authentication", true)]
    [InlineData(EmailType.EmailVerification, "Business", false)]
    [InlineData(EmailType.BusinessNotification, "Business", true)]
    [InlineData(EmailType.BusinessNotification, "Marketing", false)]
    [InlineData(EmailType.Marketing, "Marketing", true)]
    [InlineData(EmailType.Welcome, "Notification", true)]
    [InlineData(EmailType.Transactional, "System", true)]
    public void ValidateEmailTypeForCategory_ShouldReturnCorrectValidation(EmailType emailType, string categoryValue, bool expectedResult)
    {
        // Arrange
        var category = EmailTemplateCategory.FromValue(categoryValue).Value;

        // Act
        var result = _service.ValidateEmailTypeForCategory(emailType, category);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void GetCategoryCounts_ShouldReturnCorrectCounts()
    {
        // Act
        var counts = _service.GetCategoryCounts();

        // Assert
        Assert.Equal(5, counts.Count); // All categories should be present
        Assert.Equal(2, counts[EmailTemplateCategory.Authentication]); // EmailVerification, PasswordReset
        Assert.Equal(1, counts[EmailTemplateCategory.Business]); // BusinessNotification
        Assert.Equal(2, counts[EmailTemplateCategory.Marketing]); // Marketing, Newsletter
        Assert.Equal(2, counts[EmailTemplateCategory.Notification]); // Welcome, EventNotification
        Assert.Equal(1, counts[EmailTemplateCategory.System]); // Transactional
    }

    [Fact]
    public void GetCategoryCounts_ShouldIncludeAllEmailTypes()
    {
        // Arrange
        var allEmailTypes = Enum.GetValues<EmailType>();
        
        // Act
        var counts = _service.GetCategoryCounts();
        var totalCount = counts.Values.Sum();

        // Assert
        Assert.Equal(allEmailTypes.Length, totalCount);
    }

    [Fact]
    public void GetEmailTypesForCategory_ShouldReturnUniqueEmailTypes()
    {
        // Arrange
        var allCategories = EmailTemplateCategory.All;

        // Act & Assert
        var allEmailTypesFromCategories = new HashSet<EmailType>();
        
        foreach (var category in allCategories)
        {
            var emailTypes = _service.GetEmailTypesForCategory(category);
            foreach (var emailType in emailTypes)
            {
                Assert.True(allEmailTypesFromCategories.Add(emailType), 
                    $"Email type {emailType} is assigned to multiple categories");
            }
        }
    }

    [Fact]
    public void Service_ShouldMapAllEmailTypesToSingleCategory()
    {
        // Arrange
        var allEmailTypes = Enum.GetValues<EmailType>();

        // Act & Assert
        foreach (var emailType in allEmailTypes)
        {
            var category1 = _service.DetermineCategory(emailType);
            var category2 = _service.DetermineCategory(emailType);

            // Each email type should consistently map to the same category
            Assert.Equal(category2, category1);

            // Validate that the mapping is bidirectional
            var emailTypesInCategory = _service.GetEmailTypesForCategory(category1);
            Assert.Contains(emailType, emailTypesInCategory);
        }
    }
}