# Event Media Features - Risk Assessment and Mitigation Strategies

## Executive Summary

This document identifies potential risks in implementing Image Replace and Video Support features, along with detailed mitigation strategies. Risks are categorized by severity and likelihood, with clear action plans.

---

## Risk Matrix Overview

```
Impact
  ^
H │ 3,7,10      │ 1,4,5,8
  │             │
M │ 6,11        │ 2,9,12
  │             │
L │             │ 13,14,15
  │_____________│____________>
     Low    Med    High     Probability
```

**Risk Categories:**
- 🔴 Critical (High Impact + High Probability): Immediate action required
- 🟠 High (High Impact OR High Probability): Plan mitigation before implementation
- 🟡 Medium (Medium Impact + Medium Probability): Monitor and plan
- 🟢 Low (Low Impact + Low Probability): Accept or minimal mitigation

---

## 🔴 Critical Risks

### Risk 1: Large Video Upload Timeouts
**Category:** Technical
**Impact:** High | **Probability:** High | **Severity:** CRITICAL

**Description:**
Video files (50-100 MB) may cause HTTP request timeouts during upload, especially on slow connections.

**Consequences:**
- Poor user experience (failed uploads)
- Orphaned blobs in storage (partial uploads)
- Frustrated event organizers
- Support ticket volume increase

**Mitigation Strategies:**

1. **Stream Directly to Blob Storage (P0 - Must Have)**
   ```csharp
   // DON'T: Buffer entire file in memory
   byte[] fileBytes = await request.Video.ReadAsByteArrayAsync();

   // DO: Stream directly to blob
   using var stream = request.Video.OpenReadStream();
   await blobClient.UploadAsync(stream, ...);
   ```
   - Implementation: Use `Stream` instead of `byte[]`
   - Benefit: Constant memory usage, faster uploads
   - Timeline: Implement in Phase 1

2. **Increase Request Timeout (P0)**
   ```csharp
   // appsettings.json
   "Kestrel": {
     "Limits": {
       "RequestBodySize": 104857600, // 100 MB
       "KeepAliveTimeout": "00:02:00",
       "RequestHeadersTimeout": "00:02:00"
     }
   }
   ```

3. **Client-Side Chunked Upload (P1 - Should Have)**
   - Phase 3 enhancement
   - Use Azure Blob Storage Block Upload API
   - Resume failed uploads
   - Show progress bar to user

4. **Background Job Processing (P1)**
   ```csharp
   // Store video in temp storage
   // Create Hangfire job for actual upload
   // Return immediately to user
   BackgroundJob.Enqueue<VideoUploadJob>(
       job => job.ProcessVideoAsync(tempPath, eventId));
   ```

**Success Metrics:**
- ✅ 95% of video uploads succeed within 30 seconds
- ✅ Zero timeout errors for files < 100 MB
- ✅ User receives upload progress feedback

**Monitoring:**
- Azure Application Insights: Track upload duration
- Alert when average upload time > 30s
- Track timeout exception rate

**Rollback Plan:**
- If timeouts persist, reduce max file size to 50 MB
- Enable background processing immediately

---

### Risk 4: Storage Costs Exceed Budget
**Category:** Financial
**Impact:** High | **Probability:** High | **Severity:** CRITICAL

**Description:**
Video files are 20-50x larger than images. With 3 videos per event @ 50 MB average, storage costs could escalate quickly.

**Cost Projection:**
```
Scenario: 10,000 events with videos
- Average video size: 50 MB
- Videos per event: 2 (average)
- Total storage: 10,000 * 2 * 50 MB = 1 TB
- Azure Blob Hot Tier: ~$20/month for 1 TB
- Bandwidth (egress): ~$100/month at 10% retrieval rate
- TOTAL: ~$120/month
```

**Consequences:**
- Unexpected cloud bills
- Need to reduce features or increase pricing
- Negative impact on profit margins

**Mitigation Strategies:**

1. **Implement Storage Quota per Event (P0)**
   ```csharp
   public class Event
   {
       private const long MaxStorageBytesPerEvent = 150_000_000; // 150 MB

       public Result AddVideo(...)
       {
           var totalStorage = _videos.Sum(v => v.FileSizeBytes)
                            + _images.Sum(i => i.FileSizeBytes);

           if (totalStorage + fileSizeBytes > MaxStorageBytesPerEvent)
           {
               return Result.Failure("Event storage quota exceeded (150 MB)");
           }
           // ...
       }
   }
   ```

