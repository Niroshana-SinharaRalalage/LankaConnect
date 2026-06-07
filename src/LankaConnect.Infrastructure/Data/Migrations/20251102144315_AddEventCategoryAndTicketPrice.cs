using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventCategoryAndTicketPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "events",
                table: "events",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Community");

            migrationBuilder.AddColumn<decimal>(
                name: "ticket_price_amount",
                schema: "events",
                table: "events",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ticket_price_currency",
                schema: "events",
                table: "events",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "ticket_price_amount",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "ticket_price_currency",
                schema: "events",
                table: "events");
        }
    }
}
