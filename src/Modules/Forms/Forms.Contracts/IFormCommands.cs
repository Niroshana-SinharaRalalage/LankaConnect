namespace LankaConnect.Modules.Forms.Contracts;

/// <summary>
/// Cross-module mutator API for the Forms module. SCOPE: only operations
/// invoked FROM other modules. All within-Forms mutators (Create, Update,
/// Publish, Close, Delete etc.) move into Forms.Application in Wave 5.3c
/// and are NOT exposed via this contract — they remain MediatR commands
/// dispatched by the controllers that already live inside the module.
/// </summary>
/// <remarks>
/// Wave 5.3a (2026-06-11). Today the surface is a single method invoked
/// from <c>LankaConnect.Application.Events.Commands.CancelRsvp</c> when an
/// attendee cancels and any FormResponses they submitted need to be
/// scrubbed. Implementation in Forms.Application (Wave 5.3b) self-saves on
/// FormsDbContext — two-DbContext atomicity is lost vs. today's shared
/// IUnitOfWork; partial-failure semantics surface as a Result.Warning to
/// the caller (existing <c>warnings</c> collection in CancelRsvp handler).
/// </remarks>
public interface IFormCommands
{
    /// <summary>
    /// Deletes all FormResponses submitted by <paramref name="userId"/>
    /// against any form belonging to <paramref name="eventId"/>. Returns
    /// the number of responses deleted. Idempotent: returns 0 when nothing
    /// matches.
    /// </summary>
    Task<int> DeleteResponsesByEventAndUserAsync(
        Guid eventId,
        Guid userId,
        CancellationToken ct = default);
}
