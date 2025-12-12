using LankaConnect.Domain.Common;
using LankaConnect.Domain.Badges.Enums;

namespace LankaConnect.Domain.Badges;

/// <summary>
/// Represents a visual overlay badge/sticker that can be applied to event images
/// Examples: "New Event", "Cancelled", "New Year", "Valentine", etc.
/// Badge images are displayed as corner ribbons or decorative overlays on event cards
/// </summary>
public class Badge : BaseEntity
{
    /// <summary>
    /// Display name of the badge (e.g., "New Event", "Valentine")
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Azure Blob Storage URL for the badge image (PNG with transparency recommended)
    /// </summary>
    public string ImageUrl { get; private set; }

    /// <summary>
    /// Azure Blob name for deletion purposes
    /// </summary>
    public string BlobName { get; private set; }

    /// <summary>
    /// Position where the badge should appear on event images
    /// </summary>
    public BadgePosition Position { get; private set; }

    /// <summary>
    /// Whether the badge is currently active and available for assignment
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// System badges are predefined and cannot be deleted (only deactivated)
    /// Custom badges created by users have IsSystem = false
    /// </summary>
    public bool IsSystem { get; private set; }

    /// <summary>
    /// Display order in the badge selection list (lower numbers appear first)
    /// </summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// User who created this badge (null for system badges)
    /// </summary>
    public Guid? CreatedByUserId { get; private set; }

    // EF Core constructor
    private Badge()
    {
        Name = null!;
        ImageUrl = null!;
        BlobName = null!;
    }

    private Badge(
        string name,
        string imageUrl,
        string blobName,
        BadgePosition position,
        bool isSystem,
        int displayOrder,
        Guid? createdByUserId)
    {
        Name = name;
        ImageUrl = imageUrl;
        BlobName = blobName;
        Position = position;
        IsActive = true;
        IsSystem = isSystem;
        DisplayOrder = displayOrder;
        CreatedByUserId = createdByUserId;
    }

    /// <summary>
    /// Factory method to create a new custom badge
    /// </summary>
    public static Result<Badge> Create(
        string name,
        string imageUrl,
        string blobName,
        BadgePosition position,
        int displayOrder,
        Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Badge>.Failure("Badge name is required");

        if (name.Length > 50)
            return Result<Badge>.Failure("Badge name cannot exceed 50 characters");

        if (string.IsNullOrWhiteSpace(imageUrl))
            return Result<Badge>.Failure("Badge image URL is required");

        if (string.IsNullOrWhiteSpace(blobName))
            return Result<Badge>.Failure("Badge blob name is required");

        if (displayOrder < 0)
            return Result<Badge>.Failure("Display order must be non-negative");

        if (createdByUserId == Guid.Empty)
            return Result<Badge>.Failure("Creator user ID is required");

        var badge = new Badge(
            name.Trim(),
            imageUrl,
            blobName,
            position,
            isSystem: false,
            displayOrder,
            createdByUserId);

        return Result<Badge>.Success(badge);
    }

    /// <summary>
    /// Factory method to create a system/predefined badge (used for seeding)
    /// </summary>
    public static Badge CreateSystemBadge(
        string name,
        string imageUrl,
        string blobName,
        BadgePosition position,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Badge name is required", nameof(name));

        return new Badge(
            name.Trim(),
            imageUrl,
            blobName,
            position,
            isSystem: true,
            displayOrder,
            createdByUserId: null);
    }

    /// <summary>
    /// Updates the badge details
    /// System badges can only have their active status changed
    /// </summary>
    public Result Update(string name, BadgePosition position, int displayOrder)
    {
        if (IsSystem)
            return Result.Failure("System badges cannot have their name, position, or display order modified");

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Badge name is required");

        if (name.Length > 50)
            return Result.Failure("Badge name cannot exceed 50 characters");

        if (displayOrder < 0)
            return Result.Failure("Display order must be non-negative");

        Name = name.Trim();
        Position = position;
        DisplayOrder = displayOrder;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Updates the badge image
    /// </summary>
    public Result UpdateImage(string newImageUrl, string newBlobName)
    {
        if (string.IsNullOrWhiteSpace(newImageUrl))
            return Result.Failure("Badge image URL is required");

        if (string.IsNullOrWhiteSpace(newBlobName))
            return Result.Failure("Badge blob name is required");

        ImageUrl = newImageUrl;
        BlobName = newBlobName;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Activates the badge making it available for assignment
    /// </summary>
    public Result Activate()
    {
        if (IsActive)
            return Result.Failure("Badge is already active");

        IsActive = true;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Deactivates the badge, hiding it from selection lists
    /// System badges can be deactivated but not deleted
    /// </summary>
    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure("Badge is already inactive");

        IsActive = false;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Checks if this badge can be deleted
    /// System badges cannot be deleted, only deactivated
    /// </summary>
    public bool CanDelete() => !IsSystem;
}