2. **Use Azure Cool/Archive Tier for Old Events (P1)**
   ```csharp
   // Hangfire job: Move blobs for events > 90 days old
   public async Task ArchiveOldEventMediaAsync()
   {
       var oldEvents = await _context.Events
           .Where(e => e.EventDate < DateTime.UtcNow.AddDays(-90))
           .ToListAsync();

       foreach (var evt in oldEvents)
       {
           await _blobService.SetAccessTierAsync(
               evt.Videos.Select(v => v.BlobName),
               AccessTier.Cool); // Hot: $20/TB -> Cool: $10/TB
       }
   }
   ```

3. **Implement CDN for Video Delivery (P1)**
   - Reduce egress costs by 60-70%
   - Cache popular videos at edge locations
   - Azure CDN Standard: ~$0.08/GB vs Direct: ~$0.12/GB

4. **Retention Policy (P1)**
   ```csharp
   // Delete media for events > 1 year old
   // Notify organizers 30 days before deletion
   ```

5. **Monitoring and Alerts (P0)**
   ```csharp
   // Azure Monitor alert: Storage growth > 100 GB/month
   // Weekly cost report email to admins
   ```

**Success Metrics:**
- ✅ Storage cost < $150/month for first 10,000 events
- ✅ No unexpected bills > 20% of budget
- ✅ Average storage per event < 50 MB

**Monitoring:**
- Daily storage cost tracking
- Alert when monthly projected cost > budget + 20%
- Dashboard showing storage growth trend

---

### Risk 5: Malicious File Uploads
**Category:** Security
**Impact:** High | **Probability:** High | **Severity:** CRITICAL

**Description:**
Users could upload malicious files disguised as videos/images (malware, scripts, executable content).

**Consequences:**
- Security breach
- Infected user devices
- Legal liability
- Reputation damage
- Platform suspension by Azure

**Mitigation Strategies:**

1. **File Type Validation - Magic Bytes (P0)**
   ```csharp
   public class VideoValidator
   {
       private static readonly Dictionary<string, byte[]> FileSignatures = new()
       {
           { "mp4",  new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 } },
           { "webm", new byte[] { 0x1A, 0x45, 0xDF, 0xA3 } },
           { "jpg",  new byte[] { 0xFF, 0xD8, 0xFF } }
       };

       public async Task<bool> ValidateFileSignatureAsync(Stream stream, string extension)
       {
           if (!FileSignatures.TryGetValue(extension, out var signature))
               return false;

           var buffer = new byte[signature.Length];
           await stream.ReadAsync(buffer, 0, buffer.Length);

           return buffer.SequenceEqual(signature);
       }
   }
   ```

2. **Azure Defender for Storage (P0)**
   ```bash
   # Enable in Azure Portal
   # Auto-scans uploaded blobs for malware
   # Cost: ~$10/month per storage account
   ```

3. **Content Security Policy (P1)**
   ```csharp
   // Serve videos with restrictive headers
   app.Use(async (context, next) =>
   {
       if (context.Request.Path.StartsWithSegments("/media"))
       {
           context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
           context.Response.Headers.Add("Content-Disposition", "attachment");
       }
       await next();
   });
   ```

4. **Rate Limiting per User (P0)**
   ```csharp
   [RateLimit(10, "1m")] // Max 10 uploads per minute
   public async Task<IActionResult> AddEventVideo(...)
   ```

5. **File Size Limits (P0)**
   - Images: 5 MB
   - Videos: 100 MB
   - Hard-coded in both client and server

**Success Metrics:**
- ✅ Zero malicious files uploaded to production
- ✅ 100% of files validated for magic bytes
- ✅ Azure Defender enabled with zero exceptions

**Monitoring:**
- Azure Defender alerts
- Track validation failures by user
- Ban users with repeated validation failures

---

### Risk 8: Domain Invariant Violations
**Category:** Technical
**Impact:** High | **Probability:** Medium | **Severity:** HIGH

**Description:**
Business rules (max 10 images, max 3 videos, unique DisplayOrder) could be violated due to concurrency or bugs.

**Consequences:**
- Data integrity issues
- Broken UI (unexpected number of media)
- Database constraint violations
- Production errors

**Mitigation Strategies:**

