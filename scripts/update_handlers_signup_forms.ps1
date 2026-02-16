# Phase 6A.112: Batch update 7 email handlers with signup forms logic

$handlers = @(
    "AnonymousRegistrationConfirmedEventHandler.cs",
    "AttendeesAddedEventHandler.cs",
    "EventPublishedEventHandler.cs",
    "PaymentCompletedEventHandler.cs",
    "RefundCompletedEventHandler.cs",
    "RefundRequestedEventHandler.cs",
    "RegistrationCancelledEventHandler.cs"
)

$basePath = "c:\Work\LankaConnect\src\LankaConnect.Application\Events\EventHandlers"

foreach ($handler in $handlers) {
    $filePath = Join-Path $basePath $handler
    Write-Host "Processing: $handler" -ForegroundColor Cyan

    if (!(Test-Path $filePath)) {
        Write-Host "  SKIP: File not found" -ForegroundColor Yellow
        continue
    }

    $content = Get-Content $filePath -Raw

    # Check if already updated
    if ($content -match "Phase 6A.112") {
        Write-Host "  SKIP: Already has Phase 6A.112 logic" -ForegroundColor Green
        continue
    }

    # Pattern 1: Add using statement for IEventFormRepository
    if ($content -notmatch "using LankaConnect.Domain.Events.Repositories;") {
        $pattern1 = '(using LankaConnect\.Domain\.Events\.Enums;)'
        $replacement1 = '$1' + "`r`nusing LankaConnect.Domain.Events.Repositories;"
        $content = $content -replace $pattern1, $replacement1
    }

    # Pattern 2: Add IEventFormRepository to fields
    $pattern2 = '(private readonly IRegistrationRepository _registrationRepository;)'
    $replacement2 = '$1' + "`r`n    private readonly IEventFormRepository _eventFormRepository;"
    $content = $content -replace $pattern2, $replacement2

    # Pattern 3: Add to constructor parameter list
    $pattern3 = '(IRegistrationRepository registrationRepository,)'
    $replacement3 = '$1' + "`r`n        IEventFormRepository eventFormRepository,"
    $content = $content -replace $pattern3, $replacement3

    # Pattern 4: Add to constructor assignment
    $pattern4 = '(_registrationRepository = registrationRepository;)'
    $replacement4 = '$1' + "`r`n        _eventFormRepository = eventFormRepository;"
    $content = $content -replace $pattern4, $replacement4

    # Pattern 5: Add signup forms check after WithSignUpListsUrl()
    $pattern5 = '(\s+)(typedParams\.WithSignUpListsUrl\([^\)]+\);)\s+(\})\s+(//.*Set )'
    $replacement5 = '$1$2' + "`r`n$1}`r`n`r`n$1// Phase 6A.112: Check if event has active signup forms`r`n$1var forms = await _eventFormRepository.GetByEventIdAsync(@event.Id, cancellationToken);`r`n$1var hasActiveForms = forms.Any(f => f.Status == EventFormStatus.Active);`r`n`r`n$1if (hasActiveForms)`r`n$1{`r`n$1    typedParams.WithSignupForms(`$`"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#signup-forms`");`r`n$1}`r`n`r`n$1$4"
    $content = $content -replace $pattern5, $replacement5

    # Write back
    Set-Content -Path $filePath -Value $content -NoNewline
    Write-Host "  SUCCESS: Updated $handler" -ForegroundColor Green
}

Write-Host "`nDone! Updated email handlers." -ForegroundColor Green
Write-Host "Next: Build solution to verify changes" -ForegroundColor Yellow
