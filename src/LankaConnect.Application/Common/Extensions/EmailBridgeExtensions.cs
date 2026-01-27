using LankaConnect.Application.Common.Services;
using LankaConnect.Shared.Email.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LankaConnect.Application.Common.Extensions;

/// <summary>
/// Phase 6A.87: DI registration extension for the email service bridge.
///
/// This is separate from EmailServiceExtensions (in Shared) because:
/// - IEmailServiceBridge is defined in Shared
/// - EmailServiceBridgeAdapter is defined in Application
/// - Application depends on Shared, not vice versa
///
/// Usage in Startup/Program.cs:
///
///   // First, register shared services
///   services.AddTypedEmailServices(configuration);
///
///   // Then, register the bridge adapter
///   services.AddEmailServiceBridge();
/// </summary>
public static class EmailBridgeExtensions
{
    /// <summary>
    /// Registers the EmailServiceBridgeAdapter that connects
    /// TypedEmailServiceAdapter to the existing IEmailService.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddEmailServiceBridge(this IServiceCollection services)
    {
        // Register the bridge adapter
        // This connects the Shared project's IEmailServiceBridge to Application's IEmailService
        services.AddScoped<IEmailServiceBridge, EmailServiceBridgeAdapter>();

        return services;
    }
}
