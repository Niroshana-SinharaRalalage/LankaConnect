using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailGroupsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_groups",
                schema: "communications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_addresses = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_groups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailGroups_IsActive",
                schema: "communications",
                table: "email_groups",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_EmailGroups_Owner_IsActive",
                schema: "communications",
                table: "email_groups",
                columns: new[] { "owner_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailGroups_Owner_Name",
                schema: "communications",
                table: "email_groups",
                columns: new[] { "owner_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailGroups_OwnerId",
                schema: "communications",
                table: "email_groups",
                column: "owner_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_groups",
                schema: "communications");
        }
    }
}
