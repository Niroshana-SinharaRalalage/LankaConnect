#!/usr/bin/env pwsh
# LankaConnect TDD GREEN Phase Automation
# Implements features to make failing tests pass while maintaining cultural intelligence standards

param(
    [Parameter(Mandatory=$true)]
    [string]$FeatureName,
    
    [Parameter(Mandatory=$false)]
    [string]$Domain = "CulturalIntelligence",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("Domain", "Application", "Infrastructure", "API")]
    [string]$Layer = "Domain",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipTests,
    
    [Parameter(Mandatory=$false)]
    [switch]$AutoRefactor,
    
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
$ScriptName = "TDD-GREEN-Phase"
$ProjectRoot = Split-Path $PSScriptRoot -Parent

function Initialize-GreenPhase {
    Write-Log "🟢 Starting TDD GREEN Phase for feature: $FeatureName" "INFO"
    Write-Log "Target Domain: $Domain, Layer: $Layer" "INFO"
    
    # Validate project structure
    if (!(Test-ProjectStructure -RootPath $ProjectRoot)) {
        Write-Log "Project structure validation failed" "ERROR"
        return $false
    }
    
    # Verify RED phase tests exist
    $testExists = Test-RedPhaseArtifacts -FeatureName $FeatureName -Domain $Domain -Layer $Layer
    if (!$testExists) {
        Write-Log "RED phase artifacts not found. Run tdd-red-phase.ps1 first." "ERROR"
        return $false
    }
    
    Write-Log "GREEN phase initialization successful" "SUCCESS"
    return $true
}

function Test-RedPhaseArtifacts {
    param([string]$FeatureName, [string]$Domain, [string]$Layer)
    
    $testProject = switch ($Layer) {
        "Domain" { "tests/LankaConnect.Domain.Tests" }
        "Application" { "tests/LankaConnect.Application.Tests" }  
        "Infrastructure" { "tests/LankaConnect.Infrastructure.Tests" }
        "API" { "tests/LankaConnect.IntegrationTests" }
    }
    
    $testPath = Join-Path $ProjectRoot $testProject
    $domainPath = Join-Path $testPath $Domain
    $testFile = Join-Path $domainPath "${FeatureName}Tests.cs"
    
    return Test-Path $testFile
}

function New-ImplementationFiles {
    param([string]$FeatureName, [string]$Domain, [string]$Layer)
    
    $implementations = @()
    
    switch ($Layer) {
        "Domain" { 
            $implementations += New-DomainImplementation -FeatureName $FeatureName -Domain $Domain
        }
        "Application" { 
            $implementations += New-ApplicationImplementation -FeatureName $FeatureName -Domain $Domain
        }
        "Infrastructure" { 
            $implementations += New-InfrastructureImplementation -FeatureName $FeatureName -Domain $Domain
        }
        "API" { 
            $implementations += New-ApiImplementation -FeatureName $FeatureName -Domain $Domain
        }
    }
    
    return $implementations
}

function New-DomainImplementation {
    param([string]$FeatureName, [string]$Domain)
    
    $domainPath = Join-Path $ProjectRoot "src/LankaConnect.Domain/$Domain"
    if (!(Test-Path $domainPath)) {
        New-Item -ItemType Directory -Path $domainPath -Force | Out-Null
    }
    
    # Create main entity/aggregate
    $entityFile = Join-Path $domainPath "$FeatureName.cs"
    $entityContent = Get-DomainEntityTemplate -FeatureName $FeatureName -Domain $Domain
    Set-Content -Path $entityFile -Value $entityContent
    
    # Create value objects
    $valueObjectFile = Join-Path $domainPath "${FeatureName}ValueObjects.cs"
    $valueObjectContent = Get-ValueObjectTemplate -FeatureName $FeatureName -Domain $Domain
    Set-Content -Path $valueObjectFile -Value $valueObjectContent
    
    # Create domain events
    $eventsFile = Join-Path $domainPath "${FeatureName}Events.cs"
    $eventsContent = Get-DomainEventsTemplate -FeatureName $FeatureName -Domain $Domain
    Set-Content -Path $eventsFile -Value $eventsContent
    
    Write-Log "Created domain implementation files for $FeatureName" "SUCCESS"
    return @($entityFile, $valueObjectFile, $eventsFile)
}

