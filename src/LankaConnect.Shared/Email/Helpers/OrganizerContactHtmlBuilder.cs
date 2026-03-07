using System.Text;

namespace LankaConnect.Shared.Email.Helpers;

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
    /// Generates email-safe HTML for a list of organizer contacts.
    /// Each contact shows name (bold, with Primary badge if applicable), email (mailto link), and phone.
    /// Contacts are separated by horizontal dividers.
    /// </summary>
    public static string BuildContactListHtml(IReadOnlyList<OrganizerContactInfo> contacts)
    {
        if (contacts.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();

        for (var i = 0; i < contacts.Count; i++)
        {
            var contact = contacts[i];

            // Add divider between contacts (not before first)
            if (i > 0)
            {
                sb.AppendLine(@"<tr><td style=""padding: 0;""><table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td style=""border-bottom: 1px solid #e2e8f0; padding: 0; height: 1px; font-size: 0; line-height: 0;"">&nbsp;</td></tr></table></td></tr>");
                sb.AppendLine(@"<tr><td style=""padding: 4px 0;""></td></tr>");
            }

            // Contact name row (bold, with optional Primary badge)
            sb.Append(@"<tr><td style=""padding: 4px 0; font-size: 15px; color: #4a5568;"">");
            sb.Append($@"<strong style=""color: #2d3748;"">{contact.Name}</strong>");

            if (contact.IsPrimary)
            {
                sb.Append(@" <span style=""display: inline-block; background-color: #fff1f2; color: #9f1239; font-size: 11px; font-weight: 600; padding: 2px 8px; border-radius: 10px; vertical-align: middle;"">Primary</span>");
            }

            sb.AppendLine("</td></tr>");

            // Email row (if available)
            if (!string.IsNullOrWhiteSpace(contact.Email))
            {
                sb.AppendLine($@"<tr><td style=""padding: 2px 0 2px 12px; font-size: 14px; color: #4a5568;""><a href=""mailto:{contact.Email}"" style=""color: #9f1239; text-decoration: none;"">{contact.Email}</a></td></tr>");
            }

            // Phone row (if available)
            if (!string.IsNullOrWhiteSpace(contact.Phone))
            {
                sb.AppendLine($@"<tr><td style=""padding: 2px 0 2px 12px; font-size: 14px; color: #4a5568;""><a href=""tel:{contact.Phone}"" style=""color: #4a5568; text-decoration: none;"">{contact.Phone}</a></td></tr>");
            }
        }

        return sb.ToString().TrimEnd();
    }
}
