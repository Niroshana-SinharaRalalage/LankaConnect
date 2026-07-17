using System.Diagnostics;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Modules.Forms.Infrastructure.Data;
using System.Security.Cryptography;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Forms.Application.Commands.DeleteFormResponse;

/// <summary>
/// Handles deletion of form responses with priority-based authorization.
/// Phase 6A.106: Implements architect-approved security model.
///
/// SECURITY MODEL (PRIORITY ORDER):
/// 1. If response belongs to logged-in user → ONLY userId can delete (token ignored)
/// 2. If response is anonymous → ONLY valid token can delete
///
/// This prevents cross-user deletion attacks where an attacker with a valid token
/// tries to delete a logged-in user's response.
/// </summary>
public class DeleteFormResponseCommandHandler : ICommandHandler<DeleteFormResponseCommand>
{
    private readonly IFormResponseRepository _formResponseRepository;
    private readonly FormsDbContext _formsContext;
    private readonly ILogger<DeleteFormResponseCommandHandler> _logger;

    public DeleteFormResponseCommandHandler(
        IFormResponseRepository formResponseRepository,
        FormsDbContext formsContext,
        ILogger<DeleteFormResponseCommandHandler> logger)
    {
        _formResponseRepository = formResponseRepository;
        _formsContext = formsContext;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteFormResponseCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "DeleteFormResponse"))
        using (LogContext.PushProperty("EntityType", "FormResponse"))
        using (LogContext.PushProperty("ResponseId", request.ResponseId))
        using (LogContext.PushProperty("FormId", request.FormId))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "DeleteFormResponse START: ResponseId={ResponseId}, FormId={FormId}, EventId={EventId}, HasToken={HasToken}, RequestingUserId={RequestingUserId}",
                request.ResponseId, request.FormId, request.EventId,
                !string.IsNullOrEmpty(request.AccessToken), request.RequestingUserId ?? Guid.Empty);

            try
            {
                // Load response with answers (for cascade delete verification)
                var response = await _formResponseRepository.GetByIdWithAnswersAsync(request.ResponseId, cancellationToken);

                // ARCHITECT FIX: Handle concurrent delete race condition
                if (response == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteFormResponse FAILED: Response not found or already deleted - ResponseId={ResponseId}, Duration={ElapsedMs}ms",
                        request.ResponseId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Response not found or already deleted");
                }

                // Verify form/event ownership
                if (response.EventFormId != request.FormId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteFormResponse FAILED: Response does not belong to form - ResponseId={ResponseId}, FormId={FormId}, ActualFormId={ActualFormId}, Duration={ElapsedMs}ms",
                        request.ResponseId, request.FormId, response.EventFormId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Response does not belong to the specified form");
                }

                if (response.EventId != request.EventId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteFormResponse FAILED: Response does not belong to event - ResponseId={ResponseId}, EventId={EventId}, ActualEventId={ActualEventId}, Duration={ElapsedMs}ms",
                        request.ResponseId, request.EventId, response.EventId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("Response does not belong to the specified event");
                }

                // ARCHITECT FIX: Priority-based authorization (CRITICAL FOR SECURITY)
                // PRIORITY ORDER:
                // 1. If response has RespondentUserId (logged-in user) → ONLY userId can delete (ignore token)
                // 2. If response is anonymous (no userId) → ONLY token can delete

                if (response.RespondentUserId.HasValue)
                {
                    // This response was submitted by a logged-in user
                    // ONLY that user can delete it (token auth doesn't apply)
                    if (request.RequestingUserId == null || request.RequestingUserId != response.RespondentUserId)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "DeleteFormResponse FAILED: Unauthorized - Response belongs to user {OwnerId}, requester {RequesterId}, Duration={ElapsedMs}ms",
                            response.RespondentUserId, request.RequestingUserId ?? Guid.Empty, stopwatch.ElapsedMilliseconds);
                        return Result.Failure("You are not authorized to delete this response");
                    }

                    _logger.LogInformation(
                        "DeleteFormResponse AUTH: Logged-in user authorization successful - UserId={UserId}",
                        request.RequestingUserId);
                }
                else
                {
                    // This response was submitted anonymously
                    // ONLY valid access token can delete it
                    if (string.IsNullOrEmpty(request.AccessToken))
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "DeleteFormResponse FAILED: Unauthorized - Anonymous response requires access token, Duration={ElapsedMs}ms",
                            stopwatch.ElapsedMilliseconds);
                        return Result.Failure("Access token is required to delete this response");
                    }

                    var tokenHash = ComputeSha256Hash(request.AccessToken);
                    if (tokenHash != response.AccessTokenHash)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "DeleteFormResponse FAILED: Unauthorized - Invalid access token, Duration={ElapsedMs}ms",
                            stopwatch.ElapsedMilliseconds);
                        return Result.Failure("Invalid access token");
                    }

                    _logger.LogInformation(
                        "DeleteFormResponse AUTH: Anonymous token authorization successful - ResponseId={ResponseId}",
                        request.ResponseId);
                }

                // Raise domain event BEFORE deletion (handler needs access to properties)
                var raiseEventResult = response.RaiseDeletedEvent();
                if (raiseEventResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DeleteFormResponse FAILED: Could not raise deleted event - ResponseId={ResponseId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.ResponseId, raiseEventResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result.Failure(raiseEventResult.Error);
                }

                // Hard delete (GDPR compliant - users control their data)
                await _formResponseRepository.DeleteAsync(response, cancellationToken);

                // Wave 8.5.h (D-01): direct-SaveChanges per Consult #25 Q6.
                // Cascade delete removes FormAnswers via EF Core configuration.
                await _formsContext.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "DeleteFormResponse COMPLETE: ResponseId={ResponseId}, FormId={FormId}, EventId={EventId}, AnswerCount={AnswerCount}, Duration={ElapsedMs}ms",
                    request.ResponseId, request.FormId, request.EventId, response.Answers.Count, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "DeleteFormResponse FAILED: Exception - ResponseId={ResponseId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.ResponseId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
