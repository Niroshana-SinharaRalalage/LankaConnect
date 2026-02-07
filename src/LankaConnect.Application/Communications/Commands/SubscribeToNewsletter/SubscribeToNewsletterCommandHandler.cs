using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using Microsoft.EntityFrameworkCore;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.SubscribeToNewsletter;

/// <summary>
/// Handler for newsletter subscription command
/// Phase 6A.64: Newsletter subscription with email confirmation
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
/// Phase 6A.100: Migrated to ITypedEmailService with NewsletterSubscriptionEmailParams
/// </summary>
public class SubscribeToNewsletterCommandHandler : IRequestHandler<SubscribeToNewsletterCommand, Result<SubscribeToNewsletterResponse>>
{
    private readonly INewsletterSubscriberRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITypedEmailService _typedEmailService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<SubscribeToNewsletterCommandHandler> _logger;
    private readonly IConfiguration _configuration;

    public SubscribeToNewsletterCommandHandler(
        INewsletterSubscriberRepository repository,
        IUnitOfWork unitOfWork,
        ITypedEmailService typedEmailService,
        IApplicationDbContext dbContext,
        ILogger<SubscribeToNewsletterCommandHandler> logger,
        IConfiguration configuration)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _typedEmailService = typedEmailService;
        _dbContext = dbContext;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Phase 6A.85 Part 3: Populate all metro areas when ReceiveAllLocations = true
    /// Same logic as CreateNewsletterCommandHandler to maintain architectural consistency
    /// </summary>
    private async Task<IEnumerable<Guid>> PopulateMetroAreasIfNeededAsync(
        List<Guid>? requestedMetroAreaIds,
        bool receiveAllLocations,
        CancellationToken cancellationToken)
    {
        // If user selected specific metros, use them as-is
        if (requestedMetroAreaIds != null && requestedMetroAreaIds.Any())
        {
            _logger.LogInformation(
                "[Phase 6A.85 Part 3] Subscriber selected {Count} specific metro area(s)",
                requestedMetroAreaIds.Count);
            return requestedMetroAreaIds;
        }

        // If ReceiveAllLocations = true, query all active metros from database
        if (receiveAllLocations)
        {
            _logger.LogInformation(
                "[Phase 6A.85 Part 3] Subscriber selected 'All Locations' - querying all active metro areas");

            try
            {
                var dbContext = _dbContext as DbContext
                    ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

                var allMetroAreaIds = await dbContext.Set<MetroArea>()
                    .Where(m => m.IsActive)
                    .Select(m => m.Id)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation(
                    "[Phase 6A.85 Part 3] Successfully populated {Count} metro areas for 'All Locations' subscriber",
                    allMetroAreaIds.Count);

                return allMetroAreaIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Phase 6A.85 Part 3] FAILED to query metro areas for 'All Locations' subscriber");
                throw;
            }
        }

