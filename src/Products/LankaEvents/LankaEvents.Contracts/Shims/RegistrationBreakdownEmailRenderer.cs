using System.Net;
using System.Text;
using LankaConnect.Products.LankaEvents.Contracts.DTOs;

namespace LankaConnect.Products.LankaEvents.Contracts.Shims; // Wave 8.5.d (2026-07-18): split from LegacyPromotions/ per Consult #17 Q2 Day 10 debt.

/// <summary>
/// Phase 7F-E.3 (architect-approved 2026-05-01): renders a <see cref="RegistrationBreakdown"/>
/// into an inline-styled HTML fragment that drops into the existing Phase 7F-A
/// <c>&lt;!-- attendee-block-7e --&gt;</c> anchor of every email template that carries
/// the Mode-B head-count card.
///
/// One renderer, one token (<c>{{RegistrationBreakdownHtml}}</c>), one consistent
/// rendering across the 4 (eventually 5) confirmation / cancellation / reminder /
/// add-attendee / paid-event templates.
///
/// Architect rules (review iteration 1, 2026-05-01):
/// - <c>BreakdownPair.Captured == false</c> → render the literal string "N/A".
/// - Match the existing 7F-A aesthetic: rounded warm card (#fefaf7 / #f3e4d5), 14px
///   text, "Segoe UI" first, table-based layout, inline styles only (Outlook-safe).
/// - All injected user-controlled content (lead name, tier name) is HTML-encoded.
/// - Renderer-template coupling is the explicit trade-off: keeps logic out of the
///   template (memory <c>feedback_regex_on_email_html.md</c>); the template now has
///   exactly one token to substitute.
/// </summary>
public static class RegistrationBreakdownEmailRenderer
{
    /// <summary>
    /// Returns an empty string when the breakdown has no rows so the surrounding
    /// template's anchor block contributes nothing (defensive — caller drops in the
    /// rendered string verbatim).
    /// </summary>
    public static string Render(RegistrationBreakdown breakdown, string? leadAttendeeName)
    {
        if (breakdown == null) return string.Empty;
        if (breakdown.Rows.Count == 0) return string.Empty;

        var sb = new StringBuilder(2048);

        // Outer warm card — same colour palette as the 7F-A block we're replacing.
        sb.Append(@"<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 0 0 16px"">");
        sb.Append(@"<tr><td style=""background: #fefaf7; border: 1px solid #f3e4d5; border-radius: 12px; overflow: hidden;"">");

        // Header row — "Registered Attendees" matching the existing block's emoji + colour.
        sb.Append(@"<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">");
        sb.Append(@"<tr><td style=""padding: 14px 22px 10px; border-bottom: 1px solid #f3e4d5;"">");
        sb.Append(@"<p style=""font-family: 'Segoe UI', Arial, sans-serif; font-size: 12px; font-weight: 700; color: #9f1239; margin: 0;"">");
        sb.Append("&#128101;&ensp;Registered Attendees");
        sb.Append(@"</p></td></tr></table>");

        // Body
        sb.Append(@"<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0""><tr><td style=""padding: 16px 22px 18px; font-family: 'Segoe UI', Arial, sans-serif; font-size: 14px; color: #374151; line-height: 1.7;"">");

        // Lead name (Mode B only; absent for Mode A which uses the per-attendee list).
        if (!string.IsNullOrWhiteSpace(leadAttendeeName))
        {
            sb.Append(@"<p style=""margin: 0 0 6px; font-size: 16px; font-weight: 600;"">");
            sb.Append(WebUtility.HtmlEncode(leadAttendeeName));
            sb.Append("</p>");
        }

        // Total attendees
        sb.Append(@"<p style=""margin: 0 0 14px; color: #6b7280;"">Total attendees: <strong style=""color: #374151;"">");
        sb.Append(breakdown.TotalAttendees);
        sb.Append("</strong></p>");

        // Per-row breakdown — one inner card per tier (or one card total if non-tiered).
        foreach (var row in breakdown.Rows)
        {
            sb.Append(@"<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 0 0 8px""><tr><td style=""background: #ffffff; border: 1px solid #f3e4d5; border-radius: 8px; padding: 10px 14px; font-size: 13px;"">");

            if (!string.IsNullOrWhiteSpace(row.TierName))
            {
                sb.Append(@"<p style=""margin: 0 0 4px;""><span style=""color: #6b7280;"">Tier:</span> <strong style=""color: #8B1538;"">");
                sb.Append(WebUtility.HtmlEncode(row.TierName));
                sb.Append("</strong></p>");
            }

            sb.Append(@"<p style=""margin: 0 0 2px;""><span style=""color: #6b7280;"">Number of attendees:</span> <strong>");
            sb.Append(row.Count);
            sb.Append("</strong></p>");

            sb.Append(@"<p style=""margin: 0 0 2px;""><span style=""color: #6b7280;"">Adult/Child:</span> <strong>");
            sb.Append(FormatPair(row.Age));
            sb.Append("</strong></p>");

            sb.Append(@"<p style=""margin: 0;""><span style=""color: #6b7280;"">Male/Female:</span> <strong>");
            sb.Append(FormatPair(row.Gender));
            sb.Append("</strong></p>");

            sb.Append("</td></tr></table>");
        }

        // Phase 7F-E.6.A (architect-approved 2026-05-04): registration-level totals row
        // for multi-tier B-mode breakdowns. Per-tier rows above can't carry registration-
        // level demographics (architect §2.2 #4 deferred per-tier-gender storage), so
        // when the registration DID capture top-level demographics this surfaces them
        // honestly. Architect placement guidance: bottom of the per-tier list, with
        // visual separation so the reader doesn't confuse it with another tier card.
        if (breakdown.Totals is { } totals)
        {
            sb.Append(@"<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 4px 0 0""><tr><td style=""background: #fefaf7; border: 1px solid #f3e4d5; border-radius: 8px; padding: 10px 14px; font-size: 13px;"">");
            sb.Append(@"<p style=""margin: 0 0 4px;""><strong style=""color: #8B1538;"">Total</strong> <span style=""color: #6b7280;"">(across all tiers)</span></p>");
            sb.Append(@"<p style=""margin: 0 0 2px;""><span style=""color: #6b7280;"">Adult/Child:</span> <strong>");
            sb.Append(FormatPair(totals.Age));
            sb.Append("</strong></p>");
            sb.Append(@"<p style=""margin: 0;""><span style=""color: #6b7280;"">Male/Female:</span> <strong>");
            sb.Append(FormatPair(totals.Gender));
            sb.Append("</strong></p>");
            sb.Append("</td></tr></table>");
        }

        sb.Append("</td></tr></table>");
        sb.Append("</td></tr></table>");

        return sb.ToString();
    }

    private static string FormatPair(BreakdownPair pair)
    {
        if (!pair.Captured) return "N/A";
        return $"{pair.Left}/{pair.Right}";
    }
}
