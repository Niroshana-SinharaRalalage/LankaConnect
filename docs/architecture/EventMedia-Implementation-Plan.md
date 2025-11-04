# Event Media Implementation Plan

## Overview
This document provides a detailed, step-by-step implementation plan for adding Image Replace and Video Support features using TDD methodology.

## Implementation Phases

### Phase 1: Image Replace Feature (2-3 days)
**Goal:** Allow updating individual images while maintaining DisplayOrder

### Phase 2: Video Support Foundation (3-4 days)
**Goal:** Add video upload, delete, and basic management

### Phase 3: Video Advanced Features (2-3 days)
**Goal:** Add reordering, thumbnail generation, and optimization

---

## Phase 1: Image Replace Feature

### Step 1.1: Domain Layer - Tests First (RED)
**File:** `tests/LankaConnect.Domain.Tests/Events/EventTests.cs`

**New Test Methods:**
```csharp
[Fact]
public void ReplaceImage_WithValidImage_ReplacesSuccessfully()
{
    // Arrange: Create event with 3 images
    // Act: Replace middle image (DisplayOrder = 1)
    // Assert:
    //   - Still 3 images
    //   - New image at DisplayOrder 1
    //   - Other images unchanged
    //   - EventImageReplacedDomainEvent raised
}

[Fact]
public void ReplaceImage_WithNonExistentImageId_ReturnsFailure()
{
    // Arrange: Event with 2 images
    // Act: Replace with random Guid
    // Assert: Result.IsFailure with "Image not found"
}

[Fact]
public void ReplaceImage_MaintainsDisplayOrder()
{
    // Arrange: Event with 5 images (0,1,2,3,4)
    // Act: Replace image at DisplayOrder 2
    // Assert: New image has DisplayOrder 2
}

[Fact]
public void ReplaceImage_OnEmptyImageCollection_ReturnsFailure()
{
    // Arrange: Event with no images
    // Act: Attempt replace
    // Assert: Failure
}
```

**Tasks:**
- [ ] Add ReplaceImage_WithValidImage_ReplacesSuccessfully test
- [ ] Add ReplaceImage_WithNonExistentImageId_ReturnsFailure test
- [ ] Add ReplaceImage_MaintainsDisplayOrder test
- [ ] Add ReplaceImage_OnEmptyImageCollection_ReturnsFailure test
- [ ] Run tests → All should FAIL (RED)

---

### Step 1.2: Domain Layer - Implementation (GREEN)
**File:** `src/LankaConnect.Domain/Events/Event.cs`

**Implementation:**
```csharp
public Result ReplaceImage(Guid imageId, string newImageUrl, string newBlobName)
{
    var existingImage = _images.FirstOrDefault(i => i.Id == imageId);
    if (existingImage == null)
    {
        return Result.Failure($"Image with ID {imageId} not found");
    }

    var oldBlobName = existingImage.BlobName;
    var displayOrder = existingImage.DisplayOrder;

    // Remove old image
    _images.Remove(existingImage);

    // Create new image with same display order
    var newImage = EventImage.Create(
        Id,
        newImageUrl,
        newBlobName,
        displayOrder
    );

    _images.Add(newImage);

    // Raise domain event
    AddDomainEvent(new EventImageReplacedDomainEvent(
        Id,
        imageId,
        newImage.Id,
        oldBlobName,
        newBlobName
    ));

    return Result.Success();
}
```

**File:** `src/LankaConnect.Domain/Events/DomainEvents/EventImageReplacedDomainEvent.cs`

```csharp
namespace LankaConnect.Domain.Events.DomainEvents;

public record EventImageReplacedDomainEvent(
    Guid EventId,
    Guid OldImageId,
    Guid NewImageId,
    string OldBlobName,
    string NewBlobName
) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
```

**Tasks:**
- [ ] Implement Event.ReplaceImage() method
- [ ] Create EventImageReplacedDomainEvent
- [ ] Run domain tests → All should PASS (GREEN)
- [ ] Verify no compilation errors

---

### Step 1.3: Domain Layer - Refactor
**Optional improvements:**
- Extract image validation to separate method
- Add XML documentation comments
- Consider adding overload with EventImage parameter

**Tasks:**
- [ ] Review code for DRY violations
- [ ] Add XML comments
- [ ] Run tests again → Still GREEN

---

### Step 1.4: Application Layer - Tests First (RED)
**File:** `tests/LankaConnect.Application.Tests/Events/Commands/ReplaceEventImageCommandHandlerTests.cs`

