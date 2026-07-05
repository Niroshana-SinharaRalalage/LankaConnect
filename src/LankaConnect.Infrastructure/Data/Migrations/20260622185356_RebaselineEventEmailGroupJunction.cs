using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Wave 5.4.c.0 (2026-06-22). EF-snapshot rebaseline after replacing the
    /// Phase 6A.32 typed M2M nav <c>HasMany&lt;EmailGroup&gt;("_emailGroupEntities")
    /// ...UsingEntity&lt;Dictionary&lt;string, object&gt;&gt;</c> with an explicit
    /// junction CLR type <c>EventEmailGroupLink</c>.
    ///
    /// Up() drops a SINGLE foreign key constraint
    /// (<c>FK_event_email_groups_email_groups_email_group_id</c>) — the
    /// junction's <c>email_group_id</c> column is now a raw <c>uuid</c> with no
    /// referential integrity to <c>communications.email_groups</c>, which is
    /// what makes Wave 5.4.d.3 cut <c>LankaConnect.Domain -&gt;
    /// Communications.Domain</c>. No table is dropped; column shape, composite
    /// PK, and indexes are unchanged.
    ///
    /// The auto-scaffolded <c>reference_values.created_at</c> UpdateData calls
    /// are removed by hand — they are noise caused by Phase 6A.47 seed data
    /// using <c>DateTime.UtcNow</c>. The snapshot's new timestamps were kept
    /// to silence future drift; no migration action is required.
    /// </summary>
    public partial class RebaselineEventEmailGroupJunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_event_email_groups_email_groups_email_group_id",
                table: "event_email_groups");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_event_email_groups_email_groups_email_group_id",
                table: "event_email_groups",
                column: "email_group_id",
                principalSchema: "communications",
                principalTable: "email_groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