function Get-DomainEntityTemplate {
    param([string]$FeatureName, [string]$Domain)
    
    return @"
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared;

namespace LankaConnect.Domain.$Domain
{
    /// <summary>
    /// $FeatureName aggregate root with cultural intelligence capabilities
    /// Implements domain logic for Sri Lankan cultural context
    /// </summary>
    public class $FeatureName : AggregateRoot<${FeatureName}Id>
    {
        private readonly List<${FeatureName}CulturalAttribute> _culturalAttributes = new();
        
        public $FeatureName(${FeatureName}Id id, CulturalContext culturalContext) : base(id)
        {
            CulturalContext = culturalContext ?? throw new ArgumentNullException(nameof(culturalContext));
            CreatedAt = DateTime.UtcNow;
            
            // Raise domain event
            RaiseDomainEvent(new ${FeatureName}CreatedEvent(Id, CulturalContext));
        }
        
        // Private constructor for EF Core
        private $FeatureName() { }
        
        public CulturalContext CulturalContext { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public IReadOnlyList<${FeatureName}CulturalAttribute> CulturalAttributes => _culturalAttributes.AsReadOnly();
        
        /// <summary>
        /// Validates business rules specific to Sri Lankan cultural context
        /// </summary>
        public bool ValidateBusinessRules()
        {
            // Cultural validation rules
            if (!CulturalContext.IsValidForSriLanka())
            {
                return false;
            }
            
            // Feature-specific validation
            return ValidateFeatureSpecificRules();
        }
        
        /// <summary>
        /// Adds cultural attribute with validation
        /// </summary>
        public void AddCulturalAttribute(${FeatureName}CulturalAttribute attribute)
        {
            if (attribute == null)
                throw new ArgumentNullException(nameof(attribute));
                
            if (!attribute.IsValidForContext(CulturalContext))
                throw new DomainException($"Cultural attribute not valid for context: {CulturalContext.Language}");
                
            _culturalAttributes.Add(attribute);
            UpdatedAt = DateTime.UtcNow;
            
            RaiseDomainEvent(new ${FeatureName}CulturalAttributeAddedEvent(Id, attribute));
        }
        
        /// <summary>
        /// Updates cultural context with validation
        /// </summary>
        public void UpdateCulturalContext(CulturalContext newContext)
        {
            if (newContext == null)
                throw new ArgumentNullException(nameof(newContext));
                
            if (!newContext.IsValidForSriLanka())
                throw new DomainException("Cultural context must be valid for Sri Lanka");
                
            var oldContext = CulturalContext;
            CulturalContext = newContext;
            UpdatedAt = DateTime.UtcNow;
            
            RaiseDomainEvent(new ${FeatureName}CulturalContextUpdatedEvent(Id, oldContext, newContext));
        }
        
        private bool ValidateFeatureSpecificRules()
        {
            // Implement feature-specific business rules
            // This should be customized based on the actual feature requirements
            return true;
        }
    }
    
    /// <summary>
    /// Strongly-typed identifier for $FeatureName
    /// </summary>
    public record ${FeatureName}Id(Guid Value) : EntityId(Value)
    {
        public static ${FeatureName}Id NewId() => new(Guid.NewGuid());
        public static ${FeatureName}Id FromGuid(Guid value) => new(value);
    }
}
"@
}

function Get-ValueObjectTemplate {
    param([string]$FeatureName, [string]$Domain)
    
    return @"
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared;

namespace LankaConnect.Domain.$Domain
{
    /// <summary>
    /// Cultural attribute value object for $FeatureName
    /// Represents cultural characteristics specific to Sri Lankan context
    /// </summary>
    public record ${FeatureName}CulturalAttribute : ValueObject
    {
        public ${FeatureName}CulturalAttribute(string name, string value, CulturalRelevance relevance)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Cultural attribute name cannot be empty", nameof(name));
                
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Cultural attribute value cannot be empty", nameof(value));
                
            Name = name;
            Value = value;
            Relevance = relevance;
        }
        
        public string Name { get; }
        public string Value { get; }
        public CulturalRelevance Relevance { get; }
        
        /// <summary>
        /// Validates if this cultural attribute is appropriate for the given context
        /// </summary>
        public bool IsValidForContext(CulturalContext context)
        {
            // Validate against Sri Lankan cultural norms
            if (!context.IsValidForSriLanka())
                return false;
                
            // Check language-specific relevance
            return Relevance.IsApplicableToLanguage(context.Language);
        }
        
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
            yield return Value;
            yield return Relevance;
        }
    }
    
    /// <summary>
    /// Represents the cultural relevance and applicability of attributes
    /// </summary>
    public record CulturalRelevance : ValueObject
    {
        public CulturalRelevance(
            IReadOnlyList<string> applicableLanguages,
            IReadOnlyList<string> applicableRegions,
            double significanceScore)
        {
            ApplicableLanguages = applicableLanguages ?? throw new ArgumentNullException(nameof(applicableLanguages));
            ApplicableRegions = applicableRegions ?? throw new ArgumentNullException(nameof(applicableRegions));
            
            if (significanceScore < 0 || significanceScore > 1)
                throw new ArgumentException("Significance score must be between 0 and 1", nameof(significanceScore));
                
            SignificanceScore = significanceScore;
        }
        
        public IReadOnlyList<string> ApplicableLanguages { get; }
        public IReadOnlyList<string> ApplicableRegions { get; }
        public double SignificanceScore { get; }
        
        public bool IsApplicableToLanguage(string language)
        {
            return ApplicableLanguages.Contains(language, StringComparer.OrdinalIgnoreCase);
        }
        
        public bool IsApplicableToRegion(string region)
        {
            return ApplicableRegions.Contains(region, StringComparer.OrdinalIgnoreCase);
        }
        
        protected override IEnumerable<object> GetEqualityComponents()
        {
            foreach (var language in ApplicableLanguages)
                yield return language;
            foreach (var region in ApplicableRegions)
                yield return region;
            yield return SignificanceScore;
        }
        
        // Predefined cultural relevance patterns for Sri Lanka
        public static CulturalRelevance SinhalaHigh => new(
            new[] { "Sinhala", "si-LK" },
            new[] { "Western", "Central", "Southern", "North Western", "North Central", "Uva", "Sabaragamuwa" },
            0.9
        );
        
        public static CulturalRelevance TamilHigh => new(
            new[] { "Tamil", "ta-LK" },
            new[] { "Northern", "Eastern" },
            0.9
        );
        
        public static CulturalRelevance Universal => new(
            new[] { "Sinhala", "Tamil", "English", "si-LK", "ta-LK", "en-LK" },
            new[] { "Western", "Central", "Southern", "Northern", "Eastern", "North Western", "North Central", "Uva", "Sabaragamuwa" },
            0.7
        );
    }
}
"@
}

