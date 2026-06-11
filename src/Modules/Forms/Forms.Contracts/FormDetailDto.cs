namespace LankaConnect.Modules.Forms.Contracts;

/// <summary>
/// Cross-module projection of a Form with its question definitions
/// (no response data). Returned by
/// <see cref="IFormQueries.GetByIdWithQuestionsAsync"/>.
/// Wave 5.3a (2026-06-11).
/// </summary>
public sealed record FormDetailDto(
    Guid Id,
    FormOwnerEntityTypeDto OwnerEntityType,
    Guid OwnerEntityId,
    string Title,
    string? Description,
    FormStatusDto Status,
    bool AllowMultipleResponses,
    DateTime? ResponseDeadline,
    int? MaxResponses,
    bool HasResponses,
    int ResponseCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool AllowAttendeesToViewResponses,
    IReadOnlyList<FormQuestionContractDto> Questions);
