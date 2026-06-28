using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Services;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.SaveLayoutAsTemplate;

/// <summary>
/// Slice 8 S8.9b handler for <see cref="SaveLayoutAsTemplateCommand"/>.
/// Flow:
/// <list type="number">
///   <item>Validate input (non-empty template name + non-empty owner ID).</item>
///   <item>Authorize the caller for the source layout via
///         <see cref="ILayoutAuthorizationService"/> (same check used by
///         every layout-mutation command).</item>
///   <item>Load the source aggregate with full zone/table/seat structure.</item>
///   <item>Call <see cref="VenueLayout.CloneAsTemplate"/> — domain owns
///         the clone invariants (template markers, fresh IDs, seat flag fidelity).</item>
///   <item>Persist via <see cref="IVenueLayoutRepository.AddAsync"/> +
///         <see cref="IUnitOfWork.CommitAsync"/>.</item>
///   <item>Emit <c>layout.created (from_preset=false)</c> for dashboard parity
///         with the preset-creation flow.</item>
/// </list>
/// </summary>
public class SaveLayoutAsTemplateCommandHandler
    : ICommandHandler<SaveLayoutAsTemplateCommand, VenueLayoutDto>
{
    private readonly ILayoutAuthorizationService _authorizationService;
    private readonly IVenueLayoutRepository _venueLayoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILayoutMetrics _metrics;
    private readonly ILogger<SaveLayoutAsTemplateCommandHandler> _logger;

    public SaveLayoutAsTemplateCommandHandler(
        ILayoutAuthorizationService authorizationService,
        IVenueLayoutRepository venueLayoutRepository,
        IUnitOfWork unitOfWork,
        ILayoutMetrics metrics,
        ILogger<SaveLayoutAsTemplateCommandHandler> logger)
    {
        _authorizationService = authorizationService;
        _venueLayoutRepository = venueLayoutRepository;
        _unitOfWork = unitOfWork;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<Result<VenueLayoutDto>> Handle(
        SaveLayoutAsTemplateCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "SaveLayoutAsTemplate START: SourceLayoutId={SourceLayoutId}, NewOwnerUserId={NewOwnerUserId}, TemplateName={TemplateName}",
            request.SourceLayoutId, request.NewOwnerUserId, request.TemplateName);

        if (request.NewOwnerUserId == Guid.Empty)
            return Result<VenueLayoutDto>.Failure("New owner user ID is required");

        if (string.IsNullOrWhiteSpace(request.TemplateName))
            return Result<VenueLayoutDto>.Failure("Template name is required");

        // Reuse the standard layout authorization gate — caller must already be
        // allowed to mutate the source layout (creator-for-templates,
        // organizer-for-event-attached). Architect-flagged for S8.9b: anyone
        // with view-only access can be added later if/when that role exists.
        var authResult = await _authorizationService.AuthorizeAsync(request.SourceLayoutId, cancellationToken);
        if (authResult.IsFailure)
        {
            _logger.LogWarning(
                "SaveLayoutAsTemplate: authorization rejected for SourceLayoutId={SourceLayoutId} by user {UserId}",
                request.SourceLayoutId, request.NewOwnerUserId);
            return Result<VenueLayoutDto>.Failure(authResult.Error, authResult.ErrorKind);
        }

        VenueLayout? source;
        try
        {
            source = await _venueLayoutRepository.GetWithZonesAndSeatsAsync(
                request.SourceLayoutId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SaveLayoutAsTemplate: failed to load source aggregate. SourceLayoutId={SourceLayoutId}",
                request.SourceLayoutId);
            throw;
        }

        if (source is null)
        {
            return Result<VenueLayoutDto>.NotFound(
                $"Source layout {request.SourceLayoutId} not found");
        }

        // Domain factory does the structural clone; domain enforces the
        // template invariants + seat fidelity.
        var cloneResult = VenueLayout.CloneAsTemplate(
            source, request.TemplateName, request.NewOwnerUserId);
        if (cloneResult.IsFailure)
        {
            _logger.LogInformation(
                "SaveLayoutAsTemplate: clone factory rejected — error={Error}",
                cloneResult.Error);
            return Result<VenueLayoutDto>.Failure(cloneResult.Error, cloneResult.ErrorKind);
        }

        var clone = cloneResult.Value;

        try
        {
            await _venueLayoutRepository.AddAsync(clone, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SaveLayoutAsTemplate: persistence failed. SourceLayoutId={SourceLayoutId}, NewLayoutId={NewLayoutId}",
                request.SourceLayoutId, clone.Id);
            throw;
        }

        _logger.LogInformation(
            "SaveLayoutAsTemplate DONE: SourceLayoutId={SourceLayoutId}, NewLayoutId={NewLayoutId}, " +
            "ZoneCount={ZoneCount}, TableCount={TableCount}, DecorationCount={DecorationCount}, TotalCapacity={Capacity}",
            request.SourceLayoutId, clone.Id,
            clone.Zones.Count, clone.Tables.Count, clone.Decorations.Count, clone.TotalCapacity);

        try
        {
            _metrics.LayoutCreated(clone.LayoutType, fromPreset: false);
        }
        catch (Exception ex)
        {
            // Metric emission must never fail a successful clone — log + swallow.
            _logger.LogWarning(ex,
                "SaveLayoutAsTemplate: metric emission failed (commit already succeeded). NewLayoutId={NewLayoutId}",
                clone.Id);
        }

        return Result<VenueLayoutDto>.Success(VenueLayoutDtoMapper.Map(clone));
    }
}
