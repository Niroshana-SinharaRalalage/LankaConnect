using FluentValidation;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Modules.Forms.Application.Commands.AddFormQuestion;

public class AddFormQuestionCommandValidator : AbstractValidator<AddFormQuestionCommand>
{
    private static readonly FormQuestionType[] ChoiceTypes =
    {
        FormQuestionType.SingleChoice,
        FormQuestionType.MultipleChoice,
        FormQuestionType.Dropdown
    };

    public AddFormQuestionCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required");

        RuleFor(x => x.FormId)
            .NotEmpty()
            .WithMessage("Form ID is required");

        RuleFor(x => x.QuestionText)
            .NotEmpty()
            .WithMessage("Question text is required")
            .MaximumLength(500)
            .WithMessage("Question text must not exceed 500 characters");

        RuleFor(x => x.QuestionType)
            .IsInEnum()
            .WithMessage("Invalid question type");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Sort order must be non-negative");

        RuleFor(x => x.HelpText)
            .MaximumLength(300)
            .WithMessage("Help text must not exceed 300 characters");

        RuleFor(x => x.Options)
            .NotEmpty()
            .WithMessage("Options are required for choice-type questions")
            .When(x => ChoiceTypes.Contains(x.QuestionType));

        RuleFor(x => x.Options)
            .Must(options => options == null || options.Count == 0)
            .WithMessage("Options are not allowed for this question type")
            .When(x => !ChoiceTypes.Contains(x.QuestionType));

        RuleForEach(x => x.Options)
            .ChildRules(option =>
            {
                option.RuleFor(o => o.Text)
                    .NotEmpty()
                    .WithMessage("Option text is required")
                    .MaximumLength(500)
                    .WithMessage("Option text must not exceed 500 characters");
            })
            .When(x => x.Options != null && x.Options.Count > 0);
    }
}
