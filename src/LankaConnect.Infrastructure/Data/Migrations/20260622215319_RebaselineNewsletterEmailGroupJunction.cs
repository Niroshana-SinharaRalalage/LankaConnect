using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Wave 5.4.d.1b (2026-06-22). EF-snapshot rebaseline after replacing the
    /// Phase 6A.74 Newsletter typed M2M nav
    /// <c>HasMany&lt;EmailGroup&gt;("_emailGroupEntities")...UsingEntity&lt;Dictionary&lt;string, object&gt;&gt;</c>
    /// with an explicit junction CLR type <c>NewsletterEmailGroupLink</c>.
    /// Mirrors the W5.4.c.0 EventEmailGroupLink rebaseline.
    ///
    /// Up() drops a SINGLE foreign key constraint
    /// (<c>FK_newsletter_email_groups_email_groups_email_group_id</c>) — the
    /// junction's <c>email_group_id</c> column is now a raw <c>uuid</c> with no
    /// referential integrity to <c>communications.email_groups</c>, which is
    /// what allows EmailGroup to relocate cleanly in W5.4.d.2. No table is
    /// dropped; column shape, composite PK, and indexes are unchanged.
    ///
    /// The auto-scaffolded <c>reference_values.created_at</c> UpdateData calls
    /// are removed by hand (same Phase 6A.47 seed-time DateTime.UtcNow noise
    /// pattern as the W5.4.c.0 migration). The snapshot's new timestamps are
    /// kept to silence future drift.
    /// </summary>
    public partial class RebaselineNewsletterEmailGroupJunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_newsletter_email_groups_email_groups_email_group_id",
                schema: "communications",
                table: "newsletter_email_groups");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_newsletter_email_groups_email_groups_email_group_id",
                schema: "communications",
                table: "newsletter_email_groups",
                column: "email_group_id",
                principalSchema: "communications",
                principalTable: "email_groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
