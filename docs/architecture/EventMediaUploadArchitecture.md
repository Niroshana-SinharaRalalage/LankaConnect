# Event Media Upload Architecture
## Clean Architecture + DDD Design

**Date:** 2025-12-01
**Status:** Design Phase
**Author:** System Architecture Designer

---

## Executive Summary

This document defines the complete architecture for event image and video upload functionality, following Clean Architecture principles and Domain-Driven Design patterns. The solution enables event organizers to upload, manage, and display media assets (images and videos) stored in Azure Blob Storage.

### Key Design Decisions

1. **Azure Blob Storage** as the primary storage mechanism
2. **Event aggregate** maintains media ownership and invariants
3. **Domain events** for asynchronous blob cleanup
4. **Multipart upload** with client-side progress tracking
5. **CDN integration** for optimized media delivery
6. **Separate tables** for EventImages and EventVideos
7. **Cascade delete** ensures data consistency

---

## 1. Database Schema

### 1.1 Tables

#### EventImages Table
```sql
CREATE TABLE "EventImages" (
    "Id" uuid PRIMARY KEY NOT NULL,
    "EventId" uuid NOT NULL,
    "ImageUrl" varchar(500) NOT NULL,
    "BlobName" varchar(255) NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "UploadedAt" timestamp NOT NULL,
    "CreatedAt" timestamp NOT NULL,
    "UpdatedAt" timestamp NULL,

    CONSTRAINT "FK_EventImages_Events_EventId"
        FOREIGN KEY ("EventId")
        REFERENCES "Events"("Id")
        ON DELETE CASCADE
);
```

#### EventVideos Table
```sql
CREATE TABLE "EventVideos" (
    "Id" uuid PRIMARY KEY NOT NULL,
    "EventId" uuid NOT NULL,
    "VideoUrl" varchar(500) NOT NULL,
    "BlobName" varchar(255) NOT NULL,
    "ThumbnailUrl" varchar(500) NOT NULL,
    "ThumbnailBlobName" varchar(255) NOT NULL,
    "Duration" interval NULL,
    "Format" varchar(50) NOT NULL,
    "FileSizeBytes" bigint NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "UploadedAt" timestamp NOT NULL,
    "CreatedAt" timestamp NOT NULL,
    "UpdatedAt" timestamp NULL,

    CONSTRAINT "FK_EventVideos_Events_EventId"
        FOREIGN KEY ("EventId")
        REFERENCES "Events"("Id")
        ON DELETE CASCADE
);
```

### 1.2 Indexes

```sql
-- EventImages indexes (optimized for queries and uniqueness)
CREATE UNIQUE INDEX "IX_EventImages_EventId_DisplayOrder"
    ON "EventImages" ("EventId", "DisplayOrder");

CREATE INDEX "IX_EventImages_EventId"
    ON "EventImages" ("EventId");

-- EventVideos indexes
CREATE UNIQUE INDEX "IX_EventVideos_EventId_DisplayOrder"
    ON "EventVideos" ("EventId", "DisplayOrder");

CREATE INDEX "IX_EventVideos_EventId"
    ON "EventVideos" ("EventId");
```

### 1.3 Constraints and Invariants

- **Max Images per Event:** 10 (enforced in domain)
- **Max Videos per Event:** 3 (enforced in domain)
- **Display Order:** Sequential from 1, unique per event
- **Cascade Delete:** Deleting event removes all media
- **URL Length:** Max 500 characters
- **Blob Name Length:** Max 255 characters

### 1.4 Data Relationships

```
Event (1) ──────< (N) EventImage
Event (1) ──────< (N) EventVideo
```

---

## 2. Domain Model (DDD)

### 2.1 Aggregate: Event

The Event aggregate is the consistency boundary for all media operations.

#### Entities Within Aggregate

```csharp
public class Event : BaseEntity
{
    private readonly List<EventImage> _images = new();
    private readonly List<EventVideo> _videos = new();

    private const int MAX_IMAGES = 10;
    private const int MAX_VIDEOS = 3;

    public IReadOnlyList<EventImage> Images => _images.AsReadOnly();
    public IReadOnlyList<EventVideo> Videos => _videos.AsReadOnly();
}
```

### 2.2 Entity: EventImage

```csharp
public class EventImage : BaseEntity
{
    public string ImageUrl { get; private set; }
    public string BlobName { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public Guid EventId { get; private set; }

    // Factory methods
    public static EventImage Create(Guid eventId, string imageUrl,
        string blobName, int displayOrder);

    internal static EventImage CreateWithId(Guid id, Guid eventId,
        string imageUrl, string blobName, int displayOrder);

    // Behavior
    internal void UpdateDisplayOrder(int newOrder);
}
```

### 2.3 Entity: EventVideo

```csharp
public class EventVideo : BaseEntity
{
    public string VideoUrl { get; private set; }
    public string BlobName { get; private set; }
    public string ThumbnailUrl { get; private set; }
    public string ThumbnailBlobName { get; private set; }
    public TimeSpan? Duration { get; private set; }
    public string Format { get; private set; }
    public long FileSizeBytes { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public Guid EventId { get; private set; }

    // Factory method
    public static EventVideo Create(Guid eventId, string videoUrl,
        string blobName, string thumbnailUrl, string thumbnailBlobName,
        TimeSpan? duration, string format, long fileSizeBytes,
        int displayOrder);

    // Behavior
    internal void UpdateDisplayOrder(int newOrder);
}
```

### 2.4 Domain Events

```csharp
// Image events
public record ImageAddedToEventDomainEvent(
    Guid EventId, Guid ImageId, string ImageUrl);

public record ImageRemovedFromEventDomainEvent(
    Guid EventId, Guid ImageId, string BlobName);

public record ImageReplacedInEventDomainEvent(
    Guid EventId, Guid ImageId, string OldBlobName, string NewImageUrl);

public record ImagesReorderedDomainEvent(Guid EventId);

// Video events
public record VideoAddedToEventDomainEvent(
    Guid EventId, Guid VideoId, string VideoUrl);

public record VideoRemovedFromEventDomainEvent(
    Guid EventId, Guid VideoId, string VideoBlobName,
    string ThumbnailBlobName);
```

### 2.5 Domain Invariants

**Event Aggregate Enforces:**
- Maximum 10 images per event
- Maximum 3 videos per event
- Display orders are sequential starting from 1
- Display orders are unique within event
- Image/video URLs and blob names are not empty
- Automatic resequencing after deletion

---

## 3. Application Layer

### 3.1 Commands

