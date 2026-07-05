using System;
using System.Collections.Generic;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomFormSurveyFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "event_forms",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    allow_multiple_responses = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    response_deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    max_responses = table.Column<int>(type: "integer", nullable: true),
                    has_responses = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_forms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "form_responses",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    respondent_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    respondent_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    respondent_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_responses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "form_questions",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    question_type = table.Column<int>(type: "integer", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    help_text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    options = table.Column<IReadOnlyList<QuestionOption>>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_questions", x => x.id);
                    table.ForeignKey(
                        name: "FK_form_questions_event_forms_event_form_id",
                        column: x => x.event_form_id,
                        principalSchema: "events",
                        principalTable: "event_forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "form_answers",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_response_id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_text_snapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    text_value = table.Column<string>(type: "text", nullable: true),
                    selected_option_ids = table.Column<IReadOnlyList<Guid>>(type: "jsonb", nullable: false),
                    selected_option_text_snapshots = table.Column<IReadOnlyList<string>>(type: "jsonb", nullable: false),
                    boolean_value = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_answers", x => x.id);
                    table.ForeignKey(
                        name: "FK_form_answers_form_responses_form_response_id",
                        column: x => x.form_response_id,
                        principalSchema: "events",
                        principalTable: "form_responses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(4640));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(4981));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(4554));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(4696));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(4908));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(5116));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(4666));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(4610));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(5069));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(5044));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(5009));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(5092));

            migrationBuilder.CreateIndex(
                name: "ix_event_forms_event_id",
                schema: "events",
                table: "event_forms",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_forms_status",
                schema: "events",
                table: "event_forms",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_form_answers_form_question_id",
                schema: "events",
                table: "form_answers",
                column: "form_question_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_answers_form_response_id",
                schema: "events",
                table: "form_answers",
                column: "form_response_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_questions_event_form_id",
                schema: "events",
                table: "form_questions",
                column: "event_form_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_questions_sort_order",
                schema: "events",
                table: "form_questions",
                column: "sort_order");

            migrationBuilder.CreateIndex(
                name: "ix_form_responses_access_token_hash",
                schema: "events",
                table: "form_responses",
                column: "access_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_form_responses_event_form_id",
                schema: "events",
                table: "form_responses",
                column: "event_form_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_responses_event_id",
                schema: "events",
                table: "form_responses",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_responses_respondent_email",
                schema: "events",
                table: "form_responses",
                column: "respondent_email");

            migrationBuilder.CreateIndex(
                name: "ix_form_responses_respondent_user_id",
                schema: "events",
                table: "form_responses",
                column: "respondent_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_responses_submitted_at",
                schema: "events",
                table: "form_responses",
                column: "submitted_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "form_answers",
                schema: "events");

            migrationBuilder.DropTable(
                name: "form_questions",
                schema: "events");

            migrationBuilder.DropTable(
                name: "form_responses",
                schema: "events");

            migrationBuilder.DropTable(
                name: "event_forms",
                schema: "events");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6644));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6873));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6557));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6806));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6831));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(7003));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6778));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6612));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6957));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6932));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6899));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6981));
        }
    }
}
