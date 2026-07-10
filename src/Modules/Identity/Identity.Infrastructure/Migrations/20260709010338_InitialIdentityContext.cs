using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Modules.Identity.Infrastructure.Migrations
{
    /// <summary>
    /// Sub-slice 4C.e.3 (2026-07-08) — IdentityDbContext baseline migration.
    ///
    /// EMPTY-Up() REBASELINE per <c>[[feedback-empty-up-snapshot-rebaseline]]</c>.
    /// The auto-generated CreateTable calls have been intentionally removed —
    /// every table in this migration (users, external_logins, user_refresh_tokens,
    /// user_preferred_metro_areas, user_cultural_interests, user_languages)
    /// already exists in the physical database, materialized by earlier
    /// <c>AppDbContext</c> migrations before UserConfiguration was relocated to
    /// Identity.Infrastructure by 4C.e.1.
    ///
    /// The Designer.cs + IdentityDbContextModelSnapshot.cs files stay EF-generated
    /// and unmodified — they represent the model shape the IdentityDbContext expects.
    /// Only the Up()/Down() bodies are neutralized so `dotnet ef database update`
    /// registers the migration in <c>identity.__EFMigrationsHistory</c> without
    /// touching the existing schema.
    /// </summary>
    public partial class InitialIdentityContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 4C.e.3: empty-Up() rebaseline. Physical DDL already applied by
            // AppDbContext migrations pre-4C.e. This migration only registers the
            // baseline in identity.__EFMigrationsHistory.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 4C.e.3: no-op mirror of Up().
        }
    }
}
