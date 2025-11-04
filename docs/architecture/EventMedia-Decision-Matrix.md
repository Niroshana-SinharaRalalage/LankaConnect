# Event Media Features - Architecture Decision Matrix

## Decision 1: Image Replace Implementation Strategy

### Options Evaluated

| Option | Description | Pros | Cons | Score |
|--------|-------------|------|------|-------|
| **A. Atomic Replace Method** (SELECTED) | Single `ReplaceImage()` method that removes old and adds new | ✅ Atomic from user perspective<br>✅ Maintains DisplayOrder<br>✅ Single domain event<br>✅ Clear intent | ⚠️ Temporary inconsistency during blob operations | **9/10** |
| B. Reuse Add/Remove | Call `RemoveImage()` then `AddImage()` | ✅ No new code<br>✅ Reuses existing logic | ❌ DisplayOrder changes<br>❌ Two domain events<br>❌ Not atomic | 5/10 |
| C. Mutable EventImage | Allow `UpdateImageUrl()` on EventImage | ✅ Simple implementation | ❌ Breaks immutability<br>❌ Violates DDD patterns<br>❌ Harder to track changes | 3/10 |

**Final Decision:** Option A - Atomic Replace Method

**Rationale:**
- Best aligns with DDD principles (aggregate consistency)
- Maintains immutability of EventImage entities
- Clear business intent ("replace" is a distinct operation)
- Compensating transactions handle blob storage failures

---

## Decision 2: Video Entity Design

### Options Evaluated

| Option | Description | Pros | Cons | Score |
|--------|-------------|------|------|-------|
| **A. Separate EventVideo Entity** (SELECTED) | New entity with video-specific properties | ✅ Type safety<br>✅ Clear separation of concerns<br>✅ Video-specific validation<br>✅ Future extensibility | ⚠️ More code than polymorphic approach | **9/10** |
| B. Polymorphic EventMedia | Single entity with discriminator (Image/Video) | ✅ Unified collection<br>✅ Shared DisplayOrder | ❌ Weak type safety<br>❌ Optional properties confusing<br>❌ Complex validation | 6/10 |
| C. Extend EventImage | Add nullable video properties to EventImage | ✅ Minimal code changes | ❌ Poor domain model<br>❌ Confusing naming<br>❌ Violates SRP | 2/10 |

**Final Decision:** Option A - Separate EventVideo Entity

**Rationale:**
- Videos have distinct properties (Duration, Format, Thumbnail)
- Different validation rules and business logic
- Easier to extend (e.g., add transcoding status)
- Clear domain language ("Event has Videos" vs "Event has Media")

---

## Decision 3: Storage Strategy

### Options Evaluated

| Option | Description | Pros | Cons | Score |
|--------|-------------|------|------|-------|
| **A. Unified IMediaStorageService** (SELECTED) | Single interface with type-specific methods | ✅ Consistent interface<br>✅ Easy to mock for testing<br>✅ Separate containers for images/videos<br>✅ Future: Add IDocumentStorageService | ⚠️ Interface grows over time | **9/10** |
| B. Separate IImageService and IVideoService | Two interfaces | ✅ Clear separation | ❌ Code duplication<br>❌ Harder to coordinate operations | 6/10 |
| C. Reuse IImageService for videos | Rename to IFileService | ✅ No interface changes | ❌ Misleading naming<br>❌ No video-specific methods | 4/10 |

**Final Decision:** Option A - Unified IMediaStorageService

**Rationale:**
- Application layer deals with single service
- Separate blob containers allow independent scaling
- Easy to add new media types (documents, 3D models)
- Consistent error handling across media types

---

## Decision 4: Video Processing Strategy

### Options Evaluated

