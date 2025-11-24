using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.DomainEvents;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Represents a sign-up list for an event where users can commit to bringing items
/// Example: Food sign-up list where users volunteer to bring dishes
/// Similar to SignupGenius functionality
/// </summary>
public class SignUpList : BaseEntity
{
    private readonly List<SignUpCommitment> _commitments = new();
    private readonly List<string> _predefinedItems = new();

    public string Category { get; private set; }
    public string Description { get; private set; }
    public SignUpType SignUpType { get; private set; }
    public IReadOnlyList<SignUpCommitment> Commitments => _commitments.AsReadOnly();
    public IReadOnlyList<string> PredefinedItems => _predefinedItems.AsReadOnly();

    // EF Core constructor
    private SignUpList()
    {
        Category = null!;
        Description = null!;
    }

    private SignUpList(string category, string description, SignUpType signUpType)
    {
        Category = category;
        Description = description;
        SignUpType = signUpType;
    }

    /// <summary>
    /// Creates an open sign-up list where users can specify what they want to bring
    /// </summary>
    public static Result<SignUpList> Create(string category, string description, SignUpType signUpType)
    {
        if (string.IsNullOrWhiteSpace(category))
            return Result<SignUpList>.Failure("Category cannot be empty");

        if (string.IsNullOrWhiteSpace(description))
            return Result<SignUpList>.Failure("Description cannot be empty");

        var signUpList = new SignUpList(category.Trim(), description.Trim(), signUpType);
        return Result<SignUpList>.Success(signUpList);
    }

    /// <summary>
    /// Creates a predefined sign-up list with specific items users can choose from
    /// </summary>
    public static Result<SignUpList> CreateWithPredefinedItems(
        string category,
        string description,
        IEnumerable<string> predefinedItems)
    {
        if (string.IsNullOrWhiteSpace(category))
            return Result<SignUpList>.Failure("Category cannot be empty");

        if (string.IsNullOrWhiteSpace(description))
            return Result<SignUpList>.Failure("Description cannot be empty");

        var itemsList = predefinedItems.Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => i.Trim()).ToList();
        if (!itemsList.Any())
            return Result<SignUpList>.Failure("Predefined items list cannot be empty");

        var signUpList = new SignUpList(category.Trim(), description.Trim(), SignUpType.Predefined);
        signUpList._predefinedItems.AddRange(itemsList);

        return Result<SignUpList>.Success(signUpList);
    }

    /// <summary>
    /// User commits to bringing an item
    /// </summary>
    public Result AddCommitment(Guid userId, string itemDescription, int quantity)
    {
        // Check if user already has a commitment
        if (_commitments.Any(c => c.UserId == userId))
            return Result.Failure("User has already committed to this sign-up");

        // For predefined lists, validate the item is in the list
        if (SignUpType == SignUpType.Predefined)
        {
            if (!_predefinedItems.Any(i => i.Equals(itemDescription, StringComparison.OrdinalIgnoreCase)))
                return Result.Failure($"Item '{itemDescription}' is not in the predefined items list");
        }

        // Create commitment
        var commitmentResult = SignUpCommitment.Create(userId, itemDescription, quantity);
        if (commitmentResult.IsFailure)
            return Result.Failure(commitmentResult.Error);

        _commitments.Add(commitmentResult.Value);
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new UserCommittedToSignUpEvent(
            Id,
            userId,
            itemDescription,
            quantity,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// User cancels their commitment
    /// </summary>
    public Result CancelCommitment(Guid userId)
    {
        var commitment = _commitments.FirstOrDefault(c => c.UserId == userId);
        if (commitment == null)
            return Result.Failure("User has no commitment to cancel");

        _commitments.Remove(commitment);
        MarkAsUpdated();

        // Raise domain event
        RaiseDomainEvent(new UserCancelledSignUpCommitmentEvent(
            Id,
            userId,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Gets a user's commitment if it exists
    /// </summary>
    public SignUpCommitment? GetUserCommitment(Guid userId)
    {
        return _commitments.FirstOrDefault(c => c.UserId == userId);
    }

    /// <summary>
    /// Checks if a user has committed to this sign-up
    /// </summary>
    public bool HasUserCommitted(Guid userId)
    {
        return _commitments.Any(c => c.UserId == userId);
    }

    /// <summary>
    /// Gets the total count of commitments
    /// </summary>
    public int GetCommitmentCount() => _commitments.Count;

    /// <summary>
    /// Checks if the sign-up list has any commitments
    /// </summary>
    public bool HasCommitments() => _commitments.Any();
}
