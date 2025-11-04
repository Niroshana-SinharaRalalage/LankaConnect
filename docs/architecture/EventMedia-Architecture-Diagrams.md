# Event Media Architecture - Visual Diagrams

## 1. System Context Diagram (C4 Level 1)

```mermaid
graph TB
    User[Event Organizer]
    Admin[Admin User]
    System[LankaConnect Event Management System]
    BlobStorage[(Azure Blob Storage)]
    Database[(SQL Database)]

    User -->|Manage event media| System
    Admin -->|Monitor events| System
    System -->|Store images/videos| BlobStorage
    System -->|Persist metadata| Database

    style System fill:#2563eb,color:#fff
    style BlobStorage fill:#0078d4,color:#fff
    style Database fill:#10b981,color:#fff
```

---

## 2. Container Diagram (C4 Level 2)

```mermaid
graph TB
    subgraph "Client Applications"
        WebApp[Web Application]
        MobileApp[Mobile App]
    end

    subgraph "LankaConnect System"
        API[Web API<br/>ASP.NET Core]
        AppLayer[Application Layer<br/>CQRS + MediatR]
        DomainLayer[Domain Layer<br/>Aggregates + Entities]
        InfraLayer[Infrastructure Layer<br/>EF Core + Blob Storage]
    end

    subgraph "External Systems"
        AzureBlob[(Azure Blob Storage<br/>Images + Videos)]
        Database[(SQL Server<br/>Event Metadata)]
    end

    WebApp -->|HTTPS/JSON| API
    MobileApp -->|HTTPS/JSON| API
    API -->|Send Commands/Queries| AppLayer
    AppLayer -->|Call Domain Methods| DomainLayer
    AppLayer -->|Persist via Repository| InfraLayer
    InfraLayer -->|EF Core| Database
    InfraLayer -->|Azure SDK| AzureBlob

    style API fill:#2563eb,color:#fff
    style AppLayer fill:#8b5cf6,color:#fff
    style DomainLayer fill:#ec4899,color:#fff
    style InfraLayer fill:#10b981,color:#fff
```

---

## 3. Component Diagram - Image Replace Flow (C4 Level 3)

```mermaid
sequenceDiagram
    actor User
    participant API as EventsController
    participant Handler as ReplaceImageCommandHandler
    participant Event as Event Aggregate
    participant Media as IMediaStorageService
    participant Repo as IEventRepository
    participant DB as Database
    participant Blob as Azure Blob Storage

    User->>API: PUT /events/{id}/images/{imageId}
    API->>API: Validate file (size, format)
    API->>API: Check authorization
    API->>Handler: Send ReplaceImageCommand

    Handler->>Repo: GetByIdAsync(eventId)
    Repo->>DB: Query event + images
    DB-->>Repo: Event aggregate
    Repo-->>Handler: Event with images

    Handler->>Media: UploadImageAsync(newImage)
    Media->>Blob: Upload to event-images container
    Blob-->>Media: New blob URL + name
    Media-->>Handler: (newUrl, newBlobName)

    Handler->>Event: ReplaceImage(imageId, newUrl, newBlobName)
    Event->>Event: Find existing image
    Event->>Event: Remove old EventImage
    Event->>Event: Add new EventImage (same DisplayOrder)
    Event->>Event: Raise EventImageReplacedDomainEvent
    Event-->>Handler: Result.Success()

    Handler->>Repo: SaveChangesAsync()
    Repo->>DB: Update event_images table
    DB-->>Repo: Success

    Handler->>Media: DeleteImageAsync(oldBlobName)
    Note over Handler,Media: Best effort - log if fails
    Media->>Blob: Delete old blob

    Handler-->>API: Result<EventImageDto>
    API-->>User: 200 OK with new image data
```

---

## 4. Component Diagram - Video Upload Flow (C4 Level 3)

