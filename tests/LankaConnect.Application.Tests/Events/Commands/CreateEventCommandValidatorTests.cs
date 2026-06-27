using FluentValidation.TestHelper;
using LankaConnect.Application.Events.Commands.CreateEvent;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Phase 8X.4a — validator coverage for <see cref="CreateEventCommand"/>.
/// Locks the architect-locked inference table + ExternalPaid-only rules in tests so
/// future refactors of the validator can't silently weaken the security defaults.
/// </summary>
public class CreateEventCommandValidatorTests
{
    private readonly CreateEventCommandValidator _validator = new();

    private static CreateEventCommand BaseCommand(
        bool? isFree = null,
        EventPaymentMode? paymentMode = null,
        string? externalUrl = null,
        string? externalInstructions = null,
        string? externalVendor = null,
        RegistrationMode? registrationMode = null) =>
        new(
            Title: "Validator test event",
            Description: "Phase 8X.4a coverage",
            StartDate: DateTime.UtcNow.AddDays(7),
            EndDate: DateTime.UtcNow.AddDays(8),
            OrganizerId: Guid.NewGuid(),
            Capacity: 100,
            IsFree: isFree,
            PaymentMode: paymentMode,
            ExternalRegistrationUrl: externalUrl,
            ExternalRegistrationInstructions: externalInstructions,
            ExternalRegistrationVendorName: externalVendor,
            RegistrationMode: registrationMode);

    // ─────────────────────────────────────────────────────────────────────────
    //  Inference table (architect-locked)
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(true, null)]                                  // true + null → Free
    [InlineData(true, EventPaymentMode.Free)]                 // true + Free → Free
    [InlineData(false, null)]                                 // false + null → OnPlatformPaid
    [InlineData(false, EventPaymentMode.OnPlatformPaid)]
    [InlineData(null, null)]                                  // null + null → OnPlatformPaid (security)
    [InlineData(null, EventPaymentMode.OnPlatformPaid)]
    public void Inference_ValidCombinations_DoNotFail(bool? isFree, EventPaymentMode? mode)
    {
        var cmd = BaseCommand(isFree: isFree, paymentMode: mode);
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(c => c.PaymentMode);
    }

    [Theory]
    [InlineData(true, EventPaymentMode.OnPlatformPaid)]
    [InlineData(true, EventPaymentMode.ExternalPaid)]
    [InlineData(false, EventPaymentMode.Free)]
    public void Inference_InconsistentCombinations_Fail(bool? isFree, EventPaymentMode? mode)
    {
        var cmd = BaseCommand(isFree: isFree, paymentMode: mode);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.PaymentMode);
    }

    [Fact]
    public void IsFreeNull_PaymentModeNull_InfersToOnPlatformPaid_SecurityDefault()
    {
        var cmd = BaseCommand(isFree: null, paymentMode: null);
        var (mode, _, _) = CreateEventCommandValidator.InferPaymentMode(cmd.IsFree, cmd.PaymentMode);
        Assert.Equal(EventPaymentMode.OnPlatformPaid, mode); // NOT Free — Phase 6A.81 security default
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ExternalPaid-only rules
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ExternalPaid_HappyPath_NoErrors()
    {
        var cmd = BaseCommand(
            isFree: false,
            paymentMode: EventPaymentMode.ExternalPaid,
            externalUrl: "https://eventbrite.com/e/test-12345");

        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ExternalPaid_MissingUrl_AndAllOtherFieldsEmpty_Succeeds_StoresNullVo()
    {
        // Phase 8X.11 — URL is optional. All-three-empty also passes (architect-approved
        // per product owner Q2 = B). Backend handler stores ExternalRegistration = null
        // and the public detail page shows "Contact organiser for registration details".
        var cmd = BaseCommand(
            isFree: false,
            paymentMode: EventPaymentMode.ExternalPaid,
            externalUrl: null);

        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(c => c.ExternalRegistrationUrl);
    }

    [Fact]
    public void ExternalPaid_MissingUrl_WithInstructions_Succeeds()
    {
        // Phase 8X.11 — cash-at-door / bank-deposit / phone-only patterns.
        var cmd = BaseCommand(
            isFree: false,
            paymentMode: EventPaymentMode.ExternalPaid,
            externalUrl: null,
            externalInstructions: "Pay $25 cash at door, bring this email");

        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(c => c.ExternalRegistrationUrl);
    }

    [Fact]
    public void ExternalPaid_HttpUrl_Fails()
    {
        var cmd = BaseCommand(
            isFree: false,
            paymentMode: EventPaymentMode.ExternalPaid,
            externalUrl: "http://eventbrite.com/e/test");

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(c => c.ExternalRegistrationUrl);
    }

    [Theory]
    [InlineData("https://localhost/event")]
    [InlineData("https://127.0.0.1/event")]
    [InlineData("https://10.0.0.1/event")]
    [InlineData("https://192.168.1.1/event")]
    [InlineData("https://169.254.0.1/event")]
    public void ExternalPaid_LoopbackOrPrivateUrl_Fails(string url)
    {
        var cmd = BaseCommand(
            isFree: false,
            paymentMode: EventPaymentMode.ExternalPaid,
            externalUrl: url);

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(c => c.ExternalRegistrationUrl);
    }

    [Fact]
    public void ExternalPaid_RegMode_DetailedAttendees_Fails()
    {
        var cmd = BaseCommand(
            isFree: false,
            paymentMode: EventPaymentMode.ExternalPaid,
            externalUrl: "https://eventbrite.com/e/test",
            registrationMode: RegistrationMode.DetailedAttendees);

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(c => c.RegistrationMode);
    }

    [Fact]
    public void ExternalPaid_RegMode_External_Succeeds()
    {
        // Phase 8X.11 — RegistrationMode for ExternalPaid events is now `External` (was
        // `NoRegistration`). The validator accepts null + External; everything else fails.
        var cmd = BaseCommand(
            isFree: false,
            paymentMode: EventPaymentMode.ExternalPaid,
            externalUrl: "https://eventbrite.com/e/test",
            registrationMode: RegistrationMode.External);

        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(c => c.RegistrationMode);
    }

    [Fact]
    public void ExternalPaid_RegMode_NoRegistration_Fails()
    {
        // Phase 8X.11 — NoRegistration is no longer the valid mode for ExternalPaid;
        // External is. This test pins the strict 400 (architect Q1 + product owner Q1).
        var cmd = BaseCommand(
            isFree: false,
            paymentMode: EventPaymentMode.ExternalPaid,
            externalUrl: "https://eventbrite.com/e/test",
            registrationMode: RegistrationMode.NoRegistration);

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(c => c.RegistrationMode);
    }

    [Fact]
    public void ExternalPaid_RegMode_Null_Succeeds_HandlerWillCoerce()
    {
        var cmd = BaseCommand(
            isFree: false,
            paymentMode: EventPaymentMode.ExternalPaid,
            externalUrl: "https://eventbrite.com/e/test",
            registrationMode: null);

        var result = _validator.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(c => c.RegistrationMode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Length caps
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ExternalPaid_InstructionsTooLong_Fails()
    {
        var cmd = BaseCommand(
            isFree: false,
            paymentMode: EventPaymentMode.ExternalPaid,
            externalUrl: "https://eventbrite.com/e/test",
            externalInstructions: new string('x', 4001));

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(c => c.ExternalRegistrationInstructions);
    }

    [Fact]
    public void ExternalPaid_VendorTooLong_Fails()
    {
        var cmd = BaseCommand(
            isFree: false,
            paymentMode: EventPaymentMode.ExternalPaid,
            externalUrl: "https://eventbrite.com/e/test",
            externalVendor: new string('y', 101));

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(c => c.ExternalRegistrationVendorName);
    }
}
