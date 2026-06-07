using LankaConnect.Domain.Common;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Infrastructure.WhatsApp.Configuration;
using LankaConnect.Shared.WhatsApp.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LankaConnect.Infrastructure.WhatsApp.Services;

/// <summary>
/// Phase 7B.4: Startup-time seeder that reconciles the <c>communications.whatsapp_templates</c>
/// table with <see cref="WhatsAppSettings.TwilioContentSids"/>. For every
/// <c>(template_name, HX-sid)</c> pair in config:
///  - If the template row exists, its <c>TwilioContentSid</c> is updated and the row is
///    approved via <see cref="WhatsAppTemplate.MarkApprovedForTwilio"/>.
///  - If the row is missing, it is created from the built-in Phase 7B.3 catalog (only
///    known names are accepted — typos are logged and skipped).
///
/// The seeder is idempotent — running it every startup produces zero new rows after the
/// first successful run. Failures on individual rows are logged but never thrown: we must
/// not crash the host on seed errors.
/// </summary>
public class TwilioTemplateSeeder : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<TwilioTemplateSeeder> _logger;

    public TwilioTemplateSeeder(
        IServiceScopeFactory scopeFactory,
        IOptions<WhatsAppSettings> settings,
        ILogger<TwilioTemplateSeeder> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var templateRepo = scope.ServiceProvider.GetRequiredService<IWhatsAppTemplateRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var summary = await SeedAsync(templateRepo, cancellationToken);

            if (summary.Seeded > 0 || summary.Updated > 0)
            {
                await unitOfWork.CommitAsync(cancellationToken);
            }

            _logger.LogInformation(
                "[Phase 7B.4] TwilioTemplateSeeder: seeded={Seeded}, updated={Updated}, skipped={Skipped}, failed={Failed}. {Reason}",
                summary.Seeded, summary.Updated, summary.Skipped, summary.Failed, summary.Reason);
        }
        catch (Exception ex)
        {
            // Never throw from a hosted service StartAsync — app must still boot.
            _logger.LogError(ex,
                "[Phase 7B.4] TwilioTemplateSeeder: unexpected failure during startup seed. " +
                "WhatsApp sends will fail with 'TwilioContentSid not configured' until the seed is fixed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Test-visible seed loop. Given a concrete template repository, applies every entry in
    /// <c>TwilioContentSids</c> and returns a summary. Caller is responsible for persisting
    /// (UnitOfWork commit).
    /// </summary>
    public async Task<SeedSummary> SeedAsync(
        IWhatsAppTemplateRepository templateRepo,
        CancellationToken ct)
    {
        if (_settings.Provider != WhatsAppProvider.Twilio)
        {
            return new SeedSummary(0, 0, 0, 0,
                $"Skipped — Provider={_settings.Provider}, not Twilio");
        }

        if (_settings.TwilioContentSids.Count == 0)
        {
            return new SeedSummary(0, 0, 0, 0,
                "Skipped — no TwilioContentSids configured (empty dictionary)");
        }

        int seeded = 0, updated = 0, skipped = 0, failed = 0;

        foreach (var (templateName, contentSid) in _settings.TwilioContentSids)
        {
            try
            {
                var existing = await templateRepo.GetByNameAsync(templateName, ct);

                if (existing != null)
                {
                    existing.SetTwilioContentSid(contentSid);
                    existing.MarkApprovedForTwilio();
                    updated++;
                    _logger.LogInformation(
                        "[Phase 7B.4] Updated template {TemplateName} with Twilio ContentSid {ContentSid}",
                        templateName, contentSid);
                    continue;
                }

                // Missing — must come from the Phase 7B.3 catalog. Reject typos.
                if (!TwilioTemplateCatalog.TryBuild(templateName, out var created))
                {
                    skipped++;
                    _logger.LogWarning(
                        "[Phase 7B.4] Template {TemplateName} is not in the built-in catalog and does not exist in the DB. " +
                        "Likely a typo in WhatsAppSettings:TwilioContentSids. Skipping.",
                        templateName);
                    continue;
                }

                created!.SetTwilioContentSid(contentSid);
                created.MarkApprovedForTwilio();
                await templateRepo.AddAsync(created, ct);
                seeded++;
                _logger.LogInformation(
                    "[Phase 7B.4] Created template {TemplateName} with Twilio ContentSid {ContentSid}",
                    templateName, contentSid);
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex,
                    "[Phase 7B.4] Failed to seed template {TemplateName}. Continuing with remaining templates.",
                    templateName);
            }
        }

        return new SeedSummary(seeded, updated, skipped, failed,
            $"Processed {_settings.TwilioContentSids.Count} entries");
    }

    public record SeedSummary(int Seeded, int Updated, int Skipped, int Failed, string Reason);
}

