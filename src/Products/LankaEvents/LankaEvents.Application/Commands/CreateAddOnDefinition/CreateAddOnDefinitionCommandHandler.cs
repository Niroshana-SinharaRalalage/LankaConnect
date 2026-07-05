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
namespace LankaConnect.Products.LankaEvents.Application.Commands.CreateAddOnDefinition;

/// <summary>
/// Handles creating a new add-on definition for an event.
/// Flow: validate event exists + published + add-ons enabled -> create Money ->
/// create AddOnDefinition entity -> save -> return definition ID
/// </summary>
public class CreateAddOnDefinitionCommandHandler : ICommandHandler<CreateAddOnDefinitionCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly IAddOnDefinitionRepository _addOnDefinitionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateAddOnDefinitionCommandHandler> _logger;

    public CreateAddOnDefinitionCommandHandler(
        IEventRepository eventRepository,
        IAddOnDefinitionRepository addOnDefinitionRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateAddOnDefinitionCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _addOnDefinitionRepository = addOnDefinitionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateAddOnDefinitionCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CreateAddOnDefinition"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CreateAddOnDefinition START: EventId={EventId}, Name={Name}, Price={Price}, Currency={Currency}, QuantityLimit={QuantityLimit}",
                request.EventId, request.Name, request.Price, request.Currency, request.QuantityLimit);

            try
            {
                // 1. Validate event exists and is published
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                    return Result<Guid>.Failure("Event not found");

                if (@event.Status != LankaConnect.Products.LankaEvents.Domain.Enums.EventStatus.Published)
                    return Result<Guid>.Failure("Add-on definitions can only be created for published events");

                // 2. Validate add-ons are enabled
                if (!@event.AreAddOnsEnabled())
                    return Result<Guid>.Failure("Add-ons are not enabled for this event");

                // 3. Parse currency and create Money
                if (!Enum.TryParse<Currency>(request.Currency, true, out var currency))
                    return Result<Guid>.Failure($"Invalid currency: {request.Currency}");

                var priceResult = Money.Create(request.Price, currency);
                if (priceResult.IsFailure)
                    return Result<Guid>.Failure(priceResult.Error);

                // 4. Create AddOnDefinition entity
                var definitionResult = AddOnDefinition.Create(
                    request.EventId,
                    request.Name,
                    request.Description,
                    priceResult.Value,
                    request.QuantityLimit,
                    request.SortOrder);

                if (definitionResult.IsFailure)
                    return Result<Guid>.Failure(definitionResult.Error);

                var definition = definitionResult.Value;

                // 5. Save + commit
                await _addOnDefinitionRepository.AddAsync(definition, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "CreateAddOnDefinition COMPLETE: DefinitionId={DefinitionId}, EventId={EventId}, Name={Name}, Duration={ElapsedMs}ms",
                    definition.Id, request.EventId, request.Name, stopwatch.ElapsedMilliseconds);

                return Result<Guid>.Success(definition.Id);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CreateAddOnDefinition FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result<Guid>.Failure($"Add-on definition creation failed: {ex.Message}");
            }
        }
    }
}
