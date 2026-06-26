using LankaConnect.Modules.Identity.Domain.Enums;

namespace LankaConnect.Modules.Identity.Application.DTOs;

/// <summary>
/// DTO for language with proficiency level
/// </summary>
public class LanguageDto
{
    public string LanguageCode { get; set; } = null!;
    public ProficiencyLevel ProficiencyLevel { get; set; }
}
