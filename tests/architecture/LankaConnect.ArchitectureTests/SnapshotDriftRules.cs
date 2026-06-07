using System.Text.RegularExpressions;

namespace LankaConnect.ArchitectureTests;

/// <summary>
/// Phase 0 (2026-06-07) — turns silent EF Core snapshot drift into a build failure.
/// </summary>
/// <remarks>
/// <para>
/// The 2026-06-07 retro on the W4.2 Media + W4.3 Forms extractions revealed a
/// failure mode: when an entity moves from <c>AppDbContext</c> to a module-owned
/// <see cref="Microsoft.EntityFrameworkCore.DbContext"/>, the legacy
/// <c>AppDbContextModelSnapshot.cs</c> snapshot retains the moved entity's
/// type-name string until the NEXT <c>dotnet ef migrations add</c> is run on
/// <c>AppDbContext</c>. EF then auto-generates destructive DDL (DropTable +
/// DropForeignKey) for the moved table — exactly the 2,038-line monster the
/// retro caught.
/// </para>
/// <para>
/// These tests fail the build the moment any module entity name leaks into a
/// legacy snapshot, forcing the extraction commit to also include the snapshot
/// resync. Per the no-Docker discipline in CLAUDE.md §5: CI gates do the work
/// that local Postgres dry-run would have done.
/// </para>
/// </remarks>
public sealed class SnapshotDriftRules
{
    /// <summary>
    /// Legacy <c>AppDbContextModelSnapshot.cs</c> must not reference any entity
    /// from the <c>LankaConnect.Modules.*</c> namespace. Module entities live in
    /// their own DbContexts (per ADR-002 module boundary); presence here is
    /// snapshot drift and will produce a destructive DropTable on the next
    /// AppDbContext migration.
    /// </summary>
    [Fact]
    [Trait("Category", "ArchTest")]
    public void AppDbContextSnapshot_DoesNotReferenceAnyModulesEntity()
    {
        // Phase 0.5 (2026-06-07) consolidated the two-migration-directory split;
        // the snapshot now lives under Data/Migrations/. The earlier path
        // (Migrations/AppDbContextModelSnapshot.cs) is gone.
        var snapshotPath = LocateSnapshot(
            relativePath: Path.Combine(
                "src", "LankaConnect.Infrastructure", "Data", "Migrations", "AppDbContextModelSnapshot.cs"));

        var content = File.ReadAllText(snapshotPath);

        // Match a `modelBuilder.Entity("LankaConnect.Modules.<X>...")` reference.
        // This is the exact shape EF writes into snapshot files for tracked entities.
        var pattern = new Regex(
            pattern: @"modelBuilder\.Entity\(""LankaConnect\.Modules\.([A-Za-z0-9_.]+)""",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        var matches = pattern.Matches(content);

        if (matches.Count == 0)
        {
            return;
        }

        var leakedTypes = matches
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        Assert.Fail(
            $"AppDbContextModelSnapshot.cs references {leakedTypes.Count} entity " +
            $"type(s) from LankaConnect.Modules.*:\n  - " +
            string.Join("\n  - ", leakedTypes) + "\n\n" +
            "These entities live in module-owned DbContexts and should not appear in the " +
            "legacy AppDbContext snapshot. Their presence will cause `dotnet ef migrations add` " +
            "on AppDbContext to generate destructive DropTable / DropForeignKey statements that " +
            "would conflict with the module schema-rename migrations. " +
            "Fix: regenerate AppDbContext snapshot via an empty-Up()/Down() snapshot-sync migration " +
            "with manually-pruned Designer.cs + ModelSnapshot.cs entries (Phase 3 pattern).");
    }

    private static string LocateSnapshot(string relativePath)
    {
        // Walk up from the test assembly until we find the solution root (the
        // directory containing LankaConnect.sln), then join with the relative path.
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "LankaConnect.sln")))
            {
                var full = Path.Combine(current.FullName, relativePath);
                if (File.Exists(full))
                {
                    return full;
                }
                throw new FileNotFoundException(
                    $"Found LankaConnect.sln at '{current.FullName}' but the expected " +
                    $"snapshot file does not exist at '{full}'. Either the snapshot was " +
                    $"deleted (also bad — this test cannot run) or the relative path is wrong.");
            }
            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the solution root (no LankaConnect.sln found by walking up from " +
            $"{AppContext.BaseDirectory}). This test assumes it runs from a project that lives " +
            "inside the LankaConnect solution.");
    }
}
