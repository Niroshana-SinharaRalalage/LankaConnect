<#
.SYNOPSIS
  LankaConnect Event-aggregate fixture builders for Wave 9 API Smoke Suite.

.DESCRIPTION
  Creates + cleans up Event-family fixtures (events, registrations) with smoke-run
  tagging so artifacts can be cleaned up after a run and never pollute staging
  long-term.

  Hard rule (architect Q2): every New-Lc* function MUST tag. Untagged smoke artifacts
  become cross-run contamination.

  Tag pattern:
    [SMOKE-9a-<UTC-yyyyMMddTHHmmss>-<random4>] in title field

  Exposes:
   - New-LcSmokeTag — generates a tag for this run
   - New-LcFreeEvent / New-LcPaidEvent — create event with smoke tag
   - Publish-LcEvent — publish an event (publish flow required before RSVP)
   - New-LcRegistration — RSVP to an event
   - Remove-LcFixturesByTag — bulk cleanup at end of run

.NOTES
  Wave 9.a Foundation module (architect-ruled 2026-06-29 Q1, split from Lc-Fixtures
  to keep per-aggregate fixtures separate from cross-controller ones in Lc-CommonFixtures).
#>

# Current run tag — set once per smoke run, reused by all New-Lc* calls
$script:LcCurrentRunTag = $null

function New-LcSmokeTag {
    <#
    .SYNOPSIS
      Generates a smoke-run tag and stores it as the current tag for subsequent fixture calls.
      Pattern: [SMOKE-<Prefix>-<UTC-yyyyMMddTHHmmss>-<random4>]

    .PARAMETER Prefix
      Short identifier for the smoke run (e.g. '9a' for Wave 9.a).

    .EXAMPLE
      $tag = New-LcSmokeTag -Prefix '9a'
      # $tag = "[SMOKE-9a-20260629T143005-A7F2]"
    #>
    [CmdletBinding()]
    param([string]$Prefix = '9a')
    $timestamp = [datetime]::UtcNow.ToString('yyyyMMddTHHmmss')
    $random = -join ((48..57 + 65..70) | Get-Random -Count 4 | ForEach-Object { [char]$_ })  # 4 hex chars
    $tag = "[SMOKE-$Prefix-$timestamp-$random]"
    $script:LcCurrentRunTag = $tag
    return $tag
}

function Get-LcCurrentRunTag {
    [CmdletBinding()] param()
    if (-not $script:LcCurrentRunTag) {
        # Default tag if caller forgot to set one
        $script:LcCurrentRunTag = New-LcSmokeTag
    }
    return $script:LcCurrentRunTag
}

