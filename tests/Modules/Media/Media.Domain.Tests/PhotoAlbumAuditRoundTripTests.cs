using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Enums;

namespace LankaConnect.Modules.Media.Domain.Tests;

/// <summary>
/// Wave4.9.1.6 (2026-06-08): per-mutator round-trip audit-field tests for
/// the PhotoAlbum aggregate. Each mutator that materially changes state
/// MUST set <c>UpdatedAt = DateTime.UtcNow</c>; this is the same invariant
/// fixed in commits d4e27b54 + 035148d6 for VenueLayout/Decoration/Table.
///
/// Pattern: CREATE -> assert CreatedAt fresh, UpdatedAt null -> MUTATE ->
/// re-assert UpdatedAt &gt; CreatedAt.
/// </summary>
/// <remarks>
/// Per CLAUDE.md §13.1 trigger T2 (mutator touching IAuditable).
/// </remarks>
public sealed class PhotoAlbumAuditRoundTripTests
{
    private const string SampleEventTitle = "Wave4.9.1.6 Round-Trip Event";
    private const string SampleAlbumName = "Wave4.9.1.6 Album";

    private static PhotoAlbum NewAlbum()
    {
        var result = PhotoAlbum.Create(
            eventId: Guid.NewGuid(),
            organizerId: Guid.NewGuid(),
            eventTitle: SampleEventTitle,
            name: SampleAlbumName,
            description: "round-trip seed");
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static (string OriginalUrl, string OriginalBlob, string ThumbUrl, string ThumbBlob, string MedUrl, string MedBlob) SamplePhotoArgs()
        => ("https://cdn/orig.jpg", "blob/orig.jpg", "https://cdn/thumb.jpg", "blob/thumb.jpg", "https://cdn/med.jpg", "blob/med.jpg");

    [Fact]
    public void Create_Sets_CreatedAt_And_Leaves_UpdatedAt_Null()
    {
        var before = DateTime.UtcNow;
        var album = NewAlbum();

        album.CreatedAt.Should().BeOnOrAfter(before.AddSeconds(-1));
        album.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
        album.UpdatedAt.Should().BeNull(because: "freshly-created entity has not been mutated");
    }

    [Fact]
    public void UpdateDetails_Advances_UpdatedAt()
    {
        var album = NewAlbum();
        var createdAt = album.CreatedAt;
        Thread.Sleep(20);                          // ensure measurable tick

        var result = album.UpdateDetails(name: "Wave4.9.1.6 Renamed", description: "round-trip mutation");

        result.IsSuccess.Should().BeTrue();
        album.UpdatedAt.Should().NotBeNull();
        album.UpdatedAt!.Value.Should().BeAfter(createdAt,
            because: "UpdateDetails is a state-mutating action and must advance UpdatedAt for the audit trail.");
    }

    [Fact]
    public void Publish_Advances_UpdatedAt()
    {
        var album = NewAlbum();
        var p = SamplePhotoArgs();
        // Publish requires at least one photo
        album.AddPhoto(
            uploaderId: album.OrganizerId, uploaderName: "Org",
            originalUrl: p.OriginalUrl, originalBlobName: p.OriginalBlob,
            thumbnailUrl: p.ThumbUrl, thumbnailBlobName: p.ThumbBlob,
            mediumUrl: p.MedUrl, mediumBlobName: p.MedBlob,
            caption: "seed photo", fileSizeBytes: 100).IsSuccess.Should().BeTrue();
        var createdAt = album.CreatedAt;
        Thread.Sleep(20);

        var result = album.Publish();

        result.IsSuccess.Should().BeTrue();
        album.Status.Should().Be(AlbumStatus.Published);
        album.UpdatedAt.Should().NotBeNull();
        album.UpdatedAt!.Value.Should().BeAfter(createdAt);
    }

    [Fact]
    public void AddPhoto_Advances_UpdatedAt()
    {
        var album = NewAlbum();
        var createdAt = album.CreatedAt;
        Thread.Sleep(20);
        var p = SamplePhotoArgs();

        var result = album.AddPhoto(
            uploaderId: album.OrganizerId, uploaderName: "Org",
            originalUrl: p.OriginalUrl, originalBlobName: p.OriginalBlob,
            thumbnailUrl: p.ThumbUrl, thumbnailBlobName: p.ThumbBlob,
            mediumUrl: p.MedUrl, mediumBlobName: p.MedBlob,
            caption: "first", fileSizeBytes: 100);

        result.IsSuccess.Should().BeTrue();
        album.PhotoCount.Should().Be(1);
        album.UpdatedAt.Should().NotBeNull();
        album.UpdatedAt!.Value.Should().BeAfter(createdAt);
    }

    [Fact]
    public void AddVideo_Advances_UpdatedAt()
    {
        var album = NewAlbum();
        var createdAt = album.CreatedAt;
        Thread.Sleep(20);

        var result = album.AddVideo(
            uploaderId: album.OrganizerId, uploaderName: "Org",
            originalUrl: "https://cdn/v.mp4", originalBlobName: "blob/v.mp4",
            thumbnailUrl: "https://cdn/v-thumb.jpg", thumbnailBlobName: "blob/v-thumb.jpg",
            caption: null, fileSizeBytes: 200, durationSeconds: 30);

        result.IsSuccess.Should().BeTrue();
        album.PhotoCount.Should().Be(1);
        album.UpdatedAt.Should().NotBeNull();
        album.UpdatedAt!.Value.Should().BeAfter(createdAt);
    }

    [Fact]
    public void RemovePhoto_Advances_UpdatedAt()
    {
        var album = NewAlbum();
        var p = SamplePhotoArgs();
        var photoResult = album.AddPhoto(
            uploaderId: album.OrganizerId, uploaderName: "Org",
            originalUrl: p.OriginalUrl, originalBlobName: p.OriginalBlob,
            thumbnailUrl: p.ThumbUrl, thumbnailBlobName: p.ThumbBlob,
            mediumUrl: p.MedUrl, mediumBlobName: p.MedBlob,
            caption: "to-remove", fileSizeBytes: 100);
        photoResult.IsSuccess.Should().BeTrue();
        var photoId = photoResult.Value.Id;
        var addUpdatedAt = album.UpdatedAt;
        Thread.Sleep(20);

        var result = album.RemovePhoto(photoId, requesterId: album.OrganizerId);

        result.IsSuccess.Should().BeTrue();
        album.PhotoCount.Should().Be(0);
        album.UpdatedAt.Should().NotBeNull();
        album.UpdatedAt!.Value.Should().BeAfter(addUpdatedAt!.Value,
            because: "RemovePhoto mutates state (photo collection + count) and must advance UpdatedAt past the prior AddPhoto timestamp.");
    }

    [Fact]
    public void SetCoverPhoto_Advances_UpdatedAt()
    {
        var album = NewAlbum();
        var p = SamplePhotoArgs();
        var photoResult = album.AddPhoto(
            uploaderId: album.OrganizerId, uploaderName: "Org",
            originalUrl: p.OriginalUrl, originalBlobName: p.OriginalBlob,
            thumbnailUrl: p.ThumbUrl, thumbnailBlobName: p.ThumbBlob,
            mediumUrl: p.MedUrl, mediumBlobName: p.MedBlob,
            caption: "cover-candidate", fileSizeBytes: 100);
        photoResult.IsSuccess.Should().BeTrue();
        var addUpdatedAt = album.UpdatedAt;
        Thread.Sleep(20);

        var result = album.SetCoverPhoto(photoResult.Value.Id);

        result.IsSuccess.Should().BeTrue();
        album.CoverPhotoUrl.Should().NotBeNull();
        album.UpdatedAt.Should().NotBeNull();
        album.UpdatedAt!.Value.Should().BeAfter(addUpdatedAt!.Value);
    }
}
