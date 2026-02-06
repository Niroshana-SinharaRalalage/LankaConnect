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

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251031125825_AddUserProfilePhoto') THEN
    ALTER TABLE identity.users ADD "ProfilePhotoBlobName" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251031125825_AddUserProfilePhoto') THEN
    ALTER TABLE identity.users ADD "ProfilePhotoUrl" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251031125825_AddUserProfilePhoto') THEN
    ALTER TABLE communications.email_messages ADD cultural_context jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251031125825_AddUserProfilePhoto') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251031125825_AddUserProfilePhoto', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251031131720_AddUserLocation') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251031131720_AddUserLocation', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251031194253_AddUserCulturalInterestsAndLanguages') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251031194253_AddUserCulturalInterestsAndLanguages', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'users') THEN
            CREATE SCHEMA users;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    CREATE EXTENSION IF NOT EXISTS hstore;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE identity.users ADD city character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE identity.users ADD country character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE identity.users ADD state character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE identity.users ADD zip_code character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE community.topics ADD title character varying(100) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.services ADD "PriceAmount" numeric(10,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.services ADD "PriceCurrency" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.reviews ADD cons jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.reviews ADD content character varying(2000) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.reviews ADD pros jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.reviews ADD rating integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.reviews ADD title character varying(200) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE events.events ADD description character varying(2000) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE events.events ADD title character varying(200) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.businesses ADD "AddressCity" character varying(50) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.businesses ADD "AddressCountry" character varying(50) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.businesses ADD "AddressState" character varying(50) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.businesses ADD "AddressStreet" character varying(200) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.businesses ADD "AddressZipCode" character varying(10) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.businesses ADD "ContactEmail" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.businesses ADD "ContactFacebook" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.businesses ADD "ContactInstagram" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.businesses ADD "ContactPhone" character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.businesses ADD "ContactTwitter" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.businesses ADD "ContactWebsite" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.businesses ADD "Description" character varying(2000) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.businesses ADD "LocationLatitude" numeric(10,8);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.businesses ADD "LocationLongitude" numeric(11,8);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    ALTER TABLE business.businesses ADD "Name" character varying(200) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
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
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    CREATE TABLE users.user_cultural_interests (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        interest_code character varying(50) NOT NULL,
        "UserId" uuid NOT NULL,
        CONSTRAINT "PK_user_cultural_interests" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_user_cultural_interests_users_UserId" FOREIGN KEY ("UserId") REFERENCES identity.users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    CREATE TABLE users.user_languages (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        language_code character varying(10) NOT NULL,
        proficiency_level integer NOT NULL,
        "UserId" uuid NOT NULL,
        CONSTRAINT "PK_user_languages" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_user_languages_users_UserId" FOREIGN KEY ("UserId") REFERENCES identity.users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
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
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    CREATE INDEX "IX_BusinessImage_BusinessId" ON "BusinessImage" ("BusinessId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    CREATE UNIQUE INDEX ix_user_cultural_interests_user_code ON users.user_cultural_interests ("UserId", interest_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    CREATE INDEX ix_user_languages_code ON users.user_languages (language_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    CREATE INDEX "IX_user_languages_UserId" ON users.user_languages ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    CREATE INDEX ix_user_refresh_tokens_expires_at ON identity.user_refresh_tokens ("ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    CREATE UNIQUE INDEX ix_user_refresh_tokens_token ON identity.user_refresh_tokens ("Token");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251101194703_CreateUserCulturalInterestsAndLanguagesTables') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251101194703_CreateUserCulturalInterestsAndLanguagesTables', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102000439_AddExternalLoginsSupport') THEN
    CREATE TABLE identity.external_logins (
        "UserId" uuid NOT NULL,
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        provider integer NOT NULL,
        external_provider_id character varying(255) NOT NULL,
        provider_email character varying(255) NOT NULL,
        linked_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_external_logins" PRIMARY KEY ("UserId", "Id"),
        CONSTRAINT "FK_external_logins_users_UserId" FOREIGN KEY ("UserId") REFERENCES identity.users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102000439_AddExternalLoginsSupport') THEN
    CREATE UNIQUE INDEX ix_external_logins_provider_external_id ON identity.external_logins (provider, external_provider_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102000439_AddExternalLoginsSupport') THEN
    CREATE INDEX ix_external_logins_user_id ON identity.external_logins ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102000439_AddExternalLoginsSupport') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251102000439_AddExternalLoginsSupport', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102061243_AddEventLocationWithPostGIS') THEN
    CREATE EXTENSION IF NOT EXISTS hstore;
    CREATE EXTENSION IF NOT EXISTS postgis;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102061243_AddEventLocationWithPostGIS') THEN
    ALTER TABLE events.events ADD address_city character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102061243_AddEventLocationWithPostGIS') THEN
    ALTER TABLE events.events ADD address_country character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102061243_AddEventLocationWithPostGIS') THEN
    ALTER TABLE events.events ADD address_state character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102061243_AddEventLocationWithPostGIS') THEN
    ALTER TABLE events.events ADD address_street character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102061243_AddEventLocationWithPostGIS') THEN
    ALTER TABLE events.events ADD address_zip_code character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102061243_AddEventLocationWithPostGIS') THEN
    ALTER TABLE events.events ADD coordinates_latitude numeric(10,7);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102061243_AddEventLocationWithPostGIS') THEN
    ALTER TABLE events.events ADD coordinates_longitude numeric(10,7);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102061243_AddEventLocationWithPostGIS') THEN
    ALTER TABLE events.events ADD has_location boolean DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102061243_AddEventLocationWithPostGIS') THEN

                    ALTER TABLE events.events
                    ADD COLUMN location GEOGRAPHY(POINT, 4326)
                    GENERATED ALWAYS AS (
                        CASE
                            WHEN coordinates_latitude IS NOT NULL AND coordinates_longitude IS NOT NULL
                            THEN ST_SetSRID(ST_MakePoint(coordinates_longitude, coordinates_latitude), 4326)::geography
                            ELSE NULL
                        END
                    ) STORED;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102061243_AddEventLocationWithPostGIS') THEN

                    CREATE INDEX ix_events_location_gist
                    ON events.events
                    USING GIST (location)
                    WHERE location IS NOT NULL;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102061243_AddEventLocationWithPostGIS') THEN

                    CREATE INDEX ix_events_city
                    ON events.events (address_city)
                    WHERE address_city IS NOT NULL;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102061243_AddEventLocationWithPostGIS') THEN

                    CREATE INDEX ix_events_status_city_startdate
                    ON events.events ("Status", address_city, "StartDate")
                    WHERE address_city IS NOT NULL;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102061243_AddEventLocationWithPostGIS') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251102061243_AddEventLocationWithPostGIS', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102144315_AddEventCategoryAndTicketPrice') THEN
    ALTER TABLE events.events ADD "Category" character varying(20) NOT NULL DEFAULT 'Community';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102144315_AddEventCategoryAndTicketPrice') THEN
    ALTER TABLE events.events ADD ticket_price_amount numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102144315_AddEventCategoryAndTicketPrice') THEN
    ALTER TABLE events.events ADD ticket_price_currency character varying(3);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251102144315_AddEventCategoryAndTicketPrice') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251102144315_AddEventCategoryAndTicketPrice', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251103040053_AddEventImages') THEN
    CREATE TABLE events."EventImages" (
        "Id" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "ImageUrl" character varying(500) NOT NULL,
        "BlobName" character varying(255) NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "UploadedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_EventImages" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EventImages_events_EventId" FOREIGN KEY ("EventId") REFERENCES events.events ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251103040053_AddEventImages') THEN
    CREATE INDEX "IX_EventImages_EventId" ON events."EventImages" ("EventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251103040053_AddEventImages') THEN
    CREATE UNIQUE INDEX "IX_EventImages_EventId_DisplayOrder" ON events."EventImages" ("EventId", "DisplayOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251103040053_AddEventImages') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251103040053_AddEventImages', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104060300_AddEventAnalytics') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'analytics') THEN
            CREATE SCHEMA analytics;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104060300_AddEventAnalytics') THEN
    CREATE TABLE analytics.event_analytics (
        id uuid NOT NULL,
        event_id uuid NOT NULL,
        total_views integer NOT NULL DEFAULT 0,
        unique_viewers integer NOT NULL DEFAULT 0,
        registration_count integer NOT NULL DEFAULT 0,
        last_viewed_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
        updated_at timestamp with time zone,
        CONSTRAINT "PK_event_analytics" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104060300_AddEventAnalytics') THEN
    CREATE TABLE analytics.event_view_records (
        id uuid NOT NULL,
        event_id uuid NOT NULL,
        user_id uuid,
        ip_address character varying(45) NOT NULL,
        user_agent character varying(500),
        viewed_at timestamp with time zone NOT NULL DEFAULT (NOW()),
        CONSTRAINT "PK_event_view_records" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104060300_AddEventAnalytics') THEN
    CREATE UNIQUE INDEX ix_event_analytics_event_id_unique ON analytics.event_analytics (event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104060300_AddEventAnalytics') THEN
    CREATE INDEX ix_event_analytics_last_viewed_at ON analytics.event_analytics (last_viewed_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104060300_AddEventAnalytics') THEN
    CREATE INDEX ix_event_analytics_total_views ON analytics.event_analytics (total_views);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104060300_AddEventAnalytics') THEN
    CREATE INDEX ix_event_view_records_dedup_ip ON analytics.event_view_records (event_id, ip_address, viewed_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104060300_AddEventAnalytics') THEN
    CREATE INDEX ix_event_view_records_dedup_user ON analytics.event_view_records (event_id, user_id, viewed_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104060300_AddEventAnalytics') THEN
    CREATE INDEX ix_event_view_records_event_id ON analytics.event_view_records (event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104060300_AddEventAnalytics') THEN
    CREATE INDEX ix_event_view_records_ip_address ON analytics.event_view_records (ip_address);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104060300_AddEventAnalytics') THEN
    CREATE INDEX ix_event_view_records_user_id ON analytics.event_view_records (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104060300_AddEventAnalytics') THEN
    CREATE INDEX ix_event_view_records_viewed_at ON analytics.event_view_records (viewed_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104060300_AddEventAnalytics') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251104060300_AddEventAnalytics', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104184035_AddFullTextSearchSupport') THEN

                    ALTER TABLE events.events
                    ADD COLUMN search_vector tsvector
                    GENERATED ALWAYS AS (
                        setweight(to_tsvector('english', coalesce(title, '')), 'A') ||
                        setweight(to_tsvector('english', coalesce(description, '')), 'B')
                    ) STORED;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104184035_AddFullTextSearchSupport') THEN

                    CREATE INDEX idx_events_search_vector
                    ON events.events
                    USING GIN(search_vector);
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104184035_AddFullTextSearchSupport') THEN
    ANALYZE events.events;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104184035_AddFullTextSearchSupport') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251104184035_AddFullTextSearchSupport', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104195443_AddWaitingListAndSocialSharing') THEN
    ALTER TABLE analytics.event_analytics ADD share_count integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104195443_AddWaitingListAndSocialSharing') THEN
    CREATE TABLE events.event_waiting_list (
        "Id" uuid NOT NULL,
        user_id uuid NOT NULL,
        joined_at timestamp with time zone NOT NULL,
        position integer NOT NULL,
        "EventId" uuid NOT NULL,
        CONSTRAINT "PK_event_waiting_list" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_event_waiting_list_events_EventId" FOREIGN KEY ("EventId") REFERENCES events.events ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104195443_AddWaitingListAndSocialSharing') THEN
    CREATE INDEX ix_event_waiting_list_event_position ON events.event_waiting_list ("EventId", position);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104195443_AddWaitingListAndSocialSharing') THEN
    CREATE UNIQUE INDEX ix_event_waiting_list_event_user ON events.event_waiting_list ("EventId", user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251104195443_AddWaitingListAndSocialSharing') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251104195443_AddWaitingListAndSocialSharing', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251109152709_AddNewsletterSubscribers') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'communications') THEN
            CREATE SCHEMA communications;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251109152709_AddNewsletterSubscribers') THEN
    CREATE TABLE communications.newsletter_subscribers (
        id uuid NOT NULL,
        email character varying(255) NOT NULL,
        metro_area_id uuid,
        receive_all_locations boolean NOT NULL DEFAULT FALSE,
        is_active boolean NOT NULL DEFAULT TRUE,
        is_confirmed boolean NOT NULL DEFAULT FALSE,
        confirmation_token character varying(100),
        confirmation_sent_at timestamp with time zone,
        confirmed_at timestamp with time zone,
        unsubscribe_token character varying(100) NOT NULL,
        unsubscribed_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        version bytea NOT NULL,
        CONSTRAINT pk_newsletter_subscribers PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251109152709_AddNewsletterSubscribers') THEN
    CREATE INDEX idx_newsletter_subscribers_active_confirmed ON communications.newsletter_subscribers (is_active, is_confirmed);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251109152709_AddNewsletterSubscribers') THEN
    CREATE INDEX idx_newsletter_subscribers_confirmation_token ON communications.newsletter_subscribers (confirmation_token);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251109152709_AddNewsletterSubscribers') THEN
    CREATE UNIQUE INDEX idx_newsletter_subscribers_email ON communications.newsletter_subscribers (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251109152709_AddNewsletterSubscribers') THEN
    CREATE INDEX idx_newsletter_subscribers_metro_area_id ON communications.newsletter_subscribers (metro_area_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251109152709_AddNewsletterSubscribers') THEN
    CREATE INDEX idx_newsletter_subscribers_unsubscribe_token ON communications.newsletter_subscribers (unsubscribe_token);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251109152709_AddNewsletterSubscribers') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251109152709_AddNewsletterSubscribers', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251110004152_CreateMetroAreasTable') THEN
    CREATE TABLE events.metro_areas (
        id uuid NOT NULL,
        name character varying(100) NOT NULL,
        state character varying(2) NOT NULL,
        center_latitude double precision NOT NULL,
        center_longitude double precision NOT NULL,
        radius_miles integer NOT NULL,
        is_state_level_area boolean NOT NULL DEFAULT FALSE,
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_metro_areas" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251110004152_CreateMetroAreasTable') THEN
    CREATE INDEX idx_metro_areas_is_active ON events.metro_areas (is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251110004152_CreateMetroAreasTable') THEN
    CREATE INDEX idx_metro_areas_name ON events.metro_areas (name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251110004152_CreateMetroAreasTable') THEN
    CREATE INDEX idx_metro_areas_state ON events.metro_areas (state);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251110004152_CreateMetroAreasTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251110004152_CreateMetroAreasTable', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251110031400_AddUserPreferredMetroAreas') THEN
    CREATE TABLE identity.user_preferred_metro_areas (
        user_id uuid NOT NULL,
        metro_area_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
        CONSTRAINT "PK_user_preferred_metro_areas" PRIMARY KEY (user_id, metro_area_id),
        CONSTRAINT fk_user_preferred_metro_areas_metro_area_id FOREIGN KEY (metro_area_id) REFERENCES events.metro_areas (id) ON DELETE CASCADE,
        CONSTRAINT fk_user_preferred_metro_areas_user_id FOREIGN KEY (user_id) REFERENCES identity.users ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251110031400_AddUserPreferredMetroAreas') THEN
    CREATE INDEX ix_user_preferred_metro_areas_metro_area_id ON identity.user_preferred_metro_areas (metro_area_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251110031400_AddUserPreferredMetroAreas') THEN
    CREATE INDEX ix_user_preferred_metro_areas_user_id ON identity.user_preferred_metro_areas (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251110031400_AddUserPreferredMetroAreas') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251110031400_AddUserPreferredMetroAreas', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111055748_AddUserRoleUpgradeTracking') THEN
    ALTER TABLE identity.users ADD "PendingUpgradeRole" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111055748_AddUserRoleUpgradeTracking') THEN
    ALTER TABLE identity.users ADD "UpgradeRequestedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111055748_AddUserRoleUpgradeTracking') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251111055748_AddUserRoleUpgradeTracking', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111125348_AddSubscriptionManagement') THEN
    ALTER TABLE identity.users ADD "FreeTrialEndsAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111125348_AddSubscriptionManagement') THEN
    ALTER TABLE identity.users ADD "FreeTrialStartedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111125348_AddSubscriptionManagement') THEN
    ALTER TABLE identity.users ADD "StripeCustomerId" character varying(255);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111125348_AddSubscriptionManagement') THEN
    ALTER TABLE identity.users ADD "StripeSubscriptionId" character varying(255);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111125348_AddSubscriptionManagement') THEN
    ALTER TABLE identity.users ADD "SubscriptionActivatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111125348_AddSubscriptionManagement') THEN
    ALTER TABLE identity.users ADD "SubscriptionCanceledAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111125348_AddSubscriptionManagement') THEN
    ALTER TABLE identity.users ADD "SubscriptionStatus" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111125348_AddSubscriptionManagement') THEN
    CREATE INDEX ix_users_free_trial_ends_at ON identity.users ("FreeTrialEndsAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111125348_AddSubscriptionManagement') THEN
    CREATE INDEX ix_users_stripe_customer_id ON identity.users ("StripeCustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111125348_AddSubscriptionManagement') THEN
    CREATE INDEX ix_users_subscription_status ON identity.users ("SubscriptionStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111125348_AddSubscriptionManagement') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251111125348_AddSubscriptionManagement', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111172127_AddNotificationsTable') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'notifications') THEN
            CREATE SCHEMA notifications;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111172127_AddNotificationsTable') THEN
    CREATE TABLE notifications.notifications (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Title" character varying(200) NOT NULL,
        "Message" character varying(1000) NOT NULL,
        "Type" integer NOT NULL,
        "IsRead" boolean NOT NULL DEFAULT FALSE,
        "ReadAt" timestamp with time zone,
        "RelatedEntityId" character varying(100),
        "RelatedEntityType" character varying(100),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_notifications" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111172127_AddNotificationsTable') THEN
    CREATE INDEX ix_notifications_created_at ON notifications.notifications ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111172127_AddNotificationsTable') THEN
    CREATE INDEX ix_notifications_user_id ON notifications.notifications ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111172127_AddNotificationsTable') THEN
    CREATE INDEX ix_notifications_user_id_is_read ON notifications.notifications ("UserId", "IsRead");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111172127_AddNotificationsTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251111172127_AddNotificationsTable', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111221521_AddEventTemplatesTable') THEN
    DROP INDEX identity.ix_users_free_trial_ends_at;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111221521_AddEventTemplatesTable') THEN
    DROP INDEX identity.ix_users_stripe_customer_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111221521_AddEventTemplatesTable') THEN
    DROP INDEX identity.ix_users_subscription_status;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111221521_AddEventTemplatesTable') THEN
    ALTER TABLE identity.users DROP COLUMN "FreeTrialEndsAt";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111221521_AddEventTemplatesTable') THEN
    ALTER TABLE identity.users DROP COLUMN "FreeTrialStartedAt";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111221521_AddEventTemplatesTable') THEN
    ALTER TABLE identity.users DROP COLUMN "StripeCustomerId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111221521_AddEventTemplatesTable') THEN
    ALTER TABLE identity.users DROP COLUMN "StripeSubscriptionId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111221521_AddEventTemplatesTable') THEN
    ALTER TABLE identity.users DROP COLUMN "SubscriptionActivatedAt";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111221521_AddEventTemplatesTable') THEN
    ALTER TABLE identity.users DROP COLUMN "SubscriptionCanceledAt";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111221521_AddEventTemplatesTable') THEN
    ALTER TABLE identity.users DROP COLUMN "SubscriptionStatus";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111221521_AddEventTemplatesTable') THEN
    CREATE TABLE events.event_templates (
        id uuid NOT NULL,
        name character varying(100) NOT NULL,
        description character varying(500) NOT NULL,
        category character varying(50) NOT NULL,
        thumbnail_svg text NOT NULL,
        template_data_json jsonb NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        display_order integer NOT NULL DEFAULT 0,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_event_templates" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111221521_AddEventTemplatesTable') THEN
    CREATE INDEX idx_event_templates_active_category_order ON events.event_templates (is_active, category, display_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111221521_AddEventTemplatesTable') THEN
    CREATE INDEX idx_event_templates_category ON events.event_templates (category);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111221521_AddEventTemplatesTable') THEN
    CREATE INDEX idx_event_templates_display_order ON events.event_templates (display_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111221521_AddEventTemplatesTable') THEN
    CREATE INDEX idx_event_templates_is_active ON events.event_templates (is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251111221521_AddEventTemplatesTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251111221521_AddEventTemplatesTable', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251112204434_SeedMetroAreasReferenceData') THEN

    INSERT INTO events.metro_areas (id, name, state, center_latitude, center_longitude, radius_miles, is_state_level_area, is_active, created_at, updated_at)
    VALUES
      ('01000000-0000-0000-0000-000000000001', 'All Alabama', 'AL', 32.8067, -86.7113, 200, true, true, CURRENT_TIMESTAMP, NULL),
      ('01111111-1111-1111-1111-111111111001', 'Birmingham', 'AL', 33.5186, -86.8104, 30, false, true, CURRENT_TIMESTAMP, NULL),
      ('01111111-1111-1111-1111-111111111002', 'Montgomery', 'AL', 32.3792, -86.3077, 25, false, true, CURRENT_TIMESTAMP, NULL),
      ('01111111-1111-1111-1111-111111111003', 'Mobile', 'AL', 30.6954, -88.0399, 25, false, true, CURRENT_TIMESTAMP, NULL),
      ('02000000-0000-0000-0000-000000000001', 'All Alaska', 'AK', 64.0685, -152.2782, 300, true, true, CURRENT_TIMESTAMP, NULL),
      ('02111111-1111-1111-1111-111111111001', 'Anchorage', 'AK', 61.2181, -149.9003, 30, false, true, CURRENT_TIMESTAMP, NULL),
      ('04000000-0000-0000-0000-000000000001', 'All Arizona', 'AZ', 33.7298, -111.4312, 200, true, true, CURRENT_TIMESTAMP, NULL),
      ('04111111-1111-1111-1111-111111111001', 'Phoenix', 'AZ', 33.4484, -112.0742, 35, false, true, CURRENT_TIMESTAMP, NULL),
      ('04111111-1111-1111-1111-111111111002', 'Tucson', 'AZ', 32.2226, -110.9747, 30, false, true, CURRENT_TIMESTAMP, NULL),
      ('04111111-1111-1111-1111-111111111003', 'Mesa', 'AZ', 33.4152, -111.8317, 25, false, true, CURRENT_TIMESTAMP, NULL),
      ('06000000-0000-0000-0000-000000000001', 'All California', 'CA', 36.1162, -119.6816, 250, true, true, CURRENT_TIMESTAMP, NULL),
      ('06111111-1111-1111-1111-111111111001', 'Los Angeles', 'CA', 34.0522, -118.2437, 40, false, true, CURRENT_TIMESTAMP, NULL),
      ('06111111-1111-1111-1111-111111111002', 'San Francisco Bay Area', 'CA', 37.7749, -122.4194, 40, false, true, CURRENT_TIMESTAMP, NULL),
      ('06111111-1111-1111-1111-111111111003', 'San Diego', 'CA', 32.7157, -117.1611, 35, false, true, CURRENT_TIMESTAMP, NULL),
      ('17000000-0000-0000-0000-000000000001', 'All Illinois', 'IL', 40.3495, -88.9861, 200, true, true, CURRENT_TIMESTAMP, NULL),
      ('17111111-1111-1111-1111-111111111001', 'Chicago', 'IL', 41.8781, -87.6298, 45, false, true, CURRENT_TIMESTAMP, NULL),
      ('36000000-0000-0000-0000-000000000001', 'All New York', 'NY', 42.1657, -74.9481, 250, true, true, CURRENT_TIMESTAMP, NULL),
      ('36111111-1111-1111-1111-111111111001', 'New York City', 'NY', 40.7128, -74.0060, 40, false, true, CURRENT_TIMESTAMP, NULL),
      ('48000000-0000-0000-0000-000000000001', 'All Texas', 'TX', 31.9686, -99.9018, 300, true, true, CURRENT_TIMESTAMP, NULL),
      ('48111111-1111-1111-1111-111111111001', 'Houston', 'TX', 29.7604, -95.3698, 40, false, true, CURRENT_TIMESTAMP, NULL),
      ('48111111-1111-1111-1111-111111111002', 'Dallas-Fort Worth', 'TX', 32.7767, -96.7970, 40, false, true, CURRENT_TIMESTAMP, NULL),
      ('48111111-1111-1111-1111-111111111003', 'Austin', 'TX', 30.2672, -97.7431, 30, false, true, CURRENT_TIMESTAMP, NULL)
    ON CONFLICT (id) DO NOTHING;

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251112204434_SeedMetroAreasReferenceData') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251112204434_SeedMetroAreasReferenceData', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115044807_RecreateNewsletterTableFixVersionColumn') THEN
    DROP TABLE IF EXISTS communications.newsletter_subscribers CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115044807_RecreateNewsletterTableFixVersionColumn') THEN
    CREATE TABLE communications.newsletter_subscribers (
        id uuid NOT NULL,
        email character varying(255) NOT NULL,
        metro_area_id uuid,
        receive_all_locations boolean NOT NULL DEFAULT FALSE,
        is_active boolean NOT NULL DEFAULT TRUE,
        is_confirmed boolean NOT NULL DEFAULT FALSE,
        confirmation_token character varying(100),
        confirmation_sent_at timestamp with time zone,
        confirmed_at timestamp with time zone,
        unsubscribe_token character varying(100) NOT NULL,
        unsubscribed_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        version bytea NOT NULL,
        CONSTRAINT pk_newsletter_subscribers PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115044807_RecreateNewsletterTableFixVersionColumn') THEN
    CREATE INDEX idx_newsletter_subscribers_active_confirmed ON communications.newsletter_subscribers (is_active, is_confirmed);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115044807_RecreateNewsletterTableFixVersionColumn') THEN
    CREATE INDEX idx_newsletter_subscribers_confirmation_token ON communications.newsletter_subscribers (confirmation_token);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115044807_RecreateNewsletterTableFixVersionColumn') THEN
    CREATE UNIQUE INDEX idx_newsletter_subscribers_email ON communications.newsletter_subscribers (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115044807_RecreateNewsletterTableFixVersionColumn') THEN
    CREATE INDEX idx_newsletter_subscribers_metro_area_id ON communications.newsletter_subscribers (metro_area_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115044807_RecreateNewsletterTableFixVersionColumn') THEN
    CREATE INDEX idx_newsletter_subscribers_unsubscribe_token ON communications.newsletter_subscribers (unsubscribe_token);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251115044807_RecreateNewsletterTableFixVersionColumn') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251115044807_RecreateNewsletterTableFixVersionColumn', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123072228_AddEventPassAndPassPurchaseEntities') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251123072228_AddEventPassAndPassPurchaseEntities', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123072848_AddEventPassAndPassPurchaseTables') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251123072848_AddEventPassAndPassPurchaseTables', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123163612_AddSignUpListAndSignUpCommitmentTables') THEN
    CREATE TABLE event_passes (
        "Id" uuid NOT NULL,
        total_quantity integer NOT NULL,
        reserved_quantity integer NOT NULL DEFAULT 0,
        event_id uuid NOT NULL,
        description character varying(500) NOT NULL,
        name character varying(100) NOT NULL,
        price_amount numeric(18,2) NOT NULL,
        price_currency character varying(3) NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
        updated_at timestamp with time zone,
        CONSTRAINT "PK_event_passes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_event_passes_events_event_id" FOREIGN KEY (event_id) REFERENCES events.events ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123163612_AddSignUpListAndSignUpCommitmentTables') THEN
    CREATE TABLE pass_purchases (
        "Id" uuid NOT NULL,
        user_id uuid NOT NULL,
        event_id uuid NOT NULL,
        event_pass_id uuid NOT NULL,
        quantity integer NOT NULL,
        status character varying(20) NOT NULL DEFAULT 'Pending',
        qr_code character varying(200) NOT NULL,
        confirmed_at timestamp with time zone,
        cancelled_at timestamp with time zone,
        total_price_amount numeric(18,2) NOT NULL,
        total_price_currency character varying(3) NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
        updated_at timestamp with time zone,
        CONSTRAINT "PK_pass_purchases" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123163612_AddSignUpListAndSignUpCommitmentTables') THEN
    CREATE TABLE sign_up_lists (
        id uuid NOT NULL,
        category character varying(100) NOT NULL,
        description character varying(500) NOT NULL,
        sign_up_type character varying(20) NOT NULL,
        event_id uuid NOT NULL,
        predefined_items jsonb NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_sign_up_lists" PRIMARY KEY (id),
        CONSTRAINT "FK_sign_up_lists_events_event_id" FOREIGN KEY (event_id) REFERENCES events.events ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123163612_AddSignUpListAndSignUpCommitmentTables') THEN
    CREATE TABLE sign_up_commitments (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        item_description character varying(500) NOT NULL,
        quantity integer NOT NULL,
        committed_at timestamp with time zone NOT NULL,
        "SignUpListId" uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_sign_up_commitments" PRIMARY KEY (id),
        CONSTRAINT "FK_sign_up_commitments_sign_up_lists_SignUpListId" FOREIGN KEY ("SignUpListId") REFERENCES sign_up_lists (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123163612_AddSignUpListAndSignUpCommitmentTables') THEN
    CREATE INDEX ix_event_passes_event_id ON event_passes (event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123163612_AddSignUpListAndSignUpCommitmentTables') THEN
    CREATE INDEX ix_pass_purchases_event_id ON pass_purchases (event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123163612_AddSignUpListAndSignUpCommitmentTables') THEN
    CREATE INDEX ix_pass_purchases_event_pass_id ON pass_purchases (event_pass_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123163612_AddSignUpListAndSignUpCommitmentTables') THEN
    CREATE INDEX ix_pass_purchases_event_user_status ON pass_purchases (event_id, user_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123163612_AddSignUpListAndSignUpCommitmentTables') THEN
    CREATE UNIQUE INDEX ix_pass_purchases_qr_code ON pass_purchases (qr_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123163612_AddSignUpListAndSignUpCommitmentTables') THEN
    CREATE INDEX ix_pass_purchases_user_id ON pass_purchases (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123163612_AddSignUpListAndSignUpCommitmentTables') THEN
    CREATE INDEX "IX_sign_up_commitments_SignUpListId" ON sign_up_commitments ("SignUpListId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123163612_AddSignUpListAndSignUpCommitmentTables') THEN
    CREATE INDEX ix_sign_up_commitments_user_id ON sign_up_commitments (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123163612_AddSignUpListAndSignUpCommitmentTables') THEN
    CREATE INDEX ix_sign_up_lists_category ON sign_up_lists (category);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123163612_AddSignUpListAndSignUpCommitmentTables') THEN
    CREATE INDEX ix_sign_up_lists_event_id ON sign_up_lists (event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251123163612_AddSignUpListAndSignUpCommitmentTables') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251123163612_AddSignUpListAndSignUpCommitmentTables', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124194005_AddStripePaymentInfrastructure') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'payments') THEN
            CREATE SCHEMA payments;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124194005_AddStripePaymentInfrastructure') THEN
    ALTER TABLE identity.users ADD "StripeCustomerId" character varying(255);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124194005_AddStripePaymentInfrastructure') THEN
    ALTER TABLE identity.users ADD "StripeSubscriptionId" character varying(255);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124194005_AddStripePaymentInfrastructure') THEN
    ALTER TABLE identity.users ADD "SubscriptionEndDate" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124194005_AddStripePaymentInfrastructure') THEN
    ALTER TABLE identity.users ADD "SubscriptionStartDate" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124194005_AddStripePaymentInfrastructure') THEN
    ALTER TABLE identity.users ADD "SubscriptionStatus" integer NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124194005_AddStripePaymentInfrastructure') THEN
    ALTER TABLE identity.users ADD "TrialEndDate" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124194005_AddStripePaymentInfrastructure') THEN
    CREATE TABLE payments.stripe_customers (
        "Id" uuid NOT NULL,
        user_id uuid NOT NULL,
        stripe_customer_id character varying(255) NOT NULL,
        email character varying(255) NOT NULL,
        name character varying(255) NOT NULL,
        stripe_created_at timestamp with time zone NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
        updated_at timestamp with time zone,
        CONSTRAINT "PK_stripe_customers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124194005_AddStripePaymentInfrastructure') THEN
    CREATE TABLE payments.stripe_webhook_events (
        "Id" uuid NOT NULL,
        event_id character varying(255) NOT NULL,
        event_type character varying(100) NOT NULL,
        processed boolean NOT NULL DEFAULT FALSE,
        processed_at timestamp with time zone,
        error_message character varying(2000),
        attempt_count integer NOT NULL DEFAULT 0,
        created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
        updated_at timestamp with time zone,
        CONSTRAINT "PK_stripe_webhook_events" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124194005_AddStripePaymentInfrastructure') THEN
    CREATE UNIQUE INDEX ix_users_stripe_customer_id ON identity.users ("StripeCustomerId") WHERE "StripeCustomerId" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124194005_AddStripePaymentInfrastructure') THEN
    CREATE UNIQUE INDEX ix_users_stripe_subscription_id ON identity.users ("StripeSubscriptionId") WHERE "StripeSubscriptionId" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124194005_AddStripePaymentInfrastructure') THEN
    CREATE INDEX ix_users_subscription_status ON identity.users ("SubscriptionStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124194005_AddStripePaymentInfrastructure') THEN
    CREATE INDEX ix_users_trial_end_date ON identity.users ("TrialEndDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124194005_AddStripePaymentInfrastructure') THEN
    CREATE UNIQUE INDEX ix_stripe_customers_stripe_customer_id ON payments.stripe_customers (stripe_customer_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124194005_AddStripePaymentInfrastructure') THEN
    CREATE UNIQUE INDEX ix_stripe_customers_user_id ON payments.stripe_customers (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124194005_AddStripePaymentInfrastructure') THEN
    CREATE UNIQUE INDEX ix_stripe_webhook_events_event_id ON payments.stripe_webhook_events (event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124194005_AddStripePaymentInfrastructure') THEN
    CREATE INDEX ix_stripe_webhook_events_event_type ON payments.stripe_webhook_events (event_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124194005_AddStripePaymentInfrastructure') THEN
    CREATE INDEX ix_stripe_webhook_events_processed ON payments.stripe_webhook_events (processed);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124194005_AddStripePaymentInfrastructure') THEN
    CREATE INDEX ix_stripe_webhook_events_processed_created_at ON payments.stripe_webhook_events (processed, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251124194005_AddStripePaymentInfrastructure') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251124194005_AddStripePaymentInfrastructure', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    DROP TABLE event_passes;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    DROP TABLE pass_purchases;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    DROP TABLE payments.stripe_customers;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    DROP TABLE payments.stripe_webhook_events;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    DROP INDEX identity.ix_users_stripe_customer_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    DROP INDEX identity.ix_users_stripe_subscription_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    DROP INDEX identity.ix_users_subscription_status;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    DROP INDEX identity.ix_users_trial_end_date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    ALTER TABLE identity.users DROP COLUMN "StripeCustomerId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    ALTER TABLE identity.users DROP COLUMN "StripeSubscriptionId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    ALTER TABLE identity.users DROP COLUMN "SubscriptionEndDate";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    ALTER TABLE identity.users DROP COLUMN "SubscriptionStartDate";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    ALTER TABLE identity.users DROP COLUMN "SubscriptionStatus";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    ALTER TABLE identity.users DROP COLUMN "TrialEndDate";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    ALTER TABLE sign_up_lists SET SCHEMA events;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    ALTER TABLE sign_up_commitments SET SCHEMA events;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    ALTER TABLE events.sign_up_lists ADD has_mandatory_items boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    ALTER TABLE events.sign_up_lists ADD has_preferred_items boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    ALTER TABLE events.sign_up_lists ADD has_suggested_items boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    ALTER TABLE events.sign_up_commitments ADD notes character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    ALTER TABLE events.sign_up_commitments ADD sign_up_item_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    CREATE TABLE events.sign_up_items (
        id uuid NOT NULL,
        sign_up_list_id uuid NOT NULL,
        item_description character varying(200) NOT NULL,
        quantity integer NOT NULL,
        item_category integer NOT NULL,
        remaining_quantity integer NOT NULL,
        notes character varying(500),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_sign_up_items" PRIMARY KEY (id),
        CONSTRAINT "FK_sign_up_items_sign_up_lists_sign_up_list_id" FOREIGN KEY (sign_up_list_id) REFERENCES events.sign_up_lists (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    CREATE INDEX ix_sign_up_commitments_sign_up_item_id ON events.sign_up_commitments (sign_up_item_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    CREATE INDEX ix_sign_up_items_category ON events.sign_up_items (item_category);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    CREATE INDEX ix_sign_up_items_list_id ON events.sign_up_items (sign_up_list_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    ALTER TABLE events.sign_up_commitments ADD CONSTRAINT "FK_sign_up_commitments_sign_up_items_sign_up_item_id" FOREIGN KEY (sign_up_item_id) REFERENCES events.sign_up_items (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251129201535_AddSignUpItemCategorySupport') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251129201535_AddSignUpItemCategorySupport', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251201232956_AddAnonymousRegistrationSupport') THEN
    DROP INDEX events.ix_registrations_event_user_unique;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251201232956_AddAnonymousRegistrationSupport') THEN
    ALTER TABLE events.registrations ALTER COLUMN "UserId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251201232956_AddAnonymousRegistrationSupport') THEN
    ALTER TABLE events.registrations ADD attendee_info jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251201232956_AddAnonymousRegistrationSupport') THEN
    ALTER TABLE events.registrations ADD CONSTRAINT ck_registrations_user_xor_attendee CHECK (("UserId" IS NOT NULL AND attendee_info IS NULL) OR ("UserId" IS NULL AND attendee_info IS NOT NULL));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251201232956_AddAnonymousRegistrationSupport') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251201232956_AddAnonymousRegistrationSupport', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202124837_AddDualTicketPricingAndMultiAttendee') THEN
    ALTER TABLE events.registrations DROP CONSTRAINT ck_registrations_user_xor_attendee;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202124837_AddDualTicketPricingAndMultiAttendee') THEN
    ALTER TABLE events.registrations ADD attendees jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202124837_AddDualTicketPricingAndMultiAttendee') THEN
    ALTER TABLE events.registrations ADD contact jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202124837_AddDualTicketPricingAndMultiAttendee') THEN
    ALTER TABLE events.registrations ADD total_price_amount numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202124837_AddDualTicketPricingAndMultiAttendee') THEN
    ALTER TABLE events.registrations ADD total_price_currency character varying(3);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202124837_AddDualTicketPricingAndMultiAttendee') THEN
    ALTER TABLE events.events ADD pricing jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202124837_AddDualTicketPricingAndMultiAttendee') THEN
    ALTER TABLE events.registrations ADD CONSTRAINT ck_registrations_valid_format CHECK ((
                        ("UserId" IS NOT NULL AND attendee_info IS NULL) OR
                        ("UserId" IS NULL AND attendee_info IS NOT NULL) OR
                        (attendees IS NOT NULL AND contact IS NOT NULL)
                    ));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251202124837_AddDualTicketPricingAndMultiAttendee') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251202124837_AddDualTicketPricingAndMultiAttendee', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203013353_AddEventPaymentIntegration') THEN
    ALTER TABLE events.registrations ADD "PaymentStatus" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203013353_AddEventPaymentIntegration') THEN
    ALTER TABLE events.registrations ADD "StripeCheckoutSessionId" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203013353_AddEventPaymentIntegration') THEN
    ALTER TABLE events.registrations ADD "StripePaymentIntentId" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203013353_AddEventPaymentIntegration') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251203013353_AddEventPaymentIntegration', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203162215_AddPricingJsonbColumn') THEN
    ALTER TABLE events.events ADD ticket_price jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203162215_AddPricingJsonbColumn') THEN

                    UPDATE events.events
                    SET ticket_price = jsonb_build_object(
                        'Amount', ticket_price_amount,
                        'Currency', ticket_price_currency
                    )
                    WHERE ticket_price_amount IS NOT NULL
                      AND ticket_price_currency IS NOT NULL;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203162215_AddPricingJsonbColumn') THEN
    ALTER TABLE events.events DROP COLUMN ticket_price_amount;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203162215_AddPricingJsonbColumn') THEN
    ALTER TABLE events.events DROP COLUMN ticket_price_currency;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251203162215_AddPricingJsonbColumn') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251203162215_AddPricingJsonbColumn', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204051150_AddEventVideos') THEN
    CREATE TABLE events."EventVideos" (
        "Id" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "VideoUrl" character varying(500) NOT NULL,
        "BlobName" character varying(255) NOT NULL,
        "ThumbnailUrl" character varying(500) NOT NULL,
        "ThumbnailBlobName" character varying(255) NOT NULL,
        "Duration" interval,
        "Format" character varying(50) NOT NULL,
        "FileSizeBytes" bigint NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "UploadedAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_EventVideos" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_EventVideos_events_EventId" FOREIGN KEY ("EventId") REFERENCES events.events ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204051150_AddEventVideos') THEN
    CREATE INDEX "IX_EventVideos_EventId" ON events."EventVideos" ("EventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204051150_AddEventVideos') THEN
    CREATE UNIQUE INDEX "IX_EventVideos_EventId_DisplayOrder" ON events."EventVideos" ("EventId", "DisplayOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204051150_AddEventVideos') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251204051150_AddEventVideos', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204065504_AddAuditFieldsToEventImages') THEN
    ALTER TABLE events."EventImages" ADD "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204065504_AddAuditFieldsToEventImages') THEN
    ALTER TABLE events."EventImages" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204065504_AddAuditFieldsToEventImages') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251204065504_AddAuditFieldsToEventImages', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204172917_AddContactInfoToSignUpCommitments') THEN
    ALTER TABLE events.sign_up_commitments ADD "ContactEmail" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204172917_AddContactInfoToSignUpCommitments') THEN
    ALTER TABLE events.sign_up_commitments ADD "ContactName" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204172917_AddContactInfoToSignUpCommitments') THEN
    ALTER TABLE events.sign_up_commitments ADD "ContactPhone" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251204172917_AddContactInfoToSignUpCommitments') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251204172917_AddContactInfoToSignUpCommitments', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205210534_AddIsPrimaryToEventImages') THEN
    ALTER TABLE events."EventImages" ADD "IsPrimary" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205210534_AddIsPrimaryToEventImages') THEN

                    UPDATE events."EventImages"
                    SET "IsPrimary" = true
                    WHERE "DisplayOrder" = 1;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205210534_AddIsPrimaryToEventImages') THEN

                    CREATE UNIQUE INDEX "IX_EventImages_EventId_IsPrimary_True"
                    ON events."EventImages"("EventId")
                    WHERE "IsPrimary" = true;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251205210534_AddIsPrimaryToEventImages') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251205210534_AddIsPrimaryToEventImages', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208044133_FixEventImagePrimaryDataConsistency') THEN

                    UPDATE events."EventImages" ei
                    SET "IsPrimary" = false
                    WHERE ei."IsPrimary" = true
                      AND ei."EventId" IN (
                        -- Find events with multiple primary images
                        SELECT "EventId"
                        FROM events."EventImages"
                        WHERE "IsPrimary" = true
                        GROUP BY "EventId"
                        HAVING COUNT(*) > 1
                      )
                      AND ei."Id" NOT IN (
                        -- Keep the one with lowest DisplayOrder
                        SELECT ei2."Id"
                        FROM events."EventImages" ei2
                        WHERE ei2."EventId" = ei."EventId"
                          AND ei2."IsPrimary" = true
                        ORDER BY ei2."DisplayOrder" ASC
                        LIMIT 1
                      );
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208044133_FixEventImagePrimaryDataConsistency') THEN

                    WITH events_with_images AS (
                        SELECT DISTINCT ei."EventId"
                        FROM events."EventImages" ei
                    ),
                    events_without_primary AS (
                        SELECT ewa."EventId"
                        FROM events_with_images ewa
                        WHERE NOT EXISTS (
                            SELECT 1
                            FROM events."EventImages" ei
                            WHERE ei."EventId" = ewa."EventId"
                              AND ei."IsPrimary" = true
                        )
                    )
                    UPDATE events."EventImages" ei
                    SET "IsPrimary" = true
                    WHERE ei."EventId" IN (SELECT "EventId" FROM events_without_primary)
                      AND ei."DisplayOrder" = 1;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208044133_FixEventImagePrimaryDataConsistency') THEN
    ALTER TABLE events."EventImages" ALTER COLUMN "IsPrimary" SET DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208044133_FixEventImagePrimaryDataConsistency') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251208044133_FixEventImagePrimaryDataConsistency', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208202922_FixEventImagePrimaryConstraint') THEN

                    UPDATE events."EventImages" ei
                    SET "IsPrimary" = false
                    WHERE ei."IsPrimary" = true
                      AND ei."EventId" IN (
                        -- Find events with multiple primary images
                        SELECT "EventId"
                        FROM events."EventImages"
                        WHERE "IsPrimary" = true
                        GROUP BY "EventId"
                        HAVING COUNT(*) > 1
                      )
                      AND ei."Id" NOT IN (
                        -- Keep the one with lowest DisplayOrder for each event
                        SELECT DISTINCT ON (ei2."EventId") ei2."Id"
                        FROM events."EventImages" ei2
                        WHERE ei2."IsPrimary" = true
                        ORDER BY ei2."EventId", ei2."DisplayOrder" ASC
                      );
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208202922_FixEventImagePrimaryConstraint') THEN

                    WITH events_with_images AS (
                        SELECT DISTINCT ei."EventId"
                        FROM events."EventImages" ei
                    ),
                    events_without_primary AS (
                        SELECT ewa."EventId"
                        FROM events_with_images ewa
                        WHERE NOT EXISTS (
                            SELECT 1
                            FROM events."EventImages" ei
                            WHERE ei."EventId" = ewa."EventId"
                              AND ei."IsPrimary" = true
                        )
                    ),
                    first_image_per_event AS (
                        SELECT DISTINCT ON (ei."EventId") ei."Id"
                        FROM events."EventImages" ei
                        WHERE ei."EventId" IN (SELECT "EventId" FROM events_without_primary)
                        ORDER BY ei."EventId", ei."DisplayOrder" ASC
                    )
                    UPDATE events."EventImages" ei
                    SET "IsPrimary" = true
                    WHERE ei."Id" IN (SELECT "Id" FROM first_image_per_event);
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208202922_FixEventImagePrimaryConstraint') THEN

                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_EventImages_EventId_IsPrimary_True"
                    ON events."EventImages" ("EventId")
                    WHERE "IsPrimary" = true;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251208202922_FixEventImagePrimaryConstraint') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251208202922_FixEventImagePrimaryConstraint', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211180710_AddTicketsTable_Phase6A24') THEN
    CREATE TABLE events.tickets (
        "Id" uuid NOT NULL,
        "RegistrationId" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "UserId" uuid,
        "TicketCode" character varying(50) NOT NULL,
        "QrCodeData" text NOT NULL,
        "PdfBlobUrl" character varying(500),
        "IsValid" boolean NOT NULL DEFAULT TRUE,
        "ValidatedAt" timestamp with time zone,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_tickets" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_tickets_events_EventId" FOREIGN KEY ("EventId") REFERENCES events.events ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_tickets_registrations_RegistrationId" FOREIGN KEY ("RegistrationId") REFERENCES events.registrations ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_tickets_users_UserId" FOREIGN KEY ("UserId") REFERENCES identity.users ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211180710_AddTicketsTable_Phase6A24') THEN
    CREATE INDEX "IX_tickets_EventId" ON events.tickets ("EventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211180710_AddTicketsTable_Phase6A24') THEN
    CREATE INDEX "IX_tickets_RegistrationId" ON events.tickets ("RegistrationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211180710_AddTicketsTable_Phase6A24') THEN
    CREATE UNIQUE INDEX "IX_tickets_TicketCode" ON events.tickets ("TicketCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211180710_AddTicketsTable_Phase6A24') THEN
    CREATE INDEX "IX_tickets_UserId" ON events.tickets ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211180710_AddTicketsTable_Phase6A24') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251211180710_AddTicketsTable_Phase6A24', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211184730_AddEmailGroups') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'badges') THEN
            CREATE SCHEMA badges;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211184730_AddEmailGroups') THEN
    CREATE TABLE badges.badges (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "ImageUrl" character varying(500) NOT NULL,
        "BlobName" character varying(255) NOT NULL,
        "Position" character varying(20) NOT NULL,
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "IsSystem" boolean NOT NULL DEFAULT FALSE,
        "DisplayOrder" integer NOT NULL,
        "CreatedByUserId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_badges" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211184730_AddEmailGroups') THEN
    CREATE TABLE badges.event_badges (
        "Id" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "BadgeId" uuid NOT NULL,
        "AssignedAt" timestamp with time zone NOT NULL,
        "AssignedByUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (NOW()),
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_event_badges" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_event_badges_badges_BadgeId" FOREIGN KEY ("BadgeId") REFERENCES badges.badges ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_event_badges_events_EventId" FOREIGN KEY ("EventId") REFERENCES events.events ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211184730_AddEmailGroups') THEN
    CREATE INDEX "IX_Badges_DisplayOrder" ON badges.badges ("DisplayOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211184730_AddEmailGroups') THEN
    CREATE INDEX "IX_Badges_IsActive" ON badges.badges ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211184730_AddEmailGroups') THEN
    CREATE INDEX "IX_Badges_IsSystem" ON badges.badges ("IsSystem");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211184730_AddEmailGroups') THEN
    CREATE UNIQUE INDEX "IX_Badges_Name" ON badges.badges ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211184730_AddEmailGroups') THEN
    CREATE INDEX "IX_EventBadges_BadgeId" ON badges.event_badges ("BadgeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211184730_AddEmailGroups') THEN
    CREATE INDEX "IX_EventBadges_EventId" ON badges.event_badges ("EventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211184730_AddEmailGroups') THEN
    CREATE UNIQUE INDEX "IX_EventBadges_EventId_BadgeId" ON badges.event_badges ("EventId", "BadgeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251211184730_AddEmailGroups') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251211184730_AddEmailGroups', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251212143334_AddEmailGroupsTable') THEN
    CREATE TABLE communications.email_groups (
        "Id" uuid NOT NULL,
        name character varying(200) NOT NULL,
        description character varying(500),
        owner_id uuid NOT NULL,
        email_addresses text NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at timestamp with time zone NOT NULL DEFAULT (NOW()),
        updated_at timestamp with time zone,
        CONSTRAINT "PK_email_groups" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251212143334_AddEmailGroupsTable') THEN
    CREATE INDEX "IX_EmailGroups_IsActive" ON communications.email_groups (is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251212143334_AddEmailGroupsTable') THEN
    CREATE INDEX "IX_EmailGroups_Owner_IsActive" ON communications.email_groups (owner_id, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251212143334_AddEmailGroupsTable') THEN
    CREATE UNIQUE INDEX "IX_EmailGroups_Owner_Name" ON communications.email_groups (owner_id, name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251212143334_AddEmailGroupsTable') THEN
    CREATE INDEX "IX_EmailGroups_OwnerId" ON communications.email_groups (owner_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251212143334_AddEmailGroupsTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251212143334_AddEmailGroupsTable', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251212210859_AddBadgeExpiryDate_Phase6A27') THEN
    ALTER TABLE badges.badges ADD "ExpiresAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251212210859_AddBadgeExpiryDate_Phase6A27') THEN
    CREATE INDEX "IX_Badges_ExpiresAt" ON badges.badges ("ExpiresAt") WHERE "ExpiresAt" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251212210859_AddBadgeExpiryDate_Phase6A27') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251212210859_AddBadgeExpiryDate_Phase6A27', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213010332_AddOpenItemsCategoryPhase6A27') THEN
    ALTER TABLE events.sign_up_lists ADD has_open_items boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213010332_AddOpenItemsCategoryPhase6A27') THEN
    ALTER TABLE events.sign_up_items ADD created_by_user_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213010332_AddOpenItemsCategoryPhase6A27') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251213010332_AddOpenItemsCategoryPhase6A27', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213032018_ConvertBadgeExpiryToDuration') THEN
    DROP INDEX badges."IX_Badges_ExpiresAt";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213032018_ConvertBadgeExpiryToDuration') THEN
    ALTER TABLE badges.badges DROP COLUMN "ExpiresAt";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213032018_ConvertBadgeExpiryToDuration') THEN
    ALTER TABLE badges.event_badges ADD "DurationDays" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213032018_ConvertBadgeExpiryToDuration') THEN
    ALTER TABLE badges.event_badges ADD "ExpiresAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213032018_ConvertBadgeExpiryToDuration') THEN
    ALTER TABLE badges.badges ADD "DefaultDurationDays" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213032018_ConvertBadgeExpiryToDuration') THEN
    CREATE INDEX "IX_EventBadges_ExpiresAt" ON badges.event_badges ("ExpiresAt") WHERE "ExpiresAt" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213032018_ConvertBadgeExpiryToDuration') THEN
    CREATE INDEX "IX_Badges_DefaultDurationDays" ON badges.badges ("DefaultDurationDays") WHERE "DefaultDurationDays" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251213032018_ConvertBadgeExpiryToDuration') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251213032018_ConvertBadgeExpiryToDuration', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216022927_AddHasOpenItemsToSignUpListsSafe') THEN

                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'events'
                            AND table_name = 'sign_up_lists'
                            AND column_name = 'has_open_items'
                        ) THEN
                            ALTER TABLE events.sign_up_lists
                            ADD COLUMN has_open_items boolean NOT NULL DEFAULT FALSE;
                        END IF;
                    END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216022927_AddHasOpenItemsToSignUpListsSafe') THEN
    ALTER TABLE badges.badges ADD position_x_detail numeric(5,4) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216022927_AddHasOpenItemsToSignUpListsSafe') THEN
    ALTER TABLE badges.badges ADD position_x_featured numeric(5,4) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216022927_AddHasOpenItemsToSignUpListsSafe') THEN
    ALTER TABLE badges.badges ADD position_x_listing numeric(5,4) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216022927_AddHasOpenItemsToSignUpListsSafe') THEN
    ALTER TABLE badges.badges ADD position_y_detail numeric(5,4) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216022927_AddHasOpenItemsToSignUpListsSafe') THEN
    ALTER TABLE badges.badges ADD position_y_featured numeric(5,4) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216022927_AddHasOpenItemsToSignUpListsSafe') THEN
    ALTER TABLE badges.badges ADD position_y_listing numeric(5,4) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216022927_AddHasOpenItemsToSignUpListsSafe') THEN
    ALTER TABLE badges.badges ADD rotation_detail numeric(5,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216022927_AddHasOpenItemsToSignUpListsSafe') THEN
    ALTER TABLE badges.badges ADD rotation_featured numeric(5,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216022927_AddHasOpenItemsToSignUpListsSafe') THEN
    ALTER TABLE badges.badges ADD rotation_listing numeric(5,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216022927_AddHasOpenItemsToSignUpListsSafe') THEN
    ALTER TABLE badges.badges ADD size_height_detail numeric(5,4) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216022927_AddHasOpenItemsToSignUpListsSafe') THEN
    ALTER TABLE badges.badges ADD size_height_featured numeric(5,4) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216022927_AddHasOpenItemsToSignUpListsSafe') THEN
    ALTER TABLE badges.badges ADD size_height_listing numeric(5,4) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216022927_AddHasOpenItemsToSignUpListsSafe') THEN
    ALTER TABLE badges.badges ADD size_width_detail numeric(5,4) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216022927_AddHasOpenItemsToSignUpListsSafe') THEN
    ALTER TABLE badges.badges ADD size_width_featured numeric(5,4) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216022927_AddHasOpenItemsToSignUpListsSafe') THEN
    ALTER TABLE badges.badges ADD size_width_listing numeric(5,4) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216022927_AddHasOpenItemsToSignUpListsSafe') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251216022927_AddHasOpenItemsToSignUpListsSafe', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216051336_AddEventEmailGroups') THEN
    CREATE TABLE event_email_groups (
        event_id uuid NOT NULL,
        email_group_id uuid NOT NULL,
        assigned_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        CONSTRAINT "PK_event_email_groups" PRIMARY KEY (event_id, email_group_id),
        CONSTRAINT "FK_event_email_groups_email_groups_email_group_id" FOREIGN KEY (email_group_id) REFERENCES communications.email_groups ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_event_email_groups_events_event_id" FOREIGN KEY (event_id) REFERENCES events.events ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216051336_AddEventEmailGroups') THEN
    CREATE INDEX "IX_event_email_groups_email_group_id" ON event_email_groups (email_group_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216051336_AddEventEmailGroups') THEN
    CREATE INDEX "IX_event_email_groups_event_id" ON event_email_groups (event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216051336_AddEventEmailGroups') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251216051336_AddEventEmailGroups', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216150703_UpdateBadgeLocationConfigsWithDefaults') THEN

                    UPDATE badges.badges
                    SET
                        position_x_listing = COALESCE(position_x_listing, 1.0),
                        position_y_listing = COALESCE(position_y_listing, 0.0),
                        size_width_listing = COALESCE(size_width_listing, 0.26),
                        size_height_listing = COALESCE(size_height_listing, 0.26),
                        rotation_listing = COALESCE(rotation_listing, 0.0),
                        position_x_featured = COALESCE(position_x_featured, 1.0),
                        position_y_featured = COALESCE(position_y_featured, 0.0),
                        size_width_featured = COALESCE(size_width_featured, 0.26),
                        size_height_featured = COALESCE(size_height_featured, 0.26),
                        rotation_featured = COALESCE(rotation_featured, 0.0),
                        position_x_detail = COALESCE(position_x_detail, 1.0),
                        position_y_detail = COALESCE(position_y_detail, 0.0),
                        size_width_detail = COALESCE(size_width_detail, 0.21),
                        size_height_detail = COALESCE(size_height_detail, 0.21),
                        rotation_detail = COALESCE(rotation_detail, 0.0)
                    WHERE
                        position_x_listing IS NULL OR position_y_listing IS NULL OR
                        size_width_listing IS NULL OR size_height_listing IS NULL OR rotation_listing IS NULL OR
                        position_x_featured IS NULL OR position_y_featured IS NULL OR
                        size_width_featured IS NULL OR size_height_featured IS NULL OR rotation_featured IS NULL OR
                        position_x_detail IS NULL OR position_y_detail IS NULL OR
                        size_width_detail IS NULL OR size_height_detail IS NULL OR rotation_detail IS NULL;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216150703_UpdateBadgeLocationConfigsWithDefaults') THEN
    ALTER TABLE badges.badges ALTER COLUMN size_width_listing SET DEFAULT 0.26;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216150703_UpdateBadgeLocationConfigsWithDefaults') THEN
    ALTER TABLE badges.badges ALTER COLUMN size_width_featured SET DEFAULT 0.26;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216150703_UpdateBadgeLocationConfigsWithDefaults') THEN
    ALTER TABLE badges.badges ALTER COLUMN size_width_detail SET DEFAULT 0.21;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216150703_UpdateBadgeLocationConfigsWithDefaults') THEN
    ALTER TABLE badges.badges ALTER COLUMN size_height_listing SET DEFAULT 0.26;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216150703_UpdateBadgeLocationConfigsWithDefaults') THEN
    ALTER TABLE badges.badges ALTER COLUMN size_height_featured SET DEFAULT 0.26;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216150703_UpdateBadgeLocationConfigsWithDefaults') THEN
    ALTER TABLE badges.badges ALTER COLUMN size_height_detail SET DEFAULT 0.21;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216150703_UpdateBadgeLocationConfigsWithDefaults') THEN
    ALTER TABLE badges.badges ALTER COLUMN rotation_listing SET DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216150703_UpdateBadgeLocationConfigsWithDefaults') THEN
    ALTER TABLE badges.badges ALTER COLUMN rotation_featured SET DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216150703_UpdateBadgeLocationConfigsWithDefaults') THEN
    ALTER TABLE badges.badges ALTER COLUMN rotation_detail SET DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216150703_UpdateBadgeLocationConfigsWithDefaults') THEN
    ALTER TABLE badges.badges ALTER COLUMN position_y_listing SET DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216150703_UpdateBadgeLocationConfigsWithDefaults') THEN
    ALTER TABLE badges.badges ALTER COLUMN position_y_featured SET DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216150703_UpdateBadgeLocationConfigsWithDefaults') THEN
    ALTER TABLE badges.badges ALTER COLUMN position_y_detail SET DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216150703_UpdateBadgeLocationConfigsWithDefaults') THEN
    ALTER TABLE badges.badges ALTER COLUMN position_x_listing SET DEFAULT 1.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216150703_UpdateBadgeLocationConfigsWithDefaults') THEN
    ALTER TABLE badges.badges ALTER COLUMN position_x_featured SET DEFAULT 1.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216150703_UpdateBadgeLocationConfigsWithDefaults') THEN
    ALTER TABLE badges.badges ALTER COLUMN position_x_detail SET DEFAULT 1.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251216150703_UpdateBadgeLocationConfigsWithDefaults') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251216150703_UpdateBadgeLocationConfigsWithDefaults', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217053649_ApplyBadgeLocationConfigDefaults') THEN

                    UPDATE badges.badges
                    SET
                        position_x_listing = COALESCE(position_x_listing, 1.0),
                        position_y_listing = COALESCE(position_y_listing, 0.0),
                        size_width_listing = COALESCE(size_width_listing, 0.26),
                        size_height_listing = COALESCE(size_height_listing, 0.26),
                        rotation_listing = COALESCE(rotation_listing, 0.0),
                        position_x_featured = COALESCE(position_x_featured, 1.0),
                        position_y_featured = COALESCE(position_y_featured, 0.0),
                        size_width_featured = COALESCE(size_width_featured, 0.26),
                        size_height_featured = COALESCE(size_height_featured, 0.26),
                        rotation_featured = COALESCE(rotation_featured, 0.0),
                        position_x_detail = COALESCE(position_x_detail, 1.0),
                        position_y_detail = COALESCE(position_y_detail, 0.0),
                        size_width_detail = COALESCE(size_width_detail, 0.21),
                        size_height_detail = COALESCE(size_height_detail, 0.21),
                        rotation_detail = COALESCE(rotation_detail, 0.0)
                    WHERE
                        position_x_listing IS NULL OR position_y_listing IS NULL OR
                        size_width_listing IS NULL OR size_height_listing IS NULL OR rotation_listing IS NULL OR
                        position_x_featured IS NULL OR position_y_featured IS NULL OR
                        size_width_featured IS NULL OR size_height_featured IS NULL OR rotation_featured IS NULL OR
                        position_x_detail IS NULL OR position_y_detail IS NULL OR
                        size_width_detail IS NULL OR size_height_detail IS NULL OR rotation_detail IS NULL;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217053649_ApplyBadgeLocationConfigDefaults') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251217053649_ApplyBadgeLocationConfigDefaults', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217150538_AddRefreshTokenPrimaryKey_Phase6A33') THEN
    ALTER TABLE identity.user_refresh_tokens DROP CONSTRAINT "PK_user_refresh_tokens";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217150538_AddRefreshTokenPrimaryKey_Phase6A33') THEN
    ALTER TABLE identity.user_refresh_tokens ADD CONSTRAINT "PK_user_refresh_tokens" PRIMARY KEY ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217150538_AddRefreshTokenPrimaryKey_Phase6A33') THEN
    CREATE INDEX "IX_user_refresh_tokens_UserId" ON identity.user_refresh_tokens ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217150538_AddRefreshTokenPrimaryKey_Phase6A33') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251217150538_AddRefreshTokenPrimaryKey_Phase6A33', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217175941_EnforceBadgeLocationConfigNotNull') THEN

                    UPDATE badges.badges
                    SET
                        position_x_listing = COALESCE(position_x_listing, 1.0),
                        position_y_listing = COALESCE(position_y_listing, 0.0),
                        size_width_listing = COALESCE(size_width_listing, 0.26),
                        size_height_listing = COALESCE(size_height_listing, 0.26),
                        rotation_listing = COALESCE(rotation_listing, 0.0),
                        position_x_featured = COALESCE(position_x_featured, 1.0),
                        position_y_featured = COALESCE(position_y_featured, 0.0),
                        size_width_featured = COALESCE(size_width_featured, 0.26),
                        size_height_featured = COALESCE(size_height_featured, 0.26),
                        rotation_featured = COALESCE(rotation_featured, 0.0),
                        position_x_detail = COALESCE(position_x_detail, 1.0),
                        position_y_detail = COALESCE(position_y_detail, 0.0),
                        size_width_detail = COALESCE(size_width_detail, 0.21),
                        size_height_detail = COALESCE(size_height_detail, 0.21),
                        rotation_detail = COALESCE(rotation_detail, 0.0);
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217175941_EnforceBadgeLocationConfigNotNull') THEN
    UPDATE badges.badges SET position_x_listing = 1.0 WHERE position_x_listing IS NULL;
    ALTER TABLE badges.badges ALTER COLUMN position_x_listing SET NOT NULL;
    ALTER TABLE badges.badges ALTER COLUMN position_x_listing SET DEFAULT 1.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217175941_EnforceBadgeLocationConfigNotNull') THEN
    UPDATE badges.badges SET position_y_listing = 0.0 WHERE position_y_listing IS NULL;
    ALTER TABLE badges.badges ALTER COLUMN position_y_listing SET NOT NULL;
    ALTER TABLE badges.badges ALTER COLUMN position_y_listing SET DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217175941_EnforceBadgeLocationConfigNotNull') THEN
    UPDATE badges.badges SET size_width_listing = 0.26 WHERE size_width_listing IS NULL;
    ALTER TABLE badges.badges ALTER COLUMN size_width_listing SET NOT NULL;
    ALTER TABLE badges.badges ALTER COLUMN size_width_listing SET DEFAULT 0.26;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217175941_EnforceBadgeLocationConfigNotNull') THEN
    UPDATE badges.badges SET size_height_listing = 0.26 WHERE size_height_listing IS NULL;
    ALTER TABLE badges.badges ALTER COLUMN size_height_listing SET NOT NULL;
    ALTER TABLE badges.badges ALTER COLUMN size_height_listing SET DEFAULT 0.26;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217175941_EnforceBadgeLocationConfigNotNull') THEN
    UPDATE badges.badges SET rotation_listing = 0.0 WHERE rotation_listing IS NULL;
    ALTER TABLE badges.badges ALTER COLUMN rotation_listing SET NOT NULL;
    ALTER TABLE badges.badges ALTER COLUMN rotation_listing SET DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217175941_EnforceBadgeLocationConfigNotNull') THEN
    UPDATE badges.badges SET position_x_featured = 1.0 WHERE position_x_featured IS NULL;
    ALTER TABLE badges.badges ALTER COLUMN position_x_featured SET NOT NULL;
    ALTER TABLE badges.badges ALTER COLUMN position_x_featured SET DEFAULT 1.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217175941_EnforceBadgeLocationConfigNotNull') THEN
    UPDATE badges.badges SET position_y_featured = 0.0 WHERE position_y_featured IS NULL;
    ALTER TABLE badges.badges ALTER COLUMN position_y_featured SET NOT NULL;
    ALTER TABLE badges.badges ALTER COLUMN position_y_featured SET DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217175941_EnforceBadgeLocationConfigNotNull') THEN
    UPDATE badges.badges SET size_width_featured = 0.26 WHERE size_width_featured IS NULL;
    ALTER TABLE badges.badges ALTER COLUMN size_width_featured SET NOT NULL;
    ALTER TABLE badges.badges ALTER COLUMN size_width_featured SET DEFAULT 0.26;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217175941_EnforceBadgeLocationConfigNotNull') THEN
    UPDATE badges.badges SET size_height_featured = 0.26 WHERE size_height_featured IS NULL;
    ALTER TABLE badges.badges ALTER COLUMN size_height_featured SET NOT NULL;
    ALTER TABLE badges.badges ALTER COLUMN size_height_featured SET DEFAULT 0.26;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217175941_EnforceBadgeLocationConfigNotNull') THEN
    UPDATE badges.badges SET rotation_featured = 0.0 WHERE rotation_featured IS NULL;
    ALTER TABLE badges.badges ALTER COLUMN rotation_featured SET NOT NULL;
    ALTER TABLE badges.badges ALTER COLUMN rotation_featured SET DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217175941_EnforceBadgeLocationConfigNotNull') THEN
    UPDATE badges.badges SET position_x_detail = 1.0 WHERE position_x_detail IS NULL;
    ALTER TABLE badges.badges ALTER COLUMN position_x_detail SET NOT NULL;
    ALTER TABLE badges.badges ALTER COLUMN position_x_detail SET DEFAULT 1.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217175941_EnforceBadgeLocationConfigNotNull') THEN
    UPDATE badges.badges SET position_y_detail = 0.0 WHERE position_y_detail IS NULL;
    ALTER TABLE badges.badges ALTER COLUMN position_y_detail SET NOT NULL;
    ALTER TABLE badges.badges ALTER COLUMN position_y_detail SET DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217175941_EnforceBadgeLocationConfigNotNull') THEN
    UPDATE badges.badges SET size_width_detail = 0.21 WHERE size_width_detail IS NULL;
    ALTER TABLE badges.badges ALTER COLUMN size_width_detail SET NOT NULL;
    ALTER TABLE badges.badges ALTER COLUMN size_width_detail SET DEFAULT 0.21;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217175941_EnforceBadgeLocationConfigNotNull') THEN
    UPDATE badges.badges SET size_height_detail = 0.21 WHERE size_height_detail IS NULL;
    ALTER TABLE badges.badges ALTER COLUMN size_height_detail SET NOT NULL;
    ALTER TABLE badges.badges ALTER COLUMN size_height_detail SET DEFAULT 0.21;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217175941_EnforceBadgeLocationConfigNotNull') THEN
    UPDATE badges.badges SET rotation_detail = 0.0 WHERE rotation_detail IS NULL;
    ALTER TABLE badges.badges ALTER COLUMN rotation_detail SET NOT NULL;
    ALTER TABLE badges.badges ALTER COLUMN rotation_detail SET DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217175941_EnforceBadgeLocationConfigNotNull') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251217175941_EnforceBadgeLocationConfigNotNull', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217205258_FixBadgeNullsDataOnly') THEN

                    UPDATE badges.badges
                    SET
                        position_x_listing = COALESCE(position_x_listing, 1.0),
                        position_y_listing = COALESCE(position_y_listing, 0.0),
                        size_width_listing = COALESCE(size_width_listing, 0.26),
                        size_height_listing = COALESCE(size_height_listing, 0.26),
                        rotation_listing = COALESCE(rotation_listing, 0.0),
                        position_x_featured = COALESCE(position_x_featured, 1.0),
                        position_y_featured = COALESCE(position_y_featured, 0.0),
                        size_width_featured = COALESCE(size_width_featured, 0.26),
                        size_height_featured = COALESCE(size_height_featured, 0.26),
                        rotation_featured = COALESCE(rotation_featured, 0.0),
                        position_x_detail = COALESCE(position_x_detail, 1.0),
                        position_y_detail = COALESCE(position_y_detail, 0.0),
                        size_width_detail = COALESCE(size_width_detail, 0.21),
                        size_height_detail = COALESCE(size_height_detail, 0.21),
                        rotation_detail = COALESCE(rotation_detail, 0.0);
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251217205258_FixBadgeNullsDataOnly') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251217205258_FixBadgeNullsDataOnly', '8.0.19');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251218044022_FixBadgeLocationConfigZeroValues') THEN

                    UPDATE badges.badges
                    SET
                        -- Listing config: TopRight (x=1.0, y=0.0) with 26% size
                        position_x_listing = 1.0,
                        position_y_listing = 0.0,
                        size_width_listing = 0.26,
                        size_height_listing = 0.26,
                        rotation_listing = 0.0,

                        -- Featured config: TopRight (x=1.0, y=0.0) with 26% size
                        position_x_featured = 1.0,
                        position_y_featured = 0.0,
                        size_width_featured = 0.26,
                        size_height_featured = 0.26,
                        rotation_featured = 0.0,

                        -- Detail config: TopRight (x=1.0, y=0.0) with 21% size (smaller for large images)
                        position_x_detail = 1.0,
                        position_y_detail = 0.0,
                        size_width_detail = 0.21,
                        size_height_detail = 0.21,
                        rotation_detail = 0.0,

                        "UpdatedAt" = NOW()
                    WHERE
                        -- Only update badges with incorrect zero values (result of NULL->NOT NULL conversion)
                        position_x_listing = 0 OR size_width_listing = 0;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251218044022_FixBadgeLocationConfigZeroValues') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251218044022_FixBadgeLocationConfigZeroValues', '8.0.19');
    END IF;
END $EF$;
COMMIT;

