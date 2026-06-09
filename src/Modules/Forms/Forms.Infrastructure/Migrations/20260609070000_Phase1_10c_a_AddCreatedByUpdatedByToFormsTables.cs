using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Modules.Forms.Infrastructure.Migrations
{
    /// <summary>
    /// Wave4.9.2.10c.a Phase 1.10c.a (2026-06-09) — adds physical
    /// <c>created_by</c> and <c>updated_by</c> columns on the 4 Forms
    /// module tables (cross-schema overrides into <c>events</c>):
    /// <c>events.event_forms</c>, <c>events.form_questions</c>,
    /// <c>events.form_responses</c>, <c>events.form_answers</c>.
    /// 8 columns total.
    ///
    /// Generated via FormsDbContext (not AppDbContext) because the
    /// Forms module owns these tables' migration history per the
    /// W4.3 module-DbContext pattern.
    /// </summary>
    public partial class Phase1_10c_a_AddCreatedByUpdatedByToFormsTables : Migration
    {
        private static readonly string[] Tables = new[]
        {
            "event_forms",
            "form_questions",
            "form_responses",
            "form_answers",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.AddColumn<string>(name: "created_by", schema: "events", table: table, type: "text", nullable: true);
                migrationBuilder.AddColumn<string>(name: "updated_by", schema: "events", table: table, type: "text", nullable: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.DropColumn(name: "created_by", schema: "events", table: table);
                migrationBuilder.DropColumn(name: "updated_by", schema: "events", table: table);
            }
        }
    }
}
