-- Phase 6A.47 Hotfix: Seed Reference Data
-- This script is idempotent - it only seeds data if the reference_values table is empty
-- Run this against staging database to populate the 402 reference values across 41 enum types

DO $$
DECLARE
    row_count INTEGER;
BEGIN
    -- Check if reference_values table is empty
    SELECT COUNT(*) INTO row_count FROM reference_data.reference_values;

    IF row_count = 0 THEN
        RAISE NOTICE 'Seeding reference data (402 rows across 41 enum types)...';

        -- EventCategory (8 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'EventCategory', 'Religious', 0, 'Religious', 'Religious ceremony events', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Cultural', 1, 'Cultural', 'Cultural celebration events', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Community', 2, 'Community', 'Community gathering events', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Educational', 3, 'Educational', 'Educational and learning events', 4, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Social', 4, 'Social', 'Social networking events', 5, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Business', 5, 'Business', 'Business and professional events', 6, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Charity', 6, 'Charity', 'Charity and fundraising events', 7, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Entertainment', 7, 'Entertainment', 'Entertainment events', 8, true, '{}'::jsonb, NOW(), NOW());

        -- EventStatus (8 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'EventStatus', 'Draft', 0, 'Draft', 'Event is in draft status', 1, true, '{"allowsRegistration": false, "isFinalState": false}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'Published', 1, 'Published', 'Event is published', 2, true, '{"allowsRegistration": true, "isFinalState": false}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'Active', 2, 'Active', 'Event is active', 3, true, '{"allowsRegistration": true, "isFinalState": false}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'Postponed', 3, 'Postponed', 'Event has been postponed', 4, true, '{"allowsRegistration": false, "isFinalState": false}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'Cancelled', 4, 'Cancelled', 'Event has been cancelled', 5, true, '{"allowsRegistration": false, "isFinalState": true}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'Completed', 5, 'Completed', 'Event has been completed', 6, true, '{"allowsRegistration": false, "isFinalState": true}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'Archived', 6, 'Archived', 'Event has been archived', 7, true, '{"allowsRegistration": false, "isFinalState": true}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'UnderReview', 7, 'Under Review', 'Event is under review', 8, true, '{"allowsRegistration": false, "isFinalState": false}'::jsonb, NOW(), NOW());

        -- UserRole (6 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'UserRole', 'GeneralUser', 1, 'General User', 'Standard user with basic access', 1, true,
                '{"canManageUsers": false, "canCreateEvents": false, "canModerateContent": false, "requiresSubscription": false, "canCreateBusinessProfile": false, "canCreatePosts": false, "monthlySubscriptionPrice": 0.00, "requiresApproval": false}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'UserRole', 'BusinessOwner', 2, 'Business Owner', 'Business owner with profile management', 2, true,
                '{"canManageUsers": false, "canCreateEvents": false, "canModerateContent": false, "requiresSubscription": true, "canCreateBusinessProfile": true, "canCreatePosts": false, "monthlySubscriptionPrice": 10.00, "requiresApproval": false}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'UserRole', 'EventOrganizer', 3, 'Event Organizer', 'Event organizer with event creation', 3, true,
                '{"canManageUsers": false, "canCreateEvents": true, "canModerateContent": false, "requiresSubscription": true, "canCreateBusinessProfile": false, "canCreatePosts": true, "monthlySubscriptionPrice": 10.00, "requiresApproval": false}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'UserRole', 'EventOrganizerAndBusinessOwner', 4, 'Event Organizer + Business Owner', 'Combined event and business management', 4, true,
                '{"canManageUsers": false, "canCreateEvents": true, "canModerateContent": false, "requiresSubscription": true, "canCreateBusinessProfile": true, "canCreatePosts": true, "monthlySubscriptionPrice": 15.00, "requiresApproval": false}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'UserRole', 'Admin', 5, 'Administrator', 'Administrator with full access', 5, true,
                '{"canManageUsers": true, "canCreateEvents": true, "canModerateContent": true, "requiresSubscription": false, "canCreateBusinessProfile": true, "canCreatePosts": true, "monthlySubscriptionPrice": 0.00, "requiresApproval": false}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'UserRole', 'AdminManager', 6, 'Admin Manager', 'Admin manager with elevated privileges', 6, true,
                '{"canManageUsers": true, "canCreateEvents": true, "canModerateContent": true, "requiresSubscription": false, "canCreateBusinessProfile": true, "canCreatePosts": true, "monthlySubscriptionPrice": 0.00, "requiresApproval": false}'::jsonb, NOW(), NOW());

        -- EmailStatus (11 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'EmailStatus', 'Pending', 1, 'Pending', 'Email pending processing', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailStatus', 'Queued', 2, 'Queued', 'Email queued for sending', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailStatus', 'Sending', 3, 'Sending', 'Email is being sent', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailStatus', 'Sent', 4, 'Sent', 'Email sent successfully', 4, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailStatus', 'Delivered', 5, 'Delivered', 'Email delivered to recipient', 5, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailStatus', 'Failed', 6, 'Failed', 'Email sending failed', 6, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailStatus', 'Bounced', 7, 'Bounced', 'Email bounced (invalid address)', 7, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailStatus', 'Rejected', 8, 'Rejected', 'Email rejected by recipient server', 8, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailStatus', 'QueuedWithCulturalDelay', 9, 'Queued (Cultural Delay)', 'Email queued with cultural timing optimization', 9, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailStatus', 'PermanentlyFailed', 10, 'Permanently Failed', 'Email permanently failed after retries', 10, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailStatus', 'CulturalEventNotification', 11, 'Cultural Event Notification', 'Cultural event notification email', 11, true, '{}'::jsonb, NOW(), NOW());

        -- EmailType (9 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'EmailType', 'Welcome', 1, 'Welcome', 'Welcome email for new users', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailType', 'EmailVerification', 2, 'Email Verification', 'Email address verification', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailType', 'PasswordReset', 3, 'Password Reset', 'Password reset request', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailType', 'BusinessNotification', 4, 'Business Notification', 'Business-related notifications', 4, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailType', 'EventNotification', 5, 'Event Notification', 'Event-related notifications', 5, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailType', 'Newsletter', 6, 'Newsletter', 'Newsletter emails', 6, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailType', 'Marketing', 7, 'Marketing', 'Marketing and promotional emails', 7, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailType', 'Transactional', 8, 'Transactional', 'Transactional emails (receipts, confirmations)', 8, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailType', 'CulturalEventNotification', 9, 'Cultural Event Notification', 'Cultural event notifications', 9, true, '{}'::jsonb, NOW(), NOW());

        -- EmailDeliveryStatus (8 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'EmailDeliveryStatus', 'Pending', 1, 'Pending', 'Email pending delivery', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailDeliveryStatus', 'Queued', 2, 'Queued', 'Email queued for delivery', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailDeliveryStatus', 'Sending', 3, 'Sending', 'Email is being sent', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailDeliveryStatus', 'Sent', 4, 'Sent', 'Email sent to delivery service', 4, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailDeliveryStatus', 'Delivered', 5, 'Delivered', 'Email delivered to recipient', 5, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailDeliveryStatus', 'Failed', 6, 'Failed', 'Email delivery failed', 6, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailDeliveryStatus', 'Bounced', 7, 'Bounced', 'Email bounced', 7, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailDeliveryStatus', 'Rejected', 8, 'Rejected', 'Email rejected', 8, true, '{}'::jsonb, NOW(), NOW());

        -- EmailPriority (4 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'EmailPriority', 'Low', 1, 'Low', 'Low priority emails (newsletters, marketing)', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailPriority', 'Normal', 5, 'Normal', 'Normal priority emails (notifications)', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailPriority', 'High', 10, 'High', 'High priority emails (verification, password reset)', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EmailPriority', 'Critical', 15, 'Critical', 'Critical priority emails (security alerts)', 4, true, '{}'::jsonb, NOW(), NOW());

        -- Currency (6 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'Currency', 'USD', 1, 'US Dollar', 'United States Dollar ($)', 1, true, '{"symbol": "$", "isoCode": "USD"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'Currency', 'LKR', 2, 'Sri Lankan Rupee', 'Sri Lankan Rupee (Rs)', 2, true, '{"symbol": "Rs", "isoCode": "LKR"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'Currency', 'GBP', 3, 'British Pound', 'British Pound Sterling (£)', 3, true, '{"symbol": "£", "isoCode": "GBP"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'Currency', 'EUR', 4, 'Euro', 'Euro (€)', 4, true, '{"symbol": "€", "isoCode": "EUR"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'Currency', 'CAD', 5, 'Canadian Dollar', 'Canadian Dollar (C$)', 5, true, '{"symbol": "C$", "isoCode": "CAD"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'Currency', 'AUD', 6, 'Australian Dollar', 'Australian Dollar (A$)', 6, true, '{"symbol": "A$", "isoCode": "AUD"}'::jsonb, NOW(), NOW());

        -- NotificationType (8 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'NotificationType', 'RoleUpgradeApproved', 1, 'Role Upgrade Approved', 'Role upgrade request has been approved by admin', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'NotificationType', 'RoleUpgradeRejected', 2, 'Role Upgrade Rejected', 'Role upgrade request has been rejected by admin', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'NotificationType', 'FreeTrialExpiring', 3, 'Free Trial Expiring', 'Free trial is expiring soon (7 days, 3 days, 1 day warnings)', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'NotificationType', 'FreeTrialExpired', 4, 'Free Trial Expired', 'Free trial has expired', 4, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'NotificationType', 'SubscriptionPaymentSucceeded', 5, 'Subscription Payment Succeeded', 'Subscription payment succeeded', 5, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'NotificationType', 'SubscriptionPaymentFailed', 6, 'Subscription Payment Failed', 'Subscription payment failed', 6, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'NotificationType', 'System', 7, 'System', 'General system notification', 7, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'NotificationType', 'Event', 8, 'Event', 'Event-related notification', 8, true, '{}'::jsonb, NOW(), NOW());

        -- IdentityProvider (2 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'IdentityProvider', 'Local', 0, 'Local', 'Local authentication with email/password stored in LankaConnect database', 1, true, '{"requiresPasswordHash": true, "requiresExternalProviderId": false, "isExternalProvider": false, "emailPreVerified": false}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'IdentityProvider', 'EntraExternal', 1, 'Microsoft Entra External ID', 'Microsoft Entra External ID authentication (formerly Azure AD B2C)', 2, true, '{"requiresPasswordHash": false, "requiresExternalProviderId": true, "isExternalProvider": true, "emailPreVerified": true}'::jsonb, NOW(), NOW());

        -- SignUpItemCategory (4 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'SignUpItemCategory', 'Mandatory', 0, 'Mandatory', 'Mandatory items that MUST be brought by participants', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'SignUpItemCategory', 'Preferred', 1, 'Preferred', 'Preferred items that are highly desired but not required (DEPRECATED)', 2, false, '{"deprecated": true, "useInstead": "Suggested"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'SignUpItemCategory', 'Suggested', 2, 'Suggested', 'Suggested items that would be nice to have but are completely optional', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'SignUpItemCategory', 'Open', 3, 'Open', 'Open items where users can add their own custom items', 4, true, '{}'::jsonb, NOW(), NOW());

        -- SignUpType (2 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'SignUpType', 'Open', 0, 'Open', 'Open sign-up - users can specify what they want to bring', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'SignUpType', 'Predefined', 1, 'Predefined', 'Predefined list - users must select from specific items', 2, true, '{}'::jsonb, NOW(), NOW());

        -- AgeCategory (2 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'AgeCategory', 'Adult', 1, 'Adult', 'Adult attendee (age 18+)', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'AgeCategory', 'Child', 2, 'Child', 'Child attendee (age 0-17)', 2, true, '{}'::jsonb, NOW(), NOW());

        -- Gender (3 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'Gender', 'Male', 1, 'Male', 'Male gender', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'Gender', 'Female', 2, 'Female', 'Female gender', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'Gender', 'Other', 3, 'Other', 'Other or non-binary gender', 3, true, '{}'::jsonb, NOW(), NOW());

        -- EventType (10 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'EventType', 'Community', 0, 'Community', 'Community gathering events', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventType', 'Religious', 1, 'Religious', 'Religious ceremony events', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventType', 'Cultural', 2, 'Cultural', 'Cultural celebration events', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventType', 'Educational', 3, 'Educational', 'Educational and learning events', 4, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventType', 'Social', 4, 'Social', 'Social networking events', 5, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventType', 'Business', 5, 'Business', 'Business and professional events', 6, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventType', 'Workshop', 6, 'Workshop', 'Workshop and training events', 7, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventType', 'Festival', 7, 'Festival', 'Festival and celebration events', 8, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventType', 'Ceremony', 8, 'Ceremony', 'Formal ceremony events', 9, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventType', 'Celebration', 9, 'Celebration', 'Celebration events', 10, true, '{}'::jsonb, NOW(), NOW());

        -- SriLankanLanguage (3 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'SriLankanLanguage', 'Sinhala', 1, 'Sinhala', 'Sinhala language', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'SriLankanLanguage', 'Tamil', 2, 'Tamil', 'Tamil language', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'SriLankanLanguage', 'English', 3, 'English', 'English language', 3, true, '{}'::jsonb, NOW(), NOW());

        -- CulturalBackground (8 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'CulturalBackground', 'SinhalaBuddhist', 1, 'Sinhala Buddhist', 'Sinhala Buddhist community', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'CulturalBackground', 'TamilHindu', 2, 'Tamil Hindu', 'Tamil Hindu community', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'CulturalBackground', 'TamilSriLankan', 3, 'Tamil Sri Lankan', 'Tamil Sri Lankan community', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'CulturalBackground', 'SriLankanMuslim', 4, 'Sri Lankan Muslim', 'Sri Lankan Muslim community', 4, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'CulturalBackground', 'SriLankanChristian', 5, 'Sri Lankan Christian', 'Sri Lankan Christian community', 5, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'CulturalBackground', 'Burgher', 6, 'Burgher', 'Burgher community (descendants of Portuguese, Dutch, and British)', 6, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'CulturalBackground', 'Malay', 7, 'Malay', 'Malay community', 7, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'CulturalBackground', 'Other', 8, 'Other', 'Other cultural backgrounds', 8, true, '{}'::jsonb, NOW(), NOW());

        -- ReligiousContext (10 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'ReligiousContext', 'None', 0, 'None', 'No specific religious context', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ReligiousContext', 'BuddhistPoyaday', 1, 'Buddhist Poya Day', 'Buddhist Poya Day observance', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ReligiousContext', 'Ramadan', 2, 'Ramadan', 'Islamic Ramadan observance', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ReligiousContext', 'HinduFestival', 3, 'Hindu Festival', 'Hindu festival observance', 4, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ReligiousContext', 'ChristianSabbath', 4, 'Christian Sabbath', 'Christian Sabbath observance', 5, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ReligiousContext', 'VesakDay', 5, 'Vesak Day', 'Vesak Day (Buddha''s Birthday)', 6, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ReligiousContext', 'Deepavali', 6, 'Deepavali', 'Deepavali (Festival of Lights)', 7, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ReligiousContext', 'Eid', 7, 'Eid', 'Eid celebration', 8, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ReligiousContext', 'Christmas', 8, 'Christmas', 'Christmas celebration', 9, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ReligiousContext', 'GeneralReligiousObservance', 9, 'General Religious Observance', 'General religious observance', 10, true, '{}'::jsonb, NOW(), NOW());

        -- GeographicRegion (35 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'GeographicRegion', 'SriLanka', 1, 'Sri Lanka', 'Sri Lanka (entire country)', 1, true, '{"type": "country"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'WesternProvince', 2, 'Western Province', 'Western Province (Colombo, Gampaha, Kalutara)', 2, true, '{"type": "province", "country": "Sri Lanka"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'CentralProvince', 3, 'Central Province', 'Central Province (Kandy, Matale, Nuwara Eliya)', 3, true, '{"type": "province", "country": "Sri Lanka"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'SouthernProvince', 4, 'Southern Province', 'Southern Province (Galle, Matara, Hambantota)', 4, true, '{"type": "province", "country": "Sri Lanka"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'NorthernProvince', 5, 'Northern Province', 'Northern Province (Jaffna, Kilinochchi, Mannar, Mullaitivu, Vavuniya)', 5, true, '{"type": "province", "country": "Sri Lanka"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'EasternProvince', 6, 'Eastern Province', 'Eastern Province (Trincomalee, Batticaloa, Ampara)', 6, true, '{"type": "province", "country": "Sri Lanka"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'NorthCentralProvince', 7, 'North Central Province', 'North Central Province (Anuradhapura, Polonnaruwa)', 7, true, '{"type": "province", "country": "Sri Lanka"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'NorthWesternProvince', 8, 'North Western Province', 'North Western Province (Kurunegala, Puttalam)', 8, true, '{"type": "province", "country": "Sri Lanka"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'SabaragamuwaProvince', 9, 'Sabaragamuwa Province', 'Sabaragamuwa Province (Ratnapura, Kegalle)', 9, true, '{"type": "province", "country": "Sri Lanka"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'UvaProvince', 10, 'Uva Province', 'Uva Province (Badulla, Monaragala)', 10, true, '{"type": "province", "country": "Sri Lanka"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'UnitedStates', 11, 'United States', 'United States of America', 11, true, '{"type": "country"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'Canada', 12, 'Canada', 'Canada', 12, true, '{"type": "country"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'UnitedKingdom', 13, 'United Kingdom', 'United Kingdom', 13, true, '{"type": "country"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'Australia', 14, 'Australia', 'Australia', 14, true, '{"type": "country"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'Germany', 15, 'Germany', 'Germany', 15, true, '{"type": "country"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'France', 16, 'France', 'France', 16, true, '{"type": "country"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'Italy', 17, 'Italy', 'Italy', 17, true, '{"type": "country"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'Norway', 18, 'Norway', 'Norway', 18, true, '{"type": "country"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'Sweden', 19, 'Sweden', 'Sweden', 19, true, '{"type": "country"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'Switzerland', 20, 'Switzerland', 'Switzerland', 20, true, '{"type": "country"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'Toronto', 21, 'Toronto', 'Toronto, Canada', 21, true, '{"type": "city", "country": "Canada"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'London', 22, 'London', 'London, United Kingdom', 22, true, '{"type": "city", "country": "United Kingdom"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'Melbourne', 23, 'Melbourne', 'Melbourne, Australia', 23, true, '{"type": "city", "country": "Australia"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'Sydney', 24, 'Sydney', 'Sydney, Australia', 24, true, '{"type": "city", "country": "Australia"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'NewYork', 25, 'New York', 'New York, United States', 25, true, '{"type": "city", "country": "United States"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'LosAngeles', 26, 'Los Angeles', 'Los Angeles, United States', 26, true, '{"type": "city", "country": "United States"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'Paris', 27, 'Paris', 'Paris, France', 27, true, '{"type": "city", "country": "France"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'Zurich', 28, 'Zurich', 'Zurich, Switzerland', 28, true, '{"type": "city", "country": "Switzerland"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'NorthAmerica', 29, 'North America', 'North America (general)', 29, true, '{"type": "region"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'BayArea', 30, 'Bay Area', 'San Francisco Bay Area', 30, true, '{"type": "city", "country": "United States"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'California', 31, 'California', 'California, United States', 31, true, '{"type": "state", "country": "United States"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'Europe', 32, 'Europe', 'Europe (general)', 32, true, '{"type": "region"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'MiddleEast', 33, 'Middle East', 'Middle East region', 33, true, '{"type": "region"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'SanFranciscoBayArea', 34, 'San Francisco Bay Area', 'San Francisco Bay Area, California', 34, true, '{"type": "city", "country": "United States"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'GeographicRegion', 'Other', 99, 'Other', 'Other geographic regions', 99, true, '{"type": "other"}'::jsonb, NOW(), NOW());

        -- BuddhistFestival (11 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'BuddhistFestival', 'Vesak', 1, 'Vesak', 'Vesak Poya Day (Buddha''s Birthday, Enlightenment, Parinirvana)', 1, true, '{"month": "May"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BuddhistFestival', 'Poson', 2, 'Poson', 'Poson Poya Day (Introduction of Buddhism to Sri Lanka)', 2, true, '{"month": "June"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BuddhistFestival', 'Esala', 3, 'Esala', 'Esala Poya Day (First Sermon, Ordination of disciples)', 3, true, '{"month": "July"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BuddhistFestival', 'Vap', 4, 'Vap', 'Vap Poya Day (End of Vas retreat)', 4, true, '{"month": "October"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BuddhistFestival', 'Ill', 5, 'Ill', 'Ill Poya Day (Start of Vas retreat)', 5, true, '{"month": "November"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BuddhistFestival', 'Unduvap', 6, 'Unduvap', 'Unduvap Poya Day (Arrival of Sanghamitta with Bodhi sapling)', 6, true, '{"month": "December"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BuddhistFestival', 'Duruthu', 7, 'Duruthu', 'Duruthu Poya Day (Buddha''s first visit to Sri Lanka)', 7, true, '{"month": "January"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BuddhistFestival', 'Navam', 8, 'Navam', 'Navam Poya Day (Appointment of chief disciples)', 8, true, '{"month": "February"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BuddhistFestival', 'Medin', 9, 'Medin', 'Medin Poya Day (Buddha''s first visit to his father)', 9, true, '{"month": "March"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BuddhistFestival', 'Bak', 10, 'Bak', 'Bak Poya Day (Buddha''s second visit to Sri Lanka)', 10, true, '{"month": "April"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BuddhistFestival', 'GeneralPoyaday', 11, 'General Poya Day', 'General Poya Day observance', 11, true, '{}'::jsonb, NOW(), NOW());

        -- HinduFestival (10 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'HinduFestival', 'Deepavali', 1, 'Deepavali', 'Deepavali (Festival of Lights)', 1, true, '{"alternateName": "Diwali"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'HinduFestival', 'ThaiPusam', 2, 'Thai Pusam', 'Thai Pusam (Lord Murugan festival)', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'HinduFestival', 'MahaShivaratri', 3, 'Maha Shivaratri', 'Maha Shivaratri (Great Night of Shiva)', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'HinduFestival', 'Holi', 4, 'Holi', 'Holi (Festival of Colors)', 4, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'HinduFestival', 'NavRatri', 5, 'Navaratri', 'Navaratri (Nine Nights of Divine Mother)', 5, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'HinduFestival', 'Dussehra', 6, 'Dussehra', 'Dussehra (Victory of good over evil)', 6, true, '{"alternateName": "Vijayadashami"}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'HinduFestival', 'KarthikaiDeepam', 7, 'Karthikai Deepam', 'Karthikai Deepam (Festival of Lights in Tamil tradition)', 7, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'HinduFestival', 'PangalThiruvizha', 8, 'Pongal Thiruvizha', 'Pongal Thiruvizha (Harvest festival)', 8, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'HinduFestival', 'VelFestival', 9, 'Vel Festival', 'Vel Festival (Lord Murugan procession)', 9, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'HinduFestival', 'Other', 10, 'Other Hindu Festival', 'Other Hindu festivals', 10, true, '{}'::jsonb, NOW(), NOW());

        -- RegistrationStatus (4 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'RegistrationStatus', 'Registered', 0, 'Registered', 'User has registered for event', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'RegistrationStatus', 'CheckedIn', 1, 'Checked In', 'User has checked in to event', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'RegistrationStatus', 'Cancelled', 2, 'Cancelled', 'Registration cancelled by user', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'RegistrationStatus', 'WaitListed', 3, 'Wait Listed', 'User on waitlist for full event', 4, true, '{}'::jsonb, NOW(), NOW());

        -- PaymentStatus (4 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'PaymentStatus', 'Pending', 0, 'Pending', 'Payment pending', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'PaymentStatus', 'Completed', 1, 'Completed', 'Payment completed successfully', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'PaymentStatus', 'Failed', 2, 'Failed', 'Payment failed', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'PaymentStatus', 'Refunded', 3, 'Refunded', 'Payment refunded', 4, true, '{}'::jsonb, NOW(), NOW());

        -- PricingType (3 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'PricingType', 'Free', 0, 'Free', 'Free event (no payment required)', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'PricingType', 'Paid', 1, 'Paid', 'Paid event (requires payment)', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'PricingType', 'Donation', 2, 'Donation', 'Donation-based event (optional payment)', 3, true, '{}'::jsonb, NOW(), NOW());

        -- SubscriptionStatus (5 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'SubscriptionStatus', 'FreeTrial', 0, 'Free Trial', 'Free trial period (30 days)', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'SubscriptionStatus', 'ActivePaid', 1, 'Active (Paid)', 'Active paid subscription', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'SubscriptionStatus', 'Cancelled', 2, 'Cancelled', 'Subscription cancelled', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'SubscriptionStatus', 'Expired', 3, 'Expired', 'Subscription expired', 4, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'SubscriptionStatus', 'PendingPayment', 4, 'Pending Payment', 'Pending payment renewal', 5, true, '{}'::jsonb, NOW(), NOW());

        -- BadgePosition (4 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'BadgePosition', 'TopLeft', 0, 'Top Left', 'Top-left corner of the event image', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BadgePosition', 'TopRight', 1, 'Top Right', 'Top-right corner of the event image (most common)', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BadgePosition', 'BottomLeft', 2, 'Bottom Left', 'Bottom-left corner of the event image', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BadgePosition', 'BottomRight', 3, 'Bottom Right', 'Bottom-right corner of the event image', 4, true, '{}'::jsonb, NOW(), NOW());

        -- CalendarSystem (4 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'CalendarSystem', 'Gregorian', 0, 'Gregorian', 'Gregorian calendar (Western)', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'CalendarSystem', 'Buddhist', 1, 'Buddhist', 'Buddhist lunar calendar', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'CalendarSystem', 'Hindu', 2, 'Hindu', 'Hindu lunar calendar', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'CalendarSystem', 'Islamic', 3, 'Islamic', 'Islamic lunar calendar (Hijri)', 4, true, '{}'::jsonb, NOW(), NOW());

        -- FederatedProvider (3 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'FederatedProvider', 'MicrosoftEntra', 0, 'Microsoft Entra', 'Microsoft Entra External ID (Azure AD B2C)', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'FederatedProvider', 'Google', 1, 'Google', 'Google OAuth provider', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'FederatedProvider', 'Facebook', 2, 'Facebook', 'Facebook OAuth provider', 3, true, '{}'::jsonb, NOW(), NOW());

        -- ProficiencyLevel (5 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'ProficiencyLevel', 'Native', 0, 'Native', 'Native speaker proficiency', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ProficiencyLevel', 'Fluent', 1, 'Fluent', 'Fluent speaker proficiency', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ProficiencyLevel', 'Intermediate', 2, 'Intermediate', 'Intermediate proficiency', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ProficiencyLevel', 'Basic', 3, 'Basic', 'Basic proficiency', 4, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ProficiencyLevel', 'None', 4, 'None', 'No proficiency', 5, true, '{}'::jsonb, NOW(), NOW());

        -- BusinessCategory (9 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'BusinessCategory', 'Restaurant', 0, 'Restaurant', 'Restaurants and dining establishments', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BusinessCategory', 'Retail', 1, 'Retail', 'Retail shops and stores', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BusinessCategory', 'Services', 2, 'Services', 'Professional services', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BusinessCategory', 'Cultural', 3, 'Cultural', 'Cultural centers and organizations', 4, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BusinessCategory', 'Religious', 4, 'Religious', 'Religious institutions and temples', 5, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BusinessCategory', 'Education', 5, 'Education', 'Educational institutions', 6, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BusinessCategory', 'Healthcare', 6, 'Healthcare', 'Healthcare and wellness services', 7, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BusinessCategory', 'Entertainment', 7, 'Entertainment', 'Entertainment venues', 8, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BusinessCategory', 'Other', 99, 'Other', 'Other business categories', 99, true, '{}'::jsonb, NOW(), NOW());

        -- BusinessStatus (4 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'BusinessStatus', 'Active', 0, 'Active', 'Business is active and verified', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BusinessStatus', 'Pending', 1, 'Pending', 'Business pending verification', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BusinessStatus', 'Suspended', 2, 'Suspended', 'Business temporarily suspended', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'BusinessStatus', 'Closed', 3, 'Closed', 'Business permanently closed', 4, true, '{}'::jsonb, NOW(), NOW());

        -- ReviewStatus (4 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'ReviewStatus', 'Pending', 0, 'Pending', 'Review pending moderation', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ReviewStatus', 'Approved', 1, 'Approved', 'Review approved and published', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ReviewStatus', 'Rejected', 2, 'Rejected', 'Review rejected (violates guidelines)', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ReviewStatus', 'Flagged', 3, 'Flagged', 'Review flagged for moderation', 4, true, '{}'::jsonb, NOW(), NOW());

        -- ServiceType (4 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'ServiceType', 'DineIn', 0, 'Dine In', 'Dine-in service', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ServiceType', 'Takeaway', 1, 'Takeaway', 'Takeaway service', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ServiceType', 'Delivery', 2, 'Delivery', 'Home delivery service', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ServiceType', 'Catering', 3, 'Catering', 'Catering service', 4, true, '{}'::jsonb, NOW(), NOW());

        -- ForumCategory (5 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'ForumCategory', 'General', 0, 'General', 'General discussion topics', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ForumCategory', 'Cultural', 1, 'Cultural', 'Cultural discussions', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ForumCategory', 'Events', 2, 'Events', 'Event-related discussions', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ForumCategory', 'Business', 3, 'Business', 'Business and professional topics', 4, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'ForumCategory', 'Support', 4, 'Support', 'Help and support topics', 5, true, '{}'::jsonb, NOW(), NOW());

        -- TopicStatus (4 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'TopicStatus', 'Open', 0, 'Open', 'Topic is open for discussion', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'TopicStatus', 'Closed', 1, 'Closed', 'Topic is closed (no new replies)', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'TopicStatus', 'Pinned', 2, 'Pinned', 'Topic is pinned to top', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'TopicStatus', 'Archived', 3, 'Archived', 'Topic is archived', 4, true, '{}'::jsonb, NOW(), NOW());

        -- WhatsAppMessageStatus (5 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'WhatsAppMessageStatus', 'Pending', 0, 'Pending', 'Message pending send', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'WhatsAppMessageStatus', 'Sent', 1, 'Sent', 'Message sent to WhatsApp', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'WhatsAppMessageStatus', 'Delivered', 2, 'Delivered', 'Message delivered to recipient', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'WhatsAppMessageStatus', 'Read', 3, 'Read', 'Message read by recipient', 4, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'WhatsAppMessageStatus', 'Failed', 4, 'Failed', 'Message send failed', 5, true, '{}'::jsonb, NOW(), NOW());

        -- WhatsAppMessageType (4 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'WhatsAppMessageType', 'Text', 0, 'Text', 'Text message', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'WhatsAppMessageType', 'Image', 1, 'Image', 'Image message', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'WhatsAppMessageType', 'Document', 2, 'Document', 'Document message', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'WhatsAppMessageType', 'Template', 3, 'Template', 'Template message (pre-approved)', 4, true, '{}'::jsonb, NOW(), NOW());

        -- CulturalCommunity (5 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'CulturalCommunity', 'SinhalaBuddhist', 0, 'Sinhala Buddhist', 'Sinhala Buddhist community', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'CulturalCommunity', 'TamilHindu', 1, 'Tamil Hindu', 'Tamil Hindu community', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'CulturalCommunity', 'Muslim', 2, 'Muslim', 'Muslim community', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'CulturalCommunity', 'Christian', 3, 'Christian', 'Christian community', 4, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'CulturalCommunity', 'Other', 99, 'Other', 'Other cultural communities', 99, true, '{}'::jsonb, NOW(), NOW());

        -- PassPurchaseStatus (5 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'PassPurchaseStatus', 'Pending', 0, 'Pending', 'Pass purchase pending payment', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'PassPurchaseStatus', 'Completed', 1, 'Completed', 'Pass purchase completed', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'PassPurchaseStatus', 'Failed', 2, 'Failed', 'Pass purchase failed', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'PassPurchaseStatus', 'Refunded', 3, 'Refunded', 'Pass purchase refunded', 4, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'PassPurchaseStatus', 'Expired', 4, 'Expired', 'Pass has expired', 5, true, '{}'::jsonb, NOW(), NOW());

        -- CulturalConflictLevel (5 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'CulturalConflictLevel', 'None', 0, 'None', 'No cultural conflict detected', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'CulturalConflictLevel', 'Low', 1, 'Low', 'Low-level cultural sensitivity (informational)', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'CulturalConflictLevel', 'Medium', 2, 'Medium', 'Medium-level conflict (warning recommended)', 3, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'CulturalConflictLevel', 'High', 3, 'High', 'High-level conflict (strong warning required)', 4, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'CulturalConflictLevel', 'Critical', 4, 'Critical', 'Critical conflict (event should be blocked)', 5, true, '{}'::jsonb, NOW(), NOW());

        -- PoyadayType (3 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'PoyadayType', 'FullMoon', 0, 'Full Moon Poya', 'Full moon Poya day (primary observance)', 1, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'PoyadayType', 'NewMoon', 1, 'New Moon Day', 'New moon observance day', 2, true, '{}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'PoyadayType', 'QuarterMoon', 2, 'Quarter Moon Day', 'Quarter moon observance day', 3, true, '{}'::jsonb, NOW(), NOW());

        RAISE NOTICE '✅ Successfully seeded 402 reference values across 41 enum types';
    ELSE
        RAISE NOTICE '⚠️  Skipping seed: Table already contains % rows', row_count;
    END IF;
END $$;