#### UploadEventImageCommand
```csharp
public record UploadEventImageCommand(
    Guid EventId,
    IFormFile ImageFile,
    int? TargetDisplayOrder = null
) : IRequest<Result<EventImageDto>>;
```

#### UploadEventVideoCommand
```csharp
public record UploadEventVideoCommand(
    Guid EventId,
    IFormFile VideoFile,
    IFormFile? ThumbnailFile = null
) : IRequest<Result<EventVideoDto>>;
```

#### DeleteEventImageCommand
```csharp
public record DeleteEventImageCommand(
    Guid EventId,
    Guid ImageId
) : IRequest<Result>;
```

#### DeleteEventVideoCommand
```csharp
public record DeleteEventVideoCommand(
    Guid EventId,
    Guid VideoId
) : IRequest<Result>;
```

#### ReplaceEventImageCommand
```csharp
public record ReplaceEventImageCommand(
    Guid EventId,
    Guid ImageId,
    IFormFile NewImageFile
) : IRequest<Result<EventImageDto>>;
```

#### ReorderEventImagesCommand
```csharp
public record ReorderEventImagesCommand(
    Guid EventId,
    Dictionary<Guid, int> NewDisplayOrders
) : IRequest<Result>;
```

### 3.2 Queries

#### GetEventMediaQuery
```csharp
public record GetEventMediaQuery(Guid EventId)
    : IRequest<Result<EventMediaDto>>;
```

### 3.3 DTOs

```csharp
public record EventImageDto(
    Guid Id,
    string ImageUrl,
    string CdnUrl,
    int DisplayOrder,
    DateTime UploadedAt
);

public record EventVideoDto(
    Guid Id,
    string VideoUrl,
    string ThumbnailUrl,
    string CdnVideoUrl,
    string CdnThumbnailUrl,
    TimeSpan? Duration,
    string Format,
    long FileSizeBytes,
    int DisplayOrder,
    DateTime UploadedAt
);

public record EventMediaDto(
    Guid EventId,
    List<EventImageDto> Images,
    List<EventVideoDto> Videos,
    int ImagesCount,
    int VideosCount,
    bool CanAddMoreImages,
    bool CanAddMoreVideos
);

public record MediaUploadProgressDto(
    Guid UploadId,
    long BytesUploaded,
    long TotalBytes,
    int PercentComplete,
    MediaUploadStatus Status,
    string? ErrorMessage = null
);

public enum MediaUploadStatus
{
    Pending,
    Uploading,
    Processing,
    Completed,
    Failed
}
```

### 3.4 Command Handlers

#### UploadEventImageCommandHandler
```csharp
public class UploadEventImageCommandHandler
    : IRequestHandler<UploadEventImageCommand, Result<EventImageDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IAzureBlobService _blobService;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<EventImageDto>> Handle(
        UploadEventImageCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate file (size, format, dimensions)
        var validation = await ValidateImageFile(request.ImageFile);
        if (validation.IsFailure)
            return Result<EventImageDto>.Failure(validation.Error);

        // 2. Load event aggregate
        var @event = await _eventRepository.GetByIdAsync(
            request.EventId, cancellationToken);
        if (@event == null)
            return Result<EventImageDto>.Failure("Event not found");

        // 3. Upload to Azure Blob Storage
        var blobName = GenerateBlobName(request.EventId, "image");
        var uploadResult = await _blobService.UploadImageAsync(
            request.ImageFile,
            blobName,
            cancellationToken);
        if (uploadResult.IsFailure)
            return Result<EventImageDto>.Failure(uploadResult.Error);

        // 4. Add image to event aggregate (domain logic)
        var addResult = @event.AddImage(
            uploadResult.Value.BlobUrl,
            blobName);
        if (addResult.IsFailure)
        {
            // Cleanup uploaded blob on domain failure
            await _blobService.DeleteBlobAsync(blobName, cancellationToken);
            return Result<EventImageDto>.Failure(addResult.Error);
        }

        // 5. Persist changes (UnitOfWork pattern)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Return DTO with CDN URL
        var dto = MapToDto(addResult.Value);
        return Result<EventImageDto>.Success(dto);
    }
}
```

### 3.5 Validation Rules

**Image Validation:**
- Format: jpg, jpeg, png, gif, webp
- Max size: 10 MB
- Min dimensions: 800x600
- Max dimensions: 8000x8000
- Aspect ratio: Flexible (no restriction)

**Video Validation:**
- Format: mp4, mov, avi, webm
- Max size: 500 MB
- Max duration: 10 minutes
- Codec: H.264, VP9, AV1
- Min resolution: 720p
- Max resolution: 4K

---

## 4. Infrastructure Layer

### 4.1 Azure Blob Service Interface

```csharp
public interface IAzureBlobService
{
    Task<Result<BlobUploadResult>> UploadImageAsync(
        IFormFile file,
        string blobName,
        CancellationToken cancellationToken = default);

    Task<Result<BlobUploadResult>> UploadVideoAsync(
        IFormFile file,
        string blobName,
        IProgress<long>? progress = null,
        CancellationToken cancellationToken = default);

    Task<Result<BlobUploadResult>> UploadThumbnailAsync(
        IFormFile file,
        string blobName,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteBlobAsync(
        string blobName,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteBlobsAsync(
        List<string> blobNames,
        CancellationToken cancellationToken = default);

    Task<string> GetBlobUrlAsync(string blobName);

    Task<string> GetCdnUrlAsync(string blobName);
}

public record BlobUploadResult(
    string BlobUrl,
    string CdnUrl,
    string BlobName,
    long FileSizeBytes,
    string ContentType
);
```

### 4.2 Azure Blob Service Implementation

