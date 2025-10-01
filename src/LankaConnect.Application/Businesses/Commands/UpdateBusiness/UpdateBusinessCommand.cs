using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Businesses.Commands.UpdateBusiness;

public record UpdateBusinessCommand(
    Guid Id,
    string Name,
    string Description,
    string ContactPhone,
    string ContactEmail,
    string Website,
    string Address,
    string City,
    string Province,
    string PostalCode,
    double Latitude,
    double Longitude,
    List<string> Categories,
    List<string> Tags
) : ICommand;