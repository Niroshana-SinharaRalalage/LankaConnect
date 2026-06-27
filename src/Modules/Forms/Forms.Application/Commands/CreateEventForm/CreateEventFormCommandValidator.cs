using FluentValidation;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.Enums;

namespace LankaConnect.Modules.Forms.Application.Commands.CreateEventForm;

public class CreateEventFormCommandValidator : AbstractValidator<CreateEventFormCommand>
{
    public CreateEventFormCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Form title is required")
            .MaximumLength(200)
            .WithMessage("Form title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Form description must not exceed 2000 characters");

        RuleFor(x => x.MaxResponses)
            .GreaterThan(0)
            .When(x => x.MaxResponses.HasValue)
            .WithMessage("Max responses must be greater than 0");

        RuleFor(x => x.ResponseDeadline)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.ResponseDeadline.HasValue)
            .WithMessage("Response deadline must be in the future");

        RuleFor(x => x.Questions)
            .NotNull()
            .WithMessage("Questions list cannot be null");

        RuleForEach(x => x.Questions).ChildRules(question =>
        {
            question.RuleFor(q => q.QuestionText)
                .NotEmpty()
                .WithMessage("Question text is required")
                .MaximumLength(500)
                .WithMessage("Question text must not exceed 500 characters");

            question.RuleFor(q => q.SortOrder)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Sort order cannot be negative");

            question.RuleFor(q => q.HelpText)
                .MaximumLength(300)
                .When(q => q.HelpText != null)
                .WithMessage("Help text must not exceed 300 characters");

            question.RuleFor(q => q.Options)
                .Must(opts => opts != null && opts.Count >= 2)
                .When(q => q.QuestionType is FormQuestionType.SingleChoice
                    or FormQuestionType.MultipleChoice
                    or FormQuestionType.Dropdown)
                .WithMessage("Choice-type questions must have at least 2 options");

            question.RuleFor(q => q.Options)
                .Must(opts => opts == null || opts.Count == 0)
                .When(q => q.QuestionType is FormQuestionType.ShortText
                    or FormQuestionType.LongText
                    or FormQuestionType.Number
                    or FormQuestionType.Date
                    or FormQuestionType.YesNo)
                .WithMessage("Options are not allowed for this question type");
        });
    }
}