```mermaid
sequenceDiagram
    actor User
    participant API as EventsController
    participant Handler as AddVideoCommandHandler
    participant Event as Event Aggregate
    participant VideoProc as IVideoProcessingService
    participant Media as IMediaStorageService
    participant Repo as IEventRepository
    participant DB as Database
    participant Blob as Azure Blob Storage

    User->>API: POST /events/{id}/videos
    API->>API: Validate file (size, format, max count)
    API->>Handler: Send AddVideoCommand

    Handler->>Repo: GetByIdAsync(eventId)
    Repo-->>Handler: Event aggregate

    Handler->>VideoProc: GetFormatAsync(videoStream)
    VideoProc-->>Handler: "mp4"

    Handler->>VideoProc: GetDurationAsync(videoStream)
    VideoProc-->>Handler: TimeSpan or null

    Handler->>Media: UploadVideoAsync(videoStream)
    Media->>Blob: Upload to event-videos container
    Blob-->>Media: Video URL + blob name

    opt Thumbnail Generation
        Media->>VideoProc: GenerateThumbnailAsync(videoStream)
        VideoProc-->>Media: Thumbnail stream or null
        Media->>Blob: Upload to event-video-thumbnails
        Blob-->>Media: Thumbnail URL + blob name
    end

    Media-->>Handler: (videoUrl, videoBlobName, thumbUrl, thumbBlobName)

    Handler->>Event: AddVideo(videoUrl, blobName, ...)
    Event->>Event: Validate max count (3)
    Event->>Event: Calculate DisplayOrder
    Event->>Event: Create EventVideo entity
    Event->>Event: Add to _videos collection
    Event->>Event: Raise EventVideoAddedDomainEvent
    Event-->>Handler: Result.Success()

    Handler->>Repo: SaveChangesAsync()
    Repo->>DB: Insert into event_videos table
    DB-->>Repo: Success

    Handler-->>API: Result<EventVideoDto>
    API-->>User: 201 Created with video data
```

---

## 5. Domain Model Diagram

```mermaid
classDiagram
    class Event {
        <<AggregateRoot>>
        +Guid Id
        +string Title
        +DateTime EventDate
        -List~EventImage~ _images
        -List~EventVideo~ _videos
        +IReadOnlyCollection~EventImage~ Images
        +IReadOnlyCollection~EventVideo~ Videos
        +AddImage(url, blobName) Result
        +ReplaceImage(imageId, url, blobName) Result
        +RemoveImage(imageId) Result
        +ReorderImages(requests) Result
        +AddVideo(url, blobName, ...) Result
        +RemoveVideo(videoId) Result
        +ReorderVideos(requests) Result
    }

    class EventImage {
        <<Entity>>
        +Guid Id
        +Guid EventId
        +string ImageUrl
        +string BlobName
        +int DisplayOrder
        +DateTime UploadedAt
        +Create(eventId, url, blobName, order)$ EventImage
        +UpdateDisplayOrder(newOrder) void
    }

    class EventVideo {
        <<Entity>>
        +Guid Id
        +Guid EventId
        +string VideoUrl
        +string BlobName
        +string ThumbnailUrl
        +string ThumbnailBlobName
        +TimeSpan? Duration
        +string Format
        +long FileSizeBytes
        +int DisplayOrder
        +DateTime UploadedAt
        +Create(eventId, url, ...)$ EventVideo
        +UpdateDisplayOrder(newOrder) void
    }

    class EventImageReplacedDomainEvent {
        <<DomainEvent>>
        +Guid EventId
        +Guid OldImageId
        +Guid NewImageId
        +string OldBlobName
        +string NewBlobName
        +DateTime OccurredOn
    }

    class EventVideoAddedDomainEvent {
        <<DomainEvent>>
        +Guid EventId
        +Guid VideoId
        +string VideoUrl
        +string ThumbnailUrl
        +DateTime OccurredOn
    }

    class EventVideoRemovedDomainEvent {
        <<DomainEvent>>
        +Guid EventId
        +Guid VideoId
        +string VideoBlobName
        +string ThumbnailBlobName
        +DateTime OccurredOn
    }

    Event "1" *-- "0..10" EventImage : contains
    Event "1" *-- "0..3" EventVideo : contains
    Event ..> EventImageReplacedDomainEvent : raises
    Event ..> EventVideoAddedDomainEvent : raises
    Event ..> EventVideoRemovedDomainEvent : raises
```

