using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventByVanitySlug;

public class GetEventByVanitySlugQueryHandler : IQueryHandler<GetEventByVanitySlugQuery, EventDto?>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetEventByVanitySlugQueryHandler> _logger;

    public GetEventByVanitySlugQueryHandler(
        IEventRepository eventRepository,
        IMapper mapper,
        ILogger<GetEventByVanitySlugQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<EventDto?>> Handle(
        GetEventByVanitySlugQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var @event = await _eventRepository.GetByVanitySlugAsync(request.Slug, cancellationToken);
            if (@event == null)
            {
                _logger.LogInformation(
                    "GetEventByVanitySlug: slug not found - Slug={Slug}",
                    request.Slug);
                return Result<EventDto?>.Success(null);
            }

            var dto = _mapper.Map<EventDto>(@event);
            return Result<EventDto?>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "GetEventByVanitySlug: unexpected error - Slug={Slug}, Error={Error}",
                request.Slug, ex.Message);
            throw;
        }
    }
}
