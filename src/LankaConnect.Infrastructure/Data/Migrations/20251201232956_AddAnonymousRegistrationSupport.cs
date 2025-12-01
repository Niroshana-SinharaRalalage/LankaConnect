using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnonymousRegistrationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_registrations_event_user_unique",
                schema: "events",
                table: "registrations");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "events",
                table: "registrations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "attendee_info",
                schema: "events",
                table: "registrations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_registrations_user_xor_attendee",
                schema: "events",
                table: "registrations",
                sql: "(user_id IS NOT NULL AND attendee_info IS NULL) OR (user_id IS NULL AND attendee_info IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_registrations_user_xor_attendee",
                schema: "events",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "attendee_info",
                schema: "events",
                table: "registrations");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "events",
                table: "registrations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_registrations_event_user_unique",
                schema: "events",
                table: "registrations",
                columns: new[] { "EventId", "UserId" },
                unique: true);
        }
    }
}
