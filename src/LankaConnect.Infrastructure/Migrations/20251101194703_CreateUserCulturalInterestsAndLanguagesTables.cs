using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LankaConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateUserCulturalInterestsAndLanguagesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "users");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.AddColumn<string>(
                name: "city",
                schema: "identity",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country",
                schema: "identity",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "state",
                schema: "identity",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "zip_code",
                schema: "identity",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

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
                name: "user_cultural_interests",
                schema: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    interest_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_cultural_interests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_cultural_interests_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_languages",
                schema: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    language_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    proficiency_level = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_languages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_languages_users_UserId",
                        column: x => x.UserId,
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

            migrationBuilder.CreateIndex(
                name: "IX_BusinessImage_BusinessId",
                table: "BusinessImage",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "ix_user_cultural_interests_user_code",
                schema: "users",
                table: "user_cultural_interests",
                columns: new[] { "UserId", "interest_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_languages_code",
                schema: "users",
                table: "user_languages",
                column: "language_code");

            migrationBuilder.CreateIndex(
                name: "IX_user_languages_UserId",
                schema: "users",
                table: "user_languages",
                column: "UserId");

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
                name: "user_cultural_interests",
                schema: "users");

            migrationBuilder.DropTable(
                name: "user_languages",
                schema: "users");

            migrationBuilder.DropTable(
                name: "user_refresh_tokens",
                schema: "identity");

            migrationBuilder.DropColumn(
                name: "city",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "country",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "state",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "zip_code",
                schema: "identity",
                table: "users");

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
        }
    }
}
