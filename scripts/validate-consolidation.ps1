# Enum Consolidation Validation Script
# Quick check to verify consolidation success

$workDir = "C:\Work\LankaConnect"
Set-Location $workDir

Write-Host "`n=== Enum Consolidation Validation ===" -ForegroundColor Cyan

# 1. Check for SacredPriorityLevel in production code
Write-Host "`n1. Checking for SacredPriorityLevel in src/..." -ForegroundColor Yellow
$sacredEnums = rg "enum\s+SacredPriorityLevel" --type cs src/ 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "   FAIL: SacredPriorityLevel still exists!" -ForegroundColor Red
    Write-Host $sacredEnums
    $failCount++
} else {
    Write-Host "   PASS: No SacredPriorityLevel found" -ForegroundColor Green
}

# 2. Check for exactly one CulturalDataPriority
Write-Host "`n2. Checking for CulturalDataPriority definitions..." -ForegroundColor Yellow
$culturalEnums = rg "enum\s+CulturalDataPriority" --type cs src/ 2>&1
$enumCount = ($culturalEnums | Measure-Object -Line).Lines

if ($enumCount -eq 1) {
    Write-Host "   PASS: Exactly 1 CulturalDataPriority definition found" -ForegroundColor Green
    Write-Host "   Location: $culturalEnums" -ForegroundColor Gray
} else {
    Write-Host "   FAIL: Found $enumCount definitions (expected 1)" -ForegroundColor Red
    Write-Host $culturalEnums
}

# 3. Check build errors
Write-Host "`n3. Checking build errors..." -ForegroundColor Yellow
$buildOutput = dotnet build --no-restore 2>&1 | Out-String
$errorCount = ($buildOutput | Select-String "error" -AllMatches).Matches.Count

Write-Host "   Current error count: $errorCount" -ForegroundColor $(if ($errorCount -le 6) { "Green" } else { "Red" })
if ($errorCount -le 6) {
    Write-Host "   PASS: Error count acceptable (≤ 6)" -ForegroundColor Green
} else {
    Write-Host "   FAIL: Error count increased!" -ForegroundColor Red
}

# 4. Run verification tests
Write-Host "`n4. Running verification tests..." -ForegroundColor Yellow
$testOutput = dotnet test tests/LankaConnect.Domain.Tests --filter "FullyQualifiedName~EnumConsolidation" --no-build 2>&1 | Out-String

if ($testOutput -match "Passed!") {
    Write-Host "   PASS: All verification tests passed" -ForegroundColor Green
} else {
    Write-Host "   FAIL: Some verification tests failed" -ForegroundColor Red
    Write-Host $testOutput
}

# 5. Check for duplicate CulturalDataPriority in CulturalStateReplicationService
Write-Host "`n5. Checking for removed duplicate enum..." -ForegroundColor Yellow
$duplicateCheck = rg "Sacred.*Critical.*High.*Medium.*Low" src/LankaConnect.Domain/Infrastructure/Failover/CulturalStateReplicationService.cs 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "   PASS: Duplicate enum removed" -ForegroundColor Green
} else {
    Write-Host "   FAIL: Duplicate enum still present!" -ForegroundColor Red
}

# Summary
Write-Host "`n=== Validation Summary ===" -ForegroundColor Cyan
Write-Host "Consolidation Status: " -NoNewline
if ($errorCount -le 6 -and $enumCount -eq 1 -and $LASTEXITCODE -ne 0) {
    Write-Host "SUCCESS" -ForegroundColor Green
    Write-Host "`nThe enum consolidation appears successful!" -ForegroundColor Green
    Write-Host "You can now safely commit and push the changes." -ForegroundColor Green
} else {
    Write-Host "INCOMPLETE or FAILED" -ForegroundColor Red
    Write-Host "`nSome validations failed. Review the output above." -ForegroundColor Yellow
    Write-Host "Consider rolling back: git reset --hard consolidation-step-X.Y" -ForegroundColor Yellow
}

Write-Host ""
