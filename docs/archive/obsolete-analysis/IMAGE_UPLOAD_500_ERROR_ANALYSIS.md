# Image Upload 500 Internal Server Error - Root Cause Analysis

**Date**: 2025-12-02
**Status**: CRITICAL ISSUE IDENTIFIED
**Impact**: Event image uploads completely broken in staging environment

---

## Executive Summary

The image upload feature is failing with a **500 Internal Server Error** due to a **configuration mismatch** between the Next.js API proxy and the .NET backend's Azure Blob Storage configuration.

### Root Cause

**Configuration Key Mismatch:**
- Backend expects: `AzureStorage:ConnectionString` and `AzureStorage:DefaultContainer`
- Production config uses: `AzureBlobStorage:ConnectionString` (wrong key!)

This causes the Azure Storage service initialization to fail, resulting in 500 errors when trying to upload images.

---

## Complete Architecture Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        IMAGE UPLOAD FLOW                                 │
└─────────────────────────────────────────────────────────────────────────┘

1. FRONTEND (localhost:3000)
   ┌──────────────────────────────────────────────────────────────┐
   │ web/src/presentation/components/ImageUploader.tsx            │
   │  - User selects image file                                   │
   │  - Client-side validation (size, type, magic numbers)        │
   │  - FormData created with 'image' field                       │
   └──────────────────────────────────────────────────────────────┘
                              ↓
   ┌──────────────────────────────────────────────────────────────┐
   │ web/src/infrastructure/api/repositories/events.repository.ts │
   │  - uploadEventImage(eventId: string, file: File)             │
   │  - Calls: apiClient.postMultipart()                          │
   │  - URL: /events/{id}/images                                  │
   └──────────────────────────────────────────────────────────────┘
                              ↓
   ┌──────────────────────────────────────────────────────────────┐
   │ web/src/infrastructure/api/client/api-client.ts              │
   │  - postMultipart<T>(url, formData, config)                   │
   │  - Sets Content-Type: undefined (browser auto-sets boundary) │
   │  - Adds Authorization: Bearer {token}                        │
   │  - withCredentials: true                                     │
   └──────────────────────────────────────────────────────────────┘
                              ↓
2. NEXT.JS API PROXY (localhost:3000/api/proxy)
   ┌──────────────────────────────────────────────────────────────┐
   │ web/src/app/api/proxy/[...path]/route.ts                     │
   │  ❌ ISSUE #1: Reads body as TEXT (await request.text())     │
   │  ❌ ISSUE #2: Cannot forward multipart/form-data as text!   │
   │  - Forwards to: BACKEND_URL + path                           │
   │  - Copies Authorization header                               │
   │  - Copies Cookie header                                      │
   └──────────────────────────────────────────────────────────────┘
                              ↓
3. BACKEND API (Azure Staging - HTTPS)
   ┌──────────────────────────────────────────────────────────────┐
   │ src/LankaConnect.API/Controllers/EventsController.cs         │
   │  [HttpPost("{id:guid}/images")]                              │
   │  [Authorize]                                                 │
   │  [Consumes("multipart/form-data")]                           │
   │  - Receives IFormFile image parameter                        │
   │  - Reads image.CopyToAsync(memoryStream)                     │
   │  - Converts to byte[] for command                            │
   └──────────────────────────────────────────────────────────────┘
                              ↓
   ┌──────────────────────────────────────────────────────────────┐
   │ Application Layer - MediatR Command Handler                  │
   │ AddImageToEventCommandHandler                                │
   │  1. Validate image (size, type, magic numbers)               │
   │  2. Get event from repository                                │
   │  3. Upload to Azure Blob Storage                             │
   │  4. Add metadata to Event aggregate                          │
   │  5. Save changes via UnitOfWork                              │
   └──────────────────────────────────────────────────────────────┘
                              ↓
   ┌──────────────────────────────────────────────────────────────┐
   │ Infrastructure - ImageService.cs                             │
   │  - ValidateImage() - checks size, extension, magic numbers   │
   │  - UploadImageAsync() - wraps Azure Blob Storage call        │
   │  ❌ ISSUE #3: Depends on AzureBlobStorageService            │
   └──────────────────────────────────────────────────────────────┘
                              ↓
   ┌──────────────────────────────────────────────────────────────┐
   │ Infrastructure - AzureBlobStorageService.cs                  │
   │  ❌ CRITICAL ISSUE: Constructor throws exception!           │
   │  - Reads: configuration["AzureStorage:ConnectionString"]     │
   │  - Production config has: AzureBlobStorage:ConnectionString  │
   │  - Result: NULL → throws InvalidOperationException           │
   │  - This causes 500 Internal Server Error!                    │
   └──────────────────────────────────────────────────────────────┘
                              ↓
   ┌──────────────────────────────────────────────────────────────┐
   │ Azure Blob Storage (event-media container)                   │
   │  - Should upload file with unique GUID name                  │
   │  - Should return blob URL                                    │
   │  ❌ Never reached due to service initialization failure!    │
   └──────────────────────────────────────────────────────────────┘