**New Test Class:**
```csharp
public class ReplaceEventImageCommandHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ReplaceEventImageCommandHandler _handler;

    [Fact]
    public async Task Handle_ValidCommand_ReplacesImageSuccessfully()
    {
        // Arrange:
        //   - Event exists with 2 images
        //   - IImageService.UploadImageAsync returns new URL
        //   - IImageService.DeleteImageAsync succeeds
        // Act: Handle command
        // Assert:
        //   - Image replaced in domain
        //   - Old blob deleted
        //   - UnitOfWork saved
        //   - Returns EventImageDto
    }

    [Fact]
    public async Task Handle_EventNotFound_ReturnsNotFoundFailure()
    {
        // Arrange: Repository returns null
        // Act: Handle
        // Assert: Result.IsFailure with "Event not found"
    }

    [Fact]
    public async Task Handle_ImageUploadFails_ReturnsFailure()
    {
        // Arrange: IImageService.UploadImageAsync throws
        // Act: Handle
        // Assert: Failure, no database changes
    }

    [Fact]
    public async Task Handle_ImageNotInEvent_ReturnsFailure()
    {
        // Arrange: Event exists but imageId not in collection
        // Act: Handle
        // Assert: Failure, no blob operations
    }

    [Fact]
    public async Task Handle_OldBlobDeletionFails_StillSucceeds()
    {
        // Arrange: IImageService.DeleteImageAsync throws
        // Act: Handle
        // Assert: Success (logs error but doesn't fail transaction)
    }
}
```

**Tasks:**
- [ ] Create ReplaceEventImageCommandHandlerTests.cs
- [ ] Add all 5 test methods
- [ ] Run tests → All should FAIL (RED)

---

### Step 1.5: Application Layer - Implementation (GREEN)
**File:** `src/LankaConnect.Application/Events/Commands/ReplaceEventImage/ReplaceEventImageCommand.cs`

```csharp
using MediatR;
using Microsoft.AspNetCore.Http;

namespace LankaConnect.Application.Events.Commands.ReplaceEventImage;

public record ReplaceEventImageCommand(
    Guid EventId,
    Guid ImageId,
    IFormFile NewImage
) : IRequest<Result<EventImageDto>>;
```

**File:** `src/LankaConnect.Application/Events/Commands/ReplaceEventImage/ReplaceEventImageCommandHandler.cs`

```csharp
public class ReplaceEventImageCommandHandler
    : IRequestHandler<ReplaceEventImageCommand, Result<EventImageDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReplaceEventImageCommandHandler> _logger;

    public async Task<Result<EventImageDto>> Handle(
        ReplaceEventImageCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load event
        var eventEntity = await _eventRepository.GetByIdAsync(
            request.EventId,
            cancellationToken);

        if (eventEntity == null)
        {
            return Result.Failure<EventImageDto>("Event not found");
        }

        // 2. Get old image for blob name
        var oldImage = eventEntity.Images
            .FirstOrDefault(i => i.Id == request.ImageId);

        if (oldImage == null)
        {
            return Result.Failure<EventImageDto>("Image not found in event");
        }

        var oldBlobName = oldImage.BlobName;

        // 3. Upload new image
        string newImageUrl, newBlobName;
        try
        {
            (newImageUrl, newBlobName) = await _imageService.UploadImageAsync(
                request.NewImage.OpenReadStream(),
                request.NewImage.FileName,
                request.EventId.ToString(),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload new image for event {EventId}",
                request.EventId);
            return Result.Failure<EventImageDto>("Failed to upload image");
        }

        // 4. Replace in domain
        var replaceResult = eventEntity.ReplaceImage(
            request.ImageId,
            newImageUrl,
            newBlobName);

        if (replaceResult.IsFailure)
        {
            // Cleanup uploaded blob
            try
            {
                await _imageService.DeleteImageAsync(newBlobName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup blob after domain failure");
            }

            return Result.Failure<EventImageDto>(replaceResult.Error);
        }

        // 5. Save to database
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Delete old blob (best effort - don't fail if this errors)
        try
        {
            await _imageService.DeleteImageAsync(oldBlobName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to delete old blob {BlobName} - orphaned blob",
                oldBlobName);
            // Continue - database is consistent, blob cleanup can happen later
        }

        // 7. Return DTO
        var newImage = eventEntity.Images
            .First(i => i.BlobName == newBlobName);

        return Result.Success(new EventImageDto(
            newImage.Id,
            newImage.ImageUrl,
            newImage.DisplayOrder,
            newImage.UploadedAt
        ));
    }
}
```

**Tasks:**
- [ ] Create ReplaceEventImageCommand.cs
- [ ] Create ReplaceEventImageCommandHandler.cs
- [ ] Add command validator (file size, format)
- [ ] Register handler in DI
- [ ] Run application tests → All should PASS (GREEN)

---

### Step 1.6: API Layer - Tests First (RED)
**File:** `tests/LankaConnect.Api.Tests/Controllers/EventsControllerTests.cs`

**New Test Methods:**
```csharp
[Fact]
public async Task ReplaceEventImage_ValidRequest_ReturnsOk()
{
    // Arrange: Mock mediator to return success
    // Act: PUT /api/Events/{id}/images/{imageId}
    // Assert: 200 OK with EventImageDto
}

[Fact]
public async Task ReplaceEventImage_EventNotFound_ReturnsNotFound()
{
    // Arrange: Mock mediator to return failure
    // Act: PUT
    // Assert: 404 Not Found
}

[Fact]
public async Task ReplaceEventImage_InvalidFile_ReturnsBadRequest()
{
    // Arrange: Send non-image file
    // Act: PUT
    // Assert: 400 Bad Request
}

[Fact]
public async Task ReplaceEventImage_Unauthorized_ReturnsUnauthorized()
{
    // Arrange: No auth token
    // Act: PUT
    // Assert: 401 Unauthorized
}

[Fact]
public async Task ReplaceEventImage_NotEventOrganizer_ReturnsForbidden()
{
    // Arrange: Auth token but different user
    // Act: PUT
    // Assert: 403 Forbidden
}
```

