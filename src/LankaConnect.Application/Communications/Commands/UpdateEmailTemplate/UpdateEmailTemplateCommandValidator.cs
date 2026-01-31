using FluentValidation;

namespace LankaConnect.Application.Communications.Commands.UpdateEmailTemplate;

/// <summary>
/// Phase 6A.89: Validator for UpdateEmailTemplateCommand
/// </summary>
public class UpdateEmailTemplateCommandValidator : AbstractValidator<UpdateEmailTemplateCommand>
{
    public UpdateEmailTemplateCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Template ID is required");

        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithMessage("Subject is required")
            .MaximumLength(500)
            .WithMessage("Subject must not exceed 500 characters");

        RuleFor(x => x.TextTemplate)
            .NotEmpty()
            .WithMessage("Text template is required")
            .MaximumLength(100000)
            .WithMessage("Text template must not exceed 100,000 characters");

        RuleFor(x => x.HtmlTemplate)
            .MaximumLength(500000)
            .When(x => x.HtmlTemplate != null)
            .WithMessage("HTML template must not exceed 500,000 characters");

        RuleFor(x => x.Tags)
            .MaximumLength(500)
            .When(x => x.Tags != null)
            .WithMessage("Tags must not exceed 500 characters");
    }
}

/// <summary>
/// Phase 6A.89: Validator for ToggleEmailTemplateActiveCommand
/// </summary>
public class ToggleEmailTemplateActiveCommandValidator : AbstractValidator<ToggleEmailTemplateActiveCommand>
{
    public ToggleEmailTemplateActiveCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Template ID is required");
    }
}
