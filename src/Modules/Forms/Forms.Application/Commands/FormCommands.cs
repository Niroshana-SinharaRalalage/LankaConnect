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

        foreach (var response in responses)
        {
            await _formResponseRepository.DeleteAsync(response, ct);
        }

        return responses.Count;
    }
}
