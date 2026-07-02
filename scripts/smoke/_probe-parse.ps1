<#
.SYNOPSIS
  Parse + dedupe rotating-tail files + emit probe-marker classification.

.DESCRIPTION
  Reads all `tail-NNNN.log` files in $CaptureDir, unions their content, dedupes
  by full-line + correlationId. Emits:

  - `probe-union.log` -- all deduped log lines (raw)
  - `probe-events.tsv` -- one row per SendEmailAsync invocation with columns:
      correlationId, template, recipient, entryTs, validationResult,
      providerInvokeTs, providerSuccess, providerError, durationMs, exceptionType, exceptionMessage
  - `probe-classification.md` -- markdown table classifying each unique template
      by matrix row (Class 1 delivered / Class 2 validation-fail / Class 3 ENTRY-absent-in-log)

.PARAMETER CaptureDir
  Directory containing rotating-tail files.

.PARAMETER ExpectedTemplates
  Optional comma-separated list of templates expected to fire during the run.
  Templates in this list but NOT present as ENTRY markers get "ENTRY-absent"
  classification. Templates NOT in this list still get classified from their
  markers.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$CaptureDir,
    [string]$ExpectedTemplates = ''
)

$expected = $ExpectedTemplates -split ',' | Where-Object { $_ -and $_.Trim() }

# Read all rotating-tail files, union content, dedupe by "Log" JSON payload
$files = Get-ChildItem -Path $CaptureDir -Filter 'tail-*.log' | Sort-Object Name
Write-Host "[parse] reading $($files.Count) tail files"

$seen = [System.Collections.Generic.HashSet[string]]::new()
$union = [System.Collections.Generic.List[string]]::new()
foreach ($f in $files) {
    Get-Content $f.FullName | ForEach-Object {
        if ($_ -and $seen.Add($_)) { $union.Add($_) }
    }
}
$unionPath = Join-Path $CaptureDir 'probe-union.log'
$union | Set-Content -Encoding utf8 $unionPath
Write-Host "[parse] union: $($union.Count) unique lines -> $unionPath"

# Extract probe events by correlationId
$events = @{}  # correlationId -> hashtable of fields

function _Parse-JsonLog([string]$line) {
    if ($line -notmatch '"Log":\s*"([^"]*)"') { return $null }
    return $Matches[1]
}

foreach ($line in $union) {
    $logPayload = _Parse-JsonLog $line
    if (-not $logPayload) { continue }

    if ($logPayload -match 'ENTRY correlationId=([a-f0-9-]+) template=([a-z-]+) recipient=([^\s]+)') {
        $cid = $Matches[1]
        if (-not $events.ContainsKey($cid)) { $events[$cid] = @{ correlationId = $cid } }
        $events[$cid].template = $Matches[2]
        $events[$cid].recipient = $Matches[3]
        $events[$cid].entryTs = if ($line -match '"TimeStamp":\s*"([^"]+)"') { $Matches[1] } else { '' }
    }
    elseif ($logPayload -match 'VALIDATION-OK correlationId=([a-f0-9-]+)') {
        $cid = $Matches[1]
        if (-not $events.ContainsKey($cid)) { $events[$cid] = @{ correlationId = $cid } }
        $events[$cid].validationResult = 'OK'
    }
    elseif ($logPayload -match 'VALIDATION-FAIL correlationId=([a-f0-9-]+) template=([a-z-]+) errors=(.+)$') {
        $cid = $Matches[1]
        if (-not $events.ContainsKey($cid)) { $events[$cid] = @{ correlationId = $cid } }
        $events[$cid].template = $Matches[2]
        $events[$cid].validationResult = 'FAIL'
        $events[$cid].validationErrors = $Matches[3]
    }
    elseif ($logPayload -match 'PROVIDER-INVOKE correlationId=([a-f0-9-]+)') {
        $cid = $Matches[1]
        if (-not $events.ContainsKey($cid)) { $events[$cid] = @{ correlationId = $cid } }
        $events[$cid].providerInvokeTs = if ($line -match '"TimeStamp":\s*"([^"]+)"') { $Matches[1] } else { '' }
    }
    elseif ($logPayload -match 'PROVIDER-RESULT correlationId=([a-f0-9-]+) template=([a-z-]+) success=(\w+) error=([^\s]*) durationMs=(\d+)') {
        $cid = $Matches[1]
        if (-not $events.ContainsKey($cid)) { $events[$cid] = @{ correlationId = $cid } }
        $events[$cid].template = $Matches[2]
        $events[$cid].providerSuccess = $Matches[3]
        $events[$cid].providerError = $Matches[4]
        $events[$cid].durationMs = $Matches[5]
    }
    elseif ($logPayload -match 'EXCEPTION correlationId=([a-f0-9-]+) template=([a-z-]+) exceptionType=([^\s]+) message=(.+)$') {
        $cid = $Matches[1]
        if (-not $events.ContainsKey($cid)) { $events[$cid] = @{ correlationId = $cid } }
        $events[$cid].template = $Matches[2]
        $events[$cid].exceptionType = $Matches[3]
        $events[$cid].exceptionMessage = $Matches[4]
    }
}

