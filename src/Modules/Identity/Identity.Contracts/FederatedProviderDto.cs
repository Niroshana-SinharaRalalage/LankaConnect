namespace LankaConnect.Modules.Identity.Contracts;

/// <summary>
/// Cross-module mirror of <c>LankaConnect.Modules.Identity.Domain.Enums.FederatedProvider</c>.
/// Identifies the upstream OAuth/OIDC provider for SSO-provisioned accounts.
/// </summary>
/// <remarks>
/// Wave 4.6.a (2026-06-24). 1:1 byte cast.
/// </remarks>
public enum FederatedProviderDto : byte
{
    Microsoft = 0,
    Google = 1,
    Facebook = 2,
    Apple = 3,
}
