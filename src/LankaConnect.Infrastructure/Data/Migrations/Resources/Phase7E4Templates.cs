using System;
using System.IO;
using System.Reflection;

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations.Resources;

/// <summary>
/// Phase 7E.4: Loads v2 email-template HTML for the flexible registration modes feature
/// (mode-aware Handlebars block + tone-B copy + anchor comments).
///
/// Embedded resources (not <c>File.ReadAllText</c>) per MEMORY 6A.129b — disk layout
/// differs between local dev, CI/CD, and Docker containers. Mirrors the Phase 7C.2
/// recovery pattern at <see cref="Phase7C2RecoveryTemplates"/>.
///
/// Templates currently shipped in this resource bundle:
/// - <c>template-free-event-registration-confirmation.html</c> (registration confirmation,
///   used by both <c>RegistrationConfirmedEventHandler</c> and
///   <c>AnonymousRegistrationConfirmedEventHandler</c> per Phase 6A.80).
/// - More templates land here as the rest of 7E.4 ships (cancellation, event-cancellation,
///   reminder, attendees-added).
/// </summary>
public static class Phase7E4Templates
{
    private const string ResourceNamespace =
        "LankaConnect.SPLIT_PER_ENTITY.Migrations.Resources.Phase7E4";

    public static string LoadHtml(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name must not be empty.", nameof(templateName));

        var resourceName = $"{ResourceNamespace}.{templateName}.html";
        var assembly = typeof(Phase7E4Templates).Assembly;

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
