<#
.SYNOPSIS
  Rule 5j.4 handler-context audit -- enforces IMultiContextUnitOfWork usage in
  module command handlers whose repositories target a module DbContext.

.DESCRIPTION
  Fires from Day 7 onwards. Suspended during Days 2-6 per sprint plan.

  For every ICommandHandler<T> implementation under src/Modules/ and
  src/Products/, verifies that:

    1. The handler injects IMultiContextUnitOfWork (not IUnitOfWork).
    2. The handler injects the module DbContext type as a ctor parameter.
    3. The handler calls _unitOfWork.CommitAsync(new DbContext[] { ... }, ct)
       (the multi-context overload), NOT _unitOfWork.CommitAsync(ct).

  Fails RED with actionable file+line references. Exit code 1 on any violation.

.PARAMETER RepoRoot
  Repository root. Default: current directory.

.PARAMETER FailOnViolation
  Exit with code 1 if any violation found (default: true).
  Set -FailOnViolation:$false for report-only.

.PARAMETER ShowClean
  Print each file scanned even if clean.

.EXAMPLE
  # Day 7+ pre-push hook usage
  .\Audit-HandlerContext.ps1

.EXAMPLE
  # Report-only for sprint dry-runs
  .\Audit-HandlerContext.ps1 -FailOnViolation:$false -ShowClean

.NOTES
  Sprint bible: docs/MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md
  Rule memory:  feedback-rule-5j-4-handler-migration-audit
  Wave scope:   6.5.f.7 (LankaEvents ~120), 6.5.g (Payments 11), 6.5.h (Rule 5 14+7)
#>

param(
    [string]$RepoRoot = ".",
    [bool]$FailOnViolation = $true,
    [switch]$ShowClean
)

$ErrorActionPreference = "Stop"
$repoAbs = (Resolve-Path -LiteralPath $RepoRoot).Path

Write-Host "Rule 5j.4 Handler-Context Audit"
Write-Host "  Repo: $repoAbs"
Write-Host ""

# Scan roots -- only Modules and Products (AppDbContext PLAT handlers exempt)
$scanRoots = @(
    (Join-Path $repoAbs "src\Modules"),
    (Join-Path $repoAbs "src\Products")
) | Where-Object { Test-Path $_ }

if (-not $scanRoots) {
    Write-Warning "No Modules or Products directories under $repoAbs"
    exit 0
}

$violations = [System.Collections.Generic.List[psobject]]::new()
$scanned = 0

foreach ($root in $scanRoots) {
    $handlerFiles = Get-ChildItem -LiteralPath $root -Recurse -Filter *.cs -File |
        Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }

    foreach ($f in $handlerFiles) {
        $content = Get-Content -Raw -LiteralPath $f.FullName

        # Skip files that don't declare an ICommandHandler impl
        if ($content -notmatch 'ICommandHandler\s*<') { continue }

        $scanned++
        if ($ShowClean) { Write-Host "  scan: $($f.FullName)" -ForegroundColor DarkGray }

        $fileViolations = @()

        # Determine module category per Consult #7:
        #   MOD (module DbContext) = LankaEvents / Notifications / Media / Forms
        #   PLAT (AppDbContext)    = Identity / Payments / Communications / CulturalIntelligence / Scheduling
        # Rule 5j.4 only enforces on MOD modules. PLAT handlers legitimately use IUnitOfWork.
        $expectedContext = $null
        $isModModule = $false
        if ($f.FullName -match '\\Products\\LankaEvents\\')      { $expectedContext = 'LankaEventsDbContext'; $isModModule = $true }
        elseif ($f.FullName -match '\\Modules\\Notifications\\') { $expectedContext = 'NotificationsDbContext'; $isModModule = $true }
        elseif ($f.FullName -match '\\Modules\\Media\\')         { $expectedContext = 'MediaDbContext'; $isModModule = $true }
        elseif ($f.FullName -match '\\Modules\\Forms\\')         { $expectedContext = 'FormsDbContext'; $isModModule = $true }

        if (-not $isModModule) { continue }

        # Check 1: MOD handler injects IMultiContextUnitOfWork (not plain IUnitOfWork)
        if ($content -match '\bIUnitOfWork\b' -and $content -notmatch '\bIMultiContextUnitOfWork\b') {
            $fileViolations += "Injects IUnitOfWork instead of IMultiContextUnitOfWork (MOD module)"
        }

        # Check 2: MOD handler avoids single-context CommitAsync
        if ($content -match '\.CommitAsync\s*\(\s*(cancellationToken|ct)\s*\)') {
            $lineMatches = [regex]::Matches($content, '\.CommitAsync\s*\(\s*(cancellationToken|ct)\s*\)')
            foreach ($m in $lineMatches) {
                $lineNum = ($content.Substring(0, $m.Index) -split "`n").Count
                $fileViolations += "Line ${lineNum}: single-context CommitAsync - use multi-context overload"
            }
        }

        # Check 3: MOD handler injects its module DbContext type
        if ($content -notmatch "\b$expectedContext\b") {
            $fileViolations += "Missing $expectedContext ctor injection"
        }

        if ($fileViolations.Count -gt 0) {
            $violations.Add([pscustomobject]@{
                File   = $f.FullName.Substring($repoAbs.Length + 1)
                Issues = $fileViolations
            })
        }
    }
}

Write-Host ""
Write-Host "Scanned: $scanned command handler(s)"

if ($violations.Count -eq 0) {
    Write-Host "PASS - no Rule 5j.4 violations." -ForegroundColor Green
    exit 0
}

Write-Host ""
Write-Host "FAIL - $($violations.Count) handler(s) violate Rule 5j.4:" -ForegroundColor Red
foreach ($v in $violations) {
    Write-Host ""
    Write-Host "  $($v.File)" -ForegroundColor Yellow
    foreach ($i in $v.Issues) {
        Write-Host "    - $i" -ForegroundColor Red
    }
}
Write-Host ""
Write-Host "Reference: docs/MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md"

if ($FailOnViolation) { exit 1 } else { exit 0 }
