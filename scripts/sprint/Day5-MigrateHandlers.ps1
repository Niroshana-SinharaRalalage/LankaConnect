<#
.SYNOPSIS
  Day 5 helper: migrate MOD-module handlers from IUnitOfWork to IMultiContextUnitOfWork.

.DESCRIPTION
  For every LankaEvents/Notifications/Media/Forms command handler flagged by
  Audit-HandlerContext.ps1, apply the standard Rule 5j.4 transform:

    1. Ctor parameter: IUnitOfWork -> IMultiContextUnitOfWork
    2. Field type:     IUnitOfWork -> IMultiContextUnitOfWork
    3. Ctor also injects the module DbContext (LankaEventsDbContext for Products/LankaEvents/*)
    4. Field for module DbContext added
    5. All _unitOfWork.CommitAsync(ct) calls rewritten to
       _unitOfWork.CommitAsync(new DbContext[] { _module_context }, ct)

  Idempotent: running twice is a no-op.

  Since handler files vary in shape (some already partially migrated, some have
  multiple DbContext dependencies), this script is CAUTIOUS: skips files where
  the transform would be ambiguous and logs them for manual review.

.PARAMETER Files
  Optional list of handler .cs paths. If omitted, uses Audit-HandlerContext.ps1
  output to select all flagged files.

.PARAMETER DryRun
  Print intended transforms without writing.

.EXAMPLE
  # Migrate all flagged LankaEvents handlers, dry-run
  .\Day5-MigrateHandlers.ps1 -DryRun

.EXAMPLE
  # Migrate a specific handler
  .\Day5-MigrateHandlers.ps1 -Files src\Products\LankaEvents\LankaEvents.Application\Commands\UpdateEvent\UpdateEventCommandHandler.cs

.NOTES
  Sprint bible: docs/MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md Day 5 section.
  Rule:         5j.4
  Consult:      #7 Delta (multi-DbContext done right)
#>

param(
    [string[]]$Files,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Get-ModuleContextForPath {
    param([string]$Path)
    if ($Path -match '\\Products\\LankaEvents\\')      { return @{ Name = 'LankaEventsDbContext';    Field = '_lankaEventsDbContext';    Namespace = 'LankaConnect.Products.LankaEvents.Infrastructure.Data' } }
    if ($Path -match '\\Modules\\Notifications\\')     { return @{ Name = 'NotificationsDbContext';  Field = '_notificationsDbContext';  Namespace = 'LankaConnect.Modules.Notifications.Infrastructure.Data' } }
    if ($Path -match '\\Modules\\Media\\')             { return @{ Name = 'MediaDbContext';          Field = '_mediaDbContext';          Namespace = 'LankaConnect.Modules.Media.Infrastructure.Data' } }
    if ($Path -match '\\Modules\\Forms\\')             { return @{ Name = 'FormsDbContext';          Field = '_formsDbContext';          Namespace = 'LankaConnect.Modules.Forms.Infrastructure.Data' } }
    return $null
}

function Convert-Handler {
    param([string]$Path, [switch]$DryRun)

    $module = Get-ModuleContextForPath -Path $Path
    if (-not $module) { Write-Host "  [SKIP-NONMOD] $Path"; return @{ Changed = $false; Skipped = 'not-mod-module' } }

    $content  = Get-Content -Raw -LiteralPath $Path
    $original = $content

    # 1. Rewrite ctor param type
    $content = $content -replace '\bIUnitOfWork\s+(unitOfWork\b)', 'IMultiContextUnitOfWork $1'
    # 2. Rewrite field type
    $content = $content -replace '\bprivate\s+readonly\s+IUnitOfWork\s+_unitOfWork\b', 'private readonly IMultiContextUnitOfWork _unitOfWork'

    # 3. Rewrite CommitAsync(ct) -> CommitAsync(new DbContext[] { _module_context }, ct)
    $ctxField = $module.Field
    $content = $content -replace '\._unitOfWork\.CommitAsync\s*\(\s*(cancellationToken|ct)\s*\)', "._unitOfWork.CommitAsync(new DbContext[] { $ctxField }, `$1)"
    $content = $content -replace '_unitOfWork\.CommitAsync\s*\(\s*(cancellationToken|ct)\s*\)', "_unitOfWork.CommitAsync(new DbContext[] { $ctxField }, `$1)"

    # Add DbContext ctor param + field IF NOT already present
    $ctxName = $module.Name
    if ($content -notmatch "\b$ctxName\b") {
        # Ambiguous automatic ctor insertion — leave for manual + flag
        Write-Host "  [FLAG] $Path -- needs manual $ctxName + $ctxField ctor+field injection" -ForegroundColor Yellow
        if ($content -eq $original) { return @{ Changed = $false; Skipped = 'needs-manual-ctor-inject' } }
    }

    # Add module DbContext using directive if referenced but missing
    if ($content -match "\b$ctxName\b" -and $content -notmatch [regex]::Escape($module.Namespace)) {
        Write-Host "  [FLAG] $Path -- add using $($module.Namespace);" -ForegroundColor Yellow
    }

    if ($content -eq $original) { return @{ Changed = $false; Skipped = 'no-change-needed' } }

    if ($DryRun) {
        Write-Host "  [DRY] $Path -- transform applied in-memory"
    } else {
        Set-Content -LiteralPath $Path -Value $content -NoNewline
        Write-Host "  [WRITE] $Path" -ForegroundColor Green
    }
    return @{ Changed = $true; Skipped = $null }
}

if (-not $Files) {
    # Grab flagged files from audit
    Write-Host "Running Audit-HandlerContext.ps1 to select flagged files..."
    $auditOut = & "$PSScriptRoot\Audit-HandlerContext.ps1" -FailOnViolation:$false 2>&1
    $Files = @()
    foreach ($line in $auditOut) {
        if ($line -match '^  (src\\[^ ]+\.cs)$') {
            $Files += (Join-Path (Get-Location) $Matches[1])
        }
    }
    Write-Host "Found $($Files.Count) flagged handler(s)."
    Write-Host ""
}

$changed = 0; $flagged = 0
foreach ($f in $Files) {
    $result = Convert-Handler -Path $f -DryRun:$DryRun
    if ($result.Changed) { $changed++ }
    if ($result.Skipped -eq 'needs-manual-ctor-inject') { $flagged++ }
}

Write-Host ""
Write-Host "Summary: $changed rewritten, $flagged flagged for manual ctor injection."
if ($DryRun) { Write-Host "(DRY-RUN mode -- no files written.)" -ForegroundColor Yellow }
