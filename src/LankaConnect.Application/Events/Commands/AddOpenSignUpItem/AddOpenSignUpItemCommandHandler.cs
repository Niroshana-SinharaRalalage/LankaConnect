using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.AddOpenSignUpItem;

/// <summary>
/// Phase 6A.27: Handler for adding a user-submitted Open item to a sign-up list
/// </summary>
public class AddOpenSignUpItemCommandHandler : ICommandHandler<AddOpenSignUpItemCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddOpenSignUpItemCommandHandler> _logger;

    public AddOpenSignUpItemCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddOpenSignUpItemCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(AddOpenSignUpItemCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "AddOpenSignUpItem"))
        using (LogContext.PushProperty("EntityType", "SignUpItem"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("SignUpListId", request.SignUpListId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug(
                "AddOpenSignUpItem START: EventId={EventId}, SignUpListId={SignUpListId}, UserId={UserId}, ItemName={ItemName}, Quantity={Quantity}",
                request.EventId, request.SignUpListId, request.UserId, request.ItemName, request.Quantity);

            try
            {
                // Get the event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddOpenSignUpItem FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure($"Event with ID {request.EventId} not found");
                }

                _logger.LogInformation(
                    "AddOpenSignUpItem: Event loaded - EventId={EventId}, Title={Title}",
                    @event.Id, @event.Title.Value);

                // Get the sign-up list from the event
                var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
                if (signUpList == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddOpenSignUpItem FAILED: Sign-up list not found - EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure($"Sign-up list with ID {request.SignUpListId} not found");
                }

                _logger.LogInformation(
                    "AddOpenSignUpItem: Sign-up list loaded - SignUpListId={SignUpListId}, Category={Category}",
                    signUpList.Id, signUpList.Category);

                // Add the Open item (domain method handles validation and auto-commitment)
                var itemResult = signUpList.AddOpenItem(
                    request.UserId,
                    request.ItemName,
                    request.Quantity,
                    request.Notes,
                    request.ContactName,
                    request.ContactEmail,
                    request.ContactPhone);

                if (itemResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddOpenSignUpItem FAILED: Domain validation failed - EventId={EventId}, SignUpListId={SignUpListId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, itemResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure(itemResult.Error);
                }

                _logger.LogInformation(
                    "AddOpenSignUpItem: Domain method succeeded - ItemId={ItemId}, ItemName={ItemName}",
                    itemResult.Value.Id, request.ItemName);

                // Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "AddOpenSignUpItem COMPLETE: EventId={EventId}, SignUpListId={SignUpListId}, ItemId={ItemId}, Duration={ElapsedMs}ms",
                    request.EventId, request.SignUpListId, itemResult.Value.Id, stopwatch.ElapsedMilliseconds);

                // Return the created item ID
                return Result<Guid>.Success(itemResult.Value.Id);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "AddOpenSignUpItem FAILED: Exception occurred - EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
