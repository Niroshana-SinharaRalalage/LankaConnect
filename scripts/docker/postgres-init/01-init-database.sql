-- Initialize LankaConnect Database
-- This script runs when the PostgreSQL container starts for the first time

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "citext";

-- Create schemas for different bounded contexts
CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS events;
CREATE SCHEMA IF NOT EXISTS community;
CREATE SCHEMA IF NOT EXISTS business;
CREATE SCHEMA IF NOT EXISTS content;
CREATE SCHEMA IF NOT EXISTS shared;

-- Create database roles
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'lankaconnect_app') THEN
        CREATE ROLE lankaconnect_app WITH LOGIN PASSWORD 'app_password_123';
    END IF;
    
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'lankaconnect_readonly') THEN
        CREATE ROLE lankaconnect_readonly WITH LOGIN PASSWORD 'readonly_password_123';
    END IF;
END
$$;

-- Grant permissions
GRANT CONNECT ON DATABASE "LankaConnectDB" TO lankaconnect_app;
GRANT CONNECT ON DATABASE "LankaConnectDB" TO lankaconnect_readonly;

-- Grant schema usage
GRANT USAGE ON SCHEMA identity TO lankaconnect_app, lankaconnect_readonly;
GRANT USAGE ON SCHEMA events TO lankaconnect_app, lankaconnect_readonly;
GRANT USAGE ON SCHEMA community TO lankaconnect_app, lankaconnect_readonly;
GRANT USAGE ON SCHEMA business TO lankaconnect_app, lankaconnect_readonly;
GRANT USAGE ON SCHEMA content TO lankaconnect_app, lankaconnect_readonly;
GRANT USAGE ON SCHEMA shared TO lankaconnect_app, lankaconnect_readonly;

-- Grant permissions to app role
GRANT CREATE ON SCHEMA identity TO lankaconnect_app;
GRANT CREATE ON SCHEMA events TO lankaconnect_app;
GRANT CREATE ON SCHEMA community TO lankaconnect_app;
GRANT CREATE ON SCHEMA business TO lankaconnect_app;
GRANT CREATE ON SCHEMA content TO lankaconnect_app;
GRANT CREATE ON SCHEMA shared TO lankaconnect_app;

-- Create audit table for tracking changes
CREATE TABLE IF NOT EXISTS shared.audit_log (
    id uuid DEFAULT uuid_generate_v4() PRIMARY KEY,
    table_name varchar(100) NOT NULL,
    operation varchar(10) NOT NULL,
    old_values jsonb,
    new_values jsonb,
    user_id uuid,
    created_at timestamp with time zone DEFAULT now()
);

-- Create sequence for entity IDs (if needed for non-UUID scenarios)
CREATE SEQUENCE IF NOT EXISTS shared.entity_id_seq;

-- Set default privileges for future tables
ALTER DEFAULT PRIVILEGES IN SCHEMA identity GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO lankaconnect_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA events GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO lankaconnect_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA community GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO lankaconnect_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA business GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO lankaconnect_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA content GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO lankaconnect_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA shared GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO lankaconnect_app;

ALTER DEFAULT PRIVILEGES IN SCHEMA identity GRANT SELECT ON TABLES TO lankaconnect_readonly;
ALTER DEFAULT PRIVILEGES IN SCHEMA events GRANT SELECT ON TABLES TO lankaconnect_readonly;
ALTER DEFAULT PRIVILEGES IN SCHEMA community GRANT SELECT ON TABLES TO lankaconnect_readonly;
ALTER DEFAULT PRIVILEGES IN SCHEMA business GRANT SELECT ON TABLES TO lankaconnect_readonly;
ALTER DEFAULT PRIVILEGES IN SCHEMA content GRANT SELECT ON TABLES TO lankaconnect_readonly;
ALTER DEFAULT PRIVILEGES IN SCHEMA shared GRANT SELECT ON TABLES TO lankaconnect_readonly;

-- Create a test connectivity function
CREATE OR REPLACE FUNCTION shared.test_connection()
RETURNS text
LANGUAGE sql
AS $$
    SELECT 'LankaConnect database connection successful at ' || now()::text;
$$;

-- Insert initial test data
INSERT INTO shared.audit_log (table_name, operation, new_values, created_at)
VALUES ('database', 'INIT', '{"message": "Database initialized successfully"}', now())
ON CONFLICT DO NOTHING;