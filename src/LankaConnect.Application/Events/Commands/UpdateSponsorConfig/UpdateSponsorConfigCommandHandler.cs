using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.UpdateSponsorConfig;

/// <summary>
/// Handles updating the sponsor configuration on an event.
/// Creates a SponsorConfiguration value object and sets it on the event aggregate.
/// </summary>
public class UpdateSponsorConfigCommandHandler : ICommandHandler<UpdateSponsorConfigCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateSponsorConfigCommandHandler> _logger;

    public UpdateSponsorConfigCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateSponsorConfigCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateSponsorConfigCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateSponsorConfig"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateSponsorConfig START: EventId={EventId}, IsEnabled={IsEnabled}, AcceptMoney={AcceptMoney}, AcceptItem={AcceptItem}",
                request.EventId, request.IsEnabled, request.AcceptMoneySponsors, request.AcceptItemSponsors);

            try
            {
                // 1. Load event, validate exists
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                    return Result.Failure("Event not found");

                if (request.IsEnabled)
                {
                    // 2. Create SponsorConfiguration and set on event
                    var configResult = SponsorConfiguration.Create(
                        request.IsEnabled,
                        request.AcceptMoneySponsors,
                        request.AcceptItemSponsors,
                        request.MinSponsorAmount,
                        request.SponsorMessage,
                        request.ShowSponsorList,
                        request.EnablePackages);

                    if (configResult.IsFailure)
                        return Result.Failure(configResult.Error);

                    var setResult = @event.SetSponsorConfiguration(configResult.Value);
                    if (setResult.IsFailure)
                        return Result.Failure(setResult.Error);
                }
                else
                {
                    // 3. Disable sponsors
                    var disableResult = @event.DisableSponsors();
                    if (disableResult.IsFailure)
                        return Result.Failure(disableResult.Error);
                }

                // 4. Save + commit
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateSponsorConfig COMPLETE: EventId={EventId}, IsEnabled={IsEnabled}, Duration={ElapsedMs}ms",
                    request.EventId, request.IsEnabled, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateSponsorConfig FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result.Failure($"Failed to update sponsor configuration: {ex.Message}");
            }
        }
    }
}
