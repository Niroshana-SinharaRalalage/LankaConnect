using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Domain.Events.Repositories;

/// <summary>
/// Repository interface for EventForm aggregate root.
/// EventForm is an independent aggregate (not a child of Event) per architect decision.
/// </summary>
public interface IEventFormRepository : IRepository<EventForm>
{
    /// <summary>
    /// Gets an EventForm with its questions eagerly loaded.
    /// Use for command handlers that need to modify questions.
    /// </summary>
    Task<EventForm?> GetByIdWithQuestionsAsync(Guid formId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all forms for a specific event.
    /// Returns forms without questions loaded (summary view).
    /// </summary>
    Task<IReadOnlyList<EventForm>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
}
