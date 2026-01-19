using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.CancelOpenSignUpItem;

/// <summary>
/// Phase 6A.27: Handler for canceling (deleting) a user-submitted Open item
/// </summary>
public class CancelOpenSignUpItemCommandHandler : ICommandHandler<CancelOpenSignUpItemCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelOpenSignUpItemCommandHandler> _logger;

    public CancelOpenSignUpItemCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<CancelOpenSignUpItemCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(CancelOpenSignUpItemCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CancelOpenSignUpItem"))
        using (LogContext.PushProperty("EntityType", "SignUpItem"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("SignUpListId", request.SignUpListId))
        using (LogContext.PushProperty("ItemId", request.ItemId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug(
                "CancelOpenSignUpItem START: EventId={EventId}, SignUpListId={SignUpListId}, ItemId={ItemId}, UserId={UserId}",
                request.EventId, request.SignUpListId, request.ItemId, request.UserId);

            try
            {
                // Get the event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CancelOpenSignUpItem FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Event with ID {request.EventId} not found");
                }

                _logger.LogInformation(
                    "CancelOpenSignUpItem: Event loaded - EventId={EventId}, Title={Title}",
                    @event.Id, @event.Title.Value);

                // Get the sign-up list from the event
                var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
                if (signUpList == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CancelOpenSignUpItem FAILED: Sign-up list not found - EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Sign-up list with ID {request.SignUpListId} not found");
                }

                _logger.LogInformation(
                    "CancelOpenSignUpItem: Sign-up list loaded - SignUpListId={SignUpListId}, Category={Category}",
                    signUpList.Id, signUpList.Category);

                // Get the item
                var item = signUpList.GetItem(request.ItemId);
                if (item == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CancelOpenSignUpItem FAILED: Sign-up item not found - EventId={EventId}, SignUpListId={SignUpListId}, ItemId={ItemId}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, request.ItemId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Sign-up item with ID {request.ItemId} not found");
                }

                _logger.LogInformation(
                    "CancelOpenSignUpItem: Sign-up item loaded - ItemId={ItemId}, ItemCategory={ItemCategory}, CommitmentsCount={CommitmentsCount}",
                    item.Id, item.ItemCategory, item.Commitments.Count);

                // Verify this is an Open item created by this user
                if (item.ItemCategory != SignUpItemCategory.Open)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CancelOpenSignUpItem FAILED: Not an Open item - ItemId={ItemId}, ItemCategory={ItemCategory}, Duration={ElapsedMs}ms",
                        request.ItemId, item.ItemCategory, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Only Open items can be canceled using this endpoint");
                }

                if (!item.IsCreatedByUser(request.UserId))
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CancelOpenSignUpItem FAILED: User not authorized - ItemId={ItemId}, UserId={UserId}, Duration={ElapsedMs}ms",
                        request.ItemId, request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("You can only cancel Open items that you created");
                }

                // Phase 6A.28 Issue 3 Fix: Cancel user's own commitment first (if exists)
                var userCommitment = item.Commitments.FirstOrDefault(c => c.UserId == request.UserId);
                if (userCommitment != null)
                {
                    _logger.LogInformation(
                        "CancelOpenSignUpItem: Canceling user's own commitment - CommitmentId={CommitmentId}",
                        userCommitment.Id);

                    var cancelCommitResult = item.CancelCommitment(request.UserId);
                    if (cancelCommitResult.IsFailure)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "CancelOpenSignUpItem FAILED: Commitment cancellation failed - CommitmentId={CommitmentId}, Error={Error}, Duration={ElapsedMs}ms",
                            userCommitment.Id, cancelCommitResult.Error, stopwatch.ElapsedMilliseconds);

                        return cancelCommitResult;
                    }

                    _logger.LogInformation(
                        "CancelOpenSignUpItem: User commitment canceled - CommitmentId={CommitmentId}",
                        userCommitment.Id);
                }

                // Phase 6A.28 Issue 3 Fix: Check if there are OTHER users' commitments
                // User can only delete their own Open item if no one else has committed to it
                var otherCommitmentsCount = item.Commitments.Count(c => c.UserId != request.UserId);
                if (otherCommitmentsCount > 0)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CancelOpenSignUpItem FAILED: Other users have commitments - ItemId={ItemId}, OtherCommitmentsCount={OtherCommitmentsCount}, Duration={ElapsedMs}ms",
                        request.ItemId, otherCommitmentsCount, stopwatch.ElapsedMilliseconds);

                    return Result.Failure(
                        $"Cannot delete this Open item because {otherCommitmentsCount} other user(s) have committed to it. " +
                        "Your commitment has been canceled, but the item will remain available for others.");
                }

                _logger.LogInformation(
                    "CancelOpenSignUpItem: No other commitments found - proceeding with item removal");

                // Remove the item from the list (now safe because only user's own commitment existed)
                var removeResult = signUpList.RemoveItem(request.ItemId);
                if (removeResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CancelOpenSignUpItem FAILED: Item removal failed - ItemId={ItemId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.ItemId, removeResult.Error, stopwatch.ElapsedMilliseconds);

                    return removeResult;
                }

                _logger.LogInformation(
                    "CancelOpenSignUpItem: Item removed successfully - ItemId={ItemId}",
                    request.ItemId);

                // Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "CancelOpenSignUpItem COMPLETE: EventId={EventId}, SignUpListId={SignUpListId}, ItemId={ItemId}, Duration={ElapsedMs}ms",
                    request.EventId, request.SignUpListId, request.ItemId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CancelOpenSignUpItem FAILED: Exception occurred - EventId={EventId}, SignUpListId={SignUpListId}, ItemId={ItemId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.SignUpListId, request.ItemId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
