using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
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
    List<CreateFormQuestionItem> Questions,
    // Phase 6A.146: optional with default false so the ~10 existing positional
    // callers compile unchanged. UI passes the toggle explicitly.
    bool AllowAttendeesToViewResponses = false
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
