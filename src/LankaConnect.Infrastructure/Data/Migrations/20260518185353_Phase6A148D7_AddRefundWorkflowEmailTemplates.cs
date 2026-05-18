using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.148.D7: Adds three dedicated refund-lifecycle email templates so attendees
    /// can tell apart "we got your request" / "organizer decided" / "organizer declined".
    ///
    /// Templates:
    /// - template-refund-pending-review — fires at attendee-initiated request creation. Header: "Refund Request Received".
    /// - template-refund-decision       — fires after organizer approves (or on organizer-initiated creation). Header: "Refund Decision". Body lists per-line decisions.
    /// - template-refund-rejected       — fires when organizer rejects. Header: "Refund Request Declined". Customer-facing RejectionReason is a top-level field.
    ///
    /// Why not reuse template-refund-requested? Its header is hard-baked "Refund In Progress"
    /// (legacy 6A.92 vocabulary). Operator UAT (E1/E2 in MASTER_TODO_PHASE_6A_148) confirmed
    /// that header misled attendees into thinking Stripe was already running.
    ///
    /// Idempotency: all 3 INSERTs use WHERE NOT EXISTS so re-running the migration on an
    /// environment that already has the templates is a no-op.
    ///
    /// The UpdateData calls for reference_data.reference_values below are snapshot-drift
    /// bookkeeping inserted by `dotnet ef migrations add` — they update created_at timestamps
    /// for seeded reference values to match the current model snapshot. They are NOT part of
    /// this phase's intent but are kept as-is per project convention (see prior migrations
    /// Phase6A148, Phase6A148b — same pattern).
    /// </summary>
    public partial class Phase6A148D7_AddRefundWorkflowEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─────────────────────────────────────────────────────────────────
            // Snapshot-drift bookkeeping (auto-generated; kept per convention)
            // ─────────────────────────────────────────────────────────────────
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 5, 18, 18, 53, 51, 603, DateTimeKind.Utc).AddTicks(5219));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 5, 18, 18, 53, 51, 603, DateTimeKind.Utc).AddTicks(5279));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 5, 18, 18, 53, 51, 603, DateTimeKind.Utc).AddTicks(5174));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 5, 18, 18, 53, 51, 603, DateTimeKind.Utc).AddTicks(5250));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 5, 18, 18, 53, 51, 603, DateTimeKind.Utc).AddTicks(5265));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 5, 18, 18, 53, 51, 603, DateTimeKind.Utc).AddTicks(5361));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 5, 18, 18, 53, 51, 603, DateTimeKind.Utc).AddTicks(5235));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 5, 18, 18, 53, 51, 603, DateTimeKind.Utc).AddTicks(5202));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 5, 18, 18, 53, 51, 603, DateTimeKind.Utc).AddTicks(5333));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 5, 18, 18, 53, 51, 603, DateTimeKind.Utc).AddTicks(5319));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 5, 18, 18, 53, 51, 603, DateTimeKind.Utc).AddTicks(5305));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 5, 18, 18, 53, 51, 603, DateTimeKind.Utc).AddTicks(5346));

            // ─────────────────────────────────────────────────────────────────
            // 6A.148.D7 templates
            // ─────────────────────────────────────────────────────────────────

            // Template 1 — Refund Pending Review
            var pendingReviewHtml = GetStandardTemplate(
                "Refund Request Received",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">We've received your refund request for <strong>{{EventTitle}}</strong>. <strong>The request is now pending review by the event organizer</strong> — no money has moved yet. You'll receive another email as soon as the organizer makes a decision.</p>

                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #fef3c7; padding: 24px; border-radius: 12px; border-left: 4px solid #d97706;"">
                            <p style=""margin: 0 0 12px 0; font-size: 13px; font-weight: 600; color: #92400e; text-transform: uppercase; letter-spacing: 0.5px;"">Requested Items</p>
                            {{{LineItemsHtml}}}
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin-top: 16px;"">
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Total Requested:</td>
                                    <td style=""padding: 6px 0; font-size: 18px; font-weight: 700; color: #92400e; text-align: right;"">{{Currency}} ${{RequestedTotal}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Requested At:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827; text-align: right;"">{{RequestedAt}}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>

                {{#if HasRequesterReason}}
                <p style=""margin: 20px 0 8px 0; font-size: 14px; font-weight: 600; color: #374151;"">Your reason:</p>
                <p style=""margin: 0 0 24px 0; padding: 12px 16px; font-size: 14px; color: #4b5563; background: #f3f4f6; border-radius: 6px; font-style: italic;"">""{{RequesterReason}}""</p>
                {{/if}}

                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td align=""center"">
                            <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #d97706, #9f1239); color: #ffffff; padding: 14px 36px; border-radius: 8px; text-decoration: none; font-size: 15px; font-weight: 600;"">View Event Details</a>
                        </td>
                    </tr>
                </table>

                {{#if HasOrganizerContact}}
                <p style=""margin: 28px 0 12px 0; font-size: 13px; font-weight: 600; color: #6b7280; text-transform: uppercase; letter-spacing: 0.5px;"">{{OrganizerContactHeader}}</p>
                {{{OrganizerContactsHtml}}}
                {{/if}}

                <p style=""margin: 20px 0 0 0; font-size: 14px; color: #6b7280;"">
                    Questions? Contact <a href=""mailto:{{SupportEmail}}"" style=""color: #ea580c; text-decoration: none;"">{{SupportEmail}}</a>.
                </p>");

            migrationBuilder.Sql($@"
                INSERT INTO communications.email_templates
                (
                    ""Id"", ""name"", ""description"",
                    ""subject_template"", ""text_template"", ""html_template"",
                    ""type"", ""category"", ""is_active"", ""created_at""
                )
                SELECT
                    gen_random_uuid(),
                    'template-refund-pending-review',
                    'Phase 6A.148.D7: Sent at attendee-initiated refund request creation. Replaces the misleading template-refund-requested reuse (E1/E2 in 6A.148.c UAT).',
                    'Refund Request Received — Pending Organizer Review — {{{{EventTitle}}}}',
                    'Hi {{{{UserName}}}},

We have received your refund request for {{{{EventTitle}}}}.

YOUR REFUND REQUEST IS PENDING ORGANIZER REVIEW.
No money has moved yet. You will receive another email when the organizer makes a decision.

REQUESTED ITEMS
---------------
(See itemized list in the HTML version)
Total Requested: {{{{Currency}}}} ${{{{RequestedTotal}}}}
Requested At: {{{{RequestedAt}}}}

View event details: {{{{EventDetailsUrl}}}}

Questions? Contact {{{{SupportEmail}}}}.

Best regards,
The LankaConnect Team

© {{{{Year}}}} LankaConnect. All rights reserved.',
                    '{EscapeSql(pendingReviewHtml)}',
                    'RefundPendingReview',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-refund-pending-review'
                );
            ");

            // Template 2 — Refund Decision
            var decisionHtml = GetStandardTemplate(
                "Refund Decision",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                {{#if IsOrganizerInitiated}}
                <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">The organizer of <strong>{{EventTitle}}</strong> has initiated a refund on your behalf. Stripe is now processing the refund(s); funds typically land in your account within 5–10 business days.</p>
                {{else}}
                <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">The organizer of <strong>{{EventTitle}}</strong> has reviewed your refund request. See the per-item decision below. Approved lines are now being processed by Stripe; funds typically land in your account within 5–10 business days.</p>
                {{/if}}

                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #f0fdf4; padding: 24px; border-radius: 12px; border-left: 4px solid #15803d;"">
                            <p style=""margin: 0 0 12px 0; font-size: 13px; font-weight: 600; color: #166534; text-transform: uppercase; letter-spacing: 0.5px;"">Per-Item Decision</p>
                            {{{LineItemsHtml}}}
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin-top: 16px;"">
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Approved Total:</td>
                                    <td style=""padding: 6px 0; font-size: 20px; font-weight: 700; color: #15803d; text-align: right;"">{{Currency}} ${{ApprovedTotal}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Originally Requested:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827; text-align: right;"">{{Currency}} ${{RequestedTotal}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Decided At:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827; text-align: right;"">{{DecidedAt}}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>

                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td align=""center"">
                            <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #15803d, #166534); color: #ffffff; padding: 14px 36px; border-radius: 8px; text-decoration: none; font-size: 15px; font-weight: 600;"">View Event Details</a>
                        </td>
                    </tr>
                </table>

                {{#if HasOrganizerContact}}
                <p style=""margin: 28px 0 12px 0; font-size: 13px; font-weight: 600; color: #6b7280; text-transform: uppercase; letter-spacing: 0.5px;"">{{OrganizerContactHeader}}</p>
                {{{OrganizerContactsHtml}}}
                {{/if}}

                <p style=""margin: 20px 0 0 0; font-size: 14px; color: #6b7280;"">
                    Questions? Contact <a href=""mailto:{{SupportEmail}}"" style=""color: #ea580c; text-decoration: none;"">{{SupportEmail}}</a>.
                </p>");

            migrationBuilder.Sql($@"
                INSERT INTO communications.email_templates
                (
                    ""Id"", ""name"", ""description"",
                    ""subject_template"", ""text_template"", ""html_template"",
                    ""type"", ""category"", ""is_active"", ""created_at""
                )
                SELECT
                    gen_random_uuid(),
                    'template-refund-decision',
                    'Phase 6A.148.D7: Sent after organizer approves an attendee-initiated request, OR at organizer-initiated request creation. Replaces the per-Sponsor standalone confirmation as the authoritative decision email (E3).',
                    'Your Refund Decision — {{{{EventTitle}}}}',
                    'Hi {{{{UserName}}}},

The organizer of {{{{EventTitle}}}} has decided on your refund request.

PER-ITEM DECISION
-----------------
(See itemized list in the HTML version)
Approved Total: {{{{Currency}}}} ${{{{ApprovedTotal}}}}
Originally Requested: {{{{Currency}}}} ${{{{RequestedTotal}}}}
Decided At: {{{{DecidedAt}}}}

Approved lines are now being processed by Stripe; funds typically land in your account within 5–10 business days.

View event details: {{{{EventDetailsUrl}}}}

Questions? Contact {{{{SupportEmail}}}}.

Best regards,
The LankaConnect Team

© {{{{Year}}}} LankaConnect. All rights reserved.',
                    '{EscapeSql(decisionHtml)}',
                    'RefundDecision',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-refund-decision'
                );
            ");

            // Template 3 — Refund Rejected
            var rejectedHtml = GetStandardTemplate(
                "Refund Request Declined",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">The organizer of <strong>{{EventTitle}}</strong> has reviewed your refund request and has decided to decline it. No refund will be processed.</p>

                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #fef2f2; padding: 24px; border-radius: 12px; border-left: 4px solid #b91c1c;"">
                            <p style=""margin: 0 0 8px 0; font-size: 13px; font-weight: 600; color: #991b1b; text-transform: uppercase; letter-spacing: 0.5px;"">Reason for Decision</p>
                            <p style=""margin: 0; padding: 12px 16px; font-size: 15px; color: #111827; background: #ffffff; border-radius: 6px; border: 1px solid #fecaca;"">""{{RejectionReason}}""</p>
                        </td>
                    </tr>
                </table>

                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 24px 0;"">
                    <tr>
                        <td style=""padding: 18px; background: #f9fafb; border-radius: 8px;"">
                            <p style=""margin: 0 0 8px 0; font-size: 13px; font-weight: 600; color: #6b7280; text-transform: uppercase; letter-spacing: 0.5px;"">Requested Items</p>
                            {{{LineItemsHtml}}}
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin-top: 12px;"">
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Total Requested:</td>
                                    <td style=""padding: 6px 0; font-size: 16px; font-weight: 600; color: #111827; text-align: right;"">{{Currency}} ${{RequestedTotal}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Decided At:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827; text-align: right;"">{{RejectedAt}}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>

                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td align=""center"">
                            <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #b91c1c, #7f1d1d); color: #ffffff; padding: 14px 36px; border-radius: 8px; text-decoration: none; font-size: 15px; font-weight: 600;"">View Event Details</a>
                        </td>
                    </tr>
                </table>

                {{#if HasOrganizerContact}}
                <p style=""margin: 28px 0 12px 0; font-size: 13px; font-weight: 600; color: #6b7280; text-transform: uppercase; letter-spacing: 0.5px;"">{{OrganizerContactHeader}}</p>
                {{{OrganizerContactsHtml}}}
                <p style=""margin: 12px 0 0 0; font-size: 13px; color: #6b7280; font-style: italic;"">If you have questions about this decision, reach out to the organizer directly.</p>
                {{/if}}

                <p style=""margin: 20px 0 0 0; font-size: 14px; color: #6b7280;"">
                    Need help? Contact <a href=""mailto:{{SupportEmail}}"" style=""color: #ea580c; text-decoration: none;"">{{SupportEmail}}</a>.
                </p>");

            migrationBuilder.Sql($@"
                INSERT INTO communications.email_templates
                (
                    ""Id"", ""name"", ""description"",
                    ""subject_template"", ""text_template"", ""html_template"",
                    ""type"", ""category"", ""is_active"", ""created_at""
                )
                SELECT
                    gen_random_uuid(),
                    'template-refund-rejected',
                    'Phase 6A.148.D7: Sent when organizer rejects the refund request. Customer-facing RejectionReason is a top-level field — no body-stuffing (replaces 148.c approach).',
                    'Refund Request Declined — {{{{EventTitle}}}}',
                    'Hi {{{{UserName}}}},

The organizer of {{{{EventTitle}}}} has reviewed your refund request and has decided to decline it. No refund will be processed.

REASON FOR DECISION
-------------------
""{{{{RejectionReason}}}}""

REQUESTED ITEMS
---------------
(See itemized list in the HTML version)
Total Requested: {{{{Currency}}}} ${{{{RequestedTotal}}}}
Decided At: {{{{RejectedAt}}}}

View event details: {{{{EventDetailsUrl}}}}

Need help? Contact {{{{SupportEmail}}}}.

Best regards,
The LankaConnect Team

© {{{{Year}}}} LankaConnect. All rights reserved.',
                    '{EscapeSql(rejectedHtml)}',
                    'RefundRejected',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-refund-rejected'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the 3 templates first so the Down() leaves a clean slate
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name IN (
                    'template-refund-pending-review',
                    'template-refund-decision',
                    'template-refund-rejected'
                );
            ");

            // Revert snapshot-drift bookkeeping
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 5, 17, 15, 16, 58, 475, DateTimeKind.Utc).AddTicks(2434));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 5, 17, 15, 16, 58, 475, DateTimeKind.Utc).AddTicks(2547));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 5, 17, 15, 16, 58, 475, DateTimeKind.Utc).AddTicks(2347));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 5, 17, 15, 16, 58, 475, DateTimeKind.Utc).AddTicks(2478));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 5, 17, 15, 16, 58, 475, DateTimeKind.Utc).AddTicks(2505));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 5, 17, 15, 16, 58, 475, DateTimeKind.Utc).AddTicks(2695));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 5, 17, 15, 16, 58, 475, DateTimeKind.Utc).AddTicks(2453));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 5, 17, 15, 16, 58, 475, DateTimeKind.Utc).AddTicks(2412));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 5, 17, 15, 16, 58, 475, DateTimeKind.Utc).AddTicks(2628));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 5, 17, 15, 16, 58, 475, DateTimeKind.Utc).AddTicks(2603));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 5, 17, 15, 16, 58, 475, DateTimeKind.Utc).AddTicks(2571));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 5, 17, 15, 16, 58, 475, DateTimeKind.Utc).AddTicks(2652));
        }

        /// <summary>
        /// Standard 850px-wide email shell with gradient header + footer. Mirrors
        /// Phase6A137B2_AddRefundEmailTemplates.GetStandardTemplate so visual identity
        /// is consistent across all refund emails (header colour is the only differentiator).
        /// </summary>
        private string GetStandardTemplate(string headerTitle, string contentHtml)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>LankaConnect</title>
