using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;

namespace LankaConnect.Modules.Forms.Application.Commands.ReorderFormQuestions;

/// <summary>
/// Reorders questions within a form by providing the full ordered list of question IDs.
/// </summary>
public record ReorderFormQuestionsCommand(
    Guid EventId,
    Guid FormId,
    List<Guid> QuestionIdsInOrder
) : ICommand;
