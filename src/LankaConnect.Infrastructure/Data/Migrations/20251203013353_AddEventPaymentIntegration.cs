using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventPaymentIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                schema: "events",
                table: "registrations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StripeCheckoutSessionId",
                schema: "events",
                table: "registrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentIntentId",
                schema: "events",
                table: "registrations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                schema: "events",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "StripeCheckoutSessionId",
                schema: "events",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "StripePaymentIntentId",
                schema: "events",
                table: "registrations");
        }
    }
}
