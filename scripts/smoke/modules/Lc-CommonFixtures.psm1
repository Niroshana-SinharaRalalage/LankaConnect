<#
.SYNOPSIS
  LankaConnect cross-controller fixture builders for Wave 9 API Smoke Suite.

.DESCRIPTION
  Fixture builders for non-Event aggregates (Newsletter, Sponsor, User, etc.).
  Separate from Lc-EventFixtures so the Events module isn't a fat module that
  every per-controller smoke imports.

  Architect-ruled (Q1): "when Wave 9.b ships Smoke-NewsletterController.ps1, it
  shouldn't import a fat Events-shaped fixtures module."

  Tag rule (architect Q2): every New-Lc* MUST tag. Reuses the current run tag
  managed by Lc-EventFixtures' $script:LcCurrentRunTag.

.NOTES
  Wave 9.a Foundation module. As more aggregates are smoked (Wave 9.b through 9.f),
  if any aggregate's fixtures grow large enough, split into its own module
  (e.g. Lc-PaymentsFixtures.psm1) following the same pattern.

  Wave 9.h.10.2 (2026-07-01): Get-LcFixtureEmail added. All smoke email
  recipients MUST route through founder's inbox via Gmail `+` aliases so the
  60+ platform templates actually deliver during smoke runs (previous
  @lankaconnect.test recipients caused silent-drop, hiding the Wave 9.h.9
  coverage gap founder flagged).
#>

function New-LcNewsletter {
    <#
    .SYNOPSIS
      Creates a newsletter draft tagged for cleanup. Suitable for read-flow smokes;
      mutator smokes can publish + cancel.

    .PARAMETER Tag
      Cleanup tag. Defaults to Lc-EventFixtures' current run tag if available.

    .PARAMETER TargetAllLocations
      Whether the newsletter targets all metro areas (the canonical setting that
      exercised IMetroAreaRepository in W5.3 smokes).

    .OUTPUTS
      pscustomobject { Success, NewsletterId, Body, Tag, Error }
    #>
    [CmdletBinding()]
    param(
        [string]$Tag = $(Get-LcCurrentRunTag),
        [bool]$TargetAllLocations = $true,
        [string]$TitleSuffix = 'newsletter'
    )

    $body = @{
        title                        = "$Tag $TitleSuffix"
        description                  = 'Auto-created by Wave 9 API Smoke Suite. Safe to delete.'
        emailGroupIds                = @()
        metroAreaIds                 = @()
        includeNewsletterSubscribers = $true
        targetAllLocations           = $TargetAllLocations
        isAnnouncementOnly           = $true
    }

    $result = Invoke-LcPost -Path '/api/newsletters' -Body $body
    if (-not $result.Success) {
        return [pscustomobject]@{
            Success      = $false
            NewsletterId = $null
            Body         = $result.Body
            Tag          = $Tag
            Error        = "Create-newsletter failed: HTTP $($result.StatusCode) $($result.Error)"
        }
    }
    # Response: raw GUID string for newsletter id (per W5.3.a1 + W5.3.a2 smoke discovery)
    $newsletterId = if ($result.Body -is [string]) { $result.Body.Trim('"') }
                    elseif ($result.Body.id) { $result.Body.id }
                    else { $null }

    return [pscustomobject]@{
        Success      = $true
        NewsletterId = $newsletterId
        Body         = $result.Body
        Tag          = $Tag
        Error        = $null
    }
}

function Remove-LcNewsletterByTag {
    <#
    .SYNOPSIS
      Cleans up smoke newsletter drafts matching the tag.

    .PARAMETER Tag
      Cleanup target. Defaults to current run tag.

    .PARAMETER DryRun
      If $true, only counts; doesn't delete.
    #>
    [CmdletBinding()]
    param(
        [string]$Tag = $(Get-LcCurrentRunTag),
        [bool]$DryRun = $false
    )

    if (-not $Tag) {
        throw "Remove-LcNewsletterByTag: no tag specified and no current run tag set"
    }

    $listResult = Invoke-LcGet -Path '/api/newsletters/my-newsletters?pageNumber=1&pageSize=100'
    if (-not $listResult.Success) {
        return [pscustomobject]@{ Found = 0; Deleted = 0; Failed = 0; DryRun = $DryRun; Error = "list failed: HTTP $($listResult.StatusCode)" }
    }

    $items = if ($listResult.Body.items) { $listResult.Body.items } else { $listResult.Body }
    $tagged = @($items | Where-Object { $_.title -and $_.title.StartsWith($Tag) })

    $deleted = 0
    $failed = 0
    foreach ($n in $tagged) {
        if ($DryRun) { continue }
        $delResult = Invoke-LcDelete -Path "/api/newsletters/$($n.id)"
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

function Get-LcFixtureEmail {
    <#
    .SYNOPSIS
      Generates a Gmail-alias recipient for smoke email delivery. All
      smoke-triggered emails MUST land in the founder inbox for verification.

    .DESCRIPTION
      Wave 9.h.10.2 recipient discipline: every email fixture routes through
      `niroshanaks+<slug>@gmail.com` (Gmail's + alias is preserved through
      the routing hop; the alias survives to the Inbox where filters key on it).

      Slug convention: use the template name (or a semantic scenario slug) so
      Gmail filters can bucket per template. Non-alphanumeric characters are
      replaced with `-`; consecutive dashes collapse. Result is lowercased.

      CI/other-run override: set $env:LC_SMOKE_INBOX to redirect the base
      address (e.g. `noreply` for CI, `qa` for staging pipeline). Base defaults
      to `niroshanaks`.

    .PARAMETER Slug
      Template-key or scenario slug. Examples:
        'template-free-event-registration-confirmation'
        'sponsor'
        'refund-requested'

    .PARAMETER Suffix
      Optional extra qualifier appended to slug for scenario disambiguation.
      Example: -Slug 'template-organizer-custom-email' -Suffix 'attendee-A'
      → niroshanaks+template-organizer-custom-email-attendee-a@gmail.com

    .OUTPUTS
      String — a Gmail alias recipient.

    .EXAMPLE
      Get-LcFixtureEmail -Slug 'template-newsletter-notification'
      # → niroshanaks+template-newsletter-notification@gmail.com

    .EXAMPLE
      Get-LcFixtureEmail -Slug 'sponsor' -Suffix (Get-LcCurrentRunTag)
      # → niroshanaks+sponsor-smk-20260701-...@gmail.com
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Slug,
        [string]$Suffix
    )

    $inbox = if ($env:LC_SMOKE_INBOX) { $env:LC_SMOKE_INBOX } else { 'niroshanaks' }

    $raw = if ($Suffix) { "$Slug-$Suffix" } else { $Slug }
    $safe = ($raw -replace '[^a-zA-Z0-9]+', '-').ToLowerInvariant().Trim('-')

    return "$inbox+$safe@gmail.com"
}

Export-ModuleMember -Function `
    New-LcNewsletter, `
    Remove-LcNewsletterByTag, `
    Get-LcFixtureEmail