```csharp
public class AzureBlobService : IAzureBlobService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureBlobService> _logger;

    private const string IMAGES_CONTAINER = "event-images";
    private const string VIDEOS_CONTAINER = "event-videos";
    private const string THUMBNAILS_CONTAINER = "event-thumbnails";

    public async Task<Result<BlobUploadResult>> UploadImageAsync(
        IFormFile file,
        string blobName,
        CancellationToken cancellationToken)
    {
        try
        {
            var containerClient = _blobServiceClient
                .GetBlobContainerClient(IMAGES_CONTAINER);

            // Ensure container exists
            await containerClient.CreateIfNotExistsAsync(
                publicAccessType: PublicAccessType.Blob,
                cancellationToken: cancellationToken);

            // Upload blob
            var blobClient = containerClient.GetBlobClient(blobName);

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = file.ContentType,
                    CacheControl = "public, max-age=31536000" // 1 year
                }
            };

            await using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(
                stream,
                uploadOptions,
                cancellationToken);

            // Get URLs
            var blobUrl = blobClient.Uri.ToString();
            var cdnUrl = GetCdnUrl(blobUrl);

            return Result<BlobUploadResult>.Success(new BlobUploadResult(
                blobUrl,
                cdnUrl,
                blobName,
                file.Length,
                file.ContentType
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to upload image {BlobName}", blobName);
            return Result<BlobUploadResult>.Failure(
                "Failed to upload image to storage");
        }
    }

    public async Task<Result<BlobUploadResult>> UploadVideoAsync(
        IFormFile file,
        string blobName,
        IProgress<long>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            var containerClient = _blobServiceClient
                .GetBlobContainerClient(VIDEOS_CONTAINER);

            await containerClient.CreateIfNotExistsAsync(
                publicAccessType: PublicAccessType.Blob,
                cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(blobName);

            // Use block blob for large files with progress
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = file.ContentType,
                    CacheControl = "public, max-age=31536000"
                },
                TransferOptions = new StorageTransferOptions
                {
                    MaximumTransferSize = 4 * 1024 * 1024, // 4MB blocks
                    InitialTransferSize = 4 * 1024 * 1024
                },
                ProgressHandler = progress != null
                    ? new Progress<long>(progress.Report)
                    : null
            };

            await using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(
                stream,
                uploadOptions,
                cancellationToken);

            var blobUrl = blobClient.Uri.ToString();
            var cdnUrl = GetCdnUrl(blobUrl);

            return Result<BlobUploadResult>.Success(new BlobUploadResult(
                blobUrl,
                cdnUrl,
                blobName,
                file.Length,
                file.ContentType
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to upload video {BlobName}", blobName);
            return Result<BlobUploadResult>.Failure(
                "Failed to upload video to storage");
        }
    }

    private string GetCdnUrl(string blobUrl)
    {
        var cdnEndpoint = _configuration["Azure:CdnEndpoint"];
        if (string.IsNullOrEmpty(cdnEndpoint))
            return blobUrl;

        var uri = new Uri(blobUrl);
        return blobUrl.Replace(uri.Host, cdnEndpoint);
    }
}
```

### 4.3 Domain Event Handlers

#### ImageRemovedEventHandler
```csharp
public class ImageRemovedEventHandler
    : INotificationHandler<ImageRemovedFromEventDomainEvent>
{
    private readonly IAzureBlobService _blobService;
    private readonly ILogger<ImageRemovedEventHandler> _logger;

    public async Task Handle(
        ImageRemovedFromEventDomainEvent notification,
        CancellationToken cancellationToken)
    {
        // Asynchronous blob cleanup
        var result = await _blobService.DeleteBlobAsync(
            notification.BlobName,
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to delete blob {BlobName} for event {EventId}",
                notification.BlobName,
                notification.EventId);
        }
    }
}
```

#### VideoRemovedEventHandler
```csharp
public class VideoRemovedEventHandler
    : INotificationHandler<VideoRemovedFromEventDomainEvent>
{
    private readonly IAzureBlobService _blobService;
    private readonly ILogger<VideoRemovedEventHandler> _logger;

    public async Task Handle(
        VideoRemovedFromEventDomainEvent notification,
        CancellationToken cancellationToken)
    {
        // Delete both video and thumbnail blobs
        var blobNames = new List<string>
        {
            notification.VideoBlobName,
            notification.ThumbnailBlobName
        };

        var result = await _blobService.DeleteBlobsAsync(
            blobNames,
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to delete video blobs for event {EventId}",
                notification.EventId);
        }
    }
}
```

### 4.4 Repository Pattern

The existing `IEventRepository` handles persistence of the Event aggregate, including images and videos through EF Core navigation properties.

```csharp
public interface IEventRepository : IRepository<Event>
{
    // Existing methods handle images/videos automatically
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Event entity, CancellationToken cancellationToken);
    Task UpdateAsync(Event entity, CancellationToken cancellationToken);
}
```

---

## 5. Presentation Layer (API)

### 5.1 API Endpoints

#### Upload Image
```
POST /api/events/{eventId}/images
Content-Type: multipart/form-data

FormData:
  - imageFile: File (required)
  - targetDisplayOrder: int (optional)

Response 201:
{
  "id": "uuid",
  "imageUrl": "https://blob.azure.com/...",
  "cdnUrl": "https://cdn.lankaconnect.com/...",
  "displayOrder": 1,
  "uploadedAt": "2025-12-01T10:00:00Z"
}
```

#### Upload Video
```
POST /api/events/{eventId}/videos
Content-Type: multipart/form-data

FormData:
  - videoFile: File (required)
  - thumbnailFile: File (optional)

Response 201:
{
  "id": "uuid",
  "videoUrl": "https://blob.azure.com/...",
  "thumbnailUrl": "https://blob.azure.com/...",
  "cdnVideoUrl": "https://cdn.lankaconnect.com/...",
  "cdnThumbnailUrl": "https://cdn.lankaconnect.com/...",
  "duration": "00:05:30",
  "format": "mp4",
  "fileSizeBytes": 52428800,
  "displayOrder": 1,
  "uploadedAt": "2025-12-01T10:00:00Z"
}
```

#### Delete Image
```
DELETE /api/events/{eventId}/images/{imageId}

Response 204: No Content
```

#### Delete Video
```
DELETE /api/events/{eventId}/videos/{videoId}

Response 204: No Content
```

#### Replace Image
```
PUT /api/events/{eventId}/images/{imageId}
Content-Type: multipart/form-data

FormData:
  - newImageFile: File (required)

Response 200: EventImageDto
```

#### Reorder Images
```
PUT /api/events/{eventId}/images/reorder
Content-Type: application/json

Body:
{
  "newDisplayOrders": {
    "image-id-1": 3,
    "image-id-2": 1,
    "image-id-3": 2
  }
}

Response 204: No Content
```

#### Get Event Media
```
GET /api/events/{eventId}/media

Response 200:
{
  "eventId": "uuid",
  "images": [ EventImageDto[] ],
  "videos": [ EventVideoDto[] ],
  "imagesCount": 3,
  "videosCount": 1,
  "canAddMoreImages": true,
  "canAddMoreVideos": true
}
```

### 5.2 Controller Implementation

