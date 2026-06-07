using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateEventsAndRegistrationsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create events schema
            migrationBuilder.EnsureSchema(
                name: "events");

            // Create Events table
            migrationBuilder.CreateTable(
                name: "events",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OrganizerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    has_location = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    address_street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    address_state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    address_zip_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    address_country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    coordinates_latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    coordinates_longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.Id);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "ix_events_start_date",
                schema: "events",
                table: "events",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "ix_events_organizer_id",
                schema: "events",
                table: "events",
                column: "OrganizerId");

            migrationBuilder.CreateIndex(
                name: "ix_events_status",
                schema: "events",
                table: "events",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_events_status_start_date",
                schema: "events",
                table: "events",
                columns: new[] { "Status", "StartDate" });

            // Create Registrations table
            migrationBuilder.CreateTable(
                name: "registrations",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    RegistrationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancellationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_registrations_events_EventId",
                        column: x => x.EventId,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes for Registrations
            migrationBuilder.CreateIndex(
                name: "IX_registrations_EventId",
                schema: "events",
                table: "registrations",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_registrations_UserId",
                schema: "events",
                table: "registrations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_registrations_EventId_UserId",
                schema: "events",
                table: "registrations",
                columns: new[] { "EventId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registrations",
                schema: "events");

            migrationBuilder.DropTable(
                name: "events",
                schema: "events");
        }
    }
}
