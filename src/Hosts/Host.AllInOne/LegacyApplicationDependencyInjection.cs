using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using FluentValidation;
using LankaConnect.Application.Common.Behaviors;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Options;
// W5.2.d (2026-06-28): Events.Services moved to Products.LankaEvents.Application.Services.
// The 5 Event-specific service registrations (LayoutAuthorizationService,
// StructuralEditGuard, SeatAssignmentValidator, LayoutMetrics, SeatHoldMetrics)
// move from this file to LankaEventsModule.AddLankaEventsModule(). No using directive
// needed here for those types anymore.
using LankaConnect.Application.ReferenceData.Services;
namespace LankaConnect.Host.AllInOne;

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

        // Phase 6A.133: Configure event settings (co-organizer limits)
        services.Configure<EventSettings>(
            configuration.GetSection(EventSettings.SectionName));

        services.AddOptions<EventSettings>()
            .Bind(configuration.GetSection(EventSettings.SectionName))
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

        // Wave 4.4.c.4 (2026-06-23): the 6 refund service registrations relocated to
        // PaymentsModule.cs alongside the physical move of the service files into
        // Payments.Application/Services/. See AddPaymentsModule for the new home:
        //   - IRegistrationRefundService -> RegistrationRefundService
        //   - IRefundReconciliationService -> RefundReconciliationService
        //   - IAddOnRefundService -> AddOnRefundService
        //   - IRefundExecutionService -> RefundExecutionService
        //   - IRefundLineDispatcher -> RefundLineDispatcher (singleton)
        //   - IRefundTotalCalculator -> RefundTotalCalculator

        // W5.2.d (2026-06-28): Event-specific service registrations
        // (LayoutAuthorizationService, StructuralEditGuard, SeatAssignmentValidator,
        // LayoutMetrics, SeatHoldMetrics) moved to LankaEventsModule.AddLankaEventsModule()
        // because the service implementations + interfaces are now in
        // LankaConnect.Products.LankaEvents.Application.Services and this assembly
        // doesn't reference Products (would cycle).

        // Register email-related services (implementations will be provided by Infrastructure layer)
        // These are registered as transient since they will be injected by the Infrastructure layer
        // The actual implementations should be registered in the Infrastructure DependencyInjection

        return services;
    }
}