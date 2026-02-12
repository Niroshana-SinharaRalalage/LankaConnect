using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Commands.UpdateFormQuestion;

/// <summary>
/// Updates an existing form question.
/// Type changes are blocked when the form has responses.
/// </summary>
public record UpdateFormQuestionCommand(
    Guid EventId,
    Guid FormId,
    Guid QuestionId,
    string QuestionText,
    FormQuestionType QuestionType,
    bool IsRequired,
    int SortOrder,
    string? HelpText,
    List<UpdateQuestionOptionItem>? Options
) : ICommand;

public record UpdateQuestionOptionItem(
    Guid? Id,
    string Text,
    int SortOrder
);
