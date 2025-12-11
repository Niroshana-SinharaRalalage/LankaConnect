# Image Upload Flow Diagram

## Complete Request/Response Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         USER INTERACTION                                     │
└─────────────────────────────────────────────────────────────────────────────┘
                                   │
                  User clicks "Upload Image" button
                  Selects file from file picker
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                  FRONTEND VALIDATION                                         │
│  Component: ImageUploader.tsx                                                │
│  Location: web/src/presentation/components/ImageUploader.tsx                │
│                                                                              │
│  Checks:                                                                     │
│  ✓ File size < 10MB                                                         │
│  ✓ MIME type in [image/jpeg, image/png, image/gif, image/webp]             │
│  ✓ File extension in [.jpg, .jpeg, .png, .gif, .webp]                      │
│  ✓ Magic number validation (file signature)                                 │
└─────────────────────────────────────────────────────────────────────────────┘
                                   │
                          If validation passes
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                  REACT QUERY HOOK                                            │
│  Hook: useUploadEventImage                                                   │
│  Location: web/src/presentation/hooks/useImageUpload.ts                     │
│                                                                              │
│  Actions:                                                                    │
│  1. Create optimistic preview (URL.createObjectURL)                         │
│  2. Update React Query cache with temp image                                │
│  3. Call eventsRepository.uploadEventImage()                                │
└─────────────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                  API REPOSITORY                                              │
│  Class: EventsRepository                                                     │
│  Location: web/src/infrastructure/api/repositories/events.repository.ts     │
│                                                                              │
│  Method: uploadEventImage(eventId: string, file: File)                      │
│  Code:                                                                       │
│    const formData = new FormData();                                         │
│    formData.append('image', file);                                          │
│    return await apiClient.postMultipart(                                    │
│      `/events/${eventId}/images`,                                           │
│      formData                                                                │
│    );                                                                        │
└─────────────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                  API CLIENT                                                  │
│  Class: ApiClient                                                            │
│  Location: web/src/infrastructure/api/client/api-client.ts                  │
│                                                                              │
│  Method: postMultipart<T>(url, formData, config)                            │
│  Request Headers:                                                            │
│    Content-Type: undefined (browser sets multipart/form-data + boundary)    │
│    Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...           │
│    withCredentials: true                                                     │
│                                                                              │
│  HTTP Request:                                                               │
│    POST http://localhost:3000/api/proxy/events/{eventId}/images             │
│    Body: FormData with 'image' field                                        │
└─────────────────────────────────────────────────────────────────────────────┘
                                   │
                    BROWSER → NEXT.JS SERVER
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                  NEXT.JS API PROXY                                           │
│  Route: [POST] /api/proxy/[...path]                                         │
│  Location: web/src/app/api/proxy/[...path]/route.ts                         │
│                                                                              │
│  ❌ CURRENT CODE (BROKEN):                                                  │
│    body = await request.text();  // Cannot read multipart as text!          │
│                                                                              │
│  ✅ FIXED CODE:                                                             │
│    if (contentType?.includes('multipart/form-data')) {                      │
│      body = request.body;  // Stream as-is                                  │
│    } else {                                                                  │
│      body = await request.text();                                           │
│    }                                                                         │
│                                                                              │
│  Forwards to:                                                                │
│    POST https://lankaconnect-api-staging.politebay-.../api/events/.../images│
│    Headers: Authorization, Cookie, Content-Type                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                   │
                    NEXT.JS SERVER → AZURE BACKEND
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                  .NET API CONTROLLER                                         │
│  Class: EventsController                                                     │
│  Location: src/LankaConnect.API/Controllers/EventsController.cs             │
│                                                                              │
│  Endpoint: [HttpPost("{id:guid}/images")]                                   │
│  Attribute: [Authorize] [Consumes("multipart/form-data")]                   │
│                                                                              │
│  Method Signature:                                                           │
│    public async Task<IActionResult> AddImageToEvent(                        │
│      Guid id,                                                                │
│      IFormFile image  ← ASP.NET Core binds multipart file here              │
│    )                                                                         │
│                                                                              │
│  Code Flow:                                                                  │
│    1. Validate: image != null && image.Length > 0                           │
│    2. Read file: await image.CopyToAsync(memoryStream)                      │
│    3. Convert: imageData = memoryStream.ToArray()                           │
│    4. Create command: new AddImageToEventCommand {                          │
│         EventId = id,                                                        │
│         ImageData = imageData,                                               │
│         FileName = image.FileName                                            │
│       }                                                                      │
│    5. Send to MediatR: await Mediator.Send(command)                         │
└─────────────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                  APPLICATION LAYER (CQRS)                                    │
│  Handler: AddImageToEventCommandHandler                                     │
│  Location: src/.../Events/Commands/AddImageToEvent/...CommandHandler.cs    │
│                                                                              │
│  Handle() Flow:                                                              │
│    1. Validate Image:                                                        │
│       - _imageService.ValidateImage(imageData, fileName)                    │
│       - Checks: size, extension, magic numbers                               │
│       - Returns: Result.Success() or Result.Failure(errors)                 │
│                                                                              │
│    2. Get Event:                                                             │
│       - event = await _eventRepository.GetByIdAsync(eventId)                │
│       - Returns: Event aggregate or null                                     │
│                                                                              │
│    3. Upload to Azure:                                                       │
│       ❌ HERE IS WHERE IT FAILS!                                            │
│       - uploadResult = await _imageService.UploadImageAsync(...)            │
│       - Calls AzureBlobStorageService internally                             │
│       - Throws: InvalidOperationException if config missing                  │
│                                                                              │
│    4. Add to Domain:                                                         │
│       - event.AddImage(uploadResult.Value.Url, uploadResult.Value.BlobName) │
│       - Domain validation, business rules                                    │
│                                                                              │
│    5. Persist:                                                               │
│       - await _unitOfWork.CommitAsync()                                      │
│       - Saves to PostgreSQL                                                  │
└─────────────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                  INFRASTRUCTURE - IMAGE SERVICE                              │
│  Class: ImageService                                                         │
│  Location: src/LankaConnect.Infrastructure/Services/ImageService.cs         │
│                                                                              │
│  Method: UploadImageAsync(byte[] file, string fileName, Guid businessId)    │
│  Code:                                                                       │
│    1. Re-validate image (defense in depth)                                  │
│    2. Determine contentType from extension                                   │
│    3. Create MemoryStream from byte[]                                       │
│    4. Call Azure Blob Storage:                                               │
│       var (blobName, blobUrl) = await _blobStorageService.UploadFileAsync(  │
│         fileName, memoryStream, contentType                                  │
│       );                                                                     │
│    5. Return ImageUploadResult {                                             │
│         Url = blobUrl,                                                       │
│         BlobName = blobName,                                                 │
│         SizeBytes = file.Length,                                             │
│         ContentType = contentType                                            │
│       }                                                                      │
└─────────────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                  INFRASTRUCTURE - AZURE BLOB STORAGE SERVICE                 │
│  Class: AzureBlobStorageService                                              │
│  Location: src/LankaConnect.Infrastructure/Services/AzureBlobStorageService.cs │
│                                                                              │
│  ❌ CONSTRUCTOR FAILS HERE!                                                 │
│                                                                              │
│  Constructor Code:                                                           │
│    var connectionString = configuration["AzureStorage:ConnectionString"]    │
│        ?? throw new InvalidOperationException(                               │
│             "Azure Storage connection string not configured");               │
│                                                                              │
│  Why it fails:                                                               │
│    - Production config has: AzureBlobStorage:ConnectionString               │
│    - Code expects: AzureStorage:ConnectionString                             │
│    - Result: configuration["AzureStorage:ConnectionString"] returns NULL    │
│    - Throws exception → 500 Internal Server Error                           │
│                                                                              │
│  If config was correct, it would:                                            │
│    1. Initialize BlobServiceClient(connectionString)                         │
│    2. Get container: "event-media" (from config or default)                 │
│    3. Create container if not exists (with public blob access)              │
│    4. Generate unique blob name: {GUID}_{sanitized-filename}                │
│    5. Upload file with content-type header                                   │
│    6. Return (blobName, blobUrl)                                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                   │
                      (Would reach here if config was fixed)
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                  AZURE BLOB STORAGE                                          │
│  Service: Azure Storage Account                                             │
│  Container: event-media (or business-images)                                │
│  Access: PublicAccessType.Blob (public read)                                │
│                                                                              │
│  File Structure:                                                             │
│    event-media/                                                              │
│      ├── 8de25ef8-c2ca-342f-9d0a-1d42d8c1714c7_sunset-beach.jpg            │
│      ├── a3b9c2d1-e4f5-6789-abc1-23456789def0_party-photo.png              │
│      └── f1e2d3c4-b5a6-9876-543c-ba9876543210_concert.webp                 │
│                                                                              │
│  URL Format:                                                                 │
│    https://{accountName}.blob.core.windows.net/{container}/{blobName}       │
│    Example:                                                                  │
│    https://lankaconnectstorage.blob.core.windows.net/event-media/           │
│      8de25ef8-c2ca-342f-9d0a-1d42d8c1714c7_sunset-beach.jpg                │
└─────────────────────────────────────────────────────────────────────────────┘
                                   │
                  If all worked correctly, response would be:
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                  SUCCESS RESPONSE (200 OK)                                   │
│                                                                              │
│  Response Body (EventImageDto):                                              │
│  {                                                                           │
│    "id": "a3b9c2d1-e4f5-6789-abc1-23456789def0",                            │
│    "imageUrl": "https://lankaconnectstorage.blob.core.windows.net/...",     │
│    "displayOrder": 1,                                                        │
│    "uploadedAt": "2025-12-02T10:30:00Z"                                     │
│  }                                                                           │
│                                                                              │
│  Response Flow:                                                              │
│    Backend API → Next.js Proxy → ApiClient → React Query → UI Update        │
│                                                                              │
│  React Query Actions:                                                        │
│    1. Revoke optimistic preview URL                                         │
│    2. Replace temp image with real image from response                      │
│    3. Invalidate event detail query                                         │
│    4. Trigger onSuccess callback                                             │
│    5. UI shows uploaded image from Azure Blob Storage                       │
└─────────────────────────────────────────────────────────────────────────────┘

