using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Value object representing sponsor configuration for an event.
/// Stored as JSONB on the Event aggregate.
///
/// IMPORTANT (C5 Guard): This VO uses flat primitive types only — no nested Money
/// value objects — to avoid EF Core OwnsOne(ToJson) nested entity issues.
///
/// Phase 6A.145 Commit 6 — dropped the per-event MinAmountForSponsorImage threshold
/// per UAT feedback: ANY sponsor can attach an image regardless of amount. Image
/// upload is now an unconditional capability of the sponsor flow.
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
    /// Phase 6A.156 — whether sponsorship packages (organizer-curated tiers like
    /// Gold/Silver/Bronze) are exposed on the public event page. When false
    /// (default), packages can be defined and edited by the organizer but the
    /// public-facing package grid is hidden — useful for drafting before launch.
    /// Existing generic sponsorship (free-form amount/item) is governed by
    /// the IsEnabled / AcceptMoneySponsors / AcceptItemSponsors trio above.
    ///
    /// JSONB backward-compat: existing rows missing the EnablePackages key
    /// deserialize to false (default), keeping the pre-6A.156 behaviour for all
    /// existing events.
    /// </summary>
    public bool EnablePackages { get; private set; }

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
        bool enablePackages)
    {
        IsEnabled = isEnabled;
        AcceptMoneySponsors = acceptMoneySponsors;
        AcceptItemSponsors = acceptItemSponsors;
        MinSponsorAmount = minSponsorAmount;
        SponsorMessage = sponsorMessage;
        ShowSponsorList = showSponsorList;
        EnablePackages = enablePackages;
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
        bool enablePackages = false)
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

        return Result<SponsorConfiguration>.Success(new SponsorConfiguration(
            true,
            acceptMoneySponsors,
            acceptItemSponsors,
            minSponsorAmount,
            sponsorMessage?.Trim(),
            showSponsorList,
            enablePackages));
    }

    /// <summary>
    /// Creates a disabled (default) sponsor configuration.
    /// </summary>
    public static SponsorConfiguration Disabled()
    {
        return new SponsorConfiguration(false, false, false, null, null, false, false);
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

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return IsEnabled;
        yield return AcceptMoneySponsors;
        yield return AcceptItemSponsors;
        yield return MinSponsorAmount ?? 0m;
        yield return SponsorMessage ?? string.Empty;
        yield return ShowSponsorList;
        yield return EnablePackages;
    }
}