        // Fallback: no metros selected (shouldn't happen due to validation)
        _logger.LogWarning(
            "[Phase 6A.85 Part 3] No metro areas specified and receiveAllLocations=false. Returning empty list.");
        return new List<Guid>();
    }

    public async Task<Result<SubscribeToNewsletterResponse>> Handle(
        SubscribeToNewsletterCommand request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SubscribeToNewsletter"))
        using (LogContext.PushProperty("EntityType", "NewsletterSubscriber"))
        using (LogContext.PushProperty("Email", request.Email))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "SubscribeToNewsletter START: Email={Email}, MetroAreaCount={MetroAreaCount}, ReceiveAll={ReceiveAll}",
                request.Email,
                request.MetroAreaIds?.Count ?? 0,
                request.ReceiveAllLocations);

            try
            {

                // Validate email
                var emailResult = Email.Create(request.Email);
                if (!emailResult.IsSuccess)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SubscribeToNewsletter FAILED: Email validation failed - Email={Email}, Error={Error}, Duration={ElapsedMs}ms",
                        request.Email,
                        emailResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<SubscribeToNewsletterResponse>.Failure($"Invalid email: {emailResult.Error}");
                }

                var email = emailResult.Value;

                _logger.LogInformation(
                    "SubscribeToNewsletter: Email validation passed - Email={Email}",
                    request.Email);

                // Check for existing subscriber
                var existingSubscriber = await _repository.GetByEmailAsync(request.Email, cancellationToken);

                _logger.LogInformation(
                    "SubscribeToNewsletter: Existing subscriber check - Email={Email}, Found={Found}",
                    request.Email,
                    existingSubscriber != null);

                NewsletterSubscriber subscriber;

                // Phase 5B: Handle multiple metro areas by using first metro area for backward compatibility
                // In future, this could be extended to create multiple subscriptions per metro area
                var metroAreaId = request.MetroAreaIds?.FirstOrDefault();

                if (existingSubscriber != null)
                {
                    // Phase 6A.99 Issue #57: Improved logic for handling existing subscribers
                    // Case 1: Already confirmed and active - return friendly message
                    if (existingSubscriber.IsActive && existingSubscriber.IsConfirmed)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "SubscribeToNewsletter FAILED: Email already confirmed and active - Email={Email}, SubscriberId={SubscriberId}, Duration={ElapsedMs}ms",
                            request.Email,
                            existingSubscriber.Id,
                            stopwatch.ElapsedMilliseconds);
                        return Result<SubscribeToNewsletterResponse>.Failure("Email is already subscribed to the newsletter");
                    }

                    // Case 2: Active but NOT confirmed - resend confirmation email
                    if (existingSubscriber.IsActive && !existingSubscriber.IsConfirmed)
                    {
                        _logger.LogInformation(
                            "SubscribeToNewsletter: Existing unconfirmed subscription found - Email={Email}, SubscriberId={SubscriberId}, ResendingConfirmation=true",
                            request.Email,
                            existingSubscriber.Id);

                        // Regenerate confirmation token using domain method
                        existingSubscriber.ResendConfirmation();

                        // Update metro areas if changed
                        var metroAreaIds = await PopulateMetroAreasIfNeededAsync(
                            request.MetroAreaIds,
                            request.ReceiveAllLocations,
                            cancellationToken);

                        // Save changes
                        await _unitOfWork.CommitAsync(cancellationToken);

                        _logger.LogInformation(
                            "SubscribeToNewsletter: Confirmation token regenerated - Email={Email}, SubscriberId={SubscriberId}",
                            request.Email,
                            existingSubscriber.Id);

                        // Send confirmation email with new token
                        await SendConfirmationEmailAsync(existingSubscriber, request.ReceiveAllLocations, request.MetroAreaIds, cancellationToken);

                        var resendResponse = new SubscribeToNewsletterResponse(
                            existingSubscriber.Id,
                            existingSubscriber.Email.Value,
                            existingSubscriber.MetroAreaIds.FirstOrDefault(),
                            request.MetroAreaIds,
                            existingSubscriber.ReceiveAllLocations,
                            existingSubscriber.IsActive,
                            existingSubscriber.IsConfirmed,
                            existingSubscriber.ConfirmationToken!,
                            existingSubscriber.ConfirmationSentAt!.Value);

                        stopwatch.Stop();
                        _logger.LogInformation(
                            "SubscribeToNewsletter COMPLETE (Resend): Confirmation email resent - Email={Email}, SubscriberId={SubscriberId}, Duration={ElapsedMs}ms",
                            request.Email,
                            existingSubscriber.Id,
                            stopwatch.ElapsedMilliseconds);

                        return Result<SubscribeToNewsletterResponse>.Success(resendResponse);
                    }

                    // Case 3: Inactive (previously unsubscribed) - create new subscription
                    _logger.LogInformation(
                        "SubscribeToNewsletter: Reactivating inactive subscriber - Email={Email}, SubscriberId={SubscriberId}",
                        request.Email,
                        existingSubscriber.Id);

                    // For inactive subscribers, create a new subscription instead of reactivating
                    // This ensures a fresh confirmation token and follows the domain model
                    // Phase 6A.85 Part 3: Populate all metros when ReceiveAllLocations = true
                    var reactivateMetroAreaIds = await PopulateMetroAreasIfNeededAsync(
                        request.MetroAreaIds,
                        request.ReceiveAllLocations,
                        cancellationToken);

                    var reactivateResult = NewsletterSubscriber.Create(
                        email,
                        reactivateMetroAreaIds.ToList(),
                        request.ReceiveAllLocations);

                    if (!reactivateResult.IsSuccess)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "SubscribeToNewsletter FAILED: Reactivation create failed - Email={Email}, Error={Error}, Duration={ElapsedMs}ms",
                            request.Email,
                            reactivateResult.Error,
                            stopwatch.ElapsedMilliseconds);
                        return Result<SubscribeToNewsletterResponse>.Failure(reactivateResult.Error);
                    }

                    subscriber = reactivateResult.Value;

                    // Remove old subscription
                    _repository.Remove(existingSubscriber);
                    await _repository.AddAsync(subscriber, cancellationToken);

                    _logger.LogInformation(
                        "SubscribeToNewsletter: Reactivated newsletter subscription - Email={Email}, SubscriberId={SubscriberId}, MetroAreaCount={MetroAreaCount}",
                        request.Email,
                        subscriber.Id,
                        subscriber.MetroAreaIds.Count);
                }
                else
                {
                    // Create new subscriber
                    _logger.LogInformation(
                        "SubscribeToNewsletter: Creating new subscriber - Email={Email}",
                        request.Email);

                    // Phase 6A.85 Part 3: Populate all metros when ReceiveAllLocations = true
                    var metroAreaIds = await PopulateMetroAreasIfNeededAsync(
                        request.MetroAreaIds,
                        request.ReceiveAllLocations,
                        cancellationToken);

                    var createResult = NewsletterSubscriber.Create(
                        email,
                        metroAreaIds.ToList(),
                        request.ReceiveAllLocations);

                    if (!createResult.IsSuccess)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "SubscribeToNewsletter FAILED: Create failed - Email={Email}, Error={Error}, Duration={ElapsedMs}ms",
                            request.Email,
                            createResult.Error,
                            stopwatch.ElapsedMilliseconds);
                        return Result<SubscribeToNewsletterResponse>.Failure(createResult.Error);
                    }

                    subscriber = createResult.Value;

                    _logger.LogInformation(
                        "SubscribeToNewsletter: Domain entity created - Email={Email}, SubscriberId={SubscriberId}, MetroAreaCount={MetroAreaCount}",
                        request.Email,
                        subscriber.Id,
                        subscriber.MetroAreaIds.Count);

                    await _repository.AddAsync(subscriber, cancellationToken);
                }

                // Save changes
                await _unitOfWork.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "SubscribeToNewsletter: Database commit successful - Email={Email}, SubscriberId={SubscriberId}",
                    request.Email,
                    subscriber.Id);

                // Phase 6A.64: Insert junction table entries AFTER subscriber is saved
                // This avoids FK constraint violation (subscriber_id must exist in newsletter_subscribers table first)
                await _repository.InsertPendingJunctionEntriesAsync(cancellationToken);

                _logger.LogInformation(
                    "SubscribeToNewsletter: Junction table entries inserted - Email={Email}, SubscriberId={SubscriberId}",
                    request.Email,
                    subscriber.Id);

                // Send confirmation email
                var metroAreaDescription = request.ReceiveAllLocations
                    ? "All Locations"
                    : (request.MetroAreaIds?.Count > 0 ? $"{request.MetroAreaIds.Count} Location(s)" : "All Locations");

                // Phase 6A.71: Build confirmation and unsubscribe URLs from configuration
                var apiBaseUrl = _configuration["ApplicationUrls:ApiBaseUrl"] ?? "https://lankaconnect.com";
                var confirmPath = _configuration["ApplicationUrls:NewsletterConfirmPath"] ?? "/newsletter/confirm";
                var unsubscribePath = _configuration["ApplicationUrls:NewsletterUnsubscribePath"] ?? "/newsletter/unsubscribe";

                // Phase 6A.100: Use typed email params instead of dictionary
                var emailParams = NewsletterSubscriptionEmailParams.Create(
                    email: request.Email,
                    confirmationToken: subscriber.ConfirmationToken!,
                    confirmationUrl: $"{apiBaseUrl}{confirmPath}?token={subscriber.ConfirmationToken}",
                    unsubscribeUrl: $"{apiBaseUrl}{unsubscribePath}?token={subscriber.UnsubscribeToken}",
                    metroAreasText: metroAreaDescription);

                var sendEmailResult = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);

                if (!sendEmailResult.Success)
                {
                    _logger.LogWarning(
                        "[Phase 6A.100] SubscribeToNewsletter: Failed to send confirmation email - Email={Email}, Errors={Errors}. Subscription saved but email not sent.",
                        request.Email,
                        string.Join(", ", sendEmailResult.Errors));
                    // Don't fail the subscription - email sending is non-critical for testing/staging
                    // In production, email service should be properly configured
                }
                else
                {
                    _logger.LogInformation(
                        "[Phase 6A.100] SubscribeToNewsletter: Newsletter confirmation email sent - Email={Email}",
                        request.Email);
                }

                var response = new SubscribeToNewsletterResponse(
                    subscriber.Id,
                    subscriber.Email.Value,
                    subscriber.MetroAreaIds.FirstOrDefault(),  // Phase 6A.64: Use first metro area for backward compatibility
                    request.MetroAreaIds,
                    subscriber.ReceiveAllLocations,
                    subscriber.IsActive,
                    subscriber.IsConfirmed,
                    subscriber.ConfirmationToken!,
                    subscriber.ConfirmationSentAt!.Value);

                stopwatch.Stop();
                _logger.LogInformation(
                    "SubscribeToNewsletter COMPLETE: Email={Email}, SubscriberId={SubscriberId}, MetroAreaCount={MetroAreaCount}, Duration={ElapsedMs}ms",
                    request.Email,
                    subscriber.Id,
                    subscriber.MetroAreaIds.Count,
                    stopwatch.ElapsedMilliseconds);

                return Result<SubscribeToNewsletterResponse>.Success(response);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "SubscribeToNewsletter FAILED: Unexpected error - Email={Email}, MetroAreaCount={MetroAreaCount}, ReceiveAll={ReceiveAll}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.Email,
                    request.MetroAreaIds?.Count ?? 0,
                    request.ReceiveAllLocations,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }
    }

    /// <summary>
    /// Phase 6A.99: Helper method to send confirmation email for new or re-confirmed subscriptions.
    /// Phase 6A.100: Migrated to ITypedEmailService with NewsletterSubscriptionEmailParams.
    /// Extracted to support both new subscriptions and resending confirmations.
    /// </summary>
    private async Task SendConfirmationEmailAsync(
        NewsletterSubscriber subscriber,
        bool receiveAllLocations,
        List<Guid>? metroAreaIds,
        CancellationToken cancellationToken)
    {
        try
        {
            var metroAreaDescription = receiveAllLocations
                ? "All Locations"
                : (metroAreaIds?.Count > 0 ? $"{metroAreaIds.Count} Location(s)" : "All Locations");

            var apiBaseUrl = _configuration["ApplicationUrls:ApiBaseUrl"] ?? "https://lankaconnect.com";
            var confirmPath = _configuration["ApplicationUrls:NewsletterConfirmPath"] ?? "/newsletter/confirm";
            var unsubscribePath = _configuration["ApplicationUrls:NewsletterUnsubscribePath"] ?? "/newsletter/unsubscribe";

            // Phase 6A.100: Use typed email params instead of dictionary
            var emailParams = NewsletterSubscriptionEmailParams.Create(
                email: subscriber.Email.Value,
                confirmationToken: subscriber.ConfirmationToken!,
                confirmationUrl: $"{apiBaseUrl}{confirmPath}?token={subscriber.ConfirmationToken}",
                unsubscribeUrl: $"{apiBaseUrl}{unsubscribePath}?token={subscriber.UnsubscribeToken}",
                metroAreasText: metroAreaDescription);

            var sendEmailResult = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);

            if (!sendEmailResult.Success)
            {
                _logger.LogWarning(
                    "[Phase 6A.100] SendConfirmationEmail: Failed to send confirmation email - Email={Email}, Errors={Errors}",
                    subscriber.Email.Value,
                    string.Join(", ", sendEmailResult.Errors));
            }
            else
            {
                _logger.LogInformation(
                    "[Phase 6A.100] SendConfirmationEmail: Newsletter confirmation email sent - Email={Email}, SubscriberId={SubscriberId}",
                    subscriber.Email.Value,
                    subscriber.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.100] SendConfirmationEmail FAILED: Exception sending confirmation email - Email={Email}, SubscriberId={SubscriberId}",
                subscriber.Email.Value,
                subscriber.Id);
            // Don't throw - email sending failure shouldn't break the subscription flow
        }
    }
}
