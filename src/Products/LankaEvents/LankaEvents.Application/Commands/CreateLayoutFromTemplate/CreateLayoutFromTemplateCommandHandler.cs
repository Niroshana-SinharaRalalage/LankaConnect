using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Wave 8.5.g direct-SaveChanges
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Products.LankaEvents.Application.Commands.CreateLayoutFromTemplate;

/// <summary>
/// Slice 8 S8.10 handler for <see cref="CreateLayoutFromTemplateCommand"/>.
/// Flow:
/// <list type="number">
///   <item>Validate input (non-empty IDs).</item>
///   <item>Load the source template aggregate (full structure).</item>
///   <item>Verify the caller owns the template — the source must be
///         <c>CreatedByUserId == request.CreatedByUserId</c> AND
///         <c>IsTemplate == true</c>. (Defence in depth; the controller
///         passes the authenticated user as <c>CreatedByUserId</c>.)</item>
///   <item>Verify the caller owns the target event — same check the
///         preset flow uses (<c>Event.OrganizerId == request.CreatedByUserId</c>).</item>
///   <item>Call <see cref="VenueLayout.CloneFromTemplate"/> — domain owns
///         the structural-clone invariants and the source-must-be-template gate.</item>
///   <item>Persist via <see cref="IVenueLayoutRepository.AddAsync"/> +
///         <see cref="IUnitOfWork.CommitAsync"/>.</item>
///   <item>Emit <c>layout.created (fromPreset=false)</c> for dashboard parity
///         with the preset / save-as-template create paths.</item>
/// </list>
/// </summary>
public class CreateLayoutFromTemplateCommandHandler
    : ICommandHandler<CreateLayoutFromTemplateCommand, VenueLayoutDto>
{
    private readonly IVenueLayoutRepository _venueLayoutRepository;
    private readonly IEventRepository _eventRepository;
    private readonly LankaEventsDbContext _dbContext; // Wave 8.5.g direct-SaveChanges
    private readonly ILayoutMetrics _metrics;
    private readonly ILogger<CreateLayoutFromTemplateCommandHandler> _logger;

    public CreateLayoutFromTemplateCommandHandler(
        IVenueLayoutRepository venueLayoutRepository,
        IEventRepository eventRepository,
        LankaEventsDbContext dbContext,
        ILayoutMetrics metrics,
        ILogger<CreateLayoutFromTemplateCommandHandler> logger)
    {
        _venueLayoutRepository = venueLayoutRepository;
        _eventRepository = eventRepository;
        _dbContext = dbContext;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<Result<VenueLayoutDto>> Handle(
        CreateLayoutFromTemplateCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "CreateLayoutFromTemplate START: SourceTemplateId={SourceTemplateId}, " +
            "EventId={EventId}, UserId={UserId}, LayoutName={LayoutName}",
            request.SourceTemplateId, request.EventId, request.CreatedByUserId, request.LayoutName);

        if (request.SourceTemplateId == Guid.Empty)
            return Result<VenueLayoutDto>.Failure("Source template ID is required");

        if (request.CreatedByUserId == Guid.Empty)
            return Result<VenueLayoutDto>.Failure("Creator user ID is required");

        if (request.EventId == Guid.Empty)
            return Result<VenueLayoutDto>.Failure("Target event ID is required");

        // Load the source template (full structure — zones / tables / decorations / seats).
        VenueLayout? source;
        try
        {
            source = await _venueLayoutRepository.GetWithZonesAndSeatsAsync(
                request.SourceTemplateId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "CreateLayoutFromTemplate: failed to load source. SourceTemplateId={SourceTemplateId}",
                request.SourceTemplateId);
            throw;
        }

        if (source is null)
        {
            return Result<VenueLayoutDto>.NotFound(
                $"Source template {request.SourceTemplateId} not found");
        }

        // Defence in depth — ensure the caller owns the template AND that the
        // source actually IS a template (the domain factory rejects non-templates,
        // but failing fast here lets us return a more specific error).
        if (!source.IsTemplate)
        {
            _logger.LogWarning(
                "CreateLayoutFromTemplate: source {SourceId} is not a template",
                request.SourceTemplateId);
            return Result<VenueLayoutDto>.Failure(
                "Source layout is not a template — only templates can be applied via this endpoint",
                ErrorKind.Validation);
        }

        if (source.CreatedByUserId != request.CreatedByUserId)
        {
            _logger.LogWarning(
                "CreateLayoutFromTemplate: user {UserId} attempted to apply template " +
                "{TemplateId} owned by {OwnerId}",
                request.CreatedByUserId, request.SourceTemplateId, source.CreatedByUserId);
            return Result<VenueLayoutDto>.Forbidden(
                "You can only apply templates you own.");
        }

        // Validate event ownership — mirror of CreateLayoutFromPreset's check at the
        // event side, since attaching a layout to an event requires organizer rights.
        var @event = await _eventRepository.GetByIdAsync(
            request.EventId, trackChanges: false, cancellationToken);
        if (@event is null)
        {
            _logger.LogWarning(
                "CreateLayoutFromTemplate: target event {EventId} not found",
                request.EventId);
            return Result<VenueLayoutDto>.NotFound($"Event {request.EventId} not found");
        }

        if (@event.OrganizerId != request.CreatedByUserId)
        {
            _logger.LogWarning(
                "CreateLayoutFromTemplate: user {UserId} attempted to attach to event " +
                "{EventId} owned by {OwnerId}",
                request.CreatedByUserId, request.EventId, @event.OrganizerId);
            return Result<VenueLayoutDto>.Forbidden(
                "You can only attach a layout to events you created.");
        }

        // Default name: source.Name. Backend trims/validates; controller may pass null.
        var layoutName = string.IsNullOrWhiteSpace(request.LayoutName)
            ? source.Name
            : request.LayoutName;

        // Domain factory does the structural clone with seat-flag fidelity; the
        // domain enforces "source must be a template" + name length + owner rules.
        var cloneResult = VenueLayout.CloneFromTemplate(
            source, request.EventId, layoutName, request.CreatedByUserId);
        if (cloneResult.IsFailure)
        {
            _logger.LogInformation(
                "CreateLayoutFromTemplate: clone factory rejected — error={Error}",
                cloneResult.Error);
            return Result<VenueLayoutDto>.Failure(cloneResult.Error, cloneResult.ErrorKind);
        }

        var clone = cloneResult.Value;

        try
        {
            await _venueLayoutRepository.AddAsync(clone, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "CreateLayoutFromTemplate: persistence failed. SourceTemplateId={SourceTemplateId}, NewLayoutId={NewLayoutId}",
                request.SourceTemplateId, clone.Id);
            throw;
        }

        _logger.LogInformation(
            "CreateLayoutFromTemplate DONE: SourceTemplateId={SourceTemplateId}, NewLayoutId={NewLayoutId}, " +
            "EventId={EventId}, ZoneCount={ZoneCount}, TableCount={TableCount}, Capacity={Capacity}",
            request.SourceTemplateId, clone.Id, request.EventId,
            clone.Zones.Count, clone.Tables.Count, clone.TotalCapacity);

        try
        {
            _metrics.LayoutCreated(clone.LayoutType, fromPreset: false);
        }
        catch (Exception ex)
        {
            // Metric outage cannot fail a successful clone — log + swallow.
            _logger.LogWarning(ex,
                "CreateLayoutFromTemplate: metric emission failed (commit already succeeded). NewLayoutId={NewLayoutId}",
                clone.Id);
        }

        return Result<VenueLayoutDto>.Success(VenueLayoutDtoMapper.Map(clone));
    }
}
