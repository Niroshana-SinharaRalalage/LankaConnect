using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 7B: Photo Album Email Notification Fix
    ///
    /// Root cause: SendAlbumNotificationCommandHandler referenced
    /// "template-photo-album-published" but this template was never seeded.
    /// Every email send silently failed with "template not found" while the
    /// API returned HTTP 200 (false success via fire-and-forget pattern).
    ///
    /// This migration seeds the missing template inline (no File.ReadAllText —
    /// per project memory rule from Phase 6A.129b).
    ///
    /// Template variables: {UserName}, {EventTitle}, {AlbumName}, {AlbumUrl},
    ///                     {PhotoExpiryDays}, {Year}
    ///
    /// Also fixes: sign-up list participants now included as recipients
    /// (mirrors EventCancellationEmailJob Phase 6A.75 pattern).
    /// </summary>
    public partial class Phase7B_AddPhotoAlbumEmailTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─── Seed the missing photo-album-published email template ────────────
            // Uses PostgreSQL dollar-quoting ($html_template$) for the HTML body so
            // that HTML attribute quotes, apostrophes, and special characters never
            // need escaping. Idempotent: the WHERE NOT EXISTS guard prevents duplicates.
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", ""name"", ""description"", ""category"", ""type"", ""subject_template"", ""text_template"", ""html_template"", ""is_active"", ""created_at"")
                SELECT
                    gen_random_uuid(),
                    'template-photo-album-published',
                    'Photo album notification email to event attendees and sign-up list participants. Sent when organiser explicitly clicks Send Email on a Published album.',
                    'Transactional',
                    'AlbumNotification',
                    'Your photos from {{EventTitle}} are ready!',
                    E'Hi {{UserName}},\n\nThe photo album ''{{AlbumName}}'' from {{EventTitle}} is now available to view.\n\nView it here: {{AlbumUrl}}\n\nNote: photos and videos are available for {{PhotoExpiryDays}} days only, so download your favourites soon!\n\n\xA9 {{Year}} LankaEvents',
                    $html_template$<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"" />
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
<title>Photo Album Available</title>
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

<!-- Photo icon banner -->
<tr><td style=""background-color:#fef2f2;padding:16px 40px;text-align:center;border-bottom:1px solid #fee2e2;"">
<p style=""margin:0;font-size:32px;"">&#128247;</p>
<p style=""margin:8px 0 0;color:#991b1b;font-size:14px;font-weight:600;text-transform:uppercase;letter-spacing:1px;"">Photo Album Available</p>
</td></tr>

<!-- Body -->
<tr><td style=""padding:32px 40px;"">
<p style=""margin:0 0 16px;color:#374151;font-size:16px;line-height:1.6;"">Hi <strong>{{UserName}}</strong>,</p>
<p style=""margin:0 0 16px;color:#374151;font-size:16px;line-height:1.6;"">
  Great news! A photo album from <strong>{{EventTitle}}</strong> is now available for you to view and download.
</p>

<!-- Album card -->
<table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""background-color:#f9fafb;border:1px solid #e5e7eb;border-radius:8px;margin:24px 0;"">
<tr><td style=""padding:20px 24px;"">
<p style=""margin:0 0 4px;color:#6b7280;font-size:12px;text-transform:uppercase;letter-spacing:1px;font-weight:600;"">Album</p>
<p style=""margin:0 0 16px;color:#111827;font-size:18px;font-weight:700;"">{{AlbumName}}</p>
<p style=""margin:0 0 4px;color:#6b7280;font-size:12px;text-transform:uppercase;letter-spacing:1px;font-weight:600;"">Event</p>
<p style=""margin:0;color:#374151;font-size:15px;"">{{EventTitle}}</p>
</td></tr>
</table>

<!-- Expiry notice -->
<table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""background-color:#fffbeb;border:1px solid #fcd34d;border-radius:6px;margin:0 0 24px;"">
<tr><td style=""padding:12px 16px;"">
<p style=""margin:0;color:#92400e;font-size:13px;line-height:1.5;"">
  &#9200; <strong>Heads up:</strong> Photos and videos are available for <strong>{{PhotoExpiryDays}} days</strong> only. Download your favourites before they expire!
</p>
</td></tr>
</table>

<!-- CTA button -->
<table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin:0 auto;"">
<tr><td style=""border-radius:6px;background-color:#dc2626;"">
<a href=""{{AlbumUrl}}"" target=""_blank"" style=""display:inline-block;padding:14px 32px;color:#ffffff;font-size:16px;font-weight:700;text-decoration:none;border-radius:6px;"">
  View Album &#8594;
</a>
</td></tr>
</table>
</td></tr>

<!-- Footer -->
<tr><td style=""background-color:#f9fafb;padding:20px 40px;border-top:1px solid #e5e7eb;text-align:center;"">
<p style=""margin:0;color:#9ca3af;font-size:12px;line-height:1.6;"">
  You received this email because you attended or signed up for <strong>{{EventTitle}}</strong>.<br />
  &copy; {{Year}} LankaEvents. All rights reserved.
</p>
</td></tr>

</table>
</td></tr>
</table>
</body>
</html>$html_template$,
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates WHERE ""name"" = 'template-photo-album-published'
                );
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1275));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1395));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1234));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1358));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1371));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1460));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1343));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1260));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1436));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1424));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1408));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1448));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the seeded photo album email template
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE ""name"" = 'template-photo-album-published';
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 6, 3, 33, 34, 829, DateTimeKind.Utc).AddTicks(444));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 6, 3, 33, 34, 829, DateTimeKind.Utc).AddTicks(512));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 6, 3, 33, 34, 829, DateTimeKind.Utc).AddTicks(401));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 6, 3, 33, 34, 829, DateTimeKind.Utc).AddTicks(473));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 6, 3, 33, 34, 829, DateTimeKind.Utc).AddTicks(486));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 6, 3, 33, 34, 829, DateTimeKind.Utc).AddTicks(594));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 6, 3, 33, 34, 829, DateTimeKind.Utc).AddTicks(458));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 6, 3, 33, 34, 829, DateTimeKind.Utc).AddTicks(428));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 6, 3, 33, 34, 829, DateTimeKind.Utc).AddTicks(568));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 6, 3, 33, 34, 829, DateTimeKind.Utc).AddTicks(555));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 6, 3, 33, 34, 829, DateTimeKind.Utc).AddTicks(527));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 6, 3, 33, 34, 829, DateTimeKind.Utc).AddTicks(581));
        }
    }
}
