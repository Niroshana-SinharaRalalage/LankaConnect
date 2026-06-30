<#
.SYNOPSIS
  Finance + Business fixture builders for Wave 9.h. Each fixture creates a real
  staging artifact tagged with the current run tag so it cascades through
  Remove-LcFixturesByTag (for event-scoped fixtures) or its own cleanup
  (for top-level Business).

.DESCRIPTION
  Per Wave 9.h.1 architect ruling: every destructive smoke endpoint that can be
  tested via real fixture creation MUST be. "Destructive" is not a valid SKIP
  reason. This module wires up: Sponsor (money/item), Donation, Collection,
  AddOn definition, SponsorshipPackage, VenueLayout, PhotoAlbum, Newsletter,
  Business, Badge.

  Event-scoped fixtures cascade-delete with the parent event (Remove-LcFixturesByTag
  already handles this). Top-level Business + Badge get dedicated cleanup helpers.

.NOTES
  Tag prefix '9h' should be set via New-LcSmokeTag -Prefix '9h' at top of smoke run.
#>

# Event-scoped fixtures (cascade-cleaned with parent event)

function Enable-LcEventFinanceConfigs {
    <#
    .SYNOPSIS
      Enables sponsor / donation / collection / add-on subresources on an event so
      downstream mutator smokes can hit them. By default events are created with
      these subresources DISABLED; the create-event handler does not surface flags
      for enabling them.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$EventId)

    $results = @{}

    # Sponsor config -- enable both money + item
    $results.Sponsor = Invoke-LcPut -Path "/api/events/$EventId/sponsor-config" -Body @{
        isEnabled           = $true
        acceptMoneySponsors = $true
        acceptItemSponsors  = $true
        minSponsorAmount    = 5.00
        sponsorMessage      = 'Wave 9.h smoke fixture'
        showSponsorList     = $true
    }

    # Collection config (also used as proxy for donation? probe -- collections + donations are separate)
    $results.Collection = Invoke-LcPut -Path "/api/events/$EventId/collection-config" -Body @{
        isEnabled        = $true
        goalAmount       = 1000.00
        showProgress     = $true
        suggestedAmounts = @(10, 25, 50)
    }

    # Donation config -- NO dedicated endpoint exists. Donations are enabled via the
    # main /api/Events/{id} PUT endpoint which requires the FULL event body.
    # Fetch current event, merge donation fields, send back.
    $currentR = Invoke-LcGet -Path "/api/Events/$EventId"
    if ($currentR.Success) {
        $c = $currentR.Body
        # UpdateEventCommand requires the full event body (Capacity, Location, etc.)
        # Round-trip the GET body and add donation fields
        $body = @{
            eventId                   = $EventId
            title                     = $c.title
            description               = $c.description
            startDateTime             = $c.startDateTime
            endDateTime               = $c.endDateTime
            location                  = $c.location
            isFreeEvent               = $c.isFreeEvent
            capacity                  = if ($c.capacity) { $c.capacity } else { 50 }
            requiresRegistration      = if ($null -ne $c.requiresRegistration) { $c.requiresRegistration } else { $true }
            registrationDeadline      = $c.registrationDeadline
            isOnline                  = if ($null -ne $c.isOnline) { $c.isOnline } else { $false }
            category                  = $c.category
            isPubliclyVisible         = if ($null -ne $c.isPubliclyVisible) { $c.isPubliclyVisible } else { $true }
            organizerContacts         = @()
            images                    = @()
            videos                    = @()
            signUpLists               = @()
            ticketTiers               = @()
            emailGroupIds             = @()
            donationsEnabled          = $true
            donationSuggestedAmounts  = @(5, 10, 20)
            donationAllowCustomAmount = $true
            donationMinAmount         = 1.00
            donationMessage           = 'Wave 9.h smoke fixture'
            showDonationSummary       = $true
        }
        $results.Donation = Invoke-LcPut -Path "/api/Events/$EventId" -Body $body
    } else {
        $results.Donation = @{ Success = $false; StatusCode = 0; Error = 'fetch event failed' }
    }

    # Add-on config
    $results.AddOn = Invoke-LcPut -Path "/api/events/$EventId/add-on-config" -Body @{
        isEnabled                   = $true
        availableDuringRegistration = $true
        availableStandalone         = $true
        addOnMessage                = 'Wave 9.h smoke fixture'
    }

    return [pscustomobject]@{
        EventId         = $EventId
        SponsorEnabled  = ($results.Sponsor.Success -or $results.Sponsor.StatusCode -eq 200)
        CollectionEnabled = ($results.Collection.Success -or $results.Collection.StatusCode -eq 200)
        DonationEnabled = ($results.Donation.Success -or $results.Donation.StatusCode -eq 200)
        AddOnEnabled    = ($results.AddOn.Success -or $results.AddOn.StatusCode -eq 200)
        Raw             = $results
    }
}

