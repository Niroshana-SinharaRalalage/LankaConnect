using Azure.Storage.Blobs;
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
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.Repositories;
using LankaConnect.Infrastructure.Storage.Configuration;
using LankaConnect.Infrastructure.Storage.Services;
using LankaConnect.Infrastructure.Security.Services;
using LankaConnect.Infrastructure.Security;
using LankaConnect.Infrastructure.Email.Configuration;
using LankaConnect.Infrastructure.Email.Services;
using LankaConnect.Infrastructure.Email.Interfaces;

namespace LankaConnect.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext with enhanced connection pooling and detailed logging
        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("LankaConnect.Infrastructure");
                
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

        // Add Email Services
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.AddScoped<IEmailService, EmailService>();

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

        // Add Image Service
        services.AddScoped<IImageService, BasicImageService>();

        // Add Authentication Services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHashingService, PasswordHashingService>();
        services.AddScoped<ITokenConfiguration, TokenConfiguration>();

        // Add Email Services
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<ISimpleEmailService, SimpleEmailService>();
        services.AddScoped<IEmailTemplateService, RazorEmailTemplateService>();

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

        return services;
    }
}