```

---

## Issues Identified

### 🔴 CRITICAL: Configuration Key Mismatch

**File**: `src/LankaConnect.API/appsettings.Production.json`

```json
// ❌ WRONG - Backend code expects "AzureStorage"
{
  "AzureBlobStorage": {
    "ConnectionString": "${AZURE_STORAGE_CONNECTION_STRING}",
    "ContainerName": "business-images"
  }
}
```

**Backend Code**: `src/LankaConnect.Infrastructure/Services/AzureBlobStorageService.cs`

```csharp
// Line 30-31
var connectionString = configuration["AzureStorage:ConnectionString"]
    ?? throw new InvalidOperationException("Azure Storage connection string not configured");

var _defaultContainerName = configuration["AzureStorage:DefaultContainer"] ?? "event-media";
```

**Impact**:
- Service constructor throws exception on initialization
- DI container fails to inject AzureBlobStorageService
- ImageService cannot be created
- AddImageToEventCommandHandler fails
- Returns 500 Internal Server Error

---

### 🟡 MEDIUM: Next.js Proxy Cannot Forward Multipart Data

**File**: `web/src/app/api/proxy/[...path]/route.ts`

```typescript
// Line 76-83 - PROBLEMATIC CODE
async function forwardRequest(request: NextRequest, pathSegments: string[], method: string) {
  let body: string | undefined;
  if (method !== 'GET' && method !== 'DELETE') {
    try {
      body = await request.text();  // ❌ Cannot read multipart as text!
    } catch {
      body = undefined;
    }
  }
```

**Issue**:
- Multipart/form-data contains binary data and boundaries
- Cannot be read as text string
- Needs to be forwarded as-is (streaming)
- Current implementation breaks file uploads

**Why This Wasn't Caught Earlier**:
- Auth endpoints (login, register) use JSON
- Only file uploads use multipart/form-data
- First time testing file upload through proxy

---

### 🟢 MINOR: Container Name Mismatch

**Production Config**: `"ContainerName": "business-images"`
**Backend Default**: `"event-media"`

While not critical (backend uses default if not found), this creates confusion and potential issues.

---

## Configuration Details

### Backend Service Registration

**File**: `src/LankaConnect.Infrastructure/DependencyInjection.cs`

```csharp
// Line 142-143
services.Configure<AzureStorageOptions>(
    configuration.GetSection(AzureStorageOptions.SectionName));

// AzureStorageOptions.SectionName is likely "AzureStorage"
```

### Configuration Class Structure

The backend expects this structure:
```json
{
  "AzureStorage": {
    "ConnectionString": "...",
    "DefaultContainer": "event-media"
  }
}
```

But production has:
```json
{
  "AzureBlobStorage": {  // ❌ Wrong key!
    "ConnectionString": "...",
    "ContainerName": "business-images"  // ❌ Wrong property name!
  }
}
```

---

## Request/Response Analysis

### Expected Request from Frontend

```http
POST /api/proxy/events/8de25ef8-c2ca-342f/images HTTP/1.1
Host: localhost:3000
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW

------WebKitFormBoundary7MA4YWxkTrZu0gW
Content-Disposition: form-data; name="image"; filename="event-photo.jpg"
Content-Type: image/jpeg

<binary image data>
------WebKitFormBoundary7MA4YWxkTrZu0gW--
```

### Proxy Should Forward As

```http
POST /api/events/8de25ef8-c2ca-342f/images HTTP/1.1
Host: lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW
Cookie: refreshToken=...

------WebKitFormBoundary7MA4YWxkTrZu0gW
Content-Disposition: form-data; name="image"; filename="event-photo.jpg"
Content-Type: image/jpeg

<binary image data>
------WebKitFormBoundary7MA4YWxkTrZu0gW--
```

### Actual Backend Response (500 Error)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500,
  "traceId": "00-..."
}
```

**Likely Exception in Backend Logs**:
```
System.InvalidOperationException: Azure Storage connection string not configured
   at LankaConnect.Infrastructure.Services.AzureBlobStorageService..ctor(IConfiguration configuration, ILogger`1 logger)
   at DI container injection...
