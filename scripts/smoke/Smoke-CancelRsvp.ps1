<#
.SYNOPSIS
    Wave5.3d.2 S2 smoke — full cancel-RSVP round-trip exercising
    IFormCommands.DeleteResponsesByEventAndUserAsync path end-to-end.

.DESCRIPTION
    Fixture-orchestrated smoke per CLAUDE.md §13.2 S2 (mutator round-trip).
    Wave 5.3d.2 changed the cancel-RSVP form-response cleanup from
    inline IFormResponseRepository loops to a single IFormCommands call,
    and FormCommands.DeleteResponsesByEventAndUserAsync now raises
    FormResponseDeletedEvent per response before delete. This smoke
    proves the route still wires up end-to-end on staging by:

      1. login as the test organizer
      2. POST a fresh form (1 short-text question) on a fixture event
      3. POST the test organizer's RSVP to that event
      4. POST a form response as the organizer
      5. DELETE /api/Events/{eventId}/rsvp?deleteFormResponses=true
      6. assert .formResponsesDeletedCount is the actual number created
      7. cleanup: best-effort form delete (the registration is already gone)

    A non-zero FormResponsesDeletedCount with HTTP 200 confirms the
    IFormCommands route round-trips. The unit test fixture
    FormCommandsTests.RaisesDeletedEventBeforeDelete pins the
    domain-event semantics independently.

.PARAMETER EventId
    Override the fixture event (organizer-owned, Published). Defaults
    to the Smoke-FixtureOrchestrated event.

.PARAMETER StagingUrl
    Defaults to env $env:LC_STAGING_URL or the staging Container App URL.

.NOTES
    Built 2026-06-12 as part of Wave 5.3d.2 S2 verification.
    Requires the test user to be allowed to RSVP to their own event
    (which the existing API permits — CancelRsvp does not gate on
    organizer status).
