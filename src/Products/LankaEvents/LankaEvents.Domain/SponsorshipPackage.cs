using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
namespace LankaConnect.Products.LankaEvents.Domain;

/// <summary>
/// Phase 6A.156 — Organizer-defined sponsorship package (Gold / Silver / Bronze,
/// "Stage Sponsor", "Beverage Sponsor", etc.). Catalogue half of the packaged-
/// sponsorship feature, modelled byte-for-byte after <see cref="AddOnDefinition"/>:
///
/// <list type="bullet">
///   <item>The aggregate stores the curated offering (name, price, perks, stock cap).</item>
///   <item>The <see cref="Sponsor"/> aggregate stores the buyer transaction (via the
///         nullable <c>SponsorshipPackageId</c> FK introduced in 6A.156).</item>
///   <item>Stock management is via atomic SQL UPDATE in
///         <c>SponsorshipPackageRepository</c>, NOT EF Core change tracking.
///         <see cref="QuantitySold"/> MUST NOT be modified through domain methods —
///         only via <c>TryReserveStockAsync</c> / <c>TryRestoreStockAsync</c>.</item>
/// </list>
///
/// Coexistence rule: the existing generic Sponsor flow (free-form amount or item)
/// continues unchanged — generic sponsorship is simply <see cref="Sponsor"/> with
/// <c>SponsorshipPackageId IS NULL</c>. The two flows live in the same table,
/// distinguished by the nullable FK.
///
/// Money-only v1: package-based item sponsorship is deferred; the existing
/// free-form item flow on <see cref="Sponsor"/> remains the in-kind path.
/// </summary>
public class SponsorshipPackage : LegacyBaseEntity
{
    public const int MAX_NAME_LENGTH = 200;
    public const int MAX_DESCRIPTION_LENGTH = 1000;
    public const int MAX_TIER_LENGTH = 100;
    public const int MAX_PERKS_COUNT = 10;
    public const int MAX_PERK_LENGTH = 200;
    public const int MAX_INCLUDED_TICKET_COUNT = 20;

    // Event linkage
    public Guid EventId { get; private set; }

    // Package details
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Money Price { get; private set; } = null!;

    /// <summary>
    /// Maximum number of sponsorships available at this tier. Null = unlimited.
    /// </summary>
    public int? QuantityLimit { get; private set; }

    /// <summary>
    /// Current number of sponsorships sold/reserved at this tier.
    /// IMPORTANT: Managed via atomic SQL in the repository, NOT EF Core tracking.
    /// Do NOT mutate this through domain methods.
    /// </summary>
    public int QuantitySold { get; private set; }

    /// <summary>
    /// Whether this package is active and available for purchase.
    /// Soft-delete: set to false to hide without losing historical data.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Display order in the package grid (lower = first).
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Optional public URL of the package's promotional image. Always set
    /// together with <see cref="ImageBlobName"/>. Mirrors the pattern in
    /// <see cref="AddOnDefinition.ImageUrl"/>.
    /// </summary>
    public string? ImageUrl { get; private set; }

    /// <summary>
    /// Azure blob name (not URL) used by the upload handler to delete the
    /// old blob on replace, or to clear the blob on ClearImage. Always set
    /// together with <see cref="ImageUrl"/>.
    /// </summary>
    public string? ImageBlobName { get; private set; }

    /// <summary>
    /// Free-text tier label shown as a badge on the package card. Organizers
    /// choose their own taxonomy ("Gold", "Platinum", "Stage Sponsor"). Not an
    /// enum — there is no fixed list. Optional.
    /// </summary>
    public string? Tier { get; private set; }

    /// <summary>
    /// Ordered list of perks displayed as bullet points on the package card.
    /// Informational v1 — no per-perk fulfilment tracking. Persisted as a
    /// Postgres <c>text[]</c> column.
    ///
    /// Bounds enforced at create/update time: at most
    /// <see cref="MAX_PERKS_COUNT"/> entries; each entry at most
    /// <see cref="MAX_PERK_LENGTH"/> characters. Empty / whitespace entries
    /// are filtered out by the factories.
    /// </summary>
    public List<string> Perks { get; private set; } = new();