**Tasks:**
- [ ] Add ReplaceEventImage test methods
- [ ] Run API tests → All should FAIL (RED)

---

### Step 1.7: API Layer - Implementation (GREEN)
**File:** `src/LankaConnect.Api/Controllers/EventsController.cs`

**New Endpoint:**
```csharp
[HttpPut("{eventId:guid}/images/{imageId:guid}")]
[Authorize]
[ProducesResponseType(typeof(EventImageDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<IActionResult> ReplaceEventImage(
    Guid eventId,
    Guid imageId,
    [FromForm] IFormFile image,
    CancellationToken cancellationToken)
{
    // 1. Validate file
    if (image == null || image.Length == 0)
    {
        return BadRequest("Image file is required");
    }

    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

    if (!allowedExtensions.Contains(extension))
    {
        return BadRequest($"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}");
    }

    if (image.Length > 5 * 1024 * 1024) // 5 MB
    {
        return BadRequest("Image size must not exceed 5 MB");
    }

    // 2. Check authorization (user must be event organizer)
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Unauthorized();
    }

    // TODO: Check if user is event organizer
    // var eventEntity = await _eventRepository.GetByIdAsync(eventId);
    // if (eventEntity?.OrganizerId != Guid.Parse(userId))
    //     return Forbid();

    // 3. Send command
    var command = new ReplaceEventImageCommand(eventId, imageId, image);
    var result = await _mediator.Send(command, cancellationToken);

    if (result.IsFailure)
    {
        if (result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(result.Error);
        }
        return BadRequest(result.Error);
    }

    return Ok(result.Value);
}
```

**Tasks:**
- [ ] Add ReplaceEventImage endpoint
- [ ] Add file validation
- [ ] Add authorization check
- [ ] Run API tests → All should PASS (GREEN)
- [ ] Test manually with Postman/Swagger

---

### Phase 1 Completion Checklist
- [ ] All domain tests passing (ReplaceImage)
- [ ] All application tests passing (ReplaceEventImageCommandHandler)
- [ ] All API tests passing (ReplaceEventImage endpoint)
- [ ] Manual testing completed
- [ ] No compilation errors
- [ ] Code coverage > 90%
- [ ] Documentation updated
- [ ] PR created and reviewed

---

## Phase 2: Video Support Foundation

### Step 2.1: Domain Layer - EventVideo Entity (RED)
**File:** `tests/LankaConnect.Domain.Tests/Events/EventVideoTests.cs`

**New Test Class:**
```csharp
public class EventVideoTests
{
    [Fact]
    public void Create_WithValidParameters_CreatesEventVideo()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var videoUrl = "https://blob.com/video.mp4";
        var blobName = "event-123/video-456.mp4";
        var thumbnailUrl = "https://blob.com/thumb.jpg";
        var thumbnailBlobName = "event-123/thumb-456.jpg";

        // Act
        var video = EventVideo.Create(
            eventId,
            videoUrl,
            blobName,
            thumbnailUrl,
            thumbnailBlobName,
            displayOrder: 0,
            duration: TimeSpan.FromMinutes(5),
            format: "mp4",
            fileSizeBytes: 10_000_000);

        // Assert
        video.Should().NotBeNull();
        video.EventId.Should().Be(eventId);
        video.VideoUrl.Should().Be(videoUrl);
        video.Duration.Should().Be(TimeSpan.FromMinutes(5));
        video.Format.Should().Be("mp4");
        video.FileSizeBytes.Should().Be(10_000_000);
    }

    [Fact]
    public void Create_WithoutOptionalParameters_CreatesEventVideo()
    {
        // Test with null duration, default format
    }

    [Fact]
    public void UpdateDisplayOrder_ChangesOrder()
    {
        // Test internal method
    }
}
```

**File:** `tests/LankaConnect.Domain.Tests/Events/EventTests.cs` (Add video tests)

**New Test Methods:**
```csharp
[Fact]
public void AddVideo_ToEventWithNoVideos_AddsSuccessfully()
{
    // Arrange: Empty event
    // Act: AddVideo
    // Assert: 1 video, DisplayOrder = 0, EventVideoAddedDomainEvent
}

[Fact]
public void AddVideo_WhenMaxReached_ReturnsFailure()
{
    // Arrange: Event with 3 videos
    // Act: AddVideo (4th)
    // Assert: Failure "Maximum 3 videos allowed"
}

[Fact]
public void RemoveVideo_ExistingVideo_RemovesSuccessfully()
{
    // Arrange: Event with 2 videos
    // Act: RemoveVideo(first)
    // Assert: 1 video remains, EventVideoRemovedDomainEvent
}

[Fact]
public void RemoveVideo_ReordersRemainingVideos()
{
    // Arrange: 3 videos (0,1,2)
    // Act: Remove middle (1)
    // Assert: Remaining videos are (0,1)
}
```

