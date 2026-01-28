using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Support;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Support.Commands.CreateSupportTicket;

/// <summary>
/// Handler for CreateSupportTicketCommand
/// Phase 6A.90: Creates support ticket from contact form and sends auto-confirmation email
/// </summary>
public class CreateSupportTicketCommandHandler : ICommandHandler<CreateSupportTicketCommand, Guid>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IEmailService _emailService;
    private readonly IApplicationUrlsService _urlsService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateSupportTicketCommandHandler> _logger;

    public CreateSupportTicketCommandHandler(
        ISupportTicketRepository ticketRepository,
        IEmailService emailService,
        IApplicationUrlsService urlsService,
        IUnitOfWork unitOfWork,
        ILogger<CreateSupportTicketCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _emailService = emailService;
        _urlsService = urlsService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateSupportTicketCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CreateSupportTicket"))
        using (LogContext.PushProperty("EntityType", "SupportTicket"))
        using (LogContext.PushProperty("Email", request.Email))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CreateSupportTicket START: Email={Email}, Subject={Subject}",
                request.Email, request.Subject);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Create email value object
                var emailResult = Email.Create(request.Email);
                if (emailResult.IsFailure)
                {
                    _logger.LogWarning(
                        "CreateSupportTicket FAILED: Invalid email - Email={Email}, Error={Error}",
                        request.Email, emailResult.Error);
                    return Result<Guid>.Failure(emailResult.Error);
                }

                // Create support ticket
                var ticketResult = SupportTicket.Create(
                    request.Name,
                    emailResult.Value,
                    request.Subject,
                    request.Message);

                if (ticketResult.IsFailure)
                {
                    _logger.LogWarning(
                        "CreateSupportTicket FAILED: Domain validation failed - Email={Email}, Error={Error}",
                        request.Email, ticketResult.Error);
                    return Result<Guid>.Failure(ticketResult.Error);
                }

                var ticket = ticketResult.Value;

                _logger.LogInformation(
                    "CreateSupportTicket: Ticket created - TicketId={TicketId}, ReferenceId={ReferenceId}",
                    ticket.Id, ticket.ReferenceId);

                await _ticketRepository.AddAsync(ticket, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                // Send auto-confirmation email (fail-silent)
                await SendConfirmationEmailAsync(ticket, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "CreateSupportTicket COMPLETE: TicketId={TicketId}, ReferenceId={ReferenceId}, Duration={ElapsedMs}ms",
                    ticket.Id, ticket.ReferenceId, stopwatch.ElapsedMilliseconds);

                return Result<Guid>.Success(ticket.Id);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "CreateSupportTicket CANCELED: Email={Email}, Duration={ElapsedMs}ms",
                    request.Email, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "CreateSupportTicket FAILED: Email={Email}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.Email, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    private async Task SendConfirmationEmailAsync(SupportTicket ticket, CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new Dictionary<string, object>
            {
                { "Name", ticket.Name },
                { "ReferenceId", ticket.ReferenceId },
                { "Subject", ticket.Subject },
                { "SubmittedAt", ticket.CreatedAt.ToString("MMMM dd, yyyy h:mm tt") + " UTC" }
            };

            _logger.LogInformation(
                "[Phase 6A.90] Sending support ticket confirmation email to {Email}, ReferenceId={ReferenceId}",
                ticket.Email.Value, ticket.ReferenceId);

            var result = await _emailService.SendTemplatedEmailAsync(
                "template-support-ticket-confirmation",
                ticket.Email.Value,
                parameters,
                cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[Phase 6A.90] Support ticket confirmation email sent successfully to {Email}",
                    ticket.Email.Value);
            }
            else
            {
                _logger.LogWarning(
                    "[Phase 6A.90] Failed to send support ticket confirmation email to {Email}: {Errors}",
                    ticket.Email.Value, string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            // Fail-silent: Log error but don't throw
            _logger.LogError(ex,
                "[Phase 6A.90] Error sending support ticket confirmation email to {Email}",
                ticket.Email.Value);
        }
    }
}
