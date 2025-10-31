# Epic 1 Phase 3: Profile Enhancement Architecture

**Document Type:** Architecture Decision Document (ADD)
**Status:** Recommended Architecture
**Date:** 2025-10-30
**Author:** System Architecture Designer
**Epic:** Epic 1 - Authentication & User Management
**Phase:** Phase 3 - Profile Enhancement

---

## Executive Summary

This document provides comprehensive architectural guidance for implementing Epic 1 Phase 3: Profile Enhancement. After analyzing the existing LankaConnect codebase, I'm providing specific recommendations that follow Clean Architecture, DDD principles, and your established patterns.

**Key Decision:** Reuse existing components where possible, create targeted new value objects where needed, and maintain strict architectural boundaries.

---

## 1. ARCHITECTURAL DECISIONS

### Decision 1: Address Value Object Reusability ✅

**Question:** Should we reuse `Business.ValueObjects.Address` for User location?

**RECOMMENDATION: Create a Separate `UserLocation` Value Object**

**Rationale:**

1. **Domain Separation (Clean Architecture)**
   - `Business.ValueObjects.Address` belongs to the **Business domain**
   - Users belong to the **Users domain**
   - Cross-domain value object reuse violates domain boundaries
   - Would create a dependency: `Users` → `Business.ValueObjects` (❌ wrong direction)

2. **Different Requirements**
   - **Business Address**: Full address (Street, City, State, ZIP, Country) - required for business listings
   - **User Location**: City, State, ZIP only - privacy-focused, no street address
   - Business Address is **required** (line 24-37 validation)
   - User Location is **optional** (for discovery/matching, not critical)

3. **Semantic Clarity**
   - `Address` implies a full postal address (street-level)
   - `UserLocation` implies a regional/area location (privacy-respecting)

**Implementation:**

```csharp
// Location: src/LankaConnect.Domain/Users/ValueObjects/UserLocation.cs
namespace LankaConnect.Domain.Users.ValueObjects;

public class UserLocation : ValueObject
{
    public string City { get; }
    public string State { get; }
    public string ZipCode { get; }

    private UserLocation(string city, string state, string zipCode)
    {
        City = city;
        State = state;
        ZipCode = zipCode;
    }

    public static Result<UserLocation> Create(string city, string state, string zipCode)
    {
        if (string.IsNullOrWhiteSpace(city))
            return Result<UserLocation>.Failure("City is required");

        if (string.IsNullOrWhiteSpace(state))
            return Result<UserLocation>.Failure("State is required");

        if (string.IsNullOrWhiteSpace(zipCode))
            return Result<UserLocation>.Failure("ZIP code is required");

        if (city.Length > 100)
            return Result<UserLocation>.Failure("City cannot exceed 100 characters");

        if (state.Length > 100)
            return Result<UserLocation>.Failure("State cannot exceed 100 characters");

        if (zipCode.Length > 20)
            return Result<UserLocation>.Failure("ZIP code cannot exceed 20 characters");

        return Result<UserLocation>.Success(new UserLocation(
            city.Trim(),
            state.Trim(),
            zipCode.Trim()
        ));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return City;
        yield return State;
        yield return ZipCode;
    }

    public override string ToString() => $"{City}, {State} {ZipCode}";
}
```

**Benefits:**
- ✅ Clean domain separation (no cross-domain dependencies)
- ✅ Semantic clarity (UserLocation vs full Address)
- ✅ Privacy-focused (no street address storage)
- ✅ Follows existing ValueObject pattern
- ✅ Optional property on User entity (nullable)

---

### Decision 2: IImageService Generalization Strategy ✅

**Question:** Should we refactor `IImageService` to be generic?

**RECOMMENDATION: Option B - Refactor to Generic `Guid entityId` Parameter**

**Current Implementation Analysis:**
```csharp
// Current: Business-specific parameter
Task<Result<ImageUploadResult>> UploadImageAsync(
    byte[] file,
    string fileName,
    Guid businessId,  // ❌ Domain-specific
    CancellationToken cancellationToken = default);
```

**Recommended Refactoring:**

```csharp
// Location: src/LankaConnect.Application/Common/Interfaces/IImageService.cs

public interface IImageService
{
    /// <summary>
    /// Uploads an image file to Azure Blob Storage
    /// </summary>
    /// <param name="file">Image file data</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="entityId">Entity identifier for organizing images (Business, User, etc.)</param>
    /// <param name="containerName">Blob container name (e.g., "business-images", "profile-photos")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<Result<ImageUploadResult>> UploadImageAsync(
        byte[] file,
        string fileName,
        Guid entityId,
        string containerName,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteImageAsync(string imageUrl, CancellationToken cancellationToken = default);

    Task<Result<string>> GetSecureUrlAsync(
        string imageUrl,
        int expiresInHours = 24,
        CancellationToken cancellationToken = default);

    Result ValidateImage(byte[] file, string fileName);

    Task<Result<ImageResizeResult>> ResizeAndUploadAsync(
        byte[] originalImage,
        string fileName,
        Guid entityId,
        string containerName,
        CancellationToken cancellationToken = default);
}
```

