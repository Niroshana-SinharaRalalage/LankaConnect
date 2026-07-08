using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.146 — adds the AllowAttendeesToViewResponses boolean column to
    /// events.event_forms. New rows default to false; existing rows are filled
    /// with false by the DB-level DEFAULT (status-quo privacy preserved for
    /// every form that existed before this migration). The architect-approved
    /// design strips PII at the projection layer rather than at the column
    /// level, so no data backfill or migration is required for response rows.
    ///
    /// Spurious reference_data.created_at UpdateData calls scaffolded by EF
    /// (caused by seed-time DateTime drift) were removed by hand — they
    /// affect no behavior and have been stripped from every recent migration
    /// in this project (see Phase 6A.143, 6A.145).
    /// </summary>
    public partial class Phase6A146_AddResponseVisibilityToEventForms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "allow_attendees_to_view_responses",
                schema: "events",
                table: "event_forms",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allow_attendees_to_view_responses",
                schema: "events",
                table: "event_forms");
        }
    }
}
