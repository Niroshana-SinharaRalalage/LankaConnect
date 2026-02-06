using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A70_RemoveDuplicateMetros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.70: Remove old duplicate metro areas from initial seed
            // These are the 19 old metros with non-standard GUIDs that duplicate the proper state-based GUIDs

            // Delete old Ohio metros (6 duplicates)
            migrationBuilder.Sql(@"
                DELETE FROM events.metro_areas
                WHERE id IN (
                    '11111111-0000-0000-0000-000000000001', -- Old Cleveland
                    '11111111-0000-0000-0000-000000000002', -- Old Columbus
                    '11111111-0000-0000-0000-000000000003', -- Old Cincinnati
                    '11111111-0000-0000-0000-000000000004', -- Old Toledo
                    '11111111-0000-0000-0000-000000000005', -- Old Akron
                    '11111111-0000-0000-0000-000000000006'  -- Old Dayton
                );
            ");

            // Delete old New York metros (2 duplicates)
            migrationBuilder.Sql(@"
                DELETE FROM events.metro_areas
                WHERE id IN (
                    '22222222-0000-0000-0000-000000000001', -- Old NYC
                    '22222222-0000-0000-0000-000000000002'  -- Old Buffalo
                );
            ");

            // Delete old Pennsylvania metros (2 duplicates)
            migrationBuilder.Sql(@"
                DELETE FROM events.metro_areas
                WHERE id IN (
                    '33333333-0000-0000-0000-000000000001', -- Old Philadelphia
                    '33333333-0000-0000-0000-000000000002'  -- Old Pittsburgh
                );
            ");

            // Delete any other old pattern metros (44444444, 55555555, etc.)
            migrationBuilder.Sql(@"
                DELETE FROM events.metro_areas
                WHERE id::text LIKE '44444444-0000-0000-0000-%'
                   OR id::text LIKE '55555555-0000-0000-0000-%'
                   OR id::text LIKE '66666666-0000-0000-0000-%'
                   OR id::text LIKE '77777777-0000-0000-0000-%'
                   OR id::text LIKE '88888888-0000-0000-0000-%'
                   OR id::text LIKE '99999999-0000-0000-0000-%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No down migration - we don't want to restore duplicate data
            // The proper metros with state-based GUIDs will remain
        }
    }
}
