using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Modules.Identity.Application.Commands.Users.UpdateCulturalInterests;

/// <summary>
/// Command to update user's cultural interests (0-10 allowed, privacy choice)
/// Empty list clears all interests
/// </summary>
public record UpdateCulturalInterestsCommand : ICommand
{
    public Guid UserId { get; init; }
    public List<string> InterestCodes { get; init; } = new();
}