function New-LcTaggedMoneySponsor {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$EventId,
        [string]$Tag = $(Get-LcCurrentRunTag),
        [decimal]$Amount = 100.00
    )
    $body = @{
        sponsorName         = "$Tag SmokeSponsor"
        sponsorEmail        = 'smoke-sponsor@lankaconnect.test'
        sponsorPhone        = '+15555550100'
        sponsorOrganization = "$Tag SmokeOrg"
        sponsorNotes        = 'Auto-created by Wave 9.h smoke. Safe to delete.'
        amount              = $Amount
        currency            = 'USD'
        successUrl          = 'https://example.test/success'
        cancelUrl           = 'https://example.test/cancel'
    }
    $r = Invoke-LcPost -Path "/api/events/$EventId/sponsors/money" -Body $body
    return [pscustomobject]@{
        Success = $r.Success
        StatusCode = $r.StatusCode
        Body = $r.Body
        Tag = $Tag
        Error = if ($r.Success) { $null } else { "HTTP $($r.StatusCode): $($r.Error)" }
    }
}

function New-LcTaggedItemSponsor {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$EventId,
        [string]$Tag = $(Get-LcCurrentRunTag)
    )
    $body = @{
        sponsorName         = "$Tag SmokeItemSponsor"
        sponsorEmail        = 'smoke-item-sponsor@lankaconnect.test'
        sponsorOrganization = "$Tag SmokeOrg"
        sponsorNotes        = 'Auto-created by Wave 9.h smoke. Safe to delete.'
        itemName            = 'Smoke Gift Bag'
        itemDescription     = 'Smoke gift bag for Wave 9.h.3'
        estimatedValue      = 50.00
    }
    $r = Invoke-LcPost -Path "/api/events/$EventId/sponsors/item" -Body $body
    return [pscustomobject]@{
        Success = $r.Success
        StatusCode = $r.StatusCode
        Body = $r.Body
        Tag = $Tag
        Error = if ($r.Success) { $null } else { "HTTP $($r.StatusCode): $($r.Error)" }
    }
}

function New-LcTaggedDonation {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$EventId,
        [string]$Tag = $(Get-LcCurrentRunTag),
        [decimal]$Amount = 25.00
    )
    $body = @{
        donorName  = "$Tag SmokeDonor"
        donorEmail = 'smoke-donor@lankaconnect.test'
        donorPhone = '+15555550101'
        donorNotes = 'Auto-created by Wave 9.h smoke.'
        amount     = $Amount
        currency   = 'USD'
        successUrl = 'https://example.test/success'
        cancelUrl  = 'https://example.test/cancel'
    }
    $r = Invoke-LcPost -Path "/api/events/$EventId/donations" -Body $body
    return [pscustomobject]@{
        Success = $r.Success
        StatusCode = $r.StatusCode
        Body = $r.Body
        Tag = $Tag
        Error = if ($r.Success) { $null } else { "HTTP $($r.StatusCode): $($r.Error)" }
    }
}

