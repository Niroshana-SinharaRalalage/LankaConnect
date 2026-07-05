using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Wave4.9.2.10d Phase 1.10d (2026-06-09) — FINAL events-schema
    /// subset: donations + refunds + addons + registration mode
    /// conversions (10 tables, 20 columns):
    /// donations, refund_requests, refund_request_line_items,
    /// registration_additions, registration_payments,
    /// add_on_definitions, add_on_purchases, collections,
    /// registration_mode_conversions, registration_mode_conversion_rows.
    ///
    /// Completes the Wave4.9.2 physical CreatedBy/UpdatedBy rollout
    /// for ALL IAuditable entities across AppDbContext + FormsDbContext
    /// + MediaDbContext. 65 tables total.
    /// </summary>
    public partial class Phase1_10d_AddCreatedByUpdatedByToEventsFinances : Migration
    {
        private static readonly string[] Tables = new[]
        {
            "donations",
            "refund_requests",
            "refund_request_line_items",
            "registration_additions",
            "registration_payments",
            "add_on_definitions",
            "add_on_purchases",
            "collections",
            "registration_mode_conversions",
            "registration_mode_conversion_rows",
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
