using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LankaConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunicationsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "business");

            migrationBuilder.EnsureSchema(
                name: "communications");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.AddColumn<DateTime>(
                name: "AccountLockedUntil",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationToken",
                schema: "identity",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationTokenExpiresAt",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                schema: "identity",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                schema: "identity",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                schema: "identity",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiresAt",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Role",
                schema: "identity",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "businesses",
                schema: "business",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AddressStreet = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AddressCity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AddressState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AddressZipCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    AddressCountry = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LocationLatitude = table.Column<decimal>(type: "numeric(10,8)", nullable: true),
                    LocationLongitude = table.Column<decimal>(type: "numeric(11,8)", nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ContactWebsite = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContactFacebook = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContactInstagram = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ContactTwitter = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BusinessHours = table.Column<string>(type: "json", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<decimal>(type: "numeric(3,2)", nullable: true),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_businesses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "email_messages",
                schema: "communications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    text_content = table.Column<string>(type: "text", nullable: false),
                    html_content = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    clicked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_retry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    max_retries = table.Column<int>(type: "integer", nullable: false),
                    template_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    template_data = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    MessageId = table.Column<string>(type: "text", nullable: true),
                    bcc_emails = table.Column<List<string>>(type: "jsonb", nullable: false),
                    cc_emails = table.Column<List<string>>(type: "jsonb", nullable: false),
                    to_emails = table.Column<List<string>>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "email_templates",
                schema: "communications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    subject_template = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    text_template = table.Column<string>(type: "text", nullable: false),
                    html_template = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_email_preferences",
                schema: "communications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allow_marketing = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    allow_notifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_newsletters = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_transactional = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    preferred_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true, defaultValue: "en-US"),
                    timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_email_preferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEmailPreferences_Users_UserId",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_refresh_tokens",
                schema: "identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    CreatedByIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_refresh_tokens", x => new { x.UserId, x.Id });
                    table.ForeignKey(
                        name: "FK_user_refresh_tokens_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessImage",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    OriginalUrl = table.Column<string>(type: "text", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "text", nullable: false),
                    MediumUrl = table.Column<string>(type: "text", nullable: false),
                    LargeUrl = table.Column<string>(type: "text", nullable: false),
                    AltText = table.Column<string>(type: "text", nullable: false),
                    Caption = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Metadata = table.Column<Dictionary<string, string>>(type: "hstore", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessImage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessImage_businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "business",
                        principalTable: "businesses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                schema: "business",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    pros = table.Column<string>(type: "jsonb", nullable: true),
                    cons = table.Column<string>(type: "jsonb", nullable: true),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModerationNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reviews_businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "business",
                        principalTable: "businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "services",
                schema: "business",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PriceAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    PriceCurrency = table.Column<int>(type: "integer", maxLength: 3, nullable: true),
                    Duration = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_services_businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "business",
                        principalTable: "businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_email_verification_token",
                schema: "identity",
                table: "users",
                column: "EmailVerificationToken");

            migrationBuilder.CreateIndex(
                name: "ix_users_is_email_verified",
                schema: "identity",
                table: "users",
                column: "IsEmailVerified");

            migrationBuilder.CreateIndex(
                name: "ix_users_last_login_at",
                schema: "identity",
                table: "users",
                column: "LastLoginAt");

            migrationBuilder.CreateIndex(
                name: "ix_users_password_reset_token",
                schema: "identity",
                table: "users",
                column: "PasswordResetToken");

            migrationBuilder.CreateIndex(
                name: "ix_users_role",
                schema: "identity",
                table: "users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_Business_Category",
                schema: "business",
                table: "businesses",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Business_CreatedAt",
                schema: "business",
                table: "businesses",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Business_IsVerified",
                schema: "business",
                table: "businesses",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "IX_Business_OwnerId",
                schema: "business",
                table: "businesses",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Business_Rating",
                schema: "business",
                table: "businesses",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_Business_Status",
                schema: "business",
                table: "businesses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessImage_BusinessId",
                table: "BusinessImage",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_CreatedAt",
                schema: "communications",
                table: "email_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_Priority",
                schema: "communications",
                table: "email_messages",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_RetryCount_Status",
                schema: "communications",
                table: "email_messages",
                columns: new[] { "retry_count", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_Status",
                schema: "communications",
                table: "email_messages",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_Status_NextRetryAt",
                schema: "communications",
                table: "email_messages",
                columns: new[] { "status", "next_retry_at" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_Type",
                schema: "communications",
                table: "email_messages",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_Category",
                schema: "communications",
                table: "email_templates",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_Category_IsActive",
                schema: "communications",
                table: "email_templates",
                columns: new[] { "category", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_CreatedAt",
                schema: "communications",
                table: "email_templates",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_IsActive",
                schema: "communications",
                table: "email_templates",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_Name",
                schema: "communications",
                table: "email_templates",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_Type",
                schema: "communications",
                table: "email_templates",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_Type_IsActive",
                schema: "communications",
                table: "email_templates",
                columns: new[] { "type", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_reviews_business_id",
                schema: "business",
                table: "reviews",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_business_reviewer_unique",
                schema: "business",
                table: "reviews",
                columns: new[] { "BusinessId", "ReviewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reviews_business_status",
                schema: "business",
                table: "reviews",
                columns: new[] { "BusinessId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_reviews_business_status_created",
                schema: "business",
                table: "reviews",
                columns: new[] { "BusinessId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_reviews_created_at",
                schema: "business",
                table: "reviews",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_reviewer_id",
                schema: "business",
                table: "reviews",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_reviewer_status",
                schema: "business",
                table: "reviews",
                columns: new[] { "ReviewerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_reviews_status",
                schema: "business",
                table: "reviews",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Service_BusinessId",
                schema: "business",
                table: "services",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Service_IsActive",
                schema: "business",
                table: "services",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Service_Name",
                schema: "business",
                table: "services",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_UserEmailPreferences_AllowMarketing",
                schema: "communications",
                table: "user_email_preferences",
                column: "allow_marketing");

            migrationBuilder.CreateIndex(
                name: "IX_UserEmailPreferences_AllowNotifications",
                schema: "communications",
                table: "user_email_preferences",
                column: "allow_notifications");

            migrationBuilder.CreateIndex(
                name: "IX_UserEmailPreferences_CreatedAt",
                schema: "communications",
                table: "user_email_preferences",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_UserEmailPreferences_PreferredLanguage",
                schema: "communications",
                table: "user_email_preferences",
                column: "preferred_language");

            migrationBuilder.CreateIndex(
                name: "IX_UserEmailPreferences_UserId_Unique",
                schema: "communications",
                table: "user_email_preferences",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_refresh_tokens_expires_at",
                schema: "identity",
                table: "user_refresh_tokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "ix_user_refresh_tokens_token",
                schema: "identity",
                table: "user_refresh_tokens",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessImage");

            migrationBuilder.DropTable(
                name: "email_messages",
                schema: "communications");

            migrationBuilder.DropTable(
                name: "email_templates",
                schema: "communications");

            migrationBuilder.DropTable(
                name: "reviews",
                schema: "business");

            migrationBuilder.DropTable(
                name: "services",
                schema: "business");

            migrationBuilder.DropTable(
                name: "user_email_preferences",
                schema: "communications");

            migrationBuilder.DropTable(
                name: "user_refresh_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "businesses",
                schema: "business");

            migrationBuilder.DropIndex(
                name: "ix_users_email_verification_token",
                schema: "identity",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_is_email_verified",
                schema: "identity",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_last_login_at",
                schema: "identity",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_password_reset_token",
                schema: "identity",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_role",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AccountLockedUntil",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationToken",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationTokenExpiresAt",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiresAt",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Role",
                schema: "identity",
                table: "users");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:hstore", ",,");
        }
    }
}
