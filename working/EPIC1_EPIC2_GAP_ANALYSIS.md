# EPIC 1 & EPIC 2 GAP ANALYSIS
## Full Implementation Strategy for Go-Live

**Document Version**: 1.0
**Date**: 2025-10-28
**Status**: Gap Analysis Complete
**Strategy**: FULL Epic 1 + FULL Epic 2 + Web UI Only

---

## 🎯 IMPLEMENTATION STRATEGY

### Strategic Decisions (Confirmed by User)
- ✅ **Azure AD B2C** for enterprise authentication (NOT custom PostgreSQL auth)
- ✅ **FULL Epic 1** implementation (3 weeks) - social login, profile photos, cultural interests included
- ✅ **FULL Epic 2** implementation (4 weeks) - complete event discovery and management
- ✅ **Web UI ONLY** - mobile version deferred to incremental release
- ✅ **PostgreSQL** stores: Azure AD user ID reference + profile data (name, location, preferences)
- ✅ **Azure subscription** will be provided when ready

---

## 📊 EPIC 1: AUTHENTICATION & USER MANAGEMENT

### ✅ COMPLETED (What Exists)

#### **Backend - Domain Layer**
- ✅ `User` aggregate (src/LankaConnect.Domain/Users/User.cs)
  - Email, FirstName, LastName, PhoneNumber, Bio
  - PasswordHash (will be refactored for Azure AD)
  - IsEmailVerified, EmailVerificationToken
  - FailedLoginAttempts, AccountLockedUntil
  - RefreshTokens collection

- ✅ Value Objects:
  - `Email` (validation, normalization)
  - `PhoneNumber` (international format validation)

- ✅ Domain Events:
  - UserCreatedEvent
  - UserEmailVerifiedEvent
  - UserLoggedInEvent
  - UserPasswordChangedEvent
  - UserRoleChangedEvent
  - UserAccountLockedEvent

#### **Backend - Application Layer**
- ✅ `RegisterUserCommand` + Handler (basic email/password registration)
- ✅ `LoginUserCommand` + Handler
- ✅ `RefreshTokenCommand` + Handler
- ✅ `CreateUserCommand` + Handler
- ✅ `GetUserByIdQuery` + Handler

#### **Backend - API Layer**
- ✅ `AuthController` (src/LankaConnect.API/Controllers/AuthController.cs)
  - POST /api/auth/register
  - POST /api/auth/login
  - POST /api/auth/refresh-token

- ✅ `UsersController` (basic CRUD - CreateUser, GetUserById)

---

### ❌ MISSING (What Needs Building)

#### **1. Azure AD B2C Infrastructure** ⚠️ **HIGH PRIORITY - FOUNDATIONAL**

**Missing Components:**
```csharp
// Infrastructure setup
- ❌ Azure AD B2C tenant configuration
- ❌ OAuth 2.0 / OpenID Connect integration
- ❌ Microsoft.Identity.Web NuGet package
- ❌ JWT token validation with Azure AD B2C
- ❌ User flow configuration (sign-up, sign-in, password reset)
```

**Required Configuration Files:**
```json
// appsettings.json
{
  "AzureAdB2C": {
    "Instance": "https://{tenant-name}.b2clogin.com",
    "Domain": "{tenant-name}.onmicrosoft.com",
    "ClientId": "{client-id}",
    "SignUpSignInPolicyId": "B2C_1_signupsignin",
    "ResetPasswordPolicyId": "B2C_1_passwordreset",
    "EditProfilePolicyId": "B2C_1_profileediting"
  }
}
```

**Required Infrastructure Classes:**
- `AzureAdB2COptions.cs` - Configuration model
- `AzureAdB2CService.cs` - Service for managing users via Graph API
- `JwtTokenValidator.cs` - Validate tokens from Azure AD B2C
- Program.cs updates for AddMicrosoftIdentityWebApi()

