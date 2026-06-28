using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Products.LankaEvents.Application.Commands.ReleaseSeats;

public class ReleaseSeatsCommandHandler : ICommandHandler<ReleaseSeatsCommand>
{
    private readonly ISeatHoldRepository _seatHoldRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReleaseSeatsCommandHandler> _logger;

    public ReleaseSeatsCommandHandler(
        ISeatHoldRepository seatHoldRepository,
        IUnitOfWork unitOfWork,
        ILogger<ReleaseSeatsCommandHandler> logger)
    {
        _seatHoldRepository = seatHoldRepository;
        _unitOfWork = unitOfWork;
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

        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Seats released: SessionId={SessionId}, Count={Count}",
            request.SessionId, holds.Count);

        return Result.Success();
    }
}
