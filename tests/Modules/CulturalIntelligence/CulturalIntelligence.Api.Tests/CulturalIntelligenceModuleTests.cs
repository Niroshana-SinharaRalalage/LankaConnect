using LankaConnect.Products.LankaEvents.Domain.Services;
using LankaConnect.Modules.CulturalIntelligence.Api;
using LankaConnect.Modules.CulturalIntelligence.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace LankaConnect.Modules.CulturalIntelligence.Api.Tests;

/// <summary>
/// Wave4.9.1.9 (2026-06-08): DI resolution test for the
/// CulturalIntelligence module. Per route audit
/// (docs/audit/route-inventory-2026-06-08.md G5 entry):
/// <c>ICulturalCalendar</c> has no direct HTTP surface, so verifying
/// the module-extension method correctly registers the service is the
/// testable invariant.
/// </summary>
/// <remarks>
/// Per CLAUDE.md §13.1 trigger T6 (DI registration). Catches the class
/// of bug where someone refactors the AddCulturalIntelligenceModule
/// signature or removes the AddScoped registration and the bug ships
/// silently because no HTTP endpoint exercises the binding.
///
/// CulturalIntelligence.Api.Tests project was missing entirely before
/// this commit.
/// </remarks>
public sealed class CulturalIntelligenceModuleTests
{
    [Fact]
    public void AddCulturalIntelligenceModule_Registers_ICulturalCalendar_To_StubCulturalCalendar()
    {
        var services = new ServiceCollection();

        services.AddCulturalIntelligenceModule();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var resolved = scope.ServiceProvider.GetRequiredService<ICulturalCalendar>();

        resolved.Should().NotBeNull();
        resolved.Should().BeOfType<StubCulturalCalendar>(
            because: "the CulturalIntelligence module currently wires the stub implementation per W4.7; Wave 5 Products carve-out will replace it with the real engine, and this test will then need an update.");
    }

    [Fact]
    public void AddCulturalIntelligenceModule_Returns_Same_Service_Collection_For_Chaining()
    {
        var services = new ServiceCollection();

        var returned = services.AddCulturalIntelligenceModule();

        returned.Should().BeSameAs(services,
            because: "module-extension methods follow the IServiceCollection fluent-chaining convention; returning a different reference breaks composition root chaining at Program.cs.");
    }

    [Fact]
    public void AddCulturalIntelligenceModule_Throws_On_Null_Services()
    {
        IServiceCollection? services = null;

        Action act = () => services!.AddCulturalIntelligenceModule();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void ICulturalCalendar_Is_Scoped_Lifetime()
    {
        var services = new ServiceCollection();
        services.AddCulturalIntelligenceModule();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ICulturalCalendar));

        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped,
            because: "the module's docstring explicitly states AddScoped; a Singleton would change the multi-request safety model and is not approved.");
    }
}
