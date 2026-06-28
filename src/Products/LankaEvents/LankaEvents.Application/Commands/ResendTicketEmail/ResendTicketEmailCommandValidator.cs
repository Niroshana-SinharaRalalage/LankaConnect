using FluentValidation;

namespace LankaConnect.Products.LankaEvents.Application.Commands.ResendTicketEmail;

public class ResendTicketEmailCommandValidator : AbstractValidator<ResendTicketEmailCommand>
{
    public ResendTicketEmailCommandValidator()
    {
        RuleFor(x => x.RegistrationId)
            .NotEmpty()
            .WithMessage("Registration ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");
    }
}
