using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business.Enums;

namespace LankaConnect.Application.Businesses.Commands.CreateBusiness;

public record CreateBusinessCommand(
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
    BusinessCategory Category,
    Guid OwnerId,
    List<string> Categories,
    List<string> Tags
) : ICommand<Guid>;