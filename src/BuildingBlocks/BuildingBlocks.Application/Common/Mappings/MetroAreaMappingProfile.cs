using AutoMapper;
using LankaConnect.Application.MetroAreas.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
namespace LankaConnect.BuildingBlocks.Application.Common.Mappings;

/// <summary>
/// AutoMapper profile for MetroArea entity
/// Phase 5C: Metro Areas API
/// </summary>
public class MetroAreaMappingProfile : Profile
{
    public MetroAreaMappingProfile()
    {
        // MetroArea -> MetroAreaDto
        CreateMap<MetroArea, MetroAreaDto>();
    }
}
