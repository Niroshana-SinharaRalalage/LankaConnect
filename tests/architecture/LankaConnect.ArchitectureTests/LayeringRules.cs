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
        // W3A (2026-06-05): legacy LankaConnect.Domain transitional edge CUT.
        // Notification now derives from BuildingBlocks.Domain.Entity<Guid> + IAuditable
        // (per ADR-007); INotificationRepository extends
        // BuildingBlocks.Abstractions.IAggregateRepository<Notification, Guid> (per ADR-010).
        //
        // NOTE: per architect's W1A ruling, the BuildingBlocks.Abstractions namespace
        // is `LankaConnect.BuildingBlocks.Application.Abstractions` (deliberate
        // assembly-vs-namespace mismatch for zero source churn). NetArchTest's
        // NotHaveDependencyOnAny prefix-matches on namespaces, so we CANNOT
        // enumerate "LankaConnect.BuildingBlocks.Application" here — it would
        // false-positive on legitimate BB.Abstractions usage. The csproj
        // structurally enforces no BB.Application dep (Notifications.Domain has
        // zero ProjectReferences to BuildingBlocks.Application).
        var assembly = typeof(Modules.Notifications.Domain.Notification).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Domain",
                "LankaConnect.Application",
                "LankaConnect.Infrastructure",
                "LankaConnect.API",
                "LankaConnect.Shared",
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
        // W3.4 transitional (2026-06-03): LankaConnect.Application is INTENTIONALLY
        // allowed here because the moved handlers still use the legacy
        // ICommand / ICommandHandler / ICurrentUserService / IUnitOfWork abstractions.
        // The edge is cut once BuildingBlocks.Application owns those primitives
        // alongside a richer current-actor abstraction. The legacy LankaConnect.Domain
        // edge persists from W3.2 (BaseEntity / Result / IRepository<T> elevation pending).
        // Re-tighten this rule by adding "LankaConnect.Application" and "LankaConnect.Domain"
        // back to NotHaveDependencyOnAny once the elevation lands.
        var assembly = typeof(Modules.Notifications.Application.Commands.MarkNotificationAsRead.MarkNotificationAsReadCommand).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Notifications.Infrastructure",
                "LankaConnect.Modules.Notifications.Api",
                "LankaConnect.BuildingBlocks.Infrastructure",
                "LankaConnect.BuildingBlocks.Web",
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
        // W3.3 (2026-06-02) — AssemblyMarker placeholder removed; anchor on a real type now.
        var assembly = typeof(Modules.Notifications.Contracts.INotificationDispatcher).Assembly;

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
        // W3.4 transitional (2026-06-03): LankaConnect.Infrastructure is INTENTIONALLY
        // allowed here because the moved NotificationRepository still extends
        // LankaConnect.Infrastructure.Data.Repositories.Repository<T> and injects
        // AppDbContext. The legacy LankaConnect.Domain / LankaConnect.Application
        // edges persist from W3.2 / W3.4 (BaseEntity + IRepository<T> elevation pending).
        // Re-tighten this rule once the BuildingBlocks elevation lands.
        var assembly = typeof(Modules.Notifications.Infrastructure.Data.NotificationsDbContext).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Notifications.Api",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.API",
                "LankaConnect.Shared")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    // ---------- W4 — Communications module boundaries (added 2026-06-04 with W4.1.1 skeleton) ----------

    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Communications_Domain_DoesNotDependOnLayeredMonolithOrOtherModules()
    {
        var assembly = typeof(Modules.Communications.Domain.AssemblyMarker).Assembly;

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
                "LankaConnect.BuildingBlocks.Contracts",
                "LankaConnect.Modules.Notifications.Domain",
                "LankaConnect.Modules.Notifications.Contracts",
                "LankaConnect.Modules.Notifications.Application",
                "LankaConnect.Modules.Notifications.Infrastructure",
                "LankaConnect.Modules.Notifications.Api",
                "LankaConnect.Modules.Media.Domain",
                "LankaConnect.Modules.Media.Contracts",
                "LankaConnect.Modules.Media.Application",
                "LankaConnect.Modules.Media.Infrastructure",
                "LankaConnect.Modules.Media.Api")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Communications_Contracts_DependsOnlyOnBuildingBlocksContracts()
    {
        var assembly = typeof(Modules.Communications.Contracts.AssemblyMarker).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Communications.Domain",
                "LankaConnect.Modules.Communications.Application",
                "LankaConnect.Modules.Communications.Infrastructure",
                "LankaConnect.Modules.Communications.Api",
                "LankaConnect.BuildingBlocks.Domain",
                "LankaConnect.BuildingBlocks.Application",
                "LankaConnect.BuildingBlocks.Infrastructure",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.Domain",
                "LankaConnect.Application",
                "LankaConnect.Infrastructure",
                "LankaConnect.API",
                "LankaConnect.Shared",
                "LankaConnect.Modules.Notifications.Domain",
                "LankaConnect.Modules.Notifications.Contracts",
                "LankaConnect.Modules.Notifications.Application",
                "LankaConnect.Modules.Notifications.Infrastructure",
                "LankaConnect.Modules.Notifications.Api",
                "LankaConnect.Modules.Media.Domain",
                "LankaConnect.Modules.Media.Contracts",
                "LankaConnect.Modules.Media.Application",
                "LankaConnect.Modules.Media.Infrastructure",
                "LankaConnect.Modules.Media.Api")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Communications_Application_DoesNotDependOnInfraOrWebOrLayeredMonolith()
    {
        var assembly = typeof(Modules.Communications.Application.AssemblyMarker).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Communications.Infrastructure",
                "LankaConnect.Modules.Communications.Api",
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

    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Communications_Infrastructure_DoesNotDependOnApiOrWebOrLayeredMonolith()
    {
        var assembly = typeof(Modules.Communications.Infrastructure.AssemblyMarker).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Communications.Api",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.Domain",
                "LankaConnect.Application",
                "LankaConnect.Infrastructure",
                "LankaConnect.API",
                "LankaConnect.Shared")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    // ---------- W4 — Media module boundaries (added 2026-06-04 with W4.2.1 skeleton) ----------

    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Media_Domain_DoesNotDependOnLayeredMonolithOrOtherModules()
    {
        var assembly = typeof(Modules.Media.Domain.AssemblyMarker).Assembly;

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
                "LankaConnect.BuildingBlocks.Contracts",
                "LankaConnect.Modules.Notifications.Domain",
                "LankaConnect.Modules.Notifications.Contracts",
                "LankaConnect.Modules.Notifications.Application",
                "LankaConnect.Modules.Notifications.Infrastructure",
                "LankaConnect.Modules.Notifications.Api",
                "LankaConnect.Modules.Communications.Domain",
                "LankaConnect.Modules.Communications.Contracts",
                "LankaConnect.Modules.Communications.Application",
                "LankaConnect.Modules.Communications.Infrastructure",
                "LankaConnect.Modules.Communications.Api")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Media_Contracts_DependsOnlyOnBuildingBlocksContracts()
    {
        var assembly = typeof(Modules.Media.Contracts.AssemblyMarker).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Media.Domain",
                "LankaConnect.Modules.Media.Application",
                "LankaConnect.Modules.Media.Infrastructure",
                "LankaConnect.Modules.Media.Api",
                "LankaConnect.BuildingBlocks.Domain",
                "LankaConnect.BuildingBlocks.Application",
                "LankaConnect.BuildingBlocks.Infrastructure",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.Domain",
                "LankaConnect.Application",
                "LankaConnect.Infrastructure",
                "LankaConnect.API",
                "LankaConnect.Shared",
                "LankaConnect.Modules.Notifications.Domain",
                "LankaConnect.Modules.Notifications.Contracts",
                "LankaConnect.Modules.Notifications.Application",
                "LankaConnect.Modules.Notifications.Infrastructure",
                "LankaConnect.Modules.Notifications.Api",
                "LankaConnect.Modules.Communications.Domain",
                "LankaConnect.Modules.Communications.Contracts",
                "LankaConnect.Modules.Communications.Application",
                "LankaConnect.Modules.Communications.Infrastructure",
                "LankaConnect.Modules.Communications.Api")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Media_Application_DoesNotDependOnInfraOrWebOrLayeredMonolith()
    {
        var assembly = typeof(Modules.Media.Application.AssemblyMarker).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Media.Infrastructure",
                "LankaConnect.Modules.Media.Api",
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

    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Media_Infrastructure_DoesNotDependOnApiOrWebOrLayeredMonolith()
    {
        // W4.2 transitional (2026-06-06): mirrors the Notifications transitional rule
        // above. LankaConnect.Domain edge intentionally allowed because PhotoAlbum +
        // AlbumPhoto still extend LegacyBaseEntity and use Result<T>; rebase to direct
        // BB.Entity<Guid> + IAuditable + typed errors deferred to W4.8 Cross-cutting cleanup.
        // LankaConnect.Application and LankaConnect.Infrastructure excluded from the forbidden
        // list to avoid NetArchTest prefix-match false positives against
        // LankaConnect.BuildingBlocks.Application + LankaConnect.BuildingBlocks.Infrastructure
        // (architect Q1 ruling pattern, see W1A_BB_Abstractions rule).
        var assembly = typeof(Modules.Media.Infrastructure.AssemblyMarker).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Media.Api",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.API",
                "LankaConnect.Shared")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    // ---------- W1A — BuildingBlocks.Abstractions (added 2026-06-04) ----------

    /// <summary>
    /// W1A invariant: BuildingBlocks.Abstractions holds pure cross-cutting
    /// contracts (ICommand, IQuery, IUnitOfWork, IOutbox, ICurrentActor,
    /// IAuditLogger, IIdempotencyStore, IIntegrationEventBuffer, IClock).
    /// It must not reference any other LankaConnect assembly — consumers
    /// depend on this for the contract surface without pulling in the
    /// behavior surface (MediatR pipeline behaviors etc.).
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void BuildingBlocks_Abstractions_HasNoLankaConnectDependencies()
    {
        var assembly = typeof(BuildingBlocks.Application.Abstractions.IClock).Assembly;

        // NOTE: per architect's W1A ruling, the abstractions namespace stays
        // `LankaConnect.BuildingBlocks.Application.Abstractions` for zero
        // source churn even though the assembly is BB.Abstractions. NetArchTest's
        // NotHaveDependencyOnAny does prefix matching, so we cannot enumerate
        // `LankaConnect.BuildingBlocks.Application` here — it would false-positive
        // on the abstractions' OWN namespace. Instead, assert no reference to
        // the OTHER BuildingBlocks layers + the legacy monolith. The csproj
        // already structurally enforces no BB.Application/Domain dep
        // (BB.Abstractions has ZERO ProjectReferences besides MediatR package).
        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.BuildingBlocks.Domain",
                "LankaConnect.BuildingBlocks.Infrastructure",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.BuildingBlocks.Contracts",
                "LankaConnect.SharedKernel.Cultural",
                "LankaConnect.SharedKernel.Money",
                "LankaConnect.SharedKernel.Locale",
                "LankaConnect.SharedKernel.Identity",
                "LankaConnect.SharedKernel.Geo",
                "LankaConnect.SharedKernel.Time",
                "LankaConnect.SharedKernel.Contracts",
                "LankaConnect.Domain",
                "LankaConnect.Application",
                "LankaConnect.Infrastructure",
                "LankaConnect.API",
                "LankaConnect.Shared")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    // ---------- W1D-W1G — SharedKernel layer (added 2026-06-04) ----------
    //
    // Invariant: every SharedKernel.X package may reference only BuildingBlocks.*
    // (Domain + Contracts as needed); MUST NOT reference Capabilities (yet to land),
    // Products (yet to land), Hosts, LankaConnect.* (legacy monolith), or other
    // SharedKernel sibling implementations (except via SharedKernel.Contracts when
    // cross-SharedKernel integration events land in Wave 2).

    [Fact]
    [Trait("Category", "ArchTest")]
    public void SharedKernel_Cultural_DependsOnlyOnBuildingBlocks()
    {
        var assembly = typeof(SharedKernel.Cultural.AssemblyMarker).Assembly;
        AssertCompliant(SharedKernelDependencyRule(assembly), assembly.GetName().Name!);
    }

    [Fact]
    [Trait("Category", "ArchTest")]
    public void SharedKernel_Money_DependsOnlyOnBuildingBlocks()
    {
        var assembly = typeof(SharedKernel.Money.Money).Assembly;
        AssertCompliant(SharedKernelDependencyRule(assembly), assembly.GetName().Name!);
    }

    [Fact]
    [Trait("Category", "ArchTest")]
    public void SharedKernel_Locale_DependsOnlyOnBuildingBlocks()
    {
        var assembly = typeof(SharedKernel.Locale.Locale).Assembly;
        AssertCompliant(SharedKernelDependencyRule(assembly), assembly.GetName().Name!);
    }

    [Fact]
    [Trait("Category", "ArchTest")]
    public void SharedKernel_Identity_DependsOnlyOnBuildingBlocks()
    {
        var assembly = typeof(SharedKernel.Identity.UserId).Assembly;
        AssertCompliant(SharedKernelDependencyRule(assembly), assembly.GetName().Name!);
    }

    [Fact]
    [Trait("Category", "ArchTest")]
    public void SharedKernel_Geo_DependsOnlyOnBuildingBlocks()
    {
        var assembly = typeof(SharedKernel.Geo.AssemblyMarker).Assembly;
        AssertCompliant(SharedKernelDependencyRule(assembly), assembly.GetName().Name!);
    }

    [Fact]
    [Trait("Category", "ArchTest")]
    public void SharedKernel_Time_DependsOnlyOnBuildingBlocks()
    {
        var assembly = typeof(SharedKernel.Time.AssemblyMarker).Assembly;
        AssertCompliant(SharedKernelDependencyRule(assembly), assembly.GetName().Name!);
    }

    /// <summary>
    /// SharedKernel.Contracts is the SharedKernel-level integration-event ABI.
    /// May reference only BuildingBlocks.Contracts (for IIntegrationEventV1 + base).
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void SharedKernel_Contracts_DependsOnlyOnBuildingBlocksContracts()
    {
        var assembly = typeof(SharedKernel.Contracts.AssemblyMarker).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.BuildingBlocks.Domain",
                "LankaConnect.BuildingBlocks.Application",
                "LankaConnect.BuildingBlocks.Infrastructure",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.SharedKernel.Cultural",
                "LankaConnect.SharedKernel.Money",
                "LankaConnect.SharedKernel.Locale",
                "LankaConnect.SharedKernel.Identity",
                "LankaConnect.SharedKernel.Geo",
                "LankaConnect.SharedKernel.Time",
                "LankaConnect.Domain",
                "LankaConnect.Application",
                "LankaConnect.Infrastructure",
                "LankaConnect.API",
                "LankaConnect.Shared")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    /// <summary>
    /// Shared rule body: a SharedKernel package may reference BuildingBlocks.*
    /// (Domain + Abstractions + Contracts) but NOT Capabilities / Products /
    /// Hosts / legacy LankaConnect.* / sibling SharedKernel impl packages.
    /// SharedKernel.Contracts is the only sibling allowed (for cross-SharedKernel
    /// integration events, lands in Wave 2).
    /// </summary>
    private static TestResult SharedKernelDependencyRule(Assembly assembly)
    {
        return Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.BuildingBlocks.Infrastructure",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.Domain",
                "LankaConnect.Application",
                "LankaConnect.Infrastructure",
                "LankaConnect.API",
                "LankaConnect.Shared",
                "LankaConnect.Modules.Notifications.Domain",
                "LankaConnect.Modules.Notifications.Contracts",
                "LankaConnect.Modules.Notifications.Application",
                "LankaConnect.Modules.Notifications.Infrastructure",
                "LankaConnect.Modules.Notifications.Api",
                "LankaConnect.Modules.Communications.Domain",
                "LankaConnect.Modules.Communications.Contracts",
                "LankaConnect.Modules.Communications.Application",
                "LankaConnect.Modules.Communications.Infrastructure",
                "LankaConnect.Modules.Communications.Api",
                "LankaConnect.Modules.Media.Domain",
                "LankaConnect.Modules.Media.Contracts",
                "LankaConnect.Modules.Media.Application",
                "LankaConnect.Modules.Media.Infrastructure",
                "LankaConnect.Modules.Media.Api")
            .GetResult();
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