**Update BasicImageService:**

```csharp
// Location: src/LankaConnect.Infrastructure/Storage/Services/BasicImageService.cs

public async Task<Result<ImageUploadResult>> UploadImageAsync(
    byte[] file,
    string fileName,
    Guid entityId,  // ✅ Generic now
    string containerName,  // ✅ Container specified by caller
    CancellationToken cancellationToken = default)
{
    // ... validation logic (unchanged)

    // Generate blob name with generic entity ID
    var blobName = GenerateBlobName(fileName, entityId, containerName);

    // Get container client (now dynamic based on containerName)
    var containerClient = await GetContainerClientAsync(containerName, cancellationToken);

    // ... rest of implementation
}

private static string GenerateBlobName(string fileName, Guid entityId, string containerName)
{
    var extension = Path.GetExtension(fileName);
    var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
    var uniqueId = Guid.NewGuid().ToString("N")[..8];

    // Use container name to organize files
    return $"{containerName}/{entityId}/{timestamp}_{uniqueId}{extension}";
}
```

**Configuration Update:**

```csharp
// Location: src/LankaConnect.Infrastructure/Storage/Configuration/AzureStorageOptions.cs

public class AzureStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string BusinessImagesContainer { get; set; } = "business-images";
    public string ProfilePhotosContainer { get; set; } = "profile-photos";  // ✅ New
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024; // 5MB
    public List<string> AllowedContentTypes { get; set; } = new();
    public bool IsDevelopment { get; set; }
}
```

**Rationale:**

1. **Reusability:** One service for all image types (Business, User, Event, etc.)
2. **Clean Architecture:** Application layer interface remains generic
3. **Backward Compatibility:** Existing Business code can be updated gradually
4. **Container Flexibility:** Different entities can use different containers
5. **Future-Proof:** Scales to Events, Community, etc.

**Migration Path:**

1. Update `IImageService` interface (breaking change)
2. Update `BasicImageService` implementation
3. Update existing Business command handlers to pass `containerName`
4. Use for new User profile photo feature immediately

---

### Decision 3: Profile Photo Storage Strategy ✅

**Question:** What data should User entity store for profile photos?

**RECOMMENDATION: Option B - Store URL + BlobName (Simple, Pragmatic)**

**Rationale:**

1. **Cleanup Capability:** BlobName allows deletion without parsing URL
2. **Simplicity:** Two string properties vs complex value object
3. **Proven Pattern:** Business aggregate uses same approach (BusinessImage entity has `OriginalUrl` + implicit blob name)
4. **Performance:** No extra object allocation, direct EF Core mapping

**User Entity Update:**

```csharp
// Location: src/LankaConnect.Domain/Users/User.cs

public class User : BaseEntity
{
    // ... existing properties

    // ✅ Profile photo properties
    public string? ProfilePhotoUrl { get; private set; }
    public string? ProfilePhotoBlobName { get; private set; }

    // ... existing methods

    /// <summary>
    /// Updates the user's profile photo
    /// </summary>
    public Result UpdateProfilePhoto(string photoUrl, string blobName)
    {
        if (string.IsNullOrWhiteSpace(photoUrl))
            return Result.Failure("Profile photo URL is required");

        if (string.IsNullOrWhiteSpace(blobName))
            return Result.Failure("Profile photo blob name is required");

        if (photoUrl.Length > 500)
            return Result.Failure("Profile photo URL cannot exceed 500 characters");

        if (blobName.Length > 255)
            return Result.Failure("Blob name cannot exceed 255 characters");

        ProfilePhotoUrl = photoUrl.Trim();
        ProfilePhotoBlobName = blobName.Trim();
        MarkAsUpdated();

        RaiseDomainEvent(new UserProfilePhotoUpdatedEvent(Id, Email.Value, photoUrl));
        return Result.Success();
    }

    /// <summary>
    /// Removes the user's profile photo
    /// </summary>
    public Result RemoveProfilePhoto()
    {
        if (string.IsNullOrWhiteSpace(ProfilePhotoUrl))
            return Result.Failure("No profile photo to remove");

        var oldPhotoUrl = ProfilePhotoUrl;
        var oldBlobName = ProfilePhotoBlobName;

        ProfilePhotoUrl = null;
        ProfilePhotoBlobName = null;
        MarkAsUpdated();

        RaiseDomainEvent(new UserProfilePhotoRemovedEvent(Id, Email.Value, oldPhotoUrl!, oldBlobName!));
        return Result.Success();
    }

    public bool HasProfilePhoto => !string.IsNullOrWhiteSpace(ProfilePhotoUrl);
}
```

**Database Schema:**

```sql
ALTER TABLE identity.users
ADD COLUMN profile_photo_url VARCHAR(500) NULL,
ADD COLUMN profile_photo_blob_name VARCHAR(255) NULL;

CREATE INDEX idx_users_profile_photo
ON identity.users(profile_photo_url)
WHERE profile_photo_url IS NOT NULL;
```

---

### Decision 4: Cultural Interests Domain Model ✅

**Question:** How to model cultural interests?

**RECOMMENDATION: Option A - Simple String Collection with Predefined List**

