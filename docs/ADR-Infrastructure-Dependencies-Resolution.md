# Architecture Decision Record: Infrastructure Dependencies Resolution

## Status
**ACCEPTED** - 2025-09-04

## Context
The LankaConnect application is experiencing migration failures due to missing Infrastructure layer dependencies. The dependency injection container cannot resolve several critical services required for email communications functionality.

### Current Issues Identified:
1. **IUserEmailPreferencesRepository** - Missing implementation
2. **IMemoryCache** - Not registered, causing RazorEmailTemplateService to fail
3. **IEmailService** - Missing implementation 
4. **IEmailStatusRepository** - Missing implementation (conflicting with EmailMessageRepository)
5. **BusinessLocation Entity Binding** - EF Core cannot bind value object constructor parameters

### Architecture Analysis:
- **Clean Architecture**: Domain layer defines entities, Application defines interfaces, Infrastructure provides implementations
- **Existing Pattern**: EmailMessageRepository follows proper TDD and Result pattern implementation
- **Dependency Flow**: Application layer depends on abstractions, Infrastructure registers concrete implementations

## Decision

### 1. Service Implementation Priority Matrix

**Priority 1 (Critical - Blocks Migration):**
- IMemoryCache registration (Infrastructure layer)
- BusinessLocation EF Core configuration

**Priority 2 (High - Core Functionality):**
- IEmailService implementation 
- IUserEmailPreferencesRepository implementation

**Priority 3 (Medium - Administrative Features):**
- IEmailStatusRepository analysis and resolution

### 2. Architecture Patterns to Follow

#### Repository Pattern Consistency
- **Follow EmailMessageRepository pattern**: Use Result pattern, comprehensive logging, async operations
- **TDD Implementation**: Write failing tests first, implement minimal functionality, refactor
- **Error Handling**: Use Result pattern for all operations, comprehensive exception handling
- **Logging**: Use Serilog with structured logging and LogContext

#### Clean Architecture Compliance
```
Domain Layer (Pure):
├── Entities (EmailMessage, UserEmailPreferences)
├── Value Objects (BusinessLocation, Address, GeoCoordinate) 
├── Enums (EmailStatus, EmailType)
└── Interfaces (No implementations)

Application Layer (Orchestration):
├── Interfaces (IEmailService, IUserEmailPreferencesRepository, IEmailStatusRepository)
├── DTOs (EmailMessageDto, BulkEmailResult)
├── Commands/Queries (MediatR handlers)
└── Behaviors (Validation, Logging)

Infrastructure Layer (Implementation):
├── Repositories (Concrete EF Core implementations)
├── Services (Email sending, template processing)
├── Data Configuration (EF Core entity configurations)
└── DependencyInjection (Service registration)
```

### 3. Service Registration Strategy

#### IMemoryCache Registration
```csharp
// Add to Infrastructure.DependencyInjection.cs
services.AddMemoryCache(options =>
{
    options.SizeLimit = 100; // Limit template cache size
    options.CompactionPercentage = 0.25;
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
});
```

#### Repository Pattern Implementation
- **IUserEmailPreferencesRepository**: Separate entity from User aggregate
- **IEmailStatusRepository**: Merge functionality into EmailMessageRepository to avoid duplication
- **Single Responsibility**: Each repository handles one aggregate root

#### Email Service Architecture
```
IEmailService (Application Interface)
├── EmailService (Infrastructure Implementation)
├── Uses ISimpleEmailService for SMTP operations
├── Uses IEmailTemplateService for template rendering  
└── Uses IEmailMessageRepository for persistence
```

### 4. EF Core Value Object Configuration

#### BusinessLocation Configuration Strategy
- **Owned Entity Types**: Configure Address and GeoCoordinate as owned types
- **Private Constructors**: Use EF Core constructor binding with private setters
- **Value Object Pattern**: Maintain immutability while enabling EF Core binding

### 5. TDD Implementation Approach

#### Red-Green-Refactor Cycle
```
1. RED Phase:
   - Write failing repository tests (following EmailMessageRepositoryTests pattern)
   - Write failing service tests with Result pattern expectations
   - Write failing EF configuration tests

2. GREEN Phase:
   - Implement minimal functionality to pass tests
   - Focus on contract fulfillment, not optimization
   - Use existing infrastructure patterns (Result, logging)

3. REFACTOR Phase:
   - Optimize performance and error handling
   - Ensure Clean Architecture compliance
   - Add comprehensive documentation
```

## Implementation Roadmap

### Phase 1: Critical Dependencies (Day 1)
1. **IMemoryCache Registration**
   - Add to Infrastructure DependencyInjection
   - Configure cache policies for email templates
   - Test RazorEmailTemplateService resolution

2. **BusinessLocation EF Configuration**
   - Create entity type configuration
   - Handle value object constructor binding
   - Test migration generation

### Phase 2: Core Services (Day 1-2)
1. **IUserEmailPreferencesRepository**
   - TDD implementation following EmailMessageRepository pattern
   - Entity configuration and migration
   - Integration tests with Result pattern

2. **IEmailService Implementation**
   - Create concrete EmailService in Infrastructure
   - Integrate with existing ISimpleEmailService and IEmailTemplateService
   - Comprehensive error handling with Result pattern

### Phase 3: Service Consolidation (Day 2-3)
1. **IEmailStatusRepository Analysis**
   - Determine if separate repository needed or merge with EmailMessageRepository
   - Refactor GetEmailStatusQueryHandler to use consolidated approach
   - Update tests and documentation

2. **Integration Testing**
   - Test complete email flow end-to-end
   - Verify migration success
   - Performance testing with Result pattern overhead

## Consequences

### Benefits:
- **Consistent Architecture**: All services follow established Clean Architecture patterns
- **Maintainable Code**: TDD ensures comprehensive test coverage and documentation
- **Error Resilience**: Result pattern provides consistent error handling across all services
- **Performance Optimization**: Proper caching configuration and entity tracking

### Trade-offs:
- **Development Time**: TDD approach requires more upfront investment
- **Complexity**: Result pattern adds abstraction layer but improves reliability
- **Migration Dependency**: All services must be implemented before successful migration

### Risks:
- **Service Coupling**: Email services are interdependent, changes require careful coordination
- **Performance Impact**: Result pattern and logging add minimal overhead
- **Testing Complexity**: Integration tests require all services to be functioning

## Validation Criteria

### Success Metrics:
- [ ] EF Core migrations run successfully without dependency errors
- [ ] All MediatR handlers resolve dependencies correctly  
- [ ] Email functionality works end-to-end in integration tests
- [ ] Test coverage maintains 90%+ for all new implementations
- [ ] Performance benchmarks show acceptable overhead from Result pattern

### Quality Gates:
- [ ] All repository implementations follow EmailMessageRepository pattern
- [ ] Clean Architecture boundaries maintained (no Infrastructure references in Application)
- [ ] Comprehensive error handling with Result pattern
- [ ] Structured logging with correlation IDs
- [ ] Integration tests pass in both development and CI environments

## References
- Clean Architecture by Robert C. Martin
- Domain-Driven Design patterns
- ASP.NET Core DI container documentation
- EF Core value object configuration
- Existing EmailMessageRepository implementation