---

## 6. Infrastructure Layer - Storage Architecture

```mermaid
graph TB
    subgraph "Application Layer"
        Handler[Command Handlers]
    end

    subgraph "Infrastructure Services"
        MediaService[IMediaStorageService<br/>Implementation:<br/>AzureBlobMediaStorageService]
        VideoProc[IVideoProcessingService<br/>Implementation:<br/>BasicVideoProcessingService]

        MediaService --> VideoProc
    end

    subgraph "Azure Blob Storage"
        ImageContainer[event-images<br/>Container]
        VideoContainer[event-videos<br/>Container]
        ThumbContainer[event-video-thumbnails<br/>Container]

        subgraph "Blob Naming Convention"
            ImageBlob["{eventId}/{imageId}_timestamp.ext"]
            VideoBlob["{eventId}/{videoId}_timestamp.ext"]
            ThumbBlob["{eventId}/{videoId}_thumb.jpg"]
        end
    end

    Handler --> MediaService
    MediaService -->|Upload images| ImageContainer
    MediaService -->|Upload videos| VideoContainer
    MediaService -->|Upload thumbnails| ThumbContainer

    ImageContainer --> ImageBlob
    VideoContainer --> VideoBlob
    ThumbContainer --> ThumbBlob

    style MediaService fill:#2563eb,color:#fff
    style VideoProc fill:#8b5cf6,color:#fff
    style ImageContainer fill:#10b981,color:#fff
    style VideoContainer fill:#10b981,color:#fff
    style ThumbContainer fill:#10b981,color:#fff
```

---

## 7. Database Schema Diagram

```mermaid
erDiagram
    Events ||--o{ EventImages : has
    Events ||--o{ EventVideos : has

    Events {
        uniqueidentifier Id PK
        nvarchar(200) Title
        nvarchar(2000) Description
        datetime2 EventDate
        nvarchar(500) Location
        datetime2 CreatedAt
        datetime2 UpdatedAt
    }

    EventImages {
        uniqueidentifier Id PK
        uniqueidentifier EventId FK
        nvarchar(2048) ImageUrl
        nvarchar(500) BlobName
        int DisplayOrder
        datetime2 UploadedAt
    }

    EventVideos {
        uniqueidentifier Id PK
        uniqueidentifier EventId FK
        nvarchar(2048) VideoUrl
        nvarchar(500) BlobName
        nvarchar(2048) ThumbnailUrl
        nvarchar(500) ThumbnailBlobName
        bigint Duration_Ticks
        nvarchar(50) Format
        bigint FileSizeBytes
        int DisplayOrder
        datetime2 UploadedAt
    }
```

**Indexes:**
- `EventImages`: `IX_EventImages_EventId_DisplayOrder` (Unique)
- `EventVideos`: `IX_EventVideos_EventId_DisplayOrder` (Unique)
- `EventVideos`: `IX_EventVideos_EventId`

**Constraints:**
- Both tables: `ON DELETE CASCADE` from Events table
- DisplayOrder: Must be >= 0
- FileSizeBytes: Must be > 0

---

## 8. API Endpoint Architecture