1. **Domain-Level Validation (P0)**
   ```csharp
   public Result AddVideo(...)
   {
       // ALWAYS check invariants in domain layer
       if (_videos.Count >= MaxVideosPerEvent)
       {
           return Result.Failure($"Cannot add more than {MaxVideosPerEvent} videos");
       }

       // Validate DisplayOrder uniqueness
       if (_videos.Any(v => v.DisplayOrder == displayOrder))
       {
           return Result.Failure("DisplayOrder must be unique");
       }

       // ...
   }
   ```

2. **Database Constraints (P0)**
   ```csharp
   // EventVideoConfiguration.cs
   builder.HasIndex(v => new { v.EventId, v.DisplayOrder })
       .IsUnique()
       .HasDatabaseName("IX_EventVideos_EventId_DisplayOrder");

   builder.HasCheckConstraint(
       "CK_EventVideos_DisplayOrder_NonNegative",
       "[DisplayOrder] >= 0");
   ```

3. **Optimistic Concurrency (P1)**
   ```csharp
   public class Event
   {
       [Timestamp]
       public byte[] RowVersion { get; set; }
   }

   // EF Core will throw DbUpdateConcurrencyException if changed
   ```

4. **Comprehensive Domain Tests (P0)**
   ```csharp
   [Fact]
   public void AddVideo_WhenMaxReached_ReturnsFailure()
   {
       // Arrange: Event with 3 videos
       var evt = CreateEventWithVideos(3);

       // Act: Attempt to add 4th
       var result = evt.AddVideo(...);

       // Assert
       result.IsFailure.Should().BeTrue();
       result.Error.Should().Contain("Maximum");
       evt.Videos.Count.Should().Be(3); // Unchanged
   }
   ```

5. **Integration Tests with Database (P0)**
   ```csharp
   [Fact]
   public async Task AddVideo_ViolatingUniqueIndex_ThrowsException()
   {
       // Test that database constraints are enforced
   }
   ```

**Success Metrics:**
- ✅ Zero invariant violations in production
- ✅ 100% domain test coverage for invariants
- ✅ Database constraints match domain rules

**Monitoring:**
- Track `DbUpdateException` occurrences
- Alert on constraint violation errors

---

## 🟠 High Risks

### Risk 2: Blob Deletion Failures Leave Orphaned Files
**Category:** Technical
**Impact:** Medium | **Probability:** High | **Severity:** HIGH

**Description:**
When replacing images or deleting videos, the database update may succeed but blob deletion may fail (network issues, Azure outage).

**Consequences:**
- Orphaned blobs accumulate in storage
- Increased storage costs ($20/TB/month)
- Wasted storage capacity
- Difficult to identify which blobs are orphaned

**Mitigation Strategies:**

1. **Best-Effort Delete with Logging (P0)**
   ```csharp
   try
   {
       await _imageService.DeleteImageAsync(oldBlobName, cancellationToken);
   }
   catch (Exception ex)
   {
       _logger.LogWarning(ex,
           "Failed to delete blob {BlobName} for event {EventId} - orphaned blob",
           oldBlobName, eventId);

       // Store in orphaned_blobs table for cleanup
       await _context.OrphanedBlobs.AddAsync(new OrphanedBlob
       {
           BlobName = oldBlobName,
           Container = "event-images",
           EventId = eventId,
           DetectedAt = DateTime.UtcNow
       });

       // DON'T throw - allow operation to succeed
   }
   ```

2. **Background Cleanup Job (P1)**
   ```csharp
   [AutomaticRetry(Attempts = 3)]
   public class OrphanedBlobCleanupJob
   {
       public async Task CleanupOrphanedBlobsAsync()
       {
           var orphanedBlobs = await _context.OrphanedBlobs
               .Where(b => b.DetectedAt < DateTime.UtcNow.AddHours(-1))
               .ToListAsync();

           foreach (var blob in orphanedBlobs)
           {
               try
               {
                   await _blobService.DeleteIfExistsAsync(blob.BlobName);
                   _context.OrphanedBlobs.Remove(blob);
               }
               catch (Exception ex)
               {
                   _logger.LogError(ex, "Failed to cleanup orphaned blob {BlobName}",
                       blob.BlobName);
                   // Retry in next run
               }
           }

           await _context.SaveChangesAsync();
       }
   }

   // Schedule daily cleanup
   RecurringJob.AddOrUpdate<OrphanedBlobCleanupJob>(
       "cleanup-orphaned-blobs",
       job => job.CleanupOrphanedBlobsAsync(),
       Cron.Daily(2)); // 2 AM daily
   ```

