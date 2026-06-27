using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 8YA.1 — TBD-dates support.
    ///
    /// Drops NOT NULL on <c>events.events.StartDate</c> and <c>events.events.EndDate</c>
    /// so events can be created in the new <c>EventStatus.Planning</c> state without
    /// committing to start/end dates yet. <see cref="LankaConnect.Domain.Events.Event.SetDates"/>
    /// transitions a Planning event to Draft once both dates are filled in.
    ///
    /// Architect verdict 2026-05-08 (Q1=A): TBD events can be Published; the listing
    /// surfaces a "Date TBD" badge until dates are set.
    /// User answers Q2=A / Q3=A / Q4=A: registration blocked on TBD; featured/nearby
    /// queries exclude TBD; transition Planning → Draft is silent (no email).
    ///
    /// Down(): re-applies NOT NULL with a sentinel default. Safe ONLY while no
    /// Planning rows exist (rollback before Phase 3 ships UI). After Phase 3, ANY
    /// existing Planning row would fail the NOT NULL constraint on rollback.
    /// </summary>
    public partial class Phase8YA1_AllowNullEventDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                schema: "events",
                table: "events",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                schema: "events",
                table: "events",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                schema: "events",
                table: "events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                schema: "events",
                table: "events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
