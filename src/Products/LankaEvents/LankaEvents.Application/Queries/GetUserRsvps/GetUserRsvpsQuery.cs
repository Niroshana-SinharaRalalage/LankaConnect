using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetUserRsvps;

public record GetUserRsvpsQuery(Guid UserId) : IQuery<IReadOnlyList<RsvpDto>>;
