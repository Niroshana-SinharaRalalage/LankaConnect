using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 6A.143 — Snapshot-regen for the SponsorConfiguration value object's
    /// two new fields (SponsorImageUrl, SponsorImageBlobName).
    ///
    /// No DDL: the VO is serialized to JSONB on the events table — adding fields
    /// to the VO doesn't change the column structure. EF Core still requires this
    /// migration file so the model snapshot picks up the property additions; without
    /// it, future migrations error with model-snapshot drift.
    ///
    /// The Designer.cs sibling carries the actual model changes (annotations on
    /// the SponsorConfig owned entity). Reference-data created_at drift rows that
    /// EF scaffolded were hand-removed per the same convention used in Phase 6A.141
    /// and the sibling Phase 6A.143 add-on migration above.
    /// </summary>
    public partial class Phase6A143_AddSponsorImageToSponsorConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty — snapshot-regen only.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty — snapshot-regen only.
        }
    }
}
