using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.GetMyAddOnPurchases;

/// <summary>
/// Handles retrieving add-on purchases for a specific buyer email and event.
/// Returns completed and pending purchases with add-on definition names.
/// </summary>
public class GetMyAddOnPurchasesQueryHandler : IQueryHandler<GetMyAddOnPurchasesQuery, List<AddOnPurchaseDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IAddOnDefinitionRepository _addOnDefinitionRepository;
    private readonly IAddOnPurchaseRepository _addOnPurchaseRepository;
    private readonly ILogger<GetMyAddOnPurchasesQueryHandler> _logger;

    public GetMyAddOnPurchasesQueryHandler(
        IEventRepository eventRepository,
        IAddOnDefinitionRepository addOnDefinitionRepository,
        IAddOnPurchaseRepository addOnPurchaseRepository,
        ILogger<GetMyAddOnPurchasesQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _addOnDefinitionRepository = addOnDefinitionRepository;
        _addOnPurchaseRepository = addOnPurchaseRepository;
        _logger = logger;
    }

    public async Task<Result<List<AddOnPurchaseDto>>> Handle(
        GetMyAddOnPurchasesQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetMyAddOnPurchases"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("BuyerEmail", request.BuyerEmail))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetMyAddOnPurchases START: EventId={EventId}, BuyerEmail={BuyerEmail}",
                request.EventId, request.BuyerEmail);

            try
            {
                if (string.IsNullOrWhiteSpace(request.BuyerEmail))
                    return Result<List<AddOnPurchaseDto>>.Failure("Buyer email is required");

                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                    return Result<List<AddOnPurchaseDto>>.Failure("Event not found");

                var purchases = await _addOnPurchaseRepository.GetByBuyerEmailAndEventIdAsync(
                    request.BuyerEmail, request.EventId, cancellationToken);

                if (purchases.Count == 0)
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "GetMyAddOnPurchases COMPLETE: No purchases found - EventId={EventId}, BuyerEmail={BuyerEmail}, Duration={ElapsedMs}ms",
                        request.EventId, request.BuyerEmail, stopwatch.ElapsedMilliseconds);

                    return Result<List<AddOnPurchaseDto>>.Success(new List<AddOnPurchaseDto>());
                }

                // Build definition name lookup
                var definitions = await _addOnDefinitionRepository.GetByEventIdAsync(request.EventId, cancellationToken);
                var definitionLookup = definitions.ToDictionary(d => d.Id, d => d.Name);

                var purchaseDtos = purchases.Select(p => new AddOnPurchaseDto
                {
                    Id = p.Id,
                    EventId = p.EventId,
                    AddOnDefinitionId = p.AddOnDefinitionId,
                    AddOnName = definitionLookup.TryGetValue(p.AddOnDefinitionId, out var name) ? name : "Unknown",
                    RegistrationId = p.RegistrationId,
                    BuyerUserId = p.BuyerUserId,
                    BuyerName = p.BuyerName,
                    BuyerEmail = p.BuyerEmail,
                    BuyerPhone = p.BuyerPhone,
                    Quantity = p.Quantity,
                    UnitPrice = p.UnitPrice.Amount,
                    TotalAmount = p.TotalAmount.Amount,
                    Currency = p.UnitPrice.Currency.ToString(),
                    Status = p.Status.ToString(),
                    CreatedAt = p.CreatedAt,
                    PaymentCompletedAt = p.PaymentCompletedAt
                }).ToList();

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetMyAddOnPurchases COMPLETE: EventId={EventId}, BuyerEmail={BuyerEmail}, Count={Count}, Duration={ElapsedMs}ms",
                    request.EventId, request.BuyerEmail, purchaseDtos.Count, stopwatch.ElapsedMilliseconds);

                return Result<List<AddOnPurchaseDto>>.Success(purchaseDtos);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetMyAddOnPurchases FAILED: EventId={EventId}, BuyerEmail={BuyerEmail}, Duration={ElapsedMs}ms",
                    request.EventId, request.BuyerEmail, stopwatch.ElapsedMilliseconds);

                return Result<List<AddOnPurchaseDto>>.Failure($"Failed to retrieve purchases: {ex.Message}");
            }
        }
    }
}