</head>
<body style=""font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333333; margin: 0; padding: 0; background-color: #f3f4f6;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #f3f4f6;"">
        <tr>
            <td align=""center"" style=""padding: 20px 10px;"">
                <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""width: 100%; max-width: 850px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""padding: 0;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                <tr>
                                    <td style=""background: linear-gradient(to right, #ea580c 0%, #9f1239 50%, #166534 100%); padding: 35px 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                                        <span style=""font-size: 24px; font-weight: 500; color: #ffffff;"">{headerTitle}</span>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 35px 40px;"">
                            {contentHtml}
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 0;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                <tr>
                                    <td style=""background: linear-gradient(to right, #ea580c 0%, #9f1239 50%, #166534 100%); padding: 28px 30px; text-align: center; border-radius: 0 0 12px 12px;"">
                                        <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                            <tr>
                                                <td style=""text-align: center; padding-bottom: 4px;"">
                                                    <span style=""font-size: 24px; font-weight: 400; color: #ffffff; letter-spacing: 0.5px;"">LankaConnect</span>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style=""text-align: center;"">
                                                    <span style=""font-size: 13px; font-weight: 400; color: #ffffff; opacity: 0.9;"">Sri Lankan Community Hub</span>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        private string EscapeSql(string input)
        {
            return input.Replace("'", "''");
        }
    }
}
