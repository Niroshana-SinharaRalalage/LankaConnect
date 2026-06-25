using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.ApplyTemplateToEvent;

/// <summary>
/// Slice 9.2 handler — replaces the from-template+assign two-step. Mirrors
/// <see cref="ApplyPresetToEvent.ApplyPresetToEventCommandHandler"/> for
/// user-saved templates. Single Unit-of-Work transaction.
/// </summary>
public class ApplyTemplateToEventCommandHandler
    : ICommandHandler<ApplyTemplateToEventCommand, VenueLayoutDto>
{
    private readonly IVenueLayoutRepository _venueLayoutRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILayoutMetrics _metrics;
    private readonly ILogger<ApplyTemplateToEventCommandHandler> _logger;

    public ApplyTemplateToEventCommandHandler(
        IVenueLayoutRepository venueLayoutRepository,
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILayoutMetrics metrics,
        ILogger<ApplyTemplateToEventCommandHandler> logger)
    {
        _venueLayoutRepository = venueLayoutRepository;
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<Result<VenueLayoutDto>> Handle(
        ApplyTemplateToEventCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "ApplyTemplateToEvent START: SourceTemplateId={SourceTemplateId}, EventId={EventId}, UserId={UserId}, LayoutName={LayoutName}",
            request.SourceTemplateId, request.EventId, request.AppliedByUserId, request.LayoutName);

        if (request.SourceTemplateId == Guid.Empty)
            return Result<VenueLayoutDto>.Failure("Source template ID is required");

        if (request.EventId == Guid.Empty)
            return Result<VenueLayoutDto>.Failure("Event ID is required");

        if (request.AppliedByUserId == Guid.Empty)
            return Result<VenueLayoutDto>.Failure("Applied-by user ID is required");

        // Load source template — full structure for the structural clone.
        VenueLayout? source;
        try
        {
            source = await _venueLayoutRepository.GetWithZonesAndSeatsAsync(
                request.SourceTemplateId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "ApplyTemplateToEvent: failed to load source template {SourceTemplateId}",
                request.SourceTemplateId);
            throw;
        }

        if (source is null)
            return Result<VenueLayoutDto>.NotFound(
                $"Source template {request.SourceTemplateId} not found");

        if (!source.IsTemplate)
        {
            _logger.LogWarning(
                "ApplyTemplateToEvent: source {SourceId} is not a template",
                request.SourceTemplateId);
            return Result<VenueLayoutDto>.Failure(
                "Source layout is not a template — only templates can be applied via this endpoint",
                ErrorKind.Validation);
        }

        if (source.CreatedByUserId != request.AppliedByUserId)
        {
            _logger.LogWarning(
                "ApplyTemplateToEvent: user {UserId} attempted to apply template {TemplateId} owned by {OwnerId}",
                request.AppliedByUserId, request.SourceTemplateId, source.CreatedByUserId);
            return Result<VenueLayoutDto>.Forbidden("You can only apply templates you own.");
        }

        // Load event with change tracking — we will mutate it in this transaction.
        var @event = await _eventRepository.GetByIdAsync(
            request.EventId, trackChanges: true, cancellationToken);

        if (@event is null)
        {
            _logger.LogWarning("ApplyTemplateToEvent: target event {EventId} not found", request.EventId);
            return Result<VenueLayoutDto>.NotFound($"Event {request.EventId} not found");
        }

        if (@event.OrganizerId != request.AppliedByUserId)
        {
            _logger.LogWarning(
                "ApplyTemplateToEvent: user {UserId} attempted to attach to event {EventId} owned by {OwnerId}",
                request.AppliedByUserId, request.EventId, @event.OrganizerId);
            return Result<VenueLayoutDto>.Forbidden(
                "You can only attach a layout to events you created.");
        }

        var layoutName = string.IsNullOrWhiteSpace(request.LayoutName)
            ? source.Name
            : request.LayoutName;

        // Domain factory — drops tier mappings (per S8.10 design); enforces
        // source-must-be-template + name length etc.
        var cloneResult = VenueLayout.CloneFromTemplate(
            source, request.EventId, layoutName, request.AppliedByUserId);

        if (cloneResult.IsFailure)
        {
            _logger.LogInformation(
                "ApplyTemplateToEvent: clone factory rejected — error={Error}",
                cloneResult.Error);
            return Result<VenueLayoutDto>.Failure(cloneResult.Error, cloneResult.ErrorKind);
        }

        var clone = cloneResult.Value;

        // Structural-only validation per Slice 9.1 flag.
        var structuralResult = clone.ValidateForEvent(@event.TicketTiers, requireTierMapping: false);
        if (structuralResult.IsFailure)
        {
            _logger.LogWarning(
                "ApplyTemplateToEvent: structural validation failed — error={Error}",
                structuralResult.Error);
            return Result<VenueLayoutDto>.Failure(structuralResult.Error);
        }

        // Persist + flip event seating mode in one UoW transaction.
        try
        {
            // Slice S1.5 — clean prior layouts + orphans for this event before INSERT.
            // See ApplyPresetToEventCommandHandler for the rationale.
            var orphansDeleted = await _venueLayoutRepository.HardDeleteByEventIdAsync(
                request.EventId, cancellationToken);
            if (orphansDeleted > 0)
            {
                _logger.LogInformation(
                    "ApplyTemplateToEvent: cleanup deleted {OrphanCount} prior layout(s) for event {EventId}",
                    orphansDeleted, request.EventId);
            }

            await _venueLayoutRepository.AddAsync(clone, cancellationToken);

            var enableResult = @event.EnableAssignedSeating(clone.Id);
            if (enableResult.IsFailure)
            {
                _logger.LogWarning(
                    "ApplyTemplateToEvent: EnableAssignedSeating rejected — error={Error}",
                    enableResult.Error);
                return Result<VenueLayoutDto>.Failure(enableResult.Error);
            }

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "ApplyTemplateToEvent: persistence failed. SourceTemplateId={SourceTemplateId}, NewLayoutId={NewLayoutId}",
                request.SourceTemplateId, clone.Id);
            throw;
        }

        _logger.LogInformation(
            "ApplyTemplateToEvent DONE: SourceTemplateId={SourceTemplateId}, NewLayoutId={NewLayoutId}, EventId={EventId}, Capacity={Capacity}",
            request.SourceTemplateId, clone.Id, request.EventId, clone.TotalCapacity);

        try
        {
            _metrics.LayoutCreated(clone.LayoutType, fromPreset: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ApplyTemplateToEvent: metric emission failed (commit already succeeded). NewLayoutId={NewLayoutId}",
                clone.Id);
        }

        return Result<VenueLayoutDto>.Success(VenueLayoutDtoMapper.Map(clone));
    }
}
