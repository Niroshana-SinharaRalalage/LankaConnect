<#
.SYNOPSIS
  LankaConnect smoke assertion module. Foundation for Wave 9 API Smoke Suite.

.DESCRIPTION
  Standard assertion helpers for HTTP responses, JSON shape, mutator round-trips,
  and the architect-mandated dispatch-log-line assertion (catches Wave3-followup.B-class
  silent regressions before the founder notices missing emails).

  Exposes:
   - Assert-Http200 / Assert-Http201 / Assert-Http204 / Assert-Http403 / Assert-Http404
   - Assert-JsonField / Assert-JsonPath
   - Assert-CountIncremented (pre-action count -> action -> post-action count = pre+N)
   - Assert-AuditFieldsFresh (createdAt <= 60s old, updatedAt == createdAt for fresh POSTs)
   - Assert-AuditFieldsUpdated (updatedAt > createdAt after PATCH)
   - Assert-LogSilence (delegates to existing scripts/smoke/Smoke-LogSilence.ps1)
   - Assert-DomainEventDispatched (NEW per architect Q4 — log-tail check for dispatch line)

  All assertion functions THROW on failure (so the orchestrator's try/catch can capture).
  Return $true on success for fluent chaining.

.NOTES
  Wave 9.a Foundation module (architect-ruled 2026-06-29 Q4).
  Canonical S2 mutator pattern: POST -> re-fetch -> Assert-CountIncremented + Assert-DomainEventDispatched + Assert-LogSilence.
#>

class LcAssertionFailure : System.Exception {
    [string]$AssertionName
    [string]$Expected
    [string]$Actual
    [string]$Context

    LcAssertionFailure([string]$name, [string]$expected, [string]$actual, [string]$context)
        : base("Assertion '$name' failed: expected=$expected actual=$actual context=$context") {
        $this.AssertionName = $name
        $this.Expected = $expected
        $this.Actual = $actual
        $this.Context = $context
    }
}

function Assert-HttpStatus {
    <#
    .SYNOPSIS
      Generic HTTP status assertion. Most callers use the convenience wrappers below.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]$Result,
        [Parameter(Mandatory)][int]$ExpectedStatusCode,
        [string]$Context = ''
    )

    if ($null -eq $Result) {
        throw [LcAssertionFailure]::new('Assert-HttpStatus', $ExpectedStatusCode.ToString(), '$null', "$Context (result was null)")
    }
    if ($Result.StatusCode -ne $ExpectedStatusCode) {
        $bodyPreview = if ($Result.Body) { (ConvertTo-Json $Result.Body -Depth 3 -Compress) -replace '(.{200}).*', '$1...' } else { '<empty>' }
        throw [LcAssertionFailure]::new(
            'Assert-HttpStatus',
            $ExpectedStatusCode.ToString(),
            $Result.StatusCode.ToString(),
            "$Context url=$($Result.Method) $($Result.Url) bodyPreview=$bodyPreview err=$($Result.Error)")
    }
    return $true
}

function Assert-Http200 { param([Parameter(Mandatory)]$Result, [string]$Context = '') Assert-HttpStatus -Result $Result -ExpectedStatusCode 200 -Context $Context }
function Assert-Http201 { param([Parameter(Mandatory)]$Result, [string]$Context = '') Assert-HttpStatus -Result $Result -ExpectedStatusCode 201 -Context $Context }
function Assert-Http204 { param([Parameter(Mandatory)]$Result, [string]$Context = '') Assert-HttpStatus -Result $Result -ExpectedStatusCode 204 -Context $Context }
function Assert-Http400 { param([Parameter(Mandatory)]$Result, [string]$Context = '') Assert-HttpStatus -Result $Result -ExpectedStatusCode 400 -Context $Context }
function Assert-Http403 { param([Parameter(Mandatory)]$Result, [string]$Context = '') Assert-HttpStatus -Result $Result -ExpectedStatusCode 403 -Context $Context }
function Assert-Http404 { param([Parameter(Mandatory)]$Result, [string]$Context = '') Assert-HttpStatus -Result $Result -ExpectedStatusCode 404 -Context $Context }

function Assert-JsonField {
    <#
    .SYNOPSIS
      Asserts a top-level JSON field equals an expected value.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]$Result,
        [Parameter(Mandatory)][string]$FieldName,
        [Parameter(Mandatory)]$ExpectedValue,
        [string]$Context = ''
    )
    if (-not $Result.Body) {
        throw [LcAssertionFailure]::new('Assert-JsonField', "field $FieldName=$ExpectedValue", 'body=null', $Context)
    }
    $actual = $Result.Body.$FieldName
    if ($actual -ne $ExpectedValue) {
        throw [LcAssertionFailure]::new('Assert-JsonField', "$FieldName=$ExpectedValue", "$FieldName=$actual", $Context)
    }
    return $true
}

