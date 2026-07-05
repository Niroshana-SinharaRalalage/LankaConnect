using AutoMapper;
using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Domain.Badges;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Services;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using DonationConfiguration = LankaConnect.Products.LankaEvents.Domain.ValueObjects.DonationConfiguration;
namespace LankaConnect.Products.LankaEvents.Application.Common;

public class EventMappingProfile : Profile
{
    public EventMappingProfile()
    {
        // Phase 6A.X: RevenueBreakdown -> RevenueBreakdownDto mapping
        CreateMap<RevenueBreakdown, RevenueBreakdownDto>()
            .ForMember(dest => dest.GrossAmount, opt => opt.MapFrom(src => src.GrossAmount.Amount))
            .ForMember(dest => dest.SalesTaxAmount, opt => opt.MapFrom(src => src.SalesTaxAmount.Amount))
            .ForMember(dest => dest.TaxableAmount, opt => opt.MapFrom(src => src.TaxableAmount.Amount))
            .ForMember(dest => dest.StripeFeeAmount, opt => opt.MapFrom(src => src.StripeFeeAmount.Amount))
            .ForMember(dest => dest.PlatformCommissionAmount, opt => opt.MapFrom(src => src.PlatformCommission.Amount))
            .ForMember(dest => dest.OrganizerPayoutAmount, opt => opt.MapFrom(src => src.OrganizerPayout.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.GrossAmount.Currency))
            .ForMember(dest => dest.SalesTaxRate, opt => opt.MapFrom(src => src.SalesTaxRate))
            .ForMember(dest => dest.TaxRateDisplay, opt => opt.MapFrom(src => $"{src.SalesTaxRate * 100:0.##}%"))
            .ForMember(dest => dest.TaxJurisdiction, opt => opt.Ignore()); // Jurisdiction tracking can be added later

        CreateMap<Event, EventDto>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title.Value))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description.Value))
            .ForMember(dest => dest.CurrentRegistrations, opt => opt.MapFrom(src => src.CurrentRegistrations))
            // Issue #51: MaxAttendeesPerRegistration - configurable by event organizer
            .ForMember(dest => dest.MaxAttendeesPerRegistration, opt => opt.MapFrom(src => src.MaxAttendeesPerRegistration))
            // Phase 6A.154: surface vanity slug as plain string (VO.Value).
            .ForMember(dest => dest.VanitySlug, opt => opt.MapFrom(src => src.VanitySlug != null ? src.VanitySlug.Value : null))
            .ForMember(dest => dest.IsFree, opt => opt.MapFrom(src => src.IsFree()))
            // Phase 8X — payment mode + external registration projection.
            .ForMember(dest => dest.PaymentMode, opt => opt.MapFrom(src => src.PaymentMode))
            .ForMember(dest => dest.ExternalRegistrationUrl, opt => opt.MapFrom(src => src.ExternalRegistration != null ? src.ExternalRegistration.Url : null))
            .ForMember(dest => dest.ExternalRegistrationInstructions, opt => opt.MapFrom(src => src.ExternalRegistration != null ? src.ExternalRegistration.Instructions : null))
            .ForMember(dest => dest.ExternalRegistrationVendorName, opt => opt.MapFrom(src => src.ExternalRegistration != null ? src.ExternalRegistration.VendorName : null))
            // Phase 7E paid-B-mode gate (review iteration 1, 2026-04-28): tells the frontend
            // whether the configured RegistrationMode is currently implementable. "deferred" is
            // the fail-safe default in EventDto; the mapper explicitly sets "active" when the
            // compatibility validator passes for this event's shape. Frontend's "coming soon"
            // panel hangs off "deferred". When 7E.3b ships paid B-mode + Stripe, the validator
            // gate is removed and this mapping starts emitting "active" for paid + B again.
            .ForMember(dest => dest.RegistrationModeStatus, opt => opt.MapFrom(src => ComputeRegistrationModeStatus(src)))
            // Location mapping (nullable)
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Location != null ? src.Location.Address.Street : null))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Location != null ? src.Location.Address.City : null))
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.Location != null ? src.Location.Address.State : null))
            .ForMember(dest => dest.ZipCode, opt => opt.MapFrom(src => src.Location != null ? src.Location.Address.ZipCode : null))
            .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Location != null ? src.Location.Address.Country : null))
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Location != null && src.Location.Coordinates != null ? src.Location.Coordinates.Latitude : (decimal?)null))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Location != null && src.Location.Coordinates != null ? src.Location.Coordinates.Longitude : (decimal?)null))
            // Phase 7C.1: Location Name + Secondary Location
            .ForMember(dest => dest.LocationName, opt => opt.MapFrom(src => src.Location != null ? src.Location.Name : null))
            .ForMember(dest => dest.SecondaryLocationType, opt => opt.MapFrom(src => src.SecondaryLocation != null ? src.SecondaryLocation.Type.ToString() : null))
            .ForMember(dest => dest.SecondaryLocationName, opt => opt.MapFrom(src => src.SecondaryLocation != null ? src.SecondaryLocation.Location.Name : null))
            .ForMember(dest => dest.SecondaryAddress, opt => opt.MapFrom(src => src.SecondaryLocation != null ? src.SecondaryLocation.Location.Address.Street : null))
            .ForMember(dest => dest.SecondaryCity, opt => opt.MapFrom(src => src.SecondaryLocation != null ? src.SecondaryLocation.Location.Address.City : null))
            .ForMember(dest => dest.SecondaryState, opt => opt.MapFrom(src => src.SecondaryLocation != null ? src.SecondaryLocation.Location.Address.State : null))
            .ForMember(dest => dest.SecondaryZipCode, opt => opt.MapFrom(src => src.SecondaryLocation != null ? src.SecondaryLocation.Location.Address.ZipCode : null))
            .ForMember(dest => dest.SecondaryCountry, opt => opt.MapFrom(src => src.SecondaryLocation != null ? src.SecondaryLocation.Location.Address.Country : null))
            .ForMember(dest => dest.SecondaryLatitude, opt => opt.MapFrom(src => src.SecondaryLocation != null && src.SecondaryLocation.Location.Coordinates != null ? src.SecondaryLocation.Location.Coordinates.Latitude : (decimal?)null))
            .ForMember(dest => dest.SecondaryLongitude, opt => opt.MapFrom(src => src.SecondaryLocation != null && src.SecondaryLocation.Location.Coordinates != null ? src.SecondaryLocation.Location.Coordinates.Longitude : (decimal?)null))
            .ForMember(dest => dest.HasSecondaryLocation, opt => opt.MapFrom(src => src.SecondaryLocation != null))
            // Phase 6A.97: Timezone mapping for consistent date/time display
            .ForMember(dest => dest.TimeZoneId, opt => opt.MapFrom(src => src.TimeZoneId))
            // Phase 8YA-2 TODO: render TimeZoneAbbreviation as null on TBD events.
            .ForMember(dest => dest.TimeZoneAbbreviation, opt => opt.MapFrom(src =>
                (src.TimeZoneId != null && src.StartDate.HasValue)
                    ? LankaConnect.Shared.Email.Helpers.EmailDateTimeHelper.GetTimezoneAbbreviation(src.TimeZoneId, src.StartDate.Value)
                    : null))
            // Legacy ticket price mapping (nullable - backward compatibility)
            .ForMember(dest => dest.TicketPriceAmount, opt => opt.MapFrom(src => src.TicketPrice != null ? src.TicketPrice.Amount : (decimal?)null))
            .ForMember(dest => dest.TicketPriceCurrency, opt => opt.MapFrom(src => src.TicketPrice != null ? src.TicketPrice.Currency : (LankaConnect.Domain.Shared.Enums.Currency?)null))
            // Session 21: Dual pricing mapping (from TicketPricing value object)
            .ForMember(dest => dest.AdultPriceAmount, opt => opt.MapFrom(src => src.Pricing != null ? src.Pricing.AdultPrice.Amount : (decimal?)null))
            .ForMember(dest => dest.AdultPriceCurrency, opt => opt.MapFrom(src => src.Pricing != null ? src.Pricing.AdultPrice.Currency : (LankaConnect.Domain.Shared.Enums.Currency?)null))
            .ForMember(dest => dest.ChildPriceAmount, opt => opt.MapFrom(src => src.Pricing != null && src.Pricing.ChildPrice != null ? src.Pricing.ChildPrice.Amount : (decimal?)null))
            .ForMember(dest => dest.ChildPriceCurrency, opt => opt.MapFrom(src => src.Pricing != null && src.Pricing.ChildPrice != null ? src.Pricing.ChildPrice.Currency : (LankaConnect.Domain.Shared.Enums.Currency?)null))
            .ForMember(dest => dest.ChildAgeLimit, opt => opt.MapFrom(src => src.Pricing != null ? src.Pricing.ChildAgeLimit : (int?)null))
            .ForMember(dest => dest.HasDualPricing, opt => opt.MapFrom(src => src.Pricing != null && src.Pricing.HasChildPricing))
            // Phase 6D: Group tiered pricing mapping
            .ForMember(dest => dest.PricingType, opt => opt.MapFrom(src => src.Pricing != null ? src.Pricing.Type.ToString() : (string?)null))
            .ForMember(dest => dest.GroupPricingTiers, opt => opt.MapFrom(src => src.Pricing != null && src.Pricing.HasGroupTiers ? src.Pricing.GroupTiers : new List<LankaConnect.Products.LankaEvents.Domain.ValueObjects.GroupPricingTier>()))
            .ForMember(dest => dest.HasGroupPricing, opt => opt.MapFrom(src => src.Pricing != null && src.Pricing.HasGroupTiers))
            // Media galleries (Epic 2 Phase 2)
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images))
            .ForMember(dest => dest.Videos, opt => opt.MapFrom(src => src.Videos))
            // Phase 6A.25: Badge Management System
            .ForMember(dest => dest.Badges, opt => opt.MapFrom(src => src.Badges))
            // Phase 6A.46: Display label based on lifecycle
            .ForMember(dest => dest.DisplayLabel, opt => opt.MapFrom(src => src.GetDisplayLabel()))
            // Organizer Contact Details (multiple contacts)
            .ForMember(dest => dest.PublishOrganizerContact, opt => opt.MapFrom(src => src.PublishOrganizerContact))
            .ForMember(dest => dest.OrganizerContacts, opt => opt.MapFrom(src => src.OrganizerContacts))
            // Phase 6A.X: Revenue Breakdown for paid events
            .ForMember(dest => dest.RevenueBreakdown, opt => opt.MapFrom(src => src.RevenueBreakdown))
            // Donation Feature: Donation configuration mapping
            .ForMember(dest => dest.DonationConfig, opt => opt.MapFrom(src => src.DonationConfig))
            // Financial Features: Collection, Sponsor, Add-On configuration mappings
            .ForMember(dest => dest.CollectionConfig, opt => opt.MapFrom(src => src.CollectionConfig))
            .ForMember(dest => dest.SponsorConfig, opt => opt.MapFrom(src => src.SponsorConfig))
            .ForMember(dest => dest.AddOnConfig, opt => opt.MapFrom(src => src.AddOnConfig))
            // Phase 2: Seating
            .ForMember(dest => dest.SeatingMode, opt => opt.MapFrom(src => src.SeatingMode))
            .ForMember(dest => dest.VenueLayoutId, opt => opt.MapFrom(src => src.VenueLayoutId))
            // Phase 8: Multi-Tier Ticketing
            .ForMember(dest => dest.TicketingMode, opt => opt.MapFrom(src => src.TicketingMode))
            .ForMember(dest => dest.HasTicketTiers, opt => opt.MapFrom(src => src.HasTicketTiers))
            .ForMember(dest => dest.TicketTiers, opt => opt.MapFrom(src =>
                src.TicketingMode == LankaConnect.Products.LankaEvents.Domain.Enums.TicketingMode.Tiered
                    ? src.GetActiveTiers().Select(t => new TicketTierDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Description = t.Description,
                        AdultPriceAmount = t.AdultPrice.Amount,
                        AdultPriceCurrency = t.AdultPrice.Currency,
                        ChildPriceAmount = t.ChildPrice != null ? t.ChildPrice.Amount : (decimal?)null,
                        ChildPriceCurrency = t.ChildPrice != null ? t.ChildPrice.Currency : (LankaConnect.Domain.Shared.Enums.Currency?)null,
                        ChildAgeLimit = t.ChildAgeLimit,
                        HasChildPricing = t.HasChildPricing,
                        Capacity = t.Capacity,
                        ReservedCount = t.ReservedCount,
                        AvailableQuantity = t.AvailableQuantity,
                        MaxPerUser = t.MaxPerUser,
                        SortOrder = t.SortOrder,
                        IsActive = t.IsActive,
                        IsFree = t.IsFree
                    }).ToList()
                    : new List<TicketTierDto>()))
            // Phase 6A.133: IsCurrentUserOrganizer is computed post-mapping by query handlers
            .ForMember(dest => dest.IsCurrentUserOrganizer, opt => opt.Ignore());

        // Donation Feature: DonationConfiguration -> DonationConfigurationDto mapping
        CreateMap<DonationConfiguration, DonationConfigurationDto>()
            .ForMember(dest => dest.SuggestedAmounts, opt => opt.MapFrom(src => src.SuggestedAmounts.ToList()));

        // Financial Features: Value object -> DTO mappings
        CreateMap<CollectionConfiguration, CollectionConfigurationDto>()
            .ForMember(dest => dest.SuggestedAmounts, opt => opt.MapFrom(src => src.SuggestedAmounts.ToList()));
        CreateMap<SponsorConfiguration, SponsorConfigurationDto>();
        CreateMap<AddOnConfiguration, AddOnConfigurationDto>();

        // EventOrganizerContact -> OrganizerContactDto mapping
        CreateMap<EventOrganizerContact, OrganizerContactDto>();

        // EventImage -> EventImageDto mapping (Epic 2 Phase 2)
        CreateMap<EventImage, EventImageDto>();

        // EventVideo -> EventVideoDto mapping (Epic 2 Phase 2)
        CreateMap<EventVideo, EventVideoDto>();

        // Phase 6A.25: Badge Management System mappings
        CreateMap<EventBadge, EventBadgeDto>();
        CreateMap<Badge, BadgeDto>();

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

        // Phase 6A.8: EventTemplate -> EventTemplateDto mapping
        CreateMap<EventTemplate, EventTemplateDto>();
    }

    /// <summary>
    /// Phase 7E paid-B-mode gate (review iteration 1, 2026-04-28): computes
    /// <see cref="EventDto.RegistrationModeStatus"/> from the source <see cref="Event"/>.
    ///
    /// Returns <c>"active"</c> if <see cref="RegistrationModeCompatibility.Check"/> passes for
    /// the event's current mode + shape; <c>"deferred"</c> otherwise. The only failure path
    /// today is the paid-B-mode gate (returns <see cref="RegistrationModeErrorCodes.PaidHeadCountDeferred"/>),
    /// but the contract is "is the configured mode currently compatible" — any future
    /// implementation gate plugs into the same path.
    ///
    /// Context fields populated from the event: <c>IsFreeAttendance</c>, <c>HasDualPricing</c>,
    /// <c>HasGroupTiers</c>, <c>HasTicketTiers</c>. The plan-7F axes (named seating, identity-bound
    /// add-ons, matrix pricing, names-required-on-ticket) are not yet representable on the
    /// <see cref="Event"/> aggregate and default to <c>false</c>; this matches the create / update
    /// command handlers' context-building behaviour today.
    /// </summary>
    private static string ComputeRegistrationModeStatus(Event src)
    {
        try
        {
            var ctx = new RegistrationModeContext
            {
                IsFreeAttendance = src.IsFree(),
                // Phase 8X.11 — pass payment-mode axis so ExternalPaid events render the
                // External mode as "active" (compatibility helper now switches on PaymentMode).
                PaymentMode = src.PaymentMode,
                HasDualPricing = src.Pricing != null && src.Pricing.HasChildPricing,
                HasGroupTiers = src.Pricing != null && src.Pricing.HasGroupTiers,
                HasTicketTiers = src.HasTicketTiers,
            };

            var result = RegistrationModeCompatibility.Check(src.RegistrationMode, ctx);
            return result.IsSuccess ? "active" : "deferred";
        }
        catch
        {
            // Fail-safe: any unexpected exception during status computation must NOT crash the
            // mapper. Return "deferred" so the frontend renders the "coming soon" panel rather
            // than letting an event detail GET fail or render a broken form. The exception is
            // already logged at the AutoMapper layer when it bubbles.
            return "deferred";
        }
    }
}
