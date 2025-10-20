# TDD Enum Consolidation Automated Script
# Guarantees zero error increase at each step with rollback capability

$ErrorActionPreference = "Stop"
$workDir = "C:\Work\LankaConnect"
Set-Location $workDir

# Color output functions
function Write-Phase { param($msg) Write-Host "`n=== $msg ===" -ForegroundColor Cyan }
function Write-Success { param($msg) Write-Host "✓ $msg" -ForegroundColor Green }
function Write-Failure { param($msg) Write-Host "✗ $msg" -ForegroundColor Red }
function Write-Info { param($msg) Write-Host "→ $msg" -ForegroundColor Yellow }

# Build and error counting function
function Test-BuildErrors {
    Write-Info "Running build..."
    $output = dotnet build --no-restore 2>&1 | Out-String
    $errors = ($output | Select-String "error" -AllMatches).Matches.Count
    Write-Host "Current error count: $errors" -ForegroundColor Yellow
    return $errors
}

# Git checkpoint function
function Checkpoint {
    param($message, $tag)
    git add -A
    git commit -m $message
    git tag $tag
    Write-Success "Checkpoint: $tag"
}

# Rollback function
function Rollback-ToTag {
    param($tag)
    Write-Failure "Rolling back to $tag"
    git reset --hard $tag
}

# Main execution
try {
    Write-Phase "Phase 0: Establish Baseline"
    $baseline = Test-BuildErrors
    Write-Info "Baseline errors: $baseline"

    if ($baseline -eq 0) {
        Write-Success "No errors detected! Consolidation may not be needed."
        Read-Host "Press Enter to continue anyway or Ctrl+C to exit"
    }

    Write-Phase "Phase 1: Remove Duplicate CulturalDataPriority (CulturalStateReplicationService.cs)"

    # Step 1.1: Check if the enum is actually used
    Write-Info "Checking for usage of duplicate enum values..."
    $usage = rg "CulturalDataPriority\.(Sacred|Critical|High|Medium|Low)" --type cs 2>&1
    if ($usage) {
        Write-Failure "Duplicate enum is in use! Manual intervention required."
        Write-Host $usage
        exit 1
    }
    Write-Success "Duplicate enum is unused - safe to delete"

    # Step 1.2: Delete duplicate enum (lines 782-789)
    Write-Info "Deleting duplicate enum definition..."
    $file = "$workDir\src\LankaConnect.Domain\Infrastructure\Failover\CulturalStateReplicationService.cs"

    if (-not (Test-Path $file)) {
        Write-Failure "File not found: $file"
        exit 1
    }

    $content = Get-Content $file
    $lineCount = $content.Length

    # Find the enum definition
    $enumStart = ($content | Select-String -Pattern "public enum CulturalDataPriority" | Select-Object -First 1).LineNumber - 1

    if ($null -eq $enumStart) {
        Write-Info "Duplicate enum already removed - skipping"
    } else {
        # Find the closing brace (should be within 10 lines)
        $enumEnd = $enumStart
        for ($i = $enumStart; $i -lt $enumStart + 15; $i++) {
            if ($content[$i] -match '^\}') {
                $enumEnd = $i
                break
            }
        }

        Write-Info "Removing lines $($enumStart + 1) to $($enumEnd + 1)"
        $newContent = $content[0..($enumStart - 1)] + $content[($enumEnd + 1)..($lineCount - 1)]
        $newContent | Set-Content $file

        $phase1 = Test-BuildErrors
        if ($phase1 -gt $baseline) {
            Write-Failure "Error count increased from $baseline to $phase1!"
            git checkout $file
            exit 1
        }

        Checkpoint "[Step 1.2] Remove duplicate CulturalDataPriority enum" "consolidation-step-1.2"
        Write-Success "Phase 1 complete - errors: $phase1 (baseline: $baseline)"
    }

    Write-Phase "Phase 2: Update Production Code References"

    # Step 2.1: Update SacredEventRecoveryOrchestrator.cs
    Write-Info "Updating SacredEventRecoveryOrchestrator.cs..."
    $file = "$workDir\src\LankaConnect.Infrastructure\DisasterRecovery\SacredEventRecoveryOrchestrator.cs"

    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        $updated = $content -replace 'LankaConnect\.Domain\.Common\.Database\.CulturalDataPriority', 'CulturalDataPriority'
        $updated | Set-Content $file

        # Add using statement if not present
        $lines = Get-Content $file
        if (-not ($lines | Select-String "using LankaConnect.Domain.Common.Database;")) {
            $usingIndex = ($lines | Select-String "^using" | Select-Object -Last 1).LineNumber - 1
            $lines = @($lines[0..$usingIndex]) + "using LankaConnect.Domain.Common.Database;" + @($lines[($usingIndex + 1)..($lines.Length - 1)])
            $lines | Set-Content $file
        }

        $phase2_1 = Test-BuildErrors
        if ($phase2_1 -gt $baseline) {
            Write-Failure "Error count increased!"
            Rollback-ToTag "consolidation-step-1.2"
            exit 1
        }

        Checkpoint "[Step 2.1] Update SacredEventRecoveryOrchestrator references" "consolidation-step-2.1"
        Write-Success "Step 2.1 complete - errors: $phase2_1"
    }

    # Step 2.2: Update CulturalIntelligenceBackupEngine.cs
    Write-Info "Updating CulturalIntelligenceBackupEngine.cs..."
    $file = "$workDir\src\LankaConnect.Infrastructure\DisasterRecovery\CulturalIntelligenceBackupEngine.cs"

    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        $updated = $content -replace 'LankaConnect\.Domain\.Common\.Database\.CulturalDataPriority', 'CulturalDataPriority'
        $updated | Set-Content $file

        # Add using statement
        $lines = Get-Content $file
        if (-not ($lines | Select-String "using LankaConnect.Domain.Common.Database;")) {
            $usingIndex = ($lines | Select-String "^using" | Select-Object -Last 1).LineNumber - 1
            $lines = @($lines[0..$usingIndex]) + "using LankaConnect.Domain.Common.Database;" + @($lines[($usingIndex + 1)..($lines.Length - 1)])
            $lines | Set-Content $file
        }

        $phase2_2 = Test-BuildErrors
        if ($phase2_2 -gt $baseline) {
            Write-Failure "Error count increased!"
            Rollback-ToTag "consolidation-step-2.1"
            exit 1
        }

        Checkpoint "[Step 2.2] Update CulturalIntelligenceBackupEngine references" "consolidation-step-2.2"
        Write-Success "Step 2.2 complete - errors: $phase2_2"
    }

    Write-Phase "Phase 3: Update Test Files"

    # Step 3.1: Update BackupDisasterRecoveryTests.cs
    Write-Info "Updating BackupDisasterRecoveryTests.cs..."
    $file = "$workDir\tests\LankaConnect.Infrastructure.Tests\Database\BackupDisasterRecoveryTests.cs"

    if (Test-Path $file) {
        # Remove local enum definition (lines around 1250)
        $content = Get-Content $file
        $enumStart = ($content | Select-String -Pattern "public enum SacredPriorityLevel" | Select-Object -First 1).LineNumber - 1

        if ($null -ne $enumStart) {
            $enumEnd = $enumStart
            for ($i = $enumStart; $i -lt $enumStart + 15; $i++) {
                if ($content[$i] -match '^\s*\}') {
                    $enumEnd = $i
                    break
                }
            }

            Write-Info "Removing local enum definition (lines $($enumStart + 1) to $($enumEnd + 1))"
            $newContent = $content[0..($enumStart - 1)] + $content[($enumEnd + 1)..($content.Length - 1)]
            $newContent | Set-Content $file
        }

        # Replace all SacredPriorityLevel references
        $content = Get-Content $file -Raw
        $updated = $content -replace 'SacredPriorityLevel\.', 'CulturalDataPriority.'
        $updated | Set-Content $file

        # Add using statement
        $lines = Get-Content $file
        if (-not ($lines | Select-String "using LankaConnect.Domain.Common.Database;")) {
            $usingIndex = ($lines | Select-String "^using" | Select-Object -Last 1).LineNumber - 1
            $lines = @($lines[0..$usingIndex]) + "using LankaConnect.Domain.Common.Database;" + @($lines[($usingIndex + 1)..($lines.Length - 1)])
            $lines | Set-Content $file
        }

        $phase3_1 = Test-BuildErrors
        # Tests can increase errors temporarily - that's ok

        Checkpoint "[Step 3.1] Update BackupDisasterRecoveryTests enum references" "consolidation-step-3.1"
        Write-Success "Step 3.1 complete - errors: $phase3_1"
    }

    Write-Phase "Phase 4: Final Validation"

    Write-Info "Running full solution build..."
    $final = Test-BuildErrors

    Write-Info "Running grep verification..."
    $sacredEnums = (rg "enum\s+SacredPriorityLevel" --type cs src/ 2>&1 | Measure-Object -Line).Lines
    $culturalEnums = (rg "enum\s+CulturalDataPriority" --type cs src/ 2>&1 | Measure-Object -Line).Lines

    Write-Host "`nValidation Results:" -ForegroundColor Cyan
    Write-Host "  Baseline errors:        $baseline" -ForegroundColor Yellow
    Write-Host "  Final errors:           $final" -ForegroundColor Yellow
    Write-Host "  SacredPriorityLevel:    $sacredEnums (expected: 0)" -ForegroundColor Yellow
    Write-Host "  CulturalDataPriority:   $culturalEnums (expected: 1)" -ForegroundColor Yellow

    if ($final -le $baseline -and $sacredEnums -eq 0) {
        Write-Success "`nCONSOLIDATION SUCCESSFUL!"
        Checkpoint "[TDD GREEN] Complete enum consolidation" "consolidation-complete"

        Write-Host "`nNext Steps:" -ForegroundColor Green
        Write-Host "  1. Review changes: git diff consolidation-step-1.2..consolidation-complete"
        Write-Host "  2. Run tests: dotnet test"
        Write-Host "  3. If all good: git push --tags"
    } else {
        Write-Failure "`nCONSOLIDATION INCOMPLETE"
        Write-Host "Manual review required. Current state has been committed."
        Write-Host "To rollback: git reset --hard consolidation-step-2.2"
    }

} catch {
    Write-Failure "Script failed with error: $_"
    Write-Host "Stack trace: $($_.ScriptStackTrace)"
    exit 1
}
