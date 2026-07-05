using System;
using System.IO;

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations.Resources;

/// <summary>
/// Phase 7F-E.3 (architect-approved 2026-05-01): loads the 5 email-template HTML bodies
/// migrated to use the structured <c>{{{RegistrationBreakdownHtml}}}</c> token (Slice
/// 7F-E.3 — replaces the legacy flat tokens HeadCountTotal / HeadCountBreakdownLine /
/// TierBreakdownLine with a single pre-rendered HTML fragment).
///
/// Embedded resources only (memory 6A.129b — disk layout differs across local / CI /
/// Docker; never <c>File.ReadAllText</c> in migrations).
///
/// Templates:
/// - <c>template-free-event-registration-confirmation.html</c> (anchor inner replaced)
/// - <c>template-event-cancellation-notifications.html</c> (anchor inner replaced)
/// - <c>template-event-reminder.html</c> (anchor inner replaced)
/// - <c>template-attendees-added-confirmation.html</c> (anchor inner replaced)
/// - <c>template-paid-event-registration-confirmation-with-ticket.html</c> (NEW anchor
///   block inserted before the PAYMENT CONFIRMATION CARD — fixes the user-reported
///   gap where the paid-event email had no per-tier breakdown at all).
/// </summary>
public static class Phase7FETemplates
{
    private const string ResourceNamespace =
        "LankaConnect.SPLIT_PER_ENTITY.Migrations.Resources.Phase7F_E";

    public static string LoadHtml(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name must not be empty.", nameof(templateName));

        var resourceName = $"{ResourceNamespace}.{templateName}.html";
        var assembly = typeof(Phase7FETemplates).Assembly;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new ArgumentException(
                $"Embedded template '{templateName}' not found (looked up '{resourceName}'). " +
                $"Available: {string.Join(", ", assembly.GetManifestResourceNames())}",
                nameof(templateName));
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
