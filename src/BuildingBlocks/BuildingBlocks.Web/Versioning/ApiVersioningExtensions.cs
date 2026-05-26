using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace LankaConnect.BuildingBlocks.Web.Versioning;

/// <summary>
/// Asp.Versioning configuration for LankaConnect APIs. Defaults to URL segment
/// versioning (<c>/api/v{version}/...</c>) with a query-string and header reader
/// fallback to ease client migration.
/// </summary>
/// <remarks>
/// <para>
/// Default version: <c>1.0</c>. Assumes the default when a client omits it,
/// and surfaces all available versions in the <c>api-supported-versions</c>
/// response header.
/// </para>
/// <para>
/// Chain <see cref="AddBuildingBlocksApiVersioning"/> with the
/// <c>AddApiExplorer</c> call from Swashbuckle when generating Swagger docs
/// per version.
/// </para>
/// </remarks>
public static class ApiVersioningExtensions
{
    /// <summary>
    /// Registers Asp.Versioning with URL-segment + query + header version readers.
    /// </summary>
    public static IApiVersioningBuilder AddBuildingBlocksApiVersioning(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new QueryStringApiVersionReader("api-version"),
                new HeaderApiVersionReader("X-Api-Version"));
        });

        builder.AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return builder;
    }
}
