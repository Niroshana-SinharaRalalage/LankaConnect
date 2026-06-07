using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 3 (Wave 4.9): purge 6 leaked Modules.* type entries from
    /// AppDbContextModelSnapshot.cs so subsequent <c>dotnet ef migrations add</c>
    /// calls produce purely additive output instead of mixing destructive
    /// <c>DropTable</c> SQL with the legitimate Phase 1 <c>AddColumn</c> drift.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SCHEMA-DESTRUCTIVE-APPROVED: snapshot-only correction. Per the architect's
    /// Wave 4.9 plan §4 Phase 3 "ghost migration" pattern: this migration's
    /// Up()/Down() bodies are intentionally no-op. The auto-regenerated
    /// AppDbContextModelSnapshot.cs (which EF emitted alongside this file) is
    /// the substantive change — it no longer references the 6 entity types that
    /// W4.0b (Notification), W4.2 (PhotoAlbum + AlbumPhoto), and W4.3
    /// (EventForm + FormQuestion + FormResponse + FormAnswer) moved out of the
    /// AppDbContext model and into their respective module-owned DbContexts.
    /// </para>
    /// <para>
    /// The destructive <c>DropTable</c> + <c>DropForeignKey</c> + <c>AddColumn</c>
    /// + <c>DropColumn</c> + <c>CreateTable</c> SQL that EF auto-generated has
    /// been intentionally removed because:
    /// </para>
    /// <list type="bullet">
    ///   <item>The 7 <c>DropTable</c> calls would have destroyed live data —
    ///   PhotoAlbum/AlbumPhoto/Form* tables still hold rows in <c>public</c>
    ///   schema today; Phase 2 will relocate them via <c>ALTER TABLE ... SET SCHEMA</c>,
    ///   preserving data. Dropping is the wrong move.</item>
    ///   <item>The 124 <c>AddColumn</c> + 124 <c>DropColumn</c> + 21 <c>CreateIndex</c>
    ///   + 24 <c>UpdateData</c> calls reflect the W3 IAuditable interface lift +
    ///   accumulated configuration drift. Per the architect's Q9 ruling: this
    ///   belongs in Phase 1 as 10 controlled per-schema migrations, NOT bundled
    ///   into this snapshot purge.</item>
    ///   <item>The migration row WILL still record in
    ///   <c>__EFMigrationsHistory</c> on apply — EF discovers it via the
    ///   <c>[Migration]</c> attribute on the .Designer.cs partial class.
    ///   Hand-editing the Up()/Down() body does NOT remove the migration from
    ///   EF's discovery (that would require deleting the .Designer.cs, which is
    ///   the MEMORY 6A.133 anti-pattern). We only touch the Up()/Down() bodies.</item>
    /// </list>
    /// <para>
    /// Post-deploy state: live tables UNCHANGED (no DDL runs); snapshot truth-
    /// aligned with current AppDbContext model. Subsequent Phase 1 migrations
    /// can now generate cleanly without re-emitting destructive Drop* against
    /// the moved entities.
    /// </para>
    /// </remarks>
    public partial class Phase3_PurgeLeakedModuleTypesFromSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty — see SCHEMA-DESTRUCTIVE-APPROVED header on the
            // class. The substantive change is the regenerated
            // AppDbContextModelSnapshot.cs alongside this file.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty — symmetric to Up(). Rolling back this
            // snapshot-only correction would have to regenerate the OLD
            // (leaked) snapshot manually; not supported.
        }
    }
}
