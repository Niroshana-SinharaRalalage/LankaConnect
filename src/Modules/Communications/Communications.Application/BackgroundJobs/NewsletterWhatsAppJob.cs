using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Shared.WhatsApp.Contracts;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Modules.Communications.Application.BackgroundJobs;

/// <summary>
/// Phase 7A.3: Background job that sends WhatsApp newsletter broadcasts to opted-in users.
/// Parallel to NewsletterEmailJob (email). Email job is UNTOUCHED.
/// Queued by SendNewsletterCommandHandler alongside NewsletterEmailJob.
/// </summary>
public class NewsletterWhatsAppJob
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<NewsletterWhatsAppJob> _logger;

    public NewsletterWhatsAppJob(
        INewsletterRepository newsletterRepository,
        IWhatsAppService whatsAppService,
        ILogger<NewsletterWhatsAppJob> logger)
    {
        _newsletterRepository = newsletterRepository;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid newsletterId)
    {
        _logger.LogInformation("[Phase 7A] WhatsApp NewsletterBroadcast START: NewsletterId={NewsletterId}", newsletterId);

        try
        {
            var newsletter = await _newsletterRepository.GetByIdAsync(newsletterId, CancellationToken.None);
            if (newsletter == null)
            {
                _logger.LogWarning("[Phase 7A] WhatsApp NewsletterBroadcast: Newsletter not found - NewsletterId={NewsletterId}", newsletterId);
                return;
            }

            var parameters = new Dictionary<string, string>
            {
                { WhatsAppTemplateContract.Newsletter.NewsletterTitle, newsletter.Title?.Value ?? "Newsletter" },
                { WhatsAppTemplateContract.Newsletter.NewsletterPreview, TruncateContent(newsletter.Description?.Value ?? newsletter.Title?.Value ?? "", 100) },
                { WhatsAppTemplateContract.Newsletter.NewsletterUrl, $"https://lankaconnect.com/newsletters/{newsletterId}" }
            };

            // Use event ID from newsletter if it's event-associated, otherwise broadcast generally
            var eventId = newsletter.EventId ?? Guid.Empty;

            if (eventId != Guid.Empty)
            {
                var result = await _whatsAppService.BroadcastToEventAttendeesAsync(
                    eventId,
                    WhatsAppTemplateContract.TemplateNames.NewsletterBroadcast,
                    parameters,
                    WhatsAppNotificationType.Newsletter,
                    CancellationToken.None);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("[Phase 7A] WhatsApp NewsletterBroadcast SENT: NewsletterId={NewsletterId}, SentCount={Count}", newsletterId, result.Value);
                }
                else
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp NewsletterBroadcast FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            else
            {
                _logger.LogInformation(
                    "[Phase 7A] WhatsApp NewsletterBroadcast SKIPPED: No event association - NewsletterId={NewsletterId}",
                    newsletterId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 7A] WhatsApp NewsletterBroadcast EXCEPTION: NewsletterId={NewsletterId}", newsletterId);
        }
    }

    private static string TruncateContent(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content) || content.Length <= maxLength)
            return content;

        return content[..(maxLength - 3)] + "...";
    }
}
