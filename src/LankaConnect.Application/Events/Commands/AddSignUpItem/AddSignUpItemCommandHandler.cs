using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.AddSignUpItem;

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

            _logger.LogDebug(
                "AddSignUpItem START: EventId={EventId}, SignUpListId={SignUpListId}, Description={Description}, Quantity={Quantity}",
                request.EventId, request.SignUpListId, request.ItemDescription, request.Quantity);

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

                // Add item to the sign-up list
                var itemResult = signUpList.AddItem(
                    request.ItemDescription,
                    request.Quantity,
                    request.ItemCategory,
                    request.Notes);

                if (itemResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddSignUpItem FAILED: Domain validation failed - EventId={EventId}, SignUpListId={SignUpListId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, itemResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure(itemResult.Error);
                }

                _logger.LogInformation(
                    "AddSignUpItem: Domain method succeeded - ItemId={ItemId}, Description={Description}, Quantity={Quantity}",
                    itemResult.Value.Id, request.ItemDescription, request.Quantity);

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
