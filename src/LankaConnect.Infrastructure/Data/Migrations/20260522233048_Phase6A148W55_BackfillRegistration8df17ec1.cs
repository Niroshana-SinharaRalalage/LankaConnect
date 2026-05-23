using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.148.W5.5.D7: One-off backfill of operator-observed stuck registration
    /// <c>8df17ec1-42b5-41ed-808c-d66914e5699d</c> on event <c>ad8903c4</c>.
    ///
    /// Forensic context (verified via Stripe API on 2026-05-22):
    /// Refund request <c>6c367a21-d912-425c-859d-71f6916c947a</c> approved 4 lines totalling
    /// $202 of $292 requested. All 4 Stripe refunds settled successfully and metadata
    /// is intact. Workflow line items all reached <c>status=Refunded</c> with
    /// <c>stripe_refund_id</c> populated. RR rolled to <c>Completed</c>. The AddOnPurchase
    /// + Sponsor entities transitioned via their dedicated webhook handlers.
    ///
    /// However the registration row stayed <c>Status=Cancelled</c> because Bug 1 (W5.5
    /// router) mis-routed the ticket refund's <c>charge.refunded</c> webhook to the
    /// sponsor handler instead of the registration handler — so
    /// <c>RegistrationWebhookHandler.HandleChargeRefundedAsync</c>'s W5.D5 workflow
    /// branch never invoked <c>Registration.CompleteRefundFromCancelled</c>.
    ///
    /// W5.5.D4 prevents recurrence for new refunds. W5.5.D6.5 reconciler will heal new
    /// occurrences within the cron interval. This migration heals THE ONE specific
    /// row the operator UAT surfaced — applies the same DB updates
    /// <c>CompleteRefundFromCancelled</c> would have applied:
    ///   - Status='Refunded'
    ///   - PaymentStatus=3 (Refunded)
    ///   - StripeRefundId='re_3TZbwJLvfbr023L108hnAoyY' (the ticket refund)
    ///   - RefundCompletedAt=NOW() UTC
    ///   - UpdatedAt=NOW() UTC
    ///
    /// Per operator decision (Q2): quiet backfill — DO NOT raise domain events / send
    /// recovery emails. Raw SQL bypasses the EF change tracker so no events fire.
    /// Idempotent — guarded by current-state WHERE clause; re-run is a no-op.
    /// </summary>
    public partial class Phase6A148W55_BackfillRegistration8df17ec1 : Migration
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
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1519));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1585));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1477));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1548));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1561));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1656));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1534));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1504));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1631));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1618));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1600));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1643));

            // === W5.5.D7 Backfill: registration 8df17ec1 stuck Cancelled→Refunded ===
            // Quiet fix per operator Q2 — raw SQL bypasses domain event dispatch so no
            // recovery email fires. Idempotent: WHERE clause matches only the stuck state.
            migrationBuilder.Sql(@"
UPDATE events.registrations
SET ""Status"" = 'Refunded',
    ""PaymentStatus"" = 3,
    ""StripeRefundId"" = 're_3TZbwJLvfbr023L108hnAoyY',
    ""RefundCompletedAt"" = NOW() AT TIME ZONE 'UTC',
    ""UpdatedAt"" = NOW() AT TIME ZONE 'UTC'
WHERE ""Id"" = '8df17ec1-42b5-41ed-808c-d66914e5699d'::uuid
  AND ""Status"" = 'Cancelled'
  AND ""StripeRefundId"" IS NULL
  AND ""RefundCompletedAt"" IS NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5757));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5860));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5700));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5802));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5824));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5977));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5780));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5734));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5935));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5913));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5882));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 5, 21, 14, 10, 24, 194, DateTimeKind.Utc).AddTicks(5956));
        }
    }
}
