# Business Aggregate Implementation Specification

**Project:** LankaConnect  
**Version:** 1.0  
**Date:** January 2025  
**Status:** Technical Implementation Plan  
**Domain:** Business Directory & Advertisement System

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Domain Analysis](#2-domain-analysis)
3. [Business Aggregate Design](#3-business-aggregate-design)
4. [Value Objects Specification](#4-value-objects-specification)
5. [Review Aggregate Design](#5-review-aggregate-design)
6. [Domain Services & Factories](#6-domain-services--factories)
7. [Repository Interfaces](#7-repository-interfaces)
8. [Domain Events](#8-domain-events)
9. [Aggregate Boundaries](#9-aggregate-boundaries)
10. [Implementation Roadmap](#10-implementation-roadmap)

---

## 1. Executive Summary

This specification defines the complete implementation plan for the Business aggregate in LankaConnect's domain layer, following Clean Architecture and Domain-Driven Design principles. The Business aggregate serves as the core entity for the business directory system, integrating with membership tiers and supporting the advertisement system.

### Key Components
- **Business Aggregate Root**: Core business profile with invariants and business rules
- **Review Aggregate**: Customer reviews and ratings system
- **Value Objects**: 12+ specialized value objects for business data
- **Domain Services**: Business validation, subscription management, and analytics
- **Repository Interfaces**: Data access contracts following existing patterns

### Integration Points
- **Membership System**: Feature access based on Community Supporter and Business Professional tiers
- **User Aggregate**: Business ownership and review authorship
- **Advertisement System**: Business Professional tier advertisement capabilities

---

## 2. Domain Analysis

### 2.1 Existing Domain Patterns Analysis

**Pattern Consistency:**
- ✅ All aggregates inherit from `BaseEntity` with `Id`, `CreatedAt`, `UpdatedAt`
- ✅ Factory methods use `Result<T>` pattern for error handling
- ✅ Value objects inherit from `ValueObject` base class
- ✅ Private constructors with public factory methods
- ✅ Rich domain model with business logic encapsulation

**Observed Patterns from User, Event, Community Aggregates:**
```csharp
// Factory Method Pattern
public static Result<Entity> Create(parameters...)

// Invariant Enforcement
private readonly List<Child> _children = new();
public IReadOnlyList<Child> Children => _children.AsReadOnly();

// Business Logic Methods
public Result BusinessOperation(parameters...)
{
    // Validation
    // Business logic
    // State change
    MarkAsUpdated();
    return Result.Success();
}
```

### 2.2 Technical Architecture Requirements

**Membership Integration:**
- Community Supporter ($5): Basic business listing (1 per user)
- Business Professional ($25): Enhanced features + advertisements
- Free users: Browse only

**Business Rules:**
- Verified businesses get priority in search results
- Business owners can respond to reviews
- Subscription tier determines feature access
- Business status controls visibility and operations

---

## 3. Business Aggregate Design

### 3.1 Business Aggregate Root Entity

```csharp
namespace LankaConnect.Domain.Business;

public class Business : BaseEntity
{
    private readonly List<Review> _reviews = new();
    private readonly List<BusinessImage> _images = new();
    private readonly List<ServiceOffering> _serviceOfferings = new();
    
    // Core Properties
    public BusinessProfile Profile { get; private set; }
    public BusinessLocation Location { get; private set; }
    public ContactInformation ContactInfo { get; private set; }
    public BusinessCategory Category { get; private set; }
    public BusinessHours Hours { get; private set; }
    public Guid OwnerId { get; private set; }
    public BusinessStatus Status { get; private set; }
    public BusinessSubscription Subscription { get; private set; }
    public BusinessVerification Verification { get; private set; }
    
    // Computed Properties
    public IReadOnlyList<Review> Reviews => _reviews.AsReadOnly();
    public IReadOnlyList<BusinessImage> Images => _images.AsReadOnly();
    public IReadOnlyList<ServiceOffering> ServiceOfferings => _serviceOfferings.AsReadOnly();
    public BusinessRating Rating => CalculateRating();
    public int ReviewCount => _reviews.Count(r => r.Status == ReviewStatus.Approved);
    public bool IsVerified => Verification.IsVerified;
    
    // Private constructor for EF Core
    private Business() 
    {
        Profile = null!;
        Location = null!;
        ContactInfo = null!;
        Hours = null!;
        Subscription = null!;
        Verification = null!;
    }
    
    // Factory method
    public static Result<Business> Create(
        BusinessProfile profile,
        BusinessLocation location,
        ContactInformation contactInfo,
        BusinessCategory category,
        BusinessHours hours,
        Guid ownerId,
        BusinessSubscriptionTier subscriptionTier)
    {
        // Validation logic
        if (profile == null)
            return Result<Business>.Failure("Business profile is required");
        if (location == null)
            return Result<Business>.Failure("Business location is required");
        if (contactInfo == null)
            return Result<Business>.Failure("Contact information is required");
        if (ownerId == Guid.Empty)
            return Result<Business>.Failure("Owner ID is required");
        if (hours == null)
            return Result<Business>.Failure("Business hours are required");
            
        var subscriptionResult = BusinessSubscription.Create(subscriptionTier, DateTime.UtcNow);
        if (subscriptionResult.IsFailure)
            return Result<Business>.Failure(subscriptionResult.Error);
            
        var business = new Business
        {
            Profile = profile,
            Location = location,
            ContactInfo = contactInfo,
            Category = category,
            Hours = hours,
            OwnerId = ownerId,
            Status = BusinessStatus.PendingApproval,
            Subscription = subscriptionResult.Value,
            Verification = BusinessVerification.CreateUnverified()
        };
        
        return Result<Business>.Success(business);
    }
    
    // Business Operations
    public Result UpdateProfile(BusinessProfile newProfile, Guid editorId)
    {
        if (newProfile == null)
            return Result.Failure("Profile is required");
        if (editorId != OwnerId)
            return Result.Failure("Only business owner can update profile");
        if (Status == BusinessStatus.Suspended)
            return Result.Failure("Cannot update suspended business");
            
        Profile = newProfile;
        MarkAsUpdated();
        return Result.Success();
    }
    
    public Result AddReview(Review review)
    {
        if (review == null)
            return Result.Failure("Review is required");
        if (review.BusinessId != Id)
            return Result.Failure("Review does not belong to this business");
        if (Status != BusinessStatus.Active)
            return Result.Failure("Cannot add review to inactive business");
        if (_reviews.Any(r => r.ReviewerId == review.ReviewerId))
            return Result.Failure("User has already reviewed this business");
            
        _reviews.Add(review);
        MarkAsUpdated();
        return Result.Success();
    }
    
    public Result RespondToReview(Guid reviewId, string response, Guid responderId)
    {
        if (responderId != OwnerId)
            return Result.Failure("Only business owner can respond to reviews");
        if (string.IsNullOrWhiteSpace(response))
            return Result.Failure("Response cannot be empty");
            
        var review = _reviews.FirstOrDefault(r => r.Id == reviewId);
        if (review == null)
            return Result.Failure("Review not found");
            
        return review.AddBusinessResponse(response, responderId);
    }
    
    public Result AddImage(BusinessImage image, Guid uploaderId)
    {
        if (image == null)
            return Result.Failure("Image is required");
        if (uploaderId != OwnerId)
            return Result.Failure("Only business owner can upload images");
        if (!CanAddMoreImages())
            return Result.Failure("Maximum image limit reached for current subscription");
            
        _images.Add(image);
        MarkAsUpdated();
        return Result.Success();
    }
    
    public Result AddServiceOffering(ServiceOffering offering)
    {
        if (offering == null)
            return Result.Failure("Service offering is required");
        if (_serviceOfferings.Any(s => s.Name.Equals(offering.Name, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure("Service offering already exists");
            
        _serviceOfferings.Add(offering);
        MarkAsUpdated();
        return Result.Success();
    }
    
    public Result UpgradeSubscription(BusinessSubscriptionTier newTier)
    {
        if (newTier <= Subscription.Tier)
            return Result.Failure("New tier must be higher than current tier");
            
        var newSubscriptionResult = BusinessSubscription.Create(newTier, DateTime.UtcNow);
        if (newSubscriptionResult.IsFailure)
            return Result.Failure(newSubscriptionResult.Error);
            
        Subscription = newSubscriptionResult.Value;
        MarkAsUpdated();
        return Result.Success();
    }
    
    public void Activate()
    {
        if (Status == BusinessStatus.PendingApproval)
        {
            Status = BusinessStatus.Active;
            MarkAsUpdated();
        }
    }
    
    public Result Suspend(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure("Suspension reason is required");
            
        Status = BusinessStatus.Suspended;
        MarkAsUpdated();
        return Result.Success();
    }
    
    public void Verify(BusinessVerification verification)
    {
        Verification = verification;
        MarkAsUpdated();
    }
    
    // Business Logic Helpers
    public bool CanAddMoreImages()
    {
        var limit = Subscription.Tier switch
        {
            BusinessSubscriptionTier.Basic => 3,
            BusinessSubscriptionTier.Professional => 20,
            _ => 1
        };
        return _images.Count < limit;
    }
    
    public bool CanPostAdvertisements()
    {
        return Subscription.Tier >= BusinessSubscriptionTier.Professional && 
               Status == BusinessStatus.Active;
    }
    
    public bool CanUserAddReview(Guid userId)
    {
        return Status == BusinessStatus.Active && 
               userId != OwnerId && 
               !_reviews.Any(r => r.ReviewerId == userId);
    }
    
    private BusinessRating CalculateRating()
    {
        var approvedReviews = _reviews.Where(r => r.Status == ReviewStatus.Approved).ToList();
        
        if (!approvedReviews.Any())
            return BusinessRating.CreateUnrated();
            
        var averageRating = approvedReviews.Average(r => r.Rating.Value);
        var totalReviews = approvedReviews.Count;
        
        return BusinessRating.Create(averageRating, totalReviews).Value;
    }
}
```

### 3.2 Business Invariants and Rules

**Core Invariants:**
1. Business must have an owner (User aggregate)
2. Business profile, location, and contact info are required
3. Only business owner can update business information
4. Reviews can only be added by non-owners
5. One review per user per business
6. Subscription tier determines feature access

**Business Rules:**
1. **Status Transitions:** PendingApproval → Active → Suspended/Inactive
2. **Verification:** Only admin/moderators can verify businesses
3. **Image Limits:** Basic (1), Community Supporter (3), Professional (20)
4. **Advertisement Access:** Business Professional tier only
5. **Review Responses:** Only business owners can respond to reviews

---

## 4. Value Objects Specification

### 4.1 Missing Value Objects to Implement

#### BusinessImage Value Object
```csharp
public class BusinessImage : ValueObject
{
    public string ImageUrl { get; }
    public string? Caption { get; }
    public int DisplayOrder { get; }
    public bool IsPrimary { get; }
    public Guid UploadedBy { get; }
    public DateTime UploadedAt { get; }
    
    public static Result<BusinessImage> Create(
        string imageUrl, 
        string? caption, 
        int displayOrder, 
        bool isPrimary,
        Guid uploadedBy)
    {
        // Validation logic
        if (string.IsNullOrWhiteSpace(imageUrl))
            return Result<BusinessImage>.Failure("Image URL is required");
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
            return Result<BusinessImage>.Failure("Invalid image URL format");
        if (displayOrder < 0)
            return Result<BusinessImage>.Failure("Display order cannot be negative");
        if (uploadedBy == Guid.Empty)
            return Result<BusinessImage>.Failure("Uploader ID is required");
            
        return Result<BusinessImage>.Success(new BusinessImage(
            imageUrl.Trim(), 
            caption?.Trim(), 
            displayOrder, 
            isPrimary,
            uploadedBy,
            DateTime.UtcNow));
    }
}
```

#### ServiceOffering Value Object
```csharp
public class ServiceOffering : ValueObject
{
    public string Name { get; }
    public string Description { get; }
    public Money? Price { get; }
    public TimeRange? Duration { get; }
    public bool IsActive { get; }
    
    public static Result<ServiceOffering> Create(
        string name,
        string description,
        Money? price = null,
        TimeRange? duration = null,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<ServiceOffering>.Failure("Service name is required");
        if (string.IsNullOrWhiteSpace(description))
            return Result<ServiceOffering>.Failure("Service description is required");
        if (name.Length > 100)
            return Result<ServiceOffering>.Failure("Service name cannot exceed 100 characters");
        if (description.Length > 500)
            return Result<ServiceOffering>.Failure("Service description cannot exceed 500 characters");
            
        return Result<ServiceOffering>.Success(new ServiceOffering(
            name.Trim(),
            description.Trim(),
            price,
            duration,
            isActive));
    }
}
```

#### BusinessSubscription Value Object
```csharp
public class BusinessSubscription : ValueObject
{
    public BusinessSubscriptionTier Tier { get; }
    public DateTime StartDate { get; }
    public DateTime? EndDate { get; }
    public bool IsActive { get; }
    public Money MonthlyFee { get; }
    
    public static Result<BusinessSubscription> Create(
        BusinessSubscriptionTier tier, 
        DateTime startDate,
        DateTime? endDate = null)
    {
        var monthlyFee = tier switch
        {
            BusinessSubscriptionTier.Basic => Money.Zero(Currency.USD),
            BusinessSubscriptionTier.CommunitySupporter => Money.Create(5m, Currency.USD).Value,
            BusinessSubscriptionTier.Professional => Money.Create(25m, Currency.USD).Value,
            _ => Money.Zero(Currency.USD)
        };
        
        var isActive = endDate == null || endDate > DateTime.UtcNow;
        
        return Result<BusinessSubscription>.Success(new BusinessSubscription(
            tier, startDate, endDate, isActive, monthlyFee));
    }
}

public enum BusinessSubscriptionTier
{
    Basic = 0,
    CommunitySupporter = 1,
    Professional = 2
}
```

#### BusinessVerification Value Object
```csharp
public class BusinessVerification : ValueObject
{
    public bool IsVerified { get; }
    public DateTime? VerifiedAt { get; }
    public Guid? VerifiedBy { get; }
    public string? VerificationNotes { get; }
    public BusinessVerificationLevel Level { get; }
    
    public static BusinessVerification CreateUnverified() =>
        new(false, null, null, null, BusinessVerificationLevel.None);
    
    public static Result<BusinessVerification> CreateVerified(
        Guid verifiedBy, 
        string? notes, 
        BusinessVerificationLevel level)
    {
        if (verifiedBy == Guid.Empty)
            return Result<BusinessVerification>.Failure("Verifier ID is required");
        if (level == BusinessVerificationLevel.None)
            return Result<BusinessVerification>.Failure("Verification level must be specified");
            
        return Result<BusinessVerification>.Success(new BusinessVerification(
            true, DateTime.UtcNow, verifiedBy, notes?.Trim(), level));
    }
}

public enum BusinessVerificationLevel
{
    None,
    Basic,      // Phone/email verified
    Enhanced,   // Business registration verified
    Premium     // Full documentation verified
}
```

#### BusinessRating Value Object
```csharp
public class BusinessRating : ValueObject
{
    public decimal AverageRating { get; }
    public int TotalReviews { get; }
    public bool HasRatings => TotalReviews > 0;
    
    public static BusinessRating CreateUnrated() => new(0, 0);
    
    public static Result<BusinessRating> Create(decimal averageRating, int totalReviews)
    {
        if (averageRating < 0 || averageRating > 5)
            return Result<BusinessRating>.Failure("Average rating must be between 0 and 5");
        if (totalReviews < 0)
            return Result<BusinessRating>.Failure("Total reviews cannot be negative");
        if (totalReviews == 0 && averageRating != 0)
            return Result<BusinessRating>.Failure("Average rating must be 0 when no reviews exist");
            
        return Result<BusinessRating>.Success(new BusinessRating(averageRating, totalReviews));
    }
    
    public string GetRatingText() => AverageRating switch
    {
        >= 4.5m => "Excellent",
        >= 4.0m => "Very Good",
        >= 3.5m => "Good",
        >= 3.0m => "Average",
        >= 2.0m => "Below Average",
        > 0m => "Poor",
        _ => "Not Rated"
    };
}
```

### 4.2 Update Existing Value Objects

The existing `BusinessProfile`, `BusinessLocation`, `ContactInformation`, and `BusinessHours` value objects are well-designed and follow the established patterns. Minor updates needed:

1. **BusinessProfile**: Add validation for services/specializations length
2. **ContactInformation**: Already well-implemented but needs alignment with tech spec (primary/secondary phone, business/contact email)
3. **BusinessHours**: Add special hours support (holidays, temporary closures)

---

## 5. Review Aggregate Design

### 5.1 Review Aggregate Root Entity

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
    public Guid? RespondedBy { get; private set; }
    public bool IsVerifiedPurchase { get; private set; }
    public int HelpfulVotes { get; private set; }
    public ReviewMetadata Metadata { get; private set; }
    
    // Factory method
    public static Result<Review> Create(
        Guid businessId,
        Guid reviewerId,
        Rating rating,
        ReviewContent content,
        List<string>? tags = null)
    {
        if (businessId == Guid.Empty)
            return Result<Review>.Failure("Business ID is required");
        if (reviewerId == Guid.Empty)
            return Result<Review>.Failure("Reviewer ID is required");
        if (rating == null)
            return Result<Review>.Failure("Rating is required");
        if (content == null)
            return Result<Review>.Failure("Review content is required");
            
        var review = new Review
        {
            BusinessId = businessId,
            ReviewerId = reviewerId,
            Rating = rating,
            Content = content,
            Tags = tags ?? new List<string>(),
            Status = ReviewStatus.Pending,
            HelpfulVotes = 0,
            Metadata = ReviewMetadata.Create()
        };
        
        return Result<Review>.Success(review);
    }
    
    public Result AddBusinessResponse(string response, Guid businessOwnerId)
    {
        if (string.IsNullOrWhiteSpace(response))
            return Result.Failure("Response cannot be empty");
        if (response.Length > 1000)
            return Result.Failure("Response cannot exceed 1000 characters");
        if (BusinessOwnerResponse != null)
            return Result.Failure("Business has already responded to this review");
        if (Status != ReviewStatus.Approved)
            return Result.Failure("Can only respond to approved reviews");
            
        BusinessOwnerResponse = response.Trim();
        ResponseDate = DateTime.UtcNow;
        RespondedBy = businessOwnerId;
        MarkAsUpdated();
        return Result.Success();
    }
    
    public void AddHelpfulVote()
    {
        HelpfulVotes++;
        MarkAsUpdated();
    }
    
    public void RemoveHelpfulVote()
    {
        if (HelpfulVotes > 0)
        {
            HelpfulVotes--;
            MarkAsUpdated();
        }
    }
    
    public void Approve()
    {
        Status = ReviewStatus.Approved;
        MarkAsUpdated();
    }
    
    public Result Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure("Rejection reason is required");
            
        Status = ReviewStatus.Rejected;
        MarkAsUpdated();
        return Result.Success();
    }
    
    public void MarkAsVerifiedPurchase()
    {
        IsVerifiedPurchase = true;
        MarkAsUpdated();
    }
}
```

### 5.2 Review Value Objects

#### ReviewMetadata Value Object
```csharp
public class ReviewMetadata : ValueObject
{
    public string? IpAddress { get; }
    public string? UserAgent { get; }
    public DateTime SubmittedAt { get; }
    public bool IsFromMobileApp { get; }
    public string? DeviceInfo { get; }
    
    public static ReviewMetadata Create(
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceInfo = null)
    {
        var isFromMobileApp = userAgent?.Contains("Mobile", StringComparison.OrdinalIgnoreCase) == true;
        
        return new ReviewMetadata(
            ipAddress,
            userAgent,
            DateTime.UtcNow,
            isFromMobileApp,
            deviceInfo);
    }
}
```

---

## 6. Domain Services & Factories

### 6.1 BusinessFactory Domain Service

```csharp
public interface IBusinessFactory
{
    Task<Result<Business>> CreateBusinessAsync(
        CreateBusinessRequest request,
        Guid ownerId,
        BusinessSubscriptionTier subscriptionTier);
        
    Task<Result<Business>> CreateFromImportAsync(
        BusinessImportData importData,
        Guid ownerId);
}

public class BusinessFactory : IBusinessFactory
{
    private readonly IBusinessValidationService _validationService;
    private readonly IGeolocationService _geolocationService;
    
    public async Task<Result<Business>> CreateBusinessAsync(
        CreateBusinessRequest request,
        Guid ownerId,
        BusinessSubscriptionTier subscriptionTier)
    {
        // Validate business data
        var validationResult = await _validationService.ValidateBusinessDataAsync(request);
        if (validationResult.IsFailure)
            return Result<Business>.Failure(validationResult.Errors);
            
        // Create value objects
        var profileResult = BusinessProfile.Create(
            request.Name, request.Description, request.Website,
            request.SocialMedia, request.Services, request.Specializations);
        if (profileResult.IsFailure)
            return Result<Business>.Failure(profileResult.Error);
            
        // Geocode address if needed
        var locationResult = await CreateBusinessLocationAsync(request.Address);
        if (locationResult.IsFailure)
            return Result<Business>.Failure(locationResult.Error);
            
        var contactResult = ContactInformation.Create(
            request.Email, request.Phone, request.Website);
        if (contactResult.IsFailure)
            return Result<Business>.Failure(contactResult.Error);
            
        var hoursResult = BusinessHours.Create(request.BusinessHours);
        if (hoursResult.IsFailure)
            return Result<Business>.Failure(hoursResult.Error);
            
        // Create business aggregate
        return Business.Create(
            profileResult.Value,
            locationResult.Value,
            contactResult.Value,
            request.Category,
            hoursResult.Value,
            ownerId,
            subscriptionTier);
    }
}
```

### 6.2 Business Domain Services

#### BusinessValidationService
```csharp
public interface IBusinessValidationService
{
    Task<Result> ValidateBusinessDataAsync(CreateBusinessRequest request);
    Task<Result> ValidateBusinessNameAvailabilityAsync(string name, Guid? excludeBusinessId = null);
    Result ValidateSubscriptionFeatureAccess(BusinessSubscriptionTier tier, string feature);
}
```

#### BusinessAnalyticsService
```csharp
public interface IBusinessAnalyticsService
{
    Task<BusinessAnalytics> GenerateAnalyticsAsync(Guid businessId, DateRange period);
    Task<BusinessPerformanceMetrics> CalculatePerformanceMetricsAsync(Guid businessId);
    Task<List<BusinessInsight>> GenerateInsightsAsync(Guid businessId);
}
```

#### BusinessSearchService
```csharp
public interface IBusinessSearchService
{
    Task<SearchResult<Business>> SearchBusinessesAsync(BusinessSearchCriteria criteria);
    Task<List<Business>> GetNearbyBusinessesAsync(GeoCoordinate location, double radiusKm);
    Task<List<Business>> GetBusinessesByCategoryAsync(BusinessCategory category, int skip = 0, int take = 20);
}
```

---

## 7. Repository Interfaces

### 7.1 Business Repository Interface

```csharp
public interface IBusinessRepository : IRepository<Business>
{
    // Queries
    Task<Business?> GetByIdWithReviewsAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task<Business?> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<List<Business>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<List<Business>> GetByCategoryAsync(BusinessCategory category, int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task<List<Business>> GetNearLocationAsync(decimal latitude, decimal longitude, double radiusKm, int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task<List<Business>> GetVerifiedBusinessesAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task<List<Business>> GetBySubscriptionTierAsync(BusinessSubscriptionTier tier, int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<int> CountByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    
    // Search
    Task<SearchResult<Business>> SearchAsync(BusinessSearchCriteria criteria, CancellationToken cancellationToken = default);
    
    // Analytics
    Task<BusinessAnalyticsSummary> GetAnalyticsSummaryAsync(Guid businessId, DateRange period, CancellationToken cancellationToken = default);
}
```

### 7.2 Review Repository Interface

```csharp
public interface IReviewRepository : IRepository<Review>
{
    Task<List<Review>> GetByBusinessIdAsync(Guid businessId, int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task<Review?> GetByBusinessAndReviewerAsync(Guid businessId, Guid reviewerId, CancellationToken cancellationToken = default);
    Task<List<Review>> GetByReviewerIdAsync(Guid reviewerId, int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task<List<Review>> GetPendingReviewsAsync(int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task<List<Review>> GetRecentReviewsAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<bool> HasUserReviewedBusinessAsync(Guid businessId, Guid userId, CancellationToken cancellationToken = default);
    Task<BusinessRatingStatistics> GetRatingStatisticsAsync(Guid businessId, CancellationToken cancellationToken = default);
}
```

---

## 8. Domain Events

### 8.1 Business Domain Events

```csharp
// Business lifecycle events
public sealed record BusinessCreated(Guid BusinessId, Guid OwnerId, string BusinessName, BusinessCategory Category, DateTime CreatedAt) : DomainEvent;
public sealed record BusinessActivated(Guid BusinessId, DateTime ActivatedAt) : DomainEvent;
public sealed record BusinessSuspended(Guid BusinessId, string Reason, DateTime SuspendedAt) : DomainEvent;
public sealed record BusinessVerified(Guid BusinessId, BusinessVerificationLevel Level, Guid VerifiedBy, DateTime VerifiedAt) : DomainEvent;

// Business profile events
public sealed record BusinessProfileUpdated(Guid BusinessId, string PreviousName, string NewName, DateTime UpdatedAt) : DomainEvent;
public sealed record BusinessImageAdded(Guid BusinessId, string ImageUrl, bool IsPrimary, DateTime AddedAt) : DomainEvent;
public sealed record BusinessHoursUpdated(Guid BusinessId, DateTime UpdatedAt) : DomainEvent;

// Subscription events
public sealed record BusinessSubscriptionUpgraded(Guid BusinessId, BusinessSubscriptionTier PreviousTier, BusinessSubscriptionTier NewTier, DateTime UpgradedAt) : DomainEvent;
public sealed record BusinessSubscriptionExpired(Guid BusinessId, BusinessSubscriptionTier Tier, DateTime ExpiredAt) : DomainEvent;
```

### 8.2 Review Domain Events

```csharp
// Review lifecycle events
public sealed record ReviewSubmitted(Guid ReviewId, Guid BusinessId, Guid ReviewerId, int Rating, DateTime SubmittedAt) : DomainEvent;
public sealed record ReviewApproved(Guid ReviewId, Guid BusinessId, DateTime ApprovedAt) : DomainEvent;
public sealed record ReviewRejected(Guid ReviewId, Guid BusinessId, string Reason, DateTime RejectedAt) : DomainEvent;

// Review interaction events
public sealed record BusinessRespondedToReview(Guid ReviewId, Guid BusinessId, Guid ResponderId, DateTime RespondedAt) : DomainEvent;
public sealed record ReviewMarkedHelpful(Guid ReviewId, Guid BusinessId, Guid UserId, DateTime MarkedAt) : DomainEvent;
public sealed record ReviewVerifiedPurchase(Guid ReviewId, Guid BusinessId, DateTime VerifiedAt) : DomainEvent;
```

---

## 9. Aggregate Boundaries

### 9.1 Business Aggregate Boundary

**Includes:**
- ✅ Business root entity
- ✅ Business value objects (Profile, Location, ContactInfo, Hours, Subscription, Verification)
- ✅ BusinessImage entities (composition)
- ✅ ServiceOffering entities (composition)

**Excludes:**
- ❌ Review entities (separate aggregate)
- ❌ User entities (different bounded context)
- ❌ Advertisement entities (different aggregate)

### 9.2 Review Aggregate Boundary

**Includes:**
- ✅ Review root entity
- ✅ Review value objects (Rating, ReviewContent, ReviewMetadata)

**Excludes:**
- ❌ Business entities (referenced by ID only)
- ❌ User entities (referenced by ID only)

### 9.3 Cross-Aggregate References

```csharp
// Business -> User (Owner)
public Guid OwnerId { get; private set; } // Reference by ID

// Review -> Business (Target)
public Guid BusinessId { get; private set; } // Reference by ID

// Review -> User (Reviewer)
public Guid ReviewerId { get; private set; } // Reference by ID

// Business -> Advertisement (Feature check only)
// No direct reference - handled by domain service
```

### 9.4 Aggregate Transaction Boundaries

**Single Transaction:**
- Create/update business with images and services
- Add review to business
- Respond to review

**Multiple Transactions (Eventual Consistency):**
- User creates business → Update user statistics
- Review submitted → Recalculate business rating
- Business verified → Send notifications

---

## 10. Implementation Roadmap

### 10.1 Phase 1: Core Business Aggregate (Week 1)

**Tasks:**
1. ✅ Implement missing value objects (BusinessImage, ServiceOffering, BusinessSubscription, etc.)
2. ✅ Create Business aggregate root entity
3. ✅ Implement business factory and validation services
4. ✅ Create business repository interface
5. ✅ Write comprehensive unit tests for Business aggregate

**Deliverables:**
- Complete Business aggregate implementation
- Business factory and validation services
- 90%+ test coverage for Business domain

### 10.2 Phase 2: Review Aggregate (Week 1)

**Tasks:**
1. ✅ Implement Review aggregate root entity
2. ✅ Create review value objects (ReviewMetadata)
3. ✅ Update existing Rating and ReviewContent value objects
4. ✅ Create review repository interface
5. ✅ Write comprehensive unit tests for Review aggregate

**Deliverables:**
- Complete Review aggregate implementation
- Review repository interface
- 90%+ test coverage for Review domain

### 10.3 Phase 3: Domain Services & Integration (Week 1)

**Tasks:**
1. ✅ Implement business validation service
2. ✅ Create business analytics service interface
3. ✅ Implement business search service interface
4. ✅ Add domain events for business and review operations
5. ✅ Integration tests for aggregate interactions

**Deliverables:**
- Complete domain services implementation
- Domain events for business operations
- Integration test suite

### 10.4 Phase 4: Repository Implementation & Infrastructure (Week 2)

**Tasks:**
1. ✅ Implement Entity Framework configurations for Business and Review
2. ✅ Create repository implementations
3. ✅ Database migration scripts
4. ✅ Performance optimization (indexes, queries)
5. ✅ Integration with membership system

**Deliverables:**
- Complete infrastructure layer implementation
- Database schema and migrations
- Performance-optimized data access

### 10.5 Success Criteria

**Domain Layer:**
- ✅ All business rules enforced at domain level
- ✅ Rich domain model with proper encapsulation
- ✅ 90%+ unit test coverage
- ✅ All invariants properly enforced

**Integration:**
- ✅ Seamless integration with User aggregate
- ✅ Membership tier feature access working
- ✅ Domain events properly raised and handled
- ✅ Cross-aggregate consistency maintained

**Performance:**
- ✅ Business search < 500ms response time
- ✅ Review aggregation < 200ms response time
- ✅ Image upload and processing optimized
- ✅ Analytics queries performant

---

## Conclusion

This specification provides a comprehensive implementation plan for the Business and Review aggregates in LankaConnect's domain layer. The design follows established DDD patterns, maintains consistency with existing domain code, and supports the technical architecture requirements for the business directory and advertisement system.

The implementation will create a robust, maintainable, and scalable foundation for the business directory features while seamlessly integrating with the membership system and supporting future advertisement capabilities.