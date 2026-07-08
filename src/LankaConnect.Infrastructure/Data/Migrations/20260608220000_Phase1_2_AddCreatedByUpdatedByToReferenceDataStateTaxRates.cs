using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Wave4.9.2.2 Phase 1.2 (2026-06-08) — adds physical
    /// <c>created_by</c> and <c>updated_by</c> columns on
    /// <c>reference_data.state_tax_rates</c>, the only IAuditable table
    /// in the reference_data schema group. Purely additive (2 nullable
    /// text columns).
    ///
    /// Closes the loop on the entity that originally produced the
    /// Phase 3 "column s.CreatedBy does not exist" PostgreSQL 42703
    /// error at startup validation. With this migration applied + the
    /// new HasColumnName mapping in StateTaxRateConfiguration + the
    /// allowlist skip in IgnoreAuditByActorPropertiesUntilPhase1,
    /// the StateTaxRate SELECT path now reads the physical columns
    /// instead of attempting to map non-existent properties.
    ///
    /// Pattern matches Phase 1.1: hand-authored .cs/.Designer.cs +
    /// surgical 8-line snapshot edit. NO <c>dotnet ef migrations add</c>
    /// (would surface the pre-existing 28KB drift). Phase 1.3-1.10
    /// each follow this same template.
    /// </summary>
    public partial class Phase1_2_AddCreatedByUpdatedByToReferenceDataStateTaxRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "created_by",
                schema: "reference_data",
                table: "state_tax_rates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by",
                schema: "reference_data",
                table: "state_tax_rates",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "reference_data",
                table: "state_tax_rates");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "reference_data",
                table: "state_tax_rates");
        }
    }
}
