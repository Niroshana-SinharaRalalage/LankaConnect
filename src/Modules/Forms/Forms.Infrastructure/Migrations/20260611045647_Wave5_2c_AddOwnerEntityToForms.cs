using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Modules.Forms.Infrastructure.Migrations
{
    /// <summary>
    /// Wave 5.2c (2026-06-11): introduces the polymorphic owner discriminator on
    /// <c>forms.event_forms</c>. CLR-side <c>Form.OwnerEntityId</c> +
    /// <c>OwnerEntityType</c> landed in Wave 5.2b with <c>b.Ignore()</c>
    /// hiding them from EF; this migration adds the physical columns + backfills
    /// from the legacy <c>event_id</c> column + constrains NOT NULL + creates
    /// the composite query index. Wave 5.2d drops <c>event_id</c> after a
    /// &gt;=48h staging soak.
    ///
    /// EF auto-scaffolded only AddColumn ops with placeholder zero-GUID and
    /// empty-string defaults; the placeholders would have corrupted every row.
    /// Per CLAUDE.md Section 5 ("hand-editing the JUST-generated migration to
    /// remove unintended sections IS allowed"), this migration is the
    /// human-review beat: rewrote Up()/Down() to follow the
    /// expand-then-constrain pattern with an explicit SQL backfill.
    /// </summary>
    public partial class Wave5_2c_AddOwnerEntityToForms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add owner_entity_id nullable initially (cannot static-default
            //    a per-row Guid; backfill below).
            migrationBuilder.AddColumn<Guid>(
                name: "owner_entity_id",
                schema: "forms",
                table: "event_forms",
                type: "uuid",
                nullable: true);

            // 2. Add owner_entity_type with defaultValue 'Event' so existing
            //    rows + future inserts without an explicit value fall into the
            //    only valid owner kind today. NOT NULL from inception.
            migrationBuilder.AddColumn<string>(
                name: "owner_entity_type",
                schema: "forms",
                table: "event_forms",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Event");

            // 3. Backfill owner_entity_id from event_id for every existing row
            //    (today every form is Event-owned).
            migrationBuilder.Sql(@"
                UPDATE forms.event_forms
                SET owner_entity_id = event_id
                WHERE owner_entity_id IS NULL;");

            // 4. Constrain owner_entity_id NOT NULL now that all rows have a value.
            migrationBuilder.AlterColumn<Guid>(
                name: "owner_entity_id",
                schema: "forms",
                table: "event_forms",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // 5. Composite index supporting the query pattern
            //    "find forms by (owner_entity_type, owner_entity_id)" which is
            //    what GetFormsForEvent etc. will use after Wave 5.2d removes
            //    the legacy event_id-based index.
            migrationBuilder.CreateIndex(
                name: "ix_event_forms_owner",
                schema: "forms",
                table: "event_forms",
                columns: new[] { "owner_entity_type", "owner_entity_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_event_forms_owner",
                schema: "forms",
                table: "event_forms");

            migrationBuilder.DropColumn(
                name: "owner_entity_type",
                schema: "forms",
                table: "event_forms");

            migrationBuilder.DropColumn(
                name: "owner_entity_id",
                schema: "forms",
                table: "event_forms");
        }
    }
}
