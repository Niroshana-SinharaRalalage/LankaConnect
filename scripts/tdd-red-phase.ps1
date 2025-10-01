#!/usr/bin/env pwsh
# LankaConnect TDD RED Phase Automation
# Creates failing tests and validates test infrastructure for cultural intelligence features

param(
    [Parameter(Mandatory=$true)]
    [string]$FeatureName,
    
    [Parameter(Mandatory=$false)]
    [string]$Domain = "CulturalIntelligence",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("Domain", "Application", "Infrastructure", "API")]
    [string]$Layer = "Domain",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild,
    
    [Parameter(Mandatory=$false)]
    [switch]$Verbose
)

# Import shared utilities
$UtilitiesPath = Join-Path $PSScriptRoot "automation\shared-utilities.ps1"
if (Test-Path $UtilitiesPath) {
    . $UtilitiesPath
} else {
    Write-Host "ERROR: Shared utilities not found at $UtilitiesPath" -ForegroundColor Red
    exit 1
}

# Script configuration
$ScriptName = "TDD-RED-Phase"
$ProjectRoot = Split-Path $PSScriptRoot -Parent

function Initialize-RedPhase {
    Write-Log "🔴 Starting TDD RED Phase for feature: $FeatureName" "INFO"
    Write-Log "Target Domain: $Domain, Layer: $Layer" "INFO"
    
    # Validate project structure
    if (!(Test-ProjectStructure -RootPath $ProjectRoot)) {
        Write-Log "Project structure validation failed" "ERROR"
        return $false
    }
    
    Write-Log "Project structure validated successfully" "SUCCESS"
    return $true
}

function New-FailingTest {
    param(
        [string]$FeatureName,
        [string]$Domain,
        [string]$Layer
    )
    
    $testProject = switch ($Layer) {
        "Domain" { "tests/LankaConnect.Domain.Tests" }
        "Application" { "tests/LankaConnect.Application.Tests" }  
        "Infrastructure" { "tests/LankaConnect.Infrastructure.Tests"  }
        "API" { "tests/LankaConnect.IntegrationTests" }
    }
    
    $testPath = Join-Path $ProjectRoot $testProject
    $domainPath = Join-Path $testPath $Domain
    
    # Ensure test directory exists
    if (!(Test-Path $domainPath)) {
        New-Item -ItemType Directory -Path $domainPath -Force | Out-Null
        Write-Log "Created test directory: $domainPath" "INFO"
    }
    
    $testFileName = "${FeatureName}Tests.cs"
    $testFilePath = Join-Path $domainPath $testFileName
    
    # Generate failing test based on layer
    $testContent = switch ($Layer) {
        "Domain" { Get-DomainTestTemplate -FeatureName $FeatureName -Domain $Domain }
        "Application" { Get-ApplicationTestTemplate -FeatureName $FeatureName -Domain $Domain }
        "Infrastructure" { Get-InfrastructureTestTemplate -FeatureName $FeatureName -Domain $Domain }
        "API" { Get-ApiTestTemplate -FeatureName $FeatureName -Domain $Domain }
    }
    
    Set-Content -Path $testFilePath -Value $testContent
    Write-Log "Created failing test: $testFilePath" "SUCCESS"
    
    return $testFilePath
}

function Get-DomainTestTemplate {
    param([string]$FeatureName, [string]$Domain)
    
    return @"
using LankaConnect.Domain.$Domain;
using LankaConnect.Domain.Common;
using Xunit;
using FluentAssertions;

namespace LankaConnect.Domain.Tests.$Domain
{
    /// <summary>
    /// TDD RED Phase tests for $FeatureName
    /// These tests are designed to FAIL initially to drive implementation
    /// </summary>
    public class ${FeatureName}Tests
    {
        [Fact]
        public void ${FeatureName}_ShouldHaveValidCulturalContext()
        {
            // RED: This test should fail - no implementation exists yet
            // Arrange
            var culturalContext = new CulturalContext("LK", "Sinhala");
            
            // Act & Assert
            Assert.True(false, "RED PHASE: ${FeatureName} cultural context validation not implemented");
        }
        
        [Fact]
        public void ${FeatureName}_ShouldValidateBusinessRules()
        {
            // RED: This test should fail - business rules not defined
            // Arrange & Act & Assert
            Assert.True(false, "RED PHASE: ${FeatureName} business rule validation not implemented");
        }
        
        [Fact]
        public void ${FeatureName}_ShouldRaiseDomainEvents()
        {
            // RED: This test should fail - domain events not implemented
            // Arrange & Act & Assert
            Assert.True(false, "RED PHASE: ${FeatureName} domain events not implemented");
        }
        
        [Theory]
        [InlineData("LK", "Sinhala")]
        [InlineData("LK", "Tamil")]
        [InlineData("US", "English")]
        public void ${FeatureName}_ShouldSupportMultipleCultures(string countryCode, string language)
        {
            // RED: This test should fail - multicultural support not implemented
            Assert.True(false, $"RED PHASE: ${FeatureName} multicultural support for {countryCode}-{language} not implemented");
        }
    }
}
"@
}

