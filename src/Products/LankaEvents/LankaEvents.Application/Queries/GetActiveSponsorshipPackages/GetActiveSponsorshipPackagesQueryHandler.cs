using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetActiveSponsorshipPackages;

/// <summary>
/// Phase 6A.157 — handles <see cref="GetActiveSponsorshipPackagesQuery"/>.
/// Returns active, purchasable packages for the event OR an empty list when
/// any gate fails (event not Published, sponsors disabled, packages disabled,
/// event not found). NEVER errors — empty list keeps the public FE quiet.
/// </summary>
public class GetActiveSponsorshipPackagesQueryHandler
    : IQueryHandler<GetActiveSponsorshipPackagesQuery, IReadOnlyList<SponsorshipPackagePublicDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly ISponsorshipPackageRepository _packageRepository;
    private readonly ILogger<GetActiveSponsorshipPackagesQueryHandler> _logger;

    public GetActiveSponsorshipPackagesQueryHandler(
        IEventRepository eventRepository,
        ISponsorshipPackageRepository packageRepository,
        ILogger<GetActiveSponsorshipPackagesQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _packageRepository = packageRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<SponsorshipPackagePublicDto>>> Handle(
        GetActiveSponsorshipPackagesQuery request,
        CancellationToken cancellationToken)
    {
        var empty = (IReadOnlyList<SponsorshipPackagePublicDto>)Array.Empty<SponsorshipPackagePublicDto>();

        try
        {
            var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogDebug(
                    "GetActiveSponsorshipPackages: event {EventId} not found, returning empty",
                    request.EventId);
                return Result<IReadOnlyList<SponsorshipPackagePublicDto>>.Success(empty);
            }

            // Server-side filter chain — any failure short-circuits to empty
            // so the FE simply hides the package grid (no error UX needed).
            if (@event.Status != LankaConnect.Products.LankaEvents.Domain.Enums.EventStatus.Published)
                return Result<IReadOnlyList<SponsorshipPackagePublicDto>>.Success(empty);

            if (!@event.AreSponsorsEnabled())
                return Result<IReadOnlyList<SponsorshipPackagePublicDto>>.Success(empty);

            if (@event.SponsorConfig?.EnablePackages != true)
                return Result<IReadOnlyList<SponsorshipPackagePublicDto>>.Success(empty);

            var packages = await _packageRepository.GetActiveByEventIdAsync(request.EventId, cancellationToken);

            // Additional client-protection filter — exclude sold-out packages
            // so the buyer never sees an unselectable card. Sort already done
            // by SortOrder ASC in the repo.
            var dtos = packages
                .Where(p => p.QuantityLimit == null || p.QuantitySold < p.QuantityLimit.Value)
                .Select(p => new SponsorshipPackagePublicDto
                {
                    Id = p.Id,
                    EventId = p.EventId,
                    Name = p.Name,
                    Description = p.Description,
                    PriceAmount = p.Price.Amount,
                    PriceCurrency = p.Price.Currency.ToString(),
                    RemainingStock = p.QuantityLimit.HasValue
                        ? Math.Max(0, p.QuantityLimit.Value - p.QuantitySold)
                        : (int?)null,
                    IsSoldOut = p.QuantityLimit.HasValue && p.QuantitySold >= p.QuantityLimit.Value,
                    SortOrder = p.SortOrder,
                    ImageUrl = p.ImageUrl,
                    Tier = p.Tier,
                    Perks = p.Perks.ToList(),
                    IncludedTicketCount = p.IncludedTicketCount
                })
                .ToList();

            return Result<IReadOnlyList<SponsorshipPackagePublicDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "GetActiveSponsorshipPackages: exception for event {EventId}, returning empty",
                request.EventId);
            return Result<IReadOnlyList<SponsorshipPackagePublicDto>>.Success(empty);
        }
    }
}
