using FluentAssertions;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.DomainEvents;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Comprehensive domain unit tests for the PhotoAlbum aggregate root and AlbumPhoto entity.
/// Covers creation, lifecycle transitions, settings management, photo CRUD, moderation, and cover photo.
/// </summary>
public class PhotoAlbumTests
{
    #region Test Helpers

    private static readonly Guid DefaultEventId = Guid.NewGuid();
    private static readonly Guid DefaultOrganizerId = Guid.NewGuid();
    private const string DefaultEventTitle = "Community Meetup 2026";
    private const string DefaultDescription = "Photos from the meetup";

    /// <summary>
    /// Creates a valid PhotoAlbum in Draft status with default test data.
    /// </summary>
    private static PhotoAlbum CreateDraftAlbum(
        Guid? eventId = null,
        Guid? organizerId = null,
        string? eventTitle = null,
        string? description = null)
    {
        var result = PhotoAlbum.Create(
            eventId ?? DefaultEventId,
            organizerId ?? DefaultOrganizerId,
            eventTitle ?? DefaultEventTitle,
            description);

        result.IsSuccess.Should().BeTrue("test helper should create a valid album");
        return result.Value;
    }

    /// <summary>
    /// Creates a valid PhotoAlbum in Published status.
    /// </summary>
    private static PhotoAlbum CreatePublishedAlbum(
        Guid? organizerId = null,
        AlbumModerationMode? moderationMode = null)
    {
        var album = CreateDraftAlbum(organizerId: organizerId);

        if (moderationMode.HasValue)
        {
            album.UpdateSettings(moderationMode: moderationMode.Value);
        }

        var publishResult = album.Publish();
        publishResult.IsSuccess.Should().BeTrue("test helper should publish album successfully");
        album.ClearDomainEvents();
        return album;
    }

    /// <summary>
    /// Creates a valid PhotoAlbum in Closed status.
    /// </summary>
    private static PhotoAlbum CreateClosedAlbum()
    {
        var album = CreatePublishedAlbum();
        var closeResult = album.Close();
        closeResult.IsSuccess.Should().BeTrue("test helper should close album successfully");
        album.ClearDomainEvents();
        return album;
    }

    /// <summary>
    /// Adds a photo to a published album and returns the result.
    /// </summary>
    private static Result<AlbumPhoto> AddTestPhoto(
        PhotoAlbum album,
        Guid? uploaderId = null,
        string? uploaderName = null,
        string? caption = null,
        int photoIndex = 1)
    {
        return album.AddPhoto(
            uploaderId ?? Guid.NewGuid(),
            uploaderName ?? $"Test User {photoIndex}",
            $"https://blob.azure.com/albums/original_{photoIndex}.jpg",
            $"albums/original_{photoIndex}.jpg",
            $"https://blob.azure.com/albums/thumb_{photoIndex}.webp",
            $"albums/thumb_{photoIndex}.webp",
            $"https://blob.azure.com/albums/medium_{photoIndex}.webp",
            $"albums/medium_{photoIndex}.webp",
            caption,
            1024L * photoIndex);
    }

    #endregion

    #region Create Tests

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var title = "Annual Gala 2026";

        // Act
        var result = PhotoAlbum.Create(eventId, organizerId, title);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.EventId.Should().Be(eventId);
        result.Value.OrganizerId.Should().Be(organizerId);
        result.Value.EventTitle.Should().Be(title);
        result.Value.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithValidDataAndDescription_ShouldSucceed()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var title = "Annual Gala 2026";
        var description = "Memories from our wonderful evening";

