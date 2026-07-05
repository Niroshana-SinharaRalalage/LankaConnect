using System;
using FluentValidation;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Products.LankaEvents.Application.Commands.CreateEvent;

/// <summary>
/// Phase 8X.4a — Validator for <see cref="CreateEventCommand"/>.
///
/// Validator inference table (architect-locked 2026-05-07 — copied verbatim from
/// MASTER_TODO_PHASE_8X_EXTERNAL_PAYMENT.md):
/// <code>
/// | isFree | paymentMode                       | Result                                  |
/// |--------|-----------------------------------|-----------------------------------------|
/// | true   | null                              | Free                                    |
/// | true   | Free                              | Free                                    |
/// | true   | OnPlatformPaid / ExternalPaid     | 400 inconsistent                        |
/// | false  | null                              | OnPlatformPaid (security default)       |
/// | false  | OnPlatformPaid                    | OnPlatformPaid                          |
/// | false  | ExternalPaid                      | ExternalPaid                            |
/// | false  | Free                              | 400 inconsistent                        |
/// | null   | null                              | OnPlatformPaid (security default 6A.81) |
/// | null   | non-null                          | as supplied                             |
/// </code>
///
/// ExternalPaid additional rules:
/// - <c>ExternalRegistrationUrl</c> is required, https-only, parses as absolute URI,
///   length ≤ 2048; loopback / RFC1918 / link-local hosts are rejected by
///   <see cref="ExternalRegistration.Create"/>.
/// - <c>RegistrationMode</c> must be null or <see cref="RegistrationMode.NoRegistration"/>
///   (handler coerces null to NoRegistration in Slice 8X.4b).
/// </summary>
public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    private readonly ILogger<CreateEventCommandValidator>? _logger;

    public CreateEventCommandValidator() : this(logger: null) { }

    public CreateEventCommandValidator(ILogger<CreateEventCommandValidator>? logger)
    {
        _logger = logger;

        // Phase 8YA.2: TBD-dates pair invariant — both null (Planning) or both set
        // (Draft). Mixed (one null, one set) is invalid; the domain rejects too as
        // defence-in-depth, but surfacing this here gives the user a 400 with a
        // clear message instead of a generic domain "Failure" error.
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

        // Inference / inconsistency check.
        RuleFor(x => x).Custom((cmd, ctx) =>
        {
            var inferred = InferPaymentMode(cmd.IsFree, cmd.PaymentMode);
            if (!inferred.IsValid)
                ctx.AddFailure(nameof(cmd.PaymentMode), inferred.Error!);
        });

        // ExternalPaid-only rules. Phase 8X.11 — URL is OPTIONAL; the registration mode
        // is "External" (not NoRegistration); donations/sponsors/collections/signup-lists
        // are blocked at the validator (with mirroring domain guards as defence-in-depth).
        When(x => ResolvePaymentMode(x) == EventPaymentMode.ExternalPaid, () =>
        {
            // URL length cap still applies when supplied. The .NotEmpty() rule was dropped
            // in Phase 8X.11 — cash-at-door / bank-deposit / phone-only registration patterns
            // legitimately have no URL.
            RuleFor(x => x.ExternalRegistrationUrl)
                .MaximumLength(ExternalRegistration.MaxUrlLength)
                .WithMessage($"ExternalRegistrationUrl cannot exceed {ExternalRegistration.MaxUrlLength} characters");

            // VO already validates https + host security + length when URL is non-empty.
            // Re-running Create() here gives single source of truth + consistent error messages.
            RuleFor(x => x).Custom((cmd, ctx) =>
            {
                // Only run VO factory when at least one of URL/instructions/vendor is non-empty.
                // The all-empty case is intentionally allowed at the API level — the handler
                // will store ExternalRegistration = null on the event (architect-approved
                // "Contact organiser for registration details" path).
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
                    // Loopback / private / link-local rejection: log at warning so security
                    // observability captures repeated probing attempts.
                    if (voResult.Error.Contains("private", StringComparison.OrdinalIgnoreCase)
                        || voResult.Error.Contains("loopback", StringComparison.OrdinalIgnoreCase)
                        || voResult.Error.Contains("link-local", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger?.LogWarning(
                            "[Phase 8X.4a] Rejected ExternalRegistrationUrl host (organiserId={OrganizerId}, reason={Reason}, url={Url})",
                            cmd.OrganizerId,
                            voResult.Error,
                            cmd.ExternalRegistrationUrl);
                    }

                    ctx.AddFailure(nameof(cmd.ExternalRegistrationUrl), voResult.Error);
                }
            });

            // Phase 8X.11 — RegistrationMode must be null or External (was: null or NoRegistration).
            RuleFor(x => x.RegistrationMode)
                .Must(mode => mode == null || mode == LankaConnect.Products.LankaEvents.Domain.Enums.RegistrationMode.External)
                .WithMessage("ExternalPaid events must use RegistrationMode = External (or null — handler will coerce). " +
                    "Other registration modes capture internal attendee data which doesn't apply to external-paid events.");

            // Phase 8X.11 — block donations / sponsors / collections / signup-lists at request time.
            // Domain methods enforce these too as defence-in-depth.
            RuleFor(x => x.DonationsEnabled)
                .Must(enabled => enabled != true)
                .WithMessage("Donations cannot be enabled on external-paid events. " +
                    "ExternalPaid is a 'pure external' mode — donations require an on-platform contribution surface.");
        });

        // Length caps for optional fields (apply regardless of mode for defence-in-depth).
        RuleFor(x => x.ExternalRegistrationInstructions)
            .MaximumLength(ExternalRegistration.MaxInstructionsLength)
            .WithMessage($"ExternalRegistrationInstructions cannot exceed {ExternalRegistration.MaxInstructionsLength} characters");

        RuleFor(x => x.ExternalRegistrationVendorName)
            .MaximumLength(ExternalRegistration.MaxVendorNameLength)
            .WithMessage($"ExternalRegistrationVendorName cannot exceed {ExternalRegistration.MaxVendorNameLength} characters");
    }

    /// <summary>
    /// Resolves the effective <see cref="EventPaymentMode"/> from the command's
    /// <c>IsFree</c> + <c>PaymentMode</c> pair using the architect-locked inference table.
    /// Returns the inferred mode for use in <see cref="When"/> conditions.
    /// On inconsistency returns OnPlatformPaid (the security default) — the inconsistency
    /// itself is surfaced as a separate validation failure via <see cref="InferPaymentMode"/>.
    /// </summary>
    public static EventPaymentMode ResolvePaymentMode(CreateEventCommand cmd)
    {
        var inferred = InferPaymentMode(cmd.IsFree, cmd.PaymentMode);
        return inferred.Mode;
    }

    /// <summary>
    /// Pure helper — encodes the inference table without throwing. Returns a tuple of
    /// (Mode, IsValid, Error).
    /// </summary>
    public static (EventPaymentMode Mode, bool IsValid, string? Error) InferPaymentMode(bool? isFree, EventPaymentMode? paymentMode)
    {
        // Both null → security default OnPlatformPaid.
        if (isFree == null && paymentMode == null)
            return (EventPaymentMode.OnPlatformPaid, true, null);

        if (isFree == true)
        {
            if (paymentMode == null) return (EventPaymentMode.Free, true, null);
            if (paymentMode == EventPaymentMode.Free) return (EventPaymentMode.Free, true, null);
            return (EventPaymentMode.OnPlatformPaid, false,
                $"Inconsistent payment configuration: IsFree=true cannot combine with PaymentMode={paymentMode}");
        }

        if (isFree == false)
        {
            if (paymentMode == null) return (EventPaymentMode.OnPlatformPaid, true, null);
            if (paymentMode == EventPaymentMode.Free)
                return (EventPaymentMode.OnPlatformPaid, false,
                    "Inconsistent payment configuration: IsFree=false cannot combine with PaymentMode=Free");
            return (paymentMode.Value, true, null);
        }

        // isFree == null, paymentMode != null
        return (paymentMode!.Value, true, null);
    }
}
