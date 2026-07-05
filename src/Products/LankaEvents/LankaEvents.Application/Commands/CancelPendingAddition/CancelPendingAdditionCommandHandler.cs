using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.CancelPendingAddition;

/// <summary>
/// Handler for CancelPendingAdditionCommand.
/// Cancels an existing pending RegistrationAddition.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// </summary>
public class CancelPendingAdditionCommandHandler
    : ICommandHandler<CancelPendingAdditionCommand, CancelPendingAdditionResult>
{
    private readonly IRegistrationAdditionRepository _additionRepository;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CancelPendingAdditionCommandHandler> _logger;

    public CancelPendingAdditionCommandHandler(
        IRegistrationAdditionRepository additionRepository,
        IApplicationDbContext context,
        ILogger<CancelPendingAdditionCommandHandler> logger)
    {
        _additionRepository = additionRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<Result<CancelPendingAdditionResult>> Handle(
        CancelPendingAdditionCommand request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CancelPendingAddition"))
        using (LogContext.PushProperty("EntityType", "RegistrationAddition"))
        using (LogContext.PushProperty("RegistrationId", request.RegistrationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[AddOnlyAttendees] CancelPendingAddition START - RegistrationId={RegistrationId}",
                request.RegistrationId);

            try
            {
                // Find the pending addition
                var pendingAddition = await _additionRepository.GetPendingByRegistrationIdAsync(
                    request.RegistrationId,
                    cancellationToken);

                if (pendingAddition == null)
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "[AddOnlyAttendees] No pending addition found - RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                        request.RegistrationId, stopwatch.ElapsedMilliseconds);
                    return Result<CancelPendingAdditionResult>.Success(
                        CancelPendingAdditionResult.NoPendingAddition());
                }

                var additionId = pendingAddition.Id;

                // Mark the addition as abandoned (cancelled by user)
                var cancelResult = pendingAddition.MarkAsAbandoned();
                if (cancelResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "[AddOnlyAttendees] Failed to cancel addition - RegistrationId={RegistrationId}, AdditionId={AdditionId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.RegistrationId, additionId, cancelResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<CancelPendingAdditionResult>.Success(
                        CancelPendingAdditionResult.Failed(cancelResult.Error));
                }

                // Save changes
                await _context.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "[AddOnlyAttendees] CancelPendingAddition SUCCESS - RegistrationId={RegistrationId}, AdditionId={AdditionId}, Duration={ElapsedMs}ms",
                    request.RegistrationId, additionId, stopwatch.ElapsedMilliseconds);

                return Result<CancelPendingAdditionResult>.Success(
                    CancelPendingAdditionResult.Successful(additionId));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "[AddOnlyAttendees] CancelPendingAddition FAILED - RegistrationId={RegistrationId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.RegistrationId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
