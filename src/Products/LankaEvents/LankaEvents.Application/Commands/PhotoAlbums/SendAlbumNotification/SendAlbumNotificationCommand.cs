using LankaConnect.Modules.Identity.Contracts; // W4.7.d.3
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Domain.Enums;
using LankaConnect.Modules.Media.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using LankaConnect.Shared.WhatsApp.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Products.LankaEvents.Application.Commands.PhotoAlbums.SendAlbumNotification;

/// <summary>
/// Command to send email notification to registered attendees and sign-up list participants
/// about a published album.
/// Decoupled from Publish: organizer explicitly triggers notification.
/// Album must be Published before notification can be sent.
///
/// Phase 7B fixes:
/// 1. Use EmailTemplateNames.PhotoAlbumPublished constant (not a magic string).
/// 2. Include sign-up list committed users in addition to confirmed/attended registrations.
///    Pattern mirrors EventCancellationEmailJob (Phase 6A.75).
/// </summary>
public record SendAlbumNotificationCommand(
    Guid EventId,
    Guid AlbumId,
    Guid UserId
) : ICommand;

public class SendAlbumNotificationCommandHandler : ICommandHandler<SendAlbumNotificationCommand>
{
    private readonly IPhotoAlbumRepository _photoAlbumRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEventRepository _eventRepository;        // Phase 7B: sign-up list participants
    private readonly IIdentityQueries _identityQueries;          // Phase 7B: bulk email + name lookup
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SendAlbumNotificationCommandHandler> _logger;

    public SendAlbumNotificationCommandHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        IRegistrationRepository registrationRepository,
        IEventRepository eventRepository,
        IIdentityQueries identityQueries,
        IEmailUrlHelper emailUrlHelper,
        IServiceScopeFactory scopeFactory,
        ILogger<SendAlbumNotificationCommandHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _registrationRepository = registrationRepository;
        _eventRepository = eventRepository;
        _identityQueries = identityQueries;
        _emailUrlHelper = emailUrlHelper;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<Result> Handle(SendAlbumNotificationCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SendAlbumNotification"))
        using (LogContext.PushProperty("AlbumId", request.AlbumId))
        {
            _logger.LogInformation(
                "Sending album notification for AlbumId={AlbumId}, EventId={EventId}, UserId={UserId}",
                request.AlbumId, request.EventId, request.UserId);

            var album = await _photoAlbumRepository.GetByIdAsync(request.AlbumId, trackChanges: false, cancellationToken);
            if (album == null)
                return Result.Failure($"Album with ID {request.AlbumId} not found");

            // Only the event organizer can send notifications
            if (album.OrganizerId != request.UserId)
                return Result.Failure("Only the event organizer can send album notifications");

            // Album must be Published
            if (album.Status != AlbumStatus.Published)
                return Result.Failure("Album must be published before sending notifications");

            var albumUrl = $"{_emailUrlHelper.BuildEventDetailsUrl(request.EventId)}/photos?album={request.AlbumId}";

            // ─── Recipient set (case-insensitive deduplication) ─────────────────
            var seenEmails     = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var emailParamsList = new List<(string Email, string Name)>();

            // ─── 1. Confirmed / Attended registrations ───────────────────────────
            var registrations = await _registrationRepository.GetByEventAsync(
                request.EventId, cancellationToken, trackChanges: false);

            var eligibleRegistrations = registrations
                .Where(r => r.Status == RegistrationStatus.Confirmed
                         || r.Status == RegistrationStatus.Attended)
                .ToList();

            _logger.LogInformation(
                "Found {Count} eligible registrations for AlbumId={AlbumId}, EventId={EventId}",
                eligibleRegistrations.Count, request.AlbumId, request.EventId);

            foreach (var registration in eligibleRegistrations)
            {
                var email = GetRecipientEmail(registration);
                var name  = GetRecipientName(registration);

                if (!string.IsNullOrWhiteSpace(email) && seenEmails.Add(email))
                    emailParamsList.Add((email, name));
            }

            // ─── 2. Sign-up list committed users (Phase 7B) ──────────────────────
            // Mirrors the pattern from EventCancellationEmailJob (Phase 6A.75).
            // Events that use Signup Lists instead of (or in addition to) ticketed
            // registrations must also receive photo album notifications.
            try
            {
                var @event = await _eventRepository.GetByIdAsync(
                    request.EventId, trackChanges: false, cancellationToken);

                if (@event?.SignUpLists != null && @event.SignUpLists.Any())
                {
                    var signUpUserIds = @event.SignUpLists
                        .SelectMany(sl => sl.Items ?? Enumerable.Empty<SignUpItem>())
                        .SelectMany(item => item.Commitments ?? Enumerable.Empty<SignUpCommitment>())
                        .Select(c => c.UserId)
                        .Distinct()
                        .ToList();

                    _logger.LogInformation(
                        "[Phase 7B] Found {Count} unique sign-up committed users for AlbumId={AlbumId}",
                        signUpUserIds.Count, request.AlbumId);

                    if (signUpUserIds.Any())
                    {
                        // Bulk-fetch emails and names in parallel to avoid N+1
                        var emailsTask = _identityQueries.GetEmailsByUserIdsAsync(signUpUserIds, cancellationToken);
                        var namesTask  = _identityQueries.GetUserNamesAsync(signUpUserIds, cancellationToken);
                        await Task.WhenAll(emailsTask, namesTask);

                        var signUpEmails = emailsTask.Result;
                        var signUpNames  = namesTask.Result;

                        foreach (var (userId, email) in signUpEmails)
                        {
                            if (!string.IsNullOrWhiteSpace(email) && seenEmails.Add(email))
                            {
                                var name = signUpNames.TryGetValue(userId, out var n) ? n : "Attendee";
                                emailParamsList.Add((email, name));
                            }
                        }

                        _logger.LogInformation(
                            "[Phase 7B] Added {Count} sign-up list emails to album notification batch. AlbumId={AlbumId}",
                            signUpEmails.Count, request.AlbumId);
                    }
                }
                else
                {
                    _logger.LogInformation(
                        "[Phase 7B] No sign-up lists found for EventId={EventId}; skipping sign-up participant lookup",
                        request.EventId);
                }
            }
            catch (Exception ex)
            {
                // Sign-up list lookup is best-effort — log and continue with registrations only.
                // This ensures registration recipients are never blocked by a sign-up lookup failure.
                _logger.LogError(ex,
                    "[Phase 7B] Failed to load sign-up list participants for EventId={EventId}, AlbumId={AlbumId}. " +
                    "Continuing with {RegistrationCount} registration recipients only.",
                    request.EventId, request.AlbumId, emailParamsList.Count);
            }

            // ─── Final recipient check ───────────────────────────────────────────
            if (emailParamsList.Count == 0)
            {
                _logger.LogInformation(
                    "No eligible recipients found for album notification. " +
                    "EventId={EventId}, AlbumId={AlbumId}",
                    request.EventId, request.AlbumId);
                return Result.Success(); // Not an error — no recipients is a valid state
            }

            _logger.LogInformation(
                "Dispatching {EmailCount} album notification emails for AlbumId={AlbumId} " +
                "(Registrations + Sign-up combined, deduplicated)",
                emailParamsList.Count, request.AlbumId);

            // ─── Capture for closure (avoid capturing 'this' or disposed objects) ─
            var capturedScopeFactory = _scopeFactory;
            var capturedAlbumId      = request.AlbumId;
            var capturedEventId      = request.EventId;
            var capturedEventTitle   = album.EventTitle;
            var capturedAlbumName    = album.Name;
            var capturedAlbumUrl     = albumUrl;
            var capturedLogger       = _logger;

            // ─── Fire-and-forget: send emails in background ──────────────────────
            _ = Task.Run(async () =>
            {
                var sentCount   = 0;
                var failedCount = 0;

                try
                {
                    using var scope        = capturedScopeFactory.CreateScope();
                    var emailService       = scope.ServiceProvider.GetRequiredService<ITypedEmailService>();

                    foreach (var (email, name) in emailParamsList)
                    {
                        try
                        {
                            var emailParams = new AlbumNotificationEmailParams
                            {
                                RecipientEmail = email,
                                RecipientName  = name,
                                EventId        = capturedEventId,
                                EventTitle     = capturedEventTitle,
                                AlbumName      = capturedAlbumName,
                                AlbumUrl       = capturedAlbumUrl,
                            };

                            var emailResult = await emailService.SendEmailAsync(emailParams, CancellationToken.None);
                            if (emailResult.Success)
                                sentCount++;
                            else
                            {
                                failedCount++;
                                capturedLogger.LogError(
                                    "Album notification email failed: Email={Email}, AlbumId={AlbumId}, Errors={Errors}",
                                    email, capturedAlbumId, string.Join(", ", emailResult.Errors));
                            }
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            capturedLogger.LogError(ex,
                                "Album notification email exception: Email={Email}, AlbumId={AlbumId}",
                                email, capturedAlbumId);
                        }
                    }

                    capturedLogger.LogInformation(
                        "Album notification emails complete: Sent={SentCount}, Failed={FailedCount}, AlbumId={AlbumId}",
                        sentCount, failedCount, capturedAlbumId);
                }
                catch (Exception ex)
                {
                    capturedLogger.LogError(ex,
                        "Album notification email batch exception: AlbumId={AlbumId}, EventId={EventId}",
                        capturedAlbumId, capturedEventId);
                }
            }, CancellationToken.None);

            // Phase 7B.3: Send WhatsApp broadcast to all opted-in attendees (best-effort)
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = capturedScopeFactory.CreateScope();
                    var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

                    var whatsAppParams = new Dictionary<string, string>
                    {
                        { WhatsAppTemplateContract.Common.EventTitle, capturedEventTitle },
                        { WhatsAppTemplateContract.Album.AlbumName, capturedAlbumName },
                        { WhatsAppTemplateContract.Album.AlbumUrl, capturedAlbumUrl }
                    };

                    var result = await whatsAppService.BroadcastToEventAttendeesAsync(
                        capturedEventId,
                        WhatsAppTemplateContract.TemplateNames.PhotoAlbumPublished,
                        whatsAppParams,
                        WhatsAppNotificationType.PhotoAlbum,
                        CancellationToken.None);

                    if (result.IsSuccess)
                    {
                        capturedLogger.LogInformation(
                            "[Phase 7B.3] WhatsApp PhotoAlbum BROADCAST: AlbumId={AlbumId}, EventId={EventId}, SentCount={SentCount}",
                            capturedAlbumId, capturedEventId, result.Value);
                    }
                    else
                    {
                        capturedLogger.LogWarning(
                            "[Phase 7B.3] WhatsApp PhotoAlbum FAILED: AlbumId={AlbumId}, {Errors}",
                            capturedAlbumId, string.Join(", ", result.Errors));
                    }
                }
                catch (Exception ex)
                {
                    capturedLogger.LogError(ex,
                        "[Phase 7B.3] WhatsApp PhotoAlbum EXCEPTION: AlbumId={AlbumId}, EventId={EventId}",
                        capturedAlbumId, capturedEventId);
                }
            }, CancellationToken.None);

            return Result.Success();
        }
    }

    private static string GetRecipientEmail(Registration registration)
    {
        if (registration.Contact != null)
            return registration.Contact.Email;
        if (registration.AttendeeInfo != null)
            return registration.AttendeeInfo.Email.Value;
        return string.Empty;
    }

    private static string GetRecipientName(Registration registration)
    {
        if (registration.HasDetailedAttendees() && registration.Attendees.Count > 0)
            return registration.Attendees[0].Name ?? "Attendee";
        if (registration.AttendeeInfo != null)
            return registration.AttendeeInfo.Name;
        return "Attendee";
    }
}

