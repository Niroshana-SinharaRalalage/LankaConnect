# Batch 1: Remove first 5 namespace aliases from DatabaseSecurityOptimizationEngine.cs
# Aliases: SacredEventSecurityResult, SensitiveData, CulturalEncryptionPolicy, EncryptionResult, AuditScope

$filePath = "src\LankaConnect.Infrastructure\Database\LoadBalancing\DatabaseSecurityOptimizationEngine.cs"
$content = Get-Content $filePath -Raw

Write-Host "Batch 1: Removing 5 namespace aliases..." -ForegroundColor Cyan

# Batch 1 Replacements
# 1. SacredEventSecurityResult
$content = $content -replace '\bSacredEventSecurityResult\b', 'LankaConnect.Domain.Common.Database.SacredEventSecurityResult'

# 2. SensitiveData
$content = $content -replace '\bSensitiveData\b', 'LankaConnect.Domain.Common.Database.SensitiveData'

# 3. CulturalEncryptionPolicy
$content = $content -replace '\bCulturalEncryptionPolicy\b', 'LankaConnect.Domain.Common.Database.CulturalEncryptionPolicy'

# 4. EncryptionResult (but NOT EncryptionResultSet or other variations)
$content = $content -replace '\bEncryptionResult\b(?!Set)', 'LankaConnect.Domain.Common.Database.EncryptionResult'

# 5. AuditScope
$content = $content -replace '\bAuditScope\b', 'LankaConnect.Domain.Common.Database.AuditScope'

# Remove the using alias lines
$content = $content -replace 'using SacredEventSecurityResult = LankaConnect\.Domain\.Common\.Database\.SacredEventSecurityResult;\r?\n', ''
$content = $content -replace 'using SensitiveData = LankaConnect\.Domain\.Common\.Database\.SensitiveData;\r?\n', ''
$content = $content -replace 'using CulturalEncryptionPolicy = LankaConnect\.Domain\.Common\.Database\.CulturalEncryptionPolicy;\r?\n', ''
$content = $content -replace 'using EncryptionResult = LankaConnect\.Domain\.Common\.Database\.EncryptionResult;\r?\n', ''
$content = $content -replace 'using AuditScope = LankaConnect\.Domain\.Common\.Database\.AuditScope;\r?\n', ''

# Save the file
$content | Set-Content $filePath -NoNewline

Write-Host "Batch 1 complete: 5 aliases removed and references updated" -ForegroundColor Green
Write-Host "  - SacredEventSecurityResult" -ForegroundColor Gray
Write-Host "  - SensitiveData" -ForegroundColor Gray
Write-Host "  - CulturalEncryptionPolicy" -ForegroundColor Gray
Write-Host "  - EncryptionResult" -ForegroundColor Gray
Write-Host "  - AuditScope" -ForegroundColor Gray
