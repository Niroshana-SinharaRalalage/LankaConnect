using FluentValidation;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Communications.Queries.GetEmailStatus;

/// <summary>
/// Validator for GetEmailStatusQuery
/// </summary>
public class GetEmailStatusQueryValidator : AbstractValidator<GetEmailStatusQuery>
{
    public GetEmailStatusQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.EmailAddress)
            .Must(BeValidEmailWhenProvided)
            .WithMessage("Invalid email format")
            .When(x => !string.IsNullOrWhiteSpace(x.EmailAddress));

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .WithMessage("From date must be earlier than or equal to to date")
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue);

        RuleFor(x => x.ToDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("To date cannot be in the future")
            .When(x => x.ToDate.HasValue);

        RuleFor(x => x.EmailType)
            .IsInEnum()
            .WithMessage("Invalid email type")
            .When(x => x.EmailType.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Invalid email status")
            .When(x => x.Status.HasValue);
    }

    private static bool BeValidEmailWhenProvided(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return true;

        var emailResult = Email.Create(email);
        return emailResult.IsSuccess;
    }
}