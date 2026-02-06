using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Support.DTOs;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Support;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Support.Queries.GetSupportTicketById;

/// <summary>
/// Handler for GetSupportTicketByIdQuery
/// Phase 6A.90: Returns detailed support ticket for admin view
/// </summary>
public class GetSupportTicketByIdQueryHandler : IQueryHandler<GetSupportTicketByIdQuery, SupportTicketDetailsDto>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetSupportTicketByIdQueryHandler> _logger;

    public GetSupportTicketByIdQueryHandler(
        ISupportTicketRepository ticketRepository,
        IUserRepository userRepository,
        ILogger<GetSupportTicketByIdQueryHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<SupportTicketDetailsDto>> Handle(
        GetSupportTicketByIdQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetSupportTicketById"))
        using (LogContext.PushProperty("EntityType", "SupportTicket"))
        using (LogContext.PushProperty("TicketId", request.TicketId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetSupportTicketById START: TicketId={TicketId}",
                request.TicketId);

            try
            {
                var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
                if (ticket == null)
                {
                    _logger.LogWarning(
                        "GetSupportTicketById FAILED: Ticket not found - TicketId={TicketId}",
                        request.TicketId);
                    return Result<SupportTicketDetailsDto>.Failure("Support ticket not found");
                }

                // Get user names for assigned user and admin users who replied/noted
                var userIds = new List<Guid>();
                if (ticket.AssignedToUserId.HasValue)
                    userIds.Add(ticket.AssignedToUserId.Value);
                userIds.AddRange(ticket.Replies.Select(r => r.RepliedByUserId));
                userIds.AddRange(ticket.Notes.Select(n => n.CreatedByUserId));
                userIds = userIds.Distinct().ToList();

                var userNames = userIds.Any()
                    ? await _userRepository.GetUserNamesAsync(userIds, cancellationToken)
                    : new Dictionary<Guid, string>();

                var dto = new SupportTicketDetailsDto
                {
                    Id = ticket.Id,
                    ReferenceId = ticket.ReferenceId,
                    Name = ticket.Name,
                    Email = ticket.Email.Value,
                    Subject = ticket.Subject,
                    Message = ticket.Message,
                    Status = ticket.Status.ToString(),
                    Priority = ticket.Priority.ToString(),
                    AssignedToUserId = ticket.AssignedToUserId,
                    AssignedToName = ticket.AssignedToUserId.HasValue && userNames.TryGetValue(ticket.AssignedToUserId.Value, out var name)
                        ? name
                        : null,
                    CreatedAt = ticket.CreatedAt,
                    UpdatedAt = ticket.UpdatedAt,
                    Replies = ticket.Replies.OrderBy(r => r.CreatedAt).Select(r => new SupportTicketReplyDto
                    {
                        Id = r.Id,
                        Content = r.Content,
                        AdminUserId = r.RepliedByUserId,
                        AdminUserName = userNames.TryGetValue(r.RepliedByUserId, out var rName) ? rName : "Unknown",
                        CreatedAt = r.CreatedAt
                    }).ToList(),
                    Notes = ticket.Notes.OrderBy(n => n.CreatedAt).Select(n => new SupportTicketNoteDto
                    {
                        Id = n.Id,
                        Content = n.Content,
                        AdminUserId = n.CreatedByUserId,
                        AdminUserName = userNames.TryGetValue(n.CreatedByUserId, out var nName) ? nName : "Unknown",
                        CreatedAt = n.CreatedAt
                    }).ToList()
                };

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetSupportTicketById COMPLETE: TicketId={TicketId}, ReferenceId={ReferenceId}, ReplyCount={ReplyCount}, NoteCount={NoteCount}, Duration={ElapsedMs}ms",
                    ticket.Id, ticket.ReferenceId, ticket.Replies.Count, ticket.Notes.Count, stopwatch.ElapsedMilliseconds);

                return Result<SupportTicketDetailsDto>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetSupportTicketById FAILED: TicketId={TicketId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.TicketId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
