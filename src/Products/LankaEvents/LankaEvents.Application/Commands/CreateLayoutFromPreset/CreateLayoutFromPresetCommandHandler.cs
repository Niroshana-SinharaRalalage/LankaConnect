using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Presets;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Wave 8.5.g direct-SaveChanges
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Products.LankaEvents.Application.Commands.CreateLayoutFromPreset;

/// <summary>
/// Slice 6 Chunk S6.3: builds a new <see cref="LankaConnect.Products.LankaEvents.Domain.Entities.VenueLayout"/>
/// from the supplied preset ID, persists it, and emits
/// <c>layout.preset_selected</c> + <c>layout.created (from_preset=true)</c> metrics.
///
/// If an <c>EventId</c> is supplied the caller must have already validated event
/// ownership at the controller layer (matches the existing
/// <c>CreateVenueLayoutCommand</c> contract). The handler still double-checks that
/// the event exists and belongs to the current user — defence in depth against a
/// mis-wired frontend.
/// </summary>
public class CreateLayoutFromPresetCommandHandler
    : ICommandHandler<CreateLayoutFromPresetCommand, VenueLayoutDto>
{
    private readonly IVenueLayoutRepository _venueLayoutRepository;
    private readonly IEventRepository _eventRepository;
    private readonly LankaEventsDbContext _dbContext; // Wave 8.5.g direct-SaveChanges
    private readonly ILayoutMetrics _metrics;
    private readonly ILogger<CreateLayoutFromPresetCommandHandler> _logger;

    public CreateLayoutFromPresetCommandHandler(
        IVenueLayoutRepository venueLayoutRepository,
        IEventRepository eventRepository,
        LankaEventsDbContext dbContext,
        ILayoutMetrics metrics,
        ILogger<CreateLayoutFromPresetCommandHandler> logger)
    {
        _venueLayoutRepository = venueLayoutRepository;
        _eventRepository = eventRepository;
        _dbContext = dbContext;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<Result<VenueLayoutDto>> Handle(
        CreateLayoutFromPresetCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "CreateLayoutFromPreset START: PresetId={PresetId}, UserId={UserId}, EventId={EventId}",
            request.PresetId, request.CreatedByUserId, request.EventId);

        if (string.IsNullOrWhiteSpace(request.PresetId))
            return Result<VenueLayoutDto>.Failure("Preset ID is required");

        if (request.CreatedByUserId == Guid.Empty)
            return Result<VenueLayoutDto>.Failure("Creator user ID is required");

        // Event-ownership defence in depth when attaching the layout to an event.
        if (request.EventId.HasValue)
        {
            var @event = await _eventRepository.GetByIdAsync(
                request.EventId.Value, trackChanges: false, cancellationToken);

            if (@event is null)
            {
                _logger.LogWarning(
                    "CreateLayoutFromPreset: referenced event {EventId} not found",
                    request.EventId.Value);
                return Result<VenueLayoutDto>.NotFound(
                    $"Event {request.EventId.Value} not found");
            }

            if (@event.OrganizerId != request.CreatedByUserId)
            {
                _logger.LogWarning(
                    "CreateLayoutFromPreset: user {UserId} attempted to attach preset to event {EventId} owned by {OwnerId}",
                    request.CreatedByUserId, @event.Id, @event.OrganizerId);
                return Result<VenueLayoutDto>.Forbidden(
                    "You can only attach a layout to events you created.");
            }
        }

        // Build the layout from the preset factory.
        var presetResult = LayoutPresets.Create(
            request.PresetId,
            request.CreatedByUserId,
            request.EventId);

        if (presetResult.IsFailure)
        {
            _logger.LogInformation(
                "CreateLayoutFromPreset: preset factory rejected — kind={Kind}, error={Error}",
                presetResult.ErrorKind, presetResult.Error);
            return Result<VenueLayoutDto>.Failure(presetResult.Error, presetResult.ErrorKind);
        }

        var layout = presetResult.Value;

        try
        {
            await _venueLayoutRepository.AddAsync(layout, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "CreateLayoutFromPreset: persistence failed for preset {PresetId}",
                request.PresetId);
            throw;
        }

        _logger.LogInformation(
            "CreateLayoutFromPreset DONE: LayoutId={LayoutId}, Preset={PresetId}, Capacity={Capacity}",
            layout.Id, request.PresetId, layout.TotalCapacity);

        _metrics.PresetSelected(request.PresetId);
        _metrics.LayoutCreated(layout.LayoutType, fromPreset: true);

        return Result<VenueLayoutDto>.Success(VenueLayoutDtoMapper.Map(layout));
    }
}
