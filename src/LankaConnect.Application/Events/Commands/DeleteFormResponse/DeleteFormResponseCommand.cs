using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.DeleteFormResponse;

/// <summary>
/// Deletes a form response (cancellation workflow).
/// Phase 6A.106: Anonymous users use access token, logged-in users use userId.
/// Architect Review: Approved - priority-based auth ensures security.
/// </summary>
public record DeleteFormResponseCommand(
    Guid EventId,
    Guid FormId,
    Guid ResponseId,
    string? AccessToken = null,         // For anonymous respondents
    Guid? RequestingUserId = null       // For logged-in users
) : ICommand;
