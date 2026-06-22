using FluentValidation;
using LankaConnect.Modules.Communications.Application.Queries;
using LankaConnect.Modules.Communications.Contracts;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LankaConnect.Modules.Communications.Api;

/// <summary>
/// Composition-root DI extension for the Communications module. Mirrors the
/// W4.2 Media / W5.3b Forms composition pattern.
/// </summary>
/// <remarks>
/// Wave 5.4.b (2026-06-13). Today this module only registers the cross-module
/// Contracts surface (<see cref="IEmailGroupQueries"/>) so consumers can take
/// the dependency without reaching into <c>LankaConnect.Domain.Communications</c>.
/// <para>
/// The underlying <c>IEmailGroupRepository</c> is still wired in
/// <c>LankaConnect.Infrastructure.DependencyInjection</c> (the EmailGroup
/// aggregate moves to Communications.Domain in Wave 5.4.d.2). When the move
/// lands, the repository registration relocates here, the DbContext (or
/// AppDbContext-share) joins, and a MediatR / FluentValidation assembly scan
/// for the moved Communications.Application handlers is added.
/// </para>
/// </remarks>
public static class CommunicationsModule
{
    public static IServiceCollection AddCommunicationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Wave 5.4.b cross-module Contracts surface. IEmailGroupRepository is
        // injected from LankaConnect.Infrastructure's existing DI registration
        // until W5.4.d.2.
        services.AddScoped<IEmailGroupQueries, EmailGroupQueries>();

        // Wave 5.4.c.1 (2026-06-22): Communications.Application now hosts the 5
        // command handlers (CreateEmailGroup / UpdateEmailGroup / DeleteEmailGroup
        // / CreateNewsletter / UpdateNewsletter) that moved out of
        // LankaConnect.Application. The legacy DependencyInjection.cs only scans
        // its own assembly for MediatR / FluentValidation, so the moved handlers
        // + validators are invisible to the global container without explicit
        // registration here. Scan the Communications.Application assembly for
        // both handler kinds (mirrors the FormsModule 5.3c.2 hotfix pattern).
        var commsAppAssembly = typeof(EmailGroupQueries).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(commsAppAssembly));
        services.AddValidatorsFromAssembly(commsAppAssembly);

        return services;
    }
}
