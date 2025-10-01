using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using FluentValidation;
using LankaConnect.Application.Common.Behaviors;
using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Add MediatR
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
        });

        // Add pipeline behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CulturalIntelligenceCachingBehavior<,>));

        // Add FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Add AutoMapper
        services.AddAutoMapper(assembly);

        // Register email-related services (implementations will be provided by Infrastructure layer)
        // These are registered as transient since they will be injected by the Infrastructure layer
        // The actual implementations should be registered in the Infrastructure DependencyInjection

        return services;
    }
}