function Get-ApplicationTestTemplate {
    param([string]$FeatureName, [string]$Domain)
    
    return @"
using LankaConnect.Application.$Domain;
using LankaConnect.Application.Common.Interfaces;
using MediatR;
using Moq;
using Xunit;
using FluentAssertions;

namespace LankaConnect.Application.Tests.$Domain
{
    /// <summary>
    /// TDD RED Phase application tests for $FeatureName
    /// These tests focus on use cases and application services
    /// </summary>
    public class ${FeatureName}ApplicationTests
    {
        private readonly Mock<IApplicationDbContext> _mockContext;
        private readonly Mock<IMediator> _mockMediator;
        
        public ${FeatureName}ApplicationTests()
        {
            _mockContext = new Mock<IApplicationDbContext>();
            _mockMediator = new Mock<IMediator>();
        }
        
        [Fact]
        public async Task Handle_${FeatureName}Command_ShouldProcessSuccessfully()
        {
            // RED: This test should fail - command handler not implemented
            Assert.True(false, "RED PHASE: ${FeatureName} command handler not implemented");
        }
        
        [Fact]
        public async Task Handle_${FeatureName}Query_ShouldReturnExpectedResult()
        {
            // RED: This test should fail - query handler not implemented
            Assert.True(false, "RED PHASE: ${FeatureName} query handler not implemented");
        }
        
        [Fact]
        public async Task Validate_${FeatureName}Request_ShouldEnforceCulturalRules()
        {
            // RED: This test should fail - cultural validation not implemented
            Assert.True(false, "RED PHASE: ${FeatureName} cultural validation not implemented");
        }
        
        [Fact]
        public async Task Process_${FeatureName}_ShouldIntegrateWithCulturalIntelligence()
        {
            // RED: This test should fail - cultural intelligence integration not implemented
            Assert.True(false, "RED PHASE: ${FeatureName} cultural intelligence integration not implemented");
        }
    }
}
"@
}

function Get-InfrastructureTestTemplate {
    param([string]$FeatureName, [string]$Domain)
    
    return @"
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.$Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;

namespace LankaConnect.Infrastructure.Tests.$Domain
{
    /// <summary>
    /// TDD RED Phase infrastructure tests for $FeatureName
    /// These tests focus on data access and external integrations
    /// </summary>
    public class ${FeatureName}InfrastructureTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        
        public ${FeatureName}InfrastructureTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
        }
        
        [Fact]
        public async Task Repository_Should_PersistCulturalData()
        {
            // RED: This test should fail - repository not implemented
            Assert.True(false, "RED PHASE: ${FeatureName} repository persistence not implemented");
        }
        
        [Fact]
        public async Task ExternalService_Should_IntegrateWithCulturalAPIs()
        {
            // RED: This test should fail - external service integration not implemented
            Assert.True(false, "RED PHASE: ${FeatureName} external API integration not implemented");
        }
        
        [Fact]
        public async Task Cache_Should_OptimizeCulturalQueries()
        {
            // RED: This test should fail - caching mechanism not implemented
            Assert.True(false, "RED PHASE: ${FeatureName} cultural data caching not implemented");
        }
        
        [Fact]
        public async Task Migration_Should_SupportCulturalSchema()
        {
            // RED: This test should fail - database schema not implemented
            Assert.True(false, "RED PHASE: ${FeatureName} cultural database schema not implemented");
        }
        
        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
"@
}

function Get-ApiTestTemplate {
    param([string]$FeatureName, [string]$Domain)
    
    return @"
using LankaConnect.API;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using Xunit;
using FluentAssertions;
using System.Text.Json;

namespace LankaConnect.IntegrationTests.$Domain
{
    /// <summary>
    /// TDD RED Phase API integration tests for $FeatureName
    /// These tests validate end-to-end cultural intelligence workflows
    /// </summary>
    public class ${FeatureName}ApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;
        
        public ${FeatureName}ApiTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }
        
        [Fact]
        public async Task POST_${FeatureName}_ShouldReturnCreated()
        {
            // RED: This test should fail - API endpoint not implemented
            Assert.True(false, "RED PHASE: ${FeatureName} POST endpoint not implemented");
        }
        
        [Fact]
        public async Task GET_${FeatureName}_ShouldReturnCulturalData()
        {
            // RED: This test should fail - API endpoint not implemented
            Assert.True(false, "RED PHASE: ${FeatureName} GET endpoint not implemented");
        }
        
        [Fact]
        public async Task PUT_${FeatureName}_ShouldUpdateWithCulturalValidation()
        {
            // RED: This test should fail - API endpoint not implemented
            Assert.True(false, "RED PHASE: ${FeatureName} PUT endpoint with cultural validation not implemented");
        }
        
        [Theory]
        [InlineData("en-US")]
        [InlineData("si-LK")]
        [InlineData("ta-LK")]
        public async Task ${FeatureName}_ShouldSupportMultipleLocales(string locale)
        {
            // RED: This test should fail - localization not implemented
            Assert.True(false, $"RED PHASE: ${FeatureName} localization for {locale} not implemented");
        }
    }
}
"@
}