```csharp
[ApiController]
[Route("api/events/{eventId:guid}")]
public class EventMediaController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost("images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_485_760)] // 10MB
    public async Task<ActionResult<EventImageDto>> UploadImage(
        Guid eventId,
        [FromForm] IFormFile imageFile,
        [FromForm] int? targetDisplayOrder = null)
    {
        var command = new UploadEventImageCommand(
            eventId,
            imageFile,
            targetDisplayOrder);

        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? CreatedAtAction(
                nameof(GetEventMedia),
                new { eventId },
                result.Value)
            : BadRequest(result.Error);
    }

    [HttpPost("videos")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(524_288_000)] // 500MB
    [RequestFormLimits(MultipartBodyLengthLimit = 524_288_000)]
    public async Task<ActionResult<EventVideoDto>> UploadVideo(
        Guid eventId,
        [FromForm] IFormFile videoFile,
        [FromForm] IFormFile? thumbnailFile = null)
    {
        var command = new UploadEventVideoCommand(
            eventId,
            videoFile,
            thumbnailFile);

        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? CreatedAtAction(
                nameof(GetEventMedia),
                new { eventId },
                result.Value)
            : BadRequest(result.Error);
    }

    [HttpDelete("images/{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(
        Guid eventId,
        Guid imageId)
    {
        var command = new DeleteEventImageCommand(eventId, imageId);
        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? NoContent()
            : BadRequest(result.Error);
    }

    [HttpDelete("videos/{videoId:guid}")]
    public async Task<IActionResult> DeleteVideo(
        Guid eventId,
        Guid videoId)
    {
        var command = new DeleteEventVideoCommand(eventId, videoId);
        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? NoContent()
            : BadRequest(result.Error);
    }

    [HttpPut("images/{imageId:guid}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_485_760)] // 10MB
    public async Task<ActionResult<EventImageDto>> ReplaceImage(
        Guid eventId,
        Guid imageId,
        [FromForm] IFormFile newImageFile)
    {
        var command = new ReplaceEventImageCommand(
            eventId,
            imageId,
            newImageFile);

        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpGet("media")]
    public async Task<ActionResult<EventMediaDto>> GetEventMedia(
        Guid eventId)
    {
        var query = new GetEventMediaQuery(eventId);
        var result = await _mediator.Send(query);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Error);
    }
}
```

---

## 6. Frontend Components (Next.js + TypeScript)

### 6.1 Component Structure

```
web/src/
├── components/
│   ├── events/
│   │   ├── media/
│   │   │   ├── EventMediaUploader.tsx
│   │   │   ├── ImageUploadZone.tsx
│   │   │   ├── VideoUploadZone.tsx
│   │   │   ├── MediaGallery.tsx
│   │   │   ├── ImageGalleryItem.tsx
│   │   │   ├── VideoGalleryItem.tsx
│   │   │   ├── UploadProgressBar.tsx
│   │   │   └── MediaReorderableList.tsx
├── hooks/
│   ├── useMediaUpload.ts
│   ├── useImageReorder.ts
│   └── useMediaGallery.ts
├── infrastructure/
│   └── api/
│       └── repositories/
│           └── event-media-repository.ts
└── types/
    └── media.ts
```

### 6.2 Type Definitions

```typescript
// web/src/types/media.ts

export interface EventImageDto {
  id: string;
  imageUrl: string;
  cdnUrl: string;
  displayOrder: number;
  uploadedAt: string;
}

export interface EventVideoDto {
  id: string;
  videoUrl: string;
  thumbnailUrl: string;
  cdnVideoUrl: string;
  cdnThumbnailUrl: string;
  duration: string | null;
  format: string;
  fileSizeBytes: number;
  displayOrder: number;
  uploadedAt: string;
}

export interface EventMediaDto {
  eventId: string;
  images: EventImageDto[];
  videos: EventVideoDto[];
  imagesCount: number;
  videosCount: number;
  canAddMoreImages: boolean;
  canAddMoreVideos: boolean;
}

export interface MediaUploadProgress {
  uploadId: string;
  bytesUploaded: number;
  totalBytes: number;
  percentComplete: number;
  status: 'pending' | 'uploading' | 'processing' | 'completed' | 'failed';
  errorMessage?: string;
}

export interface MediaValidationError {
  field: string;
  message: string;
}
```

### 6.3 API Repository

```typescript
// web/src/infrastructure/api/repositories/event-media-repository.ts

import { apiClient } from '../api-client';
import type {
  EventMediaDto,
  EventImageDto,
  EventVideoDto
} from '@/types/media';

export class EventMediaRepository {
  async getEventMedia(eventId: string): Promise<EventMediaDto> {
    const response = await apiClient.get(`/events/${eventId}/media`);
    return response.data;
  }

  async uploadImage(
    eventId: string,
    imageFile: File,
    onProgress?: (progress: number) => void
  ): Promise<EventImageDto> {
    const formData = new FormData();
    formData.append('imageFile', imageFile);

    const response = await apiClient.post(
      `/events/${eventId}/images`,
      formData,
      {
        headers: { 'Content-Type': 'multipart/form-data' },
        onUploadProgress: (progressEvent) => {
          if (progressEvent.total && onProgress) {
            const percent = Math.round(
              (progressEvent.loaded * 100) / progressEvent.total
            );
            onProgress(percent);
          }
        }
      }
    );
    return response.data;
  }

  async uploadVideo(
    eventId: string,
    videoFile: File,
    thumbnailFile?: File,
    onProgress?: (progress: number) => void
  ): Promise<EventVideoDto> {
    const formData = new FormData();
    formData.append('videoFile', videoFile);
    if (thumbnailFile) {
      formData.append('thumbnailFile', thumbnailFile);
    }

    const response = await apiClient.post(
      `/events/${eventId}/videos`,
      formData,
      {
        headers: { 'Content-Type': 'multipart/form-data' },
        onUploadProgress: (progressEvent) => {
          if (progressEvent.total && onProgress) {
            const percent = Math.round(
              (progressEvent.loaded * 100) / progressEvent.total
            );
            onProgress(percent);
          }
        }
      }
    );
    return response.data;
  }

  async deleteImage(eventId: string, imageId: string): Promise<void> {
    await apiClient.delete(`/events/${eventId}/images/${imageId}`);
  }

  async deleteVideo(eventId: string, videoId: string): Promise<void> {
    await apiClient.delete(`/events/${eventId}/videos/${videoId}`);
  }

  async replaceImage(
    eventId: string,
    imageId: string,
    newImageFile: File
  ): Promise<EventImageDto> {
    const formData = new FormData();
    formData.append('newImageFile', newImageFile);

    const response = await apiClient.put(
      `/events/${eventId}/images/${imageId}`,
      formData,
      {
        headers: { 'Content-Type': 'multipart/form-data' }
      }
    );
    return response.data;
  }

  async reorderImages(
    eventId: string,
    newDisplayOrders: Record<string, number>
  ): Promise<void> {
    await apiClient.put(
      `/events/${eventId}/images/reorder`,
      { newDisplayOrders }
    );
  }
}

export const eventMediaRepository = new EventMediaRepository();
```

