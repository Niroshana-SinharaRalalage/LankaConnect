using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Support.DTOs;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Support;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Support.Queries.GetSupportTicketsPaged;

/// <summary>
/// Handler for GetSupportTicketsPagedQuery
/// Phase 6A.90: Returns paginated support tickets for admin view
/// </summary>
public class GetSupportTicketsPagedQueryHandler : IQueryHandler<GetSupportTicketsPagedQuery, PagedResultDto<SupportTicketDto>>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetSupportTicketsPagedQueryHandler> _logger;

    public GetSupportTicketsPagedQueryHandler(
        ISupportTicketRepository ticketRepository,
        IUserRepository userRepository,
        ILogger<GetSupportTicketsPagedQueryHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<PagedResultDto<SupportTicketDto>>> Handle(
        GetSupportTicketsPagedQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetSupportTicketsPaged"))
        using (LogContext.PushProperty("EntityType", "SupportTicket"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetSupportTicketsPaged START: Page={Page}, PageSize={PageSize}, Status={Status}, Priority={Priority}",
                request.Page, request.PageSize, request.StatusFilter, request.PriorityFilter);

            try
            {
                var (tickets, totalCount) = await _ticketRepository.GetPagedAsync(
                    request.Page,
                    request.PageSize,
                    request.SearchTerm,
                    request.StatusFilter,
                    request.PriorityFilter,
                    request.AssignedToFilter,
                    request.UnassignedOnly,
                    cancellationToken);

                // Get assigned user names
                var assignedUserIds = tickets
                    .Where(t => t.AssignedToUserId.HasValue)
                    .Select(t => t.AssignedToUserId!.Value)
                    .Distinct()
                    .ToList();

                var userNames = assignedUserIds.Any()
                    ? await _userRepository.GetUserNamesAsync(assignedUserIds, cancellationToken)
                    : new Dictionary<Guid, string>();

                var dtos = tickets.Select(t => new SupportTicketDto
                {
                    Id = t.Id,
                    ReferenceId = t.ReferenceId,
                    Name = t.Name,
                    Email = t.Email.Value,
                    Subject = t.Subject,
                    Status = t.Status.ToString(),
                    Priority = t.Priority.ToString(),
                    AssignedToUserId = t.AssignedToUserId,
                    AssignedToName = t.AssignedToUserId.HasValue && userNames.TryGetValue(t.AssignedToUserId.Value, out var name)
                        ? name
                        : null,
                    ReplyCount = t.Replies.Count,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                }).ToList();

                var result = new PagedResultDto<SupportTicketDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize
                };

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetSupportTicketsPaged COMPLETE: ItemCount={ItemCount}, TotalCount={TotalCount}, Duration={ElapsedMs}ms",
                    dtos.Count, totalCount, stopwatch.ElapsedMilliseconds);

                return Result<PagedResultDto<SupportTicketDto>>.Success(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetSupportTicketsPaged FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
