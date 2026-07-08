using LankaConnect.Modules.Communications.Domain.Entities;
using LankaConnect.Infrastructure.Data;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Modules.Communications.Infrastructure.Email.Services;

/// <summary>
/// Phase 6A.148.W5.6.B.OBS3 — see <see cref="IRefundDispatchAuditService"/>.
/// </summary>
public class RefundDispatchAuditService : IRefundDispatchAuditService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<RefundDispatchAuditService> _logger;

    public RefundDispatchAuditService(
        AppDbContext dbContext,
        ILogger<RefundDispatchAuditService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task WriteSuppressionAsync(
        string templateName,
        string recipientEmail,
        string? recipientName,
        string suppressionReason,
        Guid correlationId,
        Guid? refundRequestId,
        string? entityType,
        Guid? entityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var row = EmailDispatchLog.ForSuppress(
                correlationId: correlationId,
                templateName: templateName,
                recipientEmail: recipientEmail,
                recipientName: recipientName,
                suppressionReason: suppressionReason,
                refundRequestId: refundRequestId,
                entityType: entityType,
                entityId: entityId);

            _dbContext.EmailDispatchLogs.Add(row);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[EmailDispatchLog] Failed to persist 'suppress' row for template {Template} recipient {Recipient} reason {Reason} correlation {Correlation}",
                templateName, recipientEmail, suppressionReason, correlationId);
        }
    }
}
