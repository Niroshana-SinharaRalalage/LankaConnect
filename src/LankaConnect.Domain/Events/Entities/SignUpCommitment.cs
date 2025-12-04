using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Represents a user's commitment to bring/provide something for an event
/// Example: User commits to bringing "Homemade lasagna" for the food sign-up
/// Updated to support both legacy (SignUpList-level) and new (SignUpItem-level) commitments
/// </summary>
public class SignUpCommitment : BaseEntity
{
    public Guid? SignUpItemId { get; private set; } // Nullable for backward compatibility
    public Guid UserId { get; private set; }
    public string ItemDescription { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CommittedAt { get; private set; }
    public string? Notes { get; private set; }

    // Contact information (Phase 2: SignUpGenius-style feature)
    // Nullable - defaults to NULL, can be populated from User entity or overridden by user
    public string? ContactName { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? ContactPhone { get; private set; }

    // EF Core constructor
    private SignUpCommitment()
    {
        ItemDescription = null!;
    }

    private SignUpCommitment(
        Guid? signUpItemId,
        Guid userId,
        string itemDescription,
        int quantity,
        string? notes = null,
        string? contactName = null,
        string? contactEmail = null,
        string? contactPhone = null)
    {
        SignUpItemId = signUpItemId;
        UserId = userId;
        ItemDescription = itemDescription;
        Quantity = quantity;
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
    /// </summary>
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
