using FluentAssertions;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Domain.Enums;
using LankaConnect.Modules.Media.Domain.DomainEvents;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.DomainEvents;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// Domain unit tests for the PhotoAlbum aggregate root and AlbumPhoto entity.
/// Covers creation with name, lifecycle (Draft→Published), photo CRUD,
/// publish guard (requires photos), and cover photo.
/// </summary>
public class PhotoAlbumTests
{
    #region Test Helpers

    private static readonly Guid DefaultEventId = Guid.NewGuid();
    private static readonly Guid DefaultOrganizerId = Guid.NewGuid();
    private const string DefaultEventTitle = "Community Meetup 2026";
    private const string DefaultAlbumName = "Event Highlights";
    private const string DefaultDescription = "Photos from the meetup";

    /// <summary>
    /// Creates a valid PhotoAlbum in Draft status with default test data.
    /// </summary>
    private static PhotoAlbum CreateDraftAlbum(
        Guid? eventId = null,
        Guid? organizerId = null,
        string? eventTitle = null,
        string? name = null,
        string? description = null)
    {
        var result = PhotoAlbum.Create(
            eventId ?? DefaultEventId,
            organizerId ?? DefaultOrganizerId,
            eventTitle ?? DefaultEventTitle,
            name ?? DefaultAlbumName,
            description);

        result.IsSuccess.Should().BeTrue("test helper should create a valid album");
        return result.Value;
    }

    /// <summary>
    /// Creates a valid PhotoAlbum in Published status (with at least one photo).
    /// </summary>
    private static PhotoAlbum CreatePublishedAlbum(Guid? organizerId = null)
    {
        var album = CreateDraftAlbum(organizerId: organizerId);
        AddTestPhoto(album);
        album.ClearDomainEvents();
        var publishResult = album.Publish();
        publishResult.IsSuccess.Should().BeTrue("test helper should publish album successfully");
        album.ClearDomainEvents();
        return album;
    }

    /// <summary>
    /// Adds a photo to an album and returns the result.
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
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        var result = PhotoAlbum.Create(
            DefaultEventId,
            DefaultOrganizerId,
            DefaultEventTitle,
            DefaultAlbumName,
            DefaultDescription);

