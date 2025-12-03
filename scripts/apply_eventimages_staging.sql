-- Apply AddEventImages migration to staging database
-- Migration: 20251103040053_AddEventImages
-- Purpose: Create EventImages table for event image gallery feature

-- Check if migration already applied
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "__EFMigrationsHistory"
        WHERE "MigrationId" = '20251103040053_AddEventImages'
    ) THEN
        -- Create EventImages table
        CREATE TABLE events."EventImages" (
            "Id" uuid NOT NULL,
            "EventId" uuid NOT NULL,
            "ImageUrl" character varying(500) NOT NULL,
            "BlobName" character varying(255) NOT NULL,
            "DisplayOrder" integer NOT NULL,
            "UploadedAt" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_EventImages" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_EventImages_events_EventId" FOREIGN KEY ("EventId")
                REFERENCES events.events ("Id") ON DELETE CASCADE
        );

        -- Create index on EventId for FK lookups
        CREATE INDEX "IX_EventImages_EventId" ON events."EventImages" ("EventId");

        -- Create unique index on EventId + DisplayOrder to prevent duplicate ordering
        CREATE UNIQUE INDEX "IX_EventImages_EventId_DisplayOrder"
            ON events."EventImages" ("EventId", "DisplayOrder");

        -- Record migration in history
        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20251103040053_AddEventImages', '8.0.0');

        RAISE NOTICE 'Migration 20251103040053_AddEventImages applied successfully';
    ELSE
        RAISE NOTICE 'Migration 20251103040053_AddEventImages already applied, skipping';
    END IF;
END $$;
