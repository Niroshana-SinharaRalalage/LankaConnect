namespace LankaConnect.Modules.Forms.Contracts;

/// <summary>
/// Contracts mirror of <c>LankaConnect.Modules.Forms.Domain.Enums.FormQuestionType</c>.
/// Duplicated by deliberate design (Wave 5.3a 2026-06-11) per the same rule
/// as <see cref="FormStatusDto"/>.
/// </summary>
public enum FormQuestionTypeDto
{
    ShortText = 0,
    LongText = 1,
    SingleChoice = 2,
    MultipleChoice = 3,
    Dropdown = 4,
    Number = 5,
    Date = 6,
    YesNo = 7,
}
