using FluentAssertions;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using Xunit;

namespace LankaConnect.Application.Tests.Users.Domain;

/// <summary>
/// TDD RED Phase - Domain Tests for User Profile Photo Functionality
/// These tests will FAIL until we implement the profile photo properties and methods
/// </summary>
public class UserProfilePhotoTests
{
    private User CreateValidUser()
    {
        var email = Email.Create("test@example.com").Value;
        return User.Create(email, "John", "Doe").Value;
    }

    #region UpdateProfilePhoto Tests

    [Fact]
    public void UpdateProfilePhoto_WithValidUrlAndBlobName_ShouldSucceed()
    {
        // Arrange
        var user = CreateValidUser();
        var photoUrl = "https://lankaconnectstorage.blob.core.windows.net/users/12345-profile.jpg";
        var blobName = "users/12345-profile.jpg";

        // Act
        var result = user.UpdateProfilePhoto(photoUrl, blobName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.ProfilePhotoUrl.Should().Be(photoUrl);
        user.ProfilePhotoBlobName.Should().Be(blobName);
    }

    [Fact]
    public void UpdateProfilePhoto_WithValidUrlAndBlobName_ShouldRaiseDomainEvent()
    {
        // Arrange
        var user = CreateValidUser();
        var photoUrl = "https://lankaconnectstorage.blob.core.windows.net/users/12345-profile.jpg";
        var blobName = "users/12345-profile.jpg";

        // Act
        var result = user.UpdateProfilePhoto(photoUrl, blobName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var domainEvents = user.GetDomainEvents();
        domainEvents.Should().ContainSingle(e => e is UserProfilePhotoUpdatedEvent);

        var photoEvent = domainEvents.OfType<UserProfilePhotoUpdatedEvent>().Single();
        photoEvent.UserId.Should().Be(user.Id);
        photoEvent.PhotoUrl.Should().Be(photoUrl);
        photoEvent.BlobName.Should().Be(blobName);
    }

    [Fact]
    public void UpdateProfilePhoto_WithEmptyUrl_ShouldFail()
    {
        // Arrange
        var user = CreateValidUser();
        var blobName = "users/12345-profile.jpg";

        // Act
        var result = user.UpdateProfilePhoto(string.Empty, blobName);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Profile photo URL is required");
        user.ProfilePhotoUrl.Should().BeNull();
        user.ProfilePhotoBlobName.Should().BeNull();
    }

    [Fact]
    public void UpdateProfilePhoto_WithNullUrl_ShouldFail()
    {
        // Arrange
        var user = CreateValidUser();
        var blobName = "users/12345-profile.jpg";

        // Act
        var result = user.UpdateProfilePhoto(null!, blobName);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Profile photo URL is required");
        user.ProfilePhotoUrl.Should().BeNull();
        user.ProfilePhotoBlobName.Should().BeNull();
    }

    [Fact]
    public void UpdateProfilePhoto_WithWhitespaceUrl_ShouldFail()
    {
        // Arrange
        var user = CreateValidUser();
        var blobName = "users/12345-profile.jpg";

        // Act
        var result = user.UpdateProfilePhoto("   ", blobName);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Profile photo URL is required");
    }

    [Fact]
    public void UpdateProfilePhoto_WithEmptyBlobName_ShouldFail()
    {
        // Arrange
        var user = CreateValidUser();
        var photoUrl = "https://lankaconnectstorage.blob.core.windows.net/users/12345-profile.jpg";

        // Act
        var result = user.UpdateProfilePhoto(photoUrl, string.Empty);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Profile photo blob name is required");
        user.ProfilePhotoUrl.Should().BeNull();
        user.ProfilePhotoBlobName.Should().BeNull();
    }

    [Fact]
    public void UpdateProfilePhoto_WithNullBlobName_ShouldFail()
    {
        // Arrange
        var user = CreateValidUser();
        var photoUrl = "https://lankaconnectstorage.blob.core.windows.net/users/12345-profile.jpg";

        // Act
        var result = user.UpdateProfilePhoto(photoUrl, null!);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Profile photo blob name is required");
    }

    [Fact]
    public void UpdateProfilePhoto_WithWhitespaceBlobName_ShouldFail()
    {
        // Arrange
        var user = CreateValidUser();
        var photoUrl = "https://lankaconnectstorage.blob.core.windows.net/users/12345-profile.jpg";

        // Act
        var result = user.UpdateProfilePhoto(photoUrl, "   ");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Profile photo blob name is required");
    }

    [Fact]
    public void UpdateProfilePhoto_ReplacingExistingPhoto_ShouldSucceed()
    {
        // Arrange
        var user = CreateValidUser();
        var oldPhotoUrl = "https://lankaconnectstorage.blob.core.windows.net/users/12345-old.jpg";
        var oldBlobName = "users/12345-old.jpg";
        user.UpdateProfilePhoto(oldPhotoUrl, oldBlobName);

        var newPhotoUrl = "https://lankaconnectstorage.blob.core.windows.net/users/12345-new.jpg";
        var newBlobName = "users/12345-new.jpg";

        // Act
        var result = user.UpdateProfilePhoto(newPhotoUrl, newBlobName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.ProfilePhotoUrl.Should().Be(newPhotoUrl);
        user.ProfilePhotoBlobName.Should().Be(newBlobName);
    }

    [Fact]
    public void UpdateProfilePhoto_ReplacingExistingPhoto_ShouldRaiseNewEvent()
    {
        // Arrange
        var user = CreateValidUser();
        var oldPhotoUrl = "https://lankaconnectstorage.blob.core.windows.net/users/12345-old.jpg";
        var oldBlobName = "users/12345-old.jpg";
        user.UpdateProfilePhoto(oldPhotoUrl, oldBlobName);
        user.ClearDomainEvents(); // Clear first event

        var newPhotoUrl = "https://lankaconnectstorage.blob.core.windows.net/users/12345-new.jpg";
        var newBlobName = "users/12345-new.jpg";

        // Act
        var result = user.UpdateProfilePhoto(newPhotoUrl, newBlobName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var domainEvents = user.GetDomainEvents();
        domainEvents.Should().ContainSingle(e => e is UserProfilePhotoUpdatedEvent);

        var photoEvent = domainEvents.OfType<UserProfilePhotoUpdatedEvent>().Single();
        photoEvent.PhotoUrl.Should().Be(newPhotoUrl);
        photoEvent.BlobName.Should().Be(newBlobName);
    }

    #endregion

    #region RemoveProfilePhoto Tests

    [Fact]
    public void RemoveProfilePhoto_WhenPhotoExists_ShouldSucceed()
    {
        // Arrange
        var user = CreateValidUser();
        var photoUrl = "https://lankaconnectstorage.blob.core.windows.net/users/12345-profile.jpg";
        var blobName = "users/12345-profile.jpg";
        user.UpdateProfilePhoto(photoUrl, blobName);

        // Act
        var result = user.RemoveProfilePhoto();

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.ProfilePhotoUrl.Should().BeNull();
        user.ProfilePhotoBlobName.Should().BeNull();
    }

    [Fact]
    public void RemoveProfilePhoto_WhenPhotoExists_ShouldRaiseDomainEvent()
    {
        // Arrange
        var user = CreateValidUser();
        var photoUrl = "https://lankaconnectstorage.blob.core.windows.net/users/12345-profile.jpg";
        var blobName = "users/12345-profile.jpg";
        user.UpdateProfilePhoto(photoUrl, blobName);
        user.ClearDomainEvents(); // Clear update event

        // Act
        var result = user.RemoveProfilePhoto();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var domainEvents = user.GetDomainEvents();
        domainEvents.Should().ContainSingle(e => e is UserProfilePhotoRemovedEvent);

        var removeEvent = domainEvents.OfType<UserProfilePhotoRemovedEvent>().Single();
        removeEvent.UserId.Should().Be(user.Id);
        removeEvent.OldPhotoUrl.Should().Be(photoUrl);
        removeEvent.OldBlobName.Should().Be(blobName);
    }

    [Fact]
    public void RemoveProfilePhoto_WhenNoPhotoExists_ShouldFail()
    {
        // Arrange
        var user = CreateValidUser();

        // Act
        var result = user.RemoveProfilePhoto();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("No profile photo to remove");
    }

    [Fact]
    public void RemoveProfilePhoto_WhenAlreadyRemoved_ShouldFail()
    {
        // Arrange
        var user = CreateValidUser();
        var photoUrl = "https://lankaconnectstorage.blob.core.windows.net/users/12345-profile.jpg";
        var blobName = "users/12345-profile.jpg";
        user.UpdateProfilePhoto(photoUrl, blobName);
        user.RemoveProfilePhoto();

        // Act
        var result = user.RemoveProfilePhoto();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("No profile photo to remove");
    }

    #endregion

    #region Property Tests

    [Fact]
    public void ProfilePhotoUrl_InitialState_ShouldBeNull()
    {
        // Arrange & Act
        var user = CreateValidUser();

        // Assert
        user.ProfilePhotoUrl.Should().BeNull();
    }

    [Fact]
    public void ProfilePhotoBlobName_InitialState_ShouldBeNull()
    {
        // Arrange & Act
        var user = CreateValidUser();

        // Assert
        user.ProfilePhotoBlobName.Should().BeNull();
    }

    [Fact]
    public void ProfilePhotoUrl_AfterUpdate_ShouldReturnCorrectValue()
    {
        // Arrange
        var user = CreateValidUser();
        var photoUrl = "https://lankaconnectstorage.blob.core.windows.net/users/12345-profile.jpg";
        var blobName = "users/12345-profile.jpg";

        // Act
        user.UpdateProfilePhoto(photoUrl, blobName);

        // Assert
        user.ProfilePhotoUrl.Should().Be(photoUrl);
    }

    [Fact]
    public void ProfilePhotoBlobName_AfterUpdate_ShouldReturnCorrectValue()
    {
        // Arrange
        var user = CreateValidUser();
        var photoUrl = "https://lankaconnectstorage.blob.core.windows.net/users/12345-profile.jpg";
        var blobName = "users/12345-profile.jpg";

        // Act
        user.UpdateProfilePhoto(photoUrl, blobName);

        // Assert
        user.ProfilePhotoBlobName.Should().Be(blobName);
    }

    #endregion
}