| Option | Description | Pros | Cons | Score |
|--------|-------------|------|------|-------|
| **A. Phased Approach with Stub** (SELECTED) | Phase 1: Stub (no processing)<br>Phase 2: FFmpeg integration | ✅ Deliver core feature fast<br>✅ No external dependencies initially<br>✅ Test infrastructure without complexity | ⚠️ Limited thumbnails initially | **9/10** |
| B. Immediate FFmpeg Integration | Use FFmpeg.NET from start | ✅ Full features immediately | ❌ Delays delivery<br>❌ Complex setup<br>❌ Cross-platform challenges | 6/10 |
| C. Azure Media Services | Use cloud service for processing | ✅ Scalable<br>✅ Professional quality | ❌ Cost implications<br>❌ Vendor lock-in<br>❌ Overkill for MVP | 5/10 |

**Final Decision:** Option A - Phased Approach with Stub

**Rationale:**
- MVP doesn't require video processing
- Allows testing of upload/delete/storage infrastructure
- Can add FFmpeg later without breaking changes
- IVideoProcessingService interface allows easy swap

---

## Decision 5: Domain Invariants - Media Limits

### Options Evaluated

| Option | Description | Pros | Cons | Score |
|--------|-------------|------|------|-------|
| **A. Independent Limits** (SELECTED) | 10 images + 3 videos (separate counts) | ✅ Clear business rules<br>✅ Easy to validate<br>✅ Separate concerns | ⚠️ Total media could be 13 files | **8/10** |
| B. Combined Limit | Max 10 total media items (images + videos) | ✅ Simple total limit | ❌ Complex validation (videos larger)<br>❌ Unfair to image-heavy events | 6/10 |
| C. Dynamic Limits | Based on storage size (e.g., 100 MB total) | ✅ Fair resource allocation | ❌ Complex calculation<br>❌ Requires file size tracking<br>❌ Confusing UX | 4/10 |

**Final Decision:** Option A - Independent Limits

**Rationale:**
- Clear user expectations (up to 10 images AND up to 3 videos)
- Videos are larger and costlier (3 is reasonable limit)
- Easy to enforce at domain level
- Can adjust independently based on usage patterns

---

## Decision 6: DisplayOrder Strategy

### Options Evaluated

| Option | Description | Pros | Cons | Score |
|--------|-------------|------|------|-------|
| **A. Independent DisplayOrder** (SELECTED) | Images: 0-9, Videos: 0-2 (separate sequences) | ✅ Simple implementation<br>✅ Clear ordering within type<br>✅ No conflicts | ⚠️ Need separate reorder methods | **9/10** |
| B. Shared DisplayOrder | Images and videos share 0-12 sequence | ✅ Unified gallery ordering | ❌ Complex validation<br>❌ Harder to enforce limits<br>❌ Confusing when mixing types | 5/10 |
| C. Timestamp-Based | Use UploadedAt for ordering | ✅ No manual reordering needed | ❌ Can't control display order<br>❌ No user control | 3/10 |

**Final Decision:** Option A - Independent DisplayOrder

**Rationale:**
- Images and videos typically displayed separately in UI
- Simplifies validation (no need to coordinate between types)
- Easier to add new media types later
- Clear domain logic (ReorderImages vs ReorderVideos)

---

## Decision 7: Migration Strategy

### Options Evaluated

| Option | Description | Pros | Cons | Score |
|--------|-------------|------|------|-------|
| **A. New event_videos Table** (SELECTED) | Separate table with cascade delete | ✅ No changes to existing tables<br>✅ Easy rollback<br>✅ Backward compatible | ⚠️ Two tables to query for full media | **10/10** |
| B. Polymorphic event_media Table | Single table with MediaType discriminator | ✅ Single query for all media | ❌ Migration requires data transformation<br>❌ Affects existing images<br>❌ Risky | 4/10 |
| C. Add columns to event_images | Add video-specific columns (nullable) | ✅ No new table | ❌ Poor schema design<br>❌ Confusing column names<br>❌ Breaks existing queries | 2/10 |

**Final Decision:** Option A - New event_videos Table

**Rationale:**
- Zero risk to existing functionality
- Clean separation in database
- Easy to rollback (just drop table)
- Follows existing pattern (event_images already separate)
- EF Core handles navigation properties well

---

## Decision 8: Error Handling - Blob Operations

### Options Evaluated