```mermaid
graph LR
    subgraph "Image Endpoints"
        AddImage[POST /events/:id/images]
        ReplaceImage[PUT /events/:id/images/:imageId]
        DeleteImage[DELETE /events/:id/images/:imageId]
        ReorderImages[PUT /events/:id/images/reorder]
    end

    subgraph "Video Endpoints (NEW)"
        AddVideo[POST /events/:id/videos]
        DeleteVideo[DELETE /events/:id/videos/:videoId]
        GetVideos[GET /events/:id/videos]
        ReorderVideos[PUT /events/:id/videos/reorder]
    end

    subgraph "Commands"
        AddImageCmd[AddImageToEventCommand]
        ReplaceImageCmd[ReplaceEventImageCommand]
        DeleteImageCmd[DeleteEventImageCommand]
        ReorderImagesCmd[ReorderEventImagesCommand]
        AddVideoCmd[AddVideoToEventCommand]
        DeleteVideoCmd[DeleteEventVideoCommand]
        ReorderVideosCmd[ReorderEventVideosCommand]
    end

    subgraph "Queries"
        GetVideosQuery[GetEventVideosQuery]
    end

    AddImage --> AddImageCmd
    ReplaceImage --> ReplaceImageCmd
    DeleteImage --> DeleteImageCmd
    ReorderImages --> ReorderImagesCmd
    AddVideo --> AddVideoCmd
    DeleteVideo --> DeleteVideoCmd
    GetVideos --> GetVideosQuery
    ReorderVideos --> ReorderVideosCmd

    style ReplaceImage fill:#f59e0b,color:#000
    style AddVideo fill:#10b981,color:#fff
    style DeleteVideo fill:#10b981,color:#fff
    style GetVideos fill:#10b981,color:#fff
    style ReorderVideos fill:#10b981,color:#fff
```

---

## 9. Error Handling Flow - Compensating Transactions

```mermaid
sequenceDiagram
    participant Handler
    participant Event as Event Aggregate
    participant Media as IMediaStorageService
    participant Repo as Repository
    participant DB as Database
    participant Blob as Azure Blob

    Note over Handler,Blob: Happy Path
    Handler->>Media: UploadImageAsync()
    Media->>Blob: Upload new blob
    Blob-->>Media: Success
    Media-->>Handler: (url, blobName)

    Handler->>Event: ReplaceImage()
    Event-->>Handler: Success

    Handler->>Repo: SaveChangesAsync()
    Repo->>DB: UPDATE event_images
    DB-->>Repo: Success

    Handler->>Media: DeleteImageAsync(oldBlob)
    Media->>Blob: Delete old blob
    Blob-->>Media: Success

    Note over Handler,Blob: Error Scenario 1: Domain Failure
    Handler->>Media: UploadImageAsync()
    Media-->>Handler: (url, blobName)

    Handler->>Event: ReplaceImage()
    Event-->>Handler: Failure (image not found)

    Handler->>Media: DeleteImageAsync(newBlob)
    Note over Handler,Media: Cleanup uploaded blob
    Media-->>Handler: Cleanup complete

    Handler-->>Handler: Return failure to user

    Note over Handler,Blob: Error Scenario 2: Old Blob Delete Fails
    Handler->>Media: UploadImageAsync()
    Media-->>Handler: (url, blobName)

    Handler->>Event: ReplaceImage()
    Event-->>Handler: Success

    Handler->>Repo: SaveChangesAsync()
    Repo-->>Handler: Success

    Handler->>Media: DeleteImageAsync(oldBlob)
    Media-->>Handler: Exception!

    Note over Handler: Log warning<br/>Continue (orphaned blob)
    Handler-->>Handler: Return success to user
```

---

## 10. Deployment Architecture

```mermaid
graph TB
    subgraph "Azure Cloud"
        subgraph "App Service"
            API[Web API<br/>ASP.NET Core 8.0]
        end

        subgraph "Azure SQL"
            DB[(SQL Database<br/>Event Metadata)]
        end

        subgraph "Azure Storage Account"
            BlobService[Blob Service]

            subgraph "Containers"
                ImgContainer[event-images]
                VidContainer[event-videos]
                ThumbContainer[event-video-thumbnails]
            end
        end

        subgraph "Optional (Phase 3)"
            CDN[Azure CDN<br/>Video Delivery]
            MediaServices[Azure Media Services<br/>Video Transcoding]
        end
    end

    subgraph "Clients"
        Web[Web Browser]
        Mobile[Mobile App]
    end

    Web -->|HTTPS| API
    Mobile -->|HTTPS| API
    API -->|Entity Framework| DB
    API -->|Azure SDK| BlobService

    BlobService --> ImgContainer
    BlobService --> VidContainer
    BlobService --> ThumbContainer

    CDN -.->|Future| VidContainer
    MediaServices -.->|Future| VidContainer

    style API fill:#2563eb,color:#fff
    style DB fill:#10b981,color:#fff
    style BlobService fill:#0078d4,color:#fff
    style CDN fill:#f59e0b,color:#000
    style MediaServices fill:#f59e0b,color:#000
```

