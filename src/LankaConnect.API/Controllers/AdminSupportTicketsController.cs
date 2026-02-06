using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.API.Extensions;
using LankaConnect.Application.Support.Commands.AddSupportTicketNote;
using LankaConnect.Application.Support.Commands.AssignSupportTicket;
using LankaConnect.Application.Support.Commands.CreateSupportTicket;
using LankaConnect.Application.Support.Commands.ReplySupportTicket;
using LankaConnect.Application.Support.Commands.UpdateSupportTicketStatus;
using LankaConnect.Application.Support.DTOs;
using LankaConnect.Application.Support.Queries.GetSupportTicketById;
using LankaConnect.Application.Support.Queries.GetSupportTicketsPaged;
using LankaConnect.Application.Support.Queries.GetSupportTicketStatistics;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Domain.Support.Enums;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Admin support ticket management endpoints
/// Phase 6A.90: Support/Feedback System
/// </summary>
[ApiController]
[Route("api/admin/support-tickets")]
[Produces("application/json")]
[Authorize(Policy = "RequireAdmin")]
public class AdminSupportTicketsController : BaseController<AdminSupportTicketsController>
{
    public AdminSupportTicketsController(IMediator mediator, ILogger<AdminSupportTicketsController> logger)
        : base(mediator, logger)
    {
    }

    /// <summary>
    /// Get paginated list of support tickets
    /// Phase 6A.90: Returns filtered support tickets for admin view
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<SupportTicketDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTickets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] SupportTicketStatus? status = null,
        [FromQuery] SupportTicketPriority? priority = null,
        [FromQuery] Guid? assignedTo = null,
        [FromQuery] bool? unassignedOnly = null)
    {
        Logger.LogInformation(
            "Admin {AdminUserId} retrieving support tickets - Page={Page}, Status={Status}, Priority={Priority}",
            User.TryGetUserId(), page, status, priority);

        var query = new GetSupportTicketsPagedQuery
        {
            Page = page,
            PageSize = pageSize,
            SearchTerm = search,
            StatusFilter = status,
            PriorityFilter = priority,
            AssignedToFilter = assignedTo,
            UnassignedOnly = unassignedOnly
        };

        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Get detailed support ticket by ID
    /// Phase 6A.90: Returns full ticket details with replies and notes
    /// </summary>
    [HttpGet("{ticketId:guid}")]
    [ProducesResponseType(typeof(SupportTicketDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTicketDetails(Guid ticketId)
    {
        Logger.LogInformation(
            "Admin {AdminUserId} retrieving support ticket details - TicketId={TicketId}",
            User.TryGetUserId(), ticketId);

        var query = new GetSupportTicketByIdQuery(ticketId);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Get support ticket statistics
    /// Phase 6A.90: Returns counts by status, priority, and unassigned
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(SupportTicketStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStatistics()
    {
        Logger.LogInformation(
            "Admin {AdminUserId} retrieving support ticket statistics",
            User.TryGetUserId());

        var query = new GetSupportTicketStatisticsQuery();
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Reply to a support ticket
    /// Phase 6A.90: Adds admin reply and sends email notification to submitter
    /// </summary>
    [HttpPost("{ticketId:guid}/reply")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReplyToTicket(Guid ticketId, [FromBody] ReplyToTicketRequest request)
    {
        var (ipAddress, userAgent) = GetClientInfo();

        Logger.LogInformation(
            "Admin {AdminUserId} replying to support ticket - TicketId={TicketId}",
            User.TryGetUserId(), ticketId);

        var command = new ReplySupportTicketCommand(ticketId, request.Content, ipAddress, userAgent);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Update support ticket status
    /// Phase 6A.90: Updates status with audit logging
    /// </summary>
    [HttpPost("{ticketId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStatus(Guid ticketId, [FromBody] UpdateTicketStatusRequest request)
    {
        var (ipAddress, userAgent) = GetClientInfo();

        Logger.LogInformation(
            "Admin {AdminUserId} updating support ticket status - TicketId={TicketId}, NewStatus={NewStatus}",
            User.TryGetUserId(), ticketId, request.Status);

        var command = new UpdateSupportTicketStatusCommand(ticketId, request.Status, ipAddress, userAgent);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Assign support ticket to admin user
    /// Phase 6A.90: Assigns ticket with audit logging
    /// </summary>
    [HttpPost("{ticketId:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignTicket(Guid ticketId, [FromBody] AssignTicketRequest request)
    {
        var (ipAddress, userAgent) = GetClientInfo();

        Logger.LogInformation(
            "Admin {AdminUserId} assigning support ticket - TicketId={TicketId}, AssignTo={AssignTo}",
            User.TryGetUserId(), ticketId, request.AssignToUserId);

        var command = new AssignSupportTicketCommand(ticketId, request.AssignToUserId, ipAddress, userAgent);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Add internal note to support ticket
    /// Phase 6A.90: Adds note (not visible to submitter) with audit logging
    /// </summary>
    [HttpPost("{ticketId:guid}/notes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddNote(Guid ticketId, [FromBody] AddNoteRequest request)
    {
        var (ipAddress, userAgent) = GetClientInfo();

        Logger.LogInformation(
            "Admin {AdminUserId} adding note to support ticket - TicketId={TicketId}",
            User.TryGetUserId(), ticketId);

        var command = new AddSupportTicketNoteCommand(ticketId, request.Content, ipAddress, userAgent);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    #region Private Helpers

    private (string? IpAddress, string? UserAgent) GetClientInfo()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            ipAddress = forwardedFor.ToString().Split(',')[0].Trim();
        }

        var userAgent = Request.Headers.UserAgent.ToString();
        if (userAgent.Length > 500)
        {
            userAgent = userAgent.Substring(0, 500);
        }

        return (ipAddress, userAgent);
    }

    #endregion
}

/// <summary>
/// Request body for replying to a support ticket
/// </summary>
public record ReplyToTicketRequest
{
    public string Content { get; init; } = string.Empty;
}

/// <summary>
/// Request body for updating ticket status
/// </summary>
public record UpdateTicketStatusRequest
{
    public SupportTicketStatus Status { get; init; }
}

/// <summary>
/// Request body for assigning a ticket
/// </summary>
public record AssignTicketRequest
{
    public Guid AssignToUserId { get; init; }
}

/// <summary>
/// Request body for adding an internal note
/// </summary>
public record AddNoteRequest
{
    public string Content { get; init; } = string.Empty;
}
