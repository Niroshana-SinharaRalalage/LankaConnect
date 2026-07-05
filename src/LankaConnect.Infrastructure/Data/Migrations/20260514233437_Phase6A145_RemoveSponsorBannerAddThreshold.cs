using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 6A.145 — rolls back the misunderstood Phase 6A.143 sponsor-banner design.
    /// (Phase 6A.144 was taken by the parallel auth-encouragement work.)
    ///
    /// Strips two deprecated JSONB keys from <c>events.sponsor_config</c>:
    ///   - <c>SponsorImageUrl</c>
    ///   - <c>SponsorImageBlobName</c>
    ///
    /// Per-sponsor images now live on the <c>Sponsor</c> aggregate (added in a
    /// follow-up migration). The C# value object regen also adds a new key
    /// <c>MinAmountForSponsorImage</c> (organizer-set threshold for who gets to upload
    /// a logo). No column-level changes — the JSONB column shape stays the same;
    /// only the keys inside it shift.
    ///
    /// Forward-only: no real data lost (only the Christmas Dinner Dance staging row
    /// had a SponsorImageUrl set; that row's image will simply disappear from the
    /// public page when the new model takes over).
    /// </summary>
    public partial class Phase6A145_RemoveSponsorBannerAddThreshold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the two deprecated JSONB keys from any rows that have them.
            // Uses jsonb subtraction (key-minus operator) which is a no-op when the
            // keys aren't present — safe on every row.
            migrationBuilder.Sql(
                @"UPDATE events.events
                  SET sponsor_config = sponsor_config - 'SponsorImageUrl' - 'SponsorImageBlobName'
                  WHERE sponsor_config IS NOT NULL
                    AND (sponsor_config ? 'SponsorImageUrl' OR sponsor_config ? 'SponsorImageBlobName');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Forward-only: re-creating the banner data on rollback is not possible
            // (blob URLs were generated at upload time and aren't recoverable). The
            // Down() is a no-op; a true revert would require restoring from the prior
            // model snapshot manually + reverting the C# VO changes.
        }
    }
}
