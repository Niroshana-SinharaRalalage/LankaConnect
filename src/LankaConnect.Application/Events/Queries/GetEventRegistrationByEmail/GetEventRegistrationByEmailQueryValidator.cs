using FluentValidation;

namespace LankaConnect.Application.Events.Queries.GetEventRegistrationByEmail;

/// <summary>
/// Validator for GetEventRegistrationByEmailQuery
/// Phase 6A.15: Enhanced sign-up list UX with email validation
/// </summary>
public class GetEventRegistrationByEmailQueryValidator : AbstractValidator<GetEventRegistrationByEmailQuery>
{
    public GetEventRegistrationByEmailQueryValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Email must be a valid email address")
            .MaximumLength(256)
            .WithMessage("Email must not exceed 256 characters");
    }
}
