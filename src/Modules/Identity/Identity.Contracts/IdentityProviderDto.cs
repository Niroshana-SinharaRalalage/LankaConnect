namespace LankaConnect.Modules.Identity.Contracts;

/// <summary>
/// Cross-module mirror of <c>LankaConnect.Domain.Users.Enums.IdentityProvider</c>.
/// Indicates how the user was provisioned (local password vs federated SSO).
/// </summary>
/// <remarks>
/// Wave 4.6.a (2026-06-24). 1:1 byte cast at the
/// <c>UserContractMappings</c> seam.
/// </remarks>
public enum IdentityProviderDto : byte
{
    Local = 0,
    EntraExternalId = 1,
    Federated = 2,
}
