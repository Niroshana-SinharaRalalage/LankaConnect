using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Options;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;

namespace LankaConnect.Application.Communications.WhatsApp.Commands.EnableWhatsApp;

/// <summary>
/// Phase 7A.2: Command to enable WhatsApp notifications for a user with a phone number.
/// Phone number must be in E.164 format (e.g., +14155551234).
/// </summary>
public record EnableWhatsAppCommand(Guid UserId, string PhoneNumber) : ICommand<EnableWhatsAppResponse>;

/// <summary>
/// Response returned after enabling WhatsApp for a user.
/// </summary>
public class EnableWhatsAppResponse
{
    public Guid UserId { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public bool PhoneVerified { get; init; }
}

/// <summary>
/// Phase 7A.2: Handler for EnableWhatsAppCommand.
/// Finds or creates UserWhatsAppPreferences, calls EnableWhatsApp(phoneNumber), and persists.
/// </summary>
public class EnableWhatsAppCommandHandler : IRequestHandler<EnableWhatsAppCommand, Result<EnableWhatsAppResponse>>
{
    private readonly IUserWhatsAppPreferencesRepository _preferencesRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOptions<WhatsAppOptions> _settings;
    private readonly ILogger<EnableWhatsAppCommandHandler> _logger;

    public EnableWhatsAppCommandHandler(
        IUserWhatsAppPreferencesRepository preferencesRepository,
        IUnitOfWork unitOfWork,
        IOptions<WhatsAppOptions> settings,
        ILogger<EnableWhatsAppCommandHandler> logger)
    {
        _preferencesRepository = preferencesRepository;
        _unitOfWork = unitOfWork;
        _settings = settings;
        _logger = logger;
    }

    public async Task<Result<EnableWhatsAppResponse>> Handle(
        EnableWhatsAppCommand request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "EnableWhatsApp"))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "EnableWhatsApp START: UserId={UserId}, PhoneNumber={PhoneNumber}",
                request.UserId, request.PhoneNumber);

            try
            {
                if (!_settings.Value.Enabled)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "EnableWhatsApp FAILED: WhatsApp feature is globally disabled. UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);
                    return Result<EnableWhatsAppResponse>.Failure("WhatsApp messaging is currently disabled.");
                }

                var preferences = await _preferencesRepository.GetByUserIdAsync(request.UserId, cancellationToken);

                if (preferences is null)
                {
                    _logger.LogInformation(
                        "EnableWhatsApp: Creating new preferences for UserId={UserId}",
                        request.UserId);

                    preferences = UserWhatsAppPreferences.Create(request.UserId);
                    await _preferencesRepository.AddAsync(preferences, cancellationToken);
                }

                var enableResult = preferences.EnableWhatsApp(request.PhoneNumber);
                if (enableResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "EnableWhatsApp FAILED: Domain validation failed - UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.UserId, enableResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<EnableWhatsAppResponse>.Failure(enableResult.Error);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                var response = new EnableWhatsAppResponse
                {
                    UserId = preferences.UserId,
                    PhoneNumber = preferences.WhatsAppPhoneNumber!,
                    IsEnabled = preferences.WhatsAppEnabled,
                    PhoneVerified = preferences.PhoneVerified
                };

                stopwatch.Stop();
                _logger.LogInformation(
                    "EnableWhatsApp COMPLETE: UserId={UserId}, PhoneNumber={PhoneNumber}, Duration={ElapsedMs}ms",
                    request.UserId, request.PhoneNumber, stopwatch.ElapsedMilliseconds);

                return Result<EnableWhatsAppResponse>.Success(response);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "EnableWhatsApp FAILED: Unexpected error - UserId={UserId}, PhoneNumber={PhoneNumber}, Duration={ElapsedMs}ms",
                    request.UserId, request.PhoneNumber, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
