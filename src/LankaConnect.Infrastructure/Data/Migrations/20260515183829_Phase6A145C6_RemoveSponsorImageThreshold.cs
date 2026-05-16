using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.145 Commit 6 — strips the deprecated <c>MinAmountForSponsorImage</c> key
    /// from any <c>events.sponsor_config</c> JSONB rows that still carry it.
    ///
    /// Per UAT feedback, the per-event image-upload threshold was removed: any sponsor
    /// can attach an image regardless of amount. The C# value object no longer carries
    /// the field; this migration cleans up the residual JSONB state on staging so future
    /// reads don't see orphan keys.
    /// </summary>
    public partial class Phase6A145C6_RemoveSponsorImageThreshold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE events.events
                  SET sponsor_config = sponsor_config - 'MinAmountForSponsorImage'
                  WHERE sponsor_config IS NOT NULL
                    AND sponsor_config ? 'MinAmountForSponsorImage';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Forward-only — the threshold value is unrecoverable once the key is dropped.
        }
    }
}
