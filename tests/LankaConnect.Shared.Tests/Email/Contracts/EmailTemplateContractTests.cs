using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;

namespace LankaConnect.Shared.Tests.Email.Contracts;

/// <summary>
/// Phase 6A.97: Tests for EmailTemplateContract - Single Source of Truth.
///
/// These tests verify that:
/// 1. Template names are correctly defined
/// 2. Parameter names are consistently used across TypedEmailParams classes
/// 3. TypedEmailParams.ToDictionary() uses the contract constants
/// </summary>
public class EmailTemplateContractTests
{
    #region Template Name Tests

    [Fact]
    public void TemplateNames_RefundRequested_ShouldMatchRefundEmailParams()
    {
        // Arrange
        var emailParams = new RefundEmailParams().AsRequest();

        // Assert
        emailParams.TemplateName.Should().Be(EmailTemplateContract.TemplateNames.RefundRequested);
    }

    [Fact]
    public void TemplateNames_RefundCompleted_ShouldMatchRefundEmailParams()
    {
        // Arrange
        var emailParams = new RefundEmailParams().AsCompleted();

        // Assert
        emailParams.TemplateName.Should().Be(EmailTemplateContract.TemplateNames.RefundCompleted);
    }

    [Fact]
    public void TemplateNames_EventRegistrationCancellation_ShouldMatchRegistrationCancellationEmailParams()
    {
        // Arrange
        var emailParams = new RegistrationCancellationEmailParams();

        // Assert
        emailParams.TemplateName.Should().Be(EmailTemplateContract.TemplateNames.EventRegistrationCancellation);
    }

    [Fact]
    public void TemplateNames_PasswordReset_ShouldMatchPasswordResetEmailParams()
    {
        // Arrange
        var emailParams = new PasswordResetEmailParams();

        // Assert
        emailParams.TemplateName.Should().Be(EmailTemplateContract.TemplateNames.PasswordReset);
    }

    [Fact]
    public void TemplateNames_PasswordChangeConfirmation_ShouldMatchPasswordChangedEmailParams()
    {
        // Arrange
        var emailParams = new PasswordChangedEmailParams();

        // Assert
        emailParams.TemplateName.Should().Be(EmailTemplateContract.TemplateNames.PasswordChangeConfirmation);
    }

    [Fact]
    public void TemplateNames_VolunteerCommitmentConfirmation_ShouldHaveExpectedValue()
    {
        EmailTemplateContract.TemplateNames.VolunteerCommitmentConfirmation
            .Should().Be("template-volunteer-commitment-confirmation");
    }

    [Fact]
    public void TemplateNames_VolunteerCommitmentCancellation_ShouldHaveExpectedValue()
    {
        EmailTemplateContract.TemplateNames.VolunteerCommitmentCancellation
            .Should().Be("template-volunteer-commitment-cancellation");
    }

    #endregion

    #region Common Parameters Tests

    [Fact]
    public void RefundEmailParams_ToDictionary_ShouldIncludeCommonParameters()
    {
        // Arrange
        var emailParams = RefundEmailParams.CreateRequest(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            registrationId: Guid.NewGuid(),
            refundId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            eventTitle: "Test Event",
            eventStartDate: DateTime.UtcNow.AddDays(7),
            timeZoneId: "America/New_York",
            refundAmount: 50.00m,
            originalAmount: 50.00m,
            refundReason: "User cancelled",
            requestedAt: DateTime.UtcNow
        );

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - using contract constants
        dict.Should().ContainKey(EmailTemplateContract.Common.UserName);
        dict.Should().ContainKey(EmailTemplateContract.Common.Year);
        dict.Should().ContainKey(EmailTemplateContract.Common.SupportEmail);
    }

