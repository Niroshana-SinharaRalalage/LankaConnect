using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.SubmitFormResponse;

/// <summary>
/// Submits a response to an active event form.
/// Returns the response ID and a plaintext access token for anonymous editing.
/// </summary>
public record SubmitFormResponseCommand(
    Guid EventId,
    Guid FormId,
    string? RespondentEmail,
    string? RespondentName,
    Guid? RespondentUserId,
    List<SubmitFormAnswerItem> Answers
) : ICommand<SubmitFormResponseResult>;

public record SubmitFormAnswerItem(
    Guid QuestionId,
    string? TextValue,
    List<Guid>? SelectedOptionIds,
    bool? BooleanValue
);

public record SubmitFormResponseResult(
    Guid ResponseId,
    string AccessToken
);