3. **Blob Lifecycle Management (P1)**
   ```json
   // Azure Storage Account -> Lifecycle Management
   {
     "rules": [
       {
         "name": "delete-untracked-blobs",
         "enabled": true,
         "type": "Lifecycle",
         "definition": {
           "filters": {
             "blobTypes": ["blockBlob"]
           },
           "actions": {
             "baseBlob": {
               "delete": {
                 "daysAfterModificationGreaterThan": 90
               }
             }
           }
         }
       }
     ]
   }
   ```

4. **Monitoring and Alerts (P1)**
   ```csharp
   // Dashboard: Show orphaned blob count
   // Alert when orphaned blobs > 100
   // Weekly report of storage usage
   ```

**Success Metrics:**
- ✅ Orphaned blobs < 50 at any time
- ✅ All orphaned blobs cleaned within 24 hours
- ✅ Storage cost increase < 5% due to orphans

**Monitoring:**
- Daily count of orphaned blobs
- Storage growth rate
- Cleanup job success rate

---

### Risk 3: Video Processing Complexity Delays MVP
**Category:** Schedule
**Impact:** High | **Probability:** Medium | **Severity:** HIGH

**Description:**
Implementing video thumbnail generation, duration extraction, and format validation could be complex and time-consuming.

**Consequences:**
- Delayed feature delivery
- Increased development cost
- Missed deadlines
- Frustrated stakeholders

**Mitigation Strategies:**

1. **Phased Implementation (P0 - SELECTED)**
   ```
   Phase 1 (MVP):
   - ✅ Video upload (no processing)
   - ✅ Video delete
   - ✅ No thumbnails (return null)
   - ✅ No duration extraction
   - ✅ Default format: "mp4"
   - Timeline: 3-4 days

   Phase 2 (Enhancement):
   - ⏳ FFmpeg integration
   - ⏳ Thumbnail generation
   - ⏳ Duration extraction
   - ⏳ Format detection
   - Timeline: +2-3 days

   Phase 3 (Advanced):
   - 🔮 Video transcoding
   - 🔮 Multiple resolutions
   - 🔮 Adaptive streaming
   - Timeline: +1-2 weeks
   ```

2. **Stub IVideoProcessingService (P0)**
   ```csharp
   public class BasicVideoProcessingService : IVideoProcessingService
   {
       // Phase 1: Return defaults
       public Task<Stream?> GenerateThumbnailAsync(...) => Task.FromResult<Stream?>(null);
       public Task<TimeSpan?> GetDurationAsync(...) => Task.FromResult<TimeSpan?>(null);
       public Task<string> GetFormatAsync(...) => Task.FromResult("mp4");

       // Phase 2: Implement with FFmpeg
       // public async Task<Stream> GenerateThumbnailAsync(...)
       // {
       //     var ffmpeg = new Engine(@"C:\ffmpeg\bin");
       //     ...
       // }
   }
   ```

3. **Interface-Driven Design (P0)**
   - Define IVideoProcessingService first
   - Application layer depends on interface
   - Easy to swap implementations later

4. **External Service as Fallback (P2)**
   - If FFmpeg proves too complex, use Azure Media Services
   - Higher cost but faster implementation
   - Switch implementation via DI

**Success Metrics:**
- ✅ Phase 1 delivered within 3-4 days
- ✅ Core video upload/delete working without processing
- ✅ No blocking dependencies on FFmpeg

**Decision Point:**
- After Phase 1 deployment, evaluate user feedback
- If thumbnails not critical, defer Phase 2
- If high demand, prioritize Phase 2

---

### Risk 7: Database Migration Failures
**Category:** Deployment
**Impact:** High | **Probability:** Low | **Severity:** HIGH

**Description:**
EF Core migration to create `event_videos` table could fail in production, breaking deployments.

**Consequences:**
- Deployment rollback required
- Production downtime
- Data inconsistency
- User-facing errors

**Mitigation Strategies:**

1. **Test Migration in All Environments (P0)**
   ```bash
   # Development
   dotnet ef migrations add AddEventVideos
   dotnet ef database update

   # Staging
   dotnet ef database update --connection "$STAGING_CONNECTION_STRING"

   # Test rollback
   dotnet ef database update AddEventImages # Previous migration
   dotnet ef database update AddEventVideos # Re-apply
   ```

