using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace LankaConnect.BuildingBlocks.Web.FeatureFlags;

/// <summary>
/// Microsoft.FeatureManagement registration per ADR-004. Reads flags from the
/// configured <c>FeatureManagement</c> section and stores them in the standard
/// <see cref="IFeatureManager"/> service.
/// </summary>
/// <remarks>
/// <para>
/// Hosts call <c>AddBuildingBlocksFeatureManagement(builder.Configuration)</c>
/// once during startup. Feature flags can then be evaluated via
/// <c>IFeatureManager.IsEnabledAsync("My.Flag")</c> in any DI-aware code path.
/// </para>
/// <para>
/// The default section name is <c>FeatureManagement</c> per Microsoft convention;
/// override via <paramref name="sectionName"/> for hosts that namespace the
/// section differently.
/// </para>
/// </remarks>
public static class FeatureManagementExtensions
{
    /// <summary>Default configuration section name (Microsoft.FeatureManagement convention).</summary>
    public const string DefaultSectionName = "FeatureManagement";

    /// <summary>
    /// Registers <see cref="IFeatureManager"/> backed by the supplied configuration
    /// section. Returns the <see cref="IFeatureManagementBuilder"/> so callers
    /// can chain additional filters (TimeWindowFilter, TargetingFilter, etc).
    /// </summary>
    public static IFeatureManagementBuilder AddBuildingBlocksFeatureManagement(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = DefaultSectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services.AddFeatureManagement(configuration.GetSection(sectionName));
    }
}