function Assert-JsonPath {
    <#
    .SYNOPSIS
      Asserts a JSON path (dot-separated) resolves to an expected value.
      e.g. Assert-JsonPath -Result $r -Path 'user.email' -ExpectedValue 'foo@bar.com'
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]$Result,
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)]$ExpectedValue,
        [string]$Context = ''
    )
    $parts = $Path -split '\.'
    $current = $Result.Body
    foreach ($p in $parts) {
        if ($null -eq $current) {
            throw [LcAssertionFailure]::new('Assert-JsonPath', "$Path=$ExpectedValue", 'null along path', $Context)
        }
        $current = $current.$p
    }
    if ($current -ne $ExpectedValue) {
        throw [LcAssertionFailure]::new('Assert-JsonPath', "$Path=$ExpectedValue", "$Path=$current", $Context)
    }
    return $true
}

function Get-LcJsonPath {
    # Helper for tests + assertion authors: extract a value at a JSON path. Returns $null if any segment missing.
    [CmdletBinding()]
    param([Parameter(Mandatory)]$Object, [Parameter(Mandatory)][string]$Path)
    $parts = $Path -split '\.'
    $current = $Object
    foreach ($p in $parts) {
        if ($null -eq $current) { return $null }
        $current = $current.$p
    }
    return $current
}

function Assert-CountIncremented {
    <#
    .SYNOPSIS
      Canonical S2 mutator-pattern assertion. The architect-mandated pattern:
        PreCount = property on event before action
        Action runs (POST/PATCH)
        PostCount = property on event after action
        Assert: PostCount == PreCount + ExpectedDelta

    .EXAMPLE
      Assert-CountIncremented -Pre $preGet -Post $postGet -Path 'currentRegistrations' -Delta 1
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]$Pre,
        [Parameter(Mandatory)]$Post,
        [Parameter(Mandatory)][string]$Path,
        [int]$Delta = 1,
        [string]$Context = ''
    )

    $preVal  = if ($Pre.Body)  { Get-LcJsonPath -Object $Pre.Body  -Path $Path } else { $null }
    $postVal = if ($Post.Body) { Get-LcJsonPath -Object $Post.Body -Path $Path } else { $null }

    if ($null -eq $preVal -or $null -eq $postVal) {
        throw [LcAssertionFailure]::new('Assert-CountIncremented', "pre+$Delta", "pre=$preVal post=$postVal", $Context)
    }

    $expected = [int]$preVal + $Delta
    if ([int]$postVal -ne $expected) {
        throw [LcAssertionFailure]::new('Assert-CountIncremented', "$Path = $expected (pre[$preVal] + $Delta)", "$Path = $postVal", $Context)
    }
    return $true
}

function Assert-AuditFieldsFresh {
    <#
    .SYNOPSIS
      Asserts a newly-created entity's audit fields are fresh:
        createdAt within last MaxAgeSeconds
        updatedAt == createdAt (fresh entity, never updated)
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]$Result,
        [int]$MaxAgeSeconds = 60,
        [string]$CreatedAtField = 'createdAt',
        [string]$UpdatedAtField = 'updatedAt',
        [string]$Context = ''
    )

    if (-not $Result.Body) {
        throw [LcAssertionFailure]::new('Assert-AuditFieldsFresh', 'body with audit fields', 'body=null', $Context)
    }

    $createdAt = $Result.Body.$CreatedAtField
    $updatedAt = $Result.Body.$UpdatedAtField

    if (-not $createdAt) {
        throw [LcAssertionFailure]::new('Assert-AuditFieldsFresh', "$CreatedAtField present", "missing $CreatedAtField", $Context)
    }
    if (-not $updatedAt) {
        throw [LcAssertionFailure]::new('Assert-AuditFieldsFresh', "$UpdatedAtField present", "missing $UpdatedAtField", $Context)
    }

    $createdAtDt = [datetime]::Parse($createdAt).ToUniversalTime()
    $ageSeconds = ([datetime]::UtcNow - $createdAtDt).TotalSeconds

    if ($ageSeconds -gt $MaxAgeSeconds) {
        throw [LcAssertionFailure]::new('Assert-AuditFieldsFresh', "createdAt within $MaxAgeSeconds sec", "age=$ageSeconds sec", $Context)
    }

    # For a fresh entity, updatedAt should equal createdAt (or be very close — sub-second)
    $updatedAtDt = [datetime]::Parse($updatedAt).ToUniversalTime()
    $deltaMs = [Math]::Abs(($updatedAtDt - $createdAtDt).TotalMilliseconds)
    if ($deltaMs -gt 1000) {
        throw [LcAssertionFailure]::new('Assert-AuditFieldsFresh', 'updatedAt == createdAt (fresh entity)', "delta=$deltaMs ms", $Context)
    }
    return $true
}

