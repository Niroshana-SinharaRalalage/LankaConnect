using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Wave4.9.2.9 Phase 1.9 (2026-06-09) — adds physical
    /// <c>created_by</c> and <c>updated_by</c> columns on the WhatsApp
    /// subset of the communications schema (4 tables, 8 columns):
    /// <c>user_whatsapp_preferences</c>, <c>whatsapp_messages</c>,
    /// <c>whatsapp_templates</c>, <c>whatsapp_webhook_events</c>.
    /// </summary>
    public partial class Phase1_9_AddCreatedByUpdatedByToCommunicationsWhatsApp : Migration
    {
        private static readonly string[] Tables = new[]
        {
            "user_whatsapp_preferences",
            "whatsapp_messages",
            "whatsapp_templates",
            "whatsapp_webhook_events",
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
