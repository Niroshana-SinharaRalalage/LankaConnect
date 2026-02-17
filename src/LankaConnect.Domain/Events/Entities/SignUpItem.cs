using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Represents a specific item in a sign-up list that participants can commit to bringing
/// Example: "Chicken Curry (quantity: 2)" in the "Food & Drinks" sign-up list
/// Replaces the predefinedItems string array with a full entity supporting quantities and categories
/// Phase 6A.27: Added CreatedByUserId for Open items
/// </summary>
public class SignUpItem : BaseEntity
{
    private readonly List<SignUpCommitment> _commitments = new();

    public Guid SignUpListId { get; private set; }
    public string ItemDescription { get; private set; }

    /// <summary>
    /// Phase 6A.121: Dual nullable fields for quantity-based vs slot-based items
    /// Exactly ONE of TargetQuantity or AvailableSlots will be populated (enforced by DB CHECK constraint)
    /// </summary>
    public int? TargetQuantity { get; private set; }        // For quantity-based items (e.g., "10 plates")
    public int? AvailableSlots { get; private set; }        // For slot-based items (e.g., "3 slots")
    public int? SuggestedPerSlot { get; private set; }      // Optional: "Suggest 2-3 per slot"

    public SignUpItemCategory ItemCategory { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>
    /// Phase 6A.27: User who created this Open item (null for organizer-created items)
    /// </summary>
    public Guid? CreatedByUserId { get; private set; }

    public IReadOnlyList<SignUpCommitment> Commitments => _commitments.AsReadOnly();

    /// <summary>
    /// Phase 6A.121: Computed property determining item type based on which nullable field is populated
    /// NOT stored in database - computed at runtime
    /// </summary>
    public SignUpItemType ItemType => AvailableSlots.HasValue
        ? SignUpItemType.Slot
        : SignUpItemType.Quantity;

    // EF Core constructor
    private SignUpItem()
    {
        ItemDescription = null!;
    }

    /// <summary>
    /// Phase 6A.121: Private constructor for quantity-based items
    /// </summary>
    private SignUpItem(
        Guid signUpListId,
        string itemDescription,
        int targetQuantity,
        SignUpItemCategory itemCategory,
        string? notes = null,
        Guid? createdByUserId = null)
    {
        SignUpListId = signUpListId;
        ItemDescription = itemDescription;
        TargetQuantity = targetQuantity;
        AvailableSlots = null;           // Explicit null for quantity-based
        SuggestedPerSlot = null;
        ItemCategory = itemCategory;
        Notes = notes;
        CreatedByUserId = createdByUserId;
    }

    /// <summary>
    /// Phase 6A.121: Private constructor for slot-based items
    /// </summary>
    private SignUpItem(
        Guid signUpListId,
        string itemDescription,
        int availableSlots,
        int? suggestedPerSlot,
        SignUpItemCategory itemCategory,
        string? notes = null,
        Guid? createdByUserId = null)
    {
        SignUpListId = signUpListId;
        ItemDescription = itemDescription;
        TargetQuantity = null;           // Explicit null for slot-based
        AvailableSlots = availableSlots;
        SuggestedPerSlot = suggestedPerSlot;
        ItemCategory = itemCategory;
        Notes = notes;
        CreatedByUserId = createdByUserId;
    }

    /// <summary>
    /// Phase 6A.121: Creates a quantity-based sign-up item
    /// Example: "Rice - 10 plates" or "Paper Plates - 50 pieces"
    /// </summary>
    public static Result<SignUpItem> CreateQuantityBased(
        Guid signUpListId,
        string itemDescription,
        int targetQuantity,
        SignUpItemCategory itemCategory,
        string? notes = null)
    {
        if (signUpListId == Guid.Empty)
            return Result<SignUpItem>.Failure("Sign-up list ID is required");

        if (string.IsNullOrWhiteSpace(itemDescription))
            return Result<SignUpItem>.Failure("Item description is required");

        if (targetQuantity <= 0 || targetQuantity > 1000)
            return Result<SignUpItem>.Failure("Target quantity must be between 1 and 1000");

        var item = new SignUpItem(
            signUpListId,
            itemDescription.Trim(),
            targetQuantity,
            itemCategory,
            notes?.Trim(),
            createdByUserId: null);

        return Result<SignUpItem>.Success(item);
    }

    /// <summary>
    /// Phase 6A.121: Creates a slot-based sign-up item
    /// Example: "Assorted Fruits - 3 slots" or "Homemade Desserts - 5 slots"
    /// People can take 1 or more slots and specify what they're bringing in notes
    /// </summary>
    public static Result<SignUpItem> CreateSlotBased(
        Guid signUpListId,
        string itemDescription,
        int availableSlots,
        int? suggestedPerSlot,
        SignUpItemCategory itemCategory,
        string? notes = null)
    {
        if (signUpListId == Guid.Empty)
            return Result<SignUpItem>.Failure("Sign-up list ID is required");

        if (string.IsNullOrWhiteSpace(itemDescription))
            return Result<SignUpItem>.Failure("Item description is required");

        if (availableSlots <= 0 || availableSlots > 100)
            return Result<SignUpItem>.Failure("Available slots must be between 1 and 100");

        if (suggestedPerSlot.HasValue && (suggestedPerSlot.Value <= 0 || suggestedPerSlot.Value > 1000))
            return Result<SignUpItem>.Failure("Suggested quantity per slot must be between 1 and 1000");

        var item = new SignUpItem(
            signUpListId,
            itemDescription.Trim(),
            availableSlots,
            suggestedPerSlot,
            itemCategory,
            notes?.Trim(),
            createdByUserId: null);

        return Result<SignUpItem>.Success(item);
    }

    /// <summary>
    /// Phase 6A.27/6A.121: Creates a quantity-based Open item submitted by a user
    /// Open items are user-created items where the user volunteers to bring something
    /// </summary>
    public static Result<SignUpItem> CreateOpenItem(
        Guid signUpListId,
        Guid createdByUserId,
        string itemName,
        int quantity,
        string? notes = null)
    {
        if (signUpListId == Guid.Empty)
            return Result<SignUpItem>.Failure("Sign-up list ID is required");

        if (createdByUserId == Guid.Empty)
            return Result<SignUpItem>.Failure("User ID is required for Open items");

        if (string.IsNullOrWhiteSpace(itemName))
            return Result<SignUpItem>.Failure("Item name is required");

        if (quantity <= 0 || quantity > 1000)
            return Result<SignUpItem>.Failure("Quantity must be between 1 and 1000");

        var item = new SignUpItem(
            signUpListId,
            itemName.Trim(),
            quantity,
            SignUpItemCategory.Open,
            notes?.Trim(),
            createdByUserId);

        return Result<SignUpItem>.Success(item);
    }

    /// <summary>
    /// Phase 6A.121: Gets remaining quantity for quantity-based items
    /// Throws InvalidOperationException if called on slot-based item
    /// </summary>
    public int GetRemainingQuantity()
    {
        if (ItemType != SignUpItemType.Quantity)
            throw new InvalidOperationException("Cannot get remaining quantity for slot-based item. Use GetRemainingSlots().");

        var committed = _commitments.Sum(c => c.PhysicalQuantity ?? 0);
        return TargetQuantity!.Value - committed;
    }

    /// <summary>
    /// Phase 6A.121: Gets remaining slots for slot-based items
    /// Throws InvalidOperationException if called on quantity-based item
    /// </summary>
    public int GetRemainingSlots()
    {
        if (ItemType != SignUpItemType.Slot)
            throw new InvalidOperationException("Cannot get remaining slots for quantity-based item. Use GetRemainingQuantity().");

        var filledSlots = _commitments.Count(c => c.SlotsClaimed.HasValue && c.SlotsClaimed.Value > 0);
        return AvailableSlots!.Value - filledSlots;
    }

    /// <summary>
    /// Phase 6A.121: Gets estimated total quantity for slot-based items with SuggestedPerSlot
    /// Returns null if item is quantity-based or if no SuggestedPerSlot is defined
    /// Example: 3 filled slots * 5 suggested per slot = estimated 15 items total
    /// </summary>
    public int? GetEstimatedTotalQuantity()
    {
        if (ItemType != SignUpItemType.Slot || !SuggestedPerSlot.HasValue)
            return null;

        var filledSlots = _commitments.Count(c => c.SlotsClaimed.HasValue && c.SlotsClaimed.Value > 0);
        return filledSlots * SuggestedPerSlot.Value;
    }

    /// <summary>
    /// User commits to bringing a certain quantity of this item
    /// Phase 2: Now includes optional contact information
    /// Phase 6A.121: Temporarily only supports quantity-based items
    /// TODO: Update when SignUpCommitment is updated with PhysicalQuantity/SlotsClaimed
    /// </summary>
    public Result AddCommitment(
        Guid userId,
        int commitQuantity,
        string? commitNotes = null,
        string? contactName = null,
        string? contactEmail = null,
        string? contactPhone = null)
    {
        // Phase 6A.121: Temporary check - will be removed when SignUpCommitment is updated
        if (ItemType != SignUpItemType.Quantity)
            return Result.Failure("Adding commitments to slot-based items is not yet supported. Please update SignUpCommitment first.");

        if (userId == Guid.Empty)
            return Result.Failure("User ID is required");

        if (commitQuantity <= 0)
            return Result.Failure("Commit quantity must be greater than 0");

        var remainingQuantity = GetRemainingQuantity();
        if (commitQuantity > remainingQuantity)
            return Result.Failure($"Cannot commit {commitQuantity}. Only {remainingQuantity} remaining");

        // Check if user already committed to this specific item
        if (_commitments.Any(c => c.UserId == userId))
            return Result.Failure("User has already committed to this item");

        // Create the commitment (Phase 6A.121: Using CreateQuantityBased)
        var commitmentResult = SignUpCommitment.CreateQuantityBased(
            this,  // Pass the SignUpItem for validation
            userId,
            commitQuantity,
            commitNotes,
            contactName,
            contactEmail,
            contactPhone);

        if (commitmentResult.IsFailure)
            return Result.Failure(commitmentResult.Error);

        _commitments.Add(commitmentResult.Value);
        MarkAsUpdated();

        // Phase 6A.51: Raise domain event for sending confirmation email
        // Phase 6A.121: Updated with dual nullable fields
        RaiseDomainEvent(new DomainEvents.UserCommittedToSignUpEvent(
            SignUpListId,
            userId,
            ItemDescription,
            PhysicalQuantity: commitQuantity,  // For quantity-based items
            SlotsClaimed: null,                 // Not slot-based
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Updates an existing commitment for a user
    /// Phase 6A.17: Supports updating commitment quantity and contact info
    /// Phase 6A.18: Support for flexible updates (increase, decrease, or cancel)
    /// Phase 6A.121: Temporarily only supports quantity-based items
    /// Special case: newQuantity = 0 signals cancellation of entire commitment
    /// </summary>
    public Result UpdateCommitment(
        Guid userId,
        int newQuantity,
        string? commitNotes = null,
        string? contactName = null,
        string? contactEmail = null,
        string? contactPhone = null)
    {
        // Phase 6A.121: Temporary check - will be removed when SignUpCommitment is updated
        if (ItemType != SignUpItemType.Quantity)
            return Result.Failure("Updating commitments on slot-based items is not yet supported. Please update SignUpCommitment first.");

        if (userId == Guid.Empty)
            return Result.Failure("User ID is required");

        // Find existing commitment
        var existingCommitment = _commitments.FirstOrDefault(c => c.UserId == userId);
        if (existingCommitment == null)
            return Result.Failure("User has no commitment to this item");

        // Special case: quantity = 0 means cancel the commitment entirely
        // Phase 6A.89 FIX: Raise CommitmentCancelledEvent so cancellation email is sent
        if (newQuantity == 0)
        {
            // Capture data BEFORE removing commitment (needed for email notification)
            var cancelledQuantity = existingCommitment.PhysicalQuantity ?? 0;
            var commitmentId = existingCommitment.Id;

            _commitments.Remove(existingCommitment);
            MarkAsUpdated();

            // Phase 6A.89 FIX: Raise domain event for cancellation email notification
            // Phase 6A.121: Updated with dual nullable fields
            RaiseDomainEvent(new DomainEvents.CommitmentCancelledEvent(
                Id,
                commitmentId,
                userId,
                SignUpListId,
                ItemDescription,
                CancelledPhysicalQuantity: cancelledQuantity,  // For quantity-based items
                CancelledSlotsClaimed: null));

            return Result.Success();
        }

        // Validate new quantity is positive
        if (newQuantity <= 0)
            return Result.Failure("Quantity must be greater than 0 (or 0 to cancel)");

        // Calculate the difference in quantity
        var oldQuantity = existingCommitment.PhysicalQuantity ?? 0;
        var quantityDifference = newQuantity - oldQuantity;

        // Check if the new quantity exceeds available slots (account for quantity change)
        // If decreasing, always allow
        // If increasing, check remaining availability
        var remainingQuantity = GetRemainingQuantity();
        if (quantityDifference > 0 && quantityDifference > remainingQuantity)
            return Result.Failure($"Cannot change commitment to {newQuantity}. Only {remainingQuantity + oldQuantity} total available.");

        // Update the commitment's quantity (Phase 6A.121: Using UpdatePhysicalQuantity)
        var updateResult = existingCommitment.UpdatePhysicalQuantity(newQuantity);
        if (updateResult.IsFailure)
            return updateResult;

        // Update contact information if provided
        if (!string.IsNullOrWhiteSpace(contactName))
        {
            var nameResult = existingCommitment.UpdateContactName(contactName);
            if (nameResult.IsFailure)
                return nameResult;
        }

        if (!string.IsNullOrWhiteSpace(contactEmail))
        {
            var emailResult = existingCommitment.UpdateContactEmail(contactEmail);
            if (emailResult.IsFailure)
                return emailResult;
        }

        if (!string.IsNullOrWhiteSpace(contactPhone))
        {
            var phoneResult = existingCommitment.UpdateContactPhone(contactPhone);
            if (phoneResult.IsFailure)
                return phoneResult;
        }

        if (!string.IsNullOrWhiteSpace(commitNotes))
        {
            var notesResult = existingCommitment.UpdateNotes(commitNotes);
            if (notesResult.IsFailure)
                return notesResult;
        }

        MarkAsUpdated();

        // Phase 6A.51+: Raise domain event for sending update confirmation email
        // Phase 6A.121: Updated with dual nullable fields
        RaiseDomainEvent(new DomainEvents.CommitmentUpdatedEvent(
            Id,
            userId,
            OldPhysicalQuantity: oldQuantity,   // For quantity-based items
            NewPhysicalQuantity: newQuantity,   // For quantity-based items
            OldSlotsClaimed: null,              // Not slot-based
            NewSlotsClaimed: null,              // Not slot-based
            ItemDescription,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// User cancels their commitment to this item
    /// Phase 6A.28 Issue 4 Fix: Raises CommitmentCancelledEvent so infrastructure
    /// can explicitly mark the entity as Deleted in EF Core change tracker.
    /// Phase 6A.121: Temporarily only supports quantity-based items
    /// See ADR-008 for why this is necessary (private backing field issue).
    /// </summary>
    public Result CancelCommitment(Guid userId)
    {
        // Phase 6A.121: Temporary check - will be removed when SignUpCommitment is updated
        if (ItemType != SignUpItemType.Quantity)
            return Result.Failure("Cancelling commitments on slot-based items is not yet supported. Please update SignUpCommitment first.");

        var commitment = _commitments.FirstOrDefault(c => c.UserId == userId);
        if (commitment == null)
            return Result.Failure("User has no commitment to this item");

        // Capture data BEFORE removing commitment (needed for email notification)
        var cancelledQuantity = commitment.PhysicalQuantity ?? 0;

        _commitments.Remove(commitment);

        // Phase 6A.28 Issue 4 Fix: Raise domain event for infrastructure to handle deletion
        // EF Core cannot detect collection removals from private backing fields
        // This event allows infrastructure to explicitly mark entity as Deleted
        // Phase 6A.51+ Fix: Include data for email notification (entity may be deleted by then)
        // Phase 6A.121: Updated with dual nullable fields
        RaiseDomainEvent(new DomainEvents.CommitmentCancelledEvent(
            Id,
            commitment.Id,
            userId,
            SignUpListId,
            ItemDescription,
            CancelledPhysicalQuantity: cancelledQuantity,  // For quantity-based items
            CancelledSlotsClaimed: null));

        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Updates the item description
    /// </summary>
    public Result UpdateDescription(string newDescription)
    {
        if (string.IsNullOrWhiteSpace(newDescription))
            return Result.Failure("Item description is required");

        ItemDescription = newDescription.Trim();
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Updates the total quantity (organizer can adjust if needed)
    /// Phase 6A.121: Only works for quantity-based items
    /// </summary>
    public Result UpdateQuantity(int newQuantity)
    {
        if (ItemType != SignUpItemType.Quantity)
            return Result.Failure("Cannot update quantity for slot-based item. Use UpdateSlots() instead.");

        if (newQuantity <= 0 || newQuantity > 1000)
            return Result.Failure("Quantity must be between 1 and 1000");

        var committedQuantity = GetCommittedQuantity();
        if (newQuantity < committedQuantity)
            return Result.Failure($"Cannot reduce quantity below committed amount ({committedQuantity})");

        TargetQuantity = newQuantity;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.121: Updates the total slots (organizer can adjust if needed)
    /// Only works for slot-based items
    /// </summary>
    public Result UpdateSlots(int newSlots, int? newSuggestedPerSlot = null)
    {
        if (ItemType != SignUpItemType.Slot)
            return Result.Failure("Cannot update slots for quantity-based item. Use UpdateQuantity() instead.");

        if (newSlots <= 0 || newSlots > 100)
            return Result.Failure("Slots must be between 1 and 100");

        var filledSlots = _commitments.Count(c => c.SlotsClaimed.HasValue && c.SlotsClaimed.Value > 0);
        if (newSlots < filledSlots)
            return Result.Failure($"Cannot reduce slots below filled amount ({filledSlots})");

        if (newSuggestedPerSlot.HasValue && (newSuggestedPerSlot.Value <= 0 || newSuggestedPerSlot.Value > 1000))
            return Result.Failure("Suggested quantity per slot must be between 1 and 1000");

        AvailableSlots = newSlots;
        SuggestedPerSlot = newSuggestedPerSlot;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Checks if this item is fully committed
    /// Phase 6A.121: Works for both quantity-based and slot-based items
    /// </summary>
    public bool IsFullyCommitted()
    {
        if (ItemType == SignUpItemType.Quantity)
            return GetRemainingQuantity() == 0;
        else
            return GetRemainingSlots() == 0;
    }

    /// <summary>
    /// Gets the committed quantity (for quantity-based items only)
    /// Phase 6A.121: Throws for slot-based items
    /// </summary>
    public int GetCommittedQuantity()
    {
        if (ItemType != SignUpItemType.Quantity)
            throw new InvalidOperationException("Cannot get committed quantity for slot-based item.");

        return _commitments.Sum(c => c.PhysicalQuantity ?? 0);
    }

    /// <summary>
    /// Gets total number of commitments (users who committed)
    /// </summary>
    public int GetCommitmentCount() => _commitments.Count;

    /// <summary>
    /// Phase 6A.27: Checks if this is a user-created Open item
    /// </summary>
    public bool IsOpenItem() => ItemCategory == SignUpItemCategory.Open && CreatedByUserId.HasValue;

    /// <summary>
    /// Phase 6A.27: Checks if the specified user created this Open item
    /// </summary>
    public bool IsCreatedByUser(Guid userId) => CreatedByUserId.HasValue && CreatedByUserId.Value == userId;

    /// <summary>
    /// Updates item details (description, quantity, and notes) in a single operation
    /// Phase 6A.14: Edit Sign-Up Item feature
    /// Phase 6A.121: Only works for quantity-based items
    /// </summary>
    public Result UpdateDetails(string newDescription, int newQuantity, string? newNotes)
    {
        if (ItemType != SignUpItemType.Quantity)
            return Result.Failure("Cannot update details for slot-based item. Use UpdateDetailsSlotBased() instead.");

        // Validate description
        if (string.IsNullOrWhiteSpace(newDescription))
            return Result.Failure("Item description is required");

        if (newDescription.Length > 500)
            return Result.Failure("Item description must not exceed 500 characters");

        // Validate quantity
        if (newQuantity <= 0)
            return Result.Failure("Quantity must be greater than 0");

        if (newQuantity > 1000)
            return Result.Failure("Quantity cannot exceed 1000");

        // Check if trying to reduce quantity below committed amount
        var committedQuantity = GetCommittedQuantity();
        if (newQuantity < committedQuantity)
            return Result.Failure($"Cannot reduce quantity below committed amount ({committedQuantity})");

        // Validate notes if provided
        if (!string.IsNullOrWhiteSpace(newNotes) && newNotes.Length > 500)
            return Result.Failure("Notes must not exceed 500 characters");

        // Update properties
        ItemDescription = newDescription.Trim();
        TargetQuantity = newQuantity;
        Notes = newNotes?.Trim();

        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.121: Updates slot-based item details
    /// </summary>
    public Result UpdateDetailsSlotBased(string newDescription, int newSlots, int? newSuggestedPerSlot, string? newNotes)
    {
        if (ItemType != SignUpItemType.Slot)
            return Result.Failure("Cannot update slot-based details for quantity-based item. Use UpdateDetails() instead.");

        // Validate description
        if (string.IsNullOrWhiteSpace(newDescription))
            return Result.Failure("Item description is required");

        if (newDescription.Length > 500)
            return Result.Failure("Item description must not exceed 500 characters");

        // Validate slots
        if (newSlots <= 0 || newSlots > 100)
            return Result.Failure("Slots must be between 1 and 100");

        // Check if trying to reduce slots below filled amount
        var filledSlots = _commitments.Count(c => c.SlotsClaimed.HasValue && c.SlotsClaimed.Value > 0);
        if (newSlots < filledSlots)
            return Result.Failure($"Cannot reduce slots below filled amount ({filledSlots})");

        // Validate suggested per slot if provided
        if (newSuggestedPerSlot.HasValue && (newSuggestedPerSlot.Value <= 0 || newSuggestedPerSlot.Value > 1000))
            return Result.Failure("Suggested quantity per slot must be between 1 and 1000");

        // Validate notes if provided
        if (!string.IsNullOrWhiteSpace(newNotes) && newNotes.Length > 500)
            return Result.Failure("Notes must not exceed 500 characters");

        // Update properties
        ItemDescription = newDescription.Trim();
        AvailableSlots = newSlots;
        SuggestedPerSlot = newSuggestedPerSlot;
        Notes = newNotes?.Trim();

        MarkAsUpdated();

        return Result.Success();
    }
}
