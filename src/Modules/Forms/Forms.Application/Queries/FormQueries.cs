using LankaConnect.Modules.Forms.Application.Mappings;
using LankaConnect.Modules.Forms.Contracts;
using LankaConnect.Modules.Forms.Domain.Repositories;

namespace LankaConnect.Modules.Forms.Application.Queries;

/// <summary>
/// Implementation of <see cref="IFormQueries"/>. Wave 5.3b (2026-06-11).
/// Thin adapter over <see cref="IFormRepository"/> + <see cref="IFormResponseRepository"/>
/// that projects Domain aggregates to Contracts DTOs.
/// </summary>
public sealed class FormQueries : IFormQueries
{
    private readonly IFormRepository _formRepository;
    private readonly IFormResponseRepository _formResponseRepository;

    public FormQueries(
        IFormRepository formRepository,
        IFormResponseRepository formResponseRepository)
    {
        _formRepository = formRepository;
        _formResponseRepository = formResponseRepository;
    }

    public async Task<IReadOnlyList<FormSummaryDto>> GetByOwnerAsync(
        FormOwnerEntityTypeDto ownerType,
        Guid ownerId,
        CancellationToken ct = default)
    {
        if (ownerType != FormOwnerEntityTypeDto.Event)
        {
            // Only Event owners exist today. Future owner kinds add a new branch
            // here without touching consumers.
            return Array.Empty<FormSummaryDto>();
        }

        var forms = await _formRepository.GetByEventIdAsync(ownerId, ct);

        var result = new List<FormSummaryDto>(forms.Count);
        foreach (var form in forms)
        {
            var responseCount = await _formResponseRepository.GetCountByFormIdAsync(form.Id, ct);
            result.Add(form.ToSummaryDto(responseCount));
        }

        return result;
    }

    public async Task<FormSummaryDto?> GetByIdAsync(Guid formId, CancellationToken ct = default)
    {
        var form = await _formRepository.GetByIdAsync(formId, ct);
        if (form is null)
        {
            return null;
        }

        var responseCount = await _formResponseRepository.GetCountByFormIdAsync(formId, ct);
        return form.ToSummaryDto(responseCount);
    }

    public async Task<FormDetailDto?> GetByIdWithQuestionsAsync(Guid formId, CancellationToken ct = default)
    {
        var form = await _formRepository.GetByIdWithQuestionsAsync(formId, ct);
        if (form is null)
        {
            return null;
        }

        var responseCount = await _formResponseRepository.GetCountByFormIdAsync(formId, ct);
        return form.ToDetailDto(responseCount);
    }

    public async Task<FormResponseDetailDto?> GetResponseWithAnswersAsync(
        Guid responseId,
        CancellationToken ct = default)
    {
        var response = await _formResponseRepository.GetByIdWithAnswersAsync(responseId, ct);
        return response?.ToContractDto();
    }
}
