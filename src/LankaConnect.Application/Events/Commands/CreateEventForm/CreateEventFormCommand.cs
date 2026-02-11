using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Commands.CreateEventForm;

/// <summary>
/// Creates a new custom form for an event with initial questions.
/// Returns the created form's ID.
/// </summary>
public record CreateEventFormCommand(
    Guid EventId,
    string Title,
    string? Description,
    bool AllowMultipleResponses,
    DateTime? ResponseDeadline,
    int? MaxResponses,
    List<CreateFormQuestionItem> Questions
) : ICommand<Guid>;

/// <summary>
/// Nested DTO for questions to include when creating a form.
/// </summary>
public record CreateFormQuestionItem(
    string QuestionText,
    FormQuestionType QuestionType,
    bool IsRequired,
    int SortOrder,
    string? HelpText,
    List<CreateQuestionOptionItem>? Options);

/// <summary>
/// Nested DTO for options to include when creating a question.
/// </summary>
public record CreateQuestionOptionItem(
    string Text,
    int SortOrder);