function New-LcFreeEvent {
    <#
    .SYNOPSIS
      Creates a free RSVP event tagged for cleanup.

    .PARAMETER Tag
      Override the current-run tag. Default: $script:LcCurrentRunTag (set via New-LcSmokeTag).

    .PARAMETER DaysFromNow
      How many days from now the event starts. Default 14.

    .PARAMETER Capacity
      Event capacity. Default 50.

    .OUTPUTS
      pscustomobject { Success, EventId, Body, Tag, Error }
    #>
    [CmdletBinding()]
    param(
        [string]$Tag = $(Get-LcCurrentRunTag),
        [int]$DaysFromNow = 14,
        [int]$Capacity = 50,
        [string]$TitleSuffix = 'free event'
    )

    $startDt = [datetime]::UtcNow.AddDays($DaysFromNow).Date.AddHours(19)
    $endDt = $startDt.AddHours(2)
    $regDeadline = $startDt.AddHours(-1)

    $body = @{
        title                    = "$Tag $TitleSuffix"
        description              = 'Auto-created by Wave 9 API Smoke Suite. Safe to delete.'
        # Wave 9.h.10.5 F24 fix: field names on CreateEventCommand are `startDate` +
        # `endDate` (DateTime?). Previously we sent `startDateTime`/`endDateTime`
        # which don't match any command property, silently creating TBD events.
        startDate                = $startDt.ToString('yyyy-MM-ddTHH:mm:ssZ')
        endDate                  = $endDt.ToString('yyyy-MM-ddTHH:mm:ssZ')
        location                 = @{
            venueName  = 'Smoke Venue'
            address    = '100 Main St'
            city       = 'Boston'
            stateName  = 'Massachusetts'
            country    = 'USA'
            postalCode = '02110'
            latitude   = 42.36
            longitude  = -71.06
        }
        isFreeEvent              = $true
        capacity                 = $Capacity
        requiresRegistration     = $true
        registrationDeadline     = $regDeadline.ToString('yyyy-MM-ddTHH:mm:ssZ')
        isOnline                 = $false
        category                 = 'Social'
        isPubliclyVisible        = $true
        organizerContacts        = @()
        images                   = @()
        videos                   = @()
        signUpLists              = @()
        ticketTiers              = @()
        emailGroupIds            = @()
    }

    $result = Invoke-LcPost -Path '/api/Events' -Body $body
    if (-not $result.Success) {
        return [pscustomobject]@{
            Success = $false
            EventId = $null
            Body    = $result.Body
            Tag     = $Tag
            Error   = "Create-event failed: HTTP $($result.StatusCode) $($result.Error)"
        }
    }

    # Response body is the raw GUID string for event id (per W5.3.c2 lifecycle smoke discovery)
    $eventId = if ($result.Body -is [string]) { $result.Body.Trim('"') }
               elseif ($result.Body.id) { $result.Body.id }
               elseif ($result.Body.eventId) { $result.Body.eventId }
               else { $null }

    return [pscustomobject]@{
        Success = $true
        EventId = $eventId
        Body    = $result.Body
        Tag     = $Tag
        Error   = $null
    }
}

function New-LcPaidEvent {
    <#
    .SYNOPSIS
      Creates a paid event with ticket tiers tagged for cleanup.

    .OUTPUTS
      pscustomobject { Success, EventId, Body, Tag, Error }
    #>
    [CmdletBinding()]
    param(
        [string]$Tag = $(Get-LcCurrentRunTag),
        [int]$DaysFromNow = 14,
        [int]$Capacity = 100,
        [decimal]$TicketPrice = 25.0,
        [string]$TitleSuffix = 'paid event'
    )

    $startDt = [datetime]::UtcNow.AddDays($DaysFromNow).Date.AddHours(19)
    $endDt = $startDt.AddHours(2)
    $regDeadline = $startDt.AddHours(-1)

    $body = @{
        title                    = "$Tag $TitleSuffix"
        description              = 'Auto-created by Wave 9 API Smoke Suite. Safe to delete.'
        # Wave 9.h.10.5 F24 fix: field names on CreateEventCommand are `startDate` +
        # `endDate` (DateTime?). Previously we sent `startDateTime`/`endDateTime`
        # which don't match any command property, silently creating TBD events.
        startDate                = $startDt.ToString('yyyy-MM-ddTHH:mm:ssZ')
        endDate                  = $endDt.ToString('yyyy-MM-ddTHH:mm:ssZ')
        location                 = @{
            venueName  = 'Smoke Venue'
            address    = '100 Main St'
            city       = 'Boston'
            stateName  = 'Massachusetts'
            country    = 'USA'
            postalCode = '02110'
            latitude   = 42.36
            longitude  = -71.06
        }
        isFreeEvent              = $false
        ticketPriceAmount        = $TicketPrice
        ticketPriceCurrency      = 'USD'
        capacity                 = $Capacity
        requiresRegistration     = $true
        registrationDeadline     = $regDeadline.ToString('yyyy-MM-ddTHH:mm:ssZ')
        isOnline                 = $false
        category                 = 'Cultural'
        isPubliclyVisible        = $true
        organizerContacts        = @()
        images                   = @()
        videos                   = @()
        signUpLists              = @()
        ticketTiers              = @()
        emailGroupIds            = @()
    }

    $result = Invoke-LcPost -Path '/api/Events' -Body $body
    if (-not $result.Success) {
        return [pscustomobject]@{
            Success = $false
            EventId = $null
            Body    = $result.Body
            Tag     = $Tag
            Error   = "Create-paid-event failed: HTTP $($result.StatusCode) $($result.Error)"
        }
    }
    $eventId = if ($result.Body -is [string]) { $result.Body.Trim('"') }
               elseif ($result.Body.id) { $result.Body.id }
               elseif ($result.Body.eventId) { $result.Body.eventId }
               else { $null }
    return [pscustomobject]@{
        Success = $true
        EventId = $eventId
        Body    = $result.Body
        Tag     = $Tag
        Error   = $null
    }
}

