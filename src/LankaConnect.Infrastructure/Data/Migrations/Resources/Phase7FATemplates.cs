using System;
using System.IO;
using System.Reflection;

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations.Resources;

/// <summary>
/// Phase 7F-A: Loads v2 email-template HTML for the 3 lifecycle templates that need the
/// Mode-B head-count card (cancellation broadcast, reminder, attendees-added). Mirrors
/// <see cref="Phase7E4Templates"/> — embedded resources only (per MEMORY 6A.129b — disk
/// layout differs across local / CI / Docker).
///
/// Templates shipped in this resource bundle:
/// - <c>template-event-cancellation-notifications.html</c> (organiser-cancels-event broadcast).
/// - <c>template-event-reminder.html</c> (cron-driven reminder).
/// - <c>template-attendees-added-confirmation.html</c> (post-add-attendees confirmation).
/// </summary>
public static class Phase7FATemplates
{
    private const string ResourceNamespace =
        "LankaConnect.SPLIT_PER_ENTITY.Migrations.Resources.Phase7F_A";

    public static string LoadHtml(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name must not be empty.", nameof(templateName));

        var resourceName = $"{ResourceNamespace}.{templateName}.html";
        var assembly = typeof(Phase7FATemplates).Assembly;

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
