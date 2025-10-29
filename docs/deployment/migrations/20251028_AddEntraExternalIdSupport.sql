CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'events') THEN
            CREATE SCHEMA events;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'community') THEN
            CREATE SCHEMA community;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'identity') THEN
            CREATE SCHEMA identity;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE TABLE events.events (
        "Id" uuid NOT NULL,
        title character varying(200) NOT NULL,
        description character varying(2000) NOT NULL,
        "StartDate" timestamp with time zone NOT NULL,
        "EndDate" timestamp with time zone NOT NULL,
        "OrganizerId" uuid NOT NULL,
        "Capacity" integer NOT NULL,
        "Status" character varying(20) NOT NULL DEFAULT 'Draft',
        "CancellationReason" character varying(500),
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_events" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE TABLE community.topics (
        "Id" uuid NOT NULL,
        title character varying(100) NOT NULL,
        content character varying(10000) NOT NULL,
        "AuthorId" uuid NOT NULL,
        "ForumId" uuid NOT NULL,
        "Category" character varying(50) NOT NULL,
        "Status" character varying(20) NOT NULL DEFAULT 'Active',
        "IsPinned" boolean NOT NULL DEFAULT FALSE,
        "ViewCount" integer NOT NULL DEFAULT 0,
        "LockReason" character varying(500),
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_topics" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE TABLE identity.users (
        "Id" uuid NOT NULL,
        email character varying(255) NOT NULL,
        "FirstName" character varying(100) NOT NULL,
        "LastName" character varying(100) NOT NULL,
        phone_number character varying(20),
        "Bio" character varying(1000),
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE TABLE events.registrations (
        "Id" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Quantity" integer NOT NULL DEFAULT 1,
        "Status" character varying(20) NOT NULL DEFAULT 'Confirmed',
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_registrations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_registrations_events_EventId" FOREIGN KEY ("EventId") REFERENCES events.events ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE TABLE community.replies (
        "Id" uuid NOT NULL,
        "TopicId" uuid NOT NULL,
        content character varying(10000) NOT NULL,
        "AuthorId" uuid NOT NULL,
        "ParentReplyId" uuid,
        "HelpfulVotes" integer NOT NULL DEFAULT 0,
        "IsMarkedAsSolution" boolean NOT NULL DEFAULT FALSE,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_replies" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_replies_replies_ParentReplyId" FOREIGN KEY ("ParentReplyId") REFERENCES community.replies ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_replies_topics_TopicId" FOREIGN KEY ("TopicId") REFERENCES community.topics ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_events_organizer_id ON events.events ("OrganizerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_events_start_date ON events.events ("StartDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_events_status ON events.events ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_events_status_start_date ON events.events ("Status", "StartDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_registrations_event_id ON events.registrations ("EventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_registrations_event_user_unique ON events.registrations ("EventId", "UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_registrations_user_id ON events.registrations ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_registrations_user_status ON events.registrations ("UserId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_replies_author_id ON community.replies ("AuthorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_replies_helpful_votes ON community.replies ("HelpfulVotes");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_replies_parent_id ON community.replies ("ParentReplyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_replies_solution_topic ON community.replies ("IsMarkedAsSolution", "TopicId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_replies_topic_created ON community.replies ("TopicId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_replies_topic_id ON community.replies ("TopicId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_topics_author_id ON community.topics ("AuthorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_topics_category ON community.topics ("Category");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_topics_forum_id ON community.topics ("ForumId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_topics_forum_status_updated ON community.topics ("ForumId", "Status", "UpdatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_topics_pinned_updated ON community.topics ("IsPinned", "UpdatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_topics_status ON community.topics ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_users_created_at ON identity.users ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_users_email ON identity.users (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    CREATE INDEX ix_users_is_active ON identity.users ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250830150251_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250830150251_InitialCreate', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250831125422_InitialMigration') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250831125422_InitialMigration', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'business') THEN
            CREATE SCHEMA business;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'communications') THEN
            CREATE SCHEMA communications;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE EXTENSION IF NOT EXISTS hstore;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    ALTER TABLE identity.users ADD "AccountLockedUntil" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    ALTER TABLE identity.users ADD "EmailVerificationToken" character varying(255);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    ALTER TABLE identity.users ADD "EmailVerificationTokenExpiresAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    ALTER TABLE identity.users ADD "FailedLoginAttempts" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    ALTER TABLE identity.users ADD "IsEmailVerified" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    ALTER TABLE identity.users ADD "LastLoginAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    ALTER TABLE identity.users ADD "PasswordHash" character varying(255);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    ALTER TABLE identity.users ADD "PasswordResetToken" character varying(255);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    ALTER TABLE identity.users ADD "PasswordResetTokenExpiresAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    ALTER TABLE identity.users ADD "Role" integer NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE TABLE business.businesses (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(2000) NOT NULL,
        "AddressStreet" character varying(200) NOT NULL,
        "AddressCity" character varying(50) NOT NULL,
        "AddressState" character varying(50) NOT NULL,
        "AddressZipCode" character varying(10) NOT NULL,
        "AddressCountry" character varying(50) NOT NULL,
        "LocationLatitude" numeric(10,8),
        "LocationLongitude" numeric(11,8),
        "ContactEmail" character varying(100),
        "ContactPhone" character varying(20),
        "ContactWebsite" character varying(200),
        "ContactFacebook" character varying(100),
        "ContactInstagram" character varying(50),
        "ContactTwitter" character varying(50),
        "BusinessHours" json NOT NULL,
        "Category" character varying(50) NOT NULL,
        "Status" character varying(50) NOT NULL,
        "OwnerId" uuid NOT NULL,
        "Rating" numeric(3,2),
        "ReviewCount" integer NOT NULL DEFAULT 0,
        "IsVerified" boolean NOT NULL DEFAULT FALSE,
        "VerifiedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_businesses" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE TABLE communications.email_messages (
        "Id" uuid NOT NULL,
        from_email character varying(255) NOT NULL,
        subject character varying(200) NOT NULL,
        text_content text NOT NULL,
        html_content text,
        type character varying(50) NOT NULL,
        status character varying(50) NOT NULL,
        sent_at timestamp with time zone,
        delivered_at timestamp with time zone,
        opened_at timestamp with time zone,
        clicked_at timestamp with time zone,
        failed_at timestamp with time zone,
        next_retry_at timestamp with time zone,
        error_message character varying(1000),
        retry_count integer NOT NULL,
        max_retries integer NOT NULL,
        template_name character varying(100),
        template_data jsonb,
        priority integer NOT NULL,
        "MessageId" text,
        bcc_emails jsonb NOT NULL,
        cc_emails jsonb NOT NULL,
        to_emails jsonb NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_email_messages" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE TABLE communications.email_templates (
        "Id" uuid NOT NULL,
        name character varying(100) NOT NULL,
        description character varying(500) NOT NULL,
        subject_template character varying(200) NOT NULL,
        text_template text NOT NULL,
        html_template text,
        type character varying(50) NOT NULL,
        category character varying(50) NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        tags character varying(500),
        created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
        updated_at timestamp with time zone,
        CONSTRAINT "PK_email_templates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE TABLE communications.user_email_preferences (
        "Id" uuid NOT NULL,
        user_id uuid NOT NULL,
        allow_marketing boolean NOT NULL DEFAULT FALSE,
        allow_notifications boolean NOT NULL DEFAULT TRUE,
        allow_newsletters boolean NOT NULL DEFAULT TRUE,
        allow_transactional boolean NOT NULL DEFAULT TRUE,
        preferred_language character varying(10) DEFAULT 'en-US',
        timezone character varying(100),
        created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
        updated_at timestamp with time zone,
        CONSTRAINT "PK_user_email_preferences" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserEmailPreferences_Users_UserId" FOREIGN KEY (user_id) REFERENCES identity.users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE TABLE identity.user_refresh_tokens (
        "UserId" uuid NOT NULL,
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        "Token" character varying(255) NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "IsRevoked" boolean NOT NULL DEFAULT FALSE,
        "RevokedAt" timestamp with time zone,
        "RevokedByIp" character varying(45),
        "CreatedByIp" character varying(45) NOT NULL,
        CONSTRAINT "PK_user_refresh_tokens" PRIMARY KEY ("UserId", "Id"),
        CONSTRAINT "FK_user_refresh_tokens_users_UserId" FOREIGN KEY ("UserId") REFERENCES identity.users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE TABLE "BusinessImage" (
        "Id" text NOT NULL,
        "OriginalUrl" text NOT NULL,
        "ThumbnailUrl" text NOT NULL,
        "MediumUrl" text NOT NULL,
        "LargeUrl" text NOT NULL,
        "AltText" text NOT NULL,
        "Caption" text NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "IsPrimary" boolean NOT NULL,
        "FileSizeBytes" bigint NOT NULL,
        "ContentType" text NOT NULL,
        "UploadedAt" timestamp with time zone NOT NULL,
        "Metadata" hstore NOT NULL,
        "BusinessId" uuid,
        CONSTRAINT "PK_BusinessImage" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_BusinessImage_businesses_BusinessId" FOREIGN KEY ("BusinessId") REFERENCES business.businesses ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE TABLE business.reviews (
        "Id" uuid NOT NULL,
        rating integer NOT NULL,
        title character varying(200) NOT NULL,
        content character varying(2000) NOT NULL,
        pros jsonb,
        cons jsonb,
        "BusinessId" uuid NOT NULL,
        "ReviewerId" uuid NOT NULL,
        "Status" text NOT NULL DEFAULT 'Pending',
        "ApprovedAt" timestamp with time zone,
        "ModerationNotes" character varying(1000),
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_reviews" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_reviews_businesses_BusinessId" FOREIGN KEY ("BusinessId") REFERENCES business.businesses ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE TABLE business.services (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(1000) NOT NULL,
        "PriceAmount" numeric(10,2),
        "PriceCurrency" integer,
        "Duration" character varying(100),
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "BusinessId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_services" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_services_businesses_BusinessId" FOREIGN KEY ("BusinessId") REFERENCES business.businesses ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX ix_users_email_verification_token ON identity.users ("EmailVerificationToken");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX ix_users_is_email_verified ON identity.users ("IsEmailVerified");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX ix_users_last_login_at ON identity.users ("LastLoginAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX ix_users_password_reset_token ON identity.users ("PasswordResetToken");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX ix_users_role ON identity.users ("Role");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_Business_Category" ON business.businesses ("Category");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_Business_CreatedAt" ON business.businesses ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_Business_IsVerified" ON business.businesses ("IsVerified");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_Business_OwnerId" ON business.businesses ("OwnerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_Business_Rating" ON business.businesses ("Rating");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_Business_Status" ON business.businesses ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_BusinessImage_BusinessId" ON "BusinessImage" ("BusinessId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_EmailMessages_CreatedAt" ON communications.email_messages ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_EmailMessages_Priority" ON communications.email_messages (priority);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_EmailMessages_RetryCount_Status" ON communications.email_messages (retry_count, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_EmailMessages_Status" ON communications.email_messages (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_EmailMessages_Status_NextRetryAt" ON communications.email_messages (status, next_retry_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_EmailMessages_Type" ON communications.email_messages (type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_EmailTemplates_Category" ON communications.email_templates (category);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_EmailTemplates_Category_IsActive" ON communications.email_templates (category, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_EmailTemplates_CreatedAt" ON communications.email_templates (created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_EmailTemplates_IsActive" ON communications.email_templates (is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE UNIQUE INDEX "IX_EmailTemplates_Name" ON communications.email_templates (name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_EmailTemplates_Type" ON communications.email_templates (type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_EmailTemplates_Type_IsActive" ON communications.email_templates (type, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX ix_reviews_business_id ON business.reviews ("BusinessId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE UNIQUE INDEX ix_reviews_business_reviewer_unique ON business.reviews ("BusinessId", "ReviewerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX ix_reviews_business_status ON business.reviews ("BusinessId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX ix_reviews_business_status_created ON business.reviews ("BusinessId", "Status", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX ix_reviews_created_at ON business.reviews ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX ix_reviews_reviewer_id ON business.reviews ("ReviewerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX ix_reviews_reviewer_status ON business.reviews ("ReviewerId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX ix_reviews_status ON business.reviews ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_Service_BusinessId" ON business.services ("BusinessId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_Service_IsActive" ON business.services ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_Service_Name" ON business.services ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_UserEmailPreferences_AllowMarketing" ON communications.user_email_preferences (allow_marketing);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_UserEmailPreferences_AllowNotifications" ON communications.user_email_preferences (allow_notifications);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_UserEmailPreferences_CreatedAt" ON communications.user_email_preferences (created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX "IX_UserEmailPreferences_PreferredLanguage" ON communications.user_email_preferences (preferred_language);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE UNIQUE INDEX "IX_UserEmailPreferences_UserId_Unique" ON communications.user_email_preferences (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE INDEX ix_user_refresh_tokens_expires_at ON identity.user_refresh_tokens ("ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    CREATE UNIQUE INDEX ix_user_refresh_tokens_token ON identity.user_refresh_tokens ("Token");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250904194650_AddCommunicationsTables') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250904194650_AddCommunicationsTables', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    DROP TABLE "BusinessImage";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    DROP TABLE identity.user_refresh_tokens;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE community.topics DROP COLUMN title;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.services DROP COLUMN "PriceAmount";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.services DROP COLUMN "PriceCurrency";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.reviews DROP COLUMN cons;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.reviews DROP COLUMN content;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.reviews DROP COLUMN pros;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.reviews DROP COLUMN rating;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.reviews DROP COLUMN title;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE events.events DROP COLUMN description;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE events.events DROP COLUMN title;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.businesses DROP COLUMN "AddressCity";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.businesses DROP COLUMN "AddressCountry";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.businesses DROP COLUMN "AddressState";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.businesses DROP COLUMN "AddressStreet";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.businesses DROP COLUMN "AddressZipCode";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.businesses DROP COLUMN "ContactEmail";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.businesses DROP COLUMN "ContactFacebook";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.businesses DROP COLUMN "ContactInstagram";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.businesses DROP COLUMN "ContactPhone";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.businesses DROP COLUMN "ContactTwitter";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.businesses DROP COLUMN "ContactWebsite";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.businesses DROP COLUMN "Description";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.businesses DROP COLUMN "LocationLatitude";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.businesses DROP COLUMN "LocationLongitude";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.businesses DROP COLUMN "Name";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE identity.users ADD "ExternalProviderId" character varying(255);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE identity.users ADD "IdentityProvider" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "BackoffMultiplier" double precision;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "BypassReason" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "ConcurrentAccessAttempts" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "CulturalDelayBypassed" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "CulturalDelayReason" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "CulturalTimingOptimized" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "DeliveryConfirmationReceived" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "DiasporaOptimized" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "FestivalContext" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "GeographicOptimization" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "GeographicRegion" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "HasAllRecipientsDelivered" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "LastStateTransition" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "LocalizedSendTime" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "OptimalSendTime" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "PermanentFailureReason" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "PostponementReason" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "ReligiousObservanceConsidered" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "RetryStrategy" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "SendingStartedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD "TargetTimezone" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE communications.email_messages ADD recipient_statuses jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    ALTER TABLE business.businesses ADD "Version" bigint NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    CREATE INDEX ix_users_external_provider_id ON identity.users ("ExternalProviderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    CREATE INDEX ix_users_identity_provider ON identity.users ("IdentityProvider");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    CREATE INDEX ix_users_identity_provider_external_id ON identity.users ("IdentityProvider", "ExternalProviderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251028184528_AddEntraExternalIdSupport') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251028184528_AddEntraExternalIdSupport', '8.0.19');
    END IF;
END $EF$;
COMMIT;

