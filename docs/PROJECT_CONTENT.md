# PROJECT_CONTENT.md - LankaConnect Complete Project Context
## Essential Information for Claude Code Agents

**Last Updated:** 2025-11-01
**Project Phase:** Phase 1 Development - Epic 1 Phase 3 Complete ✅
**Architecture:** Clean Architecture + DDD + CQRS
**Stack:** .NET 8, PostgreSQL, Redis, Azure
**Test Coverage:** 495/495 tests passing (100%)
**Deployment:** Azure Container Apps (Staging environment fully functional)

---

## 🎉 Epic 1 Phase 3 Implementation Status (2025-11-01)

**Status:** ✅ COMPLETE & DEPLOYED TO STAGING

**Features Implemented:**
1. **Profile Photo Upload/Delete** - Azure Blob Storage integration with 5MB limit
2. **Location Field** - Privacy-first city-level location (UserLocation value object)
3. **Cultural Interests** - 20 pre-defined interests, 0-10 per user (optional)
4. **Languages** - 20 languages with ISO 639 codes, 1-5 per user with proficiency levels
5. **GET Endpoint Fix** - EF Core OwnsMany collections properly configured

**Technical Achievements:**
- 495/495 Application.Tests passing (100%)
- 4 database migrations applied to staging
- 6 new API endpoints (PUT methods)
- Junction tables: user_cultural_interests, user_languages
- Zero Tolerance TDD maintained throughout
- Deployed to Azure staging environment
- Migration applied and verified working

**Architecture Patterns Used:**
- Enumeration Pattern (type-safe value objects)
- EF Core OwnsMany (junction tables for owned entities)
- CQRS (MediatR commands/handlers)
- Domain Events (CulturalInterestsUpdatedEvent, LanguagesUpdatedEvent)
- Value Objects (CulturalInterest, LanguageCode, LanguagePreference, UserLocation)

---

## 1. Project Overview

### 1.1 What is LankaConnect?
LankaConnect is a comprehensive digital platform connecting the Sri Lankan community worldwide through events, forums, business directory, and educational resources. The platform serves both local residents and the diaspora, providing a unified hub for community engagement, cultural preservation, and economic opportunities.

### 1.2 Business Goals
- **Connect:** Unite Sri Lankans globally through digital engagement
- **Preserve:** Maintain cultural heritage and language
- **Empower:** Support local businesses and service providers
- **Educate:** Provide learning resources and skill development
- **Grow:** Foster economic opportunities and community growth

### 1.3 Target Users
1. **Local Residents:** Access local services, events, and businesses
2. **Diaspora:** Stay connected with homeland, culture, and community
3. **Business Owners:** Promote services and connect with customers
4. **Event Organizers:** Manage and promote cultural/community events
5. **Students/Educators:** Access educational resources and courses

---

## 2. Technical Architecture Summary

### 2.1 Architecture Pattern
```
Clean Architecture + Domain-Driven Design (DDD) + CQRS

Layers:
┌─────────────────────────────────────┐
│          Presentation               │ ← Blazor Web, API Controllers
├─────────────────────────────────────┤
│          Application                │ ← Use Cases, DTOs, CQRS
├─────────────────────────────────────┤
│            Domain                   │ ← Entities, Value Objects, Events
├─────────────────────────────────────┤
│         Infrastructure              │ ← EF Core, Azure, External Services
└─────────────────────────────────────┘
```

### 2.2 Technology Stack
- **Backend:** .NET 8, C# 12
- **API:** ASP.NET Core Web API, REST + SignalR
- **Database:** PostgreSQL 15 with PostGIS
- **Caching:** Redis 7
- **Frontend:** Blazor WebAssembly
- **Mobile:** React Native (Phase 2)
- **Cloud:** Azure (Container Apps, Service Bus, Blob Storage)
- **Auth:** Azure AD B2C
- **Search:** Azure Cognitive Search
- **AI/ML:** Azure Cognitive Services

