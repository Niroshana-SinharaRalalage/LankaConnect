using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.UpdateSignUpList;

/// <summary>
/// Command to update sign-up list details (category, description, and category flags)
/// Phase 6A.13: Edit Sign-Up List feature
/// </summary>
public record UpdateSignUpListCommand(
    Guid EventId,
    Guid SignUpListId,
    string Category,
    string Description,
    bool HasMandatoryItems,
    bool HasPreferredItems,
    bool HasSuggestedItems
) : ICommand;
