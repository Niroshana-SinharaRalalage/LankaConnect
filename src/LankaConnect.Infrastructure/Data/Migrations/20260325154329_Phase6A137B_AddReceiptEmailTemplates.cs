using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 6A.137B: Add 3 new receipt/confirmation email templates.
    ///
    /// Templates:
    /// 1. template-addon-purchase-receipt — Sent after add-on purchase payment completed
    /// 2. template-collection-receipt — Sent after collection contribution payment completed
    /// 3. template-sponsor-confirmation — Sent after monetary sponsor payment or item sponsor recorded
    ///
    /// Donation receipt template already exists (Phase 6A.130).
    /// </summary>
    public partial class Phase6A137B_AddReceiptEmailTemplates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep scaffolded reference_data timestamp updates
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 15, 43, 25, 970, DateTimeKind.Utc).AddTicks(2965));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 15, 43, 25, 970, DateTimeKind.Utc).AddTicks(3029));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 15, 43, 25, 970, DateTimeKind.Utc).AddTicks(2923));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 15, 43, 25, 970, DateTimeKind.Utc).AddTicks(2992));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 15, 43, 25, 970, DateTimeKind.Utc).AddTicks(3005));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 15, 43, 25, 970, DateTimeKind.Utc).AddTicks(3253));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 15, 43, 25, 970, DateTimeKind.Utc).AddTicks(2978));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 15, 43, 25, 970, DateTimeKind.Utc).AddTicks(2950));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 15, 43, 25, 970, DateTimeKind.Utc).AddTicks(3073));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 15, 43, 25, 970, DateTimeKind.Utc).AddTicks(3060));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 15, 43, 25, 970, DateTimeKind.Utc).AddTicks(3042));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 15, 43, 25, 970, DateTimeKind.Utc).AddTicks(3228));

            // ============================================================
            // Phase 6A.137B: INSERT 3 new email templates
            // ============================================================

            // 1. Add-On Purchase Receipt
            var addOnHtml = GetStandardTemplate(
                "Add-On Purchase Receipt",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Your add-on purchase for <strong>{{EventTitle}}</strong> has been confirmed! Here are your purchase details.</p>

                <!-- ADD-ON PURCHASE DETAILS -->
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #eff6ff; padding: 24px; border-radius: 12px; border-left: 4px solid #2563eb;"">
                            <p style=""margin: 0 0 4px 0; font-size: 13px; font-weight: 600; color: #1e40af; text-transform: uppercase; letter-spacing: 0.5px;"">Purchase Receipt</p>
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin-top: 12px;"">
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280; width: 140px;"">Add-On:</td>
                                    <td style=""padding: 6px 0; font-size: 16px; font-weight: 700; color: #111827;"">{{AddOnName}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Quantity:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827; font-weight: 500;"">{{Quantity}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Unit Price:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827;"">{{Currency}} ${{UnitPrice}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Total:</td>
                                    <td style=""padding: 6px 0; font-size: 20px; font-weight: 700; color: #111827;"">{{Currency}} ${{TotalAmount}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Event:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827; font-weight: 500;"">{{EventTitle}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Date:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827;"">{{PaymentDate}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Reference:</td>
                                    <td style=""padding: 6px 0; font-size: 13px; color: #6b7280; font-family: monospace;"">{{PaymentIntentId}}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>

                <!-- VIEW EVENT LINK -->
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td align=""center"">
                            <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #2563eb, #1d4ed8); color: #ffffff; padding: 14px 36px; border-radius: 8px; text-decoration: none; font-size: 15px; font-weight: 600;"">View Event Details</a>
                        </td>
                    </tr>
                </table>

                <p style=""margin: 20px 0 0 0; font-size: 14px; color: #6b7280;"">
                    If you have any questions about your purchase, please contact us at
                    <a href=""mailto:{{SupportEmail}}"" style=""color: #ea580c; text-decoration: none;"">{{SupportEmail}}</a>.
                </p>

                <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                    Best regards,<br>
                    <strong style=""color: #374151;"">The LankaConnect Team</strong>
                </p>");

            migrationBuilder.Sql($@"
                INSERT INTO communications.email_templates
                (
                    ""Id"",
                    ""name"",
                    ""description"",
                    ""subject_template"",
                    ""text_template"",
                    ""html_template"",
                    ""type"",
                    ""category"",
                    ""is_active"",
                    ""created_at""
                )
                SELECT
                    gen_random_uuid(),
                    'template-addon-purchase-receipt',
                    'Phase 6A.137B: Add-on purchase receipt email sent after successful payment',
                    'Your Add-On Purchase Confirmation - {{{{EventTitle}}}}',
                    'Hi {{{{UserName}}}},

Your add-on purchase for {{{{EventTitle}}}} has been confirmed!

PURCHASE RECEIPT
----------------
Add-On: {{{{AddOnName}}}}
Quantity: {{{{Quantity}}}}
Unit Price: {{{{Currency}}}} ${{{{UnitPrice}}}}
Total: {{{{Currency}}}} ${{{{TotalAmount}}}}
Event: {{{{EventTitle}}}}
Date: {{{{PaymentDate}}}}
Reference: {{{{PaymentIntentId}}}}

View event details: {{{{EventDetailsUrl}}}}

If you have any questions, contact us at {{{{SupportEmail}}}}.

Best regards,
The LankaConnect Team

© {{{{Year}}}} LankaConnect. All rights reserved.',
                    '{EscapeSql(addOnHtml)}',
                    'AddOnPurchaseReceipt',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-addon-purchase-receipt'
                );
            ");

            // 2. Collection Receipt
            var collectionHtml = GetStandardTemplate(
                "Collection Contribution Receipt",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Thank you for your contribution to <strong>{{EventTitle}}</strong>! Your generosity helps make this event a success.</p>

                <!-- COLLECTION DETAILS -->
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #f0fdf4; padding: 24px; border-radius: 12px; border-left: 4px solid #16a34a;"">
                            <p style=""margin: 0 0 4px 0; font-size: 13px; font-weight: 600; color: #166534; text-transform: uppercase; letter-spacing: 0.5px;"">Contribution Receipt</p>
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin-top: 12px;"">
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280; width: 140px;"">Amount:</td>
                                    <td style=""padding: 6px 0; font-size: 20px; font-weight: 700; color: #111827;"">{{Currency}} ${{ContributionAmount}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Event:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827; font-weight: 500;"">{{EventTitle}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Date:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827;"">{{PaymentDate}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Reference:</td>
                                    <td style=""padding: 6px 0; font-size: 13px; color: #6b7280; font-family: monospace;"">{{PaymentIntentId}}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>

                <!-- VIEW EVENT LINK -->
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td align=""center"">
                            <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #16a34a, #166534); color: #ffffff; padding: 14px 36px; border-radius: 8px; text-decoration: none; font-size: 15px; font-weight: 600;"">View Event Details</a>
                        </td>
                    </tr>
                </table>

                <p style=""margin: 20px 0 0 0; font-size: 14px; color: #6b7280;"">
                    If you have any questions about your contribution, please contact us at
                    <a href=""mailto:{{SupportEmail}}"" style=""color: #ea580c; text-decoration: none;"">{{SupportEmail}}</a>.
                </p>

                <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                    Best regards,<br>
                    <strong style=""color: #374151;"">The LankaConnect Team</strong>
                </p>");

            migrationBuilder.Sql($@"
                INSERT INTO communications.email_templates
                (
                    ""Id"",
                    ""name"",
                    ""description"",
                    ""subject_template"",
                    ""text_template"",
                    ""html_template"",
                    ""type"",
                    ""category"",
                    ""is_active"",
                    ""created_at""
                )
                SELECT
                    gen_random_uuid(),
                    'template-collection-receipt',
                    'Phase 6A.137B: Collection contribution receipt email sent after successful payment',
                    'Thank You for Your Contribution - {{{{EventTitle}}}}',
                    'Hi {{{{UserName}}}},

Thank you for your contribution to {{{{EventTitle}}}}!

CONTRIBUTION RECEIPT
--------------------
Amount: {{{{Currency}}}} ${{{{ContributionAmount}}}}
Event: {{{{EventTitle}}}}
Date: {{{{PaymentDate}}}}
Reference: {{{{PaymentIntentId}}}}

View event details: {{{{EventDetailsUrl}}}}

If you have any questions, contact us at {{{{SupportEmail}}}}.

Best regards,
The LankaConnect Team

© {{{{Year}}}} LankaConnect. All rights reserved.',
                    '{EscapeSql(collectionHtml)}',
                    'CollectionReceipt',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-collection-receipt'
                );
            ");

            // 3. Sponsor Confirmation (covers both monetary + item sponsors via Handlebars conditionals)
            var sponsorHtml = GetStandardTemplate(
                "Sponsorship Confirmation",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Thank you for sponsoring <strong>{{EventTitle}}</strong>! Your {{SponsorType}} support is greatly appreciated.</p>

                {{#if HasOrganization}}
                <p style=""margin: 0 0 20px 0; font-size: 14px; color: #6b7280;"">Organization: <strong>{{SponsorOrganization}}</strong></p>
                {{/if}}

                {{#if IsMonetarySponsor}}
                <!-- MONETARY SPONSOR DETAILS -->
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #fefce8; padding: 24px; border-radius: 12px; border-left: 4px solid #ca8a04;"">
                            <p style=""margin: 0 0 4px 0; font-size: 13px; font-weight: 600; color: #854d0e; text-transform: uppercase; letter-spacing: 0.5px;"">Sponsorship Receipt</p>
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin-top: 12px;"">
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280; width: 140px;"">Amount:</td>
                                    <td style=""padding: 6px 0; font-size: 20px; font-weight: 700; color: #111827;"">{{Currency}} ${{Amount}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Event:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827; font-weight: 500;"">{{EventTitle}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Date:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827;"">{{PaymentDate}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Reference:</td>
                                    <td style=""padding: 6px 0; font-size: 13px; color: #6b7280; font-family: monospace;"">{{PaymentIntentId}}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
                {{/if}}

                {{#if IsItemSponsor}}
                <!-- ITEM SPONSOR DETAILS -->
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #faf5ff; padding: 24px; border-radius: 12px; border-left: 4px solid #9333ea;"">
                            <p style=""margin: 0 0 4px 0; font-size: 13px; font-weight: 600; color: #6b21a8; text-transform: uppercase; letter-spacing: 0.5px;"">Item Sponsorship Acknowledgment</p>
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin-top: 12px;"">
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280; width: 140px;"">Item:</td>
                                    <td style=""padding: 6px 0; font-size: 16px; font-weight: 700; color: #111827;"">{{ItemName}}</td>
                                </tr>
                                {{#if HasItemDescription}}
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Description:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827;"">{{ItemDescription}}</td>
                                </tr>
                                {{/if}}
                                {{#if HasEstimatedValue}}
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Est. Value:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827; font-weight: 500;"">${{EstimatedValue}}</td>
                                </tr>
                                {{/if}}
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Event:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827; font-weight: 500;"">{{EventTitle}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Recorded:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827;"">{{RecordedAt}}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
                {{/if}}

                <!-- VIEW EVENT LINK -->
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td align=""center"">
                            <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #ca8a04, #854d0e); color: #ffffff; padding: 14px 36px; border-radius: 8px; text-decoration: none; font-size: 15px; font-weight: 600;"">View Event Details</a>
                        </td>
                    </tr>
                </table>

                <p style=""margin: 20px 0 0 0; font-size: 14px; color: #6b7280;"">
                    If you have any questions about your sponsorship, please contact us at
                    <a href=""mailto:{{SupportEmail}}"" style=""color: #ea580c; text-decoration: none;"">{{SupportEmail}}</a>.
                </p>

                <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                    Best regards,<br>
                    <strong style=""color: #374151;"">The LankaConnect Team</strong>
                </p>");

            migrationBuilder.Sql($@"
                INSERT INTO communications.email_templates
                (
                    ""Id"",
                    ""name"",
                    ""description"",
                    ""subject_template"",
                    ""text_template"",
                    ""html_template"",
                    ""type"",
                    ""category"",
                    ""is_active"",
                    ""created_at""
                )
                SELECT
                    gen_random_uuid(),
                    'template-sponsor-confirmation',
                    'Phase 6A.137B: Sponsor confirmation email for both monetary and item sponsors',
                    'Sponsorship Confirmation - {{{{EventTitle}}}}',
                    'Hi {{{{UserName}}}},

Thank you for sponsoring {{{{EventTitle}}}}! Your {{{{SponsorType}}}} support is greatly appreciated.

SPONSORSHIP DETAILS
-------------------
Type: {{{{SponsorType}}}}
Event: {{{{EventTitle}}}}

View event details: {{{{EventDetailsUrl}}}}

If you have any questions, contact us at {{{{SupportEmail}}}}.

Best regards,
The LankaConnect Team

© {{{{Year}}}} LankaConnect. All rights reserved.',
                    '{EscapeSql(sponsorHtml)}',
                    'SponsorConfirmation',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-sponsor-confirmation'
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the 3 new email templates
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name IN (
                    'template-addon-purchase-receipt',
                    'template-collection-receipt',
                    'template-sponsor-confirmation'
                );
            ");

            // Restore reference_data timestamps
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 3, 23, 5, 50, 54, 814, DateTimeKind.Utc).AddTicks(6676));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 3, 23, 5, 50, 54, 814, DateTimeKind.Utc).AddTicks(6751));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 3, 23, 5, 50, 54, 814, DateTimeKind.Utc).AddTicks(6633));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 3, 23, 5, 50, 54, 814, DateTimeKind.Utc).AddTicks(6710));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 3, 23, 5, 50, 54, 814, DateTimeKind.Utc).AddTicks(6725));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 3, 23, 5, 50, 54, 814, DateTimeKind.Utc).AddTicks(6831));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 3, 23, 5, 50, 54, 814, DateTimeKind.Utc).AddTicks(6695));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 3, 23, 5, 50, 54, 814, DateTimeKind.Utc).AddTicks(6659));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 3, 23, 5, 50, 54, 814, DateTimeKind.Utc).AddTicks(6802));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 3, 23, 5, 50, 54, 814, DateTimeKind.Utc).AddTicks(6787));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 3, 23, 5, 50, 54, 814, DateTimeKind.Utc).AddTicks(6770));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 3, 23, 5, 50, 54, 814, DateTimeKind.Utc).AddTicks(6816));
        }

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
                <!-- Main Container -->
                <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""width: 100%; max-width: 850px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);"">

                    <!-- Header Section - Gradient -->
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

                    <!-- BODY CONTENT -->
                    <tr>
                        <td style=""padding: 35px 40px;"">
                            {contentHtml}
                        </td>
                    </tr>

                    <!-- Footer Section - Gradient -->
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
