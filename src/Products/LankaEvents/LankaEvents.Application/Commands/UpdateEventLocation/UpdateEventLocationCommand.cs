using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateEventLocation;

public record UpdateEventLocationCommand(
    Guid EventId,
    string LocationAddress,
    string LocationCity,
    string? LocationState = null,
    string? LocationZipCode = null,
    string? LocationCountry = null,
    decimal? LocationLatitude = null,
    decimal? LocationLongitude = null
) : ICommand;
