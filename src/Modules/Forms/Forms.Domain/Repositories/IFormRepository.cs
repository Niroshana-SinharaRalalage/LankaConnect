using LankaConnect.Modules.Forms.Domain.Entities;

namespace LankaConnect.Modules.Forms.Domain.Repositories;

/// <summary>
/// Repository interface for the <see cref="Form"/> aggregate root.
/// Hand-rolled per ADR-010 (Repository-per-Aggregate) — no IRepository&lt;T&gt;
/// base extension. AddAsync + UpdateAsync + DeleteAsync are self-saving on
/// the FormsDbContext per W4.0b pattern.
/// </summary>
public interface IFormRepository
{
    Task<Form?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Form with its questions eagerly loaded — for command-handler mutations.</summary>
    Task<Form?> GetByIdWithQuestionsAsync(Guid formId, CancellationToken cancellationToken = default);

    /// <summary>All forms for a specific event (summary view, no questions).</summary>
    Task<List<Form>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task AddAsync(Form entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Form entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Form entity, CancellationToken cancellationToken = default);
}
