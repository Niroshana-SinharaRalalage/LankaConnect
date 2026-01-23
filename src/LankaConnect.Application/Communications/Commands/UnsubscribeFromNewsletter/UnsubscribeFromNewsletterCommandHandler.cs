using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.UnsubscribeFromNewsletter;

/// <summary>
/// Handler for newsletter unsubscribe command
/// Phase 6A.71: Removes subscriber from database based on unsubscribe token
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
/// </summary>
public class UnsubscribeFromNewsletterCommandHandler : IRequestHandler<UnsubscribeFromNewsletterCommand, Result<bool>>
{
    private readonly INewsletterSubscriberRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnsubscribeFromNewsletterCommandHandler> _logger;

    public UnsubscribeFromNewsletterCommandHandler(
        INewsletterSubscriberRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<UnsubscribeFromNewsletterCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        UnsubscribeFromNewsletterCommand request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UnsubscribeFromNewsletter"))
        using (LogContext.PushProperty("EntityType", "NewsletterSubscriber"))
        using (LogContext.PushProperty("UnsubscribeToken", request.UnsubscribeToken?.Substring(0, Math.Min(8, request.UnsubscribeToken?.Length ?? 0))))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UnsubscribeFromNewsletter START: Token={TokenPreview}",
                request.UnsubscribeToken?.Substring(0, Math.Min(8, request.UnsubscribeToken?.Length ?? 0)));

            try
            {
                if (string.IsNullOrWhiteSpace(request.UnsubscribeToken))
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UnsubscribeFromNewsletter FAILED: Empty token - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure("Invalid unsubscribe token");
                }

                // Find subscriber by unsubscribe token
                var subscriber = await _repository.GetByUnsubscribeTokenAsync(request.UnsubscribeToken, cancellationToken);

                if (subscriber == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UnsubscribeFromNewsletter FAILED: Subscriber not found for token - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure("Invalid or expired unsubscribe link");
                }

                _logger.LogInformation(
                    "UnsubscribeFromNewsletter: Found subscriber - Email={Email}, SubscriberId={SubscriberId}, IsActive={IsActive}",
                    subscriber.Email.Value,
                    subscriber.Id,
                    subscriber.IsActive);

                // Remove subscriber from database
                // Phase 6A.64: Junction table entries will be CASCADE deleted automatically
                _repository.Remove(subscriber);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "UnsubscribeFromNewsletter COMPLETE: Email={Email}, SubscriberId={SubscriberId}, Duration={ElapsedMs}ms",
                    subscriber.Email.Value,
                    subscriber.Id,
                    stopwatch.ElapsedMilliseconds);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "UnsubscribeFromNewsletter FAILED: Unexpected error - Token={TokenPreview}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.UnsubscribeToken?.Substring(0, Math.Min(8, request.UnsubscribeToken?.Length ?? 0)),
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }
    }
}
