using FluentValidation;
namespace LankaConnect.Modules.Communications.Application.Queries.GetUserEmailPreferences;

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