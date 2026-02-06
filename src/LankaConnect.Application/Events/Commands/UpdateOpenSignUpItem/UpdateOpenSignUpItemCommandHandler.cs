using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.UpdateOpenSignUpItem;

/// <summary>
/// Phase 6A.27: Handler for updating a user-submitted Open item
/// </summary>
public class UpdateOpenSignUpItemCommandHandler : ICommandHandler<UpdateOpenSignUpItemCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateOpenSignUpItemCommandHandler> _logger;

    public UpdateOpenSignUpItemCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateOpenSignUpItemCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateOpenSignUpItemCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateOpenSignUpItem"))
        using (LogContext.PushProperty("EntityType", "SignUpItem"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("SignUpListId", request.SignUpListId))
        using (LogContext.PushProperty("ItemId", request.ItemId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateOpenSignUpItem START: EventId={EventId}, SignUpListId={SignUpListId}, ItemId={ItemId}, UserId={UserId}, ItemName={ItemName}",
                request.EventId, request.SignUpListId, request.ItemId, request.UserId, request.ItemName);

            try
            {
                // Get the event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateOpenSignUpItem FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Event with ID {request.EventId} not found");
                }

                _logger.LogInformation(
                    "UpdateOpenSignUpItem: Event loaded - EventId={EventId}, Title={Title}",
                    @event.Id, @event.Title.Value);

                // Get the sign-up list from the event
                var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
                if (signUpList == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateOpenSignUpItem FAILED: Sign-up list not found - EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Sign-up list with ID {request.SignUpListId} not found");
                }

                _logger.LogInformation(
                    "UpdateOpenSignUpItem: Sign-up list loaded - SignUpListId={SignUpListId}, Category={Category}",
                    signUpList.Id, signUpList.Category);

                // Get the item
                var item = signUpList.GetItem(request.ItemId);
                if (item == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateOpenSignUpItem FAILED: Sign-up item not found - EventId={EventId}, SignUpListId={SignUpListId}, ItemId={ItemId}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, request.ItemId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Sign-up item with ID {request.ItemId} not found");
                }

                _logger.LogInformation(
                    "UpdateOpenSignUpItem: Sign-up item loaded - ItemId={ItemId}, ItemCategory={ItemCategory}, CreatedByUserId={CreatedByUserId}",
                    item.Id, item.ItemCategory, item.IsCreatedByUser(request.UserId));

                // Verify this is an Open item created by this user
                if (item.ItemCategory != SignUpItemCategory.Open)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateOpenSignUpItem FAILED: Not an Open item - ItemId={ItemId}, ItemCategory={ItemCategory}, Duration={ElapsedMs}ms",
                        request.ItemId, item.ItemCategory, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Only Open items can be updated using this endpoint");
                }

                if (!item.IsCreatedByUser(request.UserId))
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateOpenSignUpItem FAILED: User not authorized - ItemId={ItemId}, UserId={UserId}, Duration={ElapsedMs}ms",
                        request.ItemId, request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("You can only update Open items that you created");
                }

                // Update the item details
                var updateResult = item.UpdateDetails(request.ItemName, request.Quantity, request.Notes);
                if (updateResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateOpenSignUpItem FAILED: Domain validation failed - ItemId={ItemId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.ItemId, updateResult.Error, stopwatch.ElapsedMilliseconds);

                    return updateResult;
                }

                _logger.LogInformation(
                    "UpdateOpenSignUpItem: Item details updated - ItemId={ItemId}, NewQuantity={Quantity}",
                    item.Id, request.Quantity);

                // Update the user's commitment quantity to match
                var commitment = item.Commitments.FirstOrDefault(c => c.UserId == request.UserId);
                if (commitment != null)
                {
                    var commitResult = item.UpdateCommitment(
                        request.UserId,
                        request.Quantity,
                        request.Notes,
                        request.ContactName,
                        request.ContactEmail,
                        request.ContactPhone);

                    if (commitResult.IsFailure)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "UpdateOpenSignUpItem FAILED: Commitment update failed - ItemId={ItemId}, CommitmentId={CommitmentId}, Error={Error}, Duration={ElapsedMs}ms",
                            request.ItemId, commitment.Id, commitResult.Error, stopwatch.ElapsedMilliseconds);

                        return commitResult;
                    }

                    _logger.LogInformation(
                        "UpdateOpenSignUpItem: Commitment updated - CommitmentId={CommitmentId}, NewQuantity={Quantity}",
                        commitment.Id, request.Quantity);
                }

                // Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateOpenSignUpItem COMPLETE: EventId={EventId}, SignUpListId={SignUpListId}, ItemId={ItemId}, Duration={ElapsedMs}ms",
                    request.EventId, request.SignUpListId, request.ItemId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateOpenSignUpItem FAILED: Exception occurred - EventId={EventId}, SignUpListId={SignUpListId}, ItemId={ItemId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.SignUpListId, request.ItemId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
