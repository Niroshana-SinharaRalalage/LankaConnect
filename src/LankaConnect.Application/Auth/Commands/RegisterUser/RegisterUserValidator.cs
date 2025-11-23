using FluentValidation;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Application.Auth.Commands.RegisterUser;

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
    }

    private static bool BeValidEmail(string email)
    {
        var emailResult = LankaConnect.Domain.Shared.ValueObjects.Email.Create(email);
        return emailResult.IsSuccess;
    }
}