Write-Host "[parse] extracted $($events.Count) unique probe invocations"

# Emit TSV
$tsvPath = Join-Path $CaptureDir 'probe-events.tsv'
$header = "correlationId`ttemplate`trecipient`tentryTs`tvalidationResult`tvalidationErrors`tproviderInvokeTs`tproviderSuccess`tproviderError`tdurationMs`texceptionType`texceptionMessage"
$rows = @($header)
foreach ($cid in ($events.Keys | Sort-Object)) {
    $e = $events[$cid]
    $rows += "$($e.correlationId)`t$($e.template)`t$($e.recipient)`t$($e.entryTs)`t$($e.validationResult)`t$($e.validationErrors)`t$($e.providerInvokeTs)`t$($e.providerSuccess)`t$($e.providerError)`t$($e.durationMs)`t$($e.exceptionType)`t$($e.exceptionMessage)"
}
$rows | Set-Content -Encoding utf8 $tsvPath
Write-Host "[parse] TSV -> $tsvPath"

# Emit classification markdown
$byTemplate = @{}
foreach ($e in $events.Values) {
    $t = $e.template
    if (-not $t) { continue }
    if (-not $byTemplate.ContainsKey($t)) { $byTemplate[$t] = @{ count = 0; providerSuccess = 0; providerFail = 0; validationFail = 0; exception = 0; errors = @() } }
    $byTemplate[$t].count++
    if ($e.validationResult -eq 'FAIL') {
        $byTemplate[$t].validationFail++
        $byTemplate[$t].errors += $e.validationErrors
    }
    if ($e.providerSuccess -eq 'True') { $byTemplate[$t].providerSuccess++ }
    if ($e.providerSuccess -eq 'False') {
        $byTemplate[$t].providerFail++
        $byTemplate[$t].errors += $e.providerError
    }
    if ($e.exceptionType) {
        $byTemplate[$t].exception++
        $byTemplate[$t].errors += "$($e.exceptionType): $($e.exceptionMessage)"
    }
}

$mdPath = Join-Path $CaptureDir 'probe-classification.md'
$md = @('# Probe classification', '')
$md += '## Templates that reached SendEmailAsync'
$md += ''
$md += '| Template | Attempts | Class 1 (success) | Class 2 (validation-fail) | Provider-fail | Exception | Notes |'
$md += '|---|---:|---:|---:|---:|---:|---|'
foreach ($t in ($byTemplate.Keys | Sort-Object)) {
    $b = $byTemplate[$t]
    $notes = if ($b.errors.Count -gt 0) { ($b.errors | Select-Object -First 1) -replace '\|','\|' } else { '' }
    $md += "| $t | $($b.count) | $($b.providerSuccess) | $($b.validationFail) | $($b.providerFail) | $($b.exception) | $notes |"
}
$md += ''

if ($expected.Count -gt 0) {
    $seen = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($t in $byTemplate.Keys) { $seen.Add($t) | Out-Null }
    $missing = @()
    foreach ($e in $expected) {
        $et = $e.Trim()
        if (-not $seen.Contains($et)) { $missing += $et }
    }
    $md += '## Class 3 — Expected templates with NO probe evidence (ENTRY absent)'
    $md += ''
    if ($missing.Count -eq 0) {
        $md += '_(none)_'
    } else {
        $md += '| Template |'
        $md += '|---|'
        foreach ($m in $missing) { $md += "| $m |" }
    }
}

$md | Set-Content -Encoding utf8 $mdPath
Write-Host "[parse] markdown -> $mdPath"
Write-Host ''
Write-Host '=== summary ==='
Write-Host "unique templates that reached SendEmailAsync: $($byTemplate.Count)"
$providerSuccess = ($byTemplate.Values | Where-Object { $_.providerSuccess -gt 0 }).Count
$validationFail = ($byTemplate.Values | Where-Object { $_.validationFail -gt 0 }).Count
Write-Host "templates with >=1 provider success: $providerSuccess"
Write-Host "templates with >=1 validation fail: $validationFail"
