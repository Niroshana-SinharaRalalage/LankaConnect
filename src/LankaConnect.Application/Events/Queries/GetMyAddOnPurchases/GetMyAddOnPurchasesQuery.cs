using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetMyAddOnPurchases;

/// <summary>
/// Query to get add-on purchases for a specific buyer email and event.
/// Public access — allows buyers to see their purchase history on the event details page.
/// </summary>
public record GetMyAddOnPurchasesQuery(Guid EventId, string BuyerEmail) : IQuery<List<AddOnPurchaseDto>>;
