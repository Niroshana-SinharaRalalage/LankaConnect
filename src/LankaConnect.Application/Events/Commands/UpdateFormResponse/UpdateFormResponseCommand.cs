using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.UpdateFormResponse;

/// <summary>
/// Updates an existing form response. Requires the access token for authentication.
/// Blocked if the form's response deadline has passed.
/// </summary>
public record UpdateFormResponseCommand(
    Guid EventId,
    Guid FormId,
    Guid ResponseId,
    string AccessToken,
    List<UpdateFormAnswerItem> Answers
) : ICommand;

public record UpdateFormAnswerItem(
    Guid QuestionId,
    string? TextValue,
    List<Guid>? SelectedOptionIds,
    bool? BooleanValue
);