```

---

## Solutions

### Solution 1: Fix Configuration Keys (RECOMMENDED)

**File to Modify**: `src/LankaConnect.API/appsettings.Production.json`

```json
{
  "AzureStorage": {  // ✅ Changed from "AzureBlobStorage"
    "ConnectionString": "${AZURE_STORAGE_CONNECTION_STRING}",
    "DefaultContainer": "event-media"  // ✅ Changed from "ContainerName"
  }
}
```

**Environment Variable to Set in Azure**:
```bash
AZURE_STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=https;AccountName=lankaconnectstorage;AccountKey=...;EndpointSuffix=core.windows.net
```

### Solution 2: Fix Next.js Proxy to Handle Multipart

**File to Modify**: `web/src/app/api/proxy/[...path]/route.ts`

**Option A: Stream Request Body (Preferred)**

```typescript
async function forwardRequest(
  request: NextRequest,
  pathSegments: string[],
  method: string
) {
  try {
    const path = pathSegments.join('/');
    const targetUrl = `${BACKEND_URL}/${path}`;

    // Get cookies
    const cookies = request.cookies.getAll();
    const cookieHeader = cookies.map(c => `${c.name}=${c.value}`).join('; ');

    // Build headers
    const headers: HeadersInit = {};

    // Forward Content-Type exactly as sent (especially for multipart/form-data)
    const contentType = request.headers.get('content-type');
    if (contentType) {
      headers['Content-Type'] = contentType;
    }

    // Forward Authorization
    const authHeader = request.headers.get('authorization');
    if (authHeader) {
      headers['Authorization'] = authHeader;
    }

    // Forward cookies
    if (cookieHeader) {
      headers['Cookie'] = cookieHeader;
    }

    // ✅ NEW: Stream body for multipart/form-data
    let body: BodyInit | undefined;
    if (method !== 'GET' && method !== 'DELETE') {
      // Check if this is multipart/form-data
      if (contentType?.includes('multipart/form-data')) {
        // Forward as-is (streaming)
        body = request.body;
      } else {
        // JSON or other text-based content
        body = await request.text();
      }
    }

    console.log(`[Proxy] ${method} ${targetUrl}`);

    // Make request to backend
    const response = await fetch(targetUrl, {
      method,
      headers,
      body,
      credentials: 'include',
      // ✅ Important for streaming
      duplex: 'half',
    });

    // Handle response...
    // (rest of the code remains the same)
  } catch (error) {
    console.error('[Proxy] Error:', error);
    return NextResponse.json(
      { error: 'Proxy error', details: error instanceof Error ? error.message : 'Unknown error' },
      { status: 500 }
    );
  }
}
```

**Option B: Use FormData (Alternative)**

```typescript
// For multipart/form-data
if (contentType?.includes('multipart/form-data')) {
  const formData = await request.formData();
  body = formData;
}
```

---

## Testing Checklist

After fixes are applied, test the following:

### Backend Configuration Test
```bash
# SSH into Azure container
az containerapp exec --name lankaconnect-api-staging --resource-group ...

# Check environment variables
env | grep AZURE_STORAGE

# Should output:
# AZURE_STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=https;...

# Check if service starts without errors
# Look for initialization logs
```

### Frontend Upload Test
1. Navigate to event edit page
2. Click "Upload Image"
3. Select a valid image file (JPG, PNG, < 10MB)
4. Verify:
   - ✅ No 500 error
   - ✅ Image appears in gallery
   - ✅ Image URL points to Azure Blob Storage
   - ✅ Can view uploaded image
   - ✅ Can delete uploaded image

### Network Analysis
```javascript
// Browser DevTools → Network tab
// Look for:
POST /api/proxy/events/{id}/images
Status: 200 OK (not 500!)

