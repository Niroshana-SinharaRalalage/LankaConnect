using FluentValidation;
using LankaConnect.Application.Events.Services; // W4.4.c.4: refund service interfaces remain in legacy (avoids circular ref from 4 cross-module consumers).
using LankaConnect.Modules.Payments.Application.Queries;
using LankaConnect.Modules.Payments.Application.Services;
using LankaConnect.Modules.Payments.Contracts;
using LankaConnect.Modules.Payments.Domain.Repositories; // W4.4.d.2: 3 repo interfaces moved here
using LankaConnect.Modules.Payments.Infrastructure.Repositories; // W4.4.d.2: 3 repo impls moved here
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
/// aggregate children in <c>LankaConnect.Products.LankaEvents.Domain.Entities</c> -- they
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

        // Wave 4.4.c.4 (2026-06-23): the 6 refund service registrations move here
        // from LankaConnect.Application.DependencyInjection alongside the physical
        // move of their files into Payments.Application/Services/. Lifetimes
        // preserved from the legacy registrations -- IRefundLineDispatcher stays
        // a singleton because RefundLineDispatcher captures IServiceScopeFactory
        // once and is stateless (Phase 6A.148.W5.D2 contract).
        services.AddScoped<IRegistrationRefundService, RegistrationRefundService>();
        services.AddScoped<IRefundReconciliationService, RefundReconciliationService>();
        services.AddScoped<IAddOnRefundService, AddOnRefundService>();
        services.AddScoped<IRefundExecutionService, RefundExecutionService>();
        services.AddSingleton<IRefundLineDispatcher, RefundLineDispatcher>();
        services.AddScoped<IRefundTotalCalculator, RefundTotalCalculator>();

        // Wave 4.4.d.2 (2026-06-23): 3 repository registrations relocated from
        // LankaConnect.Infrastructure.DependencyInjection alongside the physical
        // file move into Payments.Domain (interfaces) + Payments.Infrastructure
        // (impls). Mirrors the W5.4.d.2 CommunicationsModule repository pattern.
        services.AddScoped<IStripeCustomerRepository, StripeCustomerRepository>();
        services.AddScoped<IStripeWebhookEventRepository, StripeWebhookEventRepository>();
        services.AddScoped<IRefundRequestRepository, RefundRequestRepository>();

        return services;
    }
}