        // Act
        var result = PhotoAlbum.Create(eventId, organizerId, title, description);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().Be(description);
    }

    [Fact]
    public void Create_WithEmptyEventId_ShouldFail()
    {
        // Arrange & Act
        var result = PhotoAlbum.Create(Guid.Empty, Guid.NewGuid(), "Test Event");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event ID");
    }

    [Fact]
    public void Create_WithEmptyOrganizerId_ShouldFail()
    {
        // Arrange & Act
        var result = PhotoAlbum.Create(Guid.NewGuid(), Guid.Empty, "Test Event");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Organizer ID");
    }

    [Fact]
    public void Create_WithEmptyEventTitle_ShouldFail()
    {
        // Arrange & Act
        var result = PhotoAlbum.Create(Guid.NewGuid(), Guid.NewGuid(), "");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event title");
    }

    [Fact]
    public void Create_WithWhitespaceEventTitle_ShouldFail()
    {
        // Arrange & Act
        var result = PhotoAlbum.Create(Guid.NewGuid(), Guid.NewGuid(), "   ");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event title");
    }

    [Fact]
    public void Create_WithDescriptionExceedingMaxLength_ShouldFail()
    {
        // Arrange
        var longDescription = new string('x', PhotoAlbum.MAX_DESCRIPTION_LENGTH + 1);

        // Act
        var result = PhotoAlbum.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Event", longDescription);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Description");
        result.Error.Should().Contain($"{PhotoAlbum.MAX_DESCRIPTION_LENGTH}");
    }

    [Fact]
    public void Create_WithDescriptionAtMaxLength_ShouldSucceed()
    {
        // Arrange
        var maxDescription = new string('x', PhotoAlbum.MAX_DESCRIPTION_LENGTH);

        // Act
        var result = PhotoAlbum.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Event", maxDescription);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().HaveLength(PhotoAlbum.MAX_DESCRIPTION_LENGTH);
    }

    [Fact]
    public void Create_ShouldRaisePhotoAlbumCreatedDomainEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var result = PhotoAlbum.Create(eventId, Guid.NewGuid(), "Test Event");

        // Assert
        result.Value.DomainEvents.Should().ContainSingle(e => e is PhotoAlbumCreatedDomainEvent);
        var domainEvent = result.Value.DomainEvents
            .OfType<PhotoAlbumCreatedDomainEvent>().Single();
        domainEvent.AlbumId.Should().Be(result.Value.Id);
        domainEvent.EventId.Should().Be(eventId);
    }

    [Fact]
    public void Create_DefaultStatus_ShouldBeDraft()
    {
        // Arrange & Act
        var album = CreateDraftAlbum();

        // Assert
        album.Status.Should().Be(AlbumStatus.Draft);
    }

    [Fact]
    public void Create_DefaultUploadPermission_ShouldBeOrganizerOnly()
    {
        // Arrange & Act
        var album = CreateDraftAlbum();

        // Assert
        album.UploadPermission.Should().Be(AlbumUploadPermission.OrganizerOnly);
    }

    [Fact]
    public void Create_DefaultModerationMode_ShouldBePostModeration()
    {
        // Arrange & Act
        var album = CreateDraftAlbum();

        // Assert
        album.ModerationMode.Should().Be(AlbumModerationMode.PostModeration);
    }

    [Fact]
    public void Create_DefaultRetentionDays_ShouldBe7()
    {
        // Arrange & Act
        var album = CreateDraftAlbum();

        // Assert
        album.RetentionDays.Should().Be(PhotoAlbum.DEFAULT_RETENTION_DAYS);
        album.RetentionDays.Should().Be(7);
    }

    [Fact]
    public void Create_DefaultPhotoCount_ShouldBeZero()
    {
        // Arrange & Act
        var album = CreateDraftAlbum();

        // Assert
        album.PhotoCount.Should().Be(0);
    }

    [Fact]
    public void Create_DefaultPhotos_ShouldBeEmpty()
    {
        // Arrange & Act
        var album = CreateDraftAlbum();

        // Assert
        album.Photos.Should().BeEmpty();
    }

    [Fact]
    public void Create_DefaultPublishedAt_ShouldBeNull()
    {
        // Arrange & Act
        var album = CreateDraftAlbum();

        // Assert
        album.PublishedAt.Should().BeNull();
    }

    [Fact]
    public void Create_DefaultClosedAt_ShouldBeNull()
    {
        // Arrange & Act
        var album = CreateDraftAlbum();

        // Assert
        album.ClosedAt.Should().BeNull();
    }

    [Fact]
    public void Create_DefaultCoverPhotoUrl_ShouldBeNull()
    {
        // Arrange & Act
        var album = CreateDraftAlbum();

        // Assert
        album.CoverPhotoUrl.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullDescription_ShouldSucceed()
    {
        // Arrange & Act
        var result = PhotoAlbum.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Event", null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().BeNull();
    }

    #endregion

    #region Publish Tests

    [Fact]
    public void Publish_FromDraftStatus_ShouldSucceed()
    {
        // Arrange
        var album = CreateDraftAlbum();

        // Act
        var result = album.Publish();

        // Assert
        result.IsSuccess.Should().BeTrue();
        album.Status.Should().Be(AlbumStatus.Published);
    }

    [Fact]
    public void Publish_FromPublishedStatus_ShouldFail()
    {
        // Arrange
        var album = CreatePublishedAlbum();

        // Act
        var result = album.Publish();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already published");
    }

    [Fact]
    public void Publish_FromClosedStatus_ShouldFail()
    {
        // Arrange
        var album = CreateClosedAlbum();

        // Act
        var result = album.Publish();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("closed");
    }

    [Fact]
    public void Publish_ShouldSetPublishedAtTimestamp()
    {
        // Arrange
        var album = CreateDraftAlbum();
        var beforePublish = DateTime.UtcNow;

        // Act
        album.Publish();

        // Assert
        album.PublishedAt.Should().NotBeNull();
        album.PublishedAt!.Value.Should().BeOnOrAfter(beforePublish);
        album.PublishedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Publish_ShouldRaisePhotoAlbumPublishedDomainEvent()
    {
        // Arrange
        var album = CreateDraftAlbum();
        album.ClearDomainEvents(); // Clear creation event

        // Act
        album.Publish();

        // Assert
        album.DomainEvents.Should().ContainSingle(e => e is PhotoAlbumPublishedDomainEvent);
        var domainEvent = album.DomainEvents
            .OfType<PhotoAlbumPublishedDomainEvent>().Single();
        domainEvent.AlbumId.Should().Be(album.Id);
        domainEvent.EventId.Should().Be(album.EventId);
        domainEvent.EventTitle.Should().Be(album.EventTitle);
    }

    #endregion

    #region Close Tests

    [Fact]
    public void Close_FromPublishedStatus_ShouldSucceed()
    {
        // Arrange
        var album = CreatePublishedAlbum();

        // Act
        var result = album.Close();

        // Assert
        result.IsSuccess.Should().BeTrue();
        album.Status.Should().Be(AlbumStatus.Closed);
    }

    [Fact]
    public void Close_FromDraftStatus_ShouldFail()
    {
        // Arrange
        var album = CreateDraftAlbum();

        // Act
        var result = album.Close();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("draft");
    }

    [Fact]
    public void Close_FromClosedStatus_ShouldFail()
    {
        // Arrange
        var album = CreateClosedAlbum();

        // Act
        var result = album.Close();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already closed");
    }

    [Fact]
    public void Close_ShouldSetClosedAtTimestamp()
    {
        // Arrange
        var album = CreatePublishedAlbum();
        var beforeClose = DateTime.UtcNow;

        // Act
        album.Close();

        // Assert
        album.ClosedAt.Should().NotBeNull();
        album.ClosedAt!.Value.Should().BeOnOrAfter(beforeClose);
        album.ClosedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    #endregion

    #region Status Transition Tests

    [Fact]
    public void StatusTransition_DraftToPublishedToClosed_ShouldSucceed()
    {
        // Arrange
        var album = CreateDraftAlbum();
        album.Status.Should().Be(AlbumStatus.Draft);

        // Act & Assert - Draft -> Published
        var publishResult = album.Publish();
        publishResult.IsSuccess.Should().BeTrue();
        album.Status.Should().Be(AlbumStatus.Published);

        // Act & Assert - Published -> Closed
        var closeResult = album.Close();
        closeResult.IsSuccess.Should().BeTrue();
        album.Status.Should().Be(AlbumStatus.Closed);
    }

    [Fact]
    public void StatusTransition_CannotGoFromPublishedToDraft()
    {
        // Arrange
        var album = CreatePublishedAlbum();

        // Act - There is no Unpublish method; the only transitions are Publish and Close.
        // Verify status remains Published (no way to go back to Draft)
        album.Status.Should().Be(AlbumStatus.Published);

        // Attempting to publish again fails (the closest thing to a "backwards" attempt)
        var result = album.Publish();
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void StatusTransition_CannotGoFromClosedToPublished()
    {
        // Arrange
        var album = CreateClosedAlbum();

        // Act
        var result = album.Publish();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("closed");
    }

    [Fact]
    public void StatusTransition_CannotGoFromClosedToDraft()
    {
        // Arrange
        var album = CreateClosedAlbum();

        // Act - No direct method to go back. Close again fails.
        var closeResult = album.Close();
        closeResult.IsFailure.Should().BeTrue();

        // Status remains Closed (no way to revert)
        album.Status.Should().Be(AlbumStatus.Closed);
    }

    #endregion

    #region UpdateSettings Tests

    [Fact]
    public void UpdateSettings_UpdateUploadPermission_ShouldSucceed()
    {
        // Arrange
        var album = CreateDraftAlbum();

        // Act
        var result = album.UpdateSettings(uploadPermission: AlbumUploadPermission.AnyAuthenticated);

        // Assert
        result.IsSuccess.Should().BeTrue();
        album.UploadPermission.Should().Be(AlbumUploadPermission.AnyAuthenticated);
    }

    [Fact]
    public void UpdateSettings_UpdateModerationMode_ShouldSucceed()
    {
        // Arrange
        var album = CreateDraftAlbum();

        // Act
        var result = album.UpdateSettings(moderationMode: AlbumModerationMode.PreApproval);

        // Assert
        result.IsSuccess.Should().BeTrue();
        album.ModerationMode.Should().Be(AlbumModerationMode.PreApproval);
    }

    [Fact]
    public void UpdateSettings_UpdateDescription_ShouldSucceed()
    {
        // Arrange
        var album = CreateDraftAlbum();

        // Act
        var result = album.UpdateSettings(description: "Updated description for the album");

        // Assert
        result.IsSuccess.Should().BeTrue();
        album.Description.Should().Be("Updated description for the album");
    }

    [Fact]
    public void UpdateSettings_UpdateAllFields_ShouldSucceed()
    {
        // Arrange
        var album = CreateDraftAlbum();

        // Act
        var result = album.UpdateSettings(
            uploadPermission: AlbumUploadPermission.RegisteredAttendees,
            moderationMode: AlbumModerationMode.None,
            description: "Everyone can upload");

        // Assert
        result.IsSuccess.Should().BeTrue();
        album.UploadPermission.Should().Be(AlbumUploadPermission.RegisteredAttendees);
        album.ModerationMode.Should().Be(AlbumModerationMode.None);
        album.Description.Should().Be("Everyone can upload");
    }

    [Fact]
    public void UpdateSettings_WithAllNullValues_ShouldNotChangeAnything()
    {
        // Arrange
        var album = CreateDraftAlbum();
        var originalPermission = album.UploadPermission;
        var originalModeration = album.ModerationMode;
        var originalDescription = album.Description;

        // Act
        var result = album.UpdateSettings();

        // Assert
        result.IsSuccess.Should().BeTrue();
        album.UploadPermission.Should().Be(originalPermission);
        album.ModerationMode.Should().Be(originalModeration);
        album.Description.Should().Be(originalDescription);
    }

    [Fact]
    public void UpdateSettings_WithDescriptionExceedingMaxLength_ShouldFail()
    {
        // Arrange
        var album = CreateDraftAlbum();
        var longDescription = new string('y', PhotoAlbum.MAX_DESCRIPTION_LENGTH + 1);

        // Act
        var result = album.UpdateSettings(description: longDescription);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Description");
        result.Error.Should().Contain($"{PhotoAlbum.MAX_DESCRIPTION_LENGTH}");
    }

    [Fact]
    public void UpdateSettings_OnClosedAlbum_ShouldFail()
    {
        // Arrange
        var album = CreateClosedAlbum();

        // Act
        var result = album.UpdateSettings(uploadPermission: AlbumUploadPermission.AnyAuthenticated);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("closed");
    }

    [Fact]
    public void UpdateSettings_OnPublishedAlbum_ShouldSucceed()
    {
        // Arrange
        var album = CreatePublishedAlbum();

        // Act
        var result = album.UpdateSettings(
            uploadPermission: AlbumUploadPermission.RegisteredAttendees);

        // Assert
        result.IsSuccess.Should().BeTrue();
        album.UploadPermission.Should().Be(AlbumUploadPermission.RegisteredAttendees);
    }

    [Fact]
    public void UpdateSettings_OnDraftAlbum_ShouldSucceed()
    {
        // Arrange
        var album = CreateDraftAlbum();

        // Act
        var result = album.UpdateSettings(
            moderationMode: AlbumModerationMode.PreApproval);

        // Assert
        result.IsSuccess.Should().BeTrue();
        album.ModerationMode.Should().Be(AlbumModerationMode.PreApproval);
    }

    #endregion

    #region AddPhoto Tests

    [Fact]
    public void AddPhoto_ToPublishedAlbum_ShouldSucceed()
    {
        // Arrange
        var album = CreatePublishedAlbum();
        var uploaderId = Guid.NewGuid();

        // Act
        var result = AddTestPhoto(album, uploaderId: uploaderId, uploaderName: "Alice");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.UploaderId.Should().Be(uploaderId);
        result.Value.UploaderName.Should().Be("Alice");
    }

    [Fact]
    public void AddPhoto_ShouldSetCorrectProperties()
    {
        // Arrange
        var album = CreatePublishedAlbum();
        var uploaderId = Guid.NewGuid();

        // Act
        var result = album.AddPhoto(
            uploaderId,
            "Bob Smith",
            "https://blob.azure.com/albums/original.jpg",
            "albums/original.jpg",
            "https://blob.azure.com/albums/thumb.webp",
            "albums/thumb.webp",
            "https://blob.azure.com/albums/medium.webp",
            "albums/medium.webp",
            "Great sunset photo",
            2048L);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var photo = result.Value;
        photo.AlbumId.Should().Be(album.Id);
        photo.UploaderId.Should().Be(uploaderId);
        photo.UploaderName.Should().Be("Bob Smith");
        photo.OriginalUrl.Should().Be("https://blob.azure.com/albums/original.jpg");
        photo.OriginalBlobName.Should().Be("albums/original.jpg");
        photo.ThumbnailUrl.Should().Be("https://blob.azure.com/albums/thumb.webp");
        photo.ThumbnailBlobName.Should().Be("albums/thumb.webp");
        photo.MediumUrl.Should().Be("https://blob.azure.com/albums/medium.webp");
        photo.MediumBlobName.Should().Be("albums/medium.webp");
        photo.Caption.Should().Be("Great sunset photo");
        photo.FileSizeBytes.Should().Be(2048L);
        photo.DisplayOrder.Should().Be(1);
        photo.UploadedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void AddPhoto_ShouldIncrementPhotoCount_WhenApproved()
    {
        // Arrange - PostModeration mode: photos are immediately Approved
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PostModeration);

        // Act
        AddTestPhoto(album, photoIndex: 1);
        AddTestPhoto(album, photoIndex: 2);
        AddTestPhoto(album, photoIndex: 3);

        // Assert
        album.PhotoCount.Should().Be(3);
        album.Photos.Should().HaveCount(3);
    }

    [Fact]
    public void AddPhoto_ShouldNotIncrementPhotoCount_WhenPendingApproval()
    {
        // Arrange - PreApproval mode: photos are PendingApproval
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PreApproval);

        // Act
        AddTestPhoto(album, photoIndex: 1);
        AddTestPhoto(album, photoIndex: 2);

        // Assert
        album.PhotoCount.Should().Be(0, "pending photos should not be counted");
        album.Photos.Should().HaveCount(2);
    }

    [Fact]
    public void AddPhoto_WithNullCaption_ShouldSucceed()
    {
        // Arrange
        var album = CreatePublishedAlbum();

        // Act
        var result = album.AddPhoto(
            Guid.NewGuid(), "User",
            "https://blob.azure.com/original.jpg", "original.jpg",
            "https://blob.azure.com/thumb.webp", "thumb.webp",
            "https://blob.azure.com/medium.webp", "medium.webp",
            null, 1024L);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Caption.Should().BeNull();
    }

    [Fact]
    public void AddPhoto_WhenAlbumHasMaxPhotos_ShouldFail()
    {
        // Arrange
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PostModeration);

        for (int i = 1; i <= PhotoAlbum.MAX_PHOTOS; i++)
        {
            var addResult = AddTestPhoto(album, photoIndex: i);
            addResult.IsSuccess.Should().BeTrue($"adding photo {i} should succeed");
        }

        album.Photos.Should().HaveCount(PhotoAlbum.MAX_PHOTOS);

        // Act - Try to add one more
        var result = AddTestPhoto(album, photoIndex: PhotoAlbum.MAX_PHOTOS + 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain($"{PhotoAlbum.MAX_PHOTOS}");
        result.Error.Should().Contain("maximum");
    }

    [Fact]
    public void AddPhoto_RejectedPhotosDoNotCountTowardLimit()
    {
        // Arrange - PreApproval mode so photos start as PendingApproval
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PreApproval);

        // Add MAX_PHOTOS photos
        for (int i = 1; i <= PhotoAlbum.MAX_PHOTOS; i++)
        {
            AddTestPhoto(album, photoIndex: i);
        }

        // Reject one photo to free up space
        var photoToReject = album.Photos.First();
        album.RejectPhoto(photoToReject.Id);

        // Act - Adding another photo should succeed because rejected photos don't count
        var result = AddTestPhoto(album, photoIndex: PhotoAlbum.MAX_PHOTOS + 1);

        // Assert
        result.IsSuccess.Should().BeTrue("rejected photos should not count toward the limit");
    }

    [Fact]
    public void AddPhoto_ToDraftAlbum_ShouldSucceedAndAutoPublish()
    {
        // Arrange
        var album = CreateDraftAlbum();
        album.ClearDomainEvents();

        // Act
        var result = AddTestPhoto(album);

        // Assert
        result.IsSuccess.Should().BeTrue("uploads should be allowed on Draft albums");
        album.Status.Should().Be(AlbumStatus.Published, "album should auto-publish on first upload");
        album.PublishedAt.Should().NotBeNull();
        album.PhotoCount.Should().Be(1);
        album.Photos.Should().HaveCount(1);

        // Should raise PhotoAlbumPublishedDomainEvent
        album.DomainEvents.Should().ContainSingle(e => e is PhotoAlbumPublishedDomainEvent);
        // Should also raise PhotoUploadedToAlbumDomainEvent
        album.DomainEvents.Should().ContainSingle(e => e is PhotoUploadedToAlbumDomainEvent);
    }

    [Fact]
    public void AddPhoto_SecondPhotoAfterAutoPublish_ShouldNotRePublish()
    {
        // Arrange — first photo auto-publishes
        var album = CreateDraftAlbum();
        album.ClearDomainEvents();
        AddTestPhoto(album, photoIndex: 1);
        album.ClearDomainEvents();

        // Act — second photo on Published album
        var result = AddTestPhoto(album, photoIndex: 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        album.Status.Should().Be(AlbumStatus.Published);
        album.PhotoCount.Should().Be(2);

        // Should NOT raise another PhotoAlbumPublishedDomainEvent
        album.DomainEvents.Should().NotContain(e => e is PhotoAlbumPublishedDomainEvent);
        // Should raise PhotoUploadedToAlbumDomainEvent
        album.DomainEvents.Should().ContainSingle(e => e is PhotoUploadedToAlbumDomainEvent);
    }

    [Fact]
    public void AddPhoto_ToClosedAlbum_ShouldFail()
    {
        // Arrange
        var album = CreateClosedAlbum();

        // Act
        var result = AddTestPhoto(album);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("closed");
    }

    [Fact]
    public void AddPhoto_ShouldRaisePhotoUploadedToAlbumDomainEvent()
    {
        // Arrange
        var album = CreatePublishedAlbum();
        var uploaderId = Guid.NewGuid();

        // Act
        var result = AddTestPhoto(album, uploaderId: uploaderId);

        // Assert
        album.DomainEvents.Should().ContainSingle(e => e is PhotoUploadedToAlbumDomainEvent);
        var domainEvent = album.DomainEvents
            .OfType<PhotoUploadedToAlbumDomainEvent>().Single();
        domainEvent.AlbumId.Should().Be(album.Id);
        domainEvent.PhotoId.Should().Be(result.Value.Id);
        domainEvent.UploaderId.Should().Be(uploaderId);
    }

    [Fact]
    public void AddPhoto_WithPostModerationMode_ShouldSetStatusToApproved()
    {
        // Arrange
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PostModeration);

        // Act
        var result = AddTestPhoto(album);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(AlbumPhotoStatus.Approved);
    }

    [Fact]
    public void AddPhoto_WithPreApprovalMode_ShouldSetStatusToPendingApproval()
    {
        // Arrange
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PreApproval);

        // Act
        var result = AddTestPhoto(album);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(AlbumPhotoStatus.PendingApproval);
    }

    [Fact]
    public void AddPhoto_WithNoneModerationMode_ShouldSetStatusToApproved()
    {
        // Arrange
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.None);

        // Act
        var result = AddTestPhoto(album);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(AlbumPhotoStatus.Approved);
    }

    [Fact]
    public void AddPhoto_ShouldAssignSequentialDisplayOrders()
    {
        // Arrange
        var album = CreatePublishedAlbum();

        // Act
        var photo1 = AddTestPhoto(album, photoIndex: 1).Value;
        var photo2 = AddTestPhoto(album, photoIndex: 2).Value;
        var photo3 = AddTestPhoto(album, photoIndex: 3).Value;

        // Assert
        photo1.DisplayOrder.Should().Be(1);
        photo2.DisplayOrder.Should().Be(2);
        photo3.DisplayOrder.Should().Be(3);
    }

    [Fact]
    public void AddPhoto_ShouldSetExpiresAtBasedOnRetentionDays()
    {
        // Arrange
        var album = CreatePublishedAlbum();
        var beforeAdd = DateTime.UtcNow;

        // Act
        var result = AddTestPhoto(album);

        // Assert
        result.Value.ExpiresAt.Should().BeCloseTo(
            beforeAdd.AddDays(PhotoAlbum.DEFAULT_RETENTION_DAYS),
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AddPhoto_WithEmptyUploaderName_ShouldFail()
    {
        // Arrange
        var album = CreatePublishedAlbum();

        // Act
        var result = album.AddPhoto(
            Guid.NewGuid(), "",
            "https://blob.azure.com/original.jpg", "original.jpg",
            "https://blob.azure.com/thumb.webp", "thumb.webp",
            "https://blob.azure.com/medium.webp", "medium.webp",
            null, 1024L);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Uploader name");
    }

    [Fact]
    public void AddPhoto_WithEmptyOriginalUrl_ShouldFail()
    {
        // Arrange
        var album = CreatePublishedAlbum();

        // Act
        var result = album.AddPhoto(
            Guid.NewGuid(), "User",
            "", "original.jpg",
            "https://blob.azure.com/thumb.webp", "thumb.webp",
            "https://blob.azure.com/medium.webp", "medium.webp",
            null, 1024L);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Original URL");
    }

    [Fact]
    public void AddPhoto_WithZeroFileSize_ShouldFail()
    {
        // Arrange
        var album = CreatePublishedAlbum();

        // Act
        var result = album.AddPhoto(
            Guid.NewGuid(), "User",
            "https://blob.azure.com/original.jpg", "original.jpg",
            "https://blob.azure.com/thumb.webp", "thumb.webp",
            "https://blob.azure.com/medium.webp", "medium.webp",
            null, 0L);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("File size");
    }

    [Fact]
    public void AddPhoto_WithNegativeFileSize_ShouldFail()
    {
        // Arrange
        var album = CreatePublishedAlbum();

        // Act
        var result = album.AddPhoto(
            Guid.NewGuid(), "User",
            "https://blob.azure.com/original.jpg", "original.jpg",
            "https://blob.azure.com/thumb.webp", "thumb.webp",
            "https://blob.azure.com/medium.webp", "medium.webp",
            null, -100L);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("File size");
    }

    [Fact]
    public void AddPhoto_WithCaptionExceedingMaxLength_ShouldFail()
    {
        // Arrange
        var album = CreatePublishedAlbum();
        var longCaption = new string('c', AlbumPhoto.MAX_CAPTION_LENGTH + 1);

        // Act
        var result = album.AddPhoto(
            Guid.NewGuid(), "User",
            "https://blob.azure.com/original.jpg", "original.jpg",
            "https://blob.azure.com/thumb.webp", "thumb.webp",
            "https://blob.azure.com/medium.webp", "medium.webp",
            longCaption, 1024L);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Caption");
        result.Error.Should().Contain($"{AlbumPhoto.MAX_CAPTION_LENGTH}");
    }

    [Fact]
    public void AddPhoto_WithCaptionAtMaxLength_ShouldSucceed()
    {
        // Arrange
        var album = CreatePublishedAlbum();
        var maxCaption = new string('c', AlbumPhoto.MAX_CAPTION_LENGTH);

        // Act
        var result = album.AddPhoto(
            Guid.NewGuid(), "User",
            "https://blob.azure.com/original.jpg", "original.jpg",
            "https://blob.azure.com/thumb.webp", "thumb.webp",
            "https://blob.azure.com/medium.webp", "medium.webp",
            maxCaption, 1024L);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Caption.Should().HaveLength(AlbumPhoto.MAX_CAPTION_LENGTH);
    }

    #endregion

    #region RemovePhoto Tests

    [Fact]
    public void RemovePhoto_ByOrganizer_ShouldSucceed()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var album = CreatePublishedAlbum(organizerId: organizerId);
        var uploaderId = Guid.NewGuid();
        var photoResult = AddTestPhoto(album, uploaderId: uploaderId);
        var photoId = photoResult.Value.Id;
        album.ClearDomainEvents();

        // Act
        var result = album.RemovePhoto(photoId, organizerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(photoId);
        album.Photos.Should().BeEmpty();
    }

    [Fact]
    public void RemovePhoto_ByUploader_ShouldSucceed()
    {
        // Arrange
        var album = CreatePublishedAlbum();
        var uploaderId = Guid.NewGuid();
        var photoResult = AddTestPhoto(album, uploaderId: uploaderId);
        var photoId = photoResult.Value.Id;

        // Act
        var result = album.RemovePhoto(photoId, uploaderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(photoId);
        album.Photos.Should().BeEmpty();
    }

    [Fact]
    public void RemovePhoto_ByNonOrganizerNonUploader_ShouldFail()
    {
        // Arrange
        var album = CreatePublishedAlbum();
        var uploaderId = Guid.NewGuid();
        var randomUserId = Guid.NewGuid();
        var photoResult = AddTestPhoto(album, uploaderId: uploaderId);
        var photoId = photoResult.Value.Id;

        // Act
        var result = album.RemovePhoto(photoId, randomUserId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("uploader");
        result.Error.Should().Contain("organizer");
    }

    [Fact]
    public void RemovePhoto_ShouldDecrementPhotoCount_WhenPhotoWasApproved()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var album = CreatePublishedAlbum(
            organizerId: organizerId,
            moderationMode: AlbumModerationMode.PostModeration);

        AddTestPhoto(album, photoIndex: 1);
        AddTestPhoto(album, photoIndex: 2);
        album.PhotoCount.Should().Be(2);

        var photoToRemove = album.Photos.First();

        // Act
        album.RemovePhoto(photoToRemove.Id, organizerId);

        // Assert
        album.PhotoCount.Should().Be(1);
    }

    [Fact]
    public void RemovePhoto_ShouldNotDecrementPhotoCount_WhenPhotoWasPending()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var album = CreatePublishedAlbum(
            organizerId: organizerId,
            moderationMode: AlbumModerationMode.PreApproval);

        AddTestPhoto(album, photoIndex: 1);
        album.PhotoCount.Should().Be(0, "pending photos are not counted");

        var photoToRemove = album.Photos.First();

        // Act
        album.RemovePhoto(photoToRemove.Id, organizerId);

        // Assert
        album.PhotoCount.Should().Be(0);
    }

    [Fact]
    public void RemovePhoto_PhotoNotFound_ShouldFail()
    {
        // Arrange
        var album = CreatePublishedAlbum();
        var nonExistentPhotoId = Guid.NewGuid();

        // Act
        var result = album.RemovePhoto(nonExistentPhotoId, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public void RemovePhoto_ShouldReturnRemovedPhoto()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var album = CreatePublishedAlbum(organizerId: organizerId);
        var photoResult = AddTestPhoto(album, uploaderName: "Jane Doe");
        var photoId = photoResult.Value.Id;

        // Act
        var result = album.RemovePhoto(photoId, organizerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(photoId);
        result.Value.UploaderName.Should().Be("Jane Doe");
    }

    [Fact]
    public void RemovePhoto_PhotoCountShouldNotGoBelowZero()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var album = CreatePublishedAlbum(
            organizerId: organizerId,
            moderationMode: AlbumModerationMode.PostModeration);

        var photoResult = AddTestPhoto(album);
        album.PhotoCount.Should().Be(1);

        // Act
        album.RemovePhoto(photoResult.Value.Id, organizerId);

        // Assert
        album.PhotoCount.Should().Be(0);
    }

    #endregion

    #region ApprovePhoto Tests

    [Fact]
    public void ApprovePhoto_PendingApprovalPhoto_ShouldSucceed()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var album = CreatePublishedAlbum(
            organizerId: organizerId,
            moderationMode: AlbumModerationMode.PreApproval);

        var photoResult = AddTestPhoto(album);
        photoResult.Value.Status.Should().Be(AlbumPhotoStatus.PendingApproval);
        album.ClearDomainEvents();

        // Act
        var result = album.ApprovePhoto(photoResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        photoResult.Value.Status.Should().Be(AlbumPhotoStatus.Approved);
    }

    [Fact]
    public void ApprovePhoto_ShouldIncrementPhotoCount()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var album = CreatePublishedAlbum(
            organizerId: organizerId,
            moderationMode: AlbumModerationMode.PreApproval);

        var photoResult = AddTestPhoto(album);
        album.PhotoCount.Should().Be(0, "pending photo should not be counted yet");

        // Act
        album.ApprovePhoto(photoResult.Value.Id);

        // Assert
        album.PhotoCount.Should().Be(1, "approved photo should now be counted");
    }

    [Fact]
    public void ApprovePhoto_AlreadyApprovedPhoto_ShouldFail()
    {
        // Arrange
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PostModeration);
        var photoResult = AddTestPhoto(album);
        photoResult.Value.Status.Should().Be(AlbumPhotoStatus.Approved);

        // Act
        var result = album.ApprovePhoto(photoResult.Value.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not pending approval");
    }

    [Fact]
    public void ApprovePhoto_RejectedPhoto_ShouldFail()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var album = CreatePublishedAlbum(
            organizerId: organizerId,
            moderationMode: AlbumModerationMode.PreApproval);

        var photoResult = AddTestPhoto(album);
        album.RejectPhoto(photoResult.Value.Id);

        // Act
        var result = album.ApprovePhoto(photoResult.Value.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not pending approval");
    }

    [Fact]
    public void ApprovePhoto_PhotoNotFound_ShouldFail()
    {
        // Arrange
        var album = CreatePublishedAlbum();
        var nonExistentPhotoId = Guid.NewGuid();

        // Act
        var result = album.ApprovePhoto(nonExistentPhotoId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public void ApprovePhoto_ShouldRaisePhotoApprovedDomainEvent()
    {
        // Arrange
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PreApproval);
        var photoResult = AddTestPhoto(album);
        album.ClearDomainEvents();

        // Act
        album.ApprovePhoto(photoResult.Value.Id);

        // Assert
        album.DomainEvents.Should().ContainSingle(e => e is PhotoApprovedDomainEvent);
        var domainEvent = album.DomainEvents
            .OfType<PhotoApprovedDomainEvent>().Single();
        domainEvent.AlbumId.Should().Be(album.Id);
        domainEvent.PhotoId.Should().Be(photoResult.Value.Id);
    }

    [Fact]
    public void ApprovePhoto_MultiplePhotos_ShouldTrackCountCorrectly()
    {
        // Arrange
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PreApproval);

        var photo1 = AddTestPhoto(album, photoIndex: 1).Value;
        var photo2 = AddTestPhoto(album, photoIndex: 2).Value;
        var photo3 = AddTestPhoto(album, photoIndex: 3).Value;
        album.PhotoCount.Should().Be(0);

        // Act - Approve two out of three
        album.ApprovePhoto(photo1.Id);
        album.ApprovePhoto(photo3.Id);

        // Assert
        album.PhotoCount.Should().Be(2);
        photo1.Status.Should().Be(AlbumPhotoStatus.Approved);
        photo2.Status.Should().Be(AlbumPhotoStatus.PendingApproval);
        photo3.Status.Should().Be(AlbumPhotoStatus.Approved);
    }

    #endregion

    #region RejectPhoto Tests

    [Fact]
    public void RejectPhoto_PendingApprovalPhoto_ShouldSucceed()
    {
        // Arrange
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PreApproval);
        var photoResult = AddTestPhoto(album);
        photoResult.Value.Status.Should().Be(AlbumPhotoStatus.PendingApproval);
        album.ClearDomainEvents();

        // Act
        var result = album.RejectPhoto(photoResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(AlbumPhotoStatus.Rejected);
    }

    [Fact]
    public void RejectPhoto_AlreadyRejectedPhoto_ShouldFail()
    {
        // Arrange
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PreApproval);
        var photoResult = AddTestPhoto(album);
        album.RejectPhoto(photoResult.Value.Id);

        // Act
        var result = album.RejectPhoto(photoResult.Value.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not pending approval");
    }

    [Fact]
    public void RejectPhoto_ApprovedPhoto_ShouldFail()
    {
        // Arrange
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PostModeration);
        var photoResult = AddTestPhoto(album);
        photoResult.Value.Status.Should().Be(AlbumPhotoStatus.Approved);

        // Act
        var result = album.RejectPhoto(photoResult.Value.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not pending approval");
    }

    [Fact]
    public void RejectPhoto_PhotoNotFound_ShouldFail()
    {
        // Arrange
        var album = CreatePublishedAlbum();

        // Act
        var result = album.RejectPhoto(Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public void RejectPhoto_ShouldRaisePhotoRejectedDomainEvent()
    {
        // Arrange
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PreApproval);
        var photoResult = AddTestPhoto(album);
        album.ClearDomainEvents();

        // Act
        album.RejectPhoto(photoResult.Value.Id);

        // Assert
        album.DomainEvents.Should().ContainSingle(e => e is PhotoRejectedDomainEvent);
        var domainEvent = album.DomainEvents
            .OfType<PhotoRejectedDomainEvent>().Single();
        domainEvent.AlbumId.Should().Be(album.Id);
        domainEvent.PhotoId.Should().Be(photoResult.Value.Id);
    }

    [Fact]
    public void RejectPhoto_ShouldReturnRejectedPhoto()
    {
        // Arrange
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PreApproval);
        var photoResult = AddTestPhoto(album, uploaderName: "Bad Actor");

        // Act
        var result = album.RejectPhoto(photoResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UploaderName.Should().Be("Bad Actor");
        result.Value.Status.Should().Be(AlbumPhotoStatus.Rejected);
    }

    [Fact]
    public void RejectPhoto_ShouldNotAffectPhotoCount()
    {
        // Arrange - PreApproval: photo starts as PendingApproval (count=0)
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PreApproval);
        AddTestPhoto(album);
        album.PhotoCount.Should().Be(0);

        var photoId = album.Photos.First().Id;

        // Act
        album.RejectPhoto(photoId);

        // Assert
        album.PhotoCount.Should().Be(0, "rejecting a pending photo should not change count");
    }

    #endregion

    #region SetCoverPhoto Tests

    [Fact]
    public void SetCoverPhoto_ApprovedPhoto_ShouldSucceed()
    {
        // Arrange
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PostModeration);
        var photoResult = album.AddPhoto(
            Guid.NewGuid(), "Cover Artist",
            "https://blob.azure.com/original.jpg", "original.jpg",
            "https://blob.azure.com/thumb.webp", "thumb.webp",
            "https://blob.azure.com/medium_cover.webp", "medium_cover.webp",
            "Perfect cover", 2048L);
        photoResult.Value.Status.Should().Be(AlbumPhotoStatus.Approved);

        // Act
        var result = album.SetCoverPhoto(photoResult.Value.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        album.CoverPhotoUrl.Should().Be("https://blob.azure.com/medium_cover.webp");
    }

    [Fact]
    public void SetCoverPhoto_PendingApprovalPhoto_ShouldFail()
    {
        // Arrange
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PreApproval);
        var photoResult = AddTestPhoto(album);
        photoResult.Value.Status.Should().Be(AlbumPhotoStatus.PendingApproval);

        // Act
        var result = album.SetCoverPhoto(photoResult.Value.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("approved");
    }

    [Fact]
    public void SetCoverPhoto_RejectedPhoto_ShouldFail()
    {
        // Arrange
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PreApproval);
        var photoResult = AddTestPhoto(album);
        album.RejectPhoto(photoResult.Value.Id);

        // Act
        var result = album.SetCoverPhoto(photoResult.Value.Id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("approved");
    }

    [Fact]
    public void SetCoverPhoto_PhotoNotFound_ShouldFail()
    {
        // Arrange
        var album = CreatePublishedAlbum();

        // Act
        var result = album.SetCoverPhoto(Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public void SetCoverPhoto_ShouldUseMediumUrl()
    {
        // Arrange
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PostModeration);
        var photoResult = album.AddPhoto(
            Guid.NewGuid(), "User",
            "https://blob.azure.com/original.jpg", "original.jpg",
            "https://blob.azure.com/thumb.webp", "thumb.webp",
            "https://blob.azure.com/medium_800px.webp", "medium_800px.webp",
            null, 1024L);

        // Act
        album.SetCoverPhoto(photoResult.Value.Id);

        // Assert
        album.CoverPhotoUrl.Should().Be(photoResult.Value.MediumUrl);
        album.CoverPhotoUrl.Should().Be("https://blob.azure.com/medium_800px.webp");
    }

    [Fact]
    public void SetCoverPhoto_ShouldOverridePreviousCover()
    {
        // Arrange
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PostModeration);

        var photo1 = album.AddPhoto(
            Guid.NewGuid(), "User1",
            "https://blob.azure.com/original1.jpg", "original1.jpg",
            "https://blob.azure.com/thumb1.webp", "thumb1.webp",
            "https://blob.azure.com/medium1.webp", "medium1.webp",
            null, 1024L).Value;

        var photo2 = album.AddPhoto(
            Guid.NewGuid(), "User2",
            "https://blob.azure.com/original2.jpg", "original2.jpg",
            "https://blob.azure.com/thumb2.webp", "thumb2.webp",
            "https://blob.azure.com/medium2.webp", "medium2.webp",
            null, 2048L).Value;

        // Set first photo as cover
        album.SetCoverPhoto(photo1.Id);
        album.CoverPhotoUrl.Should().Be("https://blob.azure.com/medium1.webp");

        // Act - Override with second photo
        album.SetCoverPhoto(photo2.Id);

        // Assert
        album.CoverPhotoUrl.Should().Be("https://blob.azure.com/medium2.webp");
    }

    #endregion

    #region DecrementPhotoCount Tests

    [Fact]
    public void DecrementPhotoCount_ShouldDecreaseCount()
    {
        // Arrange
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PostModeration);
        AddTestPhoto(album, photoIndex: 1);
        AddTestPhoto(album, photoIndex: 2);
        AddTestPhoto(album, photoIndex: 3);
        album.PhotoCount.Should().Be(3);

        // Act
        album.DecrementPhotoCount(2);

        // Assert
        album.PhotoCount.Should().Be(1);
    }

    [Fact]
    public void DecrementPhotoCount_ShouldNotGoBelowZero()
    {
        // Arrange
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PostModeration);
        AddTestPhoto(album, photoIndex: 1);
        album.PhotoCount.Should().Be(1);

        // Act
        album.DecrementPhotoCount(5);

        // Assert
        album.PhotoCount.Should().Be(0);
    }

    [Fact]
    public void DecrementPhotoCount_DefaultDecrement_ShouldBeOne()
    {
        // Arrange
        var album = CreatePublishedAlbum(moderationMode: AlbumModerationMode.PostModeration);
        AddTestPhoto(album, photoIndex: 1);
        AddTestPhoto(album, photoIndex: 2);
        album.PhotoCount.Should().Be(2);

        // Act
        album.DecrementPhotoCount();

        // Assert
        album.PhotoCount.Should().Be(1);
    }

    #endregion

    #region AlbumPhoto Entity Tests

    [Fact]
    public void AlbumPhoto_IsExpired_ShouldReturnFalse_WhenNotExpired()
    {
        // Arrange
        var album = CreatePublishedAlbum();
        var photoResult = AddTestPhoto(album);

        // Assert - Photo just created, should not be expired (retention is 7 days)
        photoResult.Value.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void AlbumPhoto_ShouldStoreAllBlobNames()
    {
        // Arrange
        var album = CreatePublishedAlbum();

        // Act
        var result = album.AddPhoto(
            Guid.NewGuid(), "User",
            "https://blob.azure.com/original.jpg", "events/123/original.jpg",
            "https://blob.azure.com/thumb.webp", "events/123/thumb.webp",
            "https://blob.azure.com/medium.webp", "events/123/medium.webp",
            null, 4096L);

        // Assert
        result.Value.OriginalBlobName.Should().Be("events/123/original.jpg");
        result.Value.ThumbnailBlobName.Should().Be("events/123/thumb.webp");
        result.Value.MediumBlobName.Should().Be("events/123/medium.webp");
    }

    [Fact]
    public void AlbumPhoto_ShouldHaveUniqueId()
    {
        // Arrange
        var album = CreatePublishedAlbum();

        // Act
        var photo1 = AddTestPhoto(album, photoIndex: 1).Value;
        var photo2 = AddTestPhoto(album, photoIndex: 2).Value;

        // Assert
        photo1.Id.Should().NotBe(photo2.Id);
        photo1.Id.Should().NotBe(Guid.Empty);
        photo2.Id.Should().NotBe(Guid.Empty);
    }

    #endregion

    #region Integration / Complex Scenario Tests

    [Fact]
    public void FullLifecycle_CreatePublishAddPhotosCloseShouldWork()
    {
        // Arrange & Act - Create
        var organizerId = Guid.NewGuid();
        var albumResult = PhotoAlbum.Create(
            Guid.NewGuid(), organizerId, "Sri Lanka Dev Conference 2026",
            "Photos from our amazing conference");
        albumResult.IsSuccess.Should().BeTrue();

        var album = albumResult.Value;
        album.UpdateSettings(
            uploadPermission: AlbumUploadPermission.RegisteredAttendees,
            moderationMode: AlbumModerationMode.PostModeration);

        // Publish
        var publishResult = album.Publish();
        publishResult.IsSuccess.Should().BeTrue();

        // Add photos
        var photo1 = AddTestPhoto(album, uploaderName: "Speaker 1", photoIndex: 1);
        var photo2 = AddTestPhoto(album, uploaderName: "Attendee A", photoIndex: 2);
        var photo3 = AddTestPhoto(album, uploaderName: "Attendee B", photoIndex: 3);

        photo1.IsSuccess.Should().BeTrue();
        photo2.IsSuccess.Should().BeTrue();
        photo3.IsSuccess.Should().BeTrue();
        album.PhotoCount.Should().Be(3);

        // Set cover
        album.SetCoverPhoto(photo1.Value.Id).IsSuccess.Should().BeTrue();

        // Close
        var closeResult = album.Close();
        closeResult.IsSuccess.Should().BeTrue();

        // Verify final state
        album.Status.Should().Be(AlbumStatus.Closed);
        album.Photos.Should().HaveCount(3);
        album.PhotoCount.Should().Be(3);
        album.CoverPhotoUrl.Should().NotBeNull();
        album.PublishedAt.Should().NotBeNull();
        album.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public void PreApprovalWorkflow_ShouldRequireApprovalBeforeCountingPhotos()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var album = CreatePublishedAlbum(
            organizerId: organizerId,
            moderationMode: AlbumModerationMode.PreApproval);

        // Act - Add photos
        var photo1 = AddTestPhoto(album, photoIndex: 1).Value;
        var photo2 = AddTestPhoto(album, photoIndex: 2).Value;
        var photo3 = AddTestPhoto(album, photoIndex: 3).Value;

        // Assert - No photos counted yet
        album.PhotoCount.Should().Be(0);
        album.Photos.Should().HaveCount(3);
        photo1.Status.Should().Be(AlbumPhotoStatus.PendingApproval);

        // Approve one, reject one
        album.ApprovePhoto(photo1.Id);
        album.RejectPhoto(photo3.Id);

        // Assert - Only approved photo counted
        album.PhotoCount.Should().Be(1);
        photo1.Status.Should().Be(AlbumPhotoStatus.Approved);
        photo2.Status.Should().Be(AlbumPhotoStatus.PendingApproval);
        photo3.Status.Should().Be(AlbumPhotoStatus.Rejected);
    }

    [Fact]
    public void RemoveApprovedAndPendingPhotos_ShouldAdjustCountCorrectly()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var album = CreatePublishedAlbum(
            organizerId: organizerId,
            moderationMode: AlbumModerationMode.PreApproval);

        var pendingPhoto = AddTestPhoto(album, photoIndex: 1).Value;
        var approvedPhoto = AddTestPhoto(album, photoIndex: 2).Value;
        album.ApprovePhoto(approvedPhoto.Id);

        album.PhotoCount.Should().Be(1);
        album.Photos.Should().HaveCount(2);

        // Act - Remove the pending photo (count should not change)
        album.RemovePhoto(pendingPhoto.Id, organizerId);
        album.PhotoCount.Should().Be(1);
        album.Photos.Should().HaveCount(1);

        // Act - Remove the approved photo (count should decrease)
        album.RemovePhoto(approvedPhoto.Id, organizerId);
        album.PhotoCount.Should().Be(0);
        album.Photos.Should().BeEmpty();
    }

    [Fact]
    public void DomainEvents_ShouldAccumulateAcrossOperations()
    {
        // Arrange
        var albumResult = PhotoAlbum.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Event");
        var album = albumResult.Value;
        // Already has PhotoAlbumCreatedDomainEvent

        // Act
        album.Publish(); // Adds PhotoAlbumPublishedDomainEvent
        var photo = AddTestPhoto(album); // Adds PhotoUploadedToAlbumDomainEvent

        // Assert - Should have all 3 domain events
        album.DomainEvents.Should().HaveCount(3);
        album.DomainEvents.Should().ContainSingle(e => e is PhotoAlbumCreatedDomainEvent);
        album.DomainEvents.Should().ContainSingle(e => e is PhotoAlbumPublishedDomainEvent);
        album.DomainEvents.Should().ContainSingle(e => e is PhotoUploadedToAlbumDomainEvent);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var albumResult = PhotoAlbum.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Event");
        var album = albumResult.Value;
        album.DomainEvents.Should().NotBeEmpty();

        // Act
        album.ClearDomainEvents();

        // Assert
        album.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ClosedAlbum_ShouldRejectAllPhotoOperations()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var album = CreatePublishedAlbum(organizerId: organizerId);
        var photoResult = AddTestPhoto(album);
        var photoId = photoResult.Value.Id;
        album.Close();

        // Act & Assert - Cannot add new photos
        var addResult = AddTestPhoto(album, photoIndex: 2);
        addResult.IsFailure.Should().BeTrue();
        addResult.Error.Should().Contain("closed");

        // Act & Assert - Cannot update settings
        var settingsResult = album.UpdateSettings(
            uploadPermission: AlbumUploadPermission.AnyAuthenticated);
        settingsResult.IsFailure.Should().BeTrue();

        // Note: Remove, Approve, Reject, SetCoverPhoto still work on closed albums
        // because they operate on existing photos, not new uploads
    }

    [Fact]
    public void MultipleUploaders_OrganizerCanRemoveAnyPhoto()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var album = CreatePublishedAlbum(organizerId: organizerId);

        var uploader1 = Guid.NewGuid();
        var uploader2 = Guid.NewGuid();

        var photo1 = AddTestPhoto(album, uploaderId: uploader1, photoIndex: 1).Value;
        var photo2 = AddTestPhoto(album, uploaderId: uploader2, photoIndex: 2).Value;

        // Act & Assert - Organizer removes photo from uploader1
        var result1 = album.RemovePhoto(photo1.Id, organizerId);
        result1.IsSuccess.Should().BeTrue();

        // Act & Assert - Organizer removes photo from uploader2
        var result2 = album.RemovePhoto(photo2.Id, organizerId);
        result2.IsSuccess.Should().BeTrue();

        album.Photos.Should().BeEmpty();
    }

    [Fact]
    public void Uploader_CannotRemoveOtherUploadersPhoto()
    {
        // Arrange
        var album = CreatePublishedAlbum();
        var uploader1 = Guid.NewGuid();
        var uploader2 = Guid.NewGuid();

        var photo1 = AddTestPhoto(album, uploaderId: uploader1, photoIndex: 1).Value;

        // Act - Uploader2 tries to remove Uploader1's photo
        var result = album.RemovePhoto(photo1.Id, uploader2);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("uploader");
        result.Error.Should().Contain("organizer");
    }

    #endregion
}
