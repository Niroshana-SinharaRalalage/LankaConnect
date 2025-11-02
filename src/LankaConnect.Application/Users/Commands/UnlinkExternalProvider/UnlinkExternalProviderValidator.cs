using FluentValidation;

namespace LankaConnect.Application.Users.Commands.UnlinkExternalProvider;

/// <summary>
/// Validator for UnlinkExternalProviderCommand
/// Epic 1 Phase 2: Multi-Provider Social Login
/// </summary>
public class UnlinkExternalProviderValidator : AbstractValidator<UnlinkExternalProviderCommand>
{
    public UnlinkExternalProviderValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Provider)
            .IsInEnum()
            .WithMessage("Invalid provider specified");
    }
}
