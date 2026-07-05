using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.API.Extensions;
using LankaConnect.Modules.Communications.Application.WhatsApp.Commands.EnableWhatsApp;
using LankaConnect.Modules.Communications.Application.WhatsApp.Commands.DisableWhatsApp;
using LankaConnect.Modules.Communications.Application.WhatsApp.Commands.RequestPhoneVerification;
using LankaConnect.Modules.Communications.Application.WhatsApp.Commands.VerifyWhatsAppPhone;
using LankaConnect.Modules.Communications.Application.WhatsApp.Commands.UpdateWhatsAppPreferences;
using LankaConnect.Modules.Communications.Application.WhatsApp.Queries.GetWhatsAppPreferences;
namespace LankaConnect.Modules.Communications.Api.Controllers;

/// <summary>
/// Phase 7A: WhatsApp user preferences and phone verification.
/// Allows authenticated users to manage their WhatsApp notification settings,
/// verify phone numbers, and control notification preferences.
/// </summary>
[Authorize]
[Route("api/whatsapp")]
[Produces("application/json")]
public class WhatsAppController : BaseController<WhatsAppController>
{
    public WhatsAppController(IMediator mediator, ILogger<WhatsAppController> logger)
        : base(mediator, logger)
    {
    }

    /// <summary>
    /// Get user's WhatsApp notification preferences.
    /// Returns current preferences or default values if not yet configured.
    /// </summary>
    /// <returns>The user's WhatsApp preferences</returns>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(WhatsAppPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPreferences()
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "[Phase 7A] GET WhatsApp preferences: UserId={UserId}",
            userId);

        var result = await Mediator.Send(new GetWhatsAppPreferencesQuery(userId));
        return HandleResult(result);
    }

    /// <summary>
    /// Enable WhatsApp notifications with a phone number.
    /// Initiates phone verification flow if number is not yet verified.
    /// </summary>
    /// <param name="request">The phone number to register for WhatsApp notifications</param>
    /// <returns>Enable response with verification status</returns>
    [HttpPost("enable")]
    [ProducesResponseType(typeof(EnableWhatsAppResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Enable([FromBody] EnableWhatsAppRequest request)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "[Phase 7A] Enable WhatsApp: UserId={UserId}",
            userId);

        var result = await Mediator.Send(new EnableWhatsAppCommand(userId, request.PhoneNumber));
        return HandleResult(result);
    }

    /// <summary>
    /// Disable WhatsApp notifications for the current user.
    /// Preserves phone number and preferences for re-enablement.
    /// </summary>
    [HttpPost("disable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Disable()
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "[Phase 7A] Disable WhatsApp: UserId={UserId}",
            userId);

        var result = await Mediator.Send(new DisableWhatsAppCommand(userId));
        return HandleResult(result);
    }

    /// <summary>
    /// Request a phone verification code via SMS.
    /// User must have WhatsApp enabled with a phone number before requesting verification.
    /// Rate limited to prevent abuse.
    /// </summary>
    [HttpPost("verify/request")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RequestVerification()
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "[Phase 7A] Request phone verification: UserId={UserId}",
            userId);

        var result = await Mediator.Send(new RequestPhoneVerificationCommand(userId));
        return HandleResult(result);
    }

    /// <summary>
    /// Confirm phone verification with a 6-digit code.
    /// Completes the phone verification flow and enables WhatsApp messaging.
    /// </summary>
    /// <param name="request">The 6-digit verification code</param>
    /// <returns>Verification result with updated status</returns>
    [HttpPost("verify/confirm")]
    [ProducesResponseType(typeof(VerifyWhatsAppPhoneResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ConfirmVerification([FromBody] VerifyPhoneRequest request)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "[Phase 7A] Confirm phone verification: UserId={UserId}",
            userId);

        var result = await Mediator.Send(new VerifyWhatsAppPhoneCommand(userId, request.Code));
        return HandleResult(result);
    }

    /// <summary>
    /// Update WhatsApp notification preferences.
    /// Controls which notification types are sent, language, and quiet hours.
    /// </summary>
    /// <param name="request">Updated preference settings</param>
    [HttpPut("preferences")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest request)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "[Phase 7A] Update WhatsApp preferences: UserId={UserId}",
            userId);

        var result = await Mediator.Send(new UpdateWhatsAppPreferencesCommand(
            userId,
            request.EventRegistration,
            request.EventReminder,
            request.EventCancellation,
            request.EventUpdate,
            request.SignupCommitment,
            request.Refund,
            request.Newsletter,
            request.NewEvent,
            request.Payment,
            request.PreferredLanguage,
            request.QuietHoursStart,
            request.QuietHoursEnd,
            request.RespectCulturalTiming));
        return HandleResult(result);
    }
}

// ----- Request DTOs (separate from CQRS commands) -----

/// <summary>
/// Request to enable WhatsApp notifications with a phone number.
/// </summary>
/// <param name="PhoneNumber">Phone number in E.164 format (e.g., +94771234567)</param>
public record EnableWhatsAppRequest(string PhoneNumber);

/// <summary>
/// Request to verify a phone number with a 6-digit code.
/// </summary>
/// <param name="Code">The 6-digit verification code sent via SMS</param>
public record VerifyPhoneRequest(string Code);

/// <summary>
/// Request to update WhatsApp notification preferences.
/// All boolean fields default to true (opt-out model).
/// </summary>
public record UpdatePreferencesRequest(
    bool EventRegistration = true,
    bool EventReminder = true,
    bool EventCancellation = true,
    bool EventUpdate = true,
    bool SignupCommitment = true,
    bool Refund = true,
    bool Newsletter = true,
    bool NewEvent = true,
    bool Payment = true,
    string? PreferredLanguage = null,
    TimeOnly? QuietHoursStart = null,
    TimeOnly? QuietHoursEnd = null,
    bool RespectCulturalTiming = true);
