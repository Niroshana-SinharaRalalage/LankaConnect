using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Communications.Domain;
namespace LankaConnect.Modules.Communications.Application.WhatsApp.Commands.UpdateWhatsAppPreferences;

/// <summary>
/// Phase 7A.2: Command to update WhatsApp notification preferences and cultural settings.
/// </summary>
public record UpdateWhatsAppPreferencesCommand(
    Guid UserId,
    bool EventRegistration,
    bool EventReminder,
    bool EventCancellation,
    bool EventUpdate,
    bool SignupCommitment,
    bool Refund,
    bool Newsletter,
    bool NewEvent,
    bool Payment,
    string? PreferredLanguage,
    TimeOnly? QuietHoursStart,
    TimeOnly? QuietHoursEnd,
    bool RespectCulturalTiming) : ICommand;

/// <summary>
/// Phase 7A.2: Handler for UpdateWhatsAppPreferencesCommand.
/// Gets preferences, calls UpdateNotificationPreferences and UpdateCulturalPreferences, and persists.
/// </summary>
public class UpdateWhatsAppPreferencesCommandHandler : IRequestHandler<UpdateWhatsAppPreferencesCommand, Result>
{
    private readonly IUserWhatsAppPreferencesRepository _preferencesRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateWhatsAppPreferencesCommandHandler> _logger;

    public UpdateWhatsAppPreferencesCommandHandler(
        IUserWhatsAppPreferencesRepository preferencesRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateWhatsAppPreferencesCommandHandler> logger)
    {
        _preferencesRepository = preferencesRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(
        UpdateWhatsAppPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateWhatsAppPreferences"))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateWhatsAppPreferences START: UserId={UserId}",
                request.UserId);

            try
            {
                var preferences = await _preferencesRepository.GetByUserIdAsync(request.UserId, cancellationToken);

                if (preferences is null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateWhatsAppPreferences FAILED: No WhatsApp preferences found - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("WhatsApp preferences not found. Please enable WhatsApp first.");
                }

                // Update notification type preferences
                var notificationResult = preferences.UpdateNotificationPreferences(
                    notifyRegistration: request.EventRegistration,
                    notifyReminder: request.EventReminder,
                    notifyCancellation: request.EventCancellation,
                    notifyUpdate: request.EventUpdate,
                    notifySignup: request.SignupCommitment,
                    notifyRefund: request.Refund,
                    notifyNewsletter: request.Newsletter,
                    notifyNewEvent: request.NewEvent,
                    notifyPayment: request.Payment);

                if (notificationResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateWhatsAppPreferences FAILED: Notification preferences update failed - UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.UserId, notificationResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result.Failure(notificationResult.Error);
                }

                // Update cultural preferences
                var culturalResult = preferences.UpdateCulturalPreferences(
                    preferredLanguage: request.PreferredLanguage ?? "en",
                    quietHoursStart: request.QuietHoursStart,
                    quietHoursEnd: request.QuietHoursEnd,
                    respectCulturalTiming: request.RespectCulturalTiming);

                if (culturalResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateWhatsAppPreferences FAILED: Cultural preferences update failed - UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.UserId, culturalResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result.Failure(culturalResult.Error);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "UpdateWhatsAppPreferences COMPLETE: UserId={UserId}, Language={Language}, QuietHours={QuietStart}-{QuietEnd}, Duration={ElapsedMs}ms",
                    request.UserId,
                    request.PreferredLanguage ?? "en",
                    request.QuietHoursStart?.ToString() ?? "none",
                    request.QuietHoursEnd?.ToString() ?? "none",
                    stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "UpdateWhatsAppPreferences FAILED: Unexpected error - UserId={UserId}, Duration={ElapsedMs}ms",
                    request.UserId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
