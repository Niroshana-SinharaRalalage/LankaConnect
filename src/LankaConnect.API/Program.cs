using Hangfire;
using LankaConnect.Modules.Communications.Domain.Repositories;
using LankaConnect.Modules.Communications.Domain.Entities;
using LankaConnect.API.Infrastructure;
using LankaConnect.API.Filters;
using LankaConnect.Application;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.BackgroundJobs;
using LankaConnect.Application.Badges.BackgroundJobs;
using LankaConnect.Application.Communications.BackgroundJobs;
using LankaConnect.BuildingBlocks.Web.Telemetry;
using LankaConnect.Infrastructure;
using LankaConnect.Modules.Communications.Api;
using LankaConnect.Modules.Payments.Api;
using LankaConnect.Modules.Identity.Api;
using LankaConnect.Modules.CulturalIntelligence.Api;
using LankaConnect.Modules.Forms.Api;
using LankaConnect.Modules.Media.Api;
using LankaConnect.Modules.Notifications.Api;
using LankaConnect.Infrastructure.Data;
using LankaConnect.API.Extensions;
using Serilog;
using Serilog.Events;
using Serilog.Context;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using System.Reflection;

// Configure Serilog with enhanced enrichment
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build())
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "LankaConnect.API")
    .Enrich.WithProperty("Version", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0")
    .CreateLogger();

try
{
    Log.Information("Starting LankaConnect API");
    
    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // W2.6b Phase A — OpenTelemetry + Azure Monitor (Application Insights) per
    // ADR-004 architect amendment (observability live before any module work).
    // Wires AspNetCore + HttpClient + SqlClient auto-instrumentation; when the
    // App Insights connection string is configured (staging / prod env var
    // APPLICATIONINSIGHTS_CONNECTION_STRING or config key
    // ApplicationInsights:ConnectionString), traces / metrics / logs ship to
    // App Insights. Local dev with no connection string still registers
    // activity sources for correlation IDs.
    builder.Services.AddBuildingBlocksTelemetry(
        builder.Configuration,
        serviceName: "LankaConnect.API");

    // Add services to the container
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // Enable case-insensitive property name binding for JSON deserialization
            // This allows frontend to send camelCase (email, firstName, lastName)
            // while backend expects PascalCase (Email, FirstName, LastName)
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;

            // Enable string-to-enum conversion for both regular and nullable enums
            // This allows frontend to send enum values as strings (e.g., "EventOrganizer")
            // which the deserializer will properly convert to UserRole enum values
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        })
        // W3.6 Phase A (2026-06-04) — make module-assembly controllers discoverable.
        // MVC auto-discovers controllers in the entry assembly + ApplicationParts;
        // module controllers in referenced assemblies need an explicit ApplicationPart.
        .AddApplicationPart(typeof(LankaConnect.Modules.Notifications.Api.Controllers.NotificationsController).Assembly);

    // Configure FormOptions for large file uploads (videos up to 500MB)
    builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 500L * 1024 * 1024; // 500MB
        options.ValueLengthLimit = 100 * 1024 * 1024; // 100MB (form field values, not files)
        options.MultipartHeadersLengthLimit = 100 * 1024 * 1024; // 100MB
    });

    // Add HttpContextAccessor for CurrentUserService and other services
    builder.Services.AddHttpContextAccessor();

    // W1.5 Phase A — Microsoft.FeatureManagement (ADR-004 feature flag strategy).
    // Reads from "FeatureManagement" section in appsettings*.json; future work
    // (post W1.5) will layer Azure App Configuration for runtime flag flips
    // without redeploy. IFeatureManager is now injectable across all controllers.
    builder.Services.AddFeatureManagement();

    // Add Application Layer (registers MediatR via AddMediatR — provides
    // IMediator/ISender/IPublisher consumed by AppDbContext + handlers)
    builder.Services.AddApplication(builder.Configuration);

    // Add Infrastructure Layer
    builder.Services.AddInfrastructure(builder.Configuration);

    // W3.4 Phase A (2026-06-03) — Notifications module composition. Registers
    // NotificationsDbContext + INotificationRepository. Called AFTER
    // AddInfrastructure so the AppDbContext + Repository<T> base are available
    // for the NotificationRepository transitional edge.
    builder.Services.AddNotificationsModule(builder.Configuration);

    // W4.2 (2026-06-06) — Media module composition.
    builder.Services.AddMediaModule(builder.Configuration);

    // W4.3 (2026-06-06) — Forms module composition. Registers FormsDbContext
    // (cross-schema overrides for events.event_forms / form_questions /
    // form_responses / form_answers) + IFormRepository + IFormResponseRepository.
    builder.Services.AddFormsModule(builder.Configuration);

    // W5.4.b (2026-06-13) — Communications module composition. Registers the
    // cross-module Contracts surface (IEmailGroupQueries). The underlying
    // IEmailGroupRepository is still wired by AddInfrastructure until
    // W5.4.d.2 physical move.
    builder.Services.AddCommunicationsModule(builder.Configuration);

    // Wave 4.4.b (2026-06-23) — Payments module composition. Registers the
    // cross-module Contracts surface (IPaymentQueries) + MediatR /
    // FluentValidation scan of Payments.Application. Repository
    // implementations (StripeCustomerRepository, StripeWebhookEventRepository,
    // RefundRequestRepository) stay registered by AddInfrastructure until
    // Wave 4.4.d.2 physical move. Per architect Risk #1 Option A ruling,
    // RefundRequest stays a Registration aggregate child in
    // LankaConnect.Domain.Events.Entities -- no Payments.Domain skeleton yet.
    builder.Services.AddPaymentsModule(builder.Configuration);

    // Wave 4.6.b (2026-06-24) — Identity module composition. Registers the
    // cross-module Contracts surface (IIdentityQueries) + MediatR /
    // FluentValidation scan of Identity.Application. The underlying
    // IUserRepository stays registered by AddInfrastructure until Wave 4.6.d.2.
    // CurrentUserService adapter relocates into Identity.Infrastructure at
    // Wave 4.6.c.5 + this module's AddScoped<ICurrentUserService, ...> call
    // moves here at the same time.
    builder.Services.AddIdentityModule(builder.Configuration);

    // W4.7 (2026-06-06) — CulturalIntelligence module composition. Registers
    // ICulturalCalendar (StubCulturalCalendar impl).
    builder.Services.AddCulturalIntelligenceModule();

    // Add JWT Authentication and Authorization
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddCustomAuthorization();

    // Add API documentation with JWT support
    // Phase 6A.151 — built-in .NET 8 RateLimiter for the public sponsor
    // staging-blob upload endpoint (POST /sponsors/staging-image). 10/hour per
    // remote IP. Architect H1: this endpoint is [AllowAnonymous] (registration
    // flow is anonymous) so without a per-IP cap it's a free DoS surface.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("sponsor-staging-upload", httpContext =>
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString()
                     ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim()
                     ?? "anonymous";
            return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ip,
                factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromHours(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        });
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "LankaConnect API",
            Version = "v1",
            Description = "Sri Lankan American Community Platform API",
            Contact = new OpenApiContact
            {
                Name = "LankaConnect Team",
                Email = "info@lankaconnect.app"
            }
        });

        // Add document filter for tag definitions (ensures all endpoints visible in Swagger UI)
        c.DocumentFilter<TagDescriptionsDocumentFilter>();

        // Add operation filter for file upload endpoints (handles IFormFile parameters)
        c.OperationFilter<FileUploadOperationFilter>();

        // Add JWT Authentication to Swagger
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement()
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    },
                    Scheme = "oauth2",
                    Name = "Bearer",
                    In = ParameterLocation.Header,
                },
                new List<string>()
            }
        });

        // Include XML comments if available
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    // Add CORS with more specific configuration
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Development", policy =>
        {
            policy.WithOrigins("http://localhost:3000", "https://localhost:3001")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });

        options.AddPolicy("Staging", policy =>
        {
            policy.WithOrigins(
                      "http://localhost:3000",
                      "https://localhost:3001",
                      "https://lankaconnect-staging.azurestaticapps.net")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });

        options.AddPolicy("Production", policy =>
        {
            policy.WithOrigins(
                      "https://lankaconnect.com",
                      "https://www.lankaconnect.com",
                      "https://lankaconnect.app",
                      "https://www.lankaconnect.app")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    // Add Health Checks with comprehensive database and cache monitoring
    builder.Services.AddHealthChecks()
        .AddNpgSql(
            connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
            healthQuery: "SELECT 1",
            name: "PostgreSQL Database",
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
            tags: new[] { "db", "postgresql", "ready" },
            timeout: TimeSpan.FromSeconds(10))
        .AddRedis(
            redisConnectionString: builder.Configuration.GetConnectionString("Redis")!,
            name: "Redis Cache",
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
            tags: new[] { "cache", "redis", "ready" },
            timeout: TimeSpan.FromSeconds(5))
        .AddDbContextCheck<AppDbContext>(
            name: "EF Core DbContext",
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
            tags: new[] { "db", "efcore", "ready" },
            customTestQuery: async (context, cancellationToken) =>
            {
                // Test that we can connect and perform a simple query
                return await context.Database.CanConnectAsync(cancellationToken);
            });

    var app = builder.Build();

    // Phase 6A.X: Validate configuration settings at startup
    ValidateConfiguration(builder.Configuration, app.Services.GetRequiredService<ILogger<Program>>());

    // Phase 6A.X: Validate EF Core configurations at startup (non-Development only - Development runs migrations)
    if (!app.Environment.IsDevelopment())
    {
        await ValidateEfCoreConfigurationsAsync(app.Services);
    }

    // Apply database migrations automatically on startup
    // CRITICAL FIX Phase 6A.61 Hotfix: Only run migrations in Development to avoid dual execution
    // In Staging/Production, migrations are applied exclusively via GitHub Actions CI/CD pipeline (deploy-staging.yml)
    // This prevents permission issues, connection string conflicts, and silent failures
    if (app.Environment.IsDevelopment())
    {
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<AppDbContext>();
                var logger = services.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("Applying database migrations...");
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully");

                // Seed initial data (Development only)
                var dbInitializer = new DbInitializer(
                    context,
                    services.GetRequiredService<ILogger<DbInitializer>>(),
                    services.GetRequiredService<IPasswordHashingService>());
                await dbInitializer.SeedAsync();
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while migrating the database");
                throw; // Re-throw to prevent application startup with incomplete database
            }
        }
    }
    else
    {
        // Phase 6A.61 Hotfix: Log that migrations are skipped in Staging/Production
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation(
            "[Phase 6A.61 Hotfix] Skipping automatic migrations in {Environment} environment. " +
            "Database schema changes are applied via GitHub Actions CI/CD pipeline (deploy-staging.yml lines 101-142).",
            app.Environment.EnvironmentName);
    }

    // Configure the HTTP request pipeline

    // Phase 6A.X: Global exception handling middleware (FIRST to catch all exceptions)
    app.UseMiddleware<LankaConnect.API.Middleware.GlobalExceptionMiddleware>();

    // CRITICAL FIX Phase 6A.12: Use ONLY built-in CORS middleware (removed custom middleware that was conflicting)
    // Apply CORS BEFORE other middleware to handle preflight requests correctly
    // CORS must come before UseHttpsRedirection, UseAuthentication, etc.
    if (app.Environment.IsDevelopment())
        app.UseCors("Development");
    else if (app.Environment.IsStaging())
        app.UseCors("Staging");
    else
        app.UseCors("Production");

    // CRITICAL FIX Phase 6A.14: Ensure CORS headers are preserved even on error responses
    // This middleware runs AFTER UseCors but captures the Origin header so we can re-apply CORS headers if an error occurs
    app.Use(async (context, next) =>
    {
        // Capture the origin BEFORE calling next middleware
        var origin = context.Request.Headers.Origin.ToString();
        var allowedOrigins = app.Environment.IsDevelopment()
            ? new[] { "http://localhost:3000", "https://localhost:3001" }
            : app.Environment.IsStaging()
                ? new[] { "http://localhost:3000", "https://localhost:3001", "https://lankaconnect-staging.azurestaticapps.net" }
                : new[] { "https://lankaconnect.com", "https://www.lankaconnect.com", "https://lankaconnect.app", "https://www.lankaconnect.app" };

        try
        {
            await next();

            // PHASE 6A.14: Also ensure CORS headers on successful responses
            // In case they were lost somewhere in the pipeline
            if (!string.IsNullOrEmpty(origin) && allowedOrigins.Contains(origin))
            {
                if (!context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
                {
                    context.Response.Headers["Access-Control-Allow-Origin"] = origin;
                    context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
                }
            }
        }
        catch (Exception ex)
        {
            // PHASE 6A.14: Always add CORS headers BEFORE starting response
            // This ensures headers are present even if exception occurs early in pipeline
            if (!string.IsNullOrEmpty(origin) && allowedOrigins.Contains(origin))
            {
                // Check if we can still modify headers
                if (!context.Response.HasStarted)
                {
                    context.Response.Headers["Access-Control-Allow-Origin"] = origin;
                    context.Response.Headers["Access-Control-Allow-Credentials"] = "true";

                    // Also add Vary header to prevent caching issues
                    context.Response.Headers["Vary"] = "Origin";
                }
            }

            // Log the exception for debugging
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Unhandled exception in request pipeline for {Method} {Path}. Origin: {Origin}. CORS headers added: {CorsAdded}",
                context.Request.Method,
                context.Request.Path,
                origin,
                !context.Response.HasStarted);

            // Re-throw the exception so error handling middleware can process it
            throw;
        }
    });

    // Swagger enabled in all environments for API testing and verification
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LankaConnect API V1");
        c.RoutePrefix = "swagger"; // Available at /swagger in production
        c.DisplayRequestDuration();
    });

    if (!app.Environment.IsDevelopment() && !app.Environment.IsStaging())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    // Phase 6A.116: Prevent Azure Container Apps ingress from caching mutation responses
    // Root cause: Ingress was caching POST/PUT/DELETE responses, causing requests to never reach application
    app.Use(async (context, next) =>
    {
        // For non-GET requests, add no-cache headers to response
        if (context.Request.Method != HttpMethod.Get.Method)
        {
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey("Cache-Control"))
                {
                    context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
                    context.Response.Headers["Pragma"] = "no-cache";
                    context.Response.Headers["Expires"] = "0";
                }
                return Task.CompletedTask;
            });
        }

        await next();
    });

    // Add correlation ID middleware
    app.Use(async (context, next) =>
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                          ?? context.Request.Headers["X-Request-ID"].FirstOrDefault()
                          ?? Guid.NewGuid().ToString();
        
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            context.Response.Headers.TryAdd("X-Correlation-ID", correlationId);
            await next();
        }
    });

    // Add enhanced request logging with structured data
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.GetLevel = (httpContext, elapsed, ex) => ex != null
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode > 499
                ? LogEventLevel.Error
                : LogEventLevel.Information;

        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault() ?? "Unknown");
            diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
            diagnosticContext.Set("RequestSize", httpContext.Request.ContentLength ?? 0);
            diagnosticContext.Set("ResponseSize", httpContext.Response.ContentLength ?? 0);

            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value ?? httpContext.User.FindFirst("id")?.Value ?? "Unknown");
                diagnosticContext.Set("UserName", httpContext.User.Identity.Name ?? "Unknown");
            }
        };
    });

    // CRITICAL FIX: Enable routing BEFORE authentication so [AllowAnonymous] is respected
    // This is essential for webhook endpoints that don't use JWT authentication
    app.UseRouting();

    // Authentication & Authorization
    app.UseCustomAuthentication();

    // Phase 6A.151 — RateLimiter must come after auth (so it can read the actor
    // when policies need it) but before MapControllers so [EnableRateLimiting]
    // attributes are honored.
    app.UseRateLimiter();

    // Map controllers AFTER authentication middleware
    app.MapControllers();

    // Add Health Check endpoint with detailed response
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var response = new
            {
                Status = report.Status.ToString(),
                Checks = report.Entries.Select(x => new
                {
                    Name = x.Key,
                    Status = x.Value.Status.ToString(),
                    Description = x.Value.Description,
                    Duration = x.Value.Duration.ToString()
                }),
                TotalDuration = report.TotalDuration.ToString()
            };
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }
    });

    // Add Hangfire Dashboard (Epic 2 Phase 5)
    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
        DisplayStorageConnectionString = false,
        DashboardTitle = "LankaConnect Background Jobs"
    });


    // Register Recurring Jobs (Epic 2 Phase 5)
    using (var scope = app.Services.CreateScope())
    {
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Registering Hangfire recurring jobs...");

        // Event Reminder Job - Runs hourly to send reminders for events starting in 24 hours
        recurringJobManager.AddOrUpdate<EventReminderJob>(
            "event-reminder-job",
            job => job.ExecuteAsync(),
            Cron.Hourly, // Run every hour
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // Event Status Update Job - Runs hourly to update event statuses (Published->Active, Active->Completed)
        recurringJobManager.AddOrUpdate<EventStatusUpdateJob>(
            "event-status-update-job",
            job => job.ExecuteAsync(),
            Cron.Hourly, // Run every hour
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // Phase 6A.27: Expired Badge Cleanup Job - Runs daily to remove expired badges from events
        recurringJobManager.AddOrUpdate<ExpiredBadgeCleanupJob>(
            "expired-badge-cleanup-job",
            job => job.ExecuteAsync(),
            Cron.Daily, // Run once daily at midnight UTC
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // Phase 6A.81: Cleanup Abandoned Registrations Job - Runs hourly to mark expired Preliminary registrations as Abandoned
        recurringJobManager.AddOrUpdate<CleanupAbandonedRegistrationsJob>(
            "cleanup-abandoned-registrations-job",
            job => job.ExecuteAsync(),
            Cron.Hourly, // Run every hour (Stripe checkout expires at 24h)
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // Cleanup Abandoned Add-On Purchases Job - Runs hourly to mark expired Pending add-on purchases as Abandoned and restore stock
        recurringJobManager.AddOrUpdate<CleanupAbandonedAddOnPurchasesJob>(
            "cleanup-abandoned-addon-purchases-job",
            job => job.ExecuteAsync(),
            Cron.Hourly, // Run every hour (Stripe checkout expires at 24h)
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        // Phase 7D Fix 4: Expire Unverified WhatsApp Preferences Job - Daily sweep that auto-disables
        // WhatsApp for users who enabled it but never verified within WhatsAppSettings:UnverifiedGracePeriodDays
        // (default 30). Fires WhatsAppAutoDisabledDomainEvent → notification email per user.
        recurringJobManager.AddOrUpdate<ExpireUnverifiedWhatsAppPreferencesJob>(
            "expire-unverified-whatsapp-preferences-job",
            job => job.ExecuteAsync(),
            Cron.Daily(3), // 03:00 UTC — off-peak, after nightly backups
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        logger.LogInformation("Hangfire recurring jobs registered successfully");
    }

    Log.Information("LankaConnect API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Phase 6A.X: Configuration validation methods
static void ValidateConfiguration(IConfiguration configuration, Microsoft.Extensions.Logging.ILogger logger)
{
    logger.LogInformation("Phase 6A.X: Validating configuration settings at startup");

    var requiredSettings = new Dictionary<string, bool>
    {
        // Critical settings (fail fast if missing)
        { "ConnectionStrings:DefaultConnection", true },
        { "Jwt:Key", true },
        { "Jwt:Issuer", true },
        { "Jwt:Audience", true },

        // Optional settings based on features (warn if missing)
        { "AzureStorage:ConnectionString", false },
        { "Stripe:SecretKey", false },
        { "Stripe:PublishableKey", false },
        { "EmailSettings:AzureConnectionString", false }
    };

    var errors = new List<string>();
    var warnings = new List<string>();

    foreach (var (key, required) in requiredSettings)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
            {
                errors.Add($"Missing required configuration: {key}");
                logger.LogCritical("Configuration validation: Missing required setting {Key}", key);
            }
            else
            {
                warnings.Add($"Missing optional configuration: {key} (feature may be disabled)");
                logger.LogWarning("Configuration validation: Missing optional setting {Key} - feature may be disabled", key);
            }
        }
        else
        {
            logger.LogInformation("Configuration validation: Setting {Key} is present", key);
        }
    }

    if (errors.Any())
    {
        logger.LogCritical(
            "Configuration validation FAILED with {ErrorCount} critical errors:\n{Errors}",
            errors.Count,
            string.Join("\n", errors));
        throw new InvalidOperationException($"Configuration validation failed. Missing required settings:\n{string.Join("\n", errors)}");
    }

    if (warnings.Any())
    {
        logger.LogWarning(
            "Configuration validation completed with {WarningCount} warnings:\n{Warnings}",
            warnings.Count,
            string.Join("\n", warnings));
    }
    else
    {
        logger.LogInformation("Configuration validation PASSED - all required settings present");
    }
}

static async Task ValidateEfCoreConfigurationsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Phase 6A.X: Validating EF Core configurations at startup");

    try
    {
        // Step 1: Validate migrations are applied
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            logger.LogWarning(
                "EF Core validation: Pending migrations detected - {Count} migrations not applied: {Migrations}",
                pendingMigrations.Count(),
                string.Join(", ", pendingMigrations));
        }
        else
        {
            logger.LogInformation("EF Core validation: All migrations applied");
        }

        // Step 2: Validate database connection
        var canConnect = await context.Database.CanConnectAsync();
        if (!canConnect)
        {
            logger.LogCritical("EF Core validation: Cannot connect to database");
            throw new InvalidOperationException("Cannot connect to database - check connection string");
        }

        logger.LogInformation("EF Core validation: Database connection successful");

        // Step 3: Test critical DbSets to validate configurations
        // This will throw if column mappings don't match database schema (like StateTaxRateConfiguration Phase 6A.X)
        var criticalEntityTests = new List<(string EntityName, Func<Task> TestQuery)>
        {
            ("StateTaxRates", async () => await context.StateTaxRates.AsNoTracking().FirstOrDefaultAsync()),
            ("Events", async () => await context.Events.AsNoTracking().FirstOrDefaultAsync()),
            ("Users", async () => await context.Users.AsNoTracking().FirstOrDefaultAsync()),
            ("Registrations", async () => await context.Registrations.AsNoTracking().FirstOrDefaultAsync())
        };

        foreach (var (entityName, testQuery) in criticalEntityTests)
        {
            try
            {
                await testQuery();
                logger.LogInformation("EF Core validation: {EntityName} configuration VALID", entityName);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex,
                    "EF Core validation: {EntityName} configuration INVALID - {ErrorMessage}",
                    entityName,
                    ex.Message);
                throw new InvalidOperationException(
                    $"EF Core configuration validation failed for {entityName}. " +
                    $"Error: {ex.Message}. " +
                    $"This likely indicates a mismatch between entity configuration (HasColumnName) and database schema.",
                    ex);
            }
        }

        logger.LogInformation("EF Core configuration validation PASSED - all critical entities validated");
    }
    catch (Exception ex) when (ex is not InvalidOperationException)
    {
        logger.LogCritical(ex,
            "EF Core validation: UNEXPECTED ERROR during validation - {ErrorMessage}",
            ex.Message);
        throw new InvalidOperationException(
            $"EF Core configuration validation failed with unexpected error: {ex.Message}",
            ex);
    }
}

// Make Program class public for testing
public partial class Program { }