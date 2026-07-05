using LankaConnect.Modules.Payments.Domain.Repositories; // W4.4.d.2: 3 repo interfaces moved here
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Modules.Payments.Application.Queries.RefundRequests;

public class GetEventRefundRequestsQueryHandler
    : IQueryHandler<GetEventRefundRequestsQuery, IReadOnlyList<OrganizerRefundRequestDto>>
{
    private readonly IEventRepository _eventRepo;
    private readonly IRefundRequestRepository _refundRepo;
    private readonly ILogger<GetEventRefundRequestsQueryHandler> _logger;

    public GetEventRefundRequestsQueryHandler(
        IEventRepository eventRepo,
        IRefundRequestRepository refundRepo,
        ILogger<GetEventRefundRequestsQueryHandler> logger)
    {
        _eventRepo = eventRepo;
        _refundRepo = refundRepo;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<OrganizerRefundRequestDto>>> Handle(
        GetEventRefundRequestsQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEventRefundRequests"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("CallerUserId", request.CallerUserId))
        {
            try
            {
                var @event = await _eventRepo.GetByIdAsync(request.EventId, cancellationToken);
                if (@event is null)
                    return Result<IReadOnlyList<OrganizerRefundRequestDto>>.NotFound("Event not found");

                if (!@event.IsOrganizer(request.CallerUserId))
                {
                    _logger.LogWarning(
                        "[6A.148] GetEventRefundRequests forbidden: CallerUserId={CallerId} is not an organizer of EventId={EventId}",
                        request.CallerUserId, request.EventId);
                    return Result<IReadOnlyList<OrganizerRefundRequestDto>>.Forbidden(
                        "Only organizers of this event can view refund requests");
                }

                var entities = await _refundRepo.ListByEventAsync(
                    request.EventId, request.StatusFilter, cancellationToken);

                var dtos = entities.Select(MapToOrganizerDto).ToList();
                _logger.LogInformation(
                    "[6A.148] GetEventRefundRequests COMPLETE: EventId={EventId} Filter={Filter} Count={Count}",
                    request.EventId, request.StatusFilter, dtos.Count);
                return Result<IReadOnlyList<OrganizerRefundRequestDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[6A.148] GetEventRefundRequests FAILED for EventId={EventId}",
                    request.EventId);
                throw;
            }
        }
    }

    internal static OrganizerRefundRequestDto MapToOrganizerDto(RefundRequest e) =>
        new(
            Id: e.Id,
            RegistrationId: e.RegistrationId,
            RequestedByUserId: e.RequestedByUserId,
            IsOrganizerInitiated: e.IsOrganizerInitiated,
            Status: e.Status,
            RequestedAt: e.RequestedAt,
            RequesterReason: e.RequesterReason,
            ReviewedByUserId: e.ReviewedByUserId,
            ReviewedAt: e.ReviewedAt,
            OrganizerNotes: e.OrganizerNotes,
            RejectionReason: e.RejectionReason,
            CompletedAt: e.CompletedAt,
            ScanGuardOverridden: e.ScanGuardOverridden,
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
