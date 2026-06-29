<#
.SYNOPSIS
  LankaConnect smoke reporting module. Foundation for Wave 9 API Smoke Suite.

.DESCRIPTION
  Aggregates per-test results across a smoke run. Emits two output formats:
   - Markdown summary (for commit-message bodies, GH PR descriptions, founder review)
   - HTML report (for CI artifact upload — Wave 9.f integration)

  Exposes:
   - New-LcReport — starts a new report with a name + start timestamp
   - Add-LcResult — appends a result row (PASS / FAIL / SKIP) with metadata
   - Get-LcReportSummary — counts (passed / failed / skipped / total)
   - ConvertTo-LcMarkdown — emits Markdown
   - ConvertTo-LcHtml — emits HTML
   - Save-LcReportArtifacts — writes both formats to disk

.NOTES
  Wave 9.a Foundation module (architect-ruled 2026-06-29 Q7).
  Per architect: founder UAT sees this report before Commit 3 W5.3 STAGING-VERIFIED flip.
#>

class LcReport {
    [string]$Name
    [datetime]$StartedAt
    [datetime]$FinishedAt
    [System.Collections.Generic.List[object]]$Results

    LcReport([string]$name) {
        $this.Name = $name
        $this.StartedAt = [datetime]::UtcNow
        $this.FinishedAt = [datetime]::MinValue
        $this.Results = [System.Collections.Generic.List[object]]::new()
    }
}

function New-LcReport {
    <#
    .SYNOPSIS
      Creates a new smoke report. Pass to subsequent Add-LcResult / ConvertTo-Lc* calls.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Name)
    return [LcReport]::new($Name)
}

function Add-LcResult {
    <#
    .SYNOPSIS
      Appends a test result to the report.

    .PARAMETER Status
      PASS / FAIL / SKIP (matches three smoke states per architect Q5)
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][LcReport]$Report,
        [Parameter(Mandatory)][ValidateSet('PASS', 'FAIL', 'SKIP')][string]$Status,
        [Parameter(Mandatory)][string]$Section,
        [Parameter(Mandatory)][string]$TestName,
        [string]$Endpoint = '',
        [int]$DurationMs = 0,
        [string]$ErrorMessage = '',
        [string]$SkipReason = ''
    )

    $Report.Results.Add([pscustomobject]@{
        Status       = $Status
        Section      = $Section
        TestName     = $TestName
        Endpoint     = $Endpoint
        DurationMs   = $DurationMs
        ErrorMessage = $ErrorMessage
        SkipReason   = $SkipReason
        Timestamp    = [datetime]::UtcNow
    })
}

function Complete-LcReport {
    [CmdletBinding()]
    param([Parameter(Mandatory)][LcReport]$Report)
    $Report.FinishedAt = [datetime]::UtcNow
    return $Report
}

function Get-LcReportSummary {
    [CmdletBinding()]
    param([Parameter(Mandatory)][LcReport]$Report)

    $total = $Report.Results.Count
    $passed = @($Report.Results | Where-Object Status -eq 'PASS').Count
    $failed = @($Report.Results | Where-Object Status -eq 'FAIL').Count
    $skipped = @($Report.Results | Where-Object Status -eq 'SKIP').Count

    return [pscustomobject]@{
        Total            = $total
        Passed           = $passed
        Failed           = $failed
        Skipped          = $skipped
        PassRate         = if ($total -gt 0) { [math]::Round(($passed / $total) * 100, 2) } else { 0 }
        TotalDurationSec = if ($Report.FinishedAt -gt $Report.StartedAt) {
            [math]::Round(($Report.FinishedAt - $Report.StartedAt).TotalSeconds, 2)
        } else { 0 }
    }
}

function ConvertTo-LcMarkdown {
    <#
    .SYNOPSIS
      Emits a Markdown report. Suitable for commit-message bodies + GH PR descriptions.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][LcReport]$Report)

    $summary = Get-LcReportSummary -Report $Report
    $startedAt = $Report.StartedAt.ToString('yyyy-MM-ddTHH:mm:ssZ')
    $finishedAt = if ($Report.FinishedAt -gt $Report.StartedAt) {
        $Report.FinishedAt.ToString('yyyy-MM-ddTHH:mm:ssZ')
    } else { '(not yet completed)' }

    $statusLabel = if ($summary.Failed -gt 0) { '[FAIL]' } elseif ($summary.Skipped -gt 0) { '[PASS-WITH-SKIPS]' } else { '[PASS]' }

    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine("# Smoke Report: $($Report.Name)")
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine("**Status**: $statusLabel")
    [void]$sb.AppendLine("**Started**: $startedAt UTC")
    [void]$sb.AppendLine("**Finished**: $finishedAt UTC")
    [void]$sb.AppendLine("**Duration**: $($summary.TotalDurationSec) sec")
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('## Summary')
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine("| Metric | Count |")
    [void]$sb.AppendLine("|---|---|")
    [void]$sb.AppendLine("| Total | $($summary.Total) |")
    [void]$sb.AppendLine("| Passed | $($summary.Passed) |")
    [void]$sb.AppendLine("| Failed | $($summary.Failed) |")
    [void]$sb.AppendLine("| Skipped | $($summary.Skipped) |")
    [void]$sb.AppendLine("| Pass rate | $($summary.PassRate)% |")
    [void]$sb.AppendLine('')

    # Per-section breakdown
    $sections = $Report.Results | Group-Object -Property Section | Sort-Object Name
    if ($sections.Count -gt 0) {
        [void]$sb.AppendLine('## Per-Section Results')
        [void]$sb.AppendLine('')
        [void]$sb.AppendLine("| Section | Pass | Fail | Skip |")
        [void]$sb.AppendLine("|---|---|---|---|")
        foreach ($s in $sections) {
            $sPass = @($s.Group | Where-Object Status -eq 'PASS').Count
            $sFail = @($s.Group | Where-Object Status -eq 'FAIL').Count
            $sSkip = @($s.Group | Where-Object Status -eq 'SKIP').Count
            [void]$sb.AppendLine("| $($s.Name) | $sPass | $sFail | $sSkip |")
        }
        [void]$sb.AppendLine('')
    }

    # Failures detail
    $failures = @($Report.Results | Where-Object Status -eq 'FAIL')
    if ($failures.Count -gt 0) {
        [void]$sb.AppendLine('## Failures')
        [void]$sb.AppendLine('')
        foreach ($f in $failures) {
            [void]$sb.AppendLine("- **$($f.Section) :: $($f.TestName)** ($($f.Endpoint))")
            [void]$sb.AppendLine("  - $($f.ErrorMessage)")
        }
        [void]$sb.AppendLine('')
    }

    # Skips with reasons
    $skips = @($Report.Results | Where-Object Status -eq 'SKIP')
    if ($skips.Count -gt 0) {
        [void]$sb.AppendLine('## Skipped (documented)')
        [void]$sb.AppendLine('')
        foreach ($s in $skips) {
            [void]$sb.AppendLine("- **$($s.Section) :: $($s.TestName)** ($($s.Endpoint)) - $($s.SkipReason)")
        }
        [void]$sb.AppendLine('')
    }

    return $sb.ToString()
}

