namespace LankaConnect.BuildingBlocks.Infrastructure.Helpers;

/// <summary>
/// Helper class for sanitizing search terms before passing to PostgreSQL full-text search functions.
///
/// Phase 6A.XX: Hyphen Negation Bug Fix
///
/// Problem: PostgreSQL's websearch_to_tsquery interprets " - word" as a negation operator (!word).
/// For example: "Sample - Varuni" becomes 'sampl' &amp; !'varuni' which EXCLUDES events with "Varuni".
///
/// Solution: Sanitize hyphen patterns that could be interpreted as negation before passing to websearch_to_tsquery.
/// This preserves the user's intent (finding events containing all words) while maintaining
/// other websearch features like OR operators and quoted phrases.
/// </summary>
public static class SearchTermSanitizer
{
    /// <summary>
    /// Sanitizes a search term for use with PostgreSQL's websearch_to_tsquery function.
    /// Removes hyphen patterns that would be interpreted as negation operators while
    /// preserving hyphenated compound words (e.g., "well-known") and other valid patterns.
    /// </summary>
    /// <param name="searchTerm">The raw search term from user input</param>
    /// <returns>Sanitized search term safe for websearch_to_tsquery</returns>
    public static string SanitizeForWebSearch(string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return string.Empty;
        }

        var sanitized = searchTerm.Trim();

        // Order matters: process the most specific pattern first
        // Pattern 1: " - " (space-hyphen-space) -> most common in titles like "Event - Part 2"
        sanitized = sanitized.Replace(" - ", " ");

        // Pattern 2: " -" (space-hyphen) -> potential negation like "music -classical"
        sanitized = sanitized.Replace(" -", " ");

        // Pattern 3: "- " (hyphen-space) -> less common but still a potential issue
        sanitized = sanitized.Replace("- ", " ");

        // Handle double hyphens that might remain
        sanitized = sanitized.Replace("--", " ");

        // Clean up any double spaces that might have been created
        while (sanitized.Contains("  "))
        {
            sanitized = sanitized.Replace("  ", " ");
        }

        return sanitized.Trim();
    }
}
