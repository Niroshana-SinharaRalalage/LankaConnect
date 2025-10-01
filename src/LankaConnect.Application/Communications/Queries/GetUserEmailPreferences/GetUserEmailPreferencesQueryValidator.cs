using FluentValidation;

namespace LankaConnect.Application.Communications.Queries.GetUserEmailPreferences;

/// <summary>
/// Validator for GetUserEmailPreferencesQuery
/// </summary>
public class GetUserEmailPreferencesQueryValidator : AbstractValidator<GetUserEmailPreferencesQuery>
{
    public GetUserEmailPreferencesQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");
    }
}