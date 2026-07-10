using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Identity.Application.DTOs;
namespace LankaConnect.Modules.Identity.Application.Commands.Users.UpdateLanguages;

/// <summary>
/// Command to update user's languages (1-5 required)
/// Cannot be empty - at least 1 language required
/// </summary>
public record UpdateLanguagesCommand : ICommand
{
    public Guid UserId { get; init; }
    public List<LanguageDto> Languages { get; init; } = new();
}
