<#
.SYNOPSIS
  LankaConnect smoke HTTP wrapper. Foundation module for Wave 9 API Smoke Suite.

.DESCRIPTION
  Wraps Invoke-WebRequest with:
   - Bearer token injection (from $env:LC_BEARER, set by Lc-Auth.Get-LcBearer)
   - Retry-on-transient (5xx, network errors) with exponential backoff
   - Structured error capture (HTTP code, response body, exception type)
   - Returns an Lc-HttpResult object: { Success, StatusCode, Body, Headers, Error, DurationMs }

  Uses Invoke-WebRequest (not Invoke-RestMethod) for PowerShell 5.1 compatibility — the
  -SkipHttpErrorCheck / -StatusCodeVariable / -ResponseHeadersVariable parameters on
  Invoke-RestMethod are PS 7+ only. Invoke-WebRequest works on both PS 5.1 and 7+, with
  HTTP errors caught via try/catch and status extracted from the exception's Response.

.NOTES
  Wave 9.a Foundation module (architect-ruled 2026-06-29 Q1).
  Per architect Q1: Lc-Http is the foundational HTTP plumbing that EVERY assertion + fixture module uses.

  Tests mock Invoke-LcHttpRaw (the thin wrapper around Invoke-WebRequest) rather than
  Invoke-WebRequest itself, so test scenarios stay declarative and PS-version-agnostic.
#>

$script:LcBaseUrl = if ($env:LC_STAGING_URL) { $env:LC_STAGING_URL } else { 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io' }

$script:LcRetryPolicy = @{
    MaxAttempts        = 3
    InitialDelayMs     = 500
    BackoffMultiplier  = 2.0
    RetryOnStatusCodes = @(429, 500, 502, 503, 504)
}

function Set-LcHttpBaseUrl {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Url)
    $script:LcBaseUrl = $Url.TrimEnd('/')
}

function Get-LcHttpBaseUrl {
    return $script:LcBaseUrl
}

function Set-LcHttpRetryPolicy {
    [CmdletBinding()]
    param(
        [int]$MaxAttempts = 3,
        [int]$InitialDelayMs = 500,
        [double]$BackoffMultiplier = 2.0,
        [int[]]$RetryOnStatusCodes = @(429, 500, 502, 503, 504)
    )
    $script:LcRetryPolicy = @{
        MaxAttempts        = $MaxAttempts
        InitialDelayMs     = $InitialDelayMs
        BackoffMultiplier  = $BackoffMultiplier
        RetryOnStatusCodes = $RetryOnStatusCodes
    }
}

function New-LcHttpResult {
    param(
        [bool]$Success,
        [int]$StatusCode,
        $Body,
        $Headers,
        [string]$Error,
        [int]$DurationMs,
        [int]$AttemptCount,
        [string]$Method,
        [string]$Url
    )
    return [pscustomobject]@{
        Success      = $Success
        StatusCode   = $StatusCode
        Body         = $Body
        Headers      = $Headers
        Error        = $Error
        DurationMs   = $DurationMs
        AttemptCount = $AttemptCount
        Method       = $Method
        Url          = $Url
    }
}

