using LankaConnect.Modules.CulturalIntelligence.Api;
using LankaConnect.Modules.CulturalIntelligence.Contracts.Services;
using LankaConnect.Modules.CulturalIntelligence.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LankaConnect.Modules.CulturalIntelligence.Api.Tests;

/// <summary>
/// Wave4.9.1.9 (2026-06-08): DI resolution test for the CulturalIntelligence
/// module. Per route audit (docs/audit/route-inventory-2026-06-08.md G5 entry):
/// <c>ICulturalCalendar</c> has no direct HTTP surface, so verifying the
/// module-extension method correctly registers the service is the testable
/// invariant.
///
/// Wave 8.5 GAP-1 (2026-07-19): updated post-D-13 Option A promotion — the
/// service type resolves from <c>CulturalIntelligence.Contracts.Services</c>
/// (not the old LankaEvents.Domain.Services namespace), and the impl is
/// <see cref="PoyaCalendarService"/> (StubCulturalCalendar retired).
/// </summary>
/// <remarks>
/// Per CLAUDE.md §13.1 trigger T6 (DI registration). Catches the class of
/// bug where someone refactors the AddCulturalIntelligenceModule signature
/// or removes the AddSingleton registration and the bug ships silently
/// because no HTTP endpoint exercises the binding.
/// </remarks>
public sealed class CulturalIntelligenceModuleTests
{
    [Fact]
    public void AddCulturalIntelligenceModule_Registers_ICulturalCalendar_To_PoyaCalendarService()
    {
        var services = new ServiceCollection();

        services.AddCulturalIntelligenceModule();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var resolved = scope.ServiceProvider.GetRequiredService<ICulturalCalendar>();

        resolved.Should().NotBeNull();
        resolved.Should().BeOfType<PoyaCalendarService>(
            because: "Wave 8.5 GAP-1 Part B retired StubCulturalCalendar; the module now wires the real seed-file-backed PoyaCalendarService.");
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
    public void ICulturalCalendar_Is_Singleton_Lifetime()
    {
        var services = new ServiceCollection();
        services.AddCulturalIntelligenceModule();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ICulturalCalendar));

        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton,
            because: "PoyaCalendarService is stateless read-only over an embedded JSON resource — Singleton is the correct lifetime, and the module docstring names it explicitly.");
    }
}