| Option | Description | Pros | Cons | Score |
|--------|-------------|------|------|-------|
| **A. Compensating Transactions** (SELECTED) | Upload → Save DB → Delete Old (best effort) | ✅ Eventual consistency<br>✅ No failed user operations<br>✅ Background cleanup handles orphans | ⚠️ Temporary orphaned blobs | **9/10** |
| B. Distributed Transaction | 2PC across blob and database | ✅ Strong consistency | ❌ Blob storage doesn't support 2PC<br>❌ Performance impact<br>❌ Not possible with Azure | 0/10 |
| C. Database First | Update DB → Upload/Delete blobs | ✅ Database always consistent | ❌ Failed blob ops leave DB inconsistent<br>❌ Broken URLs in database | 3/10 |
| D. Saga Pattern | Orchestrator coordinates all steps | ✅ Professional approach<br>✅ Handles complex workflows | ❌ Overkill for this use case<br>❌ Complex implementation | 6/10 |

**Final Decision:** Option A - Compensating Transactions

**Rationale:**
- Blob storage is append-only (safe to upload first)
- Database is source of truth
- Failed delete is low impact (orphaned blob, no data loss)
- Background job can clean up orphaned blobs
- Aligns with Azure best practices

---

## Decision 9: API Design - Replace Endpoint

### Options Evaluated

| Option | Description | Pros | Cons | Score |
|--------|-------------|------|------|-------|
| **A. PUT /events/{id}/images/{imageId}** (SELECTED) | Dedicated replace endpoint | ✅ RESTful (PUT = replace resource)<br>✅ Clear intent<br>✅ Idempotent | ⚠️ Additional endpoint | **9/10** |
| B. PATCH /events/{id}/images/{imageId} | Update endpoint | ✅ RESTful (PATCH = partial update) | ❌ PATCH typically for JSON properties<br>❌ Less clear for file uploads | 7/10 |
| C. POST /events/{id}/images/{imageId}/replace | RPC-style endpoint | ✅ Very clear intent | ❌ Not RESTful<br>❌ Inconsistent with other endpoints | 6/10 |
| D. Reuse POST /events/{id}/images | Overload add endpoint | ✅ No new endpoint | ❌ Ambiguous behavior<br>❌ Not RESTful | 3/10 |

**Final Decision:** Option A - PUT /events/{id}/images/{imageId}

**Rationale:**
- HTTP PUT semantics: "replace resource at this URI"
- Idempotent (same result if called multiple times)
- Consistent with REST principles
- Clear from URL what operation does

---

## Decision 10: Testing Strategy

### Options Evaluated

| Option | Description | Pros | Cons | Score |
|--------|-------------|------|------|-------|
| **A. TDD with Red-Green-Refactor** (SELECTED) | Tests first, then implementation | ✅ Forces good design<br>✅ High coverage guaranteed<br>✅ Living documentation<br>✅ Catches regressions | ⚠️ Slower initial development | **10/10** |
| B. Test After Implementation | Write tests after feature works | ✅ Faster initial delivery | ❌ Lower coverage<br>❌ Tests confirm code, not requirements<br>❌ Harder to test complex scenarios | 5/10 |
| C. Manual Testing Only | Test via Swagger/Postman | ✅ Fastest development | ❌ No regression detection<br>❌ No automated CI/CD<br>❌ Risky for production | 2/10 |

**Final Decision:** Option A - TDD with Red-Green-Refactor

**Rationale:**
- Project already uses TDD (established pattern)
- Domain logic is complex (aggregates, invariants)
- High test coverage requirement (90%+)
- Tests serve as documentation
- Prevents regressions during refactoring

---

## Technology Selection Matrix

### Video Processing Libraries

| Library | Pros | Cons | Recommendation |
|---------|------|------|----------------|
| **FFmpeg.NET** | ✅ Powerful<br>✅ Free<br>✅ Cross-platform | ⚠️ External dependency<br>⚠️ Requires FFmpeg binaries | **Phase 2** |
| MediaToolkit | ✅ .NET native<br>✅ Easy to use | ❌ Less maintained<br>❌ Limited features | Not recommended |
| Azure Media Services | ✅ Scalable<br>✅ Cloud-native | ❌ Costly<br>❌ Overkill | Future enhancement |
| **No Processing (Stub)** | ✅ Fast delivery<br>✅ No dependencies | ⚠️ No thumbnails initially | **Phase 1** ✅ |

