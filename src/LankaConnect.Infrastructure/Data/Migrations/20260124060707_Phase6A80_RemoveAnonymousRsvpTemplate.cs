using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.80: Remove Anonymous RSVP Template - Reuse Member Templates
    ///
    /// Background:
    /// - Anonymous users previously had separate template (template-anonymous-rsvp-confirmation)
    /// - This template had parameter mismatches and was redundant
    /// - Solution: Reuse existing member template (template-free-event-registration-confirmation)
    /// - Benefits: Consistency, reduced duplication, better maintainability
    ///
    /// This migration:
    /// 1. Deletes the template-anonymous-rsvp-confirmation template
    /// 2. AnonymousRegistrationConfirmedEventHandler now uses FreeEventRegistration template
    /// 3. Updates template descriptions to note they support both member and anonymous users
    /// </summary>
    public partial class Phase6A80_RemoveAnonymousRsvpTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // =====================================================
            // PART 1: DELETE ANONYMOUS RSVP TEMPLATE
            // =====================================================

            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name = 'template-anonymous-rsvp-confirmation';");

            // =====================================================
            // PART 2: UPDATE MEMBER TEMPLATE DESCRIPTIONS
            // =====================================================

            // Update FreeEventRegistration description to note it supports anonymous users
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET description = 'Confirmation email for free event registration (member and anonymous users)',
                    updated_at = NOW()
                WHERE name = 'template-free-event-registration-confirmation';");

            // Update PaidEventRegistration description to note it supports anonymous users
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET description = 'Confirmation email for paid event registration with ticket (member and anonymous users)',
                    updated_at = NOW()
                WHERE name = 'template-paid-event-registration-confirmation-with-ticket';");

            // =====================================================
            // PART 3: AUTO-GENERATED REFERENCE DATA UPDATES
            // =====================================================
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 24, 6, 7, 2, 927, DateTimeKind.Utc).AddTicks(6472));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 24, 6, 7, 2, 927, DateTimeKind.Utc).AddTicks(6558));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 24, 6, 7, 2, 927, DateTimeKind.Utc).AddTicks(6394));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 24, 6, 7, 2, 927, DateTimeKind.Utc).AddTicks(6517));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 24, 6, 7, 2, 927, DateTimeKind.Utc).AddTicks(6537));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 24, 6, 7, 2, 927, DateTimeKind.Utc).AddTicks(6663));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 24, 6, 7, 2, 927, DateTimeKind.Utc).AddTicks(6494));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 24, 6, 7, 2, 927, DateTimeKind.Utc).AddTicks(6433));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 24, 6, 7, 2, 927, DateTimeKind.Utc).AddTicks(6615));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 24, 6, 7, 2, 927, DateTimeKind.Utc).AddTicks(6596));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 24, 6, 7, 2, 927, DateTimeKind.Utc).AddTicks(6577));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 24, 6, 7, 2, 927, DateTimeKind.Utc).AddTicks(6642));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // =====================================================
            // ROLLBACK: RE-CREATE ANONYMOUS RSVP TEMPLATE
            // =====================================================

            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", ""name"", ""description"", ""category"", ""type"", ""subject_template"", ""text_template"", ""html_template"", ""is_active"", ""created_at"")
                SELECT
                    gen_random_uuid(),
                    'template-anonymous-rsvp-confirmation',
                    'Confirmation email for anonymous event RSVP',
                    'Transactional',
                    'EventRegistration',
                    'Your RSVP Confirmation - {{EventTitle}}',
                    'Your RSVP for {{EventTitle}} has been confirmed! Event Details - Date: {{EventDate}}, Time: {{EventTime}}, Location: {{EventLocation}}. View event details: {{EventUrl}}. To modify or cancel your RSVP, use this link: {{ManageRsvpUrl}}',
                    '<!DOCTYPE html><html><head><meta charset=""utf-8""><title>RSVP Confirmed</title></head><body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;""><div style=""background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;""><h1 style=""color: white; margin: 0; font-size: 28px;"">RSVP Confirmed!</h1></div><div style=""background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px;""><p>Hello,</p><p>Your RSVP for <strong>{{EventTitle}}</strong> has been confirmed!</p><div style=""background: white; padding: 20px; border-radius: 5px; margin: 20px 0;""><h3 style=""margin-top: 0;"">Event Details</h3><p><strong>Date:</strong> {{EventDate}}</p><p><strong>Time:</strong> {{EventTime}}</p><p><strong>Location:</strong> {{EventLocation}}</p>{{#if GuestCount}}<p><strong>Guests:</strong> {{GuestCount}}</p>{{/if}}</div><div style=""text-align: center; margin: 30px 0;""><a href=""{{EventUrl}}"" style=""background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold;"">View Event Details</a></div><p style=""color: #666; font-size: 14px;"">To modify or cancel your RSVP, use this link: <a href=""{{ManageRsvpUrl}}"">Manage RSVP</a></p><hr style=""border: none; border-top: 1px solid #eee; margin: 20px 0;""><p style=""color: #999; font-size: 12px; text-align: center;"">&copy; {{Year}} LankaConnect. All rights reserved.</p></div></body></html>',
                    true,
                    NOW()
                WHERE NOT EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'template-anonymous-rsvp-confirmation');");

            // Rollback member template descriptions
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET description = 'Free event registration confirmation email',
                    updated_at = NOW()
                WHERE name = 'template-free-event-registration-confirmation';");

            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET description = 'Paid event registration confirmation email (sent after payment)',
                    updated_at = NOW()
                WHERE name = 'template-paid-event-registration-confirmation-with-ticket';");

            // =====================================================
            // ROLLBACK: AUTO-GENERATED REFERENCE DATA UPDATES
            // =====================================================
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 15, 25, 51, 424, DateTimeKind.Utc).AddTicks(7860));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 15, 25, 51, 424, DateTimeKind.Utc).AddTicks(7928));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 15, 25, 51, 424, DateTimeKind.Utc).AddTicks(7805));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 15, 25, 51, 424, DateTimeKind.Utc).AddTicks(7890));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 15, 25, 51, 424, DateTimeKind.Utc).AddTicks(7904));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 15, 25, 51, 424, DateTimeKind.Utc).AddTicks(8000));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 15, 25, 51, 424, DateTimeKind.Utc).AddTicks(7876));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 15, 25, 51, 424, DateTimeKind.Utc).AddTicks(7844));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 15, 25, 51, 424, DateTimeKind.Utc).AddTicks(7974));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 15, 25, 51, 424, DateTimeKind.Utc).AddTicks(7961));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 15, 25, 51, 424, DateTimeKind.Utc).AddTicks(7941));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 23, 15, 25, 51, 424, DateTimeKind.Utc).AddTicks(7986));
        }
    }
}