        result.IsSuccess.Should().BeTrue();
        var album = result.Value;
        album.EventId.Should().Be(DefaultEventId);
        album.OrganizerId.Should().Be(DefaultOrganizerId);
        album.EventTitle.Should().Be(DefaultEventTitle);
        album.Name.Should().Be(DefaultAlbumName);
        album.Description.Should().Be(DefaultDescription);
        album.Status.Should().Be(AlbumStatus.Draft);
        album.RetentionDays.Should().Be(PhotoAlbum.DEFAULT_RETENTION_DAYS);
        album.PhotoCount.Should().Be(0);
        album.PublishedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithName_ShouldSetName()
    {
        var result = PhotoAlbum.Create(
            DefaultEventId,
            DefaultOrganizerId,
            DefaultEventTitle,
            "Diwali Night Photos");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Diwali Night Photos");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldFail()
    {
        var result = PhotoAlbum.Create(
            DefaultEventId,
            DefaultOrganizerId,
            DefaultEventTitle,
            "");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Album name is required");
    }

    [Fact]
    public void Create_WithWhitespaceName_ShouldFail()
    {
        var result = PhotoAlbum.Create(
            DefaultEventId,
            DefaultOrganizerId,
            DefaultEventTitle,
            "   ");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Album name is required");
    }

    [Fact]
    public void Create_WithNameExceedingMaxLength_ShouldFail()
    {
        var longName = new string('A', PhotoAlbum.MAX_NAME_LENGTH + 1);

        var result = PhotoAlbum.Create(
            DefaultEventId,
            DefaultOrganizerId,
            DefaultEventTitle,
            longName);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain($"{PhotoAlbum.MAX_NAME_LENGTH}");
    }

    [Fact]
    public void Create_WithMaxLengthName_ShouldSucceed()
    {
        var maxName = new string('A', PhotoAlbum.MAX_NAME_LENGTH);

        var result = PhotoAlbum.Create(
            DefaultEventId,
            DefaultOrganizerId,
            DefaultEventTitle,
            maxName);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(maxName);
    }

    [Fact]
    public void Create_WithEmptyEventId_ShouldFail()
    {
        var result = PhotoAlbum.Create(Guid.Empty, DefaultOrganizerId, DefaultEventTitle, DefaultAlbumName);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Event ID");
    }

    [Fact]
    public void Create_WithEmptyOrganizerId_ShouldFail()
    {
        var result = PhotoAlbum.Create(DefaultEventId, Guid.Empty, DefaultEventTitle, DefaultAlbumName);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Organizer ID");
    }

    [Fact]
    public void Create_WithEmptyEventTitle_ShouldFail()
    {
        var result = PhotoAlbum.Create(DefaultEventId, DefaultOrganizerId, "", DefaultAlbumName);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Event title");
    }

    [Fact]
    public void Create_WithDescriptionExceedingMaxLength_ShouldFail()
    {
        var longDescription = new string('A', PhotoAlbum.MAX_DESCRIPTION_LENGTH + 1);
        var result = PhotoAlbum.Create(
            DefaultEventId, DefaultOrganizerId, DefaultEventTitle, DefaultAlbumName, longDescription);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Description");
    }

    [Fact]
    public void Create_ShouldNotRaiseDomainEvent()
    {
        var result = PhotoAlbum.Create(
            DefaultEventId, DefaultOrganizerId, DefaultEventTitle, DefaultAlbumName);

        result.IsSuccess.Should().BeTrue();
        result.Value.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region Publish Tests

    [Fact]
    public void Publish_WithPhotos_ShouldSucceedAndRaiseDomainEvent()
    {
        var album = CreateDraftAlbum();
        AddTestPhoto(album);
        album.ClearDomainEvents();

        var result = album.Publish();

        result.IsSuccess.Should().BeTrue();
        album.Status.Should().Be(AlbumStatus.Published);
        album.PublishedAt.Should().NotBeNull();
        album.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PhotoAlbumPublishedDomainEvent>()
            .Which.AlbumName.Should().Be(DefaultAlbumName);
    }

    [Fact]
    public void Publish_WithNoPhotos_ShouldFail()
    {
        var album = CreateDraftAlbum();

        var result = album.Publish();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Upload at least one photo");
        album.Status.Should().Be(AlbumStatus.Draft);
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_ShouldFail()
    {
        var album = CreatePublishedAlbum();

        var result = album.Publish();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already published");
    }

    [Fact]
    public void Publish_DomainEvent_ShouldContainAlbumNameAndEventTitle()
    {
        var album = CreateDraftAlbum(name: "My Special Album");
        AddTestPhoto(album);
        album.ClearDomainEvents();

        album.Publish();

        var domainEvent = album.DomainEvents.OfType<PhotoAlbumPublishedDomainEvent>().Single();
        domainEvent.AlbumName.Should().Be("My Special Album");
        domainEvent.EventTitle.Should().Be(DefaultEventTitle);
        domainEvent.EventId.Should().Be(DefaultEventId);
    }

    #endregion

    #region UpdateDetails Tests

    [Fact]
    public void UpdateDetails_ShouldChangeName()
    {
        var album = CreateDraftAlbum();

        var result = album.UpdateDetails("New Album Name");

        result.IsSuccess.Should().BeTrue();
        album.Name.Should().Be("New Album Name");
    }

    [Fact]
    public void UpdateDetails_ShouldChangeDescription()
    {
        var album = CreateDraftAlbum();

        var result = album.UpdateDetails(DefaultAlbumName, "New description");

        result.IsSuccess.Should().BeTrue();
        album.Description.Should().Be("New description");
    }

    [Fact]
    public void UpdateDetails_WithEmptyName_ShouldFail()
    {
        var album = CreateDraftAlbum();

        var result = album.UpdateDetails("");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Album name is required");
    }

    [Fact]
    public void UpdateDetails_WithNameExceedingMaxLength_ShouldFail()
    {
        var album = CreateDraftAlbum();
        var longName = new string('A', PhotoAlbum.MAX_NAME_LENGTH + 1);

        var result = album.UpdateDetails(longName);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void UpdateDetails_OnPublishedAlbum_ShouldSucceed()
    {
        var album = CreatePublishedAlbum();

        var result = album.UpdateDetails("Updated Name");

        result.IsSuccess.Should().BeTrue();
        album.Name.Should().Be("Updated Name");
    }

    #endregion

    #region AddPhoto Tests

    [Fact]
    public void AddPhoto_ToDraftAlbum_ShouldSucceedWithoutAutoPublish()
    {
        var album = CreateDraftAlbum();

        var result = AddTestPhoto(album);

        result.IsSuccess.Should().BeTrue();
        album.Status.Should().Be(AlbumStatus.Draft, "Draft status should be preserved — no auto-publish");
        album.PhotoCount.Should().Be(1);
        album.PublishedAt.Should().BeNull();
    }

    [Fact]
    public void AddPhoto_ToPublishedAlbum_ShouldSucceed()
    {
        var album = CreatePublishedAlbum();

        var result = AddTestPhoto(album, photoIndex: 2);

        result.IsSuccess.Should().BeTrue();
        album.Status.Should().Be(AlbumStatus.Published);
    }

    [Fact]
    public void AddPhoto_ShouldAlwaysBeApproved()
    {
        var album = CreateDraftAlbum();

        var result = AddTestPhoto(album);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(AlbumPhotoStatus.Approved);
    }

    [Fact]
    public void AddPhoto_ShouldIncrementPhotoCount()
    {
        var album = CreateDraftAlbum();
        album.PhotoCount.Should().Be(0);

        AddTestPhoto(album, photoIndex: 1);
        album.PhotoCount.Should().Be(1);

        AddTestPhoto(album, photoIndex: 2);
        album.PhotoCount.Should().Be(2);
    }

    [Fact]
    public void AddPhoto_ShouldRaiseUploadedDomainEvent()
    {
        var album = CreateDraftAlbum();
        album.ClearDomainEvents();

        var uploaderId = Guid.NewGuid();
        AddTestPhoto(album, uploaderId: uploaderId);

        album.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PhotoUploadedToAlbumDomainEvent>()
            .Which.UploaderId.Should().Be(uploaderId);
    }

    [Fact]
    public void AddPhoto_AtMaxCapacity_ShouldFail()
    {
        var album = CreateDraftAlbum();

        for (int i = 0; i < PhotoAlbum.MAX_PHOTOS; i++)
        {
            var result = AddTestPhoto(album, photoIndex: i + 1);
            result.IsSuccess.Should().BeTrue($"photo {i + 1} should succeed");
        }

        var overflowResult = AddTestPhoto(album, photoIndex: PhotoAlbum.MAX_PHOTOS + 1);
        overflowResult.IsSuccess.Should().BeFalse();
        overflowResult.Error.Should().Contain("maximum");
    }

    [Fact]
    public void AddPhoto_ShouldSetCorrectDisplayOrder()
    {
        var album = CreateDraftAlbum();

        var result1 = AddTestPhoto(album, photoIndex: 1);
        var result2 = AddTestPhoto(album, photoIndex: 2);

        result1.Value.DisplayOrder.Should().Be(1);
        result2.Value.DisplayOrder.Should().Be(2);
    }

    [Fact]
    public void AddPhoto_ShouldSetExpiryBasedOnRetentionDays()
    {
        var album = CreateDraftAlbum();
        var beforeAdd = DateTime.UtcNow;

        var result = AddTestPhoto(album);

        result.Value.ExpiresAt.Should().BeCloseTo(
            beforeAdd.AddDays(PhotoAlbum.DEFAULT_RETENTION_DAYS),
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AddPhoto_WithCaption_ShouldStoreCaption()
    {
        var album = CreateDraftAlbum();

        var result = AddTestPhoto(album, caption: "Beautiful sunset");

        result.IsSuccess.Should().BeTrue();
        result.Value.Caption.Should().Be("Beautiful sunset");
    }

    [Fact]
    public void AddPhoto_MultipleToDraft_ShouldRemainDraft()
    {
        var album = CreateDraftAlbum();

        AddTestPhoto(album, photoIndex: 1);
        AddTestPhoto(album, photoIndex: 2);
        AddTestPhoto(album, photoIndex: 3);

        album.Status.Should().Be(AlbumStatus.Draft, "Adding photos should NOT auto-publish");
        album.PhotoCount.Should().Be(3);
    }

    #endregion

    #region RemovePhoto Tests

    [Fact]
    public void RemovePhoto_ByUploader_ShouldSucceed()
    {
        var album = CreateDraftAlbum();
        var uploaderId = Guid.NewGuid();
        var addResult = AddTestPhoto(album, uploaderId: uploaderId);
        var photoId = addResult.Value.Id;

        var result = album.RemovePhoto(photoId, uploaderId);

        result.IsSuccess.Should().BeTrue();
        album.PhotoCount.Should().Be(0);
        album.Photos.Should().BeEmpty();
    }

    [Fact]
    public void RemovePhoto_ByOrganizer_ShouldSucceed()
    {
        var album = CreateDraftAlbum(organizerId: DefaultOrganizerId);
        var uploaderId = Guid.NewGuid();
        var addResult = AddTestPhoto(album, uploaderId: uploaderId);

        var result = album.RemovePhoto(addResult.Value.Id, DefaultOrganizerId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void RemovePhoto_ByUnauthorizedUser_ShouldFail()
    {
        var album = CreateDraftAlbum();
        var uploaderId = Guid.NewGuid();
        var addResult = AddTestPhoto(album, uploaderId: uploaderId);
        var unauthorizedUser = Guid.NewGuid();

        var result = album.RemovePhoto(addResult.Value.Id, unauthorizedUser);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Only the photo uploader or event organizer");
    }

    [Fact]
    public void RemovePhoto_NonExistent_ShouldFail()
    {
        var album = CreateDraftAlbum();

        var result = album.RemovePhoto(Guid.NewGuid(), DefaultOrganizerId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public void RemovePhoto_ShouldDecrementPhotoCount()
    {
        var album = CreateDraftAlbum();
        var uploaderId = Guid.NewGuid();
        AddTestPhoto(album, uploaderId: uploaderId, photoIndex: 1);
        AddTestPhoto(album, uploaderId: uploaderId, photoIndex: 2);
        album.PhotoCount.Should().Be(2);

        album.RemovePhoto(album.Photos[0].Id, uploaderId);
        album.PhotoCount.Should().Be(1);
    }

    [Fact]
    public void RemovePhoto_FromPublishedAlbum_ShouldSucceed()
    {
        var album = CreatePublishedAlbum();
        var uploaderId = Guid.NewGuid();
        var addResult = AddTestPhoto(album, uploaderId: uploaderId, photoIndex: 2);

        var result = album.RemovePhoto(addResult.Value.Id, uploaderId);

        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region SetCoverPhoto Tests

    [Fact]
    public void SetCoverPhoto_WithValidPhoto_ShouldSucceed()
    {
        var album = CreateDraftAlbum();
        var addResult = AddTestPhoto(album);

        var result = album.SetCoverPhoto(addResult.Value.Id);

        result.IsSuccess.Should().BeTrue();
        album.CoverPhotoUrl.Should().Be(addResult.Value.MediumUrl);
    }

    [Fact]
    public void SetCoverPhoto_NonExistent_ShouldFail()
    {
        var album = CreateDraftAlbum();

        var result = album.SetCoverPhoto(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    #endregion

    #region DecrementPhotoCount Tests

    [Fact]
    public void DecrementPhotoCount_ShouldReduceCount()
    {
        var album = CreateDraftAlbum();
        AddTestPhoto(album, photoIndex: 1);
        AddTestPhoto(album, photoIndex: 2);
        album.PhotoCount.Should().Be(2);

        album.DecrementPhotoCount(1);
        album.PhotoCount.Should().Be(1);
    }

    [Fact]
    public void DecrementPhotoCount_ShouldNotGoBelowZero()
    {
        var album = CreateDraftAlbum();

        album.DecrementPhotoCount(5);
        album.PhotoCount.Should().Be(0);
    }

    #endregion

    #region RemovePhotos (Batch) Tests

    [Fact]
    public void RemovePhotos_ByOrganizer_ShouldRemoveAll()
    {
        var album = CreateDraftAlbum(organizerId: DefaultOrganizerId);
        var photo1 = AddTestPhoto(album, photoIndex: 1).Value;
        var photo2 = AddTestPhoto(album, photoIndex: 2).Value;
        var photo3 = AddTestPhoto(album, photoIndex: 3).Value;
        album.PhotoCount.Should().Be(3);

        var result = album.RemovePhotos(
            new List<Guid> { photo1.Id, photo2.Id, photo3.Id },
            DefaultOrganizerId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        album.PhotoCount.Should().Be(0);
        album.Photos.Should().BeEmpty();
    }

    [Fact]
    public void RemovePhotos_ByNonOrganizer_ShouldFail()
    {
        var album = CreateDraftAlbum(organizerId: DefaultOrganizerId);
        var photo1 = AddTestPhoto(album, photoIndex: 1).Value;
        var nonOrganizer = Guid.NewGuid();

        var result = album.RemovePhotos(
            new List<Guid> { photo1.Id },
            nonOrganizer);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("organizer");
        album.PhotoCount.Should().Be(1, "photos should not be removed on failure");
    }

    [Fact]
    public void RemovePhotos_WithSomeMissing_ShouldRemoveFoundOnly()
    {
        var album = CreateDraftAlbum(organizerId: DefaultOrganizerId);
        var photo1 = AddTestPhoto(album, photoIndex: 1).Value;
        AddTestPhoto(album, photoIndex: 2);
        album.PhotoCount.Should().Be(2);

        var missingId = Guid.NewGuid();
        var result = album.RemovePhotos(
            new List<Guid> { photo1.Id, missingId },
            DefaultOrganizerId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Id.Should().Be(photo1.Id);
        album.PhotoCount.Should().Be(1);
    }

    [Fact]
    public void RemovePhotos_EmptyList_ShouldReturnEmpty()
    {
        var album = CreateDraftAlbum(organizerId: DefaultOrganizerId);
        AddTestPhoto(album, photoIndex: 1);

        var result = album.RemovePhotos(
            new List<Guid>(),
            DefaultOrganizerId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        album.PhotoCount.Should().Be(1, "no photos should be removed");
    }

    [Fact]
    public void RemovePhotos_ShouldDecrementPhotoCountCorrectly()
    {
        var album = CreateDraftAlbum(organizerId: DefaultOrganizerId);
        var photo1 = AddTestPhoto(album, photoIndex: 1).Value;
        AddTestPhoto(album, photoIndex: 2);
        AddTestPhoto(album, photoIndex: 3);
        album.PhotoCount.Should().Be(3);

        album.RemovePhotos(new List<Guid> { photo1.Id }, DefaultOrganizerId);

        album.PhotoCount.Should().Be(2);
    }

    #endregion

    #region AlbumPhoto Entity Tests

    [Fact]
    public void AlbumPhoto_ShouldTrackIsExpired()
    {
        var album = CreateDraftAlbum();
        var result = AddTestPhoto(album);

        result.Value.IsExpired.Should().BeFalse();
    }

    #endregion
}
