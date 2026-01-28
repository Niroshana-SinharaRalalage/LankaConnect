using MediatR;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.Application.Support.Commands.CreateSupportTicket;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Public contact form endpoint
/// Phase 6A.90: Support/Feedback System - Contact form submission
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ContactController : BaseController<ContactController>
{
    public ContactController(IMediator mediator, ILogger<ContactController> logger)
        : base(mediator, logger)
    {
    }

    /// <summary>
    /// Submit a contact form / support request
    /// Phase 6A.90: Creates a support ticket and sends auto-confirmation email
    /// </summary>
    /// <remarks>
    /// This endpoint is public and does not require authentication.
    /// An auto-confirmation email will be sent to the provided email address.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ContactFormResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitContactForm([FromBody] ContactFormRequest request)
    {
        Logger.LogInformation(
            "Contact form submission - Email={Email}, Subject={Subject}",
            request.Email, request.Subject);

        var command = new CreateSupportTicketCommand
        {
            Name = request.Name,
            Email = request.Email,
            Subject = request.Subject,
            Message = request.Message
        };

        var result = await Mediator.Send(command);

        if (result.IsFailure)
        {
            return HandleResult(result);
        }

        return Ok(new ContactFormResponse
        {
            Success = true,
            Message = "Thank you for contacting us. We have received your message and will respond as soon as possible. A confirmation email has been sent to your email address."
        });
    }
}

/// <summary>
/// Request body for contact form submission
/// </summary>
public record ContactFormRequest
{
    /// <summary>
    /// Name of the person submitting the form
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Email address for replies
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Subject of the message
    /// </summary>
    public string Subject { get; init; } = string.Empty;

    /// <summary>
    /// Message content
    /// </summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Response for contact form submission
/// </summary>
public record ContactFormResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}
