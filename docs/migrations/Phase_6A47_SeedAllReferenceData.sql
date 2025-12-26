-- Phase 6A.47: Seed All Reference Data
-- Purpose: Populate reference tables with data from existing enums and hardcoded lists
-- Migration Date: 2025-12-26
-- Author: Claude Code

BEGIN;

-- ============================================================================
-- 1. EVENT CATEGORIES
-- Combines original EventCategory enum (8 values) + Cultural Interests (20 values)
-- ============================================================================

-- Original 8 Event Categories from EventCategory enum
INSERT INTO event_categories (code, name, description, icon, display_order) VALUES
('RELIGIOUS', 'Religious', 'Religious ceremonies and celebrations', 'church', 1),
('CULTURAL', 'Cultural', 'Cultural events and traditions', 'users', 2),
('COMMUNITY', 'Community', 'Community gatherings and activities', 'home', 3),
('EDUCATIONAL', 'Educational', 'Educational programs and workshops', 'book', 4),
('SOCIAL', 'Social', 'Social gatherings and networking', 'coffee', 5),
('BUSINESS', 'Business', 'Business and professional events', 'briefcase', 6),
('CHARITY', 'Charity', 'Charitable events and fundraisers', 'heart', 7),
('ENTERTAINMENT', 'Entertainment', 'Entertainment and recreational activities', 'music', 8);

-- Additional Cultural Interest categories (merged from profile.constants.ts)
INSERT INTO event_categories (code, name, description, icon, display_order) VALUES
('SL_CUISINE', 'Sri Lankan Cuisine', 'Cooking classes, food festivals, traditional recipes', 'utensils', 9),
('BUDDHIST_FEST', 'Buddhist Festivals & Traditions', 'Vesak, Poson, temple events', 'lotus', 10),
('SINHALA_LANG', 'Sinhala Language & Literature', 'Language classes, poetry, literature', 'book-open', 11),
('TAMIL_LANG', 'Tamil Language & Literature', 'Language classes, poetry, literature', 'book-open', 12),
('TRADITIONAL_MUSIC', 'Traditional Music & Dance', 'Classical music, Kandyan dance, folk performances', 'music', 13),
('AYURVEDA', 'Ayurveda & Traditional Medicine', 'Wellness practices, herbal medicine', 'leaf', 14),
('CRICKET', 'Cricket', 'Matches, leagues, watch parties', 'activity', 15),
('ARTS_CRAFTS', 'Arts & Crafts', 'Traditional crafts, batik, woodcarving', 'palette', 16),
('HINDU_FEST', 'Hindu Festivals & Traditions', 'Deepavali, Thai Pongal, temple events', 'star', 17),
('MUSLIM_FEST', 'Muslim Festivals & Traditions', 'Ramadan, Eid celebrations', 'moon', 18),
('CHRISTIAN_FEST', 'Christian Festivals & Traditions', 'Christmas, Easter celebrations', 'gift', 19),
('TEA_CULTURE', 'Tea Culture', 'Ceylon tea tastings, plantation tours', 'coffee', 20),
('YOUTH_DEV', 'Youth Development', 'Programs for Sri Lankan youth', 'users', 21),
('FAMILY_VALUES', 'Family & Community Values', 'Traditional family events', 'home', 22),
('SL_HISTORY', 'Sri Lankan History & Heritage', 'Cultural heritage education', 'landmark', 23),
('PROF_NETWORK', 'Professional Networking', 'Career development for Sri Lankan professionals', 'briefcase', 24),
('ENVIRONMENTAL', 'Environmental Conservation', 'Eco-friendly initiatives', 'globe', 25),
('SINHALA_NEW_YEAR', 'Sinhala & Tamil New Year', 'New Year celebrations and traditions', 'calendar', 26),
('SPORTS_FITNESS', 'Sports & Fitness', 'Athletic activities beyond cricket', 'dumbbell', 27),
('TECH_INNOVATION', 'Technology & Innovation', 'Tech meetups for Sri Lankan diaspora', 'cpu', 28);

-- ============================================================================
-- 2. EVENT STATUSES
-- From EventStatus enum (8 values)
-- ============================================================================
INSERT INTO event_statuses (
    code, name, description, display_order,
    allows_registration, allows_editing, allows_cancellation, is_terminal_state,
    allowed_transitions
) VALUES
('DRAFT', 'Draft', 'Event is being created', 1, false, true, true, false,
 '["PUBLISHED", "CANCELLED"]'::jsonb),

