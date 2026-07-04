<#
.SYNOPSIS
  Bulk-move Day 2 helper: rewrite C# namespace declarations and using directives
  based on the source→target namespace map, after files have been git mv'd.

.DESCRIPTION
  Runs in two phases:

    Phase 1 (per-agent, on moved files):
      For every .cs file under -TargetRoot, rewrite the file's own
      `namespace X.Y.Z;` (or `namespace X.Y.Z { ... }`) declaration to match
      its NEW physical location based on -MapFile.

    Phase 2 (repo-wide, after all 6 agents finish):
      For every .cs file in the repository, rewrite `using X.Y.Z;` and
      fully-qualified `X.Y.Z.Type` references based on the old→new namespace map.

  Idempotent: running twice on the same file is a no-op.

  Preserves git blame: does NOT git mv. Files must already be at their target
  location before calling this script.

.PARAMETER Phase
  1 = rewrite namespace declarations in moved files
  2 = rewrite using directives + qualified refs repo-wide

.PARAMETER TargetRoot
  Phase 1 only: root directory containing moved files (e.g. src/Modules/Communications/).

.PARAMETER MapFile
  Path to newline-delimited "old-ns => new-ns" map file. See docs/sprint/namespace-map.txt.
  Example line: LankaConnect.Domain.Business => LankaConnect.Modules.Business.Domain

.PARAMETER RepoRoot
  Phase 2 only: repository root (default: current directory).

.PARAMETER DryRun
  If set, prints intended changes without writing files.

.EXAMPLE
  # Phase 1 — Agent A rewrites Domain namespaces after git mv
  .\Rewrite-Namespaces.ps1 -Phase 1 -TargetRoot ..\..\src\Modules\Communications\Communications.Domain -MapFile ..\..\docs\sprint\namespace-map.txt

.EXAMPLE
  # Phase 2 — repo-wide using rewrite (Day 3 morning)
  .\Rewrite-Namespaces.ps1 -Phase 2 -MapFile ..\..\docs\sprint\namespace-map.txt -RepoRoot ..\..\

.NOTES
  Sprint bible: docs/MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md
  Manifest:     docs/sprint/bulk-move-manifest.md
  Author:       Claude (Day 0 prep, 2026-07-04)
#>

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet(1, 2)]
    [int]$Phase,

    [string]$TargetRoot,

    [Parameter(Mandatory=$true)]
    [string]$MapFile,

    [string]$RepoRoot = ".",

    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Read-NamespaceMap {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        throw "Namespace map file not found: $Path"
    }

    $map = @{}
    Get-Content $Path | ForEach-Object {
        $line = $_.Trim()
        if ($line -eq "" -or $line.StartsWith("#")) { return }

        $parts = $line -split '\s*=>\s*'
        if ($parts.Count -ne 2) {
            Write-Warning "Skipping malformed map line: $line"
            return
        }
        $map[$parts[0].Trim()] = $parts[1].Trim()
    }

    Write-Host "Loaded $($map.Count) namespace mappings from $Path"
    return $map
}

