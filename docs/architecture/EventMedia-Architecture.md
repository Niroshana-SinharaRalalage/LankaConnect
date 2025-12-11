# Event Media Architecture - Implementation Guide

## Overview
This document outlines the architecture for implementing Image Update and Video Support features in the Event Management system.

## Domain Model

### Event Aggregate (Root)

**Responsibilities:**
- Maintain consistency of image and video collections
- Enforce business rules (max counts, valid display orders)
- Raise domain events for media changes

**Invariants:**
- Maximum 10 images per event
- Maximum 3 videos per event
- DisplayOrder must be unique within each collection
- DisplayOrder must be sequential (0, 1, 2...)

**New Methods:**

```csharp
// Image Update
public Result ReplaceImage(Guid imageId, string newImageUrl, string newBlobName)
{
    // 1. Find existing image
    // 2. Create new EventImage with same DisplayOrder
    // 3. Remove old image from collection
    // 4. Add new image to collection
    // 5. Raise EventImageReplacedDomainEvent
}

// Video Management
public Result AddVideo(string videoUrl, string blobName, string thumbnailUrl,
                       string thumbnailBlobName, TimeSpan? duration, string format, long fileSizeBytes)
{
    // 1. Validate max video count (3)
    // 2. Calculate next DisplayOrder
    // 3. Create EventVideo entity
    // 4. Add to _videos collection
    // 5. Raise EventVideoAddedDomainEvent
}

public Result RemoveVideo(Guid videoId)
{
    // 1. Find video in collection
    // 2. Remove from collection
    // 3. Reorder remaining videos
    // 4. Raise EventVideoRemovedDomainEvent
}

public Result ReorderVideos(IEnumerable<(Guid VideoId, int NewOrder)> reorderRequests)
{
    // Similar pattern to ReorderImages()
}
```

### EventImage Entity

**Current Design (Keep As-Is):**
```csharp
public class EventImage
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public string ImageUrl { get; private set; }
    public string BlobName { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTime UploadedAt { get; private set; }

    // No changes needed - immutability preserved
}
```

### EventVideo Entity (NEW)

```csharp
public class EventVideo
{
    public Guid Id { get; private set; }
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
            VideoUrl = videoUrl,
            BlobName = blobName,
            ThumbnailUrl = thumbnailUrl,
            ThumbnailBlobName = thumbnailBlobName,
            DisplayOrder = displayOrder,
            Duration = duration,
            Format = format,
            FileSizeBytes = fileSizeBytes,
            UploadedAt = DateTime.UtcNow
        };
    }

    internal void UpdateDisplayOrder(int newOrder)
    {
        DisplayOrder = newOrder;
    }
}
```

## Application Layer

### Commands

**ReplaceEventImageCommand (NEW)**
```csharp
public record ReplaceEventImageCommand(
    Guid EventId,
    Guid ImageId,
    IFormFile NewImage
) : IRequest<Result<EventImageDto>>;
```

**Handler Flow:**
1. Load Event aggregate from repository
2. Upload new image to blob storage (new blob name)
3. Call Event.ReplaceImage() domain method
4. Save Event (EF tracks changes)
5. Delete old blob (compensating action if needed)
6. Return new EventImageDto

**AddVideoToEventCommand (NEW)**
```csharp
public record AddVideoToEventCommand(
    Guid EventId,
    IFormFile Video
) : IRequest<Result<EventVideoDto>>;
```

**Handler Flow:**
1. Validate video file (format, size)
2. Upload video to blob storage
3. Generate thumbnail (IVideoProcessingService)
4. Upload thumbnail to blob storage
5. Load Event aggregate
6. Call Event.AddVideo() with all URLs
7. Save Event
8. Return EventVideoDto

**DeleteEventVideoCommand (NEW)**
```csharp
public record DeleteEventVideoCommand(
    Guid EventId,
    Guid VideoId
) : IRequest<Result>;
```

