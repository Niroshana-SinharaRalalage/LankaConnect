using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.ValueObjects;

/// <summary>
/// Value object representing collection (event fund) configuration for an event.
/// Stored as JSONB on the Event aggregate.
///
/// IMPORTANT (C5 Guard): This VO uses flat primitive types only — no nested Money
/// value objects — to avoid EF Core OwnsOne(ToJson) nested entity issues.
/// </summary>
public class CollectionConfiguration : ValueObject
{
    public const int MAX_SUGGESTED_AMOUNTS = 5;
    public const int MAX_MESSAGE_LENGTH = 500;
    public const decimal MINIMUM_COLLECTION_AMOUNT = 1.00m; // Stripe minimum is $0.50; we use $1.00

    /// <summary>
    /// Whether collections (event fund) are enabled for this event.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Optional fundraising goal amount. Null means no goal (open-ended).
    /// Stored as plain decimal (C5 Guard: no nested Money types in JSONB).
    /// </summary>
    public decimal? GoalAmount { get; private set; }

    /// <summary>
    /// Whether to show collection progress (amount raised, % of goal) publicly on the event details page.
    /// Only meaningful when GoalAmount is set.
    /// </summary>
    public bool ShowProgress { get; private set; }

    /// <summary>
    /// Suggested contribution amounts displayed as quick-select buttons (up to 5).
    /// Stored as plain decimals (C5 Guard: no nested Money types in JSONB).
    /// NOTE: Uses List{decimal} with private set instead of IReadOnlyList{decimal} with
    /// private readonly backing field, because EF Core's ToJson() materializer cannot
    /// properly deserialize IReadOnlyList with readonly backing fields (Phase 6A.130).
    /// </summary>
    public List<decimal> SuggestedAmounts { get; private set; } = new();

    /// <summary>
    /// Whether contributors can enter a custom amount (not limited to suggested amounts).
    /// </summary>
    public bool AllowCustomAmount { get; private set; }

    /// <summary>
    /// Minimum contribution amount (null = no minimum beyond system minimum).
    /// </summary>
    public decimal? MinAmount { get; private set; }

    /// <summary>
    /// Maximum contribution amount (null = no maximum).
    /// </summary>
    public decimal? MaxAmount { get; private set; }

    /// <summary>
    /// Optional message from the organizer displayed on the collection form.
    /// </summary>
    public string? CollectionMessage { get; private set; }

    /// <summary>
    /// Whether to show the contributor count publicly on the event details page.
    /// </summary>
    public bool ShowContributorCount { get; private set; }

    // EF Core constructor
    private CollectionConfiguration()
    {
    }

    private CollectionConfiguration(
        bool isEnabled,
        decimal? goalAmount,
        bool showProgress,
        List<decimal>? suggestedAmounts,
        bool allowCustomAmount,
        decimal? minAmount,
        decimal? maxAmount,
        string? collectionMessage,
        bool showContributorCount)
    {
        IsEnabled = isEnabled;
        GoalAmount = goalAmount;
        ShowProgress = showProgress;
        SuggestedAmounts = suggestedAmounts != null ? new List<decimal>(suggestedAmounts) : new List<decimal>();
        AllowCustomAmount = allowCustomAmount;
        MinAmount = minAmount;
        MaxAmount = maxAmount;
        CollectionMessage = collectionMessage;
        ShowContributorCount = showContributorCount;
    }

