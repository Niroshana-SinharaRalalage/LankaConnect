# LankaConnect Incremental Development Guidelines

**Version:** 1.0  
**Date:** January 2025  
**Target:** Cultural Intelligence Platform Development Team  
**Methodology:** Clean Architecture + DDD + TDD  

---

## 🎯 Overview

This document provides comprehensive guidelines for incremental development of LankaConnect's cultural intelligence platform. These guidelines ensure consistent quality, preserve cultural features, and prevent compilation issues through systematic development practices.

## 📋 Table of Contents

1. [Daily Development Workflow](#1-daily-development-workflow)
2. [File Creation Best Practices](#2-file-creation-best-practices)
3. [Cultural Intelligence Preservation Guidelines](#3-cultural-intelligence-preservation-guidelines)
4. [Build and Test Standards](#4-build-and-test-standards)
5. [Code Review Checklist](#5-code-review-checklist)
6. [Common Pitfalls and Solutions](#6-common-pitfalls-and-solutions)
7. [Emergency Recovery Procedures](#7-emergency-recovery-procedures)
8. [Cultural Intelligence Examples](#8-cultural-intelligence-examples)

---

## 1. Daily Development Workflow

### 1.1 Pre-Development Checklist ✅

**Every day before coding:**

```bash
# 1. Pull latest changes
git pull origin master

# 2. Verify compilation
dotnet build

# 3. Run basic tests
dotnet test --no-build

# 4. Check cultural intelligence services
dotnet test tests/LankaConnect.Domain.Tests/Events/ --filter "Category=CulturalIntelligence"

# 5. Verify Sacred Event Priority Matrix
dotnet test tests/LankaConnect.Domain.Tests/Events/ --filter "TestCategory=SacredEventMatrix"
```

**Status Check Commands:**
```bash
# Check git status for cultural files
git status | grep -E "(Cultural|Event|Communication)"

# Verify all projects compile
dotnet build --no-restore

# Quick cultural intelligence smoke test
curl -X GET "https://localhost:7001/api/CulturalIntelligence/cache/health"
```

### 1.2 Development Session Structure

**🕐 Start of Session (5 minutes):**
1. Review current sprint goals
2. Check cultural intelligence test coverage
3. Verify Sacred Event Priority Matrix integrity
4. Run compilation check

**⚡ During Development (Continuous):**
1. Follow TDD Red-Green-Refactor cycle
2. Commit cultural changes separately
3. Test cultural intelligence features after each change
4. Document cultural context decisions

**🕕 End of Session (10 minutes):**
1. Run full test suite
2. Verify cultural intelligence endpoints
3. Update cultural documentation if needed
4. Commit with cultural impact assessment

### 1.3 Cultural Context Verification

**Before any cultural intelligence changes:**

```bash
# Verify Sacred Event Priority Matrix
dotnet test tests/LankaConnect.Domain.Tests/Events/SacredEventPriorityMatrixTests.cs

# Check Cultural Community mappings
dotnet test tests/LankaConnect.Domain.Tests/Communications/ --filter "TestCategory=CulturalCommunity"

# Validate Buddhist/Hindu calendar integration
dotnet test tests/LankaConnect.Application.Tests/CulturalIntelligence/ --filter "Calendar"
```

---

## 2. File Creation Best Practices

### 2.1 Clean Architecture Layer Guidelines

#### **Domain Layer** (`src/LankaConnect.Domain/`)

**✅ DO:**
```csharp
// Domain entities with cultural context
namespace LankaConnect.Domain.Events
{
    public class CulturalEvent : AggregateRoot<EventId>
    {
        public CulturalSignificance Significance { get; private set; }
        public SacredEventPriority Priority { get; private set; }
        public CulturalCommunity TargetCommunity { get; private set; }
        
        // Cultural intelligence methods
        public Result<CulturalAppropriatenessScore> EvaluateAppropriateness(
            CulturalContext context)
        {
            // Cultural validation logic
        }
    }
}
```

**❌ DON'T:**
```csharp
// Avoid generic event without cultural context
public class Event
{
    public string Name { get; set; } // Too generic
    public DateTime Date { get; set; } // No cultural calendar context
}
```

#### **Application Layer** (`src/LankaConnect.Application/`)

**✅ DO:**
```csharp
// CQRS with cultural intelligence
namespace LankaConnect.Application.CulturalIntelligence.Queries
{
    public class CulturalCalendarQuery : IRequest<Result<CulturalCalendarResponse>>
    {
        public CalendarType CalendarType { get; set; }
        public DateTime Date { get; set; }
        public string GeographicRegion { get; set; } = "sri_lanka";
        public string Language { get; set; } = "en";
        public string? UserId { get; set; }
    }
    
    public class CulturalCalendarQueryHandler : 
        IRequestHandler<CulturalCalendarQuery, Result<CulturalCalendarResponse>>
    {
        private readonly ICulturalIntelligenceService _culturalService;
        private readonly ICulturalCalendarRepository _calendarRepository;
        
        // Cultural context preservation in handler
    }
}
```

#### **Infrastructure Layer** (`src/LankaConnect.Infrastructure/`)

**✅ DO:**
```csharp
// Cultural intelligence cache implementation
namespace LankaConnect.Infrastructure.Cache
{
    public class CulturalIntelligenceCacheService : ICulturalIntelligenceCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CulturalIntelligenceCacheService> _logger;
        
        public async Task<Result<CulturalCalendarResponse>> GetCachedCalendarAsync(
            CulturalCacheKey key,
            CancellationToken cancellationToken = default)
        {
            // Cultural-aware caching with regional considerations
        }
        
        private string GenerateCulturalCacheKey(CalendarType type, DateTime date, string region)
        {
            // Ensure cultural context in cache keys
            return $"cultural:calendar:{type.ToString().ToLower()}:{date:yyyy-MM-dd}:{region}";
        }
    }
}
```

### 2.2 File Naming Conventions

**Cultural Intelligence Files:**
```
Domain/
├── Events/
│   ├── CulturalEvent.cs                    # Main aggregate
│   ├── ValueObjects/
│   │   ├── SacredEventPriority.cs         # Sacred event priority matrix
│   │   ├── CulturalSignificance.cs        # Cultural importance scoring
│   │   └── BuddhistCalendarDate.cs        # Buddhist calendar integration
│   ├── Enums/
│   │   ├── CulturalConflictLevel.cs       # Conflict resolution levels
│   │   └── SacredEventType.cs             # Types of sacred events
│   └── DomainEvents/
│       ├── CulturalEventScheduledEvent.cs # Domain event for scheduling
│       └── SacredEventConflictDetectedEvent.cs
│
Application/
├── CulturalIntelligence/
│   ├── Commands/
│   │   ├── ScheduleCulturalEventCommand.cs
│   │   └── ResolveCulturalConflictCommand.cs
│   ├── Queries/
│   │   ├── CulturalCalendarQuery.cs
│   │   └── CulturalAppropriatenessQuery.cs
│   └── Services/
│       ├── CulturalConflictResolutionService.cs
│       └── SacredEventPriorityMatrixService.cs
```

### 2.3 Cultural Intelligence Entity Template

**Use this template for new cultural entities:**

```csharp
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Domain.Events
{
    /// <summary>
    /// Represents a cultural event with Sri Lankan diaspora community context
    /// Maintains Sacred Event Priority Matrix for conflict resolution
    /// </summary>
    public class CulturalEvent : AggregateRoot<EventId>
    {
        // Cultural Intelligence Properties
        public CulturalSignificance Significance { get; private set; } = null!;
        public SacredEventPriority Priority { get; private set; } = null!;
        public CulturalCommunity TargetCommunity { get; private set; }
        public CulturalCalendarType CalendarType { get; private set; }
        
        // Basic Event Properties
        public string Title { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        
        // Cultural Context
        public Dictionary<string, string> CulturalContext { get; private set; } = new();
        public List<CulturalTag> Tags { get; private set; } = new();
        
        // Factory method with cultural validation
        public static Result<CulturalEvent> Create(
            string title,
            string description,
            DateTime startDate,
            DateTime endDate,
            CulturalCommunity targetCommunity,
            CulturalSignificance significance)
        {
            // Validate cultural appropriateness
            var validationResult = ValidateCulturalContext(
                title, description, targetCommunity, significance);
                
            if (validationResult.IsFailure)
                return Result.Failure<CulturalEvent>(validationResult.Error);
                
            // Create with cultural intelligence
            var culturalEvent = new CulturalEvent();
            culturalEvent.ApplyCulturalIntelligence(/* parameters */);
            
            return Result.Success(culturalEvent);
        }
        
        // Cultural intelligence methods
        public Result<CulturalAppropriatenessScore> EvaluateAppropriateness(
            CulturalContext context)
        {
            // Sacred Event Priority Matrix evaluation
            // Cultural conflict detection
            // Community-specific appropriateness scoring
        }
        
        private static Result ValidateCulturalContext(
            string title,
            string description,
            CulturalCommunity community,
            CulturalSignificance significance)
        {
            // Cultural validation logic
            // Sacred event conflict checking
            // Community appropriateness validation
        }
    }
}
```

---

## 3. Cultural Intelligence Preservation Guidelines

### 3.1 Sacred Event Priority Matrix Protection

**🚨 CRITICAL: Never modify these without cultural intelligence review:**

```csharp
// PROTECTED: Sacred Event Priority Matrix
public enum SacredEventPriority
{
    Supreme = 1,        // Vesak, Poson (Buddhist)
    High = 2,           // Diwali, Holi (Hindu)
    Significant = 3,    // Independence Day (Cultural)
    Community = 4,      // Local cultural events
    Social = 5          // General community gatherings
}

// PROTECTED: Cultural Conflict Resolution Levels
public enum CulturalConflictLevel
{
    None = 0,           // No conflicts detected
    Minor = 1,          // Preference conflicts
    Moderate = 2,       // Cultural appropriateness concerns
    Major = 3,          // Religious significance conflicts
    Critical = 4        // Sacred event scheduling conflicts
}
```

### 3.2 Cultural Context Preservation Rules

**✅ ALWAYS preserve:**
1. **Sacred Event Priority Matrix** - Core cultural intelligence
2. **Buddhist/Hindu Calendar Integration** - Religious context
3. **Cultural Community Mappings** - Diaspora organization
4. **Appropriateness Scoring Algorithms** - Cultural sensitivity
5. **Conflict Resolution Mechanisms** - Community harmony

**❌ NEVER modify without review:**
1. Cultural significance calculations
2. Sacred event conflict detection
3. Community-specific appropriateness rules
4. Calendar system integrations
5. Cultural context validation logic

### 3.3 Cultural Intelligence Testing Requirements

**Before any cultural changes:**

```bash
# 1. Run Sacred Event Priority Matrix tests
dotnet test tests/LankaConnect.Domain.Tests/Events/SacredEventPriorityMatrixTests.cs -v

# 2. Validate Cultural Calendar integration
dotnet test tests/LankaConnect.Application.Tests/CulturalIntelligence/CulturalCalendarQueryHandlerTests.cs -v

# 3. Check Cultural Appropriateness scoring
dotnet test tests/LankaConnect.Application.Tests/CulturalIntelligence/CulturalAppropriatenessTests.cs -v

# 4. Verify Cultural Community mappings
dotnet test tests/LankaConnect.Domain.Tests/Communications/CulturalCommunityTests.cs -v

# 5. Test Cultural Conflict Resolution
dotnet test tests/LankaConnect.Application.Tests/CulturalIntelligence/ConflictResolutionTests.cs -v
```

### 3.4 Cultural Documentation Requirements

**Document all cultural decisions:**

```csharp
/// <summary>
/// Cultural Intelligence Decision Log
/// Decision: Modified Buddhist calendar integration for diaspora communities
/// Date: 2025-01-10
/// Rationale: US-based Sri Lankan communities need timezone-aware Buddhist dates
/// Impact: Affects CulturalCalendarQuery and Buddhist festival calculations
/// Stakeholders: Cultural Intelligence Team, Sri Lankan Community Leaders
/// Testing: Added BuddhistCalendarTimezoneTests.cs
/// </summary>
public class BuddhistCalendarService : IBuddhistCalendarService
{
    // Implementation with cultural context preservation
}
```

---

## 4. Build and Test Standards

### 4.1 TDD Red-Green-Refactor for Cultural Features

**Red Phase - Write Failing Test:**

```csharp
[Fact]
public async Task CulturalCalendarQuery_ForVesakDay_ReturnsCorrectSacredEventPriority()
{
    // Arrange
    var query = new CulturalCalendarQuery
    {
        CalendarType = CalendarType.Buddhist,
        Date = new DateTime(2025, 5, 23), // Vesak Day 2025
        GeographicRegion = "sri_lanka",
        Language = "en"
    };
    
    // Act
    var result = await _handler.Handle(query, CancellationToken.None);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Events.Should().ContainSingle(e => 
        e.Name == "Vesak Day" && 
        e.Priority == SacredEventPriority.Supreme);
}
```

**Green Phase - Make Test Pass:**

```csharp
public async Task<Result<CulturalCalendarResponse>> Handle(
    CulturalCalendarQuery request, 
    CancellationToken cancellationToken)
{
    // Get cultural events with Sacred Event Priority Matrix
    var events = await _calendarRepository.GetCulturalEventsAsync(
        request.CalendarType, 
        request.Date, 
        request.GeographicRegion, 
        cancellationToken);
    
    // Apply Sacred Event Priority Matrix
    var prioritizedEvents = events.Select(e => 
        ApplySacredEventPriority(e, request.CalendarType))
        .OrderBy(e => e.Priority)
        .ToList();
    
    return Result.Success(new CulturalCalendarResponse
    {
        Events = prioritizedEvents,
        CalendarType = request.CalendarType,
        Date = request.Date,
        GeographicRegion = request.GeographicRegion
    });
}
```

**Refactor Phase - Improve Cultural Intelligence:**

```csharp
// Extract cultural intelligence service
private CulturalEventResponse ApplySacredEventPriority(
    CulturalEvent culturalEvent, 
    CalendarType calendarType)
{
    return _culturalIntelligenceService.EnrichWithCulturalContext(
        culturalEvent, calendarType);
}
```

### 4.2 Build Validation Standards

**Required build steps for cultural features:**

```bash
# 1. Clean build
dotnet clean
dotnet restore

# 2. Compile all projects
dotnet build --configuration Release --no-restore

# 3. Run cultural intelligence tests
dotnet test tests/LankaConnect.Domain.Tests/ --filter "Category=CulturalIntelligence" --no-build

# 4. Run sacred event matrix tests
dotnet test tests/LankaConnect.Domain.Tests/Events/ --filter "TestCategory=SacredEventMatrix" --no-build

# 5. Integration tests for cultural endpoints
dotnet test tests/LankaConnect.IntegrationTests/CulturalIntelligence/ --no-build

# 6. Performance tests for cultural cache
dotnet test tests/LankaConnect.IntegrationTests/ --filter "Category=Performance&TestCategory=CulturalCache" --no-build
```

### 4.3 Test Coverage Requirements

**Minimum coverage for cultural features:**
- **Domain Models**: 95% coverage
- **Cultural Intelligence Services**: 90% coverage
- **Sacred Event Priority Matrix**: 100% coverage
- **Cultural Conflict Resolution**: 90% coverage
- **API Controllers**: 85% coverage

**Coverage verification:**

```bash
# Generate coverage report
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings

# Check cultural intelligence coverage
dotnet tool run reportgenerator -reports:TestResults/*/coverage.cobertura.xml -targetdir:TestResults/CoverageReport -reporttypes:Html

# Verify minimum coverage thresholds
grep -A 5 "CulturalIntelligence" TestResults/CoverageReport/index.html
```

---

## 5. Code Review Checklist

### 5.1 Pre-Review Checklist (Author)

**Before submitting PR:**

- [ ] All cultural intelligence tests pass
- [ ] Sacred Event Priority Matrix unchanged or properly reviewed
- [ ] Cultural documentation updated
- [ ] No hardcoded cultural values
- [ ] Timezone awareness for diaspora communities
- [ ] Buddhist/Hindu calendar integration intact
- [ ] Cultural appropriateness validation working
- [ ] Build passes in Release configuration
- [ ] Integration tests for cultural endpoints pass
- [ ] Performance impact assessed for cultural cache

### 5.2 Cultural Intelligence Review Points

**Reviewer must verify:**

**🔍 Sacred Event Priority Matrix:**
- [ ] Priority levels unchanged without cultural team approval
- [ ] Conflict resolution logic preserved
- [ ] Buddhist/Hindu festival priorities correctly maintained

**🔍 Cultural Context Preservation:**
- [ ] Geographic region handling for diaspora communities
- [ ] Language support for Sinhala/Tamil/English
- [ ] Calendar system integration (Buddhist/Hindu/Gregorian)
- [ ] Cultural community mappings accurate

**🔍 Cultural Appropriateness:**
- [ ] Content validation for cultural sensitivity
- [ ] Community-specific appropriateness rules applied
- [ ] Sacred content handling appropriate
- [ ] Cultural conflict detection working

**🔍 Performance Impact:**
- [ ] Cultural cache efficiency maintained
- [ ] Database queries optimized for cultural data
- [ ] Memory usage reasonable for cultural context
- [ ] API response times within SLA

### 5.3 Clean Architecture Compliance

**Dependency Rules:**
- [ ] Domain layer has no external dependencies
- [ ] Application layer only depends on Domain
- [ ] Infrastructure implements Application interfaces
- [ ] API layer only orchestrates, no business logic

**Cultural Intelligence Architecture:**
- [ ] Cultural services properly abstracted
- [ ] Sacred Event Priority Matrix in Domain layer
- [ ] Cultural cache in Infrastructure layer
- [ ] Cultural intelligence queries in Application layer

### 5.4 Code Quality Standards

**Cultural Code Quality:**
```csharp
// ✅ GOOD: Cultural context preservation
public async Task<Result<CulturalEventResponse>> ScheduleCulturalEventAsync(
    ScheduleCulturalEventCommand command,
    CancellationToken cancellationToken = default)
{
    // Validate cultural appropriateness
    var appropriatenessResult = await _culturalIntelligenceService
        .ValidateCulturalAppropriatenessAsync(command.EventDetails, cancellationToken);
        
    if (appropriatenessResult.IsFailure)
        return Result.Failure<CulturalEventResponse>(appropriatenessResult.Error);
    
    // Check Sacred Event Priority Matrix for conflicts
    var conflictResult = await _sacredEventService
        .CheckForConflictsAsync(command.StartDate, command.EndDate, command.CulturalCommunity, cancellationToken);
        
    // Continue with cultural context...
}

// ❌ BAD: No cultural intelligence consideration
public async Task<EventResponse> ScheduleEventAsync(ScheduleEventCommand command)
{
    // Direct scheduling without cultural validation
    var eventEntity = new Event(command.Name, command.Date);
    await _repository.AddAsync(eventEntity);
    return new EventResponse { Id = eventEntity.Id };
}
```

---

## 6. Common Pitfalls and Solutions

### 6.1 Cultural Intelligence Pitfalls

**❌ Pitfall 1: Hardcoded Cultural Values**

```csharp
// BAD: Hardcoded Buddhist holidays
if (date.Month == 5 && date.Day == 23)
{
    return "Vesak Day";
}
```

**✅ Solution: Use Cultural Intelligence Service**

```csharp
// GOOD: Dynamic cultural calendar
var buddhistEvents = await _culturalCalendarService
    .GetBuddhistEventsAsync(date, "sri_lanka", cancellationToken);
    
var vesakDay = buddhistEvents.FirstOrDefault(e => 
    e.EventType == BuddhistFestival.Vesak);
```

**❌ Pitfall 2: Ignoring Sacred Event Priority Matrix**

```csharp
// BAD: Scheduling without conflict checking
public async Task ScheduleEvent(DateTime date, string title)
{
    var newEvent = new Event(title, date);
    await _repository.AddAsync(newEvent);
}
```

**✅ Solution: Sacred Event Conflict Detection**

```csharp
// GOOD: Check Sacred Event Priority Matrix
public async Task<Result> ScheduleCulturalEventAsync(
    DateTime date, 
    string title, 
    CulturalCommunity community)
{
    // Check for sacred event conflicts
    var conflictCheck = await _sacredEventService
        .CheckForConflictsAsync(date, community);
        
    if (conflictCheck.HasConflicts && conflictCheck.ConflictLevel >= CulturalConflictLevel.Major)
    {
        return Result.Failure(
            $"Cannot schedule during {conflictCheck.ConflictingEvent.Name} - Sacred Event Priority {conflictCheck.Priority}");
    }
    
    // Proceed with scheduling
    var culturalEvent = CulturalEvent.Create(title, date, community);
    await _repository.AddAsync(culturalEvent);
    
    return Result.Success();
}
```

### 6.2 Clean Architecture Pitfalls

**❌ Pitfall 3: Business Logic in Controllers**

```csharp
// BAD: Cultural validation in controller
[HttpPost("events")]
public async Task<IActionResult> CreateEvent(CreateEventRequest request)
{
    // Cultural validation logic in controller
    if (request.StartDate.DayOfWeek == DayOfWeek.Friday && 
        request.CommunityType == "Muslim")
    {
        return BadRequest("Cannot schedule during Friday prayers");
    }
    
    // More business logic in controller...
}
```

**✅ Solution: Move to Application Layer**

```csharp
// GOOD: Cultural validation in Application layer
[HttpPost("events")]
public async Task<IActionResult> CreateEvent(CreateEventRequest request)
{
    var command = new CreateCulturalEventCommand
    {
        Title = request.Title,
        StartDate = request.StartDate,
        CommunityType = request.CommunityType
    };
    
    var result = await _mediator.Send(command);
    
    return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
}

// Application layer handler with cultural intelligence
public class CreateCulturalEventCommandHandler : 
    IRequestHandler<CreateCulturalEventCommand, Result<CulturalEventResponse>>
{
    public async Task<Result<CulturalEventResponse>> Handle(
        CreateCulturalEventCommand request, 
        CancellationToken cancellationToken)
    {
        // Cultural appropriateness validation
        var culturalValidation = await _culturalIntelligenceService
            .ValidateEventAppropriatenessAsync(request, cancellationToken);
            
        // Sacred Event Priority Matrix checking
        var priorityCheck = await _sacredEventService
            .ValidatePriorityMatrixAsync(request.StartDate, request.CommunityType, cancellationToken);
            
        // Continue with cultural context preservation...
    }
}
```

### 6.3 Performance Pitfalls

**❌ Pitfall 4: Cultural Data N+1 Queries**

```csharp
// BAD: N+1 queries for cultural events
public async Task<List<CulturalEventResponse>> GetCulturalEventsAsync()
{
    var events = await _repository.GetAllEventsAsync();
    
    var responses = new List<CulturalEventResponse>();
    foreach (var eventItem in events)
    {
        // N+1: Separate query for each cultural context
        var culturalContext = await _culturalService
            .GetCulturalContextAsync(eventItem.Id);
        responses.Add(new CulturalEventResponse(eventItem, culturalContext));
    }
    
    return responses;
}
```

**✅ Solution: Batch Cultural Context Loading**

```csharp
// GOOD: Batch load cultural context
public async Task<List<CulturalEventResponse>> GetCulturalEventsAsync()
{
    var events = await _repository.GetAllEventsWithCulturalContextAsync();
    
    // Single query for all cultural contexts
    var eventIds = events.Select(e => e.Id).ToList();
    var culturalContexts = await _culturalService
        .GetCulturalContextsAsync(eventIds);
    
    var responses = events.Select(e => new CulturalEventResponse(
        e, 
        culturalContexts.GetValueOrDefault(e.Id)))
        .ToList();
    
    return responses;
}
```

### 6.4 Testing Pitfalls

**❌ Pitfall 5: Mocking Cultural Intelligence Services**

```csharp
// BAD: Over-mocking cultural services
[Fact]
public async Task Should_Schedule_Cultural_Event()
{
    // Over-simplified mock loses cultural intelligence
    _culturalServiceMock.Setup(x => x.ValidateAsync(It.IsAny<object>()))
        .ReturnsAsync(Result.Success());
        
    // Test passes but doesn't validate real cultural logic
}
```

**✅ Solution: Use Cultural Intelligence Test Fixtures**

```csharp
// GOOD: Real cultural intelligence with test data
[Fact]
public async Task Should_Respect_Sacred_Event_Priority_Matrix()
{
    // Use real Cultural Intelligence Service with test data
    var culturalService = new CulturalIntelligenceService(
        _testBuddhistCalendar,
        _testHinduCalendar,
        _testSacredEventMatrix);
    
    // Real vesak day test
    var vesakDay = new DateTime(2025, 5, 23);
    var conflictResult = await culturalService
        .CheckForConflictsAsync(vesakDay, CulturalCommunity.SriLankanBuddhist);
    
    // Assert real Sacred Event Priority Matrix behavior
    conflictResult.ConflictLevel.Should().Be(CulturalConflictLevel.Critical);
    conflictResult.SacredEventPriority.Should().Be(SacredEventPriority.Supreme);
}
```

---

## 7. Emergency Recovery Procedures

### 7.1 Cultural Intelligence Service Failure

**🚨 Symptoms:**
- Cultural calendar queries returning empty
- Sacred Event Priority Matrix not working
- Cultural appropriateness scoring fails

**🔧 Recovery Steps:**

```bash
# 1. Check cultural intelligence cache health
curl -X GET "https://localhost:7001/api/CulturalIntelligence/cache/health"

# 2. Verify cultural database connections
dotnet test tests/LankaConnect.IntegrationTests/Infrastructure/CulturalDatabaseConnectionTests.cs

# 3. Rebuild cultural intelligence cache
curl -X POST "https://localhost:7001/api/CulturalIntelligence/cache/warm" \
  -H "Content-Type: application/json" \
  -d '{"Community": "SriLankanBuddhist", "Strategy": "Priority"}'

# 4. Validate Sacred Event Priority Matrix
dotnet test tests/LankaConnect.Domain.Tests/Events/SacredEventPriorityMatrixTests.cs -v

# 5. If cache rebuild fails, restart cultural services
docker-compose restart cultural-intelligence-service
docker-compose restart redis-cache
```

### 7.2 Sacred Event Priority Matrix Corruption

**🚨 Critical Recovery - Sacred Event conflicts not detected:**

```sql
-- Emergency Sacred Event Priority Matrix restoration
-- Run in PostgreSQL to restore core cultural intelligence

BEGIN TRANSACTION;

-- Backup current state
CREATE TABLE sacred_event_backup AS 
SELECT * FROM cultural_events WHERE priority IN (1, 2, 3);

-- Restore core Sacred Events with correct priorities
UPDATE cultural_events 
SET priority = 1, priority_level = 'Supreme'
WHERE event_name IN ('Vesak Day', 'Poson Day', 'Katina Ceremony');

UPDATE cultural_events 
SET priority = 2, priority_level = 'High'
WHERE event_name IN ('Diwali', 'Holi', 'Navaratri', 'Thai Pusam');

UPDATE cultural_events 
SET priority = 3, priority_level = 'Significant'
WHERE event_name IN ('Sri Lankan Independence Day', 'Sinhala Tamil New Year');

-- Verify restoration
SELECT event_name, priority, priority_level, cultural_community 
FROM cultural_events 
WHERE priority <= 3 
ORDER BY priority, event_name;

COMMIT;
```

### 7.3 Compilation Recovery

**🚨 Build failures in cultural intelligence modules:**

```bash
# 1. Clean all build artifacts
dotnet clean --configuration Debug
dotnet clean --configuration Release
rm -rf */bin */obj

# 2. Restore packages with cultural dependencies
dotnet restore --force --no-cache

# 3. Build in specific order for cultural dependencies
dotnet build src/LankaConnect.Domain/LankaConnect.Domain.csproj --no-restore
dotnet build src/LankaConnect.Application/LankaConnect.Application.csproj --no-restore
dotnet build src/LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj --no-restore
dotnet build src/LankaConnect.API/LankaConnect.API.csproj --no-restore

# 4. If cultural intelligence compilation fails
cd src/LankaConnect.Application/CulturalIntelligence/
dotnet build --verbosity detailed

# 5. Check for missing cultural references
grep -r "LankaConnect.Domain.Events" --include="*.cs" . | grep "using"
grep -r "CulturalIntelligence" --include="*.csproj" .
```

### 7.4 Database Recovery for Cultural Data

**🚨 Cultural calendar data corruption:**

```bash
# 1. Backup current cultural data
pg_dump -h localhost -U postgres -d lankaconnect \
  --table cultural_events \
  --table cultural_calendars \
  --table sacred_event_matrix > cultural_backup_$(date +%Y%m%d_%H%M%S).sql

# 2. Restore from last known good backup
psql -h localhost -U postgres -d lankaconnect < cultural_data_seeds.sql

# 3. Verify cultural intelligence data integrity
dotnet test tests/LankaConnect.IntegrationTests/Database/CulturalDataIntegrityTests.cs

# 4. Rebuild cultural intelligence cache
curl -X POST "https://localhost:7001/api/CulturalIntelligence/cache/invalidate" \
  -H "Content-Type: application/json" \
  -d '{"DataType": "all"}'

curl -X POST "https://localhost:7001/api/CulturalIntelligence/cache/warm" \
  -H "Content-Type: application/json" \
  -d '{"Community": "all", "Strategy": "Complete"}'
```

### 7.5 Emergency Contact Procedures

**Cultural Intelligence Issues:**
1. **Immediate**: Check automated recovery scripts
2. **Escalate to**: Lead Cultural Intelligence Developer
3. **Critical**: Contact Sri Lankan Community Cultural Advisors
4. **Documentation**: Update incident log with cultural context

**Sacred Event Matrix Issues:**
1. **STOP**: All cultural event scheduling
2. **Immediate**: Run Sacred Event Priority Matrix validation
3. **Contact**: Cultural Intelligence Team Lead + Community Relations
4. **Document**: Cultural impact assessment required

---

## 8. Cultural Intelligence Examples

### 8.1 Scheduling Sacred Events

**Example: Scheduling a community event during Vesak season**

```csharp
// Check Sacred Event Priority Matrix before scheduling
public async Task<Result<CulturalEventResponse>> ScheduleCommunityEventAsync(
    ScheduleCommunityEventCommand command)
{
    var eventDate = command.ProposedDate;
    var community = command.TargetCommunity;
    
    // 1. Check Sacred Event Priority Matrix
    var sacredEventCheck = await _sacredEventService.CheckForConflictsAsync(
        eventDate, 
        community, 
        CancellationToken.None);
    
    if (sacredEventCheck.HasConflicts)
    {
        switch (sacredEventCheck.ConflictLevel)
        {
            case CulturalConflictLevel.Critical:
                // Vesak Day conflict - absolute no
                return Result.Failure<CulturalEventResponse>(
                    $"Cannot schedule during {sacredEventCheck.ConflictingEvent.Name}. " +
                    $"This is a Supreme priority sacred event for {community} community.");
                    
            case CulturalConflictLevel.Major:
                // Diwali conflict - suggest alternative
                var alternativeDate = await _culturalCalendarService
                    .FindNextAvailableDateAsync(eventDate, community);
                return Result.Failure<CulturalEventResponse>(
                    $"Scheduling conflict with {sacredEventCheck.ConflictingEvent.Name}. " +
                    $"Suggested alternative: {alternativeDate:yyyy-MM-dd}");
                    
            case CulturalConflictLevel.Moderate:
                // Cultural preference - warn but allow
                _logger.LogWarning(
                    "Scheduling event during {ConflictingEvent} may affect attendance in {Community}",
                    sacredEventCheck.ConflictingEvent.Name, community);
                break;
        }
    }
    
    // 2. Evaluate cultural appropriateness
    var appropriatenessScore = await _culturalIntelligenceService
        .EvaluateEventAppropriatenessAsync(command.EventDetails, community);
    
    if (appropriatenessScore.Score < 0.7) // 70% threshold
    {
        return Result.Failure<CulturalEventResponse>(
            $"Cultural appropriateness score too low: {appropriatenessScore.Score:P}. " +
            $"Concerns: {string.Join(", ", appropriatenessScore.Concerns)}");
    }
    
    // 3. Create culturally intelligent event
    var culturalEvent = CulturalEvent.Create(
        command.Title,
        command.Description,
        eventDate,
        eventDate.AddHours(command.DurationHours),
        community,
        CulturalSignificance.FromScore(appropriatenessScore.Score));
    
    if (culturalEvent.IsFailure)
        return Result.Failure<CulturalEventResponse>(culturalEvent.Error);
    
    // 4. Apply cultural context enrichment
    culturalEvent.Value.EnrichWithCulturalContext(new CulturalContext
    {
        GeographicRegion = command.Region ?? "global",
        Language = command.PreferredLanguage ?? "en",
        CommunitySpecificNotes = appropriatenessScore.Recommendations,
        SacredEventConsiderations = sacredEventCheck.Recommendations
    });
    
    // 5. Save with cultural intelligence metadata
    await _repository.AddAsync(culturalEvent.Value);
    
    return Result.Success(CulturalEventResponse.FromDomain(culturalEvent.Value));
}
```

### 8.2 Buddhist Calendar Integration

**Example: Getting Buddhist lunar calendar dates for diaspora communities**

```csharp
public async Task<Result<BuddhistCalendarResponse>> GetBuddhistCalendarAsync(
    DateTime gregorianDate,
    string timeZone = "America/New_York") // Diaspora timezone
{
    // 1. Convert to Buddhist calendar with timezone awareness
    var buddhistDate = await _buddhistCalendarService
        .ConvertToBuddhistDateAsync(gregorianDate, timeZone);
    
    // 2. Get sacred events for the Buddhist month
    var sacredEvents = await _sacredEventRepository
        .GetBuddhistEventsForMonthAsync(buddhistDate.Month, buddhistDate.Year);
    
    // 3. Apply Sacred Event Priority Matrix
    var prioritizedEvents = sacredEvents
        .Select(e => new BuddhistEventResponse
        {
            Name = e.Name,
            BuddhistDate = e.BuddhistDate,
            GregorianDate = e.ConvertToGregorian(timeZone),
            Priority = e.SacredEventPriority,
            Significance = e.CulturalSignificance,
            DiasporaCommunityGuidance = GetDiasporaGuidance(e, timeZone)
        })
        .OrderBy(e => e.Priority)
        .ThenBy(e => e.GregorianDate)
        .ToList();
    
    // 4. Add cultural context for diaspora communities
    var response = new BuddhistCalendarResponse
    {
        GregorianDate = gregorianDate,
        BuddhistDate = buddhistDate,
        TimeZone = timeZone,
        SacredEvents = prioritizedEvents,
        CulturalGuidance = await GetCulturalGuidanceAsync(buddhistDate, timeZone),
        CommunityRecommendations = await GetCommunityRecommendationsAsync(gregorianDate, timeZone)
    };
    
    return Result.Success(response);
}

private async Task<DiasporaCommunityGuidance> GetDiasporaGuidance(
    SacredEvent sacredEvent, 
    string timeZone)
{
    return new DiasporaCommunityGuidance
    {
        LocalObservanceTime = sacredEvent.GetLocalObservanceTime(timeZone),
        TempleRecommendations = await _templeService.GetNearbyTemplesAsync(timeZone),
        CommunityEventSuggestions = await GetCommunityEventSuggestionsAsync(sacredEvent, timeZone),
        CulturalPracticeAdaptations = GetDiasporaCulturalAdaptations(sacredEvent)
    };
}
```

### 8.3 Cultural Appropriateness Evaluation

**Example: Evaluating content for cultural sensitivity**

```csharp
public async Task<Result<CulturalAppropriatenessResponse>> EvaluateContentAsync(
    string content,
    CulturalCommunity targetCommunity,
    ContentType contentType = ContentType.Text)
{
    var culturalContext = new CulturalEvaluationContext
    {
        Content = content,
        TargetCommunity = targetCommunity,
        ContentType = contentType,
        EvaluationDate = DateTime.UtcNow
    };
    
    // 1. Sacred content validation
    var sacredContentResult = await _sacredContentValidator
        .ValidateAsync(culturalContext);
    
    if (sacredContentResult.HasViolations)
    {
        return Result.Success(new CulturalAppropriatenessResponse
        {
            Score = 0.0,
            Level = AppropriatenessLevel.Inappropriate,
            PrimaryConcern = "Sacred content violations detected",
            Violations = sacredContentResult.Violations,
            Recommendations = sacredContentResult.Recommendations
        });
    }
    
    // 2. Cultural sensitivity scoring
    var sensitivityScore = await _culturalSensitivityService
        .ScoreContentAsync(culturalContext);
    
    // 3. Community-specific appropriateness
    var communityScore = await _communityAppropriatenessService
        .EvaluateForCommunityAsync(culturalContext, targetCommunity);
    
    // 4. Aggregate cultural intelligence score
    var overallScore = CalculateOverallAppropriatenessScore(
        sensitivityScore, 
        communityScore, 
        sacredContentResult.SacredContentScore);
    
    // 5. Generate cultural recommendations
    var recommendations = await GenerateCulturalRecommendationsAsync(
        culturalContext, 
        sensitivityScore, 
        communityScore);
    
    var response = new CulturalAppropriatenessResponse
    {
        Score = overallScore,
        Level = GetAppropriatenessLevel(overallScore),
        CommunitySpecificScore = communityScore.Score,
        SensitivityScore = sensitivityScore.Score,
        SacredContentScore = sacredContentResult.SacredContentScore,
        Recommendations = recommendations,
        CulturalContext = await EnrichWithCulturalContextAsync(culturalContext),
        EvaluationMetadata = new CulturalEvaluationMetadata
        {
            EvaluatedAt = DateTime.UtcNow,
            EvaluatorVersion = "CulturalIntelligence-v2.1",
            CommunityConsultationLevel = GetConsultationLevel(overallScore)
        }
    };
    
    return Result.Success(response);
}

private AppropriatenessLevel GetAppropriatenessLevel(double score)
{
    return score switch
    {
        >= 0.9 => AppropriatenessLevel.HighlyAppropriate,
        >= 0.7 => AppropriatenessLevel.Appropriate,
        >= 0.5 => AppropriatenessLevel.CautionAdvised,
        >= 0.3 => AppropriatenessLevel.ReviewRequired,
        _ => AppropriatenessLevel.Inappropriate
    };
}
```

---

## 🎯 Quick Reference

### Daily Commands
```bash
# Morning check
git pull && dotnet build && dotnet test --filter "CulturalIntelligence"

# Cultural health check
curl -X GET "localhost:7001/api/CulturalIntelligence/cache/health"

# Sacred Event Matrix validation
dotnet test tests/LankaConnect.Domain.Tests/Events/SacredEventPriorityMatrixTests.cs
```

### Emergency Contacts
- **Cultural Intelligence Issues**: Lead Developer + Cultural Team
- **Sacred Event Matrix Problems**: STOP scheduling + Contact Community Relations
- **Build Failures**: Check cultural dependencies first

### Key Files to Monitor
- `SacredEventPriority.cs` - Core cultural intelligence
- `CulturalEvent.cs` - Main cultural aggregate
- `CulturalIntelligenceController.cs` - API endpoints
- `CulturalCalendarQuery.cs` - Calendar integration

---

**Remember**: Cultural intelligence is not just code - it represents real community values and sacred traditions. Every change must preserve the cultural context that makes LankaConnect meaningful to the Sri Lankan diaspora community.