2. **Idempotent Migration Scripts (P0)**
   ```csharp
   protected override void Up(MigrationBuilder migrationBuilder)
   {
       // Check if table exists first
       migrationBuilder.Sql(@"
           IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'event_videos')
           BEGIN
               CREATE TABLE event_videos (
                   Id uniqueidentifier NOT NULL PRIMARY KEY,
                   -- ...
               );
           END
       ");
   }
   ```

3. **Backup Before Migration (P0)**
   ```bash
   # Automated in deployment pipeline
   az sql db export \
       --resource-group lankaconnect-rg \
       --server lankaconnect-sql \
       --name lankaconnect-db \
       --storage-key $STORAGE_KEY \
       --storage-uri "https://backups.blob.core.windows.net/$(date +%Y%m%d).bacpac"
   ```

4. **Rollback Plan (P0)**
   ```bash
   # If migration fails, rollback to previous version
   dotnet ef database update AddEventImages

   # Redeploy previous application version
   az webapp deployment slot swap \
       --name lankaconnect-api \
       --resource-group lankaconnect-rg \
       --slot staging
   ```

5. **Blue-Green Deployment (P1)**
   ```
   1. Deploy new version to staging slot (with migration)
   2. Run smoke tests on staging
   3. Swap staging <-> production
   4. Monitor for errors
   5. Rollback if needed (swap back)
   ```

**Success Metrics:**
- ✅ Migration succeeds in < 30 seconds
- ✅ Zero production downtime
- ✅ Rollback plan tested and documented

**Testing Checklist:**
- [ ] Migration tested on dev database
- [ ] Migration tested on staging database
- [ ] Rollback tested on staging
- [ ] Backup verified and restorable
- [ ] Deployment runbook documented

---

### Risk 10: Concurrent Edits Cause Data Loss
**Category:** Technical
**Impact:** High | **Probability:** Low | **Severity:** HIGH

**Description:**
Two users editing the same event simultaneously could cause lost updates (last write wins).

**Scenarios:**
1. User A replaces Image 1 while User B deletes Image 2
2. User A adds Video 1 while User B adds Video 2
3. User A reorders images while User B adds new image

**Consequences:**
- Lost user changes
- Frustration and complaints
- Data inconsistency
- Needs manual intervention

**Mitigation Strategies:**

1. **Optimistic Concurrency with RowVersion (P1)**
   ```csharp
   public class Event : AggregateRoot
   {
       [Timestamp]
       public byte[] RowVersion { get; private set; }
   }

   // EF Core automatically checks RowVersion on update
   try
   {
       await _context.SaveChangesAsync();
   }
   catch (DbUpdateConcurrencyException ex)
   {
       // Conflict detected
       _logger.LogWarning("Concurrent update detected for event {EventId}", eventId);

       // Option 1: Retry with fresh data
       var freshEvent = await _repository.GetByIdAsync(eventId);
       // Re-apply changes

       // Option 2: Return error to user
       return Result.Failure("Event was modified by another user. Please refresh.");
   }
   ```

2. **Granular Locking (P2 - Future)**
   ```csharp
   // Distributed lock on specific operations
   await using var redisLock = await _lockService.AcquireLockAsync(
       $"event:{eventId}:media",
       TimeSpan.FromSeconds(30));

   if (redisLock == null)
   {
       return Result.Failure("Event is being edited by another user");
   }

   // Perform operations
   ```

3. **UI-Level Optimistic Locking (P1)**
   ```javascript
   // Frontend sends RowVersion in request
   PUT /api/events/{id}/images/{imageId}
   Headers: { "If-Match": "rowVersionBase64" }

   // API validates RowVersion matches
   if (request.Headers["If-Match"] != event.RowVersionBase64)
   {
       return StatusCode(409, "Conflict - event modified");
   }
   ```

4. **Real-Time Collaboration (P2 - Future)**
   ```csharp
   // SignalR: Notify other users when event is edited
   await _hubContext.Clients
       .Group($"event-{eventId}")
       .SendAsync("EventMediaChanged", new { imageId, action = "replaced" });
   ```

**Success Metrics:**
- ✅ Zero lost updates in production
- ✅ Users notified of conflicts
- ✅ Clear resolution path for conflicts

**Monitoring:**
- Track `DbUpdateConcurrencyException` rate
- Alert if concurrency conflicts > 5/day

---

## 🟡 Medium Risks

### Risk 6: Image Replace Maintains Wrong DisplayOrder
**Category:** Functional
**Impact:** Medium | **Probability:** Medium | **Severity:** MEDIUM