function New-LcTaggedCollection {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$EventId,
        [string]$Tag = $(Get-LcCurrentRunTag),
        [decimal]$Amount = 15.00
    )
    $body = @{
        contributorName  = "$Tag SmokeContrib"
        contributorEmail = 'smoke-contrib@lankaconnect.test'
        contributorPhone = '+15555550102'
        contributorNotes = 'Auto-created by Wave 9.h smoke.'
        amount           = $Amount
        currency         = 'USD'
        successUrl       = 'https://example.test/success'
        cancelUrl        = 'https://example.test/cancel'
    }
    $r = Invoke-LcPost -Path "/api/events/$EventId/collections" -Body $body
    return [pscustomobject]@{
        Success = $r.Success
        StatusCode = $r.StatusCode
        Body = $r.Body
        Tag = $Tag
        Error = if ($r.Success) { $null } else { "HTTP $($r.StatusCode): $($r.Error)" }
    }
}

function New-LcTaggedAddOnDefinition {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$EventId,
        [string]$Tag = $(Get-LcCurrentRunTag),
        [decimal]$Price = 10.00
    )
    $body = @{
        name          = "$Tag SmokeAddOn"
        description   = 'Auto-created by Wave 9.h smoke. Safe to delete.'
        price         = $Price
        currency      = 'USD'
        quantityLimit = 100
        sortOrder     = 0
    }
    $r = Invoke-LcPost -Path "/api/events/$EventId/add-ons" -Body $body
    return [pscustomobject]@{
        Success = $r.Success
        StatusCode = $r.StatusCode
        Body = $r.Body
        Tag = $Tag
        Error = if ($r.Success) { $null } else { "HTTP $($r.StatusCode): $($r.Error)" }
    }
}

function New-LcTaggedSponsorshipPackage {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$EventId,
        [string]$Tag = $(Get-LcCurrentRunTag),
        [decimal]$Price = 500.00
    )
    $body = @{
        name        = "$Tag SmokePackage"
        description = 'Auto-created by Wave 9.h smoke.'
        price       = $Price
        currency    = 'USD'
        perks       = @('Logo display', 'Social mention')
        quantity    = 5
    }
    $r = Invoke-LcPost -Path "/api/events/$EventId/sponsorship-packages" -Body $body
    return [pscustomobject]@{
        Success = $r.Success
        StatusCode = $r.StatusCode
        Body = $r.Body
        Tag = $Tag
        Error = if ($r.Success) { $null } else { "HTTP $($r.StatusCode): $($r.Error)" }
    }
}

function New-LcTaggedVenueLayout {
    <#
    .SYNOPSIS
      Creates a venue layout (event-scoped or template) tagged for cleanup. Wave 9.h.7.
    .PARAMETER EventId
      Optional event GUID. If $null, creates a TEMPLATE layout.
    #>
    [CmdletBinding()]
    param(
        [string]$EventId = $null,
        [string]$Tag = $(Get-LcCurrentRunTag),
        [bool]$IsTemplate = $false
    )
    $body = @{
        name             = "$Tag SmokeLayout"
        layoutType       = 'Banquet'
        eventId          = if ($EventId) { $EventId } else { $null }
        isTemplate       = $IsTemplate
        zones            = @(
            @{
                name         = 'Main'
                color        = '#FF5500'
                ticketTierId = $null
                sortOrder    = 0
            }
        )
    }
    $r = Invoke-LcPost -Path '/api/venue-layouts' -Body $body
    $layoutId = if ($r.Body.id) { $r.Body.id }
                elseif ($r.Body -is [string]) { $r.Body.Trim('"') }
                else { $null }
    return [pscustomobject]@{
        Success = $r.Success
        StatusCode = $r.StatusCode
        Body = $r.Body
        LayoutId = $layoutId
        ZoneId = $(if ($r.Body.zones -and $r.Body.zones.Count -gt 0) { $r.Body.zones[0].id } else { $null })
        Tag = $Tag
        Error = if ($r.Success) { $null } else { "HTTP $($r.StatusCode): $($r.Error)" }
    }
}

function Remove-LcVenueLayoutById {
    [CmdletBinding()] param([Parameter(Mandatory)][string]$LayoutId)
    Invoke-LcDelete -Path "/api/venue-layouts/$LayoutId" | Out-Null
}