function Publish-LcEvent {
    <#
    .SYNOPSIS
      Publishes a draft event. Required before RSVPs are accepted.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$EventId)
    $result = Invoke-LcPost -Path "/api/Events/$EventId/publish" -Body @{}
    return $result
}

function New-LcRegistration {
    <#
    .SYNOPSIS
      RSVPs the authenticated user to a published event. Uses the legacy quantity field
      for free events (per W5.3.c2 smoke discovery — POST /api/Events/{id}/rsvp).

    .PARAMETER EventId
      The event to RSVP to.

    .PARAMETER UserId
      User UUID. Defaults to current logged-in user.

    .PARAMETER Quantity
      Number of attendees (legacy format). Default 1.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$EventId,
        [string]$UserId = $(Get-LcUserId),
        [int]$Quantity = 1
    )
    $body = @{
        userId   = $UserId
        quantity = $Quantity
    }
    $result = Invoke-LcPost -Path "/api/Events/$EventId/rsvp" -Body $body
    return $result
}

function Remove-LcFixturesByTag {
    <#
    .SYNOPSIS
      Cleans up all smoke fixtures matching a tag. Defaults to the current run tag.

      Uses the my-events list endpoint to find tagged events, then DELETEs each.
      In a real production smoke flow this would be more careful; for smoke we accept
      the simple approach (find by title prefix, delete by id).

    .PARAMETER Tag
      Cleanup target. Default: current run tag.

    .PARAMETER DryRun
      If $true, only lists what would be deleted without actually deleting.

    .OUTPUTS
      pscustomobject { Found, Deleted, Failed, DryRun }
    #>
    [CmdletBinding()]
    param(
        [string]$Tag = $(Get-LcCurrentRunTag),
        [bool]$DryRun = $false
    )

    if (-not $Tag) {
        throw "Remove-LcFixturesByTag: no tag specified and no current run tag set"
    }

    # List events. The my-events endpoint returns events I organize.
    $listResult = Invoke-LcGet -Path '/api/Events/my-events?pageNumber=1&pageSize=100'
    if (-not $listResult.Success) {
        return [pscustomobject]@{ Found = 0; Deleted = 0; Failed = 0; DryRun = $DryRun; Error = "list failed: HTTP $($listResult.StatusCode)" }
    }

    # The response shape: either {items: [...]} or [...] depending on serialization
    $items = if ($listResult.Body.items) { $listResult.Body.items } else { $listResult.Body }
    $tagged = @($items | Where-Object { $_.title -and $_.title.StartsWith($Tag) })

    $deleted = 0
    $failed = 0
    foreach ($e in $tagged) {
        if ($DryRun) { continue }
        $delResult = Invoke-LcDelete -Path "/api/Events/$($e.id)"
        if ($delResult.Success -or $delResult.StatusCode -eq 204) {
            $deleted++
        } else {
            $failed++
        }
    }

    return [pscustomobject]@{
        Found   = $tagged.Count
        Deleted = $deleted
        Failed  = $failed
        DryRun  = $DryRun
        Tag     = $Tag
        Error   = $null
    }
}

Export-ModuleMember -Function `
    New-LcSmokeTag, Get-LcCurrentRunTag, `
    New-LcFreeEvent, New-LcPaidEvent, Publish-LcEvent, `
    New-LcRegistration, `
    Remove-LcFixturesByTag
