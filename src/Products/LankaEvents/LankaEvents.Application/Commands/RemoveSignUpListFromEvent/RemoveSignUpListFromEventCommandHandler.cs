using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Wave 8.5.g direct-SaveChanges
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.RemoveSignUpListFromEvent;

public class RemoveSignUpListFromEventCommandHandler : ICommandHandler<RemoveSignUpListFromEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly LankaEventsDbContext _dbContext; // Wave 8.5.g direct-SaveChanges
    private readonly ILogger<RemoveSignUpListFromEventCommandHandler> _logger;

    public RemoveSignUpListFromEventCommandHandler(
        IEventRepository eventRepository,
        LankaEventsDbContext dbContext,
        ILogger<RemoveSignUpListFromEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> Handle(RemoveSignUpListFromEventCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "RemoveSignUpListFromEvent"))
        using (LogContext.PushProperty("EntityType", "SignUpList"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("SignUpListId", request.SignUpListId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "RemoveSignUpListFromEvent START: EventId={EventId}, SignUpListId={SignUpListId}",
                request.EventId, request.SignUpListId);

            try
            {
                // Get the event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RemoveSignUpListFromEvent FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Event with ID {request.EventId} not found");
                }

                _logger.LogInformation(
                    "RemoveSignUpListFromEvent: Event loaded - EventId={EventId}, Title={Title}, CurrentSignUpListCount={SignUpListCount}",
                    @event.Id, @event.Title.Value, @event.SignUpLists.Count);

                // Remove sign-up list
                var removeResult = @event.RemoveSignUpList(request.SignUpListId);
                if (removeResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RemoveSignUpListFromEvent FAILED: Domain validation failed - EventId={EventId}, SignUpListId={SignUpListId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, removeResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result.Failure(removeResult.Error);
                }

                _logger.LogInformation(
                    "RemoveSignUpListFromEvent: Domain method succeeded - EventId={EventId}, SignUpListId={SignUpListId}, NewSignUpListCount={SignUpListCount}",
                    @event.Id, request.SignUpListId, @event.SignUpLists.Count);

                // Wave 8.5.g direct-SaveChanges
                await _dbContext.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "RemoveSignUpListFromEvent COMPLETE: EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                    request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "RemoveSignUpListFromEvent FAILED: Exception occurred - EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
