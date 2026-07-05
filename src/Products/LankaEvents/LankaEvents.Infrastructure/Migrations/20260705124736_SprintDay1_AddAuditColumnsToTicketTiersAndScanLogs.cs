using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Products.LankaEvents.Infrastructure.Migrations
{
    /// <summary>
    /// Sprint Day 1 fix (2026-07-05): add missing IAuditable audit columns
    /// (created_by, updated_by) to ticket_tiers and TicketScanLogs tables.
    ///
    /// Root cause: Wave 3.C W3C (2026-06-06) migrated TicketTier to
    /// BB.Domain.Entity&lt;Guid&gt; + IAuditable, gaining CreatedBy/UpdatedBy
    /// properties. Wave 4.9.2.10a physical-column sweep (2026-06-09) added
    /// snake_case created_by/updated_by columns to sibling tables but MISSED
    /// ticket_tiers. Same gap for TicketScanLog (Phase 6A.141, 2026-05-13).
    /// Symptom: Wave 9 smoke Events endpoints 500 with
    /// Postgres 42703 'column t.created_by does not exist' on
    /// EventRepository.GetByOrganizerAsync .Include(TicketTiers).
    ///
    /// EF scaffolder produced RenameColumn statements because the snapshot
    /// had 'CreatedBy' (PascalCase EF default) while TicketTierConfiguration
    /// now declares 'created_by' via HasColumnName. But the DB has NEITHER
    /// column, so RenameColumn would fail with 'column CreatedBy does not
    /// exist'. Hand-edited to raw SQL AddColumn IF NOT EXISTS - safe for both
    /// fresh DBs and any drift state.
    /// </summary>
    public partial class SprintDay1_AddAuditColumnsToTicketTiersAndScanLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE public.ticket_tiers
                    ADD COLUMN IF NOT EXISTS created_by text NULL,
                    ADD COLUMN IF NOT EXISTS updated_by text NULL;

                ALTER TABLE public.""TicketScanLogs""
                    ADD COLUMN IF NOT EXISTS created_by text NULL,
                    ADD COLUMN IF NOT EXISTS updated_by text NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE public.ticket_tiers
                    DROP COLUMN IF EXISTS updated_by,
                    DROP COLUMN IF EXISTS created_by;

                ALTER TABLE public.""TicketScanLogs""
                    DROP COLUMN IF EXISTS updated_by,
                    DROP COLUMN IF EXISTS created_by;
            ");
        }
    }
}
