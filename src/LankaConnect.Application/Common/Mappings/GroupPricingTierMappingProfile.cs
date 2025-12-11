using AutoMapper;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Application.Common.Mappings;

/// <summary>
/// Phase 6D: AutoMapper profile for GroupPricingTier domain object to GroupPricingTierDto
/// </summary>
public class GroupPricingTierMappingProfile : Profile
{
    public GroupPricingTierMappingProfile()
    {
        CreateMap<GroupPricingTier, GroupPricingTierDto>()
            .ForMember(dest => dest.PricePerPerson, opt => opt.MapFrom(src => src.PricePerPerson.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.PricePerPerson.Currency))
            .ForMember(dest => dest.TierRange, opt => opt.MapFrom(src =>
                src.IsUnlimitedTier
                    ? $"{src.MinAttendees}+"
                    : src.MinAttendees == src.MaxAttendees
                        ? $"{src.MinAttendees}"
                        : $"{src.MinAttendees}-{src.MaxAttendees}"));
    }
}