**Rationale:**

1. **MVP Simplicity:** Avoid premature entity/enum over-engineering
2. **Flexibility:** New cultures can be added without code changes
3. **EF Core JSON:** PostgreSQL JSONB column performs well
4. **Future Migration:** Can evolve to entity if needed

**Implementation:**

```csharp
// Location: src/LankaConnect.Domain/Users/User.cs

public class User : BaseEntity
{
    // ... existing properties

    // ✅ Cultural interests (JSON array in database)
    private readonly List<string> _culturalInterests = new();
    public IReadOnlyList<string> CulturalInterests => _culturalInterests.AsReadOnly();

    // Predefined cultural interest categories (can be moved to configuration)
    public static readonly HashSet<string> ValidCulturalInterests = new(StringComparer.OrdinalIgnoreCase)
    {
        "Sinhala Culture",
        "Tamil Culture",
        "Buddhist Traditions",
        "Hindu Traditions",
        "Islamic Traditions",
        "Christian Traditions",
        "Sri Lankan Cuisine",
        "Traditional Dance",
        "Traditional Music",
        "Festivals & Celebrations",
        "Arts & Crafts",
        "Language Learning",
        "Heritage & History",
        "Community Service"
    };

    /// <summary>
    /// Updates the user's cultural interests
    /// </summary>
    public Result UpdateCulturalInterests(List<string> interests)
    {
        if (interests == null)
            return Result.Failure("Cultural interests list cannot be null");

        if (interests.Count > 10)
            return Result.Failure("Cannot select more than 10 cultural interests");

        // Validate all interests are recognized
        var invalidInterests = interests
            .Where(i => !ValidCulturalInterests.Contains(i))
            .ToList();

        if (invalidInterests.Any())
            return Result.Failure($"Invalid cultural interests: {string.Join(", ", invalidInterests)}");

        // Remove duplicates (case-insensitive)
        var uniqueInterests = interests
            .Select(i => i.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _culturalInterests.Clear();
        _culturalInterests.AddRange(uniqueInterests);
        MarkAsUpdated();

        return Result.Success();
    }

    public Result AddCulturalInterest(string interest)
    {
        if (string.IsNullOrWhiteSpace(interest))
            return Result.Failure("Cultural interest is required");

        if (!ValidCulturalInterests.Contains(interest))
            return Result.Failure($"Invalid cultural interest: {interest}");

        if (_culturalInterests.Any(i => i.Equals(interest, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure("Cultural interest already exists");

        if (_culturalInterests.Count >= 10)
            return Result.Failure("Cannot add more than 10 cultural interests");

        _culturalInterests.Add(interest.Trim());
        MarkAsUpdated();

        return Result.Success();
    }

    public Result RemoveCulturalInterest(string interest)
    {
        var existingInterest = _culturalInterests
            .FirstOrDefault(i => i.Equals(interest, StringComparison.OrdinalIgnoreCase));

        if (existingInterest == null)
            return Result.Failure("Cultural interest not found");

        _culturalInterests.Remove(existingInterest);
        MarkAsUpdated();

        return Result.Success();
    }
}
```

**EF Core Configuration:**

```csharp
// Location: src/LankaConnect.Infrastructure/Data/Configurations/UserConfiguration.cs

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // ... existing configuration

        // Cultural interests as JSON array
        builder.Property(u => u.CulturalInterests)
            .HasColumnName("cultural_interests")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            );
    }
}
```

**Database Schema:**

```sql
ALTER TABLE identity.users
ADD COLUMN cultural_interests JSONB DEFAULT '[]'::jsonb;

CREATE INDEX idx_users_cultural_interests
ON identity.users USING GIN (cultural_interests);
```

---

### Decision 5: Language Preferences Model ✅

**Question:** How to model language preferences with proficiency?

**RECOMMENDATION: Option A - UserLanguage Value Object with JSON Storage**

**Rationale:**

1. **Rich Model:** Captures language + proficiency level
2. **Simple Storage:** PostgreSQL JSONB array
3. **No Junction Table:** Avoids over-engineering for MVP
4. **Type Safety:** Value object enforces validation

**Implementation:**

```csharp
// Location: src/LankaConnect.Domain/Users/ValueObjects/UserLanguage.cs

namespace LankaConnect.Domain.Users.ValueObjects;

public class UserLanguage : ValueObject
{
    public string Language { get; }
    public LanguageProficiency Proficiency { get; }

    private UserLanguage(string language, LanguageProficiency proficiency)
    {
        Language = language;
        Proficiency = proficiency;
    }

    public static Result<UserLanguage> Create(string language, LanguageProficiency proficiency)
    {
        if (string.IsNullOrWhiteSpace(language))
            return Result<UserLanguage>.Failure("Language is required");

        if (language.Length > 50)
            return Result<UserLanguage>.Failure("Language name cannot exceed 50 characters");

        if (!Enum.IsDefined(typeof(LanguageProficiency), proficiency))
            return Result<UserLanguage>.Failure("Invalid proficiency level");

        return Result<UserLanguage>.Success(new UserLanguage(language.Trim(), proficiency));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Language;
        yield return Proficiency;
    }

    public override string ToString() => $"{Language} ({Proficiency})";
}

// Location: src/LankaConnect.Domain/Users/Enums/LanguageProficiency.cs

namespace LankaConnect.Domain.Users.Enums;

public enum LanguageProficiency
{
    Beginner = 1,
    Intermediate = 2,
    Advanced = 3,
    Native = 4
}
```

