namespace LankaConnect.Infrastructure.Email.Configuration;

/// <summary>
/// Configuration for email branding and visual appearance.
/// Supports color scheme customization without database migrations.
/// Phase 0 - Email System Configuration Infrastructure
/// </summary>
public sealed class BrandingOptions
{
    public const string SectionName = "Branding";

    /// <summary>
    /// Primary brand color (default: #fb923c - orange from rose palette)
    /// Used for headers, buttons, and primary UI elements
    /// </summary>
    public string PrimaryColor { get; set; } = "#fb923c";

    /// <summary>
    /// Secondary brand color (default: #f43f5e - rose)
    /// Used for accents, links, and secondary elements
    /// </summary>
    public string SecondaryColor { get; set; } = "#f43f5e";

    /// <summary>
    /// Background color for email body (default: #f9fafb - light gray)
    /// </summary>
    public string BackgroundColor { get; set; } = "#f9fafb";

    /// <summary>
    /// Text color for body content (default: #374151 - dark gray)
    /// </summary>
    public string TextColor { get; set; } = "#374151";

    /// <summary>
    /// Text color for headings (default: #111827 - nearly black)
    /// </summary>
    public string HeadingColor { get; set; } = "#111827";

    /// <summary>
    /// Border color for cards and containers (default: #e5e7eb - light gray)
    /// </summary>
    public string BorderColor { get; set; } = "#e5e7eb";

    /// <summary>
    /// Application logo URL (optional)
    /// If not set, fallback to text-based branding
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Application name for text-based branding (default: LankaConnect)
    /// </summary>
    public string ApplicationName { get; set; } = "LankaConnect";

    /// <summary>
    /// Footer text for emails (copyright, legal, etc.)
    /// </summary>
    public string FooterText { get; set; } = "© 2024 LankaConnect. All rights reserved.";

    /// <summary>
    /// Support email address displayed in footer
    /// </summary>
    public string SupportEmail { get; set; } = "support@lankaconnect.com";

    /// <summary>
    /// Gets button CSS style string with primary color.
    /// Example: background-color: #fb923c; color: white; padding: 12px 24px;
    /// </summary>
    public string GetButtonStyle()
    {
        return $"background-color: {PrimaryColor}; color: white; padding: 12px 24px; " +
               $"border-radius: 6px; text-decoration: none; display: inline-block; " +
               $"font-weight: 600; border: none;";
    }

    /// <summary>
    /// Gets link CSS style string with secondary color.
    /// Example: color: #f43f5e; text-decoration: underline;
    /// </summary>
    public string GetLinkStyle()
    {
        return $"color: {SecondaryColor}; text-decoration: underline;";
    }

    /// <summary>
    /// Gets header CSS style string with gradient background.
    /// Example: background: linear-gradient(135deg, #fb923c 0%, #f43f5e 100%);
    /// </summary>
    public string GetHeaderStyle()
    {
        return $"background: linear-gradient(135deg, {PrimaryColor} 0%, {SecondaryColor} 100%); " +
               $"color: white; padding: 24px; text-align: center;";
    }

    /// <summary>
    /// Validates all required branding configuration.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(PrimaryColor))
        {
            throw new InvalidOperationException(
                $"{SectionName}:PrimaryColor is not configured. " +
                "Please set a valid hex color code (e.g., #fb923c).");
        }

        if (string.IsNullOrWhiteSpace(SecondaryColor))
        {
            throw new InvalidOperationException(
                $"{SectionName}:SecondaryColor is not configured. " +
                "Please set a valid hex color code (e.g., #f43f5e).");
        }

        if (string.IsNullOrWhiteSpace(ApplicationName))
        {
            throw new InvalidOperationException(
                $"{SectionName}:ApplicationName is not configured. " +
                "Please set the application name for email branding.");
        }

        // Validate hex color format (basic check)
        if (!IsValidHexColor(PrimaryColor) || !IsValidHexColor(SecondaryColor))
        {
            throw new InvalidOperationException(
                "Color values must be valid hex color codes (e.g., #fb923c or #f43f5e).");
        }
    }

    private static bool IsValidHexColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color)) return false;
        if (!color.StartsWith("#")) return false;
        if (color.Length != 7 && color.Length != 4) return false; // #RGB or #RRGGBB

        return color[1..].All(c =>
            (c >= '0' && c <= '9') ||
            (c >= 'a' && c <= 'f') ||
            (c >= 'A' && c <= 'F'));
    }
}
