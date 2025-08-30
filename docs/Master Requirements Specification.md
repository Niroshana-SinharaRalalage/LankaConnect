# LankaConnect - Master Requirements Specification

**Version:** 1.0  
**Date:** January 2025  
**Status:** Draft  
**Target Architecture:** Modular Monolith with DDD Bounded Contexts  
**Development Approach:** TDD with Clean Architecture  

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [System Architecture](#2-system-architecture)
3. [Phase 1 Requirements - Community Foundation](#3-phase-1-requirements---community-foundation)
4. [Phase 2 Requirements - Education Platform](#4-phase-2-requirements---education-platform)
5. [Bounded Context Specifications](#5-bounded-context-specifications)
6. [API Specifications](#6-api-specifications)
7. [Data Models & Database Schema](#7-data-models--database-schema)
8. [Non-Functional Requirements](#8-non-functional-requirements)
9. [Security Requirements](#9-security-requirements)
10. [Mobile Application Requirements](#10-mobile-application-requirements)

---

## 1. Project Overview

### 1.1 Vision Statement
LankaConnect is the comprehensive Sri Lankan American community platform that enables diaspora connection, cultural preservation, and community growth through events, forums, business networking, and educational programs.

### 1.2 Mission
To unite the Sri Lankan American diaspora by providing a centralized platform for community engagement, cultural education, business networking, and event management that preserves cultural identity while fostering American integration.

### 1.3 Core Value Propositions
- **Event Discovery**: Centralized calendar for cultural, religious, and social events
- **Community Forums**: Topic-based discussions for jobs, visa help, housing, culture
- **Business Directory**: Comprehensive Sri Lankan-owned business listings with reviews
- **Cultural Preservation**: Recipes, stories, traditions, and educational content
- **Professional Networking**: Career opportunities and business connections
- **Educational Platform**: Language learning, cultural education, professional development (Phase 2)

### 1.4 Target Users
- **Primary**: Sri Lankan Americans aged 25-55 in major metropolitan areas
- **Secondary**: Second/third generation Sri Lankan Americans seeking cultural connection
- **Tertiary**: Event organizers, business owners, cultural educators, community leaders

### 1.5 Success Metrics
- **User Engagement**: 20% daily active user rate
- **Event Participation**: 40% RSVP-to-attendance conversion
- **Business Directory Usage**: 10+ searches per user per month
- **Forum Activity**: 500+ monthly discussions
- **Revenue**: $50K monthly recurring revenue by month 12

---

## 2. System Architecture

### 2.1 Architecture Decision: Modular Monolith

**Rationale:**
- Single deployment and debugging environment for solo developer
- Maintains DDD bounded context separation
- Simplified CI/CD pipeline and testing
- Easy future migration to microservices if needed

### 2.2 Bounded Context Organization

```
LankaConnect/
├── src/
│   ├── Domain/
│   │   ├── Identity/          # User management, profiles, authentication
│   │   ├── Events/            # Event creation, RSVP, calendar, ticketing
│   │   ├── Community/         # Forums, discussions, moderation
│   │   ├── Business/          # Directory, reviews, marketplace
│   │   ├── Education/         # Courses, instructors, learning (Phase 2)
│   │   ├── Content/           # Cultural content, media management
│   │   └── Shared/            # Common domain models and interfaces
│   ├── Application/           # Use cases, commands, queries, handlers
│   ├── Infrastructure/        # Data access, external services, Azure integrations
│   └── Presentation/          # Web API, SignalR hubs, authentication
├── tests/
│   ├── UnitTests/            # Domain and application layer tests
│   ├── IntegrationTests/     # Database and external service tests
│   └── E2ETests/             # Full workflow tests
├── frontend/
│   ├── web/                  # Next.js application
│   └── mobile/               # React Native application
└── infrastructure/
    ├── docker/               # Local development containers
    └── azure/                # Azure resource templates
```

### 2.3 Technology Stack

**Backend:**
- **Framework**: ASP.NET Core 8.0
- **Database**: PostgreSQL with EF Core
- **Authentication**: Azure AD B2C
- **Caching**: Azure Cache for Redis
- **Storage**: Azure Blob Storage
- **Real-time**: SignalR
- **Background Jobs**: Hangfire
- **API Documentation**: Swagger/OpenAPI

**Frontend:**
- **Web**: Next.js 14 with TypeScript
- **Mobile**: React Native with Expo
- **State Management**: Zustand/TanStack Query
- **UI Components**: Tailwind CSS + Headless UI

**DevOps:**
- **Hosting**: Azure Container Apps
- **CI/CD**: GitHub Actions
- **Monitoring**: Azure Application Insights
- **Secrets**: Azure Key Vault

---

## 3. Phase 1 Requirements - Community Foundation

### 3.1 Phase 1 Overview

**Timeline**: 3 months (12 weeks)  
**Primary Goals**: 
- Establish core community features
- Build user base of 3,000+ members
- Generate initial revenue through memberships and event commissions
- Create foundation for Phase 2 education features

### 3.2 Phase 1 User Stories

#### 3.2.1 Epic: User Identity & Authentication

**US-001: User Registration**
```
As a Sri Lankan American
I want to create an account with social login options
So that I can quickly join the community without complex registration

Acceptance Criteria:
- User can register with email/password
- User can register with Facebook, Google, or Apple
- Profile includes: name, location, cultural background, interests
- Email verification required for full access
- Profile photo upload to Azure Blob Storage
- User receives welcome email with community guidelines

Technical Notes:
- Implement Azure AD B2C for authentication
- Store additional profile data in PostgreSQL
- Generate JWT tokens for API access
- Implement user aggregate in Identity bounded context
```

**US-002: User Profile Management**
```
As a registered user
I want to manage my profile and privacy settings
So that I can control how I appear in the community

Acceptance Criteria:
- User can edit profile information
- User can set privacy preferences (public, friends, private)
- User can manage notification preferences
- User can upload/change profile photo
- User can set location for local event recommendations
- User can specify cultural interests and languages

Technical Notes:
- Implement UserProfile aggregate with value objects
- Privacy settings affect API responses
- Location stored for geo-based event filtering
- Interests used for content recommendation algorithm
```

#### 3.2.2 Epic: Event Discovery & Management

**US-003: Event Discovery**
```
As a community member
I want to discover Sri Lankan events in my area
So that I can participate in community activities

Acceptance Criteria:
- User sees events filtered by location (25, 50, 100 mile radius)
- Events display: title, date, location, description, ticket price
- User can filter by: date range, event type, price range
- User can search events by keywords
- Events show RSVP count and attendee previews
- Cultural calendar shows Buddhist/Hindu holidays

Technical Notes:
- Implement Event aggregate with location-based queries
- PostgreSQL with PostGIS for geographic searches
- Event categories: Cultural, Religious, Professional, Social, Sports
- Integration with cultural calendar API or static data
- Caching of event queries with Redis
```

**US-004: Event RSVP System**
```
As a community member
I want to RSVP to events and manage my attendance
So that organizers can plan appropriately and I can track my commitments

Acceptance Criteria:
- User can RSVP (Going, Maybe, Not Going)
- User receives confirmation email with event details
- User can change RSVP status
- User can see their upcoming events in dashboard
- User receives reminder notifications 24 hours before event
- User can add events to external calendar (Google, Apple)

Technical Notes:
- Implement RSVP aggregate with user/event relationships
- Email notifications using Azure Communication Services
- Calendar export in ICS format
- SignalR updates for real-time RSVP count changes
- Background job for reminder notifications
```

**US-005: Event Creation (Organizers)**
```
As an event organizer
I want to create and manage events
So that I can promote community activities and sell tickets

Acceptance Criteria:
- Organizer can create events with detailed information
- Support for free and paid events with Stripe integration
- Image upload for event promotions
- Event approval workflow for quality control
- Real-time dashboard showing RSVPs and ticket sales
- Automatic social media post generation for Facebook/WhatsApp

Technical Notes:
- Implement EventOrganizer aggregate with permissions
- Stripe integration for payment processing
- Image uploads to Azure Blob Storage with CDN
- Admin approval workflow with notification system
- Analytics dashboard with charts and metrics
- Social media API integration for auto-posting
```

#### 3.2.3 Epic: Community Forums

**US-006: Forum Categories & Discussions**
```
As a community member
I want to participate in topic-based discussions
So that I can get help, share knowledge, and connect with others

Acceptance Criteria:
- Forums organized by categories: Jobs, Visa/Immigration, Housing, Cultural, Business, General
- User can create new discussion topics
- User can reply to existing discussions
- Rich text editor with image/link support
- Discussions show: author, timestamp, reply count, last activity
- Search functionality across all discussions

Technical Notes:
- Implement Forum, Topic, and Post aggregates
- Categories stored as enumeration with extensibility
- Rich text stored as HTML with sanitization
- Full-text search using PostgreSQL
- Moderation flags and reporting system
- Vote/helpful system for quality content
```

**US-007: Forum Moderation**
```
As a community moderator
I want to moderate forum content
So that discussions remain helpful and appropriate

Acceptance Criteria:
- Moderators can pin, lock, or delete topics
- Automated content filtering for inappropriate language
- User reporting system for problematic content
- Moderator dashboard showing flagged content
- User reputation system based on helpful contributions
- Community guidelines enforcement

Technical Notes:
- Implement Moderator role with permissions
- Content filtering using Azure Cognitive Services
- Reporting workflow with notification system
- Reputation calculation algorithm
- Audit log for moderation actions
```

#### 3.2.4 Epic: Business Directory

**US-008: Business Listings**
```
As a business owner
I want to list my Sri Lankan-owned business
So that community members can discover and support it

Acceptance Criteria:
- Business owners can create detailed business profiles
- Categories: Restaurant, Grocery, Services, Professional, Retail
- Location with map integration (Azure Maps)
- Business hours, contact information, website/social links
- Photo gallery with up to 10 images
- Basic listing free, premium features for $25/month

Technical Notes:
- Implement Business aggregate with location data
- Integration with Azure Maps for geocoding
- Image gallery with Azure Blob Storage
- Premium subscription management
- Search and filtering by category, location, rating
```

**US-009: Business Reviews & Ratings**
```
As a community member
I want to review and rate businesses
So that others can make informed decisions

Acceptance Criteria:
- Users can leave 1-5 star ratings with written reviews
- Reviews require actual purchase/visit verification when possible
- Business owners can respond to reviews
- Overall rating calculation with review count
- Review filtering and sorting options
- Inappropriate review reporting and moderation

Technical Notes:
- Implement Review aggregate with user/business relationships
- Review verification system where applicable
- Rating calculation algorithm weighted by review quality
- Business owner response notifications
- Review moderation workflow
```

#### 3.2.5 Epic: Cultural Content Hub

**US-010: Cultural Content Sharing**
```
As a community member
I want to share and discover cultural content
So that we can preserve and celebrate Sri Lankan traditions

Acceptance Criteria:
- Users can share recipes, stories, photos, traditions
- Content categories: Recipes, Stories, Traditions, Photos, Music, Videos
- Rich media support with image/video uploads
- Content rating and bookmarking system
- Multilingual support (English, Sinhala, Tamil)
- Cultural calendar integration

Technical Notes:
- Implement CulturalContent aggregate with media handling
- Azure Blob Storage for media files with CDN
- Content categorization and tagging system
- Multilingual content storage and search
- Content approval workflow for quality
```

### 3.3 Phase 1 Membership Tiers

#### 3.3.1 Free Tier (Community Access)
**Price**: Free  
**Features**:
- Basic event discovery and RSVP
- Forum browsing (read-only on premium topics)
- Business directory browsing
- Cultural content viewing
- 1 event creation per month (free events only)

#### 3.3.2 Premium Membership
**Price**: $10/month  
**Features**:
- All free features
- 3 event publications per month (free and paid)
- Full forum access with posting privileges
- Enhanced business listing with priority placement
- Priority customer support
- Advanced event analytics

#### 3.3.3 Super Premium Membership
**Price**: $20/month  
**Features**:
- All premium features
- Unlimited event publications
- Premium business features (featured placement, analytics)
- Early access to new features
- Direct contact with community managers
- Advanced community analytics dashboard

---

## 4. Phase 2 Requirements - Education Platform

### 4.1 Phase 2 Overview

**Timeline**: 6 months (starting month 4)  
**Primary Goals**:
- Launch comprehensive education platform
- Introduce family-focused pricing tiers
- Generate additional revenue through courses and certification
- Establish cultural preservation mission

### 4.2 Phase 2 Enhanced Membership Tiers

#### 4.2.1 Education Add-On
**Price**: $15/month (can be added to any existing plan)  
**Features**:
- Access to live interactive classes
- Recorded course library
- Basic certification programs
- Community learning forums
- Progress tracking and achievements

#### 4.2.2 Family Plan
**Price**: $45/month (for 3-5 family members)  
**Features**:
- All Super Premium features for all family members
- Full education platform access for entire family
- Family learning sessions and multi-generational classes
- Enhanced tutoring discounts (25% off)
- Family cultural challenges and activities
- Priority booking for popular courses

### 4.3 Phase 2 User Stories

#### 4.3.1 Epic: Educational Content Management

**US-011: Course Creation & Management**
```
As an instructor
I want to create and manage educational courses
So that I can teach Sri Lankan culture and language to the community

Acceptance Criteria:
- Instructors can apply and be approved to teach
- Course creation wizard with curriculum planning
- Support for live classes and recorded content
- Pricing and scheduling management
- Student enrollment and progress tracking
- Payment processing and revenue sharing (70/30 split)

Technical Notes:
- Implement Instructor and Course aggregates
- Integration with Azure Communication Services for video
- Scheduling system with calendar integration
- Payment processing with automatic splits
- Content delivery network for video streaming
```

**US-012: Live Interactive Classes**
```
As a student
I want to attend live interactive classes
So that I can learn with real-time feedback and community interaction

Acceptance Criteria:
- Video conferencing integration with up to 50 participants
- Screen sharing and virtual whiteboard capabilities
- Real-time chat and Q&A features
- Automatic recording for later viewing
- Attendance tracking and participation metrics
- Breakout rooms for small group activities

Technical Notes:
- Azure Communication Services for video calls
- SignalR for real-time chat and interactions
- Recording storage in Azure Media Services
- Participant management and permissions
- Integration with learning management features
```

#### 4.3.2 Epic: Learning Management System

**US-013: Student Learning Dashboard**
```
As a student
I want to track my learning progress and access my courses
So that I can manage my educational journey effectively

Acceptance Criteria:
- Personal dashboard showing enrolled courses
- Progress tracking with completion percentages
- Upcoming class schedule with reminders
- Access to recorded sessions and materials
- Achievement badges and certificates
- Learning analytics and recommendations

Technical Notes:
- Implement StudentProgress aggregate
- Progress calculation algorithms
- Notification system for schedules and achievements
- Certificate generation with digital signatures
- Recommendation engine based on learning patterns
```

**US-014: Certification Programs**
```
As a student
I want to earn certifications for completed courses
So that I can demonstrate my cultural knowledge and language skills

Acceptance Criteria:
- Structured certification programs with requirements
- Assessment and testing capabilities
- Digital certificate generation
- Certificate verification system
- Integration with professional networks
- Employer verification services

Technical Notes:
- Implement Certification aggregate with requirements
- Assessment engine with various question types
- Digital certificate generation with blockchain verification
- Public verification portal for employers
- Integration with LinkedIn and other professional platforms
```

---

## 5. Bounded Context Specifications

### 5.1 Identity Bounded Context

#### 5.1.1 Domain Models

**User Aggregate Root**
```csharp
public class User : AggregateRoot<UserId>
{
    public PersonalDetails PersonalDetails { get; private set; }
    public ContactInformation ContactInfo { get; private set; }
    public CulturalProfile CulturalProfile { get; private set; }
    public PrivacySettings PrivacySettings { get; private set; }
    public UserStatus Status { get; private set; }
    public List<UserRole> Roles { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastLoginAt { get; private set; }

    // Domain methods
    public void UpdatePersonalDetails(PersonalDetails details);
    public void UpdatePrivacySettings(PrivacySettings settings);
    public void AddRole(UserRole role);
    public void SetCulturalProfile(CulturalProfile profile);
    public bool CanAccessPremiumFeatures();
}
```

**Value Objects**
```csharp
public record PersonalDetails(
    string FirstName,
    string LastName,
    DateTime? DateOfBirth,
    string? Bio,
    string? ProfileImageUrl
);

public record ContactInformation(
    Email Email,
    PhoneNumber? Phone,
    Address? Address
);

public record CulturalProfile(
    List<string> Languages,
    string? RegionOfOrigin,
    List<string> Interests,
    ReligiousAffiliation? Religion
);

public record PrivacySettings(
    bool ProfileVisible,
    bool ShowInDirectory,
    bool AllowDirectMessages,
    bool EmailNotifications
);
```

#### 5.1.2 Application Services

**UserApplicationService**
```csharp
public class UserApplicationService
{
    public async Task<UserId> RegisterUserAsync(RegisterUserCommand command);
    public async Task UpdateUserProfileAsync(UpdateProfileCommand command);
    public async Task<UserProfileDto> GetUserProfileAsync(UserId userId);
    public async Task UpdatePrivacySettingsAsync(UpdatePrivacyCommand command);
    public async Task DeactivateUserAsync(UserId userId);
    public async Task<bool> VerifyEmailAsync(string token);
}
```

### 5.2 Events Bounded Context

#### 5.2.1 Domain Models

**Event Aggregate Root**
```csharp
public class Event : AggregateRoot<EventId>
{
    public EventDetails Details { get; private set; }
    public EventSchedule Schedule { get; private set; }
    public Location Location { get; private set; }
    public EventPricing Pricing { get; private set; }
    public EventCapacity Capacity { get; private set; }
    public OrganizerId OrganizerId { get; private set; }
    public EventStatus Status { get; private set; }
    public List<EventRsvp> Rsvps { get; private set; }
    public List<EventImage> Images { get; private set; }

    // Domain methods
    public void UpdateEventDetails(EventDetails details);
    public EventRsvp AddRsvp(UserId userId, RsvpStatus status);
    public void UpdateRsvp(UserId userId, RsvpStatus newStatus);
    public void CancelEvent(string reason);
    public bool CanUserRsvp(UserId userId);
    public int GetAvailableSpots();
}
```

**Value Objects**
```csharp
public record EventDetails(
    string Title,
    string Description,
    EventCategory Category,
    List<string> Tags,
    string? ExternalUrl
);

public record EventSchedule(
    DateTime StartDateTime,
    DateTime EndDateTime,
    TimeZone TimeZone,
    bool IsRecurring,
    RecurrencePattern? Pattern
);

public record Location(
    string VenueName,
    Address Address,
    GeoCoordinate Coordinates,
    string? AdditionalInfo
);

public record EventPricing(
    bool IsFree,
    Money? TicketPrice,
    Money? EarlyBirdPrice,
    DateTime? EarlyBirdDeadline,
    PaymentProvider? PaymentProvider
);
```

#### 5.2.2 Application Services

**EventApplicationService**
```csharp
public class EventApplicationService
{
    public async Task<EventId> CreateEventAsync(CreateEventCommand command);
    public async Task UpdateEventAsync(UpdateEventCommand command);
    public async Task<EventDto> GetEventAsync(EventId eventId);
    public async Task<List<EventDto>> SearchEventsAsync(EventSearchQuery query);
    public async Task<RsvpId> RsvpToEventAsync(RsvpToEventCommand command);
    public async Task CancelRsvpAsync(CancelRsvpCommand command);
    public async Task<List<EventDto>> GetUserEventsAsync(UserId userId);
}
```

### 5.3 Community Bounded Context

#### 5.3.1 Domain Models

**Forum Aggregate Root**
```csharp
public class Forum : AggregateRoot<ForumId>
{
    public ForumDetails Details { get; private set; }
    public ForumCategory Category { get; private set; }
    public List<ForumTopic> Topics { get; private set; }
    public ForumModeration Moderation { get; private set; }
    public ForumStatistics Statistics { get; private set; }

    // Domain methods
    public ForumTopic CreateTopic(UserId authorId, TopicDetails details);
    public void ModerateContent(UserId moderatorId, ModerationAction action);
    public void UpdateStatistics();
}
```

**ForumTopic Aggregate Root**
```csharp
public class ForumTopic : AggregateRoot<TopicId>
{
    public TopicDetails Details { get; private set; }
    public UserId AuthorId { get; private set; }
    public ForumId ForumId { get; private set; }
    public List<TopicPost> Posts { get; private set; }
    public TopicStatus Status { get; private set; }
    public TopicStatistics Statistics { get; private set; }

    // Domain methods
    public TopicPost AddPost(UserId authorId, PostContent content);
    public void UpdatePost(PostId postId, PostContent newContent);
    public void LockTopic(UserId moderatorId, string reason);
    public void PinTopic(UserId moderatorId);
}
```

### 5.4 Business Bounded Context

#### 5.4.1 Domain Models

**Business Aggregate Root**
```csharp
public class Business : AggregateRoot<BusinessId>
{
    public BusinessDetails Details { get; private set; }
    public BusinessLocation Location { get; private set; }
    public ContactInformation ContactInfo { get; private set; }
    public BusinessCategory Category { get; private set; }
    public List<BusinessImage> Images { get; private set; }
    public List<BusinessReview> Reviews { get; private set; }
    public BusinessSubscription Subscription { get; private set; }
    public BusinessStatistics Statistics { get; private set; }

    // Domain methods
    public void UpdateBusinessDetails(BusinessDetails details);
    public BusinessReview AddReview(UserId reviewerId, ReviewContent content, Rating rating);
    public void RespondToReview(ReviewId reviewId, string response);
    public void UpgradeSubscription(SubscriptionTier tier);
    public Money CalculateAverageRating();
}
```

---

## 6. API Specifications

### 6.1 REST API Endpoints

#### 6.1.1 Authentication Endpoints

```
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
GET  /api/auth/profile
PUT  /api/auth/profile
```

#### 6.1.2 Events Endpoints

```
GET    /api/events                    # Search and filter events
POST   /api/events                    # Create new event
GET    /api/events/{id}               # Get event details
PUT    /api/events/{id}               # Update event
DELETE /api/events/{id}               # Cancel event
POST   /api/events/{id}/rsvp          # RSVP to event
DELETE /api/events/{id}/rsvp          # Cancel RSVP
GET    /api/events/{id}/attendees     # Get event attendees
POST   /api/events/{id}/checkin       # Check in attendee
```

#### 6.1.3 Community Forum Endpoints

```
GET    /api/forums                    # Get forum categories
GET    /api/forums/{id}/topics        # Get topics in forum
POST   /api/forums/{id}/topics        # Create new topic
GET    /api/topics/{id}               # Get topic with posts
POST   /api/topics/{id}/posts         # Add post to topic
PUT    /api/posts/{id}                # Update post
DELETE /api/posts/{id}                # Delete post
POST   /api/posts/{id}/vote           # Vote on post
```

#### 6.1.4 Business Directory Endpoints

```
GET    /api/businesses                # Search businesses
POST   /api/businesses                # Create business listing
GET    /api/businesses/{id}           # Get business details
PUT    /api/businesses/{id}           # Update business
DELETE /api/businesses/{id}           # Delete business
POST   /api/businesses/{id}/reviews   # Add review
GET    /api/businesses/{id}/reviews   # Get business reviews
PUT    /api/reviews/{id}              # Update review
DELETE /api/reviews/{id}              # Delete review
```

### 6.2 API Response Formats

#### 6.2.1 Standard Response Wrapper

```json
{
  "success": true,
  "data": {},
  "message": "Operation completed successfully",
  "errors": [],
  "timestamp": "2025-01-15T10:30:00Z",
  "requestId": "uuid"
}
```

#### 6.2.2 Error Response Format

```json
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errors": [
    {
      "field": "email",
      "code": "INVALID_FORMAT",
      "message": "Email format is invalid"
    }
  ],
  "timestamp": "2025-01-15T10:30:00Z",
  "requestId": "uuid"
}
```

### 6.3 SignalR Hubs

#### 6.3.1 Event Updates Hub

```csharp
public class EventHub : Hub
{
    public async Task JoinEventGroup(string eventId);
    public async Task LeaveEventGroup(string eventId);
    
    // Server-sent events
    public async Task EventUpdated(EventUpdateDto update);
    public async Task NewRsvp(RsvpDto rsvp);
    public async Task RsvpCancelled(string userId);
}
```

#### 6.3.2 Community Chat Hub

```csharp
public class CommunityHub : Hub
{
    public async Task JoinForumGroup(string forumId);
    public async Task SendMessage(string forumId, string message);
    
    // Server-sent events
    public async Task NewPost(PostDto post);
    public async Task PostUpdated(PostDto post);
    public async Task UserOnline(string userId);
}
```

---

## 7. Data Models & Database Schema

### 7.1 Database Schema Design

#### 7.1.1 Identity Schema

```sql
-- Users table (Identity bounded context)
CREATE TABLE identity.users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    azure_ad_id VARCHAR(255) UNIQUE,
    email VARCHAR(255) UNIQUE NOT NULL,
    email_verified BOOLEAN DEFAULT FALSE,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    date_of_birth DATE,
    bio TEXT,
    profile_image_url TEXT,
    phone VARCHAR(20),
    address JSONB,
    cultural_profile JSONB,
    privacy_settings JSONB,
    status VARCHAR(20) DEFAULT 'Active',
    roles TEXT[] DEFAULT ARRAY['Member'],
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    last_login_at TIMESTAMP
);

-- User subscriptions
CREATE TABLE identity.user_subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES identity.users(id),
    subscription_type VARCHAR(50) NOT NULL,
    status VARCHAR(20) DEFAULT 'Active',
    started_at TIMESTAMP DEFAULT NOW(),
    expires_at TIMESTAMP,
    stripe_subscription_id VARCHAR(255),
    created_at TIMESTAMP DEFAULT NOW()
);
```

#### 7.1.2 Events Schema

```sql
-- Events table
CREATE TABLE events.events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organizer_id UUID REFERENCES identity.users(id),
    title VARCHAR(255) NOT NULL,
    description TEXT,
    category VARCHAR(50) NOT NULL,
    tags TEXT[],
    start_datetime TIMESTAMP NOT NULL,
    end_datetime TIMESTAMP NOT NULL,
    timezone VARCHAR(50) DEFAULT 'America/New_York',
    is_recurring BOOLEAN DEFAULT FALSE,
    recurrence_pattern JSONB,
    venue_name VARCHAR(255),
    address JSONB,
    location_coordinates POINT,
    is_free BOOLEAN DEFAULT TRUE,
    ticket_price DECIMAL(10,2),
    early_bird_price DECIMAL(10,2),
    early_bird_deadline TIMESTAMP,
    capacity INTEGER,
    status VARCHAR(20) DEFAULT 'Draft',
    external_url TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Event RSVPs
CREATE TABLE events.event_rsvps (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id UUID REFERENCES events.events(id),
    user_id UUID REFERENCES identity.users(id),
    status VARCHAR(20) NOT NULL, -- Going, Maybe, NotGoing
    guest_count INTEGER DEFAULT 0,
    special_requirements TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(event_id, user_id)
);

-- Event images
CREATE TABLE events.event_images (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id UUID REFERENCES events.events(id),
    image_url TEXT NOT NULL,
    caption TEXT,
    is_primary BOOLEAN DEFAULT FALSE,
    display_order INTEGER,
    created_at TIMESTAMP DEFAULT NOW()
);
```

#### 7.1.3 Community Schema

```sql
-- Forums
CREATE TABLE community.forums (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    category VARCHAR(50) NOT NULL,
    icon_url TEXT,
    display_order INTEGER,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Forum topics
CREATE TABLE community.forum_topics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    forum_id UUID REFERENCES community.forums(id),
    author_id UUID REFERENCES identity.users(id),
    title VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    status VARCHAR(20) DEFAULT 'Active', -- Active, Locked, Deleted
    is_pinned BOOLEAN DEFAULT FALSE,
    view_count INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    last_activity_at TIMESTAMP DEFAULT NOW()
);

-- Topic posts
CREATE TABLE community.topic_posts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    topic_id UUID REFERENCES community.forum_topics(id),
    author_id UUID REFERENCES identity.users(id),
    content TEXT NOT NULL,
    parent_post_id UUID REFERENCES community.topic_posts(id),
    helpful_votes INTEGER DEFAULT 0,
    is_solution BOOLEAN DEFAULT FALSE,
    status VARCHAR(20) DEFAULT 'Active',
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);
```

#### 7.1.4 Business Schema

```sql
-- Businesses
CREATE TABLE business.businesses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id UUID REFERENCES identity.users(id),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    category VARCHAR(50) NOT NULL,
    subcategory VARCHAR(50),
    website_url TEXT,
    phone VARCHAR(20),
    email VARCHAR(255),
    address JSONB NOT NULL,
    location_coordinates POINT,
    business_hours JSONB,
    social_media JSONB,
    subscription_tier VARCHAR(20) DEFAULT 'Basic',
    is_verified BOOLEAN DEFAULT FALSE,
    status VARCHAR(20) DEFAULT 'Active',
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Business reviews
CREATE TABLE business.business_reviews (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    business_id UUID REFERENCES business.businesses(id),
    reviewer_id UUID REFERENCES identity.users(id),
    rating INTEGER CHECK (rating >= 1 AND rating <= 5),
    title VARCHAR(255),
    content TEXT,
    is_verified BOOLEAN DEFAULT FALSE,
    helpful_votes INTEGER DEFAULT 0,
    owner_response TEXT,
    owner_response_date TIMESTAMP,
    status VARCHAR(20) DEFAULT 'Active',
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(business_id, reviewer_id)
);
```

### 7.2 Data Migration Strategy

#### 7.2.1 Schema Versioning

```sql
-- Schema version tracking
CREATE TABLE public.schema_versions (
    version VARCHAR(20) PRIMARY KEY,
    applied_at TIMESTAMP DEFAULT NOW(),
    description TEXT
);

-- Initial version
INSERT INTO public.schema_versions (version, description) 
VALUES ('1.0.0', 'Initial schema for Phase 1 features');
```

#### 7.2.2 Indexes for Performance

```sql
-- User lookup indexes
CREATE INDEX idx_users_email ON identity.users(email);
CREATE INDEX idx_users_azure_ad ON identity.users(azure_ad_id);
CREATE INDEX idx_users_status ON identity.users(status);

-- Event discovery indexes
CREATE INDEX idx_events_location ON events.events USING GIST(location_coordinates);
CREATE INDEX idx_events_datetime ON events.events(start_datetime, end_datetime);
CREATE INDEX idx_events_category ON events.events(category);
CREATE INDEX idx_events_status ON events.events(status);

-- Forum performance indexes
CREATE INDEX idx_topics_forum ON community.forum_topics(forum_id, last_activity_at DESC);
CREATE INDEX idx_posts_topic ON community.topic_posts(topic_id, created_at);
CREATE INDEX idx_posts_author ON community.topic_posts(author_id);

-- Business search indexes
CREATE INDEX idx_businesses_location ON business.businesses USING GIST(location_coordinates);
CREATE INDEX idx_businesses_category ON business.businesses(category, subcategory);
CREATE INDEX idx_reviews_business ON business.business_reviews(business_id, created_at DESC);
```

---

## 8. Non-Functional Requirements

### 8.1 Performance Requirements

#### 8.1.1 Response Time Requirements
- **API Endpoints**: 95% of requests < 500ms
- **Database Queries**: 95% of queries < 200ms
- **Page Load Times**: < 2 seconds for web, < 1.5 seconds for mobile
- **Real-time Features**: SignalR message delivery < 100ms

#### 8.1.2 Throughput Requirements
- **Concurrent Users**: Support 1,000 concurrent users initially
- **API Requests**: Handle 10,000 requests per minute
- **Database Connections**: Efficient connection pooling with max 100 connections
- **File Uploads**: Support simultaneous uploads with 10MB max file size

#### 8.1.3 Scalability Requirements
- **Horizontal Scaling**: Architecture supports adding more container instances
- **Database Scaling**: Read replicas for query-heavy operations
- **Caching Strategy**: Redis for session management and frequent queries
- **CDN Integration**: Azure CDN for static assets and images

### 8.2 Reliability Requirements

#### 8.2.1 Availability
- **Uptime Target**: 99.5% availability (approximately 3.6 hours downtime per month)
- **Planned Maintenance**: During off-peak hours (2-4 AM EST) with 24-hour notice
- **Health Checks**: Continuous monitoring of all critical services
- **Graceful Degradation**: Core features remain available during partial outages

#### 8.2.2 Data Integrity
- **Database Backups**: Automated daily backups with 30-day retention
- **Transaction Consistency**: ACID compliance for all financial transactions
- **Data Validation**: Server-side validation for all user inputs
- **Audit Logging**: Complete audit trail for all data modifications

### 8.3 Security Requirements

#### 8.3.1 Authentication & Authorization
- **Multi-Factor Authentication**: SMS or authenticator app for admin accounts
- **Password Policy**: Minimum 8 characters, mixed case, numbers, symbols
- **Session Management**: Secure JWT tokens with 24-hour expiration
- **Role-Based Access**: Granular permissions for different user types

#### 8.3.2 Data Protection
- **Encryption in Transit**: TLS 1.3 for all communications
- **Encryption at Rest**: Azure-managed encryption for database and storage
- **Personal Data Handling**: GDPR compliance for user data management
- **Payment Security**: PCI DSS compliance through Stripe integration

### 8.4 Usability Requirements

#### 8.4.1 User Interface
- **Responsive Design**: Optimal experience on desktop, tablet, and mobile
- **Accessibility**: WCAG 2.1 AA compliance for inclusivity
- **Multilingual Support**: English, Sinhala, Tamil character support
- **Cultural Sensitivity**: Appropriate cultural imagery and content

#### 8.4.2 User Experience
- **Intuitive Navigation**: Maximum 3 clicks to reach any feature
- **Search Functionality**: Intelligent search with autocomplete and filters
- **Error Handling**: Clear, helpful error messages and recovery guidance
- **Onboarding**: Guided setup process for new users

---

## 9. Security Requirements

### 9.1 Authentication Strategy

#### 9.1.1 Azure AD B2C Integration

```csharp
// Authentication configuration
public class AuthenticationConfig
{
    public string TenantId { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string Domain { get; set; }
    public string SignUpSignInPolicyId { get; set; }
    public string EditProfilePolicyId { get; set; }
    public string ResetPasswordPolicyId { get; set; }
}
```

#### 9.1.2 JWT Token Configuration

```csharp
public class JwtConfig
{
    public string SecretKey { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 30;
}
```

### 9.2 Authorization Policies

#### 9.2.1 Role-Based Authorization

```csharp
public static class AuthorizationPolicies
{
    public const string RequireAdminRole = "RequireAdminRole";
    public const string RequireModeratorRole = "RequireModeratorRole";
    public const string RequirePremiumMembership = "RequirePremiumMembership";
    public const string RequireEventOrganizerRole = "RequireEventOrganizerRole";
    public const string RequireBusinessOwner = "RequireBusinessOwner";
}

// Policy configuration
services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.RequireAdminRole,
        policy => policy.RequireRole("Admin"));
    
    options.AddPolicy(AuthorizationPolicies.RequirePremiumMembership,
        policy => policy.RequireClaim("subscription", "Premium", "SuperPremium"));
});
```

### 9.3 Data Protection

#### 9.3.1 Personal Data Encryption

```csharp
public class PersonalDataProtectionService
{
    public string EncryptPersonalData(string data);
    public string DecryptPersonalData(string encryptedData);
    public void PurgeUserData(UserId userId); // GDPR compliance
    public string AnonymizeUserData(UserId userId);
}
```

#### 9.3.2 Audit Logging

```csharp
public class AuditLog
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public string Action { get; set; }
    public string EntityType { get; set; }
    public string EntityId { get; set; }
    public string Changes { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
}
```

---

## 10. Mobile Application Requirements

### 10.1 React Native Application Structure

#### 10.1.1 Navigation Structure

```typescript
// Navigation types
type RootStackParamList = {
  Home: undefined;
  Events: { filter?: EventFilter };
  EventDetails: { eventId: string };
  Forums: { categoryId?: string };
  TopicDetails: { topicId: string };
  BusinessDirectory: { category?: string };
  BusinessDetails: { businessId: string };
  Profile: undefined;
  Settings: undefined;
};

type TabBarParamList = {
  Home: undefined;
  Events: undefined;
  Forums: undefined;
  Directory: undefined;
  Profile: undefined;
};
```

#### 10.1.2 Core Screens and Components

**HomeScreen Requirements:**
```typescript
interface HomeScreenProps {
  // Features
  showRecentActivity: boolean;
  showUpcomingEvents: boolean;
  showPopularTopics: boolean;
  showFeaturedBusinesses: boolean;
  showCulturalCalendar: boolean;
  
  // Data
  userLocation: GeoLocation;
  userPreferences: UserPreferences;
}

// Expected UI Elements
- Header with user avatar and notifications
- Quick action buttons (Create Event, Post Topic, etc.)
- Activity feed with location-based filtering
- Cultural calendar widget
- Featured content carousel
- Bottom tab navigation
```

**EventsScreen Requirements:**
```typescript
interface EventsScreenProps {
  // Filtering and search
  searchQuery: string;
  locationFilter: LocationFilter;
  categoryFilter: EventCategory[];
  dateRange: DateRange;
  priceRange: PriceRange;
  
  // Display options
  viewMode: 'list' | 'map' | 'calendar';
  sortBy: 'date' | 'distance' | 'popularity';
}

// Expected functionality
- Pull-to-refresh for latest events
- Infinite scroll for event list
- Map view with event markers
- Calendar view integration
- Quick RSVP actions
- Share event functionality
```

### 10.2 Mobile-Specific Features

#### 10.2.1 Push Notifications

```typescript
interface NotificationTypes {
  EVENT_REMINDER: {
    eventId: string;
    eventTitle: string;
    startTime: Date;
    location: string;
  };
  
  FORUM_REPLY: {
    topicId: string;
    topicTitle: string;
    replierName: string;
  };
  
  BUSINESS_REVIEW: {
    businessId: string;
    businessName: string;
    reviewerName: string;
    rating: number;
  };
  
  COMMUNITY_UPDATE: {
    type: 'announcement' | 'feature' | 'maintenance';
    title: string;
    message: string;
  };
}
```

#### 10.2.2 Offline Capabilities

```typescript
interface OfflineFeatures {
  // Cached data
  cachedEvents: Event[];
  cachedForumTopics: ForumTopic[];
  cachedBusinesses: Business[];
  
  // Offline actions
  offlineRsvps: OfflineRsvp[];
  offlineForumPosts: OfflinePost[];
  offlineReviews: OfflineReview[];
  
  // Sync when online
  syncPendingActions(): Promise<void>;
  getCachedContent(type: ContentType): any[];
}
```

### 10.3 Platform-Specific Considerations

#### 10.3.1 iOS Specific Features

- **Siri Shortcuts**: "Hey Siri, show me Sri Lankan events near me"
- **Widgets**: Home screen widget showing upcoming events
- **Apple Calendar Integration**: Add events to native calendar
- **Apple Maps Integration**: Navigation to event venues
- **Apple Pay Integration**: Quick event ticket purchases

#### 10.3.2 Android Specific Features

- **Google Assistant Integration**: Voice commands for event discovery
- **Home Screen Widgets**: Event countdown and forum activity
- **Google Calendar Integration**: Sync with Google Calendar
- **Google Maps Integration**: Navigation and location services
- **Google Pay Integration**: Payment processing

### 10.4 Mobile Performance Requirements

#### 10.4.1 Application Performance

- **App Launch Time**: < 2 seconds cold start, < 1 second warm start
- **Screen Transitions**: < 300ms between screens
- **Image Loading**: Progressive loading with placeholders
- **Battery Usage**: Efficient background processing
- **Memory Usage**: < 150MB RAM usage under normal operation

#### 10.4.2 Data Usage Optimization

- **Image Compression**: Automatic image optimization based on network
- **Caching Strategy**: Intelligent caching of frequently accessed data
- **Background Sync**: Efficient data synchronization
- **Offline Mode**: Core features available without internet

---

## Conclusion

This comprehensive requirements specification provides the foundation for building LankaConnect as a modular monolith with clear bounded contexts. The phased approach allows for rapid deployment of core community features while maintaining a clear path for educational platform expansion.

### Key Success Factors

1. **Domain-Driven Design**: Clear bounded contexts ensure maintainable code
2. **Clean Architecture**: Separation of concerns enables testing and scalability
3. **Mobile-First Approach**: Responsive design prioritizes mobile user experience
4. **Azure-Native Integration**: Leverages cloud services for scalability and reliability
5. **Community Focus**: Features designed around actual Sri Lankan American needs
6. **Cultural Authenticity**: Respects cultural nuances and multilingual requirements

### Next Steps

1. **Technical Architecture Document**: Detailed system design and implementation patterns
2. **Development Setup Guide**: Environment configuration and tooling setup
3. **Project Plan**: Sprint breakdown and development timeline
4. **Testing Strategy**: TDD implementation and automation framework
5. **Deployment Pipeline**: CI/CD configuration and release management

This specification serves as the single source of truth for all development activities and should be updated as requirements evolve through user feedback and market validation.

---

**Document Status**: Draft v1.0  
**Next Review**: Weekly during Phase 1 development  
**Approval Required**: Technical Lead, Product Owner  
**Distribution**: Development Team, Stakeholders