**User Entity Update:**

```csharp
// Location: src/LankaConnect.Domain/Users/User.cs

public class User : BaseEntity
{
    // ... existing properties

    private readonly List<UserLanguage> _languages = new();
    public IReadOnlyList<UserLanguage> Languages => _languages.AsReadOnly();

    // Supported languages (can be moved to configuration)
    public static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "Sinhala",
        "Tamil",
        "English",
        "Hindi",
        "Arabic",
        "Urdu"
    };

    /// <summary>
    /// Updates the user's language preferences
    /// </summary>
    public Result UpdateLanguages(List<UserLanguage> languages)
    {
        if (languages == null)
            return Result.Failure("Languages list cannot be null");

        if (languages.Count > 6)
            return Result.Failure("Cannot add more than 6 languages");

        // Validate all languages are supported
        var unsupportedLanguages = languages
            .Where(l => !SupportedLanguages.Contains(l.Language))
            .Select(l => l.Language)
            .ToList();

        if (unsupportedLanguages.Any())
            return Result.Failure($"Unsupported languages: {string.Join(", ", unsupportedLanguages)}");

        // Check for duplicates
        var duplicates = languages
            .GroupBy(l => l.Language, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Any())
            return Result.Failure($"Duplicate languages: {string.Join(", ", duplicates)}");

        _languages.Clear();
        _languages.AddRange(languages);
        MarkAsUpdated();

        return Result.Success();
    }

    public Result AddLanguage(UserLanguage language)
    {
        if (language == null)
            return Result.Failure("Language is required");

        if (!SupportedLanguages.Contains(language.Language))
            return Result.Failure($"Unsupported language: {language.Language}");

        if (_languages.Any(l => l.Language.Equals(language.Language, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure("Language already exists");

        if (_languages.Count >= 6)
            return Result.Failure("Cannot add more than 6 languages");

        _languages.Add(language);
        MarkAsUpdated();

        return Result.Success();
    }

    public Result RemoveLanguage(string languageName)
    {
        var existingLanguage = _languages
            .FirstOrDefault(l => l.Language.Equals(languageName, StringComparison.OrdinalIgnoreCase));

        if (existingLanguage == null)
            return Result.Failure("Language not found");

        _languages.Remove(existingLanguage);
        MarkAsUpdated();

        return Result.Success();
    }

    public Result UpdateLanguageProficiency(string languageName, LanguageProficiency newProficiency)
    {
        var existingLanguage = _languages
            .FirstOrDefault(l => l.Language.Equals(languageName, StringComparison.OrdinalIgnoreCase));

        if (existingLanguage == null)
            return Result.Failure("Language not found");

        // Remove old and add updated
        _languages.Remove(existingLanguage);

        var updatedLanguageResult = UserLanguage.Create(languageName, newProficiency);
        if (!updatedLanguageResult.IsSuccess)
            return Result.Failure(updatedLanguageResult.Errors);

        _languages.Add(updatedLanguageResult.Value);
        MarkAsUpdated();

        return Result.Success();
    }
}
```

**EF Core Configuration:**

```csharp
// Location: src/LankaConnect.Infrastructure/Data/Configurations/UserConfiguration.cs

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // ... existing configuration

        // Languages as JSON array of objects
        builder.Property(u => u.Languages)
            .HasColumnName("languages")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v.Select(l => new {
                    language = l.Language,
                    proficiency = l.Proficiency.ToString()
                }), (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<LanguageDto>>(v, (JsonSerializerOptions?)null)!
                    .Select(dto => UserLanguage.Create(dto.Language, Enum.Parse<LanguageProficiency>(dto.Proficiency)).Value)
                    .ToList()
            );
    }

    private class LanguageDto
    {
        public string Language { get; set; } = string.Empty;
        public string Proficiency { get; set; } = string.Empty;
    }
}
```

**Database Schema:**

```sql
ALTER TABLE identity.users
ADD COLUMN languages JSONB DEFAULT '[]'::jsonb;

CREATE INDEX idx_users_languages
ON identity.users USING GIN (languages);
```

---

## 2. IMPLEMENTATION ROADMAP

### Phase 3.1: Profile Photo Upload (2 days)

**Day 1: Domain + Application Layer (TDD)**

1. **Domain Event Creation** (30 min)
   ```csharp
   // UserProfilePhotoUpdatedEvent
   // UserProfilePhotoRemovedEvent
   ```

2. **User Entity Enhancement** (45 min)
   - Add `ProfilePhotoUrl` and `ProfilePhotoBlobName` properties
   - Implement `UpdateProfilePhoto()` method
   - Implement `RemoveProfilePhoto()` method
   - Write 8 comprehensive tests

