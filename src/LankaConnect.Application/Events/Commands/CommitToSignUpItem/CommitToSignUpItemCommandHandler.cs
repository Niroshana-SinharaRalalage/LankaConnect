using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.CommitToSignUpItem;

public class CommitToSignUpItemCommandHandler : ICommandHandler<CommitToSignUpItemCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CommitToSignUpItemCommandHandler> _logger;

    public CommitToSignUpItemCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<CommitToSignUpItemCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(CommitToSignUpItemCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CommitToSignUpItem"))
        using (LogContext.PushProperty("EntityType", "SignUpCommitment"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("SignUpListId", request.SignUpListId))
        using (LogContext.PushProperty("SignUpItemId", request.SignUpItemId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CommitToSignUpItem START: EventId={EventId}, SignUpListId={SignUpListId}, SignUpItemId={SignUpItemId}, UserId={UserId}, Quantity={Quantity}",
                request.EventId, request.SignUpListId, request.SignUpItemId, request.UserId, request.Quantity);

            try
            {
                // Phase 6A.114 DEBUG: Log handler execution
                _logger.LogWarning("[DEBUG-HANDLER] CommitToSignUpItemCommandHandler executing - about to load event");

                // Get the event with sign-up lists
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);

                // Phase 6A.114 DEBUG: Log event loaded status
                _logger.LogWarning("[DEBUG-HANDLER] Event loaded: {EventLoaded}, SignUpLists: {SignUpListsCount}",
                    @event != null, @event?.SignUpLists?.Count ?? 0);

                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CommitToSignUpItem FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Event with ID {request.EventId} not found");
                }

                _logger.LogInformation(
                    "CommitToSignUpItem: Event loaded - EventId={EventId}, Title={Title}",
                    @event.Id, @event.Title.Value);

                // Get the sign-up list
                var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
                if (signUpList == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CommitToSignUpItem FAILED: Sign-up list not found - EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Sign-up list with ID {request.SignUpListId} not found");
                }

                // Get the sign-up item
                var signUpItem = signUpList.GetItem(request.SignUpItemId);
                if (signUpItem == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CommitToSignUpItem FAILED: Sign-up item not found - EventId={EventId}, SignUpListId={SignUpListId}, SignUpItemId={SignUpItemId}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, request.SignUpItemId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Sign-up item with ID {request.SignUpItemId} not found");
                }

                // Phase 6A.121: SignUpItem now uses dual nullable fields (TargetQuantity or AvailableSlots)
                var targetQuantity = signUpItem.TargetQuantity ?? signUpItem.AvailableSlots ?? 0;

                _logger.LogInformation(
                    "CommitToSignUpItem: Sign-up item loaded - SignUpItemId={SignUpItemId}, Description={Description}, TargetQuantity={TargetQuantity}, CommittedQuantity={CommittedQuantity}",
                    signUpItem.Id, signUpItem.ItemDescription, targetQuantity, signUpItem.GetCommittedQuantity());

                // Phase 6A.17: Support both creating new commitments and updating existing ones
                // Check if user already has a commitment to this item
                var existingCommitment = signUpItem.Commitments.FirstOrDefault(c => c.UserId == request.UserId);

                Result commitResult;
                if (existingCommitment != null)
                {
                    // Phase 6A.121: SignUpCommitment uses backward-compatible Quantity property
                    #pragma warning disable CS0618 // Suppress obsolete warning - Quantity is backward compatible
                    _logger.LogInformation(
                        "CommitToSignUpItem: Updating existing commitment - UserId={UserId}, CurrentQuantity={CurrentQuantity}, NewQuantity={NewQuantity}",
                        request.UserId, existingCommitment.Quantity, request.Quantity);
                    #pragma warning restore CS0618

                    // User already committed - update the existing commitment
                    commitResult = signUpItem.UpdateCommitment(
                        request.UserId,
                        request.Quantity,
                        request.Notes,
                        request.ContactName,
                        request.ContactEmail,
                        request.ContactPhone);
                }
                else
                {
                    _logger.LogInformation(
                        "CommitToSignUpItem: Creating new commitment - UserId={UserId}, Quantity={Quantity}",
                        request.UserId, request.Quantity);

                    // New commitment - add it (Phase 2: with contact info)
                    commitResult = signUpItem.AddCommitment(
                        request.UserId,
                        request.Quantity,
                        request.Notes,
                        request.ContactName,
                        request.ContactEmail,
                        request.ContactPhone);
                }

                if (commitResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CommitToSignUpItem FAILED: Domain validation failed - EventId={EventId}, SignUpItemId={SignUpItemId}, UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpItemId, request.UserId, commitResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result.Failure(commitResult.Error);
                }

                _logger.LogInformation(
                    "CommitToSignUpItem: Domain method succeeded - IsUpdate={IsUpdate}",
                    existingCommitment != null);

                // Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "CommitToSignUpItem COMPLETE: EventId={EventId}, SignUpItemId={SignUpItemId}, UserId={UserId}, Quantity={Quantity}, IsUpdate={IsUpdate}, Duration={ElapsedMs}ms",
                    request.EventId, request.SignUpItemId, request.UserId, request.Quantity, existingCommitment != null, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CommitToSignUpItem FAILED: Exception occurred - EventId={EventId}, SignUpItemId={SignUpItemId}, UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.SignUpItemId, request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
