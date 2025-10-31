using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfilePhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePhotoBlobName",
                schema: "identity",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePhotoUrl",
                schema: "identity",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cultural_context",
                schema: "communications",
                table: "email_messages",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePhotoBlobName",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ProfilePhotoUrl",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "cultural_context",
                schema: "communications",
                table: "email_messages");
        }
    }
}
