using LankaConnect.Modules.Payments.Domain.Repositories; // W4.4.d.2: 3 repo interfaces moved here
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Payments.Application.Queries.RefundRequests;

public class GetMyRefundRequestQueryHandler
    : IQueryHandler<GetMyRefundRequestQuery, AttendeeRefundRequestDto?>
{
    private readonly IRefundRequestRepository _refundRepo;
    private readonly ILogger<GetMyRefundRequestQueryHandler> _logger;

    public GetMyRefundRequestQueryHandler(
        IRefundRequestRepository refundRepo,
        ILogger<GetMyRefundRequestQueryHandler> logger)
    {
        _refundRepo = refundRepo;
        _logger = logger;
    }

    public async Task<Result<AttendeeRefundRequestDto?>> Handle(
        GetMyRefundRequestQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetMyRefundRequest"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("UserId", request.CallerUserId))
        {
            try
            {
                var entity = await _refundRepo.GetMyMostRecentForEventAsync(
                    request.EventId, request.CallerUserId, cancellationToken);

                if (entity is null)
                {
                    _logger.LogDebug(
                        "[6A.148] GetMyRefundRequest: no request found for EventId={EventId} UserId={UserId}",
                        request.EventId, request.CallerUserId);
                    return Result<AttendeeRefundRequestDto?>.Success(null);
                }

                return Result<AttendeeRefundRequestDto?>.Success(MapToAttendeeDto(entity));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[6A.148] GetMyRefundRequest FAILED for EventId={EventId} UserId={UserId}",
                    request.EventId, request.CallerUserId);
                throw;
            }
        }
    }

    /// <summary>
    /// Architect F6: OrganizerNotes is intentionally excluded from the attendee projection.
    /// Reflection-asserted by a domain test.
    /// </summary>
    internal static AttendeeRefundRequestDto MapToAttendeeDto(RefundRequest e) =>
        new(
            Id: e.Id,
            RegistrationId: e.RegistrationId,
            Status: e.Status,
            RequestedAt: e.RequestedAt,
            RequesterReason: e.RequesterReason,
            ReviewedAt: e.ReviewedAt,
            RejectionReason: e.RejectionReason,
            CompletedAt: e.CompletedAt,
            LineItems: e.LineItems.Select(li => new RefundLineItemDto(
                Id: li.Id,
                Type: li.Type,
                ReferenceId: li.ReferenceId,
                RequestedAmount: li.RequestedAmount.Amount,
                RequestedCurrency: li.RequestedAmount.Currency,
                ApprovedAmount: li.ApprovedAmount?.Amount,
                ApprovedCurrency: li.ApprovedAmount?.Currency,
                Status: li.Status,
                StripeRefundId: li.StripeRefundId,
                ProcessedAt: li.ProcessedAt,
                FailureReason: li.FailureReason
            )).ToList());
}
