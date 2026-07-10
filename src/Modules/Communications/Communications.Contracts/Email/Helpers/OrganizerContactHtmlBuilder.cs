using System.Text;

namespace LankaConnect.Modules.Communications.Contracts.Email.Helpers;

/// <summary>
/// Phase 6A.133 Email: Simple DTO for organizer contact information used in email rendering.
/// </summary>
public record OrganizerContactInfo(string Name, string? Email, string? Phone, bool IsPrimary);

/// <summary>
/// Phase 6A.133 Email: Generates email-safe HTML for displaying multiple organizer contacts.
/// Follows the pre-formatted HTML pattern used by AttendeesHtml, NewAttendeesHtml, etc.
/// Output is passed via triple-brace {{{OrganizerContactsHtml}}} for unescaped rendering.
/// </summary>
public static class OrganizerContactHtmlBuilder
{
    /// <summary>
    /// Returns the appropriate header text based on contact count.
    /// Single contact: "EVENT ORGANIZER", multiple: "EVENT ORGANIZERS"
    /// </summary>
    public static string BuildHeaderText(int contactCount)
    {
        return contactCount > 1 ? "EVENT ORGANIZERS" : "EVENT ORGANIZER";
    }

    /// <summary>
    /// Generates email-safe HTML table for organizer contacts.
    /// Tabular format with Name | Email | Phone columns, matching the event details page layout.
    /// </summary>
    public static string BuildContactListHtml(IReadOnlyList<OrganizerContactInfo> contacts)
    {
        if (contacts.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();

        // Table wrapper with border
        sb.AppendLine(@"<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""border: 1px solid #e2e8f0; border-radius: 6px; border-collapse: separate; overflow: hidden;"">");

        // Header row
        sb.AppendLine(@"<tr style=""background-color: #f7fafc;"">");
        sb.AppendLine(@"<td style=""padding: 8px 12px; font-size: 12px; font-weight: 600; color: #718096; text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid #e2e8f0;"">Name</td>");
        sb.AppendLine(@"<td style=""padding: 8px 12px; font-size: 12px; font-weight: 600; color: #718096; text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid #e2e8f0;"">Email</td>");
        sb.AppendLine(@"<td style=""padding: 8px 12px; font-size: 12px; font-weight: 600; color: #718096; text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid #e2e8f0;"">Phone</td>");
        sb.AppendLine("</tr>");

        // Data rows
        for (var i = 0; i < contacts.Count; i++)
        {
            var contact = contacts[i];
            var borderStyle = i < contacts.Count - 1
                ? @"border-bottom: 1px solid #e2e8f0;"
                : "";

            sb.AppendLine("<tr>");

            // Name cell
            sb.Append($@"<td style=""padding: 10px 12px; font-size: 14px; color: #2d3748; {borderStyle}"">");
            sb.Append(contact.Name);
            if (contact.IsPrimary)
            {
                sb.Append(@" <span style=""display: inline-block; background-color: #fff1f2; color: #9f1239; font-size: 11px; font-weight: 600; padding: 1px 6px; border-radius: 10px; vertical-align: middle;"">Primary</span>");
            }
            sb.AppendLine("</td>");

            // Email cell
            sb.Append($@"<td style=""padding: 10px 12px; font-size: 14px; {borderStyle}"">");
            if (!string.IsNullOrWhiteSpace(contact.Email))
            {
                sb.Append($@"<a href=""mailto:{contact.Email}"" style=""color: #9f1239; text-decoration: none;"">{contact.Email}</a>");
            }
            else
            {
                sb.Append(@"<span style=""color: #a0aec0;"">&mdash;</span>");
            }
            sb.AppendLine("</td>");

            // Phone cell
            sb.Append($@"<td style=""padding: 10px 12px; font-size: 14px; color: #4a5568; {borderStyle}"">");
            if (!string.IsNullOrWhiteSpace(contact.Phone))
            {
                sb.Append($@"<a href=""tel:{contact.Phone}"" style=""color: #4a5568; text-decoration: none;"">{contact.Phone}</a>");
            }
            else
            {
                sb.Append(@"<span style=""color: #a0aec0;"">&mdash;</span>");
            }
            sb.AppendLine("</td>");

            sb.AppendLine("</tr>");
        }

        sb.Append("</table>");

        return sb.ToString().TrimEnd();
    }
}
