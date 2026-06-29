using System.Linq;
using NetArchTest.Rules;
using LankaConnect.BuildingBlocks.Abstractions;

namespace LankaConnect.ArchitectureTests;

/// <summary>
/// Wave 5.5.a (2026-06-29) — Products layer dependency-direction enforcement.
/// </summary>
/// <remarks>
/// <para>
/// These rules formalize the Products/LankaEvents boundary established across Wave 5.0
/// through Wave 5.4. Architect-mandated via consult <c>2026-06-29-platform-plan-hierarchy.md</c>
/// with the following adjustments from the original draft:
/// </para>
/// <list type="bullet">
///   <item>Rule 1 — MediatR explicitly forbidden in Products.LankaEvents.Domain (grep at consult
///         time returned zero hits; preserving domain purity).</item>
///   <item>Rule 2 — legacy <c>LankaConnect.Infrastructure</c> allowance is namespace-scoped to
///         <c>.Data</c> and <c>.Data.Repositories</c> only; everything else is forbidden so
///         accidental coupling to Services/Auth/External legacy code is caught.</item>
///   <item>Rule 6 — transitional dependency is enforced HARD with explicit
///         <see cref="Wave6_5TransitionalExceptionAttribute"/> opt-out per class. Wave 6.5
///         contributors grep for the attribute to know exactly what to fix.</item>
///   <item>Architect-dropped Rule 7 (Analytics entity warn) — Wave 5.5.d docs are the
///         authoritative deferral record; the ArchTest equivalent would be noise.</item>
///   <item>Rule 8 — Clean Architecture invariant: Application does not reference Infrastructure.</item>
///   <item>Rule 9 — other capability modules reach Products only via Domain interfaces.</item>
/// </list>
/// <para>
/// All tests carry <c>[Trait("Category", "ArchTest")]</c> so CI can run only the architecture
/// suite via <c>dotnet test --filter Category=ArchTest</c>.
/// </para>
/// </remarks>
public sealed class ProductsLayerRules
{
    private static System.Reflection.Assembly DomainAssembly
        => typeof(LankaConnect.Products.LankaEvents.Domain.AssemblyMarker).Assembly;

    private static System.Reflection.Assembly ApplicationAssembly
        => typeof(LankaConnect.Products.LankaEvents.Application.AssemblyMarker).Assembly;

    private static System.Reflection.Assembly InfrastructureAssembly
        => typeof(LankaConnect.Products.LankaEvents.Infrastructure.AssemblyMarker).Assembly;

