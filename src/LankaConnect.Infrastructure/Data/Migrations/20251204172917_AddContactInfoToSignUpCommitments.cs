using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContactInfoToSignUpCommitments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                schema: "events",
                table: "sign_up_commitments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                schema: "events",
                table: "sign_up_commitments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                schema: "events",
                table: "sign_up_commitments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactEmail",
                schema: "events",
                table: "sign_up_commitments");

            migrationBuilder.DropColumn(
                name: "ContactName",
                schema: "events",
                table: "sign_up_commitments");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                schema: "events",
                table: "sign_up_commitments");
        }
    }
}
