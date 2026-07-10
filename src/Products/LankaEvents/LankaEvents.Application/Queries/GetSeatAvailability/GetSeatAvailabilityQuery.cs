using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetSeatAvailability;

/// <summary>
/// Gets all seats for an event with their runtime availability status.
/// Combines structural data (from VenueLayout) with runtime state (from SeatHold + SeatReservation).
/// </summary>
public record GetSeatAvailabilityQuery(Guid EventId) : IQuery<List<SeatAvailabilityDto>>;
