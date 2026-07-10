using LankaConnect.BuildingBlocks.Domain;
using System.Diagnostics.CodeAnalysis;
namespace LankaConnect.Products.LankaEvents.Domain.Entities;

/// <summary>
/// Represents a user's commitment to bring/provide something for an event
/// Example: User commits to bringing "Homemade lasagna" for the food sign-up
/// Updated to support both legacy (SignUpList-level) and new (SignUpItem-level) commitments
/// </summary>
public class SignUpCommitment : LegacyBaseEntity
{
    public Guid? SignUpItemId { get; private set; } // Nullable for backward compatibility
    public Guid UserId { get; private set; }
    public string ItemDescription { get; private set; }

    /// <summary>
    /// Phase 6A.121: Dual nullable fields for quantity-based vs slot-based commitments
    /// Exactly ONE of PhysicalQuantity or SlotsClaimed will be populated based on item type
    /// </summary>
    public int? PhysicalQuantity { get; private set; }  // For quantity-based items (e.g., "5 plates")
    public int? SlotsClaimed { get; private set; }      // For slot-based items (e.g., "2 slots")

    /// <summary>
    /// Deprecated: Use PhysicalQuantity or SlotsClaimed instead
    /// Kept for backward compatibility - returns whichever field is populated
    /// </summary>
    [Obsolete("Use PhysicalQuantity or SlotsClaimed instead. This will be removed in Phase 7.")]
    public int Quantity => PhysicalQuantity ?? SlotsClaimed ?? 0;

    public DateTime CommittedAt { get; private set; }
    public string? Notes { get; private set; }