function Get-DomainEventsTemplate {
    param([string]$FeatureName, [string]$Domain)
    
    return @"
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared;

namespace LankaConnect.Domain.$Domain
{
    /// <summary>
    /// Domain events for $FeatureName with cultural intelligence context
    /// </summary>
    
    public record ${FeatureName}CreatedEvent(
        ${FeatureName}Id ${FeatureName}Id,
        CulturalContext CulturalContext) : DomainEvent;
    
    public record ${FeatureName}CulturalAttributeAddedEvent(
        ${FeatureName}Id ${FeatureName}Id,
        ${FeatureName}CulturalAttribute CulturalAttribute) : DomainEvent;
    
    public record ${FeatureName}CulturalContextUpdatedEvent(
        ${FeatureName}Id ${FeatureName}Id,
        CulturalContext OldContext,
        CulturalContext NewContext) : DomainEvent;
    
    public record ${FeatureName}BusinessRuleValidationFailedEvent(
        ${FeatureName}Id ${FeatureName}Id,
        string ValidationError,
        CulturalContext CulturalContext) : DomainEvent;
}
"@
}

function New-ApplicationImplementation {
    param([string]$FeatureName, [string]$Domain)
    
    $appPath = Join-Path $ProjectRoot "src/LankaConnect.Application/$Domain"
    if (!(Test-Path $appPath)) {
        New-Item -ItemType Directory -Path $appPath -Force | Out-Null
    }
    
    # Create command handlers
    $commandsPath = Join-Path $appPath "Commands"
    if (!(Test-Path $commandsPath)) {
        New-Item -ItemType Directory -Path $commandsPath -Force | Out-Null
    }
    
    $commandFile = Join-Path $commandsPath "${FeatureName}Commands.cs"
    $commandContent = Get-ApplicationCommandTemplate -FeatureName $FeatureName -Domain $Domain
    Set-Content -Path $commandFile -Value $commandContent
    
    # Create query handlers
    $queriesPath = Join-Path $appPath "Queries"
    if (!(Test-Path $queriesPath)) {
        New-Item -ItemType Directory -Path $queriesPath -Force | Out-Null
    }
    
    $queryFile = Join-Path $queriesPath "${FeatureName}Queries.cs"
    $queryContent = Get-ApplicationQueryTemplate -FeatureName $FeatureName -Domain $Domain
    Set-Content -Path $queryFile -Value $queryContent
    
    Write-Log "Created application layer implementation for $FeatureName" "SUCCESS"
    return @($commandFile, $queryFile)
}