**Database Schema Changes:**
```sql
-- User table refactoring
ALTER TABLE users ADD COLUMN azure_ad_b2c_user_id VARCHAR(255) UNIQUE;
ALTER TABLE users DROP COLUMN password_hash; -- Move to Azure AD
CREATE INDEX idx_users_azure_id ON users(azure_ad_b2c_user_id);
```

**Estimated Time**: 1 week (5 development sessions)

---

#### **2. Social Login (OAuth Providers)** ⚠️ **HIGH PRIORITY**

**Missing Features:**
- ❌ Facebook OAuth integration
- ❌ Google OAuth integration
- ❌ Apple Sign-In integration

**Required Implementation:**
```csharp
// Azure AD B2C configuration (in B2C portal)
- Facebook Identity Provider setup
- Google Identity Provider setup
- Apple Identity Provider setup

// Backend handling
- ❌ ExternalLoginCommand + Handler
- ❌ LinkExternalLoginCommand + Handler (link social account to existing user)
- ❌ UnlinkExternalLoginCommand + Handler
```

**API Endpoints to Add:**
```
POST /api/auth/external-login/{provider}
POST /api/auth/link-external-login
POST /api/auth/unlink-external-login/{provider}
GET  /api/auth/external-providers (list available providers)
```

**Estimated Time**: 3 days (OAuth flows already handled by Azure AD B2C)

---

#### **3. Profile Photo Upload** ⚠️ **MEDIUM PRIORITY**

**What Exists:**
- ✅ `BasicImageService` (src/LankaConnect.Infrastructure/Storage/Services/BasicImageService.cs)
- ✅ Azure Blob Storage integration
- ✅ Image validation, upload, delete, secure URL generation

**Missing Components:**
```csharp
// Domain Layer
- ❌ Add ProfilePhotoUrl property to User entity
- ❌ Add UpdateProfilePhoto() method to User entity
- ❌ Add RemoveProfilePhoto() method to User entity

// Application Layer
- ❌ UploadProfilePhotoCommand + Handler
- ❌ DeleteProfilePhotoCommand + Handler

// API Layer
- ❌ POST /api/users/{id}/profile-photo (multipart/form-data)
- ❌ DELETE /api/users/{id}/profile-photo
```

**Database Schema:**
```sql
ALTER TABLE users ADD COLUMN profile_photo_url VARCHAR(500);
ALTER TABLE users ADD COLUMN profile_photo_blob_name VARCHAR(255);
```

**Estimated Time**: 2 days (image service already exists)

---

#### **4. Location Field (City, State, ZIP)** ⚠️ **HIGH PRIORITY**

**Current State:**
- ❌ User entity has no location field
- ❌ RegisterUserCommand does not accept location

**Required Changes:**
```csharp
// Domain Layer - Add to User.cs
public UserLocation? Location { get; private set; }

// Value Object
public class UserLocation : ValueObject
{
    public string City { get; }
    public string State { get; }
    public string ZipCode { get; }

    public static Result<UserLocation> Create(string city, string state, string zipCode);
}

// Application Layer - Update RegisterUserCommand
public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string City,
    string State,
    string ZipCode,
    UserRole Role = UserRole.User) : IRequest<Result<RegisterUserResponse>>;

// Update UpdateUserProfileCommand
```

**Database Schema:**
```sql
ALTER TABLE users ADD COLUMN city VARCHAR(100);
ALTER TABLE users ADD COLUMN state VARCHAR(100);
ALTER TABLE users ADD COLUMN zip_code VARCHAR(20);
CREATE INDEX idx_users_location ON users(city, state);
```

**API Endpoints:**
```
POST /api/auth/register (add city, state, zipCode fields)
PUT  /api/users/{id}/location
```

**Estimated Time**: 1 day

---

#### **5. Cultural Interests & Language Preferences** ⚠️ **MEDIUM PRIORITY**