### 2.3 Key Patterns & Practices
- **DDD Bounded Contexts:** Identity, Events, Community, Business, Education
- **CQRS with MediatR:** Command/Query separation
- **Repository Pattern:** Data access abstraction
- **Unit of Work:** Transaction management
- **Domain Events:** Event-driven architecture
- **TDD:** Test-Driven Development
- **CI/CD:** GitHub Actions + Azure DevOps

---

## 3. Domain Model Overview

### 3.1 Bounded Contexts

#### Identity Context
```csharp
Aggregates:
- User (root)
  - UserProfile
  - UserPreferences
  - UserRole
  
Value Objects:
- Email
- PersonName
- PhoneNumber
- Address
```

#### Events Context
```csharp
Aggregates:
- Event (root)
  - EventSchedule
  - EventLocation
  - TicketType
  - Registration
  
Value Objects:
- Title
- Description
- DateTimeRange
- Money
- Location
```

#### Community Context
```csharp
Aggregates:
- Forum (root)
  - ForumCategory
- Topic (root)
  - Post
  - Reply
- Member (root)
  
Value Objects:
- ForumTitle
- PostContent
- Reaction
```

#### Business Context
```csharp
Aggregates:
- Business (root)
  - Service
  - OpeningHours
- Booking (root)
  - BookingItem
- Review (root)
  
Value Objects:
- BusinessName
- ServiceDescription
- Rating
- TimeSlot
```

### 3.2 Domain Events
```csharp
Key Events:
- UserRegisteredEvent
- ProfileCompletedEvent
- EventCreatedEvent
- EventPublishedEvent
- RegistrationConfirmedEvent
- TopicCreatedEvent
- PostPublishedEvent
- BusinessVerifiedEvent
- BookingConfirmedEvent
- ReviewSubmittedEvent
```

---

## 4. Core Features (Phase 1)

### 4.1 Identity & Access Management
- **User Registration:** Email/phone with verification
- **Authentication:** JWT tokens, refresh tokens
- **Social Login:** Google, Facebook, Microsoft
- **Profile Management:** Multi-language profiles
- **Role-Based Access:** Admin, Moderator, BusinessOwner, User

### 4.2 Event Management
- **Event Creation:** Rich editor, image uploads
- **Categories:** Cultural, Religious, Sports, Education, Social
- **Search & Filter:** Location, date, category, price
- **Registration:** Online registration with capacity management
- **Calendar Integration:** ICS export, reminders

### 4.3 Community Forums
- **Structured Forums:** Categories and subcategories
- **Rich Discussions:** Markdown, images, mentions
- **Moderation:** Reporting, flagging, admin tools
- **Real-time Updates:** SignalR for live notifications
- **Engagement:** Reactions, voting, following

### 4.4 Business Directory
- **Business Profiles:** Detailed listings with verification
- **Service Catalog:** Categorized services with pricing
- **Search & Discovery:** Location-based, category filters
- **Reviews & Ratings:** Verified customer feedback
- **Contact Options:** Direct messaging, phone, email

### 4.5 Service Booking
- **Online Booking:** Calendar-based availability
- **Booking Management:** Confirmations, reminders
- **Payment Integration:** Stripe/PayPal (Phase 1.5)
- **Review System:** Post-service feedback

---

## 5. API Endpoints Summary

### 5.1 Authentication
```
POST   /api/auth/register
POST   /api/auth/login
POST   /api/auth/refresh
POST   /api/auth/logout
POST   /api/auth/forgot-password
POST   /api/auth/reset-password
GET    /api/auth/verify-email
```

### 5.2 Events
```
GET    /api/events
GET    /api/events/{id}
POST   /api/events
PUT    /api/events/{id}
DELETE /api/events/{id}
GET    /api/events/search
GET    /api/events/categories
POST   /api/events/{id}/register
GET    /api/events/{id}/registrations
```

### 5.3 Community
```
GET    /api/forums
GET    /api/forums/{id}/topics
POST   /api/topics
GET    /api/topics/{id}
PUT    /api/topics/{id}
POST   /api/topics/{id}/posts
PUT    /api/posts/{id}
POST   /api/posts/{id}/reactions
POST   /api/posts/{id}/report
```

