using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;

namespace LankaConnect.Modules.Forms.Application.Commands.SubmitFormResponse;

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
