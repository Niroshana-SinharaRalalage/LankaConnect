using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase7DFix4_WhatsAppAutoDisableUnverified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "whatsapp_auto_disable_reason",
                schema: "communications",
                table: "user_whatsapp_preferences",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "whatsapp_auto_disabled_at",
                schema: "communications",
                table: "user_whatsapp_preferences",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "whatsapp_enabled_at",
                schema: "communications",
                table: "user_whatsapp_preferences",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(5890));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(6056));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(5772));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(5960));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(5992));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(6241));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(5924));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(5851));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(6175));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(6137));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(6090));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(6207));

            migrationBuilder.CreateIndex(
                name: "IX_UserWhatsAppPreferences_EnabledAt_EnabledUnverified",
                schema: "communications",
                table: "user_whatsapp_preferences",
                column: "whatsapp_enabled_at",
                filter: "whatsapp_enabled = true AND phone_verified = false");

            // Phase 7D Fix 4: seed template-whatsapp-auto-disabled email.
            // Inline SQL per MEMORY 6A.129b (no File.ReadAllText — fragile across
            // local dev / CI / Docker working directories). Dollar-quoting for the
            // HTML body so double quotes inside attributes never need escaping.
            // Idempotent: ON CONFLICT (name) DO NOTHING keeps re-runs safe.
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates
                    (""Id"", ""name"", ""description"", ""category"", ""type"",
                     ""subject_template"", ""text_template"", ""html_template"",
                     ""is_active"", ""created_at"")
                VALUES (
                    gen_random_uuid(),
                    'template-whatsapp-auto-disabled',
                    'Notifies a user that WhatsApp messaging was auto-disabled on their account because they enabled it but never completed phone verification within the grace window (Phase 7D Fix 4).',
                    'System',
                    'Transactional',
                    'We''ve turned off WhatsApp notifications for your account',
                    E'Hi {{UserName}},\n\nWe''ve turned off WhatsApp notifications on your LankaEvents account because we never received a verification code for the phone number you added ({{MaskedPhone}}) after you enabled it on {{EnabledAt}}.\n\nFor your security, we only send WhatsApp messages to verified numbers. After {{GracePeriodDays}} days without verification, we automatically turn the channel off.\n\nIf you''d still like WhatsApp updates, you can re-enable and verify your number here:\n{{ReEnableUrl}}\n\nYou''ll keep receiving the same notifications over email in the meantime — nothing else has changed on your account.\n\n\u00A9 {{Year}} LankaEvents',
                    $html_template$<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"" />
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
<title>WhatsApp notifications disabled</title>
</head>
<body style=""margin:0;padding:0;background-color:#f4f4f4;font-family:Arial,Helvetica,sans-serif;"">
<table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""background-color:#f4f4f4;"">
<tr><td align=""center"" style=""padding:20px 0;"">
<table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""600"" style=""max-width:600px;background-color:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 4px rgba(0,0,0,0.1);"">

<!-- Header -->
<tr><td style=""background:linear-gradient(135deg,#7c2d12,#dc2626);padding:30px 40px;text-align:center;"">
<h1 style=""margin:0;color:#ffffff;font-size:24px;font-weight:700;letter-spacing:1px;"">LankaEvents</h1>
<p style=""margin:4px 0 0;color:#fca5a5;font-size:13px;text-transform:uppercase;letter-spacing:2px;"">PLAN YOUR EVENT WITH EASE</p>
</td></tr>

<!-- Banner -->
<tr><td style=""background-color:#fef2f2;padding:16px 40px;text-align:center;border-bottom:1px solid #fee2e2;"">
<p style=""margin:0;font-size:32px;"">&#128241;</p>
<p style=""margin:8px 0 0;color:#991b1b;font-size:14px;font-weight:600;text-transform:uppercase;letter-spacing:1px;"">WhatsApp Notifications Disabled</p>
</td></tr>

<!-- Body -->
<tr><td style=""padding:32px 40px;"">
<p style=""margin:0 0 16px;color:#374151;font-size:16px;line-height:1.6;"">Hi <strong>{{UserName}}</strong>,</p>

<p style=""margin:0 0 16px;color:#374151;font-size:16px;line-height:1.6;"">
  We''ve turned off WhatsApp notifications on your LankaEvents account because we never received a verification code for the phone number you added after enabling the channel.
</p>

<!-- Details card -->
<table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""background-color:#f9fafb;border:1px solid #e5e7eb;border-radius:8px;margin:24px 0;"">
<tr><td style=""padding:20px 24px;"">
<p style=""margin:0 0 4px;color:#6b7280;font-size:12px;text-transform:uppercase;letter-spacing:1px;font-weight:600;"">Phone number</p>
<p style=""margin:0 0 16px;color:#111827;font-size:16px;font-weight:600;font-family:monospace;"">{{MaskedPhone}}</p>
<p style=""margin:0 0 4px;color:#6b7280;font-size:12px;text-transform:uppercase;letter-spacing:1px;font-weight:600;"">Enabled on</p>
<p style=""margin:0 0 16px;color:#111827;font-size:16px;font-weight:600;"">{{EnabledAt}}</p>
<p style=""margin:0 0 4px;color:#6b7280;font-size:12px;text-transform:uppercase;letter-spacing:1px;font-weight:600;"">Grace window</p>
<p style=""margin:0;color:#111827;font-size:16px;font-weight:600;"">{{GracePeriodDays}} days</p>
</td></tr>
</table>

<p style=""margin:0 0 16px;color:#374151;font-size:15px;line-height:1.6;"">
  For your security we only send WhatsApp messages to verified numbers. After the grace window elapses with no verification we automatically turn the channel off.
</p>

<!-- CTA -->
<table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""margin:28px 0;"">
<tr><td align=""center"">
<a href=""{{ReEnableUrl}}"" style=""display:inline-block;padding:14px 32px;background-color:#dc2626;color:#ffffff;text-decoration:none;border-radius:6px;font-size:16px;font-weight:600;"">
  Re-enable WhatsApp
</a>
</td></tr>
</table>

<p style=""margin:0 0 16px;color:#6b7280;font-size:14px;line-height:1.6;"">
  You''ll keep receiving the same notifications over email in the meantime &mdash; nothing else has changed on your account.
</p>

<p style=""margin:24px 0 0;color:#9ca3af;font-size:12px;line-height:1.5;word-break:break-all;"">
  Button not working? Copy this link into your browser:<br />
  {{ReEnableUrl}}
</p>
</td></tr>

<!-- Footer -->
<tr><td style=""background-color:#f9fafb;padding:20px 40px;text-align:center;border-top:1px solid #e5e7eb;"">
<p style=""margin:0;color:#6b7280;font-size:12px;line-height:1.5;"">
  &copy; {{Year}} LankaEvents. This is an automated message &mdash; please do not reply directly.
</p>
</td></tr>

</table>
</td></tr>
</table>
</body>
</html>$html_template$,
                    TRUE,
                    NOW()
                )
                ON CONFLICT (name) DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM communications.email_templates WHERE name = 'template-whatsapp-auto-disabled';");

            migrationBuilder.DropIndex(
                name: "IX_UserWhatsAppPreferences_EnabledAt_EnabledUnverified",
                schema: "communications",
                table: "user_whatsapp_preferences");

            migrationBuilder.DropColumn(
                name: "whatsapp_auto_disable_reason",
                schema: "communications",
                table: "user_whatsapp_preferences");

            migrationBuilder.DropColumn(
                name: "whatsapp_auto_disabled_at",
                schema: "communications",
                table: "user_whatsapp_preferences");

            migrationBuilder.DropColumn(
                name: "whatsapp_enabled_at",
                schema: "communications",
                table: "user_whatsapp_preferences");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 6, 18, 62, DateTimeKind.Utc).AddTicks(8508));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 6, 18, 62, DateTimeKind.Utc).AddTicks(8674));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 6, 18, 62, DateTimeKind.Utc).AddTicks(8355));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 6, 18, 62, DateTimeKind.Utc).AddTicks(8583));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 6, 18, 62, DateTimeKind.Utc).AddTicks(8616));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 6, 18, 62, DateTimeKind.Utc).AddTicks(8859));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 6, 18, 62, DateTimeKind.Utc).AddTicks(8544));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 6, 18, 62, DateTimeKind.Utc).AddTicks(8466));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 6, 18, 62, DateTimeKind.Utc).AddTicks(8795));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 6, 18, 62, DateTimeKind.Utc).AddTicks(8758));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 6, 18, 62, DateTimeKind.Utc).AddTicks(8710));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 6, 18, 62, DateTimeKind.Utc).AddTicks(8826));
        }
    }
}