### 6.4 Custom Hook: useMediaUpload

```typescript
// web/src/hooks/useMediaUpload.ts

import { useState } from 'react';
import { eventMediaRepository } from '@/infrastructure/api/repositories/event-media-repository';
import type { EventImageDto, EventVideoDto } from '@/types/media';

export function useMediaUpload() {
  const [isUploading, setIsUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [error, setError] = useState<string | null>(null);

  const uploadImage = async (
    eventId: string,
    file: File
  ): Promise<EventImageDto | null> => {
    setIsUploading(true);
    setError(null);
    setUploadProgress(0);

    try {
      const result = await eventMediaRepository.uploadImage(
        eventId,
        file,
        (progress) => setUploadProgress(progress)
      );
      return result;
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to upload image');
      return null;
    } finally {
      setIsUploading(false);
      setUploadProgress(0);
    }
  };

  const uploadVideo = async (
    eventId: string,
    videoFile: File,
    thumbnailFile?: File
  ): Promise<EventVideoDto | null> => {
    setIsUploading(true);
    setError(null);
    setUploadProgress(0);

    try {
      const result = await eventMediaRepository.uploadVideo(
        eventId,
        videoFile,
        thumbnailFile,
        (progress) => setUploadProgress(progress)
      );
      return result;
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to upload video');
      return null;
    } finally {
      setIsUploading(false);
      setUploadProgress(0);
    }
  };

  return {
    uploadImage,
    uploadVideo,
    isUploading,
    uploadProgress,
    error
  };
}
```

### 6.5 Component: ImageUploadZone

```typescript
// web/src/components/events/media/ImageUploadZone.tsx

'use client';

import { useCallback } from 'react';
import { useDropzone } from 'react-dropzone';
import { Upload, Image as ImageIcon } from 'lucide-react';

interface ImageUploadZoneProps {
  onFilesSelected: (files: File[]) => void;
  maxFiles?: number;
  disabled?: boolean;
}

export function ImageUploadZone({
  onFilesSelected,
  maxFiles = 10,
  disabled = false
}: ImageUploadZoneProps) {
  const onDrop = useCallback((acceptedFiles: File[]) => {
    onFilesSelected(acceptedFiles);
  }, [onFilesSelected]);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: {
      'image/jpeg': ['.jpg', '.jpeg'],
      'image/png': ['.png'],
      'image/gif': ['.gif'],
      'image/webp': ['.webp']
    },
    maxFiles,
    maxSize: 10 * 1024 * 1024, // 10MB
    disabled
  });

  return (
    <div
      {...getRootProps()}
      className={`
        border-2 border-dashed rounded-lg p-8
        flex flex-col items-center justify-center
        cursor-pointer transition-colors
        ${isDragActive
          ? 'border-blue-500 bg-blue-50'
          : 'border-gray-300 hover:border-gray-400'}
        ${disabled ? 'opacity-50 cursor-not-allowed' : ''}
      `}
    >
      <input {...getInputProps()} />

      <ImageIcon className="w-12 h-12 text-gray-400 mb-4" />

      {isDragActive ? (
        <p className="text-blue-600 font-medium">
          Drop images here...
        </p>
      ) : (
        <>
          <p className="text-gray-700 font-medium mb-2">
            Drag & drop images here
          </p>
          <p className="text-gray-500 text-sm mb-4">
            or click to browse
          </p>
          <p className="text-xs text-gray-400">
            JPG, PNG, GIF, WebP up to 10MB
          </p>
        </>
      )}
    </div>
  );
}
```

### 6.6 Component: MediaGallery

```typescript
// web/src/components/events/media/MediaGallery.tsx

'use client';

import { useState, useEffect } from 'react';
import { eventMediaRepository } from '@/infrastructure/api/repositories/event-media-repository';
import { ImageGalleryItem } from './ImageGalleryItem';
import { VideoGalleryItem } from './VideoGalleryItem';
import type { EventMediaDto } from '@/types/media';

interface MediaGalleryProps {
  eventId: string;
  editable?: boolean;
  onMediaDeleted?: () => void;
}

export function MediaGallery({
  eventId,
  editable = false,
  onMediaDeleted
}: MediaGalleryProps) {
  const [media, setMedia] = useState<EventMediaDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    loadMedia();
  }, [eventId]);

  const loadMedia = async () => {
    try {
      const data = await eventMediaRepository.getEventMedia(eventId);
      setMedia(data);
    } catch (error) {
      console.error('Failed to load media:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleDeleteImage = async (imageId: string) => {
    if (!confirm('Are you sure you want to delete this image?')) return;

    try {
      await eventMediaRepository.deleteImage(eventId, imageId);
      await loadMedia();
      onMediaDeleted?.();
    } catch (error) {
      console.error('Failed to delete image:', error);
    }
  };

  const handleDeleteVideo = async (videoId: string) => {
    if (!confirm('Are you sure you want to delete this video?')) return;

    try {
      await eventMediaRepository.deleteVideo(eventId, videoId);
      await loadMedia();
      onMediaDeleted?.();
    } catch (error) {
      console.error('Failed to delete video:', error);
    }
  };

  if (isLoading) {
    return <div>Loading media...</div>;
  }

  if (!media) {
    return <div>No media found</div>;
  }

  return (
    <div className="space-y-8">
      {/* Images Section */}
      {media.images.length > 0 && (
        <div>
          <h3 className="text-lg font-semibold mb-4">
            Images ({media.imagesCount})
          </h3>
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
            {media.images.map((image) => (
              <ImageGalleryItem
                key={image.id}
                image={image}
                editable={editable}
                onDelete={() => handleDeleteImage(image.id)}
              />
            ))}
          </div>
        </div>
      )}

      {/* Videos Section */}
      {media.videos.length > 0 && (
        <div>
          <h3 className="text-lg font-semibold mb-4">
            Videos ({media.videosCount})
          </h3>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {media.videos.map((video) => (
              <VideoGalleryItem
                key={video.id}
                video={video}
                editable={editable}
                onDelete={() => handleDeleteVideo(video.id)}
              />
            ))}
          </div>
        </div>
      )}

      {/* Empty State */}
      {media.imagesCount === 0 && media.videosCount === 0 && (
        <div className="text-center py-12 text-gray-500">
          No images or videos uploaded yet
        </div>
      )}
    </div>
  );
}
```

---

## 7. Security and Validation Strategy

### 7.1 Security Considerations