/// <summary>
/// Built-in metadata for Phase 7B.3 templates that do not exist in the Phase 7A migration.
/// The 14 original Phase 7A templates are already in the DB — for those the seeder only updates
/// the Twilio ContentSid. These 11 entries are used when the seeder needs to create a row.
/// Only templates listed here can be created by the seeder; unknown names are treated as typos.
/// Body text is a placeholder — the real message content lives in the Twilio Content Template.
/// </summary>
internal static class TwilioTemplateCatalog
{
    private static readonly Dictionary<string, TemplateDefinition> _catalog = new(StringComparer.OrdinalIgnoreCase)
    {
        // Phase 7B.3 — Tier 1 HIGH
        [WhatsAppTemplateContract.TemplateNames.PaymentPendingReminder] = new(
            "Payment Pending Reminder",
            WhatsAppTemplateCategory.Utility,
            new[] {
                WhatsAppTemplateContract.Common.UserName,
                WhatsAppTemplateContract.Common.EventTitle,
                WhatsAppTemplateContract.Payment.PaymentAmount,
                WhatsAppTemplateContract.Payment.ExpiryTime,
                WhatsAppTemplateContract.Common.EventUrl
            }),
        [WhatsAppTemplateContract.TemplateNames.EventPostponed] = new(
            "Event Postponed",
            WhatsAppTemplateCategory.Utility,
            new[] {
                WhatsAppTemplateContract.Common.UserName,
                WhatsAppTemplateContract.Common.EventTitle,
                WhatsAppTemplateContract.Postponement.PostponementReason,
                WhatsAppTemplateContract.Common.EventUrl
            }),
        [WhatsAppTemplateContract.TemplateNames.DonationReceipt] = new(
            "Donation Receipt",
            WhatsAppTemplateCategory.Utility,
            new[] {
                WhatsAppTemplateContract.Common.UserName,
                WhatsAppTemplateContract.Donation.DonationAmount,
                WhatsAppTemplateContract.Common.EventTitle,
                WhatsAppTemplateContract.Common.EventUrl
            }),

        // Phase 7B.3 — Tier 2 Organizer notifications
        [WhatsAppTemplateContract.TemplateNames.EventApproved] = new(
            "Event Approved",
            WhatsAppTemplateCategory.Utility,
            new[] {
                WhatsAppTemplateContract.Common.UserName,
                WhatsAppTemplateContract.Common.EventTitle,
                WhatsAppTemplateContract.Common.EventUrl
            }),
        [WhatsAppTemplateContract.TemplateNames.EventRejected] = new(
            "Event Rejected",
            WhatsAppTemplateCategory.Utility,
            new[] {
                WhatsAppTemplateContract.Common.UserName,
                WhatsAppTemplateContract.Common.EventTitle,
                WhatsAppTemplateContract.Rejection.RejectionReason
            }),

        // Phase 7B.3 — Tier 2 Financial confirmations
        [WhatsAppTemplateContract.TemplateNames.AttendeesAdded] = new(
            "Attendees Added",
            WhatsAppTemplateCategory.Utility,
            new[] {
                WhatsAppTemplateContract.Common.UserName,
                WhatsAppTemplateContract.Common.EventTitle,
                WhatsAppTemplateContract.Attendees.AddedCount,
                WhatsAppTemplateContract.Attendees.NewTotalCount,
                WhatsAppTemplateContract.Common.EventUrl
            }),
        [WhatsAppTemplateContract.TemplateNames.AddOnPurchaseReceipt] = new(
            "Add-on Purchase Receipt",
            WhatsAppTemplateCategory.Utility,
            new[] {
                WhatsAppTemplateContract.Common.UserName,
                WhatsAppTemplateContract.AddOn.AddOnName,
                WhatsAppTemplateContract.AddOn.Quantity,
                WhatsAppTemplateContract.AddOn.TotalAmount,
                WhatsAppTemplateContract.Common.EventTitle
            }),
        [WhatsAppTemplateContract.TemplateNames.CollectionReceipt] = new(
            "Collection Receipt",
            WhatsAppTemplateCategory.Utility,
            new[] {
                WhatsAppTemplateContract.Common.UserName,
                WhatsAppTemplateContract.CollectionParams.ContributionAmount,
                WhatsAppTemplateContract.Common.EventTitle,
                WhatsAppTemplateContract.Common.EventUrl
            }),
        [WhatsAppTemplateContract.TemplateNames.SponsorConfirmation] = new(
            "Sponsor Confirmation",
            WhatsAppTemplateCategory.Utility,
            new[] {
                WhatsAppTemplateContract.Common.UserName,
                WhatsAppTemplateContract.Sponsor.SponsorType,
                WhatsAppTemplateContract.Sponsor.SponsorDetails,
                WhatsAppTemplateContract.Common.EventTitle,
                WhatsAppTemplateContract.Common.EventUrl
            }),

        // Phase 7B.3 — Tier 2 Engagement
        [WhatsAppTemplateContract.TemplateNames.FormResponseConfirmed] = new(
            "Form Response Confirmed",
            WhatsAppTemplateCategory.Utility,
            new[] {
                WhatsAppTemplateContract.Common.UserName,
                WhatsAppTemplateContract.Form.FormTitle,
                WhatsAppTemplateContract.Common.EventTitle,
                WhatsAppTemplateContract.Common.EventUrl
            }),

        // Phase 7B.3 — Tier 3
        [WhatsAppTemplateContract.TemplateNames.PhotoAlbumPublished] = new(
            "Photo Album Published",
            WhatsAppTemplateCategory.Marketing,
            new[] {
                WhatsAppTemplateContract.Common.UserName,
                WhatsAppTemplateContract.Common.EventTitle,
                WhatsAppTemplateContract.Album.AlbumName,
                WhatsAppTemplateContract.Album.AlbumUrl
            })
    };

    public static bool TryBuild(string templateName, out WhatsAppTemplate? template)
    {
        template = null;
        if (!_catalog.TryGetValue(templateName, out var def))
        {
            return false;
        }

        // Body text is a placeholder — Twilio Content API uses the template stored in Twilio,
        // not this DB row's body_text. ParameterNames/ParameterCount remain accurate for
        // observability and the param-count parity check.
        var body = $"[Twilio-managed template: {templateName}]";
        template = WhatsAppTemplate.Create(
            templateName: templateName,
            displayName: def.DisplayName,
            category: def.Category,
            bodyText: body,
            parameterNames: def.Parameters.ToList(),
            language: "en");

        return true;
    }

    private record TemplateDefinition(
        string DisplayName,
        WhatsAppTemplateCategory Category,
        IReadOnlyList<string> Parameters);
}