3. **IImageService Refactoring** (60 min)
   - Update interface to use `Guid entityId` + `string containerName`
   - Update `BasicImageService` implementation
   - Update existing Business handlers (backward compatibility)
   - Add `profile-photos` container configuration
   - Write 5 new tests for profile photos

4. **Application Commands** (90 min)
   - `UploadProfilePhotoCommand` + Handler
   - `DeleteProfilePhotoCommand` + Handler
   - FluentValidation rules (max 5MB, image types)
   - Write 12 tests (success, validation, storage failure)

**Day 2: API + Integration Tests**

5. **API Endpoints** (60 min)
   - `POST /api/users/{id}/profile-photo` (multipart/form-data)
   - `DELETE /api/users/{id}/profile-photo`
   - Authorization (only own profile)
   - Swagger documentation

6. **Integration Tests** (90 min)
   - Upload flow with Azure Storage
   - Delete flow with cleanup verification
   - Validation scenarios
   - Authorization checks

7. **Database Migration** (30 min)
   ```sql
   -- Add profile photo columns
   -- Create index
   ```

### Phase 3.2: Location Field (1 day)

**Morning: Domain + Application (TDD)**

1. **UserLocation Value Object** (45 min)
   - Create with City, State, ZipCode
   - Validation rules
   - Write 8 tests

2. **User Entity Enhancement** (30 min)
   - Add `Location` property (nullable `UserLocation`)
   - Implement `UpdateLocation()` method
   - Write 5 tests

3. **RegisterUserCommand Update** (45 min)
   - Add optional location parameters
   - Update handler
   - Update validator
   - Write 4 tests (with/without location)

**Afternoon: API + Integration**

4. **API Endpoints** (45 min)
   - Update `POST /api/auth/register` (add location fields)
   - Add `PUT /api/users/{id}/location`
   - Swagger documentation

5. **Integration Tests** (60 min)
   - Registration with location
   - Location update endpoint
   - Validation scenarios

6. **Database Migration** (30 min)
   ```sql
   -- Add city, state, zip_code columns
   -- Create location index
   ```

### Phase 3.3: Cultural Interests & Languages (2 days)

**Day 1: Domain + Application**

1. **Cultural Interests** (90 min)
   - Update User entity with `_culturalInterests` collection
   - Implement `UpdateCulturalInterests()` methods
   - Write 8 tests

2. **UserLanguage Value Object** (60 min)
   - Create `UserLanguage` VO
   - Create `LanguageProficiency` enum
   - Write 6 tests

3. **Languages on User** (90 min)
   - Update User entity with `_languages` collection
   - Implement language management methods
   - Write 10 tests

4. **Application Commands** (90 min)
   - `UpdateCulturalInterestsCommand` + Handler
   - `UpdateLanguagesCommand` + Handler
   - FluentValidation rules
   - Write 8 tests

**Day 2: API + Integration**

5. **API Endpoints** (90 min)
   - `PUT /api/users/{id}/cultural-interests`
   - `PUT /api/users/{id}/languages`
   - `GET /api/users/{id}/profile` (include all profile data)
   - Swagger documentation

6. **Integration Tests** (120 min)
   - Cultural interests CRUD
   - Languages CRUD
   - Full profile retrieval
   - Validation scenarios

7. **Database Migration** (30 min)
   ```sql
   -- Add cultural_interests JSONB column
   -- Add languages JSONB column
   -- Create GIN indexes
   ```

---

## 3. ARCHITECTURAL CONCERNS & PATTERNS

### Clean Architecture Compliance ✅

1. **Dependency Flow:**
   ```
   API → Application → Domain
        ↓
   Infrastructure → Application
   ```
   - ✅ `UserLocation` in `Domain.Users.ValueObjects` (no external dependencies)
   - ✅ `IImageService` in `Application.Common.Interfaces` (domain-independent)
   - ✅ Commands/Handlers in `Application` layer
   - ✅ Controllers in `API` layer

2. **Domain Purity:**
   - User aggregate remains free of infrastructure concerns
   - Value objects enforce business rules
   - Domain events for side effects

### DDD Patterns ✅

1. **Aggregate Boundaries:**
   - User is the aggregate root
   - UserLocation, UserLanguage are value objects (owned by User)
   - CulturalInterests as primitive collection (simplicity)

2. **Ubiquitous Language:**
   - `UserLocation` (not Address) - clear user-centric term
   - `LanguageProficiency` - domain-specific enum
   - `CulturalInterests` - matches business terminology

3. **Invariant Enforcement:**
   - User validates all state changes
   - Value objects are immutable
   - Business rules encapsulated in domain methods

### TDD Strategy ✅

1. **Test Order:**
   - RED: Write failing tests for value objects
   - GREEN: Implement value objects
   - RED: Write failing tests for User methods
   - GREEN: Implement User methods
   - RED: Write failing tests for commands
   - GREEN: Implement command handlers
   - REFACTOR: Clean up duplication

2. **Test Coverage Targets:**
   - Value Objects: 100% (8-10 tests each)
   - User Entity Methods: 100% (5-8 tests per method)
   - Command Handlers: 90%+ (8-12 tests each)
   - Integration Tests: Critical paths (6-10 tests per feature)