    // Contact information (Phase 2: SignUpGenius-style feature)
    // Nullable - defaults to NULL, can be populated from User entity or overridden by user
    public string? ContactName { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? ContactPhone { get; private set; }

    // EF Core constructor
    [SetsRequiredMembers]
    private SignUpCommitment()
    {
        ItemDescription = null!;
    }

    /// <summary>
    /// Phase 6A.121: Private constructor for quantity-based commitments
    /// </summary>
    [SetsRequiredMembers]
    private SignUpCommitment(
        Guid? signUpItemId,
        Guid userId,
        string itemDescription,
        int physicalQuantity,
        string? notes = null,
        string? contactName = null,
        string? contactEmail = null,
        string? contactPhone = null)
    {
        SignUpItemId = signUpItemId;
        UserId = userId;
        ItemDescription = itemDescription;
        PhysicalQuantity = physicalQuantity;
        SlotsClaimed = null;  // Explicit null for quantity-based
        Notes = notes;
        ContactName = contactName;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
        CommittedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Phase 6A.121: Private constructor for slot-based commitments
    /// </summary>
    [SetsRequiredMembers]
    private SignUpCommitment(
        Guid signUpItemId,
        Guid userId,
        string itemDescription,
        int slotsClaimed,
        bool isSlotBased,  // Dummy parameter to differentiate from quantity-based constructor
        string? notes = null,
        string? contactName = null,
        string? contactEmail = null,
        string? contactPhone = null)
    {
        SignUpItemId = signUpItemId;
        UserId = userId;
        ItemDescription = itemDescription;
        PhysicalQuantity = null;  // Explicit null for slot-based
        SlotsClaimed = slotsClaimed;
        Notes = notes;
        ContactName = contactName;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
        CommittedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a commitment for legacy Open sign-up lists (no specific item)
    /// </summary>
    public static Result<SignUpCommitment> Create(Guid userId, string itemDescription, int quantity)
    {
        if (userId == Guid.Empty)
            return Result<SignUpCommitment>.Failure("User ID is required");

        if (string.IsNullOrWhiteSpace(itemDescription))
            return Result<SignUpCommitment>.Failure("Item description is required");

        if (quantity <= 0)
            return Result<SignUpCommitment>.Failure("Quantity must be greater than 0");

        var commitment = new SignUpCommitment(null, userId, itemDescription.Trim(), quantity);
        return Result<SignUpCommitment>.Success(commitment);
    }

    /// <summary>
    /// Creates a commitment for a specific SignUpItem (new category-based model)
    /// Phase 2: Now includes optional contact information
    /// Phase 6A.121: DEPRECATED - Use CreateQuantityBased or CreateSlotBased instead
    /// Kept for backward compatibility with existing code
    /// </summary>
    [Obsolete("Use CreateQuantityBased or CreateSlotBased instead. This will be removed in Phase 7.")]
    public static Result<SignUpCommitment> CreateForItem(
        Guid signUpItemId,
        Guid userId,
        string itemDescription,
        int quantity,
        string? notes = null,
        string? contactName = null,
        string? contactEmail = null,
        string? contactPhone = null)
    {
        if (signUpItemId == Guid.Empty)
            return Result<SignUpCommitment>.Failure("Sign-up item ID is required");

        if (userId == Guid.Empty)
            return Result<SignUpCommitment>.Failure("User ID is required");

        if (string.IsNullOrWhiteSpace(itemDescription))
            return Result<SignUpCommitment>.Failure("Item description is required");

        if (quantity <= 0)
            return Result<SignUpCommitment>.Failure("Quantity must be greater than 0");

        // Validate email format if provided
        if (!string.IsNullOrWhiteSpace(contactEmail) && !IsValidEmail(contactEmail))
            return Result<SignUpCommitment>.Failure("Invalid email format");

        // Phase 6A.121: Use quantity-based constructor for backward compatibility
        var commitment = new SignUpCommitment(
            signUpItemId,
            userId,
            itemDescription.Trim(),
            quantity,
            notes?.Trim(),
            contactName?.Trim(),
            contactEmail?.Trim(),
            contactPhone?.Trim());

        return Result<SignUpCommitment>.Success(commitment);
    }

    /// <summary>
    /// Phase 6A.121: Creates a quantity-based commitment
    /// Used when committing to quantity-based items (e.g., "5 plates of rice")
    /// </summary>
    public static Result<SignUpCommitment> CreateQuantityBased(
        SignUpItem item,
        Guid userId,
        int physicalQuantity,
        string? notes = null,
        string? contactName = null,
        string? contactEmail = null,
        string? contactPhone = null)
    {
        if (item.ItemType != Enums.SignUpItemType.Quantity)
            return Result<SignUpCommitment>.Failure("Cannot create quantity-based commitment for slot-based item. Use CreateSlotBased instead.");

        if (userId == Guid.Empty)
            return Result<SignUpCommitment>.Failure("User ID is required");

        if (physicalQuantity <= 0)
            return Result<SignUpCommitment>.Failure("Physical quantity must be greater than 0");

        if (physicalQuantity > item.GetRemainingQuantity())
            return Result<SignUpCommitment>.Failure($"Not enough quantity remaining. Available: {item.GetRemainingQuantity()}");

        // Validate email format if provided
        if (!string.IsNullOrWhiteSpace(contactEmail) && !IsValidEmail(contactEmail))
            return Result<SignUpCommitment>.Failure("Invalid email format");

        var commitment = new SignUpCommitment(
            item.Id,
            userId,
            item.ItemDescription,
            physicalQuantity,
            notes?.Trim(),
            contactName?.Trim(),
            contactEmail?.Trim(),
            contactPhone?.Trim());

        return Result<SignUpCommitment>.Success(commitment);
    }

    /// <summary>
    /// Phase 6A.121: Creates a slot-based commitment
    /// Used when committing to slot-based items (e.g., "2 slots for assorted fruits")
    /// </summary>
    public static Result<SignUpCommitment> CreateSlotBased(
        SignUpItem item,
        Guid userId,
        int slotsClaimed,
        string? notes = null,
        string? contactName = null,
        string? contactEmail = null,
        string? contactPhone = null)
    {
        if (item.ItemType != Enums.SignUpItemType.Slot)
            return Result<SignUpCommitment>.Failure("Cannot create slot-based commitment for quantity-based item. Use CreateQuantityBased instead.");

        if (userId == Guid.Empty)
            return Result<SignUpCommitment>.Failure("User ID is required");

        if (slotsClaimed <= 0)
            return Result<SignUpCommitment>.Failure("Slots claimed must be greater than 0");

        if (slotsClaimed > item.GetRemainingSlots())
            return Result<SignUpCommitment>.Failure($"Not enough slots remaining. Available: {item.GetRemainingSlots()}");

        // Validate email format if provided
        if (!string.IsNullOrWhiteSpace(contactEmail) && !IsValidEmail(contactEmail))
            return Result<SignUpCommitment>.Failure("Invalid email format");

        var commitment = new SignUpCommitment(
            item.Id,
            userId,
            item.ItemDescription,
            slotsClaimed,
            true,  // isSlotBased flag to use the slot-based constructor
            notes?.Trim(),
            contactName?.Trim(),
            contactEmail?.Trim(),
            contactPhone?.Trim());

        return Result<SignUpCommitment>.Success(commitment);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Updates the quantity of the commitment
    /// Phase 6A.121: DEPRECATED - Use UpdatePhysicalQuantity or UpdateSlotsClaimed instead
    /// Kept for backward compatibility
    /// </summary>
    [Obsolete("Use UpdatePhysicalQuantity or UpdateSlotsClaimed instead. This will be removed in Phase 7.")]
    public Result UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            return Result.Failure("Quantity must be greater than 0");

        // Phase 6A.121: Update whichever field is populated
        if (PhysicalQuantity.HasValue)
            PhysicalQuantity = newQuantity;
        else if (SlotsClaimed.HasValue)
            SlotsClaimed = newQuantity;


        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.121: Updates the physical quantity for quantity-based commitments
    /// </summary>
    public Result UpdatePhysicalQuantity(int newPhysicalQuantity)
    {
        if (!PhysicalQuantity.HasValue)
            return Result.Failure("Cannot update physical quantity for slot-based commitment. Use UpdateSlotsClaimed instead.");

        if (newPhysicalQuantity <= 0)
            return Result.Failure("Physical quantity must be greater than 0");

        PhysicalQuantity = newPhysicalQuantity;

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.121: Updates the slots claimed for slot-based commitments
    /// </summary>
    public Result UpdateSlotsClaimed(int newSlotsClaimed)
    {
        if (!SlotsClaimed.HasValue)
            return Result.Failure("Cannot update slots claimed for quantity-based commitment. Use UpdatePhysicalQuantity instead.");

        if (newSlotsClaimed <= 0)
            return Result.Failure("Slots claimed must be greater than 0");

        SlotsClaimed = newSlotsClaimed;

        return Result.Success();
    }

    /// <summary>
    /// Updates the contact name
    /// Phase 6A.17: New method to support updating commitment contact info
    /// </summary>
    public Result UpdateContactName(string? newContactName)
    {
        ContactName = newContactName?.Trim();
        return Result.Success();
    }

    /// <summary>
    /// Updates the contact email
    /// Phase 6A.17: New method to support updating commitment contact info
    /// </summary>
    public Result UpdateContactEmail(string? newContactEmail)
    {
        if (!string.IsNullOrWhiteSpace(newContactEmail) && !IsValidEmail(newContactEmail))
            return Result.Failure("Invalid email format");

        ContactEmail = newContactEmail?.Trim();
        return Result.Success();
    }

    /// <summary>
    /// Updates the contact phone
    /// Phase 6A.17: New method to support updating commitment contact info
    /// </summary>
    public Result UpdateContactPhone(string? newContactPhone)
    {
        ContactPhone = newContactPhone?.Trim();
        return Result.Success();
    }

    /// <summary>
    /// Updates the notes
    /// Phase 6A.17: New method to support updating commitment notes
    /// </summary>
    public Result UpdateNotes(string? newNotes)
    {
        Notes = newNotes?.Trim();
        return Result.Success();
    }
}