**ReorderEventVideosCommand (NEW)**
```csharp
public record ReorderEventVideosCommand(
    Guid EventId,
    List<VideoReorderDto> ReorderRequests
) : IRequest<Result>;
```

### DTOs

```csharp
public record EventVideoDto(
    Guid Id,
    string VideoUrl,
    string ThumbnailUrl,
    int DisplayOrder,
    TimeSpan? Duration,
    string Format,
    long FileSizeBytes,
    DateTime UploadedAt
);

public record VideoReorderDto(Guid VideoId, int DisplayOrder);
```

## Infrastructure Layer

### Storage Services

**IMediaStorageService (NEW Interface)**
```csharp
public interface IMediaStorageService
{
    Task<(string Url, string BlobName)> UploadImageAsync(
        Stream imageStream,
        string fileName,
        string eventId,
        CancellationToken cancellationToken = default);

    Task<(string VideoUrl, string VideoBlobName, string ThumbnailUrl, string ThumbnailBlobName)> UploadVideoAsync(
        Stream videoStream,
        string fileName,
        string eventId,
        CancellationToken cancellationToken = default);

    Task DeleteImageAsync(string blobName, CancellationToken cancellationToken = default);
    Task DeleteVideoAsync(string videoBlobName, string thumbnailBlobName, CancellationToken cancellationToken = default);
}
```

**Implementation:**
```csharp
public class AzureBlobMediaStorageService : IMediaStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IVideoProcessingService _videoProcessor;
    private const string ImageContainer = "event-images";
    private const string VideoContainer = "event-videos";
    private const string ThumbnailContainer = "event-video-thumbnails";

    // Implementation details...
}
```

**IVideoProcessingService (NEW Interface)**
```csharp
public interface IVideoProcessingService
{
    Task<Stream> GenerateThumbnailAsync(Stream videoStream, CancellationToken cancellationToken = default);
    Task<TimeSpan?> GetDurationAsync(Stream videoStream, CancellationToken cancellationToken = default);
    Task<string> GetFormatAsync(Stream videoStream, CancellationToken cancellationToken = default);
}
```

**Initial Implementation (Stub):**
```csharp
public class BasicVideoProcessingService : IVideoProcessingService
{
    // Phase 1: Return null/defaults (no actual processing)
    // Phase 2: Integrate FFmpeg or Azure Media Services

    public Task<Stream> GenerateThumbnailAsync(Stream videoStream, CancellationToken cancellationToken = default)
    {
        // TODO: Implement with FFmpeg
        return Task.FromResult<Stream>(null);
    }

    public Task<TimeSpan?> GetDurationAsync(Stream videoStream, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<TimeSpan?>(null);
    }

    public Task<string> GetFormatAsync(Stream videoStream, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("mp4"); // Default
    }
}
```

### EF Core Configuration

**EventVideoConfiguration (NEW)**
```csharp
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

        builder.Property(v => v.Format)
            .HasMaxLength(50);

        builder.Property(v => v.FileSizeBytes)
            .IsRequired();

        builder.Property(v => v.DisplayOrder)
            .IsRequired();

        builder.Property(v => v.UploadedAt)
            .IsRequired();

        // Relationship
        builder.HasOne<Event>()
            .WithMany(e => e.Videos) // Navigation property to add
            .HasForeignKey(v => v.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => new { v.EventId, v.DisplayOrder })
            .IsUnique();
    }
}
```

### Migration

**Create event_videos table:**
```sql
CREATE TABLE event_videos (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    EventId uniqueidentifier NOT NULL,
    VideoUrl nvarchar(2048) NOT NULL,
    BlobName nvarchar(500) NOT NULL,
    ThumbnailUrl nvarchar(2048),
    ThumbnailBlobName nvarchar(500),
    Duration bigint,  -- Store as ticks
    Format nvarchar(50),
    FileSizeBytes bigint NOT NULL,
    DisplayOrder int NOT NULL,
    UploadedAt datetime2 NOT NULL,
    CONSTRAINT FK_EventVideos_Events FOREIGN KEY (EventId)
        REFERENCES Events(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_EventVideos_EventId_DisplayOrder
    ON event_videos (EventId, DisplayOrder);
```

