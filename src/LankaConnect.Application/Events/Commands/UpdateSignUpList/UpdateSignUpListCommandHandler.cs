using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.UpdateSignUpList;

/// <summary>
/// Handler for updating sign-up list details
/// Phase 6A.13: Edit Sign-Up List feature
/// </summary>
public class UpdateSignUpListCommandHandler : ICommandHandler<UpdateSignUpListCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateSignUpListCommandHandler> _logger;

    public UpdateSignUpListCommandHandler(
        IEventRepository _eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateSignUpListCommandHandler> logger)
    {
        this._eventRepository = _eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateSignUpListCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateSignUpList"))
        using (LogContext.PushProperty("EntityType", "SignUpList"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("SignUpListId", request.SignUpListId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug(
                "UpdateSignUpList START: EventId={EventId}, SignUpListId={SignUpListId}, Category={Category}",
                request.EventId, request.SignUpListId, request.Category);

            try
            {
                // Get the event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateSignUpList FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Event with ID {request.EventId} not found");
                }

                _logger.LogInformation(
                    "UpdateSignUpList: Event loaded - EventId={EventId}, Title={Title}",
                    @event.Id, @event.Title.Value);

                // Get the sign-up list from the event
                var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
                if (signUpList == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateSignUpList FAILED: Sign-up list not found - EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure($"Sign-up list with ID {request.SignUpListId} not found");
                }

                _logger.LogInformation(
                    "UpdateSignUpList: Sign-up list loaded - SignUpListId={SignUpListId}, CurrentCategory={CurrentCategory}",
                    signUpList.Id, signUpList.Category);

                // Update the sign-up list using domain method
                // Phase 6A.27: Pass HasOpenItems parameter
                var updateResult = signUpList.UpdateDetails(
                    request.Category,
                    request.Description,
                    request.HasMandatoryItems,
                    request.HasPreferredItems,
                    request.HasSuggestedItems,
                    request.HasOpenItems);

                if (updateResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateSignUpList FAILED: Domain validation failed - EventId={EventId}, SignUpListId={SignUpListId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, updateResult.Error, stopwatch.ElapsedMilliseconds);

                    return updateResult;
                }

                _logger.LogInformation(
                    "UpdateSignUpList: Domain method succeeded - SignUpListId={SignUpListId}, NewCategory={NewCategory}",
                    signUpList.Id, request.Category);

                // Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateSignUpList COMPLETE: EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                    request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateSignUpList FAILED: Exception occurred - EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
