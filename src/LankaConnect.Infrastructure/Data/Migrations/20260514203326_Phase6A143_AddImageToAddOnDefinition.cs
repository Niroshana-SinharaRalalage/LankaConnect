using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 6A.143 — Adds optional image_url and image_blob_name columns to
    /// events.add_on_definitions so organizers can upload an image per add-on
    /// (rendered as a thumbnail in AddOnSelector + management list view).
    ///
    /// Both columns are nullable text. Handler enforces atomic set/clear and
    /// blob cleanup on replace. Existing rows have NULL on both fields.
    ///
    /// Reference-data created_at drift rows (scaffolded automatically) were
    /// hand-removed per the same convention used in Phase 6A.141.
    /// </summary>
    public partial class Phase6A143_AddImageToAddOnDefinition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_blob_name",
                schema: "events",
                table: "add_on_definitions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                schema: "events",
                table: "add_on_definitions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_blob_name",
                schema: "events",
                table: "add_on_definitions");

            migrationBuilder.DropColumn(
                name: "image_url",
                schema: "events",
                table: "add_on_definitions");
        }
    }
}
