<#
.SYNOPSIS
    Inspect Azure Container Apps logs for the last 60 seconds and FAIL if
    any error patterns appear that indicate EF Core / Postgres / DI failures.

.DESCRIPTION
    Per CLAUDE.md §13.2 S3: every mutator / mapping change / new endpoint
    smoke must include a log-silence assertion. HTTP 200 alone is not
    "verified" - the request can return 200 while errors land in logs
    (e.g., fire-and-forget handlers, cached null swallows, etc.).

    Default error patterns scanned:
      - "42703"                       (PostgreSQL undefined column)
      - "22P02"                       (invalid text representation)
      - "23505"                       (unique violation - usually app bug)
      - "NpgsqlException"
      - "DatabaseConfigurationError"
      - "Application terminated unexpectedly"
      - "[FTL]"                       (Serilog fatal)

.PARAMETER Endpoint
    Optional - for log narrowing. Currently informational; az containerapp
    logs show is full-tail, not per-endpoint filtered.

.PARAMETER TailSeconds
    How far back to look. Default 60.

.PARAMETER ExtraPatterns
    Additional regex patterns to fail on (e.g., a smoke-specific exception).

.PARAMETER ResourceGroup
    Default 'lankaconnect-staging'.

.PARAMETER AppName
    Default 'lankaconnect-api-staging'.

.OUTPUTS
    Exit 0 + summary on green.
    Exit 1 + sample log lines on red.

.NOTES
    Built as part of Gap G0 (2026-06-08). Requires az CLI authenticated.
#>
[CmdletBinding()]
param(
    [string]$Endpoint,
    [int]$TailSeconds = 60,
    [string[]]$ExtraPatterns,
    [string]$ResourceGroup = 'lankaconnect-staging',
    [string]$AppName       = 'lankaconnect-api-staging'
)

$ErrorActionPreference = 'Stop'

$basePatterns = @(
    '42703',
    '22P02',
    '23505',
    'NpgsqlException',
    'DatabaseConfigurationError',
    'Application terminated unexpectedly',
    '\[FTL\]'
)
$extra = if ($ExtraPatterns) { $ExtraPatterns } else { @() }
$allPatterns = $basePatterns + $extra
$combinedRegex = ($allPatterns -join '|')

try {
    # `az containerapp logs show --tail N` returns the most recent N lines;
    # there's no time-window filter, so we tail a generous 200 lines and
    # accept that we may include slightly older entries.
    $tailLines = [Math]::Max(50, $TailSeconds * 3)
    Write-Verbose "Fetching last $tailLines lines of $AppName ..."

    $logsRaw = & az containerapp logs show `
        --name $AppName `
        --resource-group $ResourceGroup `
        --tail $tailLines `
        --format text 2>&1

    if ($LASTEXITCODE -ne 0) {
        throw "az containerapp logs show failed (exit $LASTEXITCODE): $logsRaw"
    }

    $logs = $logsRaw -split "`n"
    $hits = $logs | Where-Object { $_ -match $combinedRegex }

    if ($hits.Count -gt 0) {
        $sample = ($hits | Select-Object -First 5) -join "`n  "
        Write-Error @"
Smoke-LogSilence FAILED - $($hits.Count) error-pattern hits in last $tailLines log lines.
First few:
  $sample
"@
        exit 1
    }

    $endpointTag = if ($Endpoint) { " for $Endpoint" } else { '' }
    $extraCount = if ($ExtraPatterns) { $ExtraPatterns.Count } else { 0 }
    $patternCount = $basePatterns.Count + $extraCount
    "LogSilence OK no $patternCount error patterns in last $tailLines lines$endpointTag"
    exit 0
}
catch {
    Write-Error "Smoke-LogSilence FAILED: $($_.Exception.Message)"
    exit 1
}
