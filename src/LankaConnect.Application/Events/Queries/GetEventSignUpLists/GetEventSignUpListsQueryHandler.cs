using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.GetEventSignUpLists;

/// <summary>
/// Handler for retrieving sign-up lists for an event
/// Returns all sign-up lists with their items and commitments
/// </summary>
public class GetEventSignUpListsQueryHandler : IQueryHandler<GetEventSignUpListsQuery, List<SignUpListDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<GetEventSignUpListsQueryHandler> _logger;

    public GetEventSignUpListsQueryHandler(
        IEventRepository eventRepository,
        ILogger<GetEventSignUpListsQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<Result<List<SignUpListDto>>> Handle(GetEventSignUpListsQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEventSignUpLists"))
        using (LogContext.PushProperty("EntityType", "SignUpList"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetEventSignUpLists START: EventId={EventId}",
                request.EventId);

            try
            {
                // Validate request
                if (request.EventId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEventSignUpLists FAILED: Invalid EventId - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<List<SignUpListDto>>.Failure("Event ID is required");
                }

                // Perf RCA 2026-04-25: read-only handler — pass trackChanges:false explicitly so
                // the EF change tracker doesn't materialize the full aggregate. Same fix as
                // GetEventByIdQueryHandler; both handlers fire on every event-detail page load.
                var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: false, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEventSignUpLists FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<List<SignUpListDto>>.Failure($"Event with ID {request.EventId} not found");
                }

                _logger.LogInformation(
                    "GetEventSignUpLists: Event loaded - EventId={EventId}, Title={Title}, SignUpListCount={SignUpListCount}, KindFilter={KindFilter}",
                    @event.Id, @event.Title.Value, @event.SignUpLists.Count, request.Kind?.ToString() ?? "All");

                var filteredLists = request.Kind.HasValue
                    ? @event.SignUpLists.Where(l => l.Kind == request.Kind.Value)
                    : @event.SignUpLists.AsEnumerable();

                var signUpListDtos = filteredLists.Select(signUpList => new SignUpListDto
                {
                    Id = signUpList.Id,
                    Category = signUpList.Category,
                    Description = signUpList.Description,
                    SignUpType = signUpList.SignUpType,
                    Kind = signUpList.Kind,

                    // Legacy fields (for Open/Predefined sign-ups)
                    PredefinedItems = signUpList.PredefinedItems.ToList(),
                    // Phase 6A.121: SignUpCommitment uses dual nullable fields (PhysicalQuantity/SlotsClaimed)
                    Commitments = signUpList.Commitments.Select(c => new SignUpCommitmentDto
                    {
                        Id = c.Id,
                        SignUpItemId = c.SignUpItemId,
                        UserId = c.UserId,
                        ItemDescription = c.ItemDescription,
                        PhysicalQuantity = c.PhysicalQuantity,
                        SlotsClaimed = c.SlotsClaimed,
                        CommittedAt = c.CommittedAt,
                        Notes = c.Notes,
                        ContactName = c.ContactName,
                        ContactEmail = c.ContactEmail,
                        ContactPhone = c.ContactPhone
                    }).ToList(),
                    CommitmentCount = signUpList.GetCommitmentCount(),

                    // New category-based fields
                    HasMandatoryItems = signUpList.HasMandatoryItems,
                    HasPreferredItems = signUpList.HasPreferredItems,
                    HasSuggestedItems = signUpList.HasSuggestedItems,
                    HasOpenItems = signUpList.HasOpenItems, // Phase 6A.27
                    // Phase 6A.123: Return typed DTOs (QuantityBasedItemDto or SlotBasedItemDto)
                    // so the frontend itemType discriminator works correctly.
                    // Phase 6A.132: OrderBy(DisplayOrder) then ItemDescription for a stable tiebreak
                    // when pre-backfill rows still share DisplayOrder=0.
                    Items = signUpList.Items
                        .OrderBy(i => i.DisplayOrder)
                        .ThenBy(i => i.ItemDescription)
                        .Select(item =>
                    {
                        var commitments = item.Commitments.Select(c => new SignUpCommitmentDto
                        {
                            Id = c.Id,
                            SignUpItemId = c.SignUpItemId,
                            UserId = c.UserId,
                            ItemDescription = c.ItemDescription,
                            PhysicalQuantity = c.PhysicalQuantity,
                            SlotsClaimed = c.SlotsClaimed,
                            CommittedAt = c.CommittedAt,
                            Notes = c.Notes,
                            ContactName = c.ContactName,
                            ContactEmail = c.ContactEmail,
                            ContactPhone = c.ContactPhone
                        }).ToList();

                        if (item.ItemType == SignUpItemType.Slot)
                        {
                            var filledSlots = (item.AvailableSlots ?? 0) - item.GetRemainingSlots();
                            return (ISignUpItemDto)new SlotBasedItemDto
                            {
                                Id = item.Id,
                                ItemDescription = item.ItemDescription,
                                ItemCategory = item.ItemCategory,
                                Notes = item.Notes,
                                CreatedByUserId = item.CreatedByUserId,
                                DisplayOrder = item.DisplayOrder,
                                TotalSlots = item.AvailableSlots ?? 0,
                                FilledSlots = filledSlots,
                                RemainingSlots = item.GetRemainingSlots(),
                                SuggestedQuantityPerSlot = item.SuggestedPerSlot,
                                Commitments = commitments
                            };
                        }
                        else
                        {
                            var committed = (item.TargetQuantity ?? 0) - item.GetRemainingQuantity();
                            return (ISignUpItemDto)new QuantityBasedItemDto
                            {
                                Id = item.Id,
                                ItemDescription = item.ItemDescription,
                                ItemCategory = item.ItemCategory,
                                Notes = item.Notes,
                                CreatedByUserId = item.CreatedByUserId,
                                DisplayOrder = item.DisplayOrder,
                                TargetQuantity = item.TargetQuantity ?? 0,
                                CommittedQuantity = committed,
                                RemainingQuantity = item.GetRemainingQuantity(),
                                Commitments = commitments
                            };
                        }
                    }).ToList()
                }).ToList();

                // Calculate totals for logging
                var totalItems = signUpListDtos.Sum(l => l.Items.Count);
                var totalCommitments = signUpListDtos.Sum(l => l.CommitmentCount);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEventSignUpLists COMPLETE: EventId={EventId}, SignUpListCount={SignUpListCount}, TotalItems={TotalItems}, TotalCommitments={TotalCommitments}, Duration={ElapsedMs}ms",
                    request.EventId, signUpListDtos.Count, totalItems, totalCommitments, stopwatch.ElapsedMilliseconds);

                return Result<List<SignUpListDto>>.Success(signUpListDtos);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetEventSignUpLists FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
