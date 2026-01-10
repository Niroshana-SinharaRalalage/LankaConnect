using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using FluentValidation;
using LankaConnect.Application.Common.Behaviors;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Options;
using LankaConnect.Application.ReferenceData.Services;

namespace LankaConnect.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Phase 6A.71: Configure commission settings
        services.Configure<CommissionSettings>(
            configuration.GetSection(CommissionSettings.SectionName));

        services.AddOptions<CommissionSettings>()
            .Bind(configuration.GetSection(CommissionSettings.SectionName))
            .ValidateOnStart();

        // Add MediatR
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
        });

        // Add pipeline behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehavior<,>));

        // Add FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Add AutoMapper
        services.AddAutoMapper(assembly);

        // Register application services
        // Phase 6A.47: Reference data service with caching
        services.AddScoped<IReferenceDataService, ReferenceDataService>();

        // Register email-related services (implementations will be provided by Infrastructure layer)
        // These are registered as transient since they will be injected by the Infrastructure layer
        // The actual implementations should be registered in the Infrastructure DependencyInjection

        return services;
    }
}