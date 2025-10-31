namespace LankaConnect.Domain.Users.Enums;

/// <summary>
/// Language proficiency level (4-level scale)
/// Architecture: Following architect guidance - sequential 1-4 scale
/// Based on CEFR-inspired simplification for diaspora community
/// </summary>
public enum ProficiencyLevel
{
    /// <summary>
    /// Basic proficiency - Can understand and use familiar everyday expressions
    /// Roughly equivalent to CEFR A1-A2 (Beginner/Elementary)
    /// </summary>
    Basic = 1,

    /// <summary>
    /// Intermediate proficiency - Can handle routine work/social situations
    /// Roughly equivalent to CEFR B1-B2 (Intermediate/Upper Intermediate)
    /// </summary>
    Intermediate = 2,

    /// <summary>
    /// Advanced proficiency - Can express ideas fluently and spontaneously
    /// Roughly equivalent to CEFR C1 (Advanced)
    /// </summary>
    Advanced = 3,

    /// <summary>
    /// Native/Near-Native proficiency - Complete mastery of the language
    /// Roughly equivalent to CEFR C2 (Proficiency) or native speaker
    /// </summary>
    Native = 4
}
