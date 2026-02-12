using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.ReorderFormQuestions;

/// <summary>
/// Reorders questions within a form by providing the full ordered list of question IDs.
/// </summary>
public record ReorderFormQuestionsCommand(
    Guid EventId,
    Guid FormId,
    List<Guid> QuestionIdsInOrder
) : ICommand;
