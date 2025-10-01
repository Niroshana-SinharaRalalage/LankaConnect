using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;

namespace LankaConnect.Domain.Tests.Communications.ValueObjects;

/// <summary>
/// Comprehensive tests for EmailTemplateCategory value object
/// </summary>
public class EmailTemplateCategoryTests
{
    [Fact]
    public void EmailTemplateCategory_PredefinedValues_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var authentication = EmailTemplateCategory.Authentication;
        var business = EmailTemplateCategory.Business;
        var marketing = EmailTemplateCategory.Marketing;
        var system = EmailTemplateCategory.System;
        var notification = EmailTemplateCategory.Notification;

        // Assert
        Assert.Equal("Authentication", authentication.Value);
        Assert.Equal("Authentication", authentication.DisplayName);
        Assert.Equal("User authentication and security related emails", authentication.Description);

        Assert.Equal("Business", business.Value);
        Assert.Equal("Business", business.DisplayName);

        Assert.Equal("Marketing", marketing.Value);
        Assert.Equal("System", system.Value);
        Assert.Equal("Notification", notification.Value);
    }

    [Fact]
    public void EmailTemplateCategory_All_ShouldContainAllPredefinedCategories()
    {
        // Arrange & Act
        var allCategories = EmailTemplateCategory.All;

        // Assert
        Assert.Equal(5, allCategories.Count);
        Assert.Contains(EmailTemplateCategory.Authentication, allCategories);
        Assert.Contains(EmailTemplateCategory.Business, allCategories);
        Assert.Contains(EmailTemplateCategory.Marketing, allCategories);
        Assert.Contains(EmailTemplateCategory.System, allCategories);
        Assert.Contains(EmailTemplateCategory.Notification, allCategories);
    }

    [Theory]
    [InlineData("Authentication", true)]
    [InlineData("Business", true)]
    [InlineData("Marketing", true)]
    [InlineData("System", true)]
    [InlineData("Notification", true)]
    [InlineData("InvalidCategory", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void EmailTemplateCategory_FromValue_ShouldReturnCorrectResult(string? value, bool expectedSuccess)
    {
        // Act
        var result = EmailTemplateCategory.FromValue(value!);

        // Assert
        if (expectedSuccess)
        {
            Assert.True(result.IsSuccess);
            Assert.Equal(value, result.Value.Value);
        }
        else
        {
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
            Assert.Contains("Invalid email template category", result.Error);
        }
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
    public void EmailTemplateCategory_ForEmailType_ShouldReturnCorrectCategory(EmailType emailType, string expectedCategory)
    {
        // Act
        var result = EmailTemplateCategory.ForEmailType(emailType);

        // Assert
        Assert.Equal(expectedCategory, result.Value);
    }

    [Fact]
    public void EmailTemplateCategory_EqualityComparison_ShouldWorkCorrectly()
    {
        // Arrange
        var category1 = EmailTemplateCategory.Authentication;
        var category2 = EmailTemplateCategory.Authentication;
        var category3 = EmailTemplateCategory.Business;

        // Act & Assert
        Assert.Equal(category1, category2);
        Assert.NotEqual(category1, category3);
        Assert.True(category1.Equals(category2));
        Assert.False(category1.Equals(category3));
        Assert.True(category1 == category2);
        Assert.False(category1 == category3);
    }

    [Fact]
    public void EmailTemplateCategory_ImplicitStringConversion_ShouldReturnValue()
    {
        // Arrange
        var category = EmailTemplateCategory.Authentication;

        // Act
        string categoryString = category;

        // Assert
        Assert.Equal("Authentication", categoryString);
    }

    [Fact]
    public void EmailTemplateCategory_ToString_ShouldReturnValue()
    {
        // Arrange
        var category = EmailTemplateCategory.Marketing;

        // Act
        var result = category.ToString();

        // Assert
        Assert.Equal("Marketing", result);
    }

    [Fact]
    public void EmailTemplateCategory_GetHashCode_ShouldBeConsistent()
    {
        // Arrange
        var category1 = EmailTemplateCategory.System;
        var category2 = EmailTemplateCategory.System;

        // Act
        var hash1 = category1.GetHashCode();
        var hash2 = category2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }
}