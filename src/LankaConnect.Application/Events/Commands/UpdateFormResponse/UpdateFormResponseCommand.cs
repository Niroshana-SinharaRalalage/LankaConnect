using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.UpdateFormResponse;

/// <summary>
/// Updates an existing form response.
/// Phase 6A.106-110 Fix: Supports both token-based (anonymous) and userId-based (logged-in) auth.
/// Blocked if the form's response deadline has passed.
/// </summary>
public record UpdateFormResponseCommand(
    Guid EventId,
    Guid FormId,
    Guid ResponseId,
    string? AccessToken,           // For anonymous respondents
    Guid? RequestingUserId,         // For logged-in users
    List<UpdateFormAnswerItem> Answers
) : ICommand;

public record UpdateFormAnswerItem(
    Guid QuestionId,
    string? TextValue,
    List<Guid>? SelectedOptionIds,
    bool? BooleanValue
);