**Tasks:**
- [ ] Create EventVideoTests.cs
- [ ] Add EventVideo creation tests
- [ ] Add Event.AddVideo tests to EventTests.cs
- [ ] Add Event.RemoveVideo tests to EventTests.cs
- [ ] Run tests → All should FAIL (RED)

---

### Step 2.2: Domain Layer - Implementation (GREEN)
**File:** `src/LankaConnect.Domain/Events/EventVideo.cs`

```csharp
namespace LankaConnect.Domain.Events;

public class EventVideo : Entity
{
    private const int MaxVideosPerEvent = 3;

    public Guid EventId { get; private set; }
    public string VideoUrl { get; private set; }
    public string BlobName { get; private set; }
    public string ThumbnailUrl { get; private set; }
    public string ThumbnailBlobName { get; private set; }
    public TimeSpan? Duration { get; private set; }
    public string Format { get; private set; }
    public long FileSizeBytes { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private EventVideo() { } // EF Core

    internal static EventVideo Create(
        Guid eventId,
        string videoUrl,
        string blobName,
        string thumbnailUrl,
        string thumbnailBlobName,
        int displayOrder,
        TimeSpan? duration = null,
        string format = "mp4",
        long fileSizeBytes = 0)
    {
        return new EventVideo
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            VideoUrl = Guard.Against.NullOrWhiteSpace(videoUrl, nameof(videoUrl)),
            BlobName = Guard.Against.NullOrWhiteSpace(blobName, nameof(blobName)),
            ThumbnailUrl = thumbnailUrl ?? string.Empty,
            ThumbnailBlobName = thumbnailBlobName ?? string.Empty,
            DisplayOrder = Guard.Against.Negative(displayOrder, nameof(displayOrder)),
            Duration = duration,
            Format = Guard.Against.NullOrWhiteSpace(format, nameof(format)),
            FileSizeBytes = Guard.Against.Negative(fileSizeBytes, nameof(fileSizeBytes)),
            UploadedAt = DateTime.UtcNow
        };
    }

    internal void UpdateDisplayOrder(int newOrder)
    {
        DisplayOrder = Guard.Against.Negative(newOrder, nameof(newOrder));
    }
}
```

**File:** `src/LankaConnect.Domain/Events/Event.cs` (Add video methods)

```csharp
public class Event : AggregateRoot
{
    // Existing fields...
    private readonly List<EventImage> _images = new();
    private readonly List<EventVideo> _videos = new(); // NEW

    public IReadOnlyCollection<EventImage> Images => _images.AsReadOnly();
    public IReadOnlyCollection<EventVideo> Videos => _videos.AsReadOnly(); // NEW

    private const int MaxImagesPerEvent = 10;
    private const int MaxVideosPerEvent = 3; // NEW

    // NEW: Add Video
    public Result AddVideo(
        string videoUrl,
        string blobName,
        string thumbnailUrl,
        string thumbnailBlobName,
        TimeSpan? duration = null,
        string format = "mp4",
        long fileSizeBytes = 0)
    {
        if (_videos.Count >= MaxVideosPerEvent)
        {
            return Result.Failure($"Cannot add more than {MaxVideosPerEvent} videos per event");
        }

        var displayOrder = _videos.Any() ? _videos.Max(v => v.DisplayOrder) + 1 : 0;

        var video = EventVideo.Create(
            Id,
            videoUrl,
            blobName,
            thumbnailUrl,
            thumbnailBlobName,
            displayOrder,
            duration,
            format,
            fileSizeBytes);

        _videos.Add(video);

        AddDomainEvent(new EventVideoAddedDomainEvent(
            Id,
            video.Id,
            videoUrl,
            thumbnailUrl));

        return Result.Success();
    }

    // NEW: Remove Video
    public Result RemoveVideo(Guid videoId)
    {
        var video = _videos.FirstOrDefault(v => v.Id == videoId);
        if (video == null)
        {
            return Result.Failure($"Video with ID {videoId} not found");
        }

        var videoBlobName = video.BlobName;
        var thumbnailBlobName = video.ThumbnailBlobName;

        _videos.Remove(video);

        // Reorder remaining videos
        var remainingVideos = _videos.OrderBy(v => v.DisplayOrder).ToList();
        for (int i = 0; i < remainingVideos.Count; i++)
        {
            remainingVideos[i].UpdateDisplayOrder(i);
        }

        AddDomainEvent(new EventVideoRemovedDomainEvent(
            Id,
            videoId,
            videoBlobName,
            thumbnailBlobName));

        return Result.Success();
    }

    // NEW: Reorder Videos
    public Result ReorderVideos(IEnumerable<(Guid VideoId, int NewOrder)> reorderRequests)
    {
        var requestsList = reorderRequests.ToList();

        // Validate all video IDs exist
        var allVideoIds = _videos.Select(v => v.Id).ToHashSet();
        var requestedIds = requestsList.Select(r => r.VideoId).ToHashSet();

        if (!requestedIds.IsSubsetOf(allVideoIds))
        {
            return Result.Failure("One or more video IDs not found");
        }

        // Validate orders are valid (0 to count-1)
        var orders = requestsList.Select(r => r.NewOrder).ToList();
        var expectedOrders = Enumerable.Range(0, _videos.Count).ToHashSet();

        if (!orders.ToHashSet().SetEquals(expectedOrders))
        {
            return Result.Failure("Display orders must be sequential starting from 0");
        }

        // Apply new orders
        foreach (var (videoId, newOrder) in requestsList)
        {
            var video = _videos.First(v => v.Id == videoId);
            video.UpdateDisplayOrder(newOrder);
        }

        return Result.Success();
    }
}
```

