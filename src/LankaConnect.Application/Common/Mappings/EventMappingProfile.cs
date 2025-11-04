using AutoMapper;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Common.Mappings;

public class EventMappingProfile : Profile
{
    public EventMappingProfile()
    {
        CreateMap<Event, EventDto>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title.Value))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description.Value))
            .ForMember(dest => dest.CurrentRegistrations, opt => opt.MapFrom(src => src.CurrentRegistrations))
            .ForMember(dest => dest.IsFree, opt => opt.MapFrom(src => src.IsFree()))
            // Location mapping (nullable)
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Location != null ? src.Location.Address.Street : null))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Location != null ? src.Location.Address.City : null))
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.Location != null ? src.Location.Address.State : null))
            .ForMember(dest => dest.ZipCode, opt => opt.MapFrom(src => src.Location != null ? src.Location.Address.ZipCode : null))
            .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Location != null ? src.Location.Address.Country : null))
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Location != null && src.Location.Coordinates != null ? src.Location.Coordinates.Latitude : (decimal?)null))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Location != null && src.Location.Coordinates != null ? src.Location.Coordinates.Longitude : (decimal?)null))
            // Ticket price mapping (nullable)
            .ForMember(dest => dest.TicketPriceAmount, opt => opt.MapFrom(src => src.TicketPrice != null ? src.TicketPrice.Amount : (decimal?)null))
            .ForMember(dest => dest.TicketPriceCurrency, opt => opt.MapFrom(src => src.TicketPrice != null ? src.TicketPrice.Currency : (Domain.Shared.Enums.Currency?)null))
            // Media galleries (Epic 2 Phase 2)
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images))
            .ForMember(dest => dest.Videos, opt => opt.MapFrom(src => src.Videos));

        // EventImage -> EventImageDto mapping (Epic 2 Phase 2)
        CreateMap<EventImage, EventImageDto>();

        // EventVideo -> EventVideoDto mapping (Epic 2 Phase 2)
        CreateMap<EventVideo, EventVideoDto>();

        // Event -> EventSearchResultDto mapping (Epic 2 Phase 3 - Full-Text Search)
        // Same mappings as EventDto, plus SearchRelevance (set to 0, will be populated by repository)
        CreateMap<Event, EventSearchResultDto>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title.Value))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description.Value))
            .ForMember(dest => dest.CurrentRegistrations, opt => opt.MapFrom(src => src.CurrentRegistrations))
            .ForMember(dest => dest.IsFree, opt => opt.MapFrom(src => src.IsFree()))
            .ForMember(dest => dest.TicketPrice, opt => opt.MapFrom(src => src.TicketPrice != null ? src.TicketPrice.Amount : (decimal?)null))
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images))
            .ForMember(dest => dest.Videos, opt => opt.MapFrom(src => src.Videos))
            .ForMember(dest => dest.SearchRelevance, opt => opt.MapFrom(src => 0m)); // Default to 0, repository will set actual value

        // Registration -> RsvpDto mapping
        CreateMap<Registration, RsvpDto>()
            .ForMember(dest => dest.EventTitle, opt => opt.Ignore())
            .ForMember(dest => dest.EventStartDate, opt => opt.Ignore())
            .ForMember(dest => dest.EventEndDate, opt => opt.Ignore())
            .ForMember(dest => dest.EventStatus, opt => opt.Ignore());
    }
}
