using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.DomainEvents;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Represents a sign-up list for an event where users can commit to bringing items
/// Example: Food sign-up list where users volunteer to bring dishes
/// Similar to SignupGenius functionality
/// Updated to support category-based items (Mandatory, Preferred, Suggested)
/// </summary>
public class SignUpList : BaseEntity
{
    private readonly List<SignUpCommitment> _commitments = new(); // Legacy: for Open sign-ups
    private readonly List<string> _predefinedItems = new(); // Legacy: deprecated, use Items instead
    private readonly List<SignUpItem> _items = new(); // New: category-based items

    public string Category { get; private set; }
    public string Description { get; private set; }
    public SignUpType SignUpType { get; private set; } // Legacy: will be deprecated

    // New category flags
    public bool HasMandatoryItems { get; private set; }
    public bool HasPreferredItems { get; private set; }
    public bool HasSuggestedItems { get; private set; }

    // Collections
    public IReadOnlyList<SignUpCommitment> Commitments => _commitments.AsReadOnly();
    public IReadOnlyList<string> PredefinedItems => _predefinedItems.AsReadOnly(); // Legacy
    public IReadOnlyList<SignUpItem> Items => _items.AsReadOnly(); // New

    // EF Core constructor
    private SignUpList()
    {
        Category = null!;
        Description = null!;
    }

    private SignUpList(
        string category,
        string description,
        SignUpType signUpType,
        bool hasMandatoryItems = false,
        bool hasPreferredItems = false,
        bool hasSuggestedItems = false)
    {
        Category = category;
        Description = description;
        SignUpType = signUpType;
        HasMandatoryItems = hasMandatoryItems;
        HasPreferredItems = hasPreferredItems;
        HasSuggestedItems = hasSuggestedItems;
    }

    /// <summary>
    /// Creates an open sign-up list where users can specify what they want to bring (Legacy)
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
    /// Creates a category-based sign-up list (New model - without items)
    /// </summary>
    public static Result<SignUpList> CreateWithCategories(
        string category,
        string description,
        bool hasMandatoryItems,
        bool hasPreferredItems,
        bool hasSuggestedItems)
    {
        if (string.IsNullOrWhiteSpace(category))
            return Result<SignUpList>.Failure("Category cannot be empty");

        if (string.IsNullOrWhiteSpace(description))
            return Result<SignUpList>.Failure("Description cannot be empty");

        if (!hasMandatoryItems && !hasPreferredItems && !hasSuggestedItems)
            return Result<SignUpList>.Failure("At least one item category must be selected");

        var signUpList = new SignUpList(
            category.Trim(),
            description.Trim(),
            SignUpType.Predefined, // Use Predefined to indicate structured items
            hasMandatoryItems,
            hasPreferredItems,
            hasSuggestedItems);

        return Result<SignUpList>.Success(signUpList);
    }

    /// <summary>
    /// Creates a category-based sign-up list WITH items in a single operation
    /// Matches requirement: POST /api/events/{eventId}/signups with items array
    /// </summary>
    public static Result<SignUpList> CreateWithCategoriesAndItems(
        string category,
        string description,
        bool hasMandatoryItems,
        bool hasPreferredItems,
        bool hasSuggestedItems,
        IEnumerable<(string description, int quantity, SignUpItemCategory category, string? notes)> items)
    {
        // Validate basic list properties
        if (string.IsNullOrWhiteSpace(category))
            return Result<SignUpList>.Failure("Category cannot be empty");

        if (string.IsNullOrWhiteSpace(description))
            return Result<SignUpList>.Failure("Description cannot be empty");

        if (!hasMandatoryItems && !hasPreferredItems && !hasSuggestedItems)
            return Result<SignUpList>.Failure("At least one item category must be selected");

        // Validate items array
        var itemsList = items.ToList();
        if (!itemsList.Any())
            return Result<SignUpList>.Failure("At least one item must be provided");

        // Create the sign-up list
        var signUpList = new SignUpList(
            category.Trim(),
            description.Trim(),
            SignUpType.Predefined,
            hasMandatoryItems,
            hasPreferredItems,
            hasSuggestedItems);

        // Add all items
        foreach (var item in itemsList)
        {
            var itemResult = signUpList.AddItem(
                item.description,
                item.quantity,
                item.category,
                item.notes);

            if (itemResult.IsFailure)
                return Result<SignUpList>.Failure(itemResult.Error);
        }

        return Result<SignUpList>.Success(signUpList);
    }