## Domain Events

```csharp
public record EventImageReplacedDomainEvent(
    Guid EventId,
    Guid OldImageId,
    Guid NewImageId,
    string OldBlobName,
    string NewBlobName
) : IDomainEvent;

public record EventVideoAddedDomainEvent(
    Guid EventId,
    Guid VideoId,
    string VideoUrl,
    string ThumbnailUrl
) : IDomainEvent;

public record EventVideoRemovedDomainEvent(
    Guid EventId,
    Guid VideoId,
    string VideoBlobName,
    string ThumbnailBlobName
) : IDomainEvent;
```

## TDD Implementation Order

### Phase 1: Image Replace Feature
1. **Domain Tests** (Red)
   - Event.ReplaceImage() success case
   - Event.ReplaceImage() image not found
   - Event.ReplaceImage() maintains DisplayOrder
   - EventImageReplacedDomainEvent raised

2. **Domain Implementation** (Green)
   - Implement Event.ReplaceImage()
   - Ensure immutability of EventImage

3. **Application Tests** (Red)
   - ReplaceEventImageCommandHandler success
   - Blob upload failure handling
   - Old blob deletion (compensating transaction)

4. **Application Implementation** (Green)
   - Create ReplaceEventImageCommand
   - Implement handler with blob operations

5. **API Tests** (Red)
   - PUT /api/Events/{id}/images/{imageId} endpoint
   - Multipart form data handling
   - Authorization

6. **API Implementation** (Green)
   - Create controller endpoint
   - Validation and error handling

### Phase 2: Video Support Feature
1. **Domain Tests** (Red)
   - Event.AddVideo() success case
   - Event.AddVideo() max count validation (3)
   - Event.RemoveVideo() success
   - Event.ReorderVideos() success
   - Domain events raised

2. **Domain Implementation** (Green)
   - Create EventVideo entity
   - Implement Event video methods

3. **Infrastructure Tests** (Red)
   - Video blob upload
   - Thumbnail generation (stub)
   - Video deletion with thumbnail

4. **Infrastructure Implementation** (Green)
   - Extend IMediaStorageService
   - Create IVideoProcessingService stub
   - EF Core configuration

5. **Application Tests** (Red)
   - AddVideoToEventCommandHandler
   - DeleteEventVideoCommandHandler
   - ReorderEventVideosCommandHandler

6. **Application Implementation** (Green)
   - Implement all video commands
   - Handlers with blob operations

7. **API Tests** (Red)
   - POST /api/Events/{id}/videos
   - DELETE /api/Events/{id}/videos/{videoId}
   - PUT /api/Events/{id}/videos/reorder
   - GET /api/Events/{id}/videos

8. **API Implementation** (Green)
   - Create controller endpoints
   - DTOs and validation

9. **Migration** (Red-Green)
   - Create migration
   - Test migration up/down
   - Seed test data

## Validation Rules

### Image Replace
- File must be valid image format (jpg, jpeg, png, gif, webp)
- Max size: 5 MB
- Image must exist in Event.Images collection
- User must be event organizer

### Video Upload
- File must be valid video format (mp4, webm, mov)
- Max size: 100 MB
- Max 3 videos per event
- User must be event organizer

### Video Deletion
- Video must exist in Event.Videos collection
- User must be event organizer

## Error Handling

### Compensating Transactions

**Image Replace:**
1. Upload new blob → Success
2. Update database → Success
3. Delete old blob → **Failure**
   - Log error
   - Continue (blob will be orphaned)
   - Background job can clean up orphaned blobs

**Video Upload:**
1. Upload video → Success
2. Generate thumbnail → **Failure**
   - Delete video blob
   - Return error to user
   - No database record created

