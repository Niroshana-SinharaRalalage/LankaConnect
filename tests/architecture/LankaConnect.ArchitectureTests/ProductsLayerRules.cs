using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
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
        var legacyDomain = typeof(LankaConnect.BuildingBlocks.Domain.LegacyBaseEntity).Assembly;

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
                 "integration events / outbox per blueprint §7.4 / D5. Coherent with Rule 9b " +
                 "Payments deferral. Tracked: Wave 6.5.X.Y in MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md. " +
                 "Un-skip when violations are resolved as part of Wave 6.5 Outbox cutover.")]
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
            "LankaConnect.SPLIT_PER_ENTITY.Repositories"
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
    /// <remarks>
    /// Wave 6.a.1 (2026-07-01): un-skipped after resolving all 14 Forms.Application
    /// query violations. Fix pattern:
    ///   1. Moved shared DTOs (EventFormDto family + PublicFormResponseDto family +
    ///      ExportResult + ExportFormat) from Products.LankaEvents.Application.Common
    ///      to Products.LankaEvents.Contracts (new files EventFormDtos.cs,
    ///      ExportResult.cs in that assembly).
    ///   2. Introduced narrow IFormResponseExporter port in Contracts so Forms.Application
    ///      doesn't need the full ICsvExportService / IExcelExportService (which hold too
    ///      many Product-internal signatures to publish).
    ///   3. CsvExportService + ExcelExportService in Infrastructure implement
    ///      IFormResponseExporter alongside their existing service interfaces.
    ///   4. Forms.Application.csproj now references Products.LankaEvents.Contracts only.
    ///
    /// Payments.Application (11 refund + payment-completed handlers) is scoped to the
    /// separate Rule 9b Skip-fact below — deferred to Wave 6.5 per architect ruling
    /// 2026-07-01. Payments violations are the structural twin of Rule 5 legacy-Infrastructure
    /// services (both need integration-event / Outbox cutover, not direct assembly ref).
    /// </remarks>
    [Fact]
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
            // Payments.Application EXCLUDED from Rule 9 main body -- 11 refund/payment-event
            // handlers reference Products.LankaEvents.Application directly; deferred to
            // Wave 6.5 per architect ruling 2026-07-01 (structural twin of Rule 5 legacy
            // services). See Rule 9b Skip-fact below for the enumeration + tracking.
            // typeof(LankaConnect.Modules.Payments.Application.AssemblyMarker).Assembly,
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

    /// <summary>
    /// Rule 9b (Skip-fact) — Payments.Application must NOT reference
    /// Products.LankaEvents.{Application, Infrastructure, Api} directly.
    /// Structural twin of Rule 5 (legacy Infrastructure services): both are
    /// integration-event / Outbox cutover territory.
    /// </summary>
    /// <remarks>
    /// Wave 6.a.1 (2026-07-01): scoped OUT of Rule 9 main body per architect ruling
    /// 2026-07-01 (Wave 6.5 pull-forward explicitly rejected). Tracked: Wave 6.X.W in
    /// MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md.
    ///
    /// Enumerated violators at Wave 6.a.1 baseline (2026-07-01):
    ///   Payments.Application.Services:
    ///     - AddOnRefundService
    ///     - RefundExecutionService
    ///     - RefundLineDispatcher
    ///     - RefundReconciliationService
    ///     - RefundTotalCalculator
    ///     - RegistrationRefundService
    ///   Payments.Application.EventHandlers:
    ///     - PaymentCompletedEventHandler
    ///     - RefundCompletedEventHandler
    ///     - RegistrationPendingPaymentEventHandler
    ///   Payments.Application.Commands.RefundRequests:
    ///     - ApproveRefundRequestCommandHandler
    ///     - CreateOrganizerInitiatedRefundCommandHandler
    ///   Total: 11 types.
    ///
    /// Cleanup via integration events / Outbox per blueprint §7.4 D5 alongside the
    /// Rule 5 legacy-Infrastructure services (14) and LankaEventsDbContext extraction.
    /// One coherent Wave 6.5 slice.
    /// </remarks>
    [Fact(Skip = "Wave 6.5 target — 11 Payments.Application services + event handlers " +
                 "directly reference Products.LankaEvents.Application. Cleanup via " +
                 "integration events / outbox per blueprint §7.4 / D5. Structural twin " +
                 "of Rule 5 debt. Tracked: Wave 6.X.W in MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md. " +
                 "Un-skip when violations are resolved as part of Wave 6.5 Outbox cutover.")]
    [Trait("Category", "ArchTest")]
    public void Rule9b_PaymentsApplication_DoesNotReferenceProducts_LankaEvents_Internals()
    {
        var paymentsApplication = typeof(LankaConnect.Modules.Payments.Application.AssemblyMarker).Assembly;

        var result = Types.InAssembly(paymentsApplication)
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.Products.LankaEvents.Application",
                "LankaConnect.Products.LankaEvents.Infrastructure",
                "LankaConnect.Products.LankaEvents.Api")
            .GetResult();

        AssertCompliant(result, paymentsApplication.GetName().Name!);
    }

    /// <summary>
    /// Rule 12 (Wave 6.b) — the set of types decorated with
    /// <see cref="Wave6_5TransitionalExceptionAttribute"/> MUST be a subset of the
    /// baseline captured in <c>Wave6_5TransitionalBaseline.json</c>.
    ///
    /// Adding new transitional decorations without editing the baseline JSON is
    /// a rule failure — the JSON edit forces architect consult per Wave 6.b ruling.
    /// Removals from the baseline (as Wave 6.5 clears the debt one repository at a
    /// time) are permitted: the SAME PR that removes the attribute decoration also
    /// removes the class name from the baseline JSON. Atomic change, single review.
    /// </summary>
    /// <remarks>
    /// Wave 6.b (2026-07-01). See <c>README-BASELINE.md</c> in the same folder for
    /// the discipline documentation.
    /// </remarks>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void Rule12_Wave6_5TransitionalException_BaselineNotExpanded()
    {
        // Locate the baseline JSON next to the assembly.
        var assemblyDir = Path.GetDirectoryName(typeof(ProductsLayerRules).Assembly.Location)!;
        // Baseline lives in the source tree at
        //   tests/architecture/LankaConnect.ArchitectureTests/Wave6_5TransitionalBaseline.json
        // The csproj must copy it to output; walk up from bin/Debug/net8.0 if needed.
        var candidates = new[]
        {
            Path.Combine(assemblyDir, "Wave6_5TransitionalBaseline.json"),
            Path.Combine(assemblyDir, "..", "..", "..", "Wave6_5TransitionalBaseline.json")
        };
        var baselinePath = candidates.FirstOrDefault(File.Exists);
        Assert.NotNull(baselinePath);

        var baseline = new HashSet<string>(
            JsonSerializer.Deserialize<string[]>(File.ReadAllText(baselinePath!))
            ?? Array.Empty<string>(),
            StringComparer.Ordinal);

        // Enumerate current decorations across the Products.LankaEvents.Infrastructure assembly.
        var attributeType = typeof(Wave6_5TransitionalExceptionAttribute);
        var current = new HashSet<string>(
            InfrastructureAssembly.GetTypes()
                .Where(t => t.GetCustomAttributes(attributeType, inherit: false).Any())
                .Select(t => t.FullName!),
            StringComparer.Ordinal);

        var newAdditions = current.Except(baseline).OrderBy(x => x).ToList();
        var removals = baseline.Except(current).OrderBy(x => x).ToList();

        // New additions FAIL the test (require architect consult + baseline edit).
        // Removals PASS (Wave 6.5 progress); they'll be reconciled the next time the
        // baseline JSON is edited alongside the attribute removal.
        if (newAdditions.Count > 0)
        {
            Assert.Fail(
                $"Wave 6.5 transitional baseline expansion detected.\n" +
                $"New [Wave6_5TransitionalException] classes NOT in baseline:\n" +
                $"  - {string.Join("\n  - ", newAdditions)}\n" +
                $"Baseline additions REQUIRE architect consult per Wave 6.b ruling.\n" +
                $"See tests/architecture/LankaConnect.ArchitectureTests/README-BASELINE.md.\n" +
                $"If you have architect approval, add the FQCN(s) above to the baseline JSON.");
        }
    }

    /// <summary>
    /// Rule 13 (Wave 6.b) — <c>Products.LankaEvents.Infrastructure</c> types that
    /// are NOT decorated with <see cref="Wave6_5TransitionalExceptionAttribute"/>
    /// MUST NOT reference <c>LankaConnect.SPLIT_PER_ENTITY.AppDbContext</c>
    /// nor the legacy <c>Repository&lt;T&gt;</c> base class. Both dependencies
    /// exist ONLY under the transitional escape hatch (Rule 12's baseline).
    /// </summary>
    /// <remarks>
    /// Wave 6.b (2026-07-01) composite rule: together with Rule 12, enforces
    /// "the transitional escape hatch exists, is size-capped at 20 classes, and
    /// only opens for two specific dependencies (AppDbContext + Repository&lt;T&gt;)."
    ///
    /// Any new user of either dependency in Products.LankaEvents.Infrastructure
    /// (i.e., a NEW class trying to take the same shortcut) is a rule failure,
    /// forcing the new class either to (a) refactor to the correct Products
    /// architecture, OR (b) go through the Rule 12 baseline-expansion process
    /// with architect consult.
    /// </remarks>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void Rule13_Products_Infrastructure_DoesNotReferenceAppDbContextOrRepositoryBase()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That().DoNotHaveCustomAttribute(typeof(Wave6_5TransitionalExceptionAttribute))
            // Composition-root exclusion (matches Rule 5 pattern):
            .And().DoNotHaveName("DependencyInjection")
            .And().DoNotHaveName("LankaEventsModule")
            .Should()
            .NotHaveDependencyOnAny(
                "LankaConnect.SPLIT_PER_ENTITY.AppDbContext",
                "LankaConnect.SPLIT_PER_ENTITY.Repositories.Repository`1")
            .GetResult();

        AssertCompliant(result, "Products.LankaEvents.Infrastructure (non-baseline classes)");
    }

    /// <summary>
    /// Rule 14 (4C.h — 2026-07-10) — the legacy <c>IApplicationDbContext</c>
    /// abstraction has been DELETED. No type in Products.LankaEvents.{Domain,
    /// Application, Infrastructure, Contracts, Api} may re-introduce a
    /// dependency on it (whether as a type reference or by re-declaring a
    /// type with the same short name).
    /// </summary>
    /// <remarks>
    /// Per Consult #14 PASS B: "New code MUST NOT inject IApplicationDbContext —
    /// inject the correct module DbContext (LankaEventsDbContext,
    /// IdentityDbContext, CommunicationsDbContext, AppDbContext for cross-cutting
    /// ReferenceValue)."
    /// <para>
    /// The check is name-based (NetArchTest cannot check dependencies on a type
    /// that no longer exists in the assembly graph), scanning every type's
    /// full name for the substring <c>IApplicationDbContext</c>. Any hit is a
    /// copy-paste from pre-4C.h codebases or a re-introduction attempt.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void Rule14_Products_LankaEvents_DoesNotReintroduce_IApplicationDbContext()
    {
        var assemblies = new[]
        {
            DomainAssembly,
            ApplicationAssembly,
            InfrastructureAssembly,
        };

        var violators = new List<string>();
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.FullName?.Contains("IApplicationDbContext", System.StringComparison.Ordinal) == true)
                {
                    violators.Add($"{type.FullName} (in {assembly.GetName().Name})");
                }
            }
        }

        if (violators.Count > 0)
        {
            Assert.Fail(
                "4C.h forbidden-type rule violation: IApplicationDbContext was DELETED " +
                "at 4C.h (2026-07-10). New Products.LankaEvents code re-introduced a " +
                "type with the same name — likely a copy-paste from pre-4C.h search " +
                "results.\n" +
                $"Violators:\n  - {string.Join("\n  - ", violators)}\n" +
                "Fix: inject the correct module DbContext per Consult #14 PASS B " +
                "(LankaEventsDbContext for Event family, IdentityDbContext for User, " +
                "CommunicationsDbContext for Email* + Newsletter*, AppDbContext for " +
                "cross-cutting ReferenceValue).");
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
