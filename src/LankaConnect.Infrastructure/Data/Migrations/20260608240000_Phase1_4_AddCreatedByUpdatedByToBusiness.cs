using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Wave4.9.2.4 Phase 1.4 (2026-06-08) — adds physical
    /// <c>created_by</c> and <c>updated_by</c> columns on the business
    /// schema group:
    /// <list type="bullet">
    ///   <item><c>business.businesses</c> (the Business aggregate)</item>
    ///   <item><c>business.services</c> (the Service entity)</item>
    ///   <item><c>business.reviews</c> (the Review entity)</item>
    /// </list>
    /// Purely additive (6 nullable text columns total).
    ///
    /// Same template as Phase 1.1-1.3.
    /// </summary>
    public partial class Phase1_4_AddCreatedByUpdatedByToBusiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "businesses", "services", "reviews" })
            {
                migrationBuilder.AddColumn<string>(name: "created_by", schema: "business", table: table, type: "text", nullable: true);
                migrationBuilder.AddColumn<string>(name: "updated_by", schema: "business", table: table, type: "text", nullable: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "businesses", "services", "reviews" })
            {
                migrationBuilder.DropColumn(name: "created_by", schema: "business", table: table);
                migrationBuilder.DropColumn(name: "updated_by", schema: "business", table: table);
            }
        }
    }
}
