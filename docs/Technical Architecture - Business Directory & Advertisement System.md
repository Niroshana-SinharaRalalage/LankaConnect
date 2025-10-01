# Technical Architecture - Business Directory & Advertisement System

**Version:** 1.0  
**Date:** January 2025  
**Status:** Technical Specification  
**Integration:** Membership-Aware Business Features

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Business Bounded Context](#2-business-bounded-context)
3. [Advertisement Bounded Context](#3-advertisement-bounded-context)  
4. [Membership Integration](#4-membership-integration)
5. [API Specifications](#5-api-specifications)
6. [Database Schema](#6-database-schema)
7. [Implementation Strategy](#7-implementation-strategy)

---

## 1. Architecture Overview

### 1.1 Bounded Context Separation

```
Business Context (Business Listings & Reviews):
├── Business Aggregate (Core business profile)
├── Review Aggregate (Customer reviews)
├── BusinessCategory (Categorization)
└── BusinessSubscription (Membership integration)

Advertisement Context (Promotional System):
├── Advertisement Aggregate (Individual ads)
├── AdCampaign Aggregate (Campaign management)
├── AdPlacement (Where ads are shown)
└── AdAnalytics (Performance tracking)
```

### 1.2 Key Integration Points

**Membership System Integration:**
- Community Supporter ($5): Basic business listing (1 per user)
- Business Professional ($25): Enhanced features + advertisement posting
- Free users: Can only browse business directory

**Platform Strategy:**
- Businesses publish advertisements only (no product sales)
- Advertisement revenue through CPM/CPC model
- Clear separation from platform-owned marketplace

---

## 2. Business Bounded Context

### 2.1 Business Aggregate

```csharp
public class Business : BaseEntity
{
    private readonly List<Review> _reviews = new();
    private readonly List<BusinessImage> _images = new();
    
    public BusinessProfile Profile { get; private set; }
    public BusinessLocation Location { get; private set; }
    public ContactInformation ContactInfo { get; private set; }
    public BusinessCategory Category { get; private set; }
    public BusinessHours Hours { get; private set; }
    public Guid OwnerId { get; private set; }
    public BusinessStatus Status { get; private set; }
    public BusinessSubscription Subscription { get; private set; }
    public bool IsVerified { get; private set; }
    
    // Computed properties
    public IReadOnlyList<Review> Reviews => _reviews.AsReadOnly();
    public IReadOnlyList<BusinessImage> Images => _images.AsReadOnly();
    public decimal AverageRating => CalculateAverageRating();
    public int ReviewCount => _reviews.Count(r => r.Status == ReviewStatus.Approved);
    
    // Business methods
    public static Result<Business> Create(
        BusinessProfile profile, 
        BusinessLocation location, 
        ContactInformation contactInfo,
        BusinessCategory category,
        Guid ownerId);
        
    public Result UpdateProfile(BusinessProfile newProfile, Guid editorId);
    public Result AddReview(Review review);
    public Result RespondToReview(Guid reviewId, string response, Guid businessOwnerId);
    public Result AddImage(BusinessImage image, Guid uploaderId);
    public Result UpgradeSubscription(BusinessSubscriptionTier tier);
    public bool CanUserAddReview(Guid userId);
    public bool CanPostAdvertisements();
}
```

### 2.2 Business Value Objects

```csharp
public record BusinessProfile(
    string Name,
    string Description,
    string? Website,
    SocialMediaLinks? SocialMedia,
    List<string> Services,
    List<string> Specializations
);

public record BusinessLocation(
    Address Address,
    GeoCoordinate? Coordinates,
    string? AdditionalDirections,
    bool IsServiceAreaBusiness,
    List<string>? ServiceAreas
);

public record ContactInformation(
    PhoneNumber PrimaryPhone,
    PhoneNumber? SecondaryPhone,
    Email BusinessEmail,
    Email? ContactEmail
);

public record BusinessHours(
    Dictionary<DayOfWeek, OpeningHours> WeeklyHours,
    List<SpecialHours> SpecialHours,
    string? AdditionalInfo
);

public record OpeningHours(
    TimeOnly OpenTime,
    TimeOnly CloseTime,
    bool IsClosed,
    List<TimeSlot>? Breaks
);
```

### 2.3 Review Aggregate

```csharp
public class Review : BaseEntity
{
    public Guid BusinessId { get; private set; }
    public Guid ReviewerId { get; private set; }
    public Rating Rating { get; private set; }
    public ReviewContent Content { get; private set; }
    public List<string> Tags { get; private set; }
    public ReviewStatus Status { get; private set; }
    public string? BusinessOwnerResponse { get; private set; }
    public DateTime? ResponseDate { get; private set; }
    public bool IsVerifiedPurchase { get; private set; }
    public int HelpfulVotes { get; private set; }
    
    public static Result<Review> Create(
        Guid businessId, 
        Guid reviewerId, 
        Rating rating, 
        ReviewContent content,
        List<string> tags = null);
        
    public Result AddBusinessResponse(string response, Guid businessOwnerId);
    public void AddHelpfulVote();
    public void RemoveHelpfulVote();
    public void MarkAsVerifiedPurchase();
}

public record Rating(int Value) // 1-5 stars
{
    public static Result<Rating> Create(int value)
    {
        if (value < 1 || value > 5)
            return Result<Rating>.Failure("Rating must be between 1 and 5");
        return Result<Rating>.Success(new Rating(value));
    }
}

public record ReviewContent(string Title, string Content, List<string>? Pros, List<string>? Cons)
{
    public static Result<ReviewContent> Create(string title, string content, List<string>? pros = null, List<string>? cons = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result<ReviewContent>.Failure("Review title is required");
        if (string.IsNullOrWhiteSpace(content))
            return Result<ReviewContent>.Failure("Review content is required");
        if (content.Length > 2000)
            return Result<ReviewContent>.Failure("Review content cannot exceed 2000 characters");
            
        return Result<ReviewContent>.Success(new ReviewContent(title.Trim(), content.Trim(), pros, cons));
    }
}
```

---

## 3. Advertisement Bounded Context

### 3.1 Advertisement Aggregate

```csharp
public class Advertisement : BaseEntity
{
    public Guid AdvertiserId { get; private set; }
    public Guid BusinessId { get; private set; }
    public AdContent Content { get; private set; }
    public AdTargeting Targeting { get; private set; }
    public AdBudget Budget { get; private set; }
    public AdSchedule Schedule { get; private set; }
    public AdStatus Status { get; private set; }
    public AdType Type { get; private set; }
    public AdPerformance Performance { get; private set; }
    
    public static Result<Advertisement> Create(
        Guid advertiserId,
        Guid businessId,
        AdContent content,
        AdType type,
        AdTargeting targeting,
        AdBudget budget,
        AdSchedule schedule);
    
    // Performance tracking
    public Result RecordImpression(Guid? viewerId, string ipAddress, string userAgent);
    public Result RecordClick(Guid? clickerId, string ipAddress, string clickedUrl);
    public Result RecordConversion(Guid? converterId, Money conversionValue);
    
    // Campaign management
    public Result Pause(Guid advertiserId);
    public Result Resume(Guid advertiserId);
    public Result UpdateBudget(AdBudget newBudget, Guid advertiserId);
    public bool IsActiveNow();
    public bool HasBudgetRemaining();
    public AdMetrics CalculateMetrics();
}
```

### 3.2 Advertisement Value Objects

```csharp
public record AdContent(
    string Headline,
    string Description,
    string CallToAction,
    string? ImageUrl,
    string? LandingPageUrl
);

public record AdTargeting(
    List<string>? Locations,
    List<BusinessCategory>? BusinessCategories,
    List<ForumCategory>? ForumInterests,
    AgeRange? AgeRange,
    List<string>? Languages,
    bool? IsBusinessOwner
);

public record AdBudget(
    Money DailyBudget,
    Money? TotalBudget,
    Money MaxCostPerClick,
    Money MaxCostPerImpression
);

public record AdSchedule(
    DateTime StartDate,
    DateTime? EndDate,
    List<DayOfWeek>? ActiveDays,
    TimeRange? DailyTimeRange,
    string TimeZone
);

public record AdPerformance(
    int Impressions,
    int Clicks,
    int Conversions,
    Money SpentAmount,
    decimal ClickThroughRate,
    Money AverageCostPerClick
);
```

### 3.3 Ad Campaign Aggregate

```csharp
public class AdCampaign : BaseEntity
{
    private readonly List<Advertisement> _advertisements = new();
    
    public Guid AdvertiserId { get; private set; }
    public CampaignDetails Details { get; private set; }
    public CampaignBudget Budget { get; private set; }
    public CampaignStatus Status { get; private set; }
    public CampaignSchedule Schedule { get; private set; }
    
    public IReadOnlyList<Advertisement> Advertisements => _advertisements.AsReadOnly();
    
    public static Result<AdCampaign> Create(
        Guid advertiserId,
        CampaignDetails details,
        CampaignBudget budget,
        CampaignSchedule schedule);
    
    public Result AddAdvertisement(Advertisement ad);
    public Result RemoveAdvertisement(Guid adId);
    public CampaignMetrics CalculateMetrics();
    public Result OptimizeBudgetAllocation();
}
```

---

## 4. Membership Integration

### 4.1 Membership-Based Feature Access

```csharp
public static class BusinessFeatureAccess
{
    public static bool CanCreateBusinessListing(UserMembershipTier tier)
    {
        return tier >= UserMembershipTier.CommunitySupporter;
    }
    
    public static bool CanPostAdvertisements(UserMembershipTier tier)
    {
        return tier >= UserMembershipTier.BusinessProfessional;
    }
    
    public static bool CanAccessAnalytics(UserMembershipTier tier)
    {
        return tier >= UserMembershipTier.BusinessProfessional;
    }
    
    public static int GetMaxBusinessListings(UserMembershipTier tier)
    {
        return tier switch
        {
            UserMembershipTier.Free => 0,
            UserMembershipTier.CommunitySupporter => 1,
            UserMembershipTier.BusinessProfessional => 3,
            UserMembershipTier.EventOrganizerPro => 1,
            _ => 0
        };
    }
    
    public static Money GetDailyAdBudgetLimit(UserMembershipTier tier)
    {
        return tier switch
        {
            UserMembershipTier.BusinessProfessional => Money.Create(100m, Currency.USD).Value,
            UserMembershipTier.EventOrganizerPro => Money.Create(50m, Currency.USD).Value,
            _ => Money.Zero(Currency.USD)
        };
    }
}
```

### 4.2 Business Professional Features

**Enhanced Business Profile:**
- Unlimited business images (vs 3 for Community Supporter)
- Custom business branding and themes
- Priority placement in search results
- Advanced analytics dashboard
- Customer messaging system

**Advertisement Capabilities:**
- Create and manage ad campaigns
- Target specific demographics and locations
- Real-time performance analytics
- Budget management and optimization
- A/B testing capabilities

---

## 5. API Specifications

### 5.1 Business Directory Endpoints

```
GET    /api/businesses                    # Search businesses with filters
POST   /api/businesses                    # Create business listing (Community Supporter+)
GET    /api/businesses/{id}               # Get business details
PUT    /api/businesses/{id}               # Update business (owner only)
DELETE /api/businesses/{id}               # Delete business (owner only)

GET    /api/businesses/{id}/reviews       # Get business reviews
POST   /api/businesses/{id}/reviews       # Add review (requires past interaction)
PUT    /api/reviews/{id}                  # Update review (author only)
DELETE /api/reviews/{id}                  # Delete review (author/moderator)
POST   /api/reviews/{id}/response         # Business owner response

GET    /api/businesses/{id}/analytics     # Business analytics (Business Professional)
POST   /api/businesses/{id}/images        # Upload business images
DELETE /api/businesses/{id}/images/{imageId} # Delete image
```

### 5.2 Advertisement Endpoints

```
GET    /api/advertisements                # Get user's advertisements (Business Professional)
POST   /api/advertisements                # Create advertisement (Business Professional)
GET    /api/advertisements/{id}           # Get advertisement details
PUT    /api/advertisements/{id}           # Update advertisement
DELETE /api/advertisements/{id}           # Delete advertisement
POST   /api/advertisements/{id}/pause     # Pause advertisement
POST   /api/advertisements/{id}/resume    # Resume advertisement

GET    /api/campaigns                     # Get user's campaigns
POST   /api/campaigns                     # Create campaign
GET    /api/campaigns/{id}                # Get campaign details
PUT    /api/campaigns/{id}                # Update campaign
GET    /api/campaigns/{id}/analytics      # Campaign performance analytics

POST   /api/advertisements/{id}/impression # Record impression (internal)
POST   /api/advertisements/{id}/click     # Record click (internal)
```

### 5.3 Business Search API

```json
GET /api/businesses?category=restaurant&location=new-york&radius=25&sort=rating

Response:
{
  "success": true,
  "data": {
    "businesses": [
      {
        "id": "uuid",
        "name": "Ceylon Spice Restaurant",
        "description": "Authentic Sri Lankan cuisine",
        "category": "Restaurant",
        "location": {
          "address": "123 Main St, New York, NY",
          "coordinates": { "lat": 40.7128, "lng": -74.0060 }
        },
        "contact": {
          "phone": "+1-555-123-4567",
          "email": "info@ceylonspice.com",
          "website": "https://ceylonspice.com"
        },
        "rating": {
          "average": 4.5,
          "count": 124
        },
        "images": [
          {
            "url": "https://cdn.lankaconnect.com/business/123/image1.jpg",
            "caption": "Restaurant interior"
          }
        ],
        "hours": {
          "monday": { "open": "11:00", "close": "22:00" },
          "tuesday": { "open": "11:00", "close": "22:00" }
        },
        "isVerified": true,
        "subscriptionTier": "BusinessProfessional",
        "distance": 2.3
      }
    ],
    "pagination": {
      "page": 1,
      "pageSize": 20,
      "totalCount": 45,
      "totalPages": 3
    },
    "filters": {
      "appliedFilters": {
        "category": "restaurant",
        "location": "new-york",
        "radius": 25
      },
      "availableFilters": {
        "categories": ["restaurant", "grocery", "services"],
        "ratings": [1, 2, 3, 4, 5],
        "verification": ["verified", "unverified"]
      }
    }
  }
}
```

---

## 6. Database Schema

### 6.1 Business Tables

```sql
-- Business profiles
CREATE TABLE business.businesses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id UUID REFERENCES identity.users(id),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    category VARCHAR(100) NOT NULL,
    subcategory VARCHAR(100),
    website_url TEXT,
    social_media JSONB,
    services TEXT[],
    specializations TEXT[],
    
    -- Location
    address JSONB NOT NULL,
    coordinates POINT,
    additional_directions TEXT,
    is_service_area_business BOOLEAN DEFAULT FALSE,
    service_areas TEXT[],
    
    -- Contact
    primary_phone VARCHAR(20) NOT NULL,
    secondary_phone VARCHAR(20),
    business_email VARCHAR(255) NOT NULL,
    contact_email VARCHAR(255),
    
    -- Business hours
    weekly_hours JSONB,
    special_hours JSONB,
    hours_additional_info TEXT,
    
    -- Status and verification
    status VARCHAR(20) DEFAULT 'Active',
    is_verified BOOLEAN DEFAULT FALSE,
    verification_date TIMESTAMP,
    
    -- Subscription
    subscription_tier VARCHAR(50) DEFAULT 'Basic',
    subscription_started_at TIMESTAMP,
    subscription_expires_at TIMESTAMP,
    
    -- Metadata
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    
    -- Indexes
    CONSTRAINT valid_status CHECK (status IN ('Active', 'Inactive', 'Suspended', 'PendingApproval'))
);

-- Business images
CREATE TABLE business.business_images (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    business_id UUID REFERENCES business.businesses(id),
    image_url TEXT NOT NULL,
    caption TEXT,
    display_order INTEGER,
    is_primary BOOLEAN DEFAULT FALSE,
    uploaded_by UUID REFERENCES identity.users(id),
    created_at TIMESTAMP DEFAULT NOW()
);

-- Business reviews
CREATE TABLE business.reviews (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    business_id UUID REFERENCES business.businesses(id),
    reviewer_id UUID REFERENCES identity.users(id),
    rating INTEGER CHECK (rating >= 1 AND rating <= 5),
    title VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    pros TEXT[],
    cons TEXT[],
    tags TEXT[],
    status VARCHAR(20) DEFAULT 'Pending',
    is_verified_purchase BOOLEAN DEFAULT FALSE,
    helpful_votes INTEGER DEFAULT 0,
    
    -- Business response
    business_response TEXT,
    response_date TIMESTAMP,
    responded_by UUID REFERENCES identity.users(id),
    
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    
    UNIQUE(business_id, reviewer_id),
    CONSTRAINT valid_review_status CHECK (status IN ('Pending', 'Approved', 'Rejected', 'Reported'))
);
```

### 6.2 Advertisement Tables

```sql
-- Advertisements
CREATE TABLE advertising.advertisements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    advertiser_id UUID REFERENCES identity.users(id),
    business_id UUID REFERENCES business.businesses(id),
    
    -- Ad content
    headline VARCHAR(255) NOT NULL,
    description TEXT NOT NULL,
    call_to_action VARCHAR(100),
    image_url TEXT,
    landing_page_url TEXT,
    
    -- Ad type and placement
    ad_type VARCHAR(50) NOT NULL,
    placement_types TEXT[] NOT NULL,
    
    -- Targeting
    target_locations TEXT[],
    target_categories TEXT[],
    target_forum_interests TEXT[],
    target_age_range JSONB,
    target_languages TEXT[],
    target_business_owners BOOLEAN,
    
    -- Budget and billing
    daily_budget DECIMAL(10,2) NOT NULL,
    total_budget DECIMAL(10,2),
    max_cost_per_click DECIMAL(6,2),
    max_cost_per_impression DECIMAL(6,2),
    spent_amount DECIMAL(10,2) DEFAULT 0,
    
    -- Schedule
    start_date TIMESTAMP NOT NULL,
    end_date TIMESTAMP,
    active_days INTEGER[], -- Array of day numbers (0=Sunday)
    daily_time_start TIME,
    daily_time_end TIME,
    timezone VARCHAR(50) DEFAULT 'UTC',
    
    -- Performance metrics
    impressions INTEGER DEFAULT 0,
    clicks INTEGER DEFAULT 0,
    conversions INTEGER DEFAULT 0,
    click_through_rate DECIMAL(5,4) DEFAULT 0,
    average_cost_per_click DECIMAL(6,2) DEFAULT 0,
    
    -- Status
    status VARCHAR(20) DEFAULT 'Pending',
    
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    
    CONSTRAINT valid_ad_status CHECK (status IN ('Pending', 'Active', 'Paused', 'Completed', 'Rejected')),
    CONSTRAINT valid_ad_type CHECK (ad_type IN ('Banner', 'SponsoredEvent', 'FeaturedBusiness', 'ForumSponsored'))
);

-- Ad campaigns
CREATE TABLE advertising.ad_campaigns (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    advertiser_id UUID REFERENCES identity.users(id),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    
    -- Campaign budget
    total_budget DECIMAL(10,2) NOT NULL,
    daily_budget DECIMAL(10,2) NOT NULL,
    spent_amount DECIMAL(10,2) DEFAULT 0,
    
    -- Schedule
    start_date TIMESTAMP NOT NULL,
    end_date TIMESTAMP,
    
    -- Status
    status VARCHAR(20) DEFAULT 'Draft',
    
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    
    CONSTRAINT valid_campaign_status CHECK (status IN ('Draft', 'Active', 'Paused', 'Completed', 'Cancelled'))
);

-- Campaign-Advertisement relationship
CREATE TABLE advertising.campaign_advertisements (
    campaign_id UUID REFERENCES advertising.ad_campaigns(id),
    advertisement_id UUID REFERENCES advertising.advertisements(id),
    budget_allocation_percentage DECIMAL(5,2),
    
    PRIMARY KEY (campaign_id, advertisement_id)
);

-- Ad performance tracking (detailed)
CREATE TABLE advertising.ad_interactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    advertisement_id UUID REFERENCES advertising.advertisements(id),
    interaction_type VARCHAR(20) NOT NULL, -- 'impression', 'click', 'conversion'
    user_id UUID REFERENCES identity.users(id), -- NULL for anonymous
    ip_address INET,
    user_agent TEXT,
    referrer_url TEXT,
    landing_page_url TEXT,
    conversion_value DECIMAL(10,2),
    
    created_at TIMESTAMP DEFAULT NOW(),
    
    CONSTRAINT valid_interaction_type CHECK (interaction_type IN ('impression', 'click', 'conversion'))
);
```

---

## 7. Implementation Strategy

### 7.1 Development Phases

**Phase 2A.1: Basic Business Directory (2 weeks)**
1. Business aggregate with basic profile
2. Business listing creation (Community Supporter tier)
3. Business search and filtering
4. Basic business profile pages

**Phase 2A.2: Review System (1 week)**
1. Review aggregate implementation
2. Rating and review creation
3. Business owner response system
4. Review moderation capabilities

**Phase 2A.3: Enhanced Business Features (1 week)**
1. Business Professional tier features
2. Advanced analytics dashboard
3. Business image gallery
4. Customer messaging system

**Phase 2B.1: Advertisement Foundation (2 weeks)**
1. Advertisement aggregate
2. Basic ad creation and management
3. Ad targeting system
4. Budget and billing integration

**Phase 2B.2: Ad Campaign System (1 week)**
1. Campaign aggregate
2. Campaign management interface
3. Budget allocation algorithms
4. Performance optimization

**Phase 2B.3: Analytics & Optimization (1 week)**
1. Real-time performance tracking
2. Advanced analytics dashboard
3. Automated bid optimization
4. A/B testing framework

### 7.2 Key Integration Points

**Membership System:**
- Feature access validation in application layer
- Subscription tier checks in domain services
- Usage limits enforced in aggregates

**Payment Processing:**
- Stripe integration for advertisement billing
- Automated budget deduction
- Invoice generation and payment tracking

**Performance Optimization:**
- Redis caching for business search
- Elasticsearch for advanced business search
- CDN integration for business images
- Real-time analytics with SignalR

### 7.3 Testing Strategy

**Unit Tests:**
- All domain aggregates and value objects
- Business rules and validation logic
- Membership integration logic

**Integration Tests:**
- API endpoint functionality
- Database operations
- Payment processing workflows
- Search functionality

**Performance Tests:**
- Business search performance
- Advertisement serving latency
- Analytics query performance
- Concurrent user handling

---

This comprehensive technical specification provides the foundation for implementing both the business directory and advertisement systems with proper membership integration and clear separation of concerns.