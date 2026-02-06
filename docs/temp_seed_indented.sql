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
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'EmailPriority', 'Low', 1, 'Low', 'Low priority emails (newsletters, marketing)', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'EmailPriority', 'Normal', 5, 'Normal', 'Normal priority emails (notifications)', 2, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'EmailPriority', 'High', 10, 'High', 'High priority emails (verification, password reset)', 3, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'EmailPriority', 'Critical', 15, 'Critical', 'Critical priority emails (security alerts)', 4, true, '{}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'Currency', 'USD', 1, 'US Dollar', 'United States Dollar ($)', 1, true, '{"symbol": "$", "isoCode": "USD"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'Currency', 'LKR', 2, 'Sri Lankan Rupee', 'Sri Lankan Rupee (Rs)', 2, true, '{"symbol": "Rs", "isoCode": "LKR"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'Currency', 'GBP', 3, 'British Pound', 'British Pound Sterling (£)', 3, true, '{"symbol": "£", "isoCode": "GBP"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'Currency', 'EUR', 4, 'Euro', 'Euro (€)', 4, true, '{"symbol": "€", "isoCode": "EUR"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'Currency', 'CAD', 5, 'Canadian Dollar', 'Canadian Dollar (C$)', 5, true, '{"symbol": "C$", "isoCode": "CAD"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'Currency', 'AUD', 6, 'Australian Dollar', 'Australian Dollar (A$)', 6, true, '{"symbol": "A$", "isoCode": "AUD"}'::jsonb, NOW(), NOW());
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
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'IdentityProvider', 'Local', 0, 'Local', 'Local authentication with email/password stored in LankaConnect database', 1, true, '{"requiresPasswordHash": true, "requiresExternalProviderId": false, "isExternalProvider": false, "emailPreVerified": false}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'IdentityProvider', 'EntraExternal', 1, 'Microsoft Entra External ID', 'Microsoft Entra External ID authentication (formerly Azure AD B2C)', 2, true, '{"requiresPasswordHash": false, "requiresExternalProviderId": true, "isExternalProvider": true, "emailPreVerified": true}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'SignUpItemCategory', 'Mandatory', 0, 'Mandatory', 'Mandatory items that MUST be brought by participants', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'SignUpItemCategory', 'Preferred', 1, 'Preferred', 'Preferred items that are highly desired but not required (DEPRECATED)', 2, false, '{"deprecated": true, "useInstead": "Suggested"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'SignUpItemCategory', 'Suggested', 2, 'Suggested', 'Suggested items that would be nice to have but are completely optional', 3, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'SignUpItemCategory', 'Open', 3, 'Open', 'Open items where users can add their own custom items', 4, true, '{}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'SignUpType', 'Open', 0, 'Open', 'Open sign-up - users can specify what they want to bring', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'SignUpType', 'Predefined', 1, 'Predefined', 'Predefined list - users must select from specific items', 2, true, '{}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'AgeCategory', 'Adult', 1, 'Adult', 'Adult attendee (age 18+)', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'AgeCategory', 'Child', 2, 'Child', 'Child attendee (age 0-17)', 2, true, '{}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'Gender', 'Male', 1, 'Male', 'Male gender', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'Gender', 'Female', 2, 'Female', 'Female gender', 2, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'Gender', 'Other', 3, 'Other', 'Other or non-binary gender', 3, true, '{}'::jsonb, NOW(), NOW());
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
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'SriLankanLanguage', 'Sinhala', 1, 'Sinhala', 'Sinhala language', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'SriLankanLanguage', 'Tamil', 2, 'Tamil', 'Tamil language', 2, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'SriLankanLanguage', 'English', 3, 'English', 'English language', 3, true, '{}'::jsonb, NOW(), NOW());
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
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'ReligiousContext', 'None', 0, 'None', 'No specific religious context', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ReligiousContext', 'BuddhistPoyaday', 1, 'Buddhist Poya Day', 'Buddhist Poya Day observance', 2, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ReligiousContext', 'Ramadan', 2, 'Ramadan', 'Islamic Ramadan observance', 3, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ReligiousContext', 'HinduFestival', 3, 'Hindu Festival', 'Hindu festival observance', 4, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ReligiousContext', 'ChristianSabbath', 4, 'Christian Sabbath', 'Christian Sabbath observance', 5, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ReligiousContext', 'VesakDay', 5, 'Vesak Day', 'Vesak Day (Buddha\'s Birthday)', 6, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ReligiousContext', 'Deepavali', 6, 'Deepavali', 'Deepavali (Festival of Lights)', 7, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ReligiousContext', 'Eid', 7, 'Eid', 'Eid celebration', 8, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ReligiousContext', 'Christmas', 8, 'Christmas', 'Christmas celebration', 9, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ReligiousContext', 'GeneralReligiousObservance', 9, 'General Religious Observance', 'General religious observance', 10, true, '{}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    -- Sri Lankan Provinces
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
                    -- Major Diaspora Regions
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
                    -- Major Cities
                    (gen_random_uuid(), 'GeographicRegion', 'Toronto', 21, 'Toronto', 'Toronto, Canada', 21, true, '{"type": "city", "country": "Canada"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'GeographicRegion', 'London', 22, 'London', 'London, United Kingdom', 22, true, '{"type": "city", "country": "United Kingdom"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'GeographicRegion', 'Melbourne', 23, 'Melbourne', 'Melbourne, Australia', 23, true, '{"type": "city", "country": "Australia"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'GeographicRegion', 'Sydney', 24, 'Sydney', 'Sydney, Australia', 24, true, '{"type": "city", "country": "Australia"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'GeographicRegion', 'NewYork', 25, 'New York', 'New York, United States', 25, true, '{"type": "city", "country": "United States"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'GeographicRegion', 'LosAngeles', 26, 'Los Angeles', 'Los Angeles, United States', 26, true, '{"type": "city", "country": "United States"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'GeographicRegion', 'Paris', 27, 'Paris', 'Paris, France', 27, true, '{"type": "city", "country": "France"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'GeographicRegion', 'Zurich', 28, 'Zurich', 'Zurich, Switzerland', 28, true, '{"type": "city", "country": "Switzerland"}'::jsonb, NOW(), NOW()),
                    -- Additional Regions
                    (gen_random_uuid(), 'GeographicRegion', 'NorthAmerica', 29, 'North America', 'North America (general)', 29, true, '{"type": "region"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'GeographicRegion', 'BayArea', 30, 'Bay Area', 'San Francisco Bay Area', 30, true, '{"type": "city", "country": "United States"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'GeographicRegion', 'California', 31, 'California', 'California, United States', 31, true, '{"type": "state", "country": "United States"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'GeographicRegion', 'Europe', 32, 'Europe', 'Europe (general)', 32, true, '{"type": "region"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'GeographicRegion', 'MiddleEast', 33, 'Middle East', 'Middle East region', 33, true, '{"type": "region"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'GeographicRegion', 'SanFranciscoBayArea', 34, 'San Francisco Bay Area', 'San Francisco Bay Area, California', 34, true, '{"type": "city", "country": "United States"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'GeographicRegion', 'Other', 99, 'Other', 'Other geographic regions', 99, true, '{"type": "other"}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'BuddhistFestival', 'Vesak', 1, 'Vesak', 'Vesak Poya Day (Buddha\'s Birthday, Enlightenment, Parinirvana)', 1, true, '{"month": "May"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BuddhistFestival', 'Poson', 2, 'Poson', 'Poson Poya Day (Introduction of Buddhism to Sri Lanka)', 2, true, '{"month": "June"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BuddhistFestival', 'Esala', 3, 'Esala', 'Esala Poya Day (First Sermon, Ordination of disciples)', 3, true, '{"month": "July"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BuddhistFestival', 'Vap', 4, 'Vap', 'Vap Poya Day (End of Vas retreat)', 4, true, '{"month": "October"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BuddhistFestival', 'Ill', 5, 'Ill', 'Ill Poya Day (Start of Vas retreat)', 5, true, '{"month": "November"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BuddhistFestival', 'Unduvap', 6, 'Unduvap', 'Unduvap Poya Day (Arrival of Sanghamitta with Bodhi sapling)', 6, true, '{"month": "December"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BuddhistFestival', 'Duruthu', 7, 'Duruthu', 'Duruthu Poya Day (Buddha\'s first visit to Sri Lanka)', 7, true, '{"month": "January"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BuddhistFestival', 'Navam', 8, 'Navam', 'Navam Poya Day (Appointment of chief disciples)', 8, true, '{"month": "February"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BuddhistFestival', 'Medin', 9, 'Medin', 'Medin Poya Day (Buddha\'s first visit to his father)', 9, true, '{"month": "March"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BuddhistFestival', 'Bak', 10, 'Bak', 'Bak Poya Day (Buddha\'s second visit to Sri Lanka)', 10, true, '{"month": "April"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BuddhistFestival', 'GeneralPoyaday', 11, 'General Poya Day', 'General Poya Day observance', 11, true, '{}'::jsonb, NOW(), NOW());
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
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    -- Sri Lankan Systems
                    (gen_random_uuid(), 'CalendarSystem', 'SriLankanBuddhist', 1, 'Sri Lankan Buddhist Calendar', 'Sri Lankan Buddhist lunar calendar', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CalendarSystem', 'SriLankanTamil', 2, 'Sri Lankan Tamil Calendar', 'Sri Lankan Tamil solar calendar', 2, true, '{}'::jsonb, NOW(), NOW()),
                    -- Indian Calendar Systems
                    (gen_random_uuid(), 'CalendarSystem', 'HinduLunar', 10, 'Hindu Lunar Calendar', 'Hindu lunar calendar system', 3, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CalendarSystem', 'HinduSolar', 11, 'Hindu Solar Calendar', 'Hindu solar calendar system', 4, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CalendarSystem', 'HinduLunisolar', 12, 'Hindu Lunisolar Calendar', 'Hindu lunisolar calendar system', 5, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CalendarSystem', 'BengaliCalendar', 13, 'Bengali Calendar', 'Bengali solar calendar', 6, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CalendarSystem', 'TamilCalendar', 14, 'Tamil Calendar', 'Tamil solar calendar', 7, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CalendarSystem', 'malayalamCalendar', 15, 'Malayalam Calendar', 'Malayalam solar calendar', 8, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CalendarSystem', 'TeluguCalendar', 16, 'Telugu Calendar', 'Telugu lunisolar calendar', 9, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CalendarSystem', 'KannadaCalendar', 17, 'Kannada Calendar', 'Kannada lunisolar calendar', 10, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CalendarSystem', 'GujaratiCalendar', 18, 'Gujarati Calendar', 'Gujarati lunisolar calendar', 11, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CalendarSystem', 'MarathiCalendar', 19, 'Marathi Calendar', 'Marathi lunisolar calendar', 12, true, '{}'::jsonb, NOW(), NOW()),
                    -- Islamic Calendar Systems
                    (gen_random_uuid(), 'CalendarSystem', 'IslamicHijri', 20, 'Islamic Hijri Calendar', 'Islamic Hijri lunar calendar', 13, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CalendarSystem', 'IslamicCalendar', 21, 'Islamic Calendar', 'General Islamic calendar', 14, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CalendarSystem', 'IslamicPakistani', 22, 'Islamic Pakistani Calendar', 'Islamic calendar used in Pakistan', 15, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CalendarSystem', 'IslamicBangladeshi', 23, 'Islamic Bangladeshi Calendar', 'Islamic calendar used in Bangladesh', 16, true, '{}'::jsonb, NOW(), NOW()),
                    -- Sikh Calendar
                    (gen_random_uuid(), 'CalendarSystem', 'NanakshahiCalendar', 30, 'Nanakshahi Calendar', 'Sikh calendar system', 17, true, '{}'::jsonb, NOW(), NOW()),
                    -- Other South Asian Systems
                    (gen_random_uuid(), 'CalendarSystem', 'NepaleseVikramSambat', 40, 'Nepalese Vikram Sambat', 'Nepalese Vikram Sambat calendar', 18, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CalendarSystem', 'BhutaneseBhutaneseCalendar', 41, 'Bhutanese Calendar', 'Bhutanese lunar calendar', 19, true, '{}'::jsonb, NOW(), NOW()),
                    -- Western Integration
                    (gen_random_uuid(), 'CalendarSystem', 'GregorianCalendar', 50, 'Gregorian Calendar', 'Western Gregorian calendar', 20, true, '{}'::jsonb, NOW(), NOW()),
                    -- Multi-Cultural Hybrid
                    (gen_random_uuid(), 'CalendarSystem', 'DiasporaHybrid', 60, 'Diaspora Hybrid Calendar', 'Multi-cultural hybrid calendar for diaspora communities', 21, true, '{}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'FederatedProvider', 'Microsoft', 0, 'Microsoft', 'Direct Microsoft Entra External ID authentication', 1, true, '{"idpClaim": "login.microsoftonline.com"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'FederatedProvider', 'Facebook', 1, 'Facebook', 'Facebook authentication federated through Entra External ID', 2, true, '{"idpClaim": "facebook.com"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'FederatedProvider', 'Google', 2, 'Google', 'Google authentication federated through Entra External ID', 3, true, '{"idpClaim": "google.com"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'FederatedProvider', 'Apple', 3, 'Apple', 'Apple authentication federated through Entra External ID', 4, true, '{"idpClaim": "appleid.apple.com"}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'ProficiencyLevel', 'Basic', 1, 'Basic', 'Basic proficiency - Can understand and use familiar everyday expressions (CEFR A1-A2)', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ProficiencyLevel', 'Intermediate', 2, 'Intermediate', 'Intermediate proficiency - Can handle routine work/social situations (CEFR B1-B2)', 2, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ProficiencyLevel', 'Advanced', 3, 'Advanced', 'Advanced proficiency - Can express ideas fluently and spontaneously (CEFR C1)', 3, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ProficiencyLevel', 'Native', 4, 'Native/Near-Native', 'Native/Near-Native proficiency - Complete mastery of the language (CEFR C2)', 4, true, '{}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'BusinessCategory', 'Restaurant', 0, 'Restaurant', 'Restaurants and dining establishments', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BusinessCategory', 'Grocery', 1, 'Grocery', 'Grocery stores and supermarkets', 2, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BusinessCategory', 'Services', 2, 'Services', 'General services', 3, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BusinessCategory', 'Healthcare', 3, 'Healthcare', 'Healthcare and medical services', 4, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BusinessCategory', 'Education', 4, 'Education', 'Educational institutions and tutoring', 5, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BusinessCategory', 'Technology', 5, 'Technology', 'Technology and IT services', 6, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BusinessCategory', 'Retail', 6, 'Retail', 'Retail stores and shops', 7, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BusinessCategory', 'Tourism', 7, 'Tourism', 'Tourism and travel services', 8, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BusinessCategory', 'RealEstate', 8, 'Real Estate', 'Real estate and property services', 9, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BusinessCategory', 'Finance', 9, 'Finance', 'Financial services and insurance', 10, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BusinessCategory', 'Legal', 10, 'Legal', 'Legal services and attorneys', 11, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BusinessCategory', 'Transportation', 11, 'Transportation', 'Transportation and logistics', 12, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BusinessCategory', 'Entertainment', 12, 'Entertainment', 'Entertainment and events', 13, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BusinessCategory', 'Beauty', 13, 'Beauty', 'Beauty and personal care', 14, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BusinessCategory', 'Fitness', 14, 'Fitness', 'Fitness and wellness', 15, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BusinessCategory', 'Other', 15, 'Other', 'Other business categories', 16, true, '{}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'BusinessStatus', 'Active', 0, 'Active', 'Business listing is active and visible', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BusinessStatus', 'Inactive', 1, 'Inactive', 'Business listing is inactive', 2, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BusinessStatus', 'Suspended', 2, 'Suspended', 'Business listing is suspended', 3, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BusinessStatus', 'PendingApproval', 3, 'Pending Approval', 'Business listing pending admin approval', 4, true, '{}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'ReviewStatus', 'Pending', 0, 'Pending', 'Review pending moderation', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ReviewStatus', 'Approved', 1, 'Approved', 'Review approved and visible', 2, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ReviewStatus', 'Rejected', 2, 'Rejected', 'Review rejected (inappropriate content)', 3, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ReviewStatus', 'Reported', 3, 'Reported', 'Review reported by users', 4, true, '{}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'ServiceType', 'Product', 0, 'Product', 'Physical product sales', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ServiceType', 'Service', 1, 'Service', 'General service offerings', 2, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ServiceType', 'Consultation', 2, 'Consultation', 'Consultation services', 3, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ServiceType', 'Installation', 3, 'Installation', 'Installation services', 4, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ServiceType', 'Maintenance', 4, 'Maintenance', 'Maintenance services', 5, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ServiceType', 'Repair', 5, 'Repair', 'Repair services', 6, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ServiceType', 'Delivery', 6, 'Delivery', 'Delivery services', 7, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ServiceType', 'Rental', 7, 'Rental', 'Rental services', 8, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ServiceType', 'Subscription', 8, 'Subscription', 'Subscription-based services', 9, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ServiceType', 'Training', 9, 'Training', 'Training and education services', 10, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ServiceType', 'Support', 10, 'Support', 'Support services', 11, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ServiceType', 'Other', 11, 'Other', 'Other service types', 12, true, '{}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'ForumCategory', 'General', 0, 'General', 'General discussions', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ForumCategory', 'Jobs', 1, 'Jobs', 'Job postings and career discussions', 2, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ForumCategory', 'Immigration', 2, 'Immigration', 'Immigration and visa discussions', 3, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ForumCategory', 'Housing', 3, 'Housing', 'Housing and accommodation discussions', 4, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ForumCategory', 'Cultural', 4, 'Cultural', 'Cultural and heritage discussions', 5, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ForumCategory', 'Business', 5, 'Business', 'Business and entrepreneurship discussions', 6, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ForumCategory', 'VisaHelp', 6, 'Visa Help', 'Visa and immigration help', 7, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ForumCategory', 'Education', 7, 'Education', 'Education and learning discussions', 8, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ForumCategory', 'HealthcareWellness', 8, 'Healthcare & Wellness', 'Healthcare and wellness discussions', 9, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ForumCategory', 'TravelTourism', 9, 'Travel & Tourism', 'Travel and tourism discussions', 10, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ForumCategory', 'FoodRecipes', 10, 'Food & Recipes', 'Food and recipe discussions', 11, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ForumCategory', 'TechSupport', 11, 'Tech Support', 'Technology support discussions', 12, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ForumCategory', 'LegalAdvice', 12, 'Legal Advice', 'Legal advice discussions', 13, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ForumCategory', 'MarriageFamily', 13, 'Marriage & Family', 'Marriage and family discussions', 14, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ForumCategory', 'StudentsAcademics', 14, 'Students & Academics', 'Student and academic discussions', 15, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'ForumCategory', 'SeniorsCommunity', 15, 'Seniors Community', 'Senior community discussions', 16, true, '{}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'TopicStatus', 'Active', 0, 'Active', 'Topic is active and accepting replies', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'TopicStatus', 'Locked', 1, 'Locked', 'Topic is locked, no new replies allowed', 2, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'TopicStatus', 'Deleted', 2, 'Deleted', 'Topic has been deleted', 3, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'TopicStatus', 'Archived', 3, 'Archived', 'Topic has been archived', 4, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'TopicStatus', 'UnderReview', 4, 'Under Review', 'Topic is under moderator review', 5, true, '{}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'WhatsAppMessageStatus', 'Draft', 1, 'Draft', 'Message is being composed and not yet sent', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'WhatsAppMessageStatus', 'Scheduled', 2, 'Scheduled', 'Message is scheduled for future delivery', 2, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'WhatsAppMessageStatus', 'PendingValidation', 3, 'Pending Validation', 'Message is undergoing cultural appropriateness validation', 3, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'WhatsAppMessageStatus', 'ValidationFailed', 4, 'Validation Failed', 'Message failed cultural validation and requires revision', 4, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'WhatsAppMessageStatus', 'Sending', 5, 'Sending', 'Message is in the process of being sent', 5, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'WhatsAppMessageStatus', 'Sent', 6, 'Sent', 'Message has been successfully sent', 6, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'WhatsAppMessageStatus', 'Delivered', 7, 'Delivered', 'Message has been delivered to recipient', 7, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'WhatsAppMessageStatus', 'Read', 8, 'Read', 'Message has been read by the recipient', 8, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'WhatsAppMessageStatus', 'Failed', 9, 'Failed', 'Message delivery failed and may be eligible for retry', 9, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'WhatsAppMessageStatus', 'Expired', 10, 'Expired', 'Message has reached maximum retry attempts', 10, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'WhatsAppMessageStatus', 'Cancelled', 11, 'Cancelled', 'Message was cancelled before sending (user action)', 11, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'WhatsAppMessageStatus', 'Rejected', 12, 'Rejected', 'Message was rejected by cultural intelligence validation', 12, true, '{}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'WhatsAppMessageType', 'Text', 1, 'Text', 'Regular text message for personal communication', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'WhatsAppMessageType', 'Template', 2, 'Template', 'Template message using pre-approved WhatsApp templates', 2, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'WhatsAppMessageType', 'Media', 3, 'Media', 'Media message with images, audio, or video content', 3, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'WhatsAppMessageType', 'Interactive', 4, 'Interactive', 'Interactive message with buttons or lists', 4, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'WhatsAppMessageType', 'Broadcast', 5, 'Broadcast', 'Broadcast message to multiple recipients simultaneously', 5, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'WhatsAppMessageType', 'EventNotification', 6, 'Event Notification', 'Event notification message with RSVP capabilities', 6, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'WhatsAppMessageType', 'FestivalGreeting', 7, 'Festival Greeting', 'Festival greeting message with cultural appropriateness validation', 7, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'WhatsAppMessageType', 'CommunityAnnouncement', 8, 'Community Announcement', 'Community announcement for diaspora groups', 8, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'WhatsAppMessageType', 'Reminder', 9, 'Reminder', 'Reminder message with cultural timing optimization', 9, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'WhatsAppMessageType', 'LocationBased', 10, 'Location Based', 'Location-based message for diaspora community events', 10, true, '{}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    -- Sri Lankan Communities
                    (gen_random_uuid(), 'CulturalCommunity', 'SriLankanBuddhist', 1, 'Sri Lankan Buddhist', 'Sri Lankan Buddhist community', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'SriLankanTamilHindu', 2, 'Sri Lankan Tamil Hindu', 'Sri Lankan Tamil Hindu community', 2, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'SriLankanMuslim', 3, 'Sri Lankan Muslim', 'Sri Lankan Muslim community', 3, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'SriLankanChristian', 4, 'Sri Lankan Christian', 'Sri Lankan Christian community', 4, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'SriLankanBurgher', 5, 'Sri Lankan Burgher', 'Sri Lankan Burgher community', 5, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'SriLankanMalay', 6, 'Sri Lankan Malay', 'Sri Lankan Malay community', 6, true, '{}'::jsonb, NOW(), NOW()),
                    -- Indian Communities
                    (gen_random_uuid(), 'CulturalCommunity', 'IndianHinduNorth', 10, 'Indian Hindu (North)', 'North Indian Hindu community', 7, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'IndianHinduSouth', 11, 'Indian Hindu (South)', 'South Indian Hindu community', 8, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'IndianHinduBengali', 12, 'Indian Hindu (Bengali)', 'Bengali Hindu community', 9, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'IndianHinduMarathi', 13, 'Indian Hindu (Marathi)', 'Marathi Hindu community', 10, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'IndianHinduGujarati', 14, 'Indian Hindu (Gujarati)', 'Gujarati Hindu community', 11, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'IndianSikh', 15, 'Indian Sikh', 'Indian Sikh community', 12, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'Sikh', 16, 'Sikh', 'General Sikh community', 13, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'Muslim', 17, 'Muslim', 'General Muslim community', 14, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'IndianJain', 18, 'Indian Jain', 'Indian Jain community', 15, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'IndianChristian', 19, 'Indian Christian', 'Indian Christian community', 16, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'IndianMuslim', 20, 'Indian Muslim', 'Indian Muslim community', 17, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'IndianBuddhist', 21, 'Indian Buddhist', 'Indian Buddhist community', 18, true, '{}'::jsonb, NOW(), NOW()),
                    -- Pakistani Communities
                    (gen_random_uuid(), 'CulturalCommunity', 'PakistaniSunniMuslim', 30, 'Pakistani Sunni Muslim', 'Pakistani Sunni Muslim community', 19, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'PakistaniShiaMuslim', 31, 'Pakistani Shia Muslim', 'Pakistani Shia Muslim community', 20, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'PakistaniSikh', 32, 'Pakistani Sikh', 'Pakistani Sikh community', 21, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'PakistaniChristian', 33, 'Pakistani Christian', 'Pakistani Christian community', 22, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'PakistaniHindu', 34, 'Pakistani Hindu', 'Pakistani Hindu community', 23, true, '{}'::jsonb, NOW(), NOW()),
                    -- Bangladeshi Communities
                    (gen_random_uuid(), 'CulturalCommunity', 'BangladeshiSunniMuslim', 40, 'Bangladeshi Sunni Muslim', 'Bangladeshi Sunni Muslim community', 24, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'BangladeshiShiaMuslim', 41, 'Bangladeshi Shia Muslim', 'Bangladeshi Shia Muslim community', 25, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'BangladeshiHindu', 42, 'Bangladeshi Hindu', 'Bangladeshi Hindu community', 26, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'BangladeshiBuddhist', 43, 'Bangladeshi Buddhist', 'Bangladeshi Buddhist community', 27, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'BangladeshiChristian', 44, 'Bangladeshi Christian', 'Bangladeshi Christian community', 28, true, '{}'::jsonb, NOW(), NOW()),
                    -- Other South Asian
                    (gen_random_uuid(), 'CulturalCommunity', 'NepaleseHindu', 50, 'Nepalese Hindu', 'Nepalese Hindu community', 29, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'NepalesesBuddhist', 51, 'Nepalese Buddhist', 'Nepalese Buddhist community', 30, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'BhutaneseBuddhist', 52, 'Bhutanese Buddhist', 'Bhutanese Buddhist community', 31, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'MaldivianMuslim', 53, 'Maldivian Muslim', 'Maldivian Muslim community', 32, true, '{}'::jsonb, NOW(), NOW()),
                    -- Cross-Cultural Blended
                    (gen_random_uuid(), 'CulturalCommunity', 'MultiCulturalSouthAsian', 60, 'Multi-Cultural South Asian', 'Multi-cultural South Asian community', 33, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalCommunity', 'DiasporaBlended', 61, 'Diaspora Blended', 'Blended diaspora community', 34, true, '{}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'PassPurchaseStatus', 'Pending', 0, 'Pending', 'Pass purchase pending payment', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'PassPurchaseStatus', 'Confirmed', 1, 'Confirmed', 'Pass purchase confirmed', 2, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'PassPurchaseStatus', 'Cancelled', 2, 'Cancelled', 'Pass purchase cancelled', 3, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'PassPurchaseStatus', 'Refunded', 3, 'Refunded', 'Pass purchase refunded', 4, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'PassPurchaseStatus', 'Used', 4, 'Used', 'Pass has been fully used', 5, true, '{}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'CulturalConflictLevel', 'None', 0, 'None', 'No cultural conflict', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalConflictLevel', 'Low', 1, 'Low', 'Low cultural conflict', 2, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalConflictLevel', 'Medium', 2, 'Medium', 'Medium cultural conflict', 3, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'CulturalConflictLevel', 'High', 3, 'High', 'High cultural conflict', 4, true, '{}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'PoyadayType', 'Duruthu', 0, 'Duruthu Poya', 'Duruthu Poya Day (January)', 1, true, '{"month": "January"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'PoyadayType', 'Navam', 1, 'Navam Poya', 'Navam Poya Day (February)', 2, true, '{"month": "February"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'PoyadayType', 'Madin', 2, 'Madin Poya', 'Madin Poya Day (March)', 3, true, '{"month": "March"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'PoyadayType', 'Bak', 3, 'Bak Poya', 'Bak Poya Day (April)', 4, true, '{"month": "April"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'PoyadayType', 'Vesak', 4, 'Vesak Poya', 'Vesak Poya Day (May)', 5, true, '{"month": "May"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'PoyadayType', 'Poson', 5, 'Poson Poya', 'Poson Poya Day (June)', 6, true, '{"month": "June"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'PoyadayType', 'Esala', 6, 'Esala Poya', 'Esala Poya Day (July)', 7, true, '{"month": "July"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'PoyadayType', 'Nikini', 7, 'Nikini Poya', 'Nikini Poya Day (August)', 8, true, '{"month": "August"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'PoyadayType', 'Binara', 8, 'Binara Poya', 'Binara Poya Day (September)', 9, true, '{"month": "September"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'PoyadayType', 'Vap', 9, 'Vap Poya', 'Vap Poya Day (October)', 10, true, '{"month": "October"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'PoyadayType', 'Il', 10, 'Il Poya', 'Il Poya Day (November)', 11, true, '{"month": "November"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'PoyadayType', 'Unduvap', 11, 'Unduvap Poya', 'Unduvap Poya Day (December)', 12, true, '{"month": "December"}'::jsonb, NOW(), NOW());
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'BadgePosition', 'TopLeft', 0, 'Top Left', 'Top-left corner of the event image', 1, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BadgePosition', 'TopRight', 1, 'Top Right', 'Top-right corner of the event image (most common for promotional badges)', 2, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BadgePosition', 'BottomLeft', 2, 'Bottom Left', 'Bottom-left corner of the event image', 3, true, '{}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'BadgePosition', 'BottomRight', 3, 'Bottom Right', 'Bottom-right corner of the event image', 4, true, '{}'::jsonb, NOW(), NOW());