**1. File Upload Security**
- Validate file extensions and MIME types
- Scan uploaded files for malware (future: Azure Defender)
- Limit file sizes (10MB images, 500MB videos)
- Use unique blob names to prevent overwrites
- Sanitize file names before storage

**2. Authorization**
- Only event organizers can upload/delete media
- Check event ownership before allowing operations
- Validate user permissions at API level

**3. Blob Storage Security**
- Public read access for media blobs
- SAS tokens for temporary upload URLs (future enhancement)
- CORS configuration for web uploads
- HTTPS only for blob access

**4. Rate Limiting**
- Implement rate limits on upload endpoints
- Prevent abuse with request throttling
- Monitor upload patterns for anomalies

### 7.2 Validation Pipeline

**Backend Validation (API Layer):**
```csharp
public class ImageFileValidator
{
    private static readonly string[] AllowedExtensions =
        { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private static readonly string[] AllowedMimeTypes =
        { "image/jpeg", "image/png", "image/gif", "image/webp" };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

    public Result Validate(IFormFile file)
    {
        // 1. Check file is provided
        if (file == null || file.Length == 0)
            return Result.Failure("No file provided");

        // 2. Check file size
        if (file.Length > MaxFileSizeBytes)
            return Result.Failure(
                $"File size exceeds maximum of {MaxFileSizeBytes / 1024 / 1024}MB");

        // 3. Check extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return Result.Failure(
                $"File type {extension} is not allowed. " +
                $"Allowed types: {string.Join(", ", AllowedExtensions)}");

        // 4. Check MIME type
        if (!AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            return Result.Failure(
                $"MIME type {file.ContentType} is not allowed");

        // 5. Validate image dimensions (future: use ImageSharp)
        // var dimensions = await GetImageDimensions(file);
        // if (dimensions.Width < 800 || dimensions.Height < 600)
        //     return Result.Failure("Image too small (min 800x600)");

        return Result.Success();
    }
}
```

**Frontend Validation (TypeScript):**
```typescript
export class MediaValidator {
  static validateImage(file: File): MediaValidationError | null {
    // Check file type
    const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
      return {
        field: 'imageFile',
        message: 'Only JPG, PNG, GIF, and WebP images are allowed'
      };
    }

    // Check file size (10MB)
    const maxSize = 10 * 1024 * 1024;
    if (file.size > maxSize) {
      return {
        field: 'imageFile',
        message: 'Image must be less than 10MB'
      };
    }

    return null;
  }

  static validateVideo(file: File): MediaValidationError | null {
    // Check file type
    const allowedTypes = ['video/mp4', 'video/quicktime', 'video/x-msvideo', 'video/webm'];
    if (!allowedTypes.includes(file.type)) {
      return {
        field: 'videoFile',
        message: 'Only MP4, MOV, AVI, and WebM videos are allowed'
      };
    }

    // Check file size (500MB)
    const maxSize = 500 * 1024 * 1024;
    if (file.size > maxSize) {
      return {
        field: 'videoFile',
        message: 'Video must be less than 500MB'
      };
    }

    return null;
  }
}
```

---

## 8. Performance Considerations

### 8.1 Lazy Loading Strategy

**Backend (EF Core):**
```csharp
// Only load media when explicitly needed
var eventWithMedia = await _context.Events
    .Include(e => e.Images.OrderBy(i => i.DisplayOrder))
    .Include(e => e.Videos.OrderBy(v => v.DisplayOrder))
    .FirstOrDefaultAsync(e => e.Id == eventId);

// Load event details separately for list views
var eventsWithoutMedia = await _context.Events
    .Select(e => new EventListDto
    {
        Id = e.Id,
        Title = e.Title.Value,
        StartDate = e.StartDate,
        // No media loaded
    })
    .ToListAsync();
```

**Frontend (Next.js):**
```typescript
// Load media on-demand with React Query
export function useEventMedia(eventId: string) {
  return useQuery({
    queryKey: ['event-media', eventId],
    queryFn: () => eventMediaRepository.getEventMedia(eventId),
    staleTime: 5 * 60 * 1000, // 5 minutes
    // Only fetch when component mounts
    enabled: !!eventId
  });
}
```

### 8.2 CDN Integration

**Azure CDN Configuration:**
```json
{
  "cdn": {
    "endpoint": "https://cdn.lankaconnect.com",
    "profile": "LankaConnect-CDN",
    "originHost": "lankaconnectstorage.blob.core.windows.net",
    "cacheRules": {
      "images": {
        "ttl": 31536000,
        "queryStringBehavior": "IgnoreQueryString"
      },
      "videos": {
        "ttl": 31536000,
        "queryStringBehavior": "IgnoreQueryString"
      }
    }
  }
}
```

**Benefits:**
- 99.9% availability SLA
- Global edge caching
- Reduced origin load
- Faster content delivery
- Automatic compression (gzip, brotli)

### 8.3 Image Optimization

**Server-Side (Future Enhancement):**
- Use ImageSharp for resizing
- Generate multiple sizes (thumbnail, medium, large)
- Convert to WebP for browsers that support it
- Store multiple versions in blob storage

**Client-Side:**
```typescript
// Progressive image loading with blur placeholder
import Image from 'next/image';

export function OptimizedEventImage({
  src,
  alt
}: {
  src: string;
  alt: string
}) {
  return (
    <Image
      src={src}
      alt={alt}
      width={800}
      height={600}
      loading="lazy"
      placeholder="blur"
      blurDataURL="data:image/svg+xml;base64,..."
      className="rounded-lg"
    />
  );
}
```

### 8.4 Database Query Optimization

**Indexes:**
- Composite index on `(EventId, DisplayOrder)` for ordering queries
- Single index on `EventId` for filtering
- Both indexes marked as unique where appropriate

**Query Patterns:**
```csharp
// Efficient batch loading with projection
var eventsWithMediaCounts = await _context.Events
    .Where(e => e.OrganizerId == organizerId)
    .Select(e => new
    {
        Event = e,
        ImageCount = e.Images.Count,
        VideoCount = e.Videos.Count
    })
    .ToListAsync();

// Avoid N+1 queries with Include
var events = await _context.Events
    .Include(e => e.Images)
    .Include(e => e.Videos)
    .Where(e => eventIds.Contains(e.Id))
    .ToListAsync();
```

### 8.5 Caching Strategy

**Backend (Distributed Cache):**
```csharp
public class CachedEventMediaService
{
    private readonly IDistributedCache _cache;
    private readonly IEventMediaService _mediaService;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);

    public async Task<EventMediaDto> GetEventMediaAsync(Guid eventId)
    {
        var cacheKey = $"event-media:{eventId}";

        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
            return JsonSerializer.Deserialize<EventMediaDto>(cached)!;

        var media = await _mediaService.GetEventMediaAsync(eventId);

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(media),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheDuration
            });

        return media;
    }
}
```

