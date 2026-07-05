using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Presets;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Products.LankaEvents.Application.Commands.ApplyPresetToEvent;

/// <summary>
/// Slice 9.2 handler — replaces the from-preset+assign two-step. Single Unit-of-Work.
///
/// <para>Sequence:</para>
/// <list type="number">
///   <item>Validate inputs (non-empty ids, non-empty preset key).</item>
///   <item>Load the event (with its tier collection — needed for structural
///         validation and to enforce TicketingMode = Tiered).</item>
///   <item>Verify caller owns the event.</item>
///   <item>Build the layout from the preset factory.</item>
///   <item>Structural validation: <c>layout.ValidateForEvent(tiers,
///         requireTierMapping: false)</c>. Capacity invariants run for any
///         zone that IS mapped (preset zones are unmapped, so this is
///         currently a "≥ 1 zone exists" check).</item>
///   <item>Persist layout + flip event seating mode + attach layout, all
///         in the same UoW transaction.</item>
///   <item>Emit <c>layout.preset_selected</c> + <c>layout.created
///         (fromPreset=true)</c> metrics.</item>
/// </list>
///
/// <para>
/// If <c>event.VenueLayoutId</c> is already set, the new layout simply
/// overwrites it. The previous layout becomes an orphan (its <c>event_id</c>
/// still references this event but no event row points back at it). With the
/// Slice 9.3 read-path fix the orphan is invisible to <c>by-event</c>; the
/// next housekeeping pass / migration cleans it up. The architect explicitly
/// chose this over inline cascade-delete to keep the apply path narrow and
/// transactional (Rev 2 §3.4).
/// </para>
/// </summary>
public class ApplyPresetToEventCommandHandler
    : ICommandHandler<ApplyPresetToEventCommand, VenueLayoutDto>
{
    private readonly IVenueLayoutRepository _venueLayoutRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILayoutMetrics _metrics;
    private readonly ILogger<ApplyPresetToEventCommandHandler> _logger;

    public ApplyPresetToEventCommandHandler(
        IVenueLayoutRepository venueLayoutRepository,
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILayoutMetrics metrics,
        ILogger<ApplyPresetToEventCommandHandler> logger)
    {
        _venueLayoutRepository = venueLayoutRepository;
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<Result<VenueLayoutDto>> Handle(
        ApplyPresetToEventCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "ApplyPresetToEvent START: PresetId={PresetId}, EventId={EventId}, UserId={UserId}",
            request.PresetId, request.EventId, request.AppliedByUserId);

        if (string.IsNullOrWhiteSpace(request.PresetId))
            return Result<VenueLayoutDto>.Failure("Preset ID is required");

        if (request.EventId == Guid.Empty)
            return Result<VenueLayoutDto>.Failure("Event ID is required");

        if (request.AppliedByUserId == Guid.Empty)
            return Result<VenueLayoutDto>.Failure("Applied-by user ID is required");

        // Load event with change tracking — we will mutate it in this transaction.
        var @event = await _eventRepository.GetByIdAsync(
            request.EventId, trackChanges: true, cancellationToken);

        if (@event is null)
        {
            _logger.LogWarning("ApplyPresetToEvent: event {EventId} not found", request.EventId);
            return Result<VenueLayoutDto>.NotFound($"Event {request.EventId} not found");
        }

        if (@event.OrganizerId != request.AppliedByUserId)
        {
            _logger.LogWarning(
                "ApplyPresetToEvent: user {UserId} attempted to attach preset to event {EventId} owned by {OwnerId}",
                request.AppliedByUserId, @event.Id, @event.OrganizerId);
            return Result<VenueLayoutDto>.Forbidden(
                "You can only attach a layout to events you created.");
        }

        // Build layout from the preset factory. EventId is set so the layout's
        // event_id column matches; assignment to the event is then completed via
        // EnableAssignedSeating below (canonical via events.venue_layout_id).
        var presetResult = LayoutPresets.Create(
            request.PresetId,
            request.AppliedByUserId,
            request.EventId);

        if (presetResult.IsFailure)
        {
            _logger.LogInformation(
                "ApplyPresetToEvent: preset factory rejected — kind={Kind}, error={Error}",
                presetResult.ErrorKind, presetResult.Error);
            return Result<VenueLayoutDto>.Failure(presetResult.Error, presetResult.ErrorKind);
        }

        var layout = presetResult.Value;

        // Structural validation only (per architect Rev 3 — no auto-tier-mapping).
        // Apply-time we accept unmapped zones; publish-time enforces mapping.
        var structuralResult = layout.ValidateForEvent(@event.TicketTiers, requireTierMapping: false);
        if (structuralResult.IsFailure)
        {
            _logger.LogWarning(
                "ApplyPresetToEvent: structural validation failed — error={Error}",
                structuralResult.Error);
            return Result<VenueLayoutDto>.Failure(structuralResult.Error);
        }

        // Persist layout AND flip event seating mode in one UoW transaction.
        try
        {
            // Slice S1.5 — clean up the previously-attached layout AND any orphan rows
            // matching this event_id BEFORE inserting the new one. Closes the unique-
            // constraint collision class on `ix_venue_layouts_event_id_name` that
            // surfaced as user-reported "Change layout doesn't work" when re-applying
            // a preset whose name matched a stale orphan. Architect Rev 4 §A1 ruling.
            var orphansDeleted = await _venueLayoutRepository.HardDeleteByEventIdAsync(
                request.EventId, cancellationToken);
            if (orphansDeleted > 0)
            {
                _logger.LogInformation(
                    "ApplyPresetToEvent: cleanup deleted {OrphanCount} prior layout(s) for event {EventId}",
                    orphansDeleted, request.EventId);
            }

            await _venueLayoutRepository.AddAsync(layout, cancellationToken);

            var enableResult = @event.EnableAssignedSeating(layout.Id);
            if (enableResult.IsFailure)
            {
                _logger.LogWarning(
                    "ApplyPresetToEvent: EnableAssignedSeating rejected — error={Error}",
                    enableResult.Error);
                return Result<VenueLayoutDto>.Failure(enableResult.Error);
            }

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "ApplyPresetToEvent: persistence failed. EventId={EventId}, PresetId={PresetId}",
                request.EventId, request.PresetId);
            throw;
        }

        _logger.LogInformation(
            "ApplyPresetToEvent DONE: NewLayoutId={LayoutId}, EventId={EventId}, Capacity={Capacity}",
            layout.Id, request.EventId, layout.TotalCapacity);

        try
        {
            _metrics.PresetSelected(request.PresetId);
            _metrics.LayoutCreated(layout.LayoutType, fromPreset: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ApplyPresetToEvent: metric emission failed (commit already succeeded). LayoutId={LayoutId}",
                layout.Id);
        }

        return Result<VenueLayoutDto>.Success(VenueLayoutDtoMapper.Map(layout));
    }
}
