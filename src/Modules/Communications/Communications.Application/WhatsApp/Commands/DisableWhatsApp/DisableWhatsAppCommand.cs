using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
namespace LankaConnect.Modules.Communications.Application.WhatsApp.Commands.DisableWhatsApp;

/// <summary>
/// Phase 7A.2: Command to disable WhatsApp notifications for a user.
/// </summary>
public record DisableWhatsAppCommand(Guid UserId) : ICommand;

/// <summary>
/// Phase 7A.2: Handler for DisableWhatsAppCommand.
/// Gets preferences by userId, calls DisableWhatsApp(), and persists.
/// </summary>
public class DisableWhatsAppCommandHandler : IRequestHandler<DisableWhatsAppCommand, Result>
{
    private readonly IUserWhatsAppPreferencesRepository _preferencesRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DisableWhatsAppCommandHandler> _logger;

    public DisableWhatsAppCommandHandler(
        IUserWhatsAppPreferencesRepository preferencesRepository,
        IUnitOfWork unitOfWork,
        ILogger<DisableWhatsAppCommandHandler> logger)
    {
        _preferencesRepository = preferencesRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DisableWhatsAppCommand request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "DisableWhatsApp"))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "DisableWhatsApp START: UserId={UserId}",
                request.UserId);

            try
            {
                var preferences = await _preferencesRepository.GetByUserIdAsync(request.UserId, cancellationToken);

                if (preferences is null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DisableWhatsApp FAILED: No WhatsApp preferences found - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("WhatsApp preferences not found for this user.");
                }

                var disableResult = preferences.DisableWhatsApp();
                if (disableResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "DisableWhatsApp FAILED: Domain validation failed - UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.UserId, disableResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result.Failure(disableResult.Error);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "DisableWhatsApp COMPLETE: UserId={UserId}, Duration={ElapsedMs}ms",
                    request.UserId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "DisableWhatsApp FAILED: Unexpected error - UserId={UserId}, Duration={ElapsedMs}ms",
                    request.UserId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
