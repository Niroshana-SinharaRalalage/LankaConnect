using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Communications.Commands.UpdateNewsletter;

/// <summary>
/// Phase 6A.74: Command to update a newsletter in Draft status
/// </summary>
public record UpdateNewsletterCommand(
    Guid Id,
    string Title,
    string Description,
    List<Guid> EmailGroupIds,
    bool IncludeNewsletterSubscribers,
    Guid? EventId = null,
    List<Guid>? MetroAreaIds = null,
    bool TargetAllLocations = false
) : ICommand<bool>;
