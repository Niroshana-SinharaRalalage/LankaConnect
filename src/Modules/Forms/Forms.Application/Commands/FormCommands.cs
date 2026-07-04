using LankaConnect.Modules.Forms.Contracts;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Modules.Forms.Infrastructure.Data;

namespace LankaConnect.Modules.Forms.Application.Commands;

/// <summary>
/// Implementation of <see cref="IFormCommands"/>. Wave 5.3b (2026-06-11).
/// Scope: cross-module mutators only.
/// </summary>
/// <remarks>
/// Wave 6.5.d (2026-07-03): the underlying <see cref="IFormResponseRepository"/>
/// no longer self-saves (per Outbox cutover). This facade preserves the pre-6.5.d
/// caller contract — <c>DeleteResponsesByEventAndUserAsync</c> commits the
/// <see cref="FormsDbContext"/> before returning so cross-module callers
/// (currently only <c>CancelRsvpCommandHandler</c>) continue to observe atomic
/// FormResponse delete + AppDbContext commit ordering without needing to know
/// about the multi-context UnitOfWork. When CancelRsvp migrates to
/// <c>IMultiContextUnitOfWork.CommitAsync(new DbContext[] { _formsContext }, ct)</c>
/// (post-Wave 6.5.f), this self-save can be removed.
/// </remarks>
public sealed class FormCommands : IFormCommands
{
    private readonly IFormResponseRepository _formResponseRepository;
    private readonly FormsDbContext _formsContext;

    public FormCommands(
        IFormResponseRepository formResponseRepository,
        FormsDbContext formsContext)
    {
        _formResponseRepository = formResponseRepository;
        _formsContext = formsContext;
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

        // Wave 6.5.d transitional: repo.DeleteAsync now stages only; commit on
        // FormsDbContext here so the pre-6.5.d cross-module caller contract
        // (fire-and-forget commit) survives. See class remarks.
        await _formsContext.SaveChangesAsync(ct);

        return responses.Count;
    }
}
