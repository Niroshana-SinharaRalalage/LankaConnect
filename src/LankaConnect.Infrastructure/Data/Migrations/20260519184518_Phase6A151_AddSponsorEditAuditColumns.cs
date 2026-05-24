using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.151 — adds two audit columns to <c>events.sponsors</c> so we can
    /// answer "what did a human change recently?" distinct from system-driven
    /// lifecycle transitions (Stripe webhook, expiry, refund).
    ///
    /// Both columns are nullable:
    /// - <c>last_edited_at</c> (timestamptz)
    /// - <c>last_edited_by</c> (uuid)
    ///
    /// Set in tandem by <c>Sponsor.MarkEdited()</c> which is invoked by each of
    /// the four content-edit mutators (UpdateContactFields / UpdateName /
    /// UpdateAmount / UpdateItemDetails). Lifecycle methods (CompletePayment,
    /// MarkAsFailed, etc.) intentionally do NOT touch these columns — they only
    /// bump the generic <c>updated_at</c>.
    ///
    /// Hand-cleaned post-scaffold: removed the reference_data.reference_values
    /// created_at drift rows that EF Core scaffolding adds whenever a snapshot
    /// is rebuilt (the project's seeders use <c>DateTime.UtcNow</c> so successive
    /// scaffolds always produce noisy idempotent UpdateData calls). Follows the
    /// same convention used in Phase 6A.141 / 6A.143 / 6A.145 / 6A.146 / 6A.148.
    /// </summary>
    public partial class Phase6A151_AddSponsorEditAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "last_edited_at",
                schema: "events",
                table: "sponsors",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "last_edited_by",
                schema: "events",
                table: "sponsors",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_edited_at",
                schema: "events",
                table: "sponsors");

            migrationBuilder.DropColumn(
                name: "last_edited_by",
                schema: "events",
                table: "sponsors");
        }
    }
}
