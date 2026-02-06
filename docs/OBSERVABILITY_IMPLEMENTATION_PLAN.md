# Comprehensive Observability & Resilience Implementation Plan

**Created**: 2026-01-17
**Status**: 🚀 In Progress
**Owner**: Development Team
**Audit Agent**: ac4d693

---

## Executive Summary

This plan addresses the debugging headaches identified during Phase 6A.X, where:
- Missing `.HasColumnName("id")` wasn't caught until runtime
- SQL queries weren't visible in logs
- 3+ deployments needed to fix one issue
- Silent error swallowing made failures invisible

**Total Vulnerabilities Found**: 103 across all layers
**Implementation Timeline**: 6-8 weeks
**Expected Outcome**: 99% reduction in invisible SQL errors, 80% reduction in debugging time

---

## Vulnerability Summary

| Priority | Count | Impact | Fix Effort |
|----------|-------|--------|-----------|
| **P0 - Critical** | 12 | Runtime crashes, silent data corruption | 3-5 days |
| **P1 - High** | 28 | Silent failures, missing diagnostics | 1-2 weeks |
| **P2 - Medium** | 45 | Debugging difficulty, observability gaps | 2-3 weeks |
| **P3 - Low** | 18 | Long-term maintainability | 1-2 weeks |

---

## Critical Findings (P0)

### 1. No Startup Validation of EF Core Configurations
**Impact**: Configuration errors like missing `.HasColumnName("id")` only discovered at runtime

**Affected Files**:
- [AppDbContext.cs](../src/LankaConnect.Infrastructure/Data/AppDbContext.cs) - no configuration validation
- All 36 `*Configuration.cs` files in [Configurations/](../src/LankaConnect.Infrastructure/Data/Configurations/)

**Fix**: Add startup validation in `Program.cs` to test all entity configurations

---

### 2. SQL Query Logging Disabled
**Impact**: Phase 6A.X demonstrated - StateTaxRate query failures invisible until PostgreSQL error