function Get-ApplicationCommandTemplate {
    param([string]$FeatureName, [string]$Domain)
    
    return @"
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.$Domain;
using LankaConnect.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Application.$Domain.Commands
{
    /// <summary>
    /// Command to create a new $FeatureName with cultural intelligence
    /// </summary>
    public record Create${FeatureName}Command(
        CulturalContext CulturalContext,
        IReadOnlyList<${FeatureName}CulturalAttribute> InitialAttributes
    ) : IRequest<${FeatureName}Id>;
    
    public class Create${FeatureName}CommandHandler : IRequestHandler<Create${FeatureName}Command, ${FeatureName}Id>
    {
        private readonly IApplicationDbContext _context;
        
        public Create${FeatureName}CommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<${FeatureName}Id> Handle(Create${FeatureName}Command request, CancellationToken cancellationToken)
        {
            // Validate cultural context
            if (!request.CulturalContext.IsValidForSriLanka())
            {
                throw new InvalidOperationException("Cultural context must be valid for Sri Lanka");
            }
            
            // Create new feature instance
            var featureId = ${FeatureName}Id.NewId();
            var feature = new $FeatureName(featureId, request.CulturalContext);
            
            // Add initial cultural attributes
            foreach (var attribute in request.InitialAttributes)
            {
                feature.AddCulturalAttribute(attribute);
            }
            
            // Validate business rules
            if (!feature.ValidateBusinessRules())
            {
                throw new InvalidOperationException("Business rule validation failed for cultural context");
            }
            
            // Persist to database
            _context.${FeatureName}s.Add(feature);
            await _context.SaveChangesAsync(cancellationToken);
            
            return featureId;
        }
    }
    
    /// <summary>
    /// Command to update cultural context of existing $FeatureName
    /// </summary>
    public record Update${FeatureName}CulturalContextCommand(
        ${FeatureName}Id ${FeatureName}Id,
        CulturalContext NewCulturalContext
    ) : IRequest<Unit>;
    
    public class Update${FeatureName}CulturalContextCommandHandler : IRequestHandler<Update${FeatureName}CulturalContextCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        
        public Update${FeatureName}CulturalContextCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<Unit> Handle(Update${FeatureName}CulturalContextCommand request, CancellationToken cancellationToken)
        {
            var feature = await _context.${FeatureName}s
                .FirstOrDefaultAsync(f => f.Id == request.${FeatureName}Id, cancellationToken);
                
            if (feature == null)
            {
                throw new NotFoundException(nameof($FeatureName), request.${FeatureName}Id);
            }
            
            feature.UpdateCulturalContext(request.NewCulturalContext);
            
            await _context.SaveChangesAsync(cancellationToken);
            
            return Unit.Value;
        }
    }
}
"@
}