**Missing Components:**
```csharp
// Domain Layer
public class User : BaseEntity
{
    private readonly List<string> _culturalInterests = new();
    public IReadOnlyList<string> CulturalInterests => _culturalInterests.AsReadOnly();

    private readonly List<string> _languages = new();
    public IReadOnlyList<string> Languages => _languages.AsReadOnly();

    public Result AddCulturalInterest(string interest);
    public Result RemoveCulturalInterest(string interest);
    public Result AddLanguage(string language);
    public Result RemoveLanguage(string language);
}

// Application Layer
- ❌ UpdateCulturalInterestsCommand + Handler
- ❌ UpdateLanguagePreferencesCommand + Handler
```

**Database Schema:**
```sql
-- Junction tables for many-to-many
CREATE TABLE user_cultural_interests (
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    interest VARCHAR(100) NOT NULL,
    added_at TIMESTAMPTZ DEFAULT NOW(),
    PRIMARY KEY (user_id, interest)
);

CREATE TABLE user_languages (
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    language VARCHAR(50) NOT NULL,
    proficiency VARCHAR(20), -- Basic, Intermediate, Fluent, Native
    added_at TIMESTAMPTZ DEFAULT NOW(),
    PRIMARY KEY (user_id, language)
);
```

**API Endpoints:**
```
PUT /api/users/{id}/cultural-interests
PUT /api/users/{id}/languages
```

**Estimated Time**: 2 days

---

#### **6. Email Verification Enhancements** ⚠️ **LOW PRIORITY**

**Current State:**
- ✅ Email verification token generation exists
- ⚠️ Email sending logic exists but needs Azure AD B2C integration

**Missing:**
- ❌ Azure Communication Services integration for production emails
- ❌ Email templates (HTML/CSS for professional look)
- ❌ ResendVerificationEmailCommand

**Estimated Time**: 1 day (deferred to Phase 1.1)

---

### 📊 Epic 1 Summary

| Component | Status | Estimated Time |
|-----------|--------|---------------|
| Azure AD B2C Infrastructure | ❌ Missing | 1 week |
| Social Login (3 providers) | ❌ Missing | 3 days |
| Profile Photo Upload | ❌ Missing | 2 days |
| Location Field | ❌ Missing | 1 day |
| Cultural Interests | ❌ Missing | 2 days |
| Language Preferences | ❌ Missing | (included above) |
| Email Verification | ⚠️ Partial | 1 day |

**Total Epic 1 Gap**: ~2.5 weeks (10 development sessions @ 4 hours each)

---

## 📊 EPIC 2: EVENT DISCOVERY & MANAGEMENT

### ✅ COMPLETED (What Exists)

#### **Backend - Domain Layer**
- ✅ `Event` aggregate (src/LankaConnect.Domain/Events/Event.cs)
  - EventTitle, EventDescription (value objects)
  - StartDate, EndDate, OrganizerId, Capacity
  - EventStatus (Draft, UnderReview, Published, Active, Cancelled, Postponed, Completed, Archived)
  - Registrations collection (RSVP functionality)
  - Methods: Publish(), Cancel(), Register(), CancelRegistration(), Complete(), ActivateEvent(), Postpone(), Archive(), SubmitForReview(), UpdateCapacity()

- ✅ `Registration` aggregate (event RSVP)
  - EventId, UserId, Quantity
  - RegistrationStatus (Confirmed, Cancelled)

- ✅ Value Objects:
  - `EventTitle` (validation)
  - `EventDescription` (validation)
  - `EventCategory` enum (Religious, Cultural, Community, Educational, Social, Business, Charity, Entertainment)
  - `EventType` enum
  - `GeographicLocation`, `GeographicRegion`

- ✅ Domain Events:
  - EventPublishedEvent
  - EventCancelledEvent
  - EventActivatedEvent
  - EventPostponedEvent
  - EventArchivedEvent
  - EventCapacityUpdatedEvent
  - EventSubmittedForReviewEvent
  - RegistrationConfirmedEvent
  - RegistrationCancelledEvent

- ✅ Domain Services:
  - `CulturalCalendar` (Buddhist/Hindu holidays - static data)
  - `EventRecommendationEngine` (scoring algorithm for personalized recommendations)

