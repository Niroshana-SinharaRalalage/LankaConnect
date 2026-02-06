using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Communications.Commands.CreateNewsletter;

/// <summary>
/// Phase 6A.74: Command to create a new newsletter/news alert
/// Phase 6A.74 Part 14: Added IsAnnouncementOnly parameter for announcement-only newsletters
/// - When false (default): Creates in Draft status, must be published to /newsletters page
/// - When true: Auto-activates, NOT visible on public page, can send emails immediately
/// </summary>
public record CreateNewsletterCommand(
    string Title,
    string Description,
    List<Guid> EmailGroupIds,
    bool IncludeNewsletterSubscribers,
    Guid? EventId = null,
    List<Guid>? MetroAreaIds = null,
    bool TargetAllLocations = false,
    bool IsAnnouncementOnly = false
) : ICommand<Guid>;
