# Phase 6A.112: Batch update 10 EmailParams classes with HasSignupForms/SignupFormsUrl
# This script adds the same pattern we manually added to FreeEventRegistrationEmailParams

$emailParamsFiles = @(
    "AttendeesAddedEmailParams.cs",
    "EventCancellationEmailParams.cs",
    "EventDetailsEmailParams.cs",
    "EventPublishedEmailParams.cs",
    "EventReminderEmailParams.cs",
    "NewsletterEmailParams.cs",
    "RefundEmailParams.cs",
    "RegistrationCancellationEmailParams.cs",
    "SignupCommitmentEmailParams.cs",
    "TicketConfirmationEmailParams.cs"
)

$basePath = "c:\Work\LankaConnect\src\LankaConnect.Shared\Email\Contracts"

foreach ($file in $emailParamsFiles) {
    $filePath = Join-Path $basePath $file
    Write-Host "Processing: $file" -ForegroundColor Cyan

    if (!(Test-Path $filePath)) {
        Write-Host "  SKIP: File not found" -ForegroundColor Yellow
        continue
    }

    $content = Get-Content $filePath -Raw

    # Check if already updated
    if ($content -match "HasSignupForms") {
        Write-Host "  SKIP: Already has HasSignupForms" -ForegroundColor Green
        continue
    }

    # Pattern 1: Add properties after SignUpListsUrl
    $pattern1 = '(public string SignUpListsUrl \{ get; set; \} = string\.Empty;)\s+(#endregion)'
    $replacement1 = '$1

    /// <summary>
    /// Whether the event has signup forms (controls {{#HasSignupForms}} conditional).
    /// Phase 6A.112: Added for "View Signup Forms" button.
    /// </summary>
    public bool HasSignupForms { get; set; } = false;

    /// <summary>
    /// URL to signup forms section of event (if event has signup forms).
    /// Phase 6A.112: Added for "View Signup Forms" button.
    /// </summary>
    public string SignupFormsUrl { get; set; } = string.Empty;

    $2'

    $content = $content -replace $pattern1, $replacement1

    # Pattern 2: Add to ToDictionary (find the class name for method signature)
    $className = $file -replace 'EmailParams\.cs$', 'EmailParams'

    # Add after SignupListUrl line in ToDictionary
    $pattern2 = '(\{\s*"SignupListUrl",\s*SignUpListsUrl\s*\}[^\n]*\n)'
    $replacement2 = '$1            { "HasSignupForms", HasSignupForms },  // Phase 6A.112
            { "SignupFormsUrl", SignupFormsUrl },  // Phase 6A.112
'

    $content = $content -replace $pattern2, $replacement2

    # Pattern 3: Add fluent method before final #endregion }
    $pattern3 = '(public\s+' + $className + '\s+WithSignUpLists\([^}]+\}\s+)'
    $replacement3 = '$1
    /// <summary>
    /// Sets signup forms URL and HasSignupForms flag together.
    /// Phase 6A.112: Added for "View Signup Forms" button.
    /// </summary>
    public ' + $className + ' WithSignupForms(string url)
    {
        HasSignupForms = !string.IsNullOrWhiteSpace(url);
        SignupFormsUrl = url ?? string.Empty;
        return this;
    }

    '

    $content = $content -replace $pattern3, $replacement3

    # Write back
    Set-Content -Path $filePath -Value $content -NoNewline
    Write-Host "  SUCCESS: Updated $file" -ForegroundColor Green
}

Write-Host "`nDone! Updated EmailParams classes." -ForegroundColor Green
Write-Host "Next: Build solution to verify changes" -ForegroundColor Yellow