**Description:**
If DisplayOrder is not preserved during image replace, gallery layout will change unexpectedly.

**Consequences:**
- User confusion (image in wrong position)
- Need to manually reorder
- Poor UX
- Support tickets

**Mitigation Strategies:**

1. **Explicit DisplayOrder Preservation (P0)**
   ```csharp
   public Result ReplaceImage(Guid imageId, string newUrl, string newBlobName)
   {
       var existingImage = _images.FirstOrDefault(i => i.Id == imageId);
       if (existingImage == null)
           return Result.Failure("Image not found");

       var displayOrder = existingImage.DisplayOrder; // ✅ Preserve

       _images.Remove(existingImage);

       var newImage = EventImage.Create(Id, newUrl, newBlobName, displayOrder);
       _images.Add(newImage);

       return Result.Success();
   }
   ```

2. **Unit Tests for DisplayOrder (P0)**
   ```csharp
   [Fact]
   public void ReplaceImage_MaintainsDisplayOrder()
   {
       // Arrange: Event with images at DisplayOrder 0,1,2
       var evt = CreateEventWithImages(3);
       var middleImageId = evt.Images.Single(i => i.DisplayOrder == 1).Id;

       // Act: Replace middle image
       evt.ReplaceImage(middleImageId, "new-url", "new-blob");

       // Assert
       evt.Images.Count.Should().Be(3);
       evt.Images.Single(i => i.ImageUrl == "new-url")
           .DisplayOrder.Should().Be(1); // ✅ Same position
   }
   ```

3. **Integration Test (P0)**
   ```csharp
   [Fact]
   public async Task ReplaceEventImage_PreservesDisplayOrder()
   {
       // End-to-end test via API
   }
   ```

**Success Metrics:**
- ✅ 100% of replace operations maintain DisplayOrder
- ✅ Zero user reports of incorrect ordering

---

### Risk 9: Inconsistent Error Messages
**Category:** UX
**Impact:** Medium | **Probability:** Medium | **Severity:** MEDIUM

**Description:**
Error messages could be inconsistent across different layers (domain, application, API).

**Examples:**
- Domain: "Image not found"
- API: "404 Not Found"
- Frontend: "Error uploading image"

**Consequences:**
- Confusing user experience
- Difficult debugging
- Poor error tracking

**Mitigation Strategies:**

1. **Standardized Error Result (P0)**
   ```csharp
   public class Result<T>
   {
       public bool IsSuccess { get; }
       public bool IsFailure => !IsSuccess;
       public T Value { get; }
       public string ErrorCode { get; } // NEW: Structured error code
       public string ErrorMessage { get; }

       public static Result<T> Failure(string errorCode, string message)
       {
           return new Result<T>
           {
               IsSuccess = false,
               ErrorCode = errorCode,
               ErrorMessage = message
           };
       }
   }

   // Usage
   return Result.Failure("EVENT_IMAGE_NOT_FOUND", "Image with ID {imageId} not found");
   ```

2. **Error Code Catalog (P1)**
   ```csharp
   public static class EventErrors
   {
       public const string ImageNotFound = "EVENT_IMAGE_NOT_FOUND";
       public const string MaxImagesReached = "EVENT_MAX_IMAGES";
       public const string VideoNotFound = "EVENT_VIDEO_NOT_FOUND";
       public const string MaxVideosReached = "EVENT_MAX_VIDEOS";
       public const string StorageQuotaExceeded = "EVENT_STORAGE_QUOTA";
   }
   ```

3. **API Error Mapping (P0)**
   ```csharp
   [HttpPut("{eventId}/images/{imageId}")]
   public async Task<IActionResult> ReplaceEventImage(...)
   {
       var result = await _mediator.Send(command);

       if (result.IsFailure)
       {
           return result.ErrorCode switch
           {
               EventErrors.EventNotFound => NotFound(new { error = result.ErrorMessage }),
               EventErrors.ImageNotFound => NotFound(new { error = result.ErrorMessage }),
               EventErrors.InvalidFile => BadRequest(new { error = result.ErrorMessage }),
               _ => StatusCode(500, new { error = "Internal server error" })
           };
       }

       return Ok(result.Value);
   }
   ```

4. **Logging with Context (P0)**
   ```csharp
   _logger.LogError("Failed to replace image. EventId={EventId}, ImageId={ImageId}, ErrorCode={ErrorCode}",
       eventId, imageId, result.ErrorCode);
   ```

