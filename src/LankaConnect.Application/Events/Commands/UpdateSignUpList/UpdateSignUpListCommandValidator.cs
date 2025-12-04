using FluentValidation;

namespace LankaConnect.Application.Events.Commands.UpdateSignUpList;

/// <summary>
/// Validator for UpdateSignUpListCommand
/// Phase 6A.13: Edit Sign-Up List feature
/// </summary>
public class UpdateSignUpListCommandValidator : AbstractValidator<UpdateSignUpListCommand>
{
    public UpdateSignUpListCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required");

        RuleFor(x => x.SignUpListId)
            .NotEmpty()
            .WithMessage("Sign-up list ID is required");

        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage("Category is required")
            .MaximumLength(100)
            .WithMessage("Category must not exceed 100 characters");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x)
            .Must(x => x.HasMandatoryItems || x.HasPreferredItems || x.HasSuggestedItems)
            .WithMessage("At least one item category must be selected");
    }
}
