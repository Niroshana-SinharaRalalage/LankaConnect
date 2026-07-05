using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.ReorderSignUpItems;

/// <summary>
/// Phase 6A.132: Handler for reordering sign-up items within a list.
/// Loads the event aggregate, finds the list, delegates the set-equality check +
/// DisplayOrder reassignment to <see cref="LankaConnect.Products.LankaEvents.Domain.Entities.SignUpList.ReorderItems"/>,
/// and persists. Returns specific failure messages (not-found vs. stale-view) so the client
/// can decide whether to surface an error or silently refetch.
/// </summary>
public class ReorderSignUpItemsCommandHandler : ICommandHandler<ReorderSignUpItemsCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReorderSignUpItemsCommandHandler> _logger;

    public ReorderSignUpItemsCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<ReorderSignUpItemsCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(ReorderSignUpItemsCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ReorderSignUpItems"))
        using (LogContext.PushProperty("EntityType", "SignUpList"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("SignUpListId", request.SignUpListId))
        using (LogContext.PushProperty("ItemCount", request.OrderedItemIds.Count))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ReorderSignUpItems START: EventId={EventId}, SignUpListId={SignUpListId}, ItemCount={ItemCount}",
                request.EventId, request.SignUpListId, request.OrderedItemIds.Count);

            try
            {
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ReorderSignUpItems FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure($"Event with ID {request.EventId} not found");
                }

                var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
                if (signUpList == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ReorderSignUpItems FAILED: Sign-up list not found - EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure($"Sign-up list with ID {request.SignUpListId} not found");
                }

                var reorderResult = signUpList.ReorderItems(request.OrderedItemIds);
                if (reorderResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ReorderSignUpItems FAILED: Domain validation failed - EventId={EventId}, SignUpListId={SignUpListId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, reorderResult.Error, stopwatch.ElapsedMilliseconds);
                    return reorderResult;
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "ReorderSignUpItems COMPLETE: EventId={EventId}, SignUpListId={SignUpListId}, ItemCount={ItemCount}, Duration={ElapsedMs}ms",
                    request.EventId, request.SignUpListId, request.OrderedItemIds.Count, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "ReorderSignUpItems FAILED: Exception occurred - EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