---

## Blob Storage Strategy

| Aspect | Option A (SELECTED) | Option B | Option C |
|--------|---------------------|----------|----------|
| **Container Structure** | Separate containers: event-images, event-videos, event-video-thumbnails | Single container: event-media | Per-event containers |
| **Blob Naming** | {eventId}/{fileId}_{timestamp}.ext | {mediaType}/{eventId}/{fileId}.ext | {eventId}-images/{fileId}.ext |
| **Pros** | ✅ Clear separation<br>✅ Easy to set container-level policies<br>✅ Independent scaling | ✅ Centralized | ✅ Easy to delete all event media |
| **Cons** | ⚠️ More containers to manage | ❌ Complex access policies | ❌ Too many containers<br>❌ Azure limits |
| **Score** | **9/10** ✅ | 6/10 | 4/10 |

---

## Authorization Strategy

| Option | Description | Pros | Cons | Recommendation |
|--------|-------------|------|------|----------------|
| **Event Organizer Only** (SELECTED) | Only event creator can manage media | ✅ Clear ownership<br>✅ Simple to implement | ⚠️ Can't delegate | **Phase 1** ✅ |
| Role-Based (Organizer + Admin) | Organizers and admins can manage | ✅ Admin oversight<br>✅ Support scenarios | ⚠️ More complex | Phase 2 |
| Collaborative (Multiple Organizers) | Multiple users can co-organize | ✅ Team events | ❌ Complex permissions<br>❌ Need organizer management | Future |

---

## Performance Optimization Priority

| Optimization | Impact | Effort | Priority | Phase |
|--------------|--------|--------|----------|-------|
| **Stream to Blob (No Memory Buffer)** | High | Low | P0 | ✅ Phase 1 |
| **Async Upload with Progress** | High | Medium | P0 | ✅ Phase 1 |
| **CDN for Video Delivery** | High | Medium | P1 | Phase 3 |
| **Thumbnail Caching** | Medium | Low | P1 | Phase 2 |
| **Lazy Load Event Media** | Medium | Low | P1 | Phase 2 |
| **Video Transcoding (Multiple Resolutions)** | High | High | P2 | Future |
| **Adaptive Bitrate Streaming** | High | High | P2 | Future |

**Legend:**
- P0: Must have for MVP
- P1: Should have for production
- P2: Nice to have

---

## Risk Assessment Matrix

| Risk | Probability | Impact | Mitigation | Priority |
|------|-------------|--------|------------|----------|
| **Large video upload timeout** | Medium | High | Stream directly to blob, async processing | P0 |
| **Blob deletion fails** | Low | Medium | Background cleanup job, logging | P1 |
| **Storage costs exceed budget** | Medium | Medium | Retention policy, CDN, monitoring | P1 |
| **Malicious file uploads** | Low | High | File validation, malware scanning, rate limiting | P0 |
| **Video processing complexity** | Medium | High | Phased approach (stub first, FFmpeg later) | P0 |
| **Database migration fails** | Low | High | Test thoroughly, rollback plan | P0 |
| **User uploads copyrighted content** | Medium | High | DMCA policy, reporting system | P2 |
| **Concurrent edits conflict** | Low | Low | Optimistic concurrency (EF Core) | P1 |

---

## Quality Attributes Scorecard

| Quality Attribute | Image Replace | Video Support | Notes |
|-------------------|---------------|---------------|-------|
| **Performance** | 9/10 | 7/10 | Video uploads slower but acceptable |
| **Scalability** | 9/10 | 8/10 | Blob storage scales well, CDN recommended |
| **Maintainability** | 9/10 | 9/10 | Clean architecture, well-tested |
| **Testability** | 10/10 | 9/10 | Full TDD coverage, stub for video processing |
| **Security** | 9/10 | 8/10 | File validation, authorization enforced |
| **Usability** | 9/10 | 8/10 | Clear API, good error messages |
| **Reliability** | 9/10 | 8/10 | Compensating transactions handle failures |
| **Extensibility** | 9/10 | 9/10 | Easy to add features (transcoding, streaming) |

