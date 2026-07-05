using LankaConnect.Domain.Common;
namespace LankaConnect.BuildingBlocks.Application.Common.Interfaces;

/// <summary>
/// Phase 7A: Processes ACS WhatsApp delivery status webhooks.
/// </summary>
public interface IWhatsAppWebhookProcessor
{
    Task<Result> ProcessDeliveryStatusAsync(string payload, CancellationToken ct = default);
}
