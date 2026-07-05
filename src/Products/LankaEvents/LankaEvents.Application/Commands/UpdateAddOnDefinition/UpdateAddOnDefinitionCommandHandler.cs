using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.BuildingBlocks.Domain.Shared.Enums;
using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateAddOnDefinition;

/// <summary>
/// Handles updating an existing add-on definition.
/// Flow: load definition -> validate belongs to event -> update details ->
/// activate/deactivate as needed -> save -> commit
/// </summary>
public class UpdateAddOnDefinitionCommandHandler : ICommandHandler<UpdateAddOnDefinitionCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IAddOnDefinitionRepository _addOnDefinitionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateAddOnDefinitionCommandHandler> _logger;

    public UpdateAddOnDefinitionCommandHandler(
        IEventRepository eventRepository,
        IAddOnDefinitionRepository addOnDefinitionRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateAddOnDefinitionCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _addOnDefinitionRepository = addOnDefinitionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateAddOnDefinitionCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateAddOnDefinition"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("DefinitionId", request.DefinitionId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateAddOnDefinition START: EventId={EventId}, DefinitionId={DefinitionId}, Name={Name}, Price={Price}, IsActive={IsActive}",
                request.EventId, request.DefinitionId, request.Name, request.Price, request.IsActive);

            try
            {
                // 1. Load definition, validate exists
                var definition = await _addOnDefinitionRepository.GetByIdAsync(request.DefinitionId, cancellationToken);
                if (definition == null)
                    return Result.Failure("Add-on definition not found");

                // 2. Validate belongs to event
                if (definition.EventId != request.EventId)
                    return Result.Failure("Add-on definition does not belong to this event");

                // 3. Parse currency and create Money for price
                if (!Enum.TryParse<Currency>(request.Currency, true, out var currency))
                    return Result.Failure($"Invalid currency: {request.Currency}");

                var priceResult = Money.Create(request.Price, currency);
                if (priceResult.IsFailure)
                    return Result.Failure(priceResult.Error);

                // 4. Update details
                var updateResult = definition.UpdateDetails(
                    request.Name,
                    request.Description,
                    priceResult.Value,
                    request.QuantityLimit,
                    request.SortOrder);

                if (updateResult.IsFailure)
                    return Result.Failure(updateResult.Error);

                // 5. Handle active/inactive state changes
                if (!request.IsActive && definition.IsActive)
                {
                    var deactivateResult = definition.Deactivate();
                    if (deactivateResult.IsFailure)
                        return Result.Failure(deactivateResult.Error);
                }
                else if (request.IsActive && !definition.IsActive)
                {
                    var activateResult = definition.Activate();
                    if (activateResult.IsFailure)
                        return Result.Failure(activateResult.Error);
                }

                // 6. Save + commit
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateAddOnDefinition COMPLETE: DefinitionId={DefinitionId}, EventId={EventId}, Name={Name}, IsActive={IsActive}, Duration={ElapsedMs}ms",
                    request.DefinitionId, request.EventId, request.Name, request.IsActive, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateAddOnDefinition FAILED: EventId={EventId}, DefinitionId={DefinitionId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.DefinitionId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result.Failure($"Failed to update add-on definition: {ex.Message}");
            }
        }
    }
}