**Success Metrics:**
- ✅ All errors have structured error codes
- ✅ Consistent error format across all endpoints
- ✅ Easy to search logs by error code

---

### Risk 11: Insufficient Test Coverage
**Category:** Quality
**Impact:** Medium | **Probability:** Medium | **Severity:** MEDIUM

**Description:**
Without comprehensive tests, bugs could slip into production, especially edge cases.

**Consequences:**
- Production bugs
- Difficult debugging
- Costly hotfixes
- Loss of confidence in codebase

**Mitigation Strategies:**

1. **TDD Workflow (P0)**
   ```
   1. Write test (RED)
   2. Implement feature (GREEN)
   3. Refactor (REFACTOR)
   4. Repeat
   ```

2. **Test Coverage Requirements (P0)**
   ```xml
   <!-- Directory.Build.props -->
   <PropertyGroup>
       <TargetCoveragePercentage>90</TargetCoveragePercentage>
       <FailBuildOnCoverageBelow>85</FailBuildOnCoverageBelow>
   </PropertyGroup>
   ```

3. **Test Pyramid (P0)**
   ```
   E2E Tests:      ~10 scenarios (Manual via Swagger)
   Integration:    ~30 tests (API + DB + Azurite)
   Unit Tests:     ~100 tests (Domain + Application)
   ```

4. **Required Test Scenarios (P0)**
   - ✅ Happy path (success cases)
   - ✅ Validation failures (bad input)
   - ✅ Not found errors
   - ✅ Authorization failures
   - ✅ Concurrency conflicts
   - ✅ Storage failures
   - ✅ Domain invariant violations

5. **Mutation Testing (P2 - Future)**
   ```bash
   dotnet tool install -g dotnet-stryker
   dotnet stryker
   ```

**Success Metrics:**
- ✅ 90%+ code coverage
- ✅ All domain invariants have tests
- ✅ All error paths covered

---

### Risk 12: Slow Image Replace Degraded UX
**Category:** Performance
**Impact:** Medium | **Probability:** Medium | **Severity:** MEDIUM

**Description:**
Image replace operation could take too long (> 5s), degrading user experience.

**Causes:**
- Large file uploads (5 MB)
- Slow Azure Blob Storage
- Network latency
- Database queries

**Mitigation Strategies:**

1. **Performance Targets (P0)**
   ```
   - Image replace < 2s (95th percentile)
   - Video upload < 10s for 50 MB
   ```

2. **Async Upload with Progress (P1)**
   ```javascript
   // Frontend: Show progress bar
   const xhr = new XMLHttpRequest();
   xhr.upload.addEventListener('progress', (e) => {
       const percentComplete = (e.loaded / e.total) * 100;
       updateProgressBar(percentComplete);
   });
   ```

3. **Optimize Blob Upload (P0)**
   ```csharp
   var blobUploadOptions = new BlobUploadOptions
   {
       TransferOptions = new StorageTransferOptions
       {
           MaximumConcurrency = 8, // Parallel upload
           InitialTransferSize = 4 * 1024 * 1024, // 4 MB chunks
           MaximumTransferSize = 4 * 1024 * 1024
       }
   };

   await blobClient.UploadAsync(stream, blobUploadOptions);
   ```

4. **Database Query Optimization (P1)**
   ```csharp
   // Include images in single query
   var eventEntity = await _context.Events
       .Include(e => e.Images)
       .AsSplitQuery() // Avoid cartesian explosion
       .FirstOrDefaultAsync(e => e.Id == eventId);
   ```

5. **Performance Monitoring (P0)**
   ```csharp
   using var activity = _activitySource.StartActivity("ReplaceEventImage");
   activity?.SetTag("event.id", eventId);
   activity?.SetTag("image.size", request.Image.Length);

   // Azure Application Insights tracks duration automatically
   ```

**Success Metrics:**
- ✅ 95% of operations < 2s
- ✅ Zero timeouts
- ✅ User receives progress feedback

**Monitoring:**
- Azure App Insights: Track operation duration
- Alert when p95 > 3s

---

## 🟢 Low Risks

### Risk 13: Video Format Compatibility Issues
**Category:** Technical
**Impact:** Low | **Probability:** Low | **Severity:** LOW

**Description:**
Some browsers may not support certain video formats (WebM on Safari).

**Mitigation:**
- Recommend MP4 (universal support)
- Document supported formats in API
- Return clear error for unsupported formats

### Risk 14: Blob Naming Collisions
**Category:** Technical
**Impact:** Low | **Probability:** Low | **Severity:** LOW

