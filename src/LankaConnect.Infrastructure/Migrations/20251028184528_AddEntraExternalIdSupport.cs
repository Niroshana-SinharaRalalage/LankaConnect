using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LankaConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEntraExternalIdSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessImage");

            migrationBuilder.DropTable(
                name: "user_refresh_tokens",
                schema: "identity");

            migrationBuilder.DropColumn(
                name: "title",
                schema: "community",
                table: "topics");

            migrationBuilder.DropColumn(
                name: "PriceAmount",
                schema: "business",
                table: "services");

            migrationBuilder.DropColumn(
                name: "PriceCurrency",
                schema: "business",
                table: "services");

            migrationBuilder.DropColumn(
                name: "cons",
                schema: "business",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "content",
                schema: "business",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "pros",
                schema: "business",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "rating",
                schema: "business",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "title",
                schema: "business",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "title",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "AddressCity",
                schema: "business",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "AddressCountry",
                schema: "business",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "AddressState",
                schema: "business",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "AddressStreet",
                schema: "business",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "AddressZipCode",
                schema: "business",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                schema: "business",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "ContactFacebook",
                schema: "business",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "ContactInstagram",
                schema: "business",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                schema: "business",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "ContactTwitter",
                schema: "business",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "ContactWebsite",
                schema: "business",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "business",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "LocationLatitude",
                schema: "business",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "LocationLongitude",
                schema: "business",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "business",
                table: "businesses");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.AddColumn<string>(
                name: "ExternalProviderId",
                schema: "identity",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdentityProvider",
                schema: "identity",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "BackoffMultiplier",
                schema: "communications",
                table: "email_messages",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BypassReason",
                schema: "communications",
                table: "email_messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConcurrentAccessAttempts",
                schema: "communications",
                table: "email_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "CulturalDelayBypassed",
                schema: "communications",
                table: "email_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CulturalDelayReason",
                schema: "communications",
                table: "email_messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CulturalTimingOptimized",
                schema: "communications",
                table: "email_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DeliveryConfirmationReceived",
                schema: "communications",
                table: "email_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DiasporaOptimized",
                schema: "communications",
                table: "email_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FestivalContext",
                schema: "communications",
                table: "email_messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GeographicOptimization",
                schema: "communications",
                table: "email_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "GeographicRegion",
                schema: "communications",
                table: "email_messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAllRecipientsDelivered",
                schema: "communications",
                table: "email_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastStateTransition",
                schema: "communications",
                table: "email_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LocalizedSendTime",
                schema: "communications",
                table: "email_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OptimalSendTime",
                schema: "communications",
                table: "email_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PermanentFailureReason",
                schema: "communications",
                table: "email_messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostponementReason",
                schema: "communications",
                table: "email_messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReligiousObservanceConsidered",
                schema: "communications",
                table: "email_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RetryStrategy",
                schema: "communications",
                table: "email_messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SendingStartedAt",
                schema: "communications",
                table: "email_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetTimezone",
                schema: "communications",
                table: "email_messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recipient_statuses",
                schema: "communications",
                table: "email_messages",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                schema: "business",
                table: "businesses",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "ix_users_external_provider_id",
                schema: "identity",
                table: "users",
                column: "ExternalProviderId");

            migrationBuilder.CreateIndex(
                name: "ix_users_identity_provider",
                schema: "identity",
                table: "users",
                column: "IdentityProvider");

            migrationBuilder.CreateIndex(
                name: "ix_users_identity_provider_external_id",
                schema: "identity",
                table: "users",
                columns: new[] { "IdentityProvider", "ExternalProviderId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_external_provider_id",
                schema: "identity",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_identity_provider",
                schema: "identity",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_identity_provider_external_id",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ExternalProviderId",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IdentityProvider",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "BackoffMultiplier",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "BypassReason",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "ConcurrentAccessAttempts",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "CulturalDelayBypassed",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "CulturalDelayReason",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "CulturalTimingOptimized",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "DeliveryConfirmationReceived",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "DiasporaOptimized",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "FestivalContext",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "GeographicOptimization",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "GeographicRegion",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "HasAllRecipientsDelivered",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "LastStateTransition",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "LocalizedSendTime",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "OptimalSendTime",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "PermanentFailureReason",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "PostponementReason",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "ReligiousObservanceConsidered",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "RetryStrategy",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "SendingStartedAt",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "TargetTimezone",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "recipient_statuses",
                schema: "communications",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "business",
                table: "businesses");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.AddColumn<string>(
                name: "title",
                schema: "community",
                table: "topics",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PriceAmount",
                schema: "business",
                table: "services",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriceCurrency",
                schema: "business",
                table: "services",
                type: "integer",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cons",
                schema: "business",
                table: "reviews",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "content",
                schema: "business",
                table: "reviews",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "pros",
                schema: "business",
                table: "reviews",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rating",
                schema: "business",
                table: "reviews",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "title",
                schema: "business",
                table: "reviews",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "events",
                table: "events",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "title",
                schema: "events",
                table: "events",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AddressCity",
                schema: "business",
                table: "businesses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AddressCountry",
                schema: "business",
                table: "businesses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AddressState",
                schema: "business",
                table: "businesses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AddressStreet",
                schema: "business",
                table: "businesses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AddressZipCode",
                schema: "business",
                table: "businesses",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                schema: "business",
                table: "businesses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactFacebook",
                schema: "business",
                table: "businesses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactInstagram",
                schema: "business",
                table: "businesses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                schema: "business",
                table: "businesses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactTwitter",
                schema: "business",
                table: "businesses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactWebsite",
                schema: "business",
                table: "businesses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "business",
                table: "businesses",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "LocationLatitude",
                schema: "business",
                table: "businesses",
                type: "numeric(10,8)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LocationLongitude",
                schema: "business",
                table: "businesses",
                type: "numeric(11,8)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "business",
                table: "businesses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "BusinessImage",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AltText = table.Column<string>(type: "text", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: true),
                    Caption = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    LargeUrl = table.Column<string>(type: "text", nullable: false),
                    MediumUrl = table.Column<string>(type: "text", nullable: false),
                    Metadata = table.Column<Dictionary<string, string>>(type: "hstore", nullable: false),
                    OriginalUrl = table.Column<string>(type: "text", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                name: "user_refresh_tokens",
                schema: "identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    Token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_BusinessImage_BusinessId",
                table: "BusinessImage",
                column: "BusinessId");

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
    }
}