---

## 11. State Transition Diagram - Event Media Lifecycle

```mermaid
stateDiagram-v2
    [*] --> NoImages: Event Created

    NoImages --> HasImages: AddImage()
    HasImages --> HasImages: AddImage() [count < 10]
    HasImages --> HasImages: ReplaceImage()
    HasImages --> HasImages: RemoveImage() [count > 1]
    HasImages --> NoImages: RemoveImage() [count = 1]
    HasImages --> HasImages: ReorderImages()

    NoImages --> NoVideos
    NoVideos --> HasVideos: AddVideo()
    HasVideos --> HasVideos: AddVideo() [count < 3]
    HasVideos --> HasVideos: RemoveVideo() [count > 1]
    HasVideos --> NoVideos: RemoveVideo() [count = 1]
    HasVideos --> HasVideos: ReorderVideos()

    note right of HasImages
        Max 10 images
        DisplayOrder: 0-9
    end note

    note right of HasVideos
        Max 3 videos
        DisplayOrder: 0-2
    end note
```

---

## 12. Testing Strategy Pyramid

```mermaid
graph TB
    subgraph "Testing Pyramid"
        E2E[End-to-End Tests<br/>Manual Testing via Swagger<br/>~10 scenarios]
        Integration[Integration Tests<br/>API + Database + Azurite<br/>~30 tests]
        Unit[Unit Tests<br/>Domain + Application Layer<br/>~100 tests]
    end

    Unit --> Integration
    Integration --> E2E

    style Unit fill:#10b981,color:#fff
    style Integration fill:#3b82f6,color:#fff
    style E2E fill:#8b5cf6,color:#fff
```

**Test Coverage Goals:**
- **Domain Layer:** 95%+ (critical business logic)
- **Application Layer:** 90%+ (command handlers, validators)
- **Infrastructure Layer:** 80%+ (blob storage, EF Core)
- **API Layer:** 85%+ (endpoints, validation)
- **Overall:** 90%+

---

## 13. Continuous Integration/Deployment Pipeline

```mermaid
graph LR
    subgraph "CI/CD Pipeline"
        Commit[Git Commit]
        Build[Build .NET Project]
        UnitTests[Run Unit Tests]
        IntegrationTests[Run Integration Tests]
        Analyze[Code Analysis<br/>SonarQube]
        Package[Package for Deploy]
        DeployStaging[Deploy to Staging]
        SmokeTests[Smoke Tests]
        DeployProd[Deploy to Production]
    end

    Commit --> Build
    Build --> UnitTests
    UnitTests --> IntegrationTests
    IntegrationTests --> Analyze
    Analyze --> Package
    Package --> DeployStaging
    DeployStaging --> SmokeTests
    SmokeTests -->|Manual Approval| DeployProd

    style Commit fill:#6b7280,color:#fff
    style Build fill:#3b82f6,color:#fff
    style UnitTests fill:#10b981,color:#fff
    style IntegrationTests fill:#10b981,color:#fff
    style Analyze fill:#f59e0b,color:#000
    style DeployProd fill:#dc2626,color:#fff
```

---

## 14. Monitoring and Observability

