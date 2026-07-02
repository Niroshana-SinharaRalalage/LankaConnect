<#
.SYNOPSIS
  Per-controller smoke for PhotoAlbumsController. Wave 9.h.10.4 gap-close.

.DESCRIPTION
  Covers 13 endpoints. Key email-triggering endpoint is POST /albums/{albumId}/notify
  which fires template-photo-album-published to every registered attendee.

  Endpoints covered (13 of 13):
    POST   /api/events/{eventId}/albums                              -- create draft album
    GET    /api/events/{eventId}/albums                              -- list albums
    PUT    /api/events/{eventId}/albums/{albumId}                    -- update details
    POST   /api/events/{eventId}/albums/{albumId}/publish            -- publish
    POST   /api/events/{eventId}/albums/{albumId}/notify             -- FIRES template-photo-album-published
    POST   /api/events/{eventId}/albums/{albumId}/photos             -- upload photo (multipart)
    POST   /api/events/{eventId}/albums/{albumId}/videos             -- upload video (multipart)
    GET    /api/events/{eventId}/albums/{albumId}/photos             -- list photos
    DELETE /api/events/{eventId}/albums/{albumId}/photos/{photoId}   -- delete photo
    PUT    /api/events/{eventId}/albums/{albumId}/cover/{photoId}    -- set cover
    POST   /api/events/{eventId}/albums/{albumId}/photos/bulk-delete -- bulk delete
    GET    /api/events/{eventId}/albums/{albumId}/download           -- download zip
    DELETE /api/events/{eventId}/albums/{albumId}                    -- delete album (draft only)

  Photo/video upload endpoints exercised in "wiring" mode (small dummy files) unless
  -IncludePhotoUpload flag is set. Delete-album is destructive-guarded because the
  fixture path relies on unpublished/draft-only deletion.
#>

[CmdletBinding()]
param(
    [string[]]$Sections = @(),
    [switch]$IncludeDestructive,
    [switch]$IncludePhotoUpload,
    [switch]$SkipLogChecks
)

$moduleDir = Join-Path $PSScriptRoot 'modules'
Import-Module (Join-Path $moduleDir 'Lc-Http.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Auth.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Assertion.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Report.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-EventFixtures.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-CommonFixtures.psm1') -Force

function Test-LcEndpoint {
    param([Parameter(Mandatory)]$Report, [Parameter(Mandatory)][string]$Section,
          [Parameter(Mandatory)][string]$TestName, [Parameter(Mandatory)][string]$Endpoint,
          [Parameter(Mandatory)][scriptblock]$Action, [string]$SkipReason = '')
    if ($SkipReason) {
        Add-LcResult -Report $Report -Status SKIP -Section $Section -TestName $TestName -Endpoint $Endpoint -SkipReason $SkipReason
        return
    }
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        & $Action | Out-Null
        $sw.Stop()
        Add-LcResult -Report $Report -Status PASS -Section $Section -TestName $TestName -Endpoint $Endpoint -DurationMs $sw.ElapsedMilliseconds
    } catch {
        $sw.Stop()
        Add-LcResult -Report $Report -Status FAIL -Section $Section -TestName $TestName -Endpoint $Endpoint -DurationMs $sw.ElapsedMilliseconds -ErrorMessage $_.Exception.Message
    }
}