    /// <summary>
    /// Number of attendee slots automatically created when a buyer purchases
    /// this package. 0 = no tickets bundled (perks-only package). Range
    /// [0, <see cref="MAX_INCLUDED_TICKET_COUNT"/>].
    ///
    /// The actual ticket-allocation logic (auto-create head-count Registration
    /// for standalone, or attach to existing Registration for RSVP-bundled)
    /// lands in 6A.158 / 6A.159. 6A.156 only persists the field.
    /// </summary>
    public int IncludedTicketCount { get; private set; }

    // EF Core constructor
    private SponsorshipPackage()
    {
    }

    /// <summary>
    /// Creates a new sponsorship package. All validation runs here; the
    /// returned <see cref="Result{T}"/> contains either the validated entity
    /// or a human-readable error message.
    /// </summary>
    public static Result<SponsorshipPackage> Create(
        Guid eventId,
        string name,
        string? description,
        Money price,
        int? quantityLimit,
        int sortOrder,
        string? tier,
        IReadOnlyList<string>? perks,
        int includedTicketCount)
    {
        var validation = ValidateCommon(name, description, price, quantityLimit, tier, perks, includedTicketCount);
        if (validation.IsFailure)
            return Result<SponsorshipPackage>.Failure(validation.Error);

        if (eventId == Guid.Empty)
            return Result<SponsorshipPackage>.Failure("Event ID is required");

        var pkg = new SponsorshipPackage
        {
            EventId = eventId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Price = price,
            QuantityLimit = quantityLimit,
            QuantitySold = 0,
            IsActive = true,
            SortOrder = sortOrder,
            Tier = string.IsNullOrWhiteSpace(tier) ? null : tier.Trim(),
            Perks = NormalizePerks(perks),
            IncludedTicketCount = includedTicketCount
        };

        return Result<SponsorshipPackage>.Success(pkg);
    }

    /// <summary>
    /// Updates the package details. Does NOT change <see cref="IsActive"/> —
    /// use <see cref="Activate"/> / <see cref="Deactivate"/> for that. Cannot
    /// reduce <see cref="QuantityLimit"/> below current <see cref="QuantitySold"/>
    /// (would orphan paid sponsors).
    /// </summary>
    public Result UpdateDetails(
        string name,
        string? description,
        Money price,
        int? quantityLimit,
        int sortOrder,
        string? tier,
        IReadOnlyList<string>? perks,
        int includedTicketCount)
    {
        var validation = ValidateCommon(name, description, price, quantityLimit, tier, perks, includedTicketCount);
        if (validation.IsFailure)
            return Result.Failure(validation.Error);

        if (quantityLimit.HasValue && quantityLimit.Value < QuantitySold)
            return Result.Failure(
                $"Quantity limit ({quantityLimit.Value}) cannot be less than quantity already sold ({QuantitySold})");

        Name = name.Trim();
        Description = description?.Trim();
        Price = price;
        QuantityLimit = quantityLimit;
        SortOrder = sortOrder;
        Tier = string.IsNullOrWhiteSpace(tier) ? null : tier.Trim();
        Perks = NormalizePerks(perks);
        IncludedTicketCount = includedTicketCount;

        return Result.Success();
    }

    /// <summary>
    /// Soft-deletes the package (hides from purchase but retains data so
    /// existing sponsor rows still link to a valid package via the FK).
    /// </summary>
    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure("Sponsorship package is already inactive");