function Invoke-LcHttpRaw {
    <#
    .SYNOPSIS
      Thin wrapper around Invoke-WebRequest with structured success/failure shape.
      Returns { StatusCode, Body, Headers, NetworkError }.
      Tests mock THIS function to control scenarios without touching real HTTP.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][string]$Uri,
        [hashtable]$Headers,
        [string]$JsonBody,
        [int]$TimeoutSec = 30
    )

    $params = @{
        Uri             = $Uri
        Method          = $Method
        Headers         = $Headers
        TimeoutSec      = $TimeoutSec
        UseBasicParsing = $true
        ErrorAction     = 'Stop'
    }
    if ($JsonBody) { $params['Body'] = $JsonBody }

    try {
        $response = Invoke-WebRequest @params
        $statusCode = [int]$response.StatusCode
        $body = if ($response.Content) {
            try { $response.Content | ConvertFrom-Json -ErrorAction Stop } catch { $response.Content }
        } else { $null }
        return [pscustomobject]@{
            StatusCode   = $statusCode
            Body         = $body
            Headers      = $response.Headers
            NetworkError = $null
        }
    }
    catch {
        $ex = $_.Exception
        # PS 7+ wraps HTTP errors in HttpResponseException; PS 5.1 wraps in WebException
        if ($ex.Response) {
            $statusCode = [int]$ex.Response.StatusCode
            $errorBody = $null
            try {
                $stream = $ex.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($stream)
                $content = $reader.ReadToEnd()
                $reader.Close()
                $errorBody = try { $content | ConvertFrom-Json -ErrorAction Stop } catch { $content }
            } catch { }
            return [pscustomobject]@{
                StatusCode   = $statusCode
                Body         = $errorBody
                Headers      = $ex.Response.Headers
                NetworkError = $null
            }
        }
        # Network / DNS / timeout - no response
        return [pscustomobject]@{
            StatusCode   = 0
            Body         = $null
            Headers      = $null
            NetworkError = $ex.Message
        }
    }
}

function Invoke-LcRequest {
    <#
    .SYNOPSIS
      Core HTTP invocation with retry + structured result.

    .PARAMETER Method
      HTTP verb (GET, POST, PUT, PATCH, DELETE).

    .PARAMETER Path
      URL path (e.g. /api/Events/my-events). Prepended with $script:LcBaseUrl unless absolute.

    .PARAMETER Body
      Request body (object; converted to JSON automatically).

    .PARAMETER Bearer
      Bearer token. If omitted, reads from $env:LC_BEARER. Pass $null to make an unauthenticated call.

    .PARAMETER ExtraHeaders
      Optional dictionary of additional headers.

    .PARAMETER TimeoutSec
      Request timeout. Default 30s.

    .OUTPUTS
      Lc-HttpResult pscustomobject - never throws on HTTP failure; caller checks .Success.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][ValidateSet('GET', 'POST', 'PUT', 'PATCH', 'DELETE')][string]$Method,
        [Parameter(Mandatory)][string]$Path,
        $Body = $null,
        [object]$Bearer = '__USE_ENV__',
        [hashtable]$ExtraHeaders = @{},
        [int]$TimeoutSec = 30
    )

    $resolvedBearer = if ($Bearer -eq '__USE_ENV__') { $env:LC_BEARER } else { $Bearer }
    $url = if ($Path -match '^https?://') { $Path } else { "$script:LcBaseUrl$Path" }

    $headers = @{ 'Content-Type' = 'application/json' }
    if ($resolvedBearer) { $headers['Authorization'] = "Bearer $resolvedBearer" }
    foreach ($k in $ExtraHeaders.Keys) { $headers[$k] = $ExtraHeaders[$k] }

    $jsonBody = if ($null -ne $Body) { ConvertTo-Json $Body -Depth 10 -Compress } else { $null }

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $attempt = 0
    $delayMs = $script:LcRetryPolicy.InitialDelayMs
    $lastRaw = $null

    while ($attempt -lt $script:LcRetryPolicy.MaxAttempts) {
        $attempt++

        $raw = Invoke-LcHttpRaw -Method $Method -Uri $url -Headers $headers -JsonBody $jsonBody -TimeoutSec $TimeoutSec
        $lastRaw = $raw

        # Network error - retry per policy
        if ($raw.NetworkError) {
            if ($attempt -lt $script:LcRetryPolicy.MaxAttempts) {
                Start-Sleep -Milliseconds $delayMs
                $delayMs = [int]($delayMs * $script:LcRetryPolicy.BackoffMultiplier)
                continue
            }
            $stopwatch.Stop()
            return (New-LcHttpResult `
                -Success $false -StatusCode 0 -Body $null -Headers $null `
                -Error "Network/transport error: $($raw.NetworkError)" `
                -DurationMs $stopwatch.ElapsedMilliseconds -AttemptCount $attempt `
                -Method $Method -Url $url)
        }

        # Transient HTTP error - retry per policy
        $isTransient = $script:LcRetryPolicy.RetryOnStatusCodes -contains $raw.StatusCode
        if ($isTransient -and $attempt -lt $script:LcRetryPolicy.MaxAttempts) {
            Start-Sleep -Milliseconds $delayMs
            $delayMs = [int]($delayMs * $script:LcRetryPolicy.BackoffMultiplier)
            continue
        }

        # Terminal result (success OR final failure)
        $isSuccess = $raw.StatusCode -ge 200 -and $raw.StatusCode -lt 400
        $stopwatch.Stop()
        return (New-LcHttpResult `
            -Success $isSuccess -StatusCode $raw.StatusCode -Body $raw.Body -Headers $raw.Headers `
            -Error $(if (-not $isSuccess) { "HTTP $($raw.StatusCode)" } else { $null }) `
            -DurationMs $stopwatch.ElapsedMilliseconds -AttemptCount $attempt `
            -Method $Method -Url $url)
    }

    # Defensive: should be unreachable; here for safety
    $stopwatch.Stop()
    return (New-LcHttpResult `
        -Success $false -StatusCode $(if ($lastRaw) { $lastRaw.StatusCode } else { 0 }) -Body $null -Headers $null `
        -Error "Exhausted $($script:LcRetryPolicy.MaxAttempts) attempts" `
        -DurationMs $stopwatch.ElapsedMilliseconds -AttemptCount $attempt `
        -Method $Method -Url $url)
}