### Security Considerations ✅

1. **Authorization:**
   - Users can only update their own profile
   - Admin endpoints for moderation (future)

2. **Image Upload Security:**
   - File size limits (5MB max)
   - Content type validation
   - File header validation (magic bytes)
   - Azure Blob Storage with private containers
   - SAS token generation for secure access

3. **Data Validation:**
   - Input sanitization in value objects
   - Length limits on all text fields
   - Enumeration protection (valid interests/languages only)

### Performance Considerations ✅

1. **Database Indexes:**
   ```sql
   -- Profile photo lookups
   CREATE INDEX idx_users_profile_photo ON users(profile_photo_url) WHERE profile_photo_url IS NOT NULL;

   -- Location-based queries
   CREATE INDEX idx_users_location ON users(city, state);

   -- Cultural interests searches (GIN for JSONB)
   CREATE INDEX idx_users_cultural_interests ON users USING GIN (cultural_interests);

   -- Language searches (GIN for JSONB)
   CREATE INDEX idx_users_languages ON users USING GIN (languages);
   ```

2. **Azure Storage:**
   - CDN integration (future)
   - Blob caching headers (1 year cache)
   - Lazy-load profile photos in lists

3. **JSON Columns:**
   - PostgreSQL JSONB for efficient querying
   - GIN indexes for fast searches
   - No N+1 query issues

---

## 4. API ENDPOINT SPECIFICATIONS

### 4.1 Profile Photo Endpoints

```http
POST /api/users/{userId}/profile-photo
Authorization: Bearer {token}
Content-Type: multipart/form-data

Request:
{
  "file": [binary image data]
}

Response 200 OK:
{
  "profilePhotoUrl": "https://storage.../profile-photos/user-123/...",
  "uploadedAt": "2025-10-30T10:30:00Z"
}

Response 400 Bad Request:
{
  "errors": ["File size exceeds 5MB limit"]
}

Response 401 Unauthorized
Response 403 Forbidden (not own profile)
```

```http
DELETE /api/users/{userId}/profile-photo
Authorization: Bearer {token}

Response 204 No Content
Response 401 Unauthorized
Response 403 Forbidden
Response 404 Not Found (no profile photo)
```

### 4.2 Location Endpoint

```http
PUT /api/users/{userId}/location
Authorization: Bearer {token}
Content-Type: application/json

Request:
{
  "city": "Los Angeles",
  "state": "California",
  "zipCode": "90001"
}

Response 200 OK:
{
  "location": {
    "city": "Los Angeles",
    "state": "California",
    "zipCode": "90001"
  }
}

Response 400 Bad Request:
{
  "errors": ["City is required"]
}
```

### 4.3 Cultural Interests Endpoint

```http
PUT /api/users/{userId}/cultural-interests
Authorization: Bearer {token}
Content-Type: application/json

Request:
{
  "interests": [
    "Sinhala Culture",
    "Buddhist Traditions",
    "Sri Lankan Cuisine"
  ]
}

Response 200 OK:
{
  "culturalInterests": [
    "Sinhala Culture",
    "Buddhist Traditions",
    "Sri Lankan Cuisine"
  ]
}

Response 400 Bad Request:
{
  "errors": ["Invalid cultural interest: XYZ"]
}
```

### 4.4 Languages Endpoint

```http
PUT /api/users/{userId}/languages
Authorization: Bearer {token}
Content-Type: application/json

Request:
{
  "languages": [
    { "language": "Sinhala", "proficiency": "Native" },
    { "language": "English", "proficiency": "Advanced" },
    { "language": "Tamil", "proficiency": "Beginner" }
  ]
}

Response 200 OK:
{
  "languages": [
    { "language": "Sinhala", "proficiency": "Native" },
    { "language": "English", "proficiency": "Advanced" },
    { "language": "Tamil", "proficiency": "Beginner" }
  ]
}

Response 400 Bad Request:
{
  "errors": ["Unsupported language: French"]
}
```

### 4.5 Complete Profile Endpoint

```http
GET /api/users/{userId}/profile
Authorization: Bearer {token}

Response 200 OK:
{
  "id": "guid",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+1234567890",
  "bio": "...",
  "profilePhotoUrl": "https://...",
  "location": {
    "city": "Los Angeles",
    "state": "California",
    "zipCode": "90001"
  },
  "culturalInterests": ["Sinhala Culture", "Buddhist Traditions"],
  "languages": [
    { "language": "Sinhala", "proficiency": "Native" },
    { "language": "English", "proficiency": "Advanced" }
  ],
  "createdAt": "2025-01-15T10:00:00Z",
  "updatedAt": "2025-10-30T12:00:00Z"
}
```

---

## 5. RISK MITIGATION

### Technical Risks

1. **Image Service Refactoring (Medium Risk)**
   - **Risk:** Breaking existing Business image uploads
   - **Mitigation:**
     - Update Business handlers first with new signature
     - Run all existing Business image tests
     - Deploy as single atomic change
     - Rollback plan: revert to businessId parameter

