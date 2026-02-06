using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.RemoveSignUpItem;

public class RemoveSignUpItemCommandHandler : ICommandHandler<RemoveSignUpItemCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveSignUpItemCommandHandler> _logger;

    public RemoveSignUpItemCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<RemoveSignUpItemCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(RemoveSignUpItemCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "RemoveSignUpItem"))
        using (LogContext.PushProperty("EntityType", "SignUpItem"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("SignUpListId", request.SignUpListId))
        using (LogContext.PushProperty("SignUpItemId", request.SignUpItemId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "RemoveSignUpItem START: EventId={EventId}, SignUpListId={SignUpListId}, SignUpItemId={SignUpItemId}",
                request.EventId, request.SignUpListId, request.SignUpItemId);

            try
            {
                // Get the event with sign-up lists
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RemoveSignUpItem FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Event with ID {request.EventId} not found");
                }

                _logger.LogInformation(
                    "RemoveSignUpItem: Event loaded - EventId={EventId}, Title={Title}",
                    @event.Id, @event.Title.Value);

                // Get the sign-up list
                var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
                if (signUpList == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RemoveSignUpItem FAILED: Sign-up list not found - EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Sign-up list with ID {request.SignUpListId} not found");
                }

                _logger.LogInformation(
                    "RemoveSignUpItem: Sign-up list loaded - SignUpListId={SignUpListId}, Category={Category}, CurrentItemsCount={ItemsCount}",
                    signUpList.Id, signUpList.Category, signUpList.Items.Count);

                // Remove item from the sign-up list
                var removeResult = signUpList.RemoveItem(request.SignUpItemId);
                if (removeResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RemoveSignUpItem FAILED: Domain validation failed - EventId={EventId}, SignUpListId={SignUpListId}, SignUpItemId={SignUpItemId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, request.SignUpItemId, removeResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result.Failure(removeResult.Error);
                }

                _logger.LogInformation(
                    "RemoveSignUpItem: Domain method succeeded - SignUpListId={SignUpListId}, SignUpItemId={SignUpItemId}, NewItemsCount={ItemsCount}",
                    signUpList.Id, request.SignUpItemId, signUpList.Items.Count);

                // Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "RemoveSignUpItem COMPLETE: EventId={EventId}, SignUpListId={SignUpListId}, SignUpItemId={SignUpItemId}, Duration={ElapsedMs}ms",
                    request.EventId, request.SignUpListId, request.SignUpItemId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "RemoveSignUpItem FAILED: Exception occurred - EventId={EventId}, SignUpListId={SignUpListId}, SignUpItemId={SignUpItemId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.SignUpListId, request.SignUpItemId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