('PUBLISHED', 'Published', 'Event is live and accepting registrations', 2, true, true, true, false,
 '["REGISTRATION_CLOSED", "CANCELLED", "ONGOING"]'::jsonb),

('REGISTRATION_CLOSED', 'Registration Closed', 'No longer accepting new registrations', 3, false, true, true, false,
 '["ONGOING", "CANCELLED"]'::jsonb),

('ONGOING', 'Ongoing', 'Event is currently happening', 4, false, false, false, false,
 '["COMPLETED", "CANCELLED"]'::jsonb),

('COMPLETED', 'Completed', 'Event has finished successfully', 5, false, false, false, true,
 '[]'::jsonb),

('CANCELLED', 'Cancelled', 'Event was cancelled', 6, false, false, false, true,
 '[]'::jsonb),

('POSTPONED', 'Postponed', 'Event is postponed to a future date', 7, false, true, true, false,
 '["PUBLISHED", "CANCELLED"]'::jsonb),

('SOLD_OUT', 'Sold Out', 'All capacity filled', 8, false, true, true, false,
 '["ONGOING", "CANCELLED"]'::jsonb);

-- ============================================================================
-- 3. REGISTRATION STATUSES
-- From RegistrationStatus enum (7 values)
-- ============================================================================
INSERT INTO registration_statuses (
    code, name, description, display_order,
    is_confirmed, requires_payment, allows_cancellation, is_terminal_state,
    allowed_transitions
) VALUES
('PENDING', 'Pending', 'Registration initiated but not confirmed', 1, false, true, true, false,
 '["CONFIRMED", "PAYMENT_PENDING", "CANCELLED"]'::jsonb),

('CONFIRMED', 'Confirmed', 'Registration confirmed', 2, true, false, true, false,
 '["CHECKED_IN", "NO_SHOW", "CANCELLED"]'::jsonb),

('PAYMENT_PENDING', 'Payment Pending', 'Awaiting payment', 3, false, true, true, false,
 '["PAYMENT_COMPLETED", "PAYMENT_FAILED", "CANCELLED"]'::jsonb),

('PAYMENT_COMPLETED', 'Payment Completed', 'Payment received', 4, true, false, true, false,
 '["CONFIRMED", "CHECKED_IN", "CANCELLED"]'::jsonb),

('CANCELLED', 'Cancelled', 'Registration cancelled by user or organizer', 5, false, false, false, true,
 '[]'::jsonb),

('CHECKED_IN', 'Checked In', 'Attendee checked in at event', 6, true, false, false, true,
 '[]'::jsonb),

('NO_SHOW', 'No Show', 'Attendee did not show up', 7, false, false, false, true,
 '[]'::jsonb);

-- ============================================================================
-- 4. PAYMENT STATUSES
-- From PaymentStatus enum (5 values)
-- ============================================================================
INSERT INTO payment_statuses (
    code, name, description, display_order,
    is_successful, allows_refund, is_terminal_state,
    allowed_transitions
) VALUES
('PENDING', 'Pending', 'Payment initiated', 1, false, false, false,
 '["COMPLETED", "FAILED", "CANCELLED"]'::jsonb),

('COMPLETED', 'Completed', 'Payment successful', 2, true, true, false,
 '["REFUNDED", "PARTIALLY_REFUNDED"]'::jsonb),

('FAILED', 'Failed', 'Payment failed', 3, false, false, true,
 '[]'::jsonb),

('REFUNDED', 'Refunded', 'Payment fully refunded', 4, false, false, true,
 '[]'::jsonb),

('CANCELLED', 'Cancelled', 'Payment cancelled', 5, false, false, true,
 '[]'::jsonb);

-- ============================================================================
-- 5. ROLES
-- From UserRole enum (6 values)
-- ============================================================================
INSERT INTO roles (code, name, description, display_order, is_system_role) VALUES
('USER', 'User', 'Standard user with basic permissions', 1, true),
('ORGANIZER', 'Organizer', 'Can create and manage events', 2, true),
('ADMIN', 'Admin', 'Full system access', 3, true),
('MODERATOR', 'Moderator', 'Can moderate content and users', 4, true),
('BUSINESS_OWNER', 'Business Owner', 'Business account with special features', 5, true),
('SUPER_ADMIN', 'Super Admin', 'Highest level system administrator', 6, true);

-- ============================================================================
-- 6. PERMISSIONS (RBAC)
-- Define granular permissions for resources
-- ============================================================================

