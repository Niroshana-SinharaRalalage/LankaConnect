using FluentAssertions;
using LankaConnect.Shared.Email.Configuration;

namespace LankaConnect.Shared.Tests.Email.Configuration;

/// <summary>
/// Phase 6A.86: Tests for EmailFeatureFlags configuration (TDD - RED phase)
/// Ensures feature flag system works correctly for gradual hybrid system rollout
/// </summary>
public class EmailFeatureFlagsTests
{
    [Fact]
    public void EmailFeatureFlags_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var flags = new EmailFeatureFlags();

        // Assert
        flags.UseTypedParameters.Should().BeFalse("Hybrid system disabled by default for safety");
        flags.EnableLogging.Should().BeTrue("Logging should be enabled by default for observability");
        flags.EnableValidation.Should().BeTrue("Validation should be enabled by default for data quality");
    }

    [Fact]
    public void EmailFeatureFlags_ShouldAllowGlobalEnable()
    {
        // Arrange
        var flags = new EmailFeatureFlags
        {
            UseTypedParameters = true
        };

        // Act & Assert
        flags.UseTypedParameters.Should().BeTrue();
    }

    [Fact]
    public void EmailFeatureFlags_ShouldAllowGlobalDisable()
    {
        // Arrange
        var flags = new EmailFeatureFlags
        {
            UseTypedParameters = false
        };

        // Act & Assert
        flags.UseTypedParameters.Should().BeFalse();
    }

    [Fact]
    public void EmailFeatureFlags_ShouldSupportPerHandlerOverrides()
    {
        // Arrange
        var flags = new EmailFeatureFlags
        {
            UseTypedParameters = false, // Global: OFF
            HandlerOverrides = new Dictionary<string, bool>
            {
                { "EventReminderJob", true } // Pilot handler: ON
            }
        };

        // Act
        var isEventReminderEnabled = flags.IsEnabledForHandler("EventReminderJob");
        var isOtherHandlerEnabled = flags.IsEnabledForHandler("PaymentCompletedEventHandler");

        // Assert
        isEventReminderEnabled.Should().BeTrue("EventReminderJob has override enabled");
        isOtherHandlerEnabled.Should().BeFalse("Other handlers use global setting (disabled)");
    }

    [Fact]
    public void EmailFeatureFlags_ShouldUseGlobalSettingWhenNoOverride()
    {
        // Arrange
        var flags = new EmailFeatureFlags
        {
            UseTypedParameters = true, // Global: ON
            HandlerOverrides = new Dictionary<string, bool>
            {
                { "EventReminderJob", false } // Only EventReminderJob: OFF
            }
        };

        // Act
        var isEventReminderEnabled = flags.IsEnabledForHandler("EventReminderJob");
        var isOtherHandlerEnabled = flags.IsEnabledForHandler("PaymentCompletedEventHandler");

        // Assert
        isEventReminderEnabled.Should().BeFalse("EventReminderJob has override disabled");
        isOtherHandlerEnabled.Should().BeTrue("Other handlers use global setting (enabled)");
    }

    [Fact]
    public void IsEnabledForHandler_ShouldReturnGlobalSettingWhenHandlerNotInOverrides()
    {
        // Arrange
        var flags = new EmailFeatureFlags
        {
            UseTypedParameters = true,
            HandlerOverrides = new Dictionary<string, bool>()
        };

        // Act
        var isEnabled = flags.IsEnabledForHandler("AnyHandler");

        // Assert
        isEnabled.Should().BeTrue("Should use global setting when no override exists");
    }

    [Fact]
    public void IsEnabledForHandler_ShouldBeCaseInsensitive()
    {
        // Arrange
        var flags = new EmailFeatureFlags
        {
            UseTypedParameters = false,
            HandlerOverrides = new Dictionary<string, bool>
            {
                { "EventReminderJob", true }
            }
        };

        // Act
        var isEnabled1 = flags.IsEnabledForHandler("EventReminderJob");
        var isEnabled2 = flags.IsEnabledForHandler("eventreminderjob"); // Different casing
        var isEnabled3 = flags.IsEnabledForHandler("EVENTREMINDERJOB"); // All caps

        // Assert
        isEnabled1.Should().BeTrue();
        isEnabled2.Should().BeTrue("Should be case-insensitive");
        isEnabled3.Should().BeTrue("Should be case-insensitive");
    }

    [Fact]
    public void EmailFeatureFlags_ShouldAllowLoggingToggle()
    {
        // Arrange
        var flags = new EmailFeatureFlags
        {
            EnableLogging = false
        };

        // Act & Assert
        flags.EnableLogging.Should().BeFalse();
    }

    [Fact]
    public void EmailFeatureFlags_ShouldAllowValidationToggle()
    {
        // Arrange
        var flags = new EmailFeatureFlags
        {
            EnableValidation = false
        };

        // Act & Assert
        flags.EnableValidation.Should().BeFalse();
    }

    [Fact]
    public void GetEnabledHandlers_ShouldReturnOnlyHandlersWithOverrideTrue()
    {
        // Arrange
        var flags = new EmailFeatureFlags
        {
            UseTypedParameters = false,
            HandlerOverrides = new Dictionary<string, bool>
            {
                { "EventReminderJob", true },
                { "PaymentCompletedEventHandler", true },
                { "EventCancellationEmailJob", false }
            }
        };

        // Act
        var enabledHandlers = flags.GetEnabledHandlers();

        // Assert
        enabledHandlers.Should().HaveCount(2);
        enabledHandlers.Should().Contain("EventReminderJob");
        enabledHandlers.Should().Contain("PaymentCompletedEventHandler");
        enabledHandlers.Should().NotContain("EventCancellationEmailJob");
    }

    [Fact]
    public void GetEnabledHandlers_ShouldReturnEmptyWhenGlobalEnabledButNoOverrides()
    {
        // Arrange
        var flags = new EmailFeatureFlags
        {
            UseTypedParameters = true, // Global ON
            HandlerOverrides = new Dictionary<string, bool>() // No overrides
        };

        // Act
        var enabledHandlers = flags.GetEnabledHandlers();

        // Assert
        enabledHandlers.Should().BeEmpty("No explicit handler overrides, global applies to ALL handlers");
    }

    [Fact]
    public void GetDisabledHandlers_ShouldReturnOnlyHandlersWithOverrideFalse()
    {
        // Arrange
        var flags = new EmailFeatureFlags
        {
            UseTypedParameters = true,
            HandlerOverrides = new Dictionary<string, bool>
            {
                { "EventReminderJob", true },
                { "PaymentCompletedEventHandler", false },
                { "EventCancellationEmailJob", false }
            }
        };

        // Act
        var disabledHandlers = flags.GetDisabledHandlers();

        // Assert
        disabledHandlers.Should().HaveCount(2);
        disabledHandlers.Should().Contain("PaymentCompletedEventHandler");
        disabledHandlers.Should().Contain("EventCancellationEmailJob");
        disabledHandlers.Should().NotContain("EventReminderJob");
    }

    [Fact]
    public void HandlerOverrides_ShouldInitializeAsEmptyDictionary()
    {
        // Arrange & Act
        var flags = new EmailFeatureFlags();

        // Assert
        flags.HandlerOverrides.Should().NotBeNull();
        flags.HandlerOverrides.Should().BeEmpty();
    }

    [Fact]
    public void EmailFeatureFlags_ShouldSupportStagedRolloutScenario()
    {
        // Arrange: Staged rollout plan
        // Week 2: Pilot with EventReminderJob only
        var week2Flags = new EmailFeatureFlags
        {
            UseTypedParameters = false, // Global OFF
            HandlerOverrides = new Dictionary<string, bool>
            {
                { "EventReminderJob", true } // Pilot handler
            }
        };

        // Week 3-4: Add 4 more HIGH priority handlers
        var week3Flags = new EmailFeatureFlags
        {
            UseTypedParameters = false, // Global still OFF
            HandlerOverrides = new Dictionary<string, bool>
            {
                { "EventReminderJob", true },
                { "PaymentCompletedEventHandler", true },
                { "EventCancellationEmailJob", true },
                { "EventPublishedEventHandler", true },
                { "EventNotificationEmailJob", true }
            }
        };

        // Week 7: Production rollout - global ON
        var week7Flags = new EmailFeatureFlags
        {
            UseTypedParameters = true, // Global ON for all handlers
            HandlerOverrides = new Dictionary<string, bool>() // No overrides needed
        };

        // Act & Assert
        // Week 2: Only EventReminderJob uses typed parameters
        week2Flags.IsEnabledForHandler("EventReminderJob").Should().BeTrue();
        week2Flags.IsEnabledForHandler("PaymentCompletedEventHandler").Should().BeFalse();

        // Week 3-4: 5 handlers use typed parameters
        week3Flags.GetEnabledHandlers().Should().HaveCount(5);

        // Week 7: ALL handlers use typed parameters
        week7Flags.IsEnabledForHandler("EventReminderJob").Should().BeTrue();
        week7Flags.IsEnabledForHandler("PaymentCompletedEventHandler").Should().BeTrue();
        week7Flags.IsEnabledForHandler("AnyOtherHandler").Should().BeTrue();
    }

    [Fact]
    public void EmailFeatureFlags_ShouldSupportEmergencyRollbackScenario()
    {
        // Arrange: Emergency rollback plan
        // Production issue detected - disable globally but keep pilot handler working
        var rollbackFlags = new EmailFeatureFlags
        {
            UseTypedParameters = false, // Emergency: Global OFF
            HandlerOverrides = new Dictionary<string, bool>
            {
                { "EventReminderJob", true } // Keep pilot handler working (proven stable)
            }
        };

        // Act & Assert
        rollbackFlags.IsEnabledForHandler("EventReminderJob").Should().BeTrue("Pilot handler continues working");
        rollbackFlags.IsEnabledForHandler("PaymentCompletedEventHandler").Should().BeFalse("All others rolled back");
        rollbackFlags.IsEnabledForHandler("EventCancellationEmailJob").Should().BeFalse("All others rolled back");
    }
}
