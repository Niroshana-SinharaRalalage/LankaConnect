using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "events");

            migrationBuilder.EnsureSchema(
                name: "community");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "events",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OrganizerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "topics",
                schema: "community",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    content = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ForumId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LockReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_topics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Bio = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "registrations",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Confirmed"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_registrations_events_EventId",
                        column: x => x.EventId,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "replies",
                schema: "community",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentReplyId = table.Column<Guid>(type: "uuid", nullable: true),
                    HelpfulVotes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsMarkedAsSolution = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_replies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_replies_replies_ParentReplyId",
                        column: x => x.ParentReplyId,
                        principalSchema: "community",
                        principalTable: "replies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_replies_topics_TopicId",
                        column: x => x.TopicId,
                        principalSchema: "community",
                        principalTable: "topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_events_organizer_id",
                schema: "events",
                table: "events",
                column: "OrganizerId");

            migrationBuilder.CreateIndex(
                name: "ix_events_start_date",
                schema: "events",
                table: "events",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "ix_events_status",
                schema: "events",
                table: "events",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_events_status_start_date",
                schema: "events",
                table: "events",
                columns: new[] { "Status", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "ix_registrations_event_id",
                schema: "events",
                table: "registrations",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "ix_registrations_event_user_unique",
                schema: "events",
                table: "registrations",
                columns: new[] { "EventId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registrations_user_id",
                schema: "events",
                table: "registrations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_registrations_user_status",
                schema: "events",
                table: "registrations",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_replies_author_id",
                schema: "community",
                table: "replies",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "ix_replies_helpful_votes",
                schema: "community",
                table: "replies",
                column: "HelpfulVotes");

            migrationBuilder.CreateIndex(
                name: "ix_replies_parent_id",
                schema: "community",
                table: "replies",
                column: "ParentReplyId");

            migrationBuilder.CreateIndex(
                name: "ix_replies_solution_topic",
                schema: "community",
                table: "replies",
                columns: new[] { "IsMarkedAsSolution", "TopicId" });

            migrationBuilder.CreateIndex(
                name: "ix_replies_topic_created",
                schema: "community",
                table: "replies",
                columns: new[] { "TopicId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_replies_topic_id",
                schema: "community",
                table: "replies",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "ix_topics_author_id",
                schema: "community",
                table: "topics",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "ix_topics_category",
                schema: "community",
                table: "topics",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "ix_topics_forum_id",
                schema: "community",
                table: "topics",
                column: "ForumId");

            migrationBuilder.CreateIndex(
                name: "ix_topics_forum_status_updated",
                schema: "community",
                table: "topics",
                columns: new[] { "ForumId", "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_topics_pinned_updated",
                schema: "community",
                table: "topics",
                columns: new[] { "IsPinned", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_topics_status",
                schema: "community",
                table: "topics",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_users_created_at",
                schema: "identity",
                table: "users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                schema: "identity",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_is_active",
                schema: "identity",
                table: "users",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registrations",
                schema: "events");

            migrationBuilder.DropTable(
                name: "replies",
                schema: "community");

            migrationBuilder.DropTable(
                name: "users",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "events",
                schema: "events");

            migrationBuilder.DropTable(
                name: "topics",
                schema: "community");
        }
    }
}