-- Event Permissions
INSERT INTO permissions (code, name, description, resource, action) VALUES
('EVENT_CREATE', 'Create Event', 'Can create new events', 'Event', 'Create'),
('EVENT_READ', 'Read Event', 'Can view events', 'Event', 'Read'),
('EVENT_UPDATE', 'Update Event', 'Can edit events', 'Event', 'Update'),
('EVENT_DELETE', 'Delete Event', 'Can delete events', 'Event', 'Delete'),
('EVENT_PUBLISH', 'Publish Event', 'Can publish events', 'Event', 'Publish'),

-- User Permissions
('USER_CREATE', 'Create User', 'Can create users', 'User', 'Create'),
('USER_READ', 'Read User', 'Can view users', 'User', 'Read'),
('USER_UPDATE', 'Update User', 'Can edit users', 'User', 'Update'),
('USER_DELETE', 'Delete User', 'Can delete users', 'User', 'Delete'),

-- Payment Permissions
('PAYMENT_CREATE', 'Process Payment', 'Can process payments', 'Payment', 'Create'),
('PAYMENT_READ', 'Read Payment', 'Can view payments', 'Payment', 'Read'),
('PAYMENT_REFUND', 'Refund Payment', 'Can issue refunds', 'Payment', 'Refund'),

-- Registration Permissions
('REGISTRATION_CREATE', 'Create Registration', 'Can register for events', 'Registration', 'Create'),
('REGISTRATION_READ', 'Read Registration', 'Can view registrations', 'Registration', 'Read'),
('REGISTRATION_UPDATE', 'Update Registration', 'Can modify registrations', 'Registration', 'Update'),
('REGISTRATION_CANCEL', 'Cancel Registration', 'Can cancel registrations', 'Registration', 'Cancel'),

-- Admin Permissions
('SYSTEM_SETTINGS_UPDATE', 'Update System Settings', 'Can modify system settings', 'System', 'Update'),
('REFERENCE_DATA_UPDATE', 'Update Reference Data', 'Can modify reference tables', 'ReferenceData', 'Update'),
('USER_ROLE_ASSIGN', 'Assign Roles', 'Can assign roles to users', 'User', 'AssignRole');

-- ============================================================================
-- 7. ROLE PERMISSIONS MAPPING
-- Assign permissions to roles
-- ============================================================================

-- USER role permissions
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id FROM roles r, permissions p
WHERE r.code = 'USER' AND p.code IN (
    'EVENT_READ',
    'REGISTRATION_CREATE',
    'REGISTRATION_READ',
    'REGISTRATION_CANCEL',
    'PAYMENT_READ'
);

-- ORGANIZER role permissions (includes USER permissions)
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id FROM roles r, permissions p
WHERE r.code = 'ORGANIZER' AND p.code IN (
    'EVENT_READ',
    'EVENT_CREATE',
    'EVENT_UPDATE',
    'EVENT_PUBLISH',
    'REGISTRATION_CREATE',
    'REGISTRATION_READ',
    'REGISTRATION_UPDATE',
    'REGISTRATION_CANCEL',
    'PAYMENT_READ',
    'PAYMENT_CREATE'
);

-- MODERATOR role permissions
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id FROM roles r, permissions p
WHERE r.code = 'MODERATOR' AND p.code IN (
    'EVENT_READ',
    'EVENT_UPDATE',
    'EVENT_DELETE',
    'USER_READ',
    'USER_UPDATE',
    'REGISTRATION_READ',
    'REGISTRATION_UPDATE',
    'REGISTRATION_CANCEL'
);

-- ADMIN role permissions (all except SUPER_ADMIN exclusive)
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id FROM roles r, permissions p
WHERE r.code = 'ADMIN' AND p.code NOT IN ('SYSTEM_SETTINGS_UPDATE', 'REFERENCE_DATA_UPDATE');

-- SUPER_ADMIN role permissions (all permissions)
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id FROM roles r, permissions p
WHERE r.code = 'SUPER_ADMIN';

-- BUSINESS_OWNER role permissions (similar to ORGANIZER with extras)
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id FROM roles r, permissions p
WHERE r.code = 'BUSINESS_OWNER' AND p.code IN (
    'EVENT_READ',
    'EVENT_CREATE',
    'EVENT_UPDATE',
    'EVENT_PUBLISH',
    'REGISTRATION_CREATE',
    'REGISTRATION_READ',
    'REGISTRATION_UPDATE',
    'PAYMENT_READ',
    'PAYMENT_CREATE',
    'PAYMENT_REFUND'
);

