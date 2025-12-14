using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenItemsCategoryPhase6A27 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payments");

            migrationBuilder.AddColumn<bool>(
                name: "has_open_items",
                schema: "events",
                table: "sign_up_lists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_user_id",
                schema: "events",
                table: "sign_up_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "stripe_customers",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stripe_customer_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    stripe_created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stripe_customers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stripe_customers_stripe_customer_id",
                schema: "payments",
                table: "stripe_customers",
                column: "stripe_customer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stripe_customers_user_id",
                schema: "payments",
                table: "stripe_customers",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stripe_customers",
                schema: "payments");

            migrationBuilder.DropColumn(
                name: "has_open_items",
                schema: "events",
                table: "sign_up_lists");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                schema: "events",
                table: "sign_up_items");
        }
    }
}
