using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Wave 8.5.g
using Microsoft.EntityFrameworkCore; // Wave 8.5.g
using Microsoft.Extensions.Logging;
namespace LankaConnect.Products.LankaEvents.Application.Commands.ReleaseSeats;

public class ReleaseSeatsCommandHandler : ICommandHandler<ReleaseSeatsCommand>
{
    private readonly ISeatHoldRepository _seatHoldRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LankaEventsDbContext _dbContext; // Wave 8.5.g direct-SaveChanges
    private readonly ILogger<ReleaseSeatsCommandHandler> _logger;

    public ReleaseSeatsCommandHandler(
        ISeatHoldRepository seatHoldRepository,
        IUnitOfWork unitOfWork,
        LankaEventsDbContext dbContext,
        ILogger<ReleaseSeatsCommandHandler> logger)
    {
        _seatHoldRepository = seatHoldRepository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> Handle(ReleaseSeatsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Releasing seats: SessionId={SessionId}, UserId={UserId}",
            request.SessionId, request.UserId);

        var holds = await _seatHoldRepository.GetActiveHoldsBySessionAsync(request.SessionId, cancellationToken);

        if (holds.Count == 0)
            return Result.Success(); // Nothing to release — idempotent

        // Verify ownership
        var unauthorized = holds.Where(h => h.UserId != request.UserId).ToList();
        if (unauthorized.Count > 0)
            return Result.Failure("Cannot release holds belonging to another user");

        foreach (var hold in holds)
        {
            hold.Release();
        }

        await _dbContext.SaveChangesAsync(cancellationToken); // Wave 8.5.g direct-SaveChanges

        _logger.LogInformation(
            "Seats released: SessionId={SessionId}, Count={Count}",
            request.SessionId, holds.Count);

        return Result.Success();
    }
}
