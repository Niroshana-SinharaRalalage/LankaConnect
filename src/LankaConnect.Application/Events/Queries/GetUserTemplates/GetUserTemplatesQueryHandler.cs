using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Queries.GetUserTemplates;

/// <summary>
/// Slice 8 S8.10 handler. Thin wrapper around
/// <see cref="IVenueLayoutRepository.GetTemplatesByUserAsync"/> — the repo
/// already filters by <c>(CreatedByUserId, IsTemplate)</c> and orders newest
/// first; this handler maps each aggregate to a <see cref="VenueLayoutDto"/>
/// (with empty tier-assignment lists since templates are tier-free).
/// </summary>
public class GetUserTemplatesQueryHandler
    : IQueryHandler<GetUserTemplatesQuery, IReadOnlyList<VenueLayoutDto>>
{
    private readonly IVenueLayoutRepository _venueLayoutRepository;
    private readonly ILogger<GetUserTemplatesQueryHandler> _logger;

    public GetUserTemplatesQueryHandler(
        IVenueLayoutRepository venueLayoutRepository,
        ILogger<GetUserTemplatesQueryHandler> logger)
    {
        _venueLayoutRepository = venueLayoutRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<VenueLayoutDto>>> Handle(
        GetUserTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
            return Result<IReadOnlyList<VenueLayoutDto>>.Failure("User ID is required");

        try
        {
            var templates = await _venueLayoutRepository.GetTemplatesByUserAsync(
                request.UserId, cancellationToken);

            // Templates carry no tier assignments by S8.9b's design (TicketTier
            // belongs to the Event aggregate; templates have no event). Pass
            // null for the tier dictionary — the mapper returns empty
            // TicketTierIds per zone/table.
            var dtos = templates
                .Select(t => VenueLayoutDtoMapper.Map(t, tiersByAssignable: null))
                .ToList();

            _logger.LogInformation(
                "GetUserTemplates: UserId={UserId}, Count={Count}",
                request.UserId, dtos.Count);

            return Result<IReadOnlyList<VenueLayoutDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "GetUserTemplates: failed to list templates for user {UserId}",
                request.UserId);
            throw;
        }
    }
}
