# Phase 6A.X Observability - Phases 2-5 Implementation Plan

**Date**: 2026-01-17
**Status**: 🎯 Ready to Begin
**Prerequisite**: Phase 1 Quick Wins ✅ COMPLETE

---

## Executive Summary

This document provides a detailed, step-by-step implementation plan for Phases 2-5 of the comprehensive observability improvements. Phase 1 (Quick Wins) addressed the immediate pain points from the StateTaxRate configuration issues. Phases 2-5 systematically eliminate debugging headaches across the entire application.

**Total Effort**: 6-8 weeks
**Total Vulnerabilities Remaining**: 91 (103 total - 12 fixed in Phase 1)

---

## Phase 1 Recap - What's Already Done ✅

### Completed (2026-01-17)

1. **SQL Query Logging Enabled** ([appsettings.json:12-13](../src/LankaConnect.API/appsettings.json#L12-L13))
   - All SQL queries visible in logs with parameters
   - Execution time logged

2. **Configuration Validation at Startup** ([Program.cs:471-532](../src/LankaConnect.API/Program.cs#L471-L532))
   - Required settings: ConnectionString, JWT keys
   - Optional settings: Stripe, Azure Storage, Email

3. **EF Core Configuration Validation** ([Program.cs:534-610](../src/LankaConnect.API/Program.cs#L534-L610))
   - Tests critical DbSets at startup
   - Would have caught StateTaxRate `.HasColumnName("id")` bug

4. **Global Exception Middleware** ([GlobalExceptionMiddleware.cs](../src/LankaConnect.API/Middleware/GlobalExceptionMiddleware.cs))
   - Catches all unhandled exceptions
   - Logs with full context (RequestId, UserId, Path, Method)
   - PostgreSQL SqlState detection

5. **StateTaxRateRepository Comprehensive Logging** ([StateTaxRateRepository.cs](../src/LankaConnect.Infrastructure/Data/Repositories/StateTaxRateRepository.cs))
   - Pre-query logging pattern established
   - Try-catch with performance timing
   - PostgreSQL error code logging

### Verification Results (2026-01-17)

**Revenue Calculation Testing**: ✅ WORKING CORRECTLY
- Tested endpoint: `/api/events/d543629f-a5ba-4475-b124-3d0fc5200f2f/attendees`
- Response includes detailed breakdown:
  - `totalSalesTax`: $20.39
  - `totalStripeFees`: $12.08
  - `totalPlatformCommission`: $7.09
  - `totalOrganizerPayout`: $335.43
  - `hasRevenueBreakdown`: true
  - `salesTaxRate`: 0.0575 (5.75% for Ohio)

**Build Status**: ✅ 0 errors, 0 warnings
**Deployment**: ✅ #21099560241 SUCCESS
**Azure Logs**: ✅ SQL queries visible, middleware active

---

## Remaining Work - Phases 2-5 Overview

| Phase | Focus Area | Effort | Vulnerabilities Fixed | Priority |
|-------|-----------|--------|----------------------|----------|
| **Phase 2** | Repository & Service Layer | 2-3 weeks | 28 (P1 High) | Critical |
| **Phase 3** | Application Layer (Handlers) | 1-2 weeks | 45 (P2 Medium) | High |
| **Phase 4** | Frontend & Infrastructure | 2-3 weeks | 18 (P3 Low) | Medium |
| **Phase 5** | Automation & Prevention | 1-2 weeks | N/A | High |

---

## Phase 2: Repository & Service Layer Logging (Weeks 2-3)

### Objective
Apply the comprehensive logging pattern (established in StateTaxRateRepository) to all 30+ repositories and external service calls.

### Scope

**Repositories to Update** (29 remaining):
1. EventRepository
2. RegistrationRepository
3. UserRepository
4. EventCategoryRepository
5. EventTagRepository
6. RsvpRepository
7. ReviewRepository
8. NewsletterRepository
9. EmailGroupRepository
10. NewsletterEmailGroupRepository
11. NewsletterSubscriberRepository
12. EmailMessageRepository
13. NotificationRepository
14. NotificationPreferenceRepository
15. SmsMessageRepository
16. CommunicationTemplateRepository
17. AttendanceRecordRepository
18. NewsletterRecipientSourceRepository
19. RefreshTokenRepository
20. PaymentRepository
21. SubscriptionRepository
22. RoleRepository
23. PermissionRepository
24. AuditLogRepository
25. BusinessRepository
26. LocationRepository
27. AddressRepository
28. MediaRepository
29. FileRepository

**External Service Calls to Wrap**:
- Azure Email Service
- Azure SMS Service
- Azure Blob Storage Service
- Stripe Payment Service
- Twilio SMS Service (if enabled)
- Google Maps API (if used)

### Implementation Steps

#### Step 1: Create Repository Logging Template (Week 2, Day 1)

**File**: `docs/templates/RepositoryLoggingTemplate.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Phase 6A.X: Repository implementation for {Entity} with comprehensive logging
/// </summary>
public class {Entity}Repository : Repository<{Entity}>, I{Entity}Repository
{
    public {Entity}Repository(AppDbContext context) : base(context)
    {
    }

    public async Task<{Entity}?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetById"))
        using (LogContext.PushProperty("EntityType", "{Entity}"))
        using (LogContext.PushProperty("EntityId", id))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.Debug(
                "GetByIdAsync START: EntityId={EntityId}",
                id);

            try
            {
                var result = await _dbSet
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

                stopwatch.Stop();

                _logger.Information(
                    "GetByIdAsync COMPLETE: EntityId={EntityId}, Found={Found}, Duration={ElapsedMs}ms",
                    id,
                    result != null,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.Error(ex,
                    "GetByIdAsync FAILED: EntityId={EntityId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    id,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    // Add similar pattern for all methods:
    // - GetAllAsync()
    // - AddAsync()
    // - UpdateAsync()
    // - DeleteAsync()
    // - Any custom query methods
}
```

#### Step 2: Apply Template to First 5 Repositories (Week 2, Days 2-3)

**Priority Order**:
1. ✅ StateTaxRateRepository (already done)
2. EventRepository (most critical - revenue calculations)
3. RegistrationRepository (revenue calculations)
4. UserRepository (authentication failures)
5. PaymentRepository (financial data)

**Verification**:
- Build succeeds with 0 errors
- All repository tests pass
- Manual API testing shows logging in Azure logs

#### Step 3: Apply Template to Remaining 24 Repositories (Week 2-3, Days 4-10)

**Batch Strategy**:
- **Batch 1 (Days 4-5)**: Core domain repositories (Event-related: 5 repositories)
  - EventCategoryRepository
  - EventTagRepository
  - RsvpRepository
  - ReviewRepository
  - AttendanceRecordRepository

- **Batch 2 (Days 6-7)**: Communication repositories (8 repositories)
  - NewsletterRepository
  - EmailGroupRepository
  - NewsletterEmailGroupRepository
  - NewsletterSubscriberRepository
  - EmailMessageRepository
  - NotificationRepository
  - NotificationPreferenceRepository
  - SmsMessageRepository

- **Batch 3 (Days 8-9)**: Infrastructure repositories (7 repositories)
  - CommunicationTemplateRepository
  - NewsletterRecipientSourceRepository
  - RefreshTokenRepository
  - SubscriptionRepository
  - RoleRepository
  - PermissionRepository
  - AuditLogRepository

- **Batch 4 (Day 10)**: Supporting repositories (4 repositories)
  - BusinessRepository
  - LocationRepository
  - AddressRepository
  - MediaRepository
  - FileRepository

**After Each Batch**:
- Run full build
- Run repository unit tests
- Deploy to staging
- Manual smoke test
- Check Azure logs for new logging

#### Step 4: External Service Logging (Week 3, Days 1-2)

**Template**: `docs/templates/ExternalServiceLoggingTemplate.cs`

```csharp
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.{ServiceArea}.Services;

/// <summary>
/// Phase 6A.X: {ServiceName} with comprehensive logging and error handling
/// </summary>
public class {ServiceName} : I{ServiceName}
{
    private readonly ILogger _logger;
    private readonly {ExternalClient} _client;

    public {ServiceName}({ExternalClient} client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Result<{Response}>> {MethodName}Async({Parameters})
    {
        using (LogContext.PushProperty("Operation", "{MethodName}"))
        using (LogContext.PushProperty("ServiceType", "{ServiceName}"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.Information(
                "{MethodName} START: {ParameterContext}",
                "{ParameterValues}");

            try
            {
                var response = await _client.{ExternalMethod}({Parameters});

                stopwatch.Stop();

                _logger.Information(
                    "{MethodName} COMPLETE: Success={Success}, Duration={ElapsedMs}ms",
                    response.IsSuccess,
                    stopwatch.ElapsedMilliseconds);

                return Result.Success(response);
            }
            catch (HttpRequestException httpEx)
            {
                stopwatch.Stop();

                _logger.Error(httpEx,
                    "{MethodName} FAILED: HttpError, StatusCode={StatusCode}, Duration={ElapsedMs}ms",
                    httpEx.StatusCode,
                    stopwatch.ElapsedMilliseconds);

                return Result.Failure<{Response}>(
                    $"{ServiceName} unavailable: {httpEx.Message}");
            }
            catch (TimeoutException timeoutEx)
            {
                stopwatch.Stop();

                _logger.Error(timeoutEx,
                    "{MethodName} FAILED: Timeout after {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);

                return Result.Failure<{Response}>(
                    $"{ServiceName} timeout");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.Error(ex,
                    "{MethodName} FAILED: UnexpectedError, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }
}
```

**Services to Update**:
1. AzureEmailService ([AzureEmailService.cs](../src/LankaConnect.Infrastructure/Email/Services/AzureEmailService.cs))
2. AzureSmsService
3. AzureBlobStorageService
4. StripePaymentService

#### Step 5: Add Retry Logic with Polly (Week 3, Day 3)

**Install Polly**:
```bash
dotnet add src/LankaConnect.Infrastructure package Polly
```

**Create Retry Policy**:
```csharp
// src/LankaConnect.Infrastructure/Common/Policies/RetryPolicies.cs
using Polly;
using Polly.Extensions.Http;

namespace LankaConnect.Infrastructure.Common.Policies;

public static class RetryPolicies
{
    public static IAsyncPolicy<HttpResponseMessage> GetExternalApiRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    Log.Warning(
                        "External API retry attempt {RetryAttempt} after {Delay}ms due to {Reason}",
                        retryAttempt,
                        timespan.TotalMilliseconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }
}
```

**Apply to HttpClient Registration**:
```csharp
// Program.cs
services.AddHttpClient<IAzureEmailService, AzureEmailService>()
    .AddPolicyHandler(RetryPolicies.GetExternalApiRetryPolicy());
```

#### Step 6: Performance Metrics (Week 3, Day 4)

**Add Performance Tracking**:
```csharp
// src/LankaConnect.Infrastructure/Common/Metrics/PerformanceMetrics.cs
using System.Diagnostics.Metrics;

namespace LankaConnect.Infrastructure.Common.Metrics;

public static class PerformanceMetrics
{
    private static readonly Meter _meter = new("LankaConnect.Infrastructure", "1.0");

    public static readonly Histogram<double> RepositoryQueryDuration = _meter.CreateHistogram<double>(
        "repository.query.duration",
        "ms",
        "Duration of repository queries");

    public static readonly Histogram<double> ExternalApiCallDuration = _meter.CreateHistogram<double>(
        "external.api.call.duration",
        "ms",
        "Duration of external API calls");

    public static readonly Counter<long> RepositoryQueryCount = _meter.CreateCounter<long>(
        "repository.query.count",
        description: "Number of repository queries");

    public static readonly Counter<long> ExternalApiErrorCount = _meter.CreateCounter<long>(
        "external.api.error.count",
        description: "Number of external API errors");
}
```

**Use in Repositories**:
```csharp
stopwatch.Stop();
PerformanceMetrics.RepositoryQueryDuration.Record(
    stopwatch.ElapsedMilliseconds,
    new KeyValuePair<string, object?>("entity", "StateTaxRate"),
    new KeyValuePair<string, object?>("operation", "GetActiveByStateCode"));
PerformanceMetrics.RepositoryQueryCount.Add(1);
```

### Phase 2 Deliverables

- [ ] 29 repositories with comprehensive logging
- [ ] 4 external services with retry logic
- [ ] Performance metrics collection
- [ ] Updated repository unit tests
- [ ] Deployment to staging with verification
- [ ] Azure logs showing repository and service logging

### Phase 2 Success Metrics

- All repository queries logged with duration
- External API failures logged with retry attempts
- Performance metrics visible in Azure Monitor
- No increase in error rates
- Build time increase < 10%

---

## Phase 3: Application Layer (CQRS Handlers) Logging (Week 4)

### Objective
Add comprehensive logging to all 150+ CQRS command and query handlers.

### Scope

**Handler Categories**:
- Command Handlers: ~80 handlers (Create, Update, Delete operations)
- Query Handlers: ~70 handlers (GetById, GetList, Search operations)

**Key Handler Groups**:
1. **Event Handlers** (20 handlers)
   - CreateEventCommandHandler
   - UpdateEventCommandHandler
   - DeleteEventCommandHandler
   - GetEventDetailsQueryHandler
   - GetEventAttendeesQueryHandler
   - SearchEventsQueryHandler
   - etc.

2. **Registration Handlers** (15 handlers)
   - CreateRegistrationCommandHandler
   - UpdateRegistrationCommandHandler
   - CancelRegistrationCommandHandler
   - GetRegistrationDetailsQueryHandler
   - etc.

3. **User/Auth Handlers** (12 handlers)
   - RegisterUserCommandHandler
   - LoginCommandHandler
   - RefreshTokenCommandHandler
   - UpdateUserProfileCommandHandler
   - etc.

4. **Newsletter Handlers** (18 handlers)
5. **Email/SMS Handlers** (22 handlers)
6. **Payment Handlers** (10 handlers)
7. **Notification Handlers** (8 handlers)
8. **Other Domain Handlers** (45 handlers)

### Implementation Steps

#### Step 1: Create Handler Logging Template (Week 4, Day 1)

**File**: `docs/templates/HandlerLoggingTemplate.cs`

```csharp
using MediatR;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Application.{Domain}.Commands.{CommandName};

/// <summary>
/// Phase 6A.X: Handler for {CommandName} with comprehensive logging
/// </summary>
public class {CommandName}Handler : IRequestHandler<{CommandName}, Result<{Response}>>
{
    private readonly I{Repository} _repository;
    private readonly ILogger _logger;

    public {CommandName}Handler(
        I{Repository} repository,
        ILogger logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<{Response}>> Handle(
        {CommandName} command,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "{CommandName}"))
        using (LogContext.PushProperty("UserId", command.UserId))
        using (LogContext.PushProperty("CommandType", typeof({CommandName}).Name))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.Information(
                "{CommandName} START: {ContextualParameters}",
                "{ParameterValues}");

            try
            {
                // Validation
                var validationResult = await ValidateCommand(command);
                if (validationResult.IsFailure)
                {
                    _logger.Warning(
                        "{CommandName} VALIDATION FAILED: {ValidationErrors}",
                        validationResult.Error);
                    return Result.Failure<{Response}>(validationResult.Error);
                }

                // Business logic
                var result = await ExecuteBusinessLogic(command, cancellationToken);

                stopwatch.Stop();

                if (result.IsSuccess)
                {
                    _logger.Information(
                        "{CommandName} COMPLETE: Success, Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.Warning(
                        "{CommandName} FAILED: {FailureReason}, Duration={ElapsedMs}ms",
                        result.Error,
                        stopwatch.ElapsedMilliseconds);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.Error(ex,
                    "{CommandName} EXCEPTION: Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    private async Task<Result> ValidateCommand({CommandName} command)
    {
        // Validation logic with logging
        _logger.Debug("{CommandName}: Validating command");

        if (string.IsNullOrWhiteSpace(command.RequiredField))
        {
            _logger.Debug("{CommandName}: Validation failed - RequiredField missing");
            return Result.Failure("RequiredField is required");
        }

        return Result.Success();
    }

    private async Task<Result<{Response}>> ExecuteBusinessLogic(
        {CommandName} command,
        CancellationToken cancellationToken)
    {
        // Business logic implementation
        _logger.Debug("{CommandName}: Executing business logic");

        // ... implementation ...

        return Result.Success(response);
    }
}
```

#### Step 2: Apply to Critical Handlers First (Week 4, Days 2-3)

**Priority Handlers** (10 handlers):
1. CreateEventCommandHandler (revenue calculations)
2. UpdateEventCommandHandler (revenue recalculations)
3. CreateRegistrationCommandHandler (payment processing)
4. GetEventAttendeesQueryHandler (revenue breakdown display)
5. LoginCommandHandler (authentication)
6. RegisterUserCommandHandler (user creation)
7. CreateNewsletterCommandHandler (email sending)
8. SendNewsletterCommandHandler (email delivery)
9. ProcessStripeWebhookCommandHandler (payment confirmations)
10. CalculateRevenueBreakdownCommandHandler (tax calculations)

#### Step 3: Batch Remaining Handlers (Week 4, Days 4-5)

**Batch Strategy**:
- 30 handlers per day
- Deploy to staging after each day
- Smoke test critical flows

#### Step 4: Add FluentValidation Logging (Week 4, Day 6)

**Update FluentValidation Pipeline Behavior**:
```csharp
// src/LankaConnect.Application/Common/Behaviors/ValidationBehavior.cs
using FluentValidation;
using MediatR;
using Serilog.Context;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("RequestType", typeof(TRequest).Name))
        {
            if (!_validators.Any())
            {
                return await next();
            }

            _logger.Debug("Validation START for {RequestType}", typeof(TRequest).Name);

            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Any())
            {
                _logger.Warning(
                    "Validation FAILED for {RequestType}: {ValidationErrors}",
                    typeof(TRequest).Name,
                    string.Join("; ", failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}")));

                throw new ValidationException(failures);
            }

            _logger.Debug("Validation PASSED for {RequestType}", typeof(TRequest).Name);

            return await next();
        }
    }
}
```

### Phase 3 Deliverables

- [ ] 150+ handlers with comprehensive logging
- [ ] FluentValidation logging enhanced
- [ ] Handler unit tests updated
- [ ] Integration tests passing
- [ ] Deployment to staging
- [ ] Azure logs showing handler execution flow

### Phase 3 Success Metrics

- All command/query executions logged
- Validation failures logged with context
- Handler duration tracked
- Business logic errors caught and logged
- No performance degradation

---

## Phase 4: Frontend & Infrastructure (Weeks 5-6)

### Objective
Add error boundaries, API response validation, health checks, and monitoring dashboards.

### Scope

**Frontend (React)**:
- Error boundaries for all major features
- API response validation
- Error toast notifications
- Retry logic for failed requests

**Infrastructure**:
- Health check endpoints
- Azure Application Insights integration
- Performance monitoring
- Alert rules for critical errors

### Implementation Steps

#### Step 1: React Error Boundaries (Week 5, Days 1-2)

**Create Global Error Boundary**:
```typescript
// web/src/presentation/components/common/ErrorBoundary.tsx
import React, { Component, ErrorInfo, ReactNode } from 'react';
import { logger } from '@/infrastructure/logging/logger';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    logger.error('React Error Boundary caught error', {
      error: error.message,
      stack: error.stack,
      componentStack: errorInfo.componentStack,
    });

    this.props.onError?.(error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      return this.props.fallback || (
        <div className="error-fallback">
          <h2>Something went wrong</h2>
          <p>{this.state.error?.message}</p>
          <button onClick={() => window.location.reload()}>Reload Page</button>
        </div>
      );
    }

    return this.props.children;
  }
}
```

**Wrap Major Features**:
- EventCreationForm
- EventEditForm
- AttendeeManagementTab
- NewsletterCreationForm
- UserProfile
- PaymentProcessing

#### Step 2: API Response Validation (Week 5, Day 3)

**Create Response Validator**:
```typescript
// web/src/infrastructure/api/validators/responseValidator.ts
import { z } from 'zod';
import { logger } from '@/infrastructure/logging/logger';

export function validateApiResponse<T>(
  response: unknown,
  schema: z.ZodSchema<T>,
  endpoint: string
): T {
  try {
    return schema.parse(response);
  } catch (error) {
    logger.error('API Response Validation Failed', {
      endpoint,
      error: error instanceof z.ZodError ? error.errors : error,
      response,
    });

    throw new Error(`Invalid response from ${endpoint}`);
  }
}

// Example usage in API client
export const getEventAttendees = async (eventId: string): Promise<EventAttendeesResponse> => {
  const response = await apiClient.get(`/api/events/${eventId}/attendees`);

  return validateApiResponse(
    response.data,
    eventAttendeesResponseSchema,
    `/api/events/${eventId}/attendees`
  );
};
```

#### Step 3: Retry Logic for API Calls (Week 5, Day 4)

**Install axios-retry**:
```bash
npm install axios-retry
```

**Configure Retry**:
```typescript
// web/src/infrastructure/api/client.ts
import axios from 'axios';
import axiosRetry from 'axios-retry';
import { logger } from '@/infrastructure/logging/logger';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
});

axiosRetry(apiClient, {
  retries: 3,
  retryDelay: axiosRetry.exponentialDelay,
  retryCondition: (error) => {
    return axiosRetry.isNetworkOrIdempotentRequestError(error) ||
      error.response?.status === 429; // Too Many Requests
  },
  onRetry: (retryCount, error, requestConfig) => {
    logger.warning('API Request Retry', {
      retryCount,
      url: requestConfig.url,
      method: requestConfig.method,
      error: error.message,
    });
  },
});
```

#### Step 4: Health Check Endpoints (Week 5, Day 5)

**Create Health Check**:
```csharp
// src/LankaConnect.API/Health/DatabaseHealthCheck.cs
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(AppDbContext context, ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                _logger.LogError("Health Check: Database connection FAILED");
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }

            // Test a simple query
            await _context.Users.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

            _logger.LogDebug("Health Check: Database HEALTHY");
            return HealthCheckResult.Healthy("Database connection successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health Check: Database check FAILED with exception");
            return HealthCheckResult.Unhealthy("Database error", ex);
        }
    }
}
```

**Register Health Checks**:
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<RedisHealthCheck>("redis")
    .AddCheck<AzureStorageHealthCheck>("azure_storage");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
});
```

#### Step 5: Azure Application Insights (Week 6, Days 1-2)

**Install SDK**:
```bash
dotnet add src/LankaConnect.API package Microsoft.ApplicationInsights.AspNetCore
```

**Configure**:
```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    options.EnableAdaptiveSampling = true;
    options.EnableQuickPulseMetricStream = true;
});
```

**Create Custom Telemetry**:
```csharp
// src/LankaConnect.API/Telemetry/CustomTelemetry.cs
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

public class CustomTelemetry
{
    private readonly TelemetryClient _telemetryClient;

    public CustomTelemetry(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }

    public void TrackRevenueCalculation(
        Guid eventId,
        decimal grossRevenue,
        decimal netRevenue,
        long durationMs)
    {
        var telemetry = new EventTelemetry("RevenueCalculation");
        telemetry.Properties.Add("EventId", eventId.ToString());
        telemetry.Metrics.Add("GrossRevenue", (double)grossRevenue);
        telemetry.Metrics.Add("NetRevenue", (double)netRevenue);
        telemetry.Metrics.Add("DurationMs", durationMs);

        _telemetryClient.TrackEvent(telemetry);
    }
}
```

#### Step 6: Alert Rules (Week 6, Day 3)

**Create Alert Rules in Azure**:
1. Database connection failures > 5 in 5 minutes
2. External API failures > 10 in 5 minutes
3. Handler exceptions > 20 in 5 minutes
4. Average response time > 2 seconds
5. Health check failures

### Phase 4 Deliverables

- [ ] Error boundaries for all major React features
- [ ] API response validation with Zod schemas
- [ ] Retry logic for failed API calls
- [ ] Health check endpoints
- [ ] Application Insights integration
- [ ] Alert rules configured

### Phase 4 Success Metrics

- All React errors caught and logged
- Invalid API responses detected
- Health checks accessible
- Telemetry visible in Application Insights
- Alert rules firing on issues

---

## Phase 5: Automation & Prevention (Weeks 7-8)

### Objective
Prevent observability issues from being introduced through automated checks and analyzers.

### Scope

**Roslyn Analyzers**:
- Detect missing logging in repositories
- Detect missing try-catch in handlers
- Detect missing validation in commands

**CI/CD Checks**:
- Enforce logging standards
- Validate health check endpoints
- Check telemetry coverage

**Pre-commit Hooks**:
- Verify new handlers have logging
- Verify new repositories have logging

### Implementation Steps

#### Step 1: Create Roslyn Analyzer Project (Week 7, Days 1-3)

**Create Project**:
```bash
dotnet new analyzer -o src/LankaConnect.Analyzers
```

**Analyzer: Missing Repository Logging**:
```csharp
// src/LankaConnect.Analyzers/MissingRepositoryLoggingAnalyzer.cs
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingRepositoryLoggingAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "LC001";
    private const string Category = "Observability";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "Repository method missing logging",
        "Repository method '{0}' should have logging for observability",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        // Check if class name ends with "Repository"
        var classDeclaration = methodDeclaration.Parent as ClassDeclarationSyntax;
        if (classDeclaration?.Identifier.Text.EndsWith("Repository") != true)
            return;

        // Check if method is public/internal
        if (!methodDeclaration.Modifiers.Any(m =>
            m.IsKind(SyntaxKind.PublicKeyword) ||
            m.IsKind(SyntaxKind.InternalKeyword)))
            return;

        // Check if method body contains logger calls
        var hasLogging = methodDeclaration.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(invocation =>
            {
                var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
                return memberAccess?.Expression.ToString().Contains("_logger") == true;
            });

        if (!hasLogging)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                methodDeclaration.Identifier.GetLocation(),
                methodDeclaration.Identifier.Text);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
```

**Analyzer: Missing Handler Try-Catch**:
```csharp
// src/LankaConnect.Analyzers/MissingHandlerTryCatchAnalyzer.cs
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingHandlerTryCatchAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "LC002";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "Handler missing try-catch",
        "Handler method '{0}' should have try-catch for error handling",
        "Observability",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // Implementation similar to above
}
```

#### Step 2: CI/CD Integration (Week 7, Days 4-5)

**Update GitHub Workflow**:
```yaml
# .github/workflows/build-and-test.yml
- name: Run Analyzers
  run: dotnet build --configuration Release /p:EnforceCodeStyleInBuild=true /p:TreatWarningsAsErrors=true

