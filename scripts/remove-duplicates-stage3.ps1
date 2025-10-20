# Stage 3: Remove duplicate type definitions from BackupDisasterRecoveryEngine.cs

$file = "C:\Work\LankaConnect\src\LankaConnect.Infrastructure\Database\LoadBalancing\BackupDisasterRecoveryEngine.cs"

# Read the file
$content = Get-Content $file -Raw

# Remove ServiceLevelAgreement duplicate (lines 2334-2337)
$pattern1 = @"
    public class ServiceLevelAgreement
    {
        public string SLAName { get; set; } = string.Empty;
    }

"@

$content = $content -replace [regex]::Escape($pattern1), ""

# Remove PerformanceMonitoringConfiguration duplicate (lines 2765-2768)
$pattern2 = @"
    public class PerformanceMonitoringConfiguration
    {
        public string MonitoringLevel { get; set; } = string.Empty;
    }

"@

$content = $content -replace [regex]::Escape($pattern2), ""

# Write back to file
$content | Set-Content $file -NoNewline

Write-Host "✅ Removed 2 duplicate type definitions from BackupDisasterRecoveryEngine.cs"
Write-Host "   - ServiceLevelAgreement (stub version)"
Write-Host "   - PerformanceMonitoringConfiguration (stub version)"