**Frontend (React Query):**
```typescript
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes
      cacheTime: 10 * 60 * 1000, // 10 minutes
      refetchOnWindowFocus: false
    }
  }
});
```

---

## 9. Testing Strategy

### 9.1 Unit Tests (Domain Layer)

```csharp
// Test: EventImage entity creation
[Fact]
public void EventImage_Create_WithValidData_ShouldSucceed()
{
    var eventId = Guid.NewGuid();
    var imageUrl = "https://blob.azure.com/image.jpg";
    var blobName = "event123-image.jpg";
    var displayOrder = 1;

    var image = EventImage.Create(eventId, imageUrl, blobName, displayOrder);

    image.Should().NotBeNull();
    image.EventId.Should().Be(eventId);
    image.ImageUrl.Should().Be(imageUrl);
    image.DisplayOrder.Should().Be(displayOrder);
}

// Test: Event aggregate enforces max images invariant
[Fact]
public void Event_AddImage_WhenMaxImagesReached_ShouldFail()
{
    var @event = CreateTestEvent();

    // Add 10 images (max limit)
    for (int i = 0; i < 10; i++)
    {
        @event.AddImage($"https://blob.azure.com/image{i}.jpg", $"image{i}.jpg");
    }

    // Attempt to add 11th image
    var result = @event.AddImage("https://blob.azure.com/image11.jpg", "image11.jpg");

    result.IsFailure.Should().BeTrue();
    result.Error.Should().Contain("cannot have more than 10 images");
}
```

### 9.2 Integration Tests (Infrastructure Layer)

```csharp
[Collection("AzureBlobTests")]
public class AzureBlobServiceTests : IAsyncLifetime
{
    private readonly AzureBlobService _blobService;
    private readonly List<string> _uploadedBlobs = new();

    [Fact]
    public async Task UploadImageAsync_WithValidFile_ShouldSucceed()
    {
        // Arrange
        var imageFile = CreateTestImageFile();
        var blobName = $"test-{Guid.NewGuid()}.jpg";
        _uploadedBlobs.Add(blobName);

        // Act
        var result = await _blobService.UploadImageAsync(imageFile, blobName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BlobUrl.Should().Contain(blobName);
        result.Value.CdnUrl.Should().Contain("cdn.lankaconnect.com");
    }

    public async Task DisposeAsync()
    {
        // Cleanup uploaded test blobs
        foreach (var blobName in _uploadedBlobs)
        {
            await _blobService.DeleteBlobAsync(blobName);
        }
    }
}
```

### 9.3 API Tests (Controller Layer)

```csharp
public class EventMediaControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    [Fact]
    public async Task UploadImage_WithAuthenticatedUser_ReturnsCreated()
    {
        // Arrange
        var eventId = await CreateTestEventAsync();
        var imageFile = CreateTestImageContent();

        using var content = new MultipartFormDataContent();
        content.Add(imageFile, "imageFile", "test.jpg");

        // Act
        var response = await _client.PostAsync(
            $"/api/events/{eventId}/images",
            content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<EventImageDto>();
        result.Should().NotBeNull();
        result!.ImageUrl.Should().NotBeEmpty();
    }
}
```

### 9.4 Frontend Tests (Component Layer)

```typescript
// ImageUploadZone.test.tsx
import { render, screen, fireEvent } from '@testing-library/react';
import { ImageUploadZone } from './ImageUploadZone';

describe('ImageUploadZone', () => {
  it('should accept valid image files', async () => {
    const onFilesSelected = jest.fn();
    render(<ImageUploadZone onFilesSelected={onFilesSelected} />);

    const file = new File(['test'], 'test.jpg', { type: 'image/jpeg' });
    const input = screen.getByRole('textbox', { hidden: true });

    fireEvent.change(input, { target: { files: [file] } });

    expect(onFilesSelected).toHaveBeenCalledWith([file]);
  });

  it('should display drag and drop UI', () => {
    render(<ImageUploadZone onFilesSelected={jest.fn()} />);

    expect(screen.getByText(/drag & drop images here/i)).toBeInTheDocument();
  });
});
```

---

## 10. Migration Plan

### 10.1 EF Core Migration

```bash
# Create migration
cd src/LankaConnect.Infrastructure
dotnet ef migrations add AddEventMediaTables --startup-project ../LankaConnect.API

# Apply migration to development database
dotnet ef database update --startup-project ../LankaConnect.API

# Generate SQL script for staging/production
dotnet ef migrations script --startup-project ../LankaConnect.API --output ../../scripts/migrations/20251201_AddEventMediaTables.sql
```

### 10.2 Azure Blob Storage Setup

```bash
# Create storage account (if not exists)
az storage account create \
  --name lankaconnectstorage \
  --resource-group LankaConnect-RG \
  --location eastus \
  --sku Standard_LRS \
  --kind StorageV2

# Create blob containers
az storage container create \
  --name event-images \
  --account-name lankaconnectstorage \
  --public-access blob

az storage container create \
  --name event-videos \
  --account-name lankaconnectstorage \
  --public-access blob

az storage container create \
  --name event-thumbnails \
  --account-name lankaconnectstorage \
  --public-access blob

# Configure CORS
az storage cors add \
  --services b \
  --methods GET POST PUT DELETE OPTIONS \
  --origins https://app.lankaconnect.com http://localhost:3000 \
  --allowed-headers '*' \
  --exposed-headers '*' \
  --max-age 3600 \
  --account-name lankaconnectstorage
```

### 10.3 Azure CDN Setup

```bash
# Create CDN profile
az cdn profile create \
  --name LankaConnect-CDN \
  --resource-group LankaConnect-RG \
  --sku Standard_Microsoft

# Create CDN endpoint
az cdn endpoint create \
  --name lankaconnect-media \
  --profile-name LankaConnect-CDN \
  --resource-group LankaConnect-RG \
  --origin lankaconnectstorage.blob.core.windows.net \
  --origin-host-header lankaconnectstorage.blob.core.windows.net \
  --enable-compression true

# Configure caching rules
az cdn endpoint rule add \
  --name lankaconnect-media \
  --profile-name LankaConnect-CDN \
  --resource-group LankaConnect-RG \
  --order 1 \
  --rule-name "CacheImages" \
  --match-variable RequestUri \
  --operator Contains \
  --match-values "event-images" "event-thumbnails" \
  --action-name CacheExpiration \
  --cache-behavior Override \
  --cache-duration 365:00:00:00
```