# Convenience verb wrappers
function Invoke-LcGet    { [CmdletBinding()] param([Parameter(Mandatory)][string]$Path, [object]$Bearer = '__USE_ENV__') Invoke-LcRequest -Method GET    -Path $Path -Bearer $Bearer }
function Invoke-LcPost   { [CmdletBinding()] param([Parameter(Mandatory)][string]$Path, $Body, [object]$Bearer = '__USE_ENV__') Invoke-LcRequest -Method POST   -Path $Path -Body $Body -Bearer $Bearer }
function Invoke-LcPut    { [CmdletBinding()] param([Parameter(Mandatory)][string]$Path, $Body, [object]$Bearer = '__USE_ENV__') Invoke-LcRequest -Method PUT    -Path $Path -Body $Body -Bearer $Bearer }
function Invoke-LcPatch  { [CmdletBinding()] param([Parameter(Mandatory)][string]$Path, $Body, [object]$Bearer = '__USE_ENV__') Invoke-LcRequest -Method PATCH  -Path $Path -Body $Body -Bearer $Bearer }
function Invoke-LcDelete { [CmdletBinding()] param([Parameter(Mandatory)][string]$Path, [object]$Bearer = '__USE_ENV__') Invoke-LcRequest -Method DELETE -Path $Path -Bearer $Bearer }

# ============================================================================
# Wave 9.h.4 Multipart upload wrapper
# ============================================================================

function Get-LcTestPng {
    <#
    .SYNOPSIS
      Returns the bytes of a tiny 1x1 transparent PNG suitable for image-upload smokes.
      Lazy-singleton (computed once per smoke run).
    #>
    [CmdletBinding()] param()
    if ($script:LcTestPngBytes) { return $script:LcTestPngBytes }
    # 1x1 transparent PNG -- 67 bytes, validated by every modern image processor
    $base64 = 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII='
    $script:LcTestPngBytes = [Convert]::FromBase64String($base64)
    return $script:LcTestPngBytes
}