#### **Backend - Application Layer (Partial)**
- ✅ `GetCulturallyAppropriateEventsQuery` + Handler (cultural filtering)
- ✅ `GetEventRecommendationsQuery` + Handler (personalized recommendations)

---

### ❌ MISSING (What Needs Building)

#### **1. Event Location with PostGIS** ⚠️ **HIGH PRIORITY - FOUNDATIONAL**

**Current State:**
- ❌ Event entity has NO location/address properties
- ❌ NO PostGIS geographic queries
- ❌ NO distance-based filtering (25/50/100 mile radius)

**Required Changes:**
```csharp
// Domain Layer - Add to Event.cs
public EventLocation Location { get; private set; }

// Value Object
public class EventLocation : ValueObject
{
    public Address Address { get; }
    public GeoCoordinate Coordinates { get; }

    public static Result<EventLocation> Create(Address address, GeoCoordinate coordinates);
}

// Address Value Object (may already exist in Business domain)
public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string ZipCode { get; }
    public string Country { get; }
}

// GeoCoordinate Value Object
public class GeoCoordinate : ValueObject
{
    public decimal Latitude { get; }
    public decimal Longitude { get; }

    public static Result<GeoCoordinate> Create(decimal latitude, decimal longitude);
    public double DistanceFrom(GeoCoordinate other); // Haversine formula
}
```

**Database Schema:**
```sql
-- Enable PostGIS extension
CREATE EXTENSION IF NOT EXISTS postgis;

-- Add location columns to events table
ALTER TABLE events ADD COLUMN street VARCHAR(200);
ALTER TABLE events ADD COLUMN city VARCHAR(100);
ALTER TABLE events ADD COLUMN state VARCHAR(100);
ALTER TABLE events ADD COLUMN zip_code VARCHAR(20);
ALTER TABLE events ADD COLUMN country VARCHAR(100);
ALTER TABLE events ADD COLUMN coordinates GEOGRAPHY(POINT, 4326);

-- Create spatial index
CREATE INDEX idx_events_coordinates ON events USING GIST(coordinates);
CREATE INDEX idx_events_location ON events(city, state);
```

**Repository Methods:**
```csharp
public interface IEventRepository
{
    Task<List<Event>> GetEventsByLocationAsync(decimal latitude, decimal longitude, int radiusMiles);
    Task<List<Event>> GetEventsByCityAsync(string city, string state);
}
```

**Estimated Time**: 3 days (PostGIS setup + domain changes + repository)

---

#### **2. Event Category Integration** ⚠️ **HIGH PRIORITY**

**Current State:**
- ✅ `EventCategory` enum EXISTS (Religious, Cultural, Community, Educational, Social, Business, Charity, Entertainment)
- ❌ Event entity does NOT have Category property

**Required Changes:**
```csharp
// Domain Layer - Add to Event.cs
public EventCategory Category { get; private set; }

// Update Create() factory method
public static Result<Event> Create(
    EventTitle title,
    EventDescription description,
    DateTime startDate,
    DateTime endDate,
    Guid organizerId,
    int capacity,
    EventCategory category,  // ADD THIS
    EventLocation location)  // ADD THIS
```

**Database Schema:**
```sql
ALTER TABLE events ADD COLUMN category VARCHAR(50) NOT NULL DEFAULT 'Community';
CREATE INDEX idx_events_category ON events(category);
```

**Estimated Time**: 0.5 days (simple addition)

---

#### **3. Ticket Pricing (Money Value Object)** ⚠️ **MEDIUM PRIORITY**

**Current State:**
- ❌ Event entity has NO pricing information
- ❌ NO Money value object for Event domain

**Required Changes:**
```csharp
// Domain Layer - Add to Event.cs
public Money? TicketPrice { get; private set; } // Nullable for free events

// Money Value Object (shared across domains)
public class Money : ValueObject
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    public static Result<Money> Create(decimal amount, Currency currency);
    public Money Add(Money other);
    public Money Subtract(Money other);
}

public enum Currency
{
    USD,
    LKR // Sri Lankan Rupee
}
```

