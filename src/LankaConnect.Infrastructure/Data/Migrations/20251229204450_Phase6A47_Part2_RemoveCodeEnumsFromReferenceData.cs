using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A47_Part2_RemoveCodeEnumsFromReferenceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("030ea891-4551-d7a4-a8dc-7b6aae04a1a3"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("09e43bc5-e08a-d3d7-ad60-8038af9b6d29"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("25c65b18-5a29-02bf-ccc7-8abdc92bcf36"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("39052dc5-732b-c91e-5d2f-2712825ffd67"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("51dd102d-1284-794a-2ce1-c94a668c123f"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6246f6e6-122b-8a19-e19d-cdd59ef66b49"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("7f3aed08-471c-9621-ae43-077e05a24f53"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("996e43b0-1aaa-4958-a81e-8fe5a7dade49"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9a393a97-dce0-972e-266a-c7d0c87e6fbe"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c028c08b-c7dc-333e-a756-2e0e0f49da15"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("f16f3460-bdd0-053d-481c-1947ad5c6f77"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("f54f2798-7e29-ae36-27fa-332729049b7a"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("f5a75980-4ea7-8af7-fab6-293538bca4fd"));

            migrationBuilder.DeleteData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("f7fb53b7-7def-8e63-6fa5-8beeb39b8fff"));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 44, 47, 587, DateTimeKind.Utc).AddTicks(1884));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 44, 47, 587, DateTimeKind.Utc).AddTicks(2029));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 44, 47, 587, DateTimeKind.Utc).AddTicks(1762));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 44, 47, 587, DateTimeKind.Utc).AddTicks(1970));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 44, 47, 587, DateTimeKind.Utc).AddTicks(2000));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 44, 47, 587, DateTimeKind.Utc).AddTicks(2186));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 44, 47, 587, DateTimeKind.Utc).AddTicks(1936));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 44, 47, 587, DateTimeKind.Utc).AddTicks(1821));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 44, 47, 587, DateTimeKind.Utc).AddTicks(2124));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 44, 47, 587, DateTimeKind.Utc).AddTicks(2094));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 44, 47, 587, DateTimeKind.Utc).AddTicks(2060));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 44, 47, 587, DateTimeKind.Utc).AddTicks(2155));
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
                value: new DateTime(2025, 12, 29, 20, 30, 36, 727, DateTimeKind.Utc).AddTicks(9968));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(215));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 30, 36, 727, DateTimeKind.Utc).AddTicks(9842));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(120));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(156));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(383));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(76));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 30, 36, 727, DateTimeKind.Utc).AddTicks(9912));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(317));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(284));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(251));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(350));

            migrationBuilder.InsertData(
                schema: "reference_data",
                table: "reference_values",
                columns: new[] { "id", "code", "created_at", "description", "display_order", "enum_type", "int_value", "is_active", "metadata", "name" },
                values: new object[,]
                {
                    { new Guid("030ea891-4551-d7a4-a8dc-7b6aae04a1a3"), "Active", new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(612), null, 3, "EventStatus", 2, true, "{\"allowsRegistration\":true,\"isFinalState\":false}", "Active" },
                    { new Guid("09e43bc5-e08a-d3d7-ad60-8038af9b6d29"), "Cancelled", new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(689), null, 5, "EventStatus", 4, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Cancelled" },
                    { new Guid("25c65b18-5a29-02bf-ccc7-8abdc92bcf36"), "EventOrganizer", new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(1140), null, 3, "UserRole", 3, true, "{\"canManageUsers\":false,\"canCreateEvents\":true,\"canModerateContent\":false,\"isEventOrganizer\":true,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":false,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":10.00}", "Event Organizer" },
                    { new Guid("39052dc5-732b-c91e-5d2f-2712825ffd67"), "AdminManager", new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(1413), null, 6, "UserRole", 6, true, "{\"canManageUsers\":true,\"canCreateEvents\":true,\"canModerateContent\":true,\"isEventOrganizer\":false,\"isAdmin\":true,\"requiresSubscription\":false,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":0.00}", "Admin Manager" },
                    { new Guid("51dd102d-1284-794a-2ce1-c94a668c123f"), "Published", new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(573), null, 2, "EventStatus", 1, true, "{\"allowsRegistration\":true,\"isFinalState\":false}", "Published" },
                    { new Guid("6246f6e6-122b-8a19-e19d-cdd59ef66b49"), "UnderReview", new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(822), null, 8, "EventStatus", 7, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Under Review" },
                    { new Guid("7f3aed08-471c-9621-ae43-077e05a24f53"), "GeneralUser", new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(969), null, 1, "UserRole", 1, true, "{\"canManageUsers\":false,\"canCreateEvents\":false,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":false,\"canCreateBusinessProfile\":false,\"canCreatePosts\":false,\"monthlySubscriptionPrice\":0.00}", "General User" },
                    { new Guid("996e43b0-1aaa-4958-a81e-8fe5a7dade49"), "Postponed", new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(650), null, 4, "EventStatus", 3, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Postponed" },
                    { new Guid("9a393a97-dce0-972e-266a-c7d0c87e6fbe"), "BusinessOwner", new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(1057), null, 2, "UserRole", 2, true, "{\"canManageUsers\":false,\"canCreateEvents\":false,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":true,\"canCreatePosts\":false,\"monthlySubscriptionPrice\":10.00}", "Business Owner" },
                    { new Guid("c028c08b-c7dc-333e-a756-2e0e0f49da15"), "Draft", new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(533), null, 1, "EventStatus", 0, true, "{\"allowsRegistration\":false,\"isFinalState\":false}", "Draft" },
                    { new Guid("f16f3460-bdd0-053d-481c-1947ad5c6f77"), "Archived", new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(782), null, 7, "EventStatus", 6, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Archived" },
                    { new Guid("f54f2798-7e29-ae36-27fa-332729049b7a"), "Admin", new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(1337), null, 5, "UserRole", 5, true, "{\"canManageUsers\":true,\"canCreateEvents\":true,\"canModerateContent\":true,\"isEventOrganizer\":false,\"isAdmin\":true,\"requiresSubscription\":false,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":0.00}", "Administrator" },
                    { new Guid("f5a75980-4ea7-8af7-fab6-293538bca4fd"), "Completed", new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(726), null, 6, "EventStatus", 5, true, "{\"allowsRegistration\":false,\"isFinalState\":true}", "Completed" },
                    { new Guid("f7fb53b7-7def-8e63-6fa5-8beeb39b8fff"), "EventOrganizerAndBusinessOwner", new DateTime(2025, 12, 29, 20, 30, 36, 728, DateTimeKind.Utc).AddTicks(1233), null, 4, "UserRole", 4, true, "{\"canManageUsers\":false,\"canCreateEvents\":true,\"canModerateContent\":false,\"isEventOrganizer\":false,\"isAdmin\":false,\"requiresSubscription\":true,\"canCreateBusinessProfile\":true,\"canCreatePosts\":true,\"monthlySubscriptionPrice\":15.00}", "Event Organizer + Business Owner" }
                });
        }
    }
}