    /// <summary>
    /// Creates a predefined sign-up list with specific items users can choose from (Legacy)
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

    // ==================== NEW CATEGORY-BASED METHODS ====================

    /// <summary>
    /// Adds a new item to the sign-up list
    /// </summary>
    public Result<SignUpItem> AddItem(
        string itemDescription,
        int quantity,
        SignUpItemCategory itemCategory,
        string? notes = null)
    {
        // Validate category is enabled
        var categoryEnabled = itemCategory switch
        {
            SignUpItemCategory.Mandatory => HasMandatoryItems,
            SignUpItemCategory.Preferred => HasPreferredItems,
            SignUpItemCategory.Suggested => HasSuggestedItems,
            _ => false
        };

        if (!categoryEnabled)
            return Result<SignUpItem>.Failure($"{itemCategory} category is not enabled for this sign-up list");

        // Create the item
        var itemResult = SignUpItem.Create(Id, itemDescription, quantity, itemCategory, notes);
        if (itemResult.IsFailure)
            return Result<SignUpItem>.Failure(itemResult.Error);

        _items.Add(itemResult.Value);
        MarkAsUpdated();

        return itemResult;
    }

    /// <summary>
    /// Removes an item from the sign-up list
    /// </summary>
    public Result RemoveItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            return Result.Failure("Item not found");

        if (item.GetCommitmentCount() > 0)
            return Result.Failure("Cannot remove item with existing commitments");

        _items.Remove(item);
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Gets an item by ID
    /// </summary>
    public SignUpItem? GetItem(Guid itemId)
    {
        return _items.FirstOrDefault(i => i.Id == itemId);
    }

    /// <summary>
    /// Gets all items in a specific category
    /// </summary>
    public IReadOnlyList<SignUpItem> GetItemsByCategory(SignUpItemCategory category)
    {
        return _items.Where(i => i.ItemCategory == category).ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets total number of items across all categories
    /// </summary>
    public int GetTotalItemCount() => _items.Count;

    /// <summary>
    /// Gets count of fully committed items
    /// </summary>
    public int GetFullyCommittedItemCount() => _items.Count(i => i.IsFullyCommitted());

    // ==================== LEGACY METHODS (Kept for backward compatibility) ====================

    /// <summary>
    /// User commits to bringing an item (Legacy - for Open sign-ups)
    /// </summary>
    public Result AddCommitment(Guid userId, string itemDescription, int quantity)
    {
        // Check if user already has a commitment
        if (_commitments.Any(c => c.UserId == userId))
            return Result.Failure("User has already committed to this sign-up");

        // For predefined lists, validate the item is in the list
        if (SignUpType == SignUpType.Predefined && _predefinedItems.Any())
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
    /// User cancels their commitment (Legacy)
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
    /// Gets a user's commitment if it exists (Legacy)
    /// </summary>
    public SignUpCommitment? GetUserCommitment(Guid userId)
    {
        return _commitments.FirstOrDefault(c => c.UserId == userId);
    }

    /// <summary>
    /// Checks if a user has committed to this sign-up (Legacy)
    /// </summary>
    public bool HasUserCommitted(Guid userId)
    {
        return _commitments.Any(c => c.UserId == userId);
    }

    /// <summary>
    /// Gets the total count of commitments (Legacy)
    /// </summary>
    public int GetCommitmentCount() => _commitments.Count;

    /// <summary>
    /// Checks if the sign-up list has any commitments (Legacy)
    /// </summary>
    public bool HasCommitments() => _commitments.Any();

    /// <summary>
    /// Checks if using new category-based model
    /// </summary>
    public bool IsCategoryBased() => _items.Any() || HasMandatoryItems || HasPreferredItems || HasSuggestedItems;

    /// <summary>
    /// Checks if using legacy predefined items model
    /// </summary>
    public bool IsLegacyPredefined() => _predefinedItems.Any();
}
