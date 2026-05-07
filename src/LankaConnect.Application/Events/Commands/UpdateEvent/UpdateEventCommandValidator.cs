using System;
using FluentValidation;
using LankaConnect.Application.Events.Commands.CreateEvent;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.UpdateEvent;

/// <summary>
/// Phase 8X.4a — Validator for <see cref="UpdateEventCommand"/>. Mirrors
/// <see cref="CreateEventCommandValidator"/>'s ExternalPaid rules. Active-registration
/// guards live in the domain (<see cref="LankaConnect.Domain.Events.Event.SetPaymentMode"/>
/// / <see cref="LankaConnect.Domain.Events.Event.SetExternalPayment"/>) and surface from
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

        // Inference / inconsistency check — same table as CreateEventCommandValidator.
        RuleFor(x => x).Custom((cmd, ctx) =>
        {
            var inferred = CreateEventCommandValidator.InferPaymentMode(cmd.IsFree, cmd.PaymentMode);
            if (!inferred.IsValid)
                ctx.AddFailure(nameof(cmd.PaymentMode), inferred.Error!);
        });

        // ExternalPaid-only rules.
        When(x => ResolvePaymentMode(x) == EventPaymentMode.ExternalPaid, () =>
        {
            RuleFor(x => x.ExternalRegistrationUrl)
                .NotEmpty()
                .WithMessage("ExternalRegistrationUrl is required when PaymentMode is ExternalPaid")
                .MaximumLength(ExternalRegistration.MaxUrlLength)
                .WithMessage($"ExternalRegistrationUrl cannot exceed {ExternalRegistration.MaxUrlLength} characters");

            RuleFor(x => x).Custom((cmd, ctx) =>
            {
                if (string.IsNullOrWhiteSpace(cmd.ExternalRegistrationUrl))
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

            RuleFor(x => x.RegistrationMode)
                .Must(mode => mode == null || mode == Domain.Events.Enums.RegistrationMode.NoRegistration)
                .WithMessage("ExternalPaid events must use RegistrationMode = NoRegistration (or null — handler will coerce)");
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
