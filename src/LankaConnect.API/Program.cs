using Hangfire;
using LankaConnect.API.Infrastructure;
using LankaConnect.API.Filters;
using LankaConnect.Application;
using LankaConnect.Application.Events.BackgroundJobs;
using LankaConnect.Infrastructure;
using LankaConnect.Infrastructure.Data;
using LankaConnect.API.Extensions;
using Serilog;
using Serilog.Events;
using Serilog.Context;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
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

    // Add services to the container
    builder.Services.AddControllers();

    // Add Application Layer
    builder.Services.AddApplication();

    // Add Infrastructure Layer
    builder.Services.AddInfrastructure(builder.Configuration);

    // Add JWT Authentication and Authorization
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddCustomAuthorization();

    // Add API documentation with JWT support
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
                Email = "support@lankaconnect.com"
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
        
        options.AddPolicy("Production", policy =>
        {
            policy.WithOrigins("https://lankaconnect.com", "https://www.lankaconnect.com")
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

    // Apply database migrations automatically on startup
    // This ensures the database schema is always up-to-date in all environments
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
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating the database");
            throw; // Re-throw to prevent application startup with incomplete database
        }
    }

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    {
        // TEMPORARILY DISABLED: Testing Swagger endpoint generation issue
        // app.UseSwagger();
        // app.UseSwaggerUI(c =>
        // {
        //     c.SwaggerEndpoint("/swagger/v1/swagger.json", "LankaConnect API V1");
        //     c.RoutePrefix = string.Empty; // Make Swagger available at root
        //     c.DisplayRequestDuration();
        // });

        app.UseCors(app.Environment.IsDevelopment() ? "Development" : "Production");
    }
    else
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
        app.UseCors("Production");
    }

    app.UseHttpsRedirection();

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

    // Authentication & Authorization
    app.UseCustomAuthentication();

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

// Make Program class public for testing
public partial class Program { }