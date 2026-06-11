namespace LankaConnect.Modules.Forms.Contracts;

/// <summary>
/// Cross-module projection of a single form question.
/// Returned inside <see cref="FormDetailDto.Questions"/>.
/// Wave 5.3a (2026-06-11).
/// </summary>
public sealed record FormQuestionContractDto(
    Guid Id,
    string QuestionText,
    FormQuestionTypeDto QuestionType,
    bool IsRequired,
    int SortOrder,
    string? HelpText,
    IReadOnlyList<QuestionOptionContractDto> Options);
