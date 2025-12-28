using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A57_SeedEventReminderTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0344df2b-671f-4746-bdd1-4da7f9de8b4d"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("053ab1e2-bef1-41e9-861b-6f17348515e9"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("3348075e-3540-42f4-85b5-dc628940a5a9"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("3779825f-bdab-4751-819f-4ff2175fe04f"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("3cc17d1b-2fd3-4109-bb7a-c8fe76f45be9"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("3ccab949-3cff-4cec-adef-117703ed4afd"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("3e66d4a2-2a78-4935-908d-05b3eb39ddcd"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4b7b550c-6887-4783-833a-72b7026c3ef6"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("5503fb87-17f1-47b7-af23-fd9e5d9b9618"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("5a679eb6-dee5-467f-81c1-f988ecea4f50"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("5bb58a1a-d39c-4f59-8ae1-eb3f1dff2b3b"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("69eb63f2-6dd6-49a7-ac93-3fc161a85b59"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6c84b73f-40ae-4517-926c-502f59cbb089"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("7c9df654-fd21-4a1a-abdd-e853a1b5f0d8"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("93886285-5265-4501-9b0a-2ef075e2708c"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("ac12aead-6531-47ac-ba2c-9c65d3ebbe44"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("b99309a3-d195-4a48-a2da-7182fe2ccdfb"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("baf10753-9e12-4454-b476-fd537241543d"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("d1e82f54-9c79-4b3e-a8d8-dd397ead5a65"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("db35acb4-5433-4003-a7f3-31ef1065c6cc"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("eb16ba17-265f-4dd9-bc9a-c6fd9a10f951"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("f1e7e1cb-f560-438c-ab76-f30722235605"));

            migrationBuilder.InsertData(
                schema: "reference_data",
                table: "reference_values",
                columns: new[] { "id", "code", "created_at", "description", "display_order", "enum_type", "int_value", "is_active", "metadata", "name" },
                values: new object[,]
                {
                    { new Guid("055b4d88-551a-4372-86ea-bbfcefc20d2d"), "AdminManager", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(1380), null, 6, "UserRole", 6, true, "{\"canManageUsers\":true,\"canCreateEvents\":true,\"canModerateContent\":true,\"isEventOrganizer\":false,\"isAdmin\":true,\"requiresSubscription\":false,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":0.00}", "Admin Manager" },
                    { new Guid("097e4119-23bc-429e-99aa-76f0dac85150"), "Community", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(734), null, 3, "EventCategory", 2, true, "{\"iconUrl\":\"\"}", "Community" },
                    { new Guid("0c5ee316-5799-476b-82b5-8aab911f1290"), "Charity", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(795), null, 7, "EventCategory", 6, true, "{\"iconUrl\":\"\"}", "Charity" },
                    { new Guid("0db9d52d-71e4-48fd-ac32-fb9ceae2f889"), "Social", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(765), null, 5, "EventCategory", 4, true, "{\"iconUrl\":\"\"}", "Social" },
                    { new Guid("16acac18-de4c-44fc-b376-bb5810a47074"), "Educational", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(750), null, 4, "EventCategory", 3, true, "{\"iconUrl\":\"\"}", "Educational" },
                    { new Guid("2354b163-3317-4d3d-823a-1d31c671ee91"), "EventOrganizer", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(1248), null, 3, "UserRole", 3, true, "{\"canManageUsers\":false,\"canCreateEvents\":true,\"canModerateContent\":false,\"isEventOrganizer\":true,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":false,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":10.00}", "Event Organizer" },
                    { new Guid("2a32e771-20ac-4f82-9239-d24b02cf8b3f"), "BusinessOwner", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(1206), null, 2, "UserRole", 2, true, "{\"canManageUsers\":false,\"canCreateEvents\":false,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":true,\"canCreatePosts\":false,\"monthlySubscriptionPrice\":10.00}", "Business Owner" },
                    { new Guid("3193e2eb-43ed-492c-81e8-e51682ebdd99"), "Religious", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(685), null, 1, "EventCategory", 0, true, "{\"iconUrl\":\"\"}", "Religious" },
                    { new Guid("510c0d28-aeea-4b80-a2d1-66dc5a8e4fcf"), "Active", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(960), null, 3, "EventStatus", 2, true, "{\"allowsRegistration\":true,\"isFinalState\":false}", "Active" },
                    { new Guid("51f4fa44-923f-49cd-98bf-e075bffc9e70"), "Completed", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(1015), null, 6, "EventStatus", 5, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Completed" },
                    { new Guid("5af6d9a7-c445-45f0-a204-df5c98714b21"), "EventOrganizerAndBusinessOwner", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(1289), null, 4, "UserRole", 4, true, "{\"canManageUsers\":false,\"canCreateEvents\":true,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":15.00}", "Event Organizer + Business Owner" },
                    { new Guid("65504056-133c-411e-b045-339c9bd8364e"), "Entertainment", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(827), null, 8, "EventCategory", 7, true, "{\"iconUrl\":\"\"}", "Entertainment" },
                    { new Guid("6569ac21-1056-48d2-bc3c-cc23614dbdea"), "Business", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(780), null, 6, "EventCategory", 5, true, "{\"iconUrl\":\"\"}", "Business" },
                    { new Guid("65afc775-d8b2-4522-b3b6-d7c6d92d9a1c"), "Cancelled", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(998), null, 5, "EventStatus", 4, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Cancelled" },
                    { new Guid("92a897c2-90f1-4341-826d-6a5849b5e6d2"), "Cultural", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(717), null, 2, "EventCategory", 1, true, "{\"iconUrl\":\"\"}", "Cultural" },
                    { new Guid("9850fbd8-de80-4b9a-9bc7-a222825df1ee"), "Archived", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(1043), null, 7, "EventStatus", 6, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Archived" },
                    { new Guid("b23af72e-20a1-4a91-a3b8-e97d775c8382"), "Draft", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(926), null, 1, "EventStatus", 0, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Draft" },
                    { new Guid("b6ab6299-5676-42b1-915d-ce776bd5313d"), "UnderReview", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(1069), null, 8, "EventStatus", 7, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Under Review" },
                    { new Guid("eb3309ca-653e-4876-a982-d6c2e846fe90"), "Admin", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(1329), null, 5, "UserRole", 5, true, "{\"canManageUsers\":true,\"canCreateEvents\":true,\"canModerateContent\":true,\"isEventOrganizer\":false,\"isAdmin\":true,\"requiresSubscription\":false,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":0.00}", "Administrator" },
                    { new Guid("f5eb978b-c746-476d-8389-54a8a6e77aa6"), "Published", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(944), null, 2, "EventStatus", 1, true, "{\"allowsRegistration\":true,\"isFinalState\":false}", "Published" },
                    { new Guid("fa2e2317-5ece-405c-b3b4-31caa21c7521"), "Postponed", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(982), null, 4, "EventStatus", 3, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Postponed" },
                    { new Guid("fdc28204-8ba0-4d20-b3bd-9112e46ab33d"), "GeneralUser", new DateTime(2025, 12, 28, 0, 39, 3, 568, DateTimeKind.Utc).AddTicks(1161), null, 1, "UserRole", 1, true, "{\"canManageUsers\":false,\"canCreateEvents\":false,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":false,\"canCreateBusinessProfile\":false,\"canCreatePosts\":false,\"monthlySubscriptionPrice\":0.00}", "General User" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("055b4d88-551a-4372-86ea-bbfcefc20d2d"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("097e4119-23bc-429e-99aa-76f0dac85150"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0c5ee316-5799-476b-82b5-8aab911f1290"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0db9d52d-71e4-48fd-ac32-fb9ceae2f889"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("16acac18-de4c-44fc-b376-bb5810a47074"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2354b163-3317-4d3d-823a-1d31c671ee91"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2a32e771-20ac-4f82-9239-d24b02cf8b3f"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("3193e2eb-43ed-492c-81e8-e51682ebdd99"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("510c0d28-aeea-4b80-a2d1-66dc5a8e4fcf"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("51f4fa44-923f-49cd-98bf-e075bffc9e70"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("5af6d9a7-c445-45f0-a204-df5c98714b21"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("65504056-133c-411e-b045-339c9bd8364e"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6569ac21-1056-48d2-bc3c-cc23614dbdea"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("65afc775-d8b2-4522-b3b6-d7c6d92d9a1c"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("92a897c2-90f1-4341-826d-6a5849b5e6d2"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9850fbd8-de80-4b9a-9bc7-a222825df1ee"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("b23af72e-20a1-4a91-a3b8-e97d775c8382"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("b6ab6299-5676-42b1-915d-ce776bd5313d"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("eb3309ca-653e-4876-a982-d6c2e846fe90"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("f5eb978b-c746-476d-8389-54a8a6e77aa6"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("fa2e2317-5ece-405c-b3b4-31caa21c7521"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("fdc28204-8ba0-4d20-b3bd-9112e46ab33d"));

            migrationBuilder.InsertData(
                schema: "reference_data",
                table: "reference_values",
                columns: new[] { "id", "code", "created_at", "description", "display_order", "enum_type", "int_value", "is_active", "metadata", "name" },
                values: new object[,]
                {
                    { new Guid("0344df2b-671f-4746-bdd1-4da7f9de8b4d"), "Religious", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(8702), null, 1, "EventCategory", 0, true, "{\"iconUrl\":\"\"}", "Religious" },
                    { new Guid("053ab1e2-bef1-41e9-861b-6f17348515e9"), "Archived", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9131), null, 7, "EventStatus", 6, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Archived" },
                    { new Guid("3348075e-3540-42f4-85b5-dc628940a5a9"), "Cancelled", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9099), null, 5, "EventStatus", 4, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Cancelled" },
                    { new Guid("3779825f-bdab-4751-819f-4ff2175fe04f"), "BusinessOwner", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9264), null, 2, "UserRole", 2, true, "{\"canManageUsers\":false,\"canCreateEvents\":false,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":true,\"canCreatePosts\":false,\"monthlySubscriptionPrice\":10.00}", "Business Owner" },
                    { new Guid("3cc17d1b-2fd3-4109-bb7a-c8fe76f45be9"), "UnderReview", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9144), null, 8, "EventStatus", 7, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Under Review" },
                    { new Guid("3ccab949-3cff-4cec-adef-117703ed4afd"), "Draft", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9026), null, 1, "EventStatus", 0, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Draft" },
                    { new Guid("3e66d4a2-2a78-4935-908d-05b3eb39ddcd"), "EventOrganizer", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9300), null, 3, "UserRole", 3, true, "{\"canManageUsers\":false,\"canCreateEvents\":true,\"canModerateContent\":false,\"isEventOrganizer\":true,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":false,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":10.00}", "Event Organizer" },
                    { new Guid("4b7b550c-6887-4783-833a-72b7026c3ef6"), "Postponed", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9085), null, 4, "EventStatus", 3, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Postponed" },
                    { new Guid("5503fb87-17f1-47b7-af23-fd9e5d9b9618"), "GeneralUser", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9227), null, 1, "UserRole", 1, true, "{\"canManageUsers\":false,\"canCreateEvents\":false,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":false,\"canCreateBusinessProfile\":false,\"canCreatePosts\":false,\"monthlySubscriptionPrice\":0.00}", "General User" },
                    { new Guid("5a679eb6-dee5-467f-81c1-f988ecea4f50"), "Active", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9070), null, 3, "EventStatus", 2, true, "{\"allowsRegistration\":true,\"isFinalState\":false}", "Active" },
                    { new Guid("5bb58a1a-d39c-4f59-8ae1-eb3f1dff2b3b"), "Entertainment", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(8910), null, 8, "EventCategory", 7, true, "{\"iconUrl\":\"\"}", "Entertainment" },
                    { new Guid("69eb63f2-6dd6-49a7-ac93-3fc161a85b59"), "Social", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(8860), null, 5, "EventCategory", 4, true, "{\"iconUrl\":\"\"}", "Social" },
                    { new Guid("6c84b73f-40ae-4517-926c-502f59cbb089"), "EventOrganizerAndBusinessOwner", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9356), null, 4, "UserRole", 4, true, "{\"canManageUsers\":false,\"canCreateEvents\":true,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":15.00}", "Event Organizer + Business Owner" },
                    { new Guid("7c9df654-fd21-4a1a-abdd-e853a1b5f0d8"), "Educational", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(8848), null, 4, "EventCategory", 3, true, "{\"iconUrl\":\"\"}", "Educational" },
                    { new Guid("93886285-5265-4501-9b0a-2ef075e2708c"), "Completed", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9118), null, 6, "EventStatus", 5, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Completed" },
                    { new Guid("ac12aead-6531-47ac-ba2c-9c65d3ebbe44"), "Published", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9041), null, 2, "EventStatus", 1, true, "{\"allowsRegistration\":true,\"isFinalState\":false}", "Published" },
                    { new Guid("b99309a3-d195-4a48-a2da-7182fe2ccdfb"), "AdminManager", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9446), null, 6, "UserRole", 6, true, "{\"canManageUsers\":true,\"canCreateEvents\":true,\"canModerateContent\":true,\"isEventOrganizer\":false,\"isAdmin\":true,\"requiresSubscription\":false,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":0.00}", "Admin Manager" },
                    { new Guid("baf10753-9e12-4454-b476-fd537241543d"), "Admin", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(9403), null, 5, "UserRole", 5, true, "{\"canManageUsers\":true,\"canCreateEvents\":true,\"canModerateContent\":true,\"isEventOrganizer\":false,\"isAdmin\":true,\"requiresSubscription\":false,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":0.00}", "Administrator" },
                    { new Guid("d1e82f54-9c79-4b3e-a8d8-dd397ead5a65"), "Business", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(8887), null, 6, "EventCategory", 5, true, "{\"iconUrl\":\"\"}", "Business" },
                    { new Guid("db35acb4-5433-4003-a7f3-31ef1065c6cc"), "Community", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(8834), null, 3, "EventCategory", 2, true, "{\"iconUrl\":\"\"}", "Community" },
                    { new Guid("eb16ba17-265f-4dd9-bc9a-c6fd9a10f951"), "Charity", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(8898), null, 7, "EventCategory", 6, true, "{\"iconUrl\":\"\"}", "Charity" },
                    { new Guid("f1e7e1cb-f560-438c-ab76-f30722235605"), "Cultural", new DateTime(2025, 12, 27, 23, 17, 49, 368, DateTimeKind.Utc).AddTicks(8816), null, 2, "EventCategory", 1, true, "{\"iconUrl\":\"\"}", "Cultural" }
                });
        }
    }
}