- name: Check Logging Coverage
  run: |
    dotnet tool install -g dotnet-coverage
    dotnet-coverage collect 'dotnet test' -f xml -o 'coverage.xml'
    # Custom script to check if new files have logging

- name: Verify Health Checks
  run: |
    dotnet run --project src/LankaConnect.API &
    sleep 10
    curl http://localhost:5000/health
    if [ $? -ne 0 ]; then
      echo "Health check endpoint not responding"
      exit 1
    fi
```

#### Step 3: Pre-commit Hooks (Week 8, Day 1)

**Install Husky**:
```bash
npm install -D husky
npx husky init
```

**Create Hook**:
```bash
# .husky/pre-commit
#!/bin/sh

echo "Running pre-commit checks..."

# Check for new repository files
NEW_REPOS=$(git diff --cached --name-only --diff-filter=A | grep "Repository.cs$")
if [ -n "$NEW_REPOS" ]; then
  echo "Checking new repositories for logging..."
  for file in $NEW_REPOS; do
    if ! grep -q "_logger" "$file"; then
      echo "ERROR: $file is missing logging implementation"
      exit 1
    fi
  done
fi

# Check for new handler files
NEW_HANDLERS=$(git diff --cached --name-only --diff-filter=A | grep "Handler.cs$")
if [ -n "$NEW_HANDLERS" ]; then
  echo "Checking new handlers for try-catch..."
  for file in $NEW_HANDLERS; do
    if ! grep -q "try" "$file"; then
      echo "WARNING: $file may be missing try-catch"
    fi
  done
