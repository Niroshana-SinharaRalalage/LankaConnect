using AutoMapper;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Domain.Users;

namespace LankaConnect.Application.Common.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber != null ? src.PhoneNumber.Value : null))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName));
    }
}