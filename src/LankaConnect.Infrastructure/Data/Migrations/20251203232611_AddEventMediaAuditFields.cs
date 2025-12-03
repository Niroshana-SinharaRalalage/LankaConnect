using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventMediaAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add CreatedAt column to EventVideos
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "events",
                table: "EventVideos",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            // Add UpdatedAt column to EventVideos
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "events",
                table: "EventVideos",
                type: "timestamp with time zone",
                nullable: true);

            // Add CreatedAt column to EventImages
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "events",
                table: "EventImages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            // Add UpdatedAt column to EventImages
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "events",
                table: "EventImages",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "events",
                table: "EventVideos");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "events",
                table: "EventVideos");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "events",
                table: "EventImages");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "events",
                table: "EventImages");
        }
    }
}
