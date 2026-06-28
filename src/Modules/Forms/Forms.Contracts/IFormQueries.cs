namespace LankaConnect.Modules.Forms.Contracts;

/// <summary>
/// Cross-module read-only API for the Forms module. Consumers outside Forms
/// (e.g. <c>LankaConnect.Products.LankaEvents.Application.EventHandlers.*</c>) inject this
/// interface instead of <c>Forms.Domain.Repositories.IFormRepository</c>,
/// preserving the ArchTest module boundary
/// <c>LegacyApplication_DoesNotDependOnFormsDomain</c> (Wave 5.3e).
/// </summary>
/// <remarks>
/// Wave 5.3a (2026-06-11). Implementation lives in Forms.Application as
/// <c>FormQueries</c> (added in Wave 5.3b); DI wiring is in
/// <c>Forms.Api.FormsModule</c>. The single mutator survives as
/// <see cref="IFormCommands.DeleteResponsesByEventAndUserAsync"/> for the
/// cancel-RSVP cleanup path.
/// </remarks>
public interface IFormQueries
{
    /// <summary>
    /// Returns the forms owned by the given entity (e.g., all forms attached
    /// to an Event). Order is unspecified at the Contracts layer; callers
    /// that need ordering should sort by <c>CreatedAt</c>.
    /// </summary>
    Task<IReadOnlyList<FormSummaryDto>> GetByOwnerAsync(
        FormOwnerEntityTypeDto ownerType,
        Guid ownerId,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a single form by primary key (no question detail). Returns
    /// <c>null</c> when no row exists.
    /// </summary>
    Task<FormSummaryDto?> GetByIdAsync(Guid formId, CancellationToken ct = default);

    /// <summary>
    /// Returns a single form by primary key WITH its question definitions
    /// hydrated. Returns <c>null</c> when no row exists.
    /// </summary>
    Task<FormDetailDto?> GetByIdWithQuestionsAsync(Guid formId, CancellationToken ct = default);

    /// <summary>
    /// Returns a single submitted response with its answers hydrated.
    /// Returns <c>null</c> when no row exists.
    /// </summary>
    Task<FormResponseDetailDto?> GetResponseWithAnswersAsync(
        Guid responseId, CancellationToken ct = default);
}