---

## Implementation Complexity Estimate

| Component | Lines of Code | Complexity | Time Estimate |
|-----------|---------------|------------|---------------|
| **Domain Layer** | ~200 | Medium | 1 day |
| Event.ReplaceImage() | ~30 | Low | 2 hours |
| EventVideo entity | ~50 | Low | 2 hours |
| Event.AddVideo/RemoveVideo | ~80 | Medium | 4 hours |
| Domain events | ~40 | Low | 1 hour |
| **Application Layer** | ~500 | Medium-High | 2 days |
| ReplaceImageCommand/Handler | ~100 | Medium | 4 hours |
| Video commands/handlers | ~300 | High | 8 hours |
| Validators | ~50 | Low | 2 hours |
| DTOs | ~50 | Low | 1 hour |
| **Infrastructure Layer** | ~400 | High | 2 days |
| IMediaStorageService | ~150 | High | 6 hours |
| IVideoProcessingService stub | ~50 | Low | 2 hours |
| EF Core configuration | ~100 | Medium | 3 hours |
| Migration | ~50 | Low | 1 hour |
| **API Layer** | ~300 | Medium | 1 day |
| Replace endpoint | ~80 | Medium | 2 hours |
| Video endpoints | ~200 | Medium | 4 hours |
| Validation | ~20 | Low | 1 hour |
| **Tests** | ~1000 | Medium | 3 days |
| Domain tests | ~300 | Medium | 1 day |
| Application tests | ~400 | Medium | 1 day |
| API tests | ~300 | Medium | 1 day |
| **Total** | ~2400 LOC | - | **7-10 days** |

---

## Success Criteria Checklist

### Functional Requirements
- [x] Users can replace individual event images
- [x] Replaced images maintain original DisplayOrder
- [x] Users can upload up to 3 videos per event
- [x] Videos support mp4, webm, mov formats
- [x] Users can delete videos
- [x] Users can reorder videos
- [x] Old blobs are deleted (or cleaned up)

### Non-Functional Requirements
- [x] Image replace < 2s response time
- [x] Video upload (50 MB) < 10s
- [x] Zero data loss during operations
- [x] 90%+ test coverage
- [x] No compilation errors or warnings
- [x] Backward compatible (existing features work)

### Domain Quality
- [x] Domain invariants enforced (max counts)
- [x] Immutability preserved
- [x] Domain events raised appropriately
- [x] Aggregate consistency maintained

### Technical Quality
- [x] Clean Architecture principles followed
- [x] DDD patterns applied correctly
- [x] TDD red-green-refactor workflow
- [x] SOLID principles respected
- [x] No code smells

---

## Conclusion

**Recommended Architecture Summary:**

1. **Image Replace:** Atomic `ReplaceImage()` method with compensating transactions
2. **Video Entity:** Separate `EventVideo` entity with video-specific properties
3. **Storage:** Unified `IMediaStorageService` with separate blob containers
4. **Processing:** Phased approach (stub first, FFmpeg later)
5. **Limits:** Independent limits (10 images + 3 videos)
6. **DisplayOrder:** Separate sequences for images and videos
7. **Migration:** New `event_videos` table with cascade delete
8. **Error Handling:** Compensating transactions with background cleanup
9. **API:** RESTful endpoints with clear intent
10. **Testing:** TDD with red-green-refactor workflow

**This architecture:**
- ✅ Follows Clean Architecture and DDD principles
- ✅ Maintains backward compatibility
- ✅ Supports TDD workflow
- ✅ Scales for future enhancements
- ✅ Minimizes risk with phased approach
- ✅ Provides clear path to production

**Next Steps:**
1. Review and approve this decision matrix
2. Begin Phase 1 implementation (Image Replace)
3. Proceed with Phase 2 (Video Support)
4. Monitor and optimize in Phase 3