fi

echo "Pre-commit checks passed!"
```

#### Step 4: Documentation (Week 8, Days 2-3)

**Create Observability Guidelines**:
```markdown
# docs/OBSERVABILITY_GUIDELINES.md

## Repository Logging Standards

All repository methods MUST include:
1. Pre-query logging with parameters
2. Try-catch with error logging
3. Performance timing with Stopwatch
4. LogContext for correlation

## Handler Logging Standards

All handlers MUST include:
1. Operation start logging
2. Validation logging
3. Business logic logging
4. Try-catch for exceptions
5. Operation completion logging with duration

## External Service Standards

All external service calls MUST include:
1. Retry logic with Polly
2. Timeout configuration
3. Error logging with context
4. Performance metrics
```

#### Step 5: Training & Knowledge Sharing (Week 8, Days 4-5)

**Create Training Materials**:
1. Video walkthrough of observability improvements
2. Code review checklist
3. Troubleshooting guide with log queries
4. Best practices document

**Team Workshop**:
1. Demo new logging in Azure logs
2. Show how to diagnose issues
3. Review analyzer rules
4. Q&A session

### Phase 5 Deliverables

- [ ] Roslyn analyzers enforcing logging standards
- [ ] CI/CD checks for observability
- [ ] Pre-commit hooks preventing issues
- [ ] Comprehensive documentation
- [ ] Team training completed

### Phase 5 Success Metrics

- Analyzers detect missing logging in PRs
- CI/CD catches observability gaps
- No new code merged without logging
- Team understands new standards
- Documentation accessible

---

## Implementation Timeline

| Week | Phase | Focus | Key Deliverables |
|------|-------|-------|------------------|
| **Week 1** | Phase 1 | Quick Wins | ✅ COMPLETE (SQL logging, startup validation, global exception middleware) |
| **Week 2** | Phase 2 | Repository Logging (Part 1) | Template created, 15 repositories updated |
| **Week 3** | Phase 2 | Repository Logging (Part 2) | 14 repositories updated, external services wrapped |
| **Week 4** | Phase 3 | Handler Logging | 150+ handlers updated, FluentValidation enhanced |
| **Week 5** | Phase 4 | Frontend & Health Checks | Error boundaries, API validation, health checks |
| **Week 6** | Phase 4 | Monitoring & Alerts | Application Insights, alert rules |
| **Week 7** | Phase 5 | Analyzers & CI/CD | Roslyn analyzers, CI/CD checks |
| **Week 8** | Phase 5 | Documentation & Training | Guidelines, training materials, workshop |

---

## Rollout Strategy

### Staging Deployment Cadence

- **Week 2**: Deploy after each repository batch (5 deployments)
- **Week 3**: Deploy after external services (1 deployment)
- **Week 4**: Deploy after each handler batch (3 deployments)
- **Week 5-6**: Deploy after each major frontend/infrastructure change (4 deployments)
- **Week 7-8**: Deploy analyzer changes (2 deployments)

**Total Staging Deployments**: ~15

### Production Deployment

**Criteria for Production**:
1. All phases complete
2. Staging verified for 1 week
3. No critical bugs found
4. Performance metrics acceptable
5. Team trained on new patterns

**Production Deployment**: Week 9

---

## Risk Mitigation

### Risk 1: Performance Impact
**Mitigation**:
- Use async logging (Serilog)
- Sampling for high-volume operations
- Monitor response times in staging
- Rollback plan if performance degrades > 10%

### Risk 2: Log Volume Explosion
**Mitigation**:
- Debug-level logs only in non-production
- Log retention policies (7 days Debug, 30 days Info/Warning, 90 days Error)
- Cost monitoring in Azure
- Sampling for high-frequency operations

### Risk 3: Breaking Changes
**Mitigation**:
- All changes are additive (logging only)
- No public API changes
- Feature flags for new logging
- Gradual rollout repository by repository

### Risk 4: Team Resistance
**Mitigation**:
- Show value with Phase 1 Quick Wins
- Provide templates and automation
- Pair programming sessions
- Recognition for early adopters

---

## Success Criteria

### Overall Success Metrics

- [ ] 91/103 vulnerabilities fixed (Phase 1: 12, Phases 2-5: 91)
- [ ] 100% repository coverage with logging
- [ ] 100% handler coverage with logging
- [ ] 100% external service calls wrapped
- [ ] Health checks operational
- [ ] Application Insights telemetry flowing
- [ ] Roslyn analyzers preventing regressions
- [ ] CI/CD enforcing standards
- [ ] Team trained and adopting patterns
- [ ] Production deployment successful

### Key Performance Indicators (KPIs)

**Before (Phase 1 Only)**:
- Time to diagnose configuration issues: 3+ deployments (hours)
- SQL query visibility: None
- Exception context: Minimal
- External API failures: Silent

**After (Phases 2-5 Complete)**:
- Time to diagnose issues: < 5 minutes (logs show exact failure)
- SQL query visibility: 100% (all queries logged)
- Exception context: Full (RequestId, UserId, Path, Method, Duration)
- External API failures: Logged with retry attempts and context
- Repository query duration: Tracked and alerted
- Handler validation failures: Logged with details
- Frontend errors: Caught and logged
- Health check response time: < 100ms
- Alert response time: < 1 minute

---

## Next Steps

1. **Review this plan** with the team
2. **Prioritize any adjustments** based on business needs
3. **Begin Phase 2** with repository logging template creation
4. **Track progress** using this document as the master plan
5. **Update weekly** with status and blockers

---

## References

- [OBSERVABILITY_IMPLEMENTATION_PLAN.md](./OBSERVABILITY_IMPLEMENTATION_PLAN.md) - Original comprehensive audit
- [PHASE_6AX_OBSERVABILITY_QUICK_WINS_STATUS.md](./PHASE_6AX_OBSERVABILITY_QUICK_WINS_STATUS.md) - Phase 1 status
- [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md) - Overall project tracking
- [StateTaxRateRepository.cs](../src/LankaConnect.Infrastructure/Data/Repositories/StateTaxRateRepository.cs) - Logging template reference

---

**Last Updated**: 2026-01-17
**Next Review**: Week 2, Day 1 (start of Phase 2)