2. **JSON Column Performance (Low Risk)**
   - **Risk:** Slow queries on cultural interests/languages
   - **Mitigation:**
     - PostgreSQL JSONB is performant for small arrays
     - GIN indexes for fast searches
     - Monitor query performance
     - Can migrate to junction tables if needed

3. **Azure Storage Costs (Low Risk)**
   - **Risk:** Storage costs for profile photos
   - **Mitigation:**
     - 5MB size limit per photo
     - Single photo per user (no gallery)
     - Azurite for development (free)
     - Monitor production storage metrics

### Implementation Risks

1. **Scope Creep (Medium Risk)**
   - **Risk:** Adding features beyond MVP
   - **Mitigation:**
     - Follow roadmap strictly
     - Defer advanced features (image cropping, galleries, etc.)
     - Focus on core profile enhancement only

2. **Test Coverage (Low Risk)**
   - **Risk:** Insufficient test coverage
   - **Mitigation:**
     - TDD from day 1
     - Minimum 90% coverage target
     - Integration tests for critical paths
     - Zero Tolerance for compilation errors

---

## 6. IMPLEMENTATION ORDER (RECOMMENDED)

**Recommended Sequence to Minimize Risk:**

### Week 1: Phase 3.1 + 3.2 (Profile Photo + Location)

**Why This Order:**
1. Profile photo requires IImageService refactoring (foundational change)
2. Once IImageService is generic, all future images use same pattern
3. Location is simple value object (low risk)
4. Both features are independent of cultural interests/languages

**Day 1-2:** Profile Photo
**Day 3:** Location Field
**Day 4:** Integration testing + bug fixes
**Day 5:** Documentation + deployment preparation

### Week 2: Phase 3.3 (Cultural Interests + Languages)

**Why This Order:**
1. Cultural interests and languages depend on each other semantically
2. Both use JSONB storage (similar pattern)
3. Can be developed in parallel by different developers
4. API endpoints can be built together

**Day 1-2:** Domain + Application layer
**Day 3-4:** API layer + Integration tests
**Day 5:** Documentation + QA

---

## 7. TESTING STRATEGY

### Unit Tests (Domain Layer)

```csharp
// UserLocationTests.cs (8 tests minimum)
- Create_WithValidData_ShouldSucceed
- Create_WithEmptyCity_ShouldFail
- Create_WithEmptyState_ShouldFail
- Create_WithEmptyZipCode_ShouldFail
- Create_WithCityTooLong_ShouldFail
- Create_WithStateTooLong_ShouldFail
- Create_WithZipCodeTooLong_ShouldFail
- ToString_ShouldFormatCorrectly

// UserLanguageTests.cs (6 tests minimum)
- Create_WithValidData_ShouldSucceed
- Create_WithEmptyLanguage_ShouldFail
- Create_WithInvalidProficiency_ShouldFail
- Create_WithLanguageTooLong_ShouldFail
- ToString_ShouldFormatCorrectly
- Equality_ShouldWorkCorrectly

// User_ProfilePhotoTests.cs (8 tests)
- UpdateProfilePhoto_WithValidData_ShouldSucceed
- UpdateProfilePhoto_WithEmptyUrl_ShouldFail
- UpdateProfilePhoto_WithEmptyBlobName_ShouldFail
- UpdateProfilePhoto_WithUrlTooLong_ShouldFail
- UpdateProfilePhoto_ShouldRaiseDomainEvent
- RemoveProfilePhoto_WhenPhotoExists_ShouldSucceed
- RemoveProfilePhoto_WhenNoPhoto_ShouldFail
- RemoveProfilePhoto_ShouldRaiseDomainEvent

// User_LocationTests.cs (5 tests)
- UpdateLocation_WithValidLocation_ShouldSucceed
- UpdateLocation_WithNullLocation_ShouldFail
- UpdateLocation_ShouldSetLocationProperty
- UpdateLocation_ShouldMarkAsUpdated
- UpdateLocation_ShouldAllowNullLocation

// User_CulturalInterestsTests.cs (8 tests)
- UpdateCulturalInterests_WithValidList_ShouldSucceed
- UpdateCulturalInterests_WithNullList_ShouldFail
- UpdateCulturalInterests_WithTooMany_ShouldFail
- UpdateCulturalInterests_WithInvalidInterest_ShouldFail
- UpdateCulturalInterests_WithDuplicates_ShouldRemoveThem
- AddCulturalInterest_WhenValid_ShouldSucceed
- AddCulturalInterest_WhenDuplicate_ShouldFail
- RemoveCulturalInterest_WhenExists_ShouldSucceed

// User_LanguagesTests.cs (10 tests)
- UpdateLanguages_WithValidList_ShouldSucceed
- UpdateLanguages_WithNullList_ShouldFail
- UpdateLanguages_WithTooMany_ShouldFail
- UpdateLanguages_WithUnsupportedLanguage_ShouldFail
- UpdateLanguages_WithDuplicates_ShouldFail
- AddLanguage_WhenValid_ShouldSucceed
- AddLanguage_WhenDuplicate_ShouldFail
- RemoveLanguage_WhenExists_ShouldSucceed
- UpdateLanguageProficiency_WhenExists_ShouldSucceed
- UpdateLanguageProficiency_WhenNotExists_ShouldFail
```

