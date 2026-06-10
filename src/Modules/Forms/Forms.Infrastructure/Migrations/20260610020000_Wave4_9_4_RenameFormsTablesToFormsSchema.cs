using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Modules.Forms.Infrastructure.Migrations
{
    /// <summary>
    /// SCHEMA-DESTRUCTIVE-APPROVED: cross-schema rename of legacy aggregate tables
    /// events.event_forms → forms.event_forms, events.form_questions → forms.form_questions,
    /// events.form_responses → forms.form_responses, events.form_answers → forms.form_answers
    /// per W4.3 architect ruling (2026-06-06) deferred to Wave 4.9.4 (2026-06-10).
    /// Operation uses ALTER TABLE ... SET SCHEMA — catalog-only, FK-preserving,
    /// index-preserving, sub-100ms apply. Mirrors the Wave 4.9.3 (Media) pattern
    /// that landed successfully on staging 2026-06-10.
    /// FK probe (verified by architect via cross-migration source grep): zero
    /// external inbound FKs. Internal FKs form_questions → event_forms and
    /// form_answers → form_responses are OID-tracked and survive SET SCHEMA.
    /// Rollback: forward SET SCHEMA events via Down().
    /// </summary>
    public partial class Wave4_9_4_RenameFormsTablesToFormsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "forms");
            migrationBuilder.Sql("ALTER TABLE events.event_forms SET SCHEMA forms;");
            migrationBuilder.Sql("ALTER TABLE events.form_questions SET SCHEMA forms;");
            migrationBuilder.Sql("ALTER TABLE events.form_responses SET SCHEMA forms;");
            migrationBuilder.Sql("ALTER TABLE events.form_answers SET SCHEMA forms;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE forms.form_answers SET SCHEMA events;");
            migrationBuilder.Sql("ALTER TABLE forms.form_responses SET SCHEMA events;");
            migrationBuilder.Sql("ALTER TABLE forms.form_questions SET SCHEMA events;");
            migrationBuilder.Sql("ALTER TABLE forms.event_forms SET SCHEMA events;");
        }
    }
}
