-- Apply AddEventVideos migration to staging database manually
-- Migration: 20251204051150_AddEventVideos
-- Purpose: Create EventVideos table for event video gallery feature
-- Updated: 2025-12-04 to match correct migration ID

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'events' AND table_name = 'EventVideos'
    ) THEN
        -- Create EventVideos table in events schema
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
            "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
            "UpdatedAt" timestamp with time zone,
            CONSTRAINT "PK_EventVideos" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_EventVideos_events_EventId" FOREIGN KEY ("EventId")
                REFERENCES events.events ("Id") ON DELETE CASCADE
        );

        -- Create index on EventId for FK lookups
        CREATE INDEX "IX_EventVideos_EventId" ON events."EventVideos" ("EventId");

        -- Create unique index on EventId + DisplayOrder
        CREATE UNIQUE INDEX "IX_EventVideos_EventId_DisplayOrder"
            ON events."EventVideos" ("EventId", "DisplayOrder");

        -- Record migration in history
        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20251204051150_AddEventVideos', '8.0.0');

        RAISE NOTICE 'EventVideos table created successfully in events schema';
    ELSE
        RAISE NOTICE 'EventVideos table already exists, skipping';
    END IF;
END $$;