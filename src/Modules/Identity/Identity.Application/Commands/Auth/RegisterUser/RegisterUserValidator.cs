using FluentValidation;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.Modules.Identity.Domain.Enums;
namespace LankaConnect.Modules.Identity.Application.Commands.Auth.RegisterUser;

public class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .Must(BeValidEmail)
            .WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long")
            .MaximumLength(128)
            .WithMessage("Password must not exceed 128 characters")
            .Matches(@"[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]")
            .WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"\d")
            .WithMessage("Password must contain at least one digit")
            .Matches(@"[^a-zA-Z\d\s]")
            .WithMessage("Password must contain at least one special character");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required")
            .MaximumLength(50)
            .WithMessage("First name must not exceed 50 characters");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required")
            .MaximumLength(50)
            .WithMessage("Last name must not exceed 50 characters");

        RuleFor(x => x.SelectedRole)
            .Must(role => !role.HasValue || Enum.IsDefined(typeof(UserRole), role.Value))
            .When(x => x.SelectedRole.HasValue)
            .WithMessage("Invalid user role");

        // Metro Areas - Required for registration (min 1, max 20)
        RuleFor(x => x.PreferredMetroAreaIds)
            .NotNull()
            .WithMessage("At least one metro area must be selected")
            .Must(ids => ids != null && ids.Count >= 1)
            .WithMessage("At least one metro area must be selected")
            .Must(ids => ids == null || ids.Count <= 20)
            .WithMessage("Maximum 20 metro areas allowed");

        // Phase 7A.6A: WhatsApp phone number - optional, but must be E.164 if provided
        RuleFor(x => x.WhatsAppPhoneNumber)
            .Matches(@"^\+[1-9]\d{1,14}$")
            .When(x => !string.IsNullOrWhiteSpace(x.WhatsAppPhoneNumber))
            .WithMessage("WhatsApp phone number must be in E.164 format (e.g., +14155551234)");
    }

    private static bool BeValidEmail(string email)
    {
        var emailResult = LankaConnect.SharedKernel.Contact.Email.Create(email);
        return emailResult.IsSuccess;
    }
}