**Affected Files**:
- [appsettings.json:12-13](../src/LankaConnect.API/appsettings.json#L12-L13) - EF Core logging at `Information` level
- [appsettings.Development.json](../src/LankaConnect.API/appsettings.Development.json) - same issue

**Fix**: Change logging level to `Debug` to see generated SQL queries

---

### 3. Missing Pre-Query Logging in Repositories
**Impact**: Can't see what's being queried before failures occur

**Affected Files**:
- [StateTaxRateRepository.cs](../src/LankaConnect.Infrastructure/Data/Repositories/StateTaxRateRepository.cs)
- 15+ other repositories without pre-query logging

**Fix**: Add logging before database queries following base `Repository<T>` pattern

---

### 4. Silent Error Swallowing in Query Handlers
**Impact**: Users see incomplete data with no indication why

**Affected Files**:
- [GetEventAttendeesQueryHandler.cs:150-194](../src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs#L150-L194) - try-catch swallows exceptions

**Fix**: Add failed calculation tracking and detailed error logging

---

### 5. No Configuration Setting Validation
**Impact**: Missing `Stripe:SecretKey`, `AzureStorage:ConnectionString` will crash at runtime

**Affected Files**:
- [appsettings.json](../src/LankaConnect.API/appsettings.json) - empty values not validated

**Fix**: Add startup validation for required configuration settings

---

### 6-12. Additional Critical Issues

6. **No null checks before revenue calculation** - [RevenueCalculatorService.cs:38-40](../src/LankaConnect.Infrastructure/Services/RevenueCalculatorService.cs#L38-L40)
7. **Money arithmetic overflow protection** - [Money.cs:32-57](../src/LankaConnect.Domain/Shared/ValueObjects/Money.cs#L32-L57)
8. **RevenueBreakdown negative payout check without logging** - [RevenueBreakdown.cs:169-174](../src/LankaConnect.Domain/Events/ValueObjects/RevenueBreakdown.cs#L169-L174)
9. **StripeWebhookHandler hardcoded test data** - [StripeWebhookHandler.cs:396-446](../src/LankaConnect.Application/Billing/StripeWebhookHandler.cs#L396-L446)
10. **AppDbContext.CommitAsync no try-catch** - SaveChangesAsync failures unhandled
11. **EventRepository.AddAsync shadow property sync** - [EventRepository.cs:30-52](../src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs#L30-L52)
12. **AzureBlobStorageService connection string validation** - only at constructor, not startup

---

## High Priority Findings (P1)

### Summary
- 28 locations with missing try-catch blocks
- 15 repositories without pre-query logging
- Missing input validation in command handlers
- Silent failures in background jobs
- No error boundaries in frontend

**Key Files**:
- All repository files (30+ files)
- All CQRS handler files (150+ files)
- External service integrations (Stripe, Azure, Email)
- Frontend API service files

---

## Medium Priority Findings (P2)

### Summary
- 45 observability gaps
- Missing performance metrics
- No request/response logging in controllers
- Inconsistent error context in logs
- Missing correlation IDs for distributed tracing

---

## Low Priority Findings (P3)

### Summary
- 18 long-term improvements
- Roslyn analyzer recommendations
- Automated testing for configuration validation
- Code review checklists
- Monitoring dashboards

---

## Implementation Strategy

### Phase 1: Quick Wins (Week 1) 🚀 **STARTING NOW**

**Goal**: Prevent runtime crashes and enable SQL visibility

**Tasks**:
1. ✅ **Enable SQL Query Logging** (5 minutes)
   - Change `appsettings.json` EF Core logging to `Debug`
   - Change `appsettings.Development.json` similarly
   - Add Serilog level switches for runtime control

2. ✅ **Add Configuration Validation** (2-3 hours)
   - Add `ValidateConfiguration()` method in `Program.cs`
   - Validate required settings (JWT, ConnectionString, etc.)
   - Add optional settings warnings (Stripe, Azure)

3. ✅ **Add EF Core Configuration Validation** (3-4 hours)
   - Add `ValidateEfCoreConfigurationsAsync()` in `Program.cs`
   - Test all DbSets can be queried at startup
   - Validate migrations are applied

4. ✅ **Fix StateTaxRateRepository Logging** (1 hour)
   - Add pre-query logging
   - Add try-catch with error context
   - Add performance timing

5. ✅ **Add Global Exception Logging Middleware** (1-2 hours)
   - Create `GlobalExceptionMiddleware.cs`
   - Log all unhandled exceptions with context
   - Return user-friendly error messages

**Deliverables**:
- SQL queries visible in logs (Seq/Azure)
- Configuration errors caught at startup
- EF Core configuration mismatches caught at startup
- All exceptions logged with full context

**Estimated Effort**: 1-2 days
**Risk**: Low (mostly additive changes)
**Testing**: Manual verification + restart staging environment

---

### Phase 2: Repository & Service Layer (Weeks 2-3)

**Goal**: Add comprehensive logging and error handling to all data access

**Tasks**:
1. Update base `Repository<T>` with performance metrics
2. Add pre-query logging to all 30+ derived repositories
3. Add try-catch to all external API calls (Stripe, Azure, Email)
4. Add null checks in service methods
5. Improve error messages in GetEventAttendeesQueryHandler

**Deliverables**:
- Every database query logged before execution
- All external API calls have error handling
- Performance metrics for slow queries
- Failed calculations tracked and reported

**Estimated Effort**: 1-2 weeks
**Risk**: Medium (requires testing all repositories)

---

### Phase 3: Application Layer (Weeks 4-5)

**Goal**: Add validation and logging to all CQRS handlers

**Tasks**:
1. Add FluentValidation validators for all commands
2. Enhance error handling in command/query handlers
3. Add operation context logging (request ID, user ID)
4. Add business operation logging (payments, registrations)
5. Add request/response logging middleware

**Deliverables**:
- All commands validated before execution
- All handlers log start/complete/error
- Correlation IDs for distributed tracing
- Business metrics logged for analytics

**Estimated Effort**: 2-3 weeks
**Risk**: Medium (requires comprehensive test coverage)

---

### Phase 4: Frontend & Infrastructure (Weeks 6-7)

**Goal**: Complete observability stack across all layers

**Tasks**:
1. Add error boundaries to React components
2. Add API response validation in TypeScript
3. Add startup health checks
4. Add performance monitoring (Application Insights or OpenTelemetry)
5. Add metrics dashboard (Grafana + Prometheus or Azure Monitor)

**Deliverables**:
- Frontend errors caught and reported
- API responses validated before use
- Health check endpoints for monitoring
- Real-time metrics dashboard

**Estimated Effort**: 2-3 weeks
**Risk**: Medium (new infrastructure required)

---

### Phase 5: Automation & Prevention (Week 8)

**Goal**: Prevent future issues through automation

**Tasks**:
1. Create custom Roslyn analyzers
2. Add CI/CD validation checks
3. Add pre-commit Git hooks
4. Create code review checklist
5. Document patterns and templates

**Deliverables**:
- Roslyn analyzers enforce logging/error handling
- CI/CD fails on missing patterns
- Pre-commit hooks catch common issues
- Team trained on new patterns

**Estimated Effort**: 1-2 weeks
**Risk**: Low (tooling and documentation)

---

## Code Templates

### Template 1: Repository Method with Full Observability

```csharp
public async Task<Entity?> GetByCustomCriteriaAsync(string criteria, CancellationToken cancellationToken = default)
{
    using (LogContext.PushProperty("Operation", "GetByCustomCriteria"))
    using (LogContext.PushProperty("EntityType", typeof(Entity).Name))
    using (LogContext.PushProperty("Criteria", criteria))
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.Debug(
            "GetByCustomCriteria START: EntityType={EntityType}, Criteria={Criteria}",
            typeof(Entity).Name,
            criteria);

        try
        {
            var result = await _dbSet
                .AsNoTracking()
                .Where(e => e.SomeProperty == criteria)
                .FirstOrDefaultAsync(cancellationToken);

            stopwatch.Stop();

            _logger.Information(
                "GetByCustomCriteria COMPLETE: EntityType={EntityType}, Criteria={Criteria}, Found={Found}, Duration={ElapsedMs}ms",
                typeof(Entity).Name,
                criteria,
                result != null,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "GetByCustomCriteria FAILED: EntityType={EntityType}, Criteria={Criteria}, Duration={ElapsedMs}ms, Error={Message}",
                typeof(Entity).Name,
                criteria,
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw;
        }
    }
}
```

---

### Template 2: Command Handler with Full Error Handling

```csharp
public async Task<Result<Guid>> Handle(CreateSomethingCommand request, CancellationToken cancellationToken)
{
    _logger.Information(
        "CreateSomethingCommandHandler START: Request={@Request}",
        new { request.Name, request.Description, UserId = request.UserId });

    try
    {
        // Step 1: Validation
        var validationResult = await ValidateRequestAsync(request, cancellationToken);
        if (validationResult.IsFailure)
        {
            _logger.Warning(
                "CreateSomethingCommandHandler VALIDATION FAILED: UserId={UserId}, Error={Error}",
                request.UserId,
                validationResult.Error);

            return Result<Guid>.Failure(validationResult.Error);
        }

        // Step 2: Business logic
        var entity = CreateEntity(request);

        _logger.Debug(
            "CreateSomethingCommandHandler entity created: EntityId={EntityId}",
            entity.Id);

        // Step 3: Persistence
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.Information(
            "CreateSomethingCommandHandler COMPLETE: EntityId={EntityId}, UserId={UserId}",
            entity.Id,
            request.UserId);

        return Result<Guid>.Success(entity.Id);
    }
    catch (DbUpdateException dbEx)
    {
        _logger.LogError(dbEx,
            "CreateSomethingCommandHandler DATABASE ERROR: UserId={UserId}, Error={Message}",
            request.UserId,
            dbEx.Message);

        return Result<Guid>.Failure("Database error occurred. Please try again.");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "CreateSomethingCommandHandler UNEXPECTED ERROR: UserId={UserId}, Error={Message}",
            request.UserId,
            ex.Message);

        throw; // Re-throw for global error handler
    }
}
```

---

### Template 3: External API Call with Retry & Circuit Breaker

```csharp
public async Task<Result<TResponse>> CallExternalApiAsync<TRequest, TResponse>(
    TRequest request,
    CancellationToken cancellationToken = default)
    where TResponse : class
{
    var stopwatch = Stopwatch.StartNew();

    _logger.Information(
        "CallExternalApiAsync START: Endpoint={Endpoint}, Request={@Request}",
        _apiEndpoint,
        request);

    try
    {
        var response = await _httpClient.PostAsJsonAsync(
            _apiEndpoint,
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogWarning(
                "CallExternalApiAsync HTTP ERROR: Endpoint={Endpoint}, StatusCode={StatusCode}, Error={ErrorContent}, Duration={ElapsedMs}ms",
                _apiEndpoint,
                response.StatusCode,
                errorContent,
                stopwatch.ElapsedMilliseconds);

            return Result<TResponse>.Failure(
                $"External API returned {response.StatusCode}: {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);

        stopwatch.Stop();

        _logger.Information(
            "CallExternalApiAsync COMPLETE: Endpoint={Endpoint}, Success=true, Duration={ElapsedMs}ms",
            _apiEndpoint,
            stopwatch.ElapsedMilliseconds);

        return result != null
            ? Result<TResponse>.Success(result)
            : Result<TResponse>.Failure("Empty response from external API");
    }
    catch (HttpRequestException httpEx)
    {
        stopwatch.Stop();

        _logger.LogError(httpEx,
            "CallExternalApiAsync NETWORK ERROR: Endpoint={Endpoint}, Duration={ElapsedMs}ms, Error={Message}",
            _apiEndpoint,
            stopwatch.ElapsedMilliseconds,
            httpEx.Message);

        return Result<TResponse>.Failure(
            $"Network error calling external API: {httpEx.Message}");
    }
    catch (TaskCanceledException timeoutEx)
    {
        stopwatch.Stop();

        _logger.LogWarning(timeoutEx,
            "CallExternalApiAsync TIMEOUT: Endpoint={Endpoint}, Duration={ElapsedMs}ms",
            _apiEndpoint,
            stopwatch.ElapsedMilliseconds);

        return Result<TResponse>.Failure("External API call timed out");
    }
    catch (Exception ex)
    {
        stopwatch.Stop();

        _logger.LogError(ex,
            "CallExternalApiAsync UNEXPECTED ERROR: Endpoint={Endpoint}, Duration={ElapsedMs}ms, Error={Message}",
            _apiEndpoint,
            stopwatch.ElapsedMilliseconds,
            ex.Message);

        throw; // Re-throw for global error handler
    }
}
```

---

### Template 4: Value Object Create with Validation Logging

```csharp
public static Result<CustomValueObject> Create(string value, int? optionalParam = null)
{
    // Use static logger for value objects (no DI)
    var logger = Log.ForContext<CustomValueObject>();

    logger.Debug(
        "CustomValueObject.Create START: Value={Value}, OptionalParam={OptionalParam}",
        value,
        optionalParam);

    // Validation 1: Null/empty check
    if (string.IsNullOrWhiteSpace(value))
    {
        logger.Warning("CustomValueObject.Create FAILED: Value is null or empty");
        return Result<CustomValueObject>.Failure("Value is required");
    }

    // Validation 2: Format check
    if (!IsValidFormat(value))
    {
        logger.Warning(
            "CustomValueObject.Create FAILED: Invalid format - Value={Value}",
            value);

        return Result<CustomValueObject>.Failure(
            $"Value '{value}' does not match required format");
    }

    // Validation 3: Business rule
    if (optionalParam.HasValue && optionalParam.Value < 0)
    {
        logger.Warning(
            "CustomValueObject.Create FAILED: Invalid optional parameter - Value={OptionalParam}",
            optionalParam.Value);

        return Result<CustomValueObject>.Failure("Optional parameter cannot be negative");
    }

    logger.Information(
        "CustomValueObject.Create SUCCESS: Value={Value}, OptionalParam={OptionalParam}",
        value,
        optionalParam);

    return Result<CustomValueObject>.Success(new CustomValueObject(value, optionalParam));
}
```

---

## Automated Detection Strategy

### 1. Roslyn Analyzers (Custom)

**Analyzer 1**: Repository Methods Must Have Logging
```
ID: LC001
Rule: All repository methods must log start and completion
Severity: Warning
```

**Analyzer 2**: External API Calls Must Have Try-Catch
```
ID: LC002
Rule: HttpClient calls must be wrapped in try-catch
Severity: Warning
```

**Analyzer 3**: Nullable Reference Validation
```
ID: LC003
Rule: Nullable properties must be checked before access
Severity: Error
```

---

### 2. CI/CD Pipeline Checks

Add to build pipeline:
```yaml
# .github/workflows/build.yml

- name: Validate EF Core Configurations
  run: dotnet test --filter "Category=ConfigurationValidation"

- name: Roslyn Analyzer Warnings as Errors
  run: dotnet build -c Release /p:TreatWarningsAsErrors=true

- name: Check Logging Coverage
  run: dotnet tool run check-logging-coverage
```

---

### 3. Pre-Commit Git Hooks

```bash
#!/bin/sh
# .git/hooks/pre-commit

echo "Running pre-commit validation..."

# Check for console.log in TypeScript files
if git diff --cached --name-only | grep -q "\.ts$"; then
    if git diff --cached | grep -q "console\.log"; then
        echo "ERROR: console.log() found. Remove before committing."
        exit 1
    fi
fi

# Check for hardcoded secrets
if git diff --cached | grep -i -q "password\s*=\s*[\"'][^\"']*[\"']"; then
    echo "ERROR: Hardcoded password found. Use configuration instead."
    exit 1
fi

# Run Roslyn analyzers on C# files
if git diff --cached --name-only | grep -q "\.cs$"; then
    dotnet build --no-restore /p:TreatWarningsAsErrors=true
    if [ $? -ne 0 ]; then
        echo "ERROR: Build failed with analyzer warnings."
        exit 1
    fi
fi

echo "Pre-commit validation passed!"
```

---

## Success Metrics

### Week 1 (Phase 1 Complete)
- ✅ SQL queries visible in logs
- ✅ Configuration errors caught at startup
- ✅ EF Core configuration mismatches fail startup
- ✅ Zero unhandled exceptions escaping middleware

### Week 3 (Phase 2 Complete)
- ✅ All repositories have pre-query logging
- ✅ All external API calls have error handling
- ✅ 90% reduction in "invisible errors"
- ✅ Average debugging time reduced by 50%

### Week 5 (Phase 3 Complete)
- ✅ All commands have FluentValidation
- ✅ All handlers log operation context
- ✅ Correlation IDs in all logs
- ✅ Business metrics tracked

### Week 7 (Phase 4 Complete)
- ✅ Frontend error boundaries on all routes
- ✅ API response validation in place
- ✅ Health check endpoints working
- ✅ Metrics dashboard deployed

### Week 8 (Phase 5 Complete)
- ✅ Roslyn analyzers enforcing patterns
- ✅ CI/CD blocking on violations
- ✅ Pre-commit hooks preventing issues
- ✅ Team trained on new standards

---

## Critical Files to Modify (Phase 1)

### 1. appsettings.json (5 minutes)
**File**: [c:\Work\LankaConnect\src\LankaConnect.API\appsettings.json](../src/LankaConnect.API/appsettings.json)
**Change**: Lines 12-13 - Change EF Core logging to `Debug`

### 2. appsettings.Development.json (5 minutes)
**File**: [c:\Work\LankaConnect\src\LankaConnect.API\appsettings.Development.json](../src/LankaConnect.API/appsettings.Development.json)
**Change**: Same as appsettings.json

### 3. Program.cs (3-4 hours)
**File**: [c:\Work\LankaConnect\src\LankaConnect.API\Program.cs](../src/LankaConnect.API/Program.cs)
**Add**:
- `ValidateConfiguration()` method
- `ValidateEfCoreConfigurationsAsync()` method
- Call both before `app.Run()`

### 4. GlobalExceptionMiddleware.cs (1-2 hours)
**File**: NEW - [c:\Work\LankaConnect\src\LankaConnect.API\Middleware\GlobalExceptionMiddleware.cs](../src/LankaConnect.API/Middleware/GlobalExceptionMiddleware.cs)
**Create**: Global exception logging and user-friendly error responses

### 5. StateTaxRateRepository.cs (1 hour)
**File**: [c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Repositories\StateTaxRateRepository.cs](../src/LankaConnect.Infrastructure/Data/Repositories/StateTaxRateRepository.cs)
**Add**: Pre-query logging, try-catch, performance timing

---

## Risk Assessment

### Low Risk (Phase 1)
- Configuration changes (logging levels)
- Additive middleware (global exception handler)
- Startup validation (fail-fast, not breaking existing functionality)

### Medium Risk (Phases 2-4)
- Repository modifications (need comprehensive testing)
- Handler modifications (need integration tests)
- Frontend changes (need E2E tests)

### Mitigation Strategy
- Deploy to staging first, validate 24 hours before production
- Feature flags for new middleware (can disable if issues)
- Comprehensive test coverage before deployment
- Rollback plan documented for each phase

---

## Next Steps

### Immediate (Today)
1. ✅ Create this implementation plan document
2. 🚀 Start Phase 1 Quick Wins:
   - Enable SQL query logging
   - Add configuration validation
   - Add EF Core configuration validation
   - Fix StateTaxRateRepository logging
   - Add global exception middleware

### This Week
1. Complete Phase 1 implementation
2. Deploy to staging environment
3. Validate all changes working
4. Document any issues encountered

### Next Week
1. Start Phase 2 (Repository & Service Layer)
2. Apply templates to 5-10 repositories as examples
3. Create PR for team review
4. Begin team training on new patterns

---

## References

- **Audit Report**: Agent ac4d693 comprehensive analysis
- **Phase 6A.X Issues**: [PHASE_6AX_FINAL_FIX_STATUS.md](./PHASE_6AX_FINAL_FIX_STATUS.md)
- **StateTaxRate Configuration Bug**: [PHASE_6AX_CRITICAL_ISSUE_RCA.md](./PHASE_6AX_CRITICAL_ISSUE_RCA.md)
- **Serilog Best Practices**: https://github.com/serilog/serilog/wiki/Configuration-Basics
- **EF Core Logging**: https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/

---

**Status**: 🚀 Phase 1 Starting Now
**Last Updated**: 2026-01-17
**Next Review**: After Phase 1 completion (Week 1)
