using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Wave 8.5.g
using Microsoft.EntityFrameworkCore; // Wave 8.5.g
namespace LankaConnect.Products.LankaEvents.Application.Commands.CreateSignUpListWithItems;

/// <summary>
/// Handler for creating a sign-up list with items in a single transactional operation
/// </summary>
public class CreateSignUpListWithItemsCommandHandler : ICommandHandler<CreateSignUpListWithItemsCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LankaEventsDbContext _dbContext; // Wave 8.5.g direct-SaveChanges
    private readonly ILogger<CreateSignUpListWithItemsCommandHandler> _logger;

    public CreateSignUpListWithItemsCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        LankaEventsDbContext dbContext,
        ILogger<CreateSignUpListWithItemsCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateSignUpListWithItemsCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CreateSignUpListWithItems"))
        using (LogContext.PushProperty("EntityType", "SignUpList"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CreateSignUpListWithItems START: EventId={EventId}, Category={Category}, ItemCount={ItemCount}",
                request.EventId, request.Category, request.Items.Count);

            try
            {
                // Get the event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CreateSignUpListWithItems FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure($"Event with ID {request.EventId} not found");
                }

                _logger.LogInformation(
                    "CreateSignUpListWithItems: Event loaded - EventId={EventId}, Title={Title}, CurrentSignUpListCount={SignUpListCount}",
                    @event.Id, @event.Title.Value, @event.SignUpLists.Count);

                _logger.LogInformation(
                    "CreateSignUpListWithItems: Creating sign-up list - Category={Category}, Kind={Kind}, HasMandatory={HasMandatory}, HasPreferred={HasPreferred}, HasSuggested={HasSuggested}, HasOpen={HasOpen}",
                    request.Category, request.Kind, request.HasMandatoryItems, request.HasPreferredItems, request.HasSuggestedItems, request.HasOpenItems);

                // Phase 7D.1: Volunteer lists are slot-only (1 volunteer = 1 slot) and have
                // no open-items; route to the dedicated factory so the invariant surfaces
                // as one clear error, not a downstream AddItem failure.
                Result<SignUpList> signUpListResult;
                if (request.Kind == SignUpKind.Volunteers)
                {
                    var invalidItem = request.Items.FirstOrDefault(i => i.ItemType != SignUpItemType.Slot);
                    if (invalidItem != null)
                    {
                        stopwatch.Stop();
                        _logger.LogWarning(
                            "CreateSignUpListWithItems FAILED: Volunteer list requires slot-based items - EventId={EventId}, OffendingItem={ItemDescription}, Duration={ElapsedMs}ms",
                            request.EventId, invalidItem.ItemDescription, stopwatch.ElapsedMilliseconds);
                        return Result<Guid>.Failure("Volunteer lists only accept slot-based roles (ItemType=Slot with AvailableSlots)");
                    }

                    var roles = request.Items.Select(i => (
                        roleName: i.ItemDescription,
                        volunteersNeeded: i.AvailableSlots ?? 1,
                        suggestedPerSlot: i.SuggestedPerSlot,
                        notes: i.Notes
                    ));

                    signUpListResult = SignUpList.CreateVolunteerList(
                        request.Category,
                        request.Description,
                        roles);
                }
                else
                {
                    // Phase 6A.131: Convert items to extended tuple format supporting both quantity-based and slot-based items
                    var items = request.Items.Select(item => (
                        description: item.ItemDescription,
                        itemType: item.ItemType,
                        category: item.ItemCategory,
                        targetQuantity: item.TargetQuantity,
                        availableSlots: item.AvailableSlots,
                        suggestedPerSlot: item.SuggestedPerSlot,
                        notes: item.Notes
                    ));

                    // Create sign-up list with items in single operation
                    // Phase 6A.131: Pass dual-field item data for quantity/slot support
                    signUpListResult = SignUpList.CreateWithCategoriesAndItems(
                        request.Category,
                        request.Description,
                        request.HasMandatoryItems,
                        request.HasPreferredItems,
                        request.HasSuggestedItems,
                        items,
                        request.HasOpenItems);
                }

                if (signUpListResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CreateSignUpListWithItems FAILED: SignUpList creation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, signUpListResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure(signUpListResult.Error);
                }

                _logger.LogInformation(
                    "CreateSignUpListWithItems: SignUpList created - SignUpListId={SignUpListId}, ItemsCount={ItemsCount}",
                    signUpListResult.Value.Id, signUpListResult.Value.Items.Count);

                // Add to event
                var addResult = @event.AddSignUpList(signUpListResult.Value);
                if (addResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CreateSignUpListWithItems FAILED: Domain validation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, addResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure(addResult.Error);
                }

                _logger.LogInformation(
                    "CreateSignUpListWithItems: Domain method succeeded - EventId={EventId}, SignUpListId={SignUpListId}",
                    @event.Id, signUpListResult.Value.Id);

                // Commit changes
                await _dbContext.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "CreateSignUpListWithItems COMPLETE: EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                    request.EventId, signUpListResult.Value.Id, stopwatch.ElapsedMilliseconds);

                // Return the created sign-up list ID
                return Result<Guid>.Success(signUpListResult.Value.Id);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CreateSignUpListWithItems FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