Response:
{
  "id": "guid",
  "imageUrl": "https://lankaconnectstorage.blob.core.windows.net/event-media/guid_filename.jpg",
  "displayOrder": 1,
  "uploadedAt": "2025-12-02T..."
}
```

---

## Security Considerations

### 1. Azure Storage Connection String
- Should be stored as environment variable (✅ Already done)
- Never commit to source control (✅ Using `${AZURE_STORAGE_CONNECTION_STRING}`)
- Rotate keys periodically

### 2. Blob Storage Access
- Currently uses `PublicAccessType.Blob` (public read)
- Consider using SAS tokens for private containers
- Implement rate limiting for uploads

### 3. File Validation
- Frontend validation can be bypassed
- Backend MUST validate file type, size, content
- Check for malicious files (ImageService already validates magic numbers ✅)

### 4. Authorization
- Upload endpoint requires `[Authorize]` ✅
- Verify user owns the event before allowing upload
- Prevent unauthorized image deletion

---

## Performance Considerations

### 1. Image Optimization
- Current implementation: No image resizing
- Consider implementing:
  - Thumbnail generation (300px)
  - Medium size (800px)
  - Large size (1600px)
  - Original size
- Use Azure Functions or Image Processing service

### 2. Upload Progress
- Currently no real progress tracking
- Implement SignalR for real-time progress
- Or use chunked uploads for large files

### 3. Caching
- Azure CDN integration
- Cache-Control headers for blob URLs
- Image lazy loading on frontend

---

## Related Files

### Configuration Files
- `src/LankaConnect.API/appsettings.Production.json` (needs fix)
- `src/LankaConnect.API/appsettings.Development.json` (check if same issue)
- `src/LankaConnect.Infrastructure/DependencyInjection.cs`

### Backend Code
- `src/LankaConnect.API/Controllers/EventsController.cs`
- `src/LankaConnect.Application/Events/Commands/AddImageToEvent/AddImageToEventCommand.cs`
- `src/LankaConnect.Infrastructure/Services/ImageService.cs`
- `src/LankaConnect.Infrastructure/Services/AzureBlobStorageService.cs`

### Frontend Code
- `web/src/app/api/proxy/[...path]/route.ts` (needs fix)
- `web/src/infrastructure/api/repositories/events.repository.ts`
- `web/src/infrastructure/api/client/api-client.ts`
- `web/src/presentation/hooks/useImageUpload.ts`

---

## Architectural Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                   IMAGE UPLOAD ARCHITECTURE                              │
└─────────────────────────────────────────────────────────────────────────┘

LAYERS:

┌────────────────────────────────────────────────────────────────────────┐
│ PRESENTATION LAYER (React/Next.js)                                     │
│  - ImageUploader component                                             │
│  - useImageUpload hook (React Query)                                   │
│  - Client-side validation                                              │
└────────────────────────────────────────────────────────────────────────┘
                              ↓ FormData
┌────────────────────────────────────────────────────────────────────────┐
│ API CLIENT LAYER                                                       │
│  - ApiClient.postMultipart()                                           │
│  - Authorization header injection                                      │
│  - Error handling                                                      │
└────────────────────────────────────────────────────────────────────────┘
                              ↓ HTTP POST
┌────────────────────────────────────────────────────────────────────────┐
│ NEXT.JS API PROXY (Same-Origin for Cookies)                           │
│  ❌ ISSUE: Cannot forward multipart as text                           │
│  ✅ FIX: Stream request.body directly                                 │
└────────────────────────────────────────────────────────────────────────┘
                              ↓ HTTPS
┌────────────────────────────────────────────────────────────────────────┐
│ API LAYER (.NET Controllers)                                           │
│  - EventsController.AddImageToEvent()                                  │
│  - IFormFile parameter binding                                         │
│  - Authorization policy check                                          │
└────────────────────────────────────────────────────────────────────────┘
                              ↓ MediatR Command
┌────────────────────────────────────────────────────────────────────────┐
│ APPLICATION LAYER (Use Cases)                                          │
│  - AddImageToEventCommand                                              │
│  - AddImageToEventCommandHandler                                       │
│  - Business validation                                                 │
└────────────────────────────────────────────────────────────────────────┘
                              ↓ IImageService
┌────────────────────────────────────────────────────────────────────────┐
│ INFRASTRUCTURE LAYER (External Services)                               │
│  - ImageService (validation, orchestration)                            │
│  - AzureBlobStorageService (storage operations)                        │
│  ❌ ISSUE: Configuration["AzureStorage:..."] returns null             │
│  ✅ FIX: Update appsettings.Production.json key                       │
└────────────────────────────────────────────────────────────────────────┘
                              ↓ Azure SDK
┌────────────────────────────────────────────────────────────────────────┐
│ AZURE BLOB STORAGE                                                     │
│  - Container: event-media                                              │
│  - Public read access                                                  │
│  - File naming: {GUID}_{sanitized-filename}                            │
└────────────────────────────────────────────────────────────────────────┘
```

---

## Next Steps

1. **Immediate**: Fix `appsettings.Production.json` configuration keys
2. **Immediate**: Set Azure environment variable `AZURE_STORAGE_CONNECTION_STRING`
3. **High Priority**: Fix Next.js proxy to handle multipart/form-data
4. **Medium Priority**: Add backend logging to diagnose future issues
5. **Low Priority**: Implement image resizing and optimization

---

## Conclusion

The 500 Internal Server Error is caused by:
1. **Configuration key mismatch** preventing Azure Blob Storage service initialization
2. **Next.js proxy** incorrectly handling multipart/form-data by reading as text

Both issues must be fixed for image upload to work. The configuration fix is critical and must be deployed immediately. The proxy fix improves reliability but may not be strictly required if the configuration is fixed (backend might be lenient about malformed multipart data).

**Severity**: CRITICAL
**Priority**: P0 - Deploy immediately
**Estimated Fix Time**: 30 minutes (config change + redeploy)
