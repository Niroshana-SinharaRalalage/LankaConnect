using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
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

                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEventSignUpLists FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<List<SignUpListDto>>.Failure($"Event with ID {request.EventId} not found");
                }

                _logger.LogInformation(
                    "GetEventSignUpLists: Event loaded - EventId={EventId}, Title={Title}, SignUpListCount={SignUpListCount}",
                    @event.Id, @event.Title.Value, @event.SignUpLists.Count);

                var signUpListDtos = @event.SignUpLists.Select(signUpList => new SignUpListDto
                {
                    Id = signUpList.Id,
                    Category = signUpList.Category,
                    Description = signUpList.Description,
                    SignUpType = signUpList.SignUpType,

                    // Legacy fields (for Open/Predefined sign-ups)
                    PredefinedItems = signUpList.PredefinedItems.ToList(),
                    Commitments = signUpList.Commitments.Select(c => new SignUpCommitmentDto
                    {
                        Id = c.Id,
                        SignUpItemId = c.SignUpItemId,
                        UserId = c.UserId,
                        ItemDescription = c.ItemDescription,
                        Quantity = c.Quantity,
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
                    Items = signUpList.Items.Select(item => new SignUpItemDto
                    {
                        Id = item.Id,
                        ItemDescription = item.ItemDescription,
                        Quantity = item.Quantity,
                        RemainingQuantity = item.RemainingQuantity,
                        ItemCategory = item.ItemCategory,
                        Notes = item.Notes,
                        CreatedByUserId = item.CreatedByUserId, // Phase 6A.27
                        Commitments = item.Commitments.Select(c => new SignUpCommitmentDto
                        {
                            Id = c.Id,
                            SignUpItemId = c.SignUpItemId,
                            UserId = c.UserId,
                            ItemDescription = c.ItemDescription,
                            Quantity = c.Quantity,
                            CommittedAt = c.CommittedAt,
                            Notes = c.Notes,
                            ContactName = c.ContactName,
                            ContactEmail = c.ContactEmail,
                            ContactPhone = c.ContactPhone
                        }).ToList()
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