3. Upload thumbnail → **Failure**
   - Delete video blob
   - Return error to user
   - No database record created

4. Save to database → **Failure**
   - Delete video blob
   - Delete thumbnail blob
   - Return error to user

## Performance Considerations

### Video Upload
- Async processing recommended for large files
- Consider Azure Media Services for transcoding
- Stream directly to blob storage (no memory buffering)

### Thumbnail Generation
- Phase 1: Return null (no thumbnail)
- Phase 2: Generate during upload (blocks request)
- Phase 3: Background job (Hangfire) - better UX

### Storage Costs
- Videos are 20-50x larger than images
- Consider CDN for video delivery
- Implement retention policies for old events

## Security Considerations

1. **File Validation:**
   - Check file signatures (magic bytes)
   - Don't trust file extensions
   - Scan for malware (Azure Defender)

2. **Authorization:**
   - Only event organizer can manage media
   - Rate limiting on uploads

3. **Blob Security:**
   - SAS tokens for temporary access
   - Private containers with authenticated access
   - CORS configuration for frontend

## Migration Strategy

### Backward Compatibility
1. Existing events: No videos (Videos collection empty)
2. API: New endpoints don't affect existing functionality
3. Database: New table, no changes to existing tables

### Deployment Steps
1. Deploy migration (creates event_videos table)
2. Deploy application code (new endpoints)
3. Update frontend (optional video upload UI)
4. Monitor error logs

### Rollback Plan
1. Remove new endpoints from API
2. Drop event_videos table
3. Revert application code

## Testing Strategy

### Unit Tests (Domain)
- Event aggregate business logic
- EventVideo entity creation
- Domain events

### Integration Tests (Application)
- Command handlers with in-memory database
- Blob storage operations (Azurite emulator)
- Thumbnail generation stubs

### API Tests
- Endpoint contracts
- Multipart form data
- Authorization

### Manual Testing Checklist
- [ ] Upload image (existing feature - regression)
- [ ] Replace image with larger file
- [ ] Replace image with smaller file
- [ ] Replace non-existent image (should fail)
- [ ] Upload first video
- [ ] Upload 3 videos (max)
- [ ] Upload 4th video (should fail)
- [ ] Delete video
- [ ] Reorder videos
- [ ] View event with videos

## Future Enhancements

### Phase 3 (Post-MVP)
1. Video transcoding (multiple resolutions)
2. Adaptive bitrate streaming
3. Video thumbnails at multiple timestamps
4. Video editing (trim, crop)
5. Live streaming support
6. Video analytics (views, completion rate)

## Risks and Mitigation

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Large video uploads timeout | High | Medium | Stream to blob, use background processing |
| Blob deletion fails | Medium | Low | Background cleanup job |
| Video processing complex | High | Medium | Phase 1: No processing, Phase 2: Add later |
| Storage costs escalate | Medium | Medium | Implement retention policy, CDN |
| Malicious file uploads | High | Low | File validation, malware scanning |

## Success Metrics

### Image Replace
- [ ] Zero data loss during replace
- [ ] No orphaned blobs after 24 hours
- [ ] <2s response time for replace operation

### Video Support
- [ ] Support 100 MB files
- [ ] <10s upload time for 50 MB video
- [ ] Background thumbnail generation <30s
- [ ] Zero data loss on upload failures

## Conclusion

This architecture:
- ✅ Follows DDD principles (aggregate consistency, domain events)
- ✅ Maintains backward compatibility
- ✅ Uses existing patterns (Commands, Handlers, DTOs)
- ✅ Separates concerns (Domain, Application, Infrastructure)
- ✅ Supports TDD workflow
- ✅ Scalable for future enhancements (transcoding, streaming)

**Next Steps:**
1. Review and approve architecture
2. Create feature branches
3. Implement Phase 1 (TDD red-green-refactor)
4. Integration testing
5. Staging deployment
6. Production deployment
