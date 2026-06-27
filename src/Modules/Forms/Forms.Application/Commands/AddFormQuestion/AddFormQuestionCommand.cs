using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Modules.Forms.Application.Commands.AddFormQuestion;

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
