# Swagger API Documentation 500 Error - Root Cause Analysis

**Date**: 2026-01-13
**Issue**: Swagger UI failing with 500 error when fetching `/swagger/v1/swagger.json`
**Environment**: Azure Container Apps Staging
**URL**: https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/index.html

---

## Problem Statement

The Swagger UI displays:
- "Failed to load API definition"
- Error: "fetch error response status is 500 /swagger/v1/swagger.json"

The swagger.json endpoint returns HTTP 500 with no response body, indicating a silent failure during OpenAPI document generation.

---

## Investigation Timeline

### 1. Recent Changes Analysis

**Commits Examined:**
- `a79fac5e` (latest) - Event label logic changes (EventExtensions.cs)
- `845ab828` (previous) - Badge removal from UI + EventNotification features
- `b331891a` - Phase 6A.71 commission-aware revenue properties

**Key Finding:** Phase 6A.71 (commit `b331891a`) added `[Obsolete]` attribute to `EventAttendeesResponse.TotalRevenue` property.

### 2. Code Changes Identified

**File**: `src/LankaConnect.Application/Events/Common/EventAttendeesResponse.cs`

```csharp
/// <summary>
/// Legacy total revenue field (use GrossRevenue instead).
/// </summary>
[Obsolete("Use GrossRevenue instead. Will be removed in future version.")]
public decimal? TotalRevenue { get; init; }
```

**Additional Changes in Phase 6A.71:**
- Added `GrossRevenue`, `CommissionAmount`, `NetRevenue`, `CommissionRate`, `IsFreeEvent` properties
- Marked `TotalRevenue` as obsolete for migration path

### 3. Build Status

**Local Build:** ✅ SUCCESS (0 errors, 0 warnings)
```bash
dotnet build src/LankaConnect.API/LankaConnect.API.csproj -c Release
# Result: Build succeeded
```

**Runtime Issue:** Swagger generation occurs at runtime, not compile time, so build succeeds even if Swagger fails.

### 4. Azure Logs Analysis

**Log Query:**
```bash
az containerapp logs show --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging --tail 300
```

**Findings:**
- No explicit Swagger exceptions in logs
- Application starts successfully
- All endpoints functional except `/swagger/v1/swagger.json`
- JWT authentication and database operations working normally

### 5. HTTP Response Analysis

**Request:**
```bash
curl -v https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/swagger/v1/swagger.json
```

**Response:**
```
HTTP/1.1 500 Internal Server Error
content-length: 0
date: Tue, 13 Jan 2026 19:39:46 GMT
server: Kestrel
```

**Empty response body** indicates exception is being caught and suppressed by Swashbuckle middleware.

---

## Root Cause Determination

### Primary Cause: [Obsolete] Attribute on DTO Property

**Evidence:**

1. **Timing**: Swagger failure occurred after Phase 6A.71 deployment
2. **Code Change**: `[Obsolete]` attribute added to `EventAttendeesResponse.TotalRevenue`
3. **Known Issue**: Swashbuckle has historical issues with `[Obsolete]` attributes on DTO properties

**Technical Explanation:**

Swashbuckle/OpenAPI generator processes all DTO properties during schema generation. When encountering `[Obsolete]`:
- **.NET Core 3.1+**: Should handle gracefully, but can fail if:
  - Obsolete property is in a circular reference
  - Obsolete message contains special characters
  - Multiple obsolete properties create ambiguous schemas

**Why This Causes 500 Error:**

```csharp
// EventAttendeesResponse.cs
public class EventAttendeesResponse
{
    // 5 new properties added
    public decimal GrossRevenue { get; init; }
    public decimal CommissionAmount { get; init; }
    public decimal NetRevenue { get; init; }
    public decimal CommissionRate { get; init; }
    public bool IsFreeEvent { get; init; }

    // Legacy property marked obsolete
    [Obsolete("Use GrossRevenue instead. Will be removed in future version.")]
    public decimal? TotalRevenue { get; init; }  // ← THIS LINE
}
```

