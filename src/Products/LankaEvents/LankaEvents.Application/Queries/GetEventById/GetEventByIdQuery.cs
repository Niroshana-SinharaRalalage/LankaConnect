using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventById;

public record GetEventByIdQuery(Guid Id) : IQuery<EventDto?>;
