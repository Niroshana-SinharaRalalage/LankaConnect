using FluentValidation;
namespace LankaConnect.Modules.Identity.Application.Commands.Users.AdminDowngradeUser;

/// <summary>
/// Validator for AdminDowngradeUserCommand
/// Phase 6A.106: Validates required fields for role downgrade
/// </summary>
public class AdminDowngradeUserCommandValidator : AbstractValidator<AdminDowngradeUserCommand>
{
    public AdminDowngradeUserCommandValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage("Target user ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
    }
}