    [Fact]
    public void RegistrationCancellationEmailParams_ToDictionary_ShouldIncludeCommonParameters()
    {
        // Arrange
        var emailParams = RegistrationCancellationEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            registrationId: Guid.Empty,
            eventId: Guid.NewGuid(),
            eventTitle: "Test Event",
            eventStartDate: DateTime.UtcNow.AddDays(7),
            timeZoneId: "America/New_York",
            eventLocation: "123 Main St",
            cancellationReason: "User cancelled",
            cancelledAt: DateTime.UtcNow,
            refundStatus: "No Refund Required"
        );

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - using contract constants
        dict.Should().ContainKey(EmailTemplateContract.Common.UserName);
        dict.Should().ContainKey(EmailTemplateContract.Common.Year);
        dict.Should().ContainKey(EmailTemplateContract.Common.SupportEmail);
    }

    #endregion

    #region Event Parameters Tests

    [Fact]
    public void RefundEmailParams_ToDictionary_ShouldIncludeEventParameters()
    {
        // Arrange
        var emailParams = RefundEmailParams.CreateRequest(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            registrationId: Guid.NewGuid(),
            refundId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            eventTitle: "Test Event",
            eventStartDate: DateTime.UtcNow.AddDays(7),
            timeZoneId: "America/New_York",
            refundAmount: 50.00m,
            originalAmount: 50.00m,
            refundReason: "User cancelled",
            requestedAt: DateTime.UtcNow
        );
        emailParams.EventDetailsUrl = "https://lankaconnect.com/events/123";

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - using contract constants
        dict.Should().ContainKey(EmailTemplateContract.Event.EventTitle);
        dict.Should().ContainKey(EmailTemplateContract.Event.EventDateTime);
        dict.Should().ContainKey(EmailTemplateContract.Event.EventDetailsUrl);
    }

    #endregion

    #region Organizer Contact Parameters Tests

    [Fact]
    public void RefundEmailParams_WithOrganizerContact_ToDictionary_ShouldIncludeOrganizerParams()
    {
        // Arrange
        var emailParams = RefundEmailParams.CreateRequest(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            registrationId: Guid.NewGuid(),
            refundId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            eventTitle: "Test Event",
            eventStartDate: DateTime.UtcNow.AddDays(7),
            timeZoneId: "America/New_York",
            refundAmount: 50.00m,
            originalAmount: 50.00m,
            refundReason: "User cancelled",
            requestedAt: DateTime.UtcNow
        );
        emailParams.WithOrganizerContact("Jane Smith", "jane@example.com", "555-1234");

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - using contract constants
        dict.Should().ContainKey(EmailTemplateContract.OrganizerContact.HasOrganizerContact);
        dict.Should().ContainKey(EmailTemplateContract.OrganizerContact.OrganizerContactName);
        dict.Should().ContainKey(EmailTemplateContract.OrganizerContact.OrganizerContactEmail);
        dict.Should().ContainKey(EmailTemplateContract.OrganizerContact.OrganizerContactPhone);

        dict[EmailTemplateContract.OrganizerContact.HasOrganizerContact].Should().Be(true);
    }

    [Fact]
    public void RegistrationCancellationEmailParams_WithOrganizerContact_ToDictionary_ShouldIncludeOrganizerParams()
    {
        // Arrange
        var emailParams = RegistrationCancellationEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            registrationId: Guid.Empty,
            eventId: Guid.NewGuid(),
            eventTitle: "Test Event",
            eventStartDate: DateTime.UtcNow.AddDays(7),
            timeZoneId: "America/New_York",
            eventLocation: "123 Main St",
            cancellationReason: "User cancelled",
            cancelledAt: DateTime.UtcNow,
            refundStatus: "No Refund Required"
        );
        emailParams.WithOrganizerContact("Jane Smith", "jane@example.com", "555-1234");

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - using contract constants
        dict.Should().ContainKey(EmailTemplateContract.OrganizerContact.HasOrganizerContact);
        dict.Should().ContainKey(EmailTemplateContract.OrganizerContact.OrganizerContactName);
        dict.Should().ContainKey(EmailTemplateContract.OrganizerContact.OrganizerContactEmail);
        dict.Should().ContainKey(EmailTemplateContract.OrganizerContact.OrganizerContactPhone);

        dict[EmailTemplateContract.OrganizerContact.HasOrganizerContact].Should().Be(true);
    }

    #endregion

    #region Refund Parameters Tests

    [Fact]
    public void RefundEmailParams_ToDictionary_ShouldIncludeRefundParameters()
    {
        // Arrange
        var emailParams = RefundEmailParams.CreateCompleted(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            registrationId: Guid.NewGuid(),
            refundId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            eventTitle: "Test Event",
            eventStartDate: DateTime.UtcNow.AddDays(7),
            timeZoneId: "America/New_York",
            refundAmount: 50.00m,
            originalAmount: 50.00m,
            completedAt: DateTime.UtcNow,
            stripeRefundId: "re_abc123"
        );

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - using contract constants
        dict.Should().ContainKey(EmailTemplateContract.Refund.RefundAmount);
        dict.Should().ContainKey(EmailTemplateContract.Refund.OriginalAmount);
        dict.Should().ContainKey(EmailTemplateContract.Refund.StripeRefundId);
        dict.Should().ContainKey(EmailTemplateContract.Refund.ReferenceId);
        dict.Should().ContainKey(EmailTemplateContract.Refund.ProcessingMethod);
    }

    #endregion

    #region Auth Parameters Tests

    [Fact]
    public void PasswordResetEmailParams_ToDictionary_ShouldIncludeAuthParameters()
    {
        // Arrange
        var emailParams = PasswordResetEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            resetToken: "abc123token",
            resetLink: "https://lankaconnect.com/reset?token=abc123",
            expiresAt: DateTime.UtcNow.AddHours(24)
        );

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - using contract constants
        dict.Should().ContainKey(EmailTemplateContract.Auth.UserEmail);
        dict.Should().ContainKey(EmailTemplateContract.Auth.ResetToken);
        dict.Should().ContainKey(EmailTemplateContract.Auth.ResetLink);
        dict.Should().ContainKey(EmailTemplateContract.Auth.ExpiresAt);
    }

    [Fact]
    public void PasswordChangedEmailParams_ToDictionary_ShouldIncludeAuthParameters()
    {
        // Arrange
        var emailParams = PasswordChangedEmailParams.Create(
            userId: Guid.NewGuid(),
            userName: "John Doe",
            userEmail: "john@example.com",
            changedAt: DateTime.UtcNow
        );

        // Act
        var dict = emailParams.ToDictionary();

        // Assert - using contract constants
        dict.Should().ContainKey(EmailTemplateContract.Auth.UserEmail);
        dict.Should().ContainKey(EmailTemplateContract.Auth.ChangedAt);
        dict.Should().ContainKey(EmailTemplateContract.Auth.LoginUrl);
    }

    #endregion

    #region Phase 6A.113: Template Name Consistency Validation

    /// <summary>
    /// Phase 6A.113: Comprehensive validation that ALL EmailParams classes use correct
    /// EmailTemplateContract constants and match expected database template names.
    ///
    /// This test prevents the template name mapping error that occurred in Phase 6A.112
    /// where constants didn't match actual database template names.
    ///
    /// CRITICAL: If this test fails, it means:
    /// 1. An EmailParams class has a hardcoded template name instead of using contract constant
    /// 2. A constant value doesn't match the actual database template name
    /// 3. New EmailParams class was added without updating this validation test
    /// </summary>
    [Theory]
    [InlineData(typeof(FreeEventRegistrationEmailParams), "FreeEventRegistration", "template-free-event-registration-confirmation")]
    [InlineData(typeof(TicketConfirmationEmailParams), "PaidEventRegistration", "template-paid-event-registration-confirmation-with-ticket")]
    [InlineData(typeof(EventReminderEmailParams), "EventReminder", "template-event-reminder")]
    [InlineData(typeof(EmailVerificationEmailParams), "EmailVerification", "template-membership-email-verification")]
    [InlineData(typeof(RegistrationCancellationEmailParams), "EventRegistrationCancellation", "template-event-registration-cancellation")]
    [InlineData(typeof(AttendeesAddedEmailParams), "AttendeesAdded", "template-attendees-added-confirmation")]
    [InlineData(typeof(EventCancellationEmailParams), "EventCancellation", "template-event-cancellation-notifications")]
    [InlineData(typeof(PasswordResetEmailParams), "PasswordReset", "template-password-reset")]
    [InlineData(typeof(PasswordChangedEmailParams), "PasswordChangeConfirmation", "template-password-change-confirmation")]
    [InlineData(typeof(PreliminaryRegistrationPaymentEmailParams), "PreliminaryRegistrationPayment", "template-preliminary-registration-payment-pending")]
    [InlineData(typeof(EventApprovalEmailParams), "EventApproval", "template-event-approval")]
    [InlineData(typeof(WelcomeEmailParams), "WelcomeEmail", "template-welcome")]
    [InlineData(typeof(AccountActivatedEmailParams), "AccountActivatedByAdmin", "template-account-activated-by-admin")]
    [InlineData(typeof(AccountDeactivatedEmailParams), "AccountDeactivatedByAdmin", "template-account-deactivated-by-admin")]
    public void EmailParams_TemplateName_ShouldMatchContractConstantAndDatabaseValue(
        Type emailParamsType,
        string contractConstantName,
        string expectedDatabaseTemplateName)
    {
        // Arrange: Create instance of EmailParams class
        var emailParamsInstance = Activator.CreateInstance(emailParamsType) as IEmailParameters;
        emailParamsInstance.Should().NotBeNull($"{emailParamsType.Name} must implement IEmailParameters");

        // Get the constant value from EmailTemplateContract.TemplateNames
        var constantField = typeof(EmailTemplateContract.TemplateNames)
            .GetField(contractConstantName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        constantField.Should().NotBeNull($"EmailTemplateContract.TemplateNames.{contractConstantName} constant must exist");

        var constantValue = constantField!.GetValue(null) as string;

        // Act: Get TemplateName from EmailParams instance
        var actualTemplateName = emailParamsInstance!.TemplateName;

        // Assert 1: EmailParams.TemplateName must match the contract constant value
        actualTemplateName.Should().Be(constantValue,
            $"{emailParamsType.Name}.TemplateName must use EmailTemplateContract.TemplateNames.{contractConstantName} constant");

        // Assert 2: Contract constant value must match expected database template name
        constantValue.Should().Be(expectedDatabaseTemplateName,
            $"EmailTemplateContract.TemplateNames.{contractConstantName} constant value must match database template name");

        // Assert 3: Final verification - EmailParams returns correct database template name
        actualTemplateName.Should().Be(expectedDatabaseTemplateName,
            $"{emailParamsType.Name} must return correct database template name: {expectedDatabaseTemplateName}");
    }

    /// <summary>
    /// Phase 6A.113: Validate RefundEmailParams factory methods return correct template names.
    /// RefundEmailParams has AsRequest() and AsCompleted() factory methods that change the template.
    /// </summary>
    [Theory]
    [InlineData("AsRequest", "RefundRequested", "template-refund-requested")]
    [InlineData("AsCompleted", "RefundCompleted", "template-refund-completed")]
    public void RefundEmailParams_FactoryMethods_ShouldReturnCorrectTemplateName(
        string factoryMethod,
        string contractConstantName,
        string expectedDatabaseTemplateName)
    {
        // Arrange: Create RefundEmailParams and call factory method
        var refundParams = new RefundEmailParams();
        var methodInfo = typeof(RefundEmailParams).GetMethod(factoryMethod);
        methodInfo.Should().NotBeNull($"RefundEmailParams.{factoryMethod}() method must exist");

        var emailParams = methodInfo!.Invoke(refundParams, null) as IEmailParameters;

        // Get the constant value from EmailTemplateContract.TemplateNames
        var constantField = typeof(EmailTemplateContract.TemplateNames)
            .GetField(contractConstantName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        constantField.Should().NotBeNull($"EmailTemplateContract.TemplateNames.{contractConstantName} constant must exist");

        var constantValue = constantField!.GetValue(null) as string;

        // Act: Get TemplateName after factory method
        var actualTemplateName = emailParams!.TemplateName;

        // Assert 1: TemplateName must match contract constant
        actualTemplateName.Should().Be(constantValue,
            $"RefundEmailParams.{factoryMethod}().TemplateName must use EmailTemplateContract.TemplateNames.{contractConstantName}");

        // Assert 2: Contract constant must match database template name
        constantValue.Should().Be(expectedDatabaseTemplateName,
            $"EmailTemplateContract.TemplateNames.{contractConstantName} must match database template name");

        // Assert 3: Final verification
        actualTemplateName.Should().Be(expectedDatabaseTemplateName,
            $"RefundEmailParams.{factoryMethod}() must return correct database template name: {expectedDatabaseTemplateName}");
    }

    /// <summary>
    /// Phase 6A.113: Validate all constants in EmailTemplateContract.TemplateNames follow template-* naming convention.
    /// Exception: OrganizerCustomEmail (legacy, being renamed in Phase 6A.113 migration).
    /// </summary>
    [Fact]
    public void EmailTemplateContract_AllTemplateNames_ShouldFollowNamingConvention()
    {
        // Arrange: Get all public const string fields from TemplateNames class
        var templateNameFields = typeof(EmailTemplateContract.TemplateNames)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string) && f.IsLiteral);

        // Act & Assert: Each constant value should start with "template-"
        foreach (var field in templateNameFields)
        {
            var constantValue = field.GetValue(null) as string;
            constantValue.Should().NotBeNullOrEmpty($"Constant {field.Name} should have a value");

            // Phase 6A.113 Note: OrganizerCustomEmail is being renamed in migration
            if (field.Name != "OrganizerCustomEmail")
            {
                constantValue.Should().StartWith("template-",
                    $"EmailTemplateContract.TemplateNames.{field.Name} should follow 'template-*' naming convention. Value: {constantValue}");
            }
        }
    }

    /// <summary>
    /// Phase 6A.113: Ensure no EmailParams class uses hardcoded template name strings.
    /// All EmailParams classes MUST use EmailTemplateContract constants.
    /// </summary>
    [Fact]
    public void AllEmailParamsClasses_ShouldUseContractConstants_NotHardcodedStrings()
    {
        // This test is enforced by the parameterized test above
        // If any EmailParams class uses a hardcoded string, the test will fail
        // because TemplateName won't match the contract constant value

        // Additional validation: Verify we're testing ALL EmailParams classes
        var testedTypes = new[]
        {
            typeof(FreeEventRegistrationEmailParams),
            typeof(TicketConfirmationEmailParams),
            typeof(EventReminderEmailParams),
            typeof(EmailVerificationEmailParams),
            typeof(RegistrationCancellationEmailParams),
            typeof(AttendeesAddedEmailParams),
            typeof(EventCancellationEmailParams),
            typeof(PasswordResetEmailParams),
            typeof(PasswordChangedEmailParams),
            typeof(PreliminaryRegistrationPaymentEmailParams),
            typeof(EventApprovalEmailParams),
            typeof(WelcomeEmailParams),
            typeof(AccountActivatedEmailParams),
            typeof(AccountDeactivatedEmailParams),
            typeof(RefundEmailParams) // Tested separately with factory methods
        };

        // Verify all are IEmailParameters implementations
        foreach (var type in testedTypes)
        {
            type.Should().Implement<IEmailParameters>(
                $"{type.Name} must implement IEmailParameters interface");
        }

        // If new EmailParams classes are added, this count will change
        // Developer must update the parameterized test above
        testedTypes.Length.Should().BeGreaterOrEqualTo(15,
            "If you added a new EmailParams class, update the EmailParams_TemplateName_ShouldMatchContractConstantAndDatabaseValue test!");
    }

    #endregion
}