---

## 11. Configuration

### 11.1 Backend Configuration (appsettings.json)

```json
{
  "Azure": {
    "Storage": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=lankaconnectstorage;...",
      "Containers": {
        "Images": "event-images",
        "Videos": "event-videos",
        "Thumbnails": "event-thumbnails"
      }
    },
    "CdnEndpoint": "lankaconnect-media.azureedge.net"
  },
  "MediaUpload": {
    "Images": {
      "MaxSizeBytes": 10485760,
      "AllowedExtensions": [".jpg", ".jpeg", ".png", ".gif", ".webp"],
      "AllowedMimeTypes": ["image/jpeg", "image/png", "image/gif", "image/webp"]
    },
    "Videos": {
      "MaxSizeBytes": 524288000,
      "AllowedExtensions": [".mp4", ".mov", ".avi", ".webm"],
      "AllowedMimeTypes": ["video/mp4", "video/quicktime", "video/x-msvideo", "video/webm"]
    }
  }
}
```

### 11.2 Frontend Configuration (.env.local)

```bash
# API Base URL
NEXT_PUBLIC_API_BASE_URL=https://api-staging.lankaconnect.com

# Azure CDN
NEXT_PUBLIC_CDN_BASE_URL=https://lankaconnect-media.azureedge.net

# Upload Limits (bytes)
NEXT_PUBLIC_MAX_IMAGE_SIZE=10485760
NEXT_PUBLIC_MAX_VIDEO_SIZE=524288000
```

---

## 12. Deployment Checklist

### 12.1 Backend Deployment
- [ ] Apply EF Core migrations to staging database
- [ ] Deploy updated API to Azure App Service
- [ ] Verify Azure Blob Storage containers exist
- [ ] Configure CORS for blob storage
- [ ] Set up Azure CDN endpoint
- [ ] Update appsettings for staging environment
- [ ] Run integration tests against staging
- [ ] Monitor application logs for errors

### 12.2 Frontend Deployment
- [ ] Update environment variables for staging
- [ ] Build Next.js application
- [ ] Deploy to Vercel/Azure Static Web Apps
- [ ] Verify API connectivity from local dev
- [ ] Test media upload flow end-to-end
- [ ] Check CDN URL resolution
- [ ] Verify responsive design on mobile

### 12.3 Testing Checklist
- [ ] Unit tests pass (domain layer)
- [ ] Integration tests pass (infrastructure)
- [ ] API tests pass (controllers)
- [ ] Frontend component tests pass
- [ ] Manual E2E testing completed
- [ ] Performance testing completed
- [ ] Security scanning completed
- [ ] Accessibility testing completed

---

## 13. Future Enhancements

### 13.1 Phase 2 Features
1. **Image Resizing & Optimization**
   - Automatic thumbnail generation
   - Multiple image sizes (small, medium, large)
   - WebP conversion for supported browsers
   - Lazy loading with blur placeholder

2. **Video Processing**
   - Automatic thumbnail extraction
   - Video transcoding (multiple resolutions)
   - HLS/DASH streaming support
   - Progress tracking during upload

3. **Advanced Media Management**
   - Bulk upload (multiple files)
   - Drag-and-drop reordering
   - Image cropping/editing
   - Alt text for accessibility
   - Media metadata (EXIF, location)

4. **Security Enhancements**
   - SAS tokens for secure uploads
   - Malware scanning integration
   - Watermarking for images
   - DRM for videos

5. **Performance Optimizations**
   - Progressive image loading
   - Adaptive bitrate streaming
   - Edge caching improvements
   - Background job for cleanup

---

## Architecture Decision Records (ADRs)

### ADR-001: Azure Blob Storage for Media

**Context:**
Need cloud storage solution for event images and videos with high availability and global CDN support.

**Decision:**
Use Azure Blob Storage with Azure CDN for media assets.

**Rationale:**
- Native Azure integration
- 99.9% availability SLA
- Global CDN with edge caching
- Cost-effective for large files
- Supports public blob access
- Easy integration with .NET SDK

**Consequences:**
- Requires Azure subscription
- Blob cleanup via domain events
- CDN configuration needed
- Must handle blob URL expiration

---

### ADR-002: Separate Tables for Images and Videos

**Context:**
Need to store both images and videos with different metadata requirements.

**Decision:**
Create separate `EventImages` and `EventVideos` tables instead of single polymorphic `EventMedia` table.

**Rationale:**
- Different metadata (videos need duration, format, file size)
- Different constraints (max 10 images, max 3 videos)
- Cleaner domain model
- Better query performance
- Type-safe entity definitions

**Consequences:**
- Two tables instead of one
- Duplicate indexes (EventId, DisplayOrder)
- More EF Core configurations
- Simpler queries (no discriminator)

---

### ADR-003: Event Aggregate Owns Media Lifecycle

**Context:**
Need to determine aggregate boundaries for media management.

**Decision:**
Event aggregate controls all media operations (add, remove, reorder).

**Rationale:**
- Enforces invariants (max images/videos)
- Maintains consistency (display order)
- Single transaction boundary
- Domain events for side effects
- Follows DDD aggregate patterns

**Consequences:**
- Cannot delete images independently
- Must load event to modify media
- Cascade delete on event removal
- Domain events for blob cleanup

---

### ADR-004: Asynchronous Blob Cleanup via Domain Events

**Context:**
When deleting images/videos, need to clean up blobs from storage.

**Decision:**
Use domain events handled by background services for blob cleanup.

**Rationale:**
- Decouples domain from infrastructure
- Prevents transaction coupling
- Allows retry on failure
- Non-blocking for users
- Follows CQRS pattern

**Consequences:**
- Eventual consistency for cleanup
- Orphaned blobs if handler fails
- Need monitoring for cleanup jobs
- Requires domain event infrastructure

---

## Conclusion

This architecture provides a comprehensive, production-ready solution for event media upload functionality. It follows Clean Architecture principles, implements DDD patterns, supports TDD workflows, and integrates seamlessly with Azure cloud services.

**Key Strengths:**
- Clear separation of concerns across layers
- Type-safe domain model with enforced invariants
- Testable components at every layer
- Scalable Azure Blob Storage with CDN
- Reusable frontend components
- Comprehensive validation and security

**Next Steps:**
1. Review and approve architecture
2. Implement database migration
3. Create Azure Blob Storage infrastructure
4. Implement backend command handlers (TDD)
5. Build frontend components
6. Integration testing
7. Deploy to staging environment
8. Production rollout

---

**Document Version:** 1.0
**Last Updated:** 2025-12-01
**Status:** Ready for Implementation