using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.API.Extensions;
using LankaConnect.Modules.Communications.Application.WhatsApp.Commands.SendTestWhatsApp;
using LankaConnect.Modules.Communications.Application.WhatsApp.Queries.GetWhatsAppMetrics;
using LankaConnect.Modules.Communications.Application.WhatsApp.Queries.GetWhatsAppTemplates;
using LankaConnect.Modules.Communications.Application.WhatsApp.Queries.GetWhatsAppMessageHistory;
namespace LankaConnect.Modules.Communications.Api.Controllers;

/// <summary>
/// Phase 7A: Admin-only endpoints for WhatsApp management.
/// Provides metrics, template management, test messaging, and message history.
/// </summary>
[ApiController]
[Route("api/whatsapp-admin")]
[Produces("application/json")]
[Authorize(Policy = "RequireAdmin")]
public class WhatsAppAdminController : BaseController<WhatsAppAdminController>
{
    public WhatsAppAdminController(IMediator mediator, ILogger<WhatsAppAdminController> logger)
        : base(mediator, logger)
    {
    }

    /// <summary>
    /// Get WhatsApp messaging metrics for a date range.
    /// Returns delivery rates, failure counts, and usage statistics.
    /// </summary>
    /// <param name="from">Start date (inclusive, defaults to 30 days ago)</param>
    /// <param name="to">End date (inclusive, defaults to today)</param>
    /// <returns>WhatsApp messaging metrics</returns>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(WhatsAppMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMetrics(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null)
    {
        var adminUserId = User.TryGetUserId();

        Logger.LogInformation(
            "[Phase 7A] Admin {AdminUserId} retrieving WhatsApp metrics - From={From}, To={To}",
            adminUserId, from, to);

        var effectiveFrom = (from ?? DateTimeOffset.UtcNow.AddDays(-30)).UtcDateTime;
        var effectiveTo = (to ?? DateTimeOffset.UtcNow).UtcDateTime;

        var result = await Mediator.Send(new GetWhatsAppMetricsQuery(effectiveFrom, effectiveTo));
        return HandleResult(result);
    }

    /// <summary>
    /// Get all registered WhatsApp message templates.
    /// Returns template names, statuses, and last-used timestamps.
    /// </summary>
    /// <returns>List of WhatsApp templates</returns>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(IReadOnlyList<WhatsAppTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTemplates()
    {
        var adminUserId = User.TryGetUserId();

        Logger.LogInformation(
            "[Phase 7A] Admin {AdminUserId} retrieving WhatsApp templates",
            adminUserId);

        var result = await Mediator.Send(new GetWhatsAppTemplatesQuery());
        return HandleResult(result);
    }

    /// <summary>
    /// Send a test WhatsApp message for template verification.
    /// Only available to admins for development and QA purposes.
    /// </summary>
    /// <param name="request">Test message details including recipient and template</param>
    [HttpPost("test-message")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SendTestMessage([FromBody] SendTestWhatsAppRequest request)
    {
        var adminUserId = User.GetUserId();

        Logger.LogInformation(
            "[Phase 7A] Admin {AdminUserId} sending test WhatsApp message - TemplateName={TemplateName}, RecipientPhone={RecipientPhone}",
            adminUserId, request.TemplateName, request.RecipientPhone);

        var result = await Mediator.Send(new SendTestWhatsAppCommand(
            adminUserId,
            request.RecipientPhone,
            request.TemplateName,
            request.Parameters ?? new Dictionary<string, string>()));
        return HandleResult(result);
    }

    /// <summary>
    /// Get WhatsApp message history with optional filtering.
    /// Returns sent messages with delivery status and timestamps.
    /// </summary>
    /// <param name="userId">Optional: filter by recipient user ID</param>
    /// <param name="eventId">Optional: filter by related event ID</param>
    /// <param name="status">Optional: filter by delivery status (Sent, Delivered, Read, Failed)</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <returns>Paginated message history</returns>
    [HttpGet("messages")]
    [ProducesResponseType(typeof(IReadOnlyList<WhatsAppMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMessageHistory(
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? eventId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var adminUserId = User.TryGetUserId();

        Logger.LogInformation(
            "[Phase 7A] Admin {AdminUserId} retrieving WhatsApp message history - UserId={FilterUserId}, EventId={EventId}, Status={Status}, Page={Page}",
            adminUserId, userId, eventId, status, page);

        // Clamp pageSize to prevent excessive queries
        var clampedPageSize = Math.Min(Math.Max(pageSize, 1), 100);

        var result = await Mediator.Send(new GetWhatsAppMessageHistoryQuery(
            userId,
            eventId,
            page,
            clampedPageSize));
        return HandleResult(result);
    }
}

// ----- Request DTOs -----

/// <summary>
/// Request to send a test WhatsApp message.
/// </summary>
/// <param name="RecipientPhone">Recipient phone number in E.164 format</param>
/// <param name="TemplateName">WhatsApp template name to use</param>
/// <param name="Parameters">Optional template parameters as key-value pairs</param>
public record SendTestWhatsAppRequest(
    string RecipientPhone,
    string TemplateName,
    Dictionary<string, string>? Parameters = null);
