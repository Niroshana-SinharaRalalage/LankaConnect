using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingJsonbColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add the new JSONB column
            migrationBuilder.AddColumn<string>(
                name: "ticket_price",
                schema: "events",
                table: "events",
                type: "jsonb",
                nullable: true);

            // Step 2: Migrate existing ticket_price data to JSONB format
            // Only migrate rows that have both amount and currency set
            migrationBuilder.Sql(@"
                UPDATE events.events
                SET ticket_price = jsonb_build_object(
                    'Amount', ticket_price_amount,
                    'Currency', ticket_price_currency
                )
                WHERE ticket_price_amount IS NOT NULL
                  AND ticket_price_currency IS NOT NULL;
            ");

            // Step 3: Drop the old columns
            migrationBuilder.DropColumn(
                name: "ticket_price_amount",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "ticket_price_currency",
                schema: "events",
                table: "events");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ticket_price",
                schema: "events",
                table: "events");

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
    }
}