**File:** `src/LankaConnect.Domain/Events/DomainEvents/EventVideoAddedDomainEvent.cs`

```csharp
namespace LankaConnect.Domain.Events.DomainEvents;

public record EventVideoAddedDomainEvent(
    Guid EventId,
    Guid VideoId,
    string VideoUrl,
    string ThumbnailUrl
) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
```

**File:** `src/LankaConnect.Domain/Events/DomainEvents/EventVideoRemovedDomainEvent.cs`

```csharp
namespace LankaConnect.Domain.Events.DomainEvents;

public record EventVideoRemovedDomainEvent(
    Guid EventId,
    Guid VideoId,
    string VideoBlobName,
    string ThumbnailBlobName
) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
```

**Tasks:**
- [ ] Create EventVideo.cs entity
- [ ] Add AddVideo method to Event.cs
- [ ] Add RemoveVideo method to Event.cs
- [ ] Add ReorderVideos method to Event.cs
- [ ] Create EventVideoAddedDomainEvent
- [ ] Create EventVideoRemovedDomainEvent
- [ ] Run domain tests → All should PASS (GREEN)

---

### Step 2.3: Infrastructure Layer - Storage Services (RED + GREEN)
**File:** `src/LankaConnect.Infrastructure/Services/IVideoProcessingService.cs`

```csharp
namespace LankaConnect.Infrastructure.Services;

public interface IVideoProcessingService
{
    Task<Stream?> GenerateThumbnailAsync(
        Stream videoStream,
        CancellationToken cancellationToken = default);

    Task<TimeSpan?> GetDurationAsync(
        Stream videoStream,
        CancellationToken cancellationToken = default);

    Task<string> GetFormatAsync(
        Stream videoStream,
        CancellationToken cancellationToken = default);
}
```

**File:** `src/LankaConnect.Infrastructure/Services/BasicVideoProcessingService.cs`

```csharp
namespace LankaConnect.Infrastructure.Services;

/// <summary>
/// Basic implementation that returns defaults (Phase 1).
/// Phase 2 will implement actual video processing with FFmpeg.
/// </summary>
public class BasicVideoProcessingService : IVideoProcessingService
{
    private readonly ILogger<BasicVideoProcessingService> _logger;

    public BasicVideoProcessingService(ILogger<BasicVideoProcessingService> logger)
    {
        _logger = logger;
    }

    public Task<Stream?> GenerateThumbnailAsync(
        Stream videoStream,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Thumbnail generation not yet implemented - returning null");
        return Task.FromResult<Stream?>(null);
    }

    public Task<TimeSpan?> GetDurationAsync(
        Stream videoStream,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Duration extraction not yet implemented - returning null");
        return Task.FromResult<TimeSpan?>(null);
    }

    public Task<string> GetFormatAsync(
        Stream videoStream,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult("mp4"); // Default format
    }
}
```

**File:** `src/LankaConnect.Infrastructure/Services/IMediaStorageService.cs` (Extend existing)

```csharp
public interface IMediaStorageService
{
    // Existing image methods
    Task<(string Url, string BlobName)> UploadImageAsync(
        Stream imageStream,
        string fileName,
        string eventId,
        CancellationToken cancellationToken = default);

    Task DeleteImageAsync(
        string blobName,
        CancellationToken cancellationToken = default);

    // NEW: Video methods
    Task<(string VideoUrl, string VideoBlobName, string ThumbnailUrl, string ThumbnailBlobName)> UploadVideoAsync(
        Stream videoStream,
        string fileName,
        string eventId,
        CancellationToken cancellationToken = default);

    Task DeleteVideoAsync(
        string videoBlobName,
        string thumbnailBlobName,
        CancellationToken cancellationToken = default);
}
```

**File:** `src/LankaConnect.Infrastructure/Services/AzureBlobMediaStorageService.cs`

