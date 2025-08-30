-- LankaConnect Database Initialization Script
-- This script creates the necessary extensions, schemas, and types for the application

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Create custom types
CREATE TYPE user_status AS ENUM ('Active', 'Inactive', 'Suspended', 'Deleted');
CREATE TYPE event_status AS ENUM ('Draft', 'Published', 'Cancelled', 'Completed');
CREATE TYPE booking_status AS ENUM ('Pending', 'Confirmed', 'Cancelled', 'Completed');
CREATE TYPE membership_tier AS ENUM ('Free', 'Premium', 'SuperPremium', 'Family');

-- Create schemas for bounded contexts
CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS events;
CREATE SCHEMA IF NOT EXISTS community;
CREATE SCHEMA IF NOT EXISTS business;
CREATE SCHEMA IF NOT EXISTS content;
CREATE SCHEMA IF NOT EXISTS audit;

-- Grant permissions to the postgres user (will be changed in production)
GRANT ALL PRIVILEGES ON SCHEMA identity TO postgres;
GRANT ALL PRIVILEGES ON SCHEMA events TO postgres;
GRANT ALL PRIVILEGES ON SCHEMA community TO postgres;
GRANT ALL PRIVILEGES ON SCHEMA business TO postgres;
GRANT ALL PRIVILEGES ON SCHEMA content TO postgres;
GRANT ALL PRIVILEGES ON SCHEMA audit TO postgres;

-- Create a simple health check table
CREATE TABLE public.health_check (
    id SERIAL PRIMARY KEY,
    service_name VARCHAR(100) NOT NULL,
    last_check TIMESTAMP DEFAULT NOW()
);

-- Insert initial health check record
INSERT INTO public.health_check (service_name) VALUES ('database');

-- Log initialization completion
DO $$
BEGIN
    RAISE NOTICE 'LankaConnect database initialization completed successfully';
END
$$;