function New-LcTaggedPhotoAlbum {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$EventId,
        [string]$Tag = $(Get-LcCurrentRunTag)
    )
    $body = @{
        name        = "$Tag SmokeAlbum"
        description = 'Auto-created by Wave 9.h smoke.'
        coverImageUrl = $null
    }
    $r = Invoke-LcPost -Path "/api/events/$EventId/albums" -Body $body
    return [pscustomobject]@{
        Success = $r.Success
        StatusCode = $r.StatusCode
        Body = $r.Body
        Tag = $Tag
        Error = if ($r.Success) { $null } else { "HTTP $($r.StatusCode): $($r.Error)" }
    }
}

function New-LcTaggedNewsletter {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$EventId,
        [string]$Tag = $(Get-LcCurrentRunTag)
    )
    $body = @{
        eventId        = $EventId
        subject        = "$Tag SmokeNewsletter"
        bodyHtml       = "<p>$Tag Auto-created. Safe to delete.</p>"
        targetSegment  = 'EventRegistrants'
    }
    $r = Invoke-LcPost -Path '/api/Newsletters' -Body $body
    return [pscustomobject]@{
        Success = $r.Success
        StatusCode = $r.StatusCode
        Body = $r.Body
        Tag = $Tag
        Error = if ($r.Success) { $null } else { "HTTP $($r.StatusCode): $($r.Error)" }
    }
}

# Top-level fixtures (NOT cascade-cleaned; need own teardown)

function New-LcTaggedBusiness {
    [CmdletBinding()]
    param(
        [string]$Tag = $(Get-LcCurrentRunTag),
        [string]$OwnerId = $(Get-LcUserId)
    )
    $body = @{
        name          = "$Tag SmokeBusiness"
        description   = 'Auto-created by Wave 9.h smoke. Safe to delete.'
        contactPhone  = '+15555550200'
        contactEmail  = 'smoke-business@lankaconnect.test'
        website       = 'https://example.test'
        address       = '200 Main St'
        city          = 'Boston'
        province      = 'Massachusetts'
        postalCode    = '02110'
        latitude      = 42.36
        longitude     = -71.06
        category      = 'Restaurant'
        ownerId       = $OwnerId
        categories    = @('Restaurant')
        tags          = @('smoke', '9h')
    }
    $r = Invoke-LcPost -Path '/api/Businesses' -Body $body
    return [pscustomobject]@{
        Success = $r.Success
        StatusCode = $r.StatusCode
        Body = $r.Body
        Tag = $Tag
        Error = if ($r.Success) { $null } else { "HTTP $($r.StatusCode): $($r.Error)" }
    }
}

function Remove-LcBusinessesByTag {
    [CmdletBinding()]
    param([string]$Tag = $(Get-LcCurrentRunTag))
    if (-not $Tag) { throw 'Remove-LcBusinessesByTag: no tag' }

    # Search for tagged businesses
    $r = Invoke-LcGet -Path "/api/Businesses/search?query=$([uri]::EscapeDataString($Tag))"
    if (-not $r.Success) {
        return [pscustomobject]@{ Found = 0; Deleted = 0; Failed = 0; Tag = $Tag; Error = "search failed: HTTP $($r.StatusCode)" }
    }
    $items = if ($r.Body.items) { $r.Body.items } else { $r.Body }
    $tagged = @($items | Where-Object { $_.name -and $_.name.StartsWith($Tag) })

    $deleted = 0
    $failed = 0
    foreach ($b in $tagged) {
        $d = Invoke-LcDelete -Path "/api/Businesses/$($b.id)"
        if ($d.Success -or $d.StatusCode -eq 204) { $deleted++ } else { $failed++ }
    }
    return [pscustomobject]@{ Found = $tagged.Count; Deleted = $deleted; Failed = $failed; Tag = $Tag; Error = $null }
}

Export-ModuleMember -Function `
    Enable-LcEventFinanceConfigs, `
    New-LcTaggedMoneySponsor, New-LcTaggedItemSponsor, `
    New-LcTaggedDonation, New-LcTaggedCollection, `
    New-LcTaggedAddOnDefinition, New-LcTaggedSponsorshipPackage, `
    New-LcTaggedPhotoAlbum, New-LcTaggedNewsletter, `
    New-LcTaggedBusiness, Remove-LcBusinessesByTag, `
    New-LcTaggedVenueLayout, Remove-LcVenueLayoutById
