namespace LankaConnect.Modules.Forms.Contracts;

/// <summary>
/// Cross-module projection of a single FormResponse (one user's submitted
/// answers to a Form). Returned by
/// <see cref="IFormQueries.GetResponseWithAnswersAsync"/>.
/// Wave 5.3a (2026-06-11).
/// </summary>
public sealed record FormResponseDetailDto(
    Guid Id,
    Guid FormId,
    Guid EventId,
    string? RespondentEmail,
    string? RespondentName,
    Guid? RespondentUserId,
    DateTime SubmittedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<FormAnswerContractDto> Answers);
