using System.Reflection;
using NetArchTest.Rules;

namespace LankaConnect.ArchitectureTests;

/// <summary>
/// Clean Architecture layering rules for the BuildingBlocks + Modules tree.
/// Per master TODO §W2.2 — gates PRs that violate dependency direction.
/// </summary>
/// <remarks>
/// Conventions (per master TODO §"Plan Delta Amendments" + ADR-002 + ADR-005):
///   - <c>BuildingBlocks.Domain</c> is the innermost layer; depends on nothing.
///   - <c>BuildingBlocks.Contracts</c> is the cross-module ABI; depends on nothing.
///   - <c>BuildingBlocks.Application</c> depends on Domain + Contracts only.
///   - <c>BuildingBlocks.Infrastructure</c> depends on Application + Domain + Contracts.
///   - <c>BuildingBlocks.Web</c> depends on Application + Domain + Contracts (and ASP.NET Core).
///
/// Future module rules (W3+) — landed as their respective modules extract.
///
/// All tests are tagged <c>Trait("Category", "ArchTest")</c> so CI can run only
/// these via <c>dotnet test --filter Category=ArchTest</c>.
/// </remarks>
public sealed class LayeringRules
{
    /// <summary>
    /// W2.2 first rule (per master TODO line 540).
    /// <c>BuildingBlocks.Domain</c> must not depend on any other
    /// <c>LankaConnect.*</c> assembly — it is the innermost layer.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void BuildingBlocks_Domain_HasNoLankaConnectDependencies()
    {
        // W2.3 (2026-05-13) — AssemblyMarker placeholder removed; anchor on a real type now.
        var assembly = typeof(BuildingBlocks.Domain.Error).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.BuildingBlocks.Application",
                "LankaConnect.BuildingBlocks.Infrastructure",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.BuildingBlocks.Contracts",
                // Existing monolith projects — Domain BuildingBlocks must not back-reference.
                "LankaConnect.Domain",
                "LankaConnect.Application",
                "LankaConnect.Infrastructure",
                "LankaConnect.API",
                "LankaConnect.Shared")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    /// <summary>
    /// <c>BuildingBlocks.Contracts</c> is the cross-module wire-format ABI.
    /// Adding any reference here couples consumers via implementation detail.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void BuildingBlocks_Contracts_HasNoLankaConnectDependencies()
    {
        // W2.7 (2026-05-30) — AssemblyMarker placeholder removed; anchor on the real IntegrationEventBase now.
        var assembly = typeof(BuildingBlocks.Contracts.IntegrationEvents.IntegrationEventBase).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.BuildingBlocks.Domain",
                "LankaConnect.BuildingBlocks.Application",
                "LankaConnect.BuildingBlocks.Infrastructure",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.Domain",
                "LankaConnect.Application",
                "LankaConnect.Infrastructure",
                "LankaConnect.API",
                "LankaConnect.Shared")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    /// <summary>
    /// <c>BuildingBlocks.Application</c> may only reach into Domain + Contracts.
    /// Reaching into Infrastructure or Web inverts the dependency arrow.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void BuildingBlocks_Application_DependsOnDomainAndContractsOnly()
    {
        // W2.4 (2026-05-13) — AssemblyMarker placeholder removed; anchor on a real type now.
        var assembly = typeof(BuildingBlocks.Application.Abstractions.ICommand<>).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.BuildingBlocks.Infrastructure",
                "LankaConnect.BuildingBlocks.Web")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    /// <summary>
    /// <c>BuildingBlocks.Infrastructure</c> must not reach into Web —
    /// Web is the outermost layer; Infrastructure depends on Application/Domain only.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void BuildingBlocks_Infrastructure_DoesNotDependOnWeb()
    {
        // W2.5 (2026-05-13) — AssemblyMarker placeholder removed; anchor on a real type now.
        var assembly = typeof(BuildingBlocks.Infrastructure.Persistence.BaseDbContext).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOn("LankaConnect.BuildingBlocks.Web")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    /// <summary>
    /// <c>BuildingBlocks.Web</c> is the outermost building-block layer.
    /// It may depend on Application/Domain/Contracts + ASP.NET Core framework refs,
    /// but must not back-reference the existing layered monolith projects.
    /// Added W2.6 (2026-05-25) once Web filled with real types (JWT, ProblemDetails,
    /// Health, RateLimiting, Asp.Versioning, FeatureManagement).
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void BuildingBlocks_Web_DoesNotDependOnLayeredMonolith()
    {
        var assembly = typeof(BuildingBlocks.Web.Authentication.JwtSettings).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Domain",
                "LankaConnect.Application",
                "LankaConnect.Infrastructure",
                "LankaConnect.API",
                "LankaConnect.Shared")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    // ---------- W3 — Notifications module boundaries (added 2026-06-02 with W3.1 skeleton) ----------

    /// <summary>
    /// W3 module-boundary invariant: the Notifications module must not back-reference
    /// the legacy layered monolith. As types move (W3.2+), this rule guards against
    /// accidental edges to <c>LankaConnect.{Domain,Application,Infrastructure,API,Shared}</c>.
    /// Anchored on Notifications.Domain (innermost layer of the module).
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Notifications_Domain_DoesNotDependOnLayeredMonolithOrOtherModules()
    {
        // W3.2 transitional (2026-06-02): LankaConnect.Domain is INTENTIONALLY allowed
        // here because Notification still derives from LankaConnect.Domain.Common.BaseEntity
        // and INotificationRepository extends LankaConnect.Domain.Common.IRepository<T>.
        // The edge is cut once BuildingBlocks.Domain owns BaseEntity + IRepository<T>
        // (planned W4/W5 alongside the next module move). At that point, re-tighten
        // this rule by adding "LankaConnect.Domain" back to NotHaveDependencyOnAny.
        var assembly = typeof(Modules.Notifications.Domain.Notification).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Application",
                "LankaConnect.Infrastructure",
                "LankaConnect.API",
                "LankaConnect.Shared",
                "LankaConnect.BuildingBlocks.Application",
                "LankaConnect.BuildingBlocks.Infrastructure",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.BuildingBlocks.Contracts")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    /// <summary>
    /// W3 module-boundary invariant: the Notifications module Application layer
    /// may reach into its own Domain + Contracts + BuildingBlocks.Application,
    /// but must not reach into Infrastructure / Web or the legacy monolith.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Notifications_Application_DoesNotDependOnInfraOrWebOrLayeredMonolith()
    {
        var assembly = typeof(Modules.Notifications.Application.AssemblyMarker).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Notifications.Infrastructure",
                "LankaConnect.Modules.Notifications.Api",
                "LankaConnect.BuildingBlocks.Infrastructure",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.Domain",
                "LankaConnect.Application",
                "LankaConnect.Infrastructure",
                "LankaConnect.API",
                "LankaConnect.Shared")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    /// <summary>
    /// W3 module-boundary invariant: the Notifications module Contracts layer
    /// is the cross-module ABI — depends only on BuildingBlocks.Contracts
    /// (for IntegrationEventBase + V1 marker). No domain entity / handler
    /// leakage; no other module reference.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Notifications_Contracts_DependsOnlyOnBuildingBlocksContracts()
    {
        var assembly = typeof(Modules.Notifications.Contracts.AssemblyMarker).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Notifications.Domain",
                "LankaConnect.Modules.Notifications.Application",
                "LankaConnect.Modules.Notifications.Infrastructure",
                "LankaConnect.Modules.Notifications.Api",
                "LankaConnect.BuildingBlocks.Domain",
                "LankaConnect.BuildingBlocks.Application",
                "LankaConnect.BuildingBlocks.Infrastructure",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.Domain",
                "LankaConnect.Application",
                "LankaConnect.Infrastructure",
                "LankaConnect.API",
                "LankaConnect.Shared")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    /// <summary>
    /// W3 module-boundary invariant: the Notifications module Infrastructure
    /// layer must not reach into Api/Web (outermost) or the legacy monolith.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Notifications_Infrastructure_DoesNotDependOnApiOrWebOrLayeredMonolith()
    {
        var assembly = typeof(Modules.Notifications.Infrastructure.AssemblyMarker).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Notifications.Api",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.Domain",
                "LankaConnect.Application",
                "LankaConnect.Infrastructure",
                "LankaConnect.API",
                "LankaConnect.Shared")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    // ---------- Helpers ----------

    private static void AssertCompliant(TestResult result, string assemblyName)
    {
        if (result.IsSuccessful)
        {
            return;
        }

        var failingTypes = result.FailingTypes is null
            ? "(none reported)"
            : string.Join("\n  - ", result.FailingTypes.Select(t => t.FullName));

        Assert.Fail(
            $"Architecture violation in {assemblyName}.\n" +
            $"Failing types:\n  - {failingTypes}\n" +
            $"Fix: remove the disallowed ProjectReference / using directive, OR re-architect so the " +
            $"dependency flows in the correct Clean Architecture direction.");
    }
}
