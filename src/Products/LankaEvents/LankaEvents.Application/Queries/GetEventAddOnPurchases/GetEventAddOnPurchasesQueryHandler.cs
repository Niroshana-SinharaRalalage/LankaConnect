using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventAddOnPurchases;

/// <summary>
/// Handles retrieving all add-on definitions and purchases for an event with summary statistics.
/// </summary>
public class GetEventAddOnPurchasesQueryHandler : IQueryHandler<GetEventAddOnPurchasesQuery, EventAddOnPurchasesResponse>
{
    private readonly IEventRepository _eventRepository;
    private readonly IAddOnDefinitionRepository _addOnDefinitionRepository;
    private readonly IAddOnPurchaseRepository _addOnPurchaseRepository;
    private readonly ILogger<GetEventAddOnPurchasesQueryHandler> _logger;

    public GetEventAddOnPurchasesQueryHandler(
        IEventRepository eventRepository,
        IAddOnDefinitionRepository addOnDefinitionRepository,
        IAddOnPurchaseRepository addOnPurchaseRepository,
        ILogger<GetEventAddOnPurchasesQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _addOnDefinitionRepository = addOnDefinitionRepository;
        _addOnPurchaseRepository = addOnPurchaseRepository;
        _logger = logger;
    }

    public async Task<Result<EventAddOnPurchasesResponse>> Handle(
        GetEventAddOnPurchasesQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEventAddOnPurchases"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetEventAddOnPurchases START: EventId={EventId}",
                request.EventId);

            try
            {
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                    return Result<EventAddOnPurchasesResponse>.Failure("Event not found");

                var definitions = await _addOnDefinitionRepository.GetByEventIdAsync(request.EventId, cancellationToken);
                var purchases = await _addOnPurchaseRepository.GetByEventIdAsync(request.EventId, cancellationToken);

                // Build a lookup for definition names to enrich purchase DTOs
                var definitionLookup = definitions.ToDictionary(d => d.Id, d => d.Name);

                var definitionDtos = definitions.Select(d => new AddOnDefinitionDto
                {
                    Id = d.Id,
                    EventId = d.EventId,
                    Name = d.Name,
                    Description = d.Description,
                    Price = d.Price.Amount,
                    Currency = d.Price.Currency.ToString(),
                    QuantityLimit = d.QuantityLimit,
                    QuantitySold = d.QuantitySold,
                    RemainingStock = d.RemainingStock,
                    IsActive = d.IsActive,
                    SortOrder = d.SortOrder,
                    ImageUrl = d.ImageUrl,
                    ImageBlobName = d.ImageBlobName,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt
                }).ToList();

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
                    StripeFeeAmount = p.StripeFeeAmount?.Amount,
                    PlatformCommissionAmount = p.PlatformCommissionAmount?.Amount,
                    OrganizerPayoutAmount = p.OrganizerPayoutAmount?.Amount,
                    CreatedAt = p.CreatedAt,
                    PaymentCompletedAt = p.PaymentCompletedAt
                }).ToList();

                var completedPurchases = purchases.Where(p => p.Status == AddOnPurchaseStatus.Completed).ToList();
                var totalRevenue = completedPurchases.Sum(p => p.TotalAmount.Amount);
                var currency = completedPurchases.FirstOrDefault()?.UnitPrice.Currency.ToString() ?? "USD";
                var totalItemsSold = completedPurchases.Sum(p => p.Quantity);

                var summary = new AddOnPurchaseSummaryDto
                {
                    TotalPurchases = purchases.Count,
                    CompletedPurchases = completedPurchases.Count,
                    TotalRevenue = totalRevenue,
                    Currency = currency,
                    TotalStripeFees = completedPurchases.Sum(p => p.StripeFeeAmount?.Amount ?? 0),
                    TotalPlatformCommission = completedPurchases.Sum(p => p.PlatformCommissionAmount?.Amount ?? 0),
                    TotalOrganizerPayout = completedPurchases.Sum(p => p.OrganizerPayoutAmount?.Amount ?? 0),
                    TotalItemsSold = totalItemsSold
                };

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEventAddOnPurchases COMPLETE: EventId={EventId}, Definitions={Definitions}, TotalPurchases={Total}, CompletedPurchases={Completed}, TotalRevenue={Revenue}, TotalItemsSold={ItemsSold}, Duration={ElapsedMs}ms",
                    request.EventId, definitions.Count, purchases.Count, completedPurchases.Count, totalRevenue, totalItemsSold, stopwatch.ElapsedMilliseconds);

                return Result<EventAddOnPurchasesResponse>.Success(new EventAddOnPurchasesResponse
                {
                    EventId = request.EventId,
                    EventTitle = @event.Title.Value,
                    Definitions = definitionDtos,
                    Purchases = purchaseDtos,
                    Summary = summary
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetEventAddOnPurchases FAILED: EventId={EventId}, Duration={ElapsedMs}ms",
                    request.EventId, stopwatch.ElapsedMilliseconds);

                return Result<EventAddOnPurchasesResponse>.Failure($"Failed to retrieve add-on purchases: {ex.Message}");
            }
        }
    }
}
