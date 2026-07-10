using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.Repositories;

/// <summary>
/// Phase 6A.156 — repository contract for the <see cref="SponsorshipPackage"/>
/// catalogue. Mirrors <see cref="IAddOnDefinitionRepository"/> byte-for-byte
/// (read-by-event, atomic stock reservation/restoration) so that consumers
/// already familiar with the add-on flow can navigate this code unchanged.
///
/// Atomic stock methods use raw SQL inside the repository (not EF Core change
/// tracking) to avoid lost-update races under concurrent purchases.
/// </summary>
public interface ISponsorshipPackageRepository : IRepository<SponsorshipPackage>
{
    /// <summary>
    /// Returns all sponsorship packages for an event, sorted by SortOrder ASC.
    /// Includes inactive (soft-deleted) packages — organizer-facing only. The
    /// public storefront should call <see cref="GetActiveByEventIdAsync"/>.
    /// </summary>
    Task<IReadOnlyList<SponsorshipPackage>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns only active sponsorship packages for an event, sorted by
    /// SortOrder ASC. This is what the buyer-facing UI consumes in 6A.157+.
    /// </summary>
    Task<IReadOnlyList<SponsorshipPackage>> GetActiveByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically reserves stock for a package purchase. Implementation runs
    /// a single SQL UPDATE with a WHERE clause that re-checks the available
    /// quantity, so concurrent buyers cannot oversell. Returns true if the
    /// reservation succeeded, false if insufficient stock or package
    /// inactive.
    /// </summary>
    Task<bool> TryReserveStockAsync(
        Guid packageId,
        int quantity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically restores stock after a failed / abandoned / refunded
    /// purchase. Implementation guards against underflow via
    /// <c>GREATEST(0, quantity_sold - @qty)</c>.
    /// </summary>
    Task<bool> TryRestoreStockAsync(
        Guid packageId,
        int quantity,
        CancellationToken cancellationToken = default);
}
