using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Value object representing sponsor configuration for an event.
/// Stored as JSONB on the Event aggregate.
///
/// IMPORTANT (C5 Guard): This VO uses flat primitive types only — no nested Money
/// value objects — to avoid EF Core OwnsOne(ToJson) nested entity issues.
///
/// Phase 6A.145 — Reverted Phase 6A.143's banner-on-config design. Per-sponsor
/// images now live on the <see cref="Sponsor"/> aggregate; this VO carries only
/// the organizer-set threshold (<see cref="MinAmountForSponsorImage"/>).
/// </summary>
public class SponsorConfiguration : ValueObject
{
    public const int MAX_MESSAGE_LENGTH = 500;
    public const decimal MINIMUM_SPONSOR_AMOUNT = 1.00m;

    /// <summary>
    /// Whether sponsorships are enabled for this event.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Whether to accept money-based sponsorships (via Stripe payment).
    /// </summary>
    public bool AcceptMoneySponsors { get; private set; }

    /// <summary>
    /// Whether to accept item-based sponsorships (no payment, just item recording).
    /// </summary>
    public bool AcceptItemSponsors { get; private set; }

    /// <summary>
    /// Minimum monetary sponsorship amount (null = no minimum beyond system minimum).
    /// Only applies to money sponsors.
    /// </summary>
    public decimal? MinSponsorAmount { get; private set; }

    /// <summary>
    /// Optional message from the organizer displayed on the sponsorship form.
    /// </summary>
    public string? SponsorMessage { get; private set; }

    /// <summary>
    /// Whether to show the sponsor list publicly on the event details page.
    /// </summary>
    public bool ShowSponsorList { get; private set; }

    /// <summary>
    /// Phase 6A.145 — opt-in threshold for sponsor image uploads. When null, sponsor
    /// images are DISABLED for this event. When set, sponsors whose contribution
    /// reaches this value can upload an image displayed on the event details page.
    /// For money sponsors the threshold gates on amount; for item sponsors it gates
    /// on <c>EstimatedValue</c> (item sponsors with no EstimatedValue are denied).
    /// </summary>
    public decimal? MinAmountForSponsorImage { get; private set; }

    // EF Core constructor
    private SponsorConfiguration()
    {
    }

    private SponsorConfiguration(
        bool isEnabled,
        bool acceptMoneySponsors,
        bool acceptItemSponsors,
        decimal? minSponsorAmount,
        string? sponsorMessage,
        bool showSponsorList,
        decimal? minAmountForSponsorImage = null)
    {
        IsEnabled = isEnabled;
        AcceptMoneySponsors = acceptMoneySponsors;
        AcceptItemSponsors = acceptItemSponsors;
        MinSponsorAmount = minSponsorAmount;
        SponsorMessage = sponsorMessage;
        ShowSponsorList = showSponsorList;
        MinAmountForSponsorImage = minAmountForSponsorImage;
    }

    /// <summary>
    /// Creates a sponsor configuration with validation.
    /// </summary>
    public static Result<SponsorConfiguration> Create(
        bool isEnabled,
        bool acceptMoneySponsors,
        bool acceptItemSponsors,
        decimal? minSponsorAmount,
        string? sponsorMessage,
        bool showSponsorList = false,
        decimal? minAmountForSponsorImage = null)
    {
        if (!isEnabled)
            return Result<SponsorConfiguration>.Success(Disabled());

        // Must accept at least one type
        if (!acceptMoneySponsors && !acceptItemSponsors)
            return Result<SponsorConfiguration>.Failure(
                "Must accept at least one sponsor type (money or item)");

        // Validate min sponsor amount
        if (minSponsorAmount.HasValue)
        {
            if (!acceptMoneySponsors)
                return Result<SponsorConfiguration>.Failure(
                    "Minimum sponsor amount only applies when money sponsors are accepted");

            if (minSponsorAmount.Value < MINIMUM_SPONSOR_AMOUNT)
                return Result<SponsorConfiguration>.Failure(
                    $"Minimum sponsor amount must be at least {MINIMUM_SPONSOR_AMOUNT:C}");
        }

        // Validate message length
        if (sponsorMessage != null && sponsorMessage.Length > MAX_MESSAGE_LENGTH)
            return Result<SponsorConfiguration>.Failure(
                $"Sponsor message cannot exceed {MAX_MESSAGE_LENGTH} characters");

        // Phase 6A.145 — image threshold must be ≥ system minimum when set
        if (minAmountForSponsorImage.HasValue && minAmountForSponsorImage.Value < MINIMUM_SPONSOR_AMOUNT)
            return Result<SponsorConfiguration>.Failure(
                $"Sponsor image threshold must be at least {MINIMUM_SPONSOR_AMOUNT:C}");

        return Result<SponsorConfiguration>.Success(new SponsorConfiguration(
            true,
            acceptMoneySponsors,
            acceptItemSponsors,
            minSponsorAmount,
            sponsorMessage?.Trim(),
            showSponsorList,
            minAmountForSponsorImage));
    }

    /// <summary>
    /// Creates a disabled (default) sponsor configuration.
    /// </summary>
    public static SponsorConfiguration Disabled()
    {
        return new SponsorConfiguration(false, false, false, null, null, false, null);
    }

    /// <summary>
    /// Validates whether a monetary sponsorship amount is acceptable per this configuration.
    /// </summary>
    public Result ValidateMoneyAmount(decimal amount)
    {
        if (!IsEnabled)
            return Result.Failure("Sponsorships are not enabled for this event");

        if (!AcceptMoneySponsors)
            return Result.Failure("Money sponsorships are not accepted for this event");

        if (amount < MINIMUM_SPONSOR_AMOUNT)
            return Result.Failure($"Sponsor amount must be at least {MINIMUM_SPONSOR_AMOUNT:C}");

        if (MinSponsorAmount.HasValue && amount < MinSponsorAmount.Value)
            return Result.Failure($"Sponsor amount must be at least {MinSponsorAmount.Value:C}");

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.145 — checks whether the given contribution amount qualifies the
    /// sponsor for image upload per <see cref="MinAmountForSponsorImage"/>. Returns
    /// false when the feature is disabled (threshold null) or the amount is null.
    /// </summary>
    public bool QualifiesForImage(decimal? contributionAmount)
    {
        if (!MinAmountForSponsorImage.HasValue) return false;
        if (!contributionAmount.HasValue) return false;
        return contributionAmount.Value >= MinAmountForSponsorImage.Value;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return IsEnabled;
        yield return AcceptMoneySponsors;
        yield return AcceptItemSponsors;
        yield return MinSponsorAmount ?? 0m;
        yield return SponsorMessage ?? string.Empty;
        yield return ShowSponsorList;
        yield return MinAmountForSponsorImage ?? 0m;
    }
}
