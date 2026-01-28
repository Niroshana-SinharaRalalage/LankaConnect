using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Support.DTOs;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Support;
using LankaConnect.Domain.Support.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Support.Queries.GetSupportTicketStatistics;

/// <summary>
/// Handler for GetSupportTicketStatisticsQuery
/// Phase 6A.90: Returns support ticket statistics for admin dashboard
/// </summary>
public class GetSupportTicketStatisticsQueryHandler : IQueryHandler<GetSupportTicketStatisticsQuery, SupportTicketStatisticsDto>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ILogger<GetSupportTicketStatisticsQueryHandler> _logger;

    public GetSupportTicketStatisticsQueryHandler(
        ISupportTicketRepository ticketRepository,
        ILogger<GetSupportTicketStatisticsQueryHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _logger = logger;
    }

    public async Task<Result<SupportTicketStatisticsDto>> Handle(
        GetSupportTicketStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetSupportTicketStatistics"))
        using (LogContext.PushProperty("EntityType", "SupportTicket"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("GetSupportTicketStatistics START");

            try
            {
                // Get counts by status
                var countsByStatus = await _ticketRepository.GetCountsByStatusAsync(cancellationToken);

                // Get counts by priority
                var countsByPriority = await _ticketRepository.GetCountsByPriorityAsync(cancellationToken);

                // Get unassigned count
                var unassignedCount = await _ticketRepository.GetUnassignedCountAsync(cancellationToken);

                var dto = new SupportTicketStatisticsDto
                {
                    TotalTickets = countsByStatus.Values.Sum(),
                    NewTickets = countsByStatus.GetValueOrDefault(SupportTicketStatus.New, 0),
                    InProgressTickets = countsByStatus.GetValueOrDefault(SupportTicketStatus.InProgress, 0),
                    WaitingForResponseTickets = countsByStatus.GetValueOrDefault(SupportTicketStatus.WaitingForResponse, 0),
                    ResolvedTickets = countsByStatus.GetValueOrDefault(SupportTicketStatus.Resolved, 0),
                    ClosedTickets = countsByStatus.GetValueOrDefault(SupportTicketStatus.Closed, 0),
                    UnassignedTickets = unassignedCount,
                    TicketsByPriority = countsByPriority.ToDictionary(
                        kvp => kvp.Key.ToString(),
                        kvp => kvp.Value)
                };

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetSupportTicketStatistics COMPLETE: TotalTickets={TotalTickets}, NewTickets={NewTickets}, UnassignedTickets={UnassignedTickets}, Duration={ElapsedMs}ms",
                    dto.TotalTickets, dto.NewTickets, dto.UnassignedTickets, stopwatch.ElapsedMilliseconds);

                return Result<SupportTicketStatisticsDto>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetSupportTicketStatistics FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
