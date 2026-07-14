using AutoMapper;
using LankaConnect.SharedKernel.Geo.MetroAreas.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
// Sprint-Day 10 (2026-07-14, Consult #26 Q1) relocated from LankaConnect.Application/MetroAreas/Mappings/
// to align physical location with MetroArea ownership (LankaEvents-owned per Consult #7 Delta).
namespace LankaConnect.Products.LankaEvents.Application.Mapping;

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