/// <summary>
/// Email parameters for photo album published notification.
/// Phase 7B: Uses EmailTemplateNames.PhotoAlbumPublished constant (eliminates magic string).
/// </summary>
internal class AlbumNotificationEmailParams : IEmailParameters
{
    // Phase 7B: was "template-photo-album-published" (magic string) — now uses the constant
    public string TemplateName => EmailTemplateNames.PhotoAlbumPublished;

    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientName  { get; set; } = "Attendee";
    public Guid   EventId        { get; set; }
    public string EventTitle     { get; set; } = string.Empty;
    public string AlbumName      { get; set; } = string.Empty;
    public string AlbumUrl       { get; set; } = string.Empty;

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { EmailTemplateContract.Common.UserName,    RecipientName },
            { EmailTemplateContract.Event.EventTitle,   EventTitle },
            { "AlbumName",                              AlbumName },
            { "AlbumUrl",                               AlbumUrl },
            { "PhotoExpiryDays",                        7 },
            { EmailTemplateContract.Common.Year,        DateTime.UtcNow.Year },
        };
    }

    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();
        if (string.IsNullOrWhiteSpace(RecipientEmail))
            errors.Add("RecipientEmail is required");
        if (EventId == Guid.Empty)
            errors.Add("EventId is required");
        if (string.IsNullOrWhiteSpace(EventTitle))
            errors.Add("EventTitle is required");
        if (string.IsNullOrWhiteSpace(AlbumUrl))
            errors.Add("AlbumUrl is required");
        return errors.Count == 0;
    }
}