    /// <summary>
    /// Creates a collection configuration with validation.
    /// </summary>
    public static Result<CollectionConfiguration> Create(
        bool isEnabled,
        decimal? goalAmount,
        bool showProgress,
        List<decimal>? suggestedAmounts,
        bool allowCustomAmount,
        decimal? minAmount,
        decimal? maxAmount,
        string? collectionMessage,
        bool showContributorCount = false)
    {
        if (!isEnabled)
            return Result<CollectionConfiguration>.Success(Disabled());

        // Validate goal amount
        if (goalAmount.HasValue && goalAmount.Value <= 0)
            return Result<CollectionConfiguration>.Failure("Goal amount must be greater than zero");

        // Validate suggested amounts
        if (suggestedAmounts != null)
        {
            if (suggestedAmounts.Count > MAX_SUGGESTED_AMOUNTS)
                return Result<CollectionConfiguration>.Failure(
                    $"Maximum {MAX_SUGGESTED_AMOUNTS} suggested amounts allowed");

            if (suggestedAmounts.Any(a => a < MINIMUM_COLLECTION_AMOUNT))
                return Result<CollectionConfiguration>.Failure(
                    $"Suggested amounts must be at least {MINIMUM_COLLECTION_AMOUNT:C}");

            if (minAmount.HasValue && suggestedAmounts.Any(a => a < minAmount.Value))
                return Result<CollectionConfiguration>.Failure(
                    $"Suggested amounts cannot be below minimum amount ({minAmount.Value:C})");

            if (maxAmount.HasValue && suggestedAmounts.Any(a => a > maxAmount.Value))
                return Result<CollectionConfiguration>.Failure(
                    $"Suggested amounts cannot exceed maximum amount ({maxAmount.Value:C})");
        }

        // Must have either suggested amounts or allow custom
        if ((suggestedAmounts == null || suggestedAmounts.Count == 0) && !allowCustomAmount)
            return Result<CollectionConfiguration>.Failure(
                "Must provide suggested amounts or allow custom amount entry");

        // Validate min/max
        if (minAmount.HasValue && minAmount.Value < MINIMUM_COLLECTION_AMOUNT)
            return Result<CollectionConfiguration>.Failure(
                $"Minimum amount must be at least {MINIMUM_COLLECTION_AMOUNT:C}");

        if (maxAmount.HasValue && maxAmount.Value < MINIMUM_COLLECTION_AMOUNT)
            return Result<CollectionConfiguration>.Failure(
                $"Maximum amount must be at least {MINIMUM_COLLECTION_AMOUNT:C}");

        if (minAmount.HasValue && maxAmount.HasValue && minAmount.Value > maxAmount.Value)
            return Result<CollectionConfiguration>.Failure(
                "Minimum amount cannot exceed maximum amount");

        // Validate message length
        if (collectionMessage != null && collectionMessage.Length > MAX_MESSAGE_LENGTH)
            return Result<CollectionConfiguration>.Failure(
                $"Collection message cannot exceed {MAX_MESSAGE_LENGTH} characters");

        // Sort suggested amounts ascending for consistent display
        var sortedAmounts = suggestedAmounts?.OrderBy(a => a).ToList();

        return Result<CollectionConfiguration>.Success(new CollectionConfiguration(
            true,
            goalAmount,
            showProgress,
            sortedAmounts,
            allowCustomAmount,
            minAmount,
            maxAmount,
            collectionMessage?.Trim(),
            showContributorCount));
    }

    /// <summary>
    /// Creates a disabled (default) collection configuration.
    /// </summary>
    public static CollectionConfiguration Disabled()
    {
        return new CollectionConfiguration(false, null, false, null, false, null, null, null, false);
    }

    /// <summary>
    /// Validates whether a contribution amount is acceptable per this configuration.
    /// </summary>
    public Result ValidateAmount(decimal amount)
    {
        if (!IsEnabled)
            return Result.Failure("Collections are not enabled for this event");

        if (amount < MINIMUM_COLLECTION_AMOUNT)
            return Result.Failure($"Contribution amount must be at least {MINIMUM_COLLECTION_AMOUNT:C}");

        if (MinAmount.HasValue && amount < MinAmount.Value)
            return Result.Failure($"Contribution amount must be at least {MinAmount.Value:C}");

        if (MaxAmount.HasValue && amount > MaxAmount.Value)
            return Result.Failure($"Contribution amount cannot exceed {MaxAmount.Value:C}");

        return Result.Success();
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return IsEnabled;
        yield return GoalAmount ?? 0m;
        yield return ShowProgress;
        yield return AllowCustomAmount;
        yield return MinAmount ?? 0m;
        yield return MaxAmount ?? 0m;
        yield return CollectionMessage ?? string.Empty;
        yield return ShowContributorCount;

        foreach (var amount in SuggestedAmounts)
            yield return amount;
    }
}
