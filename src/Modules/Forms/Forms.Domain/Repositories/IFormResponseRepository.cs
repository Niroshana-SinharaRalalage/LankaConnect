using LankaConnect.Modules.Forms.Domain.Entities;

namespace LankaConnect.Modules.Forms.Domain.Repositories;

/// <summary>
/// Repository interface for the <see cref="FormResponse"/> aggregate root.
/// Hand-rolled per ADR-010 (Repository-per-Aggregate) — no IRepository&lt;T&gt;
/// base extension. AddAsync + UpdateAsync + DeleteAsync are self-saving on
/// the FormsDbContext per W4.0b pattern.
/// </summary>
public interface IFormResponseRepository
{
    Task<FormResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>FormResponse with its answers eagerly loaded.</summary>
    Task<FormResponse?> GetByIdWithAnswersAsync(Guid responseId, CancellationToken cancellationToken = default);

    /// <summary>Lookup by SHA256(access_token) for anonymous respondent edit access.</summary>
    Task<FormResponse?> GetByAccessTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Lookup by (form, authenticated user) — used to detect duplicate submission.</summary>
    Task<FormResponse?> GetByFormAndUserAsync(Guid formId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Lookup by (form, respondent email) — used to detect duplicate anonymous submission.</summary>
    Task<FormResponse?> GetByFormAndEmailAsync(Guid formId, string email, CancellationToken cancellationToken = default);

    /// <summary>Count of responses for a specific form (MaxResponses gate).</summary>
    Task<int> GetCountByFormIdAsync(Guid formId, CancellationToken cancellationToken = default);

    /// <summary>Paginated responses with answers loaded — organizer response viewer.</summary>
    Task<(IReadOnlyList<FormResponse> Responses, int TotalCount)> GetPaginatedAsync(
        Guid formId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>All responses for a user across all forms in an event — cancellation cleanup.</summary>
    Task<IReadOnlyList<FormResponse>> GetByEventAndUserAsync(
        Guid eventId, Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(FormResponse entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(FormResponse entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(FormResponse entity, CancellationToken cancellationToken = default);
}