```csharp
public class AzureBlobMediaStorageService : IMediaStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IVideoProcessingService _videoProcessor;
    private readonly ILogger<AzureBlobMediaStorageService> _logger;

    private const string ImageContainer = "event-images";
    private const string VideoContainer = "event-videos";
    private const string ThumbnailContainer = "event-video-thumbnails";

    // Existing image methods...

    public async Task<(string VideoUrl, string VideoBlobName, string ThumbnailUrl, string ThumbnailBlobName)> UploadVideoAsync(
        Stream videoStream,
        string fileName,
        string eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Upload video
            var videoContainerClient = _blobServiceClient.GetBlobContainerClient(VideoContainer);
            await videoContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var videoId = Guid.NewGuid();
            var videoBlobName = $"{eventId}/{videoId}_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(fileName)}";
            var videoBlobClient = videoContainerClient.GetBlobClient(videoBlobName);

            await videoBlobClient.UploadAsync(
                videoStream,
                new BlobHttpHeaders { ContentType = "video/mp4" },
                cancellationToken: cancellationToken);

            var videoUrl = videoBlobClient.Uri.ToString();

            // 2. Generate thumbnail (Phase 1: Skip if not implemented)
            string thumbnailUrl = string.Empty;
            string thumbnailBlobName = string.Empty;

            videoStream.Position = 0; // Reset stream for thumbnail generation
            var thumbnailStream = await _videoProcessor.GenerateThumbnailAsync(videoStream, cancellationToken);

            if (thumbnailStream != null)
            {
                var thumbnailContainerClient = _blobServiceClient.GetBlobContainerClient(ThumbnailContainer);
                await thumbnailContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

                thumbnailBlobName = $"{eventId}/{videoId}_thumb.jpg";
                var thumbnailBlobClient = thumbnailContainerClient.GetBlobClient(thumbnailBlobName);

                await thumbnailBlobClient.UploadAsync(
                    thumbnailStream,
                    new BlobHttpHeaders { ContentType = "image/jpeg" },
                    cancellationToken: cancellationToken);

                thumbnailUrl = thumbnailBlobClient.Uri.ToString();
            }

            return (videoUrl, videoBlobName, thumbnailUrl, thumbnailBlobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload video");
            throw;
        }
    }

    public async Task DeleteVideoAsync(
        string videoBlobName,
        string thumbnailBlobName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Delete video
            var videoContainerClient = _blobServiceClient.GetBlobContainerClient(VideoContainer);
            var videoBlobClient = videoContainerClient.GetBlobClient(videoBlobName);
            await videoBlobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

            // Delete thumbnail if exists
            if (!string.IsNullOrEmpty(thumbnailBlobName))
            {
                var thumbnailContainerClient = _blobServiceClient.GetBlobContainerClient(ThumbnailContainer);
                var thumbnailBlobClient = thumbnailContainerClient.GetBlobClient(thumbnailBlobName);
                await thumbnailBlobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete video blobs");
            throw;
        }
    }
}
```

**Tasks:**
- [ ] Create IVideoProcessingService interface
- [ ] Create BasicVideoProcessingService stub
- [ ] Extend IMediaStorageService with video methods
- [ ] Implement UploadVideoAsync in AzureBlobMediaStorageService
- [ ] Implement DeleteVideoAsync
- [ ] Register services in DI (Infrastructure/DependencyInjection.cs)
- [ ] Write integration tests with Azurite emulator

---

### Step 2.4: Infrastructure Layer - EF Core Configuration
**File:** `src/LankaConnect.Infrastructure/Data/Configurations/EventVideoConfiguration.cs`

```csharp
using LankaConnect.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class EventVideoConfiguration : IEntityTypeConfiguration<EventVideo>
{
    public void Configure(EntityTypeBuilder<EventVideo> builder)
    {
        builder.ToTable("event_videos");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.VideoUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(v => v.BlobName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(v => v.ThumbnailUrl)
            .HasMaxLength(2048);

        builder.Property(v => v.ThumbnailBlobName)
            .HasMaxLength(500);

        builder.Property(v => v.Duration)
            .IsRequired(false);

        builder.Property(v => v.Format)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("mp4");

        builder.Property(v => v.FileSizeBytes)
            .IsRequired();

        builder.Property(v => v.DisplayOrder)
            .IsRequired();

        builder.Property(v => v.UploadedAt)
            .IsRequired();

        // Relationship with Event
        builder.HasOne<Event>()
            .WithMany(e => e.Videos)
            .HasForeignKey(v => v.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index on EventId + DisplayOrder
        builder.HasIndex(v => new { v.EventId, v.DisplayOrder })
            .IsUnique()
            .HasDatabaseName("IX_EventVideos_EventId_DisplayOrder");

        // Index for querying by event
        builder.HasIndex(v => v.EventId)
            .HasDatabaseName("IX_EventVideos_EventId");
    }
}
```

**File:** `src/LankaConnect.Infrastructure/Data/ApplicationDbContext.cs` (Add DbSet)

```csharp
public class ApplicationDbContext : DbContext
{
    // Existing DbSets...
    public DbSet<Event> Events { get; set; }
    public DbSet<EventImage> EventImages { get; set; }
    public DbSet<EventVideo> EventVideos { get; set; } // NEW

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Existing configurations...
        modelBuilder.ApplyConfiguration(new EventConfiguration());
        modelBuilder.ApplyConfiguration(new EventImageConfiguration());
        modelBuilder.ApplyConfiguration(new EventVideoConfiguration()); // NEW
    }
}
```

