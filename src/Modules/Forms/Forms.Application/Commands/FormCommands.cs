using LankaConnect.Modules.Forms.Contracts;
using LankaConnect.Modules.Forms.Domain.Repositories;

namespace LankaConnect.Modules.Forms.Application.Commands;

/// <summary>
/// Implementation of <see cref="IFormCommands"/>. Wave 5.3b (2026-06-11).
/// Scope: cross-module mutators only. Self-saves on FormsDbContext via
/// IFormResponseRepository.DeleteAsync (W4.0b pattern).
/// </summary>
public sealed class FormCommands : IFormCommands
{
    private readonly IFormResponseRepository _formResponseRepository;

    public FormCommands(IFormResponseRepository formResponseRepository)
    {
        _formResponseRepository = formResponseRepository;
    }

    public async Task<int> DeleteResponsesByEventAndUserAsync(
        Guid eventId,
        Guid userId,
        CancellationToken ct = default)
    {
        var responses = await _formResponseRepository.GetByEventAndUserAsync(eventId, userId, ct);

        if (responses.Count == 0)
        {
            return 0;
        }

        // Wave 5.3d.2: raise FormResponseDeletedEvent per response BEFORE delete.
        // Mirrors DeleteFormResponseCommandHandler.cs:143 — without it, the
        // FormResponseDeleted email + WhatsApp pipeline goes silent when this
        // contract method replaces the previous in-line CancelRsvp loop.
        foreach (var response in responses)
        {
            response.RaiseDeletedEvent();
            await _formResponseRepository.DeleteAsync(response, ct);
        }

        return responses.Count;
    }
}