**Database Schema:**
```sql
ALTER TABLE events ADD COLUMN ticket_price DECIMAL(10, 2);
ALTER TABLE events ADD COLUMN currency VARCHAR(3) DEFAULT 'USD';
CREATE INDEX idx_events_price ON events(ticket_price);
```

**Estimated Time**: 1 day

---

#### **4. Event Images (Azure Blob Storage)** ⚠️ **MEDIUM PRIORITY**

**What Exists:**
- ✅ `BasicImageService` (upload/delete/validate images)

**Missing Components:**
```csharp
// Domain Layer - Add to Event.cs
private readonly List<EventImage> _images = new();
public IReadOnlyList<EventImage> Images => _images.AsReadOnly();

public Result AddImage(string imageUrl, string blobName, int displayOrder);
public Result RemoveImage(Guid imageId);

// EventImage Entity
public class EventImage : BaseEntity
{
    public Guid EventId { get; }
    public string ImageUrl { get; }
    public string BlobName { get; }
    public int DisplayOrder { get; }
    public DateTime UploadedAt { get; }
}

// Application Layer
- ❌ UploadEventImageCommand + Handler
- ❌ DeleteEventImageCommand + Handler
- ❌ ReorderEventImagesCommand + Handler
```

**Database Schema:**
```sql
CREATE TABLE event_images (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    image_url VARCHAR(500) NOT NULL,
    blob_name VARCHAR(255) NOT NULL,
    display_order INT NOT NULL DEFAULT 0,
    uploaded_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_event_images_event_id ON event_images(event_id);
CREATE INDEX idx_event_images_display_order ON event_images(event_id, display_order);
```

**API Endpoints:**
```
POST /api/events/{id}/images (multipart/form-data)
DELETE /api/events/{eventId}/images/{imageId}
PUT /api/events/{id}/images/reorder
```

**Estimated Time**: 2 days

---

#### **5. Events Application Layer (CRUD Commands)** ⚠️ **HIGH PRIORITY - BLOCKING**

**Current State:**
- ✅ Only 2 queries exist (GetCulturallyAppropriateEvents, GetEventRecommendations)
- ❌ NO Commands for event creation, update, delete, publish, cancel

**Missing Commands:**
```csharp
// Create
- ❌ CreateEventCommand + Handler
- ❌ SubmitEventForApprovalCommand + Handler

// Update
- ❌ UpdateEventCommand + Handler
- ❌ UpdateEventCapacityCommand + Handler
- ❌ UpdateEventLocationCommand + Handler

// Status Changes
- ❌ PublishEventCommand + Handler
- ❌ CancelEventCommand + Handler
- ❌ PostponeEventCommand + Handler
- ❌ ArchiveEventCommand + Handler

// RSVP
- ❌ RsvpToEventCommand + Handler
- ❌ CancelRsvpCommand + Handler
- ❌ UpdateRsvpCommand + Handler

// Delete
- ❌ DeleteEventCommand + Handler
```

**Missing Queries:**
```csharp
// Basic Queries
- ❌ GetEventByIdQuery + Handler
- ❌ GetEventsQuery + Handler (with filters: location, category, date range, price range)
- ❌ GetEventsByOrganizerQuery + Handler

// User Queries
- ❌ GetUserRsvpsQuery + Handler (user dashboard)
- ❌ GetUpcomingEventsForUserQuery + Handler

// Admin Queries
- ❌ GetPendingEventsForApprovalQuery + Handler
```

**Estimated Time**: 1.5 weeks (12 development sessions)

---

#### **6. EventsController API** ⚠️ **HIGH PRIORITY - BLOCKING**

**Current State:**
- ❌ NO EventsController exists

