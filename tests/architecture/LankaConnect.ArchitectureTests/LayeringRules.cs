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
        var assembly = typeof(BuildingBlocks.Contracts.AssemblyMarker).Assembly;

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
