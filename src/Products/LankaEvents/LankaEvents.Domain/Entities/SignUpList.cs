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
public class SignUpList : LegacyBaseEntity
{
    private readonly List<SignUpCommitment> _commitments = new(); // Legacy: for Open sign-ups
    private readonly List<string> _predefinedItems = new(); // Legacy: deprecated, use Items instead
    private readonly List<SignUpItem> _items = new(); // New: category-based items

    public string Category { get; private set; }
    public string Description { get; private set; }
    public SignUpType SignUpType { get; private set; } // Legacy: will be deprecated

    // Phase 7D.1: Discriminates Items (classic signups) vs Volunteers (recruitment lists).
    // Volunteer lists are constrained to slot-based items via CreateVolunteerList factory;
    // quantity-based and open-item additions are rejected when Kind == Volunteers.
    public SignUpKind Kind { get; private set; }

    // New category flags
    public bool HasMandatoryItems { get; private set; }
    public bool HasPreferredItems { get; private set; }
    public bool HasSuggestedItems { get; private set; }

    // Phase 6A.27: Open items flag - allows users to add their own items
    public bool HasOpenItems { get; private set; }

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
        bool hasSuggestedItems = false,
        bool hasOpenItems = false,
        SignUpKind kind = SignUpKind.Items)
    {
        Category = category;
        Description = description;
        SignUpType = signUpType;
        HasMandatoryItems = hasMandatoryItems;
        HasPreferredItems = hasPreferredItems;
        HasSuggestedItems = hasSuggestedItems;
        HasOpenItems = hasOpenItems;
        Kind = kind;
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
    /// Phase 6A.27: Added hasOpenItems parameter for user-submitted items
    /// </summary>
    public static Result<SignUpList> CreateWithCategories(
        string category,
        string description,
        bool hasMandatoryItems,
        bool hasPreferredItems,
        bool hasSuggestedItems,
        bool hasOpenItems = false)
    {
        if (string.IsNullOrWhiteSpace(category))
            return Result<SignUpList>.Failure("Category cannot be empty");

        if (string.IsNullOrWhiteSpace(description))
            return Result<SignUpList>.Failure("Description cannot be empty");

        if (!hasMandatoryItems && !hasPreferredItems && !hasSuggestedItems && !hasOpenItems)
            return Result<SignUpList>.Failure("At least one item category must be selected");

        var signUpList = new SignUpList(
            category.Trim(),
            description.Trim(),
            SignUpType.Predefined, // Use Predefined to indicate structured items
            hasMandatoryItems,
            hasPreferredItems,
            hasSuggestedItems,
            hasOpenItems);

        return Result<SignUpList>.Success(signUpList);
    }

    /// <summary>
    /// Creates a category-based sign-up list WITH items in a single operation
    /// Matches requirement: POST /api/events/{eventId}/signups with items array
    /// Phase 6A.27: Added hasOpenItems parameter for user-submitted items
    /// Phase 6A.131: Updated to support both quantity-based and slot-based items
    /// </summary>
    public static Result<SignUpList> CreateWithCategoriesAndItems(
        string category,
        string description,
        bool hasMandatoryItems,
        bool hasPreferredItems,
        bool hasSuggestedItems,
        IEnumerable<(string description, SignUpItemType itemType, SignUpItemCategory category, int? targetQuantity, int? availableSlots, int? suggestedPerSlot, string? notes)> items,
        bool hasOpenItems = false)
    {
        // Validate basic list properties
        if (string.IsNullOrWhiteSpace(category))
            return Result<SignUpList>.Failure("Category cannot be empty");

        if (string.IsNullOrWhiteSpace(description))
            return Result<SignUpList>.Failure("Description cannot be empty");

        if (!hasMandatoryItems && !hasPreferredItems && !hasSuggestedItems && !hasOpenItems)
            return Result<SignUpList>.Failure("At least one item category must be selected");

        // Validate items array (only required if not Open-only list)
        var itemsList = items.ToList();
        if (!itemsList.Any() && !hasOpenItems)
            return Result<SignUpList>.Failure("At least one item must be provided");

        // Create the sign-up list
        var signUpList = new SignUpList(
            category.Trim(),
            description.Trim(),
            SignUpType.Predefined,
            hasMandatoryItems,
            hasPreferredItems,
            hasSuggestedItems,
            hasOpenItems);

        // Phase 6A.131: Add all items, branching on item type for quantity-based vs slot-based
        foreach (var item in itemsList)
        {
            Result<SignUpItem> itemResult;
            if (item.itemType == SignUpItemType.Slot)
            {
                itemResult = signUpList.AddSlotBasedItem(
                    item.description,
                    item.availableSlots ?? 1,
                    item.suggestedPerSlot,
                    item.category,
                    item.notes);
            }
            else
            {
                itemResult = signUpList.AddItem(
                    item.description,
                    item.targetQuantity ?? 1,
                    item.category,
                    item.notes);
            }

            if (itemResult.IsFailure)
                return Result<SignUpList>.Failure(itemResult.Error);
        }

        return Result<SignUpList>.Success(signUpList);
    }

    /// <summary>
    /// Phase 7D.1: Creates a volunteer recruitment list — a SignUpList with Kind=Volunteers,
    /// slot-based items only (1 slot = 1 volunteer), and open-items disabled.
    /// Each role tuple is (roleName, volunteersNeeded, suggestedPerSlot, notes).
    /// </summary>
    public static Result<SignUpList> CreateVolunteerList(
        string category,
        string description,
        IEnumerable<(string roleName, int volunteersNeeded, int? suggestedPerSlot, string? notes)> roles)
    {
        if (string.IsNullOrWhiteSpace(category))
            return Result<SignUpList>.Failure("Category cannot be empty");

        if (string.IsNullOrWhiteSpace(description))
            return Result<SignUpList>.Failure("Description cannot be empty");

        var rolesList = roles?.ToList() ?? new List<(string, int, int?, string?)>();
        if (!rolesList.Any())
            return Result<SignUpList>.Failure("Volunteer list must have at least one role");

        var signUpList = new SignUpList(
            category.Trim(),
            description.Trim(),
            SignUpType.Predefined,
            hasMandatoryItems: true,
            hasPreferredItems: false,
            hasSuggestedItems: false,
            hasOpenItems: false,
            kind: SignUpKind.Volunteers);

        foreach (var role in rolesList)
        {
            var itemResult = signUpList.AddSlotBasedItem(
                role.roleName,
                role.volunteersNeeded,
                role.suggestedPerSlot,
                SignUpItemCategory.Mandatory,
                role.notes);

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
        // Phase 7D.1: Volunteer lists are slot-based only — reject quantity-based additions
        if (Kind == SignUpKind.Volunteers)
            return Result<SignUpItem>.Failure("Cannot add quantity-based item to a volunteer list — use slot-based roles (1 slot = 1 volunteer)");

        // Validate category is enabled
#pragma warning disable CS0618 // Preferred is deprecated but still supported for backward compatibility
        var categoryEnabled = itemCategory switch
        {
            SignUpItemCategory.Mandatory => HasMandatoryItems,
            SignUpItemCategory.Preferred => HasPreferredItems,
            SignUpItemCategory.Suggested => HasSuggestedItems,
            SignUpItemCategory.Open => HasOpenItems,
            _ => false
        };
#pragma warning restore CS0618

        if (!categoryEnabled)
            return Result<SignUpItem>.Failure($"{itemCategory} category is not enabled for this sign-up list");

        // Create the item (Phase 6A.121: Using CreateQuantityBased for backward compatibility)
        var itemResult = SignUpItem.CreateQuantityBased(Id, itemDescription, quantity, itemCategory, notes);
        if (itemResult.IsFailure)
            return Result<SignUpItem>.Failure(itemResult.Error);

        // Phase 6A.132: Aggregate assigns DisplayOrder — new items append to the end.
        itemResult.Value.SetDisplayOrder(GetNextDisplayOrder());
        _items.Add(itemResult.Value);

        return itemResult;
    }

    /// <summary>
    /// Phase 6A.121: Adds a slot-based item to the sign-up list
    /// </summary>
    public Result<SignUpItem> AddSlotBasedItem(
        string itemDescription,
        int availableSlots,
        int? suggestedPerSlot,
        SignUpItemCategory itemCategory,
        string? notes = null)
    {
#pragma warning disable CS0618
        var categoryEnabled = itemCategory switch
        {
            SignUpItemCategory.Mandatory => HasMandatoryItems,
            SignUpItemCategory.Preferred => HasPreferredItems,
            SignUpItemCategory.Suggested => HasSuggestedItems,
            SignUpItemCategory.Open => HasOpenItems,
            _ => false
        };
#pragma warning restore CS0618

        if (!categoryEnabled)
            return Result<SignUpItem>.Failure($"{itemCategory} category is not enabled for this sign-up list");

        var itemResult = SignUpItem.CreateSlotBased(Id, itemDescription, availableSlots, suggestedPerSlot, itemCategory, notes);
        if (itemResult.IsFailure)
            return Result<SignUpItem>.Failure(itemResult.Error);

        // Phase 6A.132: Aggregate assigns DisplayOrder — new items append to the end.
        itemResult.Value.SetDisplayOrder(GetNextDisplayOrder());
        _items.Add(itemResult.Value);

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

        // Raise domain event (Phase 6A.121: Updated with dual nullable fields)
        RaiseDomainEvent(new UserCommittedToSignUpEvent(
            Id,
            userId,
            itemDescription,
            PhysicalQuantity: quantity,  // For legacy Open commitments (quantity-based)
            SlotsClaimed: null,          // Not slot-based
            DateTime.UtcNow,
            Kind: Kind));

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
    public bool IsCategoryBased() => _items.Any() || HasMandatoryItems || HasPreferredItems || HasSuggestedItems || HasOpenItems;

    /// <summary>
    /// Checks if using legacy predefined items model
    /// </summary>
    public bool IsLegacyPredefined() => _predefinedItems.Any();

    /// <summary>
    /// Updates sign-up list details (category, description, and category flags)
    /// Phase 6A.13: Edit Sign-Up List feature
    /// Phase 6A.27: Added hasOpenItems parameter for user-submitted items
    /// </summary>
    public Result UpdateDetails(
        string category,
        string description,
        bool hasMandatoryItems,
        bool hasPreferredItems,
        bool hasSuggestedItems,
        bool hasOpenItems = false)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(category))
            return Result.Failure("Category cannot be empty");

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure("Description cannot be empty");

        // Check if trying to disable categories that contain items (BEFORE checking if at least one is selected)
        if (!hasMandatoryItems && HasMandatoryItems)
        {
            var mandatoryItems = GetItemsByCategory(SignUpItemCategory.Mandatory);
            if (mandatoryItems.Any())
                return Result.Failure("Cannot disable Mandatory category because it contains items");
        }

#pragma warning disable CS0618 // Preferred is deprecated but still supported
        if (!hasPreferredItems && HasPreferredItems)
        {
            var preferredItems = GetItemsByCategory(SignUpItemCategory.Preferred);
            if (preferredItems.Any())
                return Result.Failure("Cannot disable Preferred category because it contains items");
        }
#pragma warning restore CS0618

        if (!hasSuggestedItems && HasSuggestedItems)
        {
            var suggestedItems = GetItemsByCategory(SignUpItemCategory.Suggested);
            if (suggestedItems.Any())
                return Result.Failure("Cannot disable Suggested category because it contains items");
        }

        if (!hasOpenItems && HasOpenItems)
        {
            var openItems = GetItemsByCategory(SignUpItemCategory.Open);
            if (openItems.Any())
                return Result.Failure("Cannot disable Open category because it contains user-submitted items");
        }

        // After checking for items in categories, validate at least one category is selected
        if (!hasMandatoryItems && !hasPreferredItems && !hasSuggestedItems && !hasOpenItems)
            return Result.Failure("At least one item category must be selected");

        // Update properties
        Category = category.Trim();
        Description = description.Trim();
        HasMandatoryItems = hasMandatoryItems;
        HasPreferredItems = hasPreferredItems;
        HasSuggestedItems = hasSuggestedItems;
        HasOpenItems = hasOpenItems;


        // Raise domain event
        RaiseDomainEvent(new SignUpListUpdatedEvent(
            Id,
            Category,
            Description,
            HasMandatoryItems,
            HasPreferredItems,
            HasSuggestedItems,
            DateTime.UtcNow));

        return Result.Success();
    }

    // ==================== PHASE 6A.27: OPEN ITEMS METHODS ====================

    /// <summary>
    /// Phase 6A.27: Adds a user-submitted Open item to the sign-up list
    /// The user who creates the item automatically commits to bringing it
    /// </summary>
    public Result<SignUpItem> AddOpenItem(
        Guid userId,
        string itemName,
        int quantity,
        string? notes = null,
        string? contactName = null,
        string? contactEmail = null,
        string? contactPhone = null)
    {
        // Phase 7D.1: Volunteer lists do not allow user-submitted roles — organizer-defined only
        if (Kind == SignUpKind.Volunteers)
            return Result<SignUpItem>.Failure("User-submitted roles are not allowed on a volunteer list");

        if (!HasOpenItems)
            return Result<SignUpItem>.Failure("Open items are not enabled for this sign-up list");

        if (userId == Guid.Empty)
            return Result<SignUpItem>.Failure("User ID is required for Open items");

        // Create the Open item
        var itemResult = SignUpItem.CreateOpenItem(Id, userId, itemName, quantity, notes);
        if (itemResult.IsFailure)
            return Result<SignUpItem>.Failure(itemResult.Error);

        var item = itemResult.Value;

        // Auto-commit the creator to their own item
        var commitResult = item.AddCommitment(
            userId,
            quantity,
            notes,
            contactName,
            contactEmail,
            contactPhone);

        if (commitResult.IsFailure)
            return Result<SignUpItem>.Failure(commitResult.Error);

        // Phase 6A.132: Aggregate assigns DisplayOrder — new items append to the end.
        item.SetDisplayOrder(GetNextDisplayOrder());
        _items.Add(item);

        return Result<SignUpItem>.Success(item);
    }

    /// <summary>
    /// Phase 6A.132: Reorders items by assigning new <see cref="SignUpItem.DisplayOrder"/>
    /// values according to the supplied <paramref name="orderedItemIds"/> sequence (index 0 =
    /// first item). Enforces exact set equality: the supplied IDs must match the current item
    /// set one-to-one. Missing, extra, duplicate, or unknown IDs all return failure so the
    /// frontend can render a clear error and re-fetch. Raises
    /// <see cref="SignUpItemsReorderedEvent"/> on success.
    /// </summary>
    public Result ReorderItems(IReadOnlyList<Guid> orderedItemIds)
    {
        if (orderedItemIds == null)
            return Result.Failure("Ordered item IDs are required");

        if (orderedItemIds.Count == 0)
            return Result.Failure("Ordered item IDs cannot be empty");

        if (orderedItemIds.Distinct().Count() != orderedItemIds.Count)
            return Result.Failure("Ordered item IDs must not contain duplicates");

        if (orderedItemIds.Count != _items.Count)
            return Result.Failure($"Expected {_items.Count} item IDs but received {orderedItemIds.Count}");

        var currentIds = _items.Select(i => i.Id).ToHashSet();
        var submittedIds = orderedItemIds.ToHashSet();
        if (!currentIds.SetEquals(submittedIds))
            return Result.Failure("Ordered item IDs do not match the items in this sign-up list");

        for (int position = 0; position < orderedItemIds.Count; position++)
        {
            var item = _items.First(i => i.Id == orderedItemIds[position]);
            item.SetDisplayOrder(position);
        }


        RaiseDomainEvent(new SignUpItemsReorderedEvent(
            Id,
            orderedItemIds.ToList().AsReadOnly(),
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.132: Next DisplayOrder to assign when appending a new item.
    /// Leaves gaps from deleted items intact — we never reuse positions, which keeps
    /// ordering stable across concurrent adds and removes.
    /// </summary>
    private int GetNextDisplayOrder()
    {
        return _items.Count == 0 ? 0 : _items.Max(i => i.DisplayOrder) + 1;
    }
}
