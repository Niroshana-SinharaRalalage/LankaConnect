using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Value object representing donation configuration for an event.
/// Stored as JSONB on the Event aggregate.
///
/// IMPORTANT (C5 Guard): This VO uses flat primitive types only — no nested Money
/// value objects — to avoid EF Core OwnsOne(ToJson) nested entity issues.
/// </summary>
public class DonationConfiguration : ValueObject
{
    public const int MAX_SUGGESTED_AMOUNTS = 3;
    public const int MAX_MESSAGE_LENGTH = 500;
    public const decimal MINIMUM_DONATION_AMOUNT = 1.00m; // Stripe minimum is $0.50; we use $1.00

    /// <summary>
    /// Whether donations are enabled for this event.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Suggested donation amounts displayed as quick-select buttons (up to 3).
    /// Stored as plain decimals (C5 Guard: no nested Money types in JSONB).
    /// </summary>
    private readonly List<decimal> _suggestedAmounts = new();
    public IReadOnlyList<decimal> SuggestedAmounts => _suggestedAmounts.AsReadOnly();

    /// <summary>
    /// Whether donors can enter a custom amount (not limited to suggested amounts).
    /// </summary>
    public bool AllowCustomAmount { get; private set; }

    /// <summary>
    /// Minimum donation amount (null = no minimum beyond system minimum).
    /// </summary>
    public decimal? MinAmount { get; private set; }

    /// <summary>
    /// Maximum donation amount (null = no maximum).
    /// </summary>
    public decimal? MaxAmount { get; private set; }

    /// <summary>
    /// Optional message from the organizer displayed on the donation form.
    /// </summary>
    public string? DonationMessage { get; private set; }

    // EF Core constructor
    private DonationConfiguration()
    {
    }

    private DonationConfiguration(
        bool isEnabled,
        List<decimal>? suggestedAmounts,
        bool allowCustomAmount,
        decimal? minAmount,
        decimal? maxAmount,
        string? donationMessage)
    {
        IsEnabled = isEnabled;
        if (suggestedAmounts != null)
            _suggestedAmounts.AddRange(suggestedAmounts);
        AllowCustomAmount = allowCustomAmount;
        MinAmount = minAmount;
        MaxAmount = maxAmount;
        DonationMessage = donationMessage;
    }

    /// <summary>
    /// Creates a donation configuration with validation.
    /// </summary>
    public static Result<DonationConfiguration> Create(
        bool isEnabled,
        List<decimal>? suggestedAmounts,
        bool allowCustomAmount,
        decimal? minAmount,
        decimal? maxAmount,
        string? donationMessage)
    {
        if (!isEnabled)
            return Result<DonationConfiguration>.Success(Disabled());

        // Validate suggested amounts
        if (suggestedAmounts != null)
        {
            if (suggestedAmounts.Count > MAX_SUGGESTED_AMOUNTS)
                return Result<DonationConfiguration>.Failure(
                    $"Maximum {MAX_SUGGESTED_AMOUNTS} suggested amounts allowed");

            if (suggestedAmounts.Any(a => a < MINIMUM_DONATION_AMOUNT))
                return Result<DonationConfiguration>.Failure(
                    $"Suggested amounts must be at least {MINIMUM_DONATION_AMOUNT:C}");

            // Validate suggested amounts fall within min/max range
            if (minAmount.HasValue && suggestedAmounts.Any(a => a < minAmount.Value))
                return Result<DonationConfiguration>.Failure(
                    $"Suggested amounts cannot be below minimum amount ({minAmount.Value:C})");

            if (maxAmount.HasValue && suggestedAmounts.Any(a => a > maxAmount.Value))
                return Result<DonationConfiguration>.Failure(
                    $"Suggested amounts cannot exceed maximum amount ({maxAmount.Value:C})");
        }

        // Must have either suggested amounts or allow custom
        if ((suggestedAmounts == null || suggestedAmounts.Count == 0) && !allowCustomAmount)
            return Result<DonationConfiguration>.Failure(
                "Must provide suggested amounts or allow custom amount entry");

        // Validate min/max
        if (minAmount.HasValue && minAmount.Value < MINIMUM_DONATION_AMOUNT)
            return Result<DonationConfiguration>.Failure(
                $"Minimum donation amount must be at least {MINIMUM_DONATION_AMOUNT:C}");

        if (maxAmount.HasValue && maxAmount.Value < MINIMUM_DONATION_AMOUNT)
            return Result<DonationConfiguration>.Failure(
                $"Maximum donation amount must be at least {MINIMUM_DONATION_AMOUNT:C}");

        if (minAmount.HasValue && maxAmount.HasValue && minAmount.Value > maxAmount.Value)
            return Result<DonationConfiguration>.Failure(
                "Minimum donation amount cannot exceed maximum amount");

        // Validate message length
        if (donationMessage != null && donationMessage.Length > MAX_MESSAGE_LENGTH)
            return Result<DonationConfiguration>.Failure(
                $"Donation message cannot exceed {MAX_MESSAGE_LENGTH} characters");

        // Sort suggested amounts ascending for consistent display
        var sortedAmounts = suggestedAmounts?.OrderBy(a => a).ToList();

        return Result<DonationConfiguration>.Success(new DonationConfiguration(
            true,
            sortedAmounts,
            allowCustomAmount,
            minAmount,
            maxAmount,
            donationMessage?.Trim()));
    }

    /// <summary>
    /// Creates a disabled (default) donation configuration.
    /// </summary>
    public static DonationConfiguration Disabled()
    {
        return new DonationConfiguration(false, null, false, null, null, null);
    }

    /// <summary>
    /// Validates whether a donation amount is acceptable per this configuration.
    /// </summary>
    public Result ValidateAmount(decimal amount)
    {
        if (!IsEnabled)
            return Result.Failure("Donations are not enabled for this event");

        if (amount < MINIMUM_DONATION_AMOUNT)
            return Result.Failure($"Donation amount must be at least {MINIMUM_DONATION_AMOUNT:C}");

        if (MinAmount.HasValue && amount < MinAmount.Value)
            return Result.Failure($"Donation amount must be at least {MinAmount.Value:C}");

        if (MaxAmount.HasValue && amount > MaxAmount.Value)
            return Result.Failure($"Donation amount cannot exceed {MaxAmount.Value:C}");

        return Result.Success();
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return IsEnabled;
        yield return AllowCustomAmount;
        yield return MinAmount ?? 0m;
        yield return MaxAmount ?? 0m;
        yield return DonationMessage ?? string.Empty;

        foreach (var amount in _suggestedAmounts)
            yield return amount;
    }
}
