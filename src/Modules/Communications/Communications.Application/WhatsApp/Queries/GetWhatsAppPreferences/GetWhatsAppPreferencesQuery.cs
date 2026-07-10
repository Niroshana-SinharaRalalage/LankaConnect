using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Communications.Domain;
namespace LankaConnect.Modules.Communications.Application.WhatsApp.Queries.GetWhatsAppPreferences;

/// <summary>
/// Phase 7A.2: Query to get WhatsApp preferences for a user.
/// Returns null if the user has no WhatsApp preferences configured.
/// </summary>
public record GetWhatsAppPreferencesQuery(Guid UserId) : IQuery<WhatsAppPreferencesDto?>;

/// <summary>
/// DTO representing WhatsApp notification preferences for a user.
/// </summary>
public class WhatsAppPreferencesDto
{
    public Guid UserId { get; init; }
    public bool WhatsAppEnabled { get; init; }
    public string? WhatsAppPhoneNumber { get; init; }
    public bool PhoneVerified { get; init; }
    public DateTime? PhoneVerifiedAt { get; init; }
    public bool IsFullyVerified { get; init; }
    public bool IsLocked { get; init; }
    public DateTime? VerificationLockedUntil { get; init; }

    // Per-notification-type preferences
    public bool NotifyEventRegistration { get; init; }
    public bool NotifyEventReminder { get; init; }
    public bool NotifyEventCancellation { get; init; }
    public bool NotifyEventUpdate { get; init; }
    public bool NotifySignupCommitment { get; init; }
    public bool NotifyRefund { get; init; }
    public bool NotifyNewsletter { get; init; }
    public bool NotifyNewEvent { get; init; }
    public bool NotifyPayment { get; init; }

    // Cultural preferences
    public string PreferredLanguage { get; init; } = "en";
    public TimeOnly? QuietHoursStart { get; init; }
    public TimeOnly? QuietHoursEnd { get; init; }
    public bool RespectCulturalTiming { get; init; }
}

/// <summary>
/// Phase 7A.2: Handler for GetWhatsAppPreferencesQuery.
/// Maps UserWhatsAppPreferences entity to WhatsAppPreferencesDto.
/// </summary>
public class GetWhatsAppPreferencesQueryHandler : IRequestHandler<GetWhatsAppPreferencesQuery, Result<WhatsAppPreferencesDto?>>
{
    private readonly IUserWhatsAppPreferencesRepository _preferencesRepository;
    private readonly ILogger<GetWhatsAppPreferencesQueryHandler> _logger;

    public GetWhatsAppPreferencesQueryHandler(
        IUserWhatsAppPreferencesRepository preferencesRepository,
        ILogger<GetWhatsAppPreferencesQueryHandler> logger)
    {
        _preferencesRepository = preferencesRepository;
        _logger = logger;
    }

    public async Task<Result<WhatsAppPreferencesDto?>> Handle(
        GetWhatsAppPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetWhatsAppPreferences"))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetWhatsAppPreferences START: UserId={UserId}",
                request.UserId);

            try
            {
                var preferences = await _preferencesRepository.GetByUserIdAsync(request.UserId, cancellationToken);

                if (preferences is null)
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "GetWhatsAppPreferences COMPLETE: No preferences found - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);
                    return Result<WhatsAppPreferencesDto?>.Success(null);
                }

                var dto = new WhatsAppPreferencesDto
                {
                    UserId = preferences.UserId,
                    WhatsAppEnabled = preferences.WhatsAppEnabled,
                    WhatsAppPhoneNumber = preferences.WhatsAppPhoneNumber,
                    PhoneVerified = preferences.PhoneVerified,
                    PhoneVerifiedAt = preferences.PhoneVerifiedAt,
                    IsFullyVerified = preferences.IsFullyVerified,
                    IsLocked = preferences.IsLocked,
                    VerificationLockedUntil = preferences.VerificationLockedUntil,
                    NotifyEventRegistration = preferences.NotifyEventRegistration,
                    NotifyEventReminder = preferences.NotifyEventReminder,
                    NotifyEventCancellation = preferences.NotifyEventCancellation,
                    NotifyEventUpdate = preferences.NotifyEventUpdate,
                    NotifySignupCommitment = preferences.NotifySignupCommitment,
                    NotifyRefund = preferences.NotifyRefund,
                    NotifyNewsletter = preferences.NotifyNewsletter,
                    NotifyNewEvent = preferences.NotifyNewEvent,
                    NotifyPayment = preferences.NotifyPayment,
                    PreferredLanguage = preferences.PreferredLanguage,
                    QuietHoursStart = preferences.QuietHoursStart,
                    QuietHoursEnd = preferences.QuietHoursEnd,
                    RespectCulturalTiming = preferences.RespectCulturalTiming
                };

                stopwatch.Stop();
                _logger.LogInformation(
                    "GetWhatsAppPreferences COMPLETE: UserId={UserId}, Enabled={Enabled}, Verified={Verified}, Duration={ElapsedMs}ms",
                    request.UserId, preferences.WhatsAppEnabled, preferences.PhoneVerified, stopwatch.ElapsedMilliseconds);

                return Result<WhatsAppPreferencesDto?>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "GetWhatsAppPreferences FAILED: Unexpected error - UserId={UserId}, Duration={ElapsedMs}ms",
                    request.UserId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