-- ============================================================================
-- 8. CURRENCIES
-- From Currency enum (6 values)
-- ============================================================================
INSERT INTO currencies (code, name, symbol, display_order) VALUES
('USD', 'US Dollar', '$', 1),
('LKR', 'Sri Lankan Rupee', 'Rs', 2),
('EUR', 'Euro', '€', 3),
('GBP', 'British Pound', '£', 4),
('AUD', 'Australian Dollar', 'A$', 5),
('CAD', 'Canadian Dollar', 'C$', 6);

-- ============================================================================
-- 9. GENDERS
-- From Gender enum (3 values)
-- ============================================================================
INSERT INTO genders (code, name, display_order) VALUES
('MALE', 'Male', 1),
('FEMALE', 'Female', 2),
('PREFER_NOT_TO_SAY', 'Prefer not to say', 3);

-- ============================================================================
-- 10. AGE CATEGORIES
-- From AgeCategory enum (2 values)
-- ============================================================================
INSERT INTO age_categories (code, name, description, display_order) VALUES
('ADULT', 'Adult', 'Age 18 and above', 1),
('CHILD', 'Child', 'Under 18 years old', 2);

-- ============================================================================
-- 11. SIGNUP ITEM CATEGORIES
-- From SignUpItemCategory enum (4 values)
-- ============================================================================
INSERT INTO signup_item_categories (code, name, description, display_order) VALUES
('FOOD', 'Food', 'Food and beverage items', 1),
('SUPPLIES', 'Supplies', 'General supplies and materials', 2),
('VOLUNTEER', 'Volunteer', 'Volunteer roles and tasks', 3),
('EQUIPMENT', 'Equipment', 'Equipment and tools', 4);

-- ============================================================================
-- 12. PRICING TYPES
-- From PricingType enum (3 values)
-- ============================================================================
INSERT INTO pricing_types (code, name, description, display_order) VALUES
('FREE', 'Free', 'No charge', 1),
('PAID', 'Paid', 'Fixed price', 2),
('DONATION', 'Donation', 'Pay what you want', 3);

-- ============================================================================
-- 13. LANGUAGES
-- From hardcoded language list (20 values)
-- ============================================================================
INSERT INTO languages (code, name, native_name, display_order) VALUES
('en', 'English', 'English', 1),
('si', 'Sinhala', 'සිංහල', 2),
('ta', 'Tamil', 'தமிழ்', 3),
('es', 'Spanish', 'Español', 4),
('fr', 'French', 'Français', 5),
('de', 'German', 'Deutsch', 6),
('it', 'Italian', 'Italiano', 7),
('pt', 'Portuguese', 'Português', 8),
('ru', 'Russian', 'Русский', 9),
('zh', 'Chinese', '中文', 10),
('ja', 'Japanese', '日本語', 11),
('ko', 'Korean', '한국어', 12),
('ar', 'Arabic', 'العربية', 13),
('hi', 'Hindi', 'हिन्दी', 14),
('bn', 'Bengali', 'বাংলা', 15),
('ur', 'Urdu', 'اردو', 16),
('ms', 'Malay', 'Bahasa Melayu', 17),
('th', 'Thai', 'ไทย', 18),
('vi', 'Vietnamese', 'Tiếng Việt', 19),
('nl', 'Dutch', 'Nederlands', 20);

