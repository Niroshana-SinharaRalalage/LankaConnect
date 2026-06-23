using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using FluentValidation;
using LankaConnect.Application.Common.Behaviors;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Options;
using LankaConnect.Application.Events.Services;
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

        // Slice 5 Chunk 2: Two-branch authorization for VenueLayout CRUD endpoints.
        services.AddScoped<ILayoutAuthorizationService, LayoutAuthorizationService>();

        // Slice 5 Chunk 3: Blocks destructive layout edits when seats are held/reserved.
        services.AddScoped<IStructuralEditGuard, StructuralEditGuard>();

        // Phase 8 S8.2.B: Validates that requested seat IDs are eligible for stash
        // on the registration before Stripe Checkout (held in session, not reserved,
        // belong to event's layout). Reusable across the auth + anonymous RSVP paths.
        services.AddScoped<ISeatAssignmentValidator, SeatAssignmentValidator>();

        // Slice 5 Chunk 13: Named-metric emission (layout.created, layout.structural_edit_rejected).
        services.AddScoped<ILayoutMetrics, LayoutMetrics>();

        // Phase 7H: Seat-hold lifecycle metrics (architect §S6 dashboard).
        services.AddScoped<ISeatHoldMetrics, SeatHoldMetrics>();

        // Register email-related services (implementations will be provided by Infrastructure layer)
        // These are registered as transient since they will be injected by the Infrastructure layer
        // The actual implementations should be registered in the Infrastructure DependencyInjection

        return services;
    }
}