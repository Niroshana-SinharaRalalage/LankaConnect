using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Options;
using LankaConnect.Application.Contact.DTOs;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Handles contact form submissions.
/// Phase 6A.76: Contact Us Feature
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ContactController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ContactSettings _contactSettings;
    private readonly ILogger<ContactController> _logger;

    public ContactController(
        IEmailService emailService,
        IOptions<ContactSettings> contactSettings,
        ILogger<ContactController> logger)
    {
        _emailService = emailService;
        _contactSettings = contactSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Submit a contact form message.
    /// Sends email to configured admin recipient (not exposed to clients).
    /// </summary>
    /// <param name="request">Contact form data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Submission result with reference ID on success</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ContactSubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ContactSubmissionResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitContactRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Contact form submission received from {Email}", request.Email);

            // Validate model state
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Contact form validation failed for {Email}", request.Email);
                return BadRequest(ContactSubmissionResponse.FailureResponse(
                    "Please check your input and try again.",
                    "VALIDATION_ERROR"
                ));
            }

            // Generate unique reference ID for tracking
            var referenceId = GenerateReferenceId();

            // Build email content
            var subject = $"{_contactSettings.EmailSubjectPrefix} {request.Subject}";
            var htmlBody = BuildContactEmailHtml(request, referenceId);
            var plainTextBody = BuildContactEmailPlainText(request, referenceId);

            // Create email message DTO
            var emailMessage = new EmailMessageDto
            {
                ToEmail = _contactSettings.RecipientEmail,
                ToName = _contactSettings.RecipientName,
                Subject = subject,
                HtmlBody = htmlBody,
                PlainTextBody = plainTextBody,
                Headers = new Dictionary<string, string>
                {
                    { "Reply-To", request.Email }
                },
                Priority = 2 // Normal priority
            };

            _logger.LogDebug("Sending contact form email. Reference: {ReferenceId}, To: {Recipient}",
                referenceId, _contactSettings.RecipientEmail);

            // Send email
            var result = await _emailService.SendEmailAsync(emailMessage, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to send contact form email. Reference: {ReferenceId}, Error: {Error}",
                    referenceId, result.Error);

                return Ok(ContactSubmissionResponse.FailureResponse(
                    "We encountered an issue sending your message. Please try again later.",
                    "EMAIL_SEND_FAILED"
                ));
            }

            _logger.LogInformation("Contact form submitted successfully. Reference: {ReferenceId}, From: {Email}",
                referenceId, request.Email);

            return Ok(ContactSubmissionResponse.SuccessResponse(referenceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing contact form submission from {Email}", request.Email);

            return StatusCode(StatusCodes.Status500InternalServerError, ContactSubmissionResponse.FailureResponse(
                "An unexpected error occurred. Please try again later.",
                "INTERNAL_ERROR"
            ));
        }
    }

    /// <summary>
    /// Generates a unique reference ID for contact submissions.
    /// Format: CONTACT-YYYYMMDD-XXXXXXXX
    /// </summary>
    private static string GenerateReferenceId()
    {
        return $"CONTACT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
    }

    /// <summary>
    /// Builds the HTML email body for the contact form submission.
    /// </summary>
    private static string BuildContactEmailHtml(SubmitContactRequest request, string referenceId)
    {
        var escapedName = WebUtility.HtmlEncode(request.Name);
        var escapedSubject = WebUtility.HtmlEncode(request.Subject);
        var escapedMessage = WebUtility.HtmlEncode(request.Message).Replace("\n", "<br>");

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; }}
        .header {{ background: linear-gradient(to right, #ea580c, #be123c, #047857); color: white; padding: 24px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .content {{ padding: 24px; background: #ffffff; }}
        .field {{ margin-bottom: 20px; }}
        .label {{ font-weight: 600; color: #374151; font-size: 14px; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 6px; }}
        .value {{ padding: 12px 16px; background: #f9fafb; border-radius: 6px; border-left: 4px solid #ea580c; }}
        .message-value {{ padding: 16px; background: #f9fafb; border-radius: 6px; border-left: 4px solid #ea580c; white-space: pre-wrap; }}
        .reference {{ background: #fef3c7; padding: 16px; border-radius: 6px; margin-top: 24px; font-size: 14px; }}
        .reference strong {{ color: #92400e; }}
        .footer {{ text-align: center; padding: 20px; color: #6b7280; font-size: 12px; background: #f9fafb; }}
        a {{ color: #ea580c; text-decoration: none; }}
        a:hover {{ text-decoration: underline; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>New Contact Form Submission</h1>
        </div>
        <div class='content'>
            <div class='field'>
                <div class='label'>From</div>
                <div class='value'>{escapedName}</div>
            </div>
            <div class='field'>
                <div class='label'>Email</div>
                <div class='value'><a href='mailto:{request.Email}'>{request.Email}</a></div>
            </div>
            <div class='field'>
                <div class='label'>Subject</div>
                <div class='value'>{escapedSubject}</div>
            </div>
            <div class='field'>
                <div class='label'>Message</div>
                <div class='message-value'>{escapedMessage}</div>
            </div>
            <div class='reference'>
                <strong>Reference ID:</strong> {referenceId}<br>
                <strong>Submitted:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
            </div>
        </div>
        <div class='footer'>
            <p>This message was sent via the LankaConnect contact form.</p>
            <p>Reply directly to this email to respond to the sender.</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Builds the plain text email body for the contact form submission.
    /// </summary>
    private static string BuildContactEmailPlainText(SubmitContactRequest request, string referenceId)
    {
        return $@"
NEW CONTACT FORM SUBMISSION
============================

From: {request.Name}
Email: {request.Email}
Subject: {request.Subject}

Message:
---------
{request.Message}

-----------------------------
Reference ID: {referenceId}
Submitted: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC

This message was sent via the LankaConnect contact form.
Reply directly to this email to respond to the sender.
";
    }
}
