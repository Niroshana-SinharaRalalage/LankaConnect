using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.148.W5.D7: One-off backfill of the operator-observed stuck refund request
    /// <c>624b07c5-71b4-4b55-adcd-114a1a195e33</c> on registration
    /// <c>4d030697-315c-4c63-bcbe-051781d5414a</c> (event <c>ad8903c4</c>).
    ///
    /// What happened (verified against the Stripe API on 2026-05-20):
    /// During the original approve at 03:35:28 UTC, <c>RefundExecutionService.DispatchAsync</c>
    /// called Stripe successfully for all 4 lines — Stripe confirms full $234 refunded on
    /// charge <c>ch_3TZ0fsLvfbr023L11evZMfWd</c> (3 refunds) + $150 refunded on charge
    /// <c>ch_3TZ0kaLvfbr023L10rT3xEpE</c> (1 refund). Refund metadata is intact and includes
    /// <c>refund_request_id=624b07c5</c> + the correct <c>refund_type</c> per line.
    ///
    /// However the terminal <c>_uow.CommitAsync()</c> at
    /// <c>RefundExecutionService.cs:149</c> threw <c>DbUpdateConcurrencyException</c> (xmin
    /// clash with concurrent Cancel flow that flipped Registration to Cancelled), rolling
    /// back ALL in-memory <c>line.MarkRefunded()</c> + <c>BeginProcessing()</c> changes. The
    /// AddOn + 2 Sponsor entities transitioned via webhook (D14 routing works). The
    /// Registration stayed Cancelled with no StripeRefundId. The 4 workflow line items
    /// stayed status=Approved with no stripe_refund_id. <c>ApproveRefundRequestCommandHandler</c>
    /// swallowed the exception with the (vacuous) "reconciler will retry" comment — but the
    /// reconciler only scans <c>RegistrationStatus.RefundRequested</c>, not stuck-Approved.
    ///
    /// This migration mirrors Stripe-side reality into the DB. Pure data-fix — no
    /// out-of-band Stripe call (money has already moved). Idempotent via guarded WHERE
    /// clauses: re-running on already-reconciled rows is a no-op.
    ///
    /// W5.D1-D6 are the durable fix that PREVENTS this recurrence (Stripe IdempotencyKey
    /// + per-line fresh-scope commits + reconciler hardening). This migration only addresses
    /// the one already-stuck row.
    ///
    /// processed_at values come from the entity-level <c>refunded_at</c> timestamps (which
    /// were set by the webhooks at 03:35:36-39 UTC). Ticket has no entity transition, so its
    /// processed_at uses the same timestamp as the bundled sponsor (same dispatch loop, same
    /// PI, same charge).
    /// </summary>
    public partial class Phase6A148W5D7_BackfillRefund624b07c5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3057));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3175));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(2956));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3116));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3146));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3325));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3085));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3026));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3267));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3241));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3203));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 3, 39, 22, 947, DateTimeKind.Utc).AddTicks(3296));

            // === W5.D7 Backfill: refund request 624b07c5 ? Completed ===
            // Mirrors Stripe-side reality. All UPDATEs are guarded by current-state WHERE
            // clauses so re-running is a no-op.
            migrationBuilder.Sql(@"
-- Line 1 of 4: Ticket $100 ? re_3TZ0fsLvfbr023L11vrwuGGu (charge ch_3TZ0fsLvfbr023L11evZMfWd)
UPDATE events.refund_request_line_items
SET status = 4,
    stripe_refund_id = 're_3TZ0fsLvfbr023L11vrwuGGu',
    stripe_charge_id = 'ch_3TZ0fsLvfbr023L11evZMfWd',
    processed_at = TIMESTAMPTZ '2026-05-20 03:35:39.910000+00',
    updated_at = NOW() AT TIME ZONE 'UTC'
WHERE ""Id"" = '68d45a81-6988-443d-a5f8-a2d77b9eaa28'::uuid
  AND status = 1;

-- Line 2 of 4: AddOn $14 ? re_3TZ0fsLvfbr023L11qndOjfy
UPDATE events.refund_request_line_items
SET status = 4,
    stripe_refund_id = 're_3TZ0fsLvfbr023L11qndOjfy',
    stripe_charge_id = 'ch_3TZ0fsLvfbr023L11evZMfWd',
    processed_at = TIMESTAMPTZ '2026-05-20 03:35:36.514000+00',
    updated_at = NOW() AT TIME ZONE 'UTC'
WHERE ""Id"" = '23d3380c-8fc8-41f9-b830-3cc690e95a46'::uuid
  AND status = 1;

-- Line 3 of 4: Sponsor $150 standalone (own PI) ? re_3TZ0kaLvfbr023L103qS9QEo (charge ch_3TZ0kaLvfbr023L10rT3xEpE)
UPDATE events.refund_request_line_items
SET status = 4,
    stripe_refund_id = 're_3TZ0kaLvfbr023L103qS9QEo',
    stripe_charge_id = 'ch_3TZ0kaLvfbr023L10rT3xEpE',
    processed_at = TIMESTAMPTZ '2026-05-20 03:35:37.259000+00',
    updated_at = NOW() AT TIME ZONE 'UTC'
WHERE ""Id"" = '3d305342-5b32-4a0b-9cdc-0b97a042f882'::uuid
  AND status = 1;

-- Line 4 of 4: Sponsor $120 bundled-at-registration ? re_3TZ0fsLvfbr023L11BWCxaO0
UPDATE events.refund_request_line_items
SET status = 4,
    stripe_refund_id = 're_3TZ0fsLvfbr023L11BWCxaO0',
    stripe_charge_id = 'ch_3TZ0fsLvfbr023L11evZMfWd',
    processed_at = TIMESTAMPTZ '2026-05-20 03:35:39.910000+00',
    updated_at = NOW() AT TIME ZONE 'UTC'
WHERE ""Id"" = 'efab9e72-f3d9-439a-bdc7-78655e01d92d'::uuid
  AND status = 1;

-- Roll up: refund request 624b07c5 ? Completed (status=3)
UPDATE events.refund_requests
SET status = 3,
    completed_at = NOW() AT TIME ZONE 'UTC',
    updated_at = NOW() AT TIME ZONE 'UTC'
WHERE ""Id"" = '624b07c5-71b4-4b55-adcd-114a1a195e33'::uuid
  AND status = 1;

-- Registration 4d030697 ? Refunded. StripeRefundId carries the ticket refund as the
-- 'primary' Stripe id (matches today's single-refund-id schema on the registration row).
-- AddOnRefundAmount captures the $14 add-on refund total. Sponsor entities track their
-- own refund_at independently.
UPDATE events.registrations
SET ""Status"" = 'Refunded',
    ""PaymentStatus"" = 3,
    ""StripeRefundId"" = 're_3TZ0fsLvfbr023L11vrwuGGu',
    ""RefundCompletedAt"" = NOW() AT TIME ZONE 'UTC',
    ""AddOnRefundAmount"" = 14.00,
    ""UpdatedAt"" = NOW() AT TIME ZONE 'UTC'
WHERE ""Id"" = '4d030697-315c-4c63-bcbe-051781d5414a'::uuid
  AND ""Status"" = 'Cancelled'
  AND ""StripeRefundId"" IS NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The Up SQL backfill has NO programmatic inverse: rolling back would re-create
            // the divergence between DB and Stripe (worse state than the current Up state).
            // To revert, restore the affected 6 rows from a DB snapshot taken before Up ran.
            // The reference_values UpdateData calls below are the standard EF snapshot-drift
            // bookkeeping inverse and are safe to run either direction.

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(1032));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(1155));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(914));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(1093));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(1123));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(1462));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(1063));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(993));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(1244));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(1216));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(1186));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(1411));
        }
    }
}
