using System.Text;

namespace LankaConnect.Modules.Communications.Contracts.Email.Contracts;

/// <summary>
/// Phase 6A.157 — typed parameters for the packaged-sponsorship confirmation
/// email. Forked from <see cref="SponsorConfirmationEmailParams"/> because the
/// content is materially different:
///   - Voice: "Welcome, Gold Tier sponsor — here's what you're getting" vs
///     "Thank you for your $X contribution"
///   - Includes perks bullet list (rendered server-side as <c>&lt;ul&gt;</c>)
///   - Includes conditional included-tickets paragraph when count &gt; 0
///   - Includes tier badge
///
/// Per user pivot 2026-05-31, the included-tickets line is INFORMATIONAL only.
/// Organizer handles admission off-platform at the gate — the system does NOT
/// issue tickets for package sponsors.
/// </summary>
public class PackageSponsorConfirmationEmailParams : IEmailParameters
{
    public string TemplateName => EmailTemplateContract.TemplateNames.PackageSponsorConfirmation;

    public string RecipientEmail => SponsorEmail;
    public string RecipientName => SponsorName;

    #region Core Fields

    public string SponsorName { get; set; } = string.Empty;
    public string SponsorEmail { get; set; } = string.Empty;
    public string? SponsorOrganization { get; set; }
    public string EventTitle { get; set; } = string.Empty;

    // Package snapshot fields (from Sponsor.Package*Snapshot)
    public string PackageNameSnapshot { get; set; } = string.Empty;
    public string? PackageTierSnapshot { get; set; }
    public decimal AmountPaid { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime PaymentDate { get; set; }
    public string PaymentIntentId { get; set; } = string.Empty;
    public string EventDetailsUrl { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = "support@lankaconnect.com";

    /// <summary>
    /// Pre-rendered HTML bullet list of perks. Empty string when the package
    /// has no perks — template conditional-renders the surrounding block.
    /// Render shape: <c>&lt;ul&gt;&lt;li&gt;Perk A&lt;/li&gt;…&lt;/ul&gt;</c>
    /// </summary>
    public string PerksHtml { get; set; } = string.Empty;

    public bool HasPerks { get; set; }

    /// <summary>
    /// Phase 6A.157 — included-ticket count. Drives the conditional
    /// informational paragraph below the package summary. The text says
    /// "the organizer will coordinate your admission directly" — accurate
    /// because no tickets are issued by the platform.
    /// </summary>
    public int IncludedTicketCount { get; set; }

    public bool HasIncludedTickets => IncludedTicketCount > 0;

    public string Year { get; set; } = DateTime.UtcNow.Year.ToString();

    #endregion

    /// <summary>
    /// Factory that snaps the params from a completed package sponsor + its
    /// originating event. Called from <c>PackageSponsorCompletedEventHandler</c>.
    /// </summary>
    public static PackageSponsorConfirmationEmailParams Create(
        string sponsorName,
        string sponsorEmail,
        string? sponsorOrganization,
        string eventTitle,
        string packageNameSnapshot,
        string? packageTierSnapshot,
        decimal amountPaid,
        string currency,
        DateTime paymentDate,
        string paymentIntentId,
        int includedTicketCount,
        IReadOnlyList<string> perks,
        string eventDetailsUrl,
        string? supportEmail = null)
    {
        var perksHtml = BuildPerksHtml(perks);
        return new PackageSponsorConfirmationEmailParams
        {
            SponsorName = sponsorName,
            SponsorEmail = sponsorEmail,
            SponsorOrganization = sponsorOrganization,
            EventTitle = eventTitle,
            PackageNameSnapshot = packageNameSnapshot,
            PackageTierSnapshot = packageTierSnapshot,
            AmountPaid = amountPaid,
            Currency = currency,
            PaymentDate = paymentDate,
            PaymentIntentId = paymentIntentId,
            IncludedTicketCount = includedTicketCount,
            PerksHtml = perksHtml,
            HasPerks = !string.IsNullOrEmpty(perksHtml),
            EventDetailsUrl = eventDetailsUrl,
            SupportEmail = supportEmail ?? "support@lankaconnect.com"
        };
    }

    private static string BuildPerksHtml(IReadOnlyList<string> perks)
    {
        if (perks == null || perks.Count == 0) return string.Empty;
        var sb = new StringBuilder();
        sb.Append("<ul style=\"margin: 8px 0; padding-left: 20px;\">");
        foreach (var perk in perks)
        {
            if (string.IsNullOrWhiteSpace(perk)) continue;
            sb.Append("<li style=\"margin: 4px 0; color: #374151;\">");
            sb.Append(System.Net.WebUtility.HtmlEncode(perk));
            sb.Append("</li>");
        }
        sb.Append("</ul>");
        return sb.ToString();
    }

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            ["SponsorName"] = SponsorName,
            ["SponsorEmail"] = SponsorEmail,
            ["SponsorOrganization"] = SponsorOrganization ?? string.Empty,
            ["HasOrganization"] = !string.IsNullOrEmpty(SponsorOrganization),
            ["EventTitle"] = EventTitle,
            ["PackageNameSnapshot"] = PackageNameSnapshot,
            ["PackageTierSnapshot"] = PackageTierSnapshot ?? string.Empty,
            ["HasTier"] = !string.IsNullOrEmpty(PackageTierSnapshot),
            ["AmountPaid"] = AmountPaid.ToString("0.00"),
            ["Currency"] = Currency,
            ["PaymentDate"] = PaymentDate.ToString("MMMM d, yyyy"),
            ["PaymentIntentId"] = PaymentIntentId,
            ["IncludedTicketCount"] = IncludedTicketCount,
            ["HasIncludedTickets"] = HasIncludedTickets,
            ["PerksHtml"] = PerksHtml,
            ["HasPerks"] = HasPerks,
            ["EventDetailsUrl"] = EventDetailsUrl,
            ["SupportEmail"] = SupportEmail,
            ["Year"] = Year
        };
    }

    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();
        if (string.IsNullOrWhiteSpace(SponsorName)) errors.Add("SponsorName is required");
        if (string.IsNullOrWhiteSpace(SponsorEmail)) errors.Add("SponsorEmail is required");
        if (string.IsNullOrWhiteSpace(EventTitle)) errors.Add("EventTitle is required");
        if (string.IsNullOrWhiteSpace(PackageNameSnapshot)) errors.Add("PackageNameSnapshot is required");
        if (AmountPaid < 0) errors.Add("AmountPaid cannot be negative");
        if (IncludedTicketCount < 0) errors.Add("IncludedTicketCount cannot be negative");
        return errors.Count == 0;
    }
}
