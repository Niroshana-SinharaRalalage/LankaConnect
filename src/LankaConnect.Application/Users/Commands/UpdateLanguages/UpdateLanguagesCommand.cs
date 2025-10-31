using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Application.Users.Commands.UpdateLanguages;

/// <summary>
/// Command to update user's languages (1-5 required)
/// Cannot be empty - at least 1 language required
/// </summary>
public record UpdateLanguagesCommand : ICommand
{
    public Guid UserId { get; init; }
    public List<LanguageDto> Languages { get; init; } = new();
}

/// <summary>
/// DTO for language with proficiency level
/// </summary>
public class LanguageDto
{
    public string LanguageCode { get; set; } = null!;
    public ProficiencyLevel ProficiencyLevel { get; set; }
}