        IsActive = false;
        return Result.Success();
    }

    /// <summary>
    /// Re-activates a previously deactivated package.
    /// </summary>
    public Result Activate()
    {
        if (IsActive)
            return Result.Failure("Sponsorship package is already active");

        IsActive = true;
        return Result.Success();
    }

    /// <summary>
    /// Checks if there is available stock for the requested quantity.
    /// True if the package is unlimited or there's room.
    /// </summary>
    public bool HasAvailableStock(int requestedQty = 1)
    {
        if (!QuantityLimit.HasValue) return true;
        return QuantitySold + requestedQty <= QuantityLimit.Value;
    }

    /// <summary>
    /// Remaining stock count. Null means unlimited.
    /// </summary>
    public int? RemainingStock => QuantityLimit.HasValue
        ? QuantityLimit.Value - QuantitySold
        : null;

    /// <summary>
    /// Sets or replaces the package image. Both URL and blob name are required
    /// and set atomically — the handler is responsible for uploading the new
    /// blob first and deleting any prior blob on replace. Mirrors the pattern
    /// in <see cref="AddOnDefinition.SetImage"/>.
    /// </summary>
    public Result SetImage(string url, string blobName)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Result.Failure("Image URL is required");
        if (string.IsNullOrWhiteSpace(blobName))
            return Result.Failure("Image blob name is required");

        ImageUrl = url.Trim();
        ImageBlobName = blobName.Trim();
        return Result.Success();
    }

    /// <summary>
    /// Clears the package image. Idempotent — succeeds even when no image is
    /// currently set. The handler is responsible for deleting the blob from
    /// storage (best-effort, after the DB commit succeeds).
    /// </summary>
    public Result ClearImage()
    {
        ImageUrl = null;
        ImageBlobName = null;
        return Result.Success();
    }

    // =========================================================================
    // Private helpers
    // =========================================================================

    private static Result ValidateCommon(
        string name,
        string? description,
        Money price,
        int? quantityLimit,
        string? tier,
        IReadOnlyList<string>? perks,
        int includedTicketCount)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Sponsorship package name is required");

        if (name.Length > MAX_NAME_LENGTH)
            return Result.Failure($"Sponsorship package name cannot exceed {MAX_NAME_LENGTH} characters");

        if (description != null && description.Length > MAX_DESCRIPTION_LENGTH)
            return Result.Failure($"Sponsorship package description cannot exceed {MAX_DESCRIPTION_LENGTH} characters");

        if (price == null)
            return Result.Failure("Sponsorship package price is required");

        if (price.Amount < 0)
            return Result.Failure("Sponsorship package price cannot be negative");

        if (quantityLimit.HasValue && quantityLimit.Value <= 0)
            return Result.Failure("Quantity limit must be greater than zero");

        if (tier != null && tier.Length > MAX_TIER_LENGTH)
            return Result.Failure($"Tier label cannot exceed {MAX_TIER_LENGTH} characters");

        if (includedTicketCount < 0)
            return Result.Failure("Included ticket count cannot be negative");

        if (includedTicketCount > MAX_INCLUDED_TICKET_COUNT)
            return Result.Failure($"Included ticket count cannot exceed {MAX_INCLUDED_TICKET_COUNT}");

        if (perks != null)
        {
            // Filter empty entries during validation count so org-side blank inputs
            // don't surprise the user with a "too many" failure they can't see.
            var meaningful = perks.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();

            if (meaningful.Count > MAX_PERKS_COUNT)
                return Result.Failure($"A sponsorship package cannot have more than {MAX_PERKS_COUNT} perks");

            foreach (var perk in meaningful)
            {
                if (perk.Length > MAX_PERK_LENGTH)
                    return Result.Failure($"Each perk cannot exceed {MAX_PERK_LENGTH} characters");
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Normalizes the incoming perks list: trims each entry, drops empty/whitespace,
    /// returns a fresh <see cref="List{T}"/> safe for EF tracking and value-typed
    /// callers. Validation upstream guarantees count + length bounds.
    /// </summary>
    private static List<string> NormalizePerks(IReadOnlyList<string>? perks)
    {
        if (perks == null) return new List<string>();

        return perks
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .ToList();
    }
}
