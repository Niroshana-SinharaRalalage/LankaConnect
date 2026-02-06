-- ================================================
-- RCA DIAGNOSTICS: User Registration 400 Error
-- Phase 1: Database State Verification
-- ================================================

-- CHECK 1: Verify migration status
-- Expected: Should see Phase6A53Fix3 migration
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%Phase6A53%'
ORDER BY "MigrationId" DESC;

-- CHECK 2: Current email template state
-- Expected: Should NOT have decorative stars ✦
SELECT
    "Id",
    "Type",
    LENGTH("HtmlTemplate") as template_length,
    CASE
        WHEN "HtmlTemplate" LIKE '%✦%' THEN 'OLD_TEMPLATE_WITH_STARS'
        WHEN "HtmlTemplate" LIKE '%UserName%' THEN 'HAS_USERNAME_PLACEHOLDER'
        WHEN "HtmlTemplate" LIKE '%VerificationLink%' THEN 'HAS_VERIFICATION_LINK'
        ELSE 'UNKNOWN_STATE'
    END as template_state,
    "Subject",
    "UpdatedAt"
FROM "EmailTemplates"
WHERE "Type" = 1  -- EmailVerification type
ORDER BY "UpdatedAt" DESC;

-- CHECK 3: Recent user registration attempts
-- Expected: Users created in last 24 hours with tokens
SELECT
    u."Id",
    u."Email",
    u."UserName",
    u."IsEmailVerified",
    u."EmailVerificationToken" IS NOT NULL as has_token,
    u."EmailVerificationTokenExpiresAt",
    u."CreatedAt",
    EXTRACT(EPOCH FROM (NOW() - u."CreatedAt"))/60 as minutes_since_creation
FROM "Users" u
WHERE u."CreatedAt" >= NOW() - INTERVAL '24 hours'
ORDER BY u."CreatedAt" DESC;

-- CHECK 4: Email verification status for recent users
-- Expected: Tokens generated but emails not sent?
SELECT
    u."Email",
    u."IsEmailVerified",
    u."EmailVerificationToken" IS NOT NULL as has_token,
    u."EmailVerificationTokenExpiresAt",
    r."Name" as role_name,
    u."CreatedAt"
FROM "Users" u
LEFT JOIN "UserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "Roles" r ON ur."RoleId" = r."Id"
WHERE u."CreatedAt" >= NOW() - INTERVAL '24 hours'
ORDER BY u."CreatedAt" DESC;

-- CHECK 5: Email template version history
-- Expected: Should see multiple UpdatedAt timestamps if migration ran
SELECT
    "Type",
    "Subject",
    "UpdatedAt",
    "CreatedAt",
    CASE
        WHEN "HtmlTemplate" LIKE '%✦%' THEN 'Contains decorative stars (OLD)'
        ELSE 'Clean template (NEW)'
    END as version
FROM "EmailTemplates"
WHERE "Type" = 1
ORDER BY "UpdatedAt" DESC
LIMIT 5;

-- CHECK 6: Reference data integrity
-- Expected: EmailVerification enum should exist
SELECT
    "EnumType",
    "EnumValue",
    "EnumName",
    "Description"
FROM "ReferenceData"
WHERE "EnumType" = 'EmailTemplateType'
ORDER BY "EnumValue";
