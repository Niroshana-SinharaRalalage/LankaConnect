using System;
using System.IO;
using System.Reflection;

namespace LankaConnect.Infrastructure.Data.Migrations.Resources;

/// <summary>
/// Phase 7C.2 recovery — loads authoritative pre-damage signup/volunteer commitment
/// email template bodies from embedded resources. The recovery migration UPDATEs the
/// live <c>communications.email_templates</c> rows with these bodies to undo the damage
/// caused by migration <c>20260421213355_Phase7C2_RemoveDuplicateLocationFromSignupCommitmentTemplates</c>,
/// whose over-greedy regex destroyed the top half of the HTML (banner, greeting,
/// COMMITMENT DETAILS card) in staging.
///
/// Embedded resources (not <c>File.ReadAllText</c>) per MEMORY 6A.129b — disk layout
/// differs between local dev, CI/CD, and Docker containers.
/// </summary>
public static class Phase7C2RecoveryTemplates
{
    private const string ResourceNamespace =
        "LankaConnect.Infrastructure.Data.Migrations.Resources.Phase7C2_Recovery";

    public static string LoadHtml(string templateName)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name must not be empty.", nameof(templateName));

        var resourceName = $"{ResourceNamespace}.{templateName}.html";
        var assembly = typeof(Phase7C2RecoveryTemplates).Assembly;

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
