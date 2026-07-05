using LankaConnect.Modules.Communications.Domain.Repositories;
using LankaConnect.Modules.Communications.Domain.Entities;
using LankaConnect.Modules.Communications.Domain;
using LankaConnect.Modules.Communications.Application.Mappings;
using LankaConnect.Modules.Communications.Contracts;
namespace LankaConnect.Modules.Communications.Application.Queries;

/// <summary>
/// Implementation of <see cref="IEmailGroupQueries"/>. Wave 5.4.b (2026-06-13).
/// Thin adapter over <see cref="IEmailGroupRepository"/> that projects Domain
/// aggregates to Contracts DTOs.
/// </summary>
/// <remarks>
/// The wrapped repository still lives in
/// <c>LankaConnect.Domain.Communications</c> until Wave 5.4.d.2 (physical
/// move). The transitional <c>ProjectReference LankaConnect.Domain</c> in
/// Communications.Application.csproj is what makes this wiring compile during
/// the transition window.
/// </remarks>
public sealed class EmailGroupQueries : IEmailGroupQueries
{
    private readonly IEmailGroupRepository _repository;

    public EmailGroupQueries(IEmailGroupRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<EmailGroupSummaryDto>> GetByIdsAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<EmailGroupSummaryDto>();
        }

        var groups = await _repository.GetByIdsAsync(ids, ct);
        var result = new List<EmailGroupSummaryDto>(groups.Count);
        foreach (var group in groups)
        {
            result.Add(group.ToSummaryDto());
        }
        return result;
    }

    public async Task<EmailGroupSummaryDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var group = await _repository.GetByIdAsync(id, ct);
        return group?.ToSummaryDto();
    }

    public async Task<EmailGroupDetailDto?> GetByIdWithEmailsAsync(Guid id, CancellationToken ct = default)
    {
        var group = await _repository.GetByIdAsync(id, ct);
        return group?.ToDetailDto();
    }

    public async Task<IReadOnlyList<EmailGroupDetailDto>> GetByIdsWithEmailsAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<EmailGroupDetailDto>();
        }

        var groups = await _repository.GetByIdsAsync(ids, ct);
        var result = new List<EmailGroupDetailDto>(groups.Count);
        foreach (var group in groups)
        {
            result.Add(group.ToDetailDto());
        }
        return result;
    }

    public async Task<IReadOnlyList<EmailGroupSummaryDto>> GetByOwnerAsync(
        Guid ownerId,
        CancellationToken ct = default)
    {
        var groups = await _repository.GetByOwnerAsync(ownerId, ct);
        var result = new List<EmailGroupSummaryDto>(groups.Count);
        foreach (var group in groups)
        {
            result.Add(group.ToSummaryDto());
        }
        return result;
    }
}
