using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Presets;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetLayoutPresets;

/// <summary>
/// Slice 6 Chunk S6.2: projects the static <see cref="LayoutPresets.All"/> list onto
/// <see cref="LayoutPresetDto"/>. Pure in-memory — no DB, no external calls.
/// Logged at debug level only; this endpoint is called once per organizer session
/// when the preset modal opens.
/// </summary>
public class GetLayoutPresetsQueryHandler
    : IQueryHandler<GetLayoutPresetsQuery, IReadOnlyList<LayoutPresetDto>>
{
    private readonly ILogger<GetLayoutPresetsQueryHandler> _logger;

    public GetLayoutPresetsQueryHandler(ILogger<GetLayoutPresetsQueryHandler> logger)
    {
        _logger = logger;
    }

    public Task<Result<IReadOnlyList<LayoutPresetDto>>> Handle(
        GetLayoutPresetsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("GetLayoutPresets: returning {Count} presets", LayoutPresets.All.Count);

        IReadOnlyList<LayoutPresetDto> dtos = LayoutPresets.All
            .Select(p => new LayoutPresetDto(
                p.Id, p.Name, p.Description, p.LayoutType, p.TotalCapacity, p.ThumbnailUrl))
            .ToList()
            .AsReadOnly();

        return Task.FromResult(Result<IReadOnlyList<LayoutPresetDto>>.Success(dtos));
    }
}
