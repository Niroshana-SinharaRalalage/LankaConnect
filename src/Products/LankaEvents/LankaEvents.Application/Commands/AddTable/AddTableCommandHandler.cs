using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Services;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Wave 8.5.g direct-SaveChanges
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Products.LankaEvents.Application.Commands.AddTable;

/// <summary>
/// Slice 5 Chunk 6: POST table handler.
/// Flow: authorize → load layout (with zones + tables + seats) → generate table
/// and seats via the aggregate's <c>GenerateRoundTable</c> / <c>GenerateRectTable</c>
/// helpers → apply <c>ExpectedRowVersion</c> → commit. Adding a table is NOT
/// structural on pre-existing seats (existing seats are untouched), so the
/// <c>IStructuralEditGuard</c> does not run here.
/// </summary>
public class AddTableCommandHandler : ICommandHandler<AddTableCommand, Guid>
{
    private readonly ILayoutAuthorizationService _authorizationService;
    private readonly IVenueLayoutRepository _layoutRepository;
    private readonly LankaEventsDbContext _dbContext; // Wave 8.5.g direct-SaveChanges
    private readonly ILogger<AddTableCommandHandler> _logger;

    public AddTableCommandHandler(
        ILayoutAuthorizationService authorizationService,
        IVenueLayoutRepository layoutRepository,
        LankaEventsDbContext dbContext,
        ILogger<AddTableCommandHandler> logger)
    {
        _authorizationService = authorizationService;
        _layoutRepository = layoutRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(AddTableCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "AddTable: LayoutId={LayoutId}, Label={Label}, Shape={Shape}, Capacity={Capacity}, SortOrder={SortOrder}, ZoneId={ZoneId}, ExpectedRowVersion={RowVersion}",
            request.LayoutId, request.Label, request.Shape, request.Capacity, request.SortOrder,
            request.ZoneId, request.ExpectedRowVersion);

        var authResult = await _authorizationService.AuthorizeAsync(request.LayoutId, cancellationToken);
        if (authResult.IsFailure)
        {
            return Result<Guid>.Failure(authResult.Error, authResult.ErrorKind);
        }

        VenueLayout? layout;
        try
        {
            layout = await _layoutRepository.GetWithZonesAndSeatsAsync(request.LayoutId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AddTable: failed to load layout aggregate. LayoutId={LayoutId}",
                request.LayoutId);
            throw;
        }

        if (layout is null)
        {
            return Result<Guid>.NotFound("Venue layout not found");
        }

        Result<VenueTable> addResult = request.Shape == TableShape.Round
            ? layout.GenerateRoundTable(
                request.Label,
                request.Capacity,
                request.SortOrder,
                request.ZoneId,
                request.Geometry,
                request.StartAngleDeg)
            : layout.GenerateRectTable(
                request.Label,
                request.Shape,
                request.Capacity,
                request.SortOrder,
                request.ZoneId,
                request.Geometry);

        if (addResult.IsFailure)
        {
            return Result<Guid>.Failure(addResult.Error);
        }

        _layoutRepository.SetOriginalRowVersion(layout, request.ExpectedRowVersion);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex,
                "AddTable: concurrency conflict. LayoutId={LayoutId}, ExpectedRowVersion={RowVersion}",
                request.LayoutId, request.ExpectedRowVersion);
            return Result<Guid>.Conflict(
                "Layout was modified by someone else. Reload the layout and retry with the current version.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AddTable: persistence failed. LayoutId={LayoutId}",
                request.LayoutId);
            throw;
        }

        _logger.LogInformation(
            "AddTable: succeeded. LayoutId={LayoutId}, TableId={TableId}, Seats={SeatCount}",
            request.LayoutId, addResult.Value.Id, addResult.Value.Seats.Count);

        return Result<Guid>.Success(addResult.Value.Id);
    }
}
