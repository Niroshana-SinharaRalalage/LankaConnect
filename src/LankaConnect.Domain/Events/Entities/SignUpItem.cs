using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Represents a specific item in a sign-up list that participants can commit to bringing
/// Example: "Chicken Curry (quantity: 2)" in the "Food & Drinks" sign-up list
/// Replaces the predefinedItems string array with a full entity supporting quantities and categories
/// </summary>
public class SignUpItem : BaseEntity
{
    private readonly List<SignUpCommitment> _commitments = new();

    public Guid SignUpListId { get; private set; }
    public string ItemDescription { get; private set; }
    public int Quantity { get; private set; }
    public SignUpItemCategory ItemCategory { get; private set; }
    public int RemainingQuantity { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyList<SignUpCommitment> Commitments => _commitments.AsReadOnly();

    // EF Core constructor
    private SignUpItem()
    {
        ItemDescription = null!;
    }

    private SignUpItem(
        Guid signUpListId,
        string itemDescription,
        int quantity,
        SignUpItemCategory itemCategory,
        string? notes = null)
    {
        SignUpListId = signUpListId;
        ItemDescription = itemDescription;
        Quantity = quantity;
        ItemCategory = itemCategory;
        RemainingQuantity = quantity; // Initially, all quantity is remaining
        Notes = notes;
    }

    /// <summary>
    /// Creates a new sign-up item
    /// </summary>
    public static Result<SignUpItem> Create(
        Guid signUpListId,
        string itemDescription,
        int quantity,
        SignUpItemCategory itemCategory,
        string? notes = null)
    {
        if (signUpListId == Guid.Empty)
            return Result<SignUpItem>.Failure("Sign-up list ID is required");

        if (string.IsNullOrWhiteSpace(itemDescription))
            return Result<SignUpItem>.Failure("Item description is required");

        if (quantity <= 0)
            return Result<SignUpItem>.Failure("Quantity must be greater than 0");

        if (quantity > 1000)
            return Result<SignUpItem>.Failure("Quantity cannot exceed 1000");

        var item = new SignUpItem(
            signUpListId,
            itemDescription.Trim(),
            quantity,
            itemCategory,
            notes?.Trim());

        return Result<SignUpItem>.Success(item);
    }

    /// <summary>
    /// User commits to bringing a certain quantity of this item
    /// </summary>
    public Result AddCommitment(Guid userId, int commitQuantity, string? commitNotes = null)
    {
        if (userId == Guid.Empty)
            return Result.Failure("User ID is required");

        if (commitQuantity <= 0)
            return Result.Failure("Commit quantity must be greater than 0");

        if (commitQuantity > RemainingQuantity)
            return Result.Failure($"Cannot commit {commitQuantity}. Only {RemainingQuantity} remaining");

        // Check if user already committed to this specific item
        if (_commitments.Any(c => c.UserId == userId))
            return Result.Failure("User has already committed to this item");

        // Create the commitment
        var commitmentResult = SignUpCommitment.CreateForItem(
            Id,
            userId,
            ItemDescription,
            commitQuantity,
            commitNotes);

        if (commitmentResult.IsFailure)
            return Result.Failure(commitmentResult.Error);

        _commitments.Add(commitmentResult.Value);
        RemainingQuantity -= commitQuantity;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// User cancels their commitment to this item
    /// </summary>
    public Result CancelCommitment(Guid userId)
    {
        var commitment = _commitments.FirstOrDefault(c => c.UserId == userId);
        if (commitment == null)
            return Result.Failure("User has no commitment to this item");

        // Return the quantity back to remaining
        RemainingQuantity += commitment.Quantity;
        _commitments.Remove(commitment);
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
    /// </summary>
    public Result UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            return Result.Failure("Quantity must be greater than 0");

        var committedQuantity = Quantity - RemainingQuantity;
        if (newQuantity < committedQuantity)
            return Result.Failure($"Cannot reduce quantity below committed amount ({committedQuantity})");

        RemainingQuantity = newQuantity - committedQuantity;
        Quantity = newQuantity;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Checks if this item is fully committed
    /// </summary>
    public bool IsFullyCommitted() => RemainingQuantity == 0;

    /// <summary>
    /// Gets the committed quantity
    /// </summary>
    public int GetCommittedQuantity() => Quantity - RemainingQuantity;

    /// <summary>
    /// Gets total number of commitments (users who committed)
    /// </summary>
    public int GetCommitmentCount() => _commitments.Count;
}