function Invoke-LcMultipart {
    <#
    .SYNOPSIS
      POSTs a multipart/form-data body to the API. Used for image-upload smokes
      (profile photo, sponsor brochure, badge image, business image, photo album).

    .PARAMETER Path
      Endpoint path (e.g. /api/Users/{id}/profile-photo).

    .PARAMETER FileFieldName
      Form field name the controller expects (e.g. 'file', 'image', 'photo').

    .PARAMETER FileName
      Filename header (e.g. 'test.png').

    .PARAMETER FileBytes
      Bytes to upload. Defaults to Get-LcTestPng.

    .PARAMETER ContentType
      MIME type. Defaults to image/png.

    .PARAMETER ExtraFields
      Hashtable of additional form fields (string key -> string value).

    .PARAMETER Bearer
      Bearer token. Defaults to env.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [string]$FileFieldName = 'file',
        [string]$FileName = 'smoke-test.png',
        [byte[]]$FileBytes = $(Get-LcTestPng),
        [string]$ContentType = 'image/png',
        [hashtable]$ExtraFields = @{},
        [object]$Bearer = '__USE_ENV__'
    )

    # Build multipart body manually (Invoke-WebRequest's -Form is PS7+; we need PS5.1 compat)
    $boundary = "----LcSmokeBoundary$(Get-Random)"
    $LF = "`r`n"

    # ASCII-encode the headers / form parts
    $sb = New-Object System.Text.StringBuilder
    foreach ($key in $ExtraFields.Keys) {
        [void]$sb.Append("--$boundary$LF")
        [void]$sb.Append("Content-Disposition: form-data; name=`"$key`"$LF$LF")
        [void]$sb.Append("$($ExtraFields[$key])$LF")
    }
    [void]$sb.Append("--$boundary$LF")
    [void]$sb.Append("Content-Disposition: form-data; name=`"$FileFieldName`"; filename=`"$FileName`"$LF")
    [void]$sb.Append("Content-Type: $ContentType$LF$LF")
    $preBytes = [System.Text.Encoding]::UTF8.GetBytes($sb.ToString())

    $postBytes = [System.Text.Encoding]::UTF8.GetBytes("$LF--$boundary--$LF")

    $combined = New-Object byte[] ($preBytes.Length + $FileBytes.Length + $postBytes.Length)
    [System.Buffer]::BlockCopy($preBytes, 0, $combined, 0, $preBytes.Length)
    [System.Buffer]::BlockCopy($FileBytes, 0, $combined, $preBytes.Length, $FileBytes.Length)
    [System.Buffer]::BlockCopy($postBytes, 0, $combined, $preBytes.Length + $FileBytes.Length, $postBytes.Length)

    # Resolve bearer
    $bearerToken = if ($Bearer -eq '__USE_ENV__') { $env:LC_BEARER } else { $Bearer }
    $headers = @{ 'Accept' = 'application/json'; 'Content-Type' = "multipart/form-data; boundary=$boundary" }
    if ($bearerToken) { $headers['Authorization'] = "Bearer $bearerToken" }

    $uri = if ($Path -match '^https?://') { $Path } else { "$script:LcBaseUrl$Path" }

    try {
        $response = Invoke-WebRequest -Uri $uri -Method POST -Headers $headers -Body $combined -UseBasicParsing -TimeoutSec 60 -ErrorAction Stop
        $statusCode = [int]$response.StatusCode
        $body = if ($response.Content) {
            try { $response.Content | ConvertFrom-Json -ErrorAction Stop } catch { $response.Content }
        } else { $null }
        return [pscustomobject]@{ Success = $true; StatusCode = $statusCode; Body = $body; Error = $null }
    }
    catch {
        $ex = $_.Exception
        if ($ex.Response) {
            $statusCode = [int]$ex.Response.StatusCode
            $errorBody = $null
            try {
                $stream = $ex.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($stream)
                $content = $reader.ReadToEnd()
                $reader.Close()
                $errorBody = try { $content | ConvertFrom-Json -ErrorAction Stop } catch { $content }
            } catch { }
            return [pscustomobject]@{ Success = $false; StatusCode = $statusCode; Body = $errorBody; Error = "HTTP $statusCode" }
        }
        return [pscustomobject]@{ Success = $false; StatusCode = 0; Body = $null; Error = $ex.Message }
    }
}

Export-ModuleMember -Function `
    Set-LcHttpBaseUrl, Get-LcHttpBaseUrl, `
    Set-LcHttpRetryPolicy, `
    Invoke-LcHttpRaw, `
    Invoke-LcRequest, Invoke-LcGet, Invoke-LcPost, Invoke-LcPut, Invoke-LcPatch, Invoke-LcDelete, `
    Get-LcTestPng, Invoke-LcMultipart