**Required Endpoints:**
```csharp
// Public Endpoints (No Auth)
GET    /api/events                    // Search/filter events
GET    /api/events/{id}                // Event details

// Authenticated Endpoints
POST   /api/events                    // Create event (organizers only)
PUT    /api/events/{id}                // Update event
DELETE /api/events/{id}                // Delete event
POST   /api/events/{id}/submit        // Submit for approval
POST   /api/events/{id}/publish       // Publish event (organizer)
POST   /api/events/{id}/cancel        // Cancel event
POST   /api/events/{id}/postpone      // Postpone event

// RSVP Endpoints
POST   /api/events/{id}/rsvp          // RSVP to event
DELETE /api/events/{id}/rsvp          // Cancel RSVP
GET    /api/events/my-rsvps           // User dashboard

// Calendar Export
GET    /api/events/{id}/ics           // ICS calendar export

// Admin Endpoints
GET    /api/admin/events/pending      // Approval queue
POST   /api/admin/events/{id}/approve // Approve event
POST   /api/admin/events/{id}/reject  // Reject event
```

**Estimated Time**: 1 week (8 development sessions)

---

#### **7. Email Notifications (RSVP Confirmations)** ⚠️ **MEDIUM PRIORITY**

**Current State:**
- ✅ Email infrastructure exists (MailHog for testing)
- ❌ NO RSVP confirmation emails

**Missing Components:**
```csharp
// Domain Event Handlers
- ❌ RegistrationConfirmedEventHandler → Send confirmation email
- ❌ RegistrationCancelledEventHandler → Send cancellation email
- ❌ EventCancelledEventHandler → Notify all attendees

// Email Templates
- ❌ RsvpConfirmationEmail.html
- ❌ RsvpCancellationEmail.html
- ❌ EventCancelledEmail.html
- ❌ EventPostponedEmail.html
```

**Estimated Time**: 2 days

---

#### **8. Hangfire Background Jobs (Reminders)** ⚠️ **MEDIUM PRIORITY**

**Current State:**
- ❌ NO Hangfire integration
- ❌ NO background job infrastructure

**Required Setup:**
```csharp
// Infrastructure - Install NuGet packages
- Hangfire.AspNetCore
- Hangfire.PostgreSql

// Configuration in Program.cs
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(connectionString));
builder.Services.AddHangfireServer();

// Background Jobs
public class EventReminderJob : IRecurringJob
{
    // Run every hour
    // Find events starting in 24 hours
    // Send reminder emails to attendees
}

public class EventStatusUpdateJob : IRecurringJob
{
    // Run every hour
    // Mark events as Active if StartDate is now
    // Mark events as Completed if EndDate has passed
}
```

**Database Schema:**
```sql
-- Hangfire creates its own tables automatically
-- No manual schema changes needed
```

**Estimated Time**: 2 days

---

#### **9. SignalR Real-Time Updates** ⚠️ **LOW PRIORITY**

**Current State:**
- ❌ NO SignalR integration

**Required Setup:**
```csharp
// Hubs
public class EventHub : Hub
{
    public async Task NotifyRsvpCountUpdate(Guid eventId, int newCount)
    {
        await Clients.Group($"event_{eventId}").SendAsync("RsvpCountUpdated", newCount);
    }
}

// Domain Event Handlers
- RegistrationConfirmedEventHandler → Broadcast to SignalR
- RegistrationCancelledEventHandler → Broadcast to SignalR

// API Integration
app.MapHub<EventHub>("/hubs/events");
```

**Estimated Time**: 1 day (deferred to Phase 1.1)

---

#### **10. ICS Calendar Export** ⚠️ **LOW PRIORITY**

**Missing Components:**
```csharp
// Application Service
public class IcsCalendarService : IIcsCalendarService
{
    public string GenerateIcsFile(Event @event);
}

// API Endpoint (already listed above)
GET /api/events/{id}/ics
```

**Estimated Time**: 0.5 days

---

#### **11. Admin Approval Workflow** ⚠️ **MEDIUM PRIORITY**

**Current State:**
- ✅ Event.SubmitForReview() method exists
- ❌ NO admin approval commands

**Missing Components:**
```csharp
// Application Layer
- ❌ ApproveEventCommand + Handler
- ❌ RejectEventCommand + Handler

// API Endpoints (already listed above)
POST /api/admin/events/{id}/approve
POST /api/admin/events/{id}/reject
```

**Estimated Time**: 1 day

---

### 📊 Epic 2 Summary

| Component | Status | Estimated Time |
|-----------|--------|---------------|
| Event Location (PostGIS) | ❌ Missing | 3 days |
| Event Category Integration | ❌ Missing | 0.5 days |
| Ticket Pricing (Money VO) | ❌ Missing | 1 day |
| Event Images | ❌ Missing | 2 days |
| Application Layer (Commands/Queries) | ❌ Missing | 1.5 weeks |
| EventsController API | ❌ Missing | 1 week |
| RSVP Email Notifications | ❌ Missing | 2 days |
| Hangfire Background Jobs | ❌ Missing | 2 days |
| SignalR Real-Time Updates | ❌ Missing | 1 day (deferred) |
| ICS Calendar Export | ❌ Missing | 0.5 days |
| Admin Approval Workflow | ❌ Missing | 1 day |

**Total Epic 2 Gap**: ~4 weeks (16 development sessions @ 4 hours each)

---

## 🎨 FRONTEND (WEB UI)

### Current State
- ❌ NO frontend directory found in codebase
- ❌ Assumed: Next.js or React frontend needs to be built from scratch

### Required Frontend Features

#### **Epic 1 - Authentication UI**
```
Pages:
- /register - Registration form (email, password, name, location, cultural interests, languages)
- /login - Login form (email/password + social login buttons)
- /verify-email - Email verification landing page
- /forgot-password - Password reset request
- /reset-password - Password reset form
- /profile - User profile management (photo upload, interests, languages)
```

#### **Epic 2 - Event Management UI**
```
Pages:
- / (Home) - Event discovery with filters (location, category, date, price)
- /events/{id} - Event details page (images, location map, RSVP button)
- /events/create - Event creation form (organizers only)
- /events/my-events - User dashboard (RSVPs, organized events)
- /events/{id}/edit - Edit event form
- /admin/events/pending - Admin approval queue
```

#### **Shared Components**
```
- Map component (Azure Maps or Google Maps integration)
- Image uploader (drag-drop, preview)
- Date/time picker
- Location autocomplete (city, state, ZIP)
- Real-time RSVP counter (SignalR)
- Cultural interest selector (multi-select)
- Language selector (multi-select)
```

**Estimated Time**: 3-4 weeks (depends on design complexity)
**Note**: Frontend analysis deferred until backend APIs are ready

---

## 📋 DATABASE SCHEMA CHANGES SUMMARY

### New Tables Required:
```sql
-- Epic 1
CREATE TABLE user_cultural_interests (...);
CREATE TABLE user_languages (...);

-- Epic 2
CREATE TABLE event_images (...);

-- Hangfire (auto-created)
CREATE TABLE hangfire.job (...);
CREATE TABLE hangfire.state (...);
```

### Columns to Add:
```sql
-- users table
ALTER TABLE users ADD COLUMN azure_ad_b2c_user_id VARCHAR(255) UNIQUE;
ALTER TABLE users DROP COLUMN password_hash;
ALTER TABLE users ADD COLUMN profile_photo_url VARCHAR(500);
ALTER TABLE users ADD COLUMN profile_photo_blob_name VARCHAR(255);
ALTER TABLE users ADD COLUMN city VARCHAR(100);
ALTER TABLE users ADD COLUMN state VARCHAR(100);
ALTER TABLE users ADD COLUMN zip_code VARCHAR(20);

-- events table
ALTER TABLE events ADD COLUMN category VARCHAR(50) NOT NULL;
ALTER TABLE events ADD COLUMN street VARCHAR(200);
ALTER TABLE events ADD COLUMN city VARCHAR(100);
ALTER TABLE events ADD COLUMN state VARCHAR(100);
ALTER TABLE events ADD COLUMN zip_code VARCHAR(20);
ALTER TABLE events ADD COLUMN country VARCHAR(100);
ALTER TABLE events ADD COLUMN coordinates GEOGRAPHY(POINT, 4326);
ALTER TABLE events ADD COLUMN ticket_price DECIMAL(10, 2);
ALTER TABLE events ADD COLUMN currency VARCHAR(3) DEFAULT 'USD';
```