**Tasks:**
- [ ] Create EventVideoConfiguration.cs
- [ ] Add EventVideos DbSet to ApplicationDbContext
- [ ] Create migration: `dotnet ef migrations add AddEventVideos`
- [ ] Review migration SQL
- [ ] Test migration up/down

---

### Step 2.5: Application Layer - Video Commands
**Commands to implement:**
1. AddVideoToEventCommand
2. DeleteEventVideoCommand

**File:** `tests/LankaConnect.Application.Tests/Events/Commands/AddVideoToEventCommandHandlerTests.cs`

```csharp
public class AddVideoToEventCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidVideo_AddsSuccessfully()
    {
        // Arrange: Event exists, IMediaStorageService succeeds
        // Act: Handle AddVideoToEventCommand
        // Assert: Video added, domain event raised, UnitOfWork saved
    }

    [Fact]
    public async Task Handle_FourthVideo_ReturnsFailure()
    {
        // Arrange: Event with 3 videos
        // Act: Add 4th
        // Assert: Failure "Maximum 3 videos"
    }

    [Fact]
    public async Task Handle_VideoUploadFails_ReturnsFailure()
    {
        // Arrange: IMediaStorageService throws
        // Act: Handle
        // Assert: Failure, no database changes
    }
}
```

**File:** `src/LankaConnect.Application/Events/Commands/AddVideoToEvent/AddVideoToEventCommand.cs`

```csharp
public record AddVideoToEventCommand(
    Guid EventId,
    IFormFile Video
) : IRequest<Result<EventVideoDto>>;
```

**File:** `src/LankaConnect.Application/Events/Commands/AddVideoToEvent/AddVideoToEventCommandHandler.cs`

```csharp
public class AddVideoToEventCommandHandler
    : IRequestHandler<AddVideoToEventCommand, Result<EventVideoDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMediaStorageService _mediaStorage;
    private readonly IVideoProcessingService _videoProcessor;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddVideoToEventCommandHandler> _logger;

    public async Task<Result<EventVideoDto>> Handle(
        AddVideoToEventCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load event
        var eventEntity = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventEntity == null)
        {
            return Result.Failure<EventVideoDto>("Event not found");
        }

        // 2. Extract video metadata
        string format;
        TimeSpan? duration;
        long fileSizeBytes = request.Video.Length;

        try
        {
            using var videoStream = request.Video.OpenReadStream();
            format = await _videoProcessor.GetFormatAsync(videoStream, cancellationToken);

            videoStream.Position = 0;
            duration = await _videoProcessor.GetDurationAsync(videoStream, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract video metadata");
            format = "mp4"; // Default
            duration = null;
        }

        // 3. Upload video (includes thumbnail generation)
        string videoUrl, videoBlobName, thumbnailUrl, thumbnailBlobName;
        try
        {
            using var videoStream = request.Video.OpenReadStream();
            (videoUrl, videoBlobName, thumbnailUrl, thumbnailBlobName) =
                await _mediaStorage.UploadVideoAsync(
                    videoStream,
                    request.Video.FileName,
                    request.EventId.ToString(),
                    cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload video");
            return Result.Failure<EventVideoDto>("Failed to upload video");
        }

        // 4. Add to domain
        var addResult = eventEntity.AddVideo(
            videoUrl,
            videoBlobName,
            thumbnailUrl,
            thumbnailBlobName,
            duration,
            format,
            fileSizeBytes);

        if (addResult.IsFailure)
        {
            // Cleanup uploaded blobs
            try
            {
                await _mediaStorage.DeleteVideoAsync(videoBlobName, thumbnailBlobName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup video blobs after domain failure");
            }

            return Result.Failure<EventVideoDto>(addResult.Error);
        }

        // 5. Save
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Return DTO
        var video = eventEntity.Videos.First(v => v.BlobName == videoBlobName);
        return Result.Success(new EventVideoDto(
            video.Id,
            video.VideoUrl,
            video.ThumbnailUrl,
            video.DisplayOrder,
            video.Duration,
            video.Format,
            video.FileSizeBytes,
            video.UploadedAt
        ));
    }
}
```

**Similar implementation for DeleteEventVideoCommand...**

**Tasks:**
- [ ] Create AddVideoToEventCommand and handler
- [ ] Create DeleteEventVideoCommand and handler
- [ ] Create ReorderEventVideosCommand and handler
- [ ] Create EventVideoDto
- [ ] Write unit tests for all handlers
- [ ] Register in DI

---

### Step 2.6: API Layer - Video Endpoints
**File:** `src/LankaConnect.Api/Controllers/EventsController.cs`

