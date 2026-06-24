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
    /// W5.3c.0 mirror of Notifications.Application's transitional ArchTest
    /// (above). Forms.Application also references the legacy
    /// LankaConnect.Application transitionally for the same shared abstractions
    /// (ICommand / ICommandHandler / ICurrentUserService / IUnitOfWork) until
    /// BuildingBlocks.Application owns them. Re-tighten this rule by adding
    /// "LankaConnect.Application" and "LankaConnect.Domain" back to
    /// NotHaveDependencyOnAny once the BB elevation lands (cuts both this and
    /// the Notifications.Application edge in a single coordinated wave).
    ///
    /// W5.3d.2 (2026-06-12) — also relaxed `LankaConnect.Shared` because the
    /// 4 FormResponse* EventHandlers moved into this assembly carry
    /// LankaConnect.Shared.Email.{Contracts,Helpers,Services} and
    /// LankaConnect.Shared.WhatsApp.Contracts usings — that namespace is the
    /// horizontal/utility email + WhatsApp templating kernel shared across
    /// every module today. Add `"LankaConnect.Shared"` back to the ban list
    /// once BuildingBlocks.Shared (or a Communications.Contracts elevation)
    /// owns those abstractions.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Forms_Application_DoesNotDependOnInfraOrWebOrLayeredMonolith()
    {
        var assembly = typeof(Modules.Forms.Application.Queries.FormQueries).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Forms.Infrastructure",
                "LankaConnect.Modules.Forms.Api",
                "LankaConnect.BuildingBlocks.Infrastructure",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.Infrastructure",
                "LankaConnect.API")
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

    /// <summary>
    /// Wave 4.4.d.2 (2026-06-23) — Payments.Domain module boundary. Permanent
    /// edge to LankaConnect.Domain per architect Risk #1 Option A (RefundRequest
    /// + RefundRequestLineItem + RegistrationPayment stay as Registration aggregate
    /// children indefinitely). So LankaConnect.Domain is NOT in the ban list.
    /// Other capability-module Domains banned per standard cross-module isolation.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Payments_Domain_DoesNotDependOnApplicationOrInfrastructureOrSiblings()
    {
        var assembly = typeof(Modules.Payments.Domain.AssemblyMarker).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Payments.Application",
                "LankaConnect.Modules.Payments.Infrastructure",
                "LankaConnect.Modules.Payments.Api",
                "LankaConnect.BuildingBlocks.Application",
                "LankaConnect.BuildingBlocks.Infrastructure",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.Application",
                "LankaConnect.Infrastructure",
                "LankaConnect.API",
                "LankaConnect.Modules.Notifications.Domain",
                "LankaConnect.Modules.Notifications.Contracts",
                "LankaConnect.Modules.Notifications.Application",
                "LankaConnect.Modules.Notifications.Infrastructure",
                "LankaConnect.Modules.Notifications.Api",
                "LankaConnect.Modules.Media.Domain",
                "LankaConnect.Modules.Media.Contracts",
                "LankaConnect.Modules.Media.Application",
                "LankaConnect.Modules.Media.Infrastructure",
                "LankaConnect.Modules.Media.Api",
                "LankaConnect.Modules.Communications.Domain",
                "LankaConnect.Modules.Communications.Contracts",
                "LankaConnect.Modules.Communications.Application",
                "LankaConnect.Modules.Communications.Infrastructure",
                "LankaConnect.Modules.Communications.Api",
                "LankaConnect.Modules.Forms.Domain",
                "LankaConnect.Modules.Forms.Contracts",
                "LankaConnect.Modules.Forms.Application",
                "LankaConnect.Modules.Forms.Infrastructure",
                "LankaConnect.Modules.Forms.Api")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    /// <summary>
    /// Wave 4.4.d.2 (2026-06-23) — Payments.Infrastructure module boundary.
    /// Mirrors W5.4.d.2 Communications.Infrastructure relaxation. The
    /// transitional <c>Payments.Infrastructure -&gt; LankaConnect.Infrastructure</c>
    /// edge is REAL (Repository&lt;T&gt; base + AppDbContext share). Re-tighten once
    /// a dedicated PaymentsDbContext lands.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Payments_Infrastructure_DoesNotDependOnApiOrWebOrLayeredMonolith()
    {
        var assembly = typeof(Modules.Payments.Infrastructure.AssemblyMarker).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Payments.Api",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.API")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    /// <summary>
    /// Wave 4.4.b (2026-06-23) — Payments.Application module boundary.
    /// Mirrors the W5.4.b Communications.Application relaxation. The
    /// transitional <c>Payments.Application -&gt; LankaConnect.Domain</c>
    /// ProjectReference is PERMANENT (architect Risk #1 Option A) because
    /// RefundRequest + RefundRequestLineItem + RegistrationPayment stay as
    /// Registration aggregate children in LankaConnect.Domain.Events.Entities.
    /// So LankaConnect.Domain is NOT in the ban list.
    /// Wave 4.4.c.1 (2026-06-23) — additionally relaxed
    /// <c>LankaConnect.Application</c> because the 7 moved RefundRequest
    /// command handlers (ApproveRefundRequest / CreateRefundRequest /
    /// CreateOrganizerInitiatedRefund / RejectRefundRequest /
    /// WithdrawRefundRequestV2 / ForceCancelStuckRefund / WithdrawRefundRequest)
    /// depend on ICommand / ICommandHandler / IUnitOfWork / ICurrentUserService /
    /// IApplicationDbContext + the legacy IRefundExecutionService /
    /// IRefundReconciliationService (until Wave 4.4.c.4 moves the services).
    /// Mirrors the W5.4.c.1 / W5.3c.1 transitional ArchTest relaxations.
    /// Re-tighten once BuildingBlocks.Application owns the ICommand/IUnitOfWork
    /// primitives AND the refund services land in Payments.Application.
    /// Wave 4.4.c.3 (2026-06-23) — additionally relaxed
    /// <c>LankaConnect.Shared</c> because the 17 moved Payment / Refund event
    /// handlers import LankaConnect.Shared.Email.{Contracts,Helpers,Services}
    /// + LankaConnect.Shared.WhatsApp.Contracts. Mirrors W5.4.c.1 Shared
    /// relaxation. Re-tighten once Shared.Email + Shared.WhatsApp Contracts
    /// elevate into BuildingBlocks or a dedicated module.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Payments_Application_DoesNotDependOnInfraOrWebOrLayeredMonolith()
    {
        var assembly = typeof(Modules.Payments.Application.AssemblyMarker).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Payments.Infrastructure",
                "LankaConnect.Modules.Payments.Api",
                "LankaConnect.BuildingBlocks.Infrastructure",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.Infrastructure",
                "LankaConnect.API")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    /// <summary>
    /// Wave 4.4.a (2026-06-23) — Payments.Contracts module boundary.
    /// Mirrors Modules_Communications_Contracts_DependsOnlyOnBuildingBlocksContracts.
    /// Contracts must depend only on BuildingBlocks.Contracts (the cross-module
    /// wire-format ABI) — never on LankaConnect.Domain, LankaConnect.Application,
    /// LankaConnect.Infrastructure, or any other module's non-Contracts layer.
    /// Catches accidental ProjectReference additions to Payments.Contracts.csproj.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Payments_Contracts_DependsOnlyOnBuildingBlocksContracts()
    {
        var assembly = typeof(Modules.Payments.Contracts.AssemblyMarker).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Payments.Domain",
                "LankaConnect.Modules.Payments.Application",
                "LankaConnect.Modules.Payments.Infrastructure",
                "LankaConnect.Modules.Payments.Api",
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
                "LankaConnect.Modules.Media.Api",
                "LankaConnect.Modules.Communications.Domain",
                "LankaConnect.Modules.Communications.Contracts",
                "LankaConnect.Modules.Communications.Application",
                "LankaConnect.Modules.Communications.Infrastructure",
                "LankaConnect.Modules.Communications.Api",
                "LankaConnect.Modules.Forms.Domain",
                "LankaConnect.Modules.Forms.Contracts",
                "LankaConnect.Modules.Forms.Application",
                "LankaConnect.Modules.Forms.Infrastructure",
                "LankaConnect.Modules.Forms.Api")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    /// <summary>
    /// W5.4.b (2026-06-13) — relaxed `LankaConnect.Domain` from the ban list.
    /// EmailGroupQueries in Communications.Application wraps the existing
    /// IEmailGroupRepository which still lives in LankaConnect.Domain.Communications
    /// until W5.4.d.2 physical move.
    /// W5.4.c.1 (2026-06-22) — additionally relaxed `LankaConnect.Application`
    /// because the 5 moved command handlers (CreateEmailGroup / UpdateEmailGroup
    /// / DeleteEmailGroup / CreateNewsletter / UpdateNewsletter) depend on
    /// ICommand / ICommandHandler / ICurrentUserService / IUnitOfWork that
    /// still live there until BuildingBlocks.Application owns those primitives.
    /// W5.4.c.1 also relaxed `LankaConnect.Shared` because the moved handlers
    /// import LankaConnect.Shared.Email.{Contracts,Helpers,Services} — same
    /// transitional rationale as the Forms 5.3d.2 hotfix.
    /// Re-tighten all three (Domain + Application + Shared) once the matching
    /// W5.4.d.2 / BB elevation passes land.
    /// </summary>
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
                "LankaConnect.Infrastructure",
                "LankaConnect.API")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    /// <summary>
    /// W5.4.d.2 (2026-06-22). Relaxed `LankaConnect.Infrastructure` from the ban
    /// list. EmailGroupRepository extends LankaConnect.Infrastructure.Data.
    /// Repositories.Repository&lt;T&gt; base and injects AppDbContext directly during
    /// the transitional window (no separate CommunicationsDbContext in W5.4).
    /// Mirrors the W3.4 Notifications.Infrastructure relaxation. Same with
    /// LankaConnect.Domain (EmailGroup extends LegacyBaseEntity) and
    /// LankaConnect.Application (Result&lt;T&gt; usage via the transitional edge).
    /// Re-tighten once a dedicated CommunicationsDbContext lands and the
    /// LegacyBaseEntity / Result&lt;T&gt; primitives elevate into BuildingBlocks.
    /// </summary>
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
                "LankaConnect.API")
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

    // ---------- W4.3 — Forms module boundaries (added 2026-06-06) ----------

    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Forms_Domain_DoesNotDependOnLayeredMonolithOrOtherModules()
    {
        var assembly = typeof(Modules.Forms.Domain.AssemblyMarker).Assembly;

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
                "LankaConnect.Modules.Media.Api",
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
    public void Modules_Forms_Infrastructure_DoesNotDependOnApiOrWebOrLayeredMonolith()
    {
        // W4.3 transitional (2026-06-06): mirrors the W4.2 Media pattern.
        // LankaConnect.Domain edge intentionally allowed because Form + FormQuestion
        // + FormResponse + FormAnswer still extend LegacyBaseEntity and use Result<T>;
        // rebase to direct BB.Entity<Guid> deferred to W4.8 Cross-cutting cleanup.
        // LankaConnect.Application + LankaConnect.Infrastructure excluded from the forbidden
        // list to avoid NetArchTest prefix-match false positives against BB.Application /
        // BB.Infrastructure.
        var assembly = typeof(Modules.Forms.Infrastructure.AssemblyMarker).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Forms.Api",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.API",
                "LankaConnect.Shared")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    /// <summary>
    /// Wave 5.3a (2026-06-11) — Forms.Contracts is the cross-module ABI. Mirrors the
    /// W3.3 Notifications + W4.2 Media + Communications Contracts rules. Must not
    /// reach any Forms-internal layer or any BuildingBlocks beyond Contracts.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Forms_Contracts_DependsOnlyOnBuildingBlocksContracts()
    {
        var assembly = typeof(Modules.Forms.Contracts.IFormQueries).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Forms.Domain",
                "LankaConnect.Modules.Forms.Application",
                "LankaConnect.Modules.Forms.Infrastructure",
                "LankaConnect.Modules.Forms.Api",
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
    /// W5.3d.3 (2026-06-13) — pins the cut completed in this commit:
    /// LankaConnect.Application no longer references Forms.Domain. Cross-module
    /// Forms access from the legacy Application layer flows through
    /// Forms.Contracts (IFormQueries / IFormCommands / *Dto). Catch any future
    /// regression where a new handler/service inside LankaConnect.Application
    /// imports a Forms.Domain symbol.
    ///
    /// Scope note: LankaConnect.Infrastructure still legitimately references
    /// Forms.Domain (RefundRequestConfiguration EF mapping, the registration
    /// email service, the Twilio template seeder). That coupling is the legacy
    /// infrastructure layer reaching module Domain for cross-module persistence
    /// wiring and is explicitly allowed by ADR-002; only the Application-layer
    /// cut is pinned here.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void LegacyApplication_DoesNotDependOnFormsDomain()
    {
        var assembly = typeof(LankaConnect.Application.Common.Interfaces.IApplicationDbContext).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Forms.Domain")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    /// <summary>
    /// W5.4.d.3 (2026-06-22) — pins the Application-side cut completed in this
    /// commit: LankaConnect.Application no longer references Communications.Domain.
    /// Cross-module Communications access from the legacy Application layer flows
    /// through Communications.Contracts (IEmailGroupQueries / *Dto). Catch any
    /// future regression where a new handler/service inside LankaConnect.Application
    /// imports a Communications.Domain symbol.
    ///
    /// Scope note: LankaConnect.Infrastructure still legitimately references
    /// Communications.Domain (AppDbContext owns the EmailGroup DbSet during the
    /// transitional window; EmailGroupConfiguration lives in the legacy infra
    /// layer to avoid a circular ref with Communications.Infrastructure). That
    /// coupling is the legacy infrastructure layer reaching module Domain for
    /// cross-module persistence wiring and is explicitly allowed by ADR-002;
    /// only the Application-layer cut is pinned here. Mirrors the W5.3d.3 Forms
    /// LegacyApplication_DoesNotDependOnFormsDomain rule.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void LegacyApplication_DoesNotDependOnCommunicationsDomain()
    {
        var assembly = typeof(LankaConnect.Application.Common.Interfaces.IApplicationDbContext).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Communications.Domain")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    /// <summary>
    /// W5.4.d.3 (2026-06-22) — pins the Domain-side cut completed in this commit:
    /// LankaConnect.Domain no longer references Communications.Domain. This rule
    /// has no Forms-side equivalent because the Forms aggregates never held a
    /// cross-aggregate typed nav from LankaConnect.Domain.Events into
    /// Forms.Domain. Communications IS different — Newsletter.cs had a typed
    /// M2M nav to EmailGroup (Phase 6A.74 `_emailGroupEntities: List&lt;EmailGroup&gt;`),
    /// and W5.4.d.1b surgery replaced that nav with the explicit
    /// NewsletterEmailGroupLink junction CLR type that holds raw Guids. This
    /// ArchTest guards against future re-introduction of a Communications.Domain
    /// type reference from any aggregate in LankaConnect.Domain (Event, Newsletter,
    /// or any future entity).
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void LegacyDomain_DoesNotDependOnCommunicationsDomain()
    {
        var assembly = typeof(LankaConnect.Domain.Events.Event).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Communications.Domain")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    /// <summary>
    /// Wave 4.4.d.3 (2026-06-23) — pins the Application-side boundary that Wave 4.4
    /// established without ever introducing the edge in the first place:
    /// LankaConnect.Application does NOT reference Payments.Domain. Cross-module
    /// Payments access from the legacy Application layer flows through
    /// Payments.Contracts (IPaymentQueries / IPaymentCommands / DTOs). Catch any
    /// future regression where a new handler/service inside LankaConnect.Application
    /// imports a Payments.Domain symbol.
    ///
    /// Mirrors W5.3d.3 (LegacyApplication_DoesNotDependOnFormsDomain) +
    /// W5.4.d.3 (LegacyApplication_DoesNotDependOnCommunicationsDomain).
    ///
    /// Scope note: <c>LankaConnect.Infrastructure</c> still legitimately references
    /// Payments.Domain (Wave 4.4.d.2 added that edge so the 3 webhook handlers +
    /// StripePaymentService still living in Infrastructure can inject the moved
    /// repository interfaces). That coupling is the legacy infrastructure layer
    /// reaching module Domain for cross-module persistence wiring and is explicitly
    /// allowed by ADR-002.
    ///
    /// **Rule 6 INTENTIONALLY ABSENT** (architect directive 2026-06-23):
    /// there is NO companion LegacyDomain_DoesNotDependOnPaymentsDomain rule
    /// because Risk #1 Option A keeps RefundRequest + RefundRequestLineItem +
    /// RegistrationPayment as Registration aggregate children in
    /// LankaConnect.Domain.Events.Entities -- AND the Payments.Domain ProjectReference
    /// to LankaConnect.Domain (added at Wave 4.4.b) is the PERMANENT structural
    /// compromise for this wave. The inverse edge (LankaConnect.Domain ->
    /// Payments.Domain) does not exist today, but adding a Rule 6 to pin its
    /// absence would mis-frame the architecture: the actual invariant is "Payments
    /// is a downstream Domain dependent on LankaConnect.Domain", not "LankaConnect.
    /// Domain is independent of Payments.Domain". Future contributor reading this
    /// docstring: do not add a Rule 6 just for symmetry with Forms/Communications --
    /// Wave 4.4's architectural shape is DIFFERENT by design.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void LegacyApplication_DoesNotDependOnPaymentsDomain()
    {
        var assembly = typeof(LankaConnect.Application.Common.Interfaces.IApplicationDbContext).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Payments.Domain")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    // ---------- W4.7 — CulturalIntelligence module boundaries (added 2026-06-06) ----------

    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_CulturalIntelligence_Domain_DoesNotDependOnLayeredMonolithOrOtherModules()
    {
        var assembly = typeof(Modules.CulturalIntelligence.Domain.AssemblyMarker).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Domain",
                "LankaConnect.Application",
                "LankaConnect.Infrastructure",
                "LankaConnect.API",
                "LankaConnect.Shared",
                "LankaConnect.BuildingBlocks.Application",
                "LankaConnect.BuildingBlocks.Infrastructure",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.BuildingBlocks.Contracts",
                "LankaConnect.Modules.Notifications.Domain",
                "LankaConnect.Modules.Media.Domain",
                "LankaConnect.Modules.Forms.Domain",
                "LankaConnect.Modules.Communications.Domain")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_CulturalIntelligence_Infrastructure_DoesNotDependOnApiOrWebOrLayeredMonolith()
    {
        // W4.7 transitional (2026-06-06): mirrors the W4.2 Media / W4.3 Forms transitional
        // pattern. LankaConnect.Domain edge intentionally allowed because StubCulturalCalendar
        // implements LankaConnect.Domain.Events.Services.ICulturalCalendar — interface stays
        // in legacy Events.Services until Wave 5 Products carves Events into Products/LankaEvents.
        // LankaConnect.Application + LankaConnect.Infrastructure excluded to avoid NetArchTest
        // prefix-match false positives against BB.Application + BB.Infrastructure.
        var assembly = typeof(Modules.CulturalIntelligence.Infrastructure.AssemblyMarker).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.CulturalIntelligence.Api",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.API",
                "LankaConnect.Shared")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    // ---------- W4.5 — Scheduling module boundaries (added 2026-06-06) ----------

    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Scheduling_Domain_DoesNotDependOnLayeredMonolithOrOtherModules()
    {
        var assembly = typeof(Modules.Scheduling.Domain.AssemblyMarker).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Domain",
                "LankaConnect.Application",
                "LankaConnect.Infrastructure",
                "LankaConnect.API",
                "LankaConnect.Shared",
                "LankaConnect.BuildingBlocks.Application",
                "LankaConnect.BuildingBlocks.Infrastructure",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.BuildingBlocks.Contracts",
                "LankaConnect.Modules.Notifications.Domain",
                "LankaConnect.Modules.Media.Domain",
                "LankaConnect.Modules.Forms.Domain",
                "LankaConnect.Modules.CulturalIntelligence.Domain",
                "LankaConnect.Modules.Communications.Domain")
            .GetResult();

        AssertCompliant(result, assembly.GetName().Name!);
    }

    [Fact]
    [Trait("Category", "ArchTest")]
    public void Modules_Scheduling_Infrastructure_DoesNotDependOnApiOrWebOrLayeredMonolith()
    {
        // W4.5 (2026-06-06): pristine skeleton — no transitional debt accepted at creation.
        // Real types arrive during Wave 5 Products carve-out.
        var assembly = typeof(Modules.Scheduling.Infrastructure.AssemblyMarker).Assembly;

        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Modules.Scheduling.Api",
                "LankaConnect.BuildingBlocks.Web",
                "LankaConnect.Domain",
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
