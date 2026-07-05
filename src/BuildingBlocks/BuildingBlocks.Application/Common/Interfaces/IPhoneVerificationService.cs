using LankaConnect.Domain.Common;
namespace LankaConnect.BuildingBlocks.Application.Common.Interfaces;

/// <summary>
/// Phase 7A: Sends verification codes via SMS (not WhatsApp -- 24-hour window limitation).
/// Uses Azure Communication Services SMS.
/// Falls back to WhatsApp template if SMS NuGet is not available.
/// </summary>
public interface IPhoneVerificationService
{
    Task<Result> SendVerificationCodeAsync(string phoneNumber, string code, CancellationToken ct = default);
}
