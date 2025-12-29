using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A53Fix_UpdateEmailTemplateBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.53 Fix: Update email template branding to Phase 6A.34 brand gradient
            // This updates the existing template with correct maroon-orange-green gradient
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET
                    html_template = '<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Verify Your Email - LankaConnect</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #f4f4f4;"">
        <tr>
            <td align=""center"" style=""padding: 40px 20px;"">
                <table role=""presentation"" width=""650"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""max-width: 650px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);"">
                    <!-- Header with Brand Gradient (Phase 6A.34 branding: Maroon → Orange → Green) -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 0;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                <tr>
                                    <td align=""center"" style=""padding: 10px 0 5px 0; font-size: 14px; letter-spacing: 8px; color: rgba(255,255,255,0.25);"">
                                        ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦
                                    </td>
                                </tr>
                                <tr>
                                    <td align=""center"" style=""padding: 15px 25px; color: white;"">
                                        <h1 style=""margin: 0; font-size: 28px; font-weight: bold;"">Welcome to LankaConnect!</h1>
                                    </td>
                                </tr>
                                <tr>
                                    <td align=""center"" style=""padding: 5px 0 10px 0; font-size: 14px; letter-spacing: 8px; color: rgba(255,255,255,0.25);"">
                                        ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px 30px; background: #ffffff;"">
                            <p style=""font-size: 16px; margin: 0 0 20px 0; color: #333;"">Hi <span style=""color: #FF6600; font-weight: bold;"">{{UserName}}</span>,</p>
                            <p style=""margin: 0 0 25px 0; color: #555; line-height: 1.6;"">Thank you for joining LankaConnect, the Sri Lankan Community Hub! To complete your registration and activate your account, please verify your email address.</p>

                            <!-- Verification Button -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{{VerificationUrl}}"" style=""display: inline-block; background: linear-gradient(135deg, #8B1538 0%, #FF6600 100%); color: white; padding: 15px 40px; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px; box-shadow: 0 4px 6px rgba(139, 21, 56, 0.3);"">
                                            Verify Email Address
                                        </a>
                                    </td>
                                </tr>
                            </table>

                            <!-- Important Info Box -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0;"">
                                <tr>
                                    <td style=""background: #fff8f5; padding: 20px; border-left: 4px solid #FF6600; border-radius: 0 8px 8px 0;"">
                                        <p style=""margin: 0 0 10px 0; font-size: 14px; color: #666;""><strong style=""color: #8B1538;"">⏰ This link expires in {{ExpirationHours}} hours</strong></p>
                                        <p style=""margin: 0; font-size: 14px; color: #666;"">For your security, please verify your email soon.</p>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 20px 0 0 0; font-size: 14px; color: #999; line-height: 1.6;"">If you didn''t create this account, please ignore this email. No further action is required.</p>
                        </td>
                    </tr>
                    <!-- Footer with Brand Gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 0;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                <tr>
                                    <td align=""center"" style=""padding: 15px 0 10px 0; font-size: 12px; letter-spacing: 6px; color: rgba(255,255,255,0.2);"">
                                        ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦
                                    </td>
                                </tr>
                                <tr>
                                    <td align=""center"" style=""padding: 8px 0;"">
                                        <p style=""color: white; font-size: 20px; font-weight: bold; margin: 0;"">LankaConnect</p>
                                    </td>
                                </tr>
                                <tr>
                                    <td align=""center"" style=""padding: 5px 0;"">
                                        <p style=""color: rgba(255,255,255,0.9); font-size: 13px; margin: 0;"">Sri Lankan Community Hub</p>
                                    </td>
                                </tr>
                                <tr>
                                    <td align=""center"" style=""padding: 15px 0;"">
                                        <p style=""color: rgba(255,255,255,0.7); font-size: 11px; margin: 0;"">&copy; 2025 LankaConnect. All rights reserved.</p>
                                    </td>
                                </tr>
                                <tr>
                                    <td align=""center"" style=""padding: 0 0 15px 0; font-size: 12px; letter-spacing: 6px; color: rgba(255,255,255,0.2);"">
                                        ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦
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
</html>',
                    description = 'Email verification for new member signups with secure token - Phase 6A.53 (Updated branding)'
                WHERE name = 'member-email-verification';
            ");

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("1263a35a-381b-415f-b36c-d2c9b82767a2"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("18f71846-cfa7-4f20-a9d8-24667fda36f0"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("1da1b9eb-b439-4a6c-b08a-de487bbf16f9"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("26684714-c077-4e67-930b-575faecc6ded"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("380b0b07-2810-4497-b13c-9805aa5fa41a"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("3a09df11-5c25-474b-a2bf-ce90d6dd6c20"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4003c27a-72c8-4899-a27b-c361cfa25aba"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e83018d-39dd-41b1-9781-7ffe93b5ed5f"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("53ce4686-daed-49d1-bc59-c7a776f24149"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("579fca6a-65b2-424f-949b-93e14c213327"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("59fe3162-c1d4-40bc-a00d-0d670ad5634f"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("60655c1a-ba9f-4632-9f92-f2269b7e2541"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("7aab297e-44d1-4dc8-8126-616c9b7e8690"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("8ea00faf-36e5-4f87-ab2b-4d1ce65102c4"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("8f373b66-11f9-460c-9e96-655cbcd2cdcd"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("bc7a07cd-8d99-4869-b711-d1d003fe0185"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cb665c73-0949-4348-aa82-5033bd136316"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("d0afebb9-6db1-48b8-a251-87e3e008eab2"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("d1810c17-b408-4a1e-b85a-81f573a6b670"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("d6fd2868-c5cf-424b-9422-3b56e8241ffe"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("d9d84eda-15a3-4455-8048-a6a756a8422b"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("f2a13555-346f-46c3-8690-db61c1d01ff4"));

            migrationBuilder.InsertData(
                schema: "reference_data",
                table: "reference_values",
                columns: new[] { "id", "code", "created_at", "description", "display_order", "enum_type", "int_value", "is_active", "metadata", "name" },
                values: new object[,]
                {
                    { new Guid("041cabca-77a9-48c8-a596-a690a53765e4"), "EventOrganizer", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(6029), null, 3, "UserRole", 3, true, "{\"canManageUsers\":false,\"canCreateEvents\":true,\"canModerateContent\":false,\"isEventOrganizer\":true,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":false,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":10.00}", "Event Organizer" },
                    { new Guid("242ff499-dd47-4772-8cea-eed49985ce82"), "Educational", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(5827), null, 4, "EventCategory", 3, true, "{\"iconUrl\":\"\"}", "Educational" },
                    { new Guid("283b4d05-4c33-411b-a633-0b61df0ae3e4"), "Social", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(5833), null, 5, "EventCategory", 4, true, "{\"iconUrl\":\"\"}", "Social" },
                    { new Guid("3267a72b-f949-4698-aa48-e991a69c953d"), "BusinessOwner", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(6011), null, 2, "UserRole", 2, true, "{\"canManageUsers\":false,\"canCreateEvents\":false,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":true,\"canCreatePosts\":false,\"monthlySubscriptionPrice\":10.00}", "Business Owner" },
                    { new Guid("3ed58612-c110-4a9f-b007-87b116c20505"), "Religious", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(5789), null, 1, "EventCategory", 0, true, "{\"iconUrl\":\"\"}", "Religious" },
                    { new Guid("513fd6ef-5165-4ef5-bf7a-ef4445652641"), "EventOrganizerAndBusinessOwner", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(6058), null, 4, "UserRole", 4, true, "{\"canManageUsers\":false,\"canCreateEvents\":true,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":15.00}", "Event Organizer + Business Owner" },
                    { new Guid("5b0713c1-bbcd-4227-a157-74fe2f4c58a0"), "UnderReview", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(5945), null, 8, "EventStatus", 7, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Under Review" },
                    { new Guid("6d6f415b-ac5b-4b05-ba75-fbd9f7609769"), "Postponed", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(5920), null, 4, "EventStatus", 3, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Postponed" },
                    { new Guid("6db4f5f1-130d-47e1-8513-b3bf1346ae8b"), "Business", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(5838), null, 6, "EventCategory", 5, true, "{\"iconUrl\":\"\"}", "Business" },
                    { new Guid("72fb3506-6924-4a58-80fe-00a2c71bafee"), "Admin", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(6077), null, 5, "UserRole", 5, true, "{\"canManageUsers\":true,\"canCreateEvents\":true,\"canModerateContent\":true,\"isEventOrganizer\":false,\"isAdmin\":true,\"requiresSubscription\":false,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":0.00}", "Administrator" },
                    { new Guid("7b1a23f6-a174-4731-8f5f-9cd7fb588698"), "Cultural", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(5806), null, 2, "EventCategory", 1, true, "{\"iconUrl\":\"\"}", "Cultural" },
                    { new Guid("7ee19589-da22-4516-873b-9ad617a63d19"), "Completed", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(5932), null, 6, "EventStatus", 5, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Completed" },
                    { new Guid("82e1355f-f40c-4ba5-9a15-546fea7010ca"), "Cancelled", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(5926), null, 5, "EventStatus", 4, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Cancelled" },
                    { new Guid("880ab410-cf0b-430d-957b-babbefbe00dc"), "Archived", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(5938), null, 7, "EventStatus", 6, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Archived" },
                    { new Guid("916e2ee5-eb1c-486d-b647-7fe582a4c4ab"), "Draft", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(5897), null, 1, "EventStatus", 0, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Draft" },
                    { new Guid("936f0d2f-68bb-4f72-97e4-5d2fda33be9d"), "Entertainment", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(5849), null, 8, "EventCategory", 7, true, "{\"iconUrl\":\"\"}", "Entertainment" },
                    { new Guid("9b37539c-abd1-4b08-a751-f66b1b24d13b"), "Active", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(5910), null, 3, "EventStatus", 2, true, "{\"allowsRegistration\":true,\"isFinalState\":false}", "Active" },
                    { new Guid("ca6769b7-a380-4c02-9a3d-6c434778a1d8"), "Charity", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(5843), null, 7, "EventCategory", 6, true, "{\"iconUrl\":\"\"}", "Charity" },
                    { new Guid("d4d53f49-6f1f-445d-8c29-d9a952612302"), "AdminManager", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(6096), null, 6, "UserRole", 6, true, "{\"canManageUsers\":true,\"canCreateEvents\":true,\"canModerateContent\":true,\"isEventOrganizer\":false,\"isAdmin\":true,\"requiresSubscription\":false,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":0.00}", "Admin Manager" },
                    { new Guid("dc955b7a-d1c3-4f41-92d1-19018ec6e006"), "Community", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(5812), null, 3, "EventCategory", 2, true, "{\"iconUrl\":\"\"}", "Community" },
                    { new Guid("e5e5037f-496e-4b1d-b585-d1def568e5a6"), "GeneralUser", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(5991), null, 1, "UserRole", 1, true, "{\"canManageUsers\":false,\"canCreateEvents\":false,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":false,\"canCreateBusinessProfile\":false,\"canCreatePosts\":false,\"monthlySubscriptionPrice\":0.00}", "General User" },
                    { new Guid("ecb254b1-301b-496a-99ed-66ee9df827d3"), "Published", new DateTime(2025, 12, 29, 17, 3, 25, 14, DateTimeKind.Utc).AddTicks(5903), null, 2, "EventStatus", 1, true, "{\"allowsRegistration\":true,\"isFinalState\":false}", "Published" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("041cabca-77a9-48c8-a596-a690a53765e4"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("242ff499-dd47-4772-8cea-eed49985ce82"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("283b4d05-4c33-411b-a633-0b61df0ae3e4"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("3267a72b-f949-4698-aa48-e991a69c953d"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("3ed58612-c110-4a9f-b007-87b116c20505"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("513fd6ef-5165-4ef5-bf7a-ef4445652641"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("5b0713c1-bbcd-4227-a157-74fe2f4c58a0"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6d6f415b-ac5b-4b05-ba75-fbd9f7609769"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6db4f5f1-130d-47e1-8513-b3bf1346ae8b"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("72fb3506-6924-4a58-80fe-00a2c71bafee"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("7b1a23f6-a174-4731-8f5f-9cd7fb588698"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("7ee19589-da22-4516-873b-9ad617a63d19"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("82e1355f-f40c-4ba5-9a15-546fea7010ca"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("880ab410-cf0b-430d-957b-babbefbe00dc"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("916e2ee5-eb1c-486d-b647-7fe582a4c4ab"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("936f0d2f-68bb-4f72-97e4-5d2fda33be9d"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b37539c-abd1-4b08-a751-f66b1b24d13b"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("ca6769b7-a380-4c02-9a3d-6c434778a1d8"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("d4d53f49-6f1f-445d-8c29-d9a952612302"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("dc955b7a-d1c3-4f41-92d1-19018ec6e006"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e5e5037f-496e-4b1d-b585-d1def568e5a6"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("ecb254b1-301b-496a-99ed-66ee9df827d3"));

            migrationBuilder.InsertData(
                schema: "reference_data",
                table: "reference_values",
                columns: new[] { "id", "code", "created_at", "description", "display_order", "enum_type", "int_value", "is_active", "metadata", "name" },
                values: new object[,]
                {
                    { new Guid("1263a35a-381b-415f-b36c-d2c9b82767a2"), "GeneralUser", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7936), null, 1, "UserRole", 1, true, "{\"canManageUsers\":false,\"canCreateEvents\":false,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":false,\"canCreateBusinessProfile\":false,\"canCreatePosts\":false,\"monthlySubscriptionPrice\":0.00}", "General User" },
                    { new Guid("18f71846-cfa7-4f20-a9d8-24667fda36f0"), "Draft", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7865), null, 1, "EventStatus", 0, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Draft" },
                    { new Guid("1da1b9eb-b439-4a6c-b08a-de487bbf16f9"), "Active", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7875), null, 3, "EventStatus", 2, true, "{\"allowsRegistration\":true,\"isFinalState\":false}", "Active" },
                    { new Guid("26684714-c077-4e67-930b-575faecc6ded"), "Cancelled", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7886), null, 5, "EventStatus", 4, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Cancelled" },
                    { new Guid("380b0b07-2810-4497-b13c-9805aa5fa41a"), "BusinessOwner", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7950), null, 2, "UserRole", 2, true, "{\"canManageUsers\":false,\"canCreateEvents\":false,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":true,\"canCreatePosts\":false,\"monthlySubscriptionPrice\":10.00}", "Business Owner" },
                    { new Guid("3a09df11-5c25-474b-a2bf-ce90d6dd6c20"), "Published", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7870), null, 2, "EventStatus", 1, true, "{\"allowsRegistration\":true,\"isFinalState\":false}", "Published" },
                    { new Guid("4003c27a-72c8-4899-a27b-c361cfa25aba"), "Business", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7797), null, 6, "EventCategory", 5, true, "{\"iconUrl\":\"\"}", "Business" },
                    { new Guid("4e83018d-39dd-41b1-9781-7ffe93b5ed5f"), "Postponed", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7880), null, 4, "EventStatus", 3, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Postponed" },
                    { new Guid("53ce4686-daed-49d1-bc59-c7a776f24149"), "Cultural", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7777), null, 2, "EventCategory", 1, true, "{\"iconUrl\":\"\"}", "Cultural" },
                    { new Guid("579fca6a-65b2-424f-949b-93e14c213327"), "Archived", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7896), null, 7, "EventStatus", 6, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Archived" },
                    { new Guid("59fe3162-c1d4-40bc-a00d-0d670ad5634f"), "Admin", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7996), null, 5, "UserRole", 5, true, "{\"canManageUsers\":true,\"canCreateEvents\":true,\"canModerateContent\":true,\"isEventOrganizer\":false,\"isAdmin\":true,\"requiresSubscription\":false,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":0.00}", "Administrator" },
                    { new Guid("60655c1a-ba9f-4632-9f92-f2269b7e2541"), "Charity", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7811), null, 7, "EventCategory", 6, true, "{\"iconUrl\":\"\"}", "Charity" },
                    { new Guid("7aab297e-44d1-4dc8-8126-616c9b7e8690"), "Social", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7792), null, 5, "EventCategory", 4, true, "{\"iconUrl\":\"\"}", "Social" },
                    { new Guid("8ea00faf-36e5-4f87-ab2b-4d1ce65102c4"), "Completed", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7891), null, 6, "EventStatus", 5, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Completed" },
                    { new Guid("8f373b66-11f9-460c-9e96-655cbcd2cdcd"), "Community", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7783), null, 3, "EventCategory", 2, true, "{\"iconUrl\":\"\"}", "Community" },
                    { new Guid("bc7a07cd-8d99-4869-b711-d1d003fe0185"), "EventOrganizer", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7969), null, 3, "UserRole", 3, true, "{\"canManageUsers\":false,\"canCreateEvents\":true,\"canModerateContent\":false,\"isEventOrganizer\":true,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":false,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":10.00}", "Event Organizer" },
                    { new Guid("cb665c73-0949-4348-aa82-5033bd136316"), "UnderReview", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7904), null, 8, "EventStatus", 7, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Under Review" },
                    { new Guid("d0afebb9-6db1-48b8-a251-87e3e008eab2"), "Educational", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7788), null, 4, "EventCategory", 3, true, "{\"iconUrl\":\"\"}", "Educational" },
                    { new Guid("d1810c17-b408-4a1e-b85a-81f573a6b670"), "Religious", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7762), null, 1, "EventCategory", 0, true, "{\"iconUrl\":\"\"}", "Religious" },
                    { new Guid("d6fd2868-c5cf-424b-9422-3b56e8241ffe"), "EventOrganizerAndBusinessOwner", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7983), null, 4, "UserRole", 4, true, "{\"canManageUsers\":false,\"canCreateEvents\":true,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":15.00}", "Event Organizer + Business Owner" },
                    { new Guid("d9d84eda-15a3-4455-8048-a6a756a8422b"), "AdminManager", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(8009), null, 6, "UserRole", 6, true, "{\"canManageUsers\":true,\"canCreateEvents\":true,\"canModerateContent\":true,\"isEventOrganizer\":false,\"isAdmin\":true,\"requiresSubscription\":false,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":0.00}", "Admin Manager" },
                    { new Guid("f2a13555-346f-46c3-8690-db61c1d01ff4"), "Entertainment", new DateTime(2025, 12, 28, 20, 28, 40, 513, DateTimeKind.Utc).AddTicks(7825), null, 8, "EventCategory", 7, true, "{\"iconUrl\":\"\"}", "Entertainment" }
                });
        }
    }
}
