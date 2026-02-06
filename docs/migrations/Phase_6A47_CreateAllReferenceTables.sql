-- Phase 6A.47: Create All Reference Tables
-- Purpose: Eliminate ALL enum hardcoding by migrating to database-driven reference data
-- Migration Date: 2025-12-26
-- Author: Claude Code

-- ============================================================================
-- REFERENCE TABLE SCHEMA PATTERN
-- ============================================================================
-- All reference tables follow this pattern:
--   - id (UUID primary key)
--   - code (VARCHAR unique - maps to enum name for backward compatibility)
--   - name (VARCHAR - display name)
--   - description (TEXT - optional detailed description)
--   - display_order (INTEGER - for UI sorting)
--   - is_active (BOOLEAN - soft delete capability)
--   - metadata (JSONB - extensibility for custom properties)
--   - created_at, updated_at (TIMESTAMP)
-- ============================================================================

BEGIN;

-- ============================================================================
-- 1. EVENT CATEGORIES (replaces EventCategory enum + Cultural Interests)
-- ============================================================================
CREATE TABLE IF NOT EXISTS event_categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    icon VARCHAR(50),
    display_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_event_categories_active ON event_categories(is_active);
CREATE INDEX idx_event_categories_order ON event_categories(display_order);

-- ============================================================================
-- 2. EVENT STATUSES (replaces EventStatus enum)
-- ============================================================================
CREATE TABLE IF NOT EXISTS event_statuses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    display_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT true,

    -- Business logic flags
    allows_registration BOOLEAN NOT NULL DEFAULT false,
    allows_editing BOOLEAN NOT NULL DEFAULT false,
    allows_cancellation BOOLEAN NOT NULL DEFAULT false,
    is_terminal_state BOOLEAN NOT NULL DEFAULT false,

    -- State machine transitions (JSONB array of allowed next states)
    allowed_transitions JSONB,

    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_event_statuses_active ON event_statuses(is_active);

-- ============================================================================
-- 3. REGISTRATION STATUSES (replaces RegistrationStatus enum)
-- ============================================================================
CREATE TABLE IF NOT EXISTS registration_statuses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    display_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT true,

    -- Business logic flags
    is_confirmed BOOLEAN NOT NULL DEFAULT false,
    requires_payment BOOLEAN NOT NULL DEFAULT false,
    allows_cancellation BOOLEAN NOT NULL DEFAULT false,
    is_terminal_state BOOLEAN NOT NULL DEFAULT false,

    -- State machine transitions
    allowed_transitions JSONB,

    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_registration_statuses_active ON registration_statuses(is_active);

-- ============================================================================
-- 4. PAYMENT STATUSES (replaces PaymentStatus enum)
-- ============================================================================
CREATE TABLE IF NOT EXISTS payment_statuses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    display_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT true,

    -- Business logic flags
    is_successful BOOLEAN NOT NULL DEFAULT false,
    allows_refund BOOLEAN NOT NULL DEFAULT false,
    is_terminal_state BOOLEAN NOT NULL DEFAULT false,

    -- State machine transitions
    allowed_transitions JSONB,

    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_payment_statuses_active ON payment_statuses(is_active);

-- ============================================================================
-- 5. ROLES (replaces UserRole enum)
-- ============================================================================
CREATE TABLE IF NOT EXISTS roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    display_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_system_role BOOLEAN NOT NULL DEFAULT false, -- Cannot be deleted
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_roles_active ON roles(is_active);

-- ============================================================================
-- 6. PERMISSIONS (new table for role-based access control)
-- ============================================================================
CREATE TABLE IF NOT EXISTS permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(100) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    resource VARCHAR(50) NOT NULL, -- e.g., "Event", "User", "Payment"
    action VARCHAR(50) NOT NULL,   -- e.g., "Create", "Read", "Update", "Delete"
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_permissions_active ON permissions(is_active);
CREATE INDEX idx_permissions_resource ON permissions(resource);

