using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Domain.Events.Repositories;

/// <summary>
/// Repository interface for FormResponse aggregate root.
/// FormResponse is an independent aggregate (not a child of EventForm) per architect decision.
/// Responses are unbounded and need pagination support.
/// </summary>
public interface IFormResponseRepository : IRepository<FormResponse>
{
    /// <summary>
    /// Gets a FormResponse with its answers eagerly loaded.
    /// </summary>
    Task<FormResponse?> GetByIdWithAnswersAsync(Guid responseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a FormResponse by the SHA256 hash of its access token.
    /// Used for anonymous respondent edit access.
    /// </summary>
    Task<FormResponse?> GetByAccessTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a FormResponse by form ID and authenticated user ID.
    /// Used to check if a logged-in user already submitted a response.
    /// </summary>
    Task<FormResponse?> GetByFormAndUserAsync(Guid formId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a FormResponse by form ID and respondent email.
    /// Used to check if an anonymous respondent already submitted a response.
    /// </summary>
    Task<FormResponse?> GetByFormAndEmailAsync(Guid formId, string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of responses for a specific form.
    /// Used to check MaxResponses limit.
    /// </summary>
    Task<int> GetCountByFormIdAsync(Guid formId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated responses for a specific form with answers loaded.
    /// Used by organizer response viewer.
    /// </summary>
    Task<(IReadOnlyList<FormResponse> Responses, int TotalCount)> GetPaginatedAsync(
        Guid formId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all FormResponses for a specific user across all forms in an event.
    /// Used during cancellation to delete all user form submissions.
    /// </summary>
    Task<IReadOnlyList<FormResponse>> GetByEventAndUserAsync(
        Guid eventId, Guid userId, CancellationToken cancellationToken = default);
}