BUT CURRENTLY FAILS WITH:

┌─────────────────────────────────────────────────────────────────────────────┐
│                  ERROR RESPONSE (500 Internal Server Error)                  │
│                                                                              │
│  Response Body:                                                              │
│  {                                                                           │
│    "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",            │
│    "title": "An error occurred while processing your request.",             │
│    "status": 500,                                                            │
│    "traceId": "00-abc123..."                                                │
│  }                                                                           │
│                                                                              │
│  Backend Logs (Azure Container Apps):                                       │
│  System.InvalidOperationException:                                           │
│    Azure Storage connection string not configured                            │
│    at AzureBlobStorageService..ctor(IConfiguration configuration, ...)      │
│    at DI container injection...                                              │
│                                                                              │
│  React Query Actions:                                                        │
│    1. onError callback triggered                                             │
│    2. Rollback optimistic update                                             │
│    3. Revoke preview URL                                                     │
│    4. Restore previous cache state                                           │
│    5. Show error message to user                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Configuration Comparison

### CURRENT (BROKEN) - appsettings.Production.json
```json
{
  "AzureBlobStorage": {  ❌ Wrong key!
    "ConnectionString": "${AZURE_STORAGE_CONNECTION_STRING}",
    "ContainerName": "business-images"  ❌ Wrong property name!
  }
}
```

