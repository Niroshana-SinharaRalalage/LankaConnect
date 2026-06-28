using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Wave 5.2.d-hotfix2 (2026-06-28). EF-snapshot rebaseline after replacing
    /// the Phase 6A.74 Newsletter typed M2M nav
    /// <c>HasMany&lt;MetroArea&gt;("_metroAreaEntities")...UsingEntity&lt;Dictionary&lt;string, object&gt;&gt;</c>
    /// (with the broken <c>List&lt;object&gt;</c> retyping from W5.1) with an
    /// explicit junction CLR type <c>NewsletterMetroAreaLink</c>. Mirrors the
    /// W5.4.d.1b NewsletterEmailGroupLink rebaseline (migration 20260622215319).
    ///
    /// Up() drops a SINGLE foreign key constraint
    /// (<c>FK_newsletter_metro_areas_metro_areas_metro_area_id</c>) -- the
    /// junction's <c>metro_area_id</c> column is now a raw <c>uuid</c> with no
    /// referential integrity to <c>events.metro_areas</c>, which is the SAME
    /// pattern the team approved for EmailGroup in W5.4.d.1b. No table is
    /// dropped; 2,662 existing rows are preserved; column shape, composite PK,
    /// indexes unchanged.
    ///
    /// SQL-level impact on staging (verified pre-push via psql probe):
    ///   - newsletter_metro_areas table exists in communications schema with
    ///     2,662 rows + both FKs (to newsletters and metro_areas)
    ///   - Up() removes the metro_areas FK only; newsletters FK preserved
    ///   - Down() restores the metro_areas FK (Cascade)
    ///
    /// DropForeignKey is NOT in CLAUDE.md §5 rule 2's destructive-DDL lint
    /// list (DropTable / DropColumn / RenameTable / RenameColumn only), so
    /// no SCHEMA-DESTRUCTIVE-APPROVED header required. Same as W5.4.d.1b
    /// which already shipped with the identical pattern.
    ///
    /// The auto-scaffolded reference_values.created_at UpdateData calls
    /// (Phase 6A.47 seed-time DateTime.UtcNow churn) are stripped by hand
    /// per [[empty-up-snapshot-rebaseline]] precedent -- would overwrite
    /// production audit timestamps for no functional change.
    /// </summary>
    public partial class Wave5_2d_hotfix2_NewsletterMetroAreaLinkJunction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_newsletter_metro_areas_metro_areas_metro_area_id",
                schema: "communications",
                table: "newsletter_metro_areas");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_newsletter_metro_areas_metro_areas_metro_area_id",
                schema: "communications",
                table: "newsletter_metro_areas",
                column: "metro_area_id",
                principalSchema: "events",
                principalTable: "metro_areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
