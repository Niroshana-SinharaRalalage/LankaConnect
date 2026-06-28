using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Products.LankaEvents.Application.Commands.AddSignUpItem;

public class AddSignUpItemCommandHandler : ICommandHandler<AddSignUpItemCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddSignUpItemCommandHandler> _logger;

    public AddSignUpItemCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddSignUpItemCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(AddSignUpItemCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "AddSignUpItem"))
        using (LogContext.PushProperty("EntityType", "SignUpItem"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("SignUpListId", request.SignUpListId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "AddSignUpItem START: EventId={EventId}, SignUpListId={SignUpListId}, Description={Description}, ItemType={ItemType}",
                request.EventId, request.SignUpListId, request.ItemDescription, request.ItemType);

            try
            {
                // Get the event with sign-up lists
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddSignUpItem FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure($"Event with ID {request.EventId} not found");
                }

                _logger.LogInformation(
                    "AddSignUpItem: Event loaded - EventId={EventId}, Title={Title}, SignUpListsCount={SignUpListsCount}",
                    @event.Id, @event.Title.Value, @event.SignUpLists.Count);

                // Get the sign-up list
                var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
                if (signUpList == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddSignUpItem FAILED: Sign-up list not found - EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure($"Sign-up list with ID {request.SignUpListId} not found");
                }

                _logger.LogInformation(
                    "AddSignUpItem: Sign-up list loaded - SignUpListId={SignUpListId}, Category={Category}, CurrentItemsCount={ItemsCount}",
                    signUpList.Id, signUpList.Category, signUpList.Items.Count);

                // Phase 6A.121: Add item using dual-field approach (quantity-based or slot-based)
                Result<LankaConnect.Products.LankaEvents.Domain.Entities.SignUpItem> itemResult;
                if (request.ItemType == SignUpItemType.Slot)
                {
                    itemResult = signUpList.AddSlotBasedItem(
                        request.ItemDescription,
                        request.AvailableSlots ?? 1,
                        request.SuggestedPerSlot,
                        request.ItemCategory,
                        request.Notes);
                }
                else
                {
                    itemResult = signUpList.AddItem(
                        request.ItemDescription,
                        request.TargetQuantity ?? 1,
                        request.ItemCategory,
                        request.Notes);
                }

                if (itemResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddSignUpItem FAILED: Domain validation failed - EventId={EventId}, SignUpListId={SignUpListId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, itemResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure(itemResult.Error);
                }

                _logger.LogInformation(
                    "AddSignUpItem: Domain method succeeded - ItemId={ItemId}, Description={Description}, ItemType={ItemType}",
                    itemResult.Value.Id, request.ItemDescription, request.ItemType);

                // Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "AddSignUpItem COMPLETE: EventId={EventId}, SignUpListId={SignUpListId}, ItemId={ItemId}, Duration={ElapsedMs}ms",
                    request.EventId, request.SignUpListId, itemResult.Value.Id, stopwatch.ElapsedMilliseconds);

                return Result<Guid>.Success(itemResult.Value.Id);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "AddSignUpItem FAILED: Exception occurred - EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
