namespace LankaConnect.Modules.Communications.Contracts.Email.Helpers;

/// <summary>
/// Builds RFC 2369 List-Unsubscribe and RFC 8058 List-Unsubscribe-Post email headers.
/// These headers enable one-click unsubscribe in Gmail, Yahoo, and other providers,
/// and are required by Google/Yahoo bulk sender guidelines (enforced since Feb 2024).
/// </summary>
public static class ListUnsubscribeHeaderBuilder
{
    public const string ListUnsubscribeHeader = "List-Unsubscribe";
    public const string ListUnsubscribePostHeader = "List-Unsubscribe-Post";
    public const string OneClickValue = "List-Unsubscribe=One-Click";

    /// <summary>
    /// Builds List-Unsubscribe and List-Unsubscribe-Post headers for a given unsubscribe URL.
    /// Returns empty dictionary if URL is null or empty.
    /// </summary>
    /// <param name="unsubscribeUrl">Per-recipient unsubscribe URL containing their unique token.</param>
    public static Dictionary<string, string> BuildHeaders(string? unsubscribeUrl)
    {
        if (string.IsNullOrWhiteSpace(unsubscribeUrl))
            return new Dictionary<string, string>();

        return new Dictionary<string, string>
        {
            // RFC 2369: URL wrapped in angle brackets
            { ListUnsubscribeHeader, $"<{unsubscribeUrl}>" },
            // RFC 8058: Tells email clients this supports one-click POST unsubscribe
            { ListUnsubscribePostHeader, OneClickValue }
        };
    }
}
