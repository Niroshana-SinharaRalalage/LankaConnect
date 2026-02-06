using FluentValidation;

namespace LankaConnect.Application.Communications.Commands.CreateEmailGroup;

/// <summary>
/// Validator for CreateEmailGroupCommand
/// Phase 6A.25: Email Groups Management
/// </summary>
public class CreateEmailGroupCommandValidator : AbstractValidator<CreateEmailGroupCommand>
{
    public CreateEmailGroupCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.EmailAddresses)
            .NotEmpty().WithMessage("At least one email address is required");
    }
}
