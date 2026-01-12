using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Communications.Commands.UpdateNewsletter;

/// <summary>
/// Command to update a draft newsletter
/// Phase 6A.74: Newsletter updates (Draft only)
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