**Description:**
Two uploads at the same millisecond could theoretically have same blob name.

**Mitigation:**
- Include Guid in blob name: `{eventId}/{Guid.NewGuid()}_{timestamp}.ext`
- Guid ensures uniqueness

### Risk 15: Missing Authorization on Endpoints
**Category:** Security
**Impact:** Low | **Probability:** Low | **Severity:** LOW

**Description:**
Forgetting `[Authorize]` attribute on new endpoints.

**Mitigation:**
- Code review checklist includes authorization check
- Integration tests verify 401 Unauthorized responses
- Consider default authorization policy

---

## Risk Response Plan

### Pre-Implementation Phase
- [ ] Review this risk document with team
- [ ] Identify risk owners
- [ ] Set up monitoring infrastructure
- [ ] Document rollback procedures
- [ ] Create incident response plan

### During Implementation
- [ ] Daily standup: Discuss any new risks
- [ ] Update risk register as needed
- [ ] Test mitigation strategies
- [ ] Track risk metrics in dashboard

### Post-Deployment
- [ ] Monitor critical risks daily
- [ ] Weekly review of metrics
- [ ] Monthly risk reassessment
- [ ] Document lessons learned

---

## Incident Response Plan

### Severity 1: Critical Production Issue
**Examples:**
- All video uploads failing
- Data loss occurring
- Security breach

**Response:**
1. **Immediate (0-15 min)**
   - Alert on-call engineer via PagerDuty
   - Create incident channel in Slack
   - Assess impact (how many users affected?)

2. **Mitigation (15-60 min)**
   - Rollback to previous version if needed
   - Disable problematic feature via feature flag
   - Communicate to users (status page)

3. **Resolution (1-4 hours)**
   - Identify root cause
   - Deploy hotfix
   - Verify fix in production
   - Re-enable feature

4. **Post-Mortem (24-48 hours)**
   - Document incident
   - Identify preventive measures
   - Update runbooks

### Severity 2: Degraded Performance
**Examples:**
- Video uploads taking > 30s
- High error rate (5-10%)

**Response:**
1. **Monitor (0-30 min)**
   - Check metrics dashboard
   - Review error logs
   - Assess trend (getting worse?)

2. **Investigate (30 min - 2 hours)**
   - Identify bottleneck
   - Check Azure service health
   - Review recent deployments

3. **Mitigate (2-4 hours)**
   - Scale up resources if needed
   - Apply configuration changes
   - Communicate to affected users

### Severity 3: Minor Issue
**Examples:**
- Orphaned blob count high
- Single user reporting issue

**Response:**
1. **Log and Track**
   - Create ticket in issue tracker
   - Schedule for next sprint

2. **Fix in Next Release**
   - Prioritize based on impact
   - Test thoroughly
   - Deploy with next release

---

## Risk Review Schedule

### Daily
- Monitor critical risk metrics
- Review error logs
- Check storage costs

### Weekly
- Team meeting: Discuss new risks
- Review mitigation progress
- Update risk register

### Monthly
- Reassess risk probability/impact
- Archive resolved risks
- Identify emerging risks

### Quarterly
- Comprehensive risk audit
- Update mitigation strategies
- Present to stakeholders

---

## Conclusion

This risk assessment identifies 15 potential risks across multiple categories:
- **Critical Risks (5):** Require immediate mitigation before implementation
- **High Risks (5):** Plan mitigation strategies during development
- **Medium Risks (3):** Monitor and address if they occur
- **Low Risks (2):** Accept with minimal mitigation

**Key Takeaways:**
1. ✅ Phased implementation reduces complexity risk
2. ✅ Compensating transactions handle storage failures gracefully
3. ✅ TDD workflow ensures high quality and test coverage
4. ✅ Monitoring and alerts catch issues early
5. ✅ Clear rollback plans minimize production impact

**Next Steps:**
1. Review risks with team and stakeholders
2. Assign risk owners
3. Implement P0 (must-have) mitigations
4. Set up monitoring and alerts
5. Document incident response procedures

**Risk Appetite:**
- **Accept:** Low risks with minimal mitigation
- **Mitigate:** High/Medium risks with planned strategies
- **Avoid:** None (all features provide business value)
- **Transfer:** None (no third-party insurance needed)

By proactively identifying and mitigating these risks, we can deliver the Image Replace and Video Support features with confidence, minimal surprises, and high quality.
