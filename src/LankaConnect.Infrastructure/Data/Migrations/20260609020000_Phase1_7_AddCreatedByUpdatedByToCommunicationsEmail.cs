using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Wave4.9.2.7 Phase 1.7 (2026-06-09) — adds physical
    /// <c>created_by</c> and <c>updated_by</c> columns on the email-side
    /// subset of the communications schema group (8 tables, 16 columns):
    /// <list type="bullet">
    ///   <item><c>communications.email_dispatch_log</c></item>
    ///   <item><c>communications.email_failure_details</c></item>
    ///   <item><c>communications.email_groups</c></item>
    ///   <item><c>communications.email_messages</c></item>
    ///   <item><c>communications.email_metrics</c></item>
    ///   <item><c>communications.email_templates</c></item>
    ///   <item><c>communications.event_notification_history</c></item>
    ///   <item><c>communications.user_email_preferences</c></item>
    /// </list>
    /// Newsletter (1.8) and WhatsApp (1.9) entities ship in subsequent
    /// phases to keep blast radius bounded.
    /// </summary>
    public partial class Phase1_7_AddCreatedByUpdatedByToCommunicationsEmail : Migration
    {
        private static readonly string[] Tables = new[]
        {
            "email_dispatch_log",
            "email_failure_details",
            "email_groups",
            "email_messages",
            "email_metrics",
            "email_templates",
            "event_notification_history",
            "user_email_preferences",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.AddColumn<string>(name: "created_by", schema: "communications", table: table, type: "text", nullable: true);
                migrationBuilder.AddColumn<string>(name: "updated_by", schema: "communications", table: table, type: "text", nullable: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.DropColumn(name: "created_by", schema: "communications", table: table);
                migrationBuilder.DropColumn(name: "updated_by", schema: "communications", table: table);
            }
        }
    }
}