```mermaid
graph TB
    subgraph "Application Telemetry"
        API[Web API]
        AppInsights[Azure Application Insights]

        API -->|Logs| AppInsights
        API -->|Metrics| AppInsights
        API -->|Traces| AppInsights
    end

    subgraph "Key Metrics"
        Perf[Performance Metrics]
        Errors[Error Rates]
        Usage[Usage Patterns]

        Perf -->|Image replace time| Dashboard
        Perf -->|Video upload time| Dashboard
        Errors -->|Failed uploads| Alerts
        Errors -->|Blob delete failures| Alerts
        Usage -->|Videos per event| Dashboard
        Usage -->|Storage growth| Dashboard
    end

    subgraph "Alerts"
        HighError[High Error Rate > 5%]
        SlowUpload[Slow Uploads > 30s]
        StorageLow[Storage Quota > 80%]
        OrphanedBlobs[Orphaned Blobs > 100]
    end

    AppInsights --> Perf
    AppInsights --> Errors
    AppInsights --> Usage

    Errors --> HighError
    Perf --> SlowUpload
    Usage --> StorageLow
    Usage --> OrphanedBlobs

    style API fill:#2563eb,color:#fff
    style AppInsights fill:#0078d4,color:#fff
    style Dashboard fill:#10b981,color:#fff
    style Alerts fill:#dc2626,color:#fff
```

**Key Performance Indicators (KPIs):**
1. Image replace latency (target: < 2s)
2. Video upload latency for 50 MB (target: < 10s)
3. Failed upload rate (target: < 1%)
4. Orphaned blob count (target: < 50)
5. Storage cost per event (target: < $0.10/month)

---

## 15. Security Architecture

```mermaid
graph TB
    subgraph "Security Layers"
        Auth[Authentication<br/>JWT Bearer Token]
        Authz[Authorization<br/>Event Organizer Check]
        Validation[File Validation<br/>Type, Size, Magic Bytes]
        RateLimit[Rate Limiting<br/>Max 10 uploads/min]
        Scanning[Malware Scanning<br/>Azure Defender]
    end

    subgraph "Blob Storage Security"
        SAS[SAS Tokens<br/>Temporary Access]
        Private[Private Containers<br/>No Anonymous Access]
        HTTPS[HTTPS Only<br/>Encrypted in Transit]
        Encryption[Encryption at Rest<br/>Azure Storage Service Encryption]
    end

    subgraph "Database Security"
        ConnString[Encrypted Connection String]
        Params[Parameterized Queries<br/>EF Core]
        RowLevel[Row-Level Security<br/>Future Enhancement]
    end

    User[User Request] --> Auth
    Auth --> Authz
    Authz --> Validation
    Validation --> RateLimit
    RateLimit --> Scanning
    Scanning --> Private
    Private --> SAS
    SAS --> HTTPS
    HTTPS --> Encryption

    API[API Layer] --> ConnString
    ConnString --> Params
    Params --> RowLevel

    style Auth fill:#dc2626,color:#fff
    style Authz fill:#dc2626,color:#fff
    style Validation fill:#f59e0b,color:#000
    style Encryption fill:#10b981,color:#fff
```

---

## Legend

**Color Coding:**
- 🔵 Blue: Core system components
- 🟣 Purple: Application logic
- 🔴 Pink: Domain layer
- 🟢 Green: Infrastructure/Data
- 🟠 Orange: External services/Future enhancements
- 🔴 Red: Security/Critical operations

**Symbols:**
- Solid lines: Direct dependencies
- Dashed lines: Future/Optional dependencies
- Arrows: Data flow direction

---

## How to Use These Diagrams

1. **For Developers:** Use the sequence diagrams to understand implementation flow
2. **For Architects:** Use C4 diagrams to understand system structure
3. **For QA:** Use state diagrams to design test scenarios
4. **For DevOps:** Use deployment diagram for infrastructure setup
5. **For Stakeholders:** Use context diagram for high-level understanding

---

## Related Documents

- [EventMedia-Architecture.md](./EventMedia-Architecture.md) - Detailed architecture guide
- [EventMedia-Implementation-Plan.md](./EventMedia-Implementation-Plan.md) - Step-by-step implementation
- [EventMedia-Decision-Matrix.md](./EventMedia-Decision-Matrix.md) - Architecture decisions and rationale
