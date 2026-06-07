using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;

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