```csharp
[HttpPost("{eventId:guid}/videos")]
[Authorize]
[ProducesResponseType(typeof(EventVideoDto), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> AddEventVideo(
    Guid eventId,
    [FromForm] IFormFile video,
    CancellationToken cancellationToken)
{
    // Validate file
    if (video == null || video.Length == 0)
    {
        return BadRequest("Video file is required");
    }

    var allowedFormats = new[] { ".mp4", ".webm", ".mov" };
    var extension = Path.GetExtension(video.FileName).ToLowerInvariant();

    if (!allowedFormats.Contains(extension))
    {
        return BadRequest($"Invalid video format. Allowed: {string.Join(", ", allowedFormats)}");
    }

    if (video.Length > 100 * 1024 * 1024) // 100 MB
    {
        return BadRequest("Video size must not exceed 100 MB");
    }

    // Send command
    var command = new AddVideoToEventCommand(eventId, video);
    var result = await _mediator.Send(command, cancellationToken);

    if (result.IsFailure)
    {
        if (result.Error.Contains("not found"))
            return NotFound(result.Error);
        return BadRequest(result.Error);
    }

    return CreatedAtAction(
        nameof(GetEventVideos),
        new { eventId },
        result.Value);
}

[HttpDelete("{eventId:guid}/videos/{videoId:guid}")]
[Authorize]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> DeleteEventVideo(
    Guid eventId,
    Guid videoId,
    CancellationToken cancellationToken)
{
    var command = new DeleteEventVideoCommand(eventId, videoId);
    var result = await _mediator.Send(command, cancellationToken);

    if (result.IsFailure)
    {
        return NotFound(result.Error);
    }

    return NoContent();
}

[HttpGet("{eventId:guid}/videos")]
[ProducesResponseType(typeof(List<EventVideoDto>), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetEventVideos(
    Guid eventId,
    CancellationToken cancellationToken)
{
    var query = new GetEventVideosQuery(eventId);
    var result = await _mediator.Send(query, cancellationToken);

    if (result.IsFailure)
    {
        return NotFound(result.Error);
    }

    return Ok(result.Value);
}

[HttpPut("{eventId:guid}/videos/reorder")]
[Authorize]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> ReorderEventVideos(
    Guid eventId,
    [FromBody] List<VideoReorderDto> reorderRequests,
    CancellationToken cancellationToken)
{
    var command = new ReorderEventVideosCommand(eventId, reorderRequests);
    var result = await _mediator.Send(command, cancellationToken);

    if (result.IsFailure)
    {
        return BadRequest(result.Error);
    }

    return NoContent();
}
```

**Tasks:**
- [ ] Add AddEventVideo endpoint
- [ ] Add DeleteEventVideo endpoint
- [ ] Add GetEventVideos endpoint
- [ ] Add ReorderEventVideos endpoint
- [ ] Add file validation
- [ ] Write API tests
- [ ] Test with Postman/Swagger

---

## Phase 3: Polish and Optimization

### Step 3.1: Background Job for Orphaned Blob Cleanup
- [ ] Create Hangfire job to find orphaned blobs
- [ ] Schedule daily cleanup
- [ ] Add logging and monitoring

### Step 3.2: Video Thumbnail Enhancement
- [ ] Research FFmpeg integration
- [ ] Implement actual thumbnail generation
- [ ] Test with various video formats

### Step 3.3: Performance Optimization
- [ ] Add caching for event media
- [ ] Optimize blob storage queries
- [ ] Add CDN configuration

### Step 3.4: Documentation
- [ ] API documentation (Swagger annotations)
- [ ] Update README
- [ ] Create user guide for video uploads

---

## Final Checklist

### Code Quality
- [ ] All tests passing (unit, integration, API)
- [ ] Code coverage > 90%
- [ ] No compilation errors or warnings
- [ ] Code reviewed

### Domain
- [ ] Domain invariants enforced
- [ ] Domain events raised appropriately
- [ ] Immutability preserved

### Application
- [ ] Commands and queries implemented
- [ ] Validation logic in place
- [ ] Error handling comprehensive

### Infrastructure
- [ ] EF Core migrations tested
- [ ] Blob storage working
- [ ] Services registered in DI

### API
- [ ] Endpoints tested
- [ ] Authorization working
- [ ] Swagger documentation updated

### Deployment
- [ ] Migration script reviewed
- [ ] Rollback plan documented
- [ ] Staging deployment successful
- [ ] Production deployment plan ready

---

## Risk Mitigation Strategies

| Risk | Mitigation |
|------|------------|
| Large video upload timeouts | Stream directly to blob storage, use async processing |
| Blob deletion failures | Implement background cleanup job, log orphaned blobs |
| Thumbnail generation complexity | Phase 1: Skip, Phase 2: Background job with Hangfire |
| Storage costs | Implement retention policy, use CDN, monitor usage |
| Malicious file uploads | Validate file signatures, scan with Azure Defender, rate limiting |

---

## Success Metrics

- [ ] Image replace operation < 2s
- [ ] Video upload (50 MB) < 10s
- [ ] Zero data loss during replace operations
- [ ] No orphaned blobs after 24 hours
- [ ] All domain invariants enforced
- [ ] 90%+ test coverage

---

## Timeline Estimate

- **Phase 1 (Image Replace):** 2-3 days
- **Phase 2 (Video Foundation):** 3-4 days
- **Phase 3 (Polish):** 2-3 days
- **Total:** 7-10 days

**Note:** Timeline assumes full-time work with no blockers. Adjust based on team capacity.
