using FluentValidation;
using LankaConnect.Modules.Payments.Application.Queries;
using LankaConnect.Modules.Payments.Contracts;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LankaConnect.Modules.Payments.Api;

/// <summary>
/// Composition-root DI extension for the Payments module. Mirrors the
/// W4.2 Media / W5.3b Forms / W5.4.b Communications composition pattern.
/// </summary>
/// <remarks>
/// Wave 4.4.b (2026-06-23). Today this module registers only the cross-module
/// Contracts surface (<see cref="IPaymentQueries"/>) + scans Payments.Application
/// for MediatR / FluentValidation registrations. Repository implementations
/// (StripeCustomerRepository, StripeWebhookEventRepository, RefundRequestRepository)
/// stay registered in the legacy <c>LankaConnect.Infrastructure.DependencyInjection</c>
/// until Wave 4.4.d.2 physically relocates them.
/// <para>
/// Per architect Risk #1 Option A ruling (2026-06-23), <c>RefundRequest</c> +
/// <c>RefundRequestLineItem</c> + <c>RegistrationPayment</c> remain Registration
/// aggregate children in <c>LankaConnect.Domain.Events.Entities</c> -- they
/// do NOT move to Payments.Domain at 4.4.d.2 or later.
/// </para>
/// </remarks>
public static class PaymentsModule
{
    public static IServiceCollection AddPaymentsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Wave 4.4.b cross-module Contracts surface.
        services.AddScoped<IPaymentQueries, PaymentQueries>();

        // MediatR / FluentValidation scan of Payments.Application -- pulled
        // forward of the 4.4.c.1 handler moves so subsequent moves don't have
        // to also re-wire DI. Mirrors the CommunicationsModule 5.4.c.1 pattern.
        // The Payments.Application assembly is currently empty of handlers; the
        // scan is a no-op until 4.4.c.1 lands the first command handler.
        var paymentsAppAssembly = typeof(PaymentQueries).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(paymentsAppAssembly));
        services.AddValidatorsFromAssembly(paymentsAppAssembly);

        return services;
    }
}