### Integration Tests (API Layer)

```csharp
// ProfilePhotoControllerTests.cs (6 tests)
- UploadProfilePhoto_WithValidImage_ShouldReturn200
- UploadProfilePhoto_WithInvalidImage_ShouldReturn400
- UploadProfilePhoto_Unauthorized_ShouldReturn401
- UploadProfilePhoto_NotOwnProfile_ShouldReturn403
- DeleteProfilePhoto_WhenExists_ShouldReturn204
- DeleteProfilePhoto_WhenNotExists_ShouldReturn404

// LocationControllerTests.cs (4 tests)
- UpdateLocation_WithValidData_ShouldReturn200
- UpdateLocation_WithInvalidData_ShouldReturn400
- UpdateLocation_Unauthorized_ShouldReturn401
- UpdateLocation_NotOwnProfile_ShouldReturn403

// CulturalInterestsControllerTests.cs (4 tests)
- UpdateCulturalInterests_WithValidData_ShouldReturn200
- UpdateCulturalInterests_WithInvalidInterest_ShouldReturn400
- UpdateCulturalInterests_Unauthorized_ShouldReturn401
- UpdateCulturalInterests_NotOwnProfile_ShouldReturn403

// LanguagesControllerTests.cs (4 tests)
- UpdateLanguages_WithValidData_ShouldReturn200
- UpdateLanguages_WithInvalidLanguage_ShouldReturn400
- UpdateLanguages_Unauthorized_ShouldReturn401
- UpdateLanguages_NotOwnProfile_ShouldReturn403
```

---

## 8. SUMMARY OF RECOMMENDATIONS

### ✅ Final Decisions

| Component | Recommendation | Rationale |
|-----------|---------------|-----------|
| **Address Reuse** | ❌ Don't reuse - Create `UserLocation` VO | Clean domain separation, different requirements |
| **IImageService** | ✅ Refactor to generic `entityId` + `containerName` | Reusable for all image types, future-proof |
| **Profile Photo** | ✅ Store `URL` + `BlobName` (2 strings) | Simple, pragmatic, allows cleanup |
| **Cultural Interests** | ✅ String collection with predefined list + JSONB | MVP simplicity, flexible, performant |
| **Languages** | ✅ `UserLanguage` VO with proficiency + JSONB | Rich model, type-safe, simple storage |

### 📋 Implementation Checklist

**Phase 3.1: Profile Photo (2 days)**
- [ ] Refactor IImageService interface (generic entityId + containerName)
- [ ] Update BasicImageService implementation
- [ ] Update existing Business handlers
- [ ] Add UserProfilePhotoUpdatedEvent + UserProfilePhotoRemovedEvent
- [ ] Add ProfilePhotoUrl + ProfilePhotoBlobName to User
- [ ] Create UploadProfilePhotoCommand + Handler
- [ ] Create DeleteProfilePhotoCommand + Handler
- [ ] Create API endpoints (POST/DELETE /api/users/{id}/profile-photo)
- [ ] Write 25+ tests (domain + application + integration)
- [ ] Database migration

**Phase 3.2: Location (1 day)**
- [ ] Create UserLocation value object
- [ ] Add Location property to User
- [ ] Update RegisterUserCommand to accept location
- [ ] Create UpdateLocationCommand + Handler
- [ ] Create API endpoint (PUT /api/users/{id}/location)
- [ ] Write 17+ tests
- [ ] Database migration

**Phase 3.3: Cultural Interests & Languages (2 days)**
- [ ] Create UserLanguage value object
- [ ] Create LanguageProficiency enum
- [ ] Add CulturalInterests collection to User
- [ ] Add Languages collection to User
- [ ] Create UpdateCulturalInterestsCommand + Handler
- [ ] Create UpdateLanguagesCommand + Handler
- [ ] Create API endpoints (PUT endpoints)
- [ ] Write 26+ tests
- [ ] Database migration

### 🎯 Success Criteria

- [ ] All features follow Clean Architecture principles
- [ ] Zero Tolerance for compilation errors maintained
- [ ] 90%+ test coverage on new code
- [ ] All domain logic encapsulated in aggregate
- [ ] API endpoints properly authorized
- [ ] Database migrations idempotent
- [ ] Documentation complete (ADRs, API specs)
- [ ] Performance indexes in place
- [ ] Security considerations addressed

---

## 9. NEXT STEPS

1. **Review this architecture document** - Validate recommendations align with product vision
2. **Approve implementation plan** - Confirm 5-day timeline is acceptable
3. **Prioritize phases** - Confirm order (photo → location → cultural/languages)
4. **Begin Phase 3.1** - Start with IImageService refactoring (foundational change)
5. **Follow TDD rigorously** - Write tests first, maintain Zero Tolerance
6. **Deploy incrementally** - Deploy each phase separately to production

---

**Document End**

This architecture provides a clear, actionable path forward for Epic 1 Phase 3. All recommendations are grounded in your existing codebase patterns, follow Clean Architecture and DDD principles, and minimize risk through incremental delivery.