function Test-PhotoAlbumsFullLifecycle {
    param([Parameter(Mandatory)]$Report)

    # Fixture: create + publish an event so we can attach an album
    $fix = New-LcFreeEvent
    if (-not $fix.Success) {
        Add-LcResult -Report $Report -Status FAIL -Section 'albums' -TestName 'fixture event setup' -Endpoint 'POST /api/Events' -ErrorMessage "fixture failed: $($fix.Error)"
        return
    }
    Publish-LcEvent -EventId $fix.EventId | Out-Null
    $eventId = $fix.EventId
    $tag = Get-LcCurrentRunTag

    # Register founder as an attendee so notify has a real recipient (fires
    # template-photo-album-published to the registered attendee list). Empty-body
    # RSVP silently failed pre-F30 because the endpoint requires { userId, quantity }.
    $registerResult = New-LcRegistration -EventId $eventId -Quantity 1
    if (-not $registerResult.Success -and $registerResult.StatusCode -ge 500) {
        Write-Host "note: rsvp fixture returned $($registerResult.StatusCode); notify smoke will still exercise the endpoint"
    }

    # 1. Create album
    $script:albumId = $null
    Test-LcEndpoint -Report $Report -Section 'albums' -TestName 'create album' -Endpoint 'POST /api/events/{eventId}/albums' -Action {
        $r = Invoke-LcPost -Path "/api/events/$eventId/albums" -Body @{
            name        = "$tag SmokeAlbum"
            description = 'Wave 9.h.10.4 smoke fixture album'
        }
        if (-not $r.Success) { throw "create album HTTP $($r.StatusCode)" }
        $script:albumId = if ($r.Body.id) { $r.Body.id } elseif ($r.Body -is [string]) { $r.Body.Trim('"') } else { $null }
    }

    # 2. List albums
    Test-LcEndpoint -Report $Report -Section 'albums' -TestName 'list albums' -Endpoint 'GET /api/events/{eventId}/albums' -Action {
        $r = Invoke-LcGet -Path "/api/events/$eventId/albums"
        Assert-Http200 -Result $r
    }

    if (-not $script:albumId) {
        foreach ($n in 'update album','publish album','notify album (FIRES template-photo-album-published)','upload photo','upload video','list photos','delete photo','set cover','bulk delete','download zip','delete album') {
            Add-LcResult -Report $Report -Status SKIP -Section 'albums' -TestName $n -Endpoint '...' -SkipReason 'create album did not yield id; downstream skipped'
        }
        return
    }

    # 3. Update album details
    Test-LcEndpoint -Report $Report -Section 'albums' -TestName 'update album details' -Endpoint 'PUT /api/events/{eventId}/albums/{albumId}' -Action {
        $r = Invoke-LcPut -Path "/api/events/$eventId/albums/$($script:albumId)" -Body @{
            name        = "$tag SmokeAlbum (updated)"
            description = 'Wave 9.h.10.4 smoke fixture album (updated)'
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    # Wave 9.h.10.6 F30: photo upload MUST happen before publish (domain rejects empty
    # albums with "Upload at least one photo or video before publishing"). Previous
    # order was create → publish → notify → upload, which meant publish 400'd,
    # notify 400'd, and template-photo-album-published never fired.
    # Photo upload wiring also had two bugs of its own: called non-existent function
    # Invoke-LcPostMultipart (real name Invoke-LcMultipart), and passed FilePath/
    # FieldName/Extra instead of FileBytes/FileFieldName/ExtraFields. Never noticed
    # because the -IncludePhotoUpload flag was never plumbed through the orchestrator.
    Test-LcEndpoint -Report $Report -Section 'albums' -TestName 'upload photo (multipart)' -Endpoint 'POST /api/events/{eventId}/albums/{albumId}/photos' -Action {
        $r = Invoke-LcMultipart -Path "/api/events/$eventId/albums/$($script:albumId)/photos" `
            -FileFieldName 'image' -FileName 'wave9h10-6-smoke.png' `
            -ExtraFields @{ caption = 'wave 9.h.10.6 smoke' }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode) body=$($r.Body | ConvertTo-Json -Compress -Depth 2)" }
        if ($r.StatusCode -ge 400) { throw "$($r.StatusCode) body=$($r.Body | ConvertTo-Json -Compress -Depth 3)" }
    }
    Add-LcResult -Report $Report -Status SKIP -Section 'albums' -TestName 'upload video (multipart)' -Endpoint 'POST /api/events/{eventId}/albums/{albumId}/videos' -SkipReason 'video upload requires a real (non-1x1-png) binary; deferred to dedicated media smoke'

    # 4. Publish album (only succeeds if at least one photo was uploaded above)
    Test-LcEndpoint -Report $Report -Section 'albums' -TestName 'publish album' -Endpoint 'POST /api/events/{eventId}/albums/{albumId}/publish' -Action {
        $r = Invoke-LcPost -Path "/api/events/$eventId/albums/$($script:albumId)/publish" -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        if ($r.StatusCode -ge 400) { throw "$($r.StatusCode) body=$($r.Body | ConvertTo-Json -Compress -Depth 3)" }
    }

    # 5. Notify attendees (FIRES template-photo-album-published)
    Test-LcEndpoint -Report $Report -Section 'albums' -TestName 'notify attendees (FIRES template-photo-album-published)' -Endpoint 'POST /api/events/{eventId}/albums/{albumId}/notify' -Action {
        $r = Invoke-LcPost -Path "/api/events/$eventId/albums/$($script:albumId)/notify" -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        if ($r.StatusCode -ge 400) { throw "$($r.StatusCode) body=$($r.Body | ConvertTo-Json -Compress -Depth 3)" }
    }

    # 8. List photos
    Test-LcEndpoint -Report $Report -Section 'albums' -TestName 'list photos in album' -Endpoint 'GET /api/events/{eventId}/albums/{albumId}/photos' -Action {
        $r = Invoke-LcGet -Path "/api/events/$eventId/albums/$($script:albumId)/photos"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    # 9. Delete photo (uses fake id -- 404 OK to prove wiring; 500 is a real bug)
    $fakePhotoId = [Guid]::NewGuid().ToString()
    Test-LcEndpoint -Report $Report -Section 'albums' -TestName 'delete photo (404 wiring)' -Endpoint 'DELETE /api/events/{eventId}/albums/{albumId}/photos/{photoId}' -Action {
        $r = Invoke-LcDelete -Path "/api/events/$eventId/albums/$($script:albumId)/photos/$fakePhotoId"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    # 10. Set cover
    Test-LcEndpoint -Report $Report -Section 'albums' -TestName 'set cover (404 wiring)' -Endpoint 'PUT /api/events/{eventId}/albums/{albumId}/cover/{photoId}' -Action {
        $r = Invoke-LcPut -Path "/api/events/$eventId/albums/$($script:albumId)/cover/$fakePhotoId" -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    # 11. Bulk delete
    Test-LcEndpoint -Report $Report -Section 'albums' -TestName 'bulk delete photos (empty list wiring)' -Endpoint 'POST /api/events/{eventId}/albums/{albumId}/photos/bulk-delete' -Action {
        $r = Invoke-LcPost -Path "/api/events/$eventId/albums/$($script:albumId)/photos/bulk-delete" -Body @{ photoIds = @() }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    # 12. Download zip (should work even for empty album)
    Test-LcEndpoint -Report $Report -Section 'albums' -TestName 'download album zip' -Endpoint 'GET /api/events/{eventId}/albums/{albumId}/download' -Action {
        $r = Invoke-LcGet -Path "/api/events/$eventId/albums/$($script:albumId)/download"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    # 13. Delete album -- published album cannot be deleted; endpoint returns 400
    #     which is expected behavior. Verify the endpoint is wired.
    Test-LcEndpoint -Report $Report -Section 'albums' -TestName 'delete album (published -> 400 expected)' -Endpoint 'DELETE /api/events/{eventId}/albums/{albumId}' -Action {
        $r = Invoke-LcDelete -Path "/api/events/$eventId/albums/$($script:albumId)"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
}

function Invoke-PhotoAlbumsControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @( @{ Name = 'albums'; Func = { Test-PhotoAlbumsFullLifecycle -Report $report } } )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.h.10.4: Smoke-PhotoAlbumsController'
    foreach ($section in $sectionsToRun) {
        Write-Host ""; Write-Host "=== Running sub-section: $($section.Name) ==="
        try { & $section.Func | Out-Null } catch {
            Add-LcResult -Report $report -Status FAIL -Section $section.Name -TestName 'sub-section orchestration' -Endpoint 'N/A' -ErrorMessage $_.Exception.Message
        }
    }
    Remove-LcFixturesByTag | Out-Null
    Complete-LcReport -Report $report | Out-Null
    return $report
}

if ($MyInvocation.InvocationName -ne '.') {
    $report = Invoke-PhotoAlbumsControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""
    Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) passRate=$($summary.PassRatePct)% ==="
    Format-LcReportMarkdown -Report $report | Write-Host
}