#>
[CmdletBinding()]
param(
    # Default = "Phase 6 Test - Free Event" — organizer-owned, Published,
    # start 2026-06-20 (future at time of write). RSVPs allowed.
    # Override to point at any future Published event the test user organizes.
    [string]$EventId    = '6d202a73-fa55-46e6-b966-e4409b8e6342',
    [string]$StagingUrl = $(if ($env:LC_STAGING_URL) { $env:LC_STAGING_URL } else { 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io' })
)

$ErrorActionPreference = 'Stop'
$env:LC_STAGING_URL = $StagingUrl

. (Join-Path $PSScriptRoot 'Invoke-Login.ps1') | Out-Host

if (-not $env:LC_BEARER) {
    Write-Error 'Smoke-CancelRsvp requires Invoke-Login.ps1 to succeed.'
    exit 1
}

$headers = @{ Authorization = "Bearer $env:LC_BEARER" }
$formId = $null
$rsvpCreated = $false

function Invoke-Api {
    param([string]$Method, [string]$Path, [object]$Body, [int[]]$AcceptedStatusCodes = @(200, 201))
    $uri = "$StagingUrl$Path"
    try {
        if ($Body) {
            $json = $Body | ConvertTo-Json -Depth 10 -Compress
            return Invoke-RestMethod -Method $Method -Uri $uri -Headers $headers -Body $json -ContentType 'application/json' -TimeoutSec 60
        }
        return Invoke-RestMethod -Method $Method -Uri $uri -Headers $headers -TimeoutSec 60
    }
    catch {
        $resp = $_.Exception.Response
        $code = if ($resp) { [int]$resp.StatusCode } else { -1 }
        if ($AcceptedStatusCodes -contains $code) {
            return $null
        }
        $body = ''
        if ($resp) {
            try {
                $stream = $resp.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($stream)
                $body = $reader.ReadToEnd()
            } catch {}
        }
        throw "API $Method $Path failed: HTTP $code  $body"
    }
}

try {
    # ----- 1. Create a fresh form on the fixture event -----
    Write-Host "[1/5] Creating fresh form on event $EventId..." -ForegroundColor Cyan
    $formCreateBody = @{
        title                        = "5.3d.2 smoke form $(Get-Random)"
        description                  = 'Wave5.3d.2 cancel-RSVP smoke ephemeral form.'
        allowMultipleResponses       = $false
        allowAttendeesToViewResponses = $false
        questions = @(@{
            questionText = 'Smoke question - any text'
            questionType = 'ShortText'
            isRequired   = $true
            sortOrder    = 0
            helpText     = $null
            options      = $null
        })
    }
    $formCreate = Invoke-Api -Method POST -Path "/api/Events/$EventId/forms" -Body $formCreateBody
    # POST /forms returns the new form id either as a bare guid (Result<Guid>) or wrapped.
    $formId = if ($formCreate -is [string]) { $formCreate } elseif ($formCreate.value) { $formCreate.value } else { $formCreate }
    if (-not $formId) { throw "Form create returned no id: $($formCreate | ConvertTo-Json -Compress)" }
    Write-Host "  formId=$formId"

    # ----- 1b. Publish the form so it accepts responses -----
    Write-Host "[2/5] Publishing form..." -ForegroundColor Cyan
    Invoke-Api -Method POST -Path "/api/Events/$EventId/forms/$formId/publish" -Body @{} | Out-Null

    # ----- 1c. Re-fetch form detail to discover questionId -----
    $formDetail = Invoke-Api -Method GET -Path "/api/Events/$EventId/forms/$formId"
    $questionId = ($formDetail.questions | Select-Object -First 1).id
    if (-not $questionId) { throw "Form detail returned no question id" }

    # ----- 2. Create an RSVP for the test organizer -----
    Write-Host "[3/5] Creating RSVP..." -ForegroundColor Cyan
    $rsvpBody = @{ }
    try {
        Invoke-Api -Method POST -Path "/api/Events/$EventId/rsvp" -Body $rsvpBody -AcceptedStatusCodes @(200, 201, 204, 409) | Out-Null
        $rsvpCreated = $true
    }
    catch {
        # 409 (already RSVP'd) is fine — we'll cancel whatever exists.
        if ($_.Exception.Message -match '409') {
            Write-Host "  RSVP already exists; proceeding."
            $rsvpCreated = $true
        }
        else { throw }
    }

    # ----- 3. Submit a form response -----
    Write-Host "[4/5] Submitting form response..." -ForegroundColor Cyan
    $responseBody = @{
        respondentEmail = $env:LC_USER_EMAIL
        respondentName  = 'Wave5.3d.2 Smoke User'
        answers         = @(@{
            questionId = $questionId
            textValue  = 'smoke-token-' + (Get-Random)
        })
    }
    Invoke-Api -Method POST -Path "/api/Events/$EventId/forms/$formId/responses" -Body $responseBody | Out-Null

    # ----- 4. Cancel RSVP with deleteFormResponses=true -----
    Write-Host "[5/5] Cancelling RSVP with deleteFormResponses=true..." -ForegroundColor Cyan
    $cancelUri = "/api/Events/$EventId/rsvp?deleteFormResponses=true"
    $cancel = Invoke-Api -Method DELETE -Path $cancelUri
    $deletedCount = $cancel.formResponsesDeletedCount
    Write-Host "  CancelRsvp: registrationCancelled=$($cancel.registrationCancelled)  formResponsesDeletedCount=$deletedCount"

    # ----- 5. Assert deletion happened -----
    if ($null -eq $deletedCount -or [int]$deletedCount -lt 1) {
        throw "ASSERTION FAILED: expected formResponsesDeletedCount >= 1, got '$deletedCount' (IFormCommands route did not delete the response)"
    }

    Write-Host ""
    Write-Host "Smoke OK 5.3d.2 cancel-RSVP route deleted $deletedCount form response(s) via IFormCommands" -ForegroundColor Green
    exit 0
}
catch {
    Write-Host ""
    Write-Host "Smoke-CancelRsvp FAILED: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
finally {
    # Best-effort cleanup — delete the ephemeral form if we created one
    if ($formId) {
        try {
            Invoke-Api -Method DELETE -Path "/api/Events/$EventId/forms/$formId" -AcceptedStatusCodes @(200, 204, 404) | Out-Null
        }
        catch {
            Write-Host "  (cleanup) form delete failed (ignorable): $($_.Exception.Message)" -ForegroundColor DarkYellow
        }
    }
}