function Assert-AuditFieldsUpdated {
    <#
    .SYNOPSIS
      After a PATCH/PUT, asserts updatedAt > createdAt by at least DeltaMs milliseconds.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]$Result,
        [int]$MinDeltaMs = 100,
        [string]$CreatedAtField = 'createdAt',
        [string]$UpdatedAtField = 'updatedAt',
        [string]$Context = ''
    )

    if (-not $Result.Body) {
        throw [LcAssertionFailure]::new('Assert-AuditFieldsUpdated', 'body with audit fields', 'body=null', $Context)
    }

    $createdAtDt = [datetime]::Parse($Result.Body.$CreatedAtField).ToUniversalTime()
    $updatedAtDt = [datetime]::Parse($Result.Body.$UpdatedAtField).ToUniversalTime()
    $deltaMs = ($updatedAtDt - $createdAtDt).TotalMilliseconds

    if ($deltaMs -lt $MinDeltaMs) {
        throw [LcAssertionFailure]::new('Assert-AuditFieldsUpdated', "updatedAt > createdAt by >= $MinDeltaMs ms", "delta=$deltaMs ms", $Context)
    }
    return $true
}

function Assert-LogSilence {
    <#
    .SYNOPSIS
      Asserts container logs contain no 42703 / 22P02 / NpgsqlException / InvalidOperationException
      / [ERR] entries in the last $TailWindowSeconds seconds.

      Delegates to existing scripts/smoke/Smoke-LogSilence.ps1 to avoid duplicating the az containerapp logs logic.

    .PARAMETER TailLines
      How many tail lines to inspect. Default 300 (Azure CLI limit).
    #>
    [CmdletBinding()]
    param(
        [int]$TailLines = 300,
        [string]$Context = '',
        [string]$ContainerAppName = 'lankaconnect-api-staging',
        [string]$ResourceGroup = 'lankaconnect-staging'
    )

    $logs = & az containerapp logs show -n $ContainerAppName -g $ResourceGroup --tail $TailLines 2>&1
    $errorPatterns = @('42703', '22P02', 'NpgsqlException', 'InvalidOperationException', '\[ERR\]')
    $combinedPattern = '(' + ($errorPatterns -join '|') + ')'
    $hits = $logs | Select-String -Pattern $combinedPattern -CaseSensitive:$false

    if ($hits) {
        $preview = ($hits | Select-Object -First 5 | ForEach-Object { $_.Line }) -join '; '
        throw [LcAssertionFailure]::new('Assert-LogSilence', 'no error patterns in last 300 log lines', "$($hits.Count) hits: $preview", $Context)
    }
    return $true
}

function Assert-DomainEventDispatched {
    <#
    .SYNOPSIS
      Asserts that a specific domain event was dispatched in recent container logs.
      Catches Wave3-followup.B-class regressions where domain events silently fail to dispatch.

      Looks for the canonical dispatch log line:
        [Phase 6A.24] Successfully dispatched domain event: <EventType>

    .PARAMETER EventType
      e.g. 'RegistrationConfirmedEvent', 'NewsletterCreatedEvent'
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$EventType,
        [int]$TailLines = 300,
        [string]$Context = '',
        [string]$ContainerAppName = 'lankaconnect-api-staging',
        [string]$ResourceGroup = 'lankaconnect-staging'
    )

    $logs = & az containerapp logs show -n $ContainerAppName -g $ResourceGroup --tail $TailLines 2>&1
    $pattern = "(Successfully dispatched domain event.*$EventType|Dispatched.*$EventType)"
    $hits = $logs | Select-String -Pattern $pattern -CaseSensitive:$false

    if (-not $hits) {
        throw [LcAssertionFailure]::new(
            'Assert-DomainEventDispatched',
            "log line containing 'dispatched ... $EventType'",
            'no matching log lines found (event was not dispatched OR log tail too short)',
            $Context)
    }
    return $true
}

Export-ModuleMember -Function `
    Assert-HttpStatus, `
    Assert-Http200, Assert-Http201, Assert-Http204, `
    Assert-Http400, Assert-Http403, Assert-Http404, `
    Assert-JsonField, Assert-JsonPath, Get-LcJsonPath, `
    Assert-CountIncremented, `
    Assert-AuditFieldsFresh, Assert-AuditFieldsUpdated, `
    Assert-LogSilence, Assert-DomainEventDispatched