-- ============================================================================
-- 7. ROLE_PERMISSIONS (many-to-many relationship)
-- ============================================================================
CREATE TABLE IF NOT EXISTS role_permissions (
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    permission_id UUID NOT NULL REFERENCES permissions(id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    PRIMARY KEY (role_id, permission_id)
);

CREATE INDEX idx_role_permissions_role ON role_permissions(role_id);
CREATE INDEX idx_role_permissions_permission ON role_permissions(permission_id);

-- ============================================================================
-- 8. CURRENCIES (replaces Currency enum)
-- ============================================================================
CREATE TABLE IF NOT EXISTS currencies (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(3) NOT NULL UNIQUE, -- ISO 4217 code (USD, EUR, etc.)
    name VARCHAR(100) NOT NULL,
    symbol VARCHAR(10) NOT NULL,
    display_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_currencies_active ON currencies(is_active);

-- ============================================================================
-- 9. GENDERS (replaces Gender enum)
-- ============================================================================
CREATE TABLE IF NOT EXISTS genders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    display_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_genders_active ON genders(is_active);

-- ============================================================================
-- 10. AGE CATEGORIES (replaces AgeCategory enum)
-- ============================================================================
CREATE TABLE IF NOT EXISTS age_categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    display_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_age_categories_active ON age_categories(is_active);

-- ============================================================================
-- 11. SIGNUP ITEM CATEGORIES (replaces SignUpItemCategory enum)
-- ============================================================================
CREATE TABLE IF NOT EXISTS signup_item_categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    display_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_signup_item_categories_active ON signup_item_categories(is_active);

-- ============================================================================
-- 12. PRICING TYPES (replaces PricingType enum)
-- ============================================================================
CREATE TABLE IF NOT EXISTS pricing_types (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    display_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_pricing_types_active ON pricing_types(is_active);

-- ============================================================================
-- 13. LANGUAGES (replaces hardcoded language list)
-- ============================================================================
CREATE TABLE IF NOT EXISTS languages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(10) NOT NULL UNIQUE, -- ISO 639-1 code (en, si, ta, etc.)
    name VARCHAR(100) NOT NULL,
    native_name VARCHAR(100),
    display_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_languages_active ON languages(is_active);

-- ============================================================================
-- 14. STATES (replaces hardcoded US states list)
-- ============================================================================
CREATE TABLE IF NOT EXISTS states (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(2) NOT NULL UNIQUE, -- Two-letter state code (CA, NY, etc.)
    name VARCHAR(100) NOT NULL,
    country_code VARCHAR(2) NOT NULL DEFAULT 'US',
    display_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_states_active ON states(is_active);
CREATE INDEX idx_states_country ON states(country_code);

-- ============================================================================
-- 15. SYSTEM SETTINGS (replaces magic numbers and configuration hardcoding)
-- ============================================================================
CREATE TABLE IF NOT EXISTS system_settings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    key VARCHAR(100) NOT NULL UNIQUE,
    value TEXT NOT NULL,
    value_type VARCHAR(20) NOT NULL, -- 'string', 'integer', 'boolean', 'json'
    description TEXT,
    category VARCHAR(50), -- 'upload_limits', 'pagination', 'email', etc.
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_system_settings_active ON system_settings(is_active);
CREATE INDEX idx_system_settings_category ON system_settings(category);

-- ============================================================================
-- 16. FILE TYPE RESTRICTIONS (replaces hardcoded allowed file types)
-- ============================================================================
CREATE TABLE IF NOT EXISTS file_type_restrictions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    context VARCHAR(50) NOT NULL, -- 'event_image', 'event_video', 'profile_avatar', etc.
    file_extension VARCHAR(10) NOT NULL,
    mime_type VARCHAR(100) NOT NULL,
    max_size_bytes BIGINT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    UNIQUE(context, file_extension)
);

CREATE INDEX idx_file_type_restrictions_context ON file_type_restrictions(context);
CREATE INDEX idx_file_type_restrictions_active ON file_type_restrictions(is_active);

-- ============================================================================
-- 17. EMAIL TEMPLATES (replaces hardcoded email templates in code)
-- ============================================================================
CREATE TABLE IF NOT EXISTS email_templates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(100) NOT NULL UNIQUE,
    name VARCHAR(200) NOT NULL,
    subject VARCHAR(500) NOT NULL,
    body_html TEXT NOT NULL,
    body_text TEXT,
    template_variables JSONB, -- Array of variable names like ["userName", "eventName"]
    category VARCHAR(50), -- 'event', 'registration', 'payment', etc.
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_email_templates_active ON email_templates(is_active);
CREATE INDEX idx_email_templates_category ON email_templates(category);

COMMIT;
