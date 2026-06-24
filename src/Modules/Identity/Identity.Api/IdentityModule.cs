using FluentValidation;
using LankaConnect.Modules.Identity.Application.Queries;
using LankaConnect.Modules.Identity.Contracts;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LankaConnect.Modules.Identity.Api;

/// <summary>
/// Composition-root DI extension for the Identity module. Mirrors the
/// W4.4 Payments / W5.4 Communications / W5.3 Forms composition pattern.
/// </summary>
/// <remarks>
/// Wave 4.6.b (2026-06-24). Today this module registers only the cross-module
/// Contracts surface (<see cref="IIdentityQueries"/>) + scans Identity.Application
/// for MediatR / FluentValidation registrations. The underlying
/// <c>IUserRepository</c> is still wired by <c>AddInfrastructure</c> until
/// Wave 4.6.d.2 physical move. <see cref="IIdentityCommands"/> adapter +
/// the moved Auth/Users command + query handlers land at 4.6.c.1 - c.4.
/// <c>CurrentUserService</c> adapter relocates here at 4.6.c.5 + this method
/// gains the matching <c>AddScoped&lt;ICurrentUserService, CurrentUserService&gt;()</c>
/// call.
/// </remarks>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Wave 4.6.b cross-module Contracts surface.
        services.AddScoped<IIdentityQueries, IdentityQueries>();

        // MediatR / FluentValidation scan of Identity.Application -- pulled
        // forward of the 4.6.c.1 handler moves so subsequent moves don't have
        // to also re-wire DI. Mirrors the PaymentsModule 4.4.b pattern. The
        // Identity.Application assembly is currently empty of handlers; the
        // scan is a no-op until 4.6.c.1 lands the first command handler.
        var identityAppAssembly = typeof(IdentityQueries).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(identityAppAssembly));
        services.AddValidatorsFromAssembly(identityAppAssembly);

        return services;
    }
}