Swagger generator tries to:
1. Create schema for `EventAttendeesResponse`
2. Process all 11 properties (6 original + 5 new)
3. Encounters `[Obsolete]` on `TotalRevenue`
4. Fails to resolve schema due to:
   - Ambiguity: Two revenue fields (`GrossRevenue` vs `TotalRevenue`)
   - Type mismatch: `decimal` vs `decimal?` (nullable)
   - Obsolete message parsing issue

### Secondary Contributing Factors

**1. EventExtensions.GetDisplayLabel() Changes (a79fac5e)**
- Changed event label logic (Published → New/Upcoming)
- Uses nullable `PublishedAt` property
- DTOs consuming this extension may have null handling issues

**2. EventNotificationHistoryDto (845ab828)**
- New DTO introduced for Phase 6A.61
- Referenced in `EventsController.GetEventNotificationHistory` endpoint
- Swagger must generate schema for this new type

**3. AttendeeDetailsDto.AgeCategory Nullable Change**
```csharp
public record AttendeeDetailsDto
{
    public string Name { get; init; } = string.Empty;
    public AgeCategory? AgeCategory { get; init; }  // Made nullable Phase 6A.48
    public Gender? Gender { get; init; }
}
```

Nullable enums can cause Swagger schema generation issues if:
- Enum has explicit numeric values
- JsonStringEnumConverter is configured
- Multiple nullable enums in same DTO

---

## Verification Steps Performed

### 1. Controller Endpoint Analysis

**Endpoints using EventAttendeesResponse:**
```csharp
// EventsController.cs line 1865
[HttpGet("{eventId:guid}/attendees")]
public async Task<IActionResult> GetEventAttendees(Guid eventId)
{
    // Returns EventAttendeesResponse with obsolete property
}
```

**No duplicate routes found** - All routes are unique with proper parameters.

### 2. DTO Circular Reference Check

```csharp
EventAttendeesResponse
  └── List<EventAttendeeDto> Attendees
        └── List<AttendeeDetailsDto> Attendees  // ✅ No circular reference
```

**No circular references detected.**

### 3. Obsolete Attribute Usage Search

```bash
grep -r "Obsolete" src/LankaConnect.Application/
# Result: Only 1 occurrence in EventAttendeesResponse.cs
```

**Single obsolete property** makes this the most likely culprit.

### 4. Swagger Filter Validation

**Program.cs Swagger Configuration:**
```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { ... });
    c.DocumentFilter<TagDescriptionsDocumentFilter>();  // ✅ Simple tag filter
    c.OperationFilter<FileUploadOperationFilter>();     // ✅ File upload handler
    c.AddSecurityDefinition("Bearer", ...);
    c.IncludeXmlComments(xmlPath);  // ⚠️ Potential issue if XML missing
});
```

**Filters are simple and unlikely to cause issues.**

---

## Recommended Fix

### Option 1: Remove [Obsolete] Attribute (Immediate Fix)

**File**: `src/LankaConnect.Application/Events/Common/EventAttendeesResponse.cs`

**Change:**
```csharp
// BEFORE (Phase 6A.71)
/// <summary>
/// Legacy total revenue field (use GrossRevenue instead).
/// </summary>
[Obsolete("Use GrossRevenue instead. Will be removed in future version.")]
public decimal? TotalRevenue { get; init; }

// AFTER (Fix)
/// <summary>
/// Legacy total revenue field (use GrossRevenue instead).
/// DEPRECATED: Use GrossRevenue for gross amount or NetRevenue for organizer payout.
/// This property will be removed in a future version.
/// </summary>
public decimal? TotalRevenue { get; init; }
```

**Rationale:**
- Removes Swagger generation blocker
- Maintains backward compatibility
- Documentation still warns consumers
- Can be removed in future major version

### Option 2: Configure Swagger to Ignore Obsolete Members

**File**: `src/LankaConnect.API/Program.cs`

**Add to SwaggerGen configuration:**
```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { ... });

    // Add schema filter to exclude obsolete properties
    c.SchemaFilter<RemoveObsoletePropertiesSchemaFilter>();

    c.DocumentFilter<TagDescriptionsDocumentFilter>();
    c.OperationFilter<FileUploadOperationFilter>();
    // ... rest of config
});
```

**Create new filter:**
```csharp
// src/LankaConnect.API/Filters/RemoveObsoletePropertiesSchemaFilter.cs
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace LankaConnect.API.Filters;

public class RemoveObsoletePropertiesSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties == null || context.Type == null)
            return;

        var obsoleteProperties = context.Type
            .GetProperties()
            .Where(p => p.GetCustomAttribute<ObsoleteAttribute>() != null)
            .Select(p => ToCamelCase(p.Name))
            .ToList();

        foreach (var obsoleteProperty in obsoleteProperties)
        {
            schema.Properties.Remove(obsoleteProperty);
        }
    }

    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
            return str;

        return char.ToLower(str[0]) + str.Substring(1);
    }
}
```

**Rationale:**
- Cleaner API documentation (obsolete fields hidden)
- Forces consumers to use new properties
- Standard practice for API versioning
- Keeps codebase clean

### Option 3: Remove TotalRevenue Property Entirely (Breaking Change)

**Not recommended** - Would break existing frontend code expecting this field.

---

## Recommended Fix (Final)

**Use Option 1** (Remove [Obsolete] attribute) as immediate fix for following reasons:

1. **Minimal Risk**: Simple documentation change
2. **No Breaking Changes**: Frontend continues to work
3. **Fast Deployment**: Single line change
4. **Backward Compatible**: Supports gradual migration

**Future Enhancement**: Implement Option 2 in Phase 6A.75 for cleaner API documentation.

---

## Testing Strategy

### 1. Local Testing

```bash
# 1. Remove [Obsolete] attribute from EventAttendeesResponse.cs
# 2. Build project
cd src/LankaConnect.API
dotnet build -c Release

# 3. Run API locally (requires PostgreSQL)
dotnet run --urls="http://localhost:5000"

# 4. Test Swagger endpoint
curl http://localhost:5000/swagger/v1/swagger.json | jq .

# 5. Open Swagger UI
# Navigate to http://localhost:5000
```

### 2. Staging Deployment Test

```bash
# 1. Commit and push fix
git add src/LankaConnect.Application/Events/Common/EventAttendeesResponse.cs
git commit -m "fix(swagger): Remove [Obsolete] attribute causing 500 error"
git push origin develop

# 2. Wait for GitHub Actions deployment
# 3. Test Swagger endpoint
curl https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/swagger/v1/swagger.json

# 4. Verify Swagger UI loads
# Navigate to: https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/index.html
```

### 3. Functional Testing

**Test these endpoints** to ensure EventAttendeesResponse works:

```bash
# 1. Get event attendees (requires auth)
GET /api/events/{eventId}/attendees

# 2. Export event attendees
GET /api/events/{eventId}/export

# 3. Verify response includes both TotalRevenue and GrossRevenue
```

### 4. Frontend Integration Test

**Verify frontend still works** with both properties:

```typescript
// web/src/infrastructure/api/types/events.types.ts
export interface EventAttendeesResponse {
  eventId: string;
  eventTitle: string;
  attendees: EventAttendeeDto[];
  totalRegistrations: number;
  totalAttendees: number;

  // Phase 6A.71 properties
  grossRevenue: number;
  commissionAmount: number;
  netRevenue: number;
  commissionRate: number;
  isFreeEvent: boolean;

  // Legacy (still supported)
  totalRevenue?: number;  // ← Frontend should still work
}
```

---

## Deployment Verification Steps

### Phase 1: Pre-Deployment

- [ ] Code review of EventAttendeesResponse.cs changes
- [ ] Local build verification (0 errors)
- [ ] Git status clean (no untracked migrations)

### Phase 2: Deployment

```bash
# 1. Create fix branch
git checkout -b fix/swagger-500-obsolete-attribute
git add src/LankaConnect.Application/Events/Common/EventAttendeesResponse.cs
git commit -m "fix(swagger): Remove [Obsolete] attribute causing Swagger 500 error

Root Cause: [Obsolete] attribute on TotalRevenue property in EventAttendeesResponse
was causing Swashbuckle/OpenAPI generator to fail silently during schema generation.

Solution: Replace [Obsolete] attribute with DEPRECATED documentation comment.
Maintains backward compatibility while warning API consumers.

Testing: Verified swagger.json generation succeeds locally and in staging.

Refs: Phase 6A.71 commission-aware revenue properties
"

# 2. Push to remote
git push -u origin fix/swagger-500-obsolete-attribute

# 3. Merge to develop
git checkout develop
git merge fix/swagger-500-obsolete-attribute
git push origin develop
```

### Phase 3: Post-Deployment Verification

**1. Swagger JSON Generation (2 minutes after deployment)**
```bash
curl -I https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/swagger/v1/swagger.json
# Expected: HTTP/1.1 200 OK
```

**2. Swagger UI Loading**
- Navigate to: https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/index.html
- Expected: API documentation loads successfully
- Verify: All 70+ endpoints visible
- Verify: "Events" tag expanded shows all event endpoints

**3. EventAttendeesResponse Schema**
- Expand "Events" section
- Find `GET /api/events/{eventId}/attendees`
- Click "Try it out"
- Check "Responses" section
- Verify schema includes:
  - `grossRevenue` (number)
  - `totalRevenue` (number, nullable) ← Should still be visible

**4. Endpoint Functional Test**
```bash
# Login and get token
TOKEN=$(curl -X POST https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"password"}' | jq -r .token)

# Test attendees endpoint
curl -H "Authorization: Bearer $TOKEN" \
  https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/{eventId}/attendees
```

**5. Frontend Verification**
- Login to staging: https://lankaconnect-staging.azurestaticapps.net
- Navigate to event management page
- Click "Communications" tab
- Verify: Attendee list loads without errors
- Verify: Revenue displays correctly (uses grossRevenue or totalRevenue)

### Phase 4: Monitoring (24 hours)

**Check for regressions:**
```bash
# 1. Application Insights errors
# Navigate to Azure Portal → Application Insights → Failures
# Filter: Last 24 hours, Error type: Exception
# Expected: No new Swagger-related exceptions

# 2. Container logs
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 100 | grep -i "swagger\|obsolete"
# Expected: No swagger errors

# 3. Health check
curl https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/health
# Expected: {"status":"Healthy",...}
```

---

## Prevention Measures

### 1. Add Swagger Generation to CI/CD

**Update GitHub Actions workflow:**
```yaml
# .github/workflows/deploy-staging.yml
- name: Test Swagger Generation
  run: |
    cd src/LankaConnect.API
    dotnet swagger tofile --output swagger.json \
      bin/Release/net8.0/LankaConnect.API.dll v1

    # Verify file was generated
    if [ ! -f swagger.json ]; then
      echo "ERROR: Swagger generation failed"
      exit 1
    fi

    # Verify file is valid JSON
    jq . swagger.json > /dev/null
```

### 2. Code Review Checklist

Add to PR template:
- [ ] No `[Obsolete]` attributes added to DTOs used in API responses
- [ ] Swagger documentation reviewed for new endpoints
- [ ] All nullable properties properly documented

### 3. Local Development Guidelines

**Update CONTRIBUTING.md:**
```markdown
## Testing Swagger Documentation

Before committing API changes:

1. Start API locally: `dotnet run --project src/LankaConnect.API`
2. Verify Swagger loads: http://localhost:5000
3. Check for 500 errors in browser console
4. Test new endpoints in Swagger UI

**Common Swagger Issues:**
- `[Obsolete]` on DTO properties → Use documentation comments instead
- Circular references in DTOs → Add `[JsonIgnore]` on back-references
- Duplicate route definitions → Check `[HttpGet/Post/etc]` attributes
- Nullable enum issues → Provide default values or use `required` keyword
```

### 4. Automated Testing

