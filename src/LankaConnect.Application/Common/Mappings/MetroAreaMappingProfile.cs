using AutoMapper;
using LankaConnect.Application.MetroAreas.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here

namespace LankaConnect.Application.Common.Mappings;

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
