using LankaConnect.Domain.Common;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Business.Enums;

namespace LankaConnect.Domain.Business;

public class Business : AggregateRoot
{
    private readonly List<Service> _services = new();
    private readonly List<Review> _reviews = new();
    private readonly List<BusinessImage> _images = new();

    public BusinessProfile Profile { get; private set; }
    public BusinessLocation Location { get; private set; }
    public ContactInformation ContactInfo { get; private set; }
    public BusinessHours Hours { get; private set; }
    public BusinessCategory Category { get; private set; }
    public BusinessStatus Status { get; private set; }
    public Guid OwnerId { get; private set; }
    public decimal? Rating { get; private set; }
    public int ReviewCount { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    
    // Navigation properties
    public IReadOnlyCollection<Service> Services => _services.AsReadOnly();
    public IReadOnlyCollection<Review> Reviews => _reviews.AsReadOnly();
    public IReadOnlyCollection<BusinessImage> Images => _images.AsReadOnly();

    private Business() // EF Core constructor
    {
        Profile = null!;
        Location = null!;
        ContactInfo = null!;
        Hours = null!;
    }

    private Business(
        BusinessProfile profile,
        BusinessLocation location,
        ContactInformation contactInfo,
        BusinessHours hours,
        BusinessCategory category,
        Guid ownerId) : base()
    {
        Profile = profile;
        Location = location;
        ContactInfo = contactInfo;
        Hours = hours;
        Category = category;
        OwnerId = ownerId;
        Status = BusinessStatus.PendingApproval;
        Rating = null;
        ReviewCount = 0;
        IsVerified = false;
        VerifiedAt = null;
    }

    public static Result<Business> Create(
        BusinessProfile profile,
        BusinessLocation location,
        ContactInformation contactInfo,
        BusinessHours hours,
        BusinessCategory category,
        Guid ownerId)
    {
        if (profile == null)
            return Result<Business>.Failure("Business profile is required");
        
        if (location == null)
            return Result<Business>.Failure("Business location is required");
        
        if (contactInfo == null)
            return Result<Business>.Failure("Contact information is required");
        
        if (hours == null)
            return Result<Business>.Failure("Business hours are required");
        
        if (ownerId == Guid.Empty)
            return Result<Business>.Failure("Owner ID is required");

        var business = new Business(profile, location, contactInfo, hours, category, ownerId);
        
        return Result<Business>.Success(business);
    }

    public Result UpdateProfile(BusinessProfile profile)
    {
        if (profile == null)
            return Result.Failure("Business profile is required");

        Profile = profile;
        MarkAsUpdated();
        
        return Result.Success();
    }

    public Result UpdateLocation(BusinessLocation location)
    {
        if (location == null)
            return Result.Failure("Business location is required");

        Location = location;
        MarkAsUpdated();
        
        return Result.Success();
    }

    public Result UpdateContactInfo(ContactInformation contactInfo)
    {
        if (contactInfo == null)
            return Result.Failure("Contact information is required");

        ContactInfo = contactInfo;
        MarkAsUpdated();
        
        return Result.Success();
    }

    public Result UpdateHours(BusinessHours hours)
    {
        if (hours == null)
            return Result.Failure("Business hours are required");

        Hours = hours;
        MarkAsUpdated();
        
        return Result.Success();
    }

    public Result UpdateCategory(BusinessCategory category)
    {
        Category = category;
        MarkAsUpdated();
        
        return Result.Success();
    }

    public Result AddService(Service service)
    {
        if (service == null)
            return Result.Failure("Service is required");

        if (_services.Any(s => s.Name.Equals(service.Name, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure("Service with this name already exists");

        _services.Add(service);
        MarkAsUpdated();
        
        return Result.Success();
    }

    public Result RemoveService(Guid serviceId)
    {
        var service = _services.FirstOrDefault(s => s.Id == serviceId);
        if (service == null)
            return Result.Failure("Service not found");

        _services.Remove(service);
        MarkAsUpdated();
        
        return Result.Success();
    }

    public Result AddReview(Review review)
    {
        if (review == null)
            return Result.Failure("Review is required");

        _reviews.Add(review);
        RecalculateRating();
        MarkAsUpdated();
        
        return Result.Success();
    }

    public Result Activate()
    {
        if (Status == BusinessStatus.Active)
            return Result.Failure("Business is already active");

        Status = BusinessStatus.Active;
        MarkAsUpdated();
        
        return Result.Success();
    }

    public Result Suspend()
    {
        if (Status == BusinessStatus.Suspended)
            return Result.Failure("Business is already suspended");

        Status = BusinessStatus.Suspended;
        MarkAsUpdated();
        
        return Result.Success();
    }

    public Result Deactivate()
    {
        Status = BusinessStatus.Inactive;
        MarkAsUpdated();
        
        return Result.Success();
    }

    public Result Verify()
    {
        if (IsVerified)
            return Result.Failure("Business is already verified");

        IsVerified = true;
        VerifiedAt = DateTime.UtcNow;
        MarkAsUpdated();
        
        return Result.Success();
    }

    public Result Unverify()
    {
        if (!IsVerified)
            return Result.Failure("Business is not verified");

        IsVerified = false;
        VerifiedAt = null;
        MarkAsUpdated();
        
        return Result.Success();
    }

    private void RecalculateRating()
    {
        var activeReviews = _reviews.Where(r => r.Status == ReviewStatus.Approved).ToList();
        
        if (activeReviews.Count == 0)
        {
            Rating = null;
            ReviewCount = 0;
            return;
        }

        Rating = (decimal)activeReviews.Average(r => r.Rating.Value);
        ReviewCount = activeReviews.Count;
    }

    public bool IsOpenAt(DateTime dateTime)
    {
        if (Status != BusinessStatus.Active)
            return false;

        return Hours.IsOpenAt(dateTime);
    }

    public double? DistanceTo(BusinessLocation location)
    {
        return Location.DistanceTo(location);
    }

    // Image management methods
    public Result AddImage(BusinessImage image)
    {
        if (image == null)
            return Result.Failure("Business image is required");

        // Ensure only one primary image exists
        if (image.IsPrimary)
        {
            // Remove primary status from existing images
            for (int i = 0; i < _images.Count; i++)
            {
                if (_images[i].IsPrimary)
                {
                    _images[i] = _images[i].RemovePrimaryStatus();
                }
            }
        }

        // Check for duplicate images (same URL)
        if (_images.Any(img => img.OriginalUrl.Equals(image.OriginalUrl, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure("An image with this URL already exists");

        _images.Add(image);
        MarkAsUpdated();

        return Result.Success();
    }

    public Result RemoveImage(string imageId)
    {
        var image = _images.FirstOrDefault(img => img.Id == imageId);
        if (image == null)
            return Result.Failure("Image not found");

        var wasRemoved = _images.Remove(image);
        if (wasRemoved)
        {
            // If we removed the primary image, set another image as primary if available
            if (image.IsPrimary && _images.Count > 0)
            {
                var newPrimaryImage = _images.OrderBy(img => img.DisplayOrder).First();
                var index = _images.IndexOf(newPrimaryImage);
                _images[index] = newPrimaryImage.SetAsPrimary();
            }

            MarkAsUpdated();
        }

        return Result.Success();
    }

    public Result SetPrimaryImage(string imageId)
    {
        var targetImage = _images.FirstOrDefault(img => img.Id == imageId);
        if (targetImage == null)
            return Result.Failure("Image not found");

        if (targetImage.IsPrimary)
            return Result.Failure("Image is already set as primary");

        // Remove primary status from all images
        for (int i = 0; i < _images.Count; i++)
        {
            if (_images[i].IsPrimary)
            {
                _images[i] = _images[i].RemovePrimaryStatus();
            }
        }

        // Set new primary image
        var targetIndex = _images.IndexOf(targetImage);
        _images[targetIndex] = targetImage.SetAsPrimary();

        MarkAsUpdated();
        return Result.Success();
    }

    public Result UpdateImageMetadata(string imageId, string altText, string caption, int displayOrder)
    {
        var targetImage = _images.FirstOrDefault(img => img.Id == imageId);
        if (targetImage == null)
            return Result.Failure("Image not found");

        var updateResult = targetImage.UpdateMetadata(altText, caption, displayOrder);
        if (!updateResult.IsSuccess)
            return Result.Failure(updateResult.Errors);

        var targetIndex = _images.IndexOf(targetImage);
        _images[targetIndex] = updateResult.Value;

        MarkAsUpdated();
        return Result.Success();
    }

    public Result ReorderImages(List<string> imageIds)
    {
        if (imageIds == null || imageIds.Count == 0)
            return Result.Failure("Image IDs are required for reordering");

        // Check for duplicates first
        if (imageIds.Distinct().Count() != imageIds.Count)
            return Result.Failure("Duplicate image IDs are not allowed");

        // Verify all image IDs exist
        var existingImageIds = _images.Select(img => img.Id).ToHashSet();
        if (!imageIds.All(id => existingImageIds.Contains(id)))
            return Result.Failure("One or more image IDs do not exist");

        // Check if all images are included
        if (imageIds.Count != _images.Count)
            return Result.Failure("All image IDs must be provided for reordering");

        // Reorder images
        var reorderedImages = new List<BusinessImage>();
        for (int i = 0; i < imageIds.Count; i++)
        {
            var image = _images.First(img => img.Id == imageIds[i]);
            var updatedImage = image.UpdateMetadata(image.AltText, image.Caption, i);
            
            if (!updatedImage.IsSuccess)
                return Result.Failure(updatedImage.Errors);
                
            reorderedImages.Add(updatedImage.Value);
        }

        _images.Clear();
        _images.AddRange(reorderedImages);

        MarkAsUpdated();
        return Result.Success();
    }

    public BusinessImage? GetPrimaryImage()
    {
        return _images.FirstOrDefault(img => img.IsPrimary);
    }

    public List<BusinessImage> GetImagesSortedByDisplayOrder()
    {
        return _images.OrderBy(img => img.DisplayOrder).ToList();
    }
    
    /// <summary>
    /// Validates the current state of the business aggregate
    /// </summary>
    public override ValidationResult Validate()
    {
        var errors = new List<string>();
        
        if (Profile == null)
            errors.Add("Business profile is required");
            
        if (Location == null)
            errors.Add("Business location is required");
            
        if (ContactInfo == null)
            errors.Add("Contact information is required");
            
        if (Hours == null)
            errors.Add("Business hours are required");
            
        if (OwnerId == Guid.Empty)
            errors.Add("Owner ID is required");
            
        return errors.Any() ? ValidationResult.Invalid(errors) : ValidationResult.Valid();
    }
}

