using FluentValidation;
namespace LankaConnect.Modules.Identity.Application.Commands.Users.AdminUpgradeUser;

/// <summary>
/// Validator for AdminUpgradeUserCommand.
/// Phase 6A.139: Mirrors AdminDowngradeUserCommandValidator for audit symmetry.
/// </summary>
public class AdminUpgradeUserCommandValidator : AbstractValidator<AdminUpgradeUserCommand>
{
    public AdminUpgradeUserCommandValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage("Target user ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
    }
}
