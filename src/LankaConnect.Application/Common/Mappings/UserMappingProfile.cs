using AutoMapper;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Domain.Users;

namespace LankaConnect.Application.Common.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber != null ? src.PhoneNumber.Value : null))
            .ForMember(dest => dest.Bio, opt => opt.MapFrom(src => src.Bio))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
            // Epic 1 Phase 3: Profile Enhancement Fields
            .ForMember(dest => dest.ProfilePhotoUrl, opt => opt.MapFrom(src => src.ProfilePhotoUrl))
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location != null ? new UserLocationDto
            {
                City = src.Location.City,
                State = src.Location.State,
                ZipCode = src.Location.ZipCode,
                Country = src.Location.Country
            } : null))
            .ForMember(dest => dest.CulturalInterests, opt => opt.MapFrom(src => src.CulturalInterests.Select(ci => ci.Code).ToList()))
            .ForMember(dest => dest.Languages, opt => opt.MapFrom(src => src.Languages.Select(l => new LanguageDto
            {
                LanguageCode = l.Language.Code,
                ProficiencyLevel = l.Proficiency
            }).ToList()))
            // Phase 5B/6A.9: User Preferred Metro Areas
            .ForMember(dest => dest.PreferredMetroAreas, opt => opt.MapFrom(src => src.PreferredMetroAreaIds));
    }
}