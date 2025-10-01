#!/usr/bin/env pwsh
# Systematic Duplicate Type Detection Script
# Part of Zero Tolerance TDD Architecture

Write-Host "=== LankaConnect Duplicate Type Detection ===" -ForegroundColor Cyan
Write-Host "Current Status: 631 compilation errors (down from 1,278)" -ForegroundColor Green
Write-Host ""

# Function to find duplicate interfaces
function Find-DuplicateInterfaces {
    Write-Host "1. DUPLICATE INTERFACES:" -ForegroundColor Yellow
    $interfaces = rg "^public interface\s+(\w+)" src/ --type cs -o -r '$1' | Sort-Object | Group-Object | Where-Object { $_.Count -gt 1 }

    foreach ($interface in $interfaces) {
        Write-Host "   ❌ $($interface.Name) found $($interface.Count) times" -ForegroundColor Red
        rg "^public interface\s+$($interface.Name)" src/ --type cs -n | ForEach-Object {
            Write-Host "      📁 $_" -ForegroundColor Gray
        }
        Write-Host ""
    }

    return $interfaces.Count
}

# Function to find duplicate classes
function Find-DuplicateClasses {
    Write-Host "2. DUPLICATE CLASSES:" -ForegroundColor Yellow
    $classes = rg "^public class\s+(\w+)" src/ --type cs -o -r '$1' | Sort-Object | Group-Object | Where-Object { $_.Count -gt 1 }

    foreach ($class in $classes) {
        Write-Host "   ❌ $($class.Name) found $($class.Count) times" -ForegroundColor Red
        rg "^public class\s+$($class.Name)" src/ --type cs -n | ForEach-Object {
            Write-Host "      📁 $_" -ForegroundColor Gray
        }
        Write-Host ""
    }

    return $classes.Count
}

# Function to find CS0104 ambiguous references
function Find-AmbiguousReferences {
    Write-Host "3. CS0104 AMBIGUOUS REFERENCES:" -ForegroundColor Yellow

    try {
        $buildOutput = dotnet build 2>&1 | Where-Object { $_ -match "CS0104" }
        $ambiguousTypes = $buildOutput | ForEach-Object {
            if ($_ -match "'([^']*)'") {
                $matches[1]
            }
        } | Sort-Object -Unique

        foreach ($type in $ambiguousTypes) {
            Write-Host "   ❌ $type is ambiguous" -ForegroundColor Red
            rg "\b$type\b" src/ --type cs -l | ForEach-Object {
                Write-Host "      📁 $_" -ForegroundColor Gray
            }
            Write-Host ""
        }

        return $ambiguousTypes.Count
    }
    catch {
        Write-Host "   ⚠️  Build analysis failed - manual review needed" -ForegroundColor Yellow
        return 0
    }
}

# Function to analyze layer violations
function Analyze-LayerViolations {
    Write-Host "4. LAYER VIOLATION ANALYSIS:" -ForegroundColor Yellow

    # Check for Domain layer dependencies (should have none)
    Write-Host "   🔍 Checking Domain layer purity..." -ForegroundColor Cyan
    $domainDeps = rg "using LankaConnect\.(Application|Infrastructure|API)" src/LankaConnect.Domain/ --type cs -l
    if ($domainDeps) {
        Write-Host "   ❌ Domain layer has invalid dependencies:" -ForegroundColor Red
        $domainDeps | ForEach-Object { Write-Host "      📁 $_" -ForegroundColor Gray }
    } else {
        Write-Host "   ✅ Domain layer is pure" -ForegroundColor Green
    }

    # Check for Application layer dependencies (should only depend on Domain)
    Write-Host "   🔍 Checking Application layer dependencies..." -ForegroundColor Cyan
    $appDeps = rg "using LankaConnect\.(Infrastructure|API)" src/LankaConnect.Application/ --type cs -l
    if ($appDeps) {
        Write-Host "   ❌ Application layer has invalid dependencies:" -ForegroundColor Red
        $appDeps | ForEach-Object { Write-Host "      📁 $_" -ForegroundColor Gray }
    } else {
        Write-Host "   ✅ Application layer dependencies are correct" -ForegroundColor Green
    }

    Write-Host ""
}

# Function to suggest next actions
function Suggest-NextActions {
    Write-Host "5. IMMEDIATE ACTION RECOMMENDATIONS:" -ForegroundColor Magenta
    Write-Host ""

    Write-Host "   🎯 PHASE 1 - Critical Ambiguous References (CS0104):" -ForegroundColor Yellow
    Write-Host "      1. Fix ISecurityMetricsCollector duplication" -ForegroundColor White
    Write-Host "         - Keep: Infrastructure.Monitoring.ISecurityMetricsCollector" -ForegroundColor Green
    Write-Host "         - Remove: From Infrastructure.Security.ICulturalSecurityService" -ForegroundColor Red
    Write-Host ""

    Write-Host "   🎯 PHASE 2 - Missing Implementations (CS0535):" -ForegroundColor Yellow
    Write-Host "      1. Implement DatabaseSecurityOptimizationEngine missing methods" -ForegroundColor White
    Write-Host "      2. Implement CulturalIntelligenceBackupEngine interface" -ForegroundColor White
    Write-Host "      3. Complete EnterpriseConnectionPoolService implementation" -ForegroundColor White
    Write-Host ""

    Write-Host "   🎯 PHASE 3 - Missing Types (CS0246):" -ForegroundColor Yellow
    Write-Host "      1. Create missing value objects in Domain.Shared" -ForegroundColor White
    Write-Host "      2. Add missing result types in appropriate layers" -ForegroundColor White
    Write-Host ""
}

# Main execution
try {
    $duplicateInterfaces = Find-DuplicateInterfaces
    $duplicateClasses = Find-DuplicateClasses
    $ambiguousRefs = Find-AmbiguousReferences
    Analyze-LayerViolations
    Suggest-NextActions

    Write-Host "=== SUMMARY ===" -ForegroundColor Cyan
    Write-Host "Duplicate Interfaces: $duplicateInterfaces" -ForegroundColor $(if ($duplicateInterfaces -gt 0) { "Red" } else { "Green" })
    Write-Host "Duplicate Classes: $duplicateClasses" -ForegroundColor $(if ($duplicateClasses -gt 0) { "Red" } else { "Green" })
    Write-Host "Ambiguous References: $ambiguousRefs" -ForegroundColor $(if ($ambiguousRefs -gt 0) { "Red" } else { "Green" })
    Write-Host ""
    Write-Host "📋 Next: Follow ADR-ZERO-TOLERANCE-DUPLICATE-ELIMINATION-ARCHITECTURE.md" -ForegroundColor Magenta
    Write-Host "🎯 Goal: Reduce from 631 to <400 errors in Phase 1" -ForegroundColor Green
}
catch {
    Write-Host "❌ Error during analysis: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}