using FluentValidation;

namespace LankaConnect.Application.Users.Commands.LinkExternalProvider;

/// <summary>
/// Validator for LinkExternalProviderCommand
/// Epic 1 Phase 2: Multi-Provider Social Login
/// </summary>
public class LinkExternalProviderValidator : AbstractValidator<LinkExternalProviderCommand>
{
    public LinkExternalProviderValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.ExternalProviderId)
            .NotEmpty()
            .WithMessage("External provider ID is required")
            .MaximumLength(500)
            .WithMessage("External provider ID cannot exceed 500 characters");

        RuleFor(x => x.ProviderEmail)
            .NotEmpty()
            .WithMessage("Provider email is required")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .MaximumLength(320)
            .WithMessage("Email cannot exceed 320 characters");

        RuleFor(x => x.Provider)
            .IsInEnum()
            .WithMessage("Invalid provider specified");
    }
}
