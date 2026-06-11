namespace LankaConnect.Modules.Forms.Contracts;

/// <summary>
/// Cross-module projection of a single answer within a FormResponse.
/// Returned inside <see cref="FormResponseDetailDto.Answers"/>.
/// Wave 5.3a (2026-06-11).
/// </summary>
/// <remarks>
/// <see cref="AnswerText"/> carries the verbatim string captured at submit
/// time. For choice-type questions this is the option text(s), for
/// numeric questions the stringified number, for Date the ISO-8601 form.
/// Consumers that need typed-back values map via the parent
/// <see cref="QuestionType"/>.
/// </remarks>
public sealed record FormAnswerContractDto(
    Guid Id,
    Guid QuestionId,
    string QuestionText,
    FormQuestionTypeDto QuestionType,
    string AnswerText);
