using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.CreateSignUpListWithItems;

/// <summary>
/// Handler for creating a sign-up list with items in a single transactional operation
/// </summary>
public class CreateSignUpListWithItemsCommandHandler : ICommandHandler<CreateSignUpListWithItemsCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateSignUpListWithItemsCommandHandler> _logger;

    public CreateSignUpListWithItemsCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateSignUpListWithItemsCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
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

                // Convert items to tuple format expected by domain method
                var items = request.Items.Select(item => (
                    description: item.ItemDescription,
                    quantity: item.Quantity,
                    category: item.ItemCategory,
                    notes: item.Notes
                ));

                _logger.LogInformation(
                    "CreateSignUpListWithItems: Creating sign-up list - Category={Category}, HasMandatory={HasMandatory}, HasPreferred={HasPreferred}, HasSuggested={HasSuggested}, HasOpen={HasOpen}",
                    request.Category, request.HasMandatoryItems, request.HasPreferredItems, request.HasSuggestedItems, request.HasOpenItems);

                // Create sign-up list with items in single operation
                // Phase 6A.27: Pass HasOpenItems parameter
                var signUpListResult = SignUpList.CreateWithCategoriesAndItems(
                    request.Category,
                    request.Description,
                    request.HasMandatoryItems,
                    request.HasPreferredItems,
                    request.HasSuggestedItems,
                    items,
                    request.HasOpenItems);

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
                await _unitOfWork.CommitAsync(cancellationToken);

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
