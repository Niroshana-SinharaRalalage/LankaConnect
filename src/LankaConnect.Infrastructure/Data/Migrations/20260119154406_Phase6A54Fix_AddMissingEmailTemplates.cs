using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A54Fix_AddMissingEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.54 Fix: Add missing email template (signup-commitment-confirmation) from original Phase6A54 migration
            // This template was never applied to staging because the original migration was missing its Designer file

            // Template: Signup Commitment Confirmation (idempotent - skip if exists)
            migrationBuilder.Sql(@"
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
                    'signup-commitment-confirmation',
                    'Confirmation email when user commits to bringing an item to an event',
                    'Item Commitment Confirmed for {{EventTitle}}',
                    'Hi {{UserName}},

Thank you for committing to bring items to {{EventTitle}}!

COMMITMENT DETAILS
------------------
Item: {{ItemDescription}}
Quantity: {{Quantity}}
Event Date: {{EventDateTime}}
Location: {{EventLocation}}

PICKUP/DELIVERY
---------------
{{PickupInstructions}}

You can view or modify your commitment in your event dashboard.

We appreciate your contribution!

(c) 2025 LankaConnect
Questions? Reply to this email or visit our support page.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #10b981; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }
        .content { padding: 20px; background: #f9fafb; }
        .commitment-box { background: white; padding: 15px; margin: 15px 0; border-left: 4px solid #10b981; border-radius: 4px; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Commitment Confirmed!</h1>
        </div>
        <div class=""content"">
            <p>Hi {{UserName}},</p>
            <p>Thank you for committing to bring items to <strong>{{EventTitle}}</strong>!</p>
            <div class=""commitment-box"">
                <h3>Commitment Details</h3>
                <p><strong>Item:</strong> {{ItemDescription}}</p>
                <p><strong>Quantity:</strong> {{Quantity}}</p>
                <p><strong>Event Date:</strong> {{EventDateTime}}</p>
                <p><strong>Location:</strong> {{EventLocation}}</p>
            </div>
            <div class=""commitment-box"">
                <h3>Pickup/Delivery</h3>
                <p>{{PickupInstructions}}</p>
            </div>
            <p>You can view or modify your commitment in your event dashboard.</p>
            <p>We appreciate your contribution!</p>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'SignupCommitmentConfirmation',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'signup-commitment-confirmation'
                );
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 15, 44, 2, 686, DateTimeKind.Utc).AddTicks(2160));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 15, 44, 2, 686, DateTimeKind.Utc).AddTicks(2285));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 15, 44, 2, 686, DateTimeKind.Utc).AddTicks(2116));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 15, 44, 2, 686, DateTimeKind.Utc).AddTicks(2190));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 15, 44, 2, 686, DateTimeKind.Utc).AddTicks(2204));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 15, 44, 2, 686, DateTimeKind.Utc).AddTicks(2361));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 15, 44, 2, 686, DateTimeKind.Utc).AddTicks(2175));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 15, 44, 2, 686, DateTimeKind.Utc).AddTicks(2142));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 15, 44, 2, 686, DateTimeKind.Utc).AddTicks(2334));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 15, 44, 2, 686, DateTimeKind.Utc).AddTicks(2320));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 15, 44, 2, 686, DateTimeKind.Utc).AddTicks(2300));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 19, 15, 44, 2, 686, DateTimeKind.Utc).AddTicks(2348));
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
                value: new DateTime(2026, 1, 18, 23, 54, 7, 723, DateTimeKind.Utc).AddTicks(2461));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 23, 54, 7, 723, DateTimeKind.Utc).AddTicks(2587));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 23, 54, 7, 723, DateTimeKind.Utc).AddTicks(2383));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 23, 54, 7, 723, DateTimeKind.Utc).AddTicks(2519));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 23, 54, 7, 723, DateTimeKind.Utc).AddTicks(2546));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 23, 54, 7, 723, DateTimeKind.Utc).AddTicks(2826));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 23, 54, 7, 723, DateTimeKind.Utc).AddTicks(2490));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 23, 54, 7, 723, DateTimeKind.Utc).AddTicks(2431));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 23, 54, 7, 723, DateTimeKind.Utc).AddTicks(2680));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 23, 54, 7, 723, DateTimeKind.Utc).AddTicks(2651));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 23, 54, 7, 723, DateTimeKind.Utc).AddTicks(2617));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 23, 54, 7, 723, DateTimeKind.Utc).AddTicks(2706));
        }
    }
}
