using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A51_AddMissingSignupCommitmentEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.51: Add missing signup commitment email templates

            // Template 1: Signup Commitment Updated
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
                VALUES
                (
                    gen_random_uuid(),
                    'signup-commitment-updated',
                    'Confirmation email when user updates their commitment quantity',
                    'Commitment Updated for {{EventTitle}}',
                    'Hi {{UserName}},

Your commitment for {{EventTitle}} has been updated!

UPDATED COMMITMENT
------------------
Item: {{ItemDescription}}
Previous Quantity: {{OldQuantity}}
New Quantity: {{NewQuantity}}
Event Date: {{EventDate}}
Location: {{EventLocation}}

You can view or modify your commitment in your event dashboard.

Thank you for your continued support!

(c) 2025 LankaConnect
Questions? Reply to this email or visit our support page.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #3b82f6; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }
        .content { padding: 20px; background: #f9fafb; }
        .commitment-box { background: white; padding: 15px; margin: 15px 0; border-left: 4px solid #3b82f6; border-radius: 4px; }
        .quantity-change { background: #dbeafe; padding: 10px; margin: 10px 0; border-radius: 4px; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Commitment Updated!</h1>
        </div>
        <div class=""content"">
            <p>Hi {{UserName}},</p>
            <p>Your commitment for <strong>{{EventTitle}}</strong> has been updated!</p>
            <div class=""commitment-box"">
                <h3>Updated Commitment</h3>
                <p><strong>Item:</strong> {{ItemDescription}}</p>
                <div class=""quantity-change"">
                    <p><strong>Previous Quantity:</strong> {{OldQuantity}}</p>
                    <p><strong>New Quantity:</strong> {{NewQuantity}}</p>
                </div>
                <p><strong>Event Date:</strong> {{EventDate}}</p>
                <p><strong>Location:</strong> {{EventLocation}}</p>
            </div>
            <p>You can view or modify your commitment in your event dashboard.</p>
            <p>Thank you for your continued support!</p>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'SignupCommitmentUpdate',
                    'Notification',
                    true,
                    NOW()
                );
            ");

            // Template 2: Signup Commitment Cancelled
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
                VALUES
                (
                    gen_random_uuid(),
                    'signup-commitment-cancelled',
                    'Confirmation email when user cancels their commitment',
                    'Commitment Cancelled for {{EventTitle}}',
                    'Hi {{UserName}},

Your commitment for {{EventTitle}} has been cancelled.

CANCELLED COMMITMENT
--------------------
Item: {{ItemDescription}}
Quantity: {{Quantity}}
Event Date: {{EventDate}}
Location: {{EventLocation}}

If you cancelled by mistake, you can commit again if spots are still available.

Thank you for considering helping out!

(c) 2025 LankaConnect
Questions? Reply to this email or visit our support page.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #ef4444; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }
        .content { padding: 20px; background: #f9fafb; }
        .commitment-box { background: white; padding: 15px; margin: 15px 0; border-left: 4px solid #ef4444; border-radius: 4px; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Commitment Cancelled</h1>
        </div>
        <div class=""content"">
            <p>Hi {{UserName}},</p>
            <p>Your commitment for <strong>{{EventTitle}}</strong> has been cancelled.</p>
            <div class=""commitment-box"">
                <h3>Cancelled Commitment</h3>
                <p><strong>Item:</strong> {{ItemDescription}}</p>
                <p><strong>Quantity:</strong> {{Quantity}}</p>
                <p><strong>Event Date:</strong> {{EventDate}}</p>
                <p><strong>Location:</strong> {{EventLocation}}</p>
            </div>
            <p>If you cancelled by mistake, you can commit again if spots are still available.</p>
            <p>Thank you for considering helping out!</p>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'SignupCommitmentCancellation',
                    'Notification',
                    true,
                    NOW()
                );
            ");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.51: Remove email templates
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name IN (
                    'signup-commitment-updated',
                    'signup-commitment-cancelled'
                );
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 22, 53, 41, 886, DateTimeKind.Utc).AddTicks(1402));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 22, 53, 41, 886, DateTimeKind.Utc).AddTicks(1468));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 22, 53, 41, 886, DateTimeKind.Utc).AddTicks(1339));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 22, 53, 41, 886, DateTimeKind.Utc).AddTicks(1436));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 22, 53, 41, 886, DateTimeKind.Utc).AddTicks(1452));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 22, 53, 41, 886, DateTimeKind.Utc).AddTicks(1544));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 22, 53, 41, 886, DateTimeKind.Utc).AddTicks(1419));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 22, 53, 41, 886, DateTimeKind.Utc).AddTicks(1383));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 22, 53, 41, 886, DateTimeKind.Utc).AddTicks(1514));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 22, 53, 41, 886, DateTimeKind.Utc).AddTicks(1500));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 22, 53, 41, 886, DateTimeKind.Utc).AddTicks(1485));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 18, 22, 53, 41, 886, DateTimeKind.Utc).AddTicks(1529));
        }
    }
}
