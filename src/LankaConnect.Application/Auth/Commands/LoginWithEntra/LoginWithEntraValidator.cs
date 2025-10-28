using FluentValidation;

namespace LankaConnect.Application.Auth.Commands.LoginWithEntra;

/// <summary>
/// Validates LoginWithEntraCommand
/// </summary>
public class LoginWithEntraValidator : AbstractValidator<LoginWithEntraCommand>
{
    public LoginWithEntraValidator()
    {
        RuleFor(x => x.AccessToken)
            .NotEmpty()
            .WithMessage("Access token is required")
            .MinimumLength(10)
            .WithMessage("Access token must be at least 10 characters");

        RuleFor(x => x.IpAddress)
            .Must(BeValidIpAddressOrEmpty)
            .WithMessage("IP address format is invalid");
    }

    private bool BeValidIpAddressOrEmpty(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return true;

        return System.Net.IPAddress.TryParse(ipAddress, out _);
    }
}
