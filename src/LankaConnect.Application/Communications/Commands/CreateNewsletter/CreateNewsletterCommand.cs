using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Communications.Commands.CreateNewsletter;

/// <summary>
/// Phase 6A.74: Command to create a new newsletter/news alert in Draft status
/// </summary>
public record CreateNewsletterCommand(
    string Title,
    string Description,
    List<Guid> EmailGroupIds,
    bool IncludeNewsletterSubscribers,
    Guid? EventId = null,
    List<Guid>? MetroAreaIds = null,
    bool TargetAllLocations = false
) : ICommand<Guid>;
