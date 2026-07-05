using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Communications.Domain.Entities;
namespace LankaConnect.Modules.Communications.Domain;

/// <summary>
/// Phase 7A: Repository interface for WhatsApp message persistence and queries.
/// </summary>
public interface IWhatsAppMessageRepository : IRepository<WhatsAppMessageRecord>
{
    Task<WhatsAppMessageRecord?> GetByAcsMessageIdAsync(string acsMessageId, CancellationToken ct = default);
    Task<IReadOnlyList<WhatsAppMessageRecord>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<WhatsAppMessageRecord>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default);
    Task<IReadOnlyList<WhatsAppMessageRecord>> GetPendingRetryAsync(int maxCount, CancellationToken ct = default);
    Task<IReadOnlyList<WhatsAppMessageRecord>> GetScheduledMessagesAsync(DateTime before, CancellationToken ct = default);
    Task<bool> HasRecentDuplicateAsync(Guid? userId, Guid? eventId, string templateName, int withinMinutes = 5, CancellationToken ct = default);
    Task<WhatsAppMessageMetrics> GetMetricsAsync(DateTime from, DateTime to, CancellationToken ct = default);
}

/// <summary>
/// Aggregated WhatsApp message delivery metrics.
/// </summary>
public class WhatsAppMessageMetrics
{
    public int TotalSent { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalRead { get; set; }
    public int TotalFailed { get; set; }
    public double DeliveryRate => TotalSent > 0 ? (double)TotalDelivered / TotalSent * 100 : 0;
    public double ReadRate => TotalDelivered > 0 ? (double)TotalRead / TotalDelivered * 100 : 0;
    public Dictionary<string, int> ByTemplate { get; set; } = new();
}
