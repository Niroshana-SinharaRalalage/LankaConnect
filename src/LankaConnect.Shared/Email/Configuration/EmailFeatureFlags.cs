namespace LankaConnect.Shared.Email.Configuration;

/// <summary>
/// Phase 6A.86: Feature flags for controlling hybrid email system rollout.
///
/// Purpose:
/// - Enable gradual migration from Dictionary-based to typed parameter system
/// - Support staged rollout (pilot → high priority → all handlers)
/// - Provide instant rollback capability (&lt;30 seconds via config update)
/// - Allow per-handler override for fine-grained control
///
/// Rollout Strategy:
/// Week 1: Foundation setup (UseTypedParameters = false globally)
/// Week 2: Pilot with EventReminderJob (override = true for that handler only)
/// Weeks 3-4: Add 4 more HIGH priority handlers (5 total enabled)
/// Weeks 5-6: Enable all remaining handlers (UseTypedParameters = true globally)
/// Week 7: Production rollout complete
/// Week 8: Remove legacy Dictionary-based code
///
/// Emergency Rollback:
/// 1. Set UseTypedParameters = false in appsettings.json
/// 2. Restart application (or wait for config reload)
/// 3. All handlers revert to Dictionary-based parameters
/// 4. &lt;30 seconds total downtime
/// </summary>
public class EmailFeatureFlags
{
    /// <summary>
    /// Global feature flag for typed email parameters.
    /// Default: false (hybrid system disabled until explicitly enabled)
    /// </summary>
    public bool UseTypedParameters { get; set; } = false;

    /// <summary>
    /// Enable comprehensive logging for email operations.
    /// Includes: correlation IDs, parameter validation results, template rendering times
    /// Default: true (logging always enabled for observability)
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Enable parameter validation before sending emails.
    /// When disabled, emails sent even with validation failures (logged as warnings)
    /// Default: true (validation enabled for data quality)
    /// </summary>
    public bool EnableValidation { get; set; } = true;

    /// <summary>
    /// Per-handler feature flag overrides.
    /// Allows enabling/disabling typed parameters for specific handlers while global setting differs.
    ///
    /// Example staged rollout:
    /// {
    ///   "EventReminderJob": true,               // Pilot handler - Week 2
    ///   "PaymentCompletedEventHandler": true,   // HIGH priority - Week 3
    ///   "EventCancellationEmailJob": true,      // HIGH priority - Week 3
    ///   "EventPublishedEventHandler": true,     // HIGH priority - Week 4
    ///   "EventNotificationEmailJob": true       // HIGH priority - Week 4
    /// }
    ///
    /// Key: Handler class name (case-insensitive)
    /// Value: true = use typed parameters, false = use Dictionary
    /// </summary>
    public Dictionary<string, bool> HandlerOverrides { get; set; } = new();

    /// <summary>
    /// Determines if typed parameters should be used for a specific handler.
    ///
    /// Logic:
    /// 1. Check HandlerOverrides dictionary first (case-insensitive)
    /// 2. If override exists, use override value
    /// 3. Otherwise, use global UseTypedParameters setting
    /// </summary>
    /// <param name="handlerName">Handler class name (e.g., "EventReminderJob")</param>
    /// <returns>True if handler should use typed parameters, false for Dictionary</returns>
    public bool IsEnabledForHandler(string handlerName)
    {
        // Check for case-insensitive override
        var overrideKey = HandlerOverrides.Keys
            .FirstOrDefault(k => k.Equals(handlerName, StringComparison.OrdinalIgnoreCase));

        if (overrideKey != null)
        {
            return HandlerOverrides[overrideKey];
        }

        // No override found, use global setting
        return UseTypedParameters;
    }

    /// <summary>
    /// Gets list of handlers explicitly enabled via overrides.
    /// Used for monitoring staged rollout progress.
    /// </summary>
    /// <returns>Handler names with override = true</returns>
    public IReadOnlyList<string> GetEnabledHandlers()
    {
        return HandlerOverrides
            .Where(kvp => kvp.Value == true)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    /// <summary>
    /// Gets list of handlers explicitly disabled via overrides.
    /// Used for monitoring rollback scenarios.
    /// </summary>
    /// <returns>Handler names with override = false</returns>
    public IReadOnlyList<string> GetDisabledHandlers()
    {
        return HandlerOverrides
            .Where(kvp => kvp.Value == false)
            .Select(kvp => kvp.Key)
            .ToList();
    }
}