### 5.4 Business
```
GET    /api/businesses
GET    /api/businesses/{id}
POST   /api/businesses
PUT    /api/businesses/{id}
GET    /api/businesses/search
GET    /api/businesses/{id}/services
POST   /api/businesses/{id}/services
GET    /api/businesses/{id}/reviews
POST   /api/businesses/{id}/reviews
```

### 5.5 Bookings
```
POST   /api/bookings
GET    /api/bookings/{id}
PUT    /api/bookings/{id}
DELETE /api/bookings/{id}
GET    /api/bookings/my-bookings
POST   /api/bookings/{id}/confirm
POST   /api/bookings/{id}/cancel
```

---

## 6. Database Schema Overview

### 6.1 Core Tables
```sql
Identity Schema:
- Users
- UserProfiles
- UserRoles
- UserPreferences
- RefreshTokens

Events Schema:
- Events
- EventCategories
- EventSchedules
- Registrations
- TicketTypes

Community Schema:
- Forums
- ForumCategories
- Topics
- Posts
- Reactions
- Reports

Business Schema:
- Businesses
- BusinessCategories
- Services
- Bookings
- Reviews
- OpeningHours
```

### 6.2 Key Relationships
- User → Many Events (as organizer)
- User → Many Registrations
- User → Many Posts
- User → Many Bookings
- Event → Many Registrations
- Business → Many Services
- Business → Many Reviews
- Service → Many Bookings

---

## 7. Security Requirements

### 7.1 Authentication & Authorization
- **JWT Tokens:** 15-minute access, 7-day refresh
- **Role-Based:** Admin, Moderator, BusinessOwner, User
- **Resource-Based:** Owner can edit own content
- **API Rate Limiting:** 1000 req/hour default
- **CORS Policy:** Configured for web/mobile apps

### 7.2 Data Protection
- **Encryption:** Data at rest and in transit
- **PII Handling:** GDPR compliant
- **Password Policy:** Min 8 chars, complexity required
- **2FA Support:** TOTP-based (Phase 2)
- **Audit Logging:** All sensitive operations

### 7.3 Security Headers
```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Strict-Transport-Security: max-age=31536000
Content-Security-Policy: default-src 'self'
```

---

## 8. Performance Targets

### 8.1 Response Times
- **API Response:** < 200ms (p95)
- **Page Load:** < 2 seconds
- **Database Query:** < 50ms average
- **Search Results:** < 500ms
- **Real-time Updates:** < 100ms latency

### 8.2 Scalability
- **Concurrent Users:** 10,000 target
- **Requests/Second:** 1,000 RPS
- **Database Connections:** 100 pool size
- **Message Throughput:** 10,000 msg/min
- **Storage:** 1TB initial capacity

### 8.3 Availability
- **Uptime Target:** 99.9%
- **RPO:** 4 hours
- **RTO:** 1 hour
- **Backup Frequency:** Daily
- **Disaster Recovery:** Multi-region

---

## 9. Integration Points

### 9.1 External Services
- **Azure AD B2C:** Authentication
- **SendGrid:** Email delivery
- **Twilio:** SMS notifications
- **Stripe/PayPal:** Payments
- **Google Maps:** Location services
- **Azure Cognitive Services:** AI/ML features

### 9.2 Azure Services
- **Container Apps:** Application hosting
- **PostgreSQL Flexible Server:** Database
- **Redis Cache:** Caching layer
- **Service Bus:** Message queue
- **Blob Storage:** File storage
- **Application Insights:** Monitoring

---

## 10. Development Guidelines

### 10.1 Coding Standards
- **C# Conventions:** Follow Microsoft guidelines
- **Naming:** PascalCase for public, camelCase for private
- **Comments:** XML documentation for public APIs
- **Line Length:** 120 character maximum
- **File Organization:** One class per file

