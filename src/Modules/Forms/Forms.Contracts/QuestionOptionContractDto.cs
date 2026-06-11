namespace LankaConnect.Modules.Forms.Contracts;

/// <summary>
/// Cross-module projection of a single option attached to a
/// <see cref="FormQuestionContractDto"/> (e.g., one choice on a SingleChoice
/// or Dropdown question). Wave 5.3a (2026-06-11).
/// </summary>
public sealed record QuestionOptionContractDto(
    Guid Id,
    string OptionText,
    int SortOrder);
