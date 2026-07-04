<#
.SYNOPSIS
  Sprint Day 2 bulk-move executor. Runs per-agent inside each worktree.

.DESCRIPTION
  Executes the bulk-move manifest for one agent (A..F). Each agent owns
  an exclusive path set per docs/sprint/bulk-move-manifest.md.

  Steps:
    1. Assert we're in the correct worktree branch (bulk-move/agent-X)
    2. For each mapping (source-folder -> target-folder):
        a. mkdir -p target
        b. For each .cs file in source:
             - If manifest marks DELETE: git rm
             - Else: git mv source-file target/relative-file
    3. Run Rewrite-Namespaces.ps1 Phase 1 to fix namespaces of moved files
    4. Commit "Day 2 agent-X bulk move -- LEAVE BROKEN, compile fixes Day 3"
    5. Push to bulk-move/agent-X

  NO cross-agent coordination. NO merge. LEAVE BROKEN. Day 3 fixes compile.

.PARAMETER Agent
  Which agent letter to execute (A..F). Required.

.PARAMETER DryRun
  Print git mv/rm commands without executing.

.EXAMPLE
  # From inside C:\Work\lc-bulk-move-A\
  .\scripts\sprint\Day2-BulkMove.ps1 -Agent A

.NOTES
  Sprint bible:   docs/MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md
  Manifest:       docs/sprint/bulk-move-manifest.md
  Discipline:     ONLY git mv (never rm+add). DELETE lines go via git rm.
#>

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('A','B','C','D','E','F')]
    [string]$Agent,

    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Invoke-GitMv {
    param([string]$Src, [string]$Dst, [switch]$DryRun)
    if (-not (Test-Path $Src)) {
        Write-Host "  [SKIP] source missing: $Src" -ForegroundColor DarkGray
        return
    }
    $dstDir = Split-Path -Parent $Dst
    if ($dstDir -and -not (Test-Path $dstDir)) {
        if ($DryRun) { Write-Host "  [DRY] mkdir $dstDir" }
        else { New-Item -Path $dstDir -ItemType Directory -Force | Out-Null }
    }
    if ($DryRun) {
        Write-Host "  [DRY] git mv $Src $Dst"
    } else {
        git mv --force $Src $Dst 2>&1 | Out-Null
    }
}

function Invoke-GitRm {
    param([string]$Path, [switch]$DryRun)
    if (-not (Test-Path $Path)) {
        Write-Host "  [SKIP] rm target missing: $Path" -ForegroundColor DarkGray
        return
    }
    if ($DryRun) {
        Write-Host "  [DRY] git rm -r $Path"
    } else {
        git rm -r $Path 2>&1 | Out-Null
    }
}

function Move-FolderRecursive {
    param([string]$SrcFolder, [string]$DstFolder, [switch]$DryRun)
    if (-not (Test-Path $SrcFolder)) {
        Write-Host "  [SKIP] source folder missing: $SrcFolder" -ForegroundColor DarkGray
        return
    }
    $files = Get-ChildItem -LiteralPath $SrcFolder -Recurse -Filter *.cs -File |
             Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }
    foreach ($f in $files) {
        $rel = $f.FullName.Substring((Resolve-Path $SrcFolder).Path.Length).TrimStart('\','/')
        $dst = Join-Path $DstFolder $rel
        Invoke-GitMv -Src $f.FullName -Dst $dst -DryRun:$DryRun
    }
}

# --- Verify we're on the right branch ---
$expected = "bulk-move/agent-$Agent"
$actual   = (git branch --show-current).Trim()
if ($actual -ne $expected) {
    throw "Expected branch $expected, on $actual. Run from correct worktree."
}
Write-Host "=== Day 2 Bulk-Move: Agent $Agent (branch $actual) ===" -ForegroundColor Cyan
Write-Host ""

switch ($Agent) {
    'A' {
        Write-Host "AGENT A: Domain move (311 files) + Delete over-engineered (~93 files)"

        # DELETE dead over-engineered types (per audit 2026-07-04: zero source refs)
        Write-Host "-- DELETE dead over-engineered types --" -ForegroundColor Yellow
        Invoke-GitRm -Path "src/LankaConnect.Domain/Common/Database"        -DryRun:$DryRun
        Invoke-GitRm -Path "src/LankaConnect.Domain/Common/DisasterRecovery" -DryRun:$DryRun
        Invoke-GitRm -Path "src/LankaConnect.Domain/Common/Monitoring"       -DryRun:$DryRun
        Invoke-GitRm -Path "src/LankaConnect.Domain/Common/Performance"      -DryRun:$DryRun
        Invoke-GitRm -Path "src/LankaConnect.Domain/Common/Privacy"          -DryRun:$DryRun
        Invoke-GitRm -Path "src/LankaConnect.Domain/Common/Recovery"         -DryRun:$DryRun
        Invoke-GitRm -Path "src/LankaConnect.Domain/Common/Security"         -DryRun:$DryRun
        Invoke-GitRm -Path "src/LankaConnect.Domain/Common/Notifications"    -DryRun:$DryRun
        Invoke-GitRm -Path "src/LankaConnect.Domain/Enterprise"              -DryRun:$DryRun
        Invoke-GitRm -Path "src/LankaConnect.Domain/Infrastructure"          -DryRun:$DryRun

        # Move survivors
        Write-Host "-- MOVE Domain survivors --" -ForegroundColor Green
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Domain/Analytics"              -DstFolder "src/Products/LankaEvents/LankaEvents.Domain/Analytics"              -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Domain/Badges"                 -DstFolder "src/Products/LankaEvents/LankaEvents.Domain/Badges"                 -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Domain/Billing"                -DstFolder "src/Modules/Payments/Payments.Domain/Billing"                       -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Domain/Business"               -DstFolder "src/LankaConnect.Legacy/Domain/Business"                            -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Domain/Communications"         -DstFolder "src/Modules/Communications/Communications.Domain"                   -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Domain/Community"              -DstFolder "src/Modules/Communications/Communications.Domain/Community"        -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Domain/CulturalIntelligence"   -DstFolder "src/Modules/CulturalIntelligence/CulturalIntelligence.Domain"       -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Domain/ReferenceData"          -DstFolder "src/SharedKernel/SharedKernel.Cultural/ReferenceData"               -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Domain/Shared"                 -DstFolder "src/BuildingBlocks/BuildingBlocks.Domain/Shared"                    -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Domain/Support"                -DstFolder "src/Modules/Communications/Communications.Domain/Support"           -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Domain/Tax"                    -DstFolder "src/Modules/Payments/Payments.Domain/Tax"                           -DryRun:$DryRun

        # Common survivors -> BuildingBlocks.Domain
        Write-Host "-- MOVE Common survivors -> BuildingBlocks.Domain --" -ForegroundColor Green
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Domain/Common"                 -DstFolder "src/BuildingBlocks/BuildingBlocks.Domain"                           -DryRun:$DryRun
    }
    'B' {
        Write-Host "AGENT B: Application move (400 files)"
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Application/Analytics"           -DstFolder "src/Products/LankaEvents/LankaEvents.Application/Analytics"          -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Application/Auth"                -DstFolder "src/Modules/Identity/Identity.Application/Auth"                      -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Application/Badges"              -DstFolder "src/Products/LankaEvents/LankaEvents.Application/Badges"             -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Application/Billing"             -DstFolder "src/Modules/Payments/Payments.Application/Billing"                   -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Application/Businesses"          -DstFolder "src/LankaConnect.Legacy/Application/Businesses"                      -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Application/Common"              -DstFolder "src/BuildingBlocks/BuildingBlocks.Application/Common"                -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Application/Communications"      -DstFolder "src/Modules/Communications/Communications.Application"               -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Application/Contact"             -DstFolder "src/Modules/Communications/Communications.Application/Contact"       -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Application/CulturalIntelligence" -DstFolder "src/Modules/CulturalIntelligence/CulturalIntelligence.Application"  -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Application/Dashboard"           -DstFolder "src/Hosts/Host.AllInOne/Dashboard"                                   -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Application/Interfaces"          -DstFolder "src/BuildingBlocks/BuildingBlocks.Application/Interfaces"            -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Application/MetroAreas"          -DstFolder "src/SharedKernel/SharedKernel.Geo/MetroAreas"                        -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Application/ReferenceData"       -DstFolder "src/SharedKernel/SharedKernel.Cultural/ReferenceData"                -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Application/Support"             -DstFolder "src/Modules/Communications/Communications.Application/Support"       -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Application/Users"               -DstFolder "src/Modules/Identity/Identity.Application/Users"                     -DryRun:$DryRun
        Invoke-GitMv -Src "src/LankaConnect.Application/DependencyInjection.cs" -Dst "src/Hosts/Host.AllInOne/LegacyApplicationDependencyInjection.cs" -DryRun:$DryRun
        Invoke-GitMv -Src "src/LankaConnect.Application/GlobalUsings.cs"        -Dst "src/Hosts/Host.AllInOne/LegacyApplicationGlobalUsings.cs"        -DryRun:$DryRun
    }
    'C' {
        Write-Host "AGENT C: Infrastructure non-EF move (~200 files) + Delete over-engineered (~6 files)"

        # DELETE dead types
        Write-Host "-- DELETE dead over-engineered types --" -ForegroundColor Yellow
        Invoke-GitRm -Path "src/LankaConnect.Infrastructure/DisasterRecovery" -DryRun:$DryRun
        Invoke-GitRm -Path "src/LankaConnect.Infrastructure/Monitoring"      -DryRun:$DryRun
        Invoke-GitRm -Path "src/LankaConnect.Infrastructure/Security"        -DryRun:$DryRun

        # Move survivors
        Write-Host "-- MOVE Infrastructure survivors --" -ForegroundColor Green
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Infrastructure/BackgroundServices" -DstFolder "src/Hosts/Host.AllInOne/BackgroundServices"                       -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Infrastructure/Common"             -DstFolder "src/BuildingBlocks/BuildingBlocks.Infrastructure/Common"           -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Infrastructure/Database"           -DstFolder "src/BuildingBlocks/BuildingBlocks.Infrastructure/Database"         -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Infrastructure/Email"              -DstFolder "src/Modules/Communications/Communications.Infrastructure/Email"   -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Infrastructure/Events"             -DstFolder "src/Products/LankaEvents/LankaEvents.Infrastructure/EventServices" -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Infrastructure/Helpers"            -DstFolder "src/BuildingBlocks/BuildingBlocks.Infrastructure/Helpers"          -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Infrastructure/Outbox"             -DstFolder "src/BuildingBlocks/BuildingBlocks.Infrastructure/Outbox"           -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Infrastructure/Payments"           -DstFolder "src/Modules/Payments/Payments.Infrastructure"                     -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Infrastructure/Services"           -DstFolder "src/Hosts/Host.AllInOne/Services"                                 -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Infrastructure/Storage"            -DstFolder "src/Modules/Media/Media.Infrastructure/Storage"                   -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Infrastructure/Templates"          -DstFolder "src/Modules/Communications/Communications.Infrastructure/Templates" -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Infrastructure/WhatsApp"           -DstFolder "src/Modules/Communications/Communications.Infrastructure/WhatsApp" -DryRun:$DryRun
        Invoke-GitMv -Src "src/LankaConnect.Infrastructure/DependencyInjection.cs" -Dst "src/Hosts/Host.AllInOne/LegacyInfrastructureDependencyInjection.cs" -DryRun:$DryRun
        Invoke-GitMv -Src "src/LankaConnect.Infrastructure/GlobalUsings.cs"        -Dst "src/Hosts/Host.AllInOne/LegacyInfrastructureGlobalUsings.cs"        -DryRun:$DryRun
    }
    'D' {
        Write-Host "AGENT D: Infrastructure Data folder (~464 files) -- Configurations relocate, Migrations STAY"
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Infrastructure/Data/Interceptors" -DstFolder "src/BuildingBlocks/BuildingBlocks.Infrastructure/Data/Interceptors" -DryRun:$DryRun

        # AppDbContext + UnitOfWork STAY per manifest; will relocate Day 10
        Write-Host "-- AppDbContext.cs + UnitOfWork.cs STAY (relocated Day 10 per manifest) --" -ForegroundColor Yellow
        Write-Host "-- Data/Migrations/ STAYS (repatriated Day 5 per manifest) --"                -ForegroundColor Yellow
        Write-Host "-- Data/Configurations/ + Data/Repositories/ REQUIRE PER-ENTITY split (Day 3 architect co-op)" -ForegroundColor Yellow
        Write-Host "     Not automated in this script -- Agent D reads manifest per-entity table" -ForegroundColor Yellow
    }
    'E' {
        Write-Host "AGENT E: API controllers move (53 files)"
        $lankaEventsCtl = @('AddOnsController','AnalyticsController','ApprovalsController','BadgesController','CollectionsController','DonationsController','EventConfigController','EventTemplatesController','EventsController','SeatingMetricsController','SponsorsController','SponsorshipPackagesController','VenueLayoutsController')
        $identityCtl    = @('AdminUsersController','AuthController','UsersController')
        $commsCtl       = @('AdminEmailTemplatesController','ContactController','EmailController','EmailGroupsController','EmailMetricsController','NewsletterController','NewslettersController','WhatsAppAdminController','WhatsAppController','WhatsAppWebhookController','AdminSupportTicketsController')
        $paymentsCtl    = @('PaymentsController','RefundReconciliationController')
        $mediaCtl       = @('PhotoAlbumsController','ContentController')
        $hostCtl        = @('AdminController','AdminRecoveryController','ConfigurationController','DashboardController','DiagnosticsController','HealthController','MetroAreasController','PublicController','ReferenceDataController','TestController','BaseController')
        $legacyCtl      = @('BusinessesController')

        foreach ($c in $lankaEventsCtl) { Invoke-GitMv -Src "src/LankaConnect.API/Controllers/$c.cs" -Dst "src/Products/LankaEvents/LankaEvents.Api/Controllers/$c.cs" -DryRun:$DryRun }
        foreach ($c in $identityCtl)    { Invoke-GitMv -Src "src/LankaConnect.API/Controllers/$c.cs" -Dst "src/Modules/Identity/Identity.Api/Controllers/$c.cs"       -DryRun:$DryRun }
        foreach ($c in $commsCtl)       { Invoke-GitMv -Src "src/LankaConnect.API/Controllers/$c.cs" -Dst "src/Modules/Communications/Communications.Api/Controllers/$c.cs" -DryRun:$DryRun }
        foreach ($c in $paymentsCtl)    { Invoke-GitMv -Src "src/LankaConnect.API/Controllers/$c.cs" -Dst "src/Modules/Payments/Payments.Api/Controllers/$c.cs"        -DryRun:$DryRun }
        foreach ($c in $mediaCtl)       { Invoke-GitMv -Src "src/LankaConnect.API/Controllers/$c.cs" -Dst "src/Modules/Media/Media.Api/Controllers/$c.cs"              -DryRun:$DryRun }
        foreach ($c in $hostCtl)        { Invoke-GitMv -Src "src/LankaConnect.API/Controllers/$c.cs" -Dst "src/Hosts/Host.AllInOne/Controllers/$c.cs"                  -DryRun:$DryRun }
        foreach ($c in $legacyCtl)      { Invoke-GitMv -Src "src/LankaConnect.API/Controllers/$c.cs" -Dst "src/Hosts/Host.AllInOne/Controllers/Legacy/$c.cs"           -DryRun:$DryRun }
        Write-Host "-- Program.cs + Startup + appsettings STAY (rename API to Host.AllInOne on Day 10) --" -ForegroundColor Yellow
    }
    'F' {
        Write-Host "AGENT F: Shared + root move (~67 files)"
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Shared/Email"    -DstFolder "src/Modules/Communications/Communications.Contracts/Email"    -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect.Shared/WhatsApp" -DstFolder "src/Modules/Communications/Communications.Contracts/WhatsApp" -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect/Application"     -DstFolder "src/BuildingBlocks/BuildingBlocks.Application/RootLegacy"      -DryRun:$DryRun
        Move-FolderRecursive -SrcFolder "src/LankaConnect/Domain"          -DstFolder "src/BuildingBlocks/BuildingBlocks.Domain/RootLegacy"           -DryRun:$DryRun
    }
}

Write-Host ""
Write-Host "=== Bulk move complete for agent $Agent ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next: run Rewrite-Namespaces.ps1 Phase 1 on the target root(s)."
Write-Host "Then: git commit -m 'Day 2 bulk-move agent-$Agent -- LEAVE BROKEN, compile fixes Day 3'"
Write-Host "Then: git push origin bulk-move/agent-$Agent"
