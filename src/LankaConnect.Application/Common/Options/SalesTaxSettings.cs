namespace LankaConnect.Application.Common.Options;

/// <summary>
/// Phase 6A.95: Configuration settings for sales tax collection feature.
/// When disabled, all revenue calculations treat tax rate as 0%.
/// This allows the platform to operate without collecting sales tax when needed.
/// </summary>
public class SalesTaxSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "SalesTax";

    /// <summary>
    /// Master toggle for sales tax collection.
    /// When false, all tax calculations return 0 regardless of state/location.
    /// Default: false (disabled) - tax collection must be explicitly enabled.
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// Rate to use when sales tax is disabled. Should always be 0.
    /// Exists for explicit configuration, logging, and audit purposes.
    /// </summary>
    public decimal DefaultRateWhenDisabled { get; init; } = 0m;

    /// <summary>
    /// Optional: Future support for graduated rollout by state.
    /// When specified and feature is enabled, only these states will have tax applied.
    /// If null or empty, all US states will have tax applied when feature is enabled.
    /// </summary>
    public List<string>? EnabledStates { get; init; }

    /// <summary>
    /// Validates the sales tax settings.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when settings are invalid.</exception>
    public void Validate()
    {
        if (DefaultRateWhenDisabled != 0m)
        {
            throw new InvalidOperationException(
                $"DefaultRateWhenDisabled must be 0 when sales tax feature is configured (got {DefaultRateWhenDisabled})");
        }

        if (EnabledStates != null && EnabledStates.Any(s => string.IsNullOrWhiteSpace(s)))
        {
            throw new InvalidOperationException(
                "EnabledStates cannot contain empty or whitespace-only values");
        }

        // Validate state codes if provided
        if (EnabledStates != null)
        {
            var invalidStates = EnabledStates.Where(s => s.Trim().Length != 2).ToList();
            if (invalidStates.Any())
            {
                throw new InvalidOperationException(
                    $"EnabledStates must contain valid 2-letter state codes. Invalid: {string.Join(", ", invalidStates)}");
            }
        }
    }

    /// <summary>
    /// Checks if sales tax should be applied for a specific state.
    /// </summary>
    /// <param name="stateCode">Two-letter US state code (e.g., "CA", "NY")</param>
    /// <returns>True if tax should be applied, false otherwise</returns>
    public bool IsTaxEnabledForState(string? stateCode)
    {
        // If feature is globally disabled, never apply tax
        if (!Enabled)
            return false;

        // If no state-specific overrides, apply tax to all states
        if (EnabledStates == null || !EnabledStates.Any())
            return true;

        // Check if this specific state is in the enabled list
        if (string.IsNullOrWhiteSpace(stateCode))
            return false;

        var normalizedCode = stateCode.Trim().ToUpperInvariant();
        return EnabledStates.Any(s =>
            s.Trim().Equals(normalizedCode, StringComparison.OrdinalIgnoreCase));
    }
}
