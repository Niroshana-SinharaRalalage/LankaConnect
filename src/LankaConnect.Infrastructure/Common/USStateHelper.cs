namespace LankaConnect.Infrastructure.Common;

/// <summary>
/// Helper class for normalizing US state names and abbreviations.
/// Used by repositories to match state data regardless of input format (full name or abbreviation).
/// Phase 6A Event Notifications: Fixes state matching in metro area and newsletter subscriber queries.
/// </summary>
public static class USStateHelper
{
    /// <summary>
    /// Maps US state names to their 2-letter abbreviations (case-insensitive lookup).
    /// </summary>
    private static readonly Dictionary<string, string> StateNameToAbbreviation = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Alabama", "AL" },
        { "Alaska", "AK" },
        { "Arizona", "AZ" },
        { "Arkansas", "AR" },
        { "California", "CA" },
        { "Colorado", "CO" },
        { "Connecticut", "CT" },
        { "Delaware", "DE" },
        { "Florida", "FL" },
        { "Georgia", "GA" },
        { "Hawaii", "HI" },
        { "Idaho", "ID" },
        { "Illinois", "IL" },
        { "Indiana", "IN" },
        { "Iowa", "IA" },
        { "Kansas", "KS" },
        { "Kentucky", "KY" },
        { "Louisiana", "LA" },
        { "Maine", "ME" },
        { "Maryland", "MD" },
        { "Massachusetts", "MA" },
        { "Michigan", "MI" },
        { "Minnesota", "MN" },
        { "Mississippi", "MS" },
        { "Missouri", "MO" },
        { "Montana", "MT" },
        { "Nebraska", "NE" },
        { "Nevada", "NV" },
        { "New Hampshire", "NH" },
        { "New Jersey", "NJ" },
        { "New Mexico", "NM" },
        { "New York", "NY" },
        { "North Carolina", "NC" },
        { "North Dakota", "ND" },
        { "Ohio", "OH" },
        { "Oklahoma", "OK" },
        { "Oregon", "OR" },
        { "Pennsylvania", "PA" },
        { "Rhode Island", "RI" },
        { "South Carolina", "SC" },
        { "South Dakota", "SD" },
        { "Tennessee", "TN" },
        { "Texas", "TX" },
        { "Utah", "UT" },
        { "Vermont", "VT" },
        { "Virginia", "VA" },
        { "Washington", "WA" },
        { "West Virginia", "WV" },
        { "Wisconsin", "WI" },
        { "Wyoming", "WY" },
        { "District of Columbia", "DC" },
        { "Puerto Rico", "PR" },
        { "Guam", "GU" },
        { "American Samoa", "AS" },
        { "U.S. Virgin Islands", "VI" },
        { "Northern Mariana Islands", "MP" }
    };

    /// <summary>
    /// All valid 2-letter state abbreviations for quick validation.
    /// </summary>
    private static readonly HashSet<string> ValidAbbreviations = new(
        StateNameToAbbreviation.Values,
        StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Normalizes a state input (full name or abbreviation) to the 2-letter abbreviation.
    /// </summary>
    /// <param name="state">State name or abbreviation to normalize</param>
    /// <returns>2-letter state abbreviation in uppercase, or null if not a valid US state</returns>
    public static string? NormalizeToAbbreviation(string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
            return null;

        var trimmed = state.Trim();

        // If already a valid 2-letter abbreviation, return it uppercase
        if (trimmed.Length == 2 && ValidAbbreviations.Contains(trimmed))
            return trimmed.ToUpperInvariant();

        // Try to find by full state name
        if (StateNameToAbbreviation.TryGetValue(trimmed, out var abbreviation))
            return abbreviation;

        // Not a recognized US state
        return null;
    }

    /// <summary>
    /// Checks if a given state input matches the database abbreviation.
    /// Handles both full state names and abbreviations.
    /// </summary>
    /// <param name="inputState">Input state from event location (could be full name or abbreviation)</param>
    /// <param name="dbAbbreviation">State abbreviation stored in database</param>
    /// <returns>True if they represent the same state</returns>
    public static bool StatesMatch(string? inputState, string? dbAbbreviation)
    {
        if (string.IsNullOrWhiteSpace(inputState) || string.IsNullOrWhiteSpace(dbAbbreviation))
            return false;

        var normalizedInput = NormalizeToAbbreviation(inputState);
        if (normalizedInput == null)
        {
            // If we can't normalize, fall back to case-insensitive string comparison
            return inputState.Trim().Equals(dbAbbreviation.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        return normalizedInput.Equals(dbAbbreviation.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