function Test-FailingTests {
    param([string]$TestFilePath)
    
    Write-Log "Validating that tests fail (RED phase requirement)" "INFO"
    
    $testProject = Split-Path (Split-Path $TestFilePath -Parent) -Parent
    $testResult = Invoke-TestExecution -ProjectPath $testProject -Configuration "Debug"
    
    if ($testResult.Success) {
        Write-Log "WARNING: Tests are passing in RED phase - this violates TDD methodology" "WARN"
        return $false
    } else {
        Write-Log "✅ Tests are failing as expected in RED phase" "SUCCESS"
        return $true
    }
}

function Invoke-RedPhaseValidation {
    Write-Log "Running RED phase validation checks" "INFO"
    
    # Build validation (should compile but tests should fail)
    if (!$SkipBuild) {
        $buildResult = Invoke-BuildValidation -ProjectRoot $ProjectRoot
        if (!$buildResult) {
            Write-Log "Build failed - cannot proceed with RED phase" "ERROR"
            return $false
        }
    }
    
    # Cultural intelligence feature validation
    $culturalFeatures = Test-CulturalFeatures -ProjectRoot $ProjectRoot
    Write-Log "Cultural features in development: $($culturalFeatures.Count)" "INFO"
    
    return $true
}

function New-RedPhaseReport {
    param([string]$TestFilePath)
    
    $metrics = @{
        Phase = "RED"
        FeatureName = $FeatureName
        Domain = $Domain
        Layer = $Layer
        TestFile = $TestFilePath
        Timestamp = Get-Date
        BuildStatus = "Unknown"
        TestsFailingAsExpected = $false
        CulturalFeatures = @()
    }
    
    # Collect metrics
    $metrics.BuildStatus = if (Invoke-BuildValidation -ProjectRoot $ProjectRoot) { "Success" } else { "Failed" }
    $metrics.CulturalFeatures = Test-CulturalFeatures -ProjectRoot $ProjectRoot
    $metrics.TestsFailingAsExpected = Test-FailingTests -TestFilePath $TestFilePath
    
    $reportPath = "red-phase-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    New-AutomationReport -Metrics $metrics -ReportPath $reportPath
    
    return $metrics
}

# Main execution
try {
    Write-Log "🔴 TDD RED Phase Automation Started" "INFO"
    
    # Initialize RED phase
    if (!(Initialize-RedPhase)) {
        Write-Log "RED phase initialization failed" "ERROR"
        exit 1
    }
    
    # Create failing test
    $testFilePath = New-FailingTest -FeatureName $FeatureName -Domain $Domain -Layer $Layer
    Write-Log "Created failing test at: $testFilePath" "SUCCESS"
    
    # Validate RED phase requirements
    if (!(Invoke-RedPhaseValidation)) {
        Write-Log "RED phase validation failed" "ERROR"
        exit 1
    }
    
    # Generate report
    $report = New-RedPhaseReport -TestFilePath $testFilePath
    
    Write-Log "🔴 TDD RED Phase completed successfully" "SUCCESS"
    Write-Log "Next step: Run tdd-green-phase.ps1 to implement the feature" "INFO"
    
    # Output summary
    Write-ColoredOutput "`n=== RED PHASE SUMMARY ===" "Info"
    Write-ColoredOutput "Feature: $FeatureName" "Info"
    Write-ColoredOutput "Domain: $Domain" "Info"
    Write-ColoredOutput "Layer: $Layer" "Info"
    Write-ColoredOutput "Test File: $testFilePath" "Info"
    Write-ColoredOutput "Tests Failing: $($report.TestsFailingAsExpected)" "Info"
    Write-ColoredOutput "Build Status: $($report.BuildStatus)" "Info"
    Write-ColoredOutput "=========================" "Info"
    
    exit 0
}
catch {
    Write-Log "RED phase automation failed: $($_.Exception.Message)" "ERROR"
    if ($Verbose) {
        Write-Log "Stack trace: $($_.ScriptStackTrace)" "ERROR"
    }
    exit 1
}