### Indexes to Create:
```sql
CREATE INDEX idx_users_azure_id ON users(azure_ad_b2c_user_id);
CREATE INDEX idx_users_location ON users(city, state);
CREATE INDEX idx_events_category ON events(category);
CREATE INDEX idx_events_coordinates ON events USING GIST(coordinates);
CREATE INDEX idx_events_location ON events(city, state);
CREATE INDEX idx_events_price ON events(ticket_price);
CREATE INDEX idx_event_images_event_id ON event_images(event_id);
```

---

## 🚀 IMPLEMENTATION PRIORITY ORDER

### Phase 1: Infrastructure (Week 1)
1. ✅ Fix database schema - Version column error
2. ⚠️ Setup Azure AD B2C infrastructure (BLOCKING)
3. ⚠️ Add Location, Category, Pricing to Event domain
4. ⚠️ Setup PostGIS extension and Event location

### Phase 2: Epic 1 Core (Week 2)
1. ⚠️ Refactor User entity for Azure AD B2C
2. ⚠️ Add Location field to User
3. ⚠️ Implement social login (Facebook, Google, Apple)
4. ⚠️ Add profile photo upload
5. ⚠️ Add cultural interests & languages

### Phase 3: Epic 2 Domain (Week 3)
1. ⚠️ Complete Event entity enhancements (location, category, price, images)
2. ⚠️ Build Event Application layer (Commands/Queries)
3. ⚠️ Build EventsController API
4. ⚠️ Implement event image upload

### Phase 4: Epic 2 Advanced Features (Week 4)
1. ⚠️ RSVP email notifications
2. ⚠️ Setup Hangfire background jobs
3. ⚠️ Admin approval workflow
4. ⚠️ ICS calendar export
5. ⚠️ (Optional) SignalR real-time updates

### Phase 5: Frontend (Weeks 5-7)
1. ⚠️ Build authentication UI (register, login, profile)
2. ⚠️ Build event discovery UI (search, filters)
3. ⚠️ Build event management UI (create, edit, RSVP)
4. ⚠️ Build admin UI (approval queue)

### Phase 6: Testing & Deployment (Week 8)
1. ⚠️ Integration tests for all new features
2. ⚠️ E2E tests for critical paths
3. ⚠️ Load testing (100 concurrent users)
4. ⚠️ Azure deployment configuration
5. ⚠️ Production database migration scripts

---

## 📈 TIME ESTIMATES

| Phase | Epic | Duration | Sessions | Status |
|-------|------|----------|----------|--------|
| Infrastructure | - | 1 week | 4 | ⚠️ Pending |
| Epic 1 Core | Epic 1 | 2 weeks | 8 | ⚠️ Pending |
| Epic 2 Domain | Epic 2 | 2 weeks | 8 | ⚠️ Pending |
| Epic 2 Advanced | Epic 2 | 2 weeks | 8 | ⚠️ Pending |
| Frontend (Web) | Both | 3 weeks | 12 | ⚠️ Pending |
| Testing & Deploy | - | 1 week | 4 | ⚠️ Pending |
| **TOTAL** | - | **11 weeks** | **44 sessions** | - |

**Note**: Original estimate was 6 weeks (Epic 1 minimal + Epic 2). With FULL Epic 1 + Frontend, it's now ~11 weeks for complete go-live.

---

## ✅ NEXT IMMEDIATE ACTIONS

1. **Fix database schema error** (Version column) - **IN PROGRESS**
2. **Setup Azure AD B2C tenant** (requires Azure subscription) - **BLOCKED (waiting for credentials)**
3. **Extend Event domain** with location, category, pricing - **READY TO START**
4. **Build Events Application layer** (Commands/Queries) - **READY TO START**
5. **Build EventsController API** - **READY TO START**

---

**Document End**

**Status**: Gap analysis complete. Ready to begin implementation immediately.