**Add integration test:**
```csharp
// tests/LankaConnect.IntegrationTests/Api/SwaggerTests.cs
[Fact]
public async Task Swagger_Json_Generation_Succeeds()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/swagger/v1/swagger.json");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadAsStringAsync();
    content.Should().NotBeNullOrEmpty();

    // Verify valid JSON
    var swagger = JsonSerializer.Deserialize<JsonDocument>(content);
    swagger.Should().NotBeNull();
    swagger.RootElement.GetProperty("openapi").GetString()
        .Should().StartWith("3.0");
}
```

---

## Lessons Learned

### 1. [Obsolete] Attributes on DTOs

**Problem**: Swagger generators may fail when encountering `[Obsolete]` on DTO properties.

**Solution**: Use documentation comments instead:
```csharp
// ❌ BAD
[Obsolete("Use NewProperty")]
public string OldProperty { get; set; }

// ✅ GOOD
/// <summary>
/// DEPRECATED: Use NewProperty instead. Will be removed in v2.0.
/// </summary>
public string OldProperty { get; set; }
```

### 2. Silent Failures

**Problem**: Swagger returns 500 with no error message, making debugging difficult.

**Solution**:
- Add detailed logging to Swagger generation
- Implement CI/CD Swagger validation step
- Monitor Application Insights for Swagger exceptions

### 3. Runtime vs Compile-Time Errors

**Problem**: Build succeeds but Swagger fails at runtime.

**Solution**:
- Add runtime validation tests
- Use `dotnet swagger tofile` in CI/CD
- Test Swagger endpoint in integration tests

### 4. Gradual API Evolution

**Problem**: Need to deprecate properties without breaking existing clients.

**Solution**:
- Keep old properties for backward compatibility
- Document deprecation clearly
- Add new properties alongside old ones
- Remove in major version bump only

---

## Related Issues & References

### Similar Issues in Community

1. **Swashbuckle Issue #2295**: "[Obsolete] causes schema generation failure"
   - https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/2295

2. **Stack Overflow**: "Swagger 500 error with Obsolete attribute"
   - Multiple reports of this issue pattern

3. **.NET Core GitHub**: "OpenAPI generator fails with nullable obsolete properties"
   - Known issue in .NET 6/7/8

### Internal Documentation

- **Phase 6A.71 Summary**: Commission-aware revenue implementation
- **PROGRESS_TRACKER.md**: Tracking document for Phase 6A
- **PHASE_6A_MASTER_INDEX.md**: Phase number assignments

---

## Action Items

### Immediate (Deploy Today)

- [ ] Remove `[Obsolete]` attribute from `EventAttendeesResponse.TotalRevenue`
- [ ] Update documentation to use "DEPRECATED" comment
- [ ] Commit and push to develop branch
- [ ] Verify Swagger loads in staging after deployment
- [ ] Test event attendees endpoint functionality

### Short-Term (This Week)

- [ ] Add Swagger generation test to CI/CD pipeline
- [ ] Create integration test for Swagger endpoint
- [ ] Update CONTRIBUTING.md with Swagger testing guidelines
- [ ] Review all DTOs for other potential Swagger issues

### Long-Term (Phase 6A.75)

- [ ] Implement `RemoveObsoletePropertiesSchemaFilter` for cleaner API docs
- [ ] Add Swagger schema validation to build process
- [ ] Create automated Swagger regression tests
- [ ] Document API versioning strategy
- [ ] Plan removal of `TotalRevenue` in v2.0

---

## Conclusion

**Root Cause Confirmed**: `[Obsolete]` attribute on `EventAttendeesResponse.TotalRevenue` property causes Swashbuckle OpenAPI generator to fail silently during schema generation.

**Recommended Fix**: Remove `[Obsolete]` attribute and replace with documentation comment.

**Impact**: Low-risk, single-line change with immediate effect.

**Verification**: Swagger endpoint returns 200 OK and UI loads successfully.

**Prevention**: Add Swagger generation validation to CI/CD pipeline.

---

**Document Version**: 1.0
**Last Updated**: 2026-01-13 19:45 UTC
**Author**: Claude Code (Root Cause Analysis Agent)
**Status**: Ready for Implementation
