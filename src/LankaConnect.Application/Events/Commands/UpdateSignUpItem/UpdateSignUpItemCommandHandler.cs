using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.UpdateSignUpItem;

/// <summary>
/// Handler for updating sign-up item details
/// Phase 6A.14: Edit Sign-Up Item feature
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

            _logger.LogDebug(
                "UpdateSignUpItem START: EventId={EventId}, SignUpListId={SignUpListId}, SignUpItemId={SignUpItemId}, Description={Description}, Quantity={Quantity}",
                request.EventId, request.SignUpListId, request.SignUpItemId, request.ItemDescription, request.Quantity);

            try
            {
                // Get the event
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

                // Get the sign-up list from the event
                var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
                if (signUpList == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateSignUpItem FAILED: Sign-up list not found - EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Sign-up list with ID {request.SignUpListId} not found");
                }

                // Get the sign-up item from the list
                var signUpItem = signUpList.GetItem(request.SignUpItemId);
                if (signUpItem == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateSignUpItem FAILED: Sign-up item not found - EventId={EventId}, SignUpListId={SignUpListId}, SignUpItemId={SignUpItemId}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, request.SignUpItemId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Sign-up item with ID {request.SignUpItemId} not found");
                }

                _logger.LogInformation(
                    "UpdateSignUpItem: Sign-up item loaded - SignUpItemId={SignUpItemId}, CurrentDescription={CurrentDescription}, CurrentQuantity={CurrentQuantity}",
                    signUpItem.Id, signUpItem.ItemDescription, signUpItem.Quantity);

                // Update the sign-up item using domain method
                var updateResult = signUpItem.UpdateDetails(
                    request.ItemDescription,
                    request.Quantity,
                    request.Notes);

                if (updateResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateSignUpItem FAILED: Domain validation failed - EventId={EventId}, SignUpItemId={SignUpItemId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpItemId, updateResult.Error, stopwatch.ElapsedMilliseconds);

                    return updateResult;
                }

                _logger.LogInformation(
                    "UpdateSignUpItem: Domain method succeeded - SignUpItemId={SignUpItemId}, NewDescription={NewDescription}, NewQuantity={NewQuantity}",
                    signUpItem.Id, request.ItemDescription, request.Quantity);

                // Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateSignUpItem COMPLETE: EventId={EventId}, SignUpItemId={SignUpItemId}, Duration={ElapsedMs}ms",
                    request.EventId, request.SignUpItemId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateSignUpItem FAILED: Exception occurred - EventId={EventId}, SignUpItemId={SignUpItemId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.SignUpItemId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
