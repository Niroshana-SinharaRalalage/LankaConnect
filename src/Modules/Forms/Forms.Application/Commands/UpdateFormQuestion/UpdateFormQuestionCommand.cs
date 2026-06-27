using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.Enums;

namespace LankaConnect.Modules.Forms.Application.Commands.UpdateFormQuestion;

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