-- ============================================================================
-- 14. US STATES
-- From hardcoded states list (50 values)
-- ============================================================================
INSERT INTO states (code, name, country_code, display_order) VALUES
('AL', 'Alabama', 'US', 1),
('AK', 'Alaska', 'US', 2),
('AZ', 'Arizona', 'US', 3),
('AR', 'Arkansas', 'US', 4),
('CA', 'California', 'US', 5),
('CO', 'Colorado', 'US', 6),
('CT', 'Connecticut', 'US', 7),
('DE', 'Delaware', 'US', 8),
('FL', 'Florida', 'US', 9),
('GA', 'Georgia', 'US', 10),
('HI', 'Hawaii', 'US', 11),
('ID', 'Idaho', 'US', 12),
('IL', 'Illinois', 'US', 13),
('IN', 'Indiana', 'US', 14),
('IA', 'Iowa', 'US', 15),
('KS', 'Kansas', 'US', 16),
('KY', 'Kentucky', 'US', 17),
('LA', 'Louisiana', 'US', 18),
('ME', 'Maine', 'US', 19),
('MD', 'Maryland', 'US', 20),
('MA', 'Massachusetts', 'US', 21),
('MI', 'Michigan', 'US', 22),
('MN', 'Minnesota', 'US', 23),
('MS', 'Mississippi', 'US', 24),
('MO', 'Missouri', 'US', 25),
('MT', 'Montana', 'US', 26),
('NE', 'Nebraska', 'US', 27),
('NV', 'Nevada', 'US', 28),
('NH', 'New Hampshire', 'US', 29),
('NJ', 'New Jersey', 'US', 30),
('NM', 'New Mexico', 'US', 31),
('NY', 'New York', 'US', 32),
('NC', 'North Carolina', 'US', 33),
('ND', 'North Dakota', 'US', 34),
('OH', 'Ohio', 'US', 35),
('OK', 'Oklahoma', 'US', 36),
('OR', 'Oregon', 'US', 37),
('PA', 'Pennsylvania', 'US', 38),
('RI', 'Rhode Island', 'US', 39),
('SC', 'South Carolina', 'US', 40),
('SD', 'South Dakota', 'US', 41),
('TN', 'Tennessee', 'US', 42),
('TX', 'Texas', 'US', 43),
('UT', 'Utah', 'US', 44),
('VT', 'Vermont', 'US', 45),
('VA', 'Virginia', 'US', 46),
('WA', 'Washington', 'US', 47),
('WV', 'West Virginia', 'US', 48),
('WI', 'Wisconsin', 'US', 49),
('WY', 'Wyoming', 'US', 50);

-- ============================================================================
-- 15. SYSTEM SETTINGS
-- From hardcoded configuration values
-- ============================================================================
INSERT INTO system_settings (key, value, value_type, description, category) VALUES
('max_event_images', '10', 'integer', 'Maximum number of images per event', 'upload_limits'),
('max_event_videos', '3', 'integer', 'Maximum number of videos per event', 'upload_limits'),
('max_image_size_mb', '5', 'integer', 'Maximum image file size in MB', 'upload_limits'),
('max_video_size_mb', '50', 'integer', 'Maximum video file size in MB', 'upload_limits'),
('default_page_size', '20', 'integer', 'Default pagination page size', 'pagination'),
('max_page_size', '100', 'integer', 'Maximum pagination page size', 'pagination'),
('search_debounce_ms', '300', 'integer', 'Search input debounce delay', 'ui'),
('cache_duration_minutes', '60', 'integer', 'Reference data cache duration', 'performance'),
('session_timeout_minutes', '30', 'integer', 'User session timeout', 'security'),
('password_min_length', '8', 'integer', 'Minimum password length', 'security'),
('max_login_attempts', '5', 'integer', 'Maximum failed login attempts before lockout', 'security'),
('lockout_duration_minutes', '15', 'integer', 'Account lockout duration after max failed logins', 'security'),
('smtp_host', 'smtp.gmail.com', 'string', 'SMTP server host', 'email'),
('smtp_port', '587', 'integer', 'SMTP server port', 'email'),
('from_email', 'noreply@lankaconnect.com', 'string', 'Default sender email address', 'email');

-- ============================================================================
-- 16. FILE TYPE RESTRICTIONS
-- From hardcoded file type arrays
-- ============================================================================
INSERT INTO file_type_restrictions (context, file_extension, mime_type, max_size_bytes) VALUES
-- Event Images
('event_image', '.jpg', 'image/jpeg', 5242880),   -- 5MB
('event_image', '.jpeg', 'image/jpeg', 5242880),
('event_image', '.png', 'image/png', 5242880),
('event_image', '.gif', 'image/gif', 5242880),
('event_image', '.webp', 'image/webp', 5242880),

-- Event Videos
('event_video', '.mp4', 'video/mp4', 52428800),   -- 50MB
('event_video', '.mov', 'video/quicktime', 52428800),
('event_video', '.avi', 'video/x-msvideo', 52428800),
('event_video', '.webm', 'video/webm', 52428800),

-- Profile Avatar
('profile_avatar', '.jpg', 'image/jpeg', 2097152),  -- 2MB
('profile_avatar', '.jpeg', 'image/jpeg', 2097152),
('profile_avatar', '.png', 'image/png', 2097152),
('profile_avatar', '.webp', 'image/webp', 2097152),

-- Documents
('event_document', '.pdf', 'application/pdf', 10485760),  -- 10MB
('event_document', '.doc', 'application/msword', 10485760),
('event_document', '.docx', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document', 10485760);

COMMIT;
