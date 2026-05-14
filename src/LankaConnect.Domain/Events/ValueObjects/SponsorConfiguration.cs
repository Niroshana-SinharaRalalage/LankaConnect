using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Value object representing sponsor configuration for an event.
/// Stored as JSONB on the Event aggregate.
///
/// IMPORTANT (C5 Guard): This VO uses flat primitive types only — no nested Money
/// value objects — to avoid EF Core OwnsOne(ToJson) nested entity issues.
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
    /// Phase 6A.143 — public URL of the sponsor banner image displayed on the event
    /// details page above the sponsor form. Null when no banner uploaded. Always set
    /// together with <see cref="SponsorImageBlobName"/>.
    /// </summary>
    public string? SponsorImageUrl { get; private set; }

    /// <summary>
    /// Phase 6A.143 — Azure blob name (not URL) used by the upload handler to delete
    /// the old blob on replace, or to clear the blob on remove. Always set together
    /// with <see cref="SponsorImageUrl"/>.
    /// </summary>
    public string? SponsorImageBlobName { get; private set; }

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
        string? sponsorImageUrl = null,
        string? sponsorImageBlobName = null)
    {
        IsEnabled = isEnabled;
        AcceptMoneySponsors = acceptMoneySponsors;
        AcceptItemSponsors = acceptItemSponsors;
        MinSponsorAmount = minSponsorAmount;
        SponsorMessage = sponsorMessage;
        ShowSponsorList = showSponsorList;
        SponsorImageUrl = sponsorImageUrl;
        SponsorImageBlobName = sponsorImageBlobName;
    }

    /// <summary>
    /// Creates a sponsor configuration with validation. Phase 6A.143 added optional
    /// banner image fields — defaulting to null preserves all existing callers.
    /// </summary>
    public static Result<SponsorConfiguration> Create(
        bool isEnabled,
        bool acceptMoneySponsors,
        bool acceptItemSponsors,
        decimal? minSponsorAmount,
        string? sponsorMessage,
        bool showSponsorList = false,
        string? sponsorImageUrl = null,
        string? sponsorImageBlobName = null)
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

        // Phase 6A.143 — image url + blob name must be set together. Either both null
        // or both non-empty. Partial set is invalid (would orphan the blob).
        var urlSet = !string.IsNullOrWhiteSpace(sponsorImageUrl);
        var blobSet = !string.IsNullOrWhiteSpace(sponsorImageBlobName);
        if (urlSet != blobSet)
            return Result<SponsorConfiguration>.Failure(
                "Sponsor image URL and blob name must be set together");

        return Result<SponsorConfiguration>.Success(new SponsorConfiguration(
            true,
            acceptMoneySponsors,
            acceptItemSponsors,
            minSponsorAmount,
            sponsorMessage?.Trim(),
            showSponsorList,
            urlSet ? sponsorImageUrl!.Trim() : null,
            blobSet ? sponsorImageBlobName!.Trim() : null));
    }

    /// <summary>
    /// Creates a disabled (default) sponsor configuration.
    /// </summary>
    public static SponsorConfiguration Disabled()
    {
        return new SponsorConfiguration(false, false, false, null, null, false, null, null);
    }

    /// <summary>
    /// Phase 6A.143 — returns a new SponsorConfiguration with the banner image set,
    /// preserving all other fields. Used by the SetSponsorConfigImage handler so the
    /// Event aggregate can swap in the updated VO without touching unrelated fields.
    /// </summary>
    public Result<SponsorConfiguration> WithImage(string url, string blobName)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Result<SponsorConfiguration>.Failure("Image URL is required");
        if (string.IsNullOrWhiteSpace(blobName))
            return Result<SponsorConfiguration>.Failure("Image blob name is required");

        return Result<SponsorConfiguration>.Success(new SponsorConfiguration(
            IsEnabled, AcceptMoneySponsors, AcceptItemSponsors,
            MinSponsorAmount, SponsorMessage, ShowSponsorList,
            url.Trim(), blobName.Trim()));
    }

    /// <summary>
    /// Phase 6A.143 — returns a new SponsorConfiguration with the banner image cleared,
    /// preserving all other fields. Handler is responsible for deleting the blob.
    /// </summary>
    public SponsorConfiguration WithoutImage()
    {
        return new SponsorConfiguration(
            IsEnabled, AcceptMoneySponsors, AcceptItemSponsors,
            MinSponsorAmount, SponsorMessage, ShowSponsorList,
            null, null);
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
        // Phase 6A.143 — image fields MUST participate in equality so EF change-tracking
        // detects updates when only the banner is set/cleared. Without this, the
        // aggregate save would silently skip the JSONB write (per architect F13).
        yield return SponsorImageUrl ?? string.Empty;
        yield return SponsorImageBlobName ?? string.Empty;
    }
}
