using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.DeleteFormQuestion;

/// <summary>
/// Deletes a question from an event form.
/// Blocked if the form has any responses submitted.
/// </summary>
public record DeleteFormQuestionCommand(
    Guid EventId,
    Guid FormId,
    Guid QuestionId
) : ICommand;
