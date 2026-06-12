using FluentValidation;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;

namespace LankaConnect.Modules.Forms.Application.Commands.UpdateEventForm;

public class UpdateEventFormCommandValidator : AbstractValidator<UpdateEventFormCommand>
{
    public UpdateEventFormCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required");

        RuleFor(x => x.FormId)
            .NotEmpty()
            .WithMessage("Form ID is required");

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
    }
}
