using FluentValidation.TestHelper;
using LankaConnect.Application.Events.Commands.UpdateEvent;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Phase 8X.4a — validator coverage for <see cref="UpdateEventCommand"/>. Mirrors the
/// CreateEventCommandValidator matrix with EventId-required addition.
/// </summary>
public class UpdateEventCommandValidatorTests
{
    private readonly UpdateEventCommandValidator _validator = new();

    private static UpdateEventCommand BaseCommand(
        Guid? eventId = null,
        bool? isFree = null,
        EventPaymentMode? paymentMode = null,
        string? externalUrl = null,
        RegistrationMode? registrationMode = null) =>
        new(
            EventId: eventId ?? Guid.NewGuid(),
            Title: "Update test",
            Description: "Phase 8X.4a coverage",
            StartDate: DateTime.UtcNow.AddDays(7),
            EndDate: DateTime.UtcNow.AddDays(8),
            Capacity: 100,
            IsFree: isFree,
            PaymentMode: paymentMode,
            ExternalRegistrationUrl: externalUrl,
            RegistrationMode: registrationMode);

    [Fact]
    public void EmptyEventId_Fails()
    {
        var cmd = BaseCommand(eventId: Guid.Empty);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.EventId);
    }

    [Fact]
    public void ExternalPaid_MissingUrl_Succeeds_PerPhase8X11()
    {
        // Phase 8X.11 — URL optional; missing URL no longer rejected at the validator.
        var cmd = BaseCommand(
            isFree: false,
            paymentMode: EventPaymentMode.ExternalPaid,
            externalUrl: null);
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

    [Fact]
    public void ExternalPaid_HappyPath_Succeeds()
    {
        var cmd = BaseCommand(
            isFree: false,
            paymentMode: EventPaymentMode.ExternalPaid,
            externalUrl: "https://eventbrite.com/e/test");
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Inconsistent_IsFreeFalse_PaymentModeFree_Fails()
    {
        var cmd = BaseCommand(isFree: false, paymentMode: EventPaymentMode.Free);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.PaymentMode);
    }

    [Fact]
    public void IdempotentSameExternalPaidPayload_Passes()
    {
        var cmd = BaseCommand(
            isFree: false,
            paymentMode: EventPaymentMode.ExternalPaid,
            externalUrl: "https://eventbrite.com/e/test");
        var result1 = _validator.TestValidate(cmd);
        var result2 = _validator.TestValidate(cmd);

        result1.ShouldNotHaveAnyValidationErrors();
        result2.ShouldNotHaveAnyValidationErrors();
    }
}