### 10.2 Git Workflow
- **Branching:** GitFlow model
- **Commits:** Conventional commits
- **PRs:** Required reviews, passing tests
- **Main Branch:** Protected, deploy-ready
- **Feature Branches:** feature/*, bugfix/*

### 10.3 Testing Requirements
- **Unit Tests:** 80% minimum coverage
- **Integration Tests:** All API endpoints
- **TDD:** Write tests first
- **Naming:** Should_ExpectedBehavior_When_StateUnderTest
- **AAA Pattern:** Arrange, Act, Assert

---

## 11. Common Patterns & Code Examples

### 11.1 Domain Entity Pattern
```csharp
public class Event : Entity, IAggregateRoot
{
    // Private setters for encapsulation
    public Title Title { get; private set; }
    public Description Description { get; private set; }
    public EventCategory Category { get; private set; }
    public UserId OrganizerId { get; private set; }
    
    // Collection exposed as read-only
    private readonly List<Registration> _registrations = new();
    public IReadOnlyCollection<Registration> Registrations => _registrations.AsReadOnly();
    
    // Factory method for creation
    public static Event Create(Title title, Description description, 
        EventCategory category, UserId organizerId)
    {
        var @event = new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Category = category,
            OrganizerId = organizerId
        };
        
        @event.AddDomainEvent(new EventCreatedEvent(@event.Id));
        return @event;
    }
    
    // Business logic methods
    public Result Register(UserId userId, int ticketQuantity)
    {
        if (!CanRegister(userId, ticketQuantity))
            return Result.Failure("Registration not allowed");
            
        var registration = Registration.Create(Id, userId, ticketQuantity);
        _registrations.Add(registration);
        
        AddDomainEvent(new UserRegisteredForEventEvent(Id, userId));
        return Result.Success();
    }
}
```

### 11.2 CQRS Command Pattern
```csharp
// Command
public record CreateEventCommand : IRequest<Result<EventId>>
{
    public string Title { get; init; }
    public string Description { get; init; }
    public Guid CategoryId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string Location { get; init; }
}

// Handler
public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Result<EventId>>
{
    private readonly IEventRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<EventId>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        // Validate
        var title = Title.Create(request.Title);
        if (title.IsFailure) return Result.Failure<EventId>(title.Error);
        
        // Create
        var @event = Event.Create(
            title.Value,
            Description.Create(request.Description).Value,
            await GetCategoryAsync(request.CategoryId),
            GetCurrentUserId());
        
        // Persist
        await _repository.AddAsync(@event);
        await _unitOfWork.CommitAsync(cancellationToken);
        
        return Result.Success(new EventId(@event.Id));
    }
}
```

### 11.3 Repository Pattern
```csharp
public interface IEventRepository : IRepository<Event>
{
    Task<Event> GetByIdWithRegistrationsAsync(EventId id);
    Task<PagedList<Event>> GetUpcomingEventsAsync(int page, int pageSize);
    Task<List<Event>> GetEventsByCategoryAsync(EventCategory category);
    Task<bool> ExistsAsync(EventId id);
}

public class EventRepository : Repository<Event>, IEventRepository
{
    public EventRepository(AppDbContext context) : base(context) { }
    
    public async Task<Event> GetByIdWithRegistrationsAsync(EventId id)
    {
        return await _context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == id.Value);
    }
    
    public async Task<PagedList<Event>> GetUpcomingEventsAsync(int page, int pageSize)
    {
        var query = _context.Events
            .Where(e => e.StartDate > DateTime.UtcNow)
            .OrderBy(e => e.StartDate);
            
        return await PagedList<Event>.CreateAsync(query, page, pageSize);
    }
}
```

---

## 12. Phase 1 Priorities

### 12.1 Must Have (MVP)
1. User registration and authentication
2. Basic event creation and listing
3. Event search and filtering
4. Event registration
5. Basic forum functionality
6. Business directory listing
7. Simple search capabilities

### 12.2 Should Have
1. Rich text editing
2. Image uploads
3. Email notifications
4. Basic moderation tools
5. User profiles
6. Business verification
7. Review system

### 12.3 Nice to Have
1. Real-time notifications
2. Advanced search filters
3. Social sharing
4. Calendar exports
5. Mobile optimization
6. Analytics dashboard
7. Recommendation engine

---

## 13. Known Constraints & Decisions

### 13.1 Technical Constraints
- **Azure Region:** South India (closest to Sri Lanka)
- **Budget:** Optimize for cost-efficiency
- **Team Size:** 1-2 developers
- **Timeline:** 12 weeks for Phase 1
- **Mobile:** Web-first, mobile in Phase 2

### 13.2 Architectural Decisions
- **Modular Monolith:** Start simple, prepare for microservices
- **CQRS:** Separate read/write for scalability
- **Event Sourcing:** Not in Phase 1, consider for Phase 3
- **Multi-tenancy:** Not required initially
- **Caching:** Aggressive caching for performance

### 13.3 Business Rules
- **Language:** Sinhala, Tamil, English support
- **Currency:** LKR primary, USD secondary
- **Time Zone:** Sri Lanka Time (UTC+5:30)
- **Phone Format:** +94 XX XXX XXXX
- **Business Hours:** Consider 24/7 for diaspora

---

## 14. Development Session Notes

### 14.1 Current Sprint (2025-11-01)
- **Sprint:** Epic 1 Phase 3 Complete
- **Week:** 5 of 12
- **Focus:** Profile Enhancement Features - COMPLETE & DEPLOYED ✅
- **Blockers:** None

### 14.2 Completed Tasks - Epic 1 Phase 3 (Profile Enhancement)
- [x] Profile Photo Upload/Delete (Azure Blob Storage integration)
- [x] Location Field (UserLocation value object with privacy-first design)
- [x] Cultural Interests (20 pre-defined interests, 0-10 per user)
- [x] Languages (20 languages with ISO 639 codes, 1-5 per user with proficiency levels)
- [x] GET Endpoint Fix (EF Core OwnsMany collections properly configured)
- [x] Database Migrations (4 migrations applied to staging)
- [x] API Endpoints (6 new PUT endpoints)
- [x] Zero Tolerance TDD (495/495 tests passing)
- [x] Azure Staging Deployment (verified working)

### 14.3 Epic 1 Phase 3 Architecture Decisions
1. **Enumeration Pattern**: Type-safe value objects for CulturalInterest and LanguageCode
2. **Junction Tables**: user_cultural_interests + user_languages for efficient querying
3. **Privacy-First**: Cultural interests optional (0-10), location city-level only
4. **Business Rules**: Languages required (1-5), with proficiency levels (Basic to Native)
5. **EF Core OwnsMany**: Proper configuration for value object collections
6. **Domain Events**: CulturalInterestsUpdatedEvent, LanguagesUpdatedEvent

### 14.4 Next Session Tasks
**Option 1: Continue Epic 1 Phase 3 Enhancements**
1. Add Bio field (rich text editor, max 1000 chars)
2. Add GET endpoint for user profile (comprehensive DTO)
3. Add profile completeness calculation
4. Add profile visibility settings

**Option 2: Start Epic 2 Phase 1 (Event Discovery & Management)**
1. Event Location with PostGIS (3 days)
2. Event Category & Pricing (1 day)
3. Event Images (2 days)

---

## 15. Quick Reference

### 15.1 Key Commands
```bash
# Run locally
dotnet run --project src/LankaConnect.API

# Run tests
dotnet test

# Add migration
dotnet ef migrations add MigrationName -s src/LankaConnect.API -p src/LankaConnect.Infrastructure

# Update database
dotnet ef database update -s src/LankaConnect.API

# Build Docker
docker-compose build

# Run services
docker-compose up
```

### 15.2 Important URLs
- **API:** https://localhost:5001
- **Swagger:** https://localhost:5001/swagger
- **Health:** https://localhost:5001/health
- **Seq Logs:** http://localhost:5341
- **MailHog:** http://localhost:8025
- **PostgreSQL:** localhost:5432
- **Redis:** localhost:6379

### 15.3 Connection Strings
```
PostgreSQL: Host=localhost;Database=lankaconnect_dev;Username=lankaconnect;Password=DevPassword123!
Redis: localhost:6379
```

---

This document provides Claude Code agents with comprehensive project context for effective development assistance.