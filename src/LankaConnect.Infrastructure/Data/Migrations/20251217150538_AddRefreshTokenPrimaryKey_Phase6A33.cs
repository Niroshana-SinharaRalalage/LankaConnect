using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenPrimaryKey_Phase6A33 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_user_refresh_tokens",
                schema: "identity",
                table: "user_refresh_tokens");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_refresh_tokens",
                schema: "identity",
                table: "user_refresh_tokens",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_user_refresh_tokens_UserId",
                schema: "identity",
                table: "user_refresh_tokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_user_refresh_tokens",
                schema: "identity",
                table: "user_refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_user_refresh_tokens_UserId",
                schema: "identity",
                table: "user_refresh_tokens");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_refresh_tokens",
                schema: "identity",
                table: "user_refresh_tokens",
                columns: new[] { "UserId", "Id" });
        }
    }
}
