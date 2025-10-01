using AutoMapper;
using LankaConnect.Application.Businesses.Common;
using LankaConnect.Domain.Business;

namespace LankaConnect.Application.Common.Mappings;

public class BusinessMappingProfile : Profile
{
    public BusinessMappingProfile()
    {
        CreateMap<Business, BusinessDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Profile.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Profile.Description))
            .ForMember(dest => dest.ContactPhone, opt => opt.MapFrom(src => src.ContactInfo.PhoneNumber != null ? src.ContactInfo.PhoneNumber.Value : string.Empty))
            .ForMember(dest => dest.ContactEmail, opt => opt.MapFrom(src => src.ContactInfo.Email != null ? src.ContactInfo.Email.Value : string.Empty))
            .ForMember(dest => dest.Website, opt => opt.MapFrom(src => src.ContactInfo.Website ?? string.Empty))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Location.Address.Street))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Location.Address.City))
            .ForMember(dest => dest.Province, opt => opt.MapFrom(src => src.Location.Address.State))
            .ForMember(dest => dest.PostalCode, opt => opt.MapFrom(src => src.Location.Address.ZipCode))
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Location.Coordinates != null ? (double)src.Location.Coordinates.Latitude : 0))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Location.Coordinates != null ? (double)src.Location.Coordinates.Longitude : 0))
            .ForMember(dest => dest.Categories, opt => opt.Ignore()) // TODO: Map from domain
            .ForMember(dest => dest.Tags, opt => opt.Ignore()); // TODO: Map from domain

        CreateMap<Service, ServiceDto>()
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price != null ? src.Price.Amount : 0m))
            .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => src.IsActive));
    }
}