using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Represents a user's commitment to bring/provide something for an event
/// Example: User commits to bringing "Homemade lasagna" for the food sign-up
/// </summary>
public class SignUpCommitment : BaseEntity
{
    public Guid UserId { get; private set; }
    public string ItemDescription { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CommittedAt { get; private set; }

    // EF Core constructor
    private SignUpCommitment()
    {
        ItemDescription = null!;
    }

    private SignUpCommitment(Guid userId, string itemDescription, int quantity)
    {
        UserId = userId;
        ItemDescription = itemDescription;
        Quantity = quantity;
        CommittedAt = DateTime.UtcNow;
    }

    public static Result<SignUpCommitment> Create(Guid userId, string itemDescription, int quantity)
    {
        if (userId == Guid.Empty)
            return Result<SignUpCommitment>.Failure("User ID is required");

        if (string.IsNullOrWhiteSpace(itemDescription))
            return Result<SignUpCommitment>.Failure("Item description is required");

        if (quantity <= 0)
            return Result<SignUpCommitment>.Failure("Quantity must be greater than 0");

        var commitment = new SignUpCommitment(userId, itemDescription.Trim(), quantity);
        return Result<SignUpCommitment>.Success(commitment);
    }

    /// <summary>
    /// Updates the quantity of the commitment
    /// </summary>
    public Result UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            return Result.Failure("Quantity must be greater than 0");

        Quantity = newQuantity;
        MarkAsUpdated();

        return Result.Success();
    }
}
