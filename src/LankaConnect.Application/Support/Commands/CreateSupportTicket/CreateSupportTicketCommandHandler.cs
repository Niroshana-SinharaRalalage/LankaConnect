using System.Diagnostics;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Support;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Support.Commands.CreateSupportTicket;

/// <summary>
/// Handler for CreateSupportTicketCommand
/// Phase 6A.90: Creates support ticket from contact form and sends auto-confirmation email
/// Phase 6A.87: Migrated to ITypedEmailService for hybrid email support
/// </summary>
public class CreateSupportTicketCommandHandler : ICommandHandler<CreateSupportTicketCommand, Guid>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IEmailService _emailService;
    private readonly ITypedEmailService _typedEmailService;  // Phase 6A.87: Typed email service
    private readonly IApplicationUrlsService _urlsService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateSupportTicketCommandHandler> _logger;

    private const string HandlerName = nameof(CreateSupportTicketCommandHandler);  // Phase 6A.87: For feature flag lookup

    public CreateSupportTicketCommandHandler(
        ISupportTicketRepository ticketRepository,
        IEmailService emailService,
        ITypedEmailService typedEmailService,  // Phase 6A.87: Typed email service
        IApplicationUrlsService urlsService,
        IUnitOfWork unitOfWork,
        ILogger<CreateSupportTicketCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _emailService = emailService;
        _typedEmailService = typedEmailService;  // Phase 6A.87: Typed email service
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
            // Phase 6A.87: Use typed email parameters for compile-time safety
            var emailParams = SupportTicketEmailParams.CreateConfirmation(
                userId: Guid.Empty,  // Contact form tickets may not have user IDs
                userName: ticket.Name,
                userEmail: ticket.Email.Value,
                ticketId: ticket.Id,
                ticketNumber: ticket.ReferenceId,
                subject: ticket.Subject,
                category: "General",
                priority: "Medium",
                message: ticket.Message,
                createdAt: ticket.CreatedAt,
                ticketUrl: ""  // Will be set if we have a ticket portal
            );

            _logger.LogInformation(
                "[Phase 6A.87] Sending support ticket confirmation email to {Email}, ReferenceId={ReferenceId}",
                ticket.Email.Value, ticket.ReferenceId);

            // Phase 6A.87: Send via typed email service (feature flags handled internally)
            var typedResult = await _typedEmailService.SendEmailAsync(
                emailParams,
                HandlerName,
                cancellationToken);

            if (typedResult.Success)
            {
                _logger.LogInformation(
                    "[Phase 6A.87] Support ticket confirmation email sent successfully to {Email}, UsedTyped={UsedTyped}",
                    ticket.Email.Value, typedResult.UsedTypedParameters);
            }
            else
            {
                _logger.LogWarning(
                    "[Phase 6A.87] Failed to send support ticket confirmation email to {Email}: {Errors}",
                    ticket.Email.Value, string.Join(", ", typedResult.Errors));
            }
        }
        catch (Exception ex)
        {
            // Fail-silent: Log error but don't throw
            _logger.LogError(ex,
                "[Phase 6A.87] Error sending support ticket confirmation email to {Email}",
                ticket.Email.Value);
        }
    }
}
