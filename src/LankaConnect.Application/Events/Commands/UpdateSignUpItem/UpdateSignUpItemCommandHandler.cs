using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.UpdateSignUpItem;

/// <summary>
/// Handler for updating sign-up item details.
/// Phase 6A.14: Edit Sign-Up Item feature
/// Phase 6A.131: Server-authoritative type routing — loads the item, inspects <c>ItemType</c>
/// on the entity, and dispatches to the matching domain method. The client cannot spoof the type.
/// If the client submits the wrong discriminated field (e.g. TargetQuantity for a slot item),
/// the handler returns an explicit failure so the frontend can render a clear error.
/// </summary>
public class UpdateSignUpItemCommandHandler : ICommandHandler<UpdateSignUpItemCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateSignUpItemCommandHandler> _logger;

    public UpdateSignUpItemCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateSignUpItemCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateSignUpItemCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateSignUpItem"))
        using (LogContext.PushProperty("EntityType", "SignUpItem"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("SignUpListId", request.SignUpListId))
        using (LogContext.PushProperty("SignUpItemId", request.SignUpItemId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateSignUpItem START: EventId={EventId}, SignUpListId={SignUpListId}, SignUpItemId={SignUpItemId}, Description={Description}, TargetQuantity={TargetQuantity}, AvailableSlots={AvailableSlots}, SuggestedPerSlot={SuggestedPerSlot}",
                request.EventId, request.SignUpListId, request.SignUpItemId, request.ItemDescription,
                request.TargetQuantity, request.AvailableSlots, request.SuggestedPerSlot);

            try
            {
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateSignUpItem FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure($"Event with ID {request.EventId} not found");
                }

                _logger.LogInformation(
                    "UpdateSignUpItem: Event loaded - EventId={EventId}, Title={Title}",
                    @event.Id, @event.Title.Value);

                var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
                if (signUpList == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateSignUpItem FAILED: Sign-up list not found - EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure($"Sign-up list with ID {request.SignUpListId} not found");
                }

                var signUpItem = signUpList.GetItem(request.SignUpItemId);
                if (signUpItem == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateSignUpItem FAILED: Sign-up item not found - EventId={EventId}, SignUpListId={SignUpListId}, SignUpItemId={SignUpItemId}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, request.SignUpItemId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure($"Sign-up item with ID {request.SignUpItemId} not found");
                }

                using (LogContext.PushProperty("ItemType", signUpItem.ItemType))
                {
                    _logger.LogInformation(
                        "UpdateSignUpItem: Sign-up item loaded - SignUpItemId={SignUpItemId}, ItemType={ItemType}, CurrentDescription={CurrentDescription}, CurrentTargetQuantity={CurrentTargetQuantity}, CurrentAvailableSlots={CurrentAvailableSlots}",
                        signUpItem.Id, signUpItem.ItemType, signUpItem.ItemDescription,
                        signUpItem.TargetQuantity, signUpItem.AvailableSlots);

                    Result updateResult;
                    if (signUpItem.ItemType == SignUpItemType.Quantity)
                    {
                        if (!request.TargetQuantity.HasValue)
                        {
                            stopwatch.Stop();
                            _logger.LogWarning(
                                "UpdateSignUpItem FAILED: Item {SignUpItemId} is quantity-based but TargetQuantity was not supplied - Duration={ElapsedMs}ms",
                                signUpItem.Id, stopwatch.ElapsedMilliseconds);
                            return Result.Failure(
                                $"Item {signUpItem.Id} is quantity-based; provide TargetQuantity, not AvailableSlots.");
                        }

                        updateResult = signUpItem.UpdateDetails(
                            request.ItemDescription,
                            request.TargetQuantity.Value,
                            request.Notes);
                    }
                    else
                    {
                        if (!request.AvailableSlots.HasValue)
                        {
                            stopwatch.Stop();
                            _logger.LogWarning(
                                "UpdateSignUpItem FAILED: Item {SignUpItemId} is slot-based but AvailableSlots was not supplied - Duration={ElapsedMs}ms",
                                signUpItem.Id, stopwatch.ElapsedMilliseconds);
                            return Result.Failure(
                                $"Item {signUpItem.Id} is slot-based; provide AvailableSlots, not TargetQuantity.");
                        }

                        // If the client omits SuggestedPerSlot, preserve the current value (minimal-scope
                        // inline edit doesn't surface this field — see Phase A plan).
                        var suggestedPerSlot = request.SuggestedPerSlot ?? signUpItem.SuggestedPerSlot;

                        updateResult = signUpItem.UpdateDetailsSlotBased(
                            request.ItemDescription,
                            request.AvailableSlots.Value,
                            suggestedPerSlot,
                            request.Notes);
                    }

                    if (updateResult.IsFailure)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "UpdateSignUpItem FAILED: Domain validation failed - EventId={EventId}, SignUpItemId={SignUpItemId}, ItemType={ItemType}, Error={Error}, Duration={ElapsedMs}ms",
                            request.EventId, request.SignUpItemId, signUpItem.ItemType, updateResult.Error, stopwatch.ElapsedMilliseconds);
                        return updateResult;
                    }

                    _logger.LogInformation(
                        "UpdateSignUpItem: Domain method succeeded - SignUpItemId={SignUpItemId}, ItemType={ItemType}, NewDescription={NewDescription}",
                        signUpItem.Id, signUpItem.ItemType, request.ItemDescription);

                    await _unitOfWork.CommitAsync(cancellationToken);

                    stopwatch.Stop();
                    _logger.LogInformation(
                        "UpdateSignUpItem COMPLETE: EventId={EventId}, SignUpItemId={SignUpItemId}, ItemType={ItemType}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpItemId, signUpItem.ItemType, stopwatch.ElapsedMilliseconds);

                    return Result.Success();
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "UpdateSignUpItem FAILED: Exception occurred - EventId={EventId}, SignUpItemId={SignUpItemId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.SignUpItemId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
