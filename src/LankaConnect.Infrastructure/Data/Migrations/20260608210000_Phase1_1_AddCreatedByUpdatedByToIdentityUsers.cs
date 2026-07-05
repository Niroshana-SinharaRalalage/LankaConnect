using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Wave4.9.2.1 Phase 1.1 (2026-06-08) — adds physical
    /// <c>created_by</c> and <c>updated_by</c> columns on
    /// <c>identity.users</c>, the only IAuditable table in the identity
    /// schema group. Purely additive (2 nullable text columns).
    ///
    /// Hand-authored .cs + .Designer.cs + AppDbContextModelSnapshot per
    /// architect ruling 2026-06-08. <c>dotnet ef migrations add</c> was
    /// attempted first and produced a 28KB migration with 7 DropTable +
    /// 7 CreateTable + 21 CreateIndex — pre-existing snapshot drift from
    /// operational changes that bypassed migrations. That drift is not
    /// in scope for Phase 1.1; only the User CreatedBy/UpdatedBy mapping
    /// belongs here.
    ///
    /// The 78 other IAuditable entities remain Ignore()'d at the
    /// AppDbContext level via <c>IgnoreAuditByActorPropertiesUntilPhase1</c>
    /// until their schema groups land in Phase 1.2-1.10.
    ///
    /// Columns are nullable because AppDbContext does not yet have an
    /// AuditableInterceptor wired (Phase 1.10 scope); User entities
    /// written through AppDbContext will persist NULL into these columns
    /// until that interceptor lands. UpdatedAt continues to advance via
    /// the existing manual <c>UpdatedAt = DateTime.UtcNow</c> in User
    /// domain mutators (e.g. <c>User.UpdateLocation</c>).
    /// </summary>
    public partial class Phase1_1_AddCreatedByUpdatedByToIdentityUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "created_by",
                schema: "identity",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by",
                schema: "identity",
                table: "users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "identity",
                table: "users");
        }
    }
}
