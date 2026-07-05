using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventSponsorshipPackages;

/// <summary>
/// Phase 6A.156 — list all sponsorship packages for an event. When
/// <paramref name="IncludeInactive"/> is true (organizer view), inactive
/// packages are included; otherwise only active packages are returned
/// (anonymous/buyer-facing path, used in 6A.157+).
///
/// Sorted by SortOrder ASC inside the repository.
/// </summary>
public record GetEventSponsorshipPackagesQuery(
    Guid EventId,
    bool IncludeInactive
) : IQuery<IReadOnlyList<SponsorshipPackageDto>>;

public class GetEventSponsorshipPackagesQueryHandler
    : IQueryHandler<GetEventSponsorshipPackagesQuery, IReadOnlyList<SponsorshipPackageDto>>
{
    private readonly ISponsorshipPackageRepository _packageRepository;
    private readonly ILogger<GetEventSponsorshipPackagesQueryHandler> _logger;

    public GetEventSponsorshipPackagesQueryHandler(
        ISponsorshipPackageRepository packageRepository,
        ILogger<GetEventSponsorshipPackagesQueryHandler> logger)
    {
        _packageRepository = packageRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<SponsorshipPackageDto>>> Handle(
        GetEventSponsorshipPackagesQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEventSponsorshipPackages"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetEventSponsorshipPackages START: EventId={EventId}, IncludeInactive={IncludeInactive}",
                request.EventId, request.IncludeInactive);

            try
            {
                var packages = request.IncludeInactive
                    ? await _packageRepository.GetByEventIdAsync(request.EventId, cancellationToken)
                    : await _packageRepository.GetActiveByEventIdAsync(request.EventId, cancellationToken);

                var dtos = packages.Select(p => new SponsorshipPackageDto
                {
                    Id = p.Id,
                    EventId = p.EventId,
                    Name = p.Name,
                    Description = p.Description,
                    PriceAmount = p.Price.Amount,
                    PriceCurrency = p.Price.Currency.ToString(),
                    QuantityLimit = p.QuantityLimit,
                    QuantitySold = p.QuantitySold,
                    RemainingStock = p.RemainingStock,
                    IsActive = p.IsActive,
                    SortOrder = p.SortOrder,
                    ImageUrl = p.ImageUrl,
                    ImageBlobName = p.ImageBlobName,
                    Tier = p.Tier,
                    Perks = p.Perks?.ToList() ?? new List<string>(),
                    IncludedTicketCount = p.IncludedTicketCount,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                }).ToList();

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEventSponsorshipPackages COMPLETE: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    request.EventId, dtos.Count, stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<SponsorshipPackageDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetEventSponsorshipPackages FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result<IReadOnlyList<SponsorshipPackageDto>>.Failure(
                    $"Failed to retrieve sponsorship packages: {ex.Message}");
            }
        }
    }
}