function Get-TargetNamespace {
    param(
        [string]$FilePath,
        [string]$TargetRootAbs
    )

    # Derive the namespace from the file's path relative to TargetRoot.
    # Convention: <TargetRootLeafName> becomes the last segment of the base namespace.
    # E.g. TargetRoot = C:\Work\LankaConnect\src\Modules\Communications\Communications.Domain
    #      File       = C:\...\Communications.Domain\Auth\LoginHandler.cs
    #      → namespace LankaConnect.Modules.Communications.Domain.Auth

    $fileDir = [System.IO.Path]::GetDirectoryName($FilePath)
    # Manual relative path (Path.GetRelativePath is not in Windows PowerShell 5.1 / .NET Framework)
    $normRoot = $TargetRootAbs.TrimEnd('\','/').Replace('/', '\')
    $normDir  = $fileDir.TrimEnd('\','/').Replace('/', '\')
    if ($normDir -eq $normRoot) {
        $rel = ""
    }
    elseif ($normDir.StartsWith($normRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        $rel = $normDir.Substring($normRoot.Length + 1)
    }
    else {
        throw "File $FilePath is not under TargetRoot $TargetRootAbs"
    }
    $rel = $rel.Replace('\', '.').Replace('/', '.')

    # Base namespace convention derived from TargetRoot path components:
    #   src\Modules\Communications\Communications.Domain => LankaConnect.Modules.Communications.Domain
    #   src\Products\LankaEvents\LankaEvents.Domain      => LankaConnect.Products.LankaEvents.Domain
    #   src\BuildingBlocks\BuildingBlocks.Domain         => LankaConnect.BuildingBlocks.Domain
    #   src\SharedKernel\SharedKernel.Cultural           => LankaConnect.SharedKernel.Cultural

    $tokens = $TargetRootAbs -split '[\\/]'
    $srcIdx = [Array]::IndexOf($tokens, 'src')
    if ($srcIdx -lt 0) {
        throw "TargetRoot must be inside a 'src' directory. Got: $TargetRootAbs"
    }

    # Layer folder = tokens[srcIdx+1] (Modules/Products/BuildingBlocks/SharedKernel/Hosts)
    # Then project folder = last token
    $layer = $tokens[$srcIdx + 1]
    $projectLeaf = $tokens[-1]  # e.g. "Communications.Domain"

    switch ($layer) {
        'Modules'         { $base = "LankaConnect.Modules.$projectLeaf" }
        'Products'        { $base = "LankaConnect.Products.$projectLeaf" }
        'BuildingBlocks'  { $base = "LankaConnect.$projectLeaf" }  # BuildingBlocks.Domain → LankaConnect.BuildingBlocks.Domain via projectLeaf already containing prefix
        'SharedKernel'    { $base = "LankaConnect.$projectLeaf" }
        'Hosts'           { $base = "LankaConnect.$projectLeaf" }
        default           { $base = "LankaConnect.$projectLeaf" }
    }

    if ($rel -eq "" -or $rel -eq ".") { return $base }
    return "$base.$rel"
}

function Update-FileNamespace {
    param(
        [string]$FilePath,
        [string]$NewNamespace,
        [switch]$DryRun
    )

    $content = Get-Content -Raw -LiteralPath $FilePath
    $original = $content

    # File-scoped namespace: `namespace X.Y.Z;`
    $content = [regex]::Replace($content, '(?m)^\s*namespace\s+[^\s;{]+\s*;', "namespace $NewNamespace;")

    # Block-scoped namespace: `namespace X.Y.Z {`
    $content = [regex]::Replace($content, '(?m)^\s*namespace\s+[^\s;{]+\s*\{', "namespace $NewNamespace {")

    if ($content -eq $original) { return $false }

    if ($DryRun) {
        Write-Host "  [DRY] $FilePath -> namespace $NewNamespace"
    } else {
        Set-Content -LiteralPath $FilePath -Value $content -NoNewline
    }
    return $true
}

function Update-Usings {
    param(
        [string]$FilePath,
        [hashtable]$Map,
        [switch]$DryRun
    )

    $content = Get-Content -Raw -LiteralPath $FilePath
    $original = $content
    $changes = 0

    foreach ($old in $Map.Keys) {
        $new = $Map[$old]
        # Escape dots for regex
        $oldEsc = [regex]::Escape($old)

        # `using X.Y.Z;` — must match exact segment boundaries
        $pattern = "(?m)^(\s*using\s+)$oldEsc(\s*;)"
        $newContent = [regex]::Replace($content, $pattern, "`${1}$new`${2}")
        if ($newContent -ne $content) { $changes++; $content = $newContent }

        # `using X.Y.Z.Sub;` — namespace prefix followed by more segments
        $patternPrefix = "(?m)^(\s*using\s+)$oldEsc(\.)"
        $newContent = [regex]::Replace($content, $patternPrefix, "`${1}$new`${2}")
        if ($newContent -ne $content) { $changes++; $content = $newContent }

        # Fully-qualified: `X.Y.Z.Foo(...)` outside using directives
        # Match on word-boundary before, and a . or other token after (not another identifier char)
        $patternFq = "(?<![\w\.])$oldEsc(\.)"
        $newContent = [regex]::Replace($content, $patternFq, "$new`${1}")
        if ($newContent -ne $content) { $changes++; $content = $newContent }
    }

    if ($content -eq $original) { return 0 }

    if ($DryRun) {
        Write-Host "  [DRY] $FilePath -> $changes using/ref rewrites"
    } else {
        Set-Content -LiteralPath $FilePath -Value $content -NoNewline
    }
    return $changes
}

# --- Main ---

$map = Read-NamespaceMap -Path $MapFile

if ($Phase -eq 1) {
    if (-not $TargetRoot) { throw "Phase 1 requires -TargetRoot" }
    $targetAbs = (Resolve-Path -LiteralPath $TargetRoot).Path
    Write-Host "Phase 1: rewriting namespace declarations under $targetAbs"

    $files = Get-ChildItem -LiteralPath $targetAbs -Recurse -Filter *.cs -File |
             Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }

    $updated = 0
    foreach ($f in $files) {
        $newNs = Get-TargetNamespace -FilePath $f.FullName -TargetRootAbs $targetAbs
        if (Update-FileNamespace -FilePath $f.FullName -NewNamespace $newNs -DryRun:$DryRun) {
            $updated++
        }
    }
    Write-Host "Phase 1 complete: $updated / $($files.Count) files updated"
}
elseif ($Phase -eq 2) {
    $repoAbs = (Resolve-Path -LiteralPath $RepoRoot).Path
    Write-Host "Phase 2: rewriting using directives repo-wide under $repoAbs"

    $files = Get-ChildItem -LiteralPath $repoAbs -Recurse -Filter *.cs -File |
             Where-Object { $_.FullName -notmatch '\\(obj|bin|\.git)\\' }

    $totalFiles = 0
    $totalChanges = 0
    foreach ($f in $files) {
        $n = Update-Usings -FilePath $f.FullName -Map $map -DryRun:$DryRun
        if ($n -gt 0) { $totalFiles++; $totalChanges += $n }
    }
    Write-Host "Phase 2 complete: $totalChanges rewrites across $totalFiles files"
}