function Get-ApplicationQueryTemplate {
    param([string]$FeatureName, [string]$Domain)
    
    return @"
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.$Domain;
using LankaConnect.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Application.$Domain.Queries
{
    /// <summary>
    /// Query to get $FeatureName by ID with cultural context
    /// </summary>
    public record Get${FeatureName}ByIdQuery(${FeatureName}Id ${FeatureName}Id) : IRequest<${FeatureName}Dto?>;
    
    public class Get${FeatureName}ByIdQueryHandler : IRequestHandler<Get${FeatureName}ByIdQuery, ${FeatureName}Dto?>
    {
        private readonly IApplicationDbContext _context;
        
        public Get${FeatureName}ByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<${FeatureName}Dto?> Handle(Get${FeatureName}ByIdQuery request, CancellationToken cancellationToken)
        {
            var feature = await _context.${FeatureName}s
                .Where(f => f.Id == request.${FeatureName}Id)
                .Select(f => new ${FeatureName}Dto
                {
                    Id = f.Id,
                    CulturalContext = f.CulturalContext,
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt,
                    CulturalAttributes = f.CulturalAttributes.ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);
                
            return feature;
        }
    }
    
    /// <summary>
    /// Query to get $FeatureName instances by cultural context
    /// </summary>
    public record Get${FeatureName}sByCulturalContextQuery(
        string Language,
        string? Region = null,
        int PageNumber = 1,
        int PageSize = 10
    ) : IRequest<${FeatureName}PagedResult>;
    
    public class Get${FeatureName}sByCulturalContextQueryHandler : IRequestHandler<Get${FeatureName}sByCulturalContextQuery, ${FeatureName}PagedResult>
    {
        private readonly IApplicationDbContext _context;
        
        public Get${FeatureName}sByCulturalContextQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<${FeatureName}PagedResult> Handle(Get${FeatureName}sByCulturalContextQuery request, CancellationToken cancellationToken)
        {
            var query = _context.${FeatureName}s.AsQueryable();
            
            // Filter by language
            query = query.Where(f => f.CulturalContext.Language == request.Language);
            
            // Filter by region if specified
            if (!string.IsNullOrEmpty(request.Region))
            {
                query = query.Where(f => f.CulturalContext.Region == request.Region);
            }
            
            var totalCount = await query.CountAsync(cancellationToken);
            
            var features = await query
                .OrderBy(f => f.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(f => new ${FeatureName}Dto
                {
                    Id = f.Id,
                    CulturalContext = f.CulturalContext,
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt,
                    CulturalAttributes = f.CulturalAttributes.ToList()
                })
                .ToListAsync(cancellationToken);
                
            return new ${FeatureName}PagedResult
            {
                Items = features,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
    }
    
    /// <summary>
    /// Data Transfer Object for $FeatureName
    /// </summary>
    public class ${FeatureName}Dto
    {
        public ${FeatureName}Id Id { get; set; }
        public CulturalContext CulturalContext { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<${FeatureName}CulturalAttribute> CulturalAttributes { get; set; } = new();
    }
    
    /// <summary>
    /// Paged result for $FeatureName queries
    /// </summary>
    public class ${FeatureName}PagedResult
    {
        public List<${FeatureName}Dto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
"@
}

function New-InfrastructureImplementation {
    param([string]$FeatureName, [string]$Domain)
    
    # Implementation for infrastructure layer
    Write-Log "Infrastructure implementation would be created here for $FeatureName" "INFO"
    return @()
}

function New-ApiImplementation {
    param([string]$FeatureName, [string]$Domain)
    
    # Implementation for API layer
    Write-Log "API implementation would be created here for $FeatureName" "INFO"
    return @()
}

function Test-GreenPhaseSuccess {
    param([string]$FeatureName, [string]$Domain, [string]$Layer)
    
    Write-Log "Running tests to validate GREEN phase success" "INFO"
    
    $testProject = switch ($Layer) {
        "Domain" { "tests/LankaConnect.Domain.Tests" }
        "Application" { "tests/LankaConnect.Application.Tests" }  
        "Infrastructure" { "tests/LankaConnect.Infrastructure.Tests" }
        "API" { "tests/LankaConnect.IntegrationTests" }
    }
    
    $testProjectPath = Join-Path $ProjectRoot $testProject
    $testResult = Invoke-TestExecution -ProjectPath $testProjectPath -Configuration "Debug"
    
    return $testResult.Success
}

function Invoke-AutoRefactor {
    param([string[]]$ImplementationFiles)
    
    if (!$AutoRefactor) {
        return
    }
    
    Write-Log "Running auto-refactoring on implementation files" "INFO"
    
    foreach ($file in $ImplementationFiles) {
        if (Test-Path $file) {
            # Basic refactoring: format code, remove unused usings
            & dotnet format $file --severity info 2>&1 | Out-Null
            Write-Log "Refactored: $file" "INFO"
        }
    }
}

function New-GreenPhaseReport {
    param([string[]]$ImplementationFiles, [bool]$TestsPass)
    
    $metrics = @{
        Phase = "GREEN"
        FeatureName = $FeatureName
        Domain = $Domain
        Layer = $Layer
        ImplementationFiles = $ImplementationFiles
        Timestamp = Get-Date
        TestsPass = $TestsPass
        BuildStatus = "Unknown"
        AutoRefactorApplied = $AutoRefactor
        CulturalFeatures = @()
    }
    
    # Collect additional metrics
    $metrics.BuildStatus = if (Invoke-BuildValidation -ProjectRoot $ProjectRoot) { "Success" } else { "Failed" }
    $metrics.CulturalFeatures = Test-CulturalFeatures -ProjectRoot $ProjectRoot
    
    $reportPath = "green-phase-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    New-AutomationReport -Metrics $metrics -ReportPath $reportPath
    
    return $metrics
}

# Main execution
try {
    Write-Log "🟢 TDD GREEN Phase Automation Started" "INFO"
    
    # Initialize GREEN phase
    if (!(Initialize-GreenPhase)) {
        Write-Log "GREEN phase initialization failed" "ERROR"
        exit 1
    }
    
    # Create implementation files
    $implementationFiles = New-ImplementationFiles -FeatureName $FeatureName -Domain $Domain -Layer $Layer
    Write-Log "Created $($implementationFiles.Count) implementation files" "SUCCESS"
    
    # Build validation
    if (!(Invoke-BuildValidation -ProjectRoot $ProjectRoot)) {
        Write-Log "Build failed after implementation - check compilation errors" "ERROR"
        exit 1
    }
    
    # Run tests to validate GREEN phase success
    $testsPass = $false
    if (!$SkipTests) {
        $testsPass = Test-GreenPhaseSuccess -FeatureName $FeatureName -Domain $Domain -Layer $Layer
        if ($testsPass) {
            Write-Log "✅ All tests are now passing - GREEN phase successful!" "SUCCESS"
        } else {
            Write-Log "⚠️ Some tests are still failing - implementation may need refinement" "WARN"
        }
    }
    
    # Auto-refactor if requested
    Invoke-AutoRefactor -ImplementationFiles $implementationFiles
    
    # Generate report
    $report = New-GreenPhaseReport -ImplementationFiles $implementationFiles -TestsPass $testsPass
    
    Write-Log "🟢 TDD GREEN Phase completed" "SUCCESS"
    if ($testsPass) {
        Write-Log "Next step: Consider refactoring or implementing next feature" "INFO"
    } else {
        Write-Log "Next step: Debug failing tests and refine implementation" "INFO"
    }
    
    # Output summary
    Write-ColoredOutput "`n=== GREEN PHASE SUMMARY ===" "Info"
    Write-ColoredOutput "Feature: $FeatureName" "Info"
    Write-ColoredOutput "Domain: $Domain" "Info"
    Write-ColoredOutput "Layer: $Layer" "Info"
    Write-ColoredOutput "Implementation Files: $($implementationFiles.Count)" "Info"
    Write-ColoredOutput "Tests Passing: $testsPass" "Info"
    Write-ColoredOutput "Build Status: $($report.BuildStatus)" "Info"
    Write-ColoredOutput "Auto-Refactor: $AutoRefactor" "Info"
    Write-ColoredOutput "=============================" "Info"
    
    exit 0
}
catch {
    Write-Log "GREEN phase automation failed: $($_.Exception.Message)" "ERROR"
    if ($Verbose) {
        Write-Log "Stack trace: $($_.ScriptStackTrace)" "ERROR"
    }
    exit 1
}