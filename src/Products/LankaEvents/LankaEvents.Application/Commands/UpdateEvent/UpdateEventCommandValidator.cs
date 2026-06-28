using System;
using FluentValidation;
using LankaConnect.Products.LankaEvents.Application.Commands.CreateEvent;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateEvent;

/// <summary>
/// Phase 8X.4a — Validator for <see cref="UpdateEventCommand"/>. Mirrors
/// <see cref="CreateEventCommandValidator"/>'s ExternalPaid rules. Active-registration
/// guards live in the domain (<see cref="LankaConnect.Products.LankaEvents.Domain.Event.SetPaymentMode"/>
/// / <see cref="LankaConnect.Products.LankaEvents.Domain.Event.SetExternalPayment"/>) and surface from
/// the handler as 400 with the domain's error message.
/// </summary>
public class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
{
    private readonly ILogger<UpdateEventCommandValidator>? _logger;

    public UpdateEventCommandValidator() : this(logger: null) { }

    public UpdateEventCommandValidator(ILogger<UpdateEventCommandValidator>? logger)
    {
        _logger = logger;

        RuleFor(x => x.EventId).NotEmpty().WithMessage("EventId is required");

        // Phase 8YA.2: TBD-dates pair invariant — same rule as CreateEventCommandValidator.
        // Both null = "leave dates unchanged" (organiser updating other fields). Both set
        // = SetDates path. Mixed = invalid.
        RuleFor(x => x).Custom((cmd, ctx) =>
        {
            var bothNull = !cmd.StartDate.HasValue && !cmd.EndDate.HasValue;
            var bothSet = cmd.StartDate.HasValue && cmd.EndDate.HasValue;
            if (!bothNull && !bothSet)
            {
                ctx.AddFailure(
                    cmd.StartDate.HasValue ? nameof(cmd.EndDate) : nameof(cmd.StartDate),
                    "Both StartDate and EndDate must be provided together, or both must be empty (TBD event)");
            }
        });

        // Inference / inconsistency check — same table as CreateEventCommandValidator.
        RuleFor(x => x).Custom((cmd, ctx) =>
        {
            var inferred = CreateEventCommandValidator.InferPaymentMode(cmd.IsFree, cmd.PaymentMode);
            if (!inferred.IsValid)
                ctx.AddFailure(nameof(cmd.PaymentMode), inferred.Error!);
        });

        // ExternalPaid-only rules — Phase 8X.11 mirrors CreateEventCommandValidator.
        When(x => ResolvePaymentMode(x) == EventPaymentMode.ExternalPaid, () =>
        {
            // URL optional (Phase 8X.11) — length cap only.
            RuleFor(x => x.ExternalRegistrationUrl)
                .MaximumLength(ExternalRegistration.MaxUrlLength)
                .WithMessage($"ExternalRegistrationUrl cannot exceed {ExternalRegistration.MaxUrlLength} characters");

            RuleFor(x => x).Custom((cmd, ctx) =>
            {
                var allEmpty = string.IsNullOrWhiteSpace(cmd.ExternalRegistrationUrl)
                    && string.IsNullOrWhiteSpace(cmd.ExternalRegistrationInstructions)
                    && string.IsNullOrWhiteSpace(cmd.ExternalRegistrationVendorName);
                if (allEmpty)
                    return;

                var voResult = ExternalRegistration.Create(
                    cmd.ExternalRegistrationUrl,
                    cmd.ExternalRegistrationInstructions,
                    cmd.ExternalRegistrationVendorName);

                if (voResult.IsFailure)
                {
                    if (voResult.Error.Contains("private", StringComparison.OrdinalIgnoreCase)
                        || voResult.Error.Contains("loopback", StringComparison.OrdinalIgnoreCase)
                        || voResult.Error.Contains("link-local", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger?.LogWarning(
                            "[Phase 8X.4a] Rejected ExternalRegistrationUrl host on update (eventId={EventId}, reason={Reason}, url={Url})",
                            cmd.EventId,
                            voResult.Error,
                            cmd.ExternalRegistrationUrl);
                    }

                    ctx.AddFailure(nameof(cmd.ExternalRegistrationUrl), voResult.Error);
                }
            });

            // Phase 8X.11 — RegistrationMode must be null or External.
            RuleFor(x => x.RegistrationMode)
                .Must(mode => mode == null || mode == LankaConnect.Products.LankaEvents.Domain.Enums.RegistrationMode.External)
                .WithMessage("ExternalPaid events must use RegistrationMode = External (or null — handler will coerce). " +
                    "Other registration modes capture internal attendee data which doesn't apply to external-paid events.");

            // Phase 8X.11 — donations enabled-bit blocked. Sponsors / collections / signup-lists
            // are not represented at the UpdateEvent command surface (organiser updates them via
            // dedicated endpoints), so the domain guards added in Phase 8X.11 are the gate.
            RuleFor(x => x.DonationsEnabled)
                .Must(enabled => enabled != true)
                .WithMessage("Donations cannot be enabled on external-paid events. " +
                    "ExternalPaid is a 'pure external' mode — donations require an on-platform contribution surface.");
        });

        RuleFor(x => x.ExternalRegistrationInstructions)
            .MaximumLength(ExternalRegistration.MaxInstructionsLength)
            .WithMessage($"ExternalRegistrationInstructions cannot exceed {ExternalRegistration.MaxInstructionsLength} characters");

        RuleFor(x => x.ExternalRegistrationVendorName)
            .MaximumLength(ExternalRegistration.MaxVendorNameLength)
            .WithMessage($"ExternalRegistrationVendorName cannot exceed {ExternalRegistration.MaxVendorNameLength} characters");
    }

    private static EventPaymentMode ResolvePaymentMode(UpdateEventCommand cmd)
    {
        var inferred = CreateEventCommandValidator.InferPaymentMode(cmd.IsFree, cmd.PaymentMode);
        return inferred.Mode;
    }
}
