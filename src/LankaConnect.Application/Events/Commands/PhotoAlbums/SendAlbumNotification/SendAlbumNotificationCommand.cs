using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.PhotoAlbums.SendAlbumNotification;

/// <summary>
/// Command to send email notification to registered attendees about a published album.
/// Decoupled from Publish: organizer explicitly triggers notification.
/// Album must be Published before notification can be sent.
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
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SendAlbumNotificationCommandHandler> _logger;

    public SendAlbumNotificationCommandHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        IRegistrationRepository registrationRepository,
        IEmailUrlHelper emailUrlHelper,
        IServiceScopeFactory scopeFactory,
        ILogger<SendAlbumNotificationCommandHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _registrationRepository = registrationRepository;
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
                "Sending album notification for album {AlbumId}, event {EventId} by user {UserId}",
                request.AlbumId, request.EventId, request.UserId);

            var album = await _photoAlbumRepository.GetByIdAsync(request.AlbumId, trackChanges: false, cancellationToken);
            if (album == null)
                return Result.Failure($"Album with ID {request.AlbumId} not found");

            // Only organizer can send notifications
            if (album.OrganizerId != request.UserId)
                return Result.Failure("Only the event organizer can send album notifications");

            // Album must be published
            if (album.Status != AlbumStatus.Published)
                return Result.Failure("Album must be published before sending notifications");

            // Get eligible registrations
            var registrations = await _registrationRepository.GetByEventAsync(
                request.EventId, cancellationToken, trackChanges: false);

            var eligibleRegistrations = registrations
                .Where(r => r.Status == RegistrationStatus.Confirmed
                         || r.Status == RegistrationStatus.Attended)
                .ToList();

            if (eligibleRegistrations.Count == 0)
            {
                _logger.LogInformation(
                    "No eligible registrations for album notification. EventId={EventId}",
                    request.EventId);
                return Result.Success(); // Not an error, just no recipients
            }

            var albumUrl = $"{_emailUrlHelper.BuildEventDetailsUrl(request.EventId)}/photos?album={request.AlbumId}";

            // Build email params
            var emailParamsList = new List<(string Email, string Name)>();

            foreach (var registration in eligibleRegistrations)
            {
                var email = GetRecipientEmail(registration);
                var name = GetRecipientName(registration);

                if (!string.IsNullOrWhiteSpace(email))
                    emailParamsList.Add((email, name));
            }

            if (emailParamsList.Count == 0)
            {
                _logger.LogWarning(
                    "No valid email addresses found for album notification. EventId={EventId}",
                    request.EventId);
                return Result.Success();
            }

            _logger.LogInformation(
                "Dispatching {EmailCount} album notification emails for AlbumId={AlbumId}",
                emailParamsList.Count, request.AlbumId);

            // Capture for closure
            var capturedScopeFactory = _scopeFactory;
            var capturedAlbumId = request.AlbumId;
            var capturedEventId = request.EventId;
            var capturedEventTitle = album.EventTitle;
            var capturedAlbumName = album.Name;
            var capturedAlbumUrl = albumUrl;

            // Fire-and-forget: send emails in background
            _ = Task.Run(async () =>
            {
                var sentCount = 0;
                var failedCount = 0;

                try
                {
                    using var scope = capturedScopeFactory.CreateScope();
                    var emailService = scope.ServiceProvider.GetRequiredService<ITypedEmailService>();

                    foreach (var (email, name) in emailParamsList)
                    {
                        try
                        {
                            var emailParams = new AlbumNotificationEmailParams
                            {
                                RecipientEmail = email,
                                RecipientName = name,
                                EventId = capturedEventId,
                                EventTitle = capturedEventTitle,
                                AlbumName = capturedAlbumName,
                                AlbumUrl = capturedAlbumUrl,
                            };

                            var emailResult = await emailService.SendEmailAsync(emailParams, CancellationToken.None);
                            if (emailResult.Success)
                                sentCount++;
                            else
                            {
                                failedCount++;
                                _logger.LogError(
                                    "Album notification email failed: Email={Email}, AlbumId={AlbumId}, Errors={Errors}",
                                    email, capturedAlbumId, string.Join(", ", emailResult.Errors));
                            }
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            _logger.LogError(ex,
                                "Album notification email exception: Email={Email}, AlbumId={AlbumId}",
                                email, capturedAlbumId);
                        }
                    }

                    _logger.LogInformation(
                        "Album notification emails complete: Sent={SentCount}, Failed={FailedCount}, AlbumId={AlbumId}",
                        sentCount, failedCount, capturedAlbumId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Album notification email batch exception: AlbumId={AlbumId}, EventId={EventId}",
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
/// Email parameters for album notification (reuses the existing template).
/// </summary>
internal class AlbumNotificationEmailParams : IEmailParameters
{
    public string TemplateName => "template-photo-album-published";

    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = "Attendee";
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string AlbumName { get; set; } = string.Empty;
    public string AlbumUrl { get; set; } = string.Empty;

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { EmailTemplateContract.Common.UserName, RecipientName },
            { EmailTemplateContract.Event.EventTitle, EventTitle },
            { "AlbumName", AlbumName },
            { "AlbumUrl", AlbumUrl },
            { "PhotoExpiryDays", 7 },
            { EmailTemplateContract.Common.Year, DateTime.UtcNow.Year },
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
