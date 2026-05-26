using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace LankaConnect.BuildingBlocks.Web.ProblemDetails;

/// <summary>
/// DI + middleware wiring for the cross-cutting <see cref="GlobalExceptionHandler"/>
/// + RFC 7807 problem-details responses across the API surface.
/// </summary>
public static class ProblemDetailsExtensions
{
    /// <summary>
    /// Registers the <see cref="GlobalExceptionHandler"/> as an
    /// <see cref="IExceptionHandler"/> and enables <c>AddProblemDetails()</c>
    /// for the framework's built-in problem-details responses.
    /// </summary>
    public static IServiceCollection AddBuildingBlocksProblemDetails(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    /// <summary>
    /// Adds the <see cref="GlobalExceptionHandler"/> + status-code-pages
    /// middleware to the pipeline. Should be called BEFORE other middleware
    /// so it catches exceptions from downstream handlers.
    /// </summary>
    public static WebApplication UseBuildingBlocksProblemDetails(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler();
        // StatusCodePages handles 4xx/5xx responses that don't have a body
        // (e.g. 404 from MapControllers when no route matches) by writing
        // a minimal problem+json body — keeps API responses consistent.
        app.UseStatusCodePages();

        return app;
    }
}
