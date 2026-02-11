using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Commands.AddFormQuestion;

/// <summary>
/// Adds a question to an existing event form.
/// Options are required for SingleChoice, MultipleChoice, and Dropdown question types.
/// </summary>
public record AddFormQuestionCommand(
    Guid EventId,
    Guid FormId,
    string QuestionText,
    FormQuestionType QuestionType,
    bool IsRequired,
    int SortOrder,
    string? HelpText,
    List<AddQuestionOptionItem>? Options
) : ICommand<Guid>;

public record AddQuestionOptionItem(
    string Text,
    int SortOrder
);
