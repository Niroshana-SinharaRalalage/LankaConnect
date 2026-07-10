using System.Globalization;
using System.Text;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;

namespace LankaConnect.Modules.Communications.Contracts.Email.Helpers;

/// <summary>
/// Phase 6A.148.D7: Renders the per-line refund table HTML for email bodies.
///
/// Follows the project's pre-formatted-HTML pattern (see <see cref="OrganizerContactHtmlBuilder"/>)
/// so templates can drop the result in via triple-brace {{{LineItemsHtml}}} for unescaped
/// rendering — keeps logic out of Handlebars (memory feedback_regex_on_email_html applies).
///
/// Two render variants:
///   1. <see cref="BuildRequestedListHtml"/> — pending-review + rejected templates show "what was asked for".
///   2. <see cref="BuildDecisionListHtml"/> — decision template shows "what was asked for AND what got approved".
/// </summary>
public static class RefundLineItemsHtmlBuilder
{
    /// <summary>
    /// Pending-review / rejected variant: 2-column table (Bucket | Requested $X).
    /// </summary>
    public static string BuildRequestedListHtml(IReadOnlyList<RefundLineItemView> lines, string currency)
    {
        if (lines == null || lines.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine(@"<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""border: 1px solid #e2e8f0; border-radius: 6px; border-collapse: separate; overflow: hidden;"">");
        sb.AppendLine(@"<tr style=""background-color: #f7fafc;"">");
        sb.AppendLine(@"<td style=""padding: 8px 12px; font-size: 12px; font-weight: 600; color: #718096; text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid #e2e8f0;"">Item</td>");
        sb.AppendLine(@"<td style=""padding: 8px 12px; font-size: 12px; font-weight: 600; color: #718096; text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid #e2e8f0; text-align: right;"">Requested</td>");
        sb.AppendLine("</tr>");

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var isLast = i == lines.Count - 1;
            var borderStyle = isLast ? string.Empty : "border-bottom: 1px solid #e2e8f0;";

            sb.AppendLine("<tr>");
            sb.AppendLine($@"<td style=""padding: 10px 12px; font-size: 14px; color: #1a202c; {borderStyle}"">{EscapeHtml(line.Type)}</td>");
            sb.AppendLine($@"<td style=""padding: 10px 12px; font-size: 14px; color: #1a202c; text-align: right; font-variant-numeric: tabular-nums; {borderStyle}"">{currency} ${FormatAmount(line.RequestedAmount)}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</table>");
        return sb.ToString();
    }

    /// <summary>
    /// Decision variant: 3-column table (Bucket | Requested | Decision).
    /// Decision cell colour-codes by status: approved=green, rejected=red, processing=amber, refunded=green-bold, failed=red-bold.
    /// </summary>
    public static string BuildDecisionListHtml(IReadOnlyList<RefundLineItemView> lines, string currency)
    {
        if (lines == null || lines.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine(@"<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""border: 1px solid #e2e8f0; border-radius: 6px; border-collapse: separate; overflow: hidden;"">");
        sb.AppendLine(@"<tr style=""background-color: #f7fafc;"">");
        sb.AppendLine(@"<td style=""padding: 8px 12px; font-size: 12px; font-weight: 600; color: #718096; text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid #e2e8f0;"">Item</td>");
        sb.AppendLine(@"<td style=""padding: 8px 12px; font-size: 12px; font-weight: 600; color: #718096; text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid #e2e8f0; text-align: right;"">Requested</td>");
        sb.AppendLine(@"<td style=""padding: 8px 12px; font-size: 12px; font-weight: 600; color: #718096; text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid #e2e8f0; text-align: right;"">Decision</td>");
        sb.AppendLine("</tr>");

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var isLast = i == lines.Count - 1;
            var borderStyle = isLast ? string.Empty : "border-bottom: 1px solid #e2e8f0;";
            var (decisionText, decisionColor, decisionWeight) = FormatDecision(line, currency);

            sb.AppendLine("<tr>");
            sb.AppendLine($@"<td style=""padding: 10px 12px; font-size: 14px; color: #1a202c; {borderStyle}"">{EscapeHtml(line.Type)}</td>");
            sb.AppendLine($@"<td style=""padding: 10px 12px; font-size: 14px; color: #1a202c; text-align: right; font-variant-numeric: tabular-nums; {borderStyle}"">{currency} ${FormatAmount(line.RequestedAmount)}</td>");
            sb.AppendLine($@"<td style=""padding: 10px 12px; font-size: 14px; color: {decisionColor}; font-weight: {decisionWeight}; text-align: right; font-variant-numeric: tabular-nums; {borderStyle}"">{decisionText}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</table>");
        return sb.ToString();
    }

    private static (string Text, string Color, string Weight) FormatDecision(RefundLineItemView line, string currency)
    {
        var status = (line.Status ?? string.Empty).ToLowerInvariant();
        return status switch
        {
            "approved" => ($"Approved {currency} ${FormatAmount(line.ApprovedAmount ?? 0m)}", "#15803d", "600"),
            "processing" => ($"Processing {currency} ${FormatAmount(line.ApprovedAmount ?? 0m)}", "#b45309", "600"),
            "refunded" => ($"Refunded {currency} ${FormatAmount(line.ApprovedAmount ?? 0m)}", "#15803d", "700"),
            "rejected" => ("Declined", "#b91c1c", "600"),
            "failed" => ("Failed — operator will retry", "#b91c1c", "700"),
            _ => (line.ApprovedAmount.HasValue
                    ? $"{currency} ${FormatAmount(line.ApprovedAmount.Value)}"
                    : "Pending", "#4a5568", "500")
        };
    }

    private static string FormatAmount(decimal value) =>
        value.ToString("F2", CultureInfo.InvariantCulture);

    private static string EscapeHtml(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}
