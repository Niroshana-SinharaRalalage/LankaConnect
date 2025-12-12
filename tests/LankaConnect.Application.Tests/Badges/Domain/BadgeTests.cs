using FluentAssertions;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Badges.Enums;

namespace LankaConnect.Application.Tests.Badges.Domain;

/// <summary>
/// TDD Tests for Badge Entity
/// Phase 6A.25: Badge Management System
/// Tests badge creation, update, activation/deactivation operations
/// </summary>
public class BadgeTests
{
    #region Badge Creation Tests

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var name = "New Event";
        var imageUrl = "https://storage.blob.core.windows.net/badges/new-event.png";
        var blobName = "badges/new-event.png";
        var position = BadgePosition.TopRight;
        var displayOrder = 1;
        var createdByUserId = Guid.NewGuid();

        // Act
        var result = Badge.Create(name, imageUrl, blobName, position, displayOrder, createdByUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(name);
        result.Value.ImageUrl.Should().Be(imageUrl);
        result.Value.BlobName.Should().Be(blobName);
        result.Value.Position.Should().Be(position);
        result.Value.DisplayOrder.Should().Be(displayOrder);
        result.Value.CreatedByUserId.Should().Be(createdByUserId);
        result.Value.IsActive.Should().BeTrue();
        result.Value.IsSystem.Should().BeFalse();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldFail()
    {
        // Act
        var result = Badge.Create(
            "",
            "https://example.com/badge.png",
            "badge.png",
            BadgePosition.TopRight,
            1,
            Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Badge name is required");
    }

    [Fact]
    public void Create_WithWhitespaceName_ShouldFail()
    {
        // Act
        var result = Badge.Create(
            "   ",
            "https://example.com/badge.png",
            "badge.png",
            BadgePosition.TopRight,
            1,
            Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Badge name is required");
    }

    [Fact]
    public void Create_WithNameExceeding50Characters_ShouldFail()
    {
        // Arrange
        var longName = new string('A', 51);

        // Act
        var result = Badge.Create(
            longName,
            "https://example.com/badge.png",
            "badge.png",
            BadgePosition.TopRight,
            1,
            Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Badge name cannot exceed 50 characters");
    }

    [Fact]
    public void Create_WithEmptyImageUrl_ShouldFail()
    {
        // Act
        var result = Badge.Create(
            "Test Badge",
            "",
            "badge.png",
            BadgePosition.TopRight,
            1,
            Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Badge image URL is required");
    }

    [Fact]
    public void Create_WithEmptyBlobName_ShouldFail()
    {
        // Act
        var result = Badge.Create(
            "Test Badge",
            "https://example.com/badge.png",
            "",
            BadgePosition.TopRight,
            1,
            Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Badge blob name is required");
    }

    [Fact]
    public void Create_WithNegativeDisplayOrder_ShouldFail()
    {
        // Act
        var result = Badge.Create(
            "Test Badge",
            "https://example.com/badge.png",
            "badge.png",
            BadgePosition.TopRight,
            -1,
            Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Display order must be non-negative");
    }

    [Fact]
    public void Create_WithEmptyCreatorUserId_ShouldFail()
    {
        // Act
        var result = Badge.Create(
            "Test Badge",
            "https://example.com/badge.png",
            "badge.png",
            BadgePosition.TopRight,
            1,
            Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Creator user ID is required");
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        // Act
        var result = Badge.Create(
            "  New Event  ",
            "https://example.com/badge.png",
            "badge.png",
            BadgePosition.TopRight,
            1,
            Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("New Event");
    }

    #endregion

    #region System Badge Creation Tests

    [Fact]
    public void CreateSystemBadge_WithValidData_ShouldSucceed()
    {
        // Arrange
        var name = "Christmas";
        var imageUrl = "https://storage.blob.core.windows.net/badges/christmas.png";
        var blobName = "badges/christmas.png";
        var position = BadgePosition.TopRight;
        var displayOrder = 6;

        // Act
        var badge = Badge.CreateSystemBadge(name, imageUrl, blobName, position, displayOrder);

        // Assert
        badge.Name.Should().Be(name);
        badge.ImageUrl.Should().Be(imageUrl);
        badge.BlobName.Should().Be(blobName);
        badge.Position.Should().Be(position);
        badge.DisplayOrder.Should().Be(displayOrder);
        badge.IsSystem.Should().BeTrue();
        badge.IsActive.Should().BeTrue();
        badge.CreatedByUserId.Should().BeNull();
    }

    [Fact]
    public void CreateSystemBadge_WithEmptyName_ShouldThrowException()
    {
        // Act & Assert
        Action act = () => Badge.CreateSystemBadge(
            "",
            "https://example.com/badge.png",
            "badge.png",
            BadgePosition.TopRight,
            1);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Badge name is required*");
    }

    [Fact]
    public void CreateSystemBadge_ShouldTrimName()
    {
        // Act
        var badge = Badge.CreateSystemBadge(
            "  New Year  ",
            "https://example.com/badge.png",
            "badge.png",
            BadgePosition.TopRight,
            1);

        // Assert
        badge.Name.Should().Be("New Year");
    }

    #endregion

    #region Badge Update Tests

    [Fact]
    public void Update_CustomBadge_WithValidData_ShouldSucceed()
    {
        // Arrange
        var badge = CreateCustomBadge();
        var newName = "Updated Badge";
        var newPosition = BadgePosition.BottomLeft;
        var newDisplayOrder = 10;

        // Act
        var result = badge.Update(newName, newPosition, newDisplayOrder);

        // Assert
        result.IsSuccess.Should().BeTrue();
        badge.Name.Should().Be(newName);
        badge.Position.Should().Be(newPosition);
        badge.DisplayOrder.Should().Be(newDisplayOrder);
    }

    [Fact]
    public void Update_SystemBadge_ShouldFail()
    {
        // Arrange
        var badge = CreateSystemBadge();

        // Act
        var result = badge.Update("New Name", BadgePosition.BottomRight, 5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("System badges cannot have their name, position, or display order modified");
    }

    [Fact]
    public void Update_WithEmptyName_ShouldFail()
    {
        // Arrange
        var badge = CreateCustomBadge();

        // Act
        var result = badge.Update("", BadgePosition.TopRight, 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Badge name is required");
    }

    [Fact]
    public void Update_WithNameExceeding50Characters_ShouldFail()
    {
        // Arrange
        var badge = CreateCustomBadge();
        var longName = new string('A', 51);

        // Act
        var result = badge.Update(longName, BadgePosition.TopRight, 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Badge name cannot exceed 50 characters");
    }

    [Fact]
    public void Update_WithNegativeDisplayOrder_ShouldFail()
    {
        // Arrange
        var badge = CreateCustomBadge();

        // Act
        var result = badge.Update("Valid Name", BadgePosition.TopRight, -5);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Display order must be non-negative");
    }

    #endregion

    #region Badge Image Update Tests

    [Fact]
    public void UpdateImage_WithValidData_ShouldSucceed()
    {
        // Arrange
        var badge = CreateCustomBadge();
        var newImageUrl = "https://storage.blob.core.windows.net/badges/new-image.png";
        var newBlobName = "badges/new-image.png";

        // Act
        var result = badge.UpdateImage(newImageUrl, newBlobName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        badge.ImageUrl.Should().Be(newImageUrl);
        badge.BlobName.Should().Be(newBlobName);
    }

    [Fact]
    public void UpdateImage_WithEmptyUrl_ShouldFail()
    {
        // Arrange
        var badge = CreateCustomBadge();

        // Act
        var result = badge.UpdateImage("", "new-blob.png");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Badge image URL is required");
    }

    [Fact]
    public void UpdateImage_WithEmptyBlobName_ShouldFail()
    {
        // Arrange
        var badge = CreateCustomBadge();

        // Act
        var result = badge.UpdateImage("https://example.com/new.png", "");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Badge blob name is required");
    }

    #endregion

    #region Badge Activation/Deactivation Tests

    [Fact]
    public void Deactivate_ActiveBadge_ShouldSucceed()
    {
        // Arrange
        var badge = CreateCustomBadge();
        badge.IsActive.Should().BeTrue();

        // Act
        var result = badge.Deactivate();

        // Assert
        result.IsSuccess.Should().BeTrue();
        badge.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_InactiveBadge_ShouldFail()
    {
        // Arrange
        var badge = CreateCustomBadge();
        badge.Deactivate();

        // Act
        var result = badge.Deactivate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Badge is already inactive");
    }

    [Fact]
    public void Activate_InactiveBadge_ShouldSucceed()
    {
        // Arrange
        var badge = CreateCustomBadge();
        badge.Deactivate();
        badge.IsActive.Should().BeFalse();

        // Act
        var result = badge.Activate();

        // Assert
        result.IsSuccess.Should().BeTrue();
        badge.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_ActiveBadge_ShouldFail()
    {
        // Arrange
        var badge = CreateCustomBadge();

        // Act
        var result = badge.Activate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Badge is already active");
    }

    [Fact]
    public void Deactivate_SystemBadge_ShouldSucceed()
    {
        // Arrange - System badges CAN be deactivated (just not deleted)
        var badge = CreateSystemBadge();

        // Act
        var result = badge.Deactivate();

        // Assert
        result.IsSuccess.Should().BeTrue();
        badge.IsActive.Should().BeFalse();
    }

    #endregion

    #region Badge Delete Eligibility Tests

    [Fact]
    public void CanDelete_CustomBadge_ShouldReturnTrue()
    {
        // Arrange
        var badge = CreateCustomBadge();

        // Act & Assert
        badge.CanDelete().Should().BeTrue();
    }

    [Fact]
    public void CanDelete_SystemBadge_ShouldReturnFalse()
    {
        // Arrange
        var badge = CreateSystemBadge();

        // Act & Assert
        badge.CanDelete().Should().BeFalse();
    }

    #endregion

    #region Badge Position Tests

    [Theory]
    [InlineData(BadgePosition.TopLeft)]
    [InlineData(BadgePosition.TopRight)]
    [InlineData(BadgePosition.BottomLeft)]
    [InlineData(BadgePosition.BottomRight)]
    public void Create_WithDifferentPositions_ShouldSucceed(BadgePosition position)
    {
        // Act
        var result = Badge.Create(
            "Test Badge",
            "https://example.com/badge.png",
            "badge.png",
            position,
            1,
            Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Position.Should().Be(position);
    }

    #endregion

    #region Test Helpers

    private static Badge CreateCustomBadge()
    {
        var result = Badge.Create(
            "Test Badge",
            "https://example.com/badge.png",
            "badge.png",
            BadgePosition.TopRight,
            1,
            Guid.NewGuid());

        return result.Value;
    }

    private static Badge CreateSystemBadge()
    {
        return Badge.CreateSystemBadge(
            "System Badge",
            "https://example.com/system-badge.png",
            "system-badge.png",
            BadgePosition.TopRight,
            1);
    }

    #endregion
}
