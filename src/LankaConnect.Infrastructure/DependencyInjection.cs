using Azure.Storage.Blobs;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Community;
using LankaConnect.Domain.Notifications;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.ReferenceData.Interfaces;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.Repositories;
using LankaConnect.Infrastructure.Storage.Configuration;
using LankaConnect.Infrastructure.Storage.Services;
using LankaConnect.Infrastructure.Security.Services;
using LankaConnect.Infrastructure.Security;
using LankaConnect.Infrastructure.Email.Configuration;
using LankaConnect.Infrastructure.Email.Services;
using LankaConnect.Infrastructure.Email.Interfaces;
using LankaConnect.Infrastructure.Services;
using LankaConnect.Application.Communications.BackgroundJobs;
using LankaConnect.Application.Common.Options;
using LankaConnect.Infrastructure.Payments.Configuration;
using LankaConnect.Infrastructure.Payments.Repositories;
using LankaConnect.Infrastructure.Payments.Services;
using LankaConnect.Infrastructure.Services.Tickets;
using LankaConnect.Domain.Payments;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Tax.Repositories;
using Stripe;
using Serilog;

namespace LankaConnect.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext with enhanced connection pooling and detailed logging
        // Configure NpgsqlDataSource with dynamic JSON support (required for List<string> and other dynamic types)
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);

        // Enable dynamic JSON serialization for List<string>, List<T>, etc. (Npgsql 8.0+ requirement)
        // This is required for properties like Event.PhotoUrls which are List<string>
        // See: https://www.npgsql.org/doc/types/json.html
        dataSourceBuilder.EnableDynamicJson();

        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(dataSource, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("LankaConnect.Infrastructure");

                // Enable NetTopologySuite for PostGIS spatial support (Epic 2 Phase 1)
                npgsqlOptions.UseNetTopologySuite();

                // Enhanced retry configuration
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);

                // Command timeout configuration (30 seconds)
                npgsqlOptions.CommandTimeout(30);
            });

            // Enhanced logging configuration based on environment
            var isDevelopment = configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Development";
            var enableDetailedErrors = configuration.GetValue<bool>("DatabaseSettings:EnableDetailedErrors", isDevelopment);
            var enableSensitiveLogging = configuration.GetValue<bool>("DatabaseSettings:EnableSensitiveDataLogging", isDevelopment);

            if (enableDetailedErrors)
            {
                options.EnableDetailedErrors();
            }

            if (enableSensitiveLogging)
            {
                options.EnableSensitiveDataLogging();
            }

            // Query performance tracking
            options.LogTo(
                message => System.Diagnostics.Debug.WriteLine(message),
                Microsoft.Extensions.Logging.LogLevel.Information);

        }, ServiceLifetime.Scoped); // Explicitly set lifetime for connection pooling

        // Add Memory Cache (required by email template service)
        services.AddMemoryCache();

        // Add Redis Cache with enhanced configuration
        services.AddStackExchangeRedisCache(options =>
        {
            var redisConnectionString = configuration.GetConnectionString("Redis");
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "LankaConnect";
                
                // Configure connection pooling and timeout
                var configOptions = StackExchange.Redis.ConfigurationOptions.Parse(redisConnectionString);
                configOptions.ConnectTimeout = 5000; // 5 seconds
                configOptions.SyncTimeout = 5000;
                configOptions.AsyncTimeout = 5000;
                configOptions.ConnectRetry = 3;
                configOptions.KeepAlive = 60;
                configOptions.AbortOnConnectFail = false;
                
                options.ConfigurationOptions = configOptions;
            }
        });

        // JWT configuration will be added in the API layer
        // This keeps infrastructure layer independent of ASP.NET Core
        
        // Register IApplicationDbContext interface
        services.AddScoped<IApplicationDbContext>(provider => provider.GetService<AppDbContext>()!);

        // Add Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Add Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBusinessRepository, BusinessRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IRegistrationRepository, RegistrationRepository>();
        services.AddScoped<IForumTopicRepository, ForumTopicRepository>();
        services.AddScoped<IReplyRepository, ReplyRepository>();
        
        // Add Communications Repositories
        services.AddScoped<IEmailMessageRepository, EmailMessageRepository>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IUserEmailPreferencesRepository, UserEmailPreferencesRepository>();
        services.AddScoped<IEmailStatusRepository, EmailStatusRepository>();
        services.AddScoped<INewsletterSubscriberRepository, NewsletterSubscriberRepository>();
        services.AddScoped<INewsletterRepository, NewsletterRepository>(); // Phase 6A.74: Newsletter Management

        // Add Notifications Repositories (Phase 6A.6)
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Add Badge Repositories (Phase 6A.25)
        services.AddScoped<IBadgeRepository, BadgeRepository>();

        // Add Email Group Repository (Phase 6A.25)
        services.AddScoped<IEmailGroupRepository, EmailGroupRepository>();

        // Add Metro Area Repository (Phase 6A Event Notifications)
        services.AddScoped<IMetroAreaRepository, MetroAreaRepository>();

        // Phase 6A.71: Event Reminder Tracking Repository
        services.AddScoped<LankaConnect.Application.Events.Repositories.IEventReminderRepository, EventReminderRepository>();

        // Phase 6A.61: Event Notification History Repository
        services.AddScoped<LankaConnect.Application.Events.Repositories.IEventNotificationHistoryRepository, EventNotificationHistoryRepository>();

        // Add Analytics Repositories (Epic 2 Phase 3)
        services.AddScoped<LankaConnect.Domain.Analytics.IEventAnalyticsRepository, EventAnalyticsRepository>();
        services.AddScoped<LankaConnect.Domain.Analytics.IEventViewRecordRepository, EventViewRecordRepository>();

        // Add Reference Data Repository (Phase 6A.47)
        services.AddScoped<IReferenceDataRepository, LankaConnect.Infrastructure.Data.Repositories.ReferenceData.ReferenceDataRepository>();

        // Phase 6A.X: Add Tax and Revenue Breakdown Services
        services.AddScoped<IStateTaxRateRepository, StateTaxRateRepository>();
        services.AddScoped<ISalesTaxService>(provider =>
        {
            var repository = provider.GetRequiredService<IStateTaxRateRepository>();
            var memoryCache = provider.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            var logger = Log.ForContext<DatabaseSalesTaxService>();
            return new DatabaseSalesTaxService(repository, memoryCache, logger);
        });
        services.AddScoped<IRevenueCalculatorService>(provider =>
        {
            var salesTaxService = provider.GetRequiredService<ISalesTaxService>();
            var commissionSettings = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CommissionSettings>>();
            var logger = Log.ForContext<RevenueCalculatorService>();
            return new RevenueCalculatorService(salesTaxService, commissionSettings, logger);
        });

        // Add Email Services (IEmailService via AzureEmailService - supports Azure SDK and SMTP fallback)
        // Note: EmailSettings is configured below with SimpleEmailService
        services.AddScoped<IEmailService, AzureEmailService>();

        // Phase 6A.47/6A.53: Add ApplicationUrlsService for email verification URLs
        services.AddScoped<IApplicationUrlsService, ApplicationUrlsService>();

        // Phase 6A.70: Add EmailUrlHelper for centralized URL building in email templates
        services.AddScoped<IEmailUrlHelper, EmailUrlHelper>();

        // Add Azure Storage Services
        services.Configure<AzureStorageOptions>(configuration.GetSection(AzureStorageOptions.SectionName));
        
        // Configure Azure Blob Storage client
        services.AddSingleton<BlobServiceClient>(provider =>
        {
            var storageOptions = configuration.GetSection(AzureStorageOptions.SectionName).Get<AzureStorageOptions>();
            var isDevelopment = configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Development";
            
            string connectionString;
            if (isDevelopment || string.IsNullOrEmpty(storageOptions?.ConnectionString))
            {
                // Use Azurite for local development
                connectionString = storageOptions?.Azurite.ConnectionString ?? 
                    "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";
            }
            else
            {
                // Use Azure Storage for production
                connectionString = storageOptions.ConnectionString;
            }

            return new BlobServiceClient(connectionString);
        });

        // Phase 6A.9: Azure Blob Storage and Image Service
        services.AddScoped<IAzureBlobStorageService, LankaConnect.Infrastructure.Services.AzureBlobStorageService>();
        services.AddScoped<IImageService, LankaConnect.Infrastructure.Services.ImageService>();

        // Add Authentication Services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHashingService, PasswordHashingService>();
        services.AddScoped<ITokenConfiguration, TokenConfiguration>();
        services.AddScoped<ICurrentUserService, CurrentUserService>(); // Phase 6A.6

        // Add Entra External ID Services
        services.Configure<EntraExternalIdOptions>(configuration.GetSection(EntraExternalIdOptions.SectionName));
        services.AddScoped<IEntraExternalIdService, EntraExternalIdService>();

        // Add Email Services
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<ISimpleEmailService, SimpleEmailService>();

        // Phase 0 (Email System): Register new configuration options
        services.Configure<ApplicationUrlsOptions>(configuration.GetSection(ApplicationUrlsOptions.SectionName));
        services.Configure<BrandingOptions>(configuration.GetSection(BrandingOptions.SectionName));

        // Phase 6A.43 Fix: Register AzureEmailService for both IEmailService and IEmailTemplateService
        // This ensures all emails (free and paid) use database-stored templates consistently
        // Previously: IEmailTemplateService → RazorEmailTemplateService (filesystem templates)
        // Now: IEmailTemplateService → AzureEmailService (database templates)
        services.AddScoped<IEmailTemplateService>(provider => provider.GetRequiredService<IEmailService>() as IEmailTemplateService
            ?? throw new InvalidOperationException("IEmailService must implement IEmailTemplateService"));

        // Phase 6A.37: Add HttpClient for email branding service to download images
        services.AddHttpClient();

        // Phase 6A.35/6A.37: Add Email Branding Service for CID inline image embedding
        services.AddScoped<IEmailBrandingService, EmailBrandingService>();

        // Add Email Queue Processor (Background Service)
        services.AddHostedService<EmailQueueProcessor>();

        // Add Cultural Intelligence Services (Stub implementations for MVP - Phase 2 will add real implementations)
        services.AddScoped<LankaConnect.Domain.Events.Services.ICulturalCalendar, LankaConnect.Infrastructure.CulturalIntelligence.StubCulturalCalendar>();
        services.AddScoped<LankaConnect.Domain.Events.Services.IUserPreferences, LankaConnect.Infrastructure.CulturalIntelligence.StubUserPreferences>();
        services.AddScoped<LankaConnect.Domain.Events.Services.IGeographicProximityService, LankaConnect.Infrastructure.CulturalIntelligence.StubGeographicProximityService>();
        services.AddScoped<LankaConnect.Domain.Events.Services.IEventRecommendationEngine, LankaConnect.Domain.Events.Services.EventRecommendationEngine>();

        // Add GeoLocation Service for distance calculations
        services.AddScoped<LankaConnect.Domain.Events.Services.IGeoLocationService, LankaConnect.Domain.Events.Services.GeoLocationService>();

        // Add Event Notification Recipient Service (Phase 6A Event Notifications)
        services.AddScoped<LankaConnect.Domain.Events.Services.IEventNotificationRecipientService, LankaConnect.Application.Events.Services.EventNotificationRecipientService>();

        // Phase 6A.74: Newsletter Recipient Service
        services.AddScoped<LankaConnect.Application.Communications.Services.INewsletterRecipientService, LankaConnect.Infrastructure.Services.NewsletterRecipientService>();

        // Phase 6A.74: Newsletter Background Jobs
        services.AddTransient<NewsletterEmailJob>();

        // Add Cultural Intelligence Cache Service
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var redisConnectionString = configuration.GetConnectionString("Redis");

            if (string.IsNullOrEmpty(redisConnectionString))
            {
                throw new InvalidOperationException("Redis connection string is not configured");
            }

            return ConnectionMultiplexer.Connect(redisConnectionString);
        });

        // Add Hangfire Background Jobs (Epic 2 Phase 5)
        services.AddHangfire(hangfireConfig =>
        {
            hangfireConfig
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"));
                });
        });

        // Add Hangfire server
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 1; // Start with 1 worker for development
            options.SchedulePollingInterval = TimeSpan.FromMinutes(1); // Check for scheduled jobs every minute
        });

        // Add Stripe Services (Phase 6A.4: Stripe Payment Integration - MVP)
        services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));

        // Configure Stripe client as singleton
        services.AddSingleton<IStripeClient>(provider =>
        {
            var stripeOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<StripeOptions>>().Value;

            if (string.IsNullOrWhiteSpace(stripeOptions.SecretKey))
            {
                throw new InvalidOperationException(
                    "Stripe:SecretKey is not configured. Please add it to appsettings.json or environment variables.");
            }

            return new StripeClient(stripeOptions.SecretKey);
        });

        // Register Stripe repositories
        services.AddScoped<IStripeCustomerRepository, StripeCustomerRepository>();
        services.AddScoped<IStripeWebhookEventRepository, StripeWebhookEventRepository>();

        // Session 23 (Phase 2B): Register Stripe payment service for event tickets
        services.AddScoped<IStripePaymentService, StripePaymentService>();

        // Phase 6A.24: Ticket services for QR code and PDF generation
        services.AddScoped<IQrCodeService, QrCodeService>();
        services.AddScoped<IPdfTicketService, PdfTicketService>();
        services.AddScoped<ITicketService, TicketService>();

        // Phase 6A.45: Export services for attendee management
        services.AddScoped<IExcelExportService, LankaConnect.Infrastructure.Services.Export.ExcelExportService>();
        services.AddScoped<ICsvExportService, LankaConnect.Infrastructure.Services.Export.CsvExportService>();
        services.AddScoped<ITicketRepository, TicketRepository>();

        return services;
    }
}