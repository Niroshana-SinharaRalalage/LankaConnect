<#
.SYNOPSIS
  Rotating-tail log capture for Wave 9.h.10.5 Pass 1/N runs.

.DESCRIPTION
  `az containerapp logs show --follow` drops mid-run (concurrent-connection
  timeouts, stream reset by container revision flip, etc). Pass 1 lost log
  coverage after ~8 min of a ~19 min smoke run. This script rotates through
  fresh `az containerapp logs show --tail 300 --output tsv` invocations every
  20 seconds into sequential files. Redundancy means each log line appears in
  ~2-3 consecutive files (300-line window > 20s activity in normal traffic);
  the parse step at end dedupes.

  Runs until stopped externally (via TaskStop). Output goes to
  $CaptureDir\tail-NNNN.log (sequential; NNNN = 4-digit invocation counter).

.PARAMETER CaptureDir
  Directory to write rotating tail files. Auto-created if missing.

.PARAMETER Revision
  Optional container revision name to pin the tail to. If a revision flip
  happens mid-run (auto-scale, redeploy), pinning ensures we don't unknowingly
  cross-mix logs. Defaults to LatestRevision (unpinned).

.EXAMPLE
  # Start in background
  Start-Job -ScriptBlock { & 'c:/Work/LankaConnect/scripts/smoke/_probe-rotating-tail.ps1' -CaptureDir 'C:/tmp/pass2' }
  # Run smoke suite in main shell
  # Stop the job when smoke ends: Stop-Job -Name <jobname>
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$CaptureDir,
    [string]$Revision,
    [int]$IntervalSeconds = 20
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding           = [System.Text.Encoding]::UTF8

if (-not (Test-Path $CaptureDir)) { New-Item -ItemType Directory -Path $CaptureDir | Out-Null }

$revArgs = @('--name', 'lankaconnect-api-staging', '--resource-group', 'lankaconnect-staging',
             '--tail', '300', '--follow', 'false', '--output', 'tsv')
if ($Revision) { $revArgs += @('--revision', $Revision) }

$counter = 0
Write-Host "[rotating-tail] starting; interval=${IntervalSeconds}s; dir=$CaptureDir"

while ($true) {
    $counter++
    $fileName = "tail-{0:D4}.log" -f $counter
    $filePath = Join-Path $CaptureDir $fileName
    try {
        # `az` stdout is UTF-16 on Windows PowerShell. Capture to a memory
        # string first then write with explicit utf8 encoding.
        $raw = & az containerapp logs show @revArgs 2>&1 | Out-String
        [System.IO.File]::WriteAllText($filePath, $raw, [System.Text.Encoding]::UTF8)
        Write-Host ("[rotating-tail] wrote {0} ({1} bytes)" -f $fileName, $raw.Length)
    } catch {
        Write-Host ("[rotating-tail] ERR on {0}: {1}" -f $fileName, $_.Exception.Message)
    }
    Start-Sleep -Seconds $IntervalSeconds
}