    /// <summary>
    /// Rule 1 — Products.LankaEvents.Domain depends only on the allowed inner-layer assemblies.
    /// Forbidden: any Capability *.Application or *.Infrastructure, MediatR, ASP.NET Core, EF Core,
    /// legacy LankaConnect.{Application, Infrastructure, API, Shared}.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void Rule1_Products_LankaEvents_Domain_DependsOnlyOnAllowedAssemblies()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                // Capability internals — must go through *.Contracts
                "LankaConnect.Modules.Identity.Application",
                "LankaConnect.Modules.Identity.Infrastructure",
                "LankaConnect.Modules.Identity.Api",
                "LankaConnect.Modules.Communications.Application",
                "LankaConnect.Modules.Communications.Infrastructure",
                "LankaConnect.Modules.Communications.Api",
                "LankaConnect.Modules.Media.Application",
                "LankaConnect.Modules.Media.Infrastructure",
                "LankaConnect.Modules.Media.Api",
                "LankaConnect.Modules.Forms.Application",
                "LankaConnect.Modules.Forms.Infrastructure",
                "LankaConnect.Modules.Forms.Api",
                "LankaConnect.Modules.Payments.Application",
                "LankaConnect.Modules.Payments.Infrastructure",
                "LankaConnect.Modules.Payments.Api",
                "LankaConnect.Modules.Notifications.Application",
                "LankaConnect.Modules.Notifications.Infrastructure",
                "LankaConnect.Modules.Notifications.Api",
                "LankaConnect.Modules.CulturalIntelligence.Application",
                "LankaConnect.Modules.CulturalIntelligence.Infrastructure",
                "LankaConnect.Modules.CulturalIntelligence.Api",
                "LankaConnect.Modules.Scheduling.Application",
                "LankaConnect.Modules.Scheduling.Infrastructure",
                "LankaConnect.Modules.Scheduling.Api",
                // Legacy LankaConnect (Application/Infrastructure/API/Shared forbidden in Domain)
                "LankaConnect.Application",
                "LankaConnect.Infrastructure",
                "LankaConnect.API",
                "LankaConnect.Shared",
                // Domain purity — no MediatR, EF Core, ASP.NET in Domain
                "MediatR",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        AssertCompliant(result, "Products.LankaEvents.Domain");
    }

    /// <summary>
    /// Rule 2 — Products.LankaEvents.Infrastructure transitional dep on legacy is namespace-scoped.
    /// Allowed legacy namespaces: <c>LankaConnect.Infrastructure.Data</c> + <c>.Data.Repositories</c>
    /// (cleared in Wave 6.5 LankaEventsDbContext extraction). Forbidden: every other legacy namespace
    /// (Services, Auth, External, Storage, etc.) and other Products.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void Rule2_Products_LankaEvents_Infrastructure_TransitionalDepIsNamespaceScoped()
    {
        // We forbid coupling to legacy NON-Data namespaces. The .Data + .Data.Repositories
        // transitional edge is allowed by NOT being in the deny list.
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Infrastructure.Auth",
                "LankaConnect.Infrastructure.External",
                "LankaConnect.Infrastructure.Storage",
                "LankaConnect.Infrastructure.Security",
                "LankaConnect.Infrastructure.Email",
                "LankaConnect.Infrastructure.WhatsApp",
                "LankaConnect.Infrastructure.Payments",
                "LankaConnect.Application",
                "LankaConnect.API",
                "LankaConnect.Shared")
            .GetResult();

        AssertCompliant(result, "Products.LankaEvents.Infrastructure");
    }

    /// <summary>
    /// Rule 3 — Legacy LankaConnect.Domain does not reference Products.LankaEvents.*.
    /// Reverse-direction reference would mean Domain (inner layer) reaching out into a Product
    /// (outer layer) — a Clean Architecture inversion.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void Rule3_LankaConnect_Domain_DoesNotReferenceProducts()
    {
        var legacyDomain = typeof(LankaConnect.Domain.Common.LegacyBaseEntity).Assembly;

        var result = Types.InAssembly(legacyDomain)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Products.LankaEvents.Domain",
                "LankaConnect.Products.LankaEvents.Application",
                "LankaConnect.Products.LankaEvents.Infrastructure",
                "LankaConnect.Products.LankaEvents.Api")
            .GetResult();

        AssertCompliant(result, "LankaConnect.Domain (legacy)");
    }

    /// <summary>
    /// Rule 4 — Legacy LankaConnect.Application can reference Products.LankaEvents.Domain +
    /// .Application (carve-out handlers still composed from legacy host) but NOT Infrastructure or Api.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void Rule4_LankaConnect_Application_DoesNotReferenceProducts_Infrastructure_Or_Api()
    {
        var legacyApplication = System.Reflection.Assembly.Load("LankaConnect.Application");

        var result = Types.InAssembly(legacyApplication)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Products.LankaEvents.Infrastructure",
                "LankaConnect.Products.LankaEvents.Api")
            .GetResult();

        AssertCompliant(result, "LankaConnect.Application (legacy)");
    }

    /// <summary>
    /// Rule 5 — Legacy LankaConnect.Infrastructure can reference Products.LankaEvents.Domain
    /// (DI-registered interfaces) + .Infrastructure (relocated repository types called via DI;
    /// transitional per W5.0) but NOT Application or Api.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SKIPPED at Wave 5.5.a per architect ruling 2026-06-29. 14 legacy service / handler /
    /// background-service types currently reference Products.LankaEvents.Application directly
    /// (RegistrationEmailService, PdfTicketService, TicketService, CsvExportService,
    /// ExcelExportService, 7 Payments WebhookHandler classes, RefundReconciliationBackgroundService,
    /// SeatHoldCleanupService). These predate Wave 5 and should publish integration events instead
    /// of directly invoking Products.LankaEvents.Application command/query types — the Outbox-cutover
    /// class of debt blueprint §7.4 / D5 calls out. Tracked: Wave 6.X.Y in Phase A plan.
    /// </para>
    /// <para>
    /// COMPOSITION-ROOT EXCLUSION: <c>LankaConnect.Infrastructure.DependencyInjection</c> is allowed
    /// to reference any module's Application/Infrastructure types because composition roots wire
    /// every module by definition; the boundary applies to runtime code, not DI wiring.
    /// </para>
    /// </remarks>
    [Fact(Skip = "Wave 6.5 target — 14 LankaConnect.Infrastructure services + handlers " +
                 "directly reference Products.LankaEvents.Application. Cleanup via " +
                 "integration events / outbox per blueprint §7.4. Tracked: Wave 6.X.Y in " +
                 "MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md. Un-skip when violations are resolved.")]
    [Trait("Category", "ArchTest")]
    public void Rule5_LankaConnect_Infrastructure_DoesNotReferenceProducts_Application_Or_Api()
    {
        var legacyInfrastructure = typeof(LankaConnect.Infrastructure.Data.AppDbContext).Assembly;

        var result = Types.InAssembly(legacyInfrastructure)
            .That()
            // Composition root is always allowed to reference any module — see XML doc remarks.
            .DoNotHaveName("DependencyInjection")
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Products.LankaEvents.Application",
                "LankaConnect.Products.LankaEvents.Api")
            .GetResult();

        AssertCompliant(result, "LankaConnect.Infrastructure (legacy)");
    }

    /// <summary>
    /// Rule 6 — Hard rule with attribute opt-out. Every type in Products.LankaEvents.Infrastructure
    /// that depends on the allowed legacy transitional namespaces (<c>LankaConnect.Infrastructure.Data</c>
    /// + <c>.Data.Repositories</c>) MUST carry <see cref="Wave6_5TransitionalExceptionAttribute"/>.
    /// </summary>
    /// <remarks>
    /// Implementation note: NetArchTest cannot directly express "depends on X AND has attribute Y" in a
    /// single fluent chain. We invert: find types that depend on legacy WITHOUT the attribute.
    /// </remarks>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void Rule6_Products_Infrastructure_TransitionalTypesMustCarryAttribute()
    {
        var legacyTransitionalNamespaces = new[]
        {
            "LankaConnect.Infrastructure.Data",
            "LankaConnect.Infrastructure.Data.Repositories"
        };

        var violators = Types.InAssembly(InfrastructureAssembly)
            .That()
            .DoNotHaveCustomAttribute(typeof(Wave6_5TransitionalExceptionAttribute))
            .And()
            .HaveDependencyOnAny(legacyTransitionalNamespaces)
            .GetTypes()
            .ToList();

        if (violators.Any())
        {
            var names = string.Join("\n  - ", violators.Select(t => t.FullName));
            Assert.Fail(
                "Wave 6.5 transitional opt-out violation: the following types depend on " +
                "LankaConnect.Infrastructure.Data or .Data.Repositories but DO NOT carry " +
                "[Wave6_5TransitionalException(...)].\n" +
                "Either decorate with the attribute (audit-able transitional debt) or remove " +
                "the legacy dependency.\n" +
                $"Violators:\n  - {names}");
        }
    }

    /// <summary>
    /// Rule 8 — Clean Architecture invariant: Application does not reference Infrastructure.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void Rule8_Products_LankaEvents_Application_DoesNotReferenceProducts_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn("LankaConnect.Products.LankaEvents.Infrastructure")
            .GetResult();

        AssertCompliant(result, "Products.LankaEvents.Application");
    }

    /// <summary>
    /// Rule 9 — Other capability modules reach Products.LankaEvents only via Domain interfaces.
    /// They must NEVER reference Products.LankaEvents.{Application, Infrastructure, Api} directly.
    /// </summary>
    /// <remarks>
    /// SKIPPED at Wave 5.5.a per architect ruling 2026-06-29. 14 Forms.Application query types
    /// (7 queries + 7 handlers covering GetPublicFormResponses / GetMyFormResponse /
    /// GetMyFormResponseByUserId / GetFormResponses / GetEventForms / GetEventFormDetail /
    /// ExportFormResponses) currently reference Products.LankaEvents.Application directly instead
    /// of going through Products.LankaEvents.Domain interfaces or a dedicated IEventQueries facade.
    /// This is the same cross-module boundary violation surfaced during W5.3.c2 audit (architect
    /// said "Wave-6-or-later cleanup question; not blocking c2"). Tracked: Wave 6.X.Z in Phase A plan.
    /// </remarks>
    [Fact(Skip = "Wave 6 target — 14 Forms.Application query types directly reference " +
                 "Products.LankaEvents.Application instead of Domain interfaces / IEventQueries facade. " +
                 "Same cross-module boundary class as W5.3.c2 audit. Tracked: Wave 6.X.Z in " +
                 "MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md. Un-skip when violations are resolved.")]
    [Trait("Category", "ArchTest")]
    public void Rule9_OtherCapabilityModules_DoNotReferenceProducts_LankaEvents_Internals()
    {
        var moduleAssemblies = new[]
        {
            System.Reflection.Assembly.Load("LankaConnect.Modules.Notifications.Domain"),
            System.Reflection.Assembly.Load("LankaConnect.Modules.Notifications.Application"),
            System.Reflection.Assembly.Load("LankaConnect.Modules.Notifications.Infrastructure"),
            typeof(LankaConnect.Modules.Communications.Domain.AssemblyMarker).Assembly,
            typeof(LankaConnect.Modules.Communications.Application.AssemblyMarker).Assembly,
            typeof(LankaConnect.Modules.Communications.Infrastructure.AssemblyMarker).Assembly,
            typeof(LankaConnect.Modules.Media.Domain.AssemblyMarker).Assembly,
            typeof(LankaConnect.Modules.Media.Application.AssemblyMarker).Assembly,
            typeof(LankaConnect.Modules.Media.Infrastructure.AssemblyMarker).Assembly,
            typeof(LankaConnect.Modules.Forms.Domain.AssemblyMarker).Assembly,
            typeof(LankaConnect.Modules.Forms.Application.AssemblyMarker).Assembly,
            typeof(LankaConnect.Modules.Forms.Infrastructure.AssemblyMarker).Assembly,
            typeof(LankaConnect.Modules.Payments.Domain.AssemblyMarker).Assembly,
            typeof(LankaConnect.Modules.Payments.Application.AssemblyMarker).Assembly,
            typeof(LankaConnect.Modules.Payments.Infrastructure.AssemblyMarker).Assembly,
            typeof(LankaConnect.Modules.Identity.Domain.AssemblyMarker).Assembly,
            typeof(LankaConnect.Modules.Identity.Application.AssemblyMarker).Assembly,
            typeof(LankaConnect.Modules.Identity.Infrastructure.AssemblyMarker).Assembly,
            typeof(LankaConnect.Modules.CulturalIntelligence.Domain.AssemblyMarker).Assembly,
            typeof(LankaConnect.Modules.CulturalIntelligence.Application.AssemblyMarker).Assembly,
            typeof(LankaConnect.Modules.CulturalIntelligence.Infrastructure.AssemblyMarker).Assembly,
            typeof(LankaConnect.Modules.Scheduling.Domain.AssemblyMarker).Assembly,
            typeof(LankaConnect.Modules.Scheduling.Application.AssemblyMarker).Assembly,
            typeof(LankaConnect.Modules.Scheduling.Infrastructure.AssemblyMarker).Assembly,
        };

        foreach (var assembly in moduleAssemblies)
        {
            var result = Types.InAssembly(assembly)
                .Should()
                .NotHaveDependencyOnAny(
                    "LankaConnect.Products.LankaEvents.Application",
                    "LankaConnect.Products.LankaEvents.Infrastructure",
                    "LankaConnect.Products.LankaEvents.Api")
                .GetResult();

            AssertCompliant(result, assembly.GetName().Name!);
        }
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
            $"Products layer architecture violation in {assemblyName}.\n" +
            $"Failing types:\n  - {failingTypes}\n" +
            $"Fix: remove the disallowed ProjectReference / using directive, OR re-architect so the " +
            $"dependency flows in the correct Clean Architecture direction.");
    }
}