### CORRECT - appsettings.Staging.json (Already Fixed)
```json
{
  "AzureStorage": {  ✅ Correct key!
    "ConnectionString": "${AZURE_STORAGE_CONNECTION_STRING}",
    "DefaultContainer": "business-images"  ✅ Correct property name!
  }
}
```

### Backend Code Expectation
```csharp
// AzureBlobStorageService.cs line 30-36
var connectionString = configuration["AzureStorage:ConnectionString"]
    ?? throw new InvalidOperationException("Azure Storage connection string not configured");

var _defaultContainerName = configuration["AzureStorage:DefaultContainer"] ?? "event-media";
```

## Error Timeline

1. **DI Container Initialization** (Application Startup)
   - ASP.NET Core reads appsettings.Production.json
   - Looks for `AzureStorage:ConnectionString` → NOT FOUND (returns null)
   - Looks for `AzureBlobStorage:ConnectionString` → FOUND (but not used)

2. **Service Registration** (DependencyInjection.cs)
   ```csharp
   services.AddScoped<IAzureBlobStorageService, AzureBlobStorageService>();
   ```
   - Service registered but not yet instantiated

3. **Image Upload Request**
   - User uploads image
   - Controller receives request
   - MediatR sends AddImageToEventCommand
   - Handler requests IImageService from DI container
   - IImageService requests IAzureBlobStorageService from DI container
   - **DI container tries to instantiate AzureBlobStorageService**
   - **Constructor throws InvalidOperationException**
   - **Exception bubbles up as 500 error**

## Fix Required

### Option 1: Update appsettings.Production.json (RECOMMENDED)
```json
{
  "AzureStorage": {
    "ConnectionString": "${AZURE_STORAGE_CONNECTION_STRING}",
    "DefaultContainer": "event-media"
  }
}
```

### Option 2: Update Backend Code (NOT RECOMMENDED - Other envs work)
```csharp
// AzureBlobStorageService.cs
var connectionString = configuration["AzureBlobStorage:ConnectionString"]
    ?? configuration["AzureStorage:ConnectionString"]  // Fallback
    ?? throw new InvalidOperationException("Azure Storage connection string not configured");
```

### Option 3: Update DI Registration (NOT RECOMMENDED - Breaks other code)
```csharp
// DependencyInjection.cs
services.Configure<AzureStorageOptions>(
    configuration.GetSection("AzureBlobStorage"));  // Use wrong section name
```

**Verdict**: Option 1 is correct. Production config has a typo that needs fixing.