function ConvertTo-LcHtml {
    <#
    .SYNOPSIS
      Emits an HTML report. Suitable for CI artifact upload (Wave 9.f).
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][LcReport]$Report)

    $summary = Get-LcReportSummary -Report $Report
    $startedAt = $Report.StartedAt.ToString('yyyy-MM-ddTHH:mm:ssZ')

    $statusClass = if ($summary.Failed -gt 0) { 'fail' } elseif ($summary.Skipped -gt 0) { 'warn' } else { 'pass' }

    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine('<!DOCTYPE html>')
    [void]$sb.AppendLine('<html><head><meta charset="utf-8">')
    [void]$sb.AppendLine("<title>Smoke Report: $($Report.Name)</title>")
    [void]$sb.AppendLine('<style>')
    [void]$sb.AppendLine('body{font-family:system-ui,sans-serif;margin:24px;color:#222}')
    [void]$sb.AppendLine('.status{padding:8px 16px;border-radius:4px;display:inline-block;color:#fff;font-weight:600}')
    [void]$sb.AppendLine('.pass{background:#22c55e}.fail{background:#dc2626}.warn{background:#eab308}')
    [void]$sb.AppendLine('table{border-collapse:collapse;margin:12px 0}td,th{border:1px solid #ddd;padding:6px 10px}th{background:#f3f4f6}')
    [void]$sb.AppendLine('tr.row-FAIL{background:#fee2e2}tr.row-SKIP{background:#fef3c7}')
    [void]$sb.AppendLine('.summary{margin:12px 0;font-size:14px}')
    [void]$sb.AppendLine('</style></head><body>')
    [void]$sb.AppendLine("<h1>Smoke Report: $([System.Web.HttpUtility]::HtmlEncode($Report.Name))</h1>")
    [void]$sb.AppendLine("<div class='status $statusClass'>$($statusClass.ToUpper()): $($summary.Passed)/$($summary.Total) passed</div>")
    [void]$sb.AppendLine("<div class='summary'>Started: $startedAt UTC | Duration: $($summary.TotalDurationSec) sec | Pass rate: $($summary.PassRate)%</div>")
    [void]$sb.AppendLine('<h2>Results</h2>')
    [void]$sb.AppendLine('<table><thead><tr><th>Status</th><th>Section</th><th>Test</th><th>Endpoint</th><th>Duration (ms)</th><th>Details</th></tr></thead><tbody>')
    foreach ($r in $Report.Results) {
        $details = if ($r.Status -eq 'FAIL') { [System.Web.HttpUtility]::HtmlEncode($r.ErrorMessage) }
                   elseif ($r.Status -eq 'SKIP') { [System.Web.HttpUtility]::HtmlEncode($r.SkipReason) }
                   else { '-' }
        [void]$sb.AppendLine("<tr class='row-$($r.Status)'><td>$($r.Status)</td><td>$([System.Web.HttpUtility]::HtmlEncode($r.Section))</td><td>$([System.Web.HttpUtility]::HtmlEncode($r.TestName))</td><td>$([System.Web.HttpUtility]::HtmlEncode($r.Endpoint))</td><td>$($r.DurationMs)</td><td>$details</td></tr>")
    }
    [void]$sb.AppendLine('</tbody></table>')
    [void]$sb.AppendLine('</body></html>')
    return $sb.ToString()
}

function Save-LcReportArtifacts {
    <#
    .SYNOPSIS
      Saves the report to disk as both .md and .html. Useful for CI artifact upload + local review.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][LcReport]$Report,
        [Parameter(Mandatory)][string]$OutputDir
    )
    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir | Out-Null
    }
    $base = $Report.Name -replace '[^a-zA-Z0-9_-]', '-'
    $mdPath = Join-Path $OutputDir "$base.md"
    $htmlPath = Join-Path $OutputDir "$base.html"
    Set-Content -Path $mdPath -Value (ConvertTo-LcMarkdown -Report $Report)
    Set-Content -Path $htmlPath -Value (ConvertTo-LcHtml -Report $Report)
    return [pscustomobject]@{ Markdown = $mdPath; Html = $htmlPath }
}

Export-ModuleMember -Function `
    New-LcReport, Add-LcResult, Complete-LcReport, `
    Get-LcReportSummary, `
    ConvertTo-LcMarkdown, ConvertTo-LcHtml, Save-LcReportArtifacts
