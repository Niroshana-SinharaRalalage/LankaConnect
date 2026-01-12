using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Communications.Commands.CreateNewsletter;

/// <summary>
/// Command to create a new newsletter
/// Phase 6A.74: Newsletter/News Alert creation with location targeting
/// </summary>
public record CreateNewsletterCommand(
    string Title,
    string Description,
    List<Guid> EmailGroupIds,
    bool IncludeNewsletterSubscribers,
    Guid? EventId = null,
    // Phase 6A.74 Enhancement 1: Location Targeting for Non-Event Newsletters
    List<Guid>? MetroAreaIds = null,
    bool TargetAllLocations = false
) : ICommand